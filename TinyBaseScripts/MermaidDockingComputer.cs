#pragma warning disable CS0162 // Unreachable code detected
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;

using Sandbox.ModAPI.Ingame;

using SpaceEngineers.Game.ModAPI.Ingame;


namespace IngameScript
{
    partial class Program
    {

        public class MermaidDockingComputer
        {

            //INSTRUCTIONS
            // You need an exterior vent to detect if we're in atmo.
            // You need a prog block, cockpit, or cryo pod to detect if we're in gravity.
            // If either of those are missing, we're assuming that we *are* in the thing.
            #region User-changeable settings
            //const string ExteriorVentNameSubstring = "Exterior Vent";

            const bool ManageReactors = true;
            const bool RechargeTanksWhenDockingToBase = true;
            const bool OnlyUseHydrosInGravity = true;
            const bool OnlyUseIonsInSpace = false;
            const bool EverTurnOnHydros = true;
            const bool KeepOneBatteryOnSoTurnOnScriptCanRun = true;
            const bool ManageOreDetectors = true;
            const bool TurnOnHydrogenEnginesOnDisconnect = false;
            #endregion User-changeable settings

            #region Don't touch unless you have to, may be necessary to make modded thrusters work!
            readonly IReadOnlyList<float> hydrogenThrusterMaxThrusts = new List<float> { 7200000, 1080000, 98400, 480000 }.AsReadOnly();
            readonly IReadOnlyList<float> ionThrusterMaxThrusts = new List<float> { 4320000, 345600, 172800, 14400 }.AsReadOnly();
            readonly IReadOnlyList<float> atmoThrusterMaxThrusts = new List<float> { 6480000, 648000, 576000, 96000 }.AsReadOnly();
            #endregion



            //Considering doing later:
            // * Look at 'significant' levels of gravity or atmosphere
            // * Consider tags for exceptions, esp. for lights and stuff

            #region Do not touch!
            private IMyGridTerminalSystem GridTerminalSystem { get; }
            private IMyProgrammableBlock Me { get; }
            IMyGridProgramRuntimeInfo Runtime { get; }
            private StringBuilder Output { get; set; }

            private readonly Action<string> Echo;

            private bool ConnectedToStationBefore { get; set; } = false;
            private bool ConnectedToStationNow { get; set; } = false;
            #endregion Do not touch!

            public MermaidDockingComputer(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime, Action<string> echo)
            {
                GridTerminalSystem = gridTerminalSystem;
                Me = me;
                Runtime = runtime;
                Echo = echo;
                Output = new StringBuilder();

                ConnectedToStationBefore = IsConnectedToStation();
                ConnectedToStationNow = ConnectedToStationBefore;
                Output.AppendLine($"On load: ConnectedToStation: {ConnectedToStationBefore}");

                Echo(Output.ToString());


                IMyTextSurface textDisplay = Me.GetSurface(0);
            }

            public string Execute()
            {
                Output = new StringBuilder();

                CheckConnectionState();

                return Output.ToString();
            }

            public void CheckConnectionState()
            {
                Output.AppendLine("Checking connection state...");
                ConnectedToStationNow = IsConnectedToStation();
                Output.AppendLine($"Before: {ConnectedToStationBefore}; Now: {ConnectedToStationNow}");

                try
                {
                    if (!ConnectedToStationBefore && ConnectedToStationNow)
                    {
                        Output.AppendLine("Connected!");
                        OnConnectedToStation();
                    }
                    else if (ConnectedToStationBefore && !ConnectedToStationNow)
                    {
                        Output.AppendLine("Disconnected!");
                        OnDisconnectedFromStation();
                    }
                    else
                    {
                        Output.AppendLine($"ConnectedToStationBefore && ConnectedToStationNow: {!ConnectedToStationBefore && ConnectedToStationNow}");
                        Output.AppendLine($"ConnectedToStationBefore && !ConnectedToStationNow: {ConnectedToStationBefore && !ConnectedToStationNow}");
                        Output.AppendLine("No change!");
                    }
                }
                finally
                {
                    ConnectedToStationBefore = ConnectedToStationNow;
                }
                Output.AppendLine("Done checking connection state...");
            }

            private void OnConnectedToStation()
            {
                TurnOffHydrogenEngines();
                RechargeBatteries();
                TurnOffHydros();
                TurnOffIons();
                TurnOffAtmos();
                TurnOffGyros();
                TurnOffSpotlights();
                LockLandingGears();

                if (ManageReactors)
                {
                    TurnOffReactors();
                }

                if (RechargeTanksWhenDockingToBase)
                {
                    RefillTanks();
                }

                if (ManageOreDetectors)
                {
                    TurnOffOreDetectors();
                }
            }

            private void OnDisconnectedFromStation()
            {
                bool isInAtmo = IsInAtmo();
                bool isInGravity = IsInGravity();

                SetAutoBatteries();
                SetToUsableTanks();
                TurnOnGyros();
                TurnOnSpotlights();
                UnlockLandingGears();

                if (ManageReactors)
                {
                    TurnOnReactors();
                }

                if (TurnOnHydrogenEnginesOnDisconnect)
                {
                    //#pragma warning disable CS0162 // Unreachable code detected
                    TurnOnHydrogenEngines();
                    //#pragma warning restore CS0162 // Unreachable code detected
                }

                if (!OnlyUseHydrosInGravity || isInGravity)
                {
                    if (EverTurnOnHydros)
                    {
                        TurnOnHydros();
                    }
                }

                if (isInAtmo)
                {
                    TurnOnAtmos();
                }

                if (!isInAtmo || !OnlyUseIonsInSpace)
                {
                    TurnOnIons();
                }

                if (ManageOreDetectors)
                {
                    TurnOnOreDetectors();
                }
            }

            private void TurnOffHydrogenEngines()
            {
                List<IMyGasGenerator> hydrogenEngines = new List<IMyGasGenerator>();
                GridTerminalSystem.GetBlocksOfType(hydrogenEngines);
                hydrogenEngines.ForEach(engine => engine.Enabled = false);
            }

            private void RechargeBatteries()
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType(batteries);

                IMyBatteryBlock batteryToKeepOn = null;
                if (KeepOneBatteryOnSoTurnOnScriptCanRun)
                {
                    IOrderedEnumerable<IMyBatteryBlock> batteryBlocksOrderedByPower = batteries
                        .OrderBy(batteryToConsiderKeepingOn => batteryToConsiderKeepingOn.MaxStoredPower)
                        .ThenBy(batteryToConsiderKeepingOn => batteryToConsiderKeepingOn.CurrentStoredPower);

                    IMyBatteryBlock batteryWithLeastPowerThatIsNotDead = batteryBlocksOrderedByPower.LastOrDefault(batteryWithLeastPower => batteryWithLeastPower.CurrentStoredPower > 0);

                    batteryToKeepOn = batteryWithLeastPowerThatIsNotDead ?? batteryBlocksOrderedByPower.LastOrDefault();
                }

                batteries = batteries.Where(battery => battery != batteryToKeepOn).ToList();
                batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Recharge);

                batteryToKeepOn.ChargeMode = ChargeMode.Auto;
            }

            private void TurnOffHydros()
            {
                GetHydrogenThrusters().ForEach(thruster => thruster.Enabled = false);
            }

            private void TurnOffIons()
            {
                GetIonThrusters().ForEach(thruster => thruster.Enabled = false);
            }

            private void TurnOffAtmos()
            {
                GetAtmoThrusters().ForEach(thruster => thruster.Enabled = false);
            }

            private void TurnOffGyros()
            {
                List<IMyGyro> gyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(gyros);
                gyros.ForEach(gyro => gyro.Enabled = false);
            }

            private void LockLandingGears()
            {
                List<IMyLandingGear> gears = new List<IMyLandingGear>();
                GridTerminalSystem.GetBlocksOfType(gears);
                gears.ForEach(gear => gear.Lock());
            }

            private void TurnOffSpotlights()
            {
                List<IMyReflectorLight> exteriorLights = new List<IMyReflectorLight>();
                GridTerminalSystem.GetBlocksOfType(exteriorLights);
                exteriorLights.ForEach(light => light.Enabled = false);
            }

            private void TurnOffReactors()
            {
                List<IMyReactor> reactors = new List<IMyReactor>();
                GridTerminalSystem.GetBlocksOfType(reactors);

                reactors.ForEach(reactor => reactor.Enabled = false);
            }

            private void RefillTanks()
            {
                List<IMyGasTank> tanks = new List<IMyGasTank>();
                GridTerminalSystem.GetBlocksOfType(tanks);

                tanks.ForEach(tank => tank.Stockpile = true);
            }

            private void TurnOffOreDetectors()
            {
                List<IMyOreDetector> detectors = new List<IMyOreDetector>();
                GridTerminalSystem.GetBlocksOfType(detectors);

                detectors.ForEach(detector => detector.Enabled = false);
            }

            private void SetAutoBatteries()
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType(batteries);

                batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Recharge);
            }

            private void SetToUsableTanks()
            {
                List<IMyGasTank> tanks = new List<IMyGasTank>();
                GridTerminalSystem.GetBlocksOfType(tanks);

                tanks.ForEach(tank => tank.Stockpile = false);
            }

            private void TurnOnGyros()
            {
                List<IMyGyro> gyros = new List<IMyGyro>();
                GridTerminalSystem.GetBlocksOfType(gyros);
                gyros.ForEach(gyro => gyro.Enabled = true);
            }

            private void UnlockLandingGears()
            {
                List<IMyLandingGear> gears = new List<IMyLandingGear>();
                GridTerminalSystem.GetBlocksOfType(gears);
                gears.ForEach(gear => gear.Unlock());
            }

            private void TurnOnSpotlights()
            {
                List<IMyReflectorLight> exteriorLights = new List<IMyReflectorLight>();
                GridTerminalSystem.GetBlocksOfType(exteriorLights);
                exteriorLights.ForEach(light => light.Enabled = true);
            }

            private void TurnOnReactors()
            {
                List<IMyReactor> reactors = new List<IMyReactor>();
                GridTerminalSystem.GetBlocksOfType(reactors);

                reactors.ForEach(reactor => reactor.Enabled = true);
            }

            private void TurnOnHydrogenEngines()
            {
                List<IMyGasGenerator> hydrogenEngines = new List<IMyGasGenerator>();
                GridTerminalSystem.GetBlocksOfType(hydrogenEngines);
                hydrogenEngines.ForEach(engine => engine.Enabled = true);
            }

            private void TurnOnHydros()
            {
                GetHydrogenThrusters().ForEach(thruster => thruster.Enabled = true);
            }

            private void TurnOnIons()
            {
                GetIonThrusters().ForEach(thruster => thruster.Enabled = true);
            }

            private void TurnOnAtmos()
            {
                GetAtmoThrusters().ForEach(thruster => thruster.Enabled = true);
            }

            private void TurnOnOreDetectors()
            {
                List<IMyOreDetector> detectors = new List<IMyOreDetector>();
                GridTerminalSystem.GetBlocksOfType(detectors);

                detectors.ForEach(detector => detector.Enabled = true);
            }


            List<IMyThrust> GetHydrogenThrusters()
            {
                return GetThrustersWithMaxThrustInRange(hydrogenThrusterMaxThrusts);
            }

            List<IMyThrust> GetIonThrusters()
            {
                return GetThrustersWithMaxThrustInRange(ionThrusterMaxThrusts);
            }

            List<IMyThrust> GetAtmoThrusters()
            {
                return GetThrustersWithMaxThrustInRange(atmoThrusterMaxThrusts);
            }

            List<IMyThrust> GetThrustersWithMaxThrustInRange(IReadOnlyList<float> range)
            {
                List<IMyThrust> applicableThrusters = new List<IMyThrust>();
                GridTerminalSystem.GetBlocksOfType(applicableThrusters, thruster =>
                    range.Any(applicableMaxThrustValue => thruster.MaxThrust >= applicableMaxThrustValue - 5 && thruster.MaxThrust <= applicableMaxThrustValue + 1));
                return applicableThrusters;
            }

            private bool IsConnectedToStation()
            {
                List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                GridTerminalSystem.GetBlocksOfType(connectors);

                foreach (IMyShipConnector connector in connectors)
                {
                    if ((connector?.OtherConnector?.CubeGrid?.IsStatic ?? false)
                        && connector.Status == MyShipConnectorStatus.Connected)
                    {
                        Output.AppendLine($"YES - {connector.CustomName}");
                        Output.AppendLine($"Static: {connector?.OtherConnector?.CubeGrid?.IsStatic}");
                        Output.AppendLine($"Status: {connector.Status}");
                        return true;
                    }
                    Output.AppendLine($"NO - {connector.CustomName}");

                }

                return false;
            }

            private bool IsInAtmo()
            {
                List<IMyAirVent> exteriorVents = new List<IMyAirVent>();
                GridTerminalSystem.GetBlocksOfType(exteriorVents, ExteriorVentsOnly);

                if (exteriorVents.Count == 0)
                {
                    Output.AppendLine("Unable to identify if ship is in atmo - there's no external vent. Assuming that there is, to minimize chances of crashing.");
                    return true;
                }
                else
                {
                    Output.AppendLine($"{exteriorVents.Count} exterior vents detected.");
                }

                return exteriorVents.First().GetOxygenLevel() > 0;
            }

            private bool IsInGravity()
            {
                List<IMyShipController> blocksIdentifyingGravity = new List<IMyShipController>();
                GridTerminalSystem.GetBlocksOfType(blocksIdentifyingGravity);

                if (blocksIdentifyingGravity.Count == 0)
                {
                    Output.Append($"Unable to identify presence of gravity - no {nameof(IMyShipController)} (cockpit, remote control, cryopod) found! Assuming that we ARE in gravity to minimize the chances of a crash.");
                    return true;
                }

                return blocksIdentifyingGravity.First().GetNaturalGravity().Sum > 0;
            }

            private bool ExteriorVentsOnly(IMyAirVent ventToConsiderAsExternal)
            {
                return !ventToConsiderAsExternal.CanPressurize;
            }
        }
    }
}


//MaxThrust:
//Large grid:
//  Large Warfare Ion: 4320000
//  Small Ion: 345600 (345.6 kN, double check that math some time)
//  Large Hydro: 
//  Small hydro:  1.08 MN
//  Large atmo: 6480000 6.48 MN
//  Small atmo: 648000 648 kN
// 
//Small grid:
//  Small ion: 14400 14.4 kN
//  Large ion: 172800 172.8 kN
//  Small hydro:  98.4 kN
//  Large hydro:  480 kN
//  Small atmo: 96000 96 kN
//  Large atmo: 576000 576 kN
//  
//  
//  
//


#pragma warning restore CS0162 // Unreachable code detected

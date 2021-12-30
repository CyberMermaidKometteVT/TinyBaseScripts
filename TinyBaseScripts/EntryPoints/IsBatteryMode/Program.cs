using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript.EntryPoints.IsBatteryMoe
{
    class Program : MyGridProgram
    {
        bool HasBlackjack { get { return true; } }
        bool HasHookers { get { return true; } }
        bool SkipOtherGrids { get { return true; } }
        bool RequireWorking { get { return true; } }
        string BatteriesToCheckSubstring { get { return "[BatteryStatusCheck]"; } }
        string PrefixText { get { return "Battery:         "; } }
        string TargetLcdName { get { return "Roderick Text Panel Selene IsBatteryMode output"; } }

        ChargeMode modeToCheckFor = ChargeMode.Auto;
        //ChargeMode modeToCheckFor = ChargeMode.Discharge;
        //ChargeMode modeToCheckFor = ChargeMode.Recharge;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateType)
        {
            IMyTextSurface outputScreen = GridTerminalSystem.GetBlockWithName(TargetLcdName) as IMyTextPanel;

            if (outputScreen == null)
            {
                throw new InvalidOperationException($"{nameof(IMyTextSurface)} named \"{TargetLcdName}\" not found.");
            }

            List<IMyBatteryBlock> matchingBatteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(matchingBatteries, DoesBlockMatch);

            if (SkipOtherGrids)
            {
                matchingBatteries.RemoveAll(battery => battery.CubeGrid != Me.CubeGrid);
            }

            if (!matchingBatteries.Any())
            {
                outputScreen.WriteText($"{PrefixText} not found!");
                return;
            }


            var failingBatteries = matchingBatteries.Where(IsBatteryInInvalidState);

            if (failingBatteries.Any())
            {
                outputScreen.WriteText($"{PrefixText} {failingBatteries.Count()}/{matchingBatteries.Count()} Not Ready");
                return;
            }

            outputScreen.WriteText($"{PrefixText}           Ready");
        }

        private bool DoesBlockMatch(IMyTerminalBlock target)
        {
            bool matches = target.CustomName.ToUpper().Contains(BatteriesToCheckSubstring.ToUpper());
            //            Echo($"Considering battery: {target.CustomName}.  matches={matches}.");
            return matches;
        }

        private bool IsBatteryInInvalidState(IMyBatteryBlock target)
        {
            bool isInCorrectChargeMode = target.ChargeMode == modeToCheckFor;
            bool isWorkingValid = target.IsWorking || !RequireWorking;
            return !isInCorrectChargeMode || !isWorkingValid;
        }


    }
}

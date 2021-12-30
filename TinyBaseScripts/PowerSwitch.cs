//NOTE: SO FAR THIS IS TOTALLY UNTESTED BUT SELENE WANTED A STARTING POINT THE NEXT TIME SHE HAD A HANKERING FOR IT!

using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PowerSwitch
        {
            private IMyGridTerminalSystem GridTerminalSystem { get; }
            private IMyProgrammableBlock Me { get; }

            public PowerSwitch(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me)
            {
                GridTerminalSystem = gridTerminalSystem;
                Me = me;
            }

            public void Execute(string argument)
            {
                string argumentUpper = argument.ToUpper();
                if (argumentUpper != "ON" && argumentUpper != "OFF")
                {
                    throw new ArgumentException($"Invalid argument to {nameof(PowerSwitch)}, the only valid values are 'ON' and 'OFF' but we got '{argument}'.");
                }

                //TODO: Add other things that get switched on too!
                //      Also consider using a [MainPower] tag or something instead of just grabbing ALL batteries!
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType(batteries);

                if (argument.ToUpper() == "ON")
                {
                    foreach (IMyBatteryBlock battery in batteries)
                    {
                        battery.ChargeMode = ChargeMode.Discharge;
                    }
                    return;
                }
                //(argument.ToUpper() == "OFF")
                
                foreach (IMyBatteryBlock battery in batteries)
                {
                    battery.ChargeMode = ChargeMode.Recharge;
                }


            }
        }
    }
}

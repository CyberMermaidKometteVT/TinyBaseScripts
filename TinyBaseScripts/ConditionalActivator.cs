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
        public class ConditionalActivator
        {
            private IMyGridTerminalSystem GridTerminalSystem { get; }
            private IMyProgrammableBlock Me { get; }

            public ConditionalActivator(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me)
            {
                GridTerminalSystem = gridTerminalSystem;
                Me = me;
            }

            public void Execute()
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType(batteries);
                batteries.First().ChargeMode = ChargeMode.Discharge;
            }
        }
    }
}

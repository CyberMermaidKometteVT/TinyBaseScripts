using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        bool SkipOtherGrids { get { return true; } }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(allBlocks);
            if (SkipOtherGrids)
            {
                allBlocks.RemoveAll(block => block.CubeGrid != Me.CubeGrid);
            }

        }


        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.


            //StatusSystem statusSystem = new StatusSystem(GridTerminalSystem, "TinyDrillSystem");
            //statusSystem.ReportStatusToScreens();


            BlockRenamerByShipName renamer = new BlockRenamerByShipName(GridTerminalSystem, Me, "YOUR OLD SHIP NAME GOES HERE IF YOU WANT TO DELETE AN OLD SHIP NAME, OTHERWISE LEAVE IT BLANK!");
            string output = renamer.Execute();

            Echo(output);
        }
    }
}
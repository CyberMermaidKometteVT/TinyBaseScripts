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
using System.Text.RegularExpressions;

namespace IngameScript
{
    partial class Program
    {
        public class BlockRenamerByShipName
        {
            public static bool ProvideDefaultNameForNamelessBlocks = true;
            public static bool SkipOtherGrids = true;

            private IMyGridTerminalSystem GridTerminalSystem { get; }
            private IMyProgrammableBlock Me { get; }
            private StringBuilder Output { get; set; }
            private string[] FormerShipNames { get; }


            public BlockRenamerByShipName(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me, string formerShipName)
            {
                GridTerminalSystem = gridTerminalSystem;
                Me = me;
                FormerShipNames = new string[] { formerShipName };
            }

            public BlockRenamerByShipName(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me, string[] formerShipNames = null)
            {
                GridTerminalSystem = gridTerminalSystem;
                Me = me;
                FormerShipNames = formerShipNames ?? new string[] { };
            }

            public string Execute()
            {
                Output = new StringBuilder();
                List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocks(allBlocks);
                if (SkipOtherGrids)
                {
                    allBlocks.RemoveAll(block => block.CubeGrid != Me.CubeGrid);
                }

                List<IMyTerminalBlock> appropriatelyNamedBlocks = allBlocks.Where(block => block.CustomName.StartsWith(block.CubeGrid.CustomName)).ToList();

                Output.AppendLine($"{appropriatelyNamedBlocks.Count} appropriately named blocks.");

                List<IMyTerminalBlock> misnamedBlocks = allBlocks.Except(appropriatelyNamedBlocks).ToList();

                Output.AppendLine($"{misnamedBlocks.Count} misnamed blocks to rename.");


                foreach (IMyTerminalBlock misnamedBlock in misnamedBlocks)
                {
                    RenameBlock(misnamedBlock);
                }

                return Output.ToString();
            }

            private void RenameBlock(IMyTerminalBlock blockToRename)
            {
                string oldBlockName = blockToRename.CustomName;

                foreach (string formerShipName in FormerShipNames)
                {
                    TrimLeadingName(blockToRename, formerShipName);
                }

                blockToRename.CustomName = $"{blockToRename.CubeGrid.CustomName} {blockToRename.CustomName}";

                Output.AppendLine($"Renaming {oldBlockName} to {blockToRename.CustomName}.");
            }

            private void TrimLeadingName(IMyTerminalBlock blockToRename, string oldName)
            {
                if (blockToRename.CustomName.StartsWith(oldName))
                {
                    //If the old ship name is the WHOLE name of the block, make up a name for the block so it's not just blank.
                    if (ProvideDefaultNameForNamelessBlocks && blockToRename.CustomName.Trim() == oldName)
                    {
                        blockToRename.CustomName = GetDefaultName(blockToRename);
                    }
                    else
                    {
                        blockToRename.CustomName = blockToRename.CustomName.Substring(oldName.Length, blockToRename.CustomName.Length - oldName.Length).Trim();
                    }
                }
            }

            private string GetDefaultName(IMyTerminalBlock blockToRename)
            {
                string nameByBlock = SplitCamelCase(blockToRename.BlockDefinition.SubtypeId);

                if (GridTerminalSystem.GetBlockWithName(nameByBlock) == null)
                {
                    return nameByBlock;
                }

                int blockIndex = 1;
                string candidateBlockName;
                do
                {
                    candidateBlockName = $"{nameByBlock} {blockIndex}";
                    blockIndex++;
                } while (GridTerminalSystem.GetBlockWithName(candidateBlockName) == null);

                return candidateBlockName;
            }

            //TODO: Move to a shared utility class
            private string SplitCamelCase(string value)
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])");

                //Note that the second one is String.Replace(), not Regex.Replace().
                return regex.Replace(value, " $1").Replace("_", "");
            }
        }
    }
}

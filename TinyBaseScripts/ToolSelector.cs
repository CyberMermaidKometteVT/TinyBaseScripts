using System;
using System.Collections.Generic;
using System.Linq;

using Sandbox.ModAPI.Ingame;

using VRageMath;

namespace IngameScript
{
    public class ToolSelector
    {

        //Set this to the block name of your cockpit whose
        //orientation will be used to define the "front."
        //If left null, this will be the cockpit marked as
        //the "Main" cockpit.
        public static string FrontCockpitNameIfNotMain = null;

        private IMyGridTerminalSystem GridTerminalSystem { get; }
        private IMyProgrammableBlock Me { get; }
        private Action<string> Echo { get; }

        enum ToolTypeInFront
        {
            Unset,
            Mixed,
            Welder,
            Grinder
        }

        public ToolSelector(IMyGridTerminalSystem gridTerminalSystem, IMyProgrammableBlock me, Action<string> echo)
        {
            GridTerminalSystem = gridTerminalSystem;
            Me = me;
            Echo = echo;
        }

        public void ToggleFrontToolEnabled()
        {
            //Get all tools
            List<IMyShipGrinder> allGrinders = new List<IMyShipGrinder>();
            List<IMyShipWelder> allWelders = new List<IMyShipWelder>();
            GridTerminalSystem.GetBlocksOfType(allGrinders, tool => tool.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(allWelders, tool => tool.IsSameConstructAs(Me));

            //Get front cockpit
            IMyShipController frontCockpit = GetFrontCockpit();
            if (frontCockpit == null)
            {
                Echo("No front cockpit - cannot identify 'front,' aborting script!");
                return;
            }

            //Identify which tools are in front
            ToolTypeInFront toolTypeInFront = IdentifyWhichToolsAreInFront(frontCockpit, allGrinders, allWelders);

            //Assert: Tools are not in a mixed state, i.e. with some grinders in front and some welders in front
            //TODO: Implement
            if (toolTypeInFront == ToolTypeInFront.Mixed || toolTypeInFront == ToolTypeInFront.Unset)
            {
                Echo("Tools are in a mixed state, please make sure that all rotors and such are oriented appropriately!");
                return;
            }

            //Disable everything else, and toggle the tool in front
                if (toolTypeInFront == ToolTypeInFront.Grinder)
            {
                DisableOtherToolsAndToggleToolInFront(allGrinders, allWelders, "Grinders");
            }
            else if (toolTypeInFront == ToolTypeInFront.Welder)
            {
                DisableOtherToolsAndToggleToolInFront(allWelders, allGrinders, "Welders");
            }
        }

        private IMyShipController GetFrontCockpit()
        {
            List<IMyShipController> cockpits = new List<IMyShipController>();

            if (!String.IsNullOrWhiteSpace(FrontCockpitNameIfNotMain))
            {
                GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => String.Equals(cockpit.CustomName, FrontCockpitNameIfNotMain, StringComparison.CurrentCultureIgnoreCase));

                if (cockpits.Count == 1)
                {
                    return cockpits.Single();
                }
                else if (cockpits.Count == 0)
                {
                    Echo($"No cockpit found by the name of {FrontCockpitNameIfNotMain}.");
                    return null;
                }
                else
                {
                    //Multiple cockpits found matching the name.
                    IEnumerable<IMyShipController> mainCockpitMatchingName = cockpits.Where(cockpit => cockpit.IsMainCockpit);
                    if (mainCockpitMatchingName.Count() == 0)
                    {
                        Echo($"Multiple cockpits found by the name of {FrontCockpitNameIfNotMain}, none were marked as main, so we can't be sure which to use.");
                        return null;
                    }
                    else
                    {
                        return mainCockpitMatchingName.First();
                    }
                }
            }
            else // No cockpit name is specified; just use the main cockpit, nice and simple!
            {
                GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => cockpit.IsMainCockpit);
                return cockpits.First();
            }
        }

        private ToolTypeInFront IdentifyWhichToolsAreInFront(IMyShipController frontCockpit, List<IMyShipGrinder> allGrinders, List<IMyShipWelder> allWelders)
        {

            //DEBUG: Outputting all of the directions of all of the blocks!
            foreach (IMyShipToolBase tool in allGrinders)
            {
                Echo($"{tool.CustomName}: {tool.Orientation.Forward}");
            }
            foreach (IMyShipToolBase tool in allWelders)
            {
                Echo($"{tool.CustomName}: {tool.Orientation.Forward}");
            }

            bool allGrindersMatch = allGrinders.All(grinder => grinder.Orientation.Forward == frontCockpit.Orientation.Forward);
            bool allWeldersMatch = allWelders.All(welder => welder.Orientation.Forward == frontCockpit.Orientation.Forward);

            if (allGrindersMatch && !allWeldersMatch)
            {
                return ToolTypeInFront.Grinder;
            }
            else if (!allGrindersMatch && allWeldersMatch)
            {
                return ToolTypeInFront.Welder;
            }
            else
            {
                return ToolTypeInFront.Mixed;
            }
        }

        private void DisableOtherToolsAndToggleToolInFront<TToolInFront, TToolInBack>(List<TToolInFront> toolsInFront, List<TToolInBack> toolsInBack, string toolName)
            where TToolInFront : IMyShipToolBase
            where TToolInBack : IMyShipToolBase
        {
            toolsInBack.ForEach(toolInBack => toolInBack.Enabled = false);

            bool newEnabledState = toolsInFront.First().Enabled;

            //DEBUG: Disabling this!
            //toolsInFront.ForEach(toolInFront => toolInFront.Enabled = newEnabledState);

            Echo($"{toolName} have been turned {(newEnabledState ? "on" : "off")}");
        }

        private Vector3I GetWorldVectors(IMyTerminalBlock block)
        {
#error finish

            return new Vector3I
            {
            };
        }

        private bool AreVectorsClose(Vector3I first, Vector3I second)
        {
            //https://forum.keenswh.com/threads/vector-math-help.7368268/page-2.html
            //post # 44 I think describes how to get the angle I want
            first * 
        }
    }
}
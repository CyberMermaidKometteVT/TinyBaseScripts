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
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        public class StatusSystem
        {
            private List<IMyPistonBase> Pistons { get; } = new List<IMyPistonBase>();
            private List<IMyMotorStator> Rotors { get; } = new List<IMyMotorStator>();
            private List<IMyShipDrill> Drills { get; } = new List<IMyShipDrill>();
            private List<IMyTextPanel> Screens { get; } = new List<IMyTextPanel>();

            private IMyGridTerminalSystem GridTerminalSystem { get; }

            private readonly string _statusSystemText;

            public StatusSystem(IMyGridTerminalSystem gridTerminalSystem, string statusSystemText)
            {
                GridTerminalSystem = gridTerminalSystem;
                _statusSystemText = statusSystemText;

                if (GridTerminalSystem == null)
                {
                    throw new ArgumentNullException(nameof(gridTerminalSystem));
                }

                if (String.IsNullOrWhiteSpace(statusSystemText))
                {
                    throw new ArgumentException(nameof(statusSystemText));
                }
            }

            public void ReportStatusToScreens()
            {
                FindComponents();
                ClearScreens();
                List<ScreenGroup> screenGroups = GroupByScreens();

                foreach (ScreenGroup screenGroup in screenGroups)
                {
                    screenGroup.ReportStatusToScreen();
                }
            }

            private  void FindComponents()
            {
                this.GridTerminalSystem.GetBlocksOfType(Pistons, candidatePiston => candidatePiston.CustomData.Contains($"[{_statusSystemText}]"));

                this.GridTerminalSystem.GetBlocksOfType(Rotors, candidateRotor => candidateRotor.CustomData.Contains($"[{_statusSystemText}]"));

                this.GridTerminalSystem.GetBlocksOfType(Drills, candidateDrill => candidateDrill.CustomData.Contains($"[{_statusSystemText}]"));

                this.GridTerminalSystem.GetBlocksOfType(Screens, candidateScreen => candidateScreen.CustomData.Contains($"[{_statusSystemText}]"));
            }

            private void ClearScreens()
            {
                foreach (IMyTextPanel screen in Screens)
                {
                    screen.WriteText("", false);
                    screen.ContentType = ContentType.TEXT_AND_IMAGE;
                }
            }

            //TODO: If multiple screens show the same data group, that data group is calculated for each screen.  This could definitely be optimized.
            //TODO: If the same data appears in multiple data groups, that data is calculated again for each group.  This could definitely be optimized.
            //TODO: CONSIDER - This method might be too long and may need to be moved to a helper class.
            private List<ScreenGroup> GroupByScreens()
            {
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex($"\\[{_statusSystemText}-group-([0-9]+)\\]");
                List<ScreenGroup> screenGroups = new List<ScreenGroup>();

                foreach (IMyTextPanel screen in Screens)
                {
                    System.Text.RegularExpressions.MatchCollection matches = regex.Matches(screen.CustomData);

                    if (matches.Count == 0)
                    {
                        continue;
                    }

                    ScreenGroup screenGroup = new ScreenGroup();
                    screenGroup.Screen = screen;
                    screenGroups.Add(screenGroup);

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string groupingTerm = match.Groups[0].ToString();

                        screenGroup.Pistons = Pistons.Where(piston => piston.CustomData.Contains(groupingTerm));
                        screenGroup.Rotors = Rotors.Where(rotor => rotor.CustomData.Contains(groupingTerm));
                        screenGroup.Drills = Drills.Where(drill => drill.CustomData.Contains(groupingTerm));
                    }

                    screenGroup.Pistons = screenGroup.Pistons.Distinct();
                    screenGroup.Rotors = screenGroup.Rotors.Distinct();
                    screenGroup.Drills = screenGroup.Drills.Distinct();
                }

                return screenGroups;
            }
        }
    }
}

using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ScreenGroup
        {
            internal IEnumerable<IMyPistonBase> Pistons { get; set; } = new List<IMyPistonBase>();
            internal IEnumerable<IMyMotorStator> Rotors { get; set; } = new List<IMyMotorStator>();
            internal IEnumerable<IMyShipDrill> Drills { get; set; } = new List<IMyShipDrill>();
            internal IMyTextPanel Screen { get; set; }

            public void ReportStatusToScreen()
            {
                IEnumerable<string> statusReports = Pistons.Select(StatusReportFormatter.GetStatusReport)
                    .Union(Rotors.Select(StatusReportFormatter.GetStatusReport))
                    .Union(Drills.Select(StatusReportFormatter.GetStatusReport));

                string concatenatedStatusReports = string.Join($"{Environment.NewLine}{Environment.NewLine}", statusReports);

                if (String.IsNullOrWhiteSpace(concatenatedStatusReports))
                {
                    concatenatedStatusReports = $"No data assigned to screen.\r\nScreen CustomData:\r\n{Screen.CustomData}";
                }

                Screen.WriteText(concatenatedStatusReports, true);
            }
        }
    }
}
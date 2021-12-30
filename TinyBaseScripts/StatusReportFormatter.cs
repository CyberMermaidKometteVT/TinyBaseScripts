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
    public static class StatusReportFormatter
    {
        public static string GetStatusReport(IMyPistonBase piston)
        {
            string enabledText = piston.Enabled ? "On " : "Off";
            string minText = $"{Math.Round(piston.MinLimit, 1)}";
            string maxText = $"{Math.Round(piston.MaxLimit, 1)}";
            string positionText = $"{Math.Round(piston.CurrentPosition, 1)}";
            string velocityPlusText = piston.Velocity >= 0 ? "+" : "";
            string velocityText = $"{ velocityPlusText }{Math.Round(piston.Velocity, 1)}";

            return $"{piston.CustomName}{Environment.NewLine}{enabledText} | {minText} < {positionText} > {maxText} | {velocityText}";
        }

        public static string GetStatusReport(IMyMotorStator rotor)
        {
            string enabledText = rotor.Enabled ? "On " : "Off";
            string lockedText = rotor.RotorLock ? "X" : " ";
            string minText = $"{GetLowerLimitDegText(rotor)}";
            string maxText = $"{GetUpperLimitDegText(rotor)}";
            string positionText = $"{Math.Round(rotor.Angle, 1)}";
            string velocityPlusText = rotor.TargetVelocityRPM >= 0 ? "+" : "";
            string velocityText = $"{ velocityPlusText }{Math.Round(rotor.TargetVelocityRPM, 2)}";

            return $"{rotor.CustomName}{Environment.NewLine}{enabledText} {lockedText} | {minText} < {positionText} > {maxText} | {velocityText}";
        }

        public static string GetStatusReport(IMyShipDrill drill)
        {
            string enabledText = drill.Enabled ? "On " : "Off";

            return $"{drill.CustomName}{Environment.NewLine}{enabledText}";
        }

        public static string GetLowerLimitDegText(IMyMotorStator rotor)
        {
            return GetDegreesText(rotor.LowerLimitDeg);
        }

        public static string GetUpperLimitDegText(IMyMotorStator rotor)
        {
            return GetDegreesText(rotor.UpperLimitDeg);
        }

        public static string GetDegreesText(float value)
        {
            if (value == float.MinValue)
            {
                return "-∞";
            }

            if (value == float.MaxValue)
            {
                return "+∞";
            }

            return $"{Math.Round(value)}";
        }

    }
}
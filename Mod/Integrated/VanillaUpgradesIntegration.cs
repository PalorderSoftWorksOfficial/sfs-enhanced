using System;
using System.Globalization;
using HarmonyLib;
using SFS;
using SFS.Builds;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace SFSEnhanced.Mod.Integrated
{
    public static class VanillaUpgradesIntegration
    {
        public static bool Enabled { get; set; } = true;
        public static bool ExtendedCamera { get; set; } = true;
        public static bool ExtendedPhysicsTimewarp { get; set; } = true;
        public static bool AccurateTwr { get; set; } = true;
        public static bool ExtendedUnits { get; set; } = true;
        public static bool ThrottleAccuracy { get; set; } = true;
        public static bool StopWarpOnEncounter { get; set; } = true;
        public static bool TorqueToggle { get; set; } = true;
        private static Harmony _harmony;

        public static void Initialize()
        {
            if (!Enabled || _harmony != null) return;
            _harmony = new Harmony("sfs-enhanced.vanillaupgrades");
            _harmony.PatchAll(typeof(VanillaUpgradesIntegration).Assembly);
        }

        public static bool MultiplayerTimeLocked => ModMain.Instance?.Client?.IsConnected == true;
    }

    [HarmonyPatch(typeof(PlayerController), "ClampTrackingOffset")]
    internal static class ExtendedCameraMovementPatch
    {
        private static bool Prefix(ref Vector2 __result, Vector2 newValue)
        {
            if (!VanillaUpgradesIntegration.ExtendedCamera || PlayerController.main?.player.Value == null) return true;
            PlayerController.main.player.Value.ClampTrackingOffset(ref newValue, -30);
            __result = newValue;
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "ClampCameraDistance")]
    internal static class ExtendedCameraZoomPatch
    {
        private static bool Prefix(ref float __result, float newValue)
        {
            if (!VanillaUpgradesIntegration.ExtendedCamera || PlayerController.main?.player.Value == null) return true;
            __result = Mathf.Clamp(newValue, 0.05f, 2.5E+10f);
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldTime), "GetTimewarpSpeed_Physics")]
    internal static class ExtendedPhysicsTimewarpPatch
    {
        private static bool Prefix(ref double __result, int timewarpIndex_Physics)
        {
            if (!VanillaUpgradesIntegration.ExtendedPhysicsTimewarp || VanillaUpgradesIntegration.MultiplayerTimeLocked) return true;
            var values = new[] { 1d, 2d, 3d, 5d, 10d, 25d };
            if (timewarpIndex_Physics < 0 || timewarpIndex_Physics >= values.Length) return true;
            __result = values[timewarpIndex_Physics];
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldTime), "MaxPhysicsIndex", MethodType.Getter)]
    internal static class ExtendedPhysicsTimewarpMaxPatch
    {
        private static bool Prefix(ref int __result)
        {
            if (!VanillaUpgradesIntegration.ExtendedPhysicsTimewarp || VanillaUpgradesIntegration.MultiplayerTimeLocked) return true;
            __result = 5;
            return false;
        }
    }

    [HarmonyPatch(typeof(FlightInfoDrawer), "Update")]
    internal static class AccurateFlightTwrPatch
    {
        private static void Postfix(ref TextAdapter ___thrustText, ref TextAdapter ___thrustToWeightText)
        {
            if (!VanillaUpgradesIntegration.AccurateTwr) return;
            if (!(PlayerController.main?.player.Value is Rocket rocket)) return;
            var mass = rocket.rb2d.mass;
            var gravity = (float)rocket.location.planet.Value.GetGravity(rocket.location.position.Value.magnitude);
            var thrust = Vector2.zero;
            foreach (var engine in rocket.partHolder.GetModules<EngineModule>())
            {
                if (!engine.engineOn.Value) continue;
                var direction = (Vector2)engine.transform.TransformVector(engine.thrustNormal.Value);
                thrust += engine.thrust.Value * direction.normalized * engine.throttle_Out.Value;
            }
            foreach (var booster in rocket.partHolder.GetModules<BoosterModule>())
            {
                if (!booster.enabled) continue;
                var direction = (Vector2)booster.transform.TransformVector(booster.thrustVector.Value);
                thrust += booster.thrustVector.Value.magnitude * direction.normalized;
            }
            var value = thrust.magnitude;
            ___thrustText.Text = value.ToThrustString().Split(':')[1];
            ___thrustToWeightText.Text = (mass > 0 ? value / mass * 9.8f / gravity : 0).ToTwrString().Split(':')[1];
        }
    }

    [HarmonyPatch(typeof(BuildStatsDrawer), "Draw")]
    internal static class AccurateBuildTwrPatch
    {
        private static void Postfix(float ___mass, float ___thrust, TextAdapter ___thrustToWeightText)
        {
            if (!VanillaUpgradesIntegration.AccurateTwr) return;
            var spaceCenter = Base.planetLoader.spaceCenter;
            var gravity = spaceCenter.address.GetPlanet().GetGravity(spaceCenter.LaunchPadLocation.position.magnitude);
            var twr = ___mass > 0 ? ___thrust * 9.8 / (___mass * gravity) : 0;
            ___thrustToWeightText.Text = twr.ToString(2, true);
        }
    }

    [HarmonyPatch(typeof(Units), nameof(Units.ToDistanceString))]
    internal static class ExtendedDistanceUnitsPatch
    {
        private static bool Prefix(double a, ref string __result)
        {
            if (!VanillaUpgradesIntegration.ExtendedUnits || double.IsInfinity(a)) return true;
            if (a >= 94607304725800440d)
            {
                __result = (a / 9460730472580044d).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "ly";
                return false;
            }
            if (a >= 100000000000d)
            {
                __result = (a / 1000000000d).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "Gm";
                return false;
            }
            if (a >= 100000000d)
            {
                __result = (a / 1000000d).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "Mm";
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Units), nameof(Units.ToVelocityString))]
    internal static class ExtendedVelocityUnitsPatch
    {
        private static bool Prefix(double a, ref string __result)
        {
            if (!VanillaUpgradesIntegration.ExtendedUnits || double.IsInfinity(a)) return true;
            if (a >= 2997924d)
            {
                __result = (a / 299792458d).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "c";
                return false;
            }
            if (a >= 10000d)
            {
                __result = (a / 1000d).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "km/s";
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Units), nameof(Units.ToMassString))]
    internal static class ExtendedMassUnitsPatch
    {
        private static bool Prefix(float a, bool forceDecimal, ref string __result)
        {
            if (!VanillaUpgradesIntegration.ExtendedUnits || float.IsInfinity(a) || a < 10000f) return true;
            var value = (a / 1000f).ToString(forceDecimal ? "F1" : "F", CultureInfo.InvariantCulture);
            __result = value + "k";
            return false;
        }
    }

    [HarmonyPatch(typeof(Units), nameof(Units.ToThrustString))]
    internal static class ExtendedThrustUnitsPatch
    {
        private static bool Prefix(float a, ref string __result)
        {
            if (!VanillaUpgradesIntegration.ExtendedUnits || float.IsInfinity(a) || a < 10000f) return true;
            var value = (a / 1000f).ToString("F1", CultureInfo.InvariantCulture);
            __result = value + "k";
            return false;
        }
    }

    [HarmonyPatch(typeof(ThrottleDrawer), "UpdatePercentUI")]
    internal static class ThrottleAccuracyPatch
    {
        private static bool Prefix(Throttle_Local ___throttle, FillSlider ___throttleSlider, TextAdapter ___throttlePercentText)
        {
            if (!VanillaUpgradesIntegration.ThrottleAccuracy) return true;
            var value = ___throttle.Value.throttlePercent.Value;
            ___throttlePercentText.Text = value.ToPercentString();
            ___throttleSlider.SetFillAmount(0.16f + value * 0.68f, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(Rocket), "GetTorque")]
    internal static class TorqueTogglePatch
    {
        private static bool _disabled;

        public static void Toggle()
        {
            if (!VanillaUpgradesIntegration.TorqueToggle || PlayerController.main == null || !PlayerController.main.HasControl(MsgDrawer.main)) return;
            _disabled = !_disabled;
            MsgDrawer.main.Log("Torque " + (_disabled ? "Disabled" : "Enabled"));
        }

        private static bool Prefix(ref float __result)
        {
            if (!VanillaUpgradesIntegration.TorqueToggle || !_disabled) return true;
            __result = 0f;
            return false;
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Stops the game spawning its stealth guards in camp scenes. The patch is skipped if the game has no such behavior.
    /// </summary>
    [HarmonyPatch]
    internal static class StealthGuardsNotInCampPatch
    {
        private const string TypeName = "SandBox.CampaignBehaviors.StealthCharactersCampaignBehavior";
        private const string MethodName = "LocationCharactersAreReadyToSpawn";

        private static MethodBase? TargetMethod()
        {
            Type? type = AccessTools.TypeByName(TypeName);
            return type == null ? null : AccessTools.Method(type, MethodName);
        }

        private static bool Prepare()
        {
            if (TargetMethod() != null)
                return true;
            IMFDiagnostics.Info("[Stealth] " + TypeName + "." + MethodName + " not found: nothing to suppress in minor-faction camps");
            return false;
        }

        private static bool Prefix(object[] __args)
        {
            try
            {
                Settlement? settlement = PlayerEncounter.LocationEncounter?.Settlement;
                if (!Helpers.IsMFHideout(settlement))
                    return true;
                int points = CountStealthPoints(__args != null && __args.Length > 0 ? __args[0] as IReadOnlyDictionary<string, int> : null);
                if (points > 0)
                    IMFDiagnostics.Verbose("[Stealth] " + settlement!.Name + ": the scene's " + points + " stealth guard spawn point(s) stay empty in the camp");
                return false;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("StealthGuardsNotInCampPatch failed:", e);
                return true;
            }
        }

        internal static readonly string[] StealthTags =
        {
            "stealth_agent",
            "stealth_agent_forced",
            "disguise_default_agent",
            "disguise_officer_agent",
            "disguise_shadow_agent",
        };

        /// <summary>
        /// Counts the free stealth spawn points in a scene, only for the log.
        /// </summary>
        internal static int CountStealthPoints(IReadOnlyDictionary<string, int>? unusedPoints)
        {
            if (unusedPoints == null)
                return 0;
            int total = 0;
            foreach (string tag in StealthTags)
                if (unusedPoints.TryGetValue(tag, out int n) && n > 0)
                    total += n;
            return total;
        }
    }

}

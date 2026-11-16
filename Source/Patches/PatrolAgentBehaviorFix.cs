using System.Reflection;
using HarmonyLib;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Skips patrol behavior for agents in scenes that have no patrol points, instead of crashing on the missing list.
    /// </summary>
    [HarmonyPatch(typeof(PatrolAgentBehavior))]
    internal static class PatrolAgentBehaviorNullPointsFix
    {
        private static readonly FieldInfo PatrolPointsField = AccessTools.Field(typeof(PatrolAgentBehavior), "_patrolPoints");

        /// <summary>
        /// Checks if the behavior's patrol point list is missing or empty.
        /// </summary>
        private static bool HasNoPatrolPoints(PatrolAgentBehavior behavior)
        {
            if (behavior == null)
                return true;
            if (PatrolPointsField == null)
                return false;
            var points = PatrolPointsField.GetValue(behavior) as PatrolPoint[];
            return points == null || points.Length == 0;
        }

        [HarmonyPatch("OnActivate")]
        [HarmonyPrefix]
        private static bool OnActivatePrefix(PatrolAgentBehavior __instance)
        {
            if (!HasNoPatrolPoints(__instance))
                return true;
            IMFDiagnostics.Verbose("PatrolAgentBehavior.OnActivate skipped: no patrol points set");
            return false;
        }

        [HarmonyPatch("Tick")]
        [HarmonyPrefix]
        private static bool TickPrefix(PatrolAgentBehavior __instance)
        {
            return !HasNoPatrolPoints(__instance);
        }

        [HarmonyPatch("OnDeactivate")]
        [HarmonyPrefix]
        private static bool OnDeactivatePrefix(PatrolAgentBehavior __instance)
        {
            return !HasNoPatrolPoints(__instance);
        }
    }
}

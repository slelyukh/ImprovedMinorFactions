using System.Reflection;
using HarmonyLib;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;

namespace ImprovedMinorFactions.Patches
{
    // some TC village scenes have no patrol points, skip the null list
    [HarmonyPatch(typeof(PatrolAgentBehavior))]
    internal static class PatrolAgentBehaviorNullPointsFix
    {
        private static readonly FieldInfo PatrolPointsField = AccessTools.Field(typeof(PatrolAgentBehavior), "_patrolPoints");

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

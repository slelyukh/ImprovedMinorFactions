using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Lets the player travel to a camp whose gate point is unreachable by using the camp's own position as the gate.
    /// </summary>
    [HarmonyPatch(typeof(global::Helpers.NavigationHelper), "GetInteractionDataForMainParty")]
    internal static class HideoutGatePatch
    {
        private static void Postfix(Settlement settlement, ref bool canNavigate, ref MobileParty.NavigationType bestNavigationType, ref bool isTargetingPort)
        {
            try
            {
                if (canNavigate || settlement == null || !Helpers.IsMFHideout(settlement))
                    return;

                CampaignVec2 gate = settlement.GatePosition;
                CampaignVec2 position = settlement.Position;
                if (gate.NearlyEquals(position, 0.01f)
                    || !global::Helpers.NavigationHelper.CanPlayerNavigateToPosition(position, out MobileParty.NavigationType navType))
                    return;

                AccessTools.PropertySetter(typeof(Settlement), "GatePosition")?.Invoke(settlement, new object[] { position });
                canNavigate = true;
                bestNavigationType = navType;
                isTargetingPort = false;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("HideoutGatePatch failed:", e);
            }
        }
    }
}

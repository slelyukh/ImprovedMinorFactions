using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace ImprovedMinorFactions.Source.Patches
{
    [HarmonyPatch(typeof(Campaign), "DailyTickSettlement")]
    public class CampaignSettlementTickPatch
    {
        static void Postfix(Campaign __instance, Settlement settlement)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null || !mfHideout.IsActive)
                return;
            mfHideout.DailyTick();
        }
    }
}

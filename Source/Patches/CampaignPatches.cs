using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Runs the daily tick of minor faction camps along with the game's daily settlement tick.
    /// </summary>
    [HarmonyPatch(typeof(Campaign), "DailyTickSettlement")]
    public class CampaignSettlementTickPatch
    {
        static void Postfix(Campaign __instance, Settlement settlement)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null)
                return;
            mfHideout.DailyTick();
        }
    }
}

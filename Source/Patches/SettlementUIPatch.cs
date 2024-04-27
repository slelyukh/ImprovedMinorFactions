using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Patches
{
    // Tooltip crash preventer
    [HarmonyPatch(typeof(CampaignUIHelper), "GetVillageMilitiaTooltip")]
    class CampaignUIHelperVillageMilitiaTooltipPatch
    {
        static bool Prefix(ref List<TooltipProperty> __result, Village village)
        {
            if (village != null)
                return true;
            Settlement curSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return true;
            __result = CampaignUIHelper.GetSettlementPropertyTooltip(
                curSettlement, new TextObject("Militia").ToString(), curSettlement.Militia, mfHideout.MilitiaChange);
            return false;
        }
    }

    // Tooltip crash preventer
    [HarmonyPatch(typeof(CampaignUIHelper), "GetVillageProsperityTooltip")]
    class CampaignUIHelperVillageProsperityTooltipPatch
    {
        static bool Prefix(ref List<TooltipProperty> __result, Village village)
        {
            if (village != null)
                return true;
            Settlement curSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return true;
            __result = CampaignUIHelper.GetSettlementPropertyTooltip(
                curSettlement, new TextObject("Hearth").ToString(), mfHideout.Hearth, mfHideout.HearthChange);

            return false;
        }
    }

    // another crash preventer copypasta
    [HarmonyPatch(typeof(SettlementMenuOverlayVM), "UpdateProperties")]
    public class SettlementMenuOverlayVMPatch
    {
        static bool Prefix(SettlementMenuOverlayVM __instance)
        {
            MobileParty mainParty = MobileParty.MainParty;
            Settlement curSettlement = mainParty.CurrentSettlement ?? mainParty.LastVisitedSettlement;
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return true;

            IFaction mapFaction = curSettlement.MapFaction;
            __instance.IsCrimeEnabled = mapFaction != null && mapFaction.MainHeroCrimeRating > 0f;
            __instance.CrimeLbl = ((int)(curSettlement.MapFaction?.MainHeroCrimeRating).Value).ToString();
            __instance.CrimeChangeAmount = (int)(curSettlement.MapFaction?.DailyCrimeRatingChange).Value;
            __instance.RemainingFoodText = "-";
            __instance.FoodChangeAmount = 0;
            __instance.MilitasLbl = ((int)curSettlement.Militia).ToString();
            __instance.MilitiaChangeAmount = (int)mfHideout.MilitiaChange.ResultNumber;
            __instance.IsLoyaltyRebellionWarning = false;
            __instance.GarrisonChangeAmount = 0;
            __instance.WallsLbl = "-";
            __instance.GarrisonLbl = "-";
            __instance.WallsLevel = 1;
            __instance.ProsperityLbl = ((int)mfHideout.Hearth).ToString();
            __instance.ProsperityChangeAmount = (int)mfHideout.HearthChange.ResultNumber;
            __instance.SettlementNameLbl = curSettlement.Name.ToString();
            __instance.LoyaltyChangeAmount = 0;
            __instance.LoyaltyLbl = "-";
            __instance.SecurityChangeAmount = 0;
            __instance.SecurityLbl = "-";
            return false;
        }
    }
}

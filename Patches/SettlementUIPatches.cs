using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    [HarmonyPatch(typeof(CampaignUIHelper), "GetVillageMilitiaTooltip")]
    class CampaignUIHelperVillageMilitiaTooltipPatch
    {
        static bool Prefix(ref List<TooltipProperty> __result, Village village)
        {

            if (village != null)
                return true;
            Settlement currentSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = currentSettlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
            {
                return true;
            }
            __result = CampaignUIHelper.GetSettlementPropertyTooltip(
                currentSettlement, new TextObject("Militia").ToString(), currentSettlement.Militia, mfHideout.MilitiaChange);
            return false;
        }
    }

    [HarmonyPatch(typeof(CampaignUIHelper), "GetVillageProsperityTooltip")]
    class CampaignUIHelperVillageProsperityTooltipPatch
    {
        static bool Prefix(ref List<TooltipProperty> __result, Village village)
        {
            if (village != null)
                return true;
            Settlement currentSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = currentSettlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return true;
            __result = CampaignUIHelper.GetSettlementPropertyTooltip(
                currentSettlement, new TextObject("Hearth").ToString(), mfHideout.Hearth, mfHideout.HearthChange);

            return false;
        }
    }

    [HarmonyPatch(typeof(SettlementMenuOverlayVM), "UpdateProperties")]
    public class SettlementMenuOverlayVMPatch
    {
        static bool Prefix(SettlementMenuOverlayVM __instance)
        {
            MobileParty mainParty = MobileParty.MainParty;
            Settlement currentSettlement = mainParty.CurrentSettlement ?? mainParty.LastVisitedSettlement;
            MinorFactionHideout? mfHideout = currentSettlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
            {
                return true;
            }

            IFaction mapFaction = currentSettlement.MapFaction;
            __instance.IsCrimeEnabled = mapFaction != null && mapFaction.MainHeroCrimeRating > 0f;
            __instance.CrimeLbl = ((int)(currentSettlement.MapFaction?.MainHeroCrimeRating).Value).ToString();
            __instance.CrimeChangeAmount = (int)(currentSettlement.MapFaction?.DailyCrimeRatingChange).Value;
            __instance.RemainingFoodText = "-";
            __instance.FoodChangeAmount = 0;
            __instance.MilitasLbl = ((int)currentSettlement.Militia).ToString();
            __instance.MilitiaChangeAmount = (int)mfHideout.MilitiaChange.ResultNumber;
            __instance.IsLoyaltyRebellionWarning = false;
            __instance.GarrisonChangeAmount = 0;
            __instance.WallsLbl = "-";
            __instance.GarrisonLbl = "-";
            __instance.WallsLevel = 1;
            __instance.ProsperityLbl = ((int)mfHideout.Hearth).ToString();
            __instance.ProsperityChangeAmount = (int)mfHideout.HearthChange.ResultNumber;
            __instance.SettlementNameLbl = currentSettlement.Name.ToString();
            __instance.LoyaltyChangeAmount = 0;
            __instance.LoyaltyLbl = "-";
            __instance.SecurityChangeAmount = 0;
            __instance.SecurityLbl = "-";
            return false;
        }
    }
}

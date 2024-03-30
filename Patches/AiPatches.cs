using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "IsSettlementSuitableForVisitingCondition")]
    public class IsSettlementSuitableForVisitingConditionPatch
    {
        static void Postfix(ref bool __result, MobileParty mobileParty, Settlement settlement)
        {
            if (__result == true || !Helpers.isMFHideout(settlement))
                return;
            var mfHideout = settlement.SettlementComponent as MinorFactionHideout;
            bool hideoutIsMercenaryOfParty = settlement.OwnerClan.IsUnderMercenaryService && settlement.OwnerClan.Kingdom == mobileParty.ActualClan.Kingdom;
            __result = settlement.Party.MapEvent == null && mfHideout.IsActive && (mobileParty.Party.Owner.MapFaction == settlement.MapFaction || hideoutIsMercenaryOfParty);
        }
    }
}

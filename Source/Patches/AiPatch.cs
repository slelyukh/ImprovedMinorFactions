using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // allow minor faction lords and lords hiring minor faction as mercenary to visit MFHideouts
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "IsSettlementSuitableForVisitingCondition")]
    public class IsSettlementSuitableForVisitingConditionPatch
    {
        static void Postfix(ref bool __result, MobileParty mobileParty, Settlement settlement)
        {
            if (__result == true || !Helpers.IsMFHideout(settlement))
                return;
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null)
                throw new System.Exception("mfHideout is somehow null even though IsMFHideout is true");
            bool hideoutIsMercenaryOfParty = (settlement.OwnerClan?.IsUnderMercenaryService ?? false) && settlement.OwnerClan?.Kingdom == mobileParty.ActualClan?.Kingdom;
            __result = settlement.Party?.MapEvent == null && mfHideout.IsActive && (mobileParty.Party?.Owner?.MapFaction == settlement.MapFaction || hideoutIsMercenaryOfParty);
        }
    }

    // make minor faction lords more likely to patrol their hideouts
    internal class IMFTargetScoreCalculatingModel : TargetScoreCalculatingModel
    {
        TargetScoreCalculatingModel _previousModel;

        public IMFTargetScoreCalculatingModel(TargetScoreCalculatingModel previousModel)
        {
            _previousModel = previousModel;
        }

        public override float TravelingToAssignmentFactor => _previousModel.TravelingToAssignmentFactor;

        public override float BesiegingFactor => _previousModel.BesiegingFactor;

        public override float AssaultingTownFactor => _previousModel.AssaultingTownFactor;

        public override float RaidingFactor => _previousModel.RaidingFactor;

        public override float DefendingFactor => _previousModel.DefendingFactor;

        public override float CalculatePatrollingScoreForSettlement(Settlement targetSettlement, MobileParty mobileParty)
        {
            float result = _previousModel.CalculatePatrollingScoreForSettlement(targetSettlement, mobileParty);
            if (!Helpers.IsMFHideout(targetSettlement) || !mobileParty.ActualClan.IsMinorFaction)
                return result;
            return result * 3;
        }

        public override float CurrentObjectiveValue(MobileParty mobileParty)
        {
            return _previousModel.CurrentObjectiveValue(mobileParty);
        }

        public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength, int numberOfEnemyFactionSettlements = -1, float totalEnemyMobilePartyStrength = -1)
        {
            return _previousModel.GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength, numberOfEnemyFactionSettlements, totalEnemyMobilePartyStrength);
        }
    }
}

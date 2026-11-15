using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // Vanilla only indexes forts and villages, add mf hideouts too
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "RefreshTheTargetingSettlementDictionary")]
    public class RefreshTheTargetingSettlementDictionaryPatch
    {
        static void Prefix(AiVisitSettlementBehavior __instance)
        {
            try
            {
                var dict = AccessTools.Field(typeof(AiVisitSettlementBehavior), "_numberOfAlliedMobilePartiesTargetingSettlement")?.GetValue(__instance) as Dictionary<Settlement, int>;
                if (dict == null)
                    return;
                foreach (Settlement settlement in Settlement.All)
                {
                    if (Helpers.IsMFHideout(settlement))
                    {
                        dict[settlement] = 0;
                    }
                }
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("RefreshTheTargetingSettlementDictionary patch failed:", e);
            }
        }
    }

    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "IsSettlementSuitableForVisitingCondition")]
    public class IsSettlementSuitableForVisitingConditionPatch
    {
        static void Postfix(ref bool __result, MobileParty mobileParty, Settlement settlement)
        {
            if (__result == true || !Helpers.IsMFHideout(settlement))
                return;
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null)
                return;   // not registered yet, e.g. while a new campaign is being set up
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

        public override float GetDefensivePatrollingFactor(bool isNavalPatrolling)
        {
            return _previousModel.GetDefensivePatrollingFactor(isNavalPatrolling);
        }

        public override float GetOffensivePatrollingFactor(bool isNavalPatrolling)
        {
            return _previousModel.GetOffensivePatrollingFactor(isNavalPatrolling);
        }

        public override float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
        {
            float result = _previousModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isTargetingPort, mobileParty);
            if (!Helpers.IsMFHideout(settlement) || mobileParty?.ActualClan?.IsMinorFaction != true)
                return result;
            return result * 3;
        }

        public override float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isTargetingPort, MobileParty mobileParty)
        {
            float result = _previousModel.CalculateOffensivePatrollingScoreForSettlement(settlement, isTargetingPort, mobileParty);
            if (!Helpers.IsMFHideout(settlement) || mobileParty?.ActualClan?.IsMinorFaction != true)
                return result;
            return result * 3;
        }

        public override float CurrentObjectiveValue(MobileParty mobileParty)
        {
            return _previousModel.CurrentObjectiveValue(mobileParty);
        }

        public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength)
        {
            return _previousModel.GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength);
        }
    }
}

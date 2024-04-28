using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.Patches
{
    internal class IMFBanditDensityModel : BanditDensityModel
    {
        BanditDensityModel _previousModel;

        public IMFBanditDensityModel(BanditDensityModel banditDensityModel)
        {
            this._previousModel = banditDensityModel;
        }

        public override int NumberOfMaximumLooterParties => _previousModel.NumberOfMaximumLooterParties;

        public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt => _previousModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;

        public override int NumberOfMaximumBanditPartiesInEachHideout => _previousModel.NumberOfMaximumBanditPartiesInEachHideout;

        public override int NumberOfMaximumBanditPartiesAroundEachHideout => _previousModel.NumberOfMaximumBanditPartiesAroundEachHideout;

        public override int NumberOfMaximumHideoutsAtEachBanditFaction => _previousModel.NumberOfMaximumHideoutsAtEachBanditFaction;

        public override int NumberOfInitialHideoutsAtEachBanditFaction => _previousModel.NumberOfInitialHideoutsAtEachBanditFaction;

        public override int NumberOfMinimumBanditTroopsInHideoutMission => _previousModel.NumberOfMinimumBanditTroopsInHideoutMission;

        // No limit for Minor Faction Hideouts :)
        public override int NumberOfMaximumTroopCountForFirstFightInHideout 
        {
            get
            {
                if (Helpers.isMFHideout(Settlement.CurrentSettlement))
                    return 150;
                return _previousModel.NumberOfMaximumTroopCountForFirstFightInHideout;
            }
        }

        public override int NumberOfMaximumTroopCountForBossFightInHideout => _previousModel.NumberOfMaximumTroopCountForBossFightInHideout;

        public override float SpawnPercentageForFirstFightInHideoutMission => _previousModel.SpawnPercentageForFirstFightInHideoutMission;

        public override int GetPlayerMaximumTroopCountForHideoutMission(MobileParty party)
        {
            return _previousModel.GetPlayerMaximumTroopCountForHideoutMission(party);
        }
    }

    // No limit for Minor Faction Hideouts :)
    //[HarmonyPatch(typeof(DefaultBanditDensityModel), "get_NumberOfMaximumTroopCountForFirstFightInHideout")]
    //public class DefaultMaxTroopsFirstHideoutBattlePatch
    //{
    //    static void Postfix(ref int __result)
    //    {
    //        if (Settlement.CurrentSettlement == null || !Helpers.isMFHideout(Settlement.CurrentSettlement))
    //            return;
    //        __result = 150;
    //    }
    //}
}

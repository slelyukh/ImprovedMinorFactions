using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Source.Patches
{
    internal class IMFEncounterModel : EncounterModel
    {
        EncounterModel _previousModel;

        public IMFEncounterModel(EncounterModel previousModel)
        {
            _previousModel = previousModel;
        }   

        public override float EstimatedMaximumMobilePartySpeedExceptPlayer => _previousModel.EstimatedMaximumMobilePartySpeedExceptPlayer;

        public override float NeededMaximumDistanceForEncounteringMobileParty => _previousModel.NeededMaximumDistanceForEncounteringMobileParty;

        public override float MaximumAllowedDistanceForEncounteringMobilePartyInArmy => _previousModel.MaximumAllowedDistanceForEncounteringMobilePartyInArmy;

        public override float NeededMaximumDistanceForEncounteringTown => _previousModel.NeededMaximumDistanceForEncounteringTown;

        public override float NeededMaximumDistanceForEncounteringVillage => _previousModel.NeededMaximumDistanceForEncounteringVillage;

        public override MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType)
        {
            return _previousModel.CreateMapEventComponentForEncounter(attackerParty, defenderParty, battleType);
        }

        public override int GetCharacterSergeantScore(Hero hero)
        {
            return _previousModel.GetCharacterSergeantScore(hero);
        }

        public override IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(Settlement settlement, MapEvent.BattleTypes mapEventType)
        {
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout != null)
                return mfHideout.GetDefenderParties(mapEventType);
            return _previousModel.GetDefenderPartiesOfSettlement(settlement, mapEventType);
        }

        public override Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side)
        {
            return _previousModel.GetLeaderOfMapEvent(mapEvent, side);
        }

        public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _previousModel.GetLeaderOfSiegeEvent(siegeEvent, side);
        }

        public override PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType)
        {
            return _previousModel.GetNextDefenderPartyOfSettlement(settlement, ref partyIndex, mapEventType);
        }

        public override bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2)
        {
            return _previousModel.IsEncounterExemptFromHostileActions(side1, side2);
        }
    }
}

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
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Wraps the game's encounter model so a camp's defenders come from the camp itself (its militia and the owner's parties in it).
    /// Everything else is passed on.
    /// </summary>
    internal class IMFEncounterModel : EncounterModel
    {
        EncounterModel _previousModel;

        public IMFEncounterModel(EncounterModel previousModel)
        {
            _previousModel = previousModel;
        }   

        public override float NeededMaximumLandDistanceForEncounteringMobileParty => _previousModel.NeededMaximumLandDistanceForEncounteringMobileParty;

        public override float NeededMaximumNavalDistanceForEncounteringMobileParty => _previousModel.NeededMaximumNavalDistanceForEncounteringMobileParty;

        public override float MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy => _previousModel.MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy;

        public override float MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy => _previousModel.MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy;

        public override float NeededMaximumDistanceForEncounteringTown => _previousModel.NeededMaximumDistanceForEncounteringTown;

        public override float NeededMaximumDistanceForEncounteringBlockade => _previousModel.NeededMaximumDistanceForEncounteringBlockade;

        public override float NeededMaximumDistanceForEncounteringVillage => _previousModel.NeededMaximumDistanceForEncounteringVillage;

        public override float GetEncounterJoiningRadius => _previousModel.GetEncounterJoiningRadius;

        public override float GetSettlementBeingNearFieldBattleRadius => _previousModel.GetSettlementBeingNearFieldBattleRadius;

        public override float PlayerParleyDistance => _previousModel.PlayerParleyDistance;

        public override int MinimumNumberOfMenForAttackingVillageViaScene => _previousModel.MinimumNumberOfMenForAttackingVillageViaScene;

        public override bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2)
        {
            return _previousModel.IsEncounterExemptFromHostileActions(side1, side2);
        }

        public override bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation)
        {
            return _previousModel.CanMainHeroDoParleyWithParty(partyBase, out explanation);
        }

        public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return _previousModel.GetLeaderOfSiegeEvent(siegeEvent, side);
        }

        public override Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side)
        {
            return _previousModel.GetLeaderOfMapEvent(mapEvent, side);
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

        public override PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType)
        {
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout != null)
                return mfHideout.GetNextDefenderParty(ref partyIndex, mapEventType);
            return _previousModel.GetNextDefenderPartyOfSettlement(settlement, ref partyIndex, mapEventType);
        }

        public override MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType)
        {
            return _previousModel.CreateMapEventComponentForEncounter(attackerParty, defenderParty, battleType);
        }

        public override ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            return _previousModel.GetBribeChance(defenderParty, attackerParty);
        }

        public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            return _previousModel.GetSurrenderChance(defenderParty, attackerParty);
        }

        public override float GetMapEventSideRunAwayChance(MapEventSide mapEventside)
        {
            return _previousModel.GetMapEventSideRunAwayChance(mapEventside);
        }

        public override void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
        {
            _previousModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(partiesToJoinPlayerSide, partiesToJoinEnemySide);
        }

        public override bool CanPlayerForceBanditsToJoin(out TextObject explanation)
        {
            return _previousModel.CanPlayerForceBanditsToJoin(out explanation);
        }

        public override bool IsPartyUnderPlayerCommand(PartyBase party)
        {
            return _previousModel.IsPartyUnderPlayerCommand(party);
        }

        public override MBReadOnlyList<MobileParty> GetPartiesToTeleportOnMapEventFinalize(MapEvent mapEvent)
        {
            return _previousModel.GetPartiesToTeleportOnMapEventFinalize(mapEvent);
        }
    }
}

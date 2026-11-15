using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImprovedMinorFactions.Source.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    // Doomstack armies would lose 25% a day on the normal cap, so give them a huge limit.
    internal class IMFPartySizeLimitModel : PartySizeLimitModel
    {
        private readonly PartySizeLimitModel _previousModel;

        private const int DoomstackPartySizeLimit = 100000;

        public IMFPartySizeLimitModel(PartySizeLimitModel previousModel)
        {
            _previousModel = previousModel;
        }

        public override int MinimumNumberOfVillagersAtVillagerParty => _previousModel.MinimumNumberOfVillagersAtVillagerParty;

        public override int GetClanTierPartySizeEffectForHero(Hero hero)
        {
            return _previousModel.GetClanTierPartySizeEffectForHero(hero);
        }

        public override int GetNextClanTierPartySizeEffectChangeForHero(Hero hero)
        {
            return _previousModel.GetNextClanTierPartySizeEffectChangeForHero(hero);
        }

        public override int GetAssumedPartySizeForLordParty(Hero leaderHero, IFaction partyMapFaction, Clan actualClan)
        {
            return _previousModel.GetAssumedPartySizeForLordParty(leaderHero, partyMapFaction, actualClan);
        }

        public override int GetIdealVillagerPartySize(Village village)
        {
            return _previousModel.GetIdealVillagerPartySize(village);
        }

        public override ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false)
        {
            return _previousModel.CalculateGarrisonPartySizeLimit(settlement, includeDescriptions);
        }

        public override ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            return _previousModel.GetPartyPrisonerSizeLimit(party, includeDescriptions);
        }

        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool isCurrentlyAtSea, bool includeDescriptions = false)
        {
            if (IsDoomstackParty(party))
            {
                ExplainedNumber result = new ExplainedNumber(DoomstackPartySizeLimit, includeDescriptions);
                if (includeDescriptions)
                {
                    result.Add(0f, new TextObject("{=IMF_Doomstack}Doomstack Army"));
                }
                return result;
            }
            return _previousModel.GetPartyMemberSizeLimit(party, isCurrentlyAtSea, includeDescriptions);
        }

        public override TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
        {
            return _previousModel.FindAppropriateInitialRosterForMobileParty(party, partyTemplate);
        }

        public override List<Ship> FindAppropriateInitialShipsForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
        {
            return _previousModel.FindAppropriateInitialShipsForMobileParty(party, partyTemplate);
        }

        // A mf party of 700+ troops (or a template that big) is a horde.
        private static bool IsDoomstackParty(PartyBase party)
        {
            if (party == null || !party.IsMobile)
                return false;
            return HordeHelper.IsHordeParty(party.MobileParty);
        }
    }
}

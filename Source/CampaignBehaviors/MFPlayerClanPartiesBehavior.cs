using System;
using System.Linq;
using Helpers;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    // free lords get their war parties when you take over a minor faction
    internal class MFPlayerClanPartiesBehavior : CampaignBehaviorBase
    {
        private static readonly string[] Foods = { "grain", "cheese", "meat", "fish" };
        private const int FoodPerKind = 10;

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, delegate { SpawnPartiesForPlayerClan(); });
            CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, delegate { SpawnPartiesForPlayerClan(); });
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static void SpawnPartiesForPlayerClan()
        {
            try
            {
                SpawnMissingParties();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("spawning the player clan's parties failed:", e);
            }
        }

        private static void SpawnMissingParties()
        {
            Clan clan = Clan.PlayerClan;
            if (clan == null || !clan.IsMinorFaction || clan.IsEliminated || IMFManager.Current?.HasFaction(clan) != true)
                return;
            int partyLimit = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier);
            foreach (Hero hero in clan.Heroes.ToList())
            {
                if (clan.WarPartyComponents.Count >= partyLimit)
                    break;
                if (!CanGetParty(hero))
                    continue;
                if (!hero.IsActive)
                    hero.ChangeState(Hero.CharacterStates.Active);
                if (hero.CanLeadParty())
                    SpawnParty(hero, clan);
            }
        }

        private static bool CanGetParty(Hero hero)
        {
            return hero != Hero.MainHero && hero.IsAlive && !hero.IsDisabled && !hero.IsPrisoner
                && hero.PartyBelongedTo == null && hero.PartyBelongedToAsPrisoner == null && hero.GovernorOf == null
                && hero.Age > Campaign.Current.Models.AgeModel.HeroComesOfAge
                && hero.CharacterObject.Occupation == Occupation.Lord;
        }

        private static void SpawnParty(Hero hero, Clan clan)
        {
            Settlement? camp = IMFManager.Current!.GetActiveHideoutsOfClan(clan).Select(h => h.Settlement).FirstOrDefault(s => s != null);
            MobileParty party = MobilePartyHelper.SpawnLordParty(hero, camp?.GatePosition ?? MobileParty.MainParty.Position, 3f);
            if (party == null)
                return;
            // SpawnLordParty gives the party an empty inventory.
            foreach (string id in Foods)
            {
                ItemObject food = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (food != null && food.IsFood)
                    party.ItemRoster.AddToCounts(food, FoodPerKind);
            }
            party.Ai.SetDoNotMakeNewDecisions(false);
            IMFDiagnostics.Info($"[PlayerClan] {hero.Name} leads {party.StringId}");
        }
    }
}

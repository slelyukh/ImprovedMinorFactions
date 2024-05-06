using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace ImprovedMinorFactions.Source
{
    internal class MFHNotablesCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
            // Location events
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));

            // Debug listeners
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, Hero, CharacterObject, int>(this.OnTroopRecruited));
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
        }

        private void OnTroopRecruited(Hero recruiterHero, Settlement settlement, Hero troopSource, CharacterObject troop, int amount)
        {
            if (!Helpers.IsMFHideout(settlement))
                return;
            //InformationManager.DisplayMessage(new InformationMessage($"{amount} troops recruited by {recruiterHero}"));
        }

        private void AddMFHLocationCharacters(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
            {
                List<MobileParty> list = Enumerable.ToList<MobileParty>(Settlement.CurrentSettlement.Parties);
                this.AddNotableLocationCharacters(settlement);
                foreach (MobileParty mobileParty in list)
                {
                    this.AddPartyHero(mobileParty, settlement);
                }
            }
        }

        // Adds PartyHero character to MFHideout center to allow dialog
        private void AddPartyHero(MobileParty mobileParty, Settlement settlement)
        {
            Hero leaderHero = mobileParty.LeaderHero;
            if (leaderHero == null || leaderHero == Hero.MainHero)
                return;

            IFaction mapFaction = leaderHero.MapFaction;
            uint color = (mapFaction != null) ? mapFaction.Color : 4291609515U;
            Monster monster = FaceGen.GetMonsterWithSuffix(leaderHero.CharacterObject.Race, "_settlement");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monster, leaderHero.CharacterObject.IsFemale, "_lord");
            AgentData agentData = new AgentData(new PartyAgentOrigin(mobileParty.Party, leaderHero.CharacterObject))
                .Monster(monster).NoHorses(true).ClothingColor1(color).ClothingColor2(color);
            string spawnTag = "sp_notable";
            Location location = MFHCenter;
            if (location != null)
            {
                location.AddCharacter(new LocationCharacter(
                    agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors),
                    spawnTag, true, LocationCharacter.CharacterRelations.Neutral, actionSet, true));
            }
        }

        private void AddNotableLocationCharacters(Settlement settlement)
        {
            if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    this.AddNotableLocationCharacter(notable, settlement);
                }
            }
        }

        private void AddNotableLocationCharacter(Hero notable, Settlement settlement)
        {
            string suffix = notable.IsArtisan ? "_villager_artisan" : (notable.IsMerchant ? "_villager_merchant" : (notable.IsPreacher ? "_villager_preacher" : (Helpers.IsMFGangLeader(notable) ? "_villager_gangleader" : (notable.IsRuralNotable ? "_villager_ruralnotable" : (notable.IsFemale ? "_lord" : "_villager_merchant")))));
            string text = notable.IsArtisan ? "sp_notable_artisan" : (notable.IsMerchant ? "sp_notable_merchant" : (notable.IsPreacher ? "sp_notable_preacher" : (Helpers.IsMFGangLeader(notable) ? "sp_notable_gangleader" : (notable.IsRuralNotable ? "sp_notable_rural_notable" : ((notable.GovernorOf == notable.CurrentSettlement.Town) ? "sp_governor" : "sp_notable")))));
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(notable.CharacterObject.Race, "_settlement");
            AgentData agentData = new AgentData(
                new PartyAgentOrigin(null, notable.CharacterObject)).Monster(monsterWithSuffix).NoHorses(true);

            // TODO maybe use different CharacterRelations depending on peace/war with MF?
            LocationCharacter locationCharacter = new LocationCharacter(agentData,
                new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors),
                text, true, LocationCharacter.CharacterRelations.Neutral,
                ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, notable.IsFemale, suffix), true);

            MFHCenter.AddCharacter(locationCharacter);
        }

        // copypasta from NotablesCampaignBehavior.ChangeDeadNotable()
        private void ChangeDeadNotable(Hero deadNotable, Hero newNotable, Settlement notableSettlement)
        {
            EnterSettlementAction.ApplyForCharacterOnly(newNotable, notableSettlement);
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (newNotable != hero)
                {
                    int relation = deadNotable.GetRelation(hero);
                    if (Math.Abs(relation) >= 20 || (relation != 0 && hero.CurrentSettlement == notableSettlement))
                    {
                        newNotable.SetPersonalRelation(hero, relation);
                    }
                }
            }
            if (deadNotable.Issue != null)
            {
                Campaign.Current.IssueManager.ChangeIssueOwner(deadNotable.Issue, newNotable);
            }
        }

        // copypasta from NotablesCampaignBehavior.OnHeroKilled()
        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (victim.IsNotable && Helpers.IsMFHideout(victim.CurrentSettlement))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{victim} died in {victim.CurrentSettlement}"));
                if (victim.CurrentSettlement != null && victim.CurrentSettlement.OwnerClan.Heroes.Count > 0)
                {
                    Hero hero = HeroCreator.CreateRelativeNotableHero(victim);
                    this.ChangeDeadNotable(victim, hero, victim.CurrentSettlement);
                }
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!Helpers.IsMFHideout(settlement))
                return;

            if (party != null && party.IsMainParty)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    notable.SetHasMet();
                }
                if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null)
                {
                    if (party == null)
                        return;
                    if (party.IsMainParty)
                        this.AddMFHLocationCharacters(settlement);
                    else if (MobileParty.MainParty.CurrentSettlement == settlement)
                        AddPartyHero(party, settlement);
                }
            }
        }

        private void OnMissionEnded(IMission mission)
        {
            if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && !Settlement.CurrentSettlement.IsUnderSiege)
            {
                this.AddMFHLocationCharacters(Settlement.CurrentSettlement);
            }
        }

        public void OnGameLoadFinished()
        {
            MFHideoutManager.InitManagerIfNone();
            if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner
                && LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null)
            {
                this.AddMFHLocationCharacters(Settlement.CurrentSettlement);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static Location MFHCenter
        {
            get
            {
                return LocationComplex.Current.GetLocationWithId("mf_hideout_center");
            }
        }
    }
}

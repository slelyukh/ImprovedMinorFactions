using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using MathF = TaleWorlds.Library.MathF;

namespace ImprovedMinorFactions
{
    internal class MFHideoutCampaignBehavior : CampaignBehaviorBase
    {

        public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            this.AddGameMenus(campaignGameStarter);
            MFHideoutManager.initManagerIfNone();
            MFHideoutManager.Current.ActivateAllFactionHideouts();
        }

        public void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            this.AddGameMenus(campaignGameStarter);
        }

        public void OnGameLoadFinished()
        {
            MFHideoutManager.initManagerIfNone();
            if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner
                && LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null)
            {
                this.AddMFHLocationCharacters(Settlement.CurrentSettlement);
            }
        }
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.HourlyTickSettlement));
            CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase>(this.OnMFHideoutSpotted));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
            // Location events
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));

            // debug listeners
            CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, new Action<MobileParty>(this.DEBUGMFPartyTick));

        }

        

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {

            if (!Helpers.isMFHideout(settlement))
                return;
            
            if (hero != null && hero.IsMinorFactionHero)
                InformationManager.DisplayMessage(new InformationMessage($"{hero} entered {settlement}"));
            if (party != null && party.IsMainParty)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    notable.SetHasMet();
                }
                var test = Campaign.Current.GameMenuManager.MenuLocations;
                if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null)
                {
                    if (party == null)
                        return;
                    if (party.IsMainParty)
                        this.AddMFHLocationCharacters(settlement);
                    else if (MobileParty.MainParty.CurrentSettlement == settlement)
                        AddPartyHero(party, settlement);
                    // add more if you want non party having notables to be able to be visible while you are here
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

        private void DEBUGMFPartyTick(MobileParty party)
        {
            if (party.MapFaction.IsMinorFaction)
            {
                // print something
            }
        }

        private void OnMFHideoutSpotted(PartyBase party, PartyBase mfHideoutParty)
        {
            MinorFactionHideout? mfHideout= mfHideoutParty.Settlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout != null)
            {
                mfHideout.IsSpotted = true;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<float>("_mfHideoutWaitProgressHours", ref this._hideoutWaitProgressHours);
            dataStore.SyncData<float>("_mfHideoutWaitTargetHours", ref this._hideoutWaitTargetHours);
        }


        public void HourlyTickSettlement(Settlement settlement)
        {
            MinorFactionHideout? mfHideout = settlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null || !mfHideout.IsActive)
                return;
            
            if (mfHideout.IsActive && !mfHideout.IsSpotted)
            {
                float hideoutSpottingDistance = Campaign.Current.Models.MapVisibilityModel.GetHideoutSpottingDistance();
                float num = MobileParty.MainParty.Position2D.DistanceSquared(settlement.Position2D);
                float num2 = 1f - num / (hideoutSpottingDistance * hideoutSpottingDistance);
                if (num2 > 0f && MBRandom.RandomFloat < num2 && !mfHideout.IsSpotted)
                {
                    mfHideout.IsSpotted = true;
                    settlement.IsVisible = true;

                    CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, settlement.Party);
                }
            }
        }

        protected void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            // entered hideout menu
            campaignGameStarter.AddGameMenu("mf_hideout_place", "{=!}{MF_HIDEOUT_TEXT}", 
                new OnInitDelegate(this.game_menu_hideout_place_on_init),
                GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "attack", "Attack hideout",
                new GameMenuOption.OnConditionDelegate(this.menu_attack_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.menu_attack_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "recruit_volunteers", "Recruit troops", 
                new GameMenuOption.OnConditionDelegate(this.recruit_troops_on_condition), 
                new GameMenuOption.OnConsequenceDelegate(this.recruit_troops_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "trade", "Buy products", 
                new GameMenuOption.OnConditionDelegate(this.menu_trade_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.menu_trade_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "wait", "Wait until nightfall",
                new GameMenuOption.OnConditionDelegate(this.menu_wait_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.menu_wait_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "leave", "Leave",
                new GameMenuOption.OnConditionDelegate(this.menu_leave_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.menu_leave_on_consequence));

            // wait until nightfall menu
            campaignGameStarter.AddWaitGameMenu("mf_hideout_wait", "Waiting until nightfall to attack.",
                null,
                new OnConditionDelegate(this.wait_menu_start_on_condition),
                null,
                new OnTickDelegate(this.wait_menu_on_tick),
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
                GameOverlays.MenuOverlayType.SettlementWithCharacters,
                this._hideoutWaitTargetHours);
            campaignGameStarter.AddGameMenuOption("mf_hideout_wait", "leave", "Leave",
                new GameMenuOption.OnConditionDelegate(this.menu_leave_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.menu_leave_on_consequence));

        }

        public bool menu_attack_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Raid;
            
            if (Hero.MainHero.IsWounded)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You cannot attack while wounded");
            }
            else
            {
                args.IsEnabled = true;
                args.Tooltip = new TextObject($"Attacking this hideout will significantly decrease your relation with this Clan");
            }
            return true;
        }

        public void menu_attack_on_consequence(MenuCallbackArgs args)
        {

            int maxPlayerTroops = MFHideoutModels.GetPlayerMaximumTroopCountForRaidMission(MobileParty.MainParty);
            TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
            troopRoster.Add(MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, maxPlayerTroops, true));
            
            args.MenuContext.OpenTroopSelection(
                MobileParty.MainParty.MemberRoster, 
                troopRoster, 
                new Func<CharacterObject, bool>(this.CanChangeStatusOfTroop), 
                new Action<TroopRoster>(this.OnTroopRosterManageDone), 
                maxPlayerTroops, 
                1
            );

            Settlement curSettlement = Settlement.CurrentSettlement;
        }

        public bool recruit_troops_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            Settlement curSettlement = Settlement.CurrentSettlement;
            if (Clan.PlayerClan.GetRelationWithClan(curSettlement.OwnerClan) <0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("your relation with this clan is too low to recruit troops");
            } else
            {
                args.IsEnabled = true;
            }
            return true;
        }

        public void recruit_troops_on_consequence(MenuCallbackArgs args)
        {
            InformationManager.DisplayMessage(new InformationMessage($"recruitTroops Pressed"));
        }

        public bool menu_trade_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }

        public void menu_trade_on_consequence(MenuCallbackArgs args)
        {
            InformationManager.DisplayMessage(new InformationMessage($"trade Pressed"));
            
        }
        private bool menu_wait_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            //Settlement curSettlement = Settlement.CurrentSettlement;
            //Clan.PlayerClan.GetRelationWithClan(curSettlement.OwnerClan);
            // return relation > 20
            return true;
        }

        private void menu_wait_on_consequence(MenuCallbackArgs args)
        {
            Settlement curSettlement = Settlement.CurrentSettlement;
            InformationManager.DisplayMessage(new InformationMessage($"my relation with this clan =  {Clan.PlayerClan.GetRelationWithClan(curSettlement.OwnerClan)}"));
            GameMenu.SwitchToMenu("mf_hideout_wait");
        }
        private bool menu_leave_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private void menu_leave_on_consequence(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            if (MobileParty.MainParty.CurrentSettlement != null)
            {
                PlayerEncounter.LeaveSettlement();
            }
            PlayerEncounter.Finish(true);
        }
        public bool wait_menu_start_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        public void wait_menu_on_tick(MenuCallbackArgs args, CampaignTime campaignTime)
        {
            this._hideoutWaitProgressHours += (float)campaignTime.ToHours;
            if (this._hideoutWaitTargetHours.ApproximatelyEqualsTo(0f, 1E-05f))
            {
                this.CalculateHideoutAttackTime();
            }
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(this._hideoutWaitProgressHours / this._hideoutWaitTargetHours);
        }



        private bool IsHideoutAttackableNow()
        {
            float currentHourInDay = CampaignTime.Now.CurrentHourInDay;
            return (this.CanAttackHideoutStart > this.CanAttackHideoutEnd && (currentHourInDay >= (float)this.CanAttackHideoutStart || currentHourInDay <= (float)this.CanAttackHideoutEnd)) || (this.CanAttackHideoutStart < this.CanAttackHideoutEnd && currentHourInDay >= (float)this.CanAttackHideoutStart && currentHourInDay <= (float)this.CanAttackHideoutEnd);
        }

        [GameMenuEventHandler("mf_hideout_place", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_ui_recruit_volunteers_on_consequence(MenuCallbackArgs args)
        {
            args.MenuContext.OpenRecruitVolunteers();
        }



        //public void hideout_wait_menu_on_consequence(MenuCallbackArgs args)
        //{
        //    GameMenu.SwitchToMenu("hideout_after_wait");
        //}


        [GameMenuInitializationHandler("mf_hideout_place")]
        private static void game_menu_hideout_ui_place_on_init(MenuCallbackArgs args)
        {
        }

        [GameMenuInitializationHandler("mf_hideout_place")]
        private static void game_menu_hideout_sound_place_on_init(MenuCallbackArgs args)
        {
            args.MenuContext.SetPanelSound("event:/ui/panels/settlement_hideout");
        }

        private static void UpdateMenuLocations(string menuID)
        {
            Campaign.Current.GameMenuManager.MenuLocations.Clear();
            Settlement settlement = ((Settlement.CurrentSettlement == null) ? MobileParty.MainParty.CurrentSettlement : Settlement.CurrentSettlement);
            if (menuID == "mf_hideout_place")
            {
                Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("mf_hideout_center"));
            }
            else if (menuID == "mf_hideout_wait")
            {
                // nothing for now
            }
        }
        

        private void game_menu_hideout_place_on_init(MenuCallbackArgs args)
        {
            Settlement curSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = curSettlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return;

            args.MenuContext.SetBackgroundMeshName(curSettlement.SettlementComponent.WaitMeshName);
            UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
            this._hideoutWaitProgressHours = 0f;
            if (!this.IsHideoutAttackableNow())
            {
                this.CalculateHideoutAttackTime();
            }
            else
            {
                this._hideoutWaitTargetHours = 0f;
            }

            // TODO: calculate num of defenders better
            int num = 1;
            if (!mfHideout.NextPossibleAttackTime.IsPast)
            {
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "The remains of a fire suggest that it's been recently occupied, but its residents - whoever they are - are well-hidden for now.");
            }
            else if (num > 0)
            {
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "You see armed men moving about. As you listen quietly, you hear scraps of conversation about raids, ransoms, and the best places to waylay travellers.");
            }
            else
            {
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "There seems to be no one inside.");
            }
            if (mfHideout.NextPossibleAttackTime.IsPast && num > 0 && Hero.MainHero.IsWounded)
            {
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "You can not attack since your wounds do not allow you.");
            }
            if (MobileParty.MainParty.CurrentSettlement == null)
            {
                PlayerEncounter.EnterSettlement();
            }
            bool isActive = mfHideout.IsActive;

            if (PlayerEncounter.Battle != null)
            {
                bool playerWon = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Current.PlayerSide;
                PlayerEncounter.Update();
                if (curSettlement != null && playerWon)
                {
                    this.SetCleanHideoutRelations(mfHideout);
                }
            }
        }

        private void CalculateHideoutAttackTime()
        {
            this._hideoutWaitTargetHours = (((float)this.CanAttackHideoutStart > CampaignTime.Now.CurrentHourInDay) ? ((float)this.CanAttackHideoutStart - CampaignTime.Now.CurrentHourInDay) : (24f - CampaignTime.Now.CurrentHourInDay + (float)this.CanAttackHideoutStart));
        }

        private void SetCleanHideoutRelations(MinorFactionHideout mfHideout)
        {
            if (!mfHideout.OwnerClan.IsOutlaw)
                return;
            
            var settlement = mfHideout.Settlement;
            List<Settlement> nearbyVillages = new List<Settlement>();
            foreach (Village village in Village.All)
            {
                if (village.Settlement.Position2D.DistanceSquared(settlement.Position2D) <= 1600f)
                {
                    nearbyVillages.Add(village.Settlement);
                }
            }
            foreach (Settlement village in nearbyVillages)
            {
                if (settlement.Notables.Count > 0)
                {
                    ChangeRelationAction.ApplyPlayerRelation(village.Notables.GetRandomElement<Hero>(), 2, true, false);
                }
            }
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
            {
                Town town = SettlementHelper.FindNearestTown(null, settlement).Town;
                Hero leader = town.OwnerClan.Leader;
                if (leader == Hero.MainHero)
                {
                    town.Loyalty += 1f;
                }
                else
                {
                    ChangeRelationAction.ApplyPlayerRelation(leader, (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus, true, true);
                }
            }
            MBTextManager.SetTextVariable("RELATION_VALUE", (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
            MBInformationManager.AddQuickInformation(new TextObject("{=o0qwDa0q}Your relation increased by {RELATION_VALUE} with nearby notables.", null), 0, null, "");
        }

        // copied from regular hideouts, does nothing right now
        private void ArrangeHideoutTroopCountsForMission()
        {
            MBList<MobileParty> hideoutParties = Enumerable.Where<MobileParty>(
                Settlement.CurrentSettlement.Parties, (MobileParty x) => x.IsMilitia).ToMBList<MobileParty>();
        }

        private void OnTroopRosterManageDone(TroopRoster playerTroops)
        {
            // Declare war
            BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);

            this.ArrangeHideoutTroopCountsForMission();
            GameMenu.SwitchToMenu("mf_hideout_place");
            var mfHideout = Settlement.CurrentSettlement.SettlementComponent as MinorFactionHideout; 
            if (mfHideout == null) {
                return;
            }
            mfHideout.UpdateNextPossibleAttackTime();
            if (PlayerEncounter.IsActive)
            {
                PlayerEncounter.LeaveEncounter = false;
            }
            else
            {
                PlayerEncounter.Start();
                PlayerEncounter.Current.SetupFields(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
            }
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            CampaignMission.OpenHideoutBattleMission(mfHideout.SceneName, playerTroops.ToFlattenedRoster());
        }

        private bool CanChangeStatusOfTroop(CharacterObject character)
        {
            return !character.IsPlayerCharacter && !character.IsNotTransferableInHideouts;
        }

        private void AddMFHLocationCharacters(Settlement settlement)
        {
            if (Helpers.isMFHideout(settlement))
            {
                List<MobileParty> list = Enumerable.ToList<MobileParty>(Settlement.CurrentSettlement.Parties);
                this.AddNotableLocationCharacters(settlement);
                foreach (MobileParty mobileParty in list)
                {
                    this.AddPartyHero(mobileParty, settlement);
                }
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
            string suffix = notable.IsArtisan ? "_villager_artisan" : (notable.IsMerchant ? "_villager_merchant" : (notable.IsPreacher ? "_villager_preacher" : (notable.IsGangLeader ? "_villager_gangleader" : (notable.IsRuralNotable ? "_villager_ruralnotable" : (notable.IsFemale ? "_lord" : "_villager_merchant")))));
            string text = notable.IsArtisan ? "sp_notable_artisan" : (notable.IsMerchant ? "sp_notable_merchant" : (notable.IsPreacher ? "sp_notable_preacher" : (notable.IsGangLeader ? "sp_notable_gangleader" : (notable.IsRuralNotable ? "sp_notable_rural_notable" : ((notable.GovernorOf == notable.CurrentSettlement.Town) ? "sp_governor" : "sp_notable")))));
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

        // Adds PartyHero character to MFHideout center to allow dialog
        private void AddPartyHero(MobileParty mobileParty, Settlement settlement)
        {
            Hero leaderHero = mobileParty.LeaderHero;
            if (leaderHero == null || leaderHero == Hero.MainHero)
                return;

            IFaction mapFaction = leaderHero.MapFaction;
            uint color = (mapFaction != null) ? mapFaction.Color : 4291609515U;

            (string actionSet, Monster monster) = GetActionSetAndMonster(leaderHero.CharacterObject);
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

        private static Tuple<string, Monster> GetActionSetAndMonster(CharacterObject character)
        {
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
            return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, character.IsFemale, "_lord"), monsterWithSuffix);
        }

        private const int MaxDistanceSquaredBetweenHideoutAndBoundVillage = 1600;

        private readonly int CanAttackHideoutStart = 23;

        private readonly int CanAttackHideoutEnd = 2;

        private float _hideoutWaitProgressHours;

        private float _hideoutWaitTargetHours;

        private static Location MFHCenter
        {
            get
            {
                return LocationComplex.Current.GetLocationWithId("mf_hideout_center");
            }
        }


    }
}

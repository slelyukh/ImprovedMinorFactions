using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using MathF = TaleWorlds.Library.MathF;
using Options = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption;

namespace ImprovedMinorFactions
{
    internal class MFHideoutCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.HourlyTickSettlement));
            CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase>(this.OnMFHideoutSpotted));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
            // debug listeners
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTick));
            CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, new Action<MobileParty>(this.DEBUGMFPartyTick));
        }

        public void OnDailyTick()
        {
            // one active hideout per clan assertion
            MFHideoutManager.Current.ValidateMaxOneActiveHideoutPerClan();
        }

        private void DEBUGMFPartyTick(MobileParty party)
        {
            if (party.MapFaction.IsMinorFaction)
            {
                // print something
            }
        }

        private void OnClanDestroyed(Clan destroyedClan)
        {
            if (destroyedClan.IsMinorFaction)
                MFHideoutManager.Current.RemoveClan(destroyedClan);
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
        {
            if (clan.IsMinorFaction && detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary)
                MFHideoutManager.Current.DeclareWarOnPlayerIfNeeded(clan);
        }

        private void OnMFHideoutSpotted(PartyBase party, PartyBase mfHideoutParty)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(mfHideoutParty.Settlement);
            if (mfHideout != null)
                mfHideout.IsSpotted = true;
        }


        public void HourlyTickSettlement(Settlement settlement)
        {

            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(settlement);
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
            campaignGameStarter.AddGameMenu("mf_hideout_place", "{=!}{MF_HIDEOUT_TEXT}", 
                new OnInitDelegate(game_menu_hideout_place_on_init),
                GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "attack", "{=zxMOqlhs}Attack Hideout",
                new Options.OnConditionDelegate(menu_attack_on_condition),
                new Options.OnConsequenceDelegate(menu_attack_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "hostile_action", "{=GM3tAYMr}Take Hostile Action",
                new Options.OnConditionDelegate(menu_hostile_action_on_condition),
                new Options.OnConsequenceDelegate(menu_hostile_action_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "recruit_volunteers", "{=nRm78XAk}Recruit troops", 
                new Options.OnConditionDelegate(recruit_troops_on_condition), 
                new Options.OnConsequenceDelegate(recruit_troops_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "wait", "{=zEoHYEUS}Wait here for some time",
                new Options.OnConditionDelegate(menu_wait_on_condition),
                new Options.OnConsequenceDelegate(menu_wait_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "leave", "{=3sRdGQou}Leave",
                new Options.OnConditionDelegate(menu_leave_on_condition),
                new Options.OnConsequenceDelegate(menu_leave_on_consequence));

            campaignGameStarter.AddWaitGameMenu("mf_hideout_wait", "{=ydbVysqv}You are waiting in {CURRENT_SETTLEMENT}.", 
                new OnInitDelegate(wait_menu_on_init), 
                new OnConditionDelegate(menu_wait_on_condition), 
                null, 
                new OnTickDelegate(wait_menu_on_tick),
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
                GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_wait", "stop_waiting", "{=UqDNAZqM}Stop waiting", 
                new Options.OnConditionDelegate(menu_leave_on_condition), 
                new Options.OnConsequenceDelegate(stop_wait_on_consequence));

            campaignGameStarter.AddGameMenu("mf_hideout_hostile_action", "{=YVNZaVCA}What action do you have in mind?", 
                new OnInitDelegate(hostile_menu_on_init), 
                GameOverlays.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_hostile_action", "attack", "{=zxMOqlhs}Attack Hideout",
                new Options.OnConditionDelegate(hostile_action_menu_attack_on_condition), 
                new Options.OnConsequenceDelegate(menu_attack_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_hostile_action", "forget_it", "{=sP9ohQTs}Forget it", 
                new Options.OnConditionDelegate(menu_leave_on_condition), 
                new Options.OnConsequenceDelegate(menu_hostile_action_forget_on_consequence));
        }

        private void ProcessMenusForAttack(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Raid;
            if (Hero.MainHero.IsWounded)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=Fv1F13jxM}You cannot attack while wounded");
            }
            else
            {
                args.IsEnabled = true;
                args.Tooltip = new TextObject("{=cl7GYHwgi}Attacking this hideout will significantly decrease your relation with this Clan");
            }
        }

        public bool menu_attack_on_condition(MenuCallbackArgs args)
        {
            ProcessMenusForAttack(args);
            return Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
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
        }

        public bool menu_hostile_action_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Submenu;
            args.Tooltip = new TextObject("{=1PM860Jco}Taking hostile action will start a war between you and this Clan or whoever hired them.");
            return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
        }

        public void menu_hostile_action_on_consequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("mf_hideout_hostile_action");
        }

        public bool recruit_troops_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Recruit;
            Settlement curSettlement = Settlement.CurrentSettlement;
            if (Clan.PlayerClan.GetRelationWithClan(curSettlement.OwnerClan) < MFHideoutModels.MinRelationToBeMFHFriend)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=pzEj8yvXS}Your relation with this clan must be at least {MIN_RELATION} to recruit troops.")
                    .SetTextVariable("MIN_RELATION", MFHideoutModels.MinRelationToBeMFHFriend);
            } else
            {
                args.IsEnabled = true;
            }
            return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
        }

        public void recruit_troops_on_consequence(MenuCallbackArgs args)
        {
        }

        private bool menu_wait_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Wait;
            MBTextManager.SetTextVariable("CURRENT_SETTLEMENT", Settlement.CurrentSettlement.Name);
            var curSettlement = Settlement.CurrentSettlement;
            if (Clan.PlayerClan.GetRelationWithClan(curSettlement.OwnerClan) < MFHideoutModels.MinRelationToBeMFHFriend)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=92PPJNVSa}You don't have enough relation to stay in this hideout.");
            }
            return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
        }

        private void menu_wait_on_consequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("mf_hideout_wait");
        }
        
        private bool menu_leave_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Leave;
            return true;
        }

        private void menu_leave_on_consequence(MenuCallbackArgs args)
        {
            if (MobileParty.MainParty.CurrentSettlement != null)
                PlayerEncounter.LeaveSettlement();
            PlayerEncounter.Finish(true);
        }

        private void wait_menu_on_init(MenuCallbackArgs args)
        {
            if (PlayerEncounter.Current != null)
                PlayerEncounter.Current.IsPlayerWaiting = true;
        }
        public bool wait_menu_start_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Wait;
            return true;
        }

        public void wait_menu_on_tick(MenuCallbackArgs args, CampaignTime dt)
        {
            this._hideoutWaitProgressHours += (float)dt.ToHours;
            if (this._hideoutWaitTargetHours.ApproximatelyEqualsTo(0f, 1E-05f))
                this.CalculateHideoutAttackTime();
            //args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(this._hideoutWaitProgressHours / this._hideoutWaitTargetHours); NIGHT MODE

            SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
        }

        public void stop_wait_on_consequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
        }

        // Moves player out of wait menu if anything of note occurs
        private void SwitchToMenuIfThereIsAnInterrupt(string currentMenuId)
        {
            string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
            if (genericStateMenu != currentMenuId)
            {
                if (!string.IsNullOrEmpty(genericStateMenu))
                {
                    GameMenu.SwitchToMenu(genericStateMenu);
                    return;
                }
                GameMenu.ExitToLast();
            }
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
                Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("mf_hideout_center"));
        }
        
        private void game_menu_hideout_place_on_init(MenuCallbackArgs args)
        {
            Settlement curSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return;

            args.MenuContext.SetBackgroundMeshName(curSettlement.SettlementComponent.WaitMeshName);
            UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
            this._hideoutWaitProgressHours = 0f;
            if (!this.IsHideoutAttackableNow())
                this.CalculateHideoutAttackTime();
            else
                this._hideoutWaitTargetHours = 0f;
            
            GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "");

            int num = 1;
            if (!mfHideout.NextPossibleAttackTime.IsPast)
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{=KLWn6yZQ}{HIDEOUT_DESCRIPTION} The remains of a fire suggest that it's been recently occupied," +
                    " but its residents - whoever they are - are well-hidden for now.");
            else if (num > 0)
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{=prcBBqMR}{HIDEOUT_DESCRIPTION} You see armed men moving about. As you listen quietly, you hear scraps" +
                    " of conversation about raids, ransoms, and the best places to waylay travellers.");
            else
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{=gywyEgZa}{HIDEOUT_DESCRIPTION} There seems to be no one inside.");
            
            if (mfHideout.NextPossibleAttackTime.IsPast && num > 0 && Hero.MainHero.IsWounded)
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{=fMekM2UH}{HIDEOUT_DESCRIPTION} You can not attack since your wounds do not allow you.");


            if (MobileParty.MainParty.CurrentSettlement == null)
                PlayerEncounter.EnterSettlement();

            if (PlayerEncounter.Battle != null)
            {
                bool playerWon = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Current.PlayerSide;
                PlayerEncounter.Update();
                if (curSettlement != null && playerWon)
                    this.SetCleanHideoutRelations(mfHideout);
            }
        }

        private void menu_hostile_action_forget_on_consequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("mf_hideout_place");
        }
        private void hostile_menu_on_init(MenuCallbackArgs args)
        {
        }
        private bool hostile_action_menu_attack_on_condition(MenuCallbackArgs args)
        {
            this.ProcessMenusForAttack(args);
            return true;
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
                if (village.Settlement.Position2D.DistanceSquared(settlement.Position2D) <= MaxDistanceSquaredBetweenHideoutAndBoundVillage)
                    nearbyVillages.Add(village.Settlement);
            }
            foreach (Settlement village in nearbyVillages)
            {
                if (settlement.Notables.Count > 0)
                    ChangeRelationAction.ApplyPlayerRelation(village.Notables.GetRandomElement<Hero>(), 2, true, false);
            }
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
            {
                Town town = SettlementHelper.FindNearestTown(null, settlement).Town;
                Hero leader = town.OwnerClan.Leader;
                if (leader == Hero.MainHero)
                    town.Loyalty += 1f;
                else
                    ChangeRelationAction.ApplyPlayerRelation(leader, (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus, true, true);
            }
            MBTextManager.SetTextVariable("RELATION_VALUE", (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
            MBInformationManager.AddQuickInformation(new TextObject("{=o0qwDa0q}Your relation increased by {RELATION_VALUE} with nearby notables.", null), 0, null, "");
        }

        private void ArrangeHideoutTroopCountsForMission()
        {
            MBList<MobileParty> hideoutParties = Enumerable.Where<MobileParty>(
                Settlement.CurrentSettlement.Parties, (MobileParty x) => x.IsMilitia).ToMBList();
        }

        private void ApplyHideoutRaidConsequences()
        {
            ApplyHideoutRaidConsequences(Settlement.CurrentSettlement);
        }

        public static void ApplyHideoutRaidConsequences(Settlement mfHideout)
        {
            Clan mfClan = mfHideout.OwnerClan;
            BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, mfHideout.Party);
            if (mfClan.IsUnderMercenaryService)
            {
                ChangeRelationAction.ApplyPlayerRelation(mfClan.Leader, -20);
                MFHideoutManager.Current.RegisterClanForPlayerWarOnEndingMercenaryContract(mfClan);
            }
            else
            {
                ChangeRelationAction.ApplyPlayerRelation(mfClan.Leader, -10);
            }

            foreach (Hero notable in mfHideout.Notables)
            {
                ChangeRelationAction.ApplyPlayerRelation(notable, -15);
            }
        }

        private void OnTroopRosterManageDone(TroopRoster playerTroops)
        {
            ApplyHideoutRaidConsequences();

            this.ArrangeHideoutTroopCountsForMission();
            GameMenu.SwitchToMenu("mf_hideout_place");
            var mfHideout = Helpers.GetMFHideout(Settlement.CurrentSettlement); 
            if (!Helpers.IsMFHideout(Settlement.CurrentSettlement))
                return;
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

        private const int MaxDistanceSquaredBetweenHideoutAndBoundVillage = 1600;

        private readonly int CanAttackHideoutStart = 23;

        private readonly int CanAttackHideoutEnd = 2;

        private float _hideoutWaitProgressHours;

        private float _hideoutWaitTargetHours;

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<float>("_mfHideoutWaitProgressHours", ref this._hideoutWaitProgressHours);
            dataStore.SyncData<float>("_mfHideoutWaitTargetHours", ref this._hideoutWaitTargetHours);
        }

        public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            this.AddGameMenus(campaignGameStarter);
            MFHideoutManager.InitManagerIfNone();
            MFHideoutManager.Current.ActivateAllFactionHideouts();
        }

        public void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            this.AddGameMenus(campaignGameStarter);
            MFHideoutManager.InitManagerIfNone();
            MFHideoutManager.Current.ActivateAllFactionHideouts();
        }
    }

    // NIGHT MODE archive

    //public void hideout_wait_menu_on_consequence(MenuCallbackArgs args)
    //{
    //    GameMenu.SwitchToMenu("hideout_after_wait");
    //}

    // wait until nightfall menu
    //campaignGameStarter.AddWaitGameMenu("mf_hideout_wait", "Waiting until nightfall to attack.",
    //    null,
    //    new OnConditionDelegate(this.wait_menu_start_on_condition),
    //    null,
    //    new OnTickDelegate(this.wait_menu_on_tick),
    //    GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption,
    //    GameOverlays.MenuOverlayType.SettlementWithCharacters,
    //    this._hideoutWaitTargetHours);
    //campaignGameStarter.AddGameMenuOption("mf_hideout_wait", "leave", "Leave",
    //    new Options.OnConditionDelegate(this.menu_leave_on_condition),
    //    new Options.OnConsequenceDelegate(this.menu_leave_on_consequence));
}

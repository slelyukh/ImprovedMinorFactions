using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using ImprovedMinorFactions.Patches;
using MathF = TaleWorlds.Library.MathF;
using Options = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    /// <summary>
    /// The main camp behavior: the camp menus (enter, attack, hostile action, recruit, wait), raiding a camp and what it costs
    /// in relations, spotting camps on the map, and activating every clan's camps on a new game or load.
    /// </summary>
    internal class MFHideoutCampaignBehavior : CampaignBehaviorBase
    {

        /// <summary>
        /// A clan another mod spawns gets its camp with its first party, see LoadXmlHideoutsPatch.SpawnGatedClans.
        /// </summary>
        private void OnPartyCreated(MobileParty party)
        {
            try
            {
                Clan? clan = party?.ActualClan;
                if (clan == null || IMFManager.Current == null || !ImprovedMinorFactions.Patches.LoadXmlHideoutsPatch.SpawnGatedClans.Contains(clan.StringId))
                    return;
                IMFManager.Current.ActivateFirstHideoutFor(clan);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("OnPartyCreated failed:", e);
            }
        }
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(HourlyTickSettlement));
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(DailyTickSettlement));
            CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, new Action<PartyBase, PartyBase>(OnMFHideoutSpotted));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnAfterSessionLaunched));
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(OnClanDestroyed));
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(OnPartyCreated));
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(OnClanChangedKingdom));
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(OnHeroCreated));
        }
        private void OnClanDestroyed(Clan destroyedClan)
        {
            if (destroyedClan.IsMinorFaction)
                IMFManager.Current!.RemoveClan(destroyedClan);
        }

        /// <summary>
        /// When a minor faction's mercenary contract ends, starts the war with the player it was holding back.
        /// </summary>
        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
        {
            if (clan.IsMinorFaction && detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary)
                IMFManager.Current!.DeclareWarOnPlayerIfNeeded(clan);
        }

        private void OnMFHideoutSpotted(PartyBase party, PartyBase mfHideoutParty)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(mfHideoutParty.Settlement);
            if (mfHideout != null)
                mfHideout.IsSpotted = true;
        }


        /// <summary>
        /// Gives starting volunteers if we refill them ourselves, and lets the player spot an active camp
        /// with a chance that grows the closer they are.
        /// </summary>
        public void HourlyTickSettlement(Settlement settlement)
        {

            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null || !mfHideout.IsActive)
                return;

            if (_refillVolunteersOurselves)
                GiveStartingVolunteers(settlement);

            if (mfHideout.IsActive && !mfHideout.IsSpotted)
            {
                float hideoutSpottingDistance = Campaign.Current.Models.MapVisibilityModel.GetHideoutSpottingDistance();
                float num = MobileParty.MainParty.GetPosition2D.DistanceSquared(settlement.GetPosition2D);
                float num2 = 1f - num / (hideoutSpottingDistance * hideoutSpottingDistance);
                if (num2 > 0f && MBRandom.RandomFloat < num2 && !mfHideout.IsSpotted)
                {
                    mfHideout.PlayerSpotHideout();
                }
            }
        }

        /// <summary>
        /// Adds all camp menus and their options.
        /// </summary>
        protected void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            // TODO: Better nomad texts
            
            campaignGameStarter.AddGameMenu("mf_hideout_place", "{=!}{MF_HIDEOUT_TEXT}",
                new OnInitDelegate(game_menu_hideout_place_on_init),
                              GameMenu.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "walk_around", "{=!}Take a walk around the camp",
              new Options.OnConditionDelegate(menu_walk_around_on_condition),
              new Options.OnConsequenceDelegate(menu_walk_around_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "attack", "{=!}{MF_HIDEOUT_ATTACK}",
                new Options.OnConditionDelegate(menu_attack_on_condition),
                new Options.OnConsequenceDelegate(menu_attack_on_consequence));
            
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "hostile_action", "{=GM3tAYMr}Take Hostile Action",
                new Options.OnConditionDelegate(menu_hostile_action_on_condition),
                new Options.OnConsequenceDelegate(menu_hostile_action_on_consequence));
            campaignGameStarter.AddGameMenuOption("mf_hideout_place", "recruit_volunteers", "{=E31IJyqs}Recruit troops",
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
                GameMenu.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_wait", "stop_waiting", "{=UqDNAZqM}Stop waiting",
                new Options.OnConditionDelegate(menu_leave_on_condition),
                new Options.OnConsequenceDelegate(stop_wait_on_consequence));

            campaignGameStarter.AddGameMenu("mf_hideout_hostile_action", "{=YVNZaVCA}What action do you have in mind?",
                new OnInitDelegate(hostile_menu_on_init),
                GameMenu.MenuOverlayType.SettlementWithBoth);
            campaignGameStarter.AddGameMenuOption("mf_hideout_hostile_action", "attack",
                    "{=!}{MF_HIDEOUT_ATTACK}",
                    new Options.OnConditionDelegate(hostile_action_menu_attack_on_condition),
                    new Options.OnConsequenceDelegate(menu_attack_on_consequence));

            campaignGameStarter.AddGameMenuOption("mf_hideout_hostile_action", "forget_it", "{=sP9ohQTs}Forget it",
                new Options.OnConditionDelegate(menu_leave_on_condition),
                new Options.OnConsequenceDelegate(menu_hostile_action_forget_on_consequence));

            campaignGameStarter.AddGameMenu("mf_hideout_nomads_left",
                "{=WAkOJrsBf}The camp is being dismantled because the land here has been used up.",
                new OnInitDelegate(game_menu_hideout_place_on_init),
                GameMenu.MenuOverlayType.SettlementWithParties);
            campaignGameStarter.AddGameMenuOption("mf_hideout_nomads_left", "leave", "{=3sRdGQou}Leave",
                new Options.OnConditionDelegate(menu_leave_on_condition),
                new Options.OnConsequenceDelegate(menu_nomad_leave_on_consequence));
        }

        /// <summary>
        /// Sets the attack option's icon and tooltip, and disables it while the player is wounded.
        /// </summary>
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
                args.Tooltip = new TextObject("{=cl7GYHwgi}Attacking this place will significantly decrease your relation with this Clan");
            }
        }

        /// <summary>
        /// Checks if a camp is friendly to the player: not at war, and either the player's own faction or friendly by Helpers.IsFriendlyCamp.
        /// </summary>
        private static bool IsFriendlyToPlayer(Settlement? camp)
        {
            if (camp == null)
                return false;
            if (camp.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
                return false;
            // good relations alone don't count, those camps can still be attacked
            return Helpers.IsFriendlyCamp(camp)
                || camp.MapFaction == Hero.MainHero.MapFaction;
        }

        /// <summary>
        /// Shows the attack option for active camps of clans at war with the player that aren't the player's own.
        /// </summary>
        public bool menu_attack_on_condition(MenuCallbackArgs args)
        {
            ProcessMenusForAttack(args);
            Settlement? camp = CurrentCamp();
            return camp != null
                && camp.IsActive
                && camp.OwnerClan != Clan.PlayerClan
                && camp.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
        }

        /// <summary>
        /// Starts the raid. Nomad camps are a field battle in a village scene that fits the ground,
        /// other camps open the troop selection for a hideout battle. Friendly camps just go back to the camp menu.
        /// </summary>
        public void menu_attack_on_consequence(MenuCallbackArgs args)
        {
            var curSettlement = CurrentCamp()!;
            if (IsFriendlyToPlayer(curSettlement))
            {
                GameMenu.SwitchToMenu("mf_hideout_place");
                return;
            }
            if (curSettlement.OwnerClan.IsNomad)
            {
                PrepareForBattle();
                var nomadHideout = Helpers.GetMFHideout(curSettlement)!;
                nomadHideout.EnsureTerrainFittingScene();
                string battleScene = HideoutScenes.ResolveNomadVillage(nomadHideout);
                if (string.IsNullOrEmpty(battleScene))
                    battleScene = nomadHideout.SceneName;
                CampaignMission.OpenBattleMission(battleScene, true);
                return;
            }

            int maxPlayerTroops = IMFModels.GetPlayerMaximumTroopCountForRaidMission(MobileParty.MainParty);
            TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
            troopRoster.Add(MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, maxPlayerTroops, true));

            args.MenuContext.OpenTroopSelection(
                MobileParty.MainParty.MemberRoster,
                troopRoster,
                new Func<CharacterObject, bool>(CanChangeStatusOfTroop),
                new Action<TroopRoster>(OnTroopRosterManageDone),
                maxPlayerTroops,
                1
            );
        }

        /// <summary>
        /// Applies the raid's relation costs, makes sure the camp has defenders and sets up the encounter with the player as attacker.
        /// </summary>
        public void PrepareForBattle()
        {
            var curSettlement = Settlement.CurrentSettlement;
            ApplyHideoutRaidConsequences();

            ArrangeHideoutTroopCountsForMission();
            GameMenu.SwitchToMenu("mf_hideout_place");
            var mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return;
            mfHideout.UpdateNextPossibleAttackTime();
            if (!PlayerEncounter.IsActive)
            {
                PlayerEncounter.Start();
            }

            PlayerEncounter.Current.SetupFields(PartyBase.MainParty, curSettlement.Party);
            Helpers.SetPlayerEncounterSides(PlayerEncounter.Current, BattleSideEnum.Attacker, BattleSideEnum.Defender);
            PlayerEncounter.LeaveEncounter = false;

            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }

            Helpers.SetPlayerEncounterSides(PlayerEncounter.Current, BattleSideEnum.Attacker, BattleSideEnum.Defender);
        }

        /// <summary>
        /// Shows the hostile action option for active camps the player isn't at war with and that aren't friendly or the player's own.
        /// </summary>
        public bool menu_hostile_action_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Submenu;
            args.Tooltip = new TextObject("{=1PM860Jco}Taking hostile action will start a war between you and this Clan or whoever hired them.");
            try
            {
                Settlement camp = CurrentCamp()!;
                bool friendly = IsFriendlyToPlayer(camp);
                return Clan.PlayerClan != camp.OwnerClan
                    && !camp.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)
                    && !friendly
                    && camp.IsActive;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("menu_hostile_action_on_condition failed, option hidden:", e);
                return false;
            }
        }

        public void menu_hostile_action_on_consequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("mf_hideout_hostile_action");
        }

        private bool menu_walk_around_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Mission;
            Settlement curSettlement = Settlement.CurrentSettlement;
            if (curSettlement == null)
                return false;
            return !curSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && curSettlement.IsActive;
        }

        private void menu_walk_around_on_consequence(MenuCallbackArgs args)
        {
            TaleWorlds.CampaignSystem.Settlements.Locations.Location? location = TaleWorlds.CampaignSystem.Settlements.Locations.LocationComplex.Current?.GetLocationWithId("mf_hideout_center");
            if (location != null && PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, null, null);
            }
        }

        /// <summary>
        /// Shows recruiting at camps not at war with the player. Disabled until the player is a friend of the clan.
        /// </summary>
        public bool recruit_troops_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Recruit;
            Settlement curSettlement = Settlement.CurrentSettlement;
            if (!Helpers.IsPlayerFriendOfMF(curSettlement.OwnerClan))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=pzEj8yvXS}Your relation with this clan must be at least {MIN_RELATION} to recruit troops.")
                    .SetTextVariable("MIN_RELATION", IMFModels.MinRelationToBeMFHFriend);
            }
            else
            {
                args.IsEnabled = true;
            }
            return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Settlement.CurrentSettlement.IsActive;
        }

        public void recruit_troops_on_consequence(MenuCallbackArgs args)
        {
        }

        /// <summary>
        /// Shows waiting at camps not at war with the player. Disabled until the player is a friend of the clan.
        /// </summary>
        private bool menu_wait_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = Options.LeaveType.Wait;
            MBTextManager.SetTextVariable("CURRENT_SETTLEMENT", Settlement.CurrentSettlement.Name);
            var curSettlement = Settlement.CurrentSettlement;
            if (!Helpers.IsPlayerFriendOfMF(curSettlement.OwnerClan))
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=92PPJNVSa}You don't have enough relation to stay here.");
            }
            return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && Settlement.CurrentSettlement.IsActive;
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
            // otherwise waiting walks the party right back into the camp
            MobileParty.MainParty.SetMoveModeHold();
        }

        /// <summary>
        /// Leaves a nomad camp that just moved away and hides it. The party is stopped, otherwise waiting walks it back into the camp.
        /// </summary>
        private void menu_nomad_leave_on_consequence(MenuCallbackArgs args)
        {
            Settlement.CurrentSettlement.IsVisible = false;
            if (MobileParty.MainParty.CurrentSettlement != null)
                PlayerEncounter.LeaveSettlement();
            PlayerEncounter.Finish(true);
            MobileParty.MainParty.SetMoveModeHold();
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
            _hideoutWaitProgressHours += (float)dt.ToHours;
            if (_hideoutWaitTargetHours.ApproximatelyEqualsTo(0f, 1E-05f))
                CalculateHideoutAttackTime();

            SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
        }

        public void stop_wait_on_consequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
        }

        /// <summary>
        /// Leaves the wait menu if the game wants to show another menu, e.g. because of an attack.
        /// </summary>
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
            return CanAttackHideoutStart > CanAttackHideoutEnd && (currentHourInDay >= CanAttackHideoutStart || currentHourInDay <= CanAttackHideoutEnd) || CanAttackHideoutStart < CanAttackHideoutEnd && currentHourInDay >= CanAttackHideoutStart && currentHourInDay <= CanAttackHideoutEnd;
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
            Settlement settlement = Settlement.CurrentSettlement == null ? MobileParty.MainParty.CurrentSettlement : Settlement.CurrentSettlement;
            if (menuID == "mf_hideout_place")
                Campaign.Current.GameMenuManager.MenuLocations.Add(settlement.LocationComplex.GetLocationWithId("mf_hideout_center"));
        }

        /// <summary>
        /// Sets up the camp menu: background, locations and the text, which depends on the camp type and the player's standing with the clan.
        /// </summary>
        private void game_menu_hideout_place_on_init(MenuCallbackArgs args)
        {
            Settlement curSettlement = Settlement.CurrentSettlement;
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(curSettlement);
            if (mfHideout == null)
                return;

            args.MenuContext.SetBackgroundMeshName(curSettlement.SettlementComponent.WaitMeshName);
            UpdateMenuLocations(args.MenuContext.GameMenu.StringId);
            _hideoutWaitProgressHours = 0f;
            if (!IsHideoutAttackableNow())
                CalculateHideoutAttackTime();
            else
                _hideoutWaitTargetHours = 0f;

            if (mfHideout.IsNomad)
                GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "The nomad camp sprawls out ahead of you. Livestock graze about, " +
                    "attended to by herders who are discussing where to move on to next.");
            else
                GameTexts.SetVariable("HIDEOUT_DESCRIPTION", "");

            // TODO: LOCALIZATION
            if (mfHideout.IsNomad)
            {
                GameTexts.SetVariable("MF_HIDEOUT_ATTACK", "{=ldONWPCl6i}Raid Camp");
                if (Helpers.IsPlayerFriendOfMF(mfHideout.OwnerClan))
                {
                    GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{HIDEOUT_DESCRIPTION} You're well-known and well-liked by the nomads, and they happily receive you into their camp.");
                }
                else if (Helpers.ConsidersMFOutlaw(Clan.PlayerClan, mfHideout.OwnerClan))
                {
                    GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{HIDEOUT_DESCRIPTION} You and the nomads are at odds as your people have marked them as outlaws.");
                }
                else if (Clan.PlayerClan.IsAtWarWith(mfHideout.OwnerClan))
                {
                    GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{HIDEOUT_DESCRIPTION} You are at war with the nomads, and they are openly hostile to you. Expect no quarter if you approach the camp," +
                        " they will defend their encampment with their lives.");
                }
                else
                {
                    GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{HIDEOUT_DESCRIPTION} You have no real standing with these nomads, do not expect any hospitality nor hostility from them.");
                }
            }
            else
            {
                GameTexts.SetVariable("MF_HIDEOUT_ATTACK", "{=zxMOqlhs}Attack Hideout");
                GameTexts.SetVariable("MF_HIDEOUT_TEXT", "{=prcBBqMR}{HIDEOUT_DESCRIPTION} You see armed men moving about. As you listen quietly, you hear scraps" +
                    " of conversation about raids, ransoms, and the best places to waylay travelers.");
            }


            if (MobileParty.MainParty.CurrentSettlement == null)
                PlayerEncounter.EnterSettlement();

            if (PlayerEncounter.Battle != null)
            {
                bool playerWon = PlayerEncounter.Battle.WinningSide == PlayerEncounter.Current.PlayerSide;
                PlayerEncounter.Update();
                if (curSettlement != null && playerWon)
                    SetCleanHideoutRelations(mfHideout);
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
            ProcessMenusForAttack(args);
            Settlement? camp = CurrentCamp();
            bool friendly = IsFriendlyToPlayer(camp);
            return camp != null && !friendly;
        }

        /// <summary>
        /// Gets the camp the player is at, from the current settlement or the encounter.
        /// </summary>
        private static Settlement? CurrentCamp()
        {
            return Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement;
        }

        private void CalculateHideoutAttackTime()
        {
            _hideoutWaitTargetHours = CanAttackHideoutStart > CampaignTime.Now.CurrentHourInDay ? CanAttackHideoutStart - CampaignTime.Now.CurrentHourInDay : 24f - CampaignTime.Now.CurrentHourInDay + CanAttackHideoutStart;
        }

        /// <summary>
        /// After clearing an outlaw camp, raises the player's relation with notables of nearby villages,
        /// and with the nearest town's owner if the player has Effort for the People.
        /// </summary>
        private void SetCleanHideoutRelations(MinorFactionHideout mfHideout)
        {
            if (!mfHideout.OwnerClan.IsOutlaw)
                return;

            var settlement = mfHideout.Settlement;
            List<Settlement> nearbyVillages = new List<Settlement>();
            foreach (Village village in Village.All)
            {
                if (village.Settlement.GetPosition2D.DistanceSquared(settlement.GetPosition2D) <= MaxDistanceSquaredBetweenHideoutAndBoundVillage)
                    nearbyVillages.Add(village.Settlement);
            }
            foreach (Settlement village in nearbyVillages)
            {
                if (village.Notables.Count > 0)
                    ChangeRelationAction.ApplyPlayerRelation(village.Notables.GetRandomElement(), 2, true, false);
            }
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.EffortForThePeople))
            {
                Town town = SettlementHelper.FindNearestTownToSettlement(settlement, MobileParty.NavigationType.All);
                Hero leader = town.OwnerClan.Leader;
                if (leader == Hero.MainHero)
                    town.Loyalty += 1f;
                else
                    ChangeRelationAction.ApplyPlayerRelation(leader, (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus, true, true);
            }
            MBTextManager.SetTextVariable("RELATION_VALUE", (int)DefaultPerks.Charm.EffortForThePeople.PrimaryBonus);
            MBInformationManager.AddQuickInformation(new TextObject("{=o0qwDa0q}Your relation increased by {RELATION_VALUE} with nearby notables.", null), 0, null, null, "");
        }

        /// <summary>
        /// Makes sure the camp has militia to fight. SelectBossAgent crashes on an empty militia roster, so the clan's entry troops are put in.
        /// </summary>
        private void ArrangeHideoutTroopCountsForMission()
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null)
                return;
            MinorFactionHideout mfh = Helpers.GetMFHideout(settlement);
            if (mfh == null)
                return;

            // SelectBossAgent crashes on an empty militia roster so put a basic troop in.
            PartyBase militiaParty = settlement.MilitiaPartyComponent?.Party;
            if (militiaParty == null || militiaParty.MemberRoster.TotalManCount == 0)
            {
                if (militiaParty != null)
                {
                    int count = Math.Max(5, (int)settlement.Militia);
                    foreach ((CharacterObject troop, int part) in Helpers.SplitEntryTroops(mfh.OwnerClan, count))
                        militiaParty.MemberRoster.AddToCounts(troop, part, false, 0, 0, false);
                }
                settlement.Militia = IMFModels.NumMilitiaPostRaid(mfh.OwnerClan);
            }
        }

        private void ApplyHideoutRaidConsequences()
        {
            ApplyHideoutRaidConsequences(Settlement.CurrentSettlement);
        }

        /// <summary>
        /// Makes the player hostile to the camp's clan and lowers relations with its leader and notables.
        /// A clan in mercenary service loses more and declares war on the player when the contract ends.
        /// </summary>
        public static void ApplyHideoutRaidConsequences(Settlement mfHideout)
        {
            Clan mfClan = mfHideout.OwnerClan;
            BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, mfHideout.Party);
            if (mfClan.IsUnderMercenaryService)
            {
                ChangeRelationAction.ApplyPlayerRelation(mfClan.Leader, -20);
                IMFManager.Current!.RegisterClanForPlayerWarOnEndingMercenaryContract(mfClan);
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

        /// <summary>
        /// Opens the hideout battle with the troops the player picked.
        /// </summary>
        private void OnTroopRosterManageDone(TroopRoster playerTroops)
        {
            PrepareForBattle();

            var mfHideout = Helpers.GetMFHideout(Settlement.CurrentSettlement);
            mfHideout!.EnsureTerrainFittingScene();
            CampaignMission.OpenHideoutBattleMission(mfHideout!.SceneName, playerTroops.ToFlattenedRoster(), false);
        }

        /// <summary>
        /// Checks if a troop can be picked for the raid, the player always goes.
        /// </summary>
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
            dataStore.SyncData("_mfHideoutWaitProgressHours", ref _hideoutWaitProgressHours);
            dataStore.SyncData("_mfHideoutWaitTargetHours", ref _hideoutWaitTargetHours);
        }

        /// <summary>
        /// Adds the menus and activates every clan's camps on a new game, then runs a second pass a few ticks later.
        /// </summary>
        public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            _ticksSinceNewGame = 0;
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTickAfterNewGame);
            AddGameMenus(campaignGameStarter);
            IMFManager.InitManagerIfNone();
            ActivateAllSafely();
        }

        // Some clans aren't ready when the game is created, a second pass catches them.
        private int _ticksSinceNewGame;

        /// <summary>
        /// Some clans aren't ready when the game is created, so camps are activated again 5 ticks later.
        /// </summary>
        private void OnTickAfterNewGame(float dt)
        {
            if (++_ticksSinceNewGame < 5)
                return;
            CampaignEvents.TickEvent.ClearListeners(this);
            ActivateAllSafely();
        }

        /// <summary>
        /// Adds the menus and fixes the active camp counts after a load.
        /// </summary>
        public void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
            IMFManager.InitManagerIfNone();
            ActivateAllSafely();
        }

        /// <summary>
        /// Activates every clan's camps, logging errors instead of crashing the load.
        /// </summary>
        private static void ActivateAllSafely()
        {
            try
            {
                IMFManager.Current!.ActivateAllFactionHideouts();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("ActivateAllFactionHideouts failed, continuing without full activation:", e);
            }
        }

        // some overhauls replace vanilla recruitment, then nothing refills camp volunteers
        private bool _refillVolunteersOurselves;

        /// <summary>
        /// Refills camp volunteers each day when another mod replaced the game's recruitment.
        /// </summary>
        private void DailyTickSettlement(Settlement settlement)
        {
            if (_refillVolunteersOurselves && Helpers.IsMFHideout(settlement))
                RecruitmentUpdateNotableVolunteersPatch.RefillCampVolunteers(settlement);
        }

        // vanilla fills volunteers once on a new game, the player's camp only activates after character creation
        private readonly HashSet<string> _gotStartingVolunteers = new HashSet<string>();
        private const int StartingVolunteerRolls = 3;

        /// <summary>
        /// Gives a camp its first volunteers during the first day of a new game. The game fills volunteers once on a new game,
        /// but the player's camp only activates after character creation.
        /// </summary>
        private void GiveStartingVolunteers(Settlement camp)
        {
            if (Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow >= 1f)
                return;
            if (!_gotStartingVolunteers.Add(camp.StringId))
                return;
            for (int i = 0; i < StartingVolunteerRolls; i++)
                RecruitmentUpdateNotableVolunteersPatch.RefillCampVolunteers(camp);
        }

        /// <summary>
        /// Checks if the game's recruitment behavior is still there, if not we refill camp volunteers ourselves.
        /// </summary>
        public void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            _refillVolunteersOurselves = Campaign.Current.GetCampaignBehavior<TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior>() == null;
            if (_refillVolunteersOurselves)
                IMFDiagnostics.Info("no vanilla recruitment behavior in this campaign, camps refill their own volunteers");
            EnsureClanHeroesAligned();
        }

        private void OnHeroCreated(Hero hero, bool isBornNaturally)
        {
            if (hero?.Clan != null)
                AlignWithClan(hero);
        }

        /// <summary>
        /// Runs AlignWithClan for every hero of every minor faction except the player's.
        /// </summary>
        private static void EnsureClanHeroesAligned()
        {
            foreach (Clan clan in Clan.All)
            {
                if (clan == null || !clan.IsMinorFaction || clan == Clan.PlayerClan || !IMFManager.Current!.HasFaction(clan))
                    continue;
                foreach (Hero hero in clan.Heroes)
                    AlignWithClan(hero);
            }
        }

        /// <summary>
        /// Makes a hero created into a minor faction the same race and culture as the clan leader, and names them "Name of Clan".
        /// Only for clans whose leader isn't the default race.
        /// </summary>
        internal static void AlignWithClan(Hero hero)
        {
            try
            {
                Clan? clan = hero?.Clan;
                Hero? leader = clan?.Leader;
                if (hero == null || clan == null || leader == null || leader == hero || hero.CharacterObject == null || leader.CharacterObject == null)
                    return;
                if (!clan.IsMinorFaction || clan == Clan.PlayerClan || IMFManager.Current?.HasFaction(clan) != true)
                    return;
                if (leader.CharacterObject.Race == 0 || hero.CharacterObject.Race == leader.CharacterObject.Race)
                    return;
                hero.CharacterObject.Race = leader.CharacterObject.Race;
                if (clan.Culture != null)
                    hero.Culture = clan.Culture;
                string suffix = " of " + (clan.Name?.ToString() ?? "").Trim();
                string first = hero.FirstName?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(first) || (hero.Name?.ToString() ?? "").Contains(suffix))
                    return;
                hero.SetName(new TextObject("{=!}" + first + suffix, null), new TextObject("{=!}" + first, null));
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("AlignWithClan failed for hero " + hero?.Name?.ToString() + ":", e);
            }
        }
    }
}

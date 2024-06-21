using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Extensions;
using MathF = TaleWorlds.Library.MathF;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Encounters;
using static ImprovedMinorFactions.IMFModels;

namespace ImprovedMinorFactions.Source.Quests.MFMafiaCaravanExtortion
{
    
    public class MFMafiaCaravanExtortionIssueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static bool ConditionsHold(Hero issueGiver)
        {
            return issueGiver.IsLord
                && issueGiver.Clan != null
                && issueGiver.Clan.IsMinorFaction
                && issueGiver.IsPartyLeader
                && Helpers.HasMFHideout(issueGiver.Clan)
                && !issueGiver.IsPrisoner
                && (issueGiver.Clan.IsMafia || issueGiver.Clan.StringId == "jawwal")
                && issueGiver.Gold > 2000
                && Helpers.IsMFHideout(issueGiver.HomeSettlement)
                && issueGiver.Clan.IsUnderMercenaryService;
        }

        private const IssueBase.IssueFrequency _IssueFrequency = IssueBase.IssueFrequency.Common;

        internal const MFRelation RelationLevelNeededForQuest = MFRelation.Tier2;

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnSelected), typeof(MFMafiaCaravanExtortionIssue), _IssueFrequency)
                    );
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MFMafiaCaravanExtortionIssue), _IssueFrequency));
        }

        private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new MFMafiaCaravanExtortionIssue(issueOwner);
        }

        public class MFMafiaCaravanExtortionIssue : IssueBase
        {
            private int RequestedCaravanCount
            {
                get => 1 + MathF.Ceiling(4 * base.IssueDifficultyMultiplier);
            }

            protected override int RewardGold
            {
                get => RequestedCaravanCount * MathF.Floor(MFMafiaExtortionQuest.SafePassagePrice * MFMafiaExtortionQuest.PlayerRewardPercentage);
            }

            public override TextObject IssueBriefByIssueGiver
            {
                get => new TextObject("{=wbcrTTku}The local notables were paying us a very modest fee to 'protect' their caravans. " +
                        "But now that we are bound by a mercenary contract they have decided they no longer require our services. " +
                        "We need you to prove them wrong.[ib:hip][if:convo_undecided_closed]");
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=kuubi1UH}Very well, and how would you like this to be handled?");
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get => new TextObject("{=mfizKp8L}Persuade them into paying what is owed... Give them steel... " +
                        "It does not matter to me. As long as the issue is resolved you shall get your fair share.");
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => new TextObject("{=y7RNwzmG}Simple enough, it will be done.");
            }

            public override bool IsThereAlternativeSolution
            {
                get => false;
            }

            public override bool IsThereLordSolution
            {
                get => false;
            }

            public override TextObject Title
            {
                get => new TextObject("{=xeOZNC4PM7}{MINOR_FACTION} Needs Caravans Extorted").SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public override TextObject Description
            {
                get => new TextObject("{=xeOZNC4PM7}{MINOR_FACTION} Needs Caravans Extorted").SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public MFMafiaCaravanExtortionIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration))
            {
            }

            // TODO: make this issue affect clan power
            protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
            {
                if (issueEffect == DefaultIssueEffects.ClanInfluence)
                {
                    return -0.1f;
                }
                return 0f;
            }

            protected override void OnGameLoad()
            {
            }

            protected override void HourlyTick()
            {
            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new MFMafiaExtortionQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration), this.RequestedCaravanCount);
            }

            public override IssueFrequency GetFrequency()
            {
                return _IssueFrequency;
            }

            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero? relationHero, out SkillObject? skill)
            {
                flag = PreconditionFlags.None;
                relationHero = null;
                skill = null;

                if ((issueGiver?.MapFaction ?? issueGiver?.Clan ?? Hero.MainHero?.MapFaction) == null 
                    || !Helpers.IsMFHideout(issueGiver?.HomeSettlement))
                {
                    return false;
                }

                if (issueGiver!.GetRelationWithPlayer() < MinRelationNeeded(RelationLevelNeededForQuest))
                {
                    flag |= PreconditionFlags.Relation;
                    relationHero = issueGiver;
                }
                if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero!.MapFaction)
                    || Helpers.IsRivalOfMinorFaction(Hero.MainHero.MapFaction, issueGiver.Clan))
                {
                    flag |= PreconditionFlags.AtWar;
                }
                return flag == PreconditionFlags.None;
            }

            public override bool IssueStayAliveConditions()
            {
                return base.IssueOwner.Clan.IsUnderMercenaryService && Helpers.IsMFHideout(base.IssueOwner.HomeSettlement)
                    && Helpers.GetMFHideout(base.IssueOwner.HomeSettlement)!.IsActive;
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {
            }

            // xp for alternative solution
            protected override int CompanionSkillRewardXP
            {
                get => 0;
            }

            private Clan IssueClan()
            {
                return base.IssueOwner.Clan;
            }

            private const int IssueAndQuestDuration = 30;
        }

        public class MFMafiaExtortionQuest : QuestBase
        {
            public MFMafiaExtortionQuest(string questId, Hero questGiver, CampaignTime duration, int requestedCaravanCount) : base(questId, questGiver, duration, 0)
            {
                this._requestedCaravanCount = requestedCaravanCount;
                this._extortedCaravanCount = 0;
                this._rewardGold = 0;
                MBTextManager.SetTextVariable("MAFIA_EXTORTION_REWARD", this._rewardGold);
                this._playerReachedRequestedAmount = false;
                this._targetCaravans = new List<MobileParty>();
                this._extortedCaravans = new List<MobileParty>();
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
                CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnHideoutCleared));

                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
                CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
            }

            private void OnHideoutCleared(Settlement hideout)
            {
                if (this.QuestGiver.HomeSettlement == hideout)
                {
                    base.AddLog(this.QuestNPCClearedHideoutLog);
                    base.CompleteQuestWithCancel();
                }
            }

            private void AddCaravanToPotentialTargets(MobileParty caravan)
            {
                if (!this._targetCaravans.Contains(caravan))
                {
                    this._targetCaravans.Add(caravan);
                    base.AddTrackedObject(caravan);
                }
            }

            private Settlement? QuestHideout()
            {
                return this.QuestGiver.HomeSettlement;
            }

            private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
            {
                if (destroyerParty == PartyBase.MainParty && mobileParty.IsCaravan && this._targetCaravans.Contains(mobileParty))
                    this.AddExtortedCaravan(mobileParty);
            }

            public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
            {
                this.AddDialogs(campaignGameStarter);

            }

            // Manage target caravans
            private void OnHourlyTick()
            {
                InformationManager.DisplayMessage(new InformationMessage($"number of target caravans: {_targetCaravans?.Count}"));
                if (_extortedCaravans == null)
                    _extortedCaravans = new List<MobileParty>();
                if (QuestHideout() != null)
                {
                    // Get caravans near hideout and track them for the player
                    LocatableSearchData<MobileParty> data = MobileParty
                        .StartFindingLocatablesAroundPosition(QuestHideout()!.Position2D, _maxDistanceToHideoutForTargetCaravan);
                    for (MobileParty mParty = MobileParty.FindNextLocatable(ref data); mParty != null; mParty = MobileParty.FindNextLocatable(ref data))
                    {
                        if (mParty?.Position2D == null || !mParty.IsActive || !mParty.IsCaravan
                            || mParty.IsMainParty || mParty.IsDisbanding || mParty.MapFaction == Hero.MainHero.MapFaction
                            || _extortedCaravans.Contains(mParty) || mParty.MapFaction == QuestGiver.MapFaction)
                            continue;

                        if (mParty.Position2D.DistanceSquared(MobileParty.MainParty.Position2D) > _maxDistanceSquaredToHideoutForTargetCaravan)
                            break;

                        AddCaravanToPotentialTargets(mParty);
                    }

                    // Remove caravans that are too far and not visible to the player
                    List<MobileParty> caravansToRemove = new List<MobileParty>();
                    foreach (var caravan in _targetCaravans!)
                    {
                        if (caravan.Position2D.DistanceSquared(QuestHideout()!.Position2D) > _maxDistanceSquaredToHideoutForTargetCaravan
                            && Hero.MainHero.IsPartyLeader && !Hero.MainHero.IsPrisoner
                            && caravan.Position2D.DistanceSquared(MobileParty.MainParty.Position2D) > MathF.Pow((MobileParty.MainParty.SeeingRange + 10f), 2))
                        {
                            caravansToRemove.Add(caravan);
                        }
                    }
                    foreach (var caravan in caravansToRemove)
                    {
                        _targetCaravans.Remove(caravan);
                        base.RemoveTrackedObject(caravan);
                    }
                } else
                {
                    _targetCaravans!.Clear();
                }
            }

            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestGiver.MapFaction, Hero.MainHero.MapFaction))
                    base.CompleteQuestWithCancel(QuestCancelledDueToWarLog);

                if (clan == QuestGiver.Clan)
                    _targetCaravans.RemoveAll(caravan => caravan?.MapFaction == QuestGiver.MapFaction);
            }

            private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestGiver.MapFaction, Hero.MainHero.MapFaction))
                {
                    if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility)
                        CompleteQuestWithFail(QuestCancelledDueToPlayerHostilityLog);
                    else
                        base.CompleteQuestWithCancel(QuestCancelledDueToWarLog);
                }
            }

            private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
            {
                if (victim == this.QuestGiver)
                    base.CompleteQuestWithCancel(QuestCancelledQuestGiverDiedLog);
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();
                base.AddTrackedObject(base.QuestGiver);
                if (Helpers.IsMFHideout(QuestHideout()))
                {
                    Helpers.GetMFHideout(QuestHideout())!.IsSpotted = true;
                    base.AddTrackedObject(QuestHideout());
                }
                this._questProgressLogTest = base.AddDiscreteLog(this.QuestStartedLogText, new TextObject("{=AJXiWK0TL1}Caravans Extorted"), this._extortedCaravanCount, this._requestedCaravanCount);

                // Make sure the nomad camp is inhabited at least for 3 days after quest is done
                if (QuestClan().IsNomad)
                     Helpers.GetMFHideout(QuestHideout())!.ExtendNomadCampMigrationTimePastTime(this.QuestDueTime);
            }

            private void AddDialogs(CampaignGameStarter starter)
            {
                starter.AddPlayerLine("caravan_extortion_start", "caravan_talk", "caravan_extortion_start", 
                    "{=WGqRQKUR}The {EXTORTION_MINOR_FACTION} tell me you have not been paying your dues...",
                    new ConversationSentence.OnConditionDelegate(this.caravan_extortion_on_condition), null);
                starter.AddDialogLine("caravan_extortion_reply", "caravan_extortion_start", "caravan_extortion_reply", 
                    "{=BtiUUTp4}The {EXTORTION_MINOR_FACTION}? Those brigands?! Thank the heavens they are off playing at war. We have only had peace since then.",
                    null, null);
                starter.AddPlayerLine("caravan_extortion_reply_player_reply", "caravan_extortion_reply", "caravan_extortion_reply_player_reply",
                    "{=fNJZPgq3}Peace is costly. You should pay up lest something happens to your goods.",
                    new ConversationSentence.OnConditionDelegate(this.caravan_extortion_on_condition), null);
                starter.AddDialogLine("caravan_extortion_success", "caravan_extortion_reply_player_reply", "close_window",
                    "{=Wt4cpJ09} I know a robbery when I see one. You bastards are all the same. Here is your filthy gold, now leave us in peace.",
                    new ConversationSentence.OnConditionDelegate(this.caravan_extortion_success_on_condition), 
                    new ConversationSentence.OnConsequenceDelegate(this.caravan_extortion_success_on_consequence));
                starter.AddDialogLine("caravan_extortion_failure", "caravan_extortion_reply_player_reply", "close_window",
                    "{=ThxtyA9x} Are you mad? You will get nothing but a good stabbing you bandit!",
                    new ConversationSentence.OnConditionDelegate(this.caravan_extortion_failure_on_condition), 
                    new ConversationSentence.OnConsequenceDelegate(this.caravan_extortion_failure_on_consequence));
            }

            private bool caravan_extortion_on_condition()
            {
                MBTextManager.SetTextVariable("EXTORTION_MINOR_FACTION", this.QuestGiver.Clan.Name);
                return Hero.OneToOneConversationHero == null && MobileParty.ConversationParty != null 
                    && MobileParty.ConversationParty.IsCaravan 
                    && this._targetCaravans.Contains(MobileParty.ConversationParty);
            }

            // TODO: increase clan power if player attacks caravan for quest and increase extortion failure likelyhood if clan is weak
            private bool caravan_extortion_failure_on_condition()
            {
                return MobileParty.ConversationParty.GetTotalStrengthWithFollowers() > PartyBase.MainParty.MobileParty.GetTotalStrengthWithFollowers();
            }

            private void caravan_extortion_failure_on_consequence()
            {
                PlayerEncounter.Current.IsEnemy = true;
                PrepareForBattle();

                BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
            }

            

            private bool caravan_extortion_success_on_condition()
            {
                return MobileParty.ConversationParty.GetTotalStrengthWithFollowers() <= PartyBase.MainParty.MobileParty.GetTotalStrengthWithFollowers();
            }

            private void caravan_extortion_success_on_consequence()
            {
                GiveGoldAction.ApplyForPartyToCharacter(MobileParty.ConversationParty.Party, Hero.MainHero, SafePassagePrice);
                BeHostileAction.ApplyMinorCoercionHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
                AddExtortedCaravan(MobileParty.ConversationParty);

                if (PlayerEncounter.Current != null)
                {
                    PlayerEncounter.LeaveEncounter = true;
                }
            }

            

            protected override void SetDialogs()
            {
                this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start")
                    .NpcLine(new TextObject("{=0QuAZ8YO}I'll be waiting. Good luck.[if:convo_relaxed_happy][ib:confident]"))
                    .Condition(() => Hero.OneToOneConversationHero == this.QuestGiver)
                    .Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
                    .CloseDialog();

                this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss")
                    .BeginNpcOptions()
                    .NpcOption(new TextObject("{=!}I see you have reminded quite a few caravans of the boundaries of our territory."),
                        () => Hero.OneToOneConversationHero == this.QuestGiver && this._playerReachedRequestedAmount)
                        .BeginPlayerOptions()
                        .PlayerOption(new TextObject("{=!}Yes, I have extorted some caravans for you. Here is your gold."))
                            .Condition(() => Hero.MainHero.Gold >= this.QuestGiverExpectedGold)
                            .NpcLine(new TextObject("{=!}Great, you can keep {MAFIA_EXTORTION_REWARD}{GOLD_ICON} of your profits.")
                                                    .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">"))
                            .Consequence(delegate {
                                this.ApplyQuestSuccessConsequences();
                                this.CompleteQuestWithSuccess();
                            })
                            .CloseDialog()
                        .PlayerOption(new TextObject("{=!}Yes, but I would like to continue this job."))
                            .Condition(() => Hero.MainHero.Gold >= this.QuestGiverExpectedGold)
                            .NpcLine(new TextObject("{=!}I see you enjoy the life of a scoundrel. You can continue extorting for us but don't even consider keeping the profits..."))
                            .GotoDialogState("hero_main_options")
                        .PlayerOption(new TextObject("{=!}I have extorted them but don't have the money ready yet"))
                            .Condition(() => Hero.OneToOneConversationHero == this.QuestGiver && Hero.MainHero.Gold < this.QuestGiverExpectedGold)
                            .NpcLine(new TextObject("{=!}You spent your profits? You better muster up some gold soon if you want to keep your head on your shoulders!"))
                            .CloseDialog()
                        .EndPlayerOptions()
                        .CloseDialog()
                    .NpcOption(new TextObject("{=!}Have you been collecting protection fees on our behalf?[ib:confident3]"), 
                                () => Hero.OneToOneConversationHero == this.QuestGiver && !this._playerReachedRequestedAmount)
                        .BeginPlayerOptions()
                        .PlayerOption(new TextObject("{=!}I am still working on it."))
                            .NpcLine(new TextObject("{=!}Why are you wasting time talking to me then?"))
                            .CloseDialog()
                        .EndPlayerOptions()
                    .CloseDialog()
                    .EndNpcOptions();
            }

            public override TextObject Title
            {
                get => new TextObject("{=xeOZNC4PM7}{MINOR_FACTION} Needs Caravans Extorted").SetTextVariable("MINOR_FACTION", QuestClan().Name);
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            private TextObject QuestStartedLogText
            {
                get
                {
                    TextObject text = new TextObject("{=ivsCj8R6rp}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs caravans extorted. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to extort {NEEDED_CARAVAN_AMOUNT} caravans near {?QUEST_GIVER.GENDER}her{?}his{\\?} hideout " +
                        "then give {?QUEST_GIVER.GENDER}her{?}him{\\?} the profits. If caravans refuse to pay safe passage fees you are instructed to " +
                        "attack them. You will be allowed to keep a portion of the profits.");

                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    text.SetTextVariable("NEEDED_CARAVAN_AMOUNT", this._requestedCaravanCount);
                    return text;
                }
            }

            private TextObject QuestNPCClearedHideoutLog
            {
                get
                {
                    TextObject text = new TextObject("{=!}{HIDEOUT_NAME} was cleared by someone. Your agreement with {QUEST_GIVER.LINK} is canceled.")
                        .SetTextVariable("{HIDEOUT_NAME}", this.QuestGiver.HomeSettlement?.Name);
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            // TODO: is this success?
            private TextObject QuestTaskCompletedLog
            {
                get
                {
                    TextObject text = new TextObject("{=N79TpDDI0n}You have extorted caravans for {QUEST_GIVER.LINK} as promised. Go to {?QUEST_GIVER.GENDER}her{?}him{\\?}" +
                        " to deliver the gold you have collected.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject QuestSuccessLog
            {
                get
                {
                    TextObject text = new TextObject("{=!}You have delivered the profits to {QUEST_GIVER.LINK}.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject QuestCancelledQuestGiverDiedLog
            {
                get
                {
                    TextObject text = new TextObject("{=gOEIZ30vl}{QUEST_GIVER.LINK} has died. {?QUEST_GIVER.GENDER}She{?}He{\\?} has no more desires.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject QuestCancelledDueToWarLog
            {
                get
                {
                    TextObject text = new TextObject("{=TrewB5c7}Now your {?IS_MAP_FACTION}clan{?}kingdom{\\?} is at war with the {QUEST_GIVER.LINK}'s lord. " +
                        "Your agreement with {QUEST_GIVER.LINK} becomes invalid.")
                        .SetTextVariable("IS_MAP_FACTION", Clan.PlayerClan.IsMapFaction ? 1 : 0); ;
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject QuestCancelledDueToPlayerHostilityLog
            {
                get
                {
                    TextObject text = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject QuestFailedWithTimeOutLogText
            {
                get
                {
                    TextObject text = new TextObject("{=!}You have failed to extort enough caravans and deliver the profits to {QUEST_GIVER.LINK} in time." +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} must be disappointed.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private void ApplyQuestSuccessConsequences()
            {
                base.AddLog(this.QuestSuccessLog, false);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.RogueSkills, QuestGiverRogueryOnSuccess)
                });

                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, PlayerRogueryXpOnQuestCompleted);

                ChangeRelationAction.ApplyPlayerRelation(this.QuestGiver, ClanRelationBonusOnSuccess);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, this.QuestGiver, this.QuestGiverExpectedGold);

                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonusOnSuccess;
            }


            protected override void OnTimedOut()
            {
                base.AddLog(this.QuestFailedWithTimeOutLogText, false);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenaltyOnFail;
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            private void PrepareForBattle()
            {
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
            }

            private void AddExtortedCaravan(MobileParty caravan)
            {
                this._extortedCaravanCount++;
                base.RemoveTrackedObject(caravan);
                this._extortedCaravans.Add(caravan);

                // Give 5 criminal rating for extorting/destroying caravan
                ChangeCrimeRatingAction.Apply(caravan.MapFaction, PlayerCriminalRatingOnExtortion);

                // give roguery xp for extorting carvan
                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, PlayerRogueryXpForCaravan);

                this._rewardGold += MathF.Ceiling(SafePassagePrice * PlayerRewardPercentage);
                MBTextManager.SetTextVariable("MAFIA_EXTORTION_REWARD", this._rewardGold);

                this._questProgressLogTest!.UpdateCurrentProgress(this._extortedCaravanCount);
                this._targetCaravans.Remove(caravan);
                
                if (_extortedCaravanCount >= _requestedCaravanCount && this._playerReachedRequestedAmount == false)
                {
                    this._playerReachedRequestedAmount = true;
                    base.AddLog(this.QuestTaskCompletedLog);
                    var description = new TextObject("{=!}You have extorted enough caravans for {QUEST_GIVER.LINK}. You can now deliver the earnings to {?QUEST_GIVER.GENDER}her{?}him{\\?}.");
                    description.SetCharacterProperties("QUEST_GIVER", this.QuestGiver.CharacterObject);
                    var title = new TextObject("{=!}{MINOR_FACTION} Extortion").SetTextVariable("MINOR_FACTION", this.QuestClan().Name);
                    InformationManager.ShowInquiry(new InquiryData(title.ToString(), description.ToString(), true, false, "Ok", "", null, null));
                }
            }

            protected override void HourlyTick() {}

            private Clan QuestClan()
            {
                return base.QuestGiver.Clan;
            }

            public int QuestGiverExpectedGold
            {
                get => SafePassagePrice * MathF.Floor(_extortedCaravanCount * (1 - PlayerRewardPercentage));
            }

            private int QuestGiverRogueryOnSuccess
            {
                get => _requestedCaravanCount * 10;
            }

            private const int QuestGiverRelationBonusOnSuccess = 10;

            private const int ClanRelationBonusOnSuccess = 10;

            private const int QuestGiverRelationPenaltyOnFail = -5;

            private const float _maxDistanceToHideoutForTargetCaravan = 50f;

            private float _maxDistanceSquaredToHideoutForTargetCaravan = MathF.Pow(50f, 2);

            public const float PlayerRewardPercentage = 0.2f;

            public const int SafePassagePrice = 2000;

            private const float PlayerRogueryXpForCaravan = 2000f;

            private const float PlayerRogueryXpOnQuestCompleted = 3000f;

            private const float PlayerCriminalRatingOnExtortion = 5f;

            [SaveableField(1)]
            private int _requestedCaravanCount;

            [SaveableField(5)]
            private int _extortedCaravanCount;

            [SaveableField(6)]
            private int _rewardGold;

            [SaveableField(7)]
            private JournalLog? _questProgressLogTest;

            [SaveableField(9)]
            private bool _playerReachedRequestedAmount;

            [SaveableField(10)]
            private List<MobileParty> _targetCaravans;

            [SaveableField(11)]
            private List<MobileParty> _extortedCaravans;
        }

        public class MFMafiaCaravanExtortionTypeDefiner : SaveableTypeDefiner
        {
            public MFMafiaCaravanExtortionTypeDefiner() : base(163_837_932)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(MFMafiaCaravanExtortionIssue), 1);
                base.AddClassDefinition(typeof(MFMafiaExtortionQuest), 2);
            }

            protected override void DefineContainerDefinitions()
            {
                //ConstructContainerDefinition(typeof(List<MobileParty>));
            }
        }
    }
}


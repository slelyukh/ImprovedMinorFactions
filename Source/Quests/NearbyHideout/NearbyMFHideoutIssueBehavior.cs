using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using MathF = TaleWorlds.Library.MathF;

namespace ImprovedMinorFactions.Source.Quests.NearbyHideout
{
    public class NearbyMFHideoutIssueBehavior : CampaignBehaviorBase
    {
        private Settlement FindSuitableHideout(Hero issueOwner)
        {
            Settlement result = null;
            float minDistance = float.MaxValue;
            MFHideoutManager.InitManagerIfNone();
            foreach (var mfHideout in (from mfh in MFHideoutManager.Current.AllMFHideouts where mfh.IsActive && Helpers.IsEnemyOfMinorFaction(issueOwner.MapFaction, mfh.OwnerClan) select mfh))
            {
                float distance = mfHideout.Settlement.GatePosition.Distance(issueOwner.GetMapPoint().Position2D);
                if (distance <= NearbyHideoutMaxDistance 
                    && distance < minDistance 
                    && mfHideout.Hearth >= MFHideoutModels.MinimumMFHHearthToAffectVillage)
                {
                    minDistance = distance;
                    result = mfHideout.Settlement;
                }
            }
            return result;
        }

        private void OnCheckForIssue(Hero hero)
        {
            if (this.ConditionsHold(hero))
            {
                Settlement settlement = this.FindSuitableHideout(hero);
                if (settlement != null)
                {
                    Campaign.Current.IssueManager.AddPotentialIssueData(
                        hero,
                        new PotentialIssueData(
                            new PotentialIssueData.StartIssueDelegate(this.OnIssueSelected),
                            typeof(NearbyMFHideoutIssue),
                            NearbyHideoutIssueFrequency,
                            settlement));
                    return;
                }
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(typeof(NearbyMFHideoutIssue),
                    NearbyHideoutIssueFrequency));
            }
        }

        private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new NearbyMFHideoutIssue(issueOwner, pid.RelatedObject as Settlement);
        }

        private bool ConditionsHold(Hero issueGiver)
        {
            return issueGiver.IsNotable && issueGiver.IsHeadman && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.Village.Bound.Town.Security <= 50f;
        }

        private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, new Action<IssueBase, IssueBase.IssueUpdateDetails, Hero>(this.OnIssueUpdated));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public class NearbyMFHideoutIssue : IssueBase
        {
            protected Clan MFClan
            {
                get => _targetHideout.OwnerClan;
            }

            public override TextObject Title
            {
                get => setCommonTextVariables(new TextObject("{MINOR_FACTION} Base Near {ISSUE_SETTLEMENT.NAME}"));
            }

            public override TextObject Description
            {
                get => setCommonTextVariables(new TextObject("{ISSUE_GIVER.LINK} wants you to clear the {MINOR_FACTION} hideout that causes trouble in {?ISSUE_GIVER.GENDER}her{?}his{\\?} region."));
            }
            public override TextObject IssueBriefByIssueGiver
            {
                get => setCommonTextVariables(new TextObject("Yes... There's this old ruin, a place that offers a good view of the roads, and is yet hard to reach. " +
                    "The {MINOR_FACTION} have moved in and they have been giving hell to the caravans and travellers passing by.[ib:closed][if:convo_undecided_open]"));
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=IqH0jFdK}So you need someone to deal with these bastards?");
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get => setCommonTextVariables(new TextObject("{=zstiYI49}Any {MINOR_FACTION} bandits there can easily spot and evade a large army moving against them, " +
                    "but if you can enter the hideout with a small group of determined warriors you can catch them unaware.[ib:closed][if:convo_thinking]"));
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => setCommonTextVariables(new TextObject("{=uhYprSnG}I will go to the {MINOR_FACTION} hideout myself and ambush the bandits."));
            }

            public override TextObject IssueAlternativeSolutionAcceptByPlayer
            {
                get => setCommonTextVariables(new TextObject("{=IFasMslv}I will assign a companion with {TROOP_COUNT} good men for {RETURN_DAYS} days."));
            }

            public override TextObject IssueDiscussAlternativeSolution
            {
                get => new TextObject("{=DgVU7owN}I pray for your warriors. The people here will be very glad to hear of their success.[ib:hip][if:convo_excited]");
            }

            public override TextObject IssueAlternativeSolutionResponseByIssueGiver
            {
                get => setCommonTextVariables(
                    new TextObject("{=aXOgAKfj}Thank you, {?PLAYER.GENDER}madam{?}sir{\\?}. I hope your people will be successful.[ib:hip][if:convo_excited]"));
            }

            public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
            {
                get => setCommonTextVariables(new TextObject("{=VNXgZ8mt}Alternatively, if you can assign a companion with {TROOP_COUNT} or so men to this task," +
                    " they can do the job.[ib:closed][if:convo_undecided_open]"));
            }

            public override TextObject IssueAsRumorInSettlement
            {
                get => setCommonTextVariables(new TextObject("{=ctgihUte}I hope {ISSUE_GIVER.NAME} has a plan to get rid of those bandits."));
            }

            public override bool IsThereAlternativeSolution
            {
                get => true;
            }

            protected override TextObject AlternativeSolutionStartLog
            {
                get
                {
                    TextObject text = new TextObject("{=G4kpabSf}{ISSUE_GIVER.LINK}, a headman from {ISSUE_SETTLEMENT}, has told you " +
                    "about {MINOR_FACTION} attacks on local villagers and asked you to clear out the outlaws' hideout. You asked " +
                    "{COMPANION.LINK} to take {TROOP_COUNT} of your best men to go and take care of it. They should report back to you in {RETURN_DAYS} days.");
                    text.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
                    return setCommonTextVariables(text);
                }
            }

            public override TextObject IssueAlternativeSolutionSuccessLog
            {
                get => setCommonTextVariables(new TextObject("You received a message from {ISSUE_GIVER.LINK}.\n\"Thank you for clearing out that outlaws' nest. Please accept these " +
                    "{REWARD}{GOLD_ICON} denars with our gratitude.\""));
            }

            public override TextObject IssueAlternativeSolutionFailLog
            {
                get => setCommonTextVariables(new TextObject("{=qsMnnfQ3}You failed to clear the hideout in time to prevent further attacks. {ISSUE_GIVER.LINK} is disappointed."));
            }

            private TextObject setCommonTextVariables(TextObject text)
            {
                text.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
                text.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
                return text.SetTextVariable("MINOR_FACTION", MFClan.Name)
                    .SetTextVariable("TROOP_COUNT", (int)(base.GetTotalAlternativeSolutionNeededMenCount() * 1.5f))
                    .SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays())
                    .SetTextVariable("ISSUE_SETTLEMENT", this._issueSettlement.EncyclopediaLinkWithName)
                    .SetTextVariable("REWARD", this.RewardGold)
                    .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            }

            protected override bool IssueQuestCanBeDuplicated
            {
                get => false;
            }

            public NearbyMFHideoutIssue(Hero issueOwner, Settlement targetHideout) : base(issueOwner, CampaignTime.DaysFromNow(IssueDuration))
            {
                this._targetHideout = targetHideout;
            }

            protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
            {
                if (issueEffect == DefaultIssueEffects.SettlementProsperity)
                    return -0.3f * (Helpers.GetMFHideout(_targetHideout).Hearth / 300);
                if (issueEffect == DefaultIssueEffects.SettlementSecurity)
                    return -0.6f * (Helpers.GetMFHideout(_targetHideout).Hearth / 300);
                return 0f;
            }

            public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
            {
                int oneHanded = hero.GetSkillValue(DefaultSkills.OneHanded);
                int twoHanded = hero.GetSkillValue(DefaultSkills.TwoHanded);
                int polearm = hero.GetSkillValue(DefaultSkills.Polearm);
                if (oneHanded >= twoHanded && oneHanded >= polearm)
                    return new ValueTuple<SkillObject, int>(DefaultSkills.OneHanded, AltSolCompanionSkillRequired);
                else if (twoHanded >= polearm)
                    return new ValueTuple<SkillObject, int>(DefaultSkills.TwoHanded, AltSolCompanionSkillRequired);
                else
                    return new ValueTuple<SkillObject, int>(DefaultSkills.Polearm, AltSolCompanionSkillRequired);
            }

            protected override void AfterIssueCreation()
            {
                this._issueSettlement = base.IssueOwner.CurrentSettlement;
            }

            public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
            {
                explanation = TextObject.Empty;
                return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, (int) (base.GetTotalAlternativeSolutionNeededMenCount() * 1.5f), ref explanation, 2, false);
            }

            public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
            {
                return character.Tier >= AltSolMinimumTroopTier;
            }

            public override bool AlternativeSolutionCondition(out TextObject explanation)
            {
                explanation = TextObject.Empty;
                return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, (int)(base.GetTotalAlternativeSolutionNeededMenCount() * 1.5f), ref explanation, 2);
            }

            protected override void AlternativeSolutionEndWithSuccessConsequence()
            {
                this.RelationshipChangeWithIssueOwner = AltSolRelationRewardOnSuccess;
                base.IssueOwner.AddPower(IssueOwnerPowerBonusOnSuccess);
                this._issueSettlement.Village.Bound.Town.Prosperity += SettlementProsperityBonusOnSuccess;
                TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(base.IssueOwner, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
                });
                GainRenownAction.Apply(Hero.MainHero, 1f);
                MFHideoutCampaignBehavior.ApplyHideoutRaidConsequences(_targetHideout);
                MFHideoutManager.Current.ClearHideout(Helpers.GetMFHideout(_targetHideout));
            }

            protected override void AlternativeSolutionEndWithFailureConsequence()
            {
                this.RelationshipChangeWithIssueOwner = AltSolRelationPenaltyOnFail;
                base.IssueOwner.AddPower(IssueOwnerPowerPenaltyOnFail);
                this._issueSettlement.Village.Bound.Town.Prosperity += SettlementProsperityPenaltyOnFail;
                MFHideoutCampaignBehavior.ApplyHideoutRaidConsequences(_targetHideout);
            }

            protected override void OnGameLoad()
            {
            }

            protected override void HourlyTick()
            {
            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new NearbyMFHideoutIssueQuest(questId, base.IssueOwner, this._targetHideout, this._issueSettlement, this.RewardGold, CampaignTime.DaysFromNow(QuestTimeLimit));
            }

            public override IssueFrequency GetFrequency()
            {
                return NearbyHideoutIssueFrequency;
            }

            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flags, out Hero relationHero, out SkillObject skill)
            {
                flags = PreconditionFlags.None;
                relationHero = null;
                skill = null;
                if (issueGiver.GetRelationWithPlayer() < MFHideoutModels.MinRelationToGetMFQuest
                    || Helpers.IsRivalOfMinorFaction(Hero.MainHero.MapFaction as Kingdom, issueGiver.Clan))
                {
                    flags |= PreconditionFlags.Relation;
                    relationHero = issueGiver;
                }
                if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero.MapFaction))
                {
                    flags |= PreconditionFlags.AtWar;
                }
                return flags == PreconditionFlags.None;
            }

            public override bool IssueStayAliveConditions()
            {
                return Helpers.GetMFHideout(this._targetHideout).IsActive 
                    && base.IssueOwner.CurrentSettlement.IsVillage 
                    && !base.IssueOwner.CurrentSettlement.IsRaided 
                    && !base.IssueOwner.CurrentSettlement.IsUnderRaid 
                    && base.IssueOwner.CurrentSettlement.Village.Bound.Town.Security <= 80f
                    && FactionManager.IsAtWarAgainstFaction(this._targetHideout.OwnerClan.MapFaction, base.IssueOwner.MapFaction);
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {
            }

            public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
            {
                get => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;
            }

            public override int AlternativeSolutionBaseNeededMenCount
            {
                get => AltSolFinalMenCount;
            }

            protected override int AlternativeSolutionBaseDurationInDaysInternal
            {
                get => 4 + MathF.Ceiling(6f * base.IssueDifficultyMultiplier);
            }

            protected override int RewardGold
            {
                get => 3000;
            }

            protected override int CompanionSkillRewardXP
            {
                get => (int)(1000f + 1250f * base.IssueDifficultyMultiplier);
            }

            public override bool CanBeCompletedByAI()
            {
                return false;
            }

            public override bool IsThereLordSolution
            {
                get => false;
            }


            internal Settlement TargetHideout
            {
                get => this._targetHideout;
            }

            private const int AltSolFinalMenCount = 10;

            private const int AltSolMinimumTroopTier = 2;

            private const int AltSolCompanionSkillRequired = 120;

            private const int AltSolRelationRewardOnSuccess = 5;

            private const int AltSolRelationPenaltyOnFail = -5;

            private const int IssueOwnerPowerBonusOnSuccess = 5;

            private const int IssueOwnerPowerPenaltyOnFail = -5;

            private const int SettlementProsperityBonusOnSuccess = 10;

            private const int SettlementProsperityPenaltyOnFail = -10;

            private const int IssueDuration = 15;

            private const int QuestTimeLimit = 30;

            [SaveableField(100)]
            private readonly Settlement _targetHideout;

            [SaveableField(101)]
            private Settlement _issueSettlement;
        }

        public class NearbyMFHideoutIssueQuest : QuestBase
        {
            private Clan QuestClan()
            {
                return base.QuestGiver.CurrentSettlement.OwnerClan;
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            public override TextObject Title
            {
                get => setCommonTextVariables(new TextObject("{MINOR_FACTION} Base Near {QUEST_SETTLEMENT.NAME}"));
            }
            private TextObject _onQuestStartedLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=ogsh3V6G}{QUEST_GIVER.LINK}, a headman from {QUEST_SETTLEMENT}, has told you about the hideout of some" +
                        " {MINOR_FACTION} members who have recently been attacking local villagers. You told {?QUEST_GIVER.GENDER}her{?}him{\\?} " +
                        "that you will take care of the situation yourself. {QUEST_GIVER.LINK} also marked the location of the hideout on your map."));
            }
            private TextObject _onQuestSucceededLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=SN3pjZiK}You received a message from {QUEST_GIVER.LINK}.\n\"Thank you for clearing out that bandits' nest. " +
                        "Please accept these {REWARD}{GOLD_ICON} denars with our gratitude.\""));
            }
            private TextObject _onQuestFailedLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=qsMnnfQ3}You failed to clear the hideout in time to prevent further attacks. {QUEST_GIVER.LINK} is disappointed."));
            }
            private TextObject _onHideoutClearedLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=4Bub0GY6}{MINOR_FACTION} Hideout was cleared by someone else. Your agreement with {QUEST_GIVER.LINK} is canceled."));
            }
            private TextObject _onNoLongerAtWarLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=4Bub0GY6}{SETTLEMENT_FACTION} is no longer at war with {MINOR_FACTION}. Your agreement with {QUEST_GIVER.LINK} is canceled.")
                        .SetTextVariable("SETTLEMENT_FACTION", this._questSettlement.MapFaction.Name));
            }
            private TextObject _onClanDestroyedLogText
            {
                get => setCommonTextVariables(
                    new TextObject("{=4Bub0GY6}The {MINOR_FACTION} are no more. Your agreement with {QUEST_GIVER.LINK} is canceled."));
            }

            private TextObject setCommonTextVariables(TextObject text)
            {
                text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                return text.SetTextVariable("MINOR_FACTION", MFClan.Name)
                    .SetTextVariable("REWARD", this.RewardGold)
                    .SetTextVariable("QUEST_SETTLEMENT", this._questSettlement.EncyclopediaLinkWithName)
                    .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            }

            protected Clan MFClan
            {
                get => _targetHideout.OwnerClan;
            }

            public NearbyMFHideoutIssueQuest(string questId, Hero questGiver, Settlement targetHideout, Settlement questSettlement, int rewardGold, CampaignTime duration) : base(questId, questGiver, duration, rewardGold)
            {
                this._targetHideout = targetHideout;
                this._questSettlement = questSettlement;
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            protected override void HourlyTick()
            {
            }

            protected override void SetDialogs()
            {
                this.OfferDialogFlow = DialogFlow
                    .CreateDialogFlow("issue_classic_quest_start")
                        .NpcLine("{=spj8bYVo}Good! I'll mark the hideout for you on a map.[if:convo_excited]")
                        .Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
                        .Consequence(new ConversationSentence.OnConsequenceDelegate(this.OnQuestAccepted))
                    .CloseDialog();
                this.DiscussDialogFlow = DialogFlow
                    .CreateDialogFlow("quest_discuss", 100)
                        .NpcLine("{=l9wYpIuV}Any news? Have you managed to clear out the hideout yet?[if:convo_astonished]")
                        .Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
                    .BeginPlayerOptions()
                        .PlayerOption("{=wErSpkjy}I'm still working on it.")
                            .NpcLine("{=XTt6gZ7h}Do make haste, if you can. As long as those scoundrels are up there, no traveller is safe![if:convo_grave]")
                            .CloseDialog()
                        .PlayerOption("{=I8raOMRH}Sorry. No progress yet.")
                            .NpcLine("{=kWruAXaF}Well... You know as long as those scoundrels remain there, no traveller is safe.[if:convo_grave]")
                            .CloseDialog()
                    .EndPlayerOptions()
                    .CloseDialog();
            }

            private void OnQuestAccepted()
            {
                base.StartQuest();

                Helpers.GetMFHideout(this._targetHideout).IsSpotted = true;
                this._targetHideout.IsVisible = true;
                base.AddTrackedObject(this._targetHideout);
                QuestHelper.AddMapArrowFromPointToTarget(new TextObject("Direction to Hideout"), this._questSettlement.Position2D, this._targetHideout.Position2D, 5f, 0.1f);
                MBInformationManager.AddQuickInformation(setCommonTextVariables(
                    new TextObject("{=XGa8MkbJ}{QUEST_GIVER.NAME} has marked the hideout on your map")));
                base.AddLog(this._onQuestStartedLogText);
            }

            private void OnQuestSucceeded()
            {
                base.AddLog(this._onQuestSucceededLogText);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold);
                GainRenownAction.Apply(Hero.MainHero, 1f, false);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
                });
                base.QuestGiver.AddPower(QuestGiverPowerBonus);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonus;
                this._questSettlement.Village.Bound.Town.Prosperity += TownProsperityBonus;
                base.CompleteQuestWithSuccess();
            }

            private void OnQuestFailed(bool isTimedOut)
            {
                base.AddLog(this._onQuestFailedLogText);
                if (!isTimedOut)
                {
                    base.QuestGiver.AddPower(QuestGiverPowerPenalty);
                    this._questSettlement.Village.Bound.Town.Prosperity += TownProsperityPenalty;
                    this._questSettlement.Village.Bound.Town.Security += TownSecurityPenalty;
                    this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenalty;
                    base.CompleteQuestWithFail();
                }
            }

            protected override void OnTimedOut()
            {
                this.OnQuestFailed(true);
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
                CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnHideoutCleared));
                CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
                CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
            }

            private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
            {
                // Fails quest if player tries raiding/coercing village
                if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
                {
                    QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
                }
            }

            private void OnHideoutCleared(Settlement hideout)
            {
                if (this._targetHideout == hideout)
                {
                    base.CompleteQuestWithCancel();
                }
            }

            private void OnMapEventEnded(MapEvent mapEvent)
            {
                if (mapEvent.IsHideoutBattle && mapEvent.MapEventSettlement == this._targetHideout)
                {
                    if (Enumerable.Contains<PartyBase>(mapEvent.InvolvedParties, PartyBase.MainParty))
                    {
                        if (mapEvent.BattleState == BattleState.DefenderVictory || mapEvent.BattleState == BattleState.None)
                        {
                            this.OnQuestFailed(false);
                            return;
                        }
                        if (mapEvent.BattleState == BattleState.AttackerVictory)
                        {
                            this.OnQuestSucceeded();
                            return;
                        }
                    }
                    else if (mapEvent.BattleState == BattleState.AttackerVictory)
                    {
                        base.AddLog(this._onHideoutClearedLogText);
                        base.CompleteQuestWithCancel();
                    }
                }
            }

            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (!FactionManager.IsAtWarAgainstFaction(QuestGiver.MapFaction, _targetHideout.OwnerClan))
                {
                    base.AddLog(this._onNoLongerAtWarLogText);
                    base.CompleteQuestWithCancel();
                }
            }

            private void OnClanDestroyed(Clan clan)
            {
                if (MFClan == clan)
                {
                    base.AddLog(this._onClanDestroyedLogText);
                    base.CompleteQuestWithCancel();
                }
            }

            private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            {
                if (!FactionManager.IsAtWarAgainstFaction(QuestGiver.MapFaction, _targetHideout.OwnerClan))
                {
                    base.AddLog(this._onNoLongerAtWarLogText);
                    base.CompleteQuestWithCancel();
                }
            }

            private const int QuestGiverRelationBonus = 5;

            private const int QuestGiverRelationPenalty = -5;

            private const int QuestGiverPowerBonus = 5;

            private const int QuestGiverPowerPenalty = -5;

            private const int TownProsperityBonus = 10;

            private const int TownProsperityPenalty = -10;

            private const int TownSecurityPenalty = -5;

            [SaveableField(100)]
            private readonly Settlement _targetHideout;

            [SaveableField(101)]
            private readonly Settlement _questSettlement;
        }

        private const int NearbyHideoutMaxDistance = 40;

        private const IssueBase.IssueFrequency NearbyHideoutIssueFrequency = IssueBase.IssueFrequency.VeryCommon;
        public class NearbyMFHideoutIssueTypeDefiner : SaveableTypeDefiner
        {
            public NearbyMFHideoutIssueTypeDefiner() : base(404_322_929)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(NearbyMFHideoutIssue), 1);
                base.AddClassDefinition(typeof(NearbyMFHideoutIssueQuest), 2);
            }
        }
    }
}

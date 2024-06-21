using System;
using System.Collections.Generic;
using static ImprovedMinorFactions.IMFModels;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using MathF = TaleWorlds.Library.MathF;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using System.Linq;
using System.IO.Ports;

namespace ImprovedMinorFactions.Source.Quests.MFNomadNeedsVillageRaidedIssueBehavior.cs
{
    internal class MFNomadNeedsVillageRaidedIssueBehavior : CampaignBehaviorBase
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
                && issueGiver.Clan.IsNomad 
                && issueGiver.Gold > 2000
                && Helpers.IsMFHideout(issueGiver.HomeSettlement)
                && issueGiver.Clan.IsUnderMercenaryService;
        }

        private const IssueBase.IssueFrequency _IssueFrequency = IssueBase.IssueFrequency.Common;

        internal const MFRelation RelationLevelNeededForQuest = MFRelation.Tier1;

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnSelected), typeof(NomadNeedsVillageRaidedIssue), _IssueFrequency)
                    );
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(NomadNeedsVillageRaidedIssue), _IssueFrequency));
        }

        private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new NomadNeedsVillageRaidedIssue(issueOwner);
        }

        public class NomadNeedsVillageRaidedIssue : IssueBase
        {
            private int RequestedVillageCount
            {
                get => MathF.Ceiling(2 * base.IssueDifficultyMultiplier);
            }

            protected override int RewardGold
            {
                get => 4000;
            }

            private TextObject SetTextVariables(TextObject text)
            {
                text.SetCharacterProperties("QUEST_GIVER", base.IssueOwner.CharacterObject);
                text.SetTextVariable("NEEDED_VILLAGES_AMOUNT", this.RequestedVillageCount);
                text.SetTextVariable("PLURAL", this.RequestedVillageCount > 1 ? 1 : 0);
                text.SetTextVariable("MINOR_FACTION", IssueClan().Name);
                return text;
            }

            public override TextObject IssueBriefByIssueGiver
            {
                get => SetTextVariables(new TextObject("{=!}As you likely know the {MINOR_FACTION} are a nomadic people " +
                    "and believe we have the right to graze our livestock wherever we please. However some villages near our " +
                    "camp have decided that they can fence off portions of the land and call it their \"property\". " +
                    "This practice is making it harder and harder for us to find proper grazing grounds for our herds." +
                    "[ib:hip][if:convo_undecided_closed]"));
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=kuubi1UH}Very well, and how would you like this to be handled?");
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get => SetTextVariables(new TextObject("{=!}We need you to raid {NEEDED_VILLAGES_AMOUNT} {?PLURAL}village{?}villages{\\?} " +
                    "near our camp. Make sure to destroy all of their fences and kill most of their livestock so they think" +
                    " before getting greedy next time. They cannot know that you are doing this on our behalf."));
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => new TextObject("{=!}I can undo this injustice.");
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
                get => new TextObject("{=!}{MINOR_FACTION} Needs Village Raided").SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public override TextObject Description
            {
                get => new TextObject("{=!}{MINOR_FACTION} Needs Village Raided").SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public NomadNeedsVillageRaidedIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration))
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
                return new NomadNeedsVillageRaidedQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration), this.RequestedVillageCount);
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

            // TODO: adapt -> deal with nomad camp movement (delay it until after quest failed) 
            // TODO: also make the issue only appear if there is war for the player or something
            // TODO: only give issue if clan not at war with target villages
            public override bool IssueStayAliveConditions()
            {
                return IssueClan().IsUnderMercenaryService && Helpers.IsMFHideout(base.IssueOwner.HomeSettlement)
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

        public class NomadNeedsVillageRaidedQuest : QuestBase
        {
            public NomadNeedsVillageRaidedQuest(string questId, Hero questGiver, CampaignTime duration, int requestedVillageCount) : base(questId, questGiver, duration, 0)
            {
                this._requestedVillageCount = requestedVillageCount;
                this._raidedVillageCount = 0;
                this._rewardGold = RewardGold;
                this._playerReachedRequestedAmount = false;
                this._targetVillages = new List<Village>();
                this._raidedVillages = new List<Village>();
                this.SetDialogs();
                base.InitializeQuestOnCreation();
                this.DetermineTargetVillages();
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));

                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
                CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
            }

            private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
            {
                MapEvent mapEvent = raidEvent.MapEvent;
                Village? village = mapEvent?.MapEventSettlement?.Village;
                if (village != null && mapEvent!.IsRaid && mapEvent.IsPlayerMapEvent && mapEvent.PlayerSide == winnerSide 
                    && !_raidedVillages.Contains(village) && _targetVillages.Contains(village))
                {
                    this._raidedVillageCount++;
                    this._questProgressLogTest!.UpdateCurrentProgress(this._raidedVillageCount);
                    Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, PlayerRogueryXpForRaid);

                    this._targetVillages.Remove(village);
                    base.RemoveTrackedObject(village.Settlement);
                    this._raidedVillages.Add(village);
                    if (_raidedVillageCount >= _requestedVillageCount)
                    {
                        this.ApplyQuestSuccessConsequences();
                        this.CompleteQuestWithSuccess();
                    }
                }
            }
            private Settlement? QuestHideout()
            {
                return this.QuestGiver.HomeSettlement;
            }

            public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
            {
            }

            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestGiver.MapFaction, Hero.MainHero.MapFaction))
                    base.CompleteQuestWithCancel(QuestCancelledDueToWarLog);

                if (clan == QuestGiver.Clan)
                    _targetVillages.RemoveAll(village => village?.Settlement.MapFaction == QuestGiver.MapFaction);
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
                this._questProgressLogTest = base.AddDiscreteLog(this.QuestStartedLogText, new TextObject("{=AJXiWK0TL1}Caravans Extorted"), this._raidedVillageCount, this._requestedVillageCount);
                // add tracking for target villages
                foreach (var village in _targetVillages)
                {
                    base.AddTrackedObject(village.Settlement);
                }
            }

            private void DetermineTargetVillages()
            {
                // Get villages close enough to the hideout and track them
                LocatableSearchData<Settlement> data = Settlement.StartFindingLocatablesAroundPosition(
                    QuestHideout()!.Position2D, _maxDistanceToHideoutForTargetVillage);

                for (Settlement settlement = Settlement.FindNextLocatable(ref data); settlement != null; settlement = Settlement.FindNextLocatable(ref data))
                {
                    if (settlement.Position2D == null || !settlement.IsVillage || !settlement.IsActive || _targetVillages.Contains(settlement.Village))
                        continue;
                    if (_targetVillages.Count > 4)
                        break;

                    this._targetVillages.Add(settlement.Village);
                }

                _requestedVillageCount = MathF.Min(_requestedVillageCount, _targetVillages.Count);

                if (_requestedVillageCount == 0)
                    throw new Exception($"No villages close enough to {QuestClan().Name} Camp for nomad village raid quest");
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
                    .NpcOption(new TextObject("{=!}Have you completed your task? Our livestock need somewhere to graze.[ib:confident3]"),
                                () => Hero.OneToOneConversationHero == this.QuestGiver && !this._playerReachedRequestedAmount)
                        .BeginPlayerOptions()
                        .PlayerOption(new TextObject("{=!}I am still working on it."))
                            .NpcLine(new TextObject("{=!}I can hear my horse's stomach grumbling. He is not very patient."))
                            .CloseDialog()
                        .EndPlayerOptions()
                    .CloseDialog()
                    .EndNpcOptions();
            }

            public override TextObject Title
            {
                get => new TextObject("{=!}{MINOR_FACTION} Needs Villages Raided").SetTextVariable("MINOR_FACTION", QuestClan().Name);
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            private TextObject QuestStartedLogText
            {
                get => SetTextVariables(new TextObject("{=!}{QUEST_GIVER.LINK} told you that {?QUEST_GIVER.GENDER}her{?}his{\\?} people want " +
                        "{?PLURAL}a village{?}villages{\\?} near their camp raided. {?QUEST_GIVER.GENDER}She{?}He{\\?} asked " +
                        "you to raid {NEEDED_VILLAGES_AMOUNT} {?PLURAL}village{?}villages{\\?}."));
            }

            private TextObject QuestSuccessLog
            {
                get => SetTextVariables(new TextObject("{=!}You have raided {?PLURAL}a village{?}villages{\\?} for {QUEST_GIVER.LINK}" +
                        " as promised."));
            }

            private TextObject QuestCancelledQuestGiverDiedLog
            {
                get => SetTextVariables(
                    new TextObject("{=gOEIZ30vl}{QUEST_GIVER.LINK} has died. {?QUEST_GIVER.GENDER}She{?}He{\\?} has no more desires."));
            }

            private TextObject QuestCancelledDueToWarLog
            {
                get => SetTextVariables(new TextObject("{=TrewB5c7}Now your {?IS_MAP_FACTION}clan{?}kingdom{\\?} is at war with the {QUEST_GIVER.LINK}'s lord. " +
                        "Your agreement with {QUEST_GIVER.LINK} becomes invalid."));
            }

            private TextObject QuestCancelledDueToPlayerHostilityLog
            {
                get => SetTextVariables(new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure."));
            }

            private TextObject QuestFailedWithTimeOutLogText
            {
                get => SetTextVariables(
                    new TextObject("{=!}You have failed to raid the {?PLURAL}village{?}villages{\\?} in time. {QUEST_GIVER.LINK} must be disappointed."));
            }

            private TextObject SetTextVariables(TextObject text)
            {
                text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                text.SetTextVariable("NEEDED_VILLAGES_AMOUNT", this._requestedVillageCount);
                text.SetTextVariable("PLURAL", this._requestedVillageCount > 1 ? 1 : 0);
                text.SetTextVariable("MINOR_FACTION", QuestClan().Name);
                return text;
            }

            private void ApplyQuestSuccessConsequences()
            {
                base.AddLog(this.QuestSuccessLog, false);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.RogueSkills, PlayerRogueryBonusOnSuccess)
                });

                // add criminal rating in every faction that was raided
                List<IFaction> factionsRaided = new List<IFaction>();
                foreach (var village in _raidedVillages)
                {
                    IFaction ownerFaction = village.Owner.MapFaction;
                    if (!factionsRaided.Contains(ownerFaction))
                    {
                        ChangeCrimeRatingAction.Apply(ownerFaction, 10f);
                        factionsRaided.Add(ownerFaction);
                    }
                }

                ChangeRelationAction.ApplyPlayerRelation(this.QuestGiver, ClanRelationBonusOnSuccess);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);

                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonusOnSuccess;
            }

            protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
            {
                if (this._raidedVillageCount >= this._requestedVillageCount)
                {
                    completeWithSuccess = true;
                    this.ApplyQuestSuccessConsequences();
                }
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

            protected override void HourlyTick() { }

            private Clan QuestClan()
            {
                return base.QuestGiver.Clan;
            }

            private int PlayerRogueryBonusOnSuccess
            {
                get => _requestedVillageCount * 30;
            }


            private const int QuestGiverRelationBonusOnSuccess = 10;

            private const int ClanRelationBonusOnSuccess = 10;

            private const int QuestGiverRelationPenaltyOnFail = -5;

            private const float PlayerRogueryXpForRaid = 2000f;

            private const float _maxDistanceToHideoutForTargetVillage= 30f;

            private float _maxDistanceSquaredToHideoutForTargetVillage = MathF.Pow(30f, 2);


            [SaveableField(1)]
            private int _requestedVillageCount;

            [SaveableField(5)]
            private int _raidedVillageCount;

            [SaveableField(6)]
            private int _rewardGold;

            [SaveableField(7)]
            private JournalLog? _questProgressLogTest;

            [SaveableField(9)]
            private bool _playerReachedRequestedAmount;

            [SaveableField(10)]
            private List<Village> _targetVillages;

            [SaveableField(11)]
            private List<Village> _raidedVillages;
        }

        public class MFNomadNeedsVillageRaidedTypeDefiner : SaveableTypeDefiner
        {
            public MFNomadNeedsVillageRaidedTypeDefiner() : base(113_932_432)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(NomadNeedsVillageRaidedIssue), 1);
                base.AddClassDefinition(typeof(NomadNeedsVillageRaidedQuest), 2);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using Helpers;
using static ImprovedMinorFactions.IMFModels;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Extensions;
using MathF = TaleWorlds.Library.MathF;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.Quests.SectEvangelism
{
    internal class SectEvangelismIssueBehavior : CampaignBehaviorBase
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
            return Helpers.IsMFHideout(issueGiver.CurrentSettlement) 
                && issueGiver.IsNotable 
                && issueGiver.CurrentSettlement.OwnerClan.IsMinorFaction
                && issueGiver.CurrentSettlement.OwnerClan.IsSect;
        }

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnSelected), typeof(SectEvangelismIssue), _IssueFrequency)
                    );
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(SectEvangelismIssue), _IssueFrequency));
        }

        private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new SectEvangelismIssue(issueOwner);
        }

        private const IssueBase.IssueFrequency _IssueFrequency = IssueBase.IssueFrequency.Common;

        internal const MFRelation RelationLevelNeededForQuest = MFRelation.Tier2;

        public class SectEvangelismIssue : IssueBase
        {
            // TODO: adapt -> companion needs to have charm to do this
            public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
            {
                get => AlternativeSolutionScaleFlag.RequiredTroops;
            }

            // 3 - 7 settlements needed
            private int RequestedSettlementCount
            {
                get => 3 + MathF.Floor(4f * base.IssueDifficultyMultiplier);
            }

            // 15 - 25 men needed
            public override int AlternativeSolutionBaseNeededMenCount
            {
                get => 15 + MathF.Floor(10f * base.IssueDifficultyMultiplier);
            }

            protected override int AlternativeSolutionBaseDurationInDaysInternal
            {
                get => 6 + MathF.Ceiling(7f * base.IssueDifficultyMultiplier);
            }

            protected override int RewardGold
            {
                get => 1000 + RequestedSettlementCount * AlternativeSolutionRewardPerSettlement;
            }

            // TODO: adapt
            public override TextObject IssueBriefByIssueGiver
            {
                get
                {
                    var text = new TextObject("{=3mADItXNb}Yes... As you no doubt know, this is rough work, and I've lost a lot of good lads recently. I haven't had much luck replacing them. " +
                    "I need men who understand how things work in our business, and that's not always easy to find. I need capable {MOUNTED}{TROOP_TYPE}...[ib:hip][if:convo_undecided_closed]");
                    return text;
                }
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=jGpBZDvC}I see. What do you want me to do?");
            }

            // TODO: adapt
            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    var text = new TextObject("{=kolxPW10J}Look, I know that warriors like you can sometimes recruit {MOUNTED}{TROOP_TYPE} to your party. Some of those men might want to take their chances working " +
                            "for me. More comfortable with us, where there's always drink and women on hand, than {?IS_OUTLAW}roaming endlessly about the countryside{?}working for a lord{\\?}, eh?" +
                            " For each one that signs up with me I'll give you a bounty, more if they have some experience.[if:convo_innocent_smile][ib:hip]");
                    return text;
                }
            }

            // TODO: adapt
            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => new TextObject("{=!}");
            }

            // TODO: adapt
            public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
            {
                get
                {
                    return new TextObject("{=FtHAENedg}You can also send me a recruiter: a trustworthy companion who is good at leading men, and also enough of a leader to recruit soldiers...[if:convo_undecided_open][ib:confident]");
                }
            }

            // TODO: adapt
            public override TextObject IssueAlternativeSolutionAcceptByPlayer
            {
                get => new TextObject("{=kxvnA811}All right, I will send you someone from my party who fits your bill.");
            }
            // TODO: adapt
            public override TextObject IssueAlternativeSolutionResponseByIssueGiver
            {
                get => new TextObject("{=8sDjwsnW}I'm sure your lieutenant will solve my problem. Thank you for your help.[if:convo_nonchalant][ib:demure2]");
            }
            // TODO: adapt
            public override TextObject IssueDiscussAlternativeSolution
            {
                get => new TextObject("{=5oIRMI8cL}Your companion seems to have a knack with the local youths. I hear a lot of fine lads have already signed up.[if:convo_relaxed_happy][ib:hip2]");
            }

            public override bool IsThereAlternativeSolution
            {
                get => true;
            }
            // TODO: let Embers lords do the evangelizing themselves (only if they are not under mercenary contract)
            public override bool IsThereLordSolution
            {
                get => false;
            }

            // TODO: adapt
            protected override TextObject AlternativeSolutionStartLog
            {
                get
                {
                    TextObject text = new TextObject("{=R3fkc5wSH}You asked {COMPANION.LINK} to deliver at least {WANTED_RECRUIT_AMOUNT} {MOUNTED}{TROOP_TYPE} to {ISSUE_GIVER.LINK} in {SETTLEMENT}. They should rejoin your party in {RETURN_DAYS} days.");
                    text.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
                    text.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
                    text.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
                    return text;
                }
            }
            // TODO: adapt
            public override TextObject Title
            {
                get
                {
                    return new TextObject("{=!}{MINOR_FACTION} Propaganda")
                        .SetTextVariable("MINOR_FACTION", IssueClan().Name);
                }
            }

            // TODO: adapt
            public override TextObject Description
            {
                get
                {
                    TextObject text = new TextObject("{=awjQeYiRo}A {MINOR_FACTION} Notable needs recruits for {?ISSUE_GIVER.GENDER}her{?}his{\\?} Clan.");
                    text.SetTextVariable("MINOR_FACTION", IssueClan().Name);
                    text.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, false);
                    return text;
                }
            }

            public SectEvangelismIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration))
            {
            }

            // TODO: make issue affect hideout somehow
            protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
            {
                return 0f;
            }

            public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
            {
                return new ValueTuple<SkillObject, int>(DefaultSkills.Charm, CompanionRequiredSkillLevel);
            }

            public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
            {
                explanation = TextObject.Empty;
                return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), ref explanation, AlternativeSolutionTroopTierRequirement);
            }

            public override bool AlternativeSolutionCondition(out TextObject explanation)
            {
                explanation = TextObject.Empty;
                return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), ref explanation, AlternativeSolutionTroopTierRequirement);
            }

            public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
            {
                return character.Tier >= AlternativeSolutionTroopTierRequirement;
            }

            protected override void OnGameLoad()
            {
            }

            protected override void HourlyTick()
            {
            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new SectEvangelismQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration), this.RequestedSettlementCount);
            }

            public override IssueFrequency GetFrequency()
            {
                return _IssueFrequency;
            }

            // TODO: adapt
            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero? relationHero, out SkillObject? skill)
            {
                flag = PreconditionFlags.None;
                relationHero = null;
                skill = null;

                if ((issueGiver?.MapFaction ?? issueGiver?.CurrentSettlement?.OwnerClan
                    ?? Hero.MainHero?.MapFaction) == null)
                {
                    return false;
                }

                if (issueGiver!.GetRelationWithPlayer() < IMFModels.MinRelationNeeded(RelationLevelNeededForQuest))
                {
                    flag |= PreconditionFlags.Relation;
                    relationHero = issueGiver;
                }
                if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero!.MapFaction)
                    || Helpers.ConsidersMFOutlaw(Hero.MainHero.MapFaction, issueGiver.CurrentSettlement.OwnerClan))
                {
                    flag |= PreconditionFlags.AtWar;
                }
                return flag == PreconditionFlags.None;
            }

            public override bool IssueStayAliveConditions()
            {
                return IssueClan().IsUnderMercenaryService;
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {
            }

            protected override int CompanionSkillRewardXP
            {
                get => (int)(500f + 500f * base.IssueDifficultyMultiplier);
            }

            // TODO: adapt -> which trait should we level?
            protected override void AlternativeSolutionEndWithSuccessConsequence()
            {
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, AlternativeSolutionCompanionHonorBonus)
                });
                base.IssueOwner.AddPower(AlternativeSolutionNotablePowerBonus);
                this.RelationshipChangeWithIssueOwner = AlternativeSolutionRelationBonus;
            }

            private Clan IssueClan()
            {
                return base.IssueOwner.CurrentSettlement.OwnerClan;
            }

            private const int IssueAndQuestDuration = 25;

            private const int AlternativeSolutionTroopTierRequirement = 2;

            private const int AlternativeSolutionRelationBonus = 5;

            private const int AlternativeSolutionNotablePowerBonus = 10;

            private const int AlternativeSolutionCompanionHonorBonus = 15;

            private const int AlternativeSolutionRewardPerSettlement = 250;

            private const int CompanionRequiredSkillLevel = 80;
        }

        public class SectEvangelismQuest : QuestBase
        {
            protected override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));

                CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            }

            private void AddGameMenus()
            {
                // TODO: add evangelize menu button to town and villages
                // add wait menu and onFinished add 1 village to quest and if the player leaves early do nothing.
            }

            private bool menu_evangelize_on_condition()
            {
                // TODO: evangelism conditions
                return false;
            }

            private void menu_evangelize_on_consequence()
            {
                // TODO: make them wait etc.
            }

            private void RemoveGameMenus()
            {
                // TODO: Once quest is done remove evangelism option
            }

            // TODO: culture check???
            private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
            {
                if (party == MobileParty.MainParty
                    && (settlement.IsTown || settlement.IsVillage) 
                    && (QuestClan().StringId != "embers_of_flame" || settlement.Culture == QuestClan().Culture)
                    && !_evangelizedSettlements.Contains(settlement))
                {
                    // give player the option to evangelize for a couple hours through popup or menu button
                    if (true)
                    {
                        this._evangelizedSettlementCount++;
                        this._evangelizedSettlements.Add(settlement);
                        ApplySettlementEvangelismConsequences(settlement);
                    }
                }
            }

            private void ApplySettlementEvangelismConsequences(Settlement settlement)
            {
                // TODO: criminal rating+, loyalty loss
            }

            private Clan QuestClan()
            {
                return base.QuestGiver.CurrentSettlement.OwnerClan;
            }

            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestClan().MapFaction, Hero.MainHero.MapFaction))
                {
                    base.CompleteQuestWithCancel(QuestCancelledDueToWarLog);
                }
            }

            private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestClan().MapFaction, Hero.MainHero.MapFaction))
                {
                    if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility)
                    {
                        CompleteQuestWithFail(QuestCancelledDueToPlayerHostilityLog);
                    }
                    else
                    {
                        base.CompleteQuestWithCancel(QuestCancelledDueToWarLog);
                    }
                }
            }

            private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
            {
                if (victim == this.QuestGiver)
                {
                    base.CompleteQuestWithCancel(QuestCancelledQuestGiverDiedLog);
                }
            }

            // TODO: adapt
            public override TextObject Title
            {
                get
                {
                    TextObject text = new TextObject("{=lpc6tS3bs}A {MINOR_FACTION} Notable needs recruits for {?ISSUE_GIVER.GENDER}her{?}his{\\?} Clan.");
                    text.SetTextVariable("MINOR_FACTION", QuestClan().Name);
                    text.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            // TODO: adapt
            private TextObject QuestStartedLogText
            {
                get
                {
                    TextObject text = new TextObject("{=kYU0MOSP1}{QUEST_GIVER.LINK}, a Notable of the {MINOR_FACTION}, told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs recruits for {?QUEST_GIVER.GENDER}her{?}his{\\?} Clan. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to recruit {NEEDED_RECRUIT_AMOUNT} {MOUNTED}{TROOP_TYPE} into your party, then transfer them to {?QUEST_GIVER.GENDER}her{?}him{\\?}. You will be paid for the recruits depending on their experience.");

                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    text.SetTextVariable("MINOR_FACTION", base.QuestGiver.CurrentSettlement.OwnerClan.EncyclopediaLinkWithName);
                    text.SetTextVariable("NEEDED_RECRUIT_AMOUNT", this._requestedSettlementCount);
                    return text;
                }
            }

            // TODO: adapt
            private TextObject QuestSuccessLog
            {
                get
                {
                    TextObject text = new TextObject("{=3ApJ6LaX}You have transferred the recruits to {QUEST_GIVER.LINK} as promised.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            // TODO: adapt
            private TextObject QuestCancelledQuestGiverDiedLog
            {
                get
                {
                    TextObject text = new TextObject("{=gOEIZ30vl}{QUEST_GIVER.LINK} has died. {?QUEST_GIVER.GENDER}She{?}He{\\?} has no more desires.")
                        .SetTextVariable("IS_MAP_FACTION", Clan.PlayerClan.IsMapFaction ? 1 : 0); ;
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            // TODO: adapt
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
            // TODO: adapt
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
            // TODO: adapt
            private TextObject QuestFailedWithTimeOutLogText
            {
                get
                {
                    TextObject textObject = new TextObject("{=iUmWTmQz}You have failed to deliver enough recruits in time. {QUEST_GIVER.LINK} must be disappointed.");
                    textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return textObject;
                }
            }
            // TODO: adapt
            public SectEvangelismQuest(string questId, Hero questGiver, CampaignTime duration, int requestedSettlementCount) : base(questId, questGiver, duration, 0)
            {
                this._requestedSettlementCount = requestedSettlementCount;
                this._evangelizedSettlementCount = 0;
                // TODO: adapt????
                //this._rewardGold = 2000;
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();
                base.AddTrackedObject(base.QuestGiver.CurrentSettlement);
                this._questProgressLog = base.AddDiscreteLog(this.QuestStartedLogText, new TextObject("{=!}Settlements Visited"), this._evangelizedSettlementCount, this._requestedSettlementCount);
            }

            protected override void SetDialogs()
            {
                this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start")
                    .NpcLine(new TextObject("{=!}I'll be waiting. May you be blessed.[if:convo_relaxed_happy][ib:confident]"))
                    .Condition(() => Hero.OneToOneConversationHero == this.QuestGiver)
                    .Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
                    .CloseDialog();

                this.DiscussDialogFlow = DialogFlow
                    .CreateDialogFlow("quest_discuss")
                        .NpcLine("{=!}Any news? Have you spread the word to enough settlements?[if:convo_astonished]")
                        .Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
                    .BeginPlayerOptions()
                        .PlayerOption("{=wErSpkjy}I'm still working on it.")
                            .NpcLine("{=!}Do make haste, many people are lost and searching for purpose.[if:convo_grave]")
                            .CloseDialog()
                    .EndPlayerOptions()
                    .CloseDialog();
            }

            // TODO: adapt
            private void ApplyQuestSuccessConsequences()
            {
                base.AddLog(this.QuestSuccessLog, false);
                
                // TODO: need better traits
                //TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                //{
                //    new Tuple<TraitObject, int>(DefaultTraits.Honor, PlayerHonorBonusOnSuccess)
                //});

                var mfHideout = Helpers.GetMFHideout(base.QuestGiver.CurrentSettlement);
                mfHideout!.Hearth += QuestSettlementHearthBonusOnSuccess;

                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, ClanRelationBonusOnSuccess);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold);
                base.QuestGiver.AddPower(QuestGiverNotablePowerBonusOnSuccess);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonusOnSuccess;
            }

            // TODO: adapt quest should auto complete when enough evangelism has happened
            protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
            {
                if (this._evangelizedSettlementCount >= this._requestedSettlementCount)
                {
                    completeWithSuccess = true;
                    this.ApplyQuestSuccessConsequences();
                }
            }

            protected override void OnTimedOut()
            {
                base.AddLog(this.QuestFailedWithTimeOutLogText);
                base.QuestGiver.AddPower(NotablePowerPenaltyOnFail);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenaltyOnFail;
            }

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }

            protected override void HourlyTick()
            {
            }

            private const int QuestGiverRelationBonusOnSuccess = 5;

            private const int ClanRelationBonusOnSuccess = 7;

            private const int QuestGiverNotablePowerBonusOnSuccess = 20;

            private const int QuestGiverRelationPenaltyOnFail = -10;

            private const int QuestSettlementHearthBonusOnSuccess = 25;

            private const int NotablePowerPenaltyOnFail = 0;

            private const int PlayerHonorBonusOnSuccess = 30;

            [SaveableField(1)]
            private int _requestedSettlementCount;

            [SaveableField(5)]
            private int _evangelizedSettlementCount;

            [SaveableField(6)]
            private int _rewardGold;

            [SaveableField(7)]
            private JournalLog _questProgressLog;

            [SaveableField(8)]
            private List<Settlement> _evangelizedSettlements;
        }

        public class SectEvangelismIssueBehaviorTypeDefiner : SaveableTypeDefiner
        {
            public SectEvangelismIssueBehaviorTypeDefiner() : base(825_266_064)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(SectEvangelismIssue), 1);
                base.AddClassDefinition(typeof(SectEvangelismQuest), 2);
            }
        }
    }
}

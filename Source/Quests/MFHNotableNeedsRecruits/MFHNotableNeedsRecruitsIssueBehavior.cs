using System;
using System.Collections.Generic;
using Helpers;
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
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using System.Linq;
using TaleWorlds.CampaignSystem.MapEvents;

namespace ImprovedMinorFactions.Source.Quests.MFHNotableNeedsRecruits
{
    // Token... comment means function has not yet been adapted to mod
    public class MFHNotableNeedsRecruitsIssueBehavior : CampaignBehaviorBase
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
            return Helpers.isMFHideout(issueGiver.CurrentSettlement) && issueGiver.IsNotable;
        }

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnSelected), typeof(MFHNotableNeedsRecruitsIssue), _IssueFrequency)
                    );
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MFHNotableNeedsRecruitsIssue), _IssueFrequency));
        }

        private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new MFHNotableNeedsRecruitsIssue(issueOwner);
        }

        private const IssueBase.IssueFrequency _IssueFrequency = IssueBase.IssueFrequency.VeryCommon;

        public class MFHNotableNeedsRecruitsIssue : IssueBase
        {

            public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
            {
                get => AlternativeSolutionScaleFlag.RequiredTroops;
            }

            private int RequestedRecruitCount
            {
                get => 6 + MathF.Ceiling(10f * base.IssueDifficultyMultiplier);
            }

            public override int AlternativeSolutionBaseNeededMenCount
            {
                get => 11 + MathF.Ceiling(9f * base.IssueDifficultyMultiplier);
            }

            protected override int AlternativeSolutionBaseDurationInDaysInternal
            {
                get => 6 + MathF.Ceiling(7f * base.IssueDifficultyMultiplier);
            }

            protected override int RewardGold
            {
                get => 2000 + RequestedRecruitCount * AlternativeSolutionRewardPerRecruit;
            }

            public override TextObject IssueBriefByIssueGiver
            {
                get
                {
                    var text = new TextObject("Yes... As you no doubt know, this is rough work, and I've lost a lot of good lads recently. I haven't had much luck replacing them. " +
                    "I need men who understand how things work in our business, and that's not always easy to find. I need capable {MOUNTED}{TROOP_TYPE}...[ib:hip][if:convo_undecided_closed]");
                    setTextTroopDescriptions(text);
                    return text;
                }
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=jGpBZDvC}I see. What do you want from me?");
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    var text = new TextObject("Look, I know that warriors like you can sometimes recruit {MOUNTED}{TROOP_TYPE} to your party. Some of those men might want to take their chances working " +
                            "for me. More comfortable with us, where there's always drink and women on hand, than {ACTION}, eh?" +
                            " For each one that signs up with me I'll give you a bounty, more if they have some experience.[if:convo_innocent_smile][ib:hip]")
                        .SetTextVariable("ACTION", IssueClan().IsOutlaw ? "roaming endlessly about the countryside" : "working for a lord");
                    setTextTroopDescriptions(text);
                    return text;
                }
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => new TextObject("{=ekLDmgS7}I'll find your recruits.");
            }

            public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
            {
                get
                {
                    if (IssueClan().IsOutlaw)
                        return new TextObject("{=bKfaMFVK}You can also send me a recruiter: a trustworthy companion who is good at leading men, and also enough of a rogue to win the trust of other rogues...[if:convo_undecided_open][ib:confident]");
                    else
                        return new TextObject("You can also send me a recruiter: a trustworthy companion who is good at leading men, and also enough of a leader to recruit soldiers...[if:convo_undecided_open][ib:confident]");
                }
            }

            public override TextObject IssueAlternativeSolutionAcceptByPlayer
            {
                get => new TextObject("{=kxvnA811}All right, I will send you someone from my party who fits your bill.");
            }

            public override TextObject IssueAlternativeSolutionResponseByIssueGiver
            {
                get => new TextObject("{=8sDjwsnW}I'm sure your lieutenant will solve my problem. Thank you for your help.[if:convo_nonchalant][ib:demure2]");
            }

            public override TextObject IssueDiscussAlternativeSolution
            {
                get => new TextObject("{=TkvsBd4H}Your companion seems to have a knack with the local never-do-wells. I hear a lot of fine lads have already signed up.[if:convo_relaxed_happy][ib:hip2]");
            }

            public override bool IsThereAlternativeSolution
            {
                get => true;
            }

            public override bool IsThereLordSolution
            {
                get => false;
            }

            protected override TextObject AlternativeSolutionStartLog
            {
                get
                {
                    TextObject textObject = new TextObject("{=wX14wxqF}You asked {COMPANION.LINK} to deliver at least {WANTED_RECRUIT_AMOUNT} {MOUNTED}{TROOP_TYPE} to {ISSUE_GIVER.LINK} in {SETTLEMENT}. They should rejoin your party in {RETURN_DAYS} days.");
                    setTextTroopDescriptions(textObject);
                    textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
                    textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
                    textObject.SetTextVariable("WANTED_RECRUIT_AMOUNT", this.RequestedRecruitCount);
                    textObject.SetTextVariable("SETTLEMENT", base.IssueOwner.CurrentSettlement.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays());
                    return textObject;
                }
            }

            private void setTextTroopDescriptions(TextObject t)
            {
                var questSettlement = base.IssueOwner.CurrentSettlement;
                if (questSettlement.OwnerClan.IsOutlaw)
                    t.SetTextVariable("TROOP_TYPE", "bandits");
                else
                    t.SetTextVariable("TROOP_TYPE", "soldiers");

                if (Helpers.mfIsMounted(questSettlement.OwnerClan))
                    t.SetTextVariable("MOUNTED", "mounted ");
                else
                    t.SetTextVariable("MOUNTED", "");
            }

            public override TextObject Title
            {
                get
                {
                    TextObject textObject = new TextObject("{MINOR_FACTION} Hideout Needs Recruits");
                    textObject.SetTextVariable("MINOR_FACTION", IssueClan().Name);
                    return textObject;
                }
            }

            public override TextObject Description
            {
                get
                {
                    TextObject textObject = new TextObject("A {MINOR_FACTION} Notable needs recruits for {?ISSUE_GIVER.GENDER}her{?}his{\\?} Clan");
                    textObject.SetTextVariable("MINOR_FACTION", IssueClan().Name);
                    textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, false);
                    return textObject;
                }
            }

            public MFHNotableNeedsRecruitsIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration))
            {
            }

            // Token: 0x06004B07 RID: 19207 RVA: 0x00159D3E File Offset: 0x00157F3E
            // TODO: make issue affect hideout somehow
            protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
            {
                if (issueEffect == DefaultIssueEffects.IssueOwnerPower)
                {
                    return -0.1f;
                }
                return 0f;
            }

            public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
            {
                if (IssueClan().IsOutlaw)
                    return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Leadership) >= hero.GetSkillValue(DefaultSkills.Roguery)) ? DefaultSkills.Leadership : DefaultSkills.Roguery, CompanionRequiredSkillLevel);
                else
                    return new ValueTuple<SkillObject, int>(DefaultSkills.Leadership, CompanionRequiredSkillLevel);
            }

            public override bool DoTroopsSatisfyAlternativeSolution(TroopRoster troopRoster, out TextObject explanation)
            {
                explanation = TextObject.Empty;
                bool mountedRequired = Helpers.mfIsMounted(base.IssueSettlement.OwnerClan);
                return QuestHelper.CheckRosterForAlternativeSolution(troopRoster, base.GetTotalAlternativeSolutionNeededMenCount(), ref explanation, AlternativeSolutionTroopTierRequirement, mountedRequired);
            }

            public override bool AlternativeSolutionCondition(out TextObject explanation)
            {
                explanation = TextObject.Empty;
                bool mountedRequired = Helpers.mfIsMounted(base.IssueSettlement.OwnerClan);
                return QuestHelper.CheckRosterForAlternativeSolution(MobileParty.MainParty.MemberRoster, base.GetTotalAlternativeSolutionNeededMenCount(), ref explanation, AlternativeSolutionTroopTierRequirement, mountedRequired);
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
                return new MFHNotableNeedsRecruitsIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(IssueAndQuestDuration), this.RequestedRecruitCount);
            }

            public override IssueFrequency GetFrequency()
            {
                return _IssueFrequency;
            }

            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
            {
                flag = PreconditionFlags.None;
                relationHero = null;
                skill = null;
                if (issueGiver.GetRelationWithPlayer() < MFHideoutModels.MinRelationToGetMFQuest)
                {
                    flag |= PreconditionFlags.Relation;
                    relationHero = issueGiver;
                }
                if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero.MapFaction)
                    || Helpers.IsRivalOfMinorFaction(Hero.MainHero.MapFaction, issueGiver.CurrentSettlement.OwnerClan))
                {
                    flag |= PreconditionFlags.AtWar;
                }
                return flag == PreconditionFlags.None;
            }

            public override bool IssueStayAliveConditions()
            {
                return true;
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {
            }

            protected override int CompanionSkillRewardXP
            {
                get => (int)(500f + 700f * base.IssueDifficultyMultiplier);
            }

            protected override void AlternativeSolutionEndWithSuccessConsequence()
            {
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, AlternativeSolutionPlayerHonorBonus)
                });
                base.IssueOwner.AddPower(AlternativeSolutionNotablePowerBonus);
                this.RelationshipChangeWithIssueOwner = AlternativeSolutionRelationBonus;
            }

            private Clan IssueClan()
            {
                return base.IssueOwner.CurrentSettlement.OwnerClan;
            }

            private const int IssueAndQuestDuration = 30;

            private const int AlternativeSolutionTroopTierRequirement = 2;

            private const int AlternativeSolutionRelationBonus = 5;

            private const int AlternativeSolutionNotablePowerBonus = 10;

            private const int AlternativeSolutionPlayerHonorBonus = 30;

            private const int AlternativeSolutionRewardPerRecruit = 100;

            private const int CompanionRequiredSkillLevel = 120;
        }

        public class MFHNotableNeedsRecruitsIssueQuest : QuestBase
        {
            protected override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));

                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
            }

            private Clan QuestClan()
            {
                return base.QuestGiver.CurrentSettlement.OwnerClan;
            }

            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestClan().MapFaction, Hero.MainHero.MapFaction))
                    base.CompleteQuestWithCancel();
            }

            private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestClan().MapFaction, Hero.MainHero.MapFaction))
                    if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility && !QuestClan().IsUnderMercenaryService)
                        CompleteQuestWithFail();
                    base.CompleteQuestWithCancel();
            }

            private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
            {
                if (victim == this.QuestGiver)
                {
                    base.CompleteQuestWithCancel();
                }
            }

            public override TextObject Title
            {
                get
                {
                    TextObject textObject = new TextObject("A {MINOR_FACTION} Notable needs recruits for {?ISSUE_GIVER.GENDER}her{?}his{\\?} Clan");
                    textObject.SetTextVariable("MINOR_FACTION", QuestClan().Name);
                    textObject.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject, false);
                    return textObject;
                }
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            private void setTextTroopDescriptions(TextObject t)
            {
                var questSettlement = base.QuestGiver.CurrentSettlement;
                if (questSettlement.OwnerClan.IsOutlaw)
                    t.SetTextVariable("TROOP_TYPE", "bandits");
                else
                    t.SetTextVariable("TROOP_TYPE", "soldiers");

                if (Helpers.mfIsMounted(questSettlement.OwnerClan))
                    t.SetTextVariable("MOUNTED", "mounted ");
                else
                    t.SetTextVariable("MOUNTED", "");
            }

            private TextObject QuestStartedLogText
            {
                get
                {
                    TextObject textObject = new TextObject("{QUEST_GIVER.LINK}, a Notable in {SETTLEMENT}, told you that {?QUEST_GIVER.GENDER}she{?}he{\\?} needs recruits for {?QUEST_GIVER.GENDER}her{?}his{\\?} Clan. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} asked you to recruit {NEEDED_RECRUIT_AMOUNT} {MOUNTED}{TROOP_TYPE} into your party, then transfer them to {?QUEST_GIVER.GENDER}her{?}him{\\?}. You will be paid for the recruits depending on their experience.");
                    setTextTroopDescriptions(textObject);

                    textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    textObject.SetTextVariable("SETTLEMENT", base.QuestGiver.CurrentSettlement.Name);
                    textObject.SetTextVariable("NEEDED_RECRUIT_AMOUNT", this._requestedRecruitCount);
                    return textObject;
                }
            }

            private TextObject QuestSuccessLog
            {
                get
                {
                    TextObject textObject = new TextObject("{=3ApJ6LaX}You have transferred the recruits to {QUEST_GIVER.LINK} as promised.");
                    textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return textObject;
                }
            }

            private TextObject QuestFailedWithTimeOutLogText
            {
                get
                {
                    TextObject textObject = new TextObject("{=iUmWTmQz}You have failed to deliver enough recruits in time. {QUEST_GIVER.LINK} must be disappointed.");
                    textObject.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return textObject;
                }
            }

            public MFHNotableNeedsRecruitsIssueQuest(string questId, Hero questGiver, CampaignTime duration, int requestedRecruitCount) : base(questId, questGiver, duration, 0)
            {
                this._requestedRecruitCount = requestedRecruitCount;
                this._deliveredRecruitCount = 0;
                this._rewardGold = 2000;
                this._playerReachedRequestedAmount = false;
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();
                base.AddTrackedObject(base.QuestGiver.CurrentSettlement);
                this._questProgressLogTest = base.AddDiscreteLog(this.QuestStartedLogText, new TextObject("{=r8rwl9ZS}Delivered Recruits"), this._deliveredRecruitCount, this._requestedRecruitCount);
            }

            protected override void SetDialogs()
            {
                this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start")
                    .NpcLine(new TextObject("{=0QuAZ8YO}I'll be waiting. Good luck.[if:convo_relaxed_happy][ib:confident]"))
                    .Condition(() => Hero.OneToOneConversationHero == this.QuestGiver)
                    .Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
                    .CloseDialog();

                TextObject npcDiscussLine = new TextObject("{=!}{MFHIDEOUT_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS}");
                TextObject npcResponseLine = new TextObject("{=!}{MFHIDEOUT_NEEDS_RECRUITS_QUEST_NOTABLE_RESPONSE}");
                bool changeDialogAfterTransfer = false;
                this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss")
                    .BeginNpcOptions()
                    .NpcOption(new TextObject("{=BGgDjRcW}I think that's enough. Here is your payment."), 
                        () => Hero.OneToOneConversationHero == this.QuestGiver && this._playerReachedRequestedAmount)
                        .Consequence( delegate {
                                    this.ApplyQuestSuccessConsequences();
                                    this.CompleteQuestWithSuccess(); })
                        .CloseDialog()
                    .NpcOption(npcDiscussLine, 
                        delegate () {
                            if (Hero.OneToOneConversationHero != this.QuestGiver)
                            {
                                return false;
                            }
                            if (!changeDialogAfterTransfer)
                            {
                                npcDiscussLine.SetTextVariable("MFHIDEOUT_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS", new TextObject("{=1hpeeCJD}Have you found any good men?[ib:confident3]"));
                                changeDialogAfterTransfer = true;
                            }
                            else
                            {
                                npcDiscussLine.SetTextVariable("MFHIDEOUT_NEEDS_RECRUITS_QUEST_NOTABLE_DISCUSS", new TextObject("{=ds294zxi}Anything else?"));
                                changeDialogAfterTransfer = false;
                            }
                            return true;
                        })
                    .BeginPlayerOptions()
                    .PlayerOption(new TextObject("{=QbaOoilS}Yes, I have brought you a few men."))
                        .Condition(() => (this.CheckIfThereIsSuitableRecruitInPlayer() && !this._playerReachedRequestedAmount) && changeDialogAfterTransfer)
                        .NpcLine(npcResponseLine)
                            .Condition(delegate {
                                        if (this._playerReachedRequestedAmount)
                                        {
                                            return false;
                                        }
                                        npcResponseLine.SetTextVariable("MFHIDEOUT_NEEDS_RECRUITS_QUEST_NOTABLE_RESPONSE", new TextObject("{=70LnOZzo}Very good. Keep searching. We still need more men.[ib:hip2]"));
                                        return true;
                                    })
                            .Consequence(new ConversationSentence.OnConsequenceDelegate(this.OpenRecruitDeliveryScreen))
                            .PlayerLine(new TextObject("{=IULW8h03}Sure."))
                            .Consequence(delegate {
                                            if (this._playerReachedRequestedAmount && Campaign.Current.ConversationManager.IsConversationInProgress)
                                            {
                                                Campaign.Current.ConversationManager.ContinueConversation();
                                            }
                                        })
                    .GotoDialogState("quest_discuss")
                    .PlayerOption(new TextObject("{=PZqGagXt}No, not yet. I'm still looking for them."))
                        .Condition(() => !this._playerReachedRequestedAmount & changeDialogAfterTransfer)
                        .Consequence(delegate {
                                changeDialogAfterTransfer = false;
                            })
                        .NpcLine(new TextObject("{=L1JyetPq}I am glad to hear that.[ib:closed2]"))
                        .CloseDialog()
                    .PlayerOption(new TextObject("{=OlOhuO7X}No thank you. Good day to you." ))
                        .Condition(() => !this._playerReachedRequestedAmount && !changeDialogAfterTransfer)
                        .CloseDialog()
                        .EndPlayerOptions()
                        .CloseDialog()
                        .EndNpcOptions();
            }

            
            private void OpenRecruitDeliveryScreen()
            {
                PartyScreenManager.OpenScreenWithCondition(
                    new IsTroopTransferableDelegate(this.IsTroopTransferable), 
                    new PartyPresentationDoneButtonConditionDelegate(this.DoneButtonCondition), 
                    new PartyPresentationDoneButtonDelegate(this.DoneClicked), 
                    null, 
                    PartyScreenLogic.TransferState.Transferable, 
                    PartyScreenLogic.TransferState.NotTransferable, 
                    base.QuestGiver.Name, 
                    this._requestedRecruitCount - this._deliveredRecruitCount, 
                    false, 
                    false, 
                    PartyScreenMode.TroopsManage);
            }

            private Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
            {
                if (this._requestedRecruitCount - this._deliveredRecruitCount < leftMemberRoster.TotalManCount)
                {
                    int num = this._requestedRecruitCount - this._deliveredRecruitCount;
                    TextObject textObject = new TextObject("{=VOr3uoRZ}You can only transfer {X} recruit{?IS_PLURAL}s{?}{\\?}.");
                    textObject.SetTextVariable("IS_PLURAL", (num > 1) ? 1 : 0);
                    textObject.SetTextVariable("X", num);
                    return new Tuple<bool, TextObject>(false, textObject);
                }
                return new Tuple<bool, TextObject>(true, null);
            }

            private bool DoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
            {
                foreach (TroopRosterElement troopRosterElement in leftMemberRoster.GetTroopRoster())
                {
                    this._rewardGold += this.RewardForEachRecruit(troopRosterElement.Character) * troopRosterElement.Number;
                    this._deliveredRecruitCount += troopRosterElement.Number;
                }
                this._questProgressLogTest.UpdateCurrentProgress(this._deliveredRecruitCount);
                this._questProgressLogTest.TaskName.SetTextVariable("TOTAL_REWARD", this._rewardGold);
                if (this._deliveredRecruitCount == this._requestedRecruitCount)
                {
                    this._playerReachedRequestedAmount = true;
                    if (Campaign.Current.ConversationManager.IsConversationInProgress)
                    {
                        Campaign.Current.ConversationManager.ContinueConversation();
                    }
                }
                return true;
            }

            private int RewardForEachRecruit(CharacterObject recruit)
            {
                return (int)(100f * ((recruit.Tier <= 1) ? 1f : ((recruit.Tier <= 3) ? 1.5f : 2f)));
            }

            private bool IsTroopUsable(CharacterObject character)
            {
                return (character.Occupation == Occupation.Bandit) == QuestClan().IsOutlaw
                    && (!Helpers.mfIsMounted(QuestClan()) || character.IsMounted);
            }

            private bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType troopType, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
            {
                bool questGiverIsOutlaw = QuestClan().IsOutlaw;
                return this._requestedRecruitCount - this._deliveredRecruitCount >= 0 
                    && (side == PartyScreenLogic.PartyRosterSide.Left 
                        || (MobileParty.MainParty.MemberRoster.Contains(character) && IsTroopUsable(character)));
            }

            private bool CheckIfThereIsSuitableRecruitInPlayer()
            {
                bool result = false;
                using (List<TroopRosterElement>.Enumerator enumerator = MobileParty.MainParty.MemberRoster.GetTroopRoster().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (IsTroopUsable(enumerator.Current.Character))
                        {
                            result = true;
                            break;
                        }
                    }
                }
                return result;
            }

            private void ApplyQuestSuccessConsequences()
            {
                base.AddLog(this.QuestSuccessLog, false);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, PlayerHonorBonusOnSuccess)
                });
                var mfHideout = Helpers.GetMFHideout(base.QuestGiver.CurrentSettlement);
                mfHideout.Hearth += QuestSettlementHearthBonusOnSuccess;
                mfHideout.Settlement.Militia += QuestSettlementMilitiaBonusOnSuccess;
                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, ClanRelationBonusOnSuccess);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this._rewardGold, false);
                base.QuestGiver.AddPower(QuestGiverNotablePowerBonusOnSuccess);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonusOnSuccess;
            }

            protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
            {
                if (this._deliveredRecruitCount >= this._requestedRecruitCount)
                {
                    completeWithSuccess = true;
                    this.ApplyQuestSuccessConsequences();
                }
            }

            protected override void OnTimedOut()
            {
                base.AddLog(this.QuestFailedWithTimeOutLogText, false);
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

            private const int ClanRelationBonusOnSuccess = 5;

            private const int QuestGiverNotablePowerBonusOnSuccess = 10;

            private const int QuestGiverRelationPenaltyOnFail = -5;

            private const int QuestSettlementHearthBonusOnSuccess = 20;

            private const int QuestSettlementMilitiaBonusOnSuccess = 2;

            private const int NotablePowerPenaltyOnFail = 0;

            private const int PlayerHonorBonusOnSuccess = 30;

            [SaveableField(1)]
            private int _requestedRecruitCount;

            [SaveableField(5)]
            private int _deliveredRecruitCount;

            [SaveableField(6)]
            private int _rewardGold;

            [SaveableField(9)]
            private bool _playerReachedRequestedAmount;

            [SaveableField(7)]
            private JournalLog _questProgressLogTest;
        }

        public class MFHNotableNeedsRecruitsIssueBehaviorTypeDefiner : SaveableTypeDefiner
        {
            public MFHNotableNeedsRecruitsIssueBehaviorTypeDefiner() : base(229_923_420)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(MFHNotableNeedsRecruitsIssue), 1);
                base.AddClassDefinition(typeof(MFHNotableNeedsRecruitsIssueQuest), 2);
            }
        }
    }
}

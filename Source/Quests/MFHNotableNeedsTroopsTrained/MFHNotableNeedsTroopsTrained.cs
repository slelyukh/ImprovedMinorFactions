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
using System.Linq;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using static ImprovedMinorFactions.IMFModels;

namespace ImprovedMinorFactions.Source.Quests.MFHNotableNeedsTroopsTrained
{
    // Token... comment means function has not yet been adapted to mod
    public class MFHNotableNeedsTroopsTrainedIssueBehavior : CampaignBehaviorBase
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
                && Helpers.GetMFHideout(issueGiver.CurrentSettlement)!.Hearth > 300;
        }

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnSelected), typeof(MFHNotableNeedsTroopsTrainedIssue), _IssueFrequency)
                    );
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(MFHNotableNeedsTroopsTrainedIssue), _IssueFrequency));
        }

        private static IssueBase OnSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            return new MFHNotableNeedsTroopsTrainedIssue(issueOwner);
        }

        private const IssueBase.IssueFrequency _IssueFrequency = IssueBase.IssueFrequency.Common;

        internal const MFRelation RelationLevelNeededForQuest = MFRelation.Neutral;

        public class MFHNotableNeedsTroopsTrainedIssue : IssueBase
        {
            public override AlternativeSolutionScaleFlag AlternativeSolutionScaleFlags
            {
                get => AlternativeSolutionScaleFlag.Casualties | AlternativeSolutionScaleFlag.FailureRisk;
            }

            private int BorrowedTroopCount
            {
                get => 3 + MathF.Ceiling(17f * base.IssueDifficultyMultiplier);
            }

            protected override bool IssueQuestCanBeDuplicated
            {
                get => false;
            }

            public override int AlternativeSolutionBaseNeededMenCount
            {
                get => 8 + MathF.Ceiling(19f * base.IssueDifficultyMultiplier);
            }

            protected override int AlternativeSolutionBaseDurationInDaysInternal
            {
                get => 9 + MathF.Ceiling(10f * base.IssueDifficultyMultiplier);
            }
            protected override int RewardGold
            {
                get => (int)(2000f + 4000f * base.IssueDifficultyMultiplier);
            }

            protected override int CompanionSkillRewardXP
            {
                get => (int)(500f + 700f * base.IssueDifficultyMultiplier);
            }


            public override TextObject Title
            {
                get => new TextObject("{=npV743inQ}Train recruits for {MINOR_FACTION} Hideout")
                        .SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public override TextObject Description
            {
                get
                {
                    TextObject text = new TextObject("{=5ORyuslE2}{ISSUE_GIVER.NAME} needs some of his {MINOR_FACTION} recruits to gain some real war experience " +
                        "{?ISSUE_GIVER.GENDER}She{?}He{\\?} wants you to take them with you on some fairly safe expeditions, such as hunting some bandits.")
                        .SetTextVariable("MINOR_FACTION", IssueClan().Name);
                    text.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject, false);
                    return text;
                }
            }
            
            public override TextObject IssueBriefByIssueGiver
            {
                get => new TextObject("{=oCbu6eG6F}We have recently acquired some new recruits for the {MINOR_FACTION}. They are young and eager " +
                        "to get into some combat but I'm afraid they won't last very long against real warriors. Maybe you could take some of them " +
                        "in your party and show them how to carry themselves in battle. In exchange they will share with you the values of the {MINOR_FACTION} " +
                        "and maybe we could form a partnership in the future.[if:convo_focused_happy][ib:hip]")
                        .SetTextVariable("MINOR_FACTION", IssueClan().Name);
            }

            public override TextObject IssueAcceptByPlayer
            {
                get => new TextObject("{=1REltXXz}Perhaps I can help.");
            }

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get => new TextObject("{=uLiRasv1}Maybe you could take them in your party for a while, until they get a bit of experience?[if:convo_thinking]");
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer
            {
                get => new TextObject("{=QxEPwLyp}I'll take your men into my party and show them a bit of the world.");
            }

            public override TextObject IssueAlternativeSolutionExplanationByIssueGiver
            {
                get => new TextObject("{=rutgr1VF}Or if you can assign a companion for a while, they can stay here and train the men... I will also give you some" +
                    " provisions and money for their expenses and your trouble.[if:convo_thinking]");
            }

            public override TextObject IssueAlternativeSolutionAcceptByPlayer
            {
                get => new TextObject("{=oT4JNyFp}I will assign one of my companions to train your men.");
            }

            public override TextObject IssueAlternativeSolutionResponseByIssueGiver
            {
                get => new TextObject("{=dE3vxfTo}Excellent.[if:convo_focused_happy] I'm sure they can learn a lot from your veterans.");
            }

            public override TextObject IssueDiscussAlternativeSolution
            {
                get => new TextObject("{=QRRgXOrN}As expected, your veterans have really sharpened up our boys. Please pass on my thanks to them, " +
                    "{?PLAYER.GENDER}madam{?}sir{\\?}.[if:convo_focused_happy][ib:hip]");
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
                    TextObject textObject = new TextObject("{=NNIqnPU40}{ISSUE_GIVER.LINK} a member of the {MINOR_FACTION}, asked you" +
                        " to train recruits for {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} gave you " +
                        "{NUMBER_OF_MEN} men, hoping to take them back when once they are veterans.{newline}You sent them with one " +
                        "of your companions {COMPANION.LINK} to hunt down some easy targets. You arranged to meet them in {RETURN_DAYS} days.")
                        .SetTextVariable("MINOR_FACTION", IssueClan().EncyclopediaLinkWithName)
                        .SetTextVariable("RETURN_DAYS", base.GetTotalAlternativeSolutionDurationInDays())
                        .SetTextVariable("NUMBER_OF_MEN", base.GetTotalAlternativeSolutionNeededMenCount());
                    textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
                    textObject.SetCharacterProperties("COMPANION", base.AlternativeSolutionHero.CharacterObject);
                    return textObject;
                }
            }

            public override TextObject IssueAlternativeSolutionSuccessLog
            {
                get
                {
                    TextObject textObject = new TextObject("{=AndfZYIJ}Your companion managed to return all of the troops {ISSUE_GIVER.LINK}" +
                        " gave you to train. {?ISSUE_GIVER.GENDER}She{?}He{\\?} sends you the following letter.\n\n“{?PLAYER.GENDER}Madam{?}Sir{\\?}," +
                        " Thank you for looking after my men. You honored our agreement, and you have my gratitude. Please accept this {GOLD}{GOLD_ICON}.")
                        .SetTextVariable("GOLD", this.RewardGold)
                        .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    textObject.SetCharacterProperties("ISSUE_GIVER", base.IssueOwner.CharacterObject);
                    return textObject;
                }
            }

            public MFHNotableNeedsTroopsTrainedIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(IssueDuration))
            {
            }
            // TODO: maybe affect hideout in some way with this issue?
            protected override float GetIssueEffectAmountInternal(IssueEffect issueEffect)
            {
                return 0f;
            }

            public override ValueTuple<SkillObject, int> GetAlternativeSolutionSkill(Hero hero)
            {
                return new ValueTuple<SkillObject, int>((hero.GetSkillValue(DefaultSkills.Steward) >= hero.GetSkillValue(DefaultSkills.Leadership)) ? DefaultSkills.Steward : DefaultSkills.Leadership, 120);
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

            protected override void AlternativeSolutionEndWithSuccessConsequence()
            {
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.IssueOwner, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, AlternativeSolutionPlayerHonorBonus)
                });
                base.IssueOwner.AddPower(AlternativeSolutionNotablePowerBonus);
                this.RelationshipChangeWithIssueOwner = AlternativeSolutionRelationBonus;
            }

            protected override void AlternativeSolutionEndWithFailureConsequence()
            {
                this.RelationshipChangeWithIssueOwner = -AlternativeSolutionRelationBonus;
                // TODO: add back when power means something
                //base.IssueOwner.AddPower(-AlternativeSolutionNotablePowerBonus);
            }
            public override bool IsTroopTypeNeededByAlternativeSolution(CharacterObject character)
            {
                return character.Tier >= AlternativeSolutionTroopTierRequirement;
            }

            public override IssueFrequency GetFrequency()
            {
                return _IssueFrequency;
            }
            public override bool IssueStayAliveConditions()
            {
                return true;
            }

            protected override void CompleteIssueWithTimedOutConsequences()
            {
            }

            protected override void OnGameLoad()
            {
            }

            protected override void HourlyTick()
            {
            }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new MFHNotableNeedsTroopsTrainedIssueQuest(questId, base.IssueOwner, CampaignTime.DaysFromNow(QuestDuration), base.IssueDifficultyMultiplier, this.RewardGold);
            }

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

                if (issueGiver!.GetRelationWithPlayer() < MinRelationNeeded(RelationLevelNeededForQuest))
                {
                    flag |= PreconditionFlags.Relation;
                    relationHero = issueGiver;
                }
                if (FactionManager.IsAtWarAgainstFaction(issueGiver.MapFaction, Hero.MainHero!.MapFaction)
                    || Helpers.IsRivalOfMinorFaction(Hero.MainHero.MapFaction, issueGiver.CurrentSettlement.OwnerClan))
                {
                    flag |= PreconditionFlags.AtWar;
                }
                if (MobileParty.MainParty.MemberRoster.TotalManCount + this.BorrowedTroopCount > PartyBase.MainParty.PartySizeLimit)
                {
                    flag |= PreconditionFlags.PartySizeLimit;
                }
                return flag == PreconditionFlags.None;
            }
            private Clan IssueClan()
            {
                return base.IssueOwner.CurrentSettlement.OwnerClan;
            }

            private const int IssueDuration = 30;

            private const int QuestDuration = 60;

            private const int AlternativeSolutionTroopTierRequirement = 2;

            private const int AlternativeSolutionRelationBonus = 5;

            private const int AlternativeSolutionNotablePowerBonus = 10;

            private const int AlternativeSolutionPlayerHonorBonus = 30;

            private const int AlternativeSolutionRewardPerRecruit = 100;

            private const int CompanionRequiredSkillLevel = 120;
        }

        public class MFHNotableNeedsTroopsTrainedIssueQuest : QuestBase
        {
            protected override void RegisterEvents()
            {
                CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
                CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
                CampaignEvents.OnTroopsDesertedEvent.AddNonSerializedListener(this, new Action<MobileParty, TroopRoster>(this.OnTroopsDeserted));
                CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, new Action<MapEvent>(this.OnPlayerBattleEnd));
                CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, new Action<CharacterObject, CharacterObject, int>(this.OnPlayerUpgradedTroops));
                CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
                CampaignEvents.OnTroopGivenToSettlementEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, TroopRoster>(this.OnTroopGivenToSettlement));
            }

            private void OnPlayerBattleEnd(MapEvent mapEvent)
            {
                if (mapEvent.IsPlayerMapEvent)
                    this.CheckFail();
            }

            private void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
            {
                if (mobileParty.IsMainParty)
                    this.CheckFail();
            }

            private void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
            {
                if (upgradeFromTroop == this._questGivenChar && upgradeToTroop == this._questTargetChar && number > 0)
                {
                    this._upgradedTroopsCount = MathF.Min(_upgradedTroopsCount + number, _borrowedTroopCount);
                    base.UpdateQuestTaskStage(this._playerStartsQuestLog, this._upgradedTroopsCount);
                }
                if (!this.CheckFail())
                {
                    this.CheckSuccess(false);
                }
            }

            private void OnSettlementLeft(MobileParty party, Settlement settlement)
            {
                if (!this.CheckFail())
                {
                    this.CheckSuccess(false);
                }
            }

            private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
            {
                if (FactionManager.IsAtWarAgainstFaction(QuestClan().MapFaction, Hero.MainHero.MapFaction))
                {
                    if (detail == DeclareWarAction.DeclareWarDetail.CausedByPlayerHostility)
                    {
                        CompleteQuestWithFail(_onPlayerDeclaredWarQuestLogText);
                    } else
                    {
                        base.CompleteQuestWithCancel(_onQuestGiverAtWarWithPlayerLogText);
                    }
                }
                    
            }

            private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
            {
                if (victim == this.QuestGiver)
                {
                    base.CompleteQuestWithCancel(_cancelQuestGiverDied);
                }
            }

            private void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
            {
                if (giverHero == Hero.MainHero && !this.CheckFail())
                {
                    this.CheckSuccess(false);
                }
            }

            protected override void HourlyTick()
            {
                if (base.IsOngoing && !this.CheckFail())
                {
                    this.CheckSuccess(false);
                }
            }
            
            private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
            {
                if (QuestClan().MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
                    base.CompleteQuestWithCancel(this._onQuestGiverAtWarWithPlayerLogText);
            }

            private bool CheckFail()
            {
                if (PartyBase.MainParty.MemberRoster.GetTroopCount(this._questTargetChar) == 0
                    && PartyBase.MainParty.MemberRoster.GetTroopCount(this._questGivenChar) == 0)
                {
                    this.Fail();
                    return true;
                }
                return false;
            }

            private void CheckSuccess(bool isConversationEnded = false)
            {
                if ((PartyBase.MainParty.MemberRoster.GetTroopCount(this._questGivenChar) == 0 || this._upgradedTroopsCount == this._borrowedTroopCount) 
                    && !this._popUpOpened && (Campaign.Current.ConversationManager.OneToOneConversationHero == null || isConversationEnded))
                {
                    this.OpenDecisionPopUp();
                }
            }

            private void RemoveBorrowedTroopsFromParty(PartyBase party)
            {
                int basicToRemove = MathF.Min(party.MemberRoster.GetTroopCount(this._questGivenChar), this._borrowedTroopCount - _upgradedTroopsCount);
                int upgradedToRemove = MathF.Min(party.MemberRoster.GetTroopCount(this._questTargetChar), _upgradedTroopsCount);
                if (basicToRemove > 0)
                    party.MemberRoster.AddToCounts(this._questGivenChar, -basicToRemove);
                if (upgradedToRemove > 0)
                    party.MemberRoster.AddToCounts(this._questTargetChar, -upgradedToRemove);
                
            }

            private void OpenDecisionPopUp()
            {
                this._popUpOpened = true;
                this._campaignTimeControlModeCacheForDecisionPopUp = Campaign.Current.TimeControlMode;
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                TextObject text = new TextObject("{=LO7EjoY7}The borrowed troops remaining in your party are now all experienced. You can send them back to {QUEST_GIVER.LINK}.");
                text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                InformationManager.ShowInquiry(new InquiryData("", text.ToString(), true, true, 
                    new TextObject("{=ikDX1Fd7}Send the troops back").ToString(), 
                    new TextObject("{=yFahppU2}Hold on to them").ToString(), 
                    new Action(this.CompleteQuestSuccessfully), 
                    new Action(this.EpicFail)));
            }

            private void CompleteQuestSuccessfully()
            {
                Campaign.Current.TimeControlMode = this._campaignTimeControlModeCacheForDecisionPopUp;

                var memberRoster = PartyBase.MainParty.MemberRoster;
                int upgradedTroopCount = MathF.Min(memberRoster.GetTroopCount(this._questTargetChar), _upgradedTroopsCount);
                memberRoster.AddToCounts(this._questTargetChar, -upgradedTroopCount);
                int basicTroopCount = MathF.Min(memberRoster.GetTroopCount(this._questGivenChar), this._borrowedTroopCount - _upgradedTroopsCount);
                memberRoster.AddToCounts(this._questGivenChar, -basicTroopCount);
                if (upgradedTroopCount >= this._borrowedTroopCount)
                {
                    this.TotalSuccess();
                    return;
                }
                if (this._borrowedTroopCount * 0.5f < upgradedTroopCount && upgradedTroopCount < this._borrowedTroopCount)
                {
                    this.PartialSuccess();
                    return;
                }
                if (0 < upgradedTroopCount && upgradedTroopCount <= this._borrowedTroopCount * 0.5f)
                {
                    this.WeakSuccess();
                }
            }
            private void TotalSuccess()
            {
                Clan.PlayerClan.AddRenown(PlayerRenownBonusOnSuccess);
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationBonusOnSuccess;
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, PlayerHonorBonusOnSuccess)
                });
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold);
                base.QuestGiver.AddPower(QuestGiverPowerBonusOnSuccess);
                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, ClanRelationBonusOnSuccess);
                Helpers.GetMFHideout(base.QuestGiver.CurrentSettlement)!.UpgradeMilitia(MilitiaToUpgradeOnSuccess);
                base.AddLog(this._totalSuccessLog);
                base.CompleteQuestWithSuccess();
            }

            private void PartialSuccess()
            {
                Clan.PlayerClan.AddRenown(PlayerRenownBonusOnSuccess * 0.5f);
                this.RelationshipChangeWithQuestGiver = (int) (QuestGiverRelationBonusOnSuccess * 0.6);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, (int) (PlayerHonorBonusOnSuccess*0.6))
                });
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold);
                base.QuestGiver.AddPower(QuestGiverPowerBonusOnSuccess);
                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, ClanRelationBonusOnSuccess);
                Helpers.GetMFHideout(base.QuestGiver.CurrentSettlement)!.UpgradeMilitia((int) (MilitiaToUpgradeOnSuccess * 0.6));
                base.AddLog(this._partialSuccessLog);
                base.CompleteQuestWithSuccess();
            }

            private void WeakSuccess()
            {
                Clan.PlayerClan.AddRenown(PlayerRenownBonusOnSuccess * 0.5f);
                this.RelationshipChangeWithQuestGiver = (int)(QuestGiverRelationBonusOnSuccess * 0.2);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, (int) (PlayerHonorBonusOnSuccess * 0.2))
                });
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.RewardGold);
                base.QuestGiver.AddPower(QuestGiverPowerBonusOnSuccess);
                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, (int) (ClanRelationBonusOnSuccess * 0.6));
                Helpers.GetMFHideout(base.QuestGiver.CurrentSettlement)!.UpgradeMilitia((int)(MilitiaToUpgradeOnSuccess * 0.2));
                base.AddLog(this._partialSuccessLog);
                base.CompleteQuestWithSuccess();
            }

            private void Fail()
            {
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenaltyOnFail;
                base.QuestGiver.AddPower(QuestGiverPowerPenaltyOnFailure);
                base.AddLog(this._failLog);
                base.CompleteQuestWithFail();
            }

            private void EpicFail()
            {
                Campaign.Current.TimeControlMode = this._campaignTimeControlModeCacheForDecisionPopUp;
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenaltyOnFail * 2;
                ChangeRelationAction.ApplyPlayerRelation(QuestClan().Leader, QuestGiverRelationPenaltyOnFail * 2);
                base.QuestGiver.AddPower(QuestGiverPowerPenaltyOnFailure * 2);
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Honor, (int) -(PlayerHonorBonusOnSuccess * 0.2))
                });
                base.AddLog(this._epicFailLog);
                base.CompleteQuestWithFail();
            }

            protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
            {
                if (PartyBase.MainParty.MemberRoster.GetTroopCount(this._questTargetChar) > 0)
                {
                    doNotResolveTheQuest = true;
                    if (!this._popUpOpened && MobileParty.MainParty.MapEvent == null)
                    {
                        this.OpenDecisionPopUp();
                    }
                }
            }

            protected override void OnTimedOut()
            {
                this.RelationshipChangeWithQuestGiver = QuestGiverRelationPenaltyOnFail;
                base.QuestGiver.AddPower(QuestGiverPowerPenaltyOnFailure);
                this.RemoveBorrowedTroopsFromParty(PartyBase.MainParty);
                base.AddLog(this._timeoutLog);
            }


            private int _borrowedTroopCount
            {
                get => 3 + MathF.Ceiling(17f * this._difficultyMultiplier);
            }

            public override TextObject Title
            {
                get
                {
                    TextObject text = new TextObject("{=q2aed7tv}Train troops for {ISSUE_OWNER.NAME}");
                    text.SetCharacterProperties("ISSUE_OWNER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _questStartLog
            {
                get
                {
                    TextObject text = new TextObject("{=1QEr8eYap}{QUEST_GIVER.LINK}, a member of the {MINOR_FACTION}, asked you to train some recruits" +
                        " for {?QUEST_GIVER.GENDER}her{?}him{\\?}. {?QUEST_GIVER.GENDER}She{?}He{\\?} gave you {NUMBER_OF_MEN} men, hoping to take them back " +
                        "when once they have some experience.\nThe easiest way to train them without putting them in too much danger is to attack weak parties.")
                        .SetTextVariable("NUMBER_OF_MEN", this._borrowedTroopCount)
                        .SetTextVariable("MINOR_FACTION", QuestClan().EncyclopediaLinkWithName);
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _totalSuccessLog
            {
                get
                {
                    TextObject text = new TextObject("{=4RNREbPW}You managed to return all of the troops {QUEST_GIVER.LINK} gave you to train. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter.{newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for " +
                        "looking after my men. You honored our agreement, and you have my gratitude. Please accept this {GOLD}{GOLD_ICON}.”")
                        .SetTextVariable("GOLD", this.RewardGold)
                        .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _partialSuccessLog
            {
                get
                {
                    TextObject text = new TextObject("{=yjAHh66a}You managed to return more than half of the troops {QUEST_GIVER.LINK} gave you to train. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, Thank you for returning " +
                        "my men to me. The losses they suffered are somewhat higher than I thought. I can only hope you did what you could to honor our agreement " +
                        "and try to keep them alive.”");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _weakSuccessLog
            {
                get
                {
                    TextObject text = new TextObject("{=NXs7kr2B}You managed to return a fraction of the troops {QUEST_GIVER.LINK} gave you to train. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. {newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, " +
                        "Thank you for returning my men to me. The losses they suffered are somewhat higher than I thought. I can only hope you did what" +
                        " you could do to honor our agreement and try to keep them alive.”");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _failLog
            {
                get
                {
                    TextObject text = new TextObject("{=YBEB7GLa}All the borrowed troops in your party are gone. You are unable to return " +
                        "any of the troops {QUEST_GIVER.LINK} gave you to train. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. " +
                        "{newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, I understand that all my men are dead. I asked you to try and keep them alive. " +
                        "I do not know what to say to their kinfolk. This is a breach of my trust.”");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _epicFailLog
            {
                get
                {
                    TextObject text = new TextObject("{=eSpRuda1}You have decided to keep the borrowed troops {QUEST_GIVER.LINK} gave you to train. " +
                        "When {?QUEST_GIVER.GENDER}She{?}He{\\?} hears about this {?QUEST_GIVER.GENDER}she{?}he{\\?} sends you the following letter. " +
                        "{newline}“{?PLAYER.GENDER}Madam{?}Sir{\\?}, I made it clear that I expected my men to be returned to me. I consider this a betrayal of my trust.”");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _onQuestGiverAtWarWithPlayerLogText
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

            private TextObject _cancelQuestGiverDied
            {
                get
                {
                    TextObject text = new TextObject("{=gOEIZ30vl}{QUEST_GIVER.LINK} has died. {?QUEST_GIVER.GENDER}She{?}He{\\?} has no more desires.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            private TextObject _onPlayerDeclaredWarQuestLogText
            {
                get
                {
                    TextObject text = new TextObject("{=bqeWVVEE}Your actions have started a war with {QUEST_GIVER.LINK}'s faction. " +
                        "{?QUEST_GIVER.GENDER}She{?}He{\\?} cancels your agreement and the quest is a failure.");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

			private TextObject _timeoutLog
            {
                get
                {
                    TextObject text = new TextObject("{=txtsL6QQ}You failed to train the troops by the time {QUEST_GIVER.LINK} needed " +
                        "them back. {?QUEST_GIVER.GENDER}She{?}He{\\?} sends you the following letter. “{?PLAYER.GENDER}Madam{?}Sir{\\?}, " +
                        "I expected my men to be returned to me. I consider this a breach of our agreement.”");
                    text.SetCharacterProperties("QUEST_GIVER", base.QuestGiver.CharacterObject);
                    return text;
                }
            }

            public override bool IsRemainingTimeHidden
            {
                get => false;
            }

            public MFHNotableNeedsTroopsTrainedIssueQuest(string questId, Hero giverHero, CampaignTime duration, float difficultyMultiplier, int rewardGold) : base(questId, giverHero, duration, rewardGold)
            {
                this._difficultyMultiplier = difficultyMultiplier;
                this.SetDialogs();
                base.InitializeQuestOnCreation();
            }

            private void QuestAcceptedConsequences()
            {
                base.StartQuest();
                this._questGivenChar = Helpers.GetBasicTroop(QuestClan());
                this._questGivenChar.SetTransferableInPartyScreen(false);
                this._questTargetChar = _questGivenChar.UpgradeTargets[0];
                this._questTargetChar.SetTransferableInPartyScreen(false);
                PartyBase.MainParty.AddElementToMemberRoster(this._questGivenChar, this._borrowedTroopCount);
                this._upgradedTroopsCount = 0;

                PartyBase.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 3);
                this._playerStartsQuestLog = base.AddDiscreteLog(
                    this._questStartLog,
                    new TextObject("{=wUb5h4a3}Trained Troops"),
                    this._upgradedTroopsCount,
                    this._borrowedTroopCount);
            }

            

            protected override void SetDialogs()
            {
                this.OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100)
                    .NpcLine(new TextObject("{=J8qFgwal}Excellent. I'll tell the lads to join your party.[if:convo_relaxed_happy][ib:confident2]"))
                    .Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
                    .Consequence(new ConversationSentence.OnConsequenceDelegate(this.QuestAcceptedConsequences))
                    .NpcLineWithVariation("{=7lee0h29}One thing - if one or two die, that's the fortunes of war, things could go even worse if we get raided " +
                    "and have no one who can fight back... But try not to get them all massacred. These men will take some risks for me, but not have their " +
                    "lives thrown away to no purpose.[if:convo_stern]")
                    .Variation("{=EaPQ2mm7}One thing - if possible, try not to get them all killed, will you? Green troops aren't much use to me, but corpses are even less.[if:convo_stern]", 
                        new object[]{"UngratefulTag", 1, "MercyTag", -1})
                    .CloseDialog();
                this.DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100)
                    .NpcLine(new TextObject("{=r9F1W4KZ}Yes? Have you been able to train my men?[if:convo_astonished]"))
                    .Condition(() => CharacterObject.OneToOneConversationCharacter == base.QuestGiver.CharacterObject)
                        .BeginPlayerOptions()
                            .PlayerOption(new TextObject("{=PVO3YFSq}Yes we are heading out now."))
                                .NpcLine(new TextObject("{=weW40mKG}Good to hear that! Safe journeys.[if:convo_relaxed_happy]"))
                                .CloseDialog()
                            .PlayerOption(new TextObject("{=wErSpkjy}I'm still working on it."))
                                .NpcLine(new TextObject("{=weW40mKG}Good to hear that! Safe journeys.[if:convo_relaxed_happy]"))
                                .CloseDialog()
                        .EndPlayerOptions()
                    .CloseDialog();
            }
            protected override void InitializeQuestOnGameLoad()
            {
                this._questGivenChar = Helpers.GetBasicTroop(QuestClan());
                this._questGivenChar.SetTransferableInPartyScreen(false);
                this._questTargetChar = _questGivenChar.UpgradeTargets[0];
                this._questTargetChar.SetTransferableInPartyScreen(false);
                if (this._playerStartsQuestLog == null)
                {
                    this._playerStartsQuestLog = Enumerable.First<JournalLog>(base.JournalEntries);
                    base.UpdateQuestTaskStage(this._playerStartsQuestLog, this._upgradedTroopsCount);
                }
                this.SetDialogs();
            }

            private Clan QuestClan()
            {
                return base.QuestGiver.CurrentSettlement.OwnerClan;
            }

            private bool _popUpOpened;

            private CharacterObject? _questGivenChar;

            private CharacterObject? _questTargetChar;

            private const int PlayerHonorBonusOnSuccess = 50;

            private const float PlayerRenownBonusOnSuccess = 2;

            private const int QuestGiverRelationBonusOnSuccess = 5;

            private const int MilitiaToUpgradeOnSuccess = 5;

            private const int QuestGiverPowerBonusOnSuccess = 10;

            private const int QuestGiverPowerPenaltyOnFailure = 0;

            private const int QuestGiverRelationPenaltyOnFail = -5;

            private const int ClanRelationBonusOnSuccess = 5;

            private const int NotablePowerPenaltyOnFail = 0;

            [SaveableField(1)]
            private readonly float _difficultyMultiplier;

            private CampaignTimeControlMode _campaignTimeControlModeCacheForDecisionPopUp;

            [SaveableField(2)]
            private JournalLog? _playerStartsQuestLog;

            [SaveableField(3)]
            private int _upgradedTroopsCount;
        }

        public class MFHNotableNeedsTroopsTrainedIssueBehaviorTypeDefiner : SaveableTypeDefiner
        {
            public MFHNotableNeedsTroopsTrainedIssueBehaviorTypeDefiner() : base(112_953_420)
            {
            }

            protected override void DefineClassTypes()
            {
                base.AddClassDefinition(typeof(MFHNotableNeedsTroopsTrainedIssue), 1);
                base.AddClassDefinition(typeof(MFHNotableNeedsTroopsTrainedIssueQuest), 2);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.Patches
{
    // TODO: move sumwhere else
    public class MFNotableDebugIssue : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnCheckForIssue));
        }

        public void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                //Campaign.Current.IssueManager.AddPotentialIssueData(hero,
                //    new PotentialIssueData(null, 
                //    typeof(GangLeaderNeedsRecruitsIssueBehavior.GangLeaderNeedsRecruitsIssue),
                //    IssueBase.IssueFrequency.VeryCommon, null));
                return;
            }
            Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(typeof(GangLeaderNeedsRecruitsIssueBehavior.GangLeaderNeedsRecruitsIssue), IssueBase.IssueFrequency.VeryCommon));
        }


        private static bool ConditionsHold(Hero issueGiver)
        {
            return Helpers.isMFHideout(issueGiver.CurrentSettlement) && issueGiver.IsGangLeader;
        }

        public override void SyncData(IDataStore dataStore)
        {
           
        }
    }

    // this should allow mfHideout notables to have quests
    //[HarmonyPatch(typeof(IssuesCampaignBehavior), "OnSessionLaunched")]
    //public class NeedsSpecialWeaponsPatch
    //{
    //    static void Postfix(IssuesCampaignBehavior __instance, ref Settlement[]  ____settlements)
    //    {
    //        var newSettlementsList = ____settlements.ToList();
    //        newSettlementsList.AppendList(
    //            Enumerable.ToList(Enumerable.Where<Settlement>(
    //                Settlement.All, 
    //                (Settlement x) => Helpers.isMFHideout(x))));
    //        Helpers.setPrivateField(__instance, "_settlements", newSettlementsList.ToArray());
    //    }
    //}


    // TODO: give more reason for needing weapons
    // lots to patch maybe I should use other quests
    //[HarmonyPatch(typeof(GangLeaderNeedsSpecialWeaponsIssueBehavior), "ConditionsHold")]
    //public class NeedsSpecialWeaponsPatch
    //{
    //    static void Postfix(ref bool __result, Hero IssueOwner)
    //    {
    //        if (__result == true || !Helpers.isMFHideout(IssueOwner.CurrentSettlement))
    //            return;
    //        __result = IssueOwner.IsGangLeader;
    //    }
    //}
}

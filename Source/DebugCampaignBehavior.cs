using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem;

namespace ImprovedMinorFactions.Source
{
    internal class DebugCampaignBehavior : CampaignBehaviorBase
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
}

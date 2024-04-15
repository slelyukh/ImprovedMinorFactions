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
                //    typeof(MFHNotableNeedsRecruitsIssueBehavior.MFHNotableNeedsRecruitsIssue),
                //    IssueBase.IssueFrequency.VeryCommon, null));
                return;
            }
        }


        private static bool ConditionsHold(Hero issueGiver)
        {
            return Helpers.isMFHideout(issueGiver.CurrentSettlement) && issueGiver.IsNotable;
        }

        public override void SyncData(IDataStore dataStore)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    internal class PlayAsMFBehavior : CampaignBehaviorBase
    {
        public PlayAsMFBehavior()
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, new Action(this.OnCharacterCreationIsOver));
        }

        private void OnCharacterCreationIsOver()
        {
            foreach (Settlement s in Settlement.All)
            {
                var mfh = Helpers.GetMFHideout(s);
                if (mfh != null && mfh.OwnerClan == Clan.PlayerClan && mfh.IsActive)
                {
                    mfh.PlayerSpotHideout();
                }

            }
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    internal class NomadMFsCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement> (OnDailySettlementTick));
        }
        public void OnDailySettlementTick(Settlement s)
        {
            var mfHideout = Helpers.GetMFHideout(s);
            if (mfHideout == null 
                || !mfHideout.OwnerClan.IsNomad 
                || MFHideoutManager.Current.IsFullHideoutOccupationMF(mfHideout.OwnerClan))
            {
                return;
            }
            if (mfHideout.ActivationTime == CampaignTime.Zero)
            {
                mfHideout.ActivationTime = CampaignTime.Now;
            }
            // if its time to move, move
            if (mfHideout.IsActive && (mfHideout.ActivationTime + MFHideoutModels.NomadHideoutLifetime).IsPast)
                MFHideoutManager.Current.ClearHideout(mfHideout, DeactivationReason.NomadMovement);

            // if anyone is in settlement kick them out?
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}

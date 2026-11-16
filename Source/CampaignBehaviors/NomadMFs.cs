using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    /// <summary>
    /// Moves nomad camps to another camp of the same clan once they have been in one place long enough.
    /// </summary>
    internal class NomadMFsCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement> (OnDailySettlementTick));
        }
        /// <summary>
        /// Moves a nomad camp once its time in one place is up. Clans with no spare camp stay put.
        /// </summary>
        public void OnDailySettlementTick(Settlement s)
        {
            var mfHideout = Helpers.GetMFHideout(s);
            if (mfHideout == null 
                || !mfHideout.IsNomad 
                || IMFManager.Current!.IsFullHideoutOccupationMF(mfHideout.OwnerClan))
            {
                return;
            }
            if (mfHideout.ActivationTime == CampaignTime.Zero)
            {
                mfHideout.ActivationTime = CampaignTime.Now;
            }
            // if its time to move, move
            if (mfHideout.IsActive && (mfHideout.ActivationTime + IMFModels.NomadHideoutLifetime).IsPast)
                IMFManager.Current.ClearHideout(mfHideout, DeactivationReason.NomadMovement);

            // if anyone is in settlement kick them out?
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}

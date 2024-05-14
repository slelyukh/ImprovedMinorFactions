﻿using System;
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
            // if its time to move, move
            // add activation time to hideout, use it here to determine if its time to move
            // if anyone is in settlement kick them out.
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}

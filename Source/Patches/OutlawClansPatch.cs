using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Declares war between the Forest People and Sturgia once per campaign.
    /// </summary>
    // Vanilla misses the Forest People's war with Sturgia, so declare it here.
    internal class MFOutlawWarCampaignBehavior : CampaignBehaviorBase
    {
        private bool _outlawWarFixApplied;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnGameLoaded));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_outlawWarFixApplied", ref _outlawWarFixApplied);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            MakeOutlawFactionsEnemyToKingdomFactions();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            MakeOutlawFactionsEnemyToKingdomFactions();
        }

        private void MakeOutlawFactionsEnemyToKingdomFactions()
        {
            if (_outlawWarFixApplied)
                return;
            _outlawWarFixApplied = true;

            foreach (Clan clan in Clan.All)
            {
                if (clan.IsMinorFaction && clan.Culture.StringId == "vakken")
                {
                    foreach (Kingdom kingdom in Kingdom.All)
                    {
                        if (kingdom.Culture.StringId == "sturgia")
                        {
                            FactionManager.DeclareWar(kingdom, clan);
                        }
                    }
                }
            }
        }
    }
}

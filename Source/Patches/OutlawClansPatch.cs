using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Source.Patches
{
    // fix Bannerlord bug that causes Forest People clan to not be at war with Sturgia due to their Vakken culture
    [HarmonyPatch(typeof(OutlawClansCampaignBehavior), "MakeOutlawFactionsEnemyToKingdomFactions")]
    public class OutlawClansPatch
    {
        static void Postfix()
        {
            foreach (Clan clan in Clan.All)
            {
                if (clan.IsMinorFaction && clan.Culture.GetCultureCode() == CultureCode.Vakken)
                {
                    foreach (Kingdom kingdom in Kingdom.All)
                    {
                        if (kingdom.Culture.GetCultureCode() == CultureCode.Sturgia)
                        {
                            FactionManager.DeclareWar(kingdom, clan);
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Patches
{
    // Nasty Nasty recursive Harmony patch that moves the HomeSettlement of MF lords after their hideout gets raided
    [HarmonyPatch(typeof(Clan), "set_HomeSettlement")]
    public class ClanSetHomeSettlementPatch
    {
        static void Postfix(Clan __instance)
        {
            if (!__instance.IsMinorFaction || Helpers.IsMFHideout(__instance.HomeSettlement) || !IMFManager.Current.HasActiveHideouts(__instance))
                return;

            var activeHideouts = IMFManager.Current.GetActiveHideoutsOfClan(__instance);
            Helpers.CallPrivateMethod(__instance, "set_HomeSettlement", new object[] { activeHideouts[MBRandom.RandomInt(activeHideouts.Count)].Settlement });
            foreach (Hero hero in __instance.Heroes)
            {
                hero.UpdateHomeSettlement();
            }
        }
    }
}

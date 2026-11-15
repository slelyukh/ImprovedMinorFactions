using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // When every hideout of a bandit culture became a minor faction camp, vanilla looks the culture up and throws.
    [HarmonyPatch(typeof(BanditSpawnCampaignBehavior), "GetInfestedHideoutCount")]
    internal static class BanditSpawnPatch
    {
        static bool Prefix(Clan banditFaction, Dictionary<CultureObject, List<Hideout>>? ____hideouts, ref int __result)
        {
            __result = 0;
            if (banditFaction?.Culture == null || ____hideouts == null
                || !____hideouts.TryGetValue(banditFaction.Culture, out List<Hideout>? hideouts) || hideouts == null)
                return false;
            foreach (Hideout hideout in hideouts)
            {
                if (hideout != null && hideout.IsInfested && hideout.MapFaction == banditFaction)
                    __result++;
            }
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.Patches
{
    internal class ClanPatch
    {
        /// <summary>
        /// A clan always has 100 relation with itself, which matters when the player's clan is the minor faction.
        /// </summary>
        [HarmonyPatch(typeof(Clan), "GetRelationWithClan")]
        public class ClanGetRelationWithClanPatch
        {
            // If player clan IS the minor faction, then they have 100 relation with each other.
            static void Postfix(ref int __result, Clan __instance, Clan other)
            {
                if (__instance == other)
                {
                    __result = 100;
                }
            }
        }
    }
}

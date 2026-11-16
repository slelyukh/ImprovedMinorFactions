using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Camp notables aren't gang leaders to the game, which expects gang leaders to live in towns.
    /// </summary>
    // patch to prevent crashes from assumption about Gang Leaders living in Towns. Use Helpers.IsMFGangLeader instead.
    [HarmonyPatch(typeof(Hero), "get_IsGangLeader")]
    public class HeroIsGangLeaderPatch
    {
        static void Postfix(Hero __instance, ref bool __result)
        {
            if (__result == true && Helpers.IsMFHideout(__instance.CurrentSettlement))
                __result = false;
        }
    }

    /// <summary>
    /// Makes camp notables count as notables.
    /// </summary>
    [HarmonyPatch(typeof(Hero), "get_IsNotable")]
    public class HeroIsNotablePatch
    {
        static void Postfix(Hero __instance, ref bool __result)
        {
            if (__result == false && Helpers.IsMFNotable(__instance))
                __result = true;
        }
    }

    /// <summary>
    /// Ignores setting a trait that doesn't exist instead of crashing.
    /// </summary>
    [HarmonyPatch(typeof(Hero), "SetTraitLevel")]
    public class HeroSetTraitLevelPatch
    {
        static bool Prefix(TraitObject trait)
        {
            if (trait == null)
                return false;
            return true;
        }
    }

    /// <summary>
    /// Gives the player 100 relation with notables of their own minor faction.
    /// </summary>
    [HarmonyPatch(typeof(Hero), "GetRelation")]
    public class HeroGetRelationPatch
    {
        // If player clan IS the minor faction then the notables have 100 relation with them
        static void Postfix(ref int __result, Hero __instance, Hero otherHero)
        {
            if (otherHero != Hero.MainHero && __instance != Hero.MainHero) return;
            // else someone is the main hero
            if (otherHero == __instance) __result = 100;
            if (otherHero == Hero.MainHero && Helpers.IsMFNotable(__instance) &&  __instance?.CurrentSettlement?.OwnerClan == Clan.PlayerClan)
            {
                __result = 100;
            }
            if (__instance == Hero.MainHero && Helpers.IsMFNotable(otherHero) && otherHero?.CurrentSettlement?.OwnerClan == Clan.PlayerClan)
            {
                __result = 100;
            }
        }
    }

    /// <summary>
    /// Gives the player 100 relation with heroes of their own minor faction.
    /// </summary>
    [HarmonyPatch(typeof(Hero), "GetRelationWithPlayer")]
    public class HeroGetRelationWithPlayerPatch
    {
        // If player clan IS the minor faction, then they have 100 relation with each other.
        static void Postfix(ref float __result, Hero __instance)
        {
            if (Helpers.IsMFNotable(__instance) && __instance.Clan == Clan.PlayerClan)
            {
                __result = 100;
            }
        }
    }
}

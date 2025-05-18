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

    [HarmonyPatch(typeof(Hero), "get_IsNotable")]
    public class HeroIsNotablePatch
    {
        static void Postfix(Hero __instance, ref bool __result)
        {
            if (__result == false && Helpers.IsMFNotable(__instance))
                __result = true;
        }
    }

    // TODO: remove debug
    /*[HarmonyPatch(typeof(GangLeaderNeedsToOffloadStolenGoodsIssueBehavior), "ConditionsHold")]
    public class debugConditionsHoldPatch
    {
        static void Prefix(Hero issueGiver)
        {
            var occupation = issueGiver.Occupation == Occupation.Preacher ? "Preacher" : "Gang Leader";
            if (Helpers.IsMFNotable(issueGiver))
            {
                //InformationManager.DisplayMessage(new InformationMessage("Checking GangLeaderNeedsToOffloadStolenGoods for :"));
                InformationManager.DisplayMessage(
                    new InformationMessage(issueGiver.Name + " Is gang leader: " + issueGiver.IsGangLeader + " Is notable: " + issueGiver.IsNotable + " occupation: " + occupation)
                    );
                if (issueGiver.IsGangLeader)
                    InformationManager.DisplayMessage(new InformationMessage("ERROR BROKEN NOTABLE", Color.Black));
            }
        }
    }*/

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
}

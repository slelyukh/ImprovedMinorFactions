using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.Patches
{

    // TODO: give more reason for needing weapons
    // lots to patch maybe I should use other quests
    //[HarmonyPatch(typeof(MFHNotableNeedsSpecialWeaponsIssueBehavior), "ConditionsHold")]
    //public class NeedsSpecialWeaponsPatch
    //{
    //    static void Postfix(ref bool __result, Hero IssueOwner)
    //    {
    //        if (__result == true || !Helpers.isMFHideout(IssueOwner.CurrentSettlement))
    //            return;
    //        __result = IssueOwner.IsMFHNotable;
    //    }
    //}
}

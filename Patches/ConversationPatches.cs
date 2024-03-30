using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(HideoutConversationsCampaignBehavior), "bandit_hideout_start_defender_on_condition")]
    public class BanditHideoutStartDefenderOnConditionPatch
    {
        static void Postfix(ref bool __result)
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (!(encounteredParty?.IsSettlement ?? false)
                || encounteredParty.Settlement == null
                || __result == true
                || !Helpers.isMFHideout(encounteredParty.Settlement))
            {
                return;
            }
            var test = PlayerEncounter.Current;
            // TODO: if I make this not a hideout battle I need a new if statement
            __result = encounteredParty.Settlement.OwnerClan.IsMinorFaction && PlayerEncounter.Battle?.IsHideoutBattle == true;
        }
    }

    [HarmonyPatch(typeof(GuardsCampaignBehavior), "conversation_guard_start_on_condition")]
    public class GuardStartOnConditionPatch
    {
        static void Postfix(ref bool __result)
        {
            if (__result == false)
                return;
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (!(encounteredParty?.IsSettlement ?? false)
                || encounteredParty.Settlement == null
                || !Helpers.isMFHideout(encounteredParty.Settlement))
            {
                return;
            }
            __result = !encounteredParty.Settlement.OwnerClan.IsMinorFaction;
        }
    }
}

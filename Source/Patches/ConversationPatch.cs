using HarmonyLib;
using TaleWorlds.CampaignSystem;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Encounters;

namespace ImprovedMinorFactions.Patches
{
    // makes sure MFHideout boss fights have correct dialog
    [HarmonyPatch(typeof(HideoutConversationsCampaignBehavior), "bandit_hideout_start_defender_on_condition")]
    public class BanditHideoutStartDefenderOnConditionPatch
    {
        static void Postfix(ref bool __result)
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            Hero encounteredHero = Hero.OneToOneConversationHero;
            if (!(encounteredParty?.IsSettlement ?? false)
                || encounteredParty.Settlement == null
                || __result == true
                || !Helpers.IsMFHideout(encounteredParty.Settlement)
                || (encounteredHero?.IsNotable ?? false))
            {
                return;
            }
            __result = encounteredParty.Settlement.OwnerClan.IsMinorFaction && PlayerEncounter.Battle?.IsHideoutBattle == true;
        }
    }

    // prevents MFHideout boss fight from using castle guard dialog
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
                || !Helpers.IsMFHideout(encounteredParty.Settlement))
            {
                return;
            }
            __result = !encounteredParty.Settlement.OwnerClan.IsMinorFaction;
        }
    }
}

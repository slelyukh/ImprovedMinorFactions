using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using SandBox.CampaignBehaviors;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Uses the bandit boss dialog for the leader in a camp raid.
    /// </summary>
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

    /// <summary>
    /// Keeps the castle guard dialog out of camp raids.
    /// </summary>
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

    /// <summary>
    /// Starts talking to a camp notable from the menu overlay in the camp's own location.
    /// </summary>
    // Start from the hideout's own location, the location complex can't find it.
    [HarmonyPatch(typeof(GameMenuOverlay), "ExecuteTroopAction")]
    public class MFHideoutNotableTalkPatch
    {
        static bool Prefix(object o, GameMenuOverlay __instance)
        {
            int action = (int)o;
            // MenuOverlayContextList: Conversation is 1, QuickConversation is 2
            if (action != 1 && action != 2)
            {
                return true;
            }

            var contextMenuItemField = AccessTools.Field(typeof(GameMenuOverlay), "_contextMenuItem");
            var contextMenuItem = contextMenuItemField?.GetValue(__instance) as GameMenuPartyItemVM;

            if (contextMenuItem?.Character?.HeroObject == null)
            {
                return true;
            }

            Hero hero = contextMenuItem.Character.HeroObject;
            bool isMFHideout = Helpers.IsMFHideout(hero.CurrentSettlement) || (Settlement.CurrentSettlement != null && Helpers.IsMFHideout(Settlement.CurrentSettlement));

            if (!isMFHideout)
            {
                return true;
            }

            LocationComplex locationComplex = LocationComplex.Current;
            if (locationComplex == null)
            {
                IMFDiagnostics.Verbose($"MFHideoutNotableTalkPatch: LocationComplex.Current is null for {hero.Name}");
                return true;
            }

            Location mfHideoutCenter = locationComplex.GetLocationWithId("mf_hideout_center");
            if (mfHideoutCenter == null)
            {
                IMFDiagnostics.Verbose($"MFHideoutNotableTalkPatch: mf_hideout_center location not found for {hero.Name}");
                return true;
            }

            if (action == 1)
            {
                CampaignEventDispatcher.Instance.OnPlayerStartTalkFromMenu(hero);
                PlayerEncounter.LocationEncounter?.CreateAndOpenMissionController(mfHideoutCenter, null, contextMenuItem.Character);
            }
            else
            {
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: false, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: true),
                    new ConversationCharacterData(contextMenuItem.Character, null, noHorse: false, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: true));
            }
            return false;
        }
    }
}

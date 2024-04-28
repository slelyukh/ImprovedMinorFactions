using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    internal class IMFEncounterGameMenuModel : EncounterGameMenuModel
    {
        EncounterGameMenuModel _previousModel;
        public IMFEncounterGameMenuModel(EncounterGameMenuModel previousModel)
        {
            _previousModel = previousModel;
        }

        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            string result = _previousModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);

            if (result == null)
            {
                PartyBase? encounteredParty = Helpers.callPrivateMethod(null, 
                    "GetEncounteredPartyBase", 
                    new object[] { attackerParty, defenderParty }, 
                    typeof(DefaultEncounterGameMenuModel)) as PartyBase;
                if (encounteredParty.IsSettlement && Helpers.isMFHideout(encounteredParty.Settlement))
                    result = "mf_hideout_place";
            }

            return result;
        }

        public override string GetGenericStateMenu()
        {
            string result = _previousModel.GetGenericStateMenu();

            if (result == null)
            {
                MobileParty mainParty = MobileParty.MainParty;
                Settlement curSettlement = mainParty.CurrentSettlement;
                if (PlayerEncounter.Current?.IsPlayerWaiting == true)
                    result = "mf_hideout_wait";
                else if (mainParty.AttachedTo == null && mainParty.CurrentSettlement != null && Helpers.isMFHideout(curSettlement))
                    result = "mf_hideout_place";
            }

            return result;
        }

        public override string GetNewPartyJoinMenu(MobileParty newParty)
        {
            return _previousModel.GetNewPartyJoinMenu(newParty);
        }

        public override string GetRaidCompleteMenu()
        {
            return _previousModel.GetRaidCompleteMenu();
        }
    }

    // makes sure correct menus appear when visiting MFHideout
    //[HarmonyPatch(typeof(DefaultEncounterGameMenuModel), "GetGenericStateMenu")]
    //public class DefaultEncounterGameMenuModelStateMenuPatch
    //{
    //    static void Postfix(ref string __result)
    //    {
    //        if (__result != null)
    //            return;

    //        MobileParty mainParty = MobileParty.MainParty;
    //        Settlement currentSettlement = mainParty.CurrentSettlement;
    //        if (PlayerEncounter.Current?.IsPlayerWaiting == true)
    //            __result = "mf_hideout_wait";
    //        else if (mainParty.AttachedTo == null && mainParty.CurrentSettlement != null && Helpers.isMFHideout(currentSettlement))
    //            __result = "mf_hideout_place";
    //    }
    //}

    //[HarmonyPatch(typeof(DefaultEncounterGameMenuModel), "GetEncounterMenu")]
    //public class DefaultEncounterGameMenuModelEncounterMenuPatch
    //{
    //    static void Postfix(DefaultEncounterGameMenuModel __instance, ref string __result, PartyBase attackerParty, PartyBase defenderParty)
    //    {
    //        if (__result != null)
    //            return;
    //        PartyBase encounteredPartyBase = GetEncounteredPartyBaseCopy(attackerParty, defenderParty);
    //        if (encounteredPartyBase.IsSettlement && Helpers.isMFHideout(encounteredPartyBase.Settlement))
    //            __result = "mf_hideout_place";
    //    }

    //    // copypasta
    //    static PartyBase GetEncounteredPartyBaseCopy(PartyBase attackerParty, PartyBase defenderParty)
    //    {
    //        if (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty)
    //        {
    //            if (attackerParty != PartyBase.MainParty)
    //                return attackerParty;
    //            return defenderParty;
    //        }
    //        else
    //        {
    //            if (defenderParty.MapEvent == null)
    //                return attackerParty;
    //            return defenderParty;
    //        }
    //    }
    //}
}

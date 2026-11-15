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

        public override string? GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            string result = _previousModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);

            if (string.IsNullOrEmpty(result))
            {
                PartyBase? encounteredParty = global::Helpers.MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty);
                if (encounteredParty == null)
                    encounteredParty = (defenderParty != null && defenderParty.IsSettlement) ? defenderParty : attackerParty;
                if (encounteredParty != null && encounteredParty.IsSettlement && Helpers.IsMFHideout(encounteredParty.Settlement))
                    result = "mf_hideout_place";
            }

            return result;
        }

        public override string GetGenericStateMenu()
        {
            string result = _previousModel.GetGenericStateMenu();

            if (string.IsNullOrEmpty(result))
            {
                MobileParty mainParty = MobileParty.MainParty;
                Settlement? curSettlement = mainParty?.CurrentSettlement;
                if (PlayerEncounter.Current?.IsPlayerWaiting == true)
                    result = "mf_hideout_wait";
                else if (mainParty?.AttachedTo == null && curSettlement != null && Helpers.IsMFHideout(curSettlement))
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

        public override bool IsPlunderMenu(string menuId)
        {
            return _previousModel.IsPlunderMenu(menuId);
        }
    }
}

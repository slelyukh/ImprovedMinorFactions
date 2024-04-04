using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions
{
    internal static class MFHideoutModels
    {

        // TODO: make a real calculation
        public static float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            return 0.05f;
        }

        public static int GetMaxMilitiaInHideout()
        {
            return 40;
        }

        // TODO: maybe increase hearths upon certain actions such as attacking a party for bandits, etc
        public static ExplainedNumber GetHearthChange(Settlement settlement, bool includeDescriptions = false)
        {
            var mfHideout = settlement.SettlementComponent as MinorFactionHideout;
            var eNum = new ExplainedNumber(0f, includeDescriptions, null);
            eNum.Add((mfHideout.Hearth < 300f) ? 0.6f : ((mfHideout.Hearth < 600f) ? 0.4f : 0.2f), BaseText);
            return eNum;

        }

        public static ExplainedNumber GetMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var eNum = new ExplainedNumber(0f, includeDescriptions);
            eNum.Add(0.2f, BaseText);
            eNum.Add((settlement.SettlementComponent as MinorFactionHideout).Hearth * 0.0005f, FromHearthsText);
            return eNum;
        }

        // TODO: monitor minor faction finances
        public static float CalculateHideoutIncome(MinorFactionHideout mfHideout)
        {
            return mfHideout.Hearth * 1.5f;
        }

        // copied from bandit hideouts
        public static int GetPlayerMaximumTroopCountForRaidMission(MobileParty party)
        {
            float num = 10f;
            if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics, false))
            {
                num += DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
            }
            return TaleWorlds.Library.MathF.Round(num);
        }

        // Not doing anything with notable power atm
        public static ExplainedNumber CalculateDailyNotablePowerChange(Hero notable, bool includeDescriptions = false)
        {
            ExplainedNumber eNum = new ExplainedNumber(0f, includeDescriptions, null);
            return eNum;
        }

        internal static int MinRelationToBeMFHFriend = 20;

        private static readonly TextObject BaseText = new TextObject("{=militarybase}Base", null);
        private static readonly TextObject RetiredText = new TextObject("{=gHnfFi1s}Retired", null);
        private static readonly TextObject FromHearthsText = new TextObject("{=ecdZglky}From Hearths", null);
    }

    // No limit for Minor Faction Hideouts :)
    [HarmonyPatch(typeof(DefaultBanditDensityModel), "get_NumberOfMaximumTroopCountForFirstFightInHideout")]
    public class DefaultMaxTroopsFirstHideoutBattlePatch
    {
        static void Postfix(ref int __result)
        {
            if (Settlement.CurrentSettlement == null || !Helpers.isMFHideout(Settlement.CurrentSettlement))
                return;
            __result = 150;
        }
    }
}

using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions
{
    internal static class IMFModels
    {
        public static float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            float num = 0.7f * (Helpers.GetMFHideout(settlement).Hearth / 400);
            return 0.75f * MathF.Clamp(MathF.Pow(num, (float)(index + 1)), 0f, 1f);
        }

        // TODO: maybe increase hearths upon certain actions such as attacking a party for bandits, etc
        public static ExplainedNumber GetHearthChange(Settlement settlement, bool includeDescriptions = false)
        {
            var mfHideout = Helpers.GetMFHideout(settlement);
            var eNum = new ExplainedNumber(0f, includeDescriptions, null);
            eNum.Add((mfHideout.Hearth < 300f) ? 0.6f : ((mfHideout.Hearth < 600f) ? 0.4f : 0.2f), BaseText);
            return eNum;

        }

        public static ExplainedNumber GetMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var eNum = new ExplainedNumber(0f, includeDescriptions);
            eNum.Add(0.05f, BaseText);
            eNum.Add((Helpers.GetMFHideout(settlement)).Hearth * 0.0005f, FromHearthsText);
            return eNum;
        }

        // TODO: monitor minor faction finances
        public static float CalculateHideoutIncome(MinorFactionHideout mfHideout)
        {
            return (mfHideout.Hearth - 300) * 2.5f;
        }

        // copied from bandit hideouts
        public static int GetPlayerMaximumTroopCountForRaidMission(MobileParty party)
        {
            float num = 10f;
            if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics, false))
            {
                num += DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
            }
            return MathF.Round(num);
        }

        // Not doing anything with notable power atm
        public static ExplainedNumber CalculateDailyNotablePowerChange(Hero notable, bool includeDescriptions = false)
        {
            ExplainedNumber eNum = new ExplainedNumber(0f, includeDescriptions, null);
            return eNum;
        }

        // TODO: maybe add nomad transfer delay?
        internal static CampaignTime HideoutActivationDelay(Clan ownerClan)
        {
            if (Helpers.IsSingleHideoutMF(ownerClan))
            {
                return CampaignTime.Days(12);
            } else
            {
                return CampaignTime.Days(5);
            }
        }

        public static int DefaultNumActiveHideouts(Clan c)
        {
            if (c.StringId == "templar")
                return 2;
            else
                return 1;
        }

        public static int DefaultNumMilitiaFirstTime (Clan c)
        {
            if (c.IsNomad)
                return 60;
            else
                return 25;
        }

        public static int DefaultNumMilitiaPostRaid (Clan c)
        {
            if (c.IsNomad)
                return 30;
            else
                return 18;
        }

        public static int DefaultNumLvl3Militia (Clan c)
        {
            if (c.IsNomad)
                return 10;
            else
                return 3;
        }

        public static int DefaultNumLvl2Militia (Clan c)
        {
            if (c.IsNomad)
                return 10;
            else
                return 5;
        }

        public static int DefaultMaxMilitia(Clan c)
        {
            if (c.IsNomad)
                return 95;
            else
                return 40;
        }
        public enum Gender
        {
            Male,
            Female,
            Any
        }


        internal static CampaignTime NomadHideoutLifetime = CampaignTime.Days(45);

        internal static int MinRelationToBeMFHFriend = 15;
        internal static float MinRelationToGetMFQuest = -30;

        private static readonly TextObject BaseText = new TextObject("{=militarybase}Base");
        private static readonly TextObject RetiredText = new TextObject("{=gHnfFi1s}Retired");
        private static readonly TextObject FromHearthsText = new TextObject("{=ecdZglky}From Hearths");

        public static float MinimumMFHHearthToAffectVillage = 300f;
    }

    
}

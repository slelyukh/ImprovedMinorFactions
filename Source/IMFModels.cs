using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions
{
    internal static class IMFModels
    {
        public static float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            float num = 0.7f * (Helpers.GetMFHideout(settlement)!.Hearth / 400);
            return 0.75f * MathF.Clamp(MathF.Pow(num, (float)(index + 1)), 0f, 1f);
        }

        // TODO: maybe increase hearths upon certain actions such as attacking a party for bandits, etc
        public static ExplainedNumber GetHearthChange(Settlement settlement, bool includeDescriptions = false)
        {
            var mfHideout = Helpers.GetMFHideout(settlement);
            var eNum = new ExplainedNumber(0f, includeDescriptions, null);
            eNum.Add((mfHideout!.Hearth < 300f) ? 0.6f : ((mfHideout.Hearth < 600f) ? 0.4f : 0.2f), BaseText);
            return eNum;

        }

        public static ExplainedNumber GetMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var eNum = new ExplainedNumber(0f, includeDescriptions);
            if (settlement.OwnerClan == null)
                return eNum;

            if (settlement.OwnerClan.IsNomad)
               eNum.Add(0.25f, BaseText);
            else
                eNum.Add(0.05f, BaseText);

            eNum.Add((Helpers.GetMFHideout(settlement)!).Hearth * 0.0005f, FromHearthsText);
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

        public static int NumActiveHideouts(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumActiveHideouts;

            // default values
            return 1;
        }

        public static bool UpgradeMilitiaRandom(Clan c)
        {
            if (c.IsNomad)
                return MBRandom.RandomInt(4) == 1;
            else
                return MBRandom.RandomInt(8) == 1;
        }

        public static int NumMilitiaFirstTime(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumMilitiaFirstTime;

            // default values
            if (c.IsNomad)
                return 65;
            else
                return 28;
        }

        public static int NumMilitiaPostRaid(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumMilitiaPostRaid;

            // default values
            if (c.IsNomad)
                return 40;
            else
                return 20;
        }

        public static int NumLvl3Militia(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumLvl3Militia;

            // default values
            if (c.IsNomad)
                return 10;
            else
                return 3;
        }

        public static int NumLvl2Militia(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumLvl2Militia;

            // default values
            if (c.IsNomad)
                return 15;
            else
                return 5;
        }

        public static int MaxMilitia(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.MaxMilitia;

            // default values
            if (c.IsNomad)
                return 120;
            else
                return 40;
        }

        public static Gender ClanGender(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.ClanGender;

            // default value
            return Gender.Any;
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

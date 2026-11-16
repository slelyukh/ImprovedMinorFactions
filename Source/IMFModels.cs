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
using static ImprovedMinorFactions.IMFModels;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// The numbers behind camps: volunteers, hearth and militia growth, income, activation delays, militia sizes and relation tiers.
    /// Most militia numbers can be overridden per faction in mf_data.xml.
    /// </summary>
    internal static class IMFModels
    {
        /// <summary>
        /// Chance per day that a camp notable gets a new volunteer in a slot. Higher hearth means more volunteers, later slots fill less often.
        /// </summary>
        /// <param name="index">the volunteer slot</param>
        public static float GetDailyVolunteerProductionProbability(Hero hero, int index, MinorFactionHideout mfh)
        {
            float num = 0.7f * (mfh.Hearth / 400);
            return 0.75f * MathF.Clamp(MathF.Pow(num, (float)(index + 1)), 0f, 1f);
        }

        /// <summary>
        /// Daily hearth growth of a camp, slower as the hearth gets higher.
        /// </summary>
        // TODO: maybe increase hearths upon certain actions such as attacking a party for bandits, etc
        public static ExplainedNumber GetHearthChange(MinorFactionHideout mfh, bool includeDescriptions = false)
        {
            var eNum = new ExplainedNumber(0f, includeDescriptions, null);
            eNum.Add((mfh.Hearth < 300f) ? 0.6f : ((mfh.Hearth < 600f) ? 0.4f : 0.2f), BaseText);
            return eNum;

        }

        /// <summary>
        /// Daily militia growth of a camp. Nomad camps grow faster, and more hearth adds more.
        /// </summary>
        public static ExplainedNumber GetMilitiaChange(MinorFactionHideout mfh, bool includeDescriptions = false)
        {
            var eNum = new ExplainedNumber(0f, includeDescriptions);
            if (mfh.OwnerClan == null)
                return eNum;

            if (mfh.OwnerClan.IsNomad)
               eNum.Add(0.25f, BaseText);
            else
                eNum.Add(0.05f, BaseText);

            eNum.Add(mfh.Hearth * 0.0005f, FromHearthsText);
            return eNum;
        }

        private const float MaxHideoutIncome = 750f;

        /// <summary>
        /// Daily gold a camp gives its clan, from hearth above 300 up to 750.
        /// </summary>
        // TODO: monitor minor faction finances
        public static float CalculateHideoutIncome(MinorFactionHideout mfHideout)
        {
            return MathF.Clamp((mfHideout.Hearth - 300) * 2.5f, 0f, MaxHideoutIncome);
        }

        /// <summary>
        /// Extra daily gold for a minor faction outside a kingdom, landless clans earn less than their parties cost.
        /// The poorer the clan the more it gets, the same way the kingdom budget tops up clans. Mercenaries and the player get none.
        /// </summary>
        public static int CalculateCampSupport(Clan clan)
        {
            if (clan.Kingdom != null || clan.IsUnderMercenaryService || clan.Leader == Hero.MainHero)
                return 0;
            int gold = clan.Gold;
            return gold < 5000 ? 2000 : gold < 10000 ? 1500 : gold < 20000 ? 1000 : gold < 30000 ? 500 : 0;
        }

        /// <summary>
        /// How many troops the player can take into a camp raid: 10, plus the Small Unit Tactics bonus.
        /// </summary>
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

        /// <summary>
        /// How long a raided clan waits before its next camp opens: 12 days if it has no spare camp, else 5.
        /// </summary>
        // TODO: maybe add nomad transfer delay?
        internal static CampaignTime HideoutActivationDelay(Clan ownerClan)
        {
            if (Helpers.IsSingleHideoutMF(ownerClan))
            {
                return CampaignTime.Days(12);
            }
            else
            {
                return CampaignTime.Days(5);
            }
        }

        /// <summary>
        /// How many camps a clan keeps active, 1 if it has no data.
        /// </summary>
        public static int NumActiveHideouts(Clan c)
        {
            MFData? mfData = IMFManager.Current?.GetClanMFData(c);
            if (mfData != null)
                return mfData.NumActiveHideouts;

            // default values
            return 1;
        }

        /// <summary>
        /// Daily roll for a random militia upgrade, 1 in 4 for nomads and 1 in 8 for others.
        /// </summary>
        public static bool UpgradeMilitiaRandom(Clan c)
        {
            if (c.IsNomad)
                return MBRandom.RandomInt(4) == 1;
            else
                return MBRandom.RandomInt(8) == 1;
        }

        /// <summary>
        /// Militia a camp starts with the first time it opens. Bigger for nomads and higher tier clans.
        /// </summary>
        public static int NumMilitiaFirstTime(Clan c)
        {
            var config = ConfigLoader.For(c?.StringId);
            if (config?.NumMilitiaFirstTime.HasValue == true)
                return config.NumMilitiaFirstTime.Value;

            if (c != null && c.IsNomad)
                return MBRandom.RandomInt(85, 116) + (c.Tier * 6);
            else
                return MBRandom.RandomInt(25, 41) + (c != null ? c.Tier * 2 : 0);
        }

        /// <summary>
        /// The lowest militia an active camp is allowed to drop to, also used after a raid.
        /// </summary>
        public static int NumMilitiaPostRaid(Clan c)
        {
            var config = ConfigLoader.For(c?.StringId);
            if (config?.NumMilitiaPostRaid.HasValue == true)
                return config.NumMilitiaPostRaid.Value;

            if (c != null && c.IsNomad)
                return 60 + (c.Tier * 3);
            else
                return 15 + (c != null ? c.Tier : 0);
        }

        /// <summary>
        /// How many militia get upgraded twice when a camp opens.
        /// </summary>
        public static int NumLvl3Militia(Clan c)
        {
            var config = ConfigLoader.For(c?.StringId);
            if (config?.NumLvl3Militia.HasValue == true)
                return config.NumLvl3Militia.Value;

            if (c != null && c.IsNomad)
                return 10 + (c.Tier * 2);
            else
                return 3 + (c != null ? c.Tier : 0);
        }

        /// <summary>
        /// How many militia get upgraded once when a camp opens.
        /// </summary>
        public static int NumLvl2Militia(Clan c)
        {
            var config = ConfigLoader.For(c?.StringId);
            if (config?.NumLvl2Militia.HasValue == true)
                return config.NumLvl2Militia.Value;

            if (c != null && c.IsNomad)
                return 15 + (c.Tier * 3);
            else
                return 5 + (c != null ? c.Tier : 0);
        }

        /// <summary>
        /// Militia cap of a camp. Grows with clan tier and hearth, nomads have a much higher cap.
        /// </summary>
        public static int MaxMilitia(Clan c, float hearth = 200f)
        {
            var config = ConfigLoader.For(c?.StringId);
            if (config?.MaxMilitia.HasValue == true)
                return config.MaxMilitia.Value;

            if (c != null && c.IsNomad)
                return 140 + (c.Tier * 15) + (int)(hearth * 0.10f);
            else
                return 45 + ((c?.Tier ?? 0) * 5) + (int)(hearth * 0.05f);
        }

        /// <summary>
        /// Which gender a clan's heroes and notables are made as, Any if not set.
        /// </summary>
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

        /// <summary>
        /// Turns a relation number into a relation tier.
        /// </summary>
        public static MFRelation GetRelationLevel(int relation)
        {
            switch (relation)
            {
                case >= 100:
                    return MFRelation.Tier6;
                case >= 80:
                    return MFRelation.Tier5;
                case >= 60:
                    return MFRelation.Tier4;
                case >= 40:
                    return MFRelation.Tier3;
                case >= 25:
                    return MFRelation.Tier2;
                case >= 15:
                    return MFRelation.Tier1;
                case >= -30:
                    return MFRelation.Neutral;
                default:
                    return MFRelation.Enemy;
            }
        }

        /// <summary>
        /// The lowest relation number that counts as the given tier.
        /// </summary>
        public static int MinRelationNeeded(MFRelation minRelationship)
        {
            switch (minRelationship)
            {
                case MFRelation.Tier6:
                    return 100;
                case MFRelation.Tier5:
                    return 80;
                case MFRelation.Tier4:
                    return 60;
                case MFRelation.Tier3:
                    return 40;
                case MFRelation.Tier2:
                    return 25;
                case MFRelation.Tier1:
                    return 15;
                case MFRelation.Neutral:
                    return -30;
                case MFRelation.Enemy:
                    return -100;
                default:
                    return -100;
            }
        }

        public enum MFRelation
        {
            Tier6,
            Tier5,
            Tier4,
            Tier3,
            Tier2,
            Tier1,
            Neutral,
            Enemy,
        }

        internal static CampaignTime NomadHideoutLifetime = CampaignTime.Days(45);

        internal static int MinRelationToBeMFHFriend = 15;
        //internal static float MinRelationToGetMFQuest = -30;

        private static readonly TextObject BaseText = new TextObject("{=militarybase}Base");
        private static readonly TextObject RetiredText = new TextObject("{=gHnfFi1s}Retired");
        private static readonly TextObject FromHearthsText = new TextObject("{=ecdZglky}From Hearths");

        public static float MinimumMFHHearthToAffectVillage = 300f;
    }

    
}

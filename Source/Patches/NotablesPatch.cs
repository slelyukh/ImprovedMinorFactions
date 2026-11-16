using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using static ImprovedMinorFactions.IMFModels;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Keeps camp notables from leaving for having too little power.
    /// </summary>
    // prevents MFNotables from disappearing for having too little Power
    [HarmonyPatch(typeof(NotablesCampaignBehavior), "CheckAndMakeNotableDisappear")]
    public class CheckAndMakeNotableDisappearPatch
    {
        static bool Prefix(Hero notable)
        {
            if (!Helpers.IsMFHideout(notable.CurrentSettlement))
                return true;
            return false;
        }
    }

    /// <summary>
    /// Skips the game's dead notable handling for camp notables, MFHNotablesCampaignBehavior replaces them instead.
    /// </summary>
    // method prevents default OnHeroKilled from running with a similar version running in MFHNotablesCampaignBehavior instead
    [HarmonyPatch(typeof(NotablesCampaignBehavior), "OnHeroKilled")]
    public class OnHeroKilledPatch
    {
        static bool Prefix(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (!Helpers.IsMFHideout(victim.CurrentSettlement))
                return true;
            return false;
        }
    }

    /// <summary>
    /// Skips the daily power loss for camp notables.
    /// </summary>
    // prevent usual Power degradation for MFHideout Notables
    [HarmonyPatch(typeof(NotablePowerManagementBehavior), "DailyTickHero")]
    public class DailyTickHeroPatch
    {
        static bool Prefix(Hero hero)
        {
            if (!hero.IsNotable || !Helpers.IsMFHideout(hero.CurrentSettlement))
                return true;
            hero.AddPower(IMFModels.CalculateDailyNotablePowerChange(hero).ResultNumber);
            return false;
        }
    }

    /// <summary>
    /// Creates notables for camps from the camp culture's templates, picking gender by the clan's settings
    /// and fixing names for cultures whose templates have the wrong ones.
    /// </summary>
    [HarmonyBefore(new string[] {"BannerKings"})]
    [HarmonyPatch(typeof(HeroCreator), "CreateNotable")]
    public class CreateHeroAtOccupationPatch
    {
        public static bool Prefix(Occupation occupation, Settlement settlement, ref Hero __result)
        {
            if (!Helpers.IsMFHideout(settlement))
                return true;

            var gender = IMFModels.ClanGender(settlement.OwnerClan);
            Settlement forcedHomeSettlement = settlement ?? SettlementHelper.GetRandomTown(null);
            IEnumerable<CharacterObject> enumerable;
                

            // NOT COPY/PASTED
            if (gender == IMFModels.Gender.Male)
                enumerable = Enumerable.Where<CharacterObject>(forcedHomeSettlement.Culture.NotableTemplates, (CharacterObject x) => x.Occupation == occupation && !x.IsFemale);
            else if (gender == IMFModels.Gender.Female)
                enumerable = Enumerable.Where<CharacterObject>(forcedHomeSettlement.Culture.NotableTemplates, (CharacterObject x) => x.Occupation == occupation && x.IsFemale);
            else
            {
                // Separate pools so it stays around 20% female no matter how many templates there are.
                var allPreachers = Enumerable.Where<CharacterObject>(forcedHomeSettlement.Culture.NotableTemplates, (CharacterObject x) => x.Occupation == occupation).ToList();
                if (!allPreachers.Any())
                {
                    // no Preacher template, use empire's instead (it always has 3 preachers)
                    var fallbackCulture = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CultureObject>("empire");
                    if (fallbackCulture != null)
                        allPreachers = Enumerable.Where<CharacterObject>(fallbackCulture.NotableTemplates, (CharacterObject x) => x.Occupation == occupation).ToList();
                }
                if (!allPreachers.Any())
                {
                    __result = null;
                    return true;
                }
                var malePool = allPreachers.Where(x => !x.IsFemale).ToList();
                var femalePool = allPreachers.Where(x => x.IsFemale).ToList();
                bool pickFemale = femalePool.Any() && settlement.RandomIntWithSeed((uint)(settlement.Notables.Count + 100), 1, 100) <= 20;
                enumerable = pickFemale && femalePool.Any() ? femalePool : (malePool.Any() ? malePool : allPreachers);
            }
            // based on the base game version, changed to work for camps

            if (!Enumerable.Any(enumerable))
            {
                __result = null;
                return true;
            }
                

            int num = 0;
            foreach (CharacterObject characterObject in enumerable)
            {
                int num2 = characterObject.GetTraitLevel(DefaultTraits.Frequency) * 10;
                num += ((num2 > 0) ? num2 : 100);
            }

            CharacterObject? template = null;
            int num3 = settlement.RandomIntWithSeed((uint)settlement.Notables.Count, 1, num);
            foreach (CharacterObject characterObject2 in enumerable)
            {
                int num4 = characterObject2.GetTraitLevel(DefaultTraits.Frequency) * 10;
                num3 -= ((num4 > 0) ? num4 : 100);
                if (num3 < 0)
                {
                    template = characterObject2;
                    break;
                }
            }

            Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, -1);
            CultureObject hideoutCulture = forcedHomeSettlement!.Culture;

            // Give Darshi, Nord, and Vakken MFs correct notable names (their templates have incorrect names)
            // NOT copy/pasted
            if (Helpers.IsMinorCulture(hideoutCulture))
            {
                var nameList = new List<TextObject>();
                if (hero.IsFemale)
                    nameList = hideoutCulture.FemaleNameList;
                else
                    nameList = hideoutCulture.MaleNameList;

                if (nameList != null && nameList.Count > 0)
                {
                    TextObject firstName = nameList.GetRandomElement();
                    TextObject fullName = (TextObject)Helpers.CallPrivateMethod(NameGenerator.Current, "GenerateHeroFullName", new object[] { hero, firstName, true })!;
                    hero.SetName(fullName, firstName);
                }
            }
            // NOT copy/pasted

            if (hero.HomeSettlement.IsVillage && hero.HomeSettlement.Village.Bound != null && hero.HomeSettlement.Village.Bound.IsCastle)
            {
                float value = MBRandom.RandomFloat * 20f;
                hero.AddPower(value);
            }
            if (occupation != Occupation.Wanderer)
            {
                hero.ChangeState(Hero.CharacterStates.Active);
                EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
                int amount = 10000;
                GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);
            }
            CharacterObject template2 = hero.Template;

            if (settlement.OwnerClan != null && settlement.OwnerClan == Clan.PlayerClan)
                hero.SupporterOf = Clan.PlayerClan;
            else if (template2?.HeroObject != null && hero?.Template?.HeroObject?.Clan != null && hero.Template.HeroObject.Clan.IsMinorFaction)
                hero.SupporterOf = hero.Template.HeroObject.Clan;
            else
                hero.SupporterOf = HeroHelper.GetRandomClanForNotable(hero);

            if (occupation != Occupation.Wanderer)
                Helpers.CallPrivateMethod(null, "AddRandomVarianceToTraits", new object[] { hero }, typeof(HeroCreator));

            __result = hero;
            return false;
        }
    }
}

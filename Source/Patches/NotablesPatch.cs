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

    // mostly copypasta
    [HarmonyBefore(new string[] {"BannerKings"})]
    [HarmonyPatch(typeof(HeroCreator), "CreateHeroAtOccupation")]
    public class CreateHeroAtOccupationPatch
    {
        public static bool Prefix(Occupation neededOccupation, Settlement forcedHomeSettlement, ref Hero __result)
        {
            if (!Helpers.IsMFHideout(forcedHomeSettlement))
                return true;

            var gender = IMFModels.ClanGender(forcedHomeSettlement.OwnerClan);
            Settlement settlement = forcedHomeSettlement ?? SettlementHelper.GetRandomTown(null);
            IEnumerable<CharacterObject> enumerable;

            // NOT COPY/PASTED
            if (gender == IMFModels.Gender.Male)
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation && !x.IsFemale);
            else if (gender == IMFModels.Gender.Female)
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation && x.IsFemale);
            else
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation);
            // NOT COPY/PASTED

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

            CharacterObject template = null;
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
            CultureObject hideoutCulture = forcedHomeSettlement.Culture;
            // Give Darshi, Nord, and Vakken MFs correct notable names (their templates have incorrect names)
            // NOT copy/pasted
            if (Helpers.IsMinorCulture(hideoutCulture))
            {
                var nameList = new List<TextObject>();
                if (hero.IsFemale)
                    nameList = hideoutCulture.FemaleNameList;
                else
                    nameList = hideoutCulture.MaleNameList;

                TextObject firstName = nameList.GetRandomElement();
                TextObject fullName = (TextObject)Helpers.CallPrivateMethod(NameGenerator.Current, "GenerateHeroFullName", new object[] { hero, firstName, true });
                hero.SetName(fullName, firstName);
            }
            // NOT copy/pasted

            if (hero.HomeSettlement.IsVillage && hero.HomeSettlement.Village.Bound != null && hero.HomeSettlement.Village.Bound.IsCastle)
            {
                float value = MBRandom.RandomFloat * 20f;
                hero.AddPower(value);
            }
            if (neededOccupation != Occupation.Wanderer)
            {
                hero.ChangeState(Hero.CharacterStates.Active);
                EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
                int amount = 10000;
                GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);
            }
            CharacterObject template2 = hero.Template;
            if (((template2 != null) ? template2.HeroObject : null) != null && hero.Template.HeroObject.Clan != null && hero.Template.HeroObject.Clan.IsMinorFaction)
                hero.SupporterOf = hero.Template.HeroObject.Clan;
            else
                hero.SupporterOf = HeroHelper.GetRandomClanForNotable(hero);

            if (neededOccupation != Occupation.Wanderer)
                Helpers.CallPrivateMethod(null, "AddRandomVarianceToTraits", new object[] { hero }, typeof(HeroCreator));

            __result = hero;
            return false;
        }
    }
}

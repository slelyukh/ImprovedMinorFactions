using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.Patches
{
    [HarmonyPatch(typeof(NotablesCampaignBehavior), "CheckAndMakeNotableDisappear")]
    public class CheckAndMakeNotableDisappearPatch
    {
        static bool Prefix(Hero notable)
        {
            if(!Helpers.isMFHideout(notable.CurrentSettlement))
                return true;
            // not making MFH lords disappear rn
            return false;
        }
    }

    [HarmonyPatch(typeof(NotablesCampaignBehavior), "OnHeroKilled")]
    public class OnHeroKilledPatch
    {
        // method is copypastad in MFHNotablesCampaignBehavior so the default should not run.
        static bool Prefix(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (!Helpers.isMFHideout(victim.CurrentSettlement))
                return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(NotablePowerManagementBehavior), "DailyTickHero")]
    public class DailyTickHeroPatch
    {
        static bool Prefix(Hero hero)
        {
            if (!hero.IsNotable || !Helpers.isMFHideout(hero.CurrentSettlement))
                return true;
            hero.AddPower(MFHideoutModels.CalculateDailyNotablePowerChange(hero).ResultNumber);
            return false;
        }
    }
}

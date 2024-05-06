using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace ImprovedMinorFactions.Source.Patches
{
    // prevents MFNotables from disappearing for having too little Power
    [HarmonyPatch(typeof(NotablesCampaignBehavior), "CheckAndMakeNotableDisappear")]
    public class CheckAndMakeNotableDisappearPatch
    {
        static bool Prefix(Hero notable)
        {
            if(!Helpers.IsMFHideout(notable.CurrentSettlement))
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
            hero.AddPower(MFHideoutModels.CalculateDailyNotablePowerChange(hero).ResultNumber);
            return false;
        }
    }
}

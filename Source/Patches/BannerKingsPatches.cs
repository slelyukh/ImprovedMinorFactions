using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Source.Patches
{
    [HarmonyPatch]
    public class BKClanBehaviorOnSettlementEnteredPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKClanBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKEducationBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKEducationBehavior"), "InitializeEducation");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Hero hero)
        {
            if (Helpers.IsMFHideout(hero.BornSettlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKGentryBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKGentryBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKLifestyleBehaviorOnSettlementEnteredPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKLifestyleBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }

    // TODO: fix occupation check (may not need to do this)
    [HarmonyPatch]
    public class BKLifestyleBehaviorOnConversationEndedPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKLifestyleBehavior"), "OnConversationEnded");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(IEnumerable<CharacterObject> characters)
        {
            // copy/paste method? transpile???
            return true;
        }
    }

    [HarmonyPatch]
    public class BKLordPropertyBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKLordPropertyBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKLordPartyBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKPartyBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKReligionsBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.BKReligionsBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKFeastBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.Feasts.BKFeastBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKPartyNeedsBehaviorPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Behaviours.PartyNeeds.BKPartyNeedsBehavior"), "OnSettlementEntered");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement target)
        {
            if (Helpers.IsMFHideout(target))
                return false;
            return true;
        }
    }

    // TODO: occupation.gangleader checks
    [HarmonyPatch]
    public class BKAIBehaviorLifestylePatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Managers.AI.AIBehavior"), "ChooseLifestyle");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix()
        {
            // aaaaaah do something.
            return true;
        }
    }

    [HarmonyPatch]
    public class BKEconomyModelPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools.TypeByName("BannerKings.Models.Vanilla.BKEconomyModel"), "GetNotableCaravanLimit");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Hero notable, ref int __result)
        {
            if (Helpers.IsMFGangLeader(notable))
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    // PATCH PATCHES
    [HarmonyPatch]
    public class BKFixesPatchesGetGarrisonLeaveOrTakeDataOfPartyPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.FixesPatches.GarrisonTroopsCampaignBehaviorPatches"),
            "GetGarrisonLeaveOrTakeDataOfPartyPrefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(MobileParty mobileparty)
        {
            if (Helpers.IsMFHideout(mobileparty.CurrentSettlement))
                return false;
            return true;
        }
    }

    // may not need this
    [HarmonyPatch]
    public class BKFixesPatchesGangLeaderNeedsToOffloadStolenGoodsIssueBehaviorPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.FixesPatches.GangLeaderNeedsToOffloadStolenGoodsIssueBehaviorPatches"),
            "ConditionsHoldPrefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Hero issueGiver)
        {
            if (Helpers.IsMFHideout(issueGiver.CurrentSettlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKFixesPatchesSiegeAftermathCampaignBehaviorPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.FixesPatches.SiegeAftermathCampaignBehaviorPatches"),
            "GetSiegeAftermathInfluenceCostPrefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKFixesPatchesCraftingCampaignBehaviorPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.FixesPatches.CraftingCampaignBehaviorPatches"),
            "CreateTownOrderPrefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKNotablePatchesCreateHeroAtOccupationPatchPatch
    {
        private static MethodBase TargetMethod()
        {
            var type = AccessTools
            .TypeByName("BannerKings.Patches.NotablesPatches.CreateHeroAtOccupationPatch");
            var method = AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.NotablesPatches.CreateHeroAtOccupationPatch"),
            "Prefix");
            return method;
        }

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement forcedHomeSettlement)
        {
            if (Helpers.IsMFHideout(forcedHomeSettlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKNotablePatchesSpawnNotablesIfNeededPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.NotablesPatches.SpawnNotablesIfNeededPatch"),
            "Prefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }

    [HarmonyPatch]
    public class BKNotablePatchesDailyTickSettlementPatchPatch
    {
        private static MethodBase TargetMethod() => AccessTools
            .Method(AccessTools
            .TypeByName("BannerKings.Patches.NotablesPatches.DailyTickSettlementPatch"),
            "Prefix");

        private static bool Prepare() => TargetMethod() != null;

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }
}

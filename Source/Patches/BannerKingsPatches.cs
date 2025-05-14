using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.Patches
{
    public static class BKHelpers
    {
        public static MethodInfo? ResolveOriginalMethod(string assemblyName, string typeName, string methodName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName) // Assembly name
                ?.GetType(typeName) // class with full namespace
                ?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static bool Prepare(MethodBase? method, string assemblyName, string typeName, string methodName)
        {
            bool res = method != null;
            if (res == false && Harmony.HasAnyPatches(assemblyName))
            {
                string whyCantISeeVariablesInTheDebuggerNormally = $"IMF Error: {assemblyName} {typeName} {methodName} patch failed!";
                InformationManager.DisplayMessage(new InformationMessage($"IMF Error: {assemblyName} {typeName} {methodName} patch failed!", Colors.Red));
            } 
            return res;
        }
    }
    
    [HarmonyPatch]
    public class BKClanBehaviorOnSettlementEnteredPatch
    {
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKClanBehavior";
        private static string methodName = "OnSettlementEntered";
        
        private static MethodBase? TargetMethod()
        {
            return BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);
        }

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKEducationBehavior";
        private static string methodName = "InitializeEducation";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKGentryBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKLifestyleBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKLifestyleBehavior";
        private static string methodName = "OnConversationEnded";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

        public static bool Prefix(IEnumerable<CharacterObject> characters)
        {
            // copy/paste method? transpile???
            return true;
        }
    }

    [HarmonyPatch]
    public class BKLordPropertyBehaviorPatch
    {
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKLordPropertyBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKPartyBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.BKReligionsBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.Feasts.BKFeastBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Behaviours.PartyNeeds.BKPartyNeedsBehavior";
        private static string methodName = "OnSettlementEntered";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Managers.AI.AIBehavior";
        private static string methodName = "ChooseLifestyle";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

        public static bool Prefix()
        {
            // aaaaaah do something.
            return true;
        }
    }

    [HarmonyPatch]
    public class BKEconomyModelPatch
    {
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Models.Vanilla.BKEconomyModel";
        private static string methodName = "GetNotableCaravanLimit";
        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

        public static bool Prefix(Hero notable, ref int __result)
        {
            if (Helpers.IsMFNotable(notable))
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    // PATCH PATCHES
    [HarmonyPatch]
    //public class BKFixesPatchesGetGarrisonLeaveOrTakeDataOfPartyPatchPatch
    //{
    //    private static string assemblyName = "BannerKings";
    //    private static string typeName = "BannerKings.Patches.FixesPatches+GarrisonTroopsCampaignBehaviorPatches";
    //    private static string methodName = "GetGarrisonLeaveOrTakeDataOfPartyPrefix";

    //    private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

    //    private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

    //    public static bool Prefix(MobileParty mobileparty)
    //    {
    //        if (Helpers.IsMFHideout(mobileparty.CurrentSettlement))
    //            return false;
    //        return true;
    //    }
    //}

    // may not need this
    [HarmonyPatch]
    public class BKFixesPatchesGangLeaderNeedsToOffloadStolenGoodsIssueBehaviorPatchPatch
    {
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.FixesPatches+GangLeaderNeedsToOffloadStolenGoodsIssueBehaviorPatches";
        private static string methodName = "ConditionsHoldPrefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.FixesPatches+SiegeAftermathCampaignBehaviorPatches";
        private static string methodName = "GetSiegeAftermathInfluenceCostPrefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.FixesPatches+CraftingCampaignBehaviorPatches";
        private static string methodName = "CreateTownOrderPrefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.NotablePatches+CreateHeroAtOccupationPatch";
        private static string methodName = "Prefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

        public static bool Prefix(Occupation neededOccupation, Settlement forcedHomeSettlement)
        {
            if (Helpers.IsMFHideout(forcedHomeSettlement))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    public class BKNotablePatchesSpawnNotablesIfNeededPatchPatch
    {
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.NotablePatches+SpawnNotablesIfNeededPatch";
        private static string methodName = "Prefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

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
        private static string assemblyName = "BannerKings";
        private static string typeName = "BannerKings.Patches.NotablePatches+DailyTickSettlementPatch";
        private static string methodName = "Prefix";

        private static MethodBase? TargetMethod() => BKHelpers.ResolveOriginalMethod(assemblyName, typeName, methodName);

        private static bool Prepare() => BKHelpers.Prepare(TargetMethod(), assemblyName, typeName, methodName);

        public static bool Prefix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                return false;
            return true;
        }
    }
}

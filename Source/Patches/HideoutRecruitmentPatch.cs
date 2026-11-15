using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "HourlyTickParty")]
    public class HideoutVolunteerRecruitmentPatch
    {
        private static readonly MethodInfo? CheckRecruiting =
            AccessTools.Method(typeof(RecruitmentCampaignBehavior), "CheckRecruiting", new[] { typeof(MobileParty), typeof(Settlement) });

        static void Postfix(RecruitmentCampaignBehavior __instance, MobileParty mobileParty)
        {
            try
            {
                if (CheckRecruiting == null || mobileParty == null || !mobileParty.IsLordParty || mobileParty == MobileParty.MainParty || mobileParty.MapEvent != null)
                    return;
                Settlement? camp = mobileParty.CurrentSettlement;
                if (camp == null || mobileParty.LeaderHero == null)
                    return;
                MinorFactionHideout? hideout = Helpers.GetMFHideout(camp);
                if (hideout == null || !hideout.IsActive)
                    return;

                int before = mobileParty.Party.NumberOfAllMembers;
                CheckRecruiting.Invoke(__instance, new object[] { mobileParty, camp });
                int gained = mobileParty.Party.NumberOfAllMembers - before;
                if (gained > 0)
                    IMFDiagnostics.Verbose($"[Visit] {mobileParty.StringId} ({mobileParty.LeaderHero.Name}) took {gained} volunteers from the notables of {camp.StringId}");
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("HideoutVolunteerRecruitmentPatch failed:", e);
            }
        }
    }
}

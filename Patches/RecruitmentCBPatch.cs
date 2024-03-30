using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
    class RecruitmentUpdateNotableVolunteersPatch
    {
        static void Postfix(Settlement settlement)
        {
            var mfHideout = settlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null || !Helpers.IsMFClanInitialized(mfHideout.OwnerClan))
                return;

            foreach (Hero notable in settlement.Notables)
            {
                if (!notable.CanHaveRecruits || !notable.IsAlive)
                    return;

                var basicVolunteer = mfHideout.OwnerClan.BasicTroop;
                for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                {
                    if (MBRandom.RandomFloat < MFHideoutModels.GetDailyVolunteerProductionProbability(notable, i, settlement))
                    {
                        if (notable.VolunteerTypes[i] == null)
                        {
                            notable.VolunteerTypes[i] = basicVolunteer;
                        }
                        else if (notable.VolunteerTypes[i].UpgradeTargets.Length != 0)
                        {
                            notable.VolunteerTypes[i] = notable.VolunteerTypes[i].UpgradeTargets[0];
                        }
                    }
                }
            }
        }
    }
}

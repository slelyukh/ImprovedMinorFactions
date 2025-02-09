﻿using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Patches
{
    // allows MFHideouts to have recruits
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
    class RecruitmentUpdateNotableVolunteersPatch
    {
        static void Postfix(Settlement settlement)
        {
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null || !mfHideout.IsActive)
                return;

            foreach (Hero notable in settlement.Notables)
            {
                if (!notable.CanHaveRecruits || !notable.IsAlive)
                    continue;

                var volunteerTypes = notable.VolunteerTypes;
                var basicVolunteer = Helpers.GetBasicTroop(mfHideout!.OwnerClan!);

                for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                {
                    if (MBRandom.RandomFloat < IMFModels.GetDailyVolunteerProductionProbability(notable, i, mfHideout))
                    {
                        var upgradeLen = volunteerTypes[i]?.UpgradeTargets.Length ?? 0;
                        if (volunteerTypes[i] == null)
                        {
                            volunteerTypes[i] = basicVolunteer;
                        }
                        else if (upgradeLen != 0)
                        {
                            volunteerTypes[i] = volunteerTypes[i].UpgradeTargets[MBRandom.RandomInt(upgradeLen)];
                        }
                    }
                }

                // shuffle volunteers to keep higher tiers right (copypasta from recruitmentCampaignBehavior.UpdateVolunteersOfNotablesInSettlement)
                for (int j = 1; j < 6; j++)
                {
                    CharacterObject volunteer1 = volunteerTypes[j];
                    if (volunteer1 != null)
                    {
                        int num = 0;
                        int swapTo = j - 1;
                        CharacterObject volunteer2 = volunteerTypes[swapTo];
                        while (swapTo >= 0 && (volunteer2 == null || (float)volunteer1.Level < (float)volunteer2.Level))
                        {
                            if (volunteer2 == null)
                            {
                                swapTo--;
                                num++;
                                if (swapTo >= 0)
                                {
                                    volunteer2 = volunteerTypes[swapTo];
                                }
                            }
                            else
                            {
                                volunteerTypes[swapTo + 1 + num] = volunteer2;
                                swapTo--;
                                num = 0;
                                if (swapTo >= 0)
                                {
                                    volunteer2 = volunteerTypes[swapTo];
                                }
                            }
                        }
                        volunteerTypes[swapTo + 1 + num] = volunteer1;
                    }
                }
            }
        }
    }
}

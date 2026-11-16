using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using MHE = Helpers;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Lets the camp itself and its own parties join the defending side of a camp raid.
    /// </summary>
    [HarmonyPatch(typeof(MapEvent), "CanPartyJoinBattle")]
    public class MapEventCanPartyJoinBattlePatch
    {
        static bool Prefix(MapEvent __instance, PartyBase party, BattleSideEnum side, ref bool __result)
        {
            if (side == BattleSideEnum.Defender && __instance.IsHideoutBattle)
            {
                Settlement mapEventSettlement = __instance.MapEventSettlement;
                if (mapEventSettlement != null && Helpers.IsMFHideout(mapEventSettlement))
                {
                    bool isDefenderOfHideout = party == mapEventSettlement.Party
                        || (party.MobileParty != null
                            && (party.MobileParty.HomeSettlement == mapEventSettlement
                                || party.MobileParty.CurrentSettlement == mapEventSettlement));
                    if (isDefenderOfHideout)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// If nobody healthy is left to defend a raided camp, the clan's entry troops defend it instead of the battle starting empty.
    /// </summary>
    [HarmonyPatch(typeof(MHE.MapEventHelper), "GetPriorityListForHideoutMission")]
    public class GetPriorityListForHideoutMissionPatch
    {
        static bool Prefix(List<MobileParty> partyList, ref FlattenedTroopRoster __result, ref int firstPhaseTroopCount)
        {
            int totalHealthy = 0;
            foreach (MobileParty party in partyList)
                totalHealthy += party.Party.MemberRoster.TotalHealthyCount;
            if (totalHealthy > 0)
                return true;

            Settlement hideoutSettlement = null;
            foreach (MobileParty party in partyList)
            {
                if (party.HomeSettlement != null && Helpers.IsMFHideout(party.HomeSettlement))
                {
                    hideoutSettlement = party.HomeSettlement;
                    break;
                }
            }
            if (hideoutSettlement == null)
                hideoutSettlement = Settlement.CurrentSettlement;
            if (hideoutSettlement == null || !Helpers.IsMFHideout(hideoutSettlement))
                return true;

            MinorFactionHideout mfh = Helpers.GetMFHideout(hideoutSettlement)!;
            int count = Math.Max(5, IMFModels.NumMilitiaPostRaid(mfh.OwnerClan));
            var parts = Helpers.SplitEntryTroops(mfh.OwnerClan, count);
            if (parts.Count == 0)
                return true;

            FlattenedTroopRoster roster = new FlattenedTroopRoster(count);
            foreach ((CharacterObject troop, int part) in parts)
                roster.Add(troop, part);
            firstPhaseTroopCount = Math.Min(
                (int)Math.Floor(count * Campaign.Current.Models.BanditDensityModel.SpawnPercentageForFirstFightInHideoutMission),
                Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForFirstFightInHideout);
            __result = roster;
            return false;
        }
    }
}

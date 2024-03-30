using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // TODO: this is a debug patch and needs ot be moved
    [HarmonyPatch(typeof(DefaultBanditDensityModel), "get_NumberOfMaximumTroopCountForFirstFightInHideout")]
    public class DefaultMaxTroopsFirstHideoutBattlePatch
    {
        static void Postfix(ref int __result)
        {
            __result = 0;
        }
     }

    [HarmonyPatch(typeof(CampaignCheats), "ShowHideouts")]
    public class DefaultEncounterGameMenuModelStateMenuPatch
    {
        static void Postfix(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
            {
                return;
            }
            int num;
            if (!CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings) || !int.TryParse(strings[0], out num) || (num != 1 && num != 2))
            {
                return;
            }

            foreach (Settlement settlement in Settlement.All)
            {
                var mfHideout = settlement.SettlementComponent as MinorFactionHideout; 
                if (mfHideout != null && (num != 1 || mfHideout.IsActive)) {
                    mfHideout.IsSpotted = true;
                    mfHideout.Owner.Settlement.IsVisible = true;
                }
            }
        }
    }
}

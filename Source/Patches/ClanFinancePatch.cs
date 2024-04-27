using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    // gives MF Clans income from their hideouts
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddSettlementIncome")]
    public class AddSettlementIncomePatch
    {
        static void Postfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
        {
            var mfHideout = MFHideoutManager.Current.GetHideoutOfClan(clan);
            if (mfHideout == null)
                return;
            goldChange.Add(MFHideoutModels.CalculateHideoutIncome(mfHideout), new TextObject("Hideout Income"), mfHideout.Name);
        }
    }
}

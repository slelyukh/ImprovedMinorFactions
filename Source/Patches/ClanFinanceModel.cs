using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    /// <summary>
    /// Wraps the game's clan finance model to add the daily income of a clan's active camps.
    /// </summary>
    // gives MF Clans income from their hideouts
    // TODO: attach model
    public class IMFClanFinanceModel : ClanFinanceModel
    {
        private ClanFinanceModel _previousModel;
        public override int PartyGoldLowerThreshold => _previousModel.PartyGoldLowerThreshold;

        public IMFClanFinanceModel(ClanFinanceModel previousModel)
        {
            this._previousModel = previousModel;
        }
        public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            return _previousModel.CalculateClanExpenses(clan, includeDescriptions, applyWithdrawals, includeDetails);
        }

        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var eNum = _previousModel.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCampIncome(clan, ref eNum);
            return eNum;
        }

        public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var eNum = _previousModel.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCampIncome(clan, ref eNum);
            return eNum;
        }

        /// <summary>
        /// Adds each active camp's income, and the camp support for clans outside a kingdom.
        /// </summary>
        private static void AddCampIncome(Clan clan, ref ExplainedNumber eNum)
        {
            var mfHideouts = IMFManager.Current!.GetActiveHideoutsOfClan(clan);
            foreach (var mfh in mfHideouts)
            {
                eNum.Add(IMFModels.CalculateHideoutIncome(mfh), new TextObject("Hideout Income"), mfh.Name);
            }
            if (mfHideouts.Count > 0)
                eNum.Add(IMFModels.CalculateCampSupport(clan), new TextObject("Camp Support"));
        }

        public override int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals)
        {
            return _previousModel.CalculateNotableDailyGoldChange(hero, applyWithdrawals);
        }

        public override int CalculateOwnerIncomeFromCaravan(MobileParty caravan)
        {
            return _previousModel.CalculateOwnerIncomeFromCaravan(caravan);
        }

        public override int CalculateOwnerIncomeFromWorkshop(Workshop workshop)
        {
            return _previousModel.CalculateOwnerIncomeFromWorkshop(workshop);
        }

        public override int CalculateTownIncomeFromProjects(Town town)
        {
            return _previousModel.CalculateTownIncomeFromProjects(town);
        }

        public override ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false)
        {
            return _previousModel.CalculateTownIncomeFromTariffs(clan, town, applyWithdrawals);
        }

        public override int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false)
        {
            return _previousModel.CalculateVillageIncome(clan, village, applyWithdrawals);
        }

        public override float RevenueSmoothenFraction()
        {
            return _previousModel.RevenueSmoothenFraction();
        }
    }
}

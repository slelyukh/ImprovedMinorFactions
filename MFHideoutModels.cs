using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedMinorFactions
{
    internal static class MFHideoutModels
    {
        
        // TODO: make a real calculation
        public static float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            return 0.2f;
        }

        // TODO: make a real calculation
        public static float GetHearthChange(Settlement settlement)
        {
            return 2f;
        }

        public static float GetMilitiaChange(Settlement settlement)
        {
            return 0.2f;
        }

        // TODO: don't just copy bandit hideouts
        public static int GetPlayerMaximumTroopCountForRaidMission(MobileParty party)
        {
            float num = 10f;
            if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics, false))
            {
                num += DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
            }
            return TaleWorlds.Library.MathF.Round(num);
        }

    }
}

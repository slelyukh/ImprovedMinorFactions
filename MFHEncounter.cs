using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions
{
    internal class MFHEncounter : LocationEncounter
    {
        public MFHEncounter(Settlement settlement) : base(settlement)
        {
        }

        // Method copypasta that may not be needed
        //public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
        //{
        //    int num = base.Settlement.Town.GetWallLevel();
        //    string sceneName = nextLocation.GetSceneName(num);
        //    IMission result;
        //    if (nextLocation.StringId == "center")
        //    {
        //        result = CampaignMission.OpenTownCenterMission(sceneName, nextLocation, talkToChar, num, playerSpecialSpawnTag);
        //    }
        //    else if (nextLocation.StringId == "arena")
        //    {
        //        result = CampaignMission.OpenArenaStartMission(sceneName, nextLocation, talkToChar);
        //    }
        //    else
        //    {
        //        num = Campaign.Current.Models.LocationModel.GetSettlementUpgradeLevel(PlayerEncounter.LocationEncounter);
        //        result = CampaignMission.OpenIndoorMission(sceneName, num, nextLocation, talkToChar);
        //    }
        //    return result;
        //}
    }
}

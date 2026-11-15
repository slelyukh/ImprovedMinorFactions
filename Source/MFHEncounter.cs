using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace ImprovedMinorFactions
{
    // Not sure why this is needed but it is
    internal class MFHEncounter : LocationEncounter
    {
        public MFHEncounter(Settlement settlement) : base(settlement)
        {
        }

        // Base returns null so talking does nothing, open the center like VillageEncounter does.
        public override IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
        {
            if (nextLocation != null && nextLocation.StringId == "mf_hideout_center")
                return CampaignMission.OpenVillageMission(nextLocation.GetSceneName(1), nextLocation, talkToChar);
            return base.CreateAndOpenMissionController(nextLocation, previousLocation, talkToChar, playerSpecialSpawnTag);
        }
    }
}

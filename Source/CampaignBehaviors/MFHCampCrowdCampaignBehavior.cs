using System.Collections.Generic;
using System.Linq;
using System;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    /// <summary>
    /// Fills the camp center scene with the camp's own militia, some standing guard and the rest walking around,
    /// and gives them a short greeting when the player talks to them. The greeting depends on whether the player
    /// owns the camp, is a friend of the clan, or a stranger.
    /// </summary>
    internal class MFHCampCrowdCampaignBehavior : CampaignBehaviorBase
    {
        private const string CenterId = "mf_hideout_center";
        private const float StandingShare = 0.35f;
        private const string GuardTag = "sp_guard";
        private const string WandererTag = "npc_common";

        private readonly List<LocationCharacter> _crowd = new List<LocationCharacter>();

        private readonly CampSceneTidy _tidy = new CampSceneTidy();

        public override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(OnReadyToSpawn));
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(_ => RemoveCrowd(null)));
            CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, new Action<float>(_tidy.Tick));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(SetDialogs));
        }

        /// <summary>
        /// Adds the militia greeting line.
        /// </summary>
        private void SetDialogs(CampaignGameStarter starter)
        {
            starter.AddDialogLine("mf_militia_talk", "start", "close_window",
                "{=!}{MF_MILITIA_GREETING}",
                MilitiaTalkCondition, null, 120);
        }

        private static readonly TextObject[] OwnClanGreetings =
        {
            new TextObject("{=imfMilitiaOwn1}At your command, {?IS_FEMALE}my lady{?}my lord{\\?}. The camp is quiet and the watch is set."),
            new TextObject("{=imfMilitiaOwn2}Good to see you, {?IS_FEMALE}my lady{?}my lord{\\?}. The men are ready whenever you are."),
            new TextObject("{=imfMilitiaOwn3}All is well in camp, {?IS_FEMALE}my lady{?}my lord{\\?}. Say the word and we ride."),
            new TextObject("{=imfMilitiaOwn4}The {CLAN} are yours to command, {?IS_FEMALE}my lady{?}my lord{\\?}."),
        };

        private static readonly TextObject[] FriendGreetings =
        {
            new TextObject("{=imfMilitiaFriend1}A friend of the {CLAN} is welcome at our fire."),
            new TextObject("{=imfMilitiaFriend2}You are known to us. Rest, eat, and speak to the elders."),
            new TextObject("{=imfMilitiaFriend3}Friends of the camp come and go as they like. Leave the supplies alone, though."),
        };

        private static readonly TextObject[] StrangerGreetings =
        {
            new TextObject("{=imfMilitiaStranger1}Welcome to the camp. Keep your hands where we can see them, and we will get along."),
            new TextObject("{=imfMilitiaStranger2}Quiet today. Speak to the elders if you want something done."),
            new TextObject("{=imfMilitiaStranger3}We keep watch so the others can sleep. Good to see a friendly face."),
            new TextObject("{=imfMilitiaStranger4}You are among friends here. Just stay out of the supplies."),
        };

        /// <summary>
        /// Picks the greeting for a camp militiaman the player is talking to. Not used during a raid.
        /// </summary>
        /// <returns>true if the greeting should play</returns>
        private static bool MilitiaTalkCondition()
        {
            CharacterObject? who = CharacterObject.OneToOneConversationCharacter;
            if (who == null || who.IsHero || !Helpers.IsMFHideout(Settlement.CurrentSettlement))
                return false;
            // Not during a raid, at priority 120 this would take over the boss duel.
            if (MapEvent.PlayerMapEvent != null || PlayerEncounter.Battle != null
                || Mission.Current?.GetMissionBehavior<SandBox.Missions.MissionLogics.Hideout.HideoutMissionController>() != null)
                return false;
            Clan? owner = Settlement.CurrentSettlement.OwnerClan;
            TextObject[] lines;
            if (owner != null && (owner == Clan.PlayerClan || Hero.MainHero.Clan == owner))
                lines = OwnClanGreetings;
            else if (owner != null && Helpers.IsPlayerFriendOfMF(owner))
                lines = FriendGreetings;
            else
                lines = StrangerGreetings;
            TextObject line = lines[MBRandom.RandomInt(lines.Length)].CopyTextObject();
            line.SetTextVariable("CLAN", owner?.Name ?? TextObject.GetEmpty());
            line.SetTextVariable("IS_FEMALE", Hero.MainHero.IsFemale ? 1 : 0);
            MBTextManager.SetTextVariable("MF_MILITIA_GREETING", line, false);
            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        /// <summary>
        /// Takes the militia we added out of the camp location.
        /// </summary>
        /// <param name="location">the camp center, or null to look it up</param>
        private void RemoveCrowd(Location? location)
        {
            if (_crowd.Count == 0)
                return;
            location ??= LocationComplex.Current?.GetLocationWithId(CenterId);
            if (location != null)
                foreach (LocationCharacter lc in _crowd)
                    location.RemoveLocationCharacter(lc);
            _crowd.Clear();
        }

        /// <summary>
        /// Adds the militia to the camp center when its scene is about to spawn people. About a third stand guard.
        /// </summary>
        /// <param name="unusedPoints">free spawn points per tag, left over after the game placed its own people</param>
        private void OnReadyToSpawn(Dictionary<string, int> unusedPoints)
        {
            try
            {
                Settlement? settlement = PlayerEncounter.LocationEncounter?.Settlement;
                Location? location = CampaignMission.Current?.Location;
                if (!Helpers.IsMFHideout(settlement) || location == null || location.StringId != CenterId)
                    return;

                RemoveCrowd(location);
                // Never more men than the scene has free spawn points or they end up inside each other.
                unusedPoints.TryGetValue(GuardTag, out int guardPoints);
                unusedPoints.TryGetValue(WandererTag, out int wandererPoints);
                List<(CharacterObject Troop, int Count)> picks = ChooseMen(settlement!);
                int guards = Math.Min(guardPoints, (int)Math.Round(picks.Sum(p => p.Count) * StandingShare));
                int wanderers = wandererPoints;
                foreach ((CharacterObject troop, int count) in picks)
                {
                    for (int i = 0; i < count && (guards > 0 || wanderers > 0); i++)
                    {
                        bool stand = guards > 0;
                        LocationCharacter? lc = MakeMilitiaman(troop, settlement!, stand);
                        if (lc == null)
                            break;
                        location.AddCharacter(lc);
                        _crowd.Add(lc);
                        if (stand)
                            guards--;
                        else
                            wanderers--;
                    }
                }
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFHCampCrowd failed:", e);
            }
        }

        /// <summary>
        /// Picks which troops to show, in proportion to the camp's militia roster.
        /// If the roster is empty it uses the clan's entry troops instead.
        /// </summary>
        private static List<(CharacterObject Troop, int Count)> ChooseMen(Settlement settlement)
        {
            var result = new List<(CharacterObject, int)>();
            var byId = new Dictionary<string, CharacterObject>();
            var stacks = new List<TroopStackInfo>();
            TroopRoster? roster = settlement.MilitiaPartyComponent?.Party?.MemberRoster;
            int total = 0;
            if (roster != null)
                foreach (TroopRosterElement e in roster.GetTroopRoster())
                {
                    if (e.Character == null || e.Character.IsHero || e.Number <= 0)
                        continue;
                    byId[e.Character.StringId] = e.Character;
                    stacks.Add(new TroopStackInfo(e.Character.StringId, e.Character.Level, e.Number));
                    total += e.Number;
                }
            if (total > 0)
            {
                int[] split = Helpers.SplitByWeight(stacks, CrowdSize(total));
                for (int i = 0; i < split.Length; i++)
                    if (split[i] > 0)
                        result.Add((byId[stacks[i].Id], split[i]));
                return result;
            }
            Clan? clan = settlement.OwnerClan;
            int shown = CrowdSize((int)settlement.Militia);
            if (clan != null && shown > 0)
                result.AddRange(Helpers.SplitEntryTroops(clan, shown));
            return result;
        }

        /// <summary>
        /// Makes one militiaman for the scene. Camp militia wear their own battle gear, troops without any gear are left out.
        /// </summary>
        /// <param name="standGuard">stand at a guard point instead of walking around</param>
        /// <returns>the character, or null if the troop has no gear</returns>
        private static LocationCharacter? MakeMilitiaman(CharacterObject troop, Settlement settlement, bool standGuard)
        {
            Equipment? gear = troop.RandomBattleEquipment;
            if (!CampSceneTidy.IsDressed(gear))
                return null;
            Monster monster = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(troop.Race, "_settlement");
            AgentData data = new AgentData(new SimpleAgentOrigin(troop, -1, settlement.OwnerClan?.Banner, default))
                .Monster(monster).Equipment(gear!).NoHorses(true);
            IAgentBehaviorManager behaviors = SandBoxManager.Instance.AgentBehaviorManager;
            LocationCharacter.AddBehaviorsDelegate add = standGuard
                ? new LocationCharacter.AddBehaviorsDelegate(behaviors.AddStandGuardBehaviors)
                : new LocationCharacter.AddBehaviorsDelegate(behaviors.AddOutdoorWandererBehaviors);
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(data.AgentMonster, data.AgentIsFemale, "_guard");
            return new LocationCharacter(data, add, standGuard ? GuardTag : WandererTag, true, LocationCharacter.CharacterRelations.Neutral,
                actionSet, false, false, null, false, false, true, null, false);
        }

        internal const int MaxShown = 14;
        private const int MinShown = 4;

        /// <summary>
        /// How many militia to show: a fifth of the militia, at least 4 if there are that many, and at most 14.
        /// </summary>
        internal static int CrowdSize(int militia)
        {
            if (militia <= 0)
                return 0;
            int quarter = (int)Math.Ceiling(militia * 0.2);
            return Math.Min(MaxShown, Math.Max(quarter, Math.Min(militia, MinShown)));
        }
    }

    /// <summary>
    /// Removes people who load into the camp scene without clothes, which happens with troops whose gear is broken.
    /// </summary>
    internal sealed class CampSceneTidy
    {
        private const float CheckEvery = 1f;

        private readonly HashSet<int> _handled = new HashSet<int>();
        private Mission? _mission;
        private bool _isCamp;
        private float _wait;

        /// <summary>
        /// Checks the camp scene about once a second for people without clothes.
        /// </summary>
        internal void Tick(float dt)
        {
            try
            {
                Mission? mission = Mission.Current;
                if (mission == null)
                {
                    _mission = null;
                    return;
                }
                if (mission != _mission)
                {
                    _mission = mission;
                    _handled.Clear();
                    _wait = 0f;
                    _isCamp = Helpers.IsMFHideout(PlayerEncounter.LocationEncounter?.Settlement) && CampaignMission.Current?.Location != null;
                }
                if (!_isCamp)
                    return;
                // Don't touch anything while the mission ends, the game is freeing the agents.
                if (mission.CurrentState != Mission.State.Continuing || mission.MissionEnded)
                    return;
                _wait -= dt;
                if (_wait > 0f)
                    return;
                _wait = CheckEvery;
                RemoveUnclothed(mission);
            }
            catch (Exception e)
            {
                IMFDiagnostics.LogOnce("CampSceneTidy failed:", e);
            }
        }

        /// <summary>
        /// Checks that an equipment set has a body or leg piece.
        /// </summary>
        internal static bool IsDressed(Equipment? equipment)
        {
            if (equipment == null)
                return false;
            // A troop with broken gear loads in underwear, any body or leg piece is enough.
            return !equipment[EquipmentIndex.Body].IsEmpty || !equipment[EquipmentIndex.Leg].IsEmpty;
        }

        private static bool IsPerson(Agent agent)
        {
            return agent.IsHuman && !agent.IsMainAgent && agent.IsActive() && agent.AgentVisuals != null;
        }

        /// <summary>
        /// Removes every non-hero agent in the scene that has no body or leg armor.
        /// </summary>
        private void RemoveUnclothed(Mission mission)
        {
            foreach (Agent agent in mission.Agents.ToList())
            {
                if (!IsPerson(agent) || _handled.Contains(agent.Index))
                    continue;
                if (agent.Character is CharacterObject character && character.IsHero)
                    continue;
                if (IsDressed(agent.SpawnEquipment))
                    continue;
                _handled.Add(agent.Index);
                Remove(agent);
            }
        }

        /// <summary>
        /// Fades an agent out and takes it out of the location so it doesn't come back.
        /// </summary>
        private static void Remove(Agent agent)
        {
            Location? location = CampaignMission.Current?.Location;
            LocationCharacter? placed = location != null && agent.Origin != null ? location.GetLocationCharacter(agent.Origin) : null;
            agent.FadeOut(true, true);
            if (placed != null)
                location!.RemoveLocationCharacter(placed);
            IMFDiagnostics.Verbose($"[Camp] took an unclothed man ({agent.Character?.StringId ?? "no character"}) out of the camp scene");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using HarmonyLib;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// Decides where minor faction camps sit on the campaign map and where parties appear next to them.
    /// Camps go near the clan's home town on land parties can actually reach, and are placed when they activate.
    /// Loading a save never moves a camp. Map lookups throw off the navmesh, so those spots count as unusable.
    /// </summary>
    internal static class HideoutPlacement
    {
        private const float MinHideoutDistanceFromSettlement = 8f;
        private const float MaxHideoutDistanceFromSettlement = 55f;

        private const float MinSpacingFromOtherSettlements = 4f;

        private const int RandomPlacementAttempts = 90;
        private const float PassabilityProbeRadius = 3f;
        private const float PassabilityAcceptanceDistance = 8f;

        private static readonly int[] FallbackRadii =
        {
            4, 8, 14, 22, 32, 45, 60, 80, 105
        };

        private static readonly TerrainType[] NotLandTerrains =
        {
            TerrainType.Lake, TerrainType.Water, TerrainType.River, TerrainType.CoastalSea,
            TerrainType.OpenSea, TerrainType.NonNavigableRiver, TerrainType.SeaRestriction,
            TerrainType.Fording, TerrainType.Swamp, TerrainType.Beach, TerrainType.Cliff,
            TerrainType.UnderBridge
        };

        private static readonly Vec2[] LandProbes = new Vec2[4]
        {
            new Vec2(5f, 0f),
            new Vec2(-5f, 0f),
            new Vec2(0f, 5f),
            new Vec2(0f, -5f)
        };

        private static readonly Dictionary<string, CampaignVec2> Anchors = new Dictionary<string, CampaignVec2>();

        /// <summary>
        /// Clears the remembered home positions. Called when a campaign starts or loads.
        /// </summary>
        internal static void ForgetAnchors() => Anchors.Clear();

        /// <summary>
        /// Remembers which town a camp belongs near. In a loaded save the clan's home can be the camp itself,
        /// so that case falls back to the clan's initial home, and a camp with no real town gets no anchor.
        /// </summary>
        /// <param name="hideout">the camp settlement</param>
        /// <param name="home">the clan's current home settlement</param>
        internal static void RegisterAnchor(Settlement hideout, Settlement home)
        {
            try
            {
                if (!IsRegularHome(home, hideout))
                    home = hideout.OwnerClan?.InitialHomeSettlement!;
                if (!IsRegularHome(home, hideout))
                    return;
                Anchors[hideout.StringId] = new CampaignVec2(home.GetPosition2D, true);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Guard] no anchor for {hideout?.StringId}: {e.Message}");
            }
        }

        private static bool IsRegularHome(Settlement? home, Settlement hideout)
        {
            return home != null && home != hideout && !home.IsHideout && !Helpers.IsMFHideout(home);
        }

        /// <summary>
        /// Gets the clan's home town or castle, skipping hideouts and the camp itself.
        /// </summary>
        /// <returns>the home settlement, or null if the clan has no real one</returns>
        private static Settlement? RegularHomeOf(Settlement hideout, Clan? ownerClan)
        {
            if (IsRegularHome(ownerClan?.HomeSettlement, hideout))
                return ownerClan!.HomeSettlement;
            if (IsRegularHome(ownerClan?.InitialHomeSettlement, hideout))
                return ownerClan!.InitialHomeSettlement;
            return null;
        }

        /// <summary>
        /// Gets the point a camp should stay near: the registered anchor, else the clan's home, else the camp's own position.
        /// </summary>
        private static CampaignVec2 ResolveAnchor(Settlement hideout, Clan ownerClan)
        {
            if (Anchors.TryGetValue(hideout.StringId, out CampaignVec2 anchor))
                return anchor;
            Settlement? home = RegularHomeOf(hideout, ownerClan);
            return new CampaignVec2(home != null ? home.GetPosition2D : hideout.GetPosition2D, true);
        }

        private static bool HasRealAnchor(Settlement hideout, Clan ownerClan)
        {
            return Anchors.ContainsKey(hideout.StringId) || RegularHomeOf(hideout, ownerClan) != null;
        }

        /// <summary>
        /// Checks that a point is dry land a normal party can walk on. Some maps report open sea as valid,
        /// so the point itself and 3 of 4 nearby probes must also be on land terrain.
        /// </summary>
        internal static bool IsValidLandPosition(CampaignVec2 position)
        {
            return IsValidLandPositionInternal(position, null);
        }

        /// <summary>
        /// Land check behind IsValidLandPosition. With an anchor it also rejects points on a different island than the anchor.
        /// </summary>
        /// <param name="anchor">the home position to compare islands with, or null to skip that check</param>
        /// <returns>true for usable land, false for water, bad terrain or anything off the navmesh</returns>
        private static bool IsValidLandPositionInternal(CampaignVec2 position, CampaignVec2? anchor)
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper == null)
                    return false;
                PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(position);
                if (!face.IsValid())
                    return false;
                int[] invalidLand = Campaign.Current.Models.PartyNavigationModel?.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
                if (invalidLand != null && invalidLand.Contains(face.FaceGroupIndex))
                    return false;
                if (!global::Helpers.NavigationHelper.IsPositionValidForNavigationType(position, MobileParty.NavigationType.Default))
                    return false;
                if (anchor.HasValue)
                {
                    try
                    {
                        PathFaceRecord anchorFace = Campaign.Current.MapSceneWrapper.GetFaceIndex(anchor.Value);
                        if (anchorFace.IsValid() && face.FaceIslandIndex != anchorFace.FaceIslandIndex)
                            return false;
                    }
                    catch { } // off the navmesh, skip the island check
                }
                TerrainType terrain = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(position);
                if (NotLandTerrains.Contains(terrain))
                    return false;
                int landProbes = 1;
                foreach (Vec2 probe in LandProbes)
                {
                    CampaignVec2 p = new CampaignVec2(position.ToVec2() + probe, position.IsOnLand);
                    if (!NotLandTerrains.Contains(Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(p)))
                        landProbes++;
                }
                return landProbes >= 4;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for mountain terrain or a spot parties can't reach. A camp up a mountain passes the water checks
        /// but parties can't get to its icon.
        /// </summary>
        private static bool IsOnHill(CampaignVec2 position)
        {
            try
            {
                return Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(position) == TerrainType.Mountain
                    || !IsReachable(position);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Guard] hill check failed at {position}: {e.Message}");
                return false;
            }
        }

        private static float GetNearestNonHideoutSettlementDistanceSquared(CampaignVec2 position, Settlement? ignoreSettlement = null)
            => NearestSettlementDistanceSquared(position, ignoreSettlement, skipHideouts: true);

        private static float GetNearestSettlementDistanceSquared(CampaignVec2 position, Settlement? ignoreSettlement = null)
            => NearestSettlementDistanceSquared(position, ignoreSettlement, skipHideouts: false);

        /// <summary>
        /// Squared distance from a point to the closest settlement.
        /// </summary>
        /// <param name="ignoreSettlement">a settlement to leave out, usually the camp being placed</param>
        /// <param name="skipHideouts">leave out bandit hideouts and minor faction camps, so only towns, castles and villages count</param>
        private static float NearestSettlementDistanceSquared(CampaignVec2 position, Settlement? ignoreSettlement, bool skipHideouts)
        {
            float nearestDistanceSq = float.MaxValue;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement == ignoreSettlement)
                    continue;
                if (skipHideouts && (Helpers.IsMFHideout(settlement) || settlement.IsHideout))
                    continue;
                float distanceSq = settlement.GetPosition2D.DistanceSquared(position.ToVec2());
                if (distanceSq < nearestDistanceSq)
                    nearestDistanceSq = distanceSq;
            }
            return nearestDistanceSq;
        }

        /// <summary>
        /// Checks that a point is a good camp spot: usable land, not on a hill, 8 to 55 units from the nearest town,
        /// castle or village, and not right on top of another settlement.
        /// </summary>
        /// <param name="ignoreSettlement">the camp being placed, so it doesn't count as being in its own way</param>
        internal static bool IsGoodHideoutPosition(CampaignVec2 position, Settlement? ignoreSettlement = null)
        {
            if (!IsValidLandPosition(position) || IsOnHill(position))
                return false;
            float townDistanceSq = GetNearestNonHideoutSettlementDistanceSquared(position, ignoreSettlement);
            if (townDistanceSq < MinHideoutDistanceFromSettlement * MinHideoutDistanceFromSettlement
                || townDistanceSq > MaxHideoutDistanceFromSettlement * MaxHideoutDistanceFromSettlement)
                return false;
            float spacingSq = GetNearestSettlementDistanceSquared(position, ignoreSettlement);
            return spacingSq >= MinSpacingFromOtherSettlements * MinSpacingFromOtherSettlements;
        }

        /// <summary>
        /// Same as IsGoodHideoutPosition, but the spot must also be on the home town's island with no water in between.
        /// </summary>
        private static bool IsGoodHideoutPositionForAnchor(CampaignVec2 position, CampaignVec2 anchor, Settlement? ignoreSettlement = null)
        {
            if (!IsValidLandPositionInternal(position, anchor) || IsOnHill(position))
                return false;
            // an island passes the land probes, but the line to the town crosses sea
            if (SegmentCrossesWater(anchor, position))
                return false;
            float townDistanceSq = GetNearestNonHideoutSettlementDistanceSquared(position, ignoreSettlement);
            if (townDistanceSq < MinHideoutDistanceFromSettlement * MinHideoutDistanceFromSettlement
                || townDistanceSq > MaxHideoutDistanceFromSettlement * MaxHideoutDistanceFromSettlement)
                return false;
            float spacingSq = GetNearestSettlementDistanceSquared(position, ignoreSettlement);
            return spacingSq >= MinSpacingFromOtherSettlements * MinSpacingFromOtherSettlements;
        }

        /// <summary>
        /// Samples the straight line between two points and checks if it goes over sea or wide water.
        /// With War Sails loaded the sea layer is asked too, since over the sea the land layer just says Plain.
        /// </summary>
        /// <returns>true if the line crosses water, false if not or if the check failed</returns>
        private static bool SegmentCrossesWater(CampaignVec2 a, CampaignVec2 b)
        {
            try
            {
                var wrapper = Campaign.Current.MapSceneWrapper;
                bool seaLayer = WarSails.Active;
                Vec2 va = a.ToVec2();
                Vec2 vb = b.ToVec2();
                return SegmentCrossesWater(va.X, va.Y, vb.X, vb.Y, (x, y) =>
                {
                    var point = new Vec2(x, y);
                    PathFaceRecord land = wrapper.GetFaceIndex(new CampaignVec2(point, true));
                    bool landValid = land.IsValid();
                    TerrainType landTerrain = landValid ? wrapper.GetFaceTerrainType(land) : TerrainType.Plain;
                    bool seaValid = !landValid && seaLayer && wrapper.GetFaceIndex(new CampaignVec2(point, false)).IsValid();
                    return ClassifyWater(landValid, landTerrain, seaValid);
                });
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Guard] water check failed between {a} and {b}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if two points are on the same navmesh island.
        /// </summary>
        /// <returns>true if they are, or if it can't tell</returns>
        internal static bool SameLandAs(CampaignVec2 a, CampaignVec2 b)
        {
            try
            {
                PathFaceRecord fa = Campaign.Current.MapSceneWrapper.GetFaceIndex(a);
                PathFaceRecord fb = Campaign.Current.MapSceneWrapper.GetFaceIndex(b);
                if (!fa.IsValid() || !fb.IsValid())
                    return true;
                return fa.FaceIslandIndex == fb.FaceIslandIndex;
            }
            catch (Exception e)
            {
                // can't tell, so don't throw the spot away over it
                IMFDiagnostics.Verbose($"[Guard] island check failed for {a} / {b}: {e.Message}");
                return true;
            }
        }

        /// <summary>
        /// Checks that parties can get to a point. If the nearest walkable point is far from the one we asked about,
        /// the point is on a small navmesh island nobody can reach.
        /// </summary>
        private static bool IsReachable(CampaignVec2 position)
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper == null)
                    return false;
                CampaignVec2 accessible = Campaign.Current.MapSceneWrapper.GetAccessiblePointNearPosition(in position, PassabilityProbeRadius);
                return accessible.ToVec2().DistanceSquared(position.ToVec2()) <= PassabilityAcceptanceDistance * PassabilityAcceptanceDistance;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries random spots around the anchor at camp distance and returns the first good, reachable one.
        /// </summary>
        /// <returns>the spot, or null after 90 failed tries</returns>
        private static CampaignVec2? FindRandomPositionNearAnchor(CampaignVec2 anchor, Settlement? ignoreSettlement = null)
        {
            if (Campaign.Current?.MapSceneWrapper == null)
                return null;
            for (int i = 0; i < RandomPlacementAttempts; i++)
            {
                float angle = MBRandom.RandomFloat * 2f * (float)Math.PI;
                float radius = MinHideoutDistanceFromSettlement
                    + MBRandom.RandomFloat * (MaxHideoutDistanceFromSettlement - MinHideoutDistanceFromSettlement);
                CampaignVec2 candidate = new CampaignVec2(
                    anchor.ToVec2() + new Vec2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius), true);
                if (!IsGoodHideoutPositionForAnchor(candidate, anchor, ignoreSettlement))
                    continue;
                if (!IsReachable(candidate))
                    continue;
                return candidate;
            }
            return null;
        }

        /// <summary>
        /// Backup for when random tries fail: asks the navmesh for walkable points at growing distances from the anchor
        /// and keeps the closest one that passes the camp checks.
        /// </summary>
        /// <returns>the spot, or null if none passed</returns>
        private static CampaignVec2? FindFallbackPositionNearAnchor(CampaignVec2 anchor, Settlement? ignoreSettlement = null)
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper == null)
                    return null;
                CampaignVec2 landAnchor = new CampaignVec2(anchor.ToVec2(), true);
                CampaignVec2? best = null;
                float bestDistanceSq = float.MaxValue;
                foreach (int radius in FallbackRadii)
                {
                    CampaignVec2 candidate = Campaign.Current.MapSceneWrapper.GetAccessiblePointNearPosition(in landAnchor, radius);
                    if (candidate.ToVec2().DistanceSquared(anchor.ToVec2()) < 1f)
                        continue;
                    if (!IsGoodHideoutPositionForAnchor(candidate, anchor, ignoreSettlement))
                        continue;
                    if (!IsReachable(candidate))
                        continue;
                    float distanceSq = candidate.ToVec2().DistanceSquared(anchor.ToVec2());
                    if (distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        best = candidate;
                    }
                }
                return best;
            }
            catch
            {
                // off the navmesh, nothing usable here
            }
            return null;
        }

        /// <summary>
        /// Places a camp when it activates. Settled camps keep their saved spot unless the ground stopped being usable,
        /// nomad camps move every time. Converted hideouts (see GeneratedHideouts) never move.
        /// </summary>
        internal static void PlaceOnActivation(MinorFactionHideout mfh)
        {
            Settlement settlement = mfh.Settlement;
            if (settlement == null || GeneratedHideouts.Contains(settlement.StringId))
                return;
            if (mfh.IsNomad)
                EnsureValidHideoutPosition(settlement, mfh.OwnerClan, reroll: true);
            else if (!mfh.HasSavedPosition || !IsUsableGround(settlement.Position))
                EnsureValidHideoutPosition(settlement, mfh.OwnerClan);
            if (!mfh.HasSavedPosition && IsUsableGround(settlement.Position))
                mfh.RememberPosition(settlement.Position.ToVec2());
        }

        private static bool IsUsableGround(CampaignVec2 position)
        {
            return IsValidLandPosition(position) && !IsOnHill(position);
        }

        /// <summary>
        /// Moves a camp to a good spot near its clan's home if where it is now isn't good.
        /// </summary>
        /// <param name="reroll">move even if the current spot is fine, used for nomads</param>
        /// <returns>true if the camp moved</returns>
        internal static bool EnsureValidHideoutPosition(Settlement settlement, Clan ownerClan, bool reroll = false)
        {
            try
            {
                if (settlement == null || Campaign.Current?.MapSceneWrapper == null)
                    return false;
                // converted hideouts never move, see IsUnplaceable
                if (GeneratedHideouts.Contains(settlement.StringId))
                    return false;
                CampaignVec2 current = settlement.Position;
                CampaignVec2 anchor = ResolveAnchor(settlement, ownerClan);
                // needs a home town for the island and water checks
                if (!reroll && (HasRealAnchor(settlement, ownerClan)
                    ? IsGoodHideoutPositionForAnchor(current, anchor, settlement)
                    : IsGoodHideoutPosition(current, settlement)))
                    return false;
                CampaignVec2? target = FindRandomPositionNearAnchor(anchor, settlement) ?? FindFallbackPositionNearAnchor(anchor, settlement);
                if (!target.HasValue)
                {
                    IMFDiagnostics.Info($"HideoutPlacement: no valid position found for {settlement.StringId}, leaving at {current}");
                    return false;
                }

                SetSettlementPosition(settlement, target.Value);
                SettlementVisualMFHideoutMapEntityPatch.MoveExistingEntity(settlement, target.Value, current);
                IMFDiagnostics.Verbose($"HideoutPlacement: moved {settlement.StringId} from {current} to {target.Value} (anchor {anchor})");
                return true;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("HideoutPlacement.EnsureValidHideoutPosition failed:", e);
                return false;
            }
        }

        private static readonly int[] SpawnRings = { 8, 12, 16, 22, 30, 40 };
        private static readonly int[] RankedSpawnRings = { 8, 12, 16, 22, 30 };
        private const int AnglesPerRing = 16;
        private const float MinSpawnDistanceFromHideout = 6f;

        /// <summary>
        /// Checks that a point is land a party can stand on and reach.
        /// </summary>
        internal static bool IsPassable(CampaignVec2 position)
        {
            return IsValidLandPositionInternal(position, null) && IsReachable(position);
        }

        /// <summary>
        /// Checks if a camp has nowhere usable to be, so IMFManager skips it when picking camps to activate.
        /// Converted hideouts count as unplaceable if they are on bad ground or on another island than their home.
        /// On an exception this says placeable, a clan on bad ground beats a clan with no camp.
        /// </summary>
        /// <param name="ownerClan">the clan that would own the camp, used to find its home</param>
        internal static bool IsUnplaceable(Settlement settlement, Clan? ownerClan)
        {
            try
            {
                if (settlement == null || Campaign.Current?.MapSceneWrapper == null)
                    return false;
                CampaignVec2 current = settlement.Position;
                if (GeneratedHideouts.Contains(settlement.StringId))
                {
                    bool hasHome = HasRealAnchor(settlement, ownerClan!);
                    return NeedsRelocation(IsPassable(current), hasHome, !hasHome || SameLandAs(current, ResolveAnchor(settlement, ownerClan!)));
                }
                if (IsValidLandPosition(current))
                    return false;
                CampaignVec2 anchor = ResolveAnchor(settlement, ownerClan);
                return FindRandomPositionNearAnchor(anchor, settlement) == null
                    && FindFallbackPositionNearAnchor(anchor, settlement) == null;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Guard] placement check failed for {settlement?.StringId}: {e.Message}");
                return false;
            }
        }

        private static CampaignVec2 RingPoint(CampaignVec2 center, float radius, double angle)
        {
            return new CampaignVec2(center.ToVec2() + new Vec2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius)), true);
        }

        // forest, mountains and dunes are walkable but too cramped to count as open
        private static readonly TerrainType[] OpenTerrains =
        {
            TerrainType.Plain, TerrainType.Steppe, TerrainType.Desert, TerrainType.Snow, TerrainType.RuralArea
        };

        private static readonly float[] OpenProbeRadii = { 6f, 12f };
        private const int OpenProbeDirections = 8;
        private const float PartyScanRadius = 70f;

        /// <summary>
        /// Finds a spot next to a camp where a party can appear. Tries the scored search first and falls back to the first passable spot.
        /// </summary>
        /// <param name="slot">number of the party being placed, turns the search so several parties don't land on the same spot</param>
        /// <param name="ignore">the camp itself, so it doesn't count as a nearby settlement</param>
        /// <param name="placing">the party being placed, it is left out of the crowding check and decides who counts as hostile</param>
        /// <param name="close">look right next to the camp first, used for the player's start</param>
        /// <returns>the spot, or null if nothing near the camp is passable</returns>
        internal static CampaignVec2? FindSpawnNear(CampaignVec2 hideoutPosition, int slot, Settlement? ignore, MobileParty? placing = null, bool close = false)
        {
            try
            {
                if (close)
                {
                    CampaignVec2? beside = FindBestSpawn(hideoutPosition, slot, ignore, placing, CloseSpawnRings, CloseMinSpawnDistance, true)
                        ?? FindBestSpawn(hideoutPosition, slot, ignore, placing, CloseSpawnRings, CloseMinSpawnDistance, false);
                    if (beside.HasValue)
                        return beside;
                }
                CampaignVec2? best = FindBestSpawn(hideoutPosition, slot, ignore, placing, RankedSpawnRings, MinSpawnDistanceFromHideout);
                if (best.HasValue)
                    return best;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("HideoutPlacement.FindBestSpawn failed, using first-fit:", e);
            }
            return FindFirstFitSpawn(hideoutPosition, slot, ignore);
        }

        // player start: rings from the camp outward, never on the camp itself
        private static readonly int[] CloseSpawnRings = { 5, 6, 8, 10, 12, 16, 22, 30 };
        private const float CloseMinSpawnDistance = 5f;
        private const int CenterProbeWeight = 4;

        /// <summary>
        /// Checks points on rings around the camp and returns the one with the best SpawnSpotScore.
        /// Stops early on a spot good enough to count as excellent.
        /// </summary>
        /// <param name="rings">ring radii to try, closest first</param>
        /// <param name="minDistanceFromHideout">points closer to the camp than this are skipped</param>
        /// <param name="shoreMargin">also require land all around the point, keeps the player off the shoreline</param>
        private static CampaignVec2? FindBestSpawn(CampaignVec2 hideoutPosition, int slot, Settlement? ignore, MobileParty? placing,
            int[] rings, float minDistanceFromHideout, bool shoreMargin = false)
        {
            if (Campaign.Current?.MapSceneWrapper == null)
                return null;
            Vec2 hideout2 = hideoutPosition.ToVec2();
            List<KeyValuePair<Vec2, bool>> parties = NearbyParties(hideout2, placing);
            PathFaceRecord hideoutFace = Campaign.Current.MapSceneWrapper.GetFaceIndex(hideoutPosition);

            CampaignVec2? best = null;
            double bestScore = double.NegativeInfinity;
            string bestInfo = "";
            int evaluated = 0;
            double start = slot * 0.9;
            foreach (int radius in rings)
            {
                for (int i = 0; i < AnglesPerRing; i++)
                {
                    CampaignVec2 candidate = RingPoint(hideoutPosition, radius, start + i * (2.0 * Math.PI / AnglesPerRing));
                    Vec2 c2 = candidate.ToVec2();
                    float distance = c2.Distance(hideout2);
                    if (distance < minDistanceFromHideout)
                        continue;
                    if (!IsPassable(candidate))
                        continue;
                    if (shoreMargin && !HasShoreMargin(candidate))
                        continue;
                    // Some maps report sea as land, the sea layer on the way from the camp tells them apart.
                    if (placing == MobileParty.MainParty && SegmentCrossesWater(hideoutPosition, candidate))
                        continue;
                    if (GetNearestSettlementDistanceSquared(candidate, ignore) < MinSpacingFromOtherSettlements * MinSpacingFromOtherSettlements)
                        continue;

                    float nearestParty = float.MaxValue;
                    float nearestHostile = float.MaxValue;
                    foreach (KeyValuePair<Vec2, bool> other in parties)
                    {
                        float d = c2.Distance(other.Key);
                        if (d < nearestParty)
                            nearestParty = d;
                        if (other.Value && d < nearestHostile)
                            nearestHostile = d;
                    }

                    int open = CountOpenProbes(candidate, out int total);
                    bool sameIsland = SameIsland(hideoutFace, candidate);
                    double score = SpawnSpotScore(open, total, nearestParty, nearestHostile, distance, sameIsland);
                    evaluated++;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                        bestInfo = $"open={open}/{total} nearestParty={Math.Min(nearestParty, 999f):F0} nearestHostile={Math.Min(nearestHostile, 999f):F0} dist={distance:F0} island={sameIsland} score={score:F0}";
                        if (IsExcellentSpawn(open, total, nearestHostile, sameIsland))
                        {
                            IMFDiagnostics.Verbose($"[Spawn] pick slot={slot} ({evaluated} spots, early out): {bestInfo}");
                            return best;
                        }
                    }
                }
            }
            if (best.HasValue)
                IMFDiagnostics.Verbose($"[Spawn] pick slot={slot} ({evaluated} spots): {bestInfo}");
            return best;
        }

        private const float ShoreMarginRadius = 1.5f;

        /// <summary>
        /// Checks that there is land in all 8 directions a short way from the spot.
        /// </summary>
        private static bool HasShoreMargin(CampaignVec2 spot)
        {
            Vec2 c = spot.ToVec2();
            for (int d = 0; d < 8; d++)
            {
                double angle = d * (Math.PI / 4.0);
                var p = new CampaignVec2(c + new Vec2((float)(Math.Cos(angle) * ShoreMarginRadius), (float)(Math.Sin(angle) * ShoreMarginRadius)), true);
                if (!IsValidLandPositionInternal(p, null))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Lists active parties near a point that aren't in a settlement.
        /// </summary>
        /// <returns>each party's position, paired with true if it is at war with the party being placed</returns>
        private static List<KeyValuePair<Vec2, bool>> NearbyParties(Vec2 around, MobileParty? placing)
        {
            var result = new List<KeyValuePair<Vec2, bool>>();
            IFaction? faction = placing?.MapFaction;
            float radiusSq = PartyScanRadius * PartyScanRadius;
            foreach (MobileParty party in MobileParty.All)
            {
                if (party == null || party == placing || !party.IsActive || party.CurrentSettlement != null)
                    continue;
                Vec2 p = party.GetPosition2D;
                if (p.DistanceSquared(around) > radiusSq)
                    continue;
                bool hostile = faction != null && party.MapFaction != null && FactionManager.IsAtWarAgainstFaction(faction, party.MapFaction);
                result.Add(new KeyValuePair<Vec2, bool>(p, hostile));
            }
            return result;
        }

        /// <summary>
        /// Counts how many points around a spot are open ground (plain, steppe, desert, snow, farmland). The center counts 4 times.
        /// </summary>
        /// <param name="total">how many probes were checked, with the center counted 4 times</param>
        /// <returns>how many of those were open</returns>
        private static int CountOpenProbes(CampaignVec2 center, out int total)
        {
            total = 0;
            int open = 0;
            Vec2 c = center.ToVec2();
            try
            {
                total += CenterProbeWeight;
                if (Campaign.Current.MapSceneWrapper.GetFaceIndex(center).IsValid()
                    && OpenTerrains.Contains(Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(center)))
                    open += CenterProbeWeight;
            }
            catch
            {
                // counts as not open
            }
            foreach (float radius in OpenProbeRadii)
            {
                for (int d = 0; d < OpenProbeDirections; d++)
                {
                    double angle = d * (2.0 * Math.PI / OpenProbeDirections);
                    CampaignVec2 p = new CampaignVec2(c + new Vec2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius)), true);
                    total++;
                    try
                    {
                        if (Campaign.Current.MapSceneWrapper.GetFaceIndex(p).IsValid()
                            && OpenTerrains.Contains(Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(p)))
                            open++;
                    }
                    catch
                    {
                        // past the map edge
                    }
                }
            }
            return open;
        }

        /// <summary>
        /// Checks if a point is on the same navmesh island as the camp. Returns true when it can't tell.
        /// </summary>
        private static bool SameIsland(PathFaceRecord hideoutFace, CampaignVec2 candidate)
        {
            try
            {
                if (!hideoutFace.IsValid())
                    return true;
                PathFaceRecord face = Campaign.Current.MapSceneWrapper.GetFaceIndex(candidate);
                return !face.IsValid() || face.FaceIslandIndex == hideoutFace.FaceIslandIndex;
            }
            catch
            {
                // can't tell, so no penalty
                return true;
            }
        }

        /// <summary>
        /// Simple backup spawn search: the first passable point on rings around the camp that isn't too close to a settlement.
        /// </summary>
        private static CampaignVec2? FindFirstFitSpawn(CampaignVec2 hideoutPosition, int slot, Settlement? ignore)
        {
            if (Campaign.Current?.MapSceneWrapper == null)
                return null;
            double start = slot * 0.9;
            foreach (int radius in SpawnRings)
            {
                for (int i = 0; i < AnglesPerRing; i++)
                {
                    CampaignVec2 candidate = RingPoint(hideoutPosition, radius, start + i * (2.0 * Math.PI / AnglesPerRing));
                    if (candidate.ToVec2().DistanceSquared(hideoutPosition.ToVec2()) < MinSpawnDistanceFromHideout * MinSpawnDistanceFromHideout)
                        continue;
                    if (!IsPassable(candidate))
                        continue;
                    if (GetNearestSettlementDistanceSquared(candidate, ignore) < MinSpacingFromOtherSettlements * MinSpacingFromOtherSettlements)
                        continue;
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks active camps once at game start and logs the ones on bad ground. They get moved on their next activation.
        /// </summary>
        internal static void ValidateAllHideouts()
        {
            try
            {
                if (Campaign.Current?.Settlements == null)
                    return;
                foreach (Settlement settlement in Campaign.Current.Settlements)
                {
                    if (settlement == null || settlement.SettlementComponent == null)
                        continue;
                    MinorFactionHideout? mfh = Helpers.GetMFHideout(settlement);
                    if (mfh == null || !mfh.IsActive || GeneratedHideouts.Contains(settlement.StringId))
                        continue;
                    if (!IsUsableGround(settlement.Position))
                        IMFDiagnostics.Verbose($"[Guard] {settlement.StringId} on bad ground at {settlement.Position}, fixed on next activation");
                }
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("HideoutPlacement.ValidateAllHideouts failed:", e);
            }
        }

        // the private setter also updates the locator, the gate moves with it
        private static readonly MethodInfo? PositionSetter = AccessTools.PropertySetter(typeof(Settlement), nameof(Settlement.Position));
        private static readonly MethodInfo? GatePositionSetter = AccessTools.PropertySetter(typeof(Settlement), nameof(Settlement.GatePosition));

        /// <summary>
        /// Moves a settlement and its gate to a new position. The game doesn't save settlement positions,
        /// so the camp also remembers it for the next load.
        /// </summary>
        internal static void SetSettlementPosition(Settlement settlement, CampaignVec2 newPosition)
        {
            PositionSetter?.Invoke(settlement, new object[] { newPosition });
            GatePositionSetter?.Invoke(settlement, new object[] { newPosition });
            settlement.CheckPositionsForMapChangeAndUpdateIfNeeded();
            // the game doesn't save settlement positions so we keep it in the hideout
            Helpers.GetMFHideout(settlement)?.RememberPosition(newPosition.ToVec2());
        }

        /// <summary>
        /// Keep the spot a camp came with only if parties can stand there and get home from it.
        /// </summary>
        /// <param name="passableHere">parties can stand at the camp's position</param>
        /// <param name="hasRealHome">the clan has a real home town to compare with</param>
        /// <param name="sameLandAsHome">the camp is on the same island as that home</param>
        /// <returns>true if the camp should move</returns>
        public static bool NeedsRelocation(bool passableHere, bool hasRealHome, bool sameLandAsHome) =>
            !passableHere || (hasRealHome && !sameLandAsHome);

        public const float MinPartyClearance = 5f;
        public const float HostileClearance = 20f;
        public const double SameIslandBonus = 50.0;
        public const double ClearOfHostileBonus = 30.0;
        public const double HostilePenaltyPerUnit = 3.0;
        public const double DistancePenaltyPerUnit = 1.0;
        public const double ExcellentOpenFraction = 0.9;

        /// <summary>
        /// Checks if a spawn spot is good enough to stop searching: mostly open ground, no enemies close and on the camp's island.
        /// </summary>
        /// <param name="openProbes">open probes from CountOpenProbes</param>
        /// <param name="totalProbes">all probes from CountOpenProbes</param>
        /// <param name="nearestHostile">distance to the closest enemy party</param>
        /// <param name="sameIsland">the spot is on the same island as the camp</param>
        public static bool IsExcellentSpawn(int openProbes, int totalProbes, float nearestHostile, bool sameIsland)
        {
            return totalProbes > 0
                && openProbes >= ExcellentOpenFraction * totalProbes
                && nearestHostile >= HostileClearance
                && sameIsland;
        }

        /// <summary>
        /// Scores a spawn spot. Open ground and being on the camp's island add points, enemies nearby and distance from the camp take them away.
        /// A spot right next to another party is never used.
        /// </summary>
        /// <param name="openProbes">open probes from CountOpenProbes</param>
        /// <param name="totalProbes">all probes from CountOpenProbes</param>
        /// <param name="nearestParty">distance to the closest party of any side</param>
        /// <param name="nearestHostile">distance to the closest enemy party</param>
        /// <param name="distanceFromHideout">distance from the spot to the camp</param>
        /// <param name="sameIsland">the spot is on the same island as the camp</param>
        /// <returns>the score, higher is better, negative infinity if the spot is too crowded</returns>
        public static double SpawnSpotScore(int openProbes, int totalProbes, float nearestParty, float nearestHostile,
            float distanceFromHideout, bool sameIsland)
        {
            if (nearestParty < MinPartyClearance)
                return double.NegativeInfinity;

            double openness = totalProbes > 0
                ? 100.0 * Math.Max(0, Math.Min(openProbes, totalProbes)) / totalProbes
                : 50.0;
            double hostile = nearestHostile >= HostileClearance
                ? ClearOfHostileBonus
                : -(HostileClearance - nearestHostile) * HostilePenaltyPerUnit;
            return openness + hostile - distanceFromHideout * DistancePenaltyPerUnit + (sameIsland ? SameIslandBonus : 0.0);
        }

        /// <summary>
        /// Tells what kind of water a map sample is. Over the sea the land layer says Plain, so the sea layer is asked there.
        /// </summary>
        /// <param name="landFaceValid">the point is on the land navmesh</param>
        /// <param name="terrain">terrain type on the land navmesh</param>
        /// <param name="seaFaceValid">the point is on the sea navmesh</param>
        internal static WaterKind ClassifyWater(bool landFaceValid, TerrainType terrain, bool seaFaceValid)
        {
            if (landFaceValid)
            {
                switch (terrain)
                {
                    case TerrainType.Lake:
                    case TerrainType.CoastalSea:
                    case TerrainType.OpenSea:
                    case TerrainType.SeaRestriction:
                        return WaterKind.Open;
                    case TerrainType.Water:
                        return WaterKind.Shallow;
                    default:
                        return WaterKind.None;
                }
            }
            return seaFaceValid ? WaterKind.Open : WaterKind.None;
        }

        /// <summary>
        /// Samples a line and checks if it crosses water. One open water sample, or minShallowRun shallow ones in a row, counts as crossing.
        /// </summary>
        /// <param name="classify">tells what kind of water is at a point</param>
        /// <param name="spacing">distance between samples</param>
        /// <param name="minShallowRun">how many shallow samples in a row count as a crossing</param>
        /// <param name="maxSamples">cap on samples for long lines</param>
        internal static bool SegmentCrossesWater(float ax, float ay, float bx, float by, Func<float, float, WaterKind> classify,
            float spacing = 2f, int minShallowRun = 3, int maxSamples = 64)
        {
            if (classify == null || spacing <= 0f)
                return false;
            float dx = bx - ax, dy = by - ay;
            double length = Math.Sqrt((double)dx * dx + (double)dy * dy);
            int samples = (int)Math.Max(8, Math.Min(maxSamples, Math.Ceiling(length / spacing)));
            return Crosses(ax, ay, bx, by, samples, classify, minShallowRun);
        }

        /// <summary>
        /// Walks the samples along the line for SegmentCrossesWater.
        /// </summary>
        private static bool Crosses(float ax, float ay, float bx, float by, int samples, Func<float, float, WaterKind> classify, int minShallowRun)
        {
            if (samples < 2)
                return false;
            int run = 0;
            for (int i = 1; i < samples; i++)
            {
                float t = i / (float)samples;
                WaterKind kind = classify(ax + (bx - ax) * t, ay + (by - ay) * t);
                if (kind == WaterKind.Open)
                    return true;
                run = kind == WaterKind.Shallow ? run + 1 : 0;
                if (run >= minShallowRun)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// What a point on a line counts as when checking for water crossings.
    /// </summary>
    internal enum WaterKind
    {
        None = 0,      // land, rivers, fords, beaches, swamps, bridges
        Shallow = 1,   // walkable shallows (30% slower), but wide ones still block
        Open = 2       // sea or lake, land parties can't cross
    }

    /// <summary>
    /// Checks once whether the War Sails DLC is loaded. There's no hard dependency on it.
    /// </summary>
    internal static class WarSails
    {
        internal const string ModuleId = "NavalDLC";

        private static bool? _active;

        internal static bool Active
        {
            get
            {
                if (!_active.HasValue)
                {
                    try
                    {
                        _active = TaleWorlds.Engine.Utilities.GetModulesNames()
                            .Any(m => string.Equals(m, ModuleId, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        _active = false;
                    }
                }
                return _active.Value;
            }
        }
    }
}

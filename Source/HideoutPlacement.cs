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
    // camps go near the clan's home town on reachable land, placed on activation, loading never moves them.
    // map lookups throw off the navmesh, those spots count as unusable unless marked otherwise
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

        internal static void ForgetAnchors() => Anchors.Clear();

        // In a loaded save the home can already be the active camp itself, that one doesn't count.
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

        private static Settlement? RegularHomeOf(Settlement hideout, Clan? ownerClan)
        {
            if (IsRegularHome(ownerClan?.HomeSettlement, hideout))
                return ownerClan!.HomeSettlement;
            if (IsRegularHome(ownerClan?.InitialHomeSettlement, hideout))
                return ownerClan!.InitialHomeSettlement;
            return null;
        }

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

        // some maps report open sea as valid, so land needs dry terrain on 3 of 4 probes
        internal static bool IsValidLandPosition(CampaignVec2 position)
        {
            return IsValidLandPositionInternal(position, null);
        }

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

        // A camp up a mountain passes the water checks but parties can't reach its icon.
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

        // walkable point far from where we asked = small unreachable navmesh island
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

        // settled camps keep their saved spot unless the ground stops being usable, nomads move
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

        internal static bool IsPassable(CampaignVec2 position)
        {
            return IsValidLandPositionInternal(position, null) && IsReachable(position);
        }

        // on an exception this says placeable, a clan on bad ground beats a clan with no camp
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

        // checks active camps once at game start, bad ones move on their next activation
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

        internal static void SetSettlementPosition(Settlement settlement, CampaignVec2 newPosition)
        {
            PositionSetter?.Invoke(settlement, new object[] { newPosition });
            GatePositionSetter?.Invoke(settlement, new object[] { newPosition });
            settlement.CheckPositionsForMapChangeAndUpdateIfNeeded();
            // the game doesn't save settlement positions so we keep it in the hideout
            Helpers.GetMFHideout(settlement)?.RememberPosition(newPosition.ToVec2());
        }

        // Keep the authored spot only if parties can stand there and get home from it.
        public static bool NeedsRelocation(bool passableHere, bool hasRealHome, bool sameLandAsHome) =>
            !passableHere || (hasRealHome && !sameLandAsHome);

        public const float MinPartyClearance = 5f;
        public const float HostileClearance = 20f;
        public const double SameIslandBonus = 50.0;
        public const double ClearOfHostileBonus = 30.0;
        public const double HostilePenaltyPerUnit = 3.0;
        public const double DistancePenaltyPerUnit = 1.0;
        public const double ExcellentOpenFraction = 0.9;

        public static bool IsExcellentSpawn(int openProbes, int totalProbes, float nearestHostile, bool sameIsland)
        {
            return totalProbes > 0
                && openProbes >= ExcellentOpenFraction * totalProbes
                && nearestHostile >= HostileClearance
                && sameIsland;
        }

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

        // over the sea the land layer says Plain, so ask the sea layer there
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

        // One open sample, or minShallowRun shallow ones in a row, counts as crossing water.
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

    internal enum WaterKind
    {
        None = 0,      // land, rivers, fords, beaches, swamps, bridges
        Shallow = 1,   // walkable shallows (30% slower), but wide ones still block
        Open = 2       // sea or lake, land parties can't cross
    }

    // No hard dependency on War Sails, we just check once if it's loaded.
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

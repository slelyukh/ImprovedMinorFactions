using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using SandBox;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Patches
{
    // lift party visuals onto the camp model, at terrain height they sink in
    [HarmonyPatch(typeof(MobilePartyVisual), nameof(MobilePartyVisual.GetVisualPosition))]
    internal static class PartyOverHideoutPatch
    {
        private const float Reach = 1.6f;            // how close to the camp (map units) before we look at lifting
        private const float RayStart = 6f;           // ray starts this far above the ground
        private const float RayEnd = 0.5f;           // and ends this far below
        private const float MaxLift = 3f;
        private const float LiftShare = 1f;
        private const float Clearance = 0.005f;
        private const float FollowPerSecond = 12f;   // how quickly the lift follows the surface, stops popping at the rock's edge
        private const float FixedLift = 0.5f;
        private const float FixedFullWithin = 0.5f;  // fixed lift is at full strength this close, then fades out to Reach

        private sealed class Smoothed
        {
            internal float Lift;
            internal double At;
        }

        private static readonly Dictionary<string, Smoothed> Parties = new Dictionary<string, Smoothed>();
        private static readonly Dictionary<string, bool> Hittable = new Dictionary<string, bool>();
        // the camp's own model and how high its center is, looked up once per camp
        private static readonly Dictionary<string, GameEntity?> ModelRoot = new Dictionary<string, GameEntity?>();
        private static readonly Dictionary<string, float> CentreLift = new Dictionary<string, float>();
        // visible mesh top, some maps have a taller collision box
        private static readonly Dictionary<string, float> VisibleTop = new Dictionary<string, float>();
        private const float CapSlack = 0f;
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        // native ray casts throw on some maps, after the first one use the fixed lift
        private static bool _rayBroken;
        private static List<Settlement>? _camps;
        private static Campaign? _campsFor;

        private static void Postfix(MobilePartyVisual __instance, ref Vec3 __result)
        {
            try
            {
                MobileParty? party = __instance.MapEntity?.MobileParty;
                if (party == null || party.CurrentSettlement != null)
                    return;
                if (_campsFor != Campaign.Current || _camps == null)
                {
                    _camps = new List<Settlement>();
                    foreach (Settlement s in Settlement.All)
                        if (Helpers.IsMFHideout(s))
                            _camps.Add(s);
                    _campsFor = Campaign.Current;
                    Parties.Clear();
                    Hittable.Clear();
                    ModelRoot.Clear();
                    CentreLift.Clear();
                    VisibleTop.Clear();
                    Footprint.Clear();
                }
                Vec2 at = __result.AsVec2;
                foreach (Settlement camp in _camps)
                {
                    MinorFactionHideout? mfh = Helpers.GetMFHideout(camp);
                    if (mfh == null || !mfh.IsActive)
                        continue;
                    float d = (camp.GetPosition2D - at).Length;
                    if (d > Reach)
                        continue;
                    float target = TargetLift(camp, __instance, at, __result.z, d);
                    __result.z += Follow(party.StringId, target);
                    return;
                }
                Parties.Remove(party.StringId);
            }
            catch (Exception e)
            {
                IMFDiagnostics.LogOnce("PartyOverHideoutPatch failed:", e);
            }
        }

        private static float TargetLift(Settlement camp, MobilePartyVisual visual, Vec2 at, float groundZ, float distanceToCamp)
        {
            Scene? scene = (Campaign.Current.MapSceneWrapper as MapScene)?.Scene;
            if (scene == null)
                return 0f;
            if (!Hittable.TryGetValue(camp.StringId, out bool hittable))
            {
                hittable = ProbeCentre(scene, camp, visual);
                Hittable[camp.StringId] = hittable;
            }
            if (!hittable || _rayBroken)
            {
                float ramp = distanceToCamp <= FixedFullWithin ? 1f : (Reach - distanceToCamp) / (Reach - FixedFullWithin);
                return FixedLift * ramp;
            }
            // The collision covers the trees too, so off the rock the party stays on the ground.
            Footprint.TryGetValue(camp.StringId, out Area? area);
            float dome = area?.Dome(at) ?? 1f;
            if (dome <= 0f)
                return 0f;
            ModelRoot.TryGetValue(camp.StringId, out GameEntity? model);
            float height = MedianHeight(scene, at.x, at.y, groundZ, visual.StrategicEntity, model);
            if (height <= 0f)
                return 0f;
            float lift = Math.Max(0f, Math.Min(MaxLift, height * LiftShare + Clearance));
            if (CentreLift.TryGetValue(camp.StringId, out float centre))
                lift = Math.Min(lift, centre + CapSlack);
            if (VisibleTop.TryGetValue(camp.StringId, out float visible))
                lift = Math.Min(lift, area != null ? visible * dome : visible);
            return lift;
        }

        private static bool ProbeCentre(Scene scene, Settlement camp, MobilePartyVisual visual)
        {
            Vec2 centre = camp.GetPosition2D;
            float ground = scene.GetGroundHeightAtPosition(new Vec3(centre.x, centre.y, 0f));
            bool hit = Cast(scene, centre.x, centre.y, ground, visual.StrategicEntity, out _, out GameEntity? ent);
            if (hit)
            {
                ModelRoot[camp.StringId] = ent != null ? CampModelOf(ent, camp.StringId) : null;
                CentreLift[camp.StringId] = Math.Max(0f, MedianHeight(scene, centre.x, centre.y, ground, visual.StrategicEntity, ModelRoot[camp.StringId]));
                float meshTop = ent != null ? SolidTop(CampModelOf(ent, camp.StringId), ground, camp.StringId) : 0f;
                if (meshTop <= 0f && ent != null)
                    meshTop = MeshTop(ent, ground);
                if (meshTop > 0f)
                    VisibleTop[camp.StringId] = meshTop;
            }
            return hit;
        }

        // Foliage on the rock, the party should stand on the rock and not the treetops.
        private static readonly string[] SoftParts = { "tree", "bush", "leaf", "plant", "grass", "flag", "banner" };

        private static bool IsSoft(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string n = name!.ToLowerInvariant();
            foreach (string s in SoftParts)
                if (n.Contains(s))
                    return true;
            return false;
        }

        private static float SolidTop(GameEntity root, float groundZ, string campId)
        {
            float top = float.MinValue;
            var area = new Area();
            try
            {
                CollectSolidTop(root, ref top, area, 0);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("PartyOverHideoutPatch: reading the camp's meshes failed:", e);
                return 0f;
            }
            float height = top - groundZ;
            bool ok = top > float.MinValue && height > 0.05f && height < 20f;
            if (ok && area.Valid)
                Footprint[campId] = area;
            return ok ? height : 0f;
        }

        private sealed class Area
        {
            internal float MinX = float.MaxValue, MaxX = float.MinValue, MinY = float.MaxValue, MaxY = float.MinValue;
            internal bool Valid => MaxX - MinX > 0.05f && MaxY - MinY > 0.05f && MaxX - MinX < 20f && MaxY - MinY < 20f;

            internal void Add(Vec3 p)
            {
                MinX = Math.Min(MinX, p.x);
                MaxX = Math.Max(MaxX, p.x);
                MinY = Math.Min(MinY, p.y);
                MaxY = Math.Max(MaxY, p.y);
            }

            internal float Dome(Vec2 at)
            {
                float rx = (MaxX - MinX) / 2f, ry = (MaxY - MinY) / 2f;
                float nx = (at.x - (MinX + MaxX) / 2f) / rx, ny = (at.y - (MinY + MaxY) / 2f) / ry;
                float r2 = nx * nx + ny * ny;
                return r2 >= 1f ? 0f : (float)Math.Sqrt(1f - r2);
            }
        }

        private static readonly Dictionary<string, Area> Footprint = new Dictionary<string, Area>();

        private static void CollectSolidTop(GameEntity entity, ref float top, Area area, int depth)
        {
            if (depth > 6)
                return;
            MatrixFrame entityFrame = entity.GetGlobalFrame();
            bool entitySoft = IsSoft(entity.Name);
            for (int i = 0; i < entity.MultiMeshComponentCount; i++)
            {
                MetaMesh metaMesh = entity.GetMetaMesh(i);
                if (metaMesh == null)
                    continue;
                bool metaSoft = entitySoft || IsSoft(metaMesh.GetName());
                MatrixFrame metaFrame = entityFrame.TransformToParent(metaMesh.Frame);
                for (int j = 0; j < metaMesh.MeshCount; j++)
                {
                    Mesh mesh = metaMesh.GetMeshAtIndex(j);
                    if (mesh == null)
                        continue;
                    if (metaSoft || IsSoft(mesh.Name) || IsSoft(mesh.GetMaterial()?.Name))
                        continue;
                    Vec3 min = mesh.GetBoundingBoxMin();
                    Vec3 max = mesh.GetBoundingBoxMax();
                    MatrixFrame meshFrame = metaFrame.TransformToParent(mesh.GetLocalFrame());
                    foreach (float x in new[] { min.x, max.x })
                        foreach (float y in new[] { min.y, max.y })
                            foreach (float z in new[] { min.z, max.z })
                            {
                                Vec3 corner = meshFrame.TransformToParent(new Vec3(x, y, z));
                                top = Math.Max(top, corner.z);
                                area.Add(corner);
                            }
                }
            }
            foreach (GameEntity child in entity.GetChildren())
                CollectSolidTop(child, ref top, area, depth + 1);
        }

        private static float MeshTop(GameEntity entity, float groundZ)
        {
            try
            {
                float top = entity.GetGlobalBoundingBox().max.z - groundZ;
                return top > 0.05f && top < 20f ? top : 0f;
            }
            catch
            {
                return 0f;
            }
        }

        private static bool Cast(Scene scene, float x, float y, float groundZ, GameEntity? ignore, out float surfaceZ)
            => Cast(scene, x, y, groundZ, ignore, out surfaceZ, out _);

        // The camp's own model, never the top of the tree (that can be a group holding a whole village)
        private static GameEntity CampModelOf(GameEntity e, string campId)
        {
            GameEntity? walk = e;
            for (int i = 0; i < 16 && walk != null; i++)
            {
                if (walk.Name == campId)
                    return walk;
                walk = walk.Parent;
            }
            return e;
        }

        private static bool IsPartOf(GameEntity hit, GameEntity model)
        {
            GameEntity? walk = hit;
            for (int i = 0; i < 16 && walk != null; i++)
            {
                if (walk.Pointer == model.Pointer)
                    return true;
                walk = walk.Parent;
            }
            return false;
        }

        private static readonly float[,] Around = { { 0f, 0f }, { 0.12f, 0f }, { -0.12f, 0f }, { 0f, 0.12f }, { 0f, -0.12f } };

        private static float MedianHeight(Scene scene, float x, float y, float groundZ, GameEntity? ignore, GameEntity? model)
        {
            var heights = new float[Around.GetLength(0)];
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = CastModel(scene, x + Around[i, 0], y + Around[i, 1], groundZ, ignore, model, out float surface)
                    ? Math.Max(0f, surface - groundZ)
                    : 0f;
            }
            Array.Sort(heights);
            return heights[heights.Length / 2];
        }

        private static bool CastModel(Scene scene, float x, float y, float groundZ, GameEntity? ignore, GameEntity? model, out float surfaceZ)
        {
            surfaceZ = groundZ;
            try
            {
                float top = groundZ + RayStart;
                float bottom = groundZ - RayEnd;
                for (int step = 0; step < 5 && top > bottom; step++)
                {
                    Vec3 from = new Vec3(x, y, top);
                    Vec3 to = new Vec3(x, y, bottom);
                    WeakGameEntity ignored = ignore != null ? ignore.WeakEntity : default;
                    bool hit = scene.RayCastForClosestEntityOrTerrainIgnoreEntity(in from, in to, ignored, out float distance, out GameEntity hitEntity, 0.01f, BodyFlags.None);
                    if (!hit || hitEntity == null)
                        return false;
                    float z = from.z - distance;
                    if (model == null || IsPartOf(hitEntity, model))
                    {
                        surfaceZ = z;
                        return true;
                    }
                    top = z - 0.02f;
                }
                return false;
            }
            catch (Exception e)
            {
                if (!_rayBroken)
                    IMFDiagnostics.Log("PartyOverHideoutPatch: the ray cast failed, using the fixed lift:", e);
                _rayBroken = true;
                return false;
            }
        }

        private static bool Cast(Scene scene, float x, float y, float groundZ, GameEntity? ignore, out float surfaceZ, out GameEntity? hitEntityOut)
        {
            hitEntityOut = null;
            surfaceZ = groundZ;
            try
            {
                Vec3 from = new Vec3(x, y, groundZ + RayStart);
                Vec3 to = new Vec3(x, y, groundZ - RayEnd);
                WeakGameEntity ignored = ignore != null ? ignore.WeakEntity : default;
                bool hit = scene.RayCastForClosestEntityOrTerrainIgnoreEntity(in from, in to, ignored, out float distance, out GameEntity hitEntity, 0.01f, BodyFlags.None);
                surfaceZ = from.z - distance;
                hitEntityOut = hitEntity;
                return hit && hitEntity != null;
            }
            catch (Exception e)
            {
                if (!_rayBroken)
                    IMFDiagnostics.Log("PartyOverHideoutPatch: the ray cast failed, using the fixed lift:", e);
                _rayBroken = true;
                return false;
            }
        }

        private static float Follow(string partyId, float target)
        {
            double now = Clock.Elapsed.TotalSeconds;
            if (!Parties.TryGetValue(partyId, out Smoothed? state))
            {
                Parties[partyId] = new Smoothed { Lift = target, At = now };
                return target;
            }
            double dt = Math.Min(0.25, now - state.At);
            state.At = now;
            state.Lift += (target - state.Lift) * (float)(1.0 - Math.Exp(-FollowPerSecond * dt));
            return state.Lift;
        }
    }
}

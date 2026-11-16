using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SandBox;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Gives camps made at load time a map model. The game expects a map icon for every settlement and throws without one,
    /// so we make one from the hideout icon that fits the camp's culture.
    /// </summary>
    [HarmonyPatch(typeof(SettlementVisual), "OnStartup")]
    public class SettlementVisualMFHideoutMapEntityPatch
    {
        private static readonly string[] HideoutPrefabVariants =
        {
            "map_icons_hideout_desert",
            "map_icons_hideout_forest",
            "map_icons_hideout_seaside",
            "map_icons_hideout_mountain",
        };

        // scale and flags copied from the map icons, simulated physics on these crashed
        private static readonly Vec3 HideoutIconScale = new Vec3(0.598f, 0.598f, 0.598f);

        static void Prefix(SettlementVisual __instance)
        {
            try
            {
                PartyBase party = __instance.MapEntity;
                if (party == null || !party.IsSettlement)
                    return;
                Settlement settlement = party.Settlement;
                if (!(settlement.SettlementComponent is MinorFactionHideout mfh))
                    return;
                if (mfh.OwnerClan == null)
                    return;

                AlignGate(settlement);
                Scene scene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
                if (scene == null)
                    return;
                GameEntity existing = scene.GetCampaignEntityWithName(settlement.StringId);
                if (existing != null)
                {
                    // A model that came with the map, the camp may have moved since the map was made.
                    Known[settlement.StringId] = existing;
                    Vec2 modelAt = existing.GetGlobalFrame().origin.AsVec2;
                    if ((modelAt - settlement.Position.ToVec2()).Length > 0.5f)
                    {
                        PositionHideoutEntity(scene, existing, settlement.Position);
                        IMFDiagnostics.Verbose("[Camp] " + settlement.StringId + ": map model brought to the camp at start-up (was " + (modelAt - settlement.Position.ToVec2()).Length.ToString("F1") + " units away)");
                    }
                    return;
                }

                GameEntity created = CreateHideoutMapEntity(scene, settlement);
                if (created == null)
                {
                    IMFDiagnostics.Info("MFHideout map entity creation FAILED for " + settlement.StringId + " - vanilla OnStartup will crash");
                    return;
                }
                created.Name = settlement.StringId;
                Known[settlement.StringId] = created;

                CampaignVec2 position = settlement.Position;
                PositionHideoutEntity(scene, created, position);
                IMFDiagnostics.Verbose("MFHideout map entity created: " + settlement.StringId + " at " + position + " prefab=" + created.GetPrefabName());
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("SettlementVisualMFHideoutMapEntityPatch failed:", e);
            }
        }

        // vanilla only makes an empty entity for the mouse, add the visible model too or hover/click do nothing
        static void Postfix(SettlementVisual __instance)
        {
            try
            {
                Settlement? settlement = __instance.MapEntity?.Settlement;
                if (!(settlement?.SettlementComponent is MinorFactionHideout)
                    || !Known.TryGetValue(settlement.StringId, out GameEntity model)
                    || model == null || model == __instance.StrategicEntity)
                    return;
                var visuals = AccessTools.PropertyGetter(typeof(MapScreen), "VisualsOfEntities")?.Invoke(null, null)
                    as Dictionary<UIntPtr, MapEntityVisual>;
                if (visuals == null)
                    return;
                var parts = new List<GameEntity> { model };
                model.GetChildrenRecursive(ref parts);
                foreach (GameEntity part in parts)
                {
                    if (!visuals.ContainsKey(part.Pointer))
                        visuals.Add(part.Pointer, __instance);
                }
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("Registering the camp model for the mouse failed:", e);
            }
        }

        /// <summary>
        /// For generated camps, puts the gate on the camp's position when that ground is passable.
        /// </summary>
        private static void AlignGate(Settlement settlement)
        {
            try
            {
                if (!GeneratedHideouts.Contains(settlement.StringId))
                    return;
                CampaignVec2 gate = settlement.GatePosition;
                CampaignVec2 position = settlement.Position;
                if (gate.NearlyEquals(position, 0.01f) || !HideoutPlacement.IsPassable(position))
                    return;
                AccessTools.PropertySetter(typeof(Settlement), "GatePosition")?.Invoke(settlement, new object[] { position });
                IMFDiagnostics.Verbose("[Camp] " + settlement.StringId + ": go-to spot moved from the authored gate to the camp's position");
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("AlignGate failed for " + settlement?.StringId + ":", e);
            }
        }
        private static GameEntity CreateHideoutMapEntity(Scene scene, Settlement settlement)
        {
            string preferred = GetVariantForCulture(settlement.Culture?.StringId);
            GameEntity created = GameEntity.Instantiate(scene, preferred, false);
            if (created != null)
            {
                ApplyVanillaHideoutProperties(created);
                return created;
            }
            foreach (string variant in HideoutPrefabVariants)
            {
                if (variant == preferred)
                    continue;
                created = GameEntity.Instantiate(scene, variant, false);
                if (created != null)
                {
                    ApplyVanillaHideoutProperties(created);
                    return created;
                }
            }
            return null;
        }

        private static void ApplyVanillaHideoutProperties(GameEntity entity)
        {
            MatrixFrame frame = entity.GetFrame();
            frame.rotation.ApplyScaleLocal(in HideoutIconScale);
            entity.SetFrame(ref frame);
            if ((entity.BodyFlag & BodyFlags.Dynamic) != 0)
                entity.ConvertDynamicBodyToRayCast();
            // dynamic bodies become raycast only so they aren't simulated, static ones stay
        }

        private static string GetVariantForCulture(string cultureId)
        {
            switch (cultureId)
            {
                case "aserai":
                case "khuzait":
                    return "map_icons_hideout_desert";
                case "sturgia":
                    return "map_icons_hideout_seaside";
                default:
                    return "map_icons_hideout_forest";
            }
        }

        // entities are kept by settlement id, name lookup misses runtime ones
        private static readonly System.Collections.Generic.Dictionary<string, GameEntity> Known = new System.Collections.Generic.Dictionary<string, GameEntity>();

        internal static void ForgetModels() => Known.Clear();

        /// <summary>
        /// Moves a camp's map model to its new position after the camp moved.
        /// </summary>
        internal static void MoveExistingEntity(Settlement settlement, CampaignVec2 position, CampaignVec2? oldPosition = null)
        {
            try
            {
                Scene scene = ((MapScene)Campaign.Current.MapSceneWrapper)?.Scene;
                if (scene == null)
                    return;
                GameEntity entity = scene.GetCampaignEntityWithName(settlement.StringId)
                    ?? scene.FindEntityWithName(settlement.StringId)
                    ?? scene.GetFirstEntityWithName(settlement.StringId);
                string how = "by name";
                if (entity == null && Known.TryGetValue(settlement.StringId, out entity))
                    how = "from the entities this mod created";
                if (entity == null && oldPosition.HasValue)
                {
                    var all = new System.Collections.Generic.List<GameEntity>();
                    scene.GetEntities(ref all);
                    float best = 2.5f;
                    foreach (GameEntity e in all)
                    {
                        string label = (e.GetPrefabName() ?? "") + " " + (e.Name ?? "");
                        if (label.IndexOf("hideout", StringComparison.OrdinalIgnoreCase) < 0 && label.IndexOf("bandit", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        float d = (e.GetGlobalFrame().origin.AsVec2 - oldPosition.Value.ToVec2()).Length;
                        if (d < best) { best = d; entity = e; }
                    }
                    how = "standing on its old spot";
                }
                if (entity == null)
                    IMFDiagnostics.Verbose("[Camp] " + settlement.StringId + ": its map model was not found, it stays where it is");
                else
                {
                    PositionHideoutEntity(scene, entity, position);
                    IMFDiagnostics.Verbose("[Camp] " + settlement.StringId + ": map model moved with the camp (" + how + ")");
                }
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MoveExistingEntity failed for " + settlement?.StringId + ":", e);
            }
        }

        private static void PositionHideoutEntity(Scene scene, GameEntity entity, CampaignVec2 position)
        {
            Vec3 pos = position.AsVec3();
            pos.z = scene.GetGroundHeightAtPosition(pos);
            entity.SetLocalPosition(pos);
            // The map finds what's under the mouse by bounds, those stay behind unless recomputed.
            entity.RecomputeBoundingBox();
            entity.UpdateGlobalBounds();
        }
    }

    /// <summary>
    /// Gives camps made at load time a map level, without one they disappear at some zoom levels.
    /// </summary>
    [HarmonyPatch(typeof(SettlementVisual), "RefreshLevelMask")]
    public class MFHideoutLevelMaskPatch
    {
        private static readonly AccessTools.FieldRef<SettlementVisual, uint> CurrentLevelMask =
            AccessTools.FieldRefAccess<SettlementVisual, uint>("_currentLevelMask");
        private static readonly Action<SettlementVisual, uint> SetLevelMask =
            AccessTools.MethodDelegate<Action<SettlementVisual, uint>>(AccessTools.Method(typeof(SettlementVisual), "SetLevelMask"));

        static void Postfix(SettlementVisual __instance)
        {
            try
            {
                if (!(__instance.MapEntity?.Settlement?.SettlementComponent is MinorFactionHideout))
                    return;
                uint level = Campaign.Current.MapSceneWrapper.GetSceneLevel("level_1");
                uint mask = CurrentLevelMask(__instance);
                if ((mask & level) != level)
                    SetLevelMask(__instance, mask | level);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFHideoutLevelMaskPatch failed:", e);
            }
        }
    }
}

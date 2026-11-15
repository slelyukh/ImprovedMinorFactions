using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using Utilities = TaleWorlds.Engine.Utilities;

namespace ImprovedMinorFactions
{
    internal static class HideoutScenes
    {
        private static List<string>? _pool;

        private static List<string> Pool() =>
            _pool ??= SceneBiomes.KnownScenes.Where(ConfigLoader.SceneExists).Concat(SceneCatalog.Hideouts()).Distinct().ToList();

        internal static HideoutTerrain Sample(CampaignVec2 center)
        {
            var wrapper = Campaign.Current.MapSceneWrapper;
            Vec2 c = center.ToVec2();
            bool seaLayer = WarSails.Active;
            bool coastal = false;
            var dry = new List<string>();
            foreach ((float radius, int count) in new[] { (0f, 1), (4f, 12), (9f, 16) })
            {
                for (int i = 0; i < count; i++)
                {
                    double angle = i * 2.0 * Math.PI / count;
                    var point = new Vec2(c.X + (float)(Math.Cos(angle) * radius), c.Y + (float)(Math.Sin(angle) * radius));
                    PathFaceRecord land = wrapper.GetFaceIndex(new CampaignVec2(point, true));
                    bool landValid = land.IsValid();
                    TerrainType terrain = landValid ? wrapper.GetFaceTerrainType(land) : TerrainType.Plain;
                    bool seaValid = !landValid && seaLayer && wrapper.GetFaceIndex(new CampaignVec2(point, false)).IsValid();
                    WaterKind water = HideoutPlacement.ClassifyWater(landValid, terrain, seaValid);
                    if (water == WaterKind.Open || (landValid && terrain == TerrainType.Beach))
                        coastal = true;
                    if (landValid && water != WaterKind.Open)
                        dry.Add(terrain.ToString());
                }
            }
            return SceneBiomes.Classify(dry, coastal);
        }

        internal static string Resolve(MinorFactionHideout hideout, out HideoutTerrain terrain)
        {
            terrain = default;
            string current = hideout.SceneName ?? "";
            Settlement? settlement = hideout.Settlement;
            if (settlement == null || Campaign.Current?.MapSceneWrapper == null)
                return current;

            terrain = Sample(settlement.Position);
            string clanId = hideout.OwnerClan?.StringId ?? "";
            IReadOnlyList<string>? own = clanId.Length > 0 ? ConfigLoader.View?.Scenes(clanId) : null;
            return SceneBiomes.Choose(current, own, Pool(), terrain, StableHash(settlement.StringId));
        }

        private static List<string>? _nomadPool;

        private static List<string> NomadPool() =>
            _nomadPool ??= SceneBiomes.KnownNomadVillageScenes.Where(ConfigLoader.SceneExists).Concat(SceneCatalog.Villages()).Distinct().ToList();

        internal static string ResolveNomadVillage(MinorFactionHideout hideout)
        {
            Settlement? settlement = hideout.Settlement;
            if (settlement == null || Campaign.Current?.MapSceneWrapper == null)
                return "";
            HideoutTerrain terrain = Sample(settlement.Position);
            string scene = SceneBiomes.ChooseNomadVillageScene(terrain, StableHash(settlement.StringId), NomadPool());
            IMFDiagnostics.Verbose($"[Scene] nomad camp {settlement.StringId} ({hideout.OwnerClan?.StringId}) stands on {terrain}: battle scene '{scene}' (village)");
            return scene;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 17;
                foreach (char ch in text ?? "")
                    hash = hash * 31 + ch;
                return hash;
            }
        }
    }

    // scenes have no terrain info, so sort them by name (prefixes, then the word lists)
    internal enum SceneFamily { Unknown, Coastal, Forest, Mountain, Desert, Steppe }

    internal enum Ground { Plain, Desert, Forest, Mountain, Steppe, Snow, Swamp }

    internal readonly struct HideoutTerrain
    {
        public HideoutTerrain(bool coastal, Ground ground)
        {
            Coastal = coastal;
            Ground = ground;
        }

        public bool Coastal { get; }     // sea, lake or beach a few map units away
        public Ground Ground { get; }    // most common dry terrain around it

        public override string ToString() => (Coastal ? "coastal " : "landlocked ") + Ground;
    }

    internal static class SceneBiomes
    {
        internal static readonly string[] KnownScenes = new[]
        {
            "sea_bandit_a", "sea_bandit_b", "sea_bandit_c", "sea_bandit_d",
            "forest_hideout_003", "forest_hideout_004", "bandit_forest",
            "mountain_hideout_002", "mountain_hideout_004",
            "desert_hideout_002", "desert_hideout_004",
            "hideout_steppe_001", "hideout_steppe_002"
        }.SelectMany(n => new[] { n, n + "_sv" }).ToArray();

        private static readonly string[] SteppeVillages = { "khuzait_village_b", "khuzait_village_c", "khuzait_village_d", "khuzait_village_e", "khuzait_village_f", "khuzait_village_g", "khuzait_village_h" };
        private static readonly string[] DesertVillages = { "aserai_village_b", "aserai_village_c", "aserai_village_d", "aserai_village_g", "aserai_village_k", "aserai_village_m" };
        private static readonly string[] ForestVillages = { "battania_village", "battania_village_c", "battania_village_e", "battania_village_f", "battania_village_g", "battania_village_m" };
        private static readonly string[] ColdVillages = { "sturgia_village_a", "sturgia_village_b", "sturgia_village_c", "sturgia_village_d", "sturgia_village_k" };
        private static readonly string[] PlainVillages = { "empire_village_a", "empire_village_b", "empire_village_c", "vlandia_village_b", "vlandia_village_c" };

        internal static readonly string[] KnownNomadVillageScenes =
            SteppeVillages.Concat(DesertVillages).Concat(ForestVillages).Concat(ColdVillages).Concat(PlainVillages).ToArray();

        internal static string ChooseNomadVillageScene(HideoutTerrain terrain, int tieBreak, IReadOnlyList<string> available)
        {
            if (available == null || available.Count == 0)
                return "";

            string[] preferred;
            switch (terrain.Ground)
            {
                case Ground.Desert:
                    preferred = DesertVillages;
                    break;
                case Ground.Forest:
                case Ground.Swamp:
                    preferred = ForestVillages;
                    break;
                case Ground.Snow:
                case Ground.Mountain:
                    preferred = ColdVillages;
                    break;
                case Ground.Plain:
                    preferred = SteppeVillages.Concat(PlainVillages).ToArray();
                    break;
                default:
                    preferred = SteppeVillages;
                    break;
            }

            var hits = available.Where(s => preferred.Contains(s) || VillageGround(s) == terrain.Ground
                || (terrain.Ground == Ground.Plain && VillageGround(s) == Ground.Steppe)
                || (terrain.Ground == Ground.Mountain && VillageGround(s) == Ground.Snow)
                || (terrain.Ground == Ground.Swamp && VillageGround(s) == Ground.Forest)).ToList();
            if (hits.Count == 0)
                hits = available.ToList();
            return hits[(int)((uint)tieBreak % (uint)hits.Count)];
        }

        private static readonly string[] CoastalWords = { "sea", "coast", "coastal", "pirate", "harbor", "dock", "shore", "beach", "island" };
        private static readonly string[] ForestWords = { "forest", "wood", "woods", "jungle" };
        private static readonly string[] MountainWords = { "mountain", "mountains", "cliff", "rock", "rocky", "cave", "hill", "hills" };
        private static readonly string[] DesertWords = { "desert", "dune", "dunes", "sand", "canyon" };
        private static readonly string[] SteppeWords = { "steppe", "plain", "plains", "meadow" };

        private static readonly string[] DesertVillageWords = { "desert", "aserai", "sand", "dune" };
        private static readonly string[] ForestVillageWords = { "forest", "battania", "wood", "woods", "swamp", "marsh" };
        private static readonly string[] ColdVillageWords = { "snow", "sturgia", "ice", "highland", "north" };
        private static readonly string[] SteppeVillageWords = { "steppe", "khuzait", "nomad" };
        private static readonly string[] PlainVillageWords = { "empire", "vlandia", "plain", "plains", "farm", "meadow" };

        internal static Ground? VillageGround(string? scene)
        {
            string[] w = SceneContent.Words(scene);
            if (w.Any(x => DesertVillageWords.Contains(x))) return Ground.Desert;
            if (w.Any(x => ForestVillageWords.Contains(x))) return Ground.Forest;
            if (w.Any(x => ColdVillageWords.Contains(x))) return Ground.Snow;
            if (w.Any(x => SteppeVillageWords.Contains(x))) return Ground.Steppe;
            if (w.Any(x => PlainVillageWords.Contains(x))) return Ground.Plain;
            return null;
        }

        internal static SceneFamily FamilyOf(string? scene)
        {
            string s = (scene ?? "").Trim().ToLowerInvariant();
            if (s.StartsWith("sea_bandit", StringComparison.Ordinal)) return SceneFamily.Coastal;
            if (s.StartsWith("forest_hideout", StringComparison.Ordinal) || s.StartsWith("bandit_forest", StringComparison.Ordinal)) return SceneFamily.Forest;
            if (s.StartsWith("mountain_hideout", StringComparison.Ordinal)) return SceneFamily.Mountain;
            if (s.StartsWith("desert_hideout", StringComparison.Ordinal)) return SceneFamily.Desert;
            if (s.StartsWith("hideout_steppe", StringComparison.Ordinal)) return SceneFamily.Steppe;

            string[] w = SceneContent.Words(s);
            if (w.Any(x => CoastalWords.Contains(x))) return SceneFamily.Coastal;
            if (w.Any(x => ForestWords.Contains(x))) return SceneFamily.Forest;
            if (w.Any(x => MountainWords.Contains(x))) return SceneFamily.Mountain;
            if (w.Any(x => DesertWords.Contains(x))) return SceneFamily.Desert;
            if (w.Any(x => SteppeWords.Contains(x))) return SceneFamily.Steppe;
            return SceneFamily.Unknown;
        }

        internal static int Fit(SceneFamily family, HideoutTerrain terrain)
        {
            switch (family)
            {
                case SceneFamily.Unknown:
                    return 1;
                case SceneFamily.Coastal:
                    return terrain.Coastal ? 4 : 0;
                case SceneFamily.Forest:
                    return Score(terrain.Ground, forest: 3, plain: 2, swamp: 2, mountain: 2, steppe: 1, snow: 1, desert: 0);
                case SceneFamily.Mountain:
                    return Score(terrain.Ground, mountain: 3, snow: 2, forest: 2, plain: 1, steppe: 1, desert: 1, swamp: 1);
                case SceneFamily.Desert:
                    return Score(terrain.Ground, desert: 3, steppe: 2, plain: 1, mountain: 1, forest: 0, snow: 0, swamp: 0);
                case SceneFamily.Steppe:
                    return Score(terrain.Ground, steppe: 3, plain: 2, desert: 1, forest: 0, mountain: 0, snow: 0, swamp: 0);
                default:
                    return 1;
            }
        }

        private static int Score(Ground g, int plain = 0, int desert = 0, int forest = 0, int mountain = 0, int steppe = 0, int snow = 0, int swamp = 0)
        {
            switch (g)
            {
                case Ground.Desert: return desert;
                case Ground.Forest: return forest;
                case Ground.Mountain: return mountain;
                case Ground.Steppe: return steppe;
                case Ground.Snow: return snow;
                case Ground.Swamp: return swamp;
                default: return plain;
            }
        }

        internal static Ground? GroundOfTerrain(string? terrainName)
        {
            switch (terrainName)
            {
                case "Desert":
                case "Dune":
                    return Ground.Desert;
                case "Forest":
                    return Ground.Forest;
                case "Mountain":
                case "Cliff":
                case "Canyon":
                    return Ground.Mountain;
                case "Steppe":
                    return Ground.Steppe;
                case "Snow":
                    return Ground.Snow;
                case "Swamp":
                    return Ground.Swamp;
                case "Plain":
                case "RuralArea":
                case "Bridge":
                case "UnderBridge":
                case "Fording":
                case "River":
                case "NonNavigableRiver":
                    return Ground.Plain;
                default:
                    return null;
            }
        }

        internal static HideoutTerrain Classify(IEnumerable<string>? dryTerrainNames, bool coastal)
        {
            var counts = new Dictionary<Ground, int>();
            int total = 0;
            if (dryTerrainNames != null)
                foreach (string name in dryTerrainNames)
                {
                    Ground? g = GroundOfTerrain(name);
                    if (!g.HasValue)
                        continue;
                    total++;
                    counts[g.Value] = counts.TryGetValue(g.Value, out int n) ? n + 1 : 1;
                }

            Ground best = Ground.Plain;
            int bestCount = 0;
            foreach (Ground g in new[] { Ground.Desert, Ground.Snow, Ground.Mountain, Ground.Swamp, Ground.Forest, Ground.Steppe })
                if (counts.TryGetValue(g, out int c) && c > bestCount)
                {
                    best = g;
                    bestCount = c;
                }
            if (total == 0 || bestCount < 0.3 * total)
                best = Ground.Plain;
            return new HideoutTerrain(coastal, best);
        }

        internal static string Choose(string? current, IReadOnlyList<string>? preferred, IReadOnlyList<string>? pool, HideoutTerrain terrain, int tieBreak)
        {
            string cur = current ?? "";
            SceneFamily family = FamilyOf(cur);
            if (family == SceneFamily.Unknown || Fit(family, terrain) > 0)
                return cur;

            bool sv = cur.EndsWith("_sv", StringComparison.OrdinalIgnoreCase);
            var candidates = new List<(string Scene, int Rank)>();
            void Add(IEnumerable<string>? scenes, int ownListBonus)
            {
                if (scenes == null)
                    return;
                foreach (string scene in scenes)
                {
                    if (string.IsNullOrWhiteSpace(scene) || candidates.Any(c => c.Scene == scene))
                        continue;
                    int fit = Fit(FamilyOf(scene), terrain);
                    if (fit <= 0 || FamilyOf(scene) == SceneFamily.Unknown)
                        continue;
                    int variant = scene.EndsWith("_sv", StringComparison.OrdinalIgnoreCase) == sv ? 1 : 0;
                    candidates.Add((scene, fit * 10 + ownListBonus * 5 + variant));
                }
            }
            Add(preferred, 1);
            Add(pool, 0);
            if (candidates.Count == 0)
                return cur;

            int top = candidates.Max(c => c.Rank);
            var best = candidates.Where(c => c.Rank == top).Select(c => c.Scene).OrderBy(s => s, StringComparer.Ordinal).ToList();
            return best[(int)((uint)tieBreak % (uint)best.Count)];
        }
    }

    internal static class SceneCatalog
    {
        private sealed class Found
        {
            public string Name = "";
            public string Module = "";
        }

        private static List<Found>? _hideouts;
        private static List<Found>? _villages;

        internal static IReadOnlyList<string> Hideouts()
        {
            Build();
            return _hideouts!.Select(f => f.Name).ToList();
        }

        internal static IReadOnlyList<string> Villages()
        {
            Build();
            return _villages!.Select(f => f.Name).ToList();
        }

        internal static bool Usable(string scene)
        {
            if (string.IsNullOrWhiteSpace(scene))
                return false;
            try
            {
                foreach (string module in Utilities.GetModulesNames())
                {
                    string folder = Path.Combine(ModuleHelper.GetModuleFullPath(module), "SceneObj", scene);
                    if (!Directory.Exists(folder))
                        continue;
                    if (OfficialModules.Provides(module))
                        return true;
                    string file = Path.Combine(folder, "scene.xscene");
                    if (Passes(file, SceneContent.HideoutTags) || Passes(file, SceneContent.BattleTags))
                        return true;
                }
            }
            catch
            {
                // unreadable module folder, skip it
            }
            return false;
        }

        private static void Build()
        {
            if (_hideouts != null)
                return;
            _hideouts = new List<Found>();
            _villages = new List<Found>();
            try
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string module in Utilities.GetModulesNames())
                {
                    string dir = Path.Combine(ModuleHelper.GetModuleFullPath(module), "SceneObj");
                    if (!Directory.Exists(dir))
                        continue;
                    bool official = OfficialModules.Provides(module);
                    foreach (string folder in Directory.GetDirectories(dir))
                    {
                        string name = Path.GetFileName(folder);
                        string file = Path.Combine(folder, "scene.xscene");
                        if (!seen.Add(name) || !File.Exists(file))
                            continue;
                        if (SceneContent.LooksLikeHideout(name) && !SceneBiomes.KnownScenes.Contains(name) && Passes(file, SceneContent.HideoutTags))
                            _hideouts.Add(new Found { Name = name, Module = module });
                        else if (!official && SceneContent.LooksLikeVillage(name) && Passes(file, SceneContent.BattleTags))
                            _villages.Add(new Found { Name = name, Module = module });
                    }
                }
                if (_hideouts.Count > 0)
                    IMFDiagnostics.Info("[Scene] hideout scenes found in loaded modules: " + string.Join(", ", _hideouts.Select(f => f.Name + " (" + f.Module + ")")));
                if (_villages.Count > 0)
                    IMFDiagnostics.Info($"[Scene] {_villages.Count} village scene(s) found in loaded modules for nomad camp battles");
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("SceneCatalog could not scan the loaded modules:", e);
            }
        }

        private static bool Passes(string file, string[] tags)
        {
            try
            {
                return File.Exists(file) && SceneContent.HasAllTags(File.ReadLines(file), tags);
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class SceneContent
    {
        internal static readonly string[] HideoutTags = { "sp_hideout_group_attacker", "sp_hideout_group_defender", "hideout_boss_fight" };

        internal static readonly string[] BattleTags = { "sp_battle_set" };

        private static readonly string[] HideoutWords = { "hideout", "bandit", "pirate", "lair", "outlaw", "den", "cave" };
        private static readonly string[] NotHideoutWords = { "interior", "tavern", "castle", "town", "arena", "dungeon", "keep", "house", "village" };
        private static readonly string[] NotVillageWords = { "raid", "navalraid", "interior", "tavern", "house", "dungeon", "castle", "stealth", "cutscene", "arena" };

        internal static string[] Words(string? name) =>
            Regex.Split((name ?? "").ToLowerInvariant(), "[^a-z]+").Where(w => w.Length > 0).ToArray();

        internal static bool LooksLikeHideout(string? name)
        {
            string[] w = Words(name);
            return w.Any(x => HideoutWords.Contains(x)) && !w.Any(x => NotHideoutWords.Contains(x));
        }

        internal static bool LooksLikeVillage(string? name)
        {
            string[] w = Words(name);
            return w.Contains("village") && !w.Any(x => NotVillageWords.Contains(x));
        }

        internal static bool HasAllTags(IEnumerable<string> lines, IReadOnlyCollection<string> tags)
        {
            var missing = new HashSet<string>(tags, StringComparer.Ordinal);
            foreach (string line in lines)
            {
                foreach (string tag in missing.ToArray())
                    if (line.IndexOf(tag, StringComparison.Ordinal) >= 0)
                        missing.Remove(tag);
                if (missing.Count == 0)
                    return true;
            }
            return missing.Count == 0;
        }
    }

    public static class OfficialModules
    {
        private static readonly string[] Ids =
        {
            "Native", "SandBoxCore", "Sandbox", "StoryMode", "CustomBattle", "BirthAndDeath", "NavalDLC",
            "ImprovedMinorFactions"
        };

        public static bool Provides(string moduleId) =>
            !string.IsNullOrEmpty(moduleId) && Ids.Any(i => string.Equals(i, moduleId, StringComparison.OrdinalIgnoreCase));
    }
}

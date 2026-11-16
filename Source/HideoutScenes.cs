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
    /// <summary>
    /// Picks a battle scene for a camp that fits the ground it stands on. A camp moved inland shouldn't keep
    /// a pirate cove scene, and nomad camps get a village scene matching the terrain around them.
    /// </summary>
    internal static class HideoutScenes
    {
        private static List<string>? _pool;

        /// <summary>
        /// All hideout scenes we can pick from: the known vanilla ones that exist plus hideout scenes found in loaded modules.
        /// </summary>
        private static List<string> Pool() =>
            _pool ??= SceneBiomes.KnownScenes.Where(ConfigLoader.SceneExists).Concat(SceneCatalog.Hideouts()).Distinct().ToList();

        /// <summary>
        /// Looks at the map around a point: whether there's sea, a lake or a beach nearby, and what dry terrain is most common.
        /// </summary>
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

        /// <summary>
        /// Gets the scene a camp should use. Keeps its current scene if it fits the ground, otherwise picks a better one,
        /// preferring the scenes mf_data.xml lists for the clan.
        /// </summary>
        /// <param name="terrain">the ground around the camp, for logging</param>
        /// <returns>the scene name, the current one if nothing fits better</returns>
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

        /// <summary>
        /// Picks a village scene for a nomad camp's battles, matching the terrain around it.
        /// </summary>
        /// <returns>the scene name, or an empty string if there's no camp or map</returns>
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

        /// <summary>
        /// Hash that's the same every run, so a camp always picks the same scene among equally good ones.
        /// </summary>
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

    /// <summary>
    /// What kind of place a scene shows. Scenes have no terrain info, so this goes by name: known prefixes first, then the word lists.
    /// </summary>
    internal enum SceneFamily { Unknown, Coastal, Forest, Mountain, Desert, Steppe }

    /// <summary>
    /// The main dry terrain around a camp.
    /// </summary>
    internal enum Ground { Plain, Desert, Forest, Mountain, Steppe, Snow, Swamp }

    /// <summary>
    /// The ground around a camp, as found by HideoutScenes.Sample.
    /// </summary>
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

    /// <summary>
    /// Matches scene names to terrain. Pure functions over names, nothing here touches the game.
    /// </summary>
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

        /// <summary>
        /// Picks a village scene that fits the terrain, e.g. desert villages for desert camps. Falls back to any available scene.
        /// </summary>
        /// <param name="tieBreak">picks among equally good scenes, keeps the choice the same between loads</param>
        /// <param name="available">village scenes that actually exist</param>
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

        /// <summary>
        /// Guesses the terrain of a village scene from words in its name.
        /// </summary>
        /// <returns>the terrain, or null if the name doesn't say</returns>
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

        /// <summary>
        /// Guesses what kind of place a hideout scene shows from its name.
        /// </summary>
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

        /// <summary>
        /// How well a kind of scene fits the ground. 0 means it doesn't fit, e.g. a desert scene in a forest.
        /// </summary>
        /// <returns>0 to 4, higher is better</returns>
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

        /// <summary>
        /// Maps a game terrain type name to our coarser Ground.
        /// </summary>
        /// <returns>the ground, or null for water and anything else that isn't dry land</returns>
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

        /// <summary>
        /// Turns sampled terrain names into one ground type. Anything other than plain has to make up at least 30% of the samples to win.
        /// </summary>
        /// <param name="dryTerrainNames">terrain type names sampled around the camp</param>
        /// <param name="coastal">water or beach was found nearby</param>
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

        /// <summary>
        /// Picks the best scene for the terrain. The current scene stays if it fits or we can't tell what it shows.
        /// Scenes from the clan's own list rank higher, and the same _sv variant as the current scene is preferred.
        /// </summary>
        /// <param name="current">the camp's scene now</param>
        /// <param name="preferred">scenes mf_data.xml lists for the clan</param>
        /// <param name="pool">all other usable hideout scenes</param>
        /// <param name="tieBreak">picks among equally good scenes, keeps the choice the same between loads</param>
        /// <returns>the scene to use</returns>
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

    /// <summary>
    /// Finds hideout and village scenes in the loaded modules' SceneObj folders, so camps can use scenes a map ships.
    /// </summary>
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

        /// <summary>
        /// Checks that a scene exists in a loaded module and has the spawn points a hideout or field battle needs.
        /// Scenes from official modules are trusted without reading the file.
        /// </summary>
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

        /// <summary>
        /// Scans every loaded module once for scenes that look like hideouts or villages by name and have the right spawn tags.
        /// </summary>
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

        /// <summary>
        /// Checks that a scene file contains all the given tags.
        /// </summary>
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

    /// <summary>
    /// Checks scene names and scene files for what they look like.
    /// </summary>
    internal static class SceneContent
    {
        internal static readonly string[] HideoutTags = { "sp_hideout_group_attacker", "sp_hideout_group_defender", "hideout_boss_fight" };

        internal static readonly string[] BattleTags = { "sp_battle_set" };

        private static readonly string[] HideoutWords = { "hideout", "bandit", "pirate", "lair", "outlaw", "den", "cave" };
        private static readonly string[] NotHideoutWords = { "interior", "tavern", "castle", "town", "arena", "dungeon", "keep", "house", "village" };
        private static readonly string[] NotVillageWords = { "raid", "navalraid", "interior", "tavern", "house", "dungeon", "castle", "stealth", "cutscene", "arena" };

        internal static string[] Words(string? name) =>
            Regex.Split((name ?? "").ToLowerInvariant(), "[^a-z]+").Where(w => w.Length > 0).ToArray();

        /// <summary>
        /// Checks if a scene name sounds like a hideout and not like an interior, town or village.
        /// </summary>
        internal static bool LooksLikeHideout(string? name)
        {
            string[] w = Words(name);
            return w.Any(x => HideoutWords.Contains(x)) && !w.Any(x => NotHideoutWords.Contains(x));
        }

        /// <summary>
        /// Checks if a scene name sounds like a village's outdoor scene and not a raid, interior or cutscene.
        /// </summary>
        internal static bool LooksLikeVillage(string? name)
        {
            string[] w = Words(name);
            return w.Contains("village") && !w.Any(x => NotVillageWords.Contains(x));
        }

        /// <summary>
        /// Reads lines until every tag has been seen.
        /// </summary>
        /// <returns>true if all tags are in the file</returns>
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

    /// <summary>
    /// Modules whose scenes we use without checking them: the game, its DLCs and this mod.
    /// </summary>
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

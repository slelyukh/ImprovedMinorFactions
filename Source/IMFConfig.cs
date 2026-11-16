using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System;
using ImprovedMinorFactions.Patches;
using Path = System.IO.Path;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// Loads mf_data.xml from the IMF module folder once per campaign and hands out each faction's settings.
    /// If the file is missing or broken the mod runs on its defaults.
    /// </summary>
    internal static class ConfigLoader
    {
        private static IMFConfig? _config;
        private static ConfigView? _view;
        private static bool _loaded;

        internal static ConfigView? View
        {
            get { Ensure(); return _view; }
        }

        /// <summary>
        /// Gets the merged settings for a clan.
        /// </summary>
        /// <returns>the settings, or null if mf_data.xml has no row for it</returns>
        internal static FactionConfig? For(string? clanId)
        {
            if (string.IsNullOrEmpty(clanId))
                return null;
            Ensure();
            return _config?.For(clanId!);
        }

        internal static bool IsEnabled(string clanId) => For(clanId)?.Enabled ?? true;

        /// <summary>
        /// Forgets the loaded file so the next campaign reads it again. Called in OnGameEnd.
        /// </summary>
        internal static void Reset()
        {
            _config = null;
            _view = null;
            _loaded = false;
        }

        private static void Ensure()
        {
            if (_loaded)
                return;
            _loaded = true;
            try
            {
                string path = Path.Combine(ModuleDir(), "ModuleData", "mf_data.xml");
                if (!File.Exists(path))
                {
                    IMFDiagnostics.Info("[Config] no mf_data.xml in the IMF module folder, running without config");
                    return;
                }
                string[] modules = Utilities.GetModulesNames();
                _config = IMFConfig.Parse(File.ReadAllText(path), modules);
                _view = new ConfigView(_config, SceneExists, note => IMFDiagnostics.Info("[Config] " + note));
                IMFDiagnostics.Info($"[Config] mf_data.xml read: {_config.RowCount} faction row(s) apply to this game ({modules.Length} modules loaded)");
                foreach (string line in _config.Log)
                    IMFDiagnostics.Verbose("[Config] " + line);
            }
            catch (Exception e)
            {
                _config = null;
                _view = null;
                IMFDiagnostics.Log("[Config] could not load mf_data.xml, running without config:", e);
            }
        }

        internal static bool SceneExists(string scene) => SceneCatalog.Usable(scene);

        /// <summary>
        /// Gets the IMF module folder from where the dll is: &lt;module&gt;\bin\Win64_Shipping_Client\ImprovedMinorFactions.dll gives &lt;module&gt;.
        /// </summary>
        internal static string ModuleDir()
        {
            string dll = typeof(ConfigLoader).Assembly.Location;
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dll)!, "..", ".."));
        }
    }

    /// <summary>
    /// One faction's settings from mf_data.xml. Anything not set in the file is null and the mod's default is used.
    /// </summary>
    public sealed class FactionConfig
    {
        public bool? Enabled { get; set; }
        public int? Hideouts { get; set; }
        public List<string>? Scenes { get; set; }
        public int? NumActiveHideouts { get; set; }
        public int? NumMilitiaFirstTime { get; set; }
        public int? NumMilitiaPostRaid { get; set; }
        public int? NumLvl2Militia { get; set; }
        public int? NumLvl3Militia { get; set; }
        public int? MaxMilitia { get; set; }
        public string? ClanGender { get; set; }
        public string? Ship { get; set; }          // ship hull the faction's parties get (War Sails only)
    }

    /// <summary>
    /// The parsed mf_data.xml. A faction can have several rows: rows with a module attribute only count when that module is loaded,
    /// and later modules in the load order override earlier ones, so a map mod can change settings for its own factions.
    /// </summary>
    public sealed class IMFConfig
    {
        private sealed class Row
        {
            public int ModuleRank = -1;   // load order index of the row's module, -1 if the row has no module
            public int Order;
            public FactionConfig Values = new FactionConfig();
        }

        private static readonly HashSet<string> Known = new HashSet<string>(StringComparer.Ordinal)
        {
            "id", "minor_faction", "module", "enabled", "hideouts", "scenes", "num_active_hideouts", "num_militia_first_time",
            "num_militia_post_raid", "num_lvl2_militia", "num_lvl3_militia", "max_militia", "clan_gender", "ship"
        };

        private readonly Dictionary<string, List<Row>> _rows = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
        private readonly Dictionary<string, FactionConfig?> _merged = new Dictionary<string, FactionConfig?>(StringComparer.Ordinal);

        public List<string> Log { get; } = new List<string>();
        public int RowCount { get; private set; }
        public IEnumerable<string> FactionIds => _rows.Keys;

        /// <summary>
        /// Reads the file. Bad rows and values are skipped and noted in Log, an unreadable file gives an empty config.
        /// </summary>
        /// <param name="xml">contents of mf_data.xml</param>
        /// <param name="loadedModules">module ids in load order</param>
        public static IMFConfig Parse(string xml, IReadOnlyList<string> loadedModules)
        {
            var cfg = new IMFConfig();
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlElement? root = doc.DocumentElement;
                if (root == null || root.Name != "MFDatas")
                {
                    cfg.Log.Add("mf_data.xml: the root element is not <MFDatas>, file ignored");
                    return cfg;
                }
                int order = 0;
                foreach (XmlElement e in root.ChildNodes.OfType<XmlElement>())
                {
                    if (e.Name != "MFData")
                    {
                        cfg.Log.Add($"mf_data.xml: unknown element <{e.Name}> ignored");
                        continue;
                    }
                    cfg.AddRow(e, loadedModules, order++);
                }
            }
            catch (Exception ex)
            {
                cfg._rows.Clear();
                cfg.RowCount = 0;
                cfg.Log.Add($"mf_data.xml: unreadable, file ignored ({ex.Message})");
            }
            return cfg;
        }

        /// <summary>
        /// Merges all rows for a clan: rows without a module first, then by load order of their module, then file order.
        /// </summary>
        /// <returns>the merged settings, or null if there are no rows for the clan</returns>
        public FactionConfig? For(string clanId)
        {
            if (_merged.TryGetValue(clanId, out FactionConfig? cached))
                return cached;
            FactionConfig? result = null;
            if (_rows.TryGetValue(clanId, out List<Row>? rows))
            {
                result = new FactionConfig();
                foreach (Row r in rows.OrderBy(r => r.ModuleRank).ThenBy(r => r.Order))
                    Overlay(result, r.Values);
            }
            _merged[clanId] = result;
            return result;
        }

        private static void Overlay(FactionConfig into, FactionConfig from)
        {
            if (from.Enabled.HasValue) into.Enabled = from.Enabled;
            if (from.Hideouts.HasValue) into.Hideouts = from.Hideouts;
            if (from.Scenes != null) into.Scenes = new List<string>(from.Scenes);
            if (from.NumActiveHideouts.HasValue) into.NumActiveHideouts = from.NumActiveHideouts;
            if (from.NumMilitiaFirstTime.HasValue) into.NumMilitiaFirstTime = from.NumMilitiaFirstTime;
            if (from.NumMilitiaPostRaid.HasValue) into.NumMilitiaPostRaid = from.NumMilitiaPostRaid;
            if (from.NumLvl2Militia.HasValue) into.NumLvl2Militia = from.NumLvl2Militia;
            if (from.NumLvl3Militia.HasValue) into.NumLvl3Militia = from.NumLvl3Militia;
            if (from.MaxMilitia.HasValue) into.MaxMilitia = from.MaxMilitia;
            if (from.ClanGender != null) into.ClanGender = from.ClanGender;
            if (from.Ship != null) into.Ship = from.Ship;
        }

        /// <summary>
        /// Reads one MFData row. Skips rows with no minor_faction and rows for modules that aren't loaded.
        /// </summary>
        /// <param name="order">position of the row in the file, breaks ties between rows of the same module</param>
        private void AddRow(XmlElement e, IReadOnlyList<string> loadedModules, int order)
        {
            string id = e.GetAttribute("minor_faction").Trim();
            if (id.StartsWith("Faction.", StringComparison.Ordinal))
                id = id.Substring("Faction.".Length);
            if (id.Length == 0)
            {
                Log.Add($"mf_data.xml: row '{e.GetAttribute("id")}' has no minor_faction, ignored");
                return;
            }

            int moduleRank = -1;
            string module = e.GetAttribute("module").Trim();
            if (module.Length > 0)
            {
                moduleRank = IndexOfModule(loadedModules, module);
                if (moduleRank < 0)
                    return;   // row is for a module that isn't loaded, skip it
            }

            foreach (XmlAttribute a in e.Attributes)
                if (!Known.Contains(a.Name))
                    Log.Add($"faction {id}: unknown attribute '{a.Name}' ignored");

            var v = new FactionConfig
            {
                Enabled = ReadBool(e, "enabled", id),
                Hideouts = ReadInt(e, "hideouts", id),
                Scenes = ReadScenes(e, id),
                NumActiveHideouts = ReadInt(e, "num_active_hideouts", id),
                NumMilitiaFirstTime = ReadInt(e, "num_militia_first_time", id),
                NumMilitiaPostRaid = ReadInt(e, "num_militia_post_raid", id),
                NumLvl2Militia = ReadInt(e, "num_lvl2_militia", id),
                NumLvl3Militia = ReadInt(e, "num_lvl3_militia", id),
                MaxMilitia = ReadInt(e, "max_militia", id),
                ClanGender = ReadGender(e, id),
                Ship = ReadShip(e)
            };

            if (!_rows.TryGetValue(id, out List<Row>? list))
                _rows[id] = list = new List<Row>();
            list.Add(new Row { ModuleRank = moduleRank, Order = order, Values = v });
            RowCount++;
        }

        private static int IndexOfModule(IReadOnlyList<string> modules, string id)
        {
            for (int i = 0; i < modules.Count; i++)
                if (string.Equals(modules[i], id, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private bool? ReadBool(XmlElement e, string name, string id)
        {
            if (!e.HasAttribute(name))
                return null;
            string s = e.GetAttribute(name).Trim();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
            Log.Add($"faction {id}: {name}='{s}' is not true/false, ignored");
            return null;
        }

        private int? ReadInt(XmlElement e, string name, string id)
        {
            if (!e.HasAttribute(name))
                return null;
            string s = e.GetAttribute(name).Trim();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0)
                return n;
            Log.Add($"faction {id}: {name}='{s}' is not a whole number >= 0, ignored");
            return null;
        }

        private List<string>? ReadScenes(XmlElement e, string id)
        {
            if (!e.HasAttribute("scenes"))
                return null;
            var scenes = new List<string>();
            foreach (string part in e.GetAttribute("scenes").Split(','))
            {
                string s = part.Trim();
                if (s.Length > 0 && !scenes.Contains(s))
                    scenes.Add(s);
            }
            if (scenes.Count == 0)
            {
                Log.Add($"faction {id}: scenes is empty, ignored");
                return null;
            }
            return scenes;
        }

        // Hull ids only get checked when used, a missing DLC hull doesn't break anything.
        private static string? ReadShip(XmlElement e)
        {
            string s = e.GetAttribute("ship").Trim();
            return s.Length == 0 ? null : s;
        }

        /// <summary>
        /// Reads clan_gender as Male, Female or Any, in any case.
        /// </summary>
        private string? ReadGender(XmlElement e, string id)
        {
            if (!e.HasAttribute("clan_gender"))
                return null;
            string s = e.GetAttribute("clan_gender").Trim();
            if (string.Equals(s, "male", StringComparison.OrdinalIgnoreCase)) return "Male";
            if (string.Equals(s, "female", StringComparison.OrdinalIgnoreCase)) return "Female";
            if (string.Equals(s, "any", StringComparison.OrdinalIgnoreCase)) return "Any";
            Log.Add($"faction {id}: clan_gender='{s}' must be Male, Female or Any, ignored");
            return null;
        }
    }

    /// <summary>
    /// Read-only access to the config for the hideout generator and scene picking. Checks that listed scenes actually exist.
    /// </summary>
    public sealed class ConfigView
    {
        private static readonly IReadOnlyList<string> NoScenes = new List<string>();

        private readonly IMFConfig _config;
        private readonly Func<string, bool> _sceneExists;
        private readonly Action<string>? _log;
        private readonly Dictionary<string, IReadOnlyList<string>> _scenes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        public ConfigView(IMFConfig config, Func<string, bool> sceneExists, Action<string>? log = null)
        {
            _config = config;
            _sceneExists = sceneExists;
            _log = log;
        }

        public bool IsEnabled(string clanId) => _config.For(clanId)?.Enabled ?? true;

        public int? HideoutCount(string clanId) => _config.For(clanId)?.Hideouts;

        public int? NumActiveHideouts(string clanId) => _config.For(clanId)?.NumActiveHideouts;

        /// <summary>
        /// Gets the scenes listed for a clan. Only scenes from a loaded module count, so a typo can't make it into an attack.
        /// </summary>
        public IReadOnlyList<string> Scenes(string clanId)
        {
            if (_scenes.TryGetValue(clanId, out IReadOnlyList<string>? cached))
                return cached;
            IReadOnlyList<string> result = NoScenes;
            List<string>? wanted = _config.For(clanId)?.Scenes;
            if (wanted != null)
            {
                var valid = new List<string>();
                foreach (string s in wanted)
                {
                    if (_sceneExists(s))
                        valid.Add(s);
                    else
                        _log?.Invoke($"faction {clanId}: scene '{s}' is in no loaded module, or is not usable for a battle, ignored");
                }
                result = valid;
            }
            _scenes[clanId] = result;
            return result;
        }
    }
}

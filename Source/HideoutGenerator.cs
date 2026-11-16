using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using ImprovedMinorFactions.Patches;
using TaleWorlds.Library;
using System;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// What the save remembers about a converted or moved camp, so the next load can rebuild it the same way.
    /// </summary>
    public sealed class SavedHideout
    {
        public string OwnerId { get; set; } = "";
        public string? CultureRef { get; set; }       // "Culture.x" of the owner clan, null if we don't know
        public bool HasPosition { get; set; }         // IMF moved this hideout and remembered the spot
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Gives minor factions camps on maps that don't ship IMF hideout data. Runs on settlements.xml while the game loads it,
    /// before any settlement exists: picks bandit hideout rows near each clan's home and rewrites them into minor faction camps.
    /// On a saved game ApplySaved redoes the conversions the save remembers instead of planning new ones.
    /// </summary>
    public static class HideoutGenerator
    {
        private const string MfComplexTemplate = "LocationComplexTemplate.mf_hideout_complex";
        private const string MfCenterId = "mf_hideout_center";

        private const string UnassignedOwner = "none";

        /// <summary>
        /// A bandit hideout row that can be turned into a camp.
        /// </summary>
        private sealed class Donor
        {
            public XmlElement Row = null!;
            public string Id = "";
            public Vec2 Pos;
            public string Scene = "";
        }

        /// <summary>
        /// A donor row picked for a clan, with the culture the camp will get.
        /// </summary>
        private sealed class Planned
        {
            public Donor D = null!;
            public ClanInfo C = null!;
            public string Culture = "";
        }

        /// <summary>
        /// Plans and converts camps for every minor faction that doesn't have one in the document yet.
        /// Each clan takes the bandit hideouts nearest its home, at most MaxDonorFraction of them in total so bandits keep some.
        /// When there aren't enough, every clan gets one camp before anyone gets a second. Camp rows for missing or disabled clans are removed.
        /// </summary>
        /// <param name="doc">the settlements document, changed in place</param>
        /// <param name="registry">clans, cultures and mf_data counts read earlier in the load</param>
        /// <param name="options">settings from imf_hideouts.xml</param>
        /// <param name="resolveClan">extra lookup for clans the registry doesn't have, e.g. ones another mod adds later</param>
        /// <param name="context">tells which of our shipped rows belong to a base map that isn't loaded</param>
        /// <param name="config">per-faction settings from mf_data.xml</param>
        /// <returns>counts and log lines for IMF.log</returns>
        public static GeneratorReport Apply(XmlDocument doc, ClanRegistry registry, GeneratorOptions options, Func<string, bool>? resolveClan = null,
            GeneratorContext? context = null, ConfigView? config = null)
        {
            var report = new GeneratorReport();
            XmlElement? root = doc.DocumentElement;
            if (root == null || root.Name != "Settlements")
            {
                report.Skipped = "not a Settlements document";
                return report;
            }
            if (!options.Enabled)
            {
                report.Skipped = "disabled";
                return report;
            }
            if (registry.Count == 0)
            {
                // Clans aren't loaded yet. If we ran now every row in settlements.xml would look like it has no clan.
                report.Skipped = "no clans captured";
                return report;
            }

            List<XmlElement> rows = root.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "Settlement").ToList();
            var coords = new Dictionary<string, Vec2>(StringComparer.Ordinal);
            foreach (XmlElement r in rows)
                if (TryPos(r, out Vec2 p))
                    coords[r.GetAttribute("id")] = p;

            var toRemove = new List<XmlElement>();
            var covered = new HashSet<string>(StringComparer.Ordinal);
            foreach (XmlElement r in rows)
            {
                if (!HasMfComponent(r))
                    continue;
                string owner = ClanRegistry.StripPrefix(r.GetAttribute("owner"), "Faction.");
                if (options.Force)
                {
                    toRemove.Add(r);
                    report.ForcedRemoved++;
                    continue;
                }
                if (context != null && context.IsForeign(r.GetAttribute("id")))
                {
                    toRemove.Add(r);
                    report.ForeignDropped++;
                    report.Lines.Add($"foreign {r.GetAttribute("id")} (shipped for the base map, which is not loaded) dropped");
                    continue;
                }
                if (config != null && owner.Length > 0 && owner != UnassignedOwner && !config.IsEnabled(owner))
                {
                    toRemove.Add(r);
                    report.DisabledDropped++;
                    report.Lines.Add($"disabled {r.GetAttribute("id")} (faction {owner} is switched off in mf_data.xml) dropped");
                    continue;
                }
                if (owner == UnassignedOwner)
                    continue;
                bool known = registry.Contains(owner) || (resolveClan != null && resolveClan(owner));
                if (!known)
                {
                    toRemove.Add(r);
                    report.OrphansDropped++;
                    report.Lines.Add($"orphan {r.GetAttribute("id")} owner={owner} dropped");
                    continue;
                }
                covered.Add(owner);
            }

            var homes = new HashSet<string>(
                registry.All.Where(c => !string.IsNullOrEmpty(c.HomeId)).Select(c => c.HomeId!),
                StringComparer.Ordinal);
            var donors = new List<Donor>();
            foreach (XmlElement r in rows)
            {
                string id = r.GetAttribute("id");
                if (id.Length == 0 || homes.Contains(id) || !coords.TryGetValue(id, out Vec2 pos))
                    continue;
                if (!HasComponent(r, "Hideout"))
                    continue;
                string scene = FirstScene(r);
                if (scene.Length == 0)
                    continue;
                donors.Add(new Donor { Row = r, Id = id, Pos = pos, Scene = scene });
            }
            report.Donors = donors.Count;

            List<ClanInfo> eligible = registry.All
                .Where(c => c.IsMinor && !c.IsBandit && !c.IsPlayer && c.Culture.Length > 0 && (config == null || config.IsEnabled(c.Id)))
                .OrderBy(c => c.Id, StringComparer.Ordinal)
                .ToList();
            List<ClanInfo> todo = eligible.Where(c => !covered.Contains(c.Id)).ToList();
            report.Clans = eligible.Count;
            report.Covered = eligible.Count - todo.Count;

            Vec2 centroid = donors.Count == 0
                ? Vec2.Zero
                : new Vec2(donors.Average(d => d.Pos.X), donors.Average(d => d.Pos.Y));
            int maxConsume = (int)Math.Floor(options.MaxDonorFraction * donors.Count);

            Vec2 AnchorOf(ClanInfo clan, bool log)
            {
                if (clan.HomeId != null && coords.TryGetValue(clan.HomeId, out Vec2 home))
                    return home;
                if (log)
                    report.Lines.Add($"clan {clan.Id}: home '{clan.HomeId ?? "?"}' not in document, anchored at donor centroid");
                return centroid;
            }

            int RawNeedOf(ClanInfo clan)
            {
                int? configured = config?.HideoutCount(clan.Id);
                if (configured.HasValue)
                    return configured.Value;
                int active = config?.NumActiveHideouts(clan.Id) ?? registry.ActiveHideoutsFor(clan.Id) ?? 1;
                return Math.Max(options.PerClan ?? (1 + active), 1);
            }

            var ownBase = new Dictionary<string, Donor>(StringComparer.Ordinal);
            var claimedHomes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ClanInfo clan in todo)
            {
                if (clan.HomeId == null || RawNeedOf(clan) == 0 || claimedHomes.Contains(clan.HomeId))
                    continue;
                XmlElement? homeRow = rows.FirstOrDefault(r => r.GetAttribute("id") == clan.HomeId);
                if (homeRow == null || !HasComponent(homeRow, "Hideout") || HasMfComponent(homeRow) || !coords.TryGetValue(clan.HomeId, out Vec2 homePos))
                    continue;
                string homeScene = FirstScene(homeRow);
                if (homeScene.Length == 0)
                    continue;
                claimedHomes.Add(clan.HomeId);
                ownBase[clan.Id] = new Donor { Row = homeRow, Id = clan.HomeId, Pos = homePos, Scene = homeScene };
            }

            int NeedOf(ClanInfo clan) => Math.Max(RawNeedOf(clan) - (ownBase.ContainsKey(clan.Id) ? 1 : 0), 0);

            List<Planned> PlanInOrder(bool log)
            {
                var free = new List<Donor>(donors);
                var planned = new List<Planned>();
                foreach (ClanInfo clan in todo)
                {
                    Vec2 anchor = AnchorOf(clan, log);
                    int need = NeedOf(clan);
                    int got = 0;
                    while (got < need && planned.Count < maxConsume && free.Count > 0)
                    {
                        Donor d = Nearest(free, anchor);
                        free.Remove(d);
                        planned.Add(new Planned { D = d, C = clan, Culture = CultureFor(clan, d, registry.IsBanditCulture, rows, coords) });
                        got++;
                    }
                }
                return planned;
            }

            // one hideout each before anyone gets a second, even past the donor cap (max 80%)
            List<Planned> PlanFairly()
            {
                var free = new List<Donor>(donors);
                var planned = new List<Planned>();
                int wanting = todo.Count(c => NeedOf(c) > 0);
                int firstRoundCap = Math.Max(maxConsume, Math.Min(wanting, (int)Math.Floor(0.8 * donors.Count)));
                foreach (ClanInfo clan in todo)
                {
                    if (NeedOf(clan) == 0)
                        continue;
                    if (planned.Count >= firstRoundCap || free.Count == 0)
                        break;
                    Donor d = Nearest(free, AnchorOf(clan, false));
                    free.Remove(d);
                    planned.Add(new Planned { D = d, C = clan, Culture = CultureFor(clan, d, registry.IsBanditCulture, rows, coords) });
                }
                foreach (ClanInfo clan in todo)
                {
                    int have = planned.Count(p => p.C == clan);
                    if (have == 0)
                        continue;
                    int need = NeedOf(clan);
                    Vec2 anchor = AnchorOf(clan, false);
                    while (have < need && planned.Count < maxConsume && free.Count > 0)
                    {
                        Donor d = Nearest(free, anchor);
                        free.Remove(d);
                        planned.Add(new Planned { D = d, C = clan, Culture = CultureFor(clan, d, registry.IsBanditCulture, rows, coords) });
                        have++;
                    }
                }
                return planned;
            }

            List<Planned> plan = PlanInOrder(true);
            if (todo.Any(c => NeedOf(c) > 0 && plan.All(p => p.C != c)) && donors.Count > 0)
            {
                List<Planned> fair = PlanFairly();
                if (fair.Select(p => p.C).Distinct().Count() > plan.Select(p => p.C).Distinct().Count())
                {
                    plan = fair;
                    report.Lines.Add("scarce donors: every clan gets one hideout before any gets a spare");
                }
            }

            foreach (ClanInfo clan in todo)
                if (ownBase.TryGetValue(clan.Id, out Donor? own))
                {
                    plan.Add(new Planned { D = own, C = clan, Culture = CultureFor(clan, own, registry.IsBanditCulture, rows, coords) });
                    report.Lines.Add($"{own.Id} is {clan.Id}'s own home and a hideout row: it becomes the clan's first hideout");
                }

            foreach (Planned p in plan)
            {
                Vec2 anchor = AnchorOf(p.C, false);
                report.Lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} -> {1} scene={2} dist={3:F1}", p.D.Id, p.C.Id, p.D.Scene, Dist(anchor, p.D.Pos)));
                if (p.Culture != p.C.Culture)
                    report.Lines.Add($"{p.D.Id}: {p.C.Culture} is a bandit culture, hideout culture set to {p.Culture}");
            }
            foreach (ClanInfo clan in todo)
                if (plan.Count(p => p.C == clan) < RawNeedOf(clan))
                    report.Short.Add(clan.Id);

            foreach (XmlElement r in toRemove)
                root.RemoveChild(r);
            foreach (Planned p in plan)
                ConvertRow(doc, p.D, p.C.Id, p.Culture, ownBase.TryGetValue(p.C.Id, out Donor? ownDonor) && ownDonor == p.D);
            report.Converted = plan.Count;
            if (config != null)
                report.ScenesSet += ApplyConfigScenes(doc, config);
            return report;
        }

        /// <summary>
        /// Picks the camp's culture. An active camp with a bandit culture crashes, so bandit-culture clans get
        /// the culture of their home, or of the nearest town to the camp.
        /// </summary>
        private static string CultureFor(ClanInfo clan, Donor donor, Func<string, bool> isBanditCulture,
            List<XmlElement> rows, Dictionary<string, Vec2> coords)
        {
            if (!isBanditCulture(clan.Culture))
                return clan.Culture;

            if (clan.HomeId != null)
            {
                XmlElement? home = rows.FirstOrDefault(r => r.GetAttribute("id") == clan.HomeId);
                if (home != null && IsRegularCultureRow(home, isBanditCulture))
                    return home.GetAttribute("culture");
            }

            return NearestRegularCulture(donor.Pos, isBanditCulture, rows, coords, clan.Culture);
        }

        /// <summary>
        /// Gets the culture of the closest non-bandit settlement to a point.
        /// </summary>
        /// <param name="fallback">returned when there is no such settlement</param>
        private static string NearestRegularCulture(Vec2 at, Func<string, bool> isBanditCulture,
            List<XmlElement> rows, Dictionary<string, Vec2> coords, string fallback)
        {
            XmlElement? nearest = null;
            double nearestD = double.MaxValue;
            foreach (XmlElement r in rows)
            {
                if (!IsRegularCultureRow(r, isBanditCulture) || !coords.TryGetValue(r.GetAttribute("id"), out Vec2 p))
                    continue;
                double d = Dist2(at, p);
                if (d < nearestD)
                {
                    nearest = r;
                    nearestD = d;
                }
            }
            return nearest != null ? nearest.GetAttribute("culture") : fallback;
        }

        private static bool IsRegularCultureRow(XmlElement row, Func<string, bool> isBanditCulture)
        {
            string culture = row.GetAttribute("culture");
            return culture.Length > 0
                && !isBanditCulture(culture)
                && !HasComponent(row, "Hideout")
                && !HasMfComponent(row);
        }

        /// <summary>
        /// Rewrites a bandit hideout row into a minor faction camp owned by the clan: swaps the Hideout component for
        /// MinorFactionHideout, keeps its icon and scene, and gives it the camp location complex.
        /// </summary>
        /// <param name="keepName">keep the map's name for the row, used when the row is the clan's own home</param>
        private static void ConvertRow(XmlDocument doc, Donor d, string clanId, string culture, bool keepName = false)
        {
            XmlElement row = d.Row;
            XmlElement old = row["Components"]!.ChildNodes.OfType<XmlElement>().First(c => c.Name == "Hideout");
            XmlElement? srcLoc = row["Locations"]?.ChildNodes.OfType<XmlElement>()
                .FirstOrDefault(l => l.Name == "Location" && l.GetAttribute("scene_name").Length > 0);

            row.SetAttribute("owner", "Faction." + clanId);
            row.SetAttribute("culture", culture);
            if (!keepName)   // the clan's own home keeps the name the map gave it
                row.SetAttribute("name", "{=!}Hideout");
            row.RemoveAttribute("type");
            row.SetAttribute("imf_generated", "1");

            XmlElement mfh = doc.CreateElement("MinorFactionHideout");
            mfh.SetAttribute("id", d.Id);
            mfh.SetAttribute("map_icon", Attr(old, "map_icon", "bandit_hideout_c"));
            mfh.SetAttribute("scene_name", d.Scene);
            mfh.SetAttribute("background_crop_position", Attr(old, "background_crop_position", "0.0"));
            mfh.SetAttribute("background_mesh", Attr(old, "background_mesh", "empire_twn_scene_bg"));
            mfh.SetAttribute("wait_mesh", Attr(old, "wait_mesh", ""));
            mfh.SetAttribute("gate_rotation", Attr(old, "gate_rotation", "0.0"));
            old.ParentNode!.ReplaceChild(mfh, old);

            XmlElement? locs = row["Locations"];
            if (locs == null)
            {
                locs = doc.CreateElement("Locations");
                row.AppendChild(locs);
            }
            while (locs.FirstChild != null)
                locs.RemoveChild(locs.FirstChild);
            locs.SetAttribute("complex_template", MfComplexTemplate);

            XmlElement center = doc.CreateElement("Location");
            center.SetAttribute("id", MfCenterId);
            center.SetAttribute("scene_name", d.Scene);
            for (int i = 1; i <= 3; i++)
            {
                string key = "scene_name_" + i;
                if (srcLoc != null && srcLoc.HasAttribute(key))
                    center.SetAttribute(key, srcLoc.GetAttribute(key));
            }
            locs.AppendChild(center);
        }

        /// <summary>
        /// Checks if a settlement row is already a minor faction camp.
        /// </summary>
        private static bool HasMfComponent(XmlElement settlement)
        {
            XmlElement? comps = settlement["Components"];
            if (comps == null)
                return false;
            foreach (XmlElement c in comps.ChildNodes.OfType<XmlElement>())
            {
                if (c.Name == "MinorFactionHideout")
                    return true;
                if (c.Name == "CustomSettlementComponent" && c.GetAttribute("component_name") == "MinorFactionHideout")
                    return true;
            }
            return false;
        }

        private static bool HasComponent(XmlElement settlement, string name)
        {
            XmlElement? comps = settlement["Components"];
            return comps != null && comps.ChildNodes.OfType<XmlElement>().Any(c => c.Name == name);
        }

        /// <summary>
        /// Gets the first scene a settlement row lists, from its locations or its hideout component.
        /// </summary>
        /// <returns>the scene name, or an empty string</returns>
        private static string FirstScene(XmlElement settlement)
        {
            XmlElement? locs = settlement["Locations"];
            if (locs != null)
            {
                foreach (XmlElement l in locs.ChildNodes.OfType<XmlElement>())
                {
                    if (l.Name == "Location" && l.GetAttribute("scene_name").Length > 0)
                        return l.GetAttribute("scene_name");
                }
            }
            XmlElement? hide = settlement["Components"]?.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.Name == "Hideout");
            return hide?.GetAttribute("scene_name") ?? "";
        }

        /// <summary>
        /// Reads posX and posY from a settlement row.
        /// </summary>
        private static bool TryPos(XmlElement r, out Vec2 p)
        {
            p = default;
            if (!double.TryParse(r.GetAttribute("posX"), NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                return false;
            if (!double.TryParse(r.GetAttribute("posY"), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                return false;
            p = new Vec2((float)x, (float)y);
            return true;
        }

        /// <summary>
        /// Gets the free donor closest to a point. Ties keep file order so every load plans the same.
        /// </summary>
        private static Donor Nearest(List<Donor> free, Vec2 anchor)
        {
            Donor best = free[0];
            double bestD = Dist2(anchor, best.Pos);
            for (int i = 1; i < free.Count; i++)
            {
                double d = Dist2(anchor, free[i].Pos);
                if (d < bestD)
                {
                    best = free[i];
                    bestD = d;
                }
            }
            return best;
        }

        private static double Dist2(Vec2 a, Vec2 b) => a.DistanceSquared(b);

        private static double Dist(Vec2 a, Vec2 b) => a.Distance(b);

        private static string Attr(XmlElement e, string name, string fallback)
        {
            string v = e.GetAttribute(name);
            return v.Length > 0 ? v : fallback;
        }

        private static readonly string[] SceneAttributes = { "scene_name", "scene_name_1", "scene_name_2", "scene_name_3" };

        /// <summary>
        /// Sets the scenes mf_data.xml lists for a faction on that faction's camps, going through the list in order.
        /// </summary>
        /// <returns>how many rows changed</returns>
        internal static int ApplyConfigScenes(XmlDocument doc, ConfigView config)
        {
            XmlElement? root = doc.DocumentElement;
            if (root == null)
                return 0;

            var byClan = new Dictionary<string, List<XmlElement>>(StringComparer.Ordinal);
            foreach (XmlElement r in root.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "Settlement"))
            {
                if (!HasMfComponent(r))
                    continue;
                string owner = ClanRegistry.StripPrefix(r.GetAttribute("owner"), "Faction.");
                if (owner.Length == 0 || owner == UnassignedOwner)
                    continue;
                if (!byClan.TryGetValue(owner, out List<XmlElement>? list))
                    byClan[owner] = list = new List<XmlElement>();
                list.Add(r);
            }

            int changed = 0;
            foreach (KeyValuePair<string, List<XmlElement>> pair in byClan)
            {
                IReadOnlyList<string> scenes = config.Scenes(pair.Key);
                if (scenes.Count == 0)
                    continue;
                List<XmlElement> rows = pair.Value.OrderBy(r => r.GetAttribute("id"), StringComparer.Ordinal).ToList();
                for (int i = 0; i < rows.Count; i++)
                    if (SetScene(rows[i], scenes[i % scenes.Count]))
                        changed++;
            }
            return changed;
        }

        /// <summary>
        /// Puts one scene on a camp row's component and its location's scene slots.
        /// </summary>
        /// <returns>true if anything changed</returns>
        private static bool SetScene(XmlElement row, string scene)
        {
            bool changed = false;
            XmlElement? comp = row["Components"]?.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.Name == "MinorFactionHideout");
            if (comp != null && comp.GetAttribute("scene_name") != scene)
            {
                comp.SetAttribute("scene_name", scene);
                changed = true;
            }
            XmlElement? locations = row["Locations"];
            if (locations != null)
            {
                foreach (XmlElement loc in locations.ChildNodes.OfType<XmlElement>().Where(l => l.Name == "Location" && l.GetAttribute("scene_name").Length > 0))
                {
                    foreach (string attr in SceneAttributes)
                    {
                        if (attr != "scene_name" && !loc.HasAttribute(attr))
                            continue;
                        if (loc.GetAttribute(attr) != scene)
                        {
                            loc.SetAttribute(attr, scene);
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        private const double PositionEpsilon = 0.01;

        /// <summary>
        /// Rebuilds camps on a saved game. settlements.xml is reread on every load but the conversions aren't in it,
        /// so rows the save remembers as camps are converted again and moved camps get their saved position back.
        /// Camp rows the save doesn't know are dropped if they belong to another map, a disabled faction or a missing clan.
        /// </summary>
        /// <param name="savedHideout">gets what the save stored for a row id, or null</param>
        /// <param name="settlementSaved">true if the save has a settlement with that id</param>
        /// <param name="clanExists">true if the clan exists in the save</param>
        /// <param name="isBanditCulture">true for bandit cultures, those can't be used on a camp</param>
        /// <returns>counts and log lines for IMF.log</returns>
        public static GeneratorReport ApplySaved(XmlDocument doc,
            Func<string, SavedHideout?> savedHideout,
            Func<string, bool> settlementSaved,
            Func<string, bool> clanExists,
            Func<string, bool> isBanditCulture,
            GeneratorContext? context = null,
            ConfigView? config = null)
        {
            var report = new GeneratorReport();
            XmlElement? root = doc.DocumentElement;
            if (root == null || root.Name != "Settlements")
            {
                report.Skipped = "not a Settlements document";
                return report;
            }

            List<XmlElement> rows = root.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "Settlement").ToList();
            var coords = new Dictionary<string, Vec2>(StringComparer.Ordinal);
            foreach (XmlElement r in rows)
                if (TryPos(r, out Vec2 p))
                    coords[r.GetAttribute("id")] = p;

            var toRemove = new List<XmlElement>();
            foreach (XmlElement r in rows)
            {
                bool isMf = HasMfComponent(r);
                bool isBandit = !isMf && HasComponent(r, "Hideout");
                if (!isMf && !isBandit)
                    continue;

                string id = r.GetAttribute("id");
                SavedHideout? saved = id.Length == 0 ? null : savedHideout(id);

                if (saved == null || saved.OwnerId.Length == 0)
                {
                    if (isMf && id.Length > 0 && !settlementSaved(id))
                    {
                        if (context != null && context.IsForeign(id))
                        {
                            toRemove.Add(r);
                            report.ForeignDropped++;
                            report.Lines.Add($"foreign {id} dropped");
                            continue;
                        }
                        string owner = ClanRegistry.StripPrefix(r.GetAttribute("owner"), "Faction.");
                        if (config != null && owner.Length > 0 && owner != UnassignedOwner && !config.IsEnabled(owner))
                        {
                            toRemove.Add(r);
                            report.DisabledDropped++;
                            report.Lines.Add($"disabled {id} dropped");
                            continue;
                        }
                        if (owner.Length > 0 && owner != UnassignedOwner && !clanExists(owner))
                        {
                            toRemove.Add(r);
                            report.OrphansDropped++;
                            report.Lines.Add($"orphan {id} owner={owner} dropped");
                        }
                    }
                    continue;
                }

                if (isBandit)
                {
                    string scene = FirstScene(r);
                    if (scene.Length == 0)
                    {
                        report.Lines.Add($"{id}: saved as an IMF hideout of {saved.OwnerId} but the row has no battle scene, left as is");
                        continue;
                    }
                    coords.TryGetValue(id, out Vec2 at);
                    var donor = new Donor { Row = r, Id = id, Pos = at, Scene = scene };
                    string culture = !string.IsNullOrEmpty(saved.CultureRef) && !isBanditCulture(saved.CultureRef!)
                        ? saved.CultureRef!
                        : NearestRegularCulture(at, isBanditCulture, rows, coords, r.GetAttribute("culture"));
                    ConvertRow(doc, donor, saved.OwnerId, culture);
                    report.Converted++;
                    report.Lines.Add($"{id} -> {saved.OwnerId} (from the save)");
                }

                if (saved.HasPosition && RestorePosition(r, saved))
                    report.PositionsRestored++;
            }

            foreach (XmlElement r in toRemove)
                root.RemoveChild(r);
            if (config != null)
                report.ScenesSet += ApplyConfigScenes(doc, config);
            return report;
        }

        /// <summary>
        /// Writes the saved position into a row and clears its gate position so the game puts the gate on the camp.
        /// </summary>
        /// <returns>true if the position changed</returns>
        private static bool RestorePosition(XmlElement row, SavedHideout saved)
        {
            bool had = TryPos(row, out Vec2 current);
            if (had && Math.Abs(current.X - saved.X) < PositionEpsilon && Math.Abs(current.Y - saved.Y) < PositionEpsilon)
                return false;
            row.SetAttribute("posX", saved.X.ToString("R", CultureInfo.InvariantCulture));
            row.SetAttribute("posY", saved.Y.ToString("R", CultureInfo.InvariantCulture));
            row.RemoveAttribute("gate_posX");
            row.RemoveAttribute("gate_posY");
            return true;
        }
    }

    /// <summary>
    /// Tells which of the camp rows we ship belong to the base map. They're only trusted when that map is actually loaded.
    /// </summary>
    public sealed class GeneratorContext
    {
        public ISet<string> ShippedRowIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);

        public bool? BaseMapPresent { get; set; }

        public bool IsForeign(string rowId) =>
            BaseMapPresent == false && ShippedRowIds.Contains(rowId);

        /// <summary>
        /// Checks if the loaded map is the base map. Matching ids isn't enough, the towns also have to be where the base map has them.
        /// </summary>
        /// <param name="baseRegular">town, castle and village positions of the base map</param>
        /// <param name="loaded">the same for the loaded map</param>
        /// <param name="minFraction">share of base settlements that must be found</param>
        /// <param name="tolerance">how far a settlement may be from its base map position</param>
        /// <returns>true or false, or null if there's nothing to compare</returns>
        public static bool? DetectBaseMap(IReadOnlyDictionary<string, Vec2> baseRegular,
            IReadOnlyDictionary<string, Vec2> loaded, double minFraction = 0.5, double tolerance = 1.0)
        {
            if (baseRegular.Count == 0)
                return null;
            int found = 0;
            foreach (KeyValuePair<string, Vec2> b in baseRegular)
            {
                if (loaded.TryGetValue(b.Key, out Vec2 p)
                    && Math.Abs(p.X - b.Value.X) <= tolerance && Math.Abs(p.Y - b.Value.Y) <= tolerance)
                    found++;
            }
            return found >= minFraction * baseRegular.Count;
        }

        public static HashSet<string> SettlementIds(XmlDocument doc, bool regularOnly) =>
            new HashSet<string>(Rows(doc, regularOnly).Select(r => r.GetAttribute("id")), StringComparer.Ordinal);

        /// <summary>
        /// Gets the position of each settlement row in a document.
        /// </summary>
        /// <param name="regularOnly">skip hideouts and camps</param>
        public static Dictionary<string, Vec2> SettlementPositions(XmlDocument doc, bool regularOnly)
        {
            var positions = new Dictionary<string, Vec2>(StringComparer.Ordinal);
            foreach (XmlElement row in Rows(doc, regularOnly))
            {
                if (double.TryParse(row.GetAttribute("posX"), NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                    && double.TryParse(row.GetAttribute("posY"), NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    positions[row.GetAttribute("id")] = new Vec2((float)x, (float)y);
            }
            return positions;
        }

        private static IEnumerable<XmlElement> Rows(XmlDocument doc, bool regularOnly)
        {
            XmlElement? root = doc.DocumentElement;
            if (root == null)
                yield break;
            foreach (XmlElement row in root.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "Settlement"))
            {
                if (row.GetAttribute("id").Length == 0)
                    continue;
                if (regularOnly)
                {
                    XmlElement? comps = row["Components"];
                    bool hideout = comps != null && comps.ChildNodes.OfType<XmlElement>().Any(c =>
                        c.Name == "Hideout" || c.Name == "MinorFactionHideout" || c.Name == "CustomSettlementComponent");
                    if (hideout)
                        continue;
                }
                yield return row;
            }
        }
    }

    /// <summary>
    /// Settings from the optional ModuleData/imf_hideouts.xml, e.g.
    /// &lt;HideoutGenerator enabled="true" force="false" per_clan="" max_donor_fraction="0.4"/&gt;
    /// Everything is optional, no file (or one we can't read) just means the defaults.
    /// </summary>
    public sealed class GeneratorOptions
    {
        public bool Enabled { get; set; } = true;

        // Force=true throws away every row from settlements.xml and generates hideouts for all clans.
        public bool Force { get; set; }

        public int? PerClan { get; set; }


        public double MaxDonorFraction { get; set; } = 0.4;

        /// <summary>
        /// Reads the options file, or returns the defaults if it's missing or broken.
        /// </summary>
        public static GeneratorOptions Load(string path)
        {
            var o = new GeneratorOptions();
            try
            {
                if (!File.Exists(path))
                    return o;
                var d = new XmlDocument();
                d.Load(path);
                XmlElement? e = d.DocumentElement;
                if (e == null || e.Name != "HideoutGenerator")
                    return o;

                o.Enabled = Bool(e, "enabled", true);
                o.Force = Bool(e, "force", false);
                if (int.TryParse(e.GetAttribute("per_clan"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pc) && pc > 0)
                    o.PerClan = pc;
                if (double.TryParse(e.GetAttribute("max_donor_fraction"), NumberStyles.Float, CultureInfo.InvariantCulture, out double f)
                    && f >= 0.0 && f <= 1.0)
                    o.MaxDonorFraction = f;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Hideouts] imf_hideouts.xml unreadable, using defaults: {e.Message}");
                return new GeneratorOptions();
            }
            return o;
        }

        private static bool Bool(XmlElement e, string name, bool fallback)
        {
            string v = e.GetAttribute(name);
            if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }
    }

    /// <summary>
    /// What the generator did, written to IMF.log after each load.
    /// </summary>
    public sealed class GeneratorReport
    {
        public string? Skipped { get; set; }

        public int Clans { get; set; }
        public int Covered { get; set; }
        public int Donors { get; set; }
        public int Converted { get; set; }
        public int OrphansDropped { get; set; }
        public int ForcedRemoved { get; set; }
        public int ForeignDropped { get; set; }      // our own rows that got dropped because the base map isn't loaded
        public int DisabledDropped { get; set; }
        public int ScenesSet { get; set; }
        public int PositionsRestored { get; set; }

        public List<string> Short { get; } = new List<string>();
        public List<string> Lines { get; } = new List<string>();

        public string Summary() =>
            Skipped != null
                ? "[Hideouts] skipped: " + Skipped
                : $"[Hideouts] clans={Clans} covered={Covered} donors={Donors} converted={Converted} " +
                  $"short=[{string.Join(",", Short)}] orphans_dropped={OrphansDropped} forced_removed={ForcedRemoved} foreign_dropped={ForeignDropped} disabled_dropped={DisabledDropped} scenes_set={ScenesSet}";

        public string SavedSummary() =>
            Skipped != null
                ? "[Hideouts] saved game: skipped: " + Skipped
                : $"[Hideouts] saved game: converted={Converted} positions_restored={PositionsRestored} orphans_dropped={OrphansDropped} foreign_dropped={ForeignDropped} disabled_dropped={DisabledDropped} scenes_set={ScenesSet}";
    }

    /// <summary>
    /// Ids of camps converted from bandit hideouts. These keep the spot the map gave them and are never moved by HideoutPlacement.
    /// </summary>
    public static class GeneratedHideouts
    {
        private static readonly HashSet<string> Ids = new HashSet<string>(StringComparer.Ordinal);

        public static void Add(string id)
        {
            if (!string.IsNullOrEmpty(id))
                Ids.Add(id);
        }

        public static bool Contains(string id) => !string.IsNullOrEmpty(id) && Ids.Contains(id);

        public static void Clear() => Ids.Clear();
    }

    /// <summary>
    /// A clan as read from the factions XML, before any Clan object exists.
    /// </summary>
    public sealed class ClanInfo
    {
        public string Id { get; set; } = "";
        public string Culture { get; set; } = "";   // raw attribute, e.g. "Culture.darshi"
        public string? HomeId { get; set; }
        public bool IsMinor { get; set; }
        public bool IsNomad { get; set; }
        public bool IsBandit { get; set; }
        public bool IsPlayer { get; set; }
    }

    /// <summary>
    /// Clans, bandit cultures and mf_data camp counts captured from the XML as it loads, for the generator to plan with.
    /// </summary>
    public sealed class ClanRegistry
    {
        private readonly Dictionary<string, ClanInfo> _clans = new Dictionary<string, ClanInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _activeHideouts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _banditCultures = new HashSet<string>(StringComparer.Ordinal);

        public int Count => _clans.Count;

        public int BanditCultureCount => _banditCultures.Count;

        public bool IsBanditCulture(string cultureRef) => _banditCultures.Contains(cultureRef);

        public IEnumerable<ClanInfo> All => _clans.Values;

        public bool Contains(string id) => _clans.ContainsKey(id);

        public int? ActiveHideoutsFor(string id) => _activeHideouts.TryGetValue(id, out int n) ? n : (int?)null;

        public void Clear()
        {
            _clans.Clear();
            _activeHideouts.Clear();
            _banditCultures.Clear();
        }

        public void CaptureCultures(XmlDocument culturesDoc)
        {
            foreach (XmlElement c in culturesDoc.GetElementsByTagName("Culture"))
            {
                string id = c.GetAttribute("id");
                if (id.Length > 0 && Flag(c, "is_bandit"))
                    _banditCultures.Add("Culture." + id);
            }
        }

        /// <summary>
        /// Reads every clan from a factions document.
        /// </summary>
        public void Capture(XmlDocument factionsDoc)
        {
            foreach (XmlElement f in factionsDoc.GetElementsByTagName("Faction"))
            {
                string id = f.GetAttribute("id");
                if (id.Length == 0)
                    continue;
                string owner = f.GetAttribute("owner");
                string home = StripPrefix(f.GetAttribute("initial_home_settlement"), "Settlement.");
                _clans[id] = new ClanInfo
                {
                    Id = id,
                    Culture = f.GetAttribute("culture"),
                    HomeId = home.Length > 0 ? home : null,
                    IsMinor = Flag(f, "is_minor_faction"),
                    IsNomad = Flag(f, "is_nomad"),
                    IsBandit = Flag(f, "is_bandit"),
                    IsPlayer = id == "player_faction" || owner.StartsWith("Hero.main_hero", StringComparison.Ordinal),
                };
            }
        }

        /// <summary>
        /// Reads num_active_hideouts per clan from mf_data.xml.
        /// </summary>
        public void CaptureMfData(XmlDocument mfDoc)
        {
            foreach (XmlElement m in mfDoc.GetElementsByTagName("MFData"))
            {
                string clan = StripPrefix(m.GetAttribute("minor_faction"), "Faction.");
                if (clan.Length == 0)
                    continue;
                if (int.TryParse(m.GetAttribute("num_active_hideouts"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                    _activeHideouts[clan] = n;
            }
        }

        internal static string StripPrefix(string value, string prefix) =>
            value.StartsWith(prefix, StringComparison.Ordinal) ? value.Substring(prefix.Length) : value;

        private static bool Flag(XmlElement e, string attr) =>
            string.Equals(e.GetAttribute(attr), "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads clan ids from another mod's CustomPartySpawns.xml, those clans spawn their own parties.
    /// </summary>
    public static class ExternalSpawns
    {
        public const string FileName = "CustomPartySpawns.xml";

        /// <summary>
        /// Gets each spawn_clan id in the file once, in file order. Broken XML gives an empty list.
        /// </summary>
        public static IEnumerable<string> ParseClanIds(string xml)
        {
            var found = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                foreach (XmlNode node in doc.SelectNodes("//*[@spawn_clan]")!)
                {
                    string id = ClanRegistry.StripPrefix(node.Attributes!["spawn_clan"]!.Value.Trim(), "Faction.");
                    if (id.Length > 0 && seen.Add(id))
                        found.Add(id);
                }
            }
            catch (XmlException e)
            {
                IMFDiagnostics.Verbose($"[Hideouts] spawn list is not valid xml: {e.Message}");
            }
            return found;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Adds extra names to minor cultures with short name lists, so their heroes don't all share a few names.
    /// </summary>
    [HarmonyPatch(typeof(MBObjectManager))]
    [HarmonyPatch("LoadXml")]
    public class MinorCultureNamesPatch
    {
        static void Postfix(XmlDocument doc)
        {
            try
            {
                string? root = doc?.DocumentElement?.Name;
                if (root != "SPCultures" && root != "Cultures")
                    return;
                foreach (string id in CultureIds)
                    Extend(id);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("[Names] could not extend the minor culture names:", e);
            }
        }

        private static void Extend(string cultureId)
        {
            CultureObject? culture = MBObjectManager.Instance?.GetObject<CultureObject>(cultureId);
            if (culture == null)
                return;
            int male = AddTo(culture, "_maleNameList", ExtraNames(cultureId, false));
            int female = AddTo(culture, "_femaleNameList", ExtraNames(cultureId, true));
            if (male + female > 0)
                IMFDiagnostics.Verbose($"[Names] {cultureId}: +{male} male, +{female} female first names");
        }

        private static int AddTo(CultureObject culture, string field, IReadOnlyList<string> extras)
        {
            List<TextObject>? list = AccessTools.Field(typeof(CultureObject), field)?.GetValue(culture) as List<TextObject>;
            if (list == null)
                return 0;
            List<string> missing = MissingNames(list.Select(t => t?.ToString() ?? ""), extras);
            foreach (string name in missing)
                list.Add(new TextObject("{=!}" + name));
            return missing.Count;
        }

        private static readonly Dictionary<string, string[]> Male = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "nord", new[] { "Aksel", "Bjorn", "Olafr", "Helgerad", "Ole", "Gundar", "Haraldr", "Hjalmar", "Njal", "Leif", "Lethwyn", "Guthric" } },
            { "vakken", new[] { "Tapio", "Tuisko", "Aamu", "Aarre", "Aimo", "Airi", "Arvo", "Eero", "Heino", "Hanno", "Liro", "Jalo", "Iskko", "Joukko", "Kaapo", "Keijo", "Kauko", "Lari" } },
            { "darshi", new[] { "Bahram", "Shahbur", "Sargon", "Hormizhd", "Ardashar", "Kavadh", "Khosrau", "Balash", "Sarhad", "Esarhaddon", "Oshalimahun", "Zar-Adan-Ekhi", "Nabu-Ikbi", "Kudur-Yutsur", "Nabu-Ekhi-Erba", "Kanun", "Mannu-Ki-Ashur", "Namtar", "Nergal-Edir", "Nergal-Sar-Ussr", "Nergiglissar", "Nidintabel", "Shagaraktiyash", "Sisuthrus", "Kasu-Nadin-Ahu", "Merodak-Mu-Basa", "Misa-Nana-Kal-Ammi", "Mulaghunnuna", "Ul-Khum-Zabu", "Idadu", "Nabonahid", "Charbil", "Sayad", "Khamis", "Zeyno", "Adrahasis", "Annunitum", "Akku", "Furqan", "Farzad" } },
        };

        private static readonly Dictionary<string, string[]> Female = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "nord", new[] { "Astrith", "Kara", "Elsa", "Kelsea", "Freja", "Annika", "Ulla", "Kay" } },
            { "vakken", new[] { "Laila", "Aira", "Alli", "Ella", "Elina", "Elsa", "Fanni", "Hanna", "Heidi", "Likka", "Lina", "Liris", "Lahja", "Kukka", "Lauri" } },
            { "darshi", new[] { "Hedu", "Sharukina", "Ashurina", "Arbella", "Larsa", "Deqlat", "Sahdah", "Nviya", "Nahrain", "Tauno", "Khaya", "Warda", "Ramina", "Warduni", "Ilona", "Neesan", "Nuri-Shapira", "Dawa", "En-hedu-anna", "Naram-sin", "Tashlultum", "Agusaya", "Raga" } },
        };

        internal static IEnumerable<string> CultureIds => Male.Keys.Union(Female.Keys, StringComparer.Ordinal);

        internal static IReadOnlyList<string> ExtraNames(string cultureId, bool female)
        {
            Dictionary<string, string[]> table = female ? Female : Male;
            return cultureId != null && table.TryGetValue(cultureId, out string[]? names) ? names : Array.Empty<string>();
        }

        /// <summary>
        /// Gets the extra names the culture doesn't have yet.
        /// </summary>
        internal static List<string> MissingNames(IEnumerable<string>? existing, IEnumerable<string>? extras)
        {
            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (existing != null)
                foreach (string name in existing)
                    if (!string.IsNullOrWhiteSpace(name))
                        have.Add(name.Trim());

            var missing = new List<string>();
            if (extras != null)
                foreach (string name in extras)
                {
                    string trimmed = name?.Trim() ?? "";
                    if (trimmed.Length > 0 && have.Add(trimmed))
                        missing.Add(trimmed);
                }
            return missing;
        }
    }

}

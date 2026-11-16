using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Turns the map's own bandit hideouts into minor faction camps while settlements are still being read from XML,
    /// using the options in imf_hideouts.xml. When a save is loaded, the camps are rebuilt the way they were saved.
    /// </summary>
    [HarmonyPatch(typeof(MBObjectManager))]
    [HarmonyPatch("LoadXml")]
    public class LoadXmlHideoutsPatch
    {
        internal static readonly ClanRegistry Registry = new ClanRegistry();
        internal static readonly HashSet<string> SpawnGatedClans = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Reads the clans other modules only spawn later, so their camps wait for them instead of being made at the start.
        /// </summary>
        private static void FindSpawnGatedClans()
        {
            SpawnGatedClans.Clear();
            try
            {
                foreach (string module in TaleWorlds.Engine.Utilities.GetModulesNames())
                {
                    string file = System.IO.Path.Combine(TaleWorlds.ModuleManager.ModuleHelper.GetModuleFullPath(module), ExternalSpawns.FileName);
                    if (!System.IO.File.Exists(file))
                        continue;
                    foreach (string id in ExternalSpawns.ParseClanIds(System.IO.File.ReadAllText(file)))
                        SpawnGatedClans.Add(id);
                }
                if (SpawnGatedClans.Count > 0)
                    IMFDiagnostics.Info("[Hideouts] spawn-gated clans: " + string.Join(", ", SpawnGatedClans));
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("FindSpawnGatedClans failed:", e);
            }
        }


        [HarmonyPriority(Priority.First)]
        static void Prefix(XmlDocument doc, bool isDevelopment)
        {
            try
            {
                string? rootName = doc?.DocumentElement?.Name;
                if (doc == null || rootName == null)
                    return;

                if (rootName == "Factions")
                {
                    Registry.Capture(doc);
                    FindSpawnGatedClans();
                    IMFDiagnostics.Info($"[Hideouts] Factions document captured, registry clans={Registry.Count}");
                    return;
                }
                if (rootName == "MFDatas")
                {
                    Registry.CaptureMfData(doc);
                    return;
                }
                if (rootName == "SPCultures" || rootName == "Cultures")
                {
                    Registry.CaptureCultures(doc);
                    IMFDiagnostics.Info($"[Hideouts] {rootName} document captured, bandit cultures={Registry.BanditCultureCount}");
                    return;
                }
                if (rootName != "Settlements")
                    return;

                if (IsSavedCampaign())
                {
                    // a load skips Factions, so read the spawn-gated clans from the module files
                    FindSpawnGatedClans();
                    ApplyFromSave(doc);
                    return;
                }

                GeneratorOptions options = GeneratorOptions.Load(OptionsPath());
                GeneratorReport report = HideoutGenerator.Apply(doc, Registry, options, ClanExists, BuildContext(doc), ConfigLoader.View);
                foreach (string line in report.Lines)
                    IMFDiagnostics.Verbose("[Hideouts] " + line);
                IMFDiagnostics.Info(report.Summary());
            }
            catch (Exception ex)
            {
                IMFDiagnostics.Log("[Hideouts] failed, Settlements document left as loaded:", ex);
            }
        }

        /// <summary>
        /// Checks if the settlements are being read for a saved game rather than a new one.
        /// </summary>
        // Settlements is reread on load but Factions isn't, so follow the save (HideoutGenerator.ApplySaved)
        private static bool IsSavedCampaign()
        {
            try
            {
                return Campaign.Current != null
                    && Campaign.Current.CampaignGameLoadingType == Campaign.GameLoadingType.SavedCampaign;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Verbose($"[Hideouts] could not tell a loaded save from a new campaign: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Rebuilds the camps from the save's own settlements, since Settlements is read again on load but Factions isn't.
        /// </summary>
        private static void ApplyFromSave(XmlDocument doc)
        {
            int found = 0;
            GeneratorReport report = HideoutGenerator.ApplySaved(doc,
                SavedHideoutOf,
                id =>
                {
                    bool inSave = SavedSettlement(id) != null;
                    if (inSave)
                        found++;
                    return inSave;
                },
                ClanExists,
                IsBanditCulture,
                BuildContext(doc),
                ConfigLoader.View);
            foreach (string line in report.Lines)
                IMFDiagnostics.Verbose("[Hideouts] " + line);
            IMFDiagnostics.Info(report.SavedSummary() + $" settlements_found_in_save={found}");
        }

        private static Settlement? SavedSettlement(string id)
        {
            try
            {
                return MBObjectManager.Instance?.GetObject<Settlement>(id) ?? Settlement.Find(id);
            }
            catch
            {
                return null;
            }
        }

        private static SavedHideout? SavedHideoutOf(string id)
        {
            Settlement? s = SavedSettlement(id);
            if (!(s?.SettlementComponent is MinorFactionHideout mfh) || mfh.OwnerClan == null)
                return null;
            Clan owner = mfh.OwnerClan;
            Vec2 saved = mfh.SavedPosition;
            return new SavedHideout
            {
                OwnerId = owner.StringId,
                CultureRef = owner.Culture != null ? "Culture." + owner.Culture.StringId : null,
                HasPosition = mfh.HasSavedPosition,
                X = saved.X,
                Y = saved.Y
            };
        }

        private static bool IsBanditCulture(string cultureRef)
        {
            try
            {
                string id = cultureRef.StartsWith("Culture.", StringComparison.Ordinal) ? cultureRef.Substring("Culture.".Length) : cultureRef;
                return MBObjectManager.Instance?.GetObject<CultureObject>(id)?.IsBandit ?? false;
            }
            catch
            {
                return false;
            }
        }

        private static bool ClanExists(string id)
        {
            try
            {
                return MBObjectManager.Instance?.GetObject<Clan>(id) != null;
            }
            catch
            {
                return false;
            }
        }


        private static string OptionsPath() => Path.Combine(ConfigLoader.ModuleDir(), "ModuleData", "imf_hideouts.xml");

        /// <summary>
        /// Gathers what the generator needs from the loaded settlements, our shipped settlements.xml and the base game's.
        /// Our shipped rows are for the base map, so they're only used when the map in use is the base one.
        /// </summary>
        /// <returns>the context, or null if a file is missing or reading failed</returns>
        private static GeneratorContext? BuildContext(XmlDocument loaded)
        {
            try
            {
                string module = ConfigLoader.ModuleDir();
                string modules = Path.GetDirectoryName(module)!;
                string shippedPath = Path.Combine(module, "ModuleData", "settlements.xml");
                string basePath = Path.Combine(modules, "SandBox", "ModuleData", "settlements.xml");
                if (!File.Exists(shippedPath) || !File.Exists(basePath))
                    return null;

                var shipped = new XmlDocument();
                shipped.Load(shippedPath);
                var baseMap = new XmlDocument();
                baseMap.Load(basePath);

                var context = new GeneratorContext
                {
                    ShippedRowIds = GeneratorContext.SettlementIds(shipped, regularOnly: false),
                    BaseMapPresent = GeneratorContext.DetectBaseMap(
                        GeneratorContext.SettlementPositions(baseMap, regularOnly: true),
                        GeneratorContext.SettlementPositions(loaded, regularOnly: true))
                };
                IMFDiagnostics.Info($"[Hideouts] base map present={(context.BaseMapPresent.HasValue ? context.BaseMapPresent.Value.ToString() : "unknown")}, IMF ships {context.ShippedRowIds.Count} rows");
                return context;
            }
            catch (Exception ex)
            {
                IMFDiagnostics.Log("[Hideouts] base map check failed, keeping shipped rows:", ex);
                return null;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    /// <summary>
    /// Fixes minor faction heroes made from templates that have no skills or gear. They get commander skills
    /// for their clan's tier and the gear of their clan's best troop. Runs once on load and then daily per clan.
    /// </summary>
    internal class MFProvisionBehavior : CampaignBehaviorBase
    {
        private static readonly EquipmentIndex[] WeaponSlots =
        {
            EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3,
        };

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(_ => ProvisionAll()));
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(ProvisionClanSafe));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void ProvisionAll()
        {
            foreach (Clan clan in Clan.All.ToList())
                ProvisionClanSafe(clan);
        }

        /// <summary>
        /// Provisions every hero of a minor faction clan. Errors are logged, not thrown.
        /// </summary>
        private void ProvisionClanSafe(Clan clan)
        {
            try
            {
                if (clan == null || !clan.IsMinorFaction || clan.IsBanditFaction || clan.IsEliminated)
                    return;
                bool horde = HordeHelper.IsHordeClan(clan);
                foreach (Hero hero in clan.Heroes.ToList())
                    ProvisionHero(hero, clan, horde);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFProvisionBehavior clan " + clan?.StringId + " failed:", e);
            }
        }

        /// <summary>
        /// Gives a hero gear and skills if their template had none. The player is only touched if made from a minor faction template.
        /// </summary>
        /// <param name="horde">the clan runs very big parties, so its heroes get extra leadership and steward</param>
        private static void ProvisionHero(Hero hero, Clan clan, bool horde)
        {
            if (hero == null || hero.IsTemplate || !hero.IsAlive)
                return;
            CharacterObject? template = hero.Template;
            if (template == null)
                return;

            bool fromMfTemplate = clan.MinorFactionCharacterTemplates != null && clan.MinorFactionCharacterTemplates.Contains(template);
            if (hero == Hero.MainHero && !fromMfTemplate)
                return;

            bool skillsBlank = SkillsBlank(hero);
            bool equipmentBlank = EquipmentBlank(hero);
            if (!skillsBlank && !equipmentBlank)
                return;

            var report = new List<string>();
            if (equipmentBlank)
            {
                string? troop = GiveGear(hero, clan);
                if (troop != null)
                    report.Add("gear=" + troop);
            }
            if (skillsBlank)
            {
                int raised = GiveSkills(hero, clan, horde);
                if (raised > 0)
                    report.Add("skills+" + raised + (horde ? "(horde)" : ""));
            }
            if (report.Count > 0)
                IMFDiagnostics.Verbose($"[Provision] {hero.Name} ({clan.StringId}) {string.Join(" ", report)}");
        }

        /// <summary>
        /// Copies the battle gear of the clan's best troop to the hero, and to their civilian set if that is empty.
        /// </summary>
        /// <returns>the troop id used, or null if there was no gear to give</returns>
        private static string? GiveGear(Hero hero, Clan clan)
        {
            CharacterObject? troop = BestTroop(clan);
            Equipment? source = troop?.RandomBattleEquipment;
            if (troop == null || source == null || source.IsEmpty())
                return null;
            global::Helpers.EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, source.Clone(false));
            if (hero.CivilianEquipment != null && hero.CivilianEquipment.IsEmpty())
                hero.CivilianEquipment.FillFrom(source, false);
            return troop.StringId;
        }

        /// <summary>
        /// Gets the highest tier troop in the clan's party template, or the basic troop.
        /// </summary>
        private static CharacterObject? BestTroop(Clan clan)
        {
            CharacterObject? best = null;
            PartyTemplateObject? template = clan.DefaultPartyTemplate;
            if (template != null)
            {
                foreach (PartyTemplateStack stack in template.Stacks)
                {
                    if (stack.Character != null && (best == null || stack.Character.Tier > best.Tier))
                        best = stack.Character;
                }
            }
            return best ?? clan.BasicTroop;
        }

        /// <summary>
        /// Raises the hero's skills to the targets from SkillTargets, never lowers them.
        /// </summary>
        /// <returns>how many skills were raised</returns>
        private static int GiveSkills(Hero hero, Clan clan, bool horde)
        {
            var weaponSkills = new List<string>();
            bool mounted = false;
            Equipment? gear = hero.BattleEquipment;
            if (gear != null)
            {
                foreach (EquipmentIndex slot in WeaponSlots)
                {
                    string? id = gear[slot].Item?.RelevantSkill?.StringId;
                    if (!string.IsNullOrEmpty(id))
                        weaponSkills.Add(id!);
                }
                mounted = gear[EquipmentIndex.Horse].Item != null;
            }

            Dictionary<string, int> targets = SkillTargets(clan.Tier, weaponSkills, mounted, horde);
            int raised = 0;
            foreach (SkillObject skill in Skills.All)
            {
                if (targets.TryGetValue(skill.StringId, out int target) && hero.GetSkillValue(skill) < target)
                {
                    hero.SetSkillValue(skill, target);
                    raised++;
                }
            }
            return raised;
        }

        public const int HordeThreshold = 700;

        public const int HordeLeadershipFloor = 300;
        public const int HordeStewardFloor = 300;

        /// <summary>
        /// Checks if a template has all skills at 0.
        /// </summary>
        public static bool SkillsBlank(IEnumerable<int> templateSkillValues)
        {
            int[] values = templateSkillValues.ToArray();
            return values.Length > 0 && values.All(v => v <= 0);
        }

        /// <summary>
        /// Checks if the hero and every equipment set of their template are empty.
        /// </summary>
        /// <param name="templateSetsEmpty">one value per template equipment set, true if it is empty</param>
        /// <param name="heroEquipmentEmpty">the hero's battle equipment is empty</param>
        public static bool EquipmentBlank(IEnumerable<bool> templateSetsEmpty, bool heroEquipmentEmpty) =>
            heroEquipmentEmpty && templateSetsEmpty.All(e => e);

        public static int PrimaryTarget(int clanTier) => Math.Min(200, Math.Max(70, 70 + 25 * clanTier));

        public static int SecondaryTarget(int clanTier) => (int)Math.Round(PrimaryTarget(clanTier) * 0.6);

        /// <summary>
        /// Skill levels a provisioned hero should have. Leadership, steward and their weapon skills get the main value (70 to 200 by clan tier),
        /// medicine, scouting and tactics 60% of that, riding the main value if they have a horse, and horde leaders at least 300 leadership and steward.
        /// </summary>
        /// <param name="weaponSkillIds">skills of the weapons the hero carries</param>
        /// <param name="mounted">the hero has a horse</param>
        /// <param name="horde">the clan runs very big parties</param>
        /// <returns>skill id to target level</returns>
        public static Dictionary<string, int> SkillTargets(int clanTier, IEnumerable<string> weaponSkillIds, bool mounted, bool horde)
        {
            int primary = PrimaryTarget(clanTier);
            int secondary = SecondaryTarget(clanTier);
            var t = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Leadership"] = primary,
                ["Steward"] = primary,
                ["Medicine"] = secondary,
                ["Scouting"] = secondary,
                ["Tactics"] = secondary,
            };
            foreach (string id in weaponSkillIds)
                if (!string.IsNullOrEmpty(id))
                    t[id] = Math.Max(primary, t.TryGetValue(id, out int existing) ? existing : 0);
            if (mounted)
                t["Riding"] = Math.Max(primary, t.TryGetValue("Riding", out int r) ? r : 0);
            if (horde)
            {
                t["Leadership"] = Math.Max(t["Leadership"], HordeLeadershipFloor);
                t["Steward"] = Math.Max(t["Steward"], HordeStewardFloor);
            }
            return t;
        }

        public static bool IsHorde(int troops) => troops >= HordeThreshold;

        internal static bool SkillsBlank(Hero hero)
        {
            CharacterObject? template = hero?.Template;
            if (template == null)
                return false;
            return SkillsBlank(Skills.All.Select(s => template.GetSkillValue(s)));
        }

        internal static bool EquipmentBlank(Hero hero)
        {
            CharacterObject? template = hero?.Template;
            if (template == null)
                return false;
            IEnumerable<Equipment> sets = (template.BattleEquipments ?? Enumerable.Empty<Equipment>())
                .Concat(template.CivilianEquipments ?? Enumerable.Empty<Equipment>());
            IEnumerable<bool> templateSetsEmpty = sets.Select(e => e == null || e.IsEmpty());
            bool heroEmpty = hero!.BattleEquipment == null || hero.BattleEquipment.IsEmpty();
            return EquipmentBlank(templateSetsEmpty, heroEmpty);
        }
    }

    /// <summary>
    /// Tells if a minor faction runs very big parties (700 troops or more), from its party template or its actual parties.
    /// </summary>
    internal static class HordeHelper
    {
        private static readonly Dictionary<string, bool> TemplateCache = new Dictionary<string, bool>(StringComparer.Ordinal);

        internal static void ClearCache() => TemplateCache.Clear();

        /// <summary>
        /// Checks if the clan's party template can make a party of 700 or more. Cached per clan.
        /// </summary>
        internal static bool IsHordeTemplate(Clan? clan)
        {
            if (clan == null || !clan.IsMinorFaction)
                return false;
            if (TemplateCache.TryGetValue(clan.StringId, out bool cached))
                return cached;
            int max = 0;
            PartyTemplateObject? template = clan.DefaultPartyTemplate;
            if (template?.Stacks != null)
            {
                foreach (PartyTemplateStack stack in template.Stacks)
                    max += stack.MaxValue;
            }
            bool horde = MFProvisionBehavior.IsHorde(max);
            TemplateCache[clan.StringId] = horde;
            return horde;
        }

        /// <summary>
        /// Checks if a minor faction party is that big, or its clan's template is.
        /// </summary>
        internal static bool IsHordeParty(MobileParty? party)
        {
            if (party == null)
                return false;
            Clan? clan = party.ActualClan;
            if (clan == null || !clan.IsMinorFaction)
                return false;
            return MFProvisionBehavior.IsHorde(party.MemberRoster?.TotalManCount ?? 0) || IsHordeTemplate(clan);
        }

        /// <summary>
        /// Checks if any of the clan's parties is that big, or its template is.
        /// </summary>
        internal static bool IsHordeClan(Clan? clan)
        {
            if (clan == null || !clan.IsMinorFaction)
                return false;
            int actual = 0;
            foreach (WarPartyComponent war in clan.WarPartyComponents)
                actual = Math.Max(actual, war?.MobileParty?.MemberRoster?.TotalManCount ?? 0);
            return MFProvisionBehavior.IsHorde(actual) || IsHordeTemplate(clan);
        }
    }
}

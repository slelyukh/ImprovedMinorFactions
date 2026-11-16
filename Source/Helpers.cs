using System.Collections.Generic;
using System.Reflection;
using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// Small shared helpers: telling camps apart from other settlements, relations between the player and minor factions,
    /// picking a clan's entry-level troops, and reflection shortcuts for private game members.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Checks if a settlement is a minor faction camp.
        /// </summary>
        internal static bool IsMFHideout(Settlement? s)
        {
            return s != null && GetMFHideout(s) != null;
        }

        internal static bool IsDebugMode = false;

        /// <summary>
        /// Checks if every camp the clan owns is active, i.e. it has no spare camp to move to.
        /// </summary>
        internal static bool IsSingleHideoutMF(Clan c)
        {
            IMFManager.InitManagerIfNone();
            return c!= null && IMFManager.Current!.IsFullHideoutOccupationMF(c);
        }

        /// <summary>
        /// Builds the militia tooltip for a village or a camp. Throws for any other settlement.
        /// </summary>
        internal static List<TooltipProperty> GetVillageOrMFHideoutMilitiaTooltip(Settlement s)
        {
            if (s == null)
                return new List<TooltipProperty>();
            if (s.IsVillage)
            {
                return CampaignUIHelper.GetVillageMilitiaTooltip(s.Village);
            }
            else
            {
                MinorFactionHideout? mfHideout = GetMFHideout(s);
                if (mfHideout == null)
                    throw new Exception("There should not be a non Minor Faction Hideout here. (getMilitiaTooltip)");
                return CampaignUIHelper.GetSettlementPropertyTooltip(
                    s, "Militia", s.Militia, mfHideout.MilitiaChange);
            }
        }

        /// <summary>
        /// Checks if a hero is a notable living in a camp. Camp notables are preachers who aren't lords.
        /// </summary>
        internal static bool IsMFNotable(Hero h)
        {
            return h != null && IsMFHideout(h.CurrentSettlement) && !h.IsLord && h.Occupation == Occupation.Preacher;
        }

        /// <summary>
        /// Builds the hearth tooltip for a camp. Throws for settlements that aren't villages or camps.
        /// </summary>
        internal static List<TooltipProperty> GetVillageOrMFHideoutProsperityTooltip(Settlement s)
        {
            if (s.IsVillage)
            {
                return CampaignUIHelper.GetVillageMilitiaTooltip(s.Village);
            }
            else
            {
                MinorFactionHideout? mfHideout = GetMFHideout(s);
                if (mfHideout == null)
                    throw new Exception("There should not be a non Minor Faction Hideout here. (getProsperityTooltip)");
                return CampaignUIHelper.GetSettlementPropertyTooltip(
                    s, "Hearth", mfHideout.Hearth, mfHideout.HearthChange);
            }
        }

        internal static bool IsEnemyOfMinorFaction(IFaction kingdom, Clan minorFaction)
        {
            return kingdom.IsAtWarWith(minorFaction);
        }

        /// <summary>
        /// Checks if a kingdom treats an outlaw minor faction as a rival it is always at war with.
        /// </summary>
        // a minor faction is a rival of a Kingdom if it is an Outlaw faction and shares a culture with the Kingdom
        // these factions are always at war unless the Minor Faction is someone's mercenary
        internal static bool ConsidersMFOutlaw(IFaction faction, Clan minorFaction)
        {
            return faction != null && minorFaction != null
                && minorFaction.IsOutlaw 
                && !faction.IsMinorFaction
                && (faction.Culture == minorFaction.Culture 
                  || (minorFaction.Culture.StringId == "vakken" && faction.Culture.StringId == "sturgia"));
        }

        /// <summary>
        /// Checks if the player's relation with the clan is high enough to be a friend of its camps.
        /// </summary>
        internal static bool IsPlayerFriendOfMF(Clan minorFaction)
        {
            return Clan.PlayerClan.GetRelationWithClan(minorFaction) >= IMFModels.MinRelationToBeMFHFriend;
        }

        /// <summary>
        /// Checks if a clan is the player's own clan, is led by or includes the player, or has the highest relation with the player.
        /// Good relations alone don't count, those camps can still be attacked.
        /// </summary>
        internal static bool IsFriendlyToPlayer(Clan minorFaction)
        {
            if (minorFaction == null)
                return false;
            // Clan.PlayerClan can still be the old clan right at the start, go through MainHero instead.
            Clan own = Hero.MainHero.Clan;
            return minorFaction == Clan.PlayerClan
                || minorFaction == own
                || minorFaction.Leader == Hero.MainHero
                || minorFaction.Heroes.Contains(Hero.MainHero)
                || (own ?? Clan.PlayerClan).GetRelationWithClan(minorFaction) >= MaxRelation;
        }

        private const int MaxRelation = 100;

        /// <summary>
        /// Checks if a camp belongs to a clan that is friendly to the player by IsFriendlyToPlayer.
        /// </summary>
        internal static bool IsFriendlyCamp(Settlement? camp)
        {
            if (camp == null)
                return false;
            return IsFriendlyToPlayer(camp.OwnerClan) || IsFriendlyToPlayer(GetMFHideout(camp)?.OwnerClan!);
        }

        /// <summary>
        /// Gets a settlement's camp component.
        /// </summary>
        /// <returns>the camp, or null if the settlement isn't one</returns>
        internal static MinorFactionHideout? GetMFHideout(Settlement? s)
        {
            var setComp = s?.SettlementComponent;
            if (setComp == null || setComp is Town || setComp is Village || setComp is RetirementSettlementComponent)
                return null;
            return s?.SettlementComponent as MinorFactionHideout;
        }

        /// <summary>
        /// Checks for the cultures that only minor factions use.
        /// </summary>
        // Nord removed due to only having 2 names for their culture...
        internal static bool IsMinorCulture(CultureObject c)
        {
            return c.StringId == "vakken" || c.StringId == "darshi" || c.StringId == "nord";
        }

        /// <summary>
        /// Removes regular militia troops from a camp's militia party, camps should only have the clan's own troops.
        /// </summary>
        /// <returns>how many troops were removed</returns>
        // Removes any normal militia units from MFHideout militia party
        internal static int removeMilitiaImposters(Settlement s)
        {
            if (!IsMFHideout(s))
                return 0;
            MobileParty militiaParty = s.MilitiaPartyComponent.MobileParty;
            var militiaRoster = militiaParty.MemberRoster;
            int removedCount = 0;
            for (int i = 0; i < militiaRoster.Count; i++)
            {
                if (IsMilitiaOfCulture(militiaRoster.GetElementCopyAtIndex(i).Character, s.Culture))
                {
                    removedCount += militiaRoster.GetElementNumber(i);
                    militiaRoster.AddToCountsAtIndex(i, -militiaRoster.GetElementNumber(i));
                }
            }
            militiaRoster.RemoveZeroCounts();
            return removedCount;
        }

        /// <summary>
        /// Checks if a troop is one of a culture's militia troops, or has militia in its id.
        /// </summary>
        internal static bool IsMilitiaOfCulture(CharacterObject troop, CultureObject culture)
        {
            return troop.StringId.Contains("militia") || troop == culture.MeleeMilitiaTroop || troop == culture.RangedMilitiaTroop || troop == culture.MeleeEliteMilitiaTroop || troop == culture.RangedEliteMilitiaTroop;
        }

        /// <summary>
        /// Makes the notables in the player's camps supporters of the player's clan, used when the player plays as a minor faction.
        /// </summary>
        /// <returns>how many notables changed</returns>
        internal static int MakeHideoutNotablesSupportPlayerClan()
        {
            Clan player = Clan.PlayerClan;
            if (player == null || !player.IsMinorFaction || IMFManager.Current == null)
                return 0;
            MFData? data = IMFManager.Current.GetClanMFData(player);
            if (data?.Hideouts == null)
                return 0;
            int changed = 0;
            foreach (MinorFactionHideout hideout in data.Hideouts)
            {
                Settlement? settlement = hideout?.Settlement;
                if (settlement == null)
                    continue;
                foreach (Hero notable in new List<Hero>(settlement.Notables))
                {
                    if (notable != null && notable.IsAlive && notable.SupporterOf != player)
                    {
                        notable.SupporterOf = player;
                        changed++;
                    }
                }
            }
            return changed;
        }

        /// <summary>
        /// Gets a clan's basic troop, working it out from the clan's party template first if it is still the culture default.
        /// </summary>
        internal static CharacterObject? GetBasicTroop(Clan minorFaction)
        {
            if (minorFaction == null) return null;
            if (minorFaction.BasicTroop == null || minorFaction.BasicTroop == minorFaction.Culture?.BasicTroop)
                DetermineBasicTroopsForMinorFactionsCopypasta();
            return minorFaction.BasicTroop;
        }

        /// <summary>
        /// Gets the lowest-tier troops in the clan's party template, weighted by their stack size.
        /// Falls back to the basic troop if the template has none.
        /// </summary>
        internal static List<(CharacterObject Troop, double Weight)> GetEntryTroops(Clan clan)
        {
            var result = new List<(CharacterObject, double)>();
            var byId = new Dictionary<string, CharacterObject>();
            var stacks = new List<TroopStackInfo>();
            if (clan?.DefaultPartyTemplate != null && clan.DefaultPartyTemplate.Stacks != null)
            {
                foreach (PartyTemplateStack stack in clan.DefaultPartyTemplate.Stacks)
                {
                    CharacterObject? troop = stack.Character;
                    if (troop == null)
                        continue;
                    byId[troop.StringId] = troop;
                    stacks.Add(new TroopStackInfo(troop.StringId, troop.Level, (stack.MinValue + stack.MaxValue) / 2.0));
                }
            }
            foreach (TroopStackInfo entry in LowestTierStacks(stacks))
                result.Add((byId[entry.Id], entry.Weight));
            
            if (result.Count == 0 && clan != null)
            {
                CharacterObject? basic = GetBasicTroop(clan);
                if (basic != null)
                    result.Add((basic, 1.0));
            }
            return result;
        }

        /// <summary>
        /// Picks one entry-level troop at random, weighted by stack size.
        /// </summary>
        /// <returns>the troop, or null if the clan has none</returns>
        internal static CharacterObject? GetEntryTroop(Clan clan)
        {
            List<(CharacterObject Troop, double Weight)> entries = GetEntryTroops(clan);
            int at = PickByWeight(AsInfos(entries), MBRandom.RandomFloat);
            return at < 0 ? null : entries[at].Troop;
        }

        /// <summary>
        /// Splits a number of recruits between the clan's entry-level troops by weight.
        /// </summary>
        /// <param name="count">total troops to split</param>
        internal static List<(CharacterObject Troop, int Count)> SplitEntryTroops(Clan clan, int count)
        {
            var parts = new List<(CharacterObject, int)>();
            List<(CharacterObject Troop, double Weight)> entries = GetEntryTroops(clan);
            int[] split = SplitByWeight(AsInfos(entries), count);
            for (int i = 0; i < split.Length; i++)
                if (split[i] > 0)
                    parts.Add((entries[i].Troop, split[i]));
            return parts;
        }

        private static List<TroopStackInfo> AsInfos(List<(CharacterObject Troop, double Weight)> entries)
        {
            var infos = new List<TroopStackInfo>(entries.Count);
            foreach ((CharacterObject troop, double weight) in entries)
                infos.Add(new TroopStackInfo(troop.StringId, troop.Level, weight));
            return infos;
        }

        /// <summary>
        /// Sets each minor faction's basic troop to the lowest-level troop in its party template,
        /// for clans still using their culture's basic troop.
        /// </summary>
        // copypasta from ClanVariablesCampaignBehavior
        internal static void DetermineBasicTroopsForMinorFactionsCopypasta()
        {
            if (Clan.All == null) return;
            foreach (Clan clan in Clan.All)
            {
                if (clan == null || !clan.IsMinorFaction)
                    continue;
                if (clan.BasicTroop == null || clan.Culture?.BasicTroop == null || clan.DefaultPartyTemplate == null
                    || clan.BasicTroop != clan.Culture.BasicTroop)
                    continue;
                CharacterObject? basicTroop = null;
                PartyTemplateObject defaultPartyTemplate = clan.DefaultPartyTemplate;
                if (defaultPartyTemplate.Stacks == null)
                    continue;
                int minLevel = 50;
                foreach (PartyTemplateStack partyTemplateStack in defaultPartyTemplate.Stacks)
                {
                    if (partyTemplateStack.Character == null)
                        continue;
                    int level = partyTemplateStack.Character.Level;
                    if (level < minLevel)
                    {
                        minLevel = level;
                        basicTroop = partyTemplateStack.Character;
                    }
                }
                if (basicTroop != null)
                    clan.BasicTroop = basicTroop;
            }
        }

        /// <summary>
        /// Sets a private instance field by name.
        /// </summary>
        internal static void setPrivateField<I, V>(I instance, string field, V value)
        {
            FieldInfo fieldInfo = typeof(I)!.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;
            fieldInfo!.SetValue(instance, value);
        }

        /// <summary>
        /// Calls a private method by name.
        /// </summary>
        /// <param name="instance">object to call it on, null for a static method</param>
        /// <param name="args">the method's arguments</param>
        /// <param name="type">the type that has the static method, leave null for instance methods</param>
        /// <returns>what the method returned</returns>
        // If method is static use null for instance and provide a type
        internal static object? CallPrivateMethod(object? instance, string methodName, object[] args, Type? type = null)
        {
            MethodInfo? method;
            if (type == null)
                method = instance?.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            else
                method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            return method?.Invoke(instance, args);
        }

        /// <summary>
        /// Sets which battle side the player and the opponent are on in an encounter, the setters are private.
        /// </summary>
        internal static void SetPlayerEncounterSides(TaleWorlds.CampaignSystem.Encounters.PlayerEncounter? encounter, BattleSideEnum playerSide, BattleSideEnum opponentSide)
        {
            if (encounter == null) return;
            CallPrivateMethod(encounter, "set_PlayerSide", new object[] { playerSide });
            CallPrivateMethod(encounter, "set_OpponentSide", new object[] { opponentSide });
        }

        public static bool mfIsMounted(Clan minorFaction)
        {
            return minorFaction.BasicTroop.IsMounted;
        }

        /// <summary>
        /// Checks if a clan owns at least one camp.
        /// </summary>
        internal static bool HasMFHideout(Clan clan)
        {
            return (IMFManager.Current!.GetClanMFData(clan)?.Hideouts.Count ?? 0) > 0;
        }

        // tier 1 is level 6 and every 5 levels after that is another tier
        internal const int TierSpan = 5;

        /// <summary>
        /// Keeps the stacks within one tier of the lowest level troop and merges repeats of the same troop.
        /// </summary>
        /// <returns>one entry per troop, weighted by stack size</returns>
        internal static List<TroopStackInfo> LowestTierStacks(IEnumerable<TroopStackInfo> stacks)
        {
            var all = new List<TroopStackInfo>();
            if (stacks != null)
                foreach (TroopStackInfo s in stacks)
                    if (!string.IsNullOrEmpty(s.Id))
                        all.Add(s);
            if (all.Count == 0)
                return all;

            int minLevel = int.MaxValue;
            foreach (TroopStackInfo s in all)
                minLevel = Math.Min(minLevel, s.Level);

            var entries = new List<TroopStackInfo>();
            foreach (TroopStackInfo s in all)
            {
                if (s.Level >= minLevel + TierSpan)
                    continue;
                double weight = Math.Max(1.0, s.Weight);
                int at = entries.FindIndex(e => e.Id == s.Id);
                if (at >= 0)
                    entries[at] = new TroopStackInfo(s.Id, entries[at].Level, entries[at].Weight + weight);
                else
                    entries.Add(new TroopStackInfo(s.Id, s.Level, weight));
            }
            return entries;
        }

        /// <summary>
        /// Picks an entry by weight.
        /// </summary>
        /// <param name="roll">random number from 0 to 1</param>
        /// <returns>the index picked, or -1 if the list is empty</returns>
        internal static int PickByWeight(IReadOnlyList<TroopStackInfo> entries, double roll)
        {
            if (entries == null || entries.Count == 0)
                return -1;
            double total = 0;
            foreach (TroopStackInfo e in entries)
                total += e.Weight;
            double at = Math.Min(Math.Max(roll, 0.0), 0.999999999) * total;
            for (int i = 0; i < entries.Count; i++)
            {
                at -= entries[i].Weight;
                if (at < 0)
                    return i;
            }
            return entries.Count - 1;
        }

        /// <summary>
        /// Splits a count between entries by weight. Leftovers from rounding go to the entries that were closest to getting one more.
        /// </summary>
        /// <returns>the count for each entry, in the same order</returns>
        internal static int[] SplitByWeight(IReadOnlyList<TroopStackInfo> entries, int count)
        {
            int n = entries?.Count ?? 0;
            var parts = new int[n];
            if (n == 0 || count <= 0)
                return parts;

            double total = 0;
            foreach (TroopStackInfo e in entries!)
                total += e.Weight;

            var remainder = new double[n];
            int given = 0;
            for (int i = 0; i < n; i++)
            {
                double exact = count * entries[i].Weight / total;
                parts[i] = (int)Math.Floor(exact);
                remainder[i] = exact - parts[i];
                given += parts[i];
            }
            for (int left = count - given; left > 0; left--)
            {
                int best = 0;
                for (int i = 1; i < n; i++)
                    if (remainder[i] > remainder[best])
                        best = i;
                parts[best]++;
                remainder[best] = -1;
            }
            return parts;
        }
    }

    /// <summary>
    /// A troop from a party template, with its level and stack size.
    /// </summary>
    internal readonly struct TroopStackInfo
    {
        public TroopStackInfo(string id, int level, double weight)
        {
            Id = id;
            Level = level;
            Weight = weight;
        }

        public string Id { get; }
        public int Level { get; }
        public double Weight { get; }   // average stack size, used as the weight
    }

}

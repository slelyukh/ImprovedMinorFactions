using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace ImprovedMinorFactions
{
    public static class Helpers
    {
        internal static bool isMFHideout(Settlement s)
        {
            return s != null && GetMFHideout(s) != null;
        }

        internal static List<TooltipProperty> GetVillageOrMFHideoutMilitiaTooltip(Settlement s)
        {
            if (s.IsVillage) {
                return CampaignUIHelper.GetVillageMilitiaTooltip(s.Village);
            } else {
                MinorFactionHideout? mfHideout = GetMFHideout(s);
                if (mfHideout == null)
                    throw new Exception("There should not be a non Minor Faction Hideout here. (getMilitiaTooltip)");
                return CampaignUIHelper.GetSettlementPropertyTooltip(
                    s, "Militia", s.Militia, mfHideout.MilitiaChange);
            }
        }

        internal static List<TooltipProperty> GetVillageOrMFHideoutProsperityTooltip(Settlement s)
        {
            if (s.IsVillage) {
                return CampaignUIHelper.GetVillageMilitiaTooltip(s.Village);
            } else {
                MinorFactionHideout? mfHideout = GetMFHideout(s);
                if (mfHideout == null)
                    throw new Exception("There should not be a non Minor Faction Hideout here. (getprosperityTooltip)");
                return CampaignUIHelper.GetSettlementPropertyTooltip(
                    s, "Hearth", mfHideout.Hearth, mfHideout.HearthChange);
            }
        }

        internal static bool IsEnemyOfMinorFaction(IFaction kingdom, Clan minorFaction)
        {
            return minorFaction.IsOutlaw && kingdom.IsAtWarWith(minorFaction);
        }

        internal static bool IsRivalOfMinorFaction(Kingdom kingdom, Clan minorFaction)
        {
            return kingdom != null && minorFaction.IsOutlaw && kingdom.Culture == minorFaction.Culture;
        }

        internal static MinorFactionHideout GetMFHideout(Settlement s)
        {
            return s.SettlementComponent as MinorFactionHideout;
        }
        internal static bool IsMFClanInitialized(Clan c)
        {
            return c.Culture != null && c.BasicTroop != c.Culture.BasicTroop;
        }

        internal static int removeMilitiaImposters(Settlement s)
        {
            if (!isMFHideout(s))
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

        internal static bool IsMilitiaOfCulture(CharacterObject troop, CultureObject culture)
        {
            return troop.StringId.Contains("militia") || troop == culture.MeleeMilitiaTroop || troop == culture.RangedMilitiaTroop || troop == culture.MeleeEliteMilitiaTroop || troop == culture.RangedEliteMilitiaTroop;
        }

        internal static CharacterObject GetBasicTroop(Clan minorFaction)
        {
            if (minorFaction.BasicTroop == minorFaction.Culture.BasicTroop)
                DetermineBasicTroopsForMinorFactionsCopypasta();
            return minorFaction.BasicTroop;
        }

        // copypasta from ClanVariablesCampaignBehavior
        internal static void DetermineBasicTroopsForMinorFactionsCopypasta()
        {
            foreach (Clan clan in Clan.All)
            {
                if (clan.IsMinorFaction)
                {
                    CharacterObject basicTroop = null;
                    PartyTemplateObject defaultPartyTemplate = clan.DefaultPartyTemplate;
                    int minLevel = 50;
                    foreach (PartyTemplateStack partyTemplateStack in defaultPartyTemplate.Stacks)
                    {
                        int level = partyTemplateStack.Character.Level;
                        if (level < minLevel)
                        {
                            minLevel = level;
                            basicTroop = partyTemplateStack.Character;
                        }
                    }
                    clan.BasicTroop = basicTroop;
                }
            }
        }

        internal static void setPrivateField<I, V>(I instance, string field, V value)
        {
            FieldInfo fieldInfo = typeof(I).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(instance, value);
        }

        // if method is static use null for instance and provide a type
        internal static object callPrivateMethod(object instance, string methodName, object[] args, Type type = null)
        {
            MethodInfo method;
            if (type == null)
                method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            else
                method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(instance, args);
        }

        public static bool mfIsMounted(Clan minorFaction)
        {
            var name = minorFaction.StringId;
            return name == "ghilman" || name == "jawwal" || name == "eleftheroi" || name == "karakhuzaits";
        }


    }
}

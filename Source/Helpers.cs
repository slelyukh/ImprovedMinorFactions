using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace ImprovedMinorFactions
{
    public static class Helpers
    {
        internal static bool IsMFHideout(Settlement s)
        {
            return s != null && GetMFHideout(s) != null;
        }

        internal static bool IsDebugMode = false;

        internal static bool IsSingleHideoutMF(Clan c)
        {
            IMFManager.InitManagerIfNone();
            return c!= null && IMFManager.Current.IsFullHideoutOccupationMF(c);
        }

        internal static List<TooltipProperty> GetVillageOrMFHideoutMilitiaTooltip(Settlement s)
        {
            if (s == null)
                return new List<TooltipProperty>();
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

        internal static bool IsMFGangLeader(Hero h)
        {
            return h != null && IsMFHideout(h.CurrentSettlement) && !h.IsLord && h.Occupation == Occupation.GangLeader;
        }

        internal static List<TooltipProperty> GetVillageOrMFHideoutProsperityTooltip(Settlement s)
        {
            if (s.IsVillage) {
                return CampaignUIHelper.GetVillageMilitiaTooltip(s.Village);
            } else {
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

        // a minor faction is a rival of a Kingdom if it is an Outlaw faction and shares a culture with the Kingdom
        // these factions are always at war unless the Minor Faction is someone's mercenary
        internal static bool IsRivalOfMinorFaction(IFaction faction, Clan minorFaction)
        {
            return faction != null && minorFaction != null
                && minorFaction.IsOutlaw 
                && (faction.Culture == minorFaction.Culture 
                || (minorFaction.Culture.GetCultureCode() == CultureCode.Vakken && faction.Culture.GetCultureCode() == CultureCode.Sturgia));
        }

        internal static MinorFactionHideout GetMFHideout(Settlement s)
        {
            return s?.SettlementComponent as MinorFactionHideout;
        }

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

        internal static bool IsMilitiaOfCulture(CharacterObject troop, CultureObject culture)
        {
            return troop.StringId.Contains("militia") || troop == culture.MeleeMilitiaTroop || troop == culture.RangedMilitiaTroop || troop == culture.MeleeEliteMilitiaTroop || troop == culture.RangedEliteMilitiaTroop;
        }

        internal static CharacterObject GetBasicTroop(Clan minorFaction)
        {
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
                    if (clan.BasicTroop == null 
                        || clan.Culture?.BasicTroop == null 
                        || clan.BasicTroop != clan.Culture.BasicTroop
                        || clan.DefaultPartyTemplate == null)
                        continue;
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

        // If method is static use null for instance and provide a type
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
            return minorFaction.BasicTroop.IsMounted;
        }


    }
}

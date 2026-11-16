using HarmonyLib;
using System.Xml;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using System;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// A camp's faction is its owner clan.
    /// </summary>
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("MapFaction", MethodType.Getter)]
    public class SettlementMapFactionPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            MinorFactionHideout mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout != null && mfHideout.OwnerClan != null)
                __result = mfHideout.MapFaction;
        }
    }

    /// <summary>
    /// A camp's owner is its owner clan.
    /// </summary>
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("OwnerClan", MethodType.Getter)]
    public class SettlementOwnerClanPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            MinorFactionHideout mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout != null && mfHideout.OwnerClan != null)
                __result = mfHideout.OwnerClan;
        }
    }

    // Prevents MFHideouts from having militia troops added and instead adds MF Basic Troops
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    public class SettlementMilitiasPatch
    {
        static bool Prefix(Settlement __instance, MobileParty militiaParty, int militiaNumberToAdd)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout == null)
                return true;
            var basicTroop = Helpers.GetBasicTroop(mfHideout.OwnerClan);
            if (basicTroop == null)
                return false;

            Helpers.removeMilitiaImposters(__instance);
            foreach ((CharacterObject troop, int count) in Helpers.SplitEntryTroops(mfHideout.OwnerClan, militiaNumberToAdd))
                militiaParty.MemberRoster.AddToCounts(troop, count);
            
            return false;
        }
    }

    /// <summary>
    /// After a camp's settlements.xml row is read, connects it to the loaded camp data and finds its owner clan
    /// if the owner reference didn't resolve.
    /// </summary>
    // XML deserialization for MFHideouts
    [HarmonyPatch(typeof(Settlement), "Deserialize")]
    public class SettlementDeserializePatch
    {
        static void Postfix(Settlement __instance, MBObjectManager objectManager, XmlNode node)
        {
            if (node?.Attributes?["imf_generated"] != null)
                GeneratedHideouts.Add(__instance.StringId);

            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout == null || node == null)
                return;
            string mfClanId = node!.Attributes!.GetNamedItem("owner")!.Value!.Replace("Faction.", "");

            // manual fix of MFHideouts not saving properly bug
            if (IMFManager.Current != null)
            {
                var settlementMfh = IMFManager.Current.GetLoadedMFHideout(mfHideout.StringId) ?? mfHideout;
                Helpers.CallPrivateMethod(__instance, "SetSettlementComponent", new object[] { settlementMfh });
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name != "Components")
                        continue;
                    XmlNode mfhNode = child.FirstChild!;
                    settlementMfh.Deserialize(MBObjectManager.Instance, mfhNode);
                    settlementMfh.AfterInitialized();
                }
            }
            // if "Faction.factionID" doesn't get us the clan we want we must manually find it
            if (__instance.OwnerClan == null || !Clan.All.Contains(__instance.OwnerClan))
            {
                Clan? mfClan = Clan.All?.Find((x) => x.StringId == mfClanId);
                if (mfClan == null)
                {
                    mfClan = objectManager.ReadObjectReferenceFromXml<Clan>("owner", node);
                    if (mfClan == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"{__instance.StringId} has owner set to {mfClanId} but {mfClanId} does not exist!"));
                        throw new Exception($"{__instance.StringId} has owner set to {mfClanId} but {mfClanId} does not exist!");
                    }
                        
                }
                mfHideout.OwnerClan = mfClan;
            }

            if (__instance.OwnerClan != null && __instance.Party != null && __instance != __instance.OwnerClan.InitialHomeSettlement)
            {
                if (__instance.OwnerClan.IsNomad)
                    __instance.Party.SetCustomName(new TextObject("{=dt9395yju}{MINOR_FACTION} Camp")
                        .SetTextVariable("MINOR_FACTION", __instance.OwnerClan.Name));
                else
                    __instance.Party.SetCustomName(new TextObject("{=dt9393yju}{MINOR_FACTION} Hideout")
                        .SetTextVariable("MINOR_FACTION", __instance.OwnerClan.Name));
            }
        }
    }

}

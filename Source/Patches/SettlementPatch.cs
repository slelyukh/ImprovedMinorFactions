using HarmonyLib;
using System.Xml;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using System;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("MapFaction", MethodType.Getter)]
    public class SettlementMapFactionPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            MinorFactionHideout mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout != null)
                __result = mfHideout.MapFaction;
        }
    }

    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("OwnerClan", MethodType.Getter)]
    public class SettlementOwnerClanPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            MinorFactionHideout mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout != null)
                __result = mfHideout.OwnerClan;
        }
    }

    // Prevents MFHideouts from having militia troops added and instead adds MF Basic Troops
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    public class SettlementMilitiasPatch
    {
        static bool Prefix(Settlement __instance, MobileParty militaParty, int militiaToAdd)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout == null)
                return true;

            Helpers.removeMilitiaImposters(__instance);
            __instance.MilitiaPartyComponent.MobileParty.MemberRoster.AddToCounts(Helpers.GetBasicTroop(mfHideout.OwnerClan), militiaToAdd);
            //Helpers.callPrivateMethod(__instance, "AddTroopToMilitiaParty", 
            //    new object[] { militaParty, Helpers.GetBasicTroop(mfHideout.OwnerClan), Helpers.GetBasicTroop(mfHideout.OwnerClan), 1f, militiaToAdd });

            return false;
        }
    }

    // XML deserialization for MFHideouts
    [HarmonyPatch(typeof(Settlement), "Deserialize")]
    public class SettlementDeserializePatch
    {
        static void Postfix(Settlement __instance, MBObjectManager objectManager, XmlNode node)
        {
            MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
            if (mfHideout == null || node == null)
                return;
            string mfClanId = node!.Attributes!.GetNamedItem("owner")!.Value!.Replace("Faction.", "");

            // manual fix of MFHideouts not saving properly bug
            if (IMFManager.Current != null)
            {
                var settlementMfh = IMFManager.Current.GetLoadedMFHideout(mfHideout.StringId) ?? mfHideout;
                __instance.SetSettlementComponent(settlementMfh);
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

            // Update hideout name only if the current owner is not the original owner
            if (__instance.OwnerClan != null && mfClanId != __instance.OwnerClan.StringId)
            {
                if (__instance.OwnerClan.IsNomad)
                    __instance.Name = new TextObject("{=dt9395yju}{MINOR_FACTION} Camp")
                        .SetTextVariable("MINOR_FACTION", __instance.OwnerClan.Name);
                else
                    __instance.Name = new TextObject("{=dt9393yju}{MINOR_FACTION} Hideout")
                        .SetTextVariable("MINOR_FACTION", __instance.OwnerClan.Name);
                
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "SetNameAttributes")]
    public class SettlementSetNameAttributesPatch
    {
        static void Postfix(Settlement __instance, ref TextObject ____name)
        {
            if (!Helpers.IsMFHideout(__instance))
                return;
            ____name.SetTextVariable("IS_MINORFACTIONHIDEOUT", 1);
        }
    }
}

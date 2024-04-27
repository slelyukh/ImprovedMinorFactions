using HarmonyLib;
using System.Xml;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using System.Reflection;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("MapFaction", MethodType.Getter)]
    public class SettlementMapFactionPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            if (__result == null)
            {
                MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
                if (mfHideout != null)
                    __result = mfHideout.MapFaction;
            }
        }
    }

    [HarmonyPatch(typeof(Settlement))]
    [HarmonyPatch("OwnerClan", MethodType.Getter)]
    public class SettlementOwnerClanPatch
    {
        static void Postfix(ref IFaction __result, Settlement __instance)
        {
            if (__result == null)
            {
                MinorFactionHideout? mfHideout = Helpers.GetMFHideout(__instance);
                if (mfHideout != null)
                    __result = mfHideout.OwnerClan;
            }
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

            int removedImposters = Helpers.removeMilitiaImposters(__instance);
            Helpers.callPrivateMethod(__instance, "AddTroopToMilitiaParty", 
                new object[] { militaParty, Helpers.GetBasicTroop(mfHideout.OwnerClan), Helpers.GetBasicTroop(mfHideout.OwnerClan), 1f, militiaToAdd + removedImposters });

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
            if (mfHideout == null)
                return;

            // manual fix of MFHideouts not saving properly bug
            if (MFHideoutManager.Current != null && MFHideoutManager.Current.GetLoadedMFHideout(mfHideout.StringId) != null)
            {
                var loadedMfh = MFHideoutManager.Current.GetLoadedMFHideout(mfHideout.StringId);
                __instance.SetSettlementComponent(loadedMfh);
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name != "Components")
                        continue;
                    XmlNode mfhNode = child.FirstChild;
                    loadedMfh.Deserialize(MBObjectManager.Instance, mfhNode);
                    loadedMfh.AfterInitialized();
                }
            }

            // Settlement deserialization does not set clan owner properly so we need to do it manually
            if (__instance.OwnerClan == null)
            {
                Clan clan = objectManager.ReadObjectReferenceFromXml<Clan>("owner", node);
                if (clan != null)
                    mfHideout.OwnerClan = clan;
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "SetNameAttributes")]
    public class SettlementSetNameAttributesPatch
    {
        static void Postfix(Settlement __instance, ref TextObject ____name)
        {
            if (!Helpers.isMFHideout(__instance))
                return;
            ____name.SetTextVariable("IS_MINORFACTIONHIDEOUT", 1);
        }
    }
}

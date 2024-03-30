using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using System.Xml;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using System.Reflection;
using TaleWorlds.Library;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Engine;
using TaleWorlds.CampaignSystem.Extensions;

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
                MinorFactionHideout? mfHideout = __instance.SettlementComponent as MinorFactionHideout;
                if (mfHideout != null)
                {
                    __result = mfHideout.MapFaction;
                }
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
                MinorFactionHideout? mfHideout = __instance.SettlementComponent as MinorFactionHideout;
                if (mfHideout != null)
                {
                    __result = mfHideout.OwnerClan;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    public class SettlementMilitiasPatch
    {
        static bool Prefix(Settlement __instance, MobileParty militaParty, int militiaToAdd)
        {
            MinorFactionHideout? mfHideout = __instance.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return true;

            // TODO: remove other lines and only have this
            if (!Helpers.IsMFClanInitialized(mfHideout.OwnerClan))
                return false;

            // Reflection to call private method
            var methodInfo = __instance.GetType().GetMethod("AddTroopToMilitiaParty", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(__instance, new object[] { militaParty, mfHideout.OwnerClan.BasicTroop, mfHideout.OwnerClan.BasicTroop, 1f, militiaToAdd });
            Helpers.removeImposters(__instance);

            return false;
        }
    }


    //TODO: move this to campaign patches
    [HarmonyPatch(typeof(Campaign), "DailyTickSettlement")]
    public class CampaignSettlementTickPatch
    {
        static void Postfix(Campaign __instance, Settlement settlement)
        {
            MinorFactionHideout? mfHideout = settlement.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return;
            mfHideout.DailyTick();
        }
    }

    [HarmonyPatch(typeof(Settlement), "Deserialize")]
    public class SettlementDeserializePatch
    {
        static void Postfix(Settlement __instance, MBObjectManager objectManager, XmlNode node)
        {

            if (__instance.IsHideout)
            {
                // bla
            }
            // SettlementComponent assumed to be initialized at this point
            MinorFactionHideout? mfHideout = __instance.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return;

            // broken saves manager...
            if (MFHideoutManager.Current != null && MFHideoutManager.Current.GetLoadedMFHideout(mfHideout.StringId) != null)
            {
                var loadedMfh = MFHideoutManager.Current.GetLoadedMFHideout(mfHideout.StringId);
                __instance.SetSettlementComponent(loadedMfh);
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name != "Components")
                        continue;
                    XmlNode mfhNode = child.FirstChild;
                    // UPDATES BUG: this code copies MBObjectManager.CreateObjectFromXmlNode
                    loadedMfh.Deserialize(MBObjectManager.Instance, mfhNode);
                    loadedMfh.AfterInitialized();
                }
            }

            // New game code
            MBObjectBase objBase = __instance;
            // normally this line is if objBase !isInitialized but it claims to be initialized even when there is no Owner
            if (__instance.OwnerClan == null)
            {
                Clan clan = objectManager.ReadObjectReferenceFromXml<Clan>("owner", node);
                if (clan != null)
                {
                    mfHideout.OwnerClan = clan;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "SetNameAttributes")]
    public class SettlementSetNameAttributesPatch
    {
        static void Postfix(Settlement __instance, ref TextObject ____name)
        {
            // SettlementComponent assumed to be initialized at this point
            MinorFactionHideout? mfHideout = __instance.SettlementComponent as MinorFactionHideout;
            if (mfHideout == null)
                return;
            ____name.SetTextVariable("IS_MINORFACTIONHIDEOUT", 1);
        }
    }
}

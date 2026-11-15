using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions.Patches
{
    // some cultures have no militia template, fall back to the clan culture, its party template, then any
    [HarmonyPatch]
    public class MilitiaTemplateFallbackPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.Inner(typeof(MilitiaPartyComponent), "InitializationArgs"), "InitializeMilitiaPartyProperties");
        }

        static bool Prefix(MobileParty mobileParty, Settlement settlement)
        {
            if (settlement?.Culture?.MilitiaPartyTemplate != null)
                return true;

            PartyTemplateObject template = FallbackTemplate(settlement);
            if (template == null)
                return true;

            mobileParty.InitializeMobilePartyAtPosition(template, settlement.GatePosition);
            return false;
        }

        private static PartyTemplateObject FallbackTemplate(Settlement settlement)
        {
            Clan owner = settlement?.OwnerClan;
            PartyTemplateObject template = owner?.Culture?.MilitiaPartyTemplate ?? owner?.DefaultPartyTemplate;
            if (template != null)
                return template;

            foreach (CultureObject culture in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                if (culture.MilitiaPartyTemplate != null)
                    return culture.MilitiaPartyTemplate;
            }
            return null;
        }
    }
}

using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions.Patches
{
    /// <summary>
    /// Some cultures have no militia template. Militia of such a camp use the owner clan's culture template, its party template,
    /// or the first culture that has one.
    /// </summary>
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

        /// <summary>
        /// Picks the template to use, see the class summary.
        /// </summary>
        /// <returns>the template, or null if no culture has a militia template</returns>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(SettlementNameplatesVM), "Initialize")]
    public class SettlementNameplatesVMInitializePatch
    {
        static void Postfix(SettlementNameplatesVM __instance, IEnumerable<Tuple<Settlement, GameEntity>> settlements)
        {
            // TODO: check if manager is initialized
            MFHideoutManager.initManagerIfNone();

            MFHideoutManager.Current._allMFHideouts =
                from x in settlements
                where Helpers.isMFHideout(x.Item1)
                select x;

            List<SettlementNameplateVM> mfhNameplates = new List<SettlementNameplateVM>();
            foreach (var nameplate in __instance.Nameplates)
            {
                
                if (Helpers.isMFHideout(nameplate.Settlement))
                {
                    //InformationManager.DisplayMessage(new InformationMessage($"WEEEE"));
                    mfhNameplates.Add(nameplate);
                }
                    
            }
            //InformationManager.DisplayMessage(new InformationMessage($"number of mfh nameplates = {mfhNameplates.Count}; total nameplates = {__instance.Nameplates.Count}"));
            foreach (var nameplate in mfhNameplates)
            {
                if (!(nameplate.Settlement.SettlementComponent as MinorFactionHideout).IsSpotted)
                {
                    var removed = __instance.Nameplates.Remove(nameplate);
                    //InformationManager.DisplayMessage(new InformationMessage($"nameplate for {nameplate.Settlement} removed = {removed}"));
                }
            }
        }
    }

    // nameplate or nameplates??
    [HarmonyPatch(typeof(SettlementNameplatesVM), "OnPartyBaseVisibilityChange")]
    public class SettlementNameplateVMOnPartyVisibilityPatch
    {
        static void Postfix(SettlementNameplatesVM __instance, PartyBase party, ref Camera ____mapCamera, ref Action<Vec2> ____fastMoveCameraToPosition)
        {
            if (!party.IsSettlement || !(party.Settlement.SettlementComponent is MinorFactionHideout))
                return;

            MFHideoutManager.initManagerIfNone();

            var desiredSettlementTuple = MFHideoutManager.Current._allMFHideouts
                .SingleOrDefault((Tuple<Settlement, GameEntity> h) => h.Item1 == party.Settlement);
            if (desiredSettlementTuple != null)
            {
                SettlementNameplateVM nameplate = __instance.Nameplates
                    .SingleOrDefault((SettlementNameplateVM n) => n.Settlement == desiredSettlementTuple.Item1);
                if (party.IsVisible && nameplate == null)
                {
                    SettlementNameplateVM newNameplate = new
                        SettlementNameplateVM(desiredSettlementTuple.Item1, desiredSettlementTuple.Item2, ____mapCamera, ____fastMoveCameraToPosition);
                    __instance.Nameplates.Add(newNameplate);
                    newNameplate.RefreshRelationStatus();
                }
                if (!party.IsVisible && nameplate != null)
                {
                    __instance.Nameplates.Remove(nameplate);
                }
            }
        }
    }
}

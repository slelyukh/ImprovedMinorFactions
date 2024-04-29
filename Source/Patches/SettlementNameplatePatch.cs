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
    // adds nameplates for MFHideouts
    [HarmonyPatch(typeof(SettlementNameplatesVM), "Initialize")]
    public class SettlementNameplatesVMInitializePatch
    {
        static void Postfix(SettlementNameplatesVM __instance, IEnumerable<Tuple<Settlement, GameEntity>> settlements)
        {
            MFHideoutManager.InitManagerIfNone();

            MFHideoutManager.Current._allMFHideouts =
                from x in settlements
                where Helpers.isMFHideout(x.Item1)
                select x;

            List<SettlementNameplateVM> mfhNameplates = new List<SettlementNameplateVM>();
            foreach (var nameplate in __instance.Nameplates)
            {
                if (Helpers.isMFHideout(nameplate.Settlement))
                    mfhNameplates.Add(nameplate);
            }

            foreach (var nameplate in mfhNameplates)
            {
                if (!Helpers.GetMFHideout(nameplate.Settlement).IsSpotted)
                    __instance.Nameplates.Remove(nameplate);
            }
        }
    }

    // Crash preventer for MFHideout Settlement Nameplates
    [HarmonyPatch(typeof(SettlementNameplatesVM), "OnPartyBaseVisibilityChange")]
    public class SettlementNameplateVMOnPartyVisibilityPatch
    {
        static void Postfix(SettlementNameplatesVM __instance, PartyBase party, ref Camera ____mapCamera, ref Action<Vec2> ____fastMoveCameraToPosition)
        {
            if (!party.IsSettlement || !(party.Settlement.SettlementComponent is MinorFactionHideout))
                return;

            MFHideoutManager.InitManagerIfNone();

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
                    __instance.Nameplates.Remove(nameplate);
            }
        }
    }
}

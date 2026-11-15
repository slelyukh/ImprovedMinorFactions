using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using Helpers;
using ImprovedMinorFactions.Source;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // mf parties start near their hideout and never in it (that opens its menu)
    internal static class SpawnNear
    {
        private static readonly MethodInfo? PositionSetter = AccessTools.PropertySetter(typeof(MobileParty), "Position");

        internal static bool Place(MobileParty party, Settlement hideout, int slot)
        {
            if (party == null || hideout == null || PositionSetter == null)
                return false;
            CampaignVec2? spot = HideoutPlacement.FindSpawnNear(new CampaignVec2(hideout.GetPosition2D, true), slot, hideout, party, close: party == MobileParty.MainParty);
            if (!spot.HasValue)
            {
                IMFDiagnostics.Verbose($"[Spawn] {party.StringId}: no open ground near {hideout.StringId}");
                return false;
            }
            if (party.CurrentSettlement != null)
                LeaveSettlement(party);
            PositionSetter.Invoke(party, new object[] { spot.Value });
            // Drop any order still aimed at the camp, or waiting next to it walks the party back in.
            if (party == MobileParty.MainParty)
            {
                party.SetMoveModeHold();
                CenterCameraOnPlayer();
            }
            return true;
        }

        // without this the camera stays on the old spot and you watch the party snap across the map
        private static void CenterCameraOnPlayer()
        {
            if (GameStateManager.Current?.ActiveState is MapState mapState && mapState.Handler != null)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
        }

        internal static void LeaveSettlement(MobileParty party)
        {
            if (party == MobileParty.MainParty && PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveSettlement();
                PlayerEncounter.Finish(true);
            }
            else
            {
                LeaveSettlementAction.ApplyForParty(party);
            }
        }
    }

    // mf lord parties spawn next to their own camp instead of anywhere near home
    [HarmonyPatch(typeof(MobilePartyHelper), nameof(MobilePartyHelper.SpawnLordParty), new Type[] { typeof(Hero), typeof(CampaignVec2), typeof(float) })]
    internal static class SpawnLordPartyAtCampPatch
    {
        private static void Postfix(MobileParty __result, Hero hero)
        {
            Clan? clan = hero?.Clan;
            if (__result == null || clan == null || clan.IsNomad || IMFManager.Current == null || !IMFManager.Current.HasFaction(clan))
                return;
            MinorFactionHideout? camp = IMFManager.Current.GetActiveHideoutsOfClan(clan).FirstOrDefault(h => h.Settlement != null);
            if (camp == null)
                return;
            try
            {
                SpawnNear.Place(__result, camp.Settlement, clan.WarPartyComponents.Count);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log($"placing {__result.StringId} next to {camp.Settlement.StringId} failed:", e);
            }
        }
    }
}

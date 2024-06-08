using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Patches
{
    [HarmonyPatch(typeof(PlayerEncounter), "CreateLocationEncounter")]
    public class CreateLocationEncounterPatch
    {
        static void Postfix(Settlement settlement)
        {
            if (Helpers.IsMFHideout(settlement))
                PlayerEncounter.LocationEncounter = new MFHEncounter(settlement);
        }
    }

    // allows MFHideouts to have a HideoutBattle
    [HarmonyPatch(typeof(PlayerEncounter), "StartBattleInternal")]
    public class StartBattleInternalPatch
    {
        static bool Prefix(ref MapEvent? __result, PlayerEncounter __instance,
            ref PartyBase ____defenderParty, ref PartyBase ____attackerParty, ref MapEvent? ____mapEvent)
        {
            if (____mapEvent == null 
                && ____defenderParty != null 
                && ____defenderParty.IsSettlement 
                && Helpers.IsMFHideout(____defenderParty.Settlement))
            {
                Helpers.setPrivateField(__instance, "_mapEvent", HideoutEventComponent.CreateHideoutEvent(____attackerParty, ____defenderParty).MapEvent);
                Helpers.CallPrivateMethod(__instance, "CheckNearbyPartiesToJoinPlayerMapEvent", new object[] { });
                __result = ____mapEvent;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    // copypasta with crash causing parts removed
    [HarmonyPatch(typeof(PlayerEncounter), "DoEnd")]
    public class DoEndPatch
    {
        static bool Prefix(PlayerEncounter __instance)
        {
            MapEvent mapEvent = PlayerEncounter.Battle;
            if (mapEvent == null || mapEvent.MapEventSettlement == null)
                return true;
            Settlement settlement = mapEvent.MapEventSettlement;
            var mfHideout = Helpers.GetMFHideout(settlement);
            if (mfHideout == null || !mapEvent.IsHideoutBattle)
                return true;

            bool flag2 = MobileParty.MainParty.MapEvent != null && __instance.PlayerSide == BattleSideEnum.Attacker;
           
            // wat is this
            bool playerLost = __instance.BattleSimulation != null && mapEvent.WinningSide != __instance.PlayerSide;

            BattleState battleState = mapEvent.BattleState;
            Helpers.setPrivateField(__instance, "_stateHandled", true); //__instance._stateHandled = true;
            if (!playerLost)
            {
                PlayerEncounter.Finish(true);
            }
            else
            {
                Helpers.CallPrivateMethod(mapEvent, "ResetBattleResults", new object[] { }); // PlayerEncounter.Battle.ResetBattleResults();
            }

            if (battleState == BattleState.AttackerVictory)
            {
                IMFManager.Current!.ClearHideout(mfHideout, DeactivationReason.Raid);
                return false;
            }
            if (battleState == BattleState.None)
            {
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                GameMenu.SwitchToMenu("mf_hideout_place");
                return false;
            }
            // TODO: what is this??
            if (playerLost)
            {
                Helpers.CallPrivateMethod(__instance, "set_EncounterState", new object[] { PlayerEncounterState.Begin }); // EncounterState = PlayerEncounterState.Begin;
                GameMenu.SwitchToMenu("mf_hideout_place");
            }
            return false;
        }
    }

    // copypasta with crash causing parts removed
    [HarmonyPatch(typeof(PlayerEncounter), "DoWait")]
    public class DoWaitPatch
    {
        static bool Prefix(PlayerEncounter __instance, ref CampaignBattleResult ____campaignBattleResult)
        {
            var mapEvent = PlayerEncounter.Battle;
            var settlement = mapEvent?.MapEventSettlement;
            if (mapEvent == null || settlement == null || !Helpers.IsMFHideout(settlement) || !mapEvent.IsHideoutBattle)
                return true;
            var mfHideout = Helpers.GetMFHideout(settlement);
            
            MBTextManager.SetTextVariable("PARTY", MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
            if (!PlayerEncounter.EncounteredPartySurrendered)
            {
                MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_PARTY"), sendClients: true);
            }
            else
            {
                MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_PARTY_they_surrendered"), sendClients: true);
            }

            if (__instance.CheckIfBattleShouldContinueAfterBattleMission())
            {
                Helpers.CallPrivateMethod(__instance, "ContinueBattle", new object[] { }); //__instance.ContinueBattle();
                return false;
            }

            
            if (____campaignBattleResult != null && ____campaignBattleResult.BattleResolved)
            {
                if (____campaignBattleResult.PlayerVictory)
                {
                    mapEvent?.SetOverrideWinner(PartyBase.MainParty.Side);
                }
                else
                {
                    mfHideout?.UpdateNextPossibleAttackTime();
                    if (!(mapEvent.GetMapEventSide(__instance.PlayerSide).RecalculateMemberCountOfSide() > 0))
                    {
                        mapEvent?.SetOverrideWinner(PartyBase.MainParty.OpponentSide);
                    }
                }
                Helpers.CallPrivateMethod(__instance, "set_EncounterState", new object[] { PlayerEncounterState.PrepareResults }); // EncounterState = PlayerEncounterState.PrepareResults;

            }
            else if (__instance.BattleSimulation != null && (PlayerEncounter.BattleState == BattleState.AttackerVictory || PlayerEncounter.BattleState == BattleState.DefenderVictory))
            {
                if (mapEvent.WinningSide == __instance.PlayerSide)
                {
                    PlayerEncounter.EnemySurrender = true;
                }
                else
                {
                    int totalManCount = MobileParty.MainParty.MemberRoster.TotalManCount;
                    int totalWounded = MobileParty.MainParty.MemberRoster.TotalWounded;
                    if (totalManCount - totalWounded == 0)
                    {
                        PlayerEncounter.PlayerSurrender = true;
                    }
                }
                Helpers.CallPrivateMethod(__instance, "set_EncounterState", new object[] { PlayerEncounterState.PrepareResults }); // EncounterState = PlayerEncounterState.PrepareResults;
            }
            else
            {
                Helpers.setPrivateField(__instance, "_stateHandled", true); //__instance._stateHandled = true;
                if (__instance.IsJoinedBattle && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "join_encounter")
                {
                    PlayerEncounter.LeaveBattle();
                }
                mfHideout?.UpdateNextPossibleAttackTime();
            }
            return false;
        }
    }
}

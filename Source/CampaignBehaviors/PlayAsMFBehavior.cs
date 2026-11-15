using System;
using System.Collections.Generic;
using System.Linq;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    // play as a minor faction: camp active, notables on your side, start beside the camp
    internal class PlayAsMFBehavior : CampaignBehaviorBase
    {
        private const float StartMaxDistanceFromCamp = 5.5f;

        private bool _startPlacementDone;
        private List<MinorFactionHideout>? _campsToStartBeside;

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
            CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, delegate { SupportPlayerClan(); });
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        private void OnCharacterCreationIsOver(int _)
        {
            try
            {
                SetUpPlayerClanCamps();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("PlayAsMF OnCharacterCreationIsOver failed:", e);
            }
        }

        private void OnPlayerCharacterChanged(Hero oldHero, Hero newHero, MobileParty newMainParty, bool isMainPartyChanged)
        {
            try
            {
                SetUpPlayerClanCamps();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("PlayAsMF OnPlayerCharacterChanged failed:", e);
            }
        }

        private void SetUpPlayerClanCamps()
        {
            Clan playerClan = Clan.PlayerClan;
            if (playerClan == null || !playerClan.IsMinorFaction || playerClan.IsEliminated || IMFManager.Current == null)
                return;
            MFData? mfData = IMFManager.Current.GetClanMFData(playerClan);
            if (mfData?.Hideouts == null || mfData.Hideouts.Count == 0)
            {
                IMFDiagnostics.Info($"PlayAsMF: player clan {playerClan.Name} has no hideouts");
                return;
            }

            List<MinorFactionHideout> activeCamps = mfData.Hideouts.Where(h => h.IsActive).ToList();
            if (activeCamps.Count == 0)
            {
                MinorFactionHideout camp = mfData.Hideouts.FirstOrDefault(h => !h.IsActiveOrScheduled) ?? mfData.Hideouts[0];
                camp.ActivateHideoutFirstTime();
                if (!camp.IsActive)
                {
                    IMFDiagnostics.Info($"PlayAsMF: could not activate {camp.Settlement.StringId} for player clan {playerClan.Name}");
                    return;
                }
                activeCamps.Add(camp);
            }
            foreach (MinorFactionHideout camp in activeCamps)
            {
                if (!camp.IsSpotted)
                    camp.PlayerSpotHideout();
            }
            SupportPlayerClan();
            // The game can still move the party into its home after this, so it's placed on the first map tick.
            _campsToStartBeside = activeCamps;
        }

        private static void SupportPlayerClan()
        {
            try
            {
                int notables = Helpers.MakeHideoutNotablesSupportPlayerClan();
                if (notables > 0)
                    IMFDiagnostics.Info($"PlayAsMF: {notables} hideout notable(s) now support {Clan.PlayerClan.Name}");
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("PlayAsMF SupportPlayerClan failed:", e);
            }
        }

        private void OnTick(float dt)
        {
            if (_campsToStartBeside == null)
                return;
            bool settled = true;
            try
            {
                settled = PlacePlayerBesideCamp(_campsToStartBeside);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("PlayAsMF start placement failed:", e);
            }
            if (settled)
                _campsToStartBeside = null;
        }

        // new game only. false while in a battle or encounter, retried next tick
        private bool PlacePlayerBesideCamp(List<MinorFactionHideout> camps)
        {
            if (_startPlacementDone)
                return true;
            if (Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow >= 1f)
                return true;
            MobileParty mainParty = MobileParty.MainParty;
            if (mainParty == null || mainParty.MapEvent != null)
                return false;

            // A game that starts you at your clan's home puts you inside the camp, you start around it instead.
            MinorFactionHideout? inside = camps.FirstOrDefault(c => c.Settlement != null && c.Settlement == mainParty.CurrentSettlement);
            if (inside != null)
            {
                SpawnNear.LeaveSettlement(mainParty);
                bool moved = SpawnNear.Place(mainParty, inside.Settlement, 0);
                if (!moved)
                    mainParty.SetMoveModeHold();   // left at the gate, but don't walk back in
                _startPlacementDone = true;
                IMFDiagnostics.Info($"[PlayerClan] started inside {inside.Settlement.StringId}, moved beside it: {moved}");
                return true;
            }
            if (PlayerEncounter.Current != null)
                return false;
            if (mainParty.CurrentSettlement != null)
                return true;

            Vec2 playerPosition = mainParty.GetPosition2D;
            MinorFactionHideout? nearest = camps.Where(c => c.Settlement != null)
                .OrderBy(c => playerPosition.Distance(c.Settlement.GetPosition2D))
                .FirstOrDefault();
            if (nearest == null || playerPosition.Distance(nearest.Settlement.GetPosition2D) <= StartMaxDistanceFromCamp)
                return true;
            _startPlacementDone = SpawnNear.Place(mainParty, nearest.Settlement, 0);
            IMFDiagnostics.Info($"[PlayerClan] started away from the camp, moved beside {nearest.Settlement.StringId}: {_startPlacementDone}");
            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System;
using HarmonyLib;
using ImprovedMinorFactions.Patches;
using ImprovedMinorFactions.Source;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// Keeps track of every minor faction's camps for the running campaign: which clan owns which camp, how many should be active,
    /// and which camp a clan moves to after a raid. There is one instance per campaign in IMFManager.Current.
    /// </summary>
    internal class IMFManager
    {
        public IMFManager() 
        {
            _LoadedMFHideouts = new Dictionary<string, MinorFactionHideout>();
            _mfData = new Dictionary<Clan, MFData>();
        }

        /// <summary>
        /// Creates the manager if there isn't one and tries to build the camp lists.
        /// </summary>
        private static void InitManager()
        {
            if (IMFManager.Current == null)
                IMFManager.Current = new IMFManager();
            Current._mfDataInitialized = Current.TryInitMFHideoutsLists();
        }

        public static void InitManagerIfNone()
        {
            InitManager();
        }
        public static void ClearManager()
        {
            IMFManager.Current = null;
        }

        /// <summary>
        /// Records a camp read from a save. Duplicate saves of the same camp keep the first one and show a warning,
        /// camps whose clan has no leader are skipped.
        /// </summary>
        public void AddLoadedMFHideout(MinorFactionHideout mfh)
        {
            if (_LoadedMFHideouts.ContainsKey(mfh.StringId))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{mfh.StringId} duplicate hideout saves! If you see any strange behavior please send your savefiles to Improved Minor Factions mod author.", Colors.Red));
                // if (ShouldReplaceHideout(mfh, _LoadedMFHideouts[mfh.StringId]))
                //    _LoadedMFHideouts[mfh.StringId] = mfh;
                // When looking at save with duplicates the existing mfhideout seems to be consistently the one we want.
                return;
            }
            
            if (mfh.OwnerClan == null || mfh.OwnerClan.Leader == null)
                return;

            _LoadedMFHideouts[mfh.StringId] = mfh;
        }

        /// <summary>
        /// Gets a camp read from the save by its settlement id.
        /// </summary>
        /// <returns>the camp, or null if the save didn't have it</returns>
        public MinorFactionHideout? GetLoadedMFHideout(string stringId)
        {
            MinorFactionHideout? mfh = null;
            _LoadedMFHideouts.TryGetValue(stringId, out mfh);
            return mfh;
        }

        // should only be called when you are sure the clan is not already here
        internal void AddMFData(Clan c, MFData mfd)
        {
            _mfData[c] = mfd;
        }

        private void CreateMFDataIfNone(Clan c)
        {
            if (!_mfData.ContainsKey(c))
            {
                _mfData[c] = new MFData(c);
            }
        }

        /// <summary>
        /// Groups every camp settlement under its owning clan. Only works once all settlements are loaded.
        /// </summary>
        /// <returns>true if the lists are ready</returns>
        // should only be done when all Settlements are loaded in
        public bool TryInitMFHideoutsLists()
        {
            if (Campaign.Current?.Settlements == null)
                return false;
            if (_mfDataInitialized)
                return true;

            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                // converted hideouts keep the clan here, OwnerClan is null once IsHideout is false
                var ownerClan = (settlement.SettlementComponent as MinorFactionHideout)?.OwnerClan
                    ?? settlement.OwnerClan;
                bool isMfHideout = Helpers.IsMFHideout(settlement);
                if ((ownerClan?.IsMinorFaction ?? false) && isMfHideout)
                {
                    // TODO: store ownerless hideouts somewhere
                    var mfClan = ownerClan!;
                    CreateMFDataIfNone(mfClan);
                    HideoutPlacement.RegisterAnchor(settlement, mfClan.HomeSettlement);
                    _mfData[mfClan].AddMFHideout(settlement);
                }
            }
            _mfDataInitialized = true;
            this._hideouts =
                (from x in Campaign.Current.Settlements
                where Helpers.IsMFHideout(x)
                select Helpers.GetMFHideout(x)).ToMBList();
            return true;
        }

        /// <summary>
        /// Compares two camps for how much a clan wants them: its own active camps first, then its own camps,
        /// then the one closer to its home. Camps already active for another clan come last.
        /// </summary>
        /// <param name="c">the clan choosing</param>
        /// <returns>positive if mfh1 is better, negative if mfh2 is</returns>
        private static int mfhComparator(Clan c, MinorFactionHideout mfh1, MinorFactionHideout mfh2)
        {
            int priority1 = 0;
            int priority2 = 0;
            if (mfh1.OwnerClan == c)
            {
                priority1 += 10;
                if (mfh1.IsActiveOrScheduled)
                    priority1 += 100;
            }
            if (mfh2.OwnerClan == c)
            {
                priority2 += 10;
                if (mfh2.IsActiveOrScheduled)
                    priority2 += 100;
            }

            Vec2 clanPosition = c.HomeSettlement?.GetPosition2D ?? c.Leader?.GetMapPoint()?.Position.ToVec2() ?? Vec2.Zero;
            if (mfh1.Settlement.GetPosition2D.Distance(clanPosition)
                < mfh2.Settlement.GetPosition2D.Distance(clanPosition))
            {
                priority1 += 1;
            }
            else
            {
                priority2 += 1;
            }

            // already active foreign hideouts are lowest priority
            if (mfh1.IsActiveOrScheduled && mfh1.OwnerClan != c)
                priority1 = 0;
            if (mfh2.IsActiveOrScheduled && mfh2.OwnerClan != c)
                priority2 = 0;

            return priority1 - priority2;
        }

        /// <summary>
        /// Works out how many camps each settled clan gets out of the free pool. Clans that already own camps get their
        /// minimum first, then other clans, and what's left is shared out one at a time.
        /// Clans that can't get their minimum are taken out of NonNomadMFClans and set to 0 active camps.
        /// </summary>
        /// <param name="numFreeHideouts">camps in the pool</param>
        /// <param name="hideoutAssignedNonNomadClans">clans that already own camps</param>
        /// <param name="NonNomadMFClans">every clan that wants camps, changed in place</param>
        /// <returns>the number of camps per clan</returns>
        private Dictionary<Clan, int> DetermineHideoutCountsPostReassignment(
            int numFreeHideouts, HashSet<Clan> hideoutAssignedNonNomadClans, ref List<Clan> NonNomadMFClans)
        {
            Dictionary<Clan, int>  numHideoutsToGive = new Dictionary<Clan, int>();

            // delegate bare minimum numbers to hideout assigned clans
            foreach (Clan c in hideoutAssignedNonNomadClans)
            {
                int numToGive = IMFModels.NumActiveHideouts(c);
                numHideoutsToGive.Add(c, numToGive);
                numFreeHideouts -= numToGive;
            }

            // delegate bare minimum numbers to other clans
            foreach (Clan c in NonNomadMFClans)
            {
                if (numHideoutsToGive.ContainsKey(c))
                    continue;
                int numToGive = IMFModels.NumActiveHideouts(c);
                if (numToGive > numFreeHideouts)
                {
                    continue;
                }
                numHideoutsToGive.Add(c, numToGive);
                numFreeHideouts -= numToGive;
            }

            // remove hideoutless clans from list and set numActiveHideouts to 0
            List<Clan> clansToRemove = NonNomadMFClans.Where(c => !numHideoutsToGive.ContainsKey(c)).ToList();
            foreach (Clan clan in clansToRemove)
            {
                NonNomadMFClans.Remove(clan);
                _mfData[clan].NumActiveHideouts = 0;
            }

            // distribute remaining numbers evenly
            while (numFreeHideouts > 0)
            {
                int hideoutsGivenThisRound = 0;
                foreach (Clan c in NonNomadMFClans)
                {
                    if (numFreeHideouts == 0) break;
                    // not assigning hideouts to lost causes or clans that already have all their hideouts
                    if (!numHideoutsToGive.ContainsKey(c) || numHideoutsToGive[c] == GetClanMFData(c)!.Hideouts.Count)
                        continue;
                    numHideoutsToGive[c]++;
                    hideoutsGivenThisRound++;
                    numFreeHideouts--;
                }
                if (hideoutsGivenThisRound == 0) break;
            }
            return numHideoutsToGive;
        }

        /// <summary>
        /// Gives a camp to another clan and renames it. Throws if the camp is active.
        /// </summary>
        // Transfer ownership of an mfh from one clan to another or do nothing.
        private void AssignHideoutToClan(Clan newOwner, MinorFactionHideout mfh)
        {
            Clan oldOwner = mfh!.OwnerClan!;
            if (oldOwner == newOwner)
                return;
            if (mfh.IsActiveOrScheduled)
                throw new Exception("Trying to reassign active hideout to another clan!");

            mfh.OwnerClan = newOwner;
            mfh.Settlement.Party.SetCustomName(new TextObject("{=dt9393yju}{MINOR_FACTION} Hideout")
                .SetTextVariable("MINOR_FACTION", newOwner.Name));

            if (newOwner.Culture.NotableTemplates.Count > 0)
                mfh.Settlement.Culture = newOwner.Culture;

            GetClanMFData(newOwner)!.AddMFHideout(mfh);
            if (_mfData.ContainsKey(oldOwner))
                GetClanMFData(oldOwner)!.Hideouts.Remove(mfh);
        }

        /// <summary>
        /// Hands out camps from the pool one per clan per round, each clan taking its best remaining camp, until clans have
        /// their share or the pool is empty.
        /// </summary>
        /// <param name="receivingClans">clans to give camps to, changed in place</param>
        /// <param name="hideoutPool">free camps, taken camps are removed</param>
        /// <param name="numHideoutsToGive">camps still owed per clan</param>
        /// <param name="priorityLists">each clan's camps sorted best first</param>
        private void AssignHideoutsForList(List<Clan> receivingClans, ref HashSet<MinorFactionHideout> hideoutPool,
            ref Dictionary<Clan, int> numHideoutsToGive, Dictionary<Clan, List<MinorFactionHideout>> priorityLists)
        {
            while (!hideoutPool.IsEmpty() && !receivingClans.IsEmpty())
            {
                HashSet<Clan> satisfiedClans = new HashSet<Clan>();
                int givenHideouts = 0;
                foreach (Clan mfClan in receivingClans)
                {
                    List<MinorFactionHideout>? priorityList = null;
                    priorityLists.TryGetValue(mfClan, out priorityList);

                    if (priorityList == null)
                        continue;

                    if (numHideoutsToGive[mfClan] == 0)
                        satisfiedClans.Add(mfClan);
                    if (satisfiedClans.Contains(mfClan)) 
                        continue;

                    HashSet<MinorFactionHideout> mfhsToRemoveFromPriorityList = new HashSet<MinorFactionHideout>();
                    foreach (var mfh in priorityList!)
                    {
                        mfhsToRemoveFromPriorityList.Add(mfh);
                        if (hideoutPool.Contains(mfh) && (!mfh.IsActiveOrScheduled || mfh.OwnerClan == mfClan))
                        {
                            AssignHideoutToClan(mfClan, mfh);
                            hideoutPool.Remove(mfh);
                            numHideoutsToGive[mfClan]--;
                            givenHideouts++;
                            break;
                        }
                    }
                    priorityList.RemoveAll(mfh => mfhsToRemoveFromPriorityList.Contains(mfh));
                }

                foreach (Clan c in satisfiedClans)
                {
                    receivingClans.Remove(c);
                }
                if (givenHideouts == 0)
                    break;
            }
        }

        /// <summary>
        /// Shares the settled camps between all minor factions, including ones settlements.xml doesn't give camps to.
        /// Nomads keep their own camps unless they have none.
        /// </summary>
        // This function dynamically distributes MFHs to all Minor factions whether or not they are listed in settlements.xml
        private void ReassignHideouts()
        {
            // Preprocess lists
            HashSet<Clan> hideoutAssignedNonNomadClans = new HashSet<Clan>();
            List<Clan> NonNomadMFClans = new List<Clan>();
            // add MFs with assigned hideouts to clan list first to give them priority later
            foreach (Clan c in Clan.All)
            {
                if (c.IsMinorFaction && c != Clan.PlayerClan && !c.IsRebelClan && !c.IsNomad && !c.IsEliminated && HasFaction(c) && GetClanMFData(c)!.Hideouts.Count > 0 && ConfigLoader.IsEnabled(c.StringId))
                {
                    hideoutAssignedNonNomadClans.Add(c);
                    NonNomadMFClans.Add(c);
                }
            }

            // add MFs without assigned hideouts to clan list second to give them less priority
            foreach (Clan c in Clan.All)
            {
                // nomads keep their hideouts unless they have none
                bool isNomadWithoutHideouts = c.IsNomad && (GetClanMFData(c)?.Hideouts.Count ?? 0) == 0;
                if (c.IsMinorFaction && c != Clan.PlayerClan && !c.IsRebelClan && (!c.IsNomad || isNomadWithoutHideouts) && !c.IsEliminated && !NonNomadMFClans.Contains(c) && ConfigLoader.IsEnabled(c.StringId))
                {
                    NonNomadMFClans.Add(c);
                    CreateMFDataIfNone(c);
                }
            }

            // Make pool of all non-nomad MF hideouts.
            HashSet<MinorFactionHideout> hideoutPool = new HashSet<MinorFactionHideout>();
            foreach (MinorFactionHideout mfh in _hideouts)
            {
                if (mfh.OwnerClan?.IsNomad == false || (mfh.OwnerClan?.IsEliminated ?? false))
                {
                    hideoutPool.Add(mfh);
                }
            }

            // Determine the number of hideouts each MF will get in the end
            Dictionary<Clan, int> numHideoutsToGive = DetermineHideoutCountsPostReassignment(
                hideoutPool.Count, hideoutAssignedNonNomadClans, ref NonNomadMFClans);

            // Make sorted version of HideoutPool for each NonNomad MFClan
            Dictionary<Clan, List<MinorFactionHideout>> priorityLists = new Dictionary<Clan, List<MinorFactionHideout>>();
            foreach (Clan c in NonNomadMFClans)
            {
                var priorityList = hideoutPool.ToList();
                priorityList.Sort((mfh1, mfh2) => - mfhComparator(c, mfh1, mfh2));
                priorityLists[c] = priorityList;
            }

            // assign hideouts to clans with existing hideouts
            AssignHideoutsForList(hideoutAssignedNonNomadClans.ToList(), ref hideoutPool,
                ref numHideoutsToGive, priorityLists);
            // assign hideouts for others
            AssignHideoutsForList(NonNomadMFClans, ref hideoutPool, ref numHideoutsToGive, priorityLists);
        }

        /// <summary>
        /// Activates a random usable camp for a clan that has none active yet, used when its first party appears.
        /// </summary>
        public void ActivateFirstHideoutFor(Clan clan)
        {
            if (!_mfData.TryGetValue(clan, out MFData? data) || data == null || NumActiveOrScheduledHideouts(clan) > 0)
                return;
            var usable = Enumerable.Range(0, data.Hideouts.Count)
                .Where(k => !data.Hideouts[k].IsActiveOrScheduled && !HideoutPlacement.IsUnplaceable(data.Hideouts[k].Settlement, data.Hideouts[k].OwnerClan))
                .ToList();
            if (usable.Count == 0)
                return;
            MinorFactionHideout camp = data.Hideouts[usable[MBRandom.RandomInt(usable.Count)]];
            camp.ActivateHideoutFirstTime();
            IMFDiagnostics.Info($"[Spawn] {clan.StringId}: first party appeared, camp {camp.Settlement.StringId} is now active");
        }
        /// <summary>
        /// Redistributes camps and then activates or deactivates camps until each clan has as many active as it should.
        /// A clan's home camp is picked first if it is usable. Throws if called before the camp lists exist.
        /// </summary>
        public void ActivateAllFactionHideouts()
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("Trying to activate faction hideouts early :(");

            ReassignHideouts();

            foreach (var (mfClan, mfData) in _mfData.Select(x => (x.Key, x.Value)).ToList())
            {
                int NumExistingActiveOrScheduled = NumActiveOrScheduledHideouts(mfClan);
                if (mfData == null)
                    continue;
                if (Patches.LoadXmlHideoutsPatch.SpawnGatedClans.Contains(mfClan.StringId) && mfClan != Clan.PlayerClan)
                {
                    continue;
                }

                int expectedActive = ExpectedActive(mfClan.IsMinorFaction, mfData.NumActiveHideouts, mfData.NumTotalHideouts);
                if (expectedActive < 0)
                {
                    mfData.NumActiveHideouts = 0;
                    continue;
                }
                if (expectedActive != mfData.NumActiveHideouts)
                {
                    mfData.NumActiveHideouts = expectedActive;
                }
                if (NumExistingActiveOrScheduled == mfData.NumActiveHideouts)
                    continue;
                int NumExpectedActiveOrScheduled = mfData.NumActiveHideouts;

                var hideouts = mfData.Hideouts;

                // deactivate hideouts if needed
                for (int i = NumExpectedActiveOrScheduled; i < NumExistingActiveOrScheduled; i++)
                {
                    int deactivateIndex = MBRandom.RandomInt(hideouts.Count);
                    while (!hideouts[deactivateIndex].IsActiveOrScheduled)
                    {
                        deactivateIndex = MBRandom.RandomInt(hideouts.Count);
                    }
                    hideouts[deactivateIndex].DeactivateHideout(false);
                }

                // activate new hideouts if needed
                for (int i = NumExistingActiveOrScheduled; i < NumExpectedActiveOrScheduled; i++)
                {
                    // only free hideouts with usable ground
                    var freeIndexes = Enumerable.Range(0, hideouts.Count).Where(k => !hideouts[k].IsActiveOrScheduled).ToList();
                    var usableIndexes = freeIndexes
                        .Where(k => !HideoutPlacement.IsUnplaceable(hideouts[k].Settlement, hideouts[k].OwnerClan)).ToList();
                    if (usableIndexes.Count == 0)
                    {
                        IMFDiagnostics.Info($"[Guard] {mfClan.StringId}: no valid ground for any of {freeIndexes.Count} free hideouts, not activating");
                        break;
                    }
                    int homeIndex = usableIndexes.FindIndex(k => hideouts[k].Settlement != null && hideouts[k].Settlement == mfClan.InitialHomeSettlement);
                    int activateIndex = homeIndex >= 0 ? usableIndexes[homeIndex] : usableIndexes[MBRandom.RandomInt(usableIndexes.Count)];
                    hideouts[activateIndex].ActivateHideoutFirstTime();
                }
            }
        }

        /// <summary>
        /// Gets a clan's camp data.
        /// </summary>
        /// <returns>the data, or null if the clan has none</returns>
        public MFData? GetClanMFData(Clan c)
        {
            MFData? mfData = null;
            _mfData?.TryGetValue(c, out mfData);
            return mfData;
        }

        /// <summary>
        /// Shuts a camp after a raid or a nomad move and opens another free camp of the same clan.
        /// If there is no other free camp, a raided camp reopens after the usual delay and a nomad camp stays where it is.
        /// </summary>
        /// <param name="reason">why the camp is being cleared, raid or nomad movement</param>
        public void ClearHideout(MinorFactionHideout oldHideout, DeactivationReason reason)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't switch Hideout due to uninitialized Hideout Manager");

            var oldSettlement = oldHideout.Settlement;
            var mfClan = oldHideout.OwnerClan;

            // -1 = nowhere to go, the raided camp reopens after the delay
            int nextIndex = -1;
            try
            {
                var owned = _mfData[mfClan].Hideouts;
                nextIndex = PickNextHideout(owned.Count, owned.IndexOf(oldHideout),
                    owned.Select(h => h == oldHideout || h.IsActiveOrScheduled || HideoutPlacement.IsUnplaceable(h.Settlement, h.OwnerClan)).ToList(),
                    MBRandom.RandomInt);
            }
            catch (KeyNotFoundException ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"IMF ERROR: Somehow we tried to clear a hideout not in MFHManager._mfData. {ex}", Colors.Red));
                return;
            }
            if (nextIndex < 0 && reason == DeactivationReason.NomadMovement)
                return;

            if (oldSettlement.Parties.Count > 0)
            {
                foreach (MobileParty mobileParty in new List<MobileParty>(oldSettlement.Parties))
                {
                    LeaveSettlementAction.ApplyForParty(mobileParty);
                    mobileParty.Ai.SetDoNotAttackMainParty(3);
                }
            }
            oldHideout.IsSpotted = false;
            oldSettlement.IsVisible = false;
            try
            {
                if (nextIndex < 0)
                {
                    IMFDiagnostics.Info($"[Raid] {mfClan.StringId} no other free hideout, reusing {oldSettlement.StringId} after the delay");
                    oldHideout.RespawnAfterRaid();
                    return;
                }
                var newHideout = _mfData[mfClan].Hideouts[nextIndex];
                IMFDiagnostics.Info($"[Raid] {mfClan.StringId}: {oldSettlement.StringId} cleared ({reason}), next camp {newHideout.Settlement.StringId}");
                if (reason == DeactivationReason.Raid)
                    oldHideout.MoveHideoutsPostRaid(newHideout);
                else if (reason == DeactivationReason.NomadMovement)
                    oldHideout.MoveHideoutsNomad(newHideout);
            }
            catch (KeyNotFoundException ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"IMF ERROR: Somehow we tried to clear a hideout not in MFHManager._mfData. {ex}", Colors.Red));
            }
            
        }

        /// <summary>
        /// Gets a clan's active camps. Empty for clans that aren't minor factions.
        /// </summary>
        public List<MinorFactionHideout> GetActiveHideoutsOfClan(Clan minorFaction)
        {
            var result = new List<MinorFactionHideout>();
            if (minorFaction == null || !minorFaction.IsMinorFaction || !this.HasFaction(minorFaction))
                return result;

            foreach(var mfHideout in _mfData[minorFaction].Hideouts)
            {
                if (mfHideout.IsActive)
                    result.Add(mfHideout);
            }
            return result;
        }

        public bool HasActiveHideouts(Clan c)
        {
            return GetActiveHideoutsOfClan(c).Count > 0;
        }

        /// <summary>
        /// Counts a clan's camps that are active or waiting to become active.
        /// </summary>
        public int NumActiveOrScheduledHideouts(Clan c)
        {
            if (c == null || !c.IsMinorFaction || !this.HasFaction(c))
                return 0;

            int count = 0;
            foreach (var mfHideout in _mfData[c].Hideouts)
            {
                if (mfHideout.IsActive || mfHideout.IsScheduledToBeActive)
                    count++;
            }
            return count;
        }

        public bool HasActiveOrScheduledHideouts(Clan c)
        {
            return NumActiveOrScheduledHideouts(c) > 0;
        }

        /// <summary>
        /// Checks if the manager tracks camps for this clan.
        /// </summary>
        public bool HasFaction(Clan minorFaction)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't initialize hideouts list in Hideout Manager");
            return _mfData.ContainsKey(minorFaction);
        }
        /// <summary>
        /// Shuts every camp of a destroyed clan, removes their notables and hands the camps to other clans.
        /// </summary>
        // Removes clan from MF data and other shit if its destroyed... DONT do this anymore, just give it activeHideouts of 0
        // TODO: remove owner of clans
        internal void RemoveClan(Clan destroyedClan)
        {
            if (!this.HasFaction(destroyedClan))
                return;
            foreach (var mfHideout in _mfData[destroyedClan].Hideouts)
            {
                // need to do this because can't kill notables and iterate over Notables list simultaneously.
                var notablesToKill = new List<Hero>();
                notablesToKill.AddRange(mfHideout.Settlement.Notables);
                foreach (Hero notable in notablesToKill)
                {
                    KillCharacterAction.ApplyByRemove(notable, true, true);
                }
                mfHideout.DeactivateHideout(false);
            }

            _mfData[destroyedClan].NumActiveHideouts = 0;
            ReassignHideouts();
        }

        /// <summary>
        /// Marks a clan to declare war on the player when its mercenary contract ends.
        /// </summary>
        public void RegisterClanForPlayerWarOnEndingMercenaryContract(Clan minorFaction)
        {
            if (!minorFaction.IsMinorFaction)
                throw new MBIllegalValueException($"{minorFaction} is not a minor faction clan, you cannot register it for a later war with Player.");
            if (HasFaction(minorFaction))
                _mfData[minorFaction].IsWaitingForWarWithPlayer = true;
        }

        /// <summary>
        /// Declares the war set up by RegisterClanForPlayerWarOnEndingMercenaryContract, if the clan is waiting for it.
        /// </summary>
        public void DeclareWarOnPlayerIfNeeded(Clan minorFaction)
        {
            if (HasFaction(minorFaction) && _mfData[minorFaction].IsWaitingForWarWithPlayer)
            {
                DeclareWarAction.ApplyByPlayerHostility(minorFaction.MapFaction, Clan.PlayerClan.MapFaction);
                _mfData[minorFaction].IsWaitingForWarWithPlayer = false;
            }
        }

        /// <summary>
        /// Checks if every camp a clan owns is active.
        /// </summary>
        internal bool IsFullHideoutOccupationMF(Clan? c)
        {
            return c != null && _mfData.ContainsKey(c) && _mfData[c].NumTotalHideouts == _mfData[c].NumActiveHideouts;
        }

        internal MBReadOnlyList<MinorFactionHideout> AllMFHideouts
        {
            get => this._hideouts;
        }

        /// <summary>
        /// Turns gang leader notables in camps into preachers, camp notables are meant to be preachers.
        /// </summary>
        public static void ConvertGangLeaderMFNotablesToPreachers()
        {
            if (Campaign.Current == null)
                return;
            foreach (Hero h in Hero.AllAliveHeroes)
            {
                if (h != null && Helpers.IsMFHideout(h.CurrentSettlement) && h.Occupation == Occupation.GangLeader)
                {
                    h.SetNewOccupation(Occupation.Preacher);
                    // TODO: remove debug
                    //InformationManager.DisplayMessage(new InformationMessage(h.Name + " Is gang leader: " + h.IsGangLeader + " Is notable: " + h.IsNotable));
                }
                    
            }
        }

        public static IMFManager? Current { get; set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<Clan, MFData> _mfData;

        private bool _mfDataInitialized;

        private MBList<MinorFactionHideout> _hideouts;

        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;

        /// <summary>
        /// How many camps a clan should have active, capped by how many it owns.
        /// </summary>
        /// <param name="isRealMinorClan">the clan is still a minor faction</param>
        /// <param name="expected">the clan's num_active_hideouts</param>
        /// <param name="totalHideouts">camps the clan owns</param>
        /// <returns>the number to activate, or -1 if the clan should have none</returns>
        public static int ExpectedActive(bool isRealMinorClan, int expected, int totalHideouts)
        {
            if (!isRealMinorClan || totalHideouts <= 0)
                return -1;
            return Math.Min(Math.Max(expected, 0), totalHideouts);
        }

        /// <summary>
        /// Picks a random free camp other than the one just cleared.
        /// </summary>
        /// <param name="count">number of camps the clan owns</param>
        /// <param name="clearedIdx">index of the camp that was just cleared</param>
        /// <param name="busy">true for camps that can't be used (active, scheduled or on bad ground)</param>
        /// <param name="random">random number below the given count, passed in so tests can fix it</param>
        /// <returns>the index of the next camp, or -1 if none is free</returns>
        public static int PickNextHideout(int count, int clearedIdx, IReadOnlyList<bool> busy, Func<int, int> random)
        {
            var free = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (i == clearedIdx)
                    continue;
                if (i < busy.Count && busy[i])
                    continue;
                free.Add(i);
            }
            if (free.Count == 0)
                return -1;
            int pick = random(free.Count);
            return free[Math.Max(0, Math.Min(pick, free.Count - 1))];
        }
    }

}

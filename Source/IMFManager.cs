using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
using ImprovedMinorFactions.Source;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions
{
    internal class IMFManager
    {
        public IMFManager() 
        {
            _LoadedMFHideouts = new Dictionary<string, MinorFactionHideout>();
            _mfData = new Dictionary<Clan, MFData>();
        }

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

        // true if mfh1 should replace mfh2.
        private bool ShouldReplaceHideout(MinorFactionHideout mfh1, MinorFactionHideout mfh2)
        {
            if (mfh1?.Settlement?.Notables == null) return false;
            if (mfh1.IsActiveOrScheduled && mfh1.Settlement.Notables.Count > 0)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public void AddLoadedMFHideout(MinorFactionHideout mfh)
        {
            if (_LoadedMFHideouts.ContainsKey(mfh.StringId))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{mfh.StringId} duplicate hideout saves! Please send your save file to modders on Nexus Mods.", Colors.Red));
                if (ShouldReplaceHideout(mfh, _LoadedMFHideouts[mfh.StringId]))
                    _LoadedMFHideouts[mfh.StringId] = mfh;
                return;
            }
            
            if (mfh.OwnerClan == null || mfh.OwnerClan.Leader == null)
                return;

            _LoadedMFHideouts[mfh.StringId] = mfh;
        }

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

        // should only be done when all Settlements are loaded in
        public bool TryInitMFHideoutsLists()
        {
            if (Campaign.Current?.Settlements == null)
                return false;
            if (_mfDataInitialized)
                return true;

            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                if ((settlement.OwnerClan?.IsMinorFaction ?? false) && Helpers.IsMFHideout(settlement))
                {
                    // TODO: store ownerless hideouts somewhere
                    var mfClan = settlement.OwnerClan;
                    CreateMFDataIfNone(mfClan);
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

            // mfh1 is closer
            if (mfh1.Settlement.Position2D.Distance(c.InitialPosition)
                < mfh2.Settlement.Position2D.Distance(c.InitialPosition))
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

        // Transfer ownership of an mfh from one clan to another or do nothing.
        private void AssignHideoutToClan(Clan newOwner, MinorFactionHideout mfh)
        {
            Clan oldOwner = mfh!.OwnerClan!;
            if (oldOwner == newOwner)
                return;
            if (mfh.IsActiveOrScheduled)
                throw new Exception("Trying to reassign active hideout to another clan!");

            // TODO: update Name
            mfh.OwnerClan = newOwner;
            mfh.Settlement.Name = new TextObject("{=dt9393yju}{MINOR_FACTION} Hideout")
                .SetTextVariable("MINOR_FACTION", newOwner.Name);

            if (newOwner.Culture.NotableAndWandererTemplates.Count > 0)
                mfh.Settlement.Culture = newOwner.Culture;

            GetClanMFData(newOwner)!.AddMFHideout(mfh);
            if (_mfData.ContainsKey(oldOwner))
                GetClanMFData(oldOwner)!.Hideouts.Remove(mfh);
        }

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

        // This function dynamically distributes MFHs to all Minor factions whether or not they are listed in settlements.xml
        private void ReassignHideouts()
        {
            // Preprocess lists
            HashSet<Clan> hideoutAssignedNonNomadClans = new HashSet<Clan>();
            List<Clan> NonNomadMFClans = new List<Clan>();
            // add MFs with assigned hideouts to clan list first to give them priority later
            foreach (Clan c in Clan.All)
            {
                if (c.IsMinorFaction && c != Clan.PlayerClan && !c.IsRebelClan && !c.IsNomad && !c.IsEliminated && HasFaction(c) && GetClanMFData(c)!.Hideouts.Count > 0)
                {
                    hideoutAssignedNonNomadClans.Add(c);
                    NonNomadMFClans.Add(c);
                }
            }

            // add MFs without assigned hideouts to clan list second to give them less priority
            foreach (Clan c in Clan.All)
            {
                if (c.IsMinorFaction && c != Clan.PlayerClan && !c.IsRebelClan && !c.IsNomad && !c.IsEliminated && !NonNomadMFClans.Contains(c))
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

        // activates 1 hideout for every Minor Faction if they have no currently active hideouts
        public void ActivateAllFactionHideouts()
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("Trying to activate faction hideouts early :(");

            ReassignHideouts();

            foreach (var (mfClan, mfData) in _mfData.Select(x => (x.Key, x.Value)))
            {
                int NumExistingActiveOrScheduled = NumActiveOrScheduledHideouts(mfClan);
                if (mfData == null || NumExistingActiveOrScheduled == mfData.NumActiveHideouts)
                    continue;
                int NumExpectedActiveOrScheduled = mfData.NumActiveHideouts;

                var hideouts = mfData.Hideouts;
                if (NumExpectedActiveOrScheduled > hideouts.Count)
                {
                    throw new Exception($"{mfClan} has more active hideouts than hideouts." +
                        $" Change num_active_hideouts in mf_data.xml to a number less than or equal to {mfData.NumTotalHideouts}");
                }

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
                    int activateIndex = MBRandom.RandomInt(hideouts.Count);
                    while (hideouts[activateIndex].IsActiveOrScheduled)
                    {
                        activateIndex = MBRandom.RandomInt(hideouts.Count);
                    }
                    hideouts[activateIndex].ActivateHideoutFirstTime();
                }
            }
        }

        public MFData? GetClanMFData(Clan c)
        {
            MFData? mfData = null;
            _mfData?.TryGetValue(c, out mfData);
            return mfData;
        }

        public void ClearHideout(MinorFactionHideout oldHideout, DeactivationReason reason)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't switch Hideout due to uninitialized Hideout Manager");

            var oldSettlement = oldHideout.Settlement;
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
            var mfClan = oldHideout.OwnerClan;
            try
            {
                var hideouts = _mfData[mfClan].Hideouts;
                int activateIndex = MBRandom.RandomInt(hideouts.Count);
                while ((hideouts[activateIndex].Settlement.Equals(oldHideout.Settlement) || hideouts[activateIndex].IsScheduledToBeActive) 
                    && !IsFullHideoutOccupationMF(mfClan))
                    activateIndex = MBRandom.RandomInt(hideouts.Count);
                var newHideout = hideouts[activateIndex];
                if (reason == DeactivationReason.Raid)
                    oldHideout.MoveHideoutsPostRaid(newHideout);
                else if (reason == DeactivationReason.NomadMovement)
                    oldHideout.MoveHideoutsNomad(newHideout);
            } catch (KeyNotFoundException ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"IMF ERROR: Somehow we tried to clear a hideout not in MFHManager._mfData. {ex}", Colors.Red));
            }
            
        }

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

        public bool HasFaction(Clan minorFaction)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't initialize hideouts list in Hideout Manager");
            return _mfData.ContainsKey(minorFaction);
        }
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

        public void RegisterClanForPlayerWarOnEndingMercenaryContract(Clan minorFaction)
        {
            if (!minorFaction.IsMinorFaction)
                throw new MBIllegalValueException($"{minorFaction} is not a minor faction clan, you cannot register it for a later war with Player.");
            if (HasFaction(minorFaction))
                _mfData[minorFaction].IsWaitingForWarWithPlayer = true;
        }

        public void DeclareWarOnPlayerIfNeeded(Clan minorFaction)
        {
            if (HasFaction(minorFaction) && _mfData[minorFaction].IsWaitingForWarWithPlayer)
            {
                DeclareWarAction.ApplyByPlayerHostility(minorFaction.MapFaction, Clan.PlayerClan.MapFaction);
                _mfData[minorFaction].IsWaitingForWarWithPlayer = false;
            }
        }

        internal bool IsFullHideoutOccupationMF(Clan? c)
        {
            return c != null && _mfData.ContainsKey(c) && _mfData[c].NumTotalHideouts == _mfData[c].NumActiveHideouts;
        }

        internal MBReadOnlyList<MinorFactionHideout> AllMFHideouts
        {
            get => this._hideouts;
        }

        public static void ConvertGangLeaderMFNotablesToPreachers()
        {
            foreach (Hero h in Hero.AllAliveHeroes)
            {
                if (h != null && Helpers.IsMFHideout(h.CurrentSettlement) && h.Occupation == Occupation.GangLeader)
                {
                    h.SetNewOccupation(Occupation.Preacher);
                    // TODO: remove debug
                    InformationManager.DisplayMessage(new InformationMessage(h.Name + " Is gang leader: " + h.IsGangLeader + " Is notable: " + h.IsNotable));
                }
                    
            }
        }

        public static IMFManager? Current { get; set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<Clan, MFData> _mfData;

        private bool _mfDataInitialized;

        private MBList<MinorFactionHideout> _hideouts;

        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;

    }
}

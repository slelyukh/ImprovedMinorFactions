using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
using Microsoft.VisualBasic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions
{
    internal class MFData : MBObjectBase
    {
        public MFData(Clan c)
        {
            Hideouts = new List<MinorFactionHideout>();
            NumActiveHideouts = IMFModels.DefaultNumActiveHideouts(c);
            NumMilitiaFirstTime = IMFModels.DefaultNumMilitiaFirstTime(c);
            NumMilitiaPostRaid = IMFModels.DefaultNumMilitiaPostRaid(c);
            NumLvl3Militia = IMFModels.DefaultNumLvl3Militia(c);
            NumLvl2Militia = IMFModels.DefaultNumLvl2Militia(c);
            MaxMilitia = IMFModels.DefaultMaxMilitia(c);
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            bool isInitialized = base.IsInitialized;
            base.Deserialize(objectManager, node);
            this.testField = node.Attributes.GetNamedItem("test").Value;
            InformationManager.DisplayMessage(new InformationMessage($"{this.testField} hello?"));
            return;
        }

        internal void AddMFHideout(MinorFactionHideout mfh)
        {
            Hideouts.Add(mfh);
        }

        internal void AddMFHideout(Settlement s)
        {
            if (!Helpers.IsMFHideout(s))
                throw new Exception("Error 3242: trying to add non mfh settlement to MF hideouts list.");
            Hideouts.Add(Helpers.GetMFHideout(s));
        }

        
        public int NumTotalHideouts
        {
            get => Hideouts.Count;
        }

        public int NumActiveHideouts;
        public int NumMilitiaFirstTime;
        public int NumMilitiaPostRaid;
        public int NumLvl3Militia;
        public int NumLvl2Militia;
        public int MaxMilitia;
        public string testField;
        public List<MinorFactionHideout> Hideouts;
        public bool IsWaitingForWarWithPlayer;
    }
    internal class IMFManager
    {
        public IMFManager() 
        {
            _LoadedMFHideouts = new Dictionary<string, MinorFactionHideout>();
        }

        private static void InitManager()
        {
            IMFManager.Current = new IMFManager();
            Current._mfDataInitialized = Current.TryInitMFHideoutsLists();
        }

        public static void InitManagerIfNone()
        {
            if (IMFManager.Current == null)
                InitManager();

        }
        public static void ClearManager()
        {
            IMFManager.Current = null;
        }

        public void AddLoadedMFHideout(MinorFactionHideout mfh)
        {
            if (_LoadedMFHideouts.ContainsKey(mfh.StringId))
                InformationManager.DisplayMessage(new InformationMessage($"{mfh.StringId} duplicate hideout saves! Please send your save file to modders on Nexus Mods.", Colors.Red));
            
            if (mfh.OwnerClan == null || mfh.OwnerClan.Leader == null)
                return;
            
            _LoadedMFHideouts[mfh.StringId] = mfh;
        }

        public MinorFactionHideout GetLoadedMFHideout(string stringId)
        {
            MinorFactionHideout mfh = null;
            _LoadedMFHideouts.TryGetValue(stringId, out mfh);
            return mfh;
        }

        // should only be done when all Settlements are loaded in
        public bool TryInitMFHideoutsLists()
        {
            if (Campaign.Current == null)
                return false;
            if (_mfDataInitialized)
                return true;
            _mfData = new Dictionary<Clan, MFData>();
            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                if ((settlement.OwnerClan?.IsMinorFaction ?? false) && Helpers.IsMFHideout(settlement))
                {
                    var mfClan = settlement.OwnerClan;
                    if (!_mfData.ContainsKey(mfClan))
                        _mfData[mfClan] = new MFData(mfClan);
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

        // activates 1 hideout for every Minor Faction if they have no currently active hideouts
        public void ActivateAllFactionHideouts()
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("Trying to activate faction hideouts early :(");

            foreach (var (mfClan, mfData) in _mfData.Select(x => (x.Key, x.Value)))
            {
                if (HasActiveOrScheduledHideouts(mfClan) || mfData == null)
                    continue;
                var hideouts = mfData.Hideouts;
                if (mfData.NumActiveHideouts > hideouts.Count)
                {
                    throw new Exception($"{mfClan} has more active hideouts than hideouts...");
                }
                for (int i = 0; i < mfData.NumActiveHideouts; i++)
                {
                    int activateIndex = MBRandom.RandomInt(hideouts.Count);
                    while (hideouts[activateIndex].IsActive || hideouts[activateIndex].IsScheduledToBeActive)
                    {
                        activateIndex = MBRandom.RandomInt(hideouts.Count);
                    }
                    hideouts[activateIndex].ActivateHideoutFirstTime();
                }
            }
        }

        public MFData GetClanMFData(Clan c)
        {
            MFData mfData = null;
            _mfData.TryGetValue(c, out mfData);
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
                InformationManager.DisplayMessage(new InformationMessage("IMF ERROR: Somehow we tried to clear a hideout not in MFHManager._mfData.", Colors.Red));
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

        public bool HasActiveOrScheduledHideouts(Clan minorFaction)
        {
            if (minorFaction == null || !minorFaction.IsMinorFaction || !this.HasFaction(minorFaction))
                return false;

            foreach (var mfHideout in _mfData[minorFaction].Hideouts)
            {
                if (mfHideout.IsActive || mfHideout.IsScheduledToBeActive)
                    return true;
            }
            return false;
        }

        public bool HasFaction(Clan minorFaction)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't initialize hideouts list in Hideout Manager");
            return _mfData.ContainsKey(minorFaction);
        }

        internal void RemoveClan(Clan destroyedClan)
        {
            if (!this.HasFaction(destroyedClan))
                return;
            foreach (var mfHideout in _mfData[destroyedClan].Hideouts)
            {
                if (mfHideout.IsActive)
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
            }
                
            _mfData.Remove(destroyedClan);
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

        internal bool IsFullHideoutOccupationMF(Clan c)
        {
            return _mfData.ContainsKey(c) && _mfData[c].NumTotalHideouts == _mfData[c].NumActiveHideouts;
        }

        internal MBReadOnlyList<MinorFactionHideout> AllMFHideouts
        {
            get => this._hideouts;
        }

        public int GetNumTotalHideouts(Clan c)
        {
            return _mfData[c].NumTotalHideouts;
        }

        public int GetNumActiveHideouts(Clan c)
        {
            return _mfData[c].NumActiveHideouts;
        }

        public int GetNumMilitiaFirstTime(Clan c)
        {
            return _mfData[c].NumMilitiaFirstTime;
        }

        public int GetNumMilitiaPostRaid(Clan c)
        {
            return _mfData[c].NumMilitiaPostRaid;
        }

        public int GetNumLvl3Militia(Clan c)
        {
            return _mfData[c].NumLvl3Militia;
        }

        public int GetNumLvl2Militia(Clan c)
        {
            return _mfData[c].NumLvl2Militia;
        }

        public int GetMaxMilitia(Clan c)
        {
            return _mfData[c].MaxMilitia;
        }

        public static IMFManager Current { get; private set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<Clan, MFData> _mfData;

        private bool _mfDataInitialized;

        private MBList<MinorFactionHideout> _hideouts;

        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;
    }
}

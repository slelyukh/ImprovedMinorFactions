using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImprovedMinorFactions.Patches;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

namespace ImprovedMinorFactions
{
    internal class MFHideoutManager
    {
        public MFHideoutManager() 
        {
            _LoadedMFHideouts = new Dictionary<string, MinorFactionHideout>();
            _factionsWaitingForWar = new HashSet<Clan>();
        }

        public static void initManager()
        {
            MFHideoutManager.Current = new MFHideoutManager();
            Current._factionHideoutsInitialized = Current.TryInitMFHideoutsLists();
        }

        public static void initManagerIfNone()
        {
            if (MFHideoutManager.Current == null)
                initManager();

        }
        public static void clearManager()
        {
            MFHideoutManager.Current = null;
        }

        public void AddLoadedMFHideout(MinorFactionHideout mfh)
        {
            _LoadedMFHideouts.Add(mfh.StringId, mfh);
        }

        public MinorFactionHideout GetLoadedMFHideout(string stringId)
        {
            return _LoadedMFHideouts[stringId];
        }

        // should only be done when all Settlements are loaded in
        public bool TryInitMFHideoutsLists()
        {
            if (Campaign.Current == null)
                return false;
            if (_factionHideoutsInitialized)
                return true;
            _factionHideouts = new Dictionary<Clan, List<MinorFactionHideout>>();
            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                if (settlement.OwnerClan?.IsMinorFaction ?? Helpers.isMFHideout(settlement))
                {
                    var key = settlement.OwnerClan;
                    List<MinorFactionHideout> list = null;
                    if (!_factionHideouts.ContainsKey(key))
                        _factionHideouts[key] = new List<MinorFactionHideout>();
                    _factionHideouts[key].Add(Helpers.GetSettlementMFHideout(settlement));
                }
            }
            _factionHideoutsInitialized = true;
            this._hideouts =
                (from x in Campaign.Current.Settlements
                where Helpers.isMFHideout(x)
                select Helpers.GetSettlementMFHideout(x)).ToMBList();
            return true;
        }

        public void ActivateAllFactionHideouts()
        {
            if (!_factionHideoutsInitialized)
                throw new Exception("Trying to activate faction hideouts early :(");
            foreach(var (faction, hideouts) in _factionHideouts.Select(x => (x.Key, x.Value)))
            {
                int activateIndex = MBRandom.RandomInt(hideouts.Count);
                hideouts[activateIndex].ActivateHideoutFirstTime();
            }
        }

        public void SwitchActiveHideout(MinorFactionHideout oldHideout)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't switch Hideout due to uninitialized Hideout Manager");
            var hideouts = _factionHideouts[oldHideout.OwnerClan];
            int activateIndex = MBRandom.RandomInt(hideouts.Count);
            while (hideouts[activateIndex].Settlement.Equals(oldHideout.Settlement))
                activateIndex = MBRandom.RandomInt(hideouts.Count);
            var newHideout = hideouts[activateIndex];
            oldHideout.MoveHideouts(newHideout);
        }

        public MinorFactionHideout GetHideoutOfClan(Clan minorFaction)
        {
            if (!minorFaction.IsMinorFaction || !this.HasFaction(minorFaction))
                return null;
            foreach(var mfHideout in _factionHideouts[minorFaction])
            {
                if (mfHideout.IsActive)
                    return mfHideout;
            }
            return null;
        }

        public bool HasFaction(Clan minorFaction)
        {
            if (!TryInitMFHideoutsLists())
                throw new Exception("can't initialize hideouts list in Hideout Manager");
            return _factionHideouts.ContainsKey(minorFaction);
        }

        internal void RemoveClan(Clan destroyedClan)
        {
            if (!this.HasFaction(destroyedClan))
                return;
            foreach (var mfHideout in _factionHideouts[destroyedClan])
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
                    mfHideout.DeactivateHideout();
                }
            }
                
            _factionHideouts.Remove(destroyedClan);
            _factionsWaitingForWar.Remove(destroyedClan);
        }

        public void RegisterClanForPlayerWarOnEndingMercenaryContract(Clan minorFaction)
        {
            if (!minorFaction.IsMinorFaction)
                throw new MBIllegalValueException($"{minorFaction} is not a minor faction clan, you cannot register it for a later war with Player.");
            _factionsWaitingForWar.Add(minorFaction);
        }

        public void DeclareWarOnPlayerIfNeeded(Clan minorFaction)
        {
            if (_factionsWaitingForWar.Contains(minorFaction))
            {
                DeclareWarAction.ApplyByPlayerHostility(minorFaction.MapFaction, Clan.PlayerClan.MapFaction);
                _factionsWaitingForWar.Remove(minorFaction);
            }
        }

        internal MBReadOnlyList<MinorFactionHideout> AllMFHideouts
        {
            get => this._hideouts;
        }

        public static MFHideoutManager Current { get; private set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<Clan, List<MinorFactionHideout>> _factionHideouts;

        private HashSet<Clan> _factionsWaitingForWar;

        private bool _factionHideoutsInitialized;

        private MBList<MinorFactionHideout> _hideouts;

        // TODO: make private
        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;
    }
}

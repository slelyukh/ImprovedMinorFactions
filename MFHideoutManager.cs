using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
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

        public void addLoadedMFHideout(MinorFactionHideout mfh)
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
            _factionHideouts = new Dictionary<IFaction, List<MinorFactionHideout>>();
            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                if (settlement.OwnerClan?.IsMinorFaction ?? Helpers.isMFHideout(settlement))
                {
                    var key = settlement.MapFaction;
                    List<MinorFactionHideout> list = null;
                    if (!_factionHideouts.ContainsKey(key))
                        _factionHideouts[key] = new List<MinorFactionHideout>();
                    _factionHideouts[key].Add(settlement.SettlementComponent as MinorFactionHideout);
                }
            }
            _factionHideoutsInitialized = true;
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
            var hideouts = _factionHideouts[oldHideout.MapFaction];
            int activateIndex = MBRandom.RandomInt(hideouts.Count);
            while (hideouts[activateIndex].Settlement.Equals(oldHideout.Settlement))
                activateIndex = MBRandom.RandomInt(hideouts.Count);
            var newHideout = hideouts[activateIndex];
            oldHideout.moveHideouts(newHideout);
        }

        public MinorFactionHideout getHideoutOfFaction(IFaction minorFaction)
        {
            if (!minorFaction.IsMinorFaction)
                throw new Exception("trying to get hideout of regular faction");
            if (!this.hasFaction(minorFaction))
                return null;
            foreach(var mfHideout in _factionHideouts[minorFaction])
            {
                if (mfHideout.IsActive)
                    return mfHideout;
            }
            return null;
        }

        public bool hasFaction(IFaction minorFaction)
        {
            return _factionHideouts.ContainsKey(minorFaction);
        }

        public static MFHideoutManager Current { get; private set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<IFaction, List<MinorFactionHideout>> _factionHideouts;

        private bool _factionHideoutsInitialized;

        // TODO: make private
        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;
    }
}

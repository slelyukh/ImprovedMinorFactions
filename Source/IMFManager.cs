using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
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
    internal class IMFManager
    {
        public IMFManager() 
        {
            _LoadedMFHideouts = new Dictionary<string, MinorFactionHideout>();
            _mfData = new Dictionary<Clan, MFData>();
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

        // should only be called when you are sure the clan is not already here
        internal void AddMFData(Clan c, MFData mfd)
        {
            _mfData[c] = mfd;
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
                int NumExistingActiveOrScheduled = NumActiveOrScheduledHideouts(mfClan);
                if (mfData == null || NumExistingActiveOrScheduled == mfData.NumActiveHideouts)
                    continue;
                int NumExpectedActiveOrScheduled = mfData.NumActiveHideouts;

                var hideouts = mfData.Hideouts;
                if (NumExpectedActiveOrScheduled > hideouts.Count)
                {
                    throw new Exception($"{mfClan} has more active hideouts than hideouts...");
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
            MFData mfData = null;
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

        public static IMFManager Current { get; private set; }

        private Dictionary<string, MinorFactionHideout> _LoadedMFHideouts;

        private Dictionary<Clan, MFData> _mfData;

        private bool _mfDataInitialized;

        private MBList<MinorFactionHideout> _hideouts;

        public IEnumerable<Tuple<Settlement, GameEntity>> _allMFHideouts;

    }


    public class MFData : MBObjectBase
    {
        public MFData()
        {
        }

        public MFData(Clan c)
        {
            InitData(c);
        }

        private void InitData(Clan c)
        {
            IMFManager.InitManagerIfNone();
            Hideouts = new List<MinorFactionHideout>();
            mfClan = c;
            NumActiveHideouts = IMFModels.NumActiveHideouts(c);
            NumMilitiaFirstTime = IMFModels.NumMilitiaFirstTime(c);
            NumMilitiaPostRaid = IMFModels.NumMilitiaPostRaid(c);
            NumLvl3Militia = IMFModels.NumLvl3Militia(c);
            NumLvl2Militia = IMFModels.NumLvl2Militia(c);
            MaxMilitia = IMFModels.MaxMilitia(c);
            ClanGender = IMFModels.ClanGender(c);
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            string? mfClanId = node.Attributes?.GetNamedItem("minor_faction")?.Value.Replace("Faction.", "");
            Clan? mfClan = Clan.All?.Find((x) => x.StringId == mfClanId);
            if (mfClan == null)
                mfClan = objectManager.ReadObjectReferenceFromXml<Clan>("minor_faction", node);
            if (mfClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"IMF: {mfClanId} not found. mf_data.xml data not loaded", Colors.Red));
                return;
            }

            InitData(mfClan);

            // parse data for me
            if (node.Attributes?.GetNamedItem("num_active_hideouts") != null)
                this.NumActiveHideouts = Int32.Parse(node.Attributes.GetNamedItem("num_active_hideouts").Value);
            if (node.Attributes?.GetNamedItem("num_militia_first_time") != null)
                this.NumMilitiaFirstTime = Int32.Parse(node.Attributes.GetNamedItem("num_militia_first_time").Value);
            if (node.Attributes?.GetNamedItem("num_militia_post_raid") != null)
                this.NumMilitiaPostRaid = Int32.Parse(node.Attributes.GetNamedItem("num_militia_post_raid").Value);
            if (node.Attributes?.GetNamedItem("num_lvl2_militia") != null)
                this.NumLvl2Militia = Int32.Parse(node.Attributes.GetNamedItem("num_lvl2_militia").Value);
            if (node.Attributes?.GetNamedItem("num_lvl3_militia") != null)
                this.NumLvl3Militia = Int32.Parse(node.Attributes.GetNamedItem("num_lvl3_militia").Value);
            if (node.Attributes?.GetNamedItem("max_militia") != null)
                this.MaxMilitia = Int32.Parse(node.Attributes.GetNamedItem("max_militia").Value);
            if (node.Attributes?.GetNamedItem("clan_gender") != null)
            {
                string genderString = node.Attributes.GetNamedItem("clan_gender").Value;
                if (genderString == "Male")
                {
                    this.ClanGender = IMFModels.Gender.Male;
                } else if (genderString == "Female")
                {
                    this.ClanGender = IMFModels.Gender.Female;
                } else if ((genderString == "Any"))
                {
                    this.ClanGender = IMFModels.Gender.Any;
                } else
                {
                    throw new Exception($"{genderString} is not a valid gender type for {mfClanId}. " +
                        $"The only valid options are 'Male', 'Female', or 'Any'");
                }
            }

            if (IMFManager.Current?.GetClanMFData(mfClan) != null)
            {
                // set values for current data
                MFData existingData = IMFManager.Current.GetClanMFData(mfClan);
                existingData.NumActiveHideouts = this.NumActiveHideouts;
                existingData.NumMilitiaFirstTime = this.NumMilitiaFirstTime;
                existingData.NumMilitiaPostRaid = this.NumMilitiaPostRaid;
                existingData.NumLvl2Militia = this.NumLvl2Militia;
                existingData.NumLvl3Militia = this.NumLvl3Militia;
                existingData.MaxMilitia = this.MaxMilitia;
                existingData.ClanGender = this.ClanGender;
            }
            else
            {
                IMFManager.Current?.AddMFData(mfClan, this);
            }
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

        public Clan mfClan;
        public int NumActiveHideouts;
        public int NumMilitiaFirstTime;
        public int NumMilitiaPostRaid;
        public int NumLvl3Militia;
        public int NumLvl2Militia;
        public int MaxMilitia;
        internal IMFModels.Gender ClanGender;
        public List<MinorFactionHideout> Hideouts;
        public bool IsWaitingForWarWithPlayer;
    }
}

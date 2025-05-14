using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using System.Xml;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using System;
using MathF = TaleWorlds.Library.MathF;

namespace ImprovedMinorFactions
{

    public class MFHideoutTypeDefiner : SaveableTypeDefiner
    {
        public MFHideoutTypeDefiner() : base(792_346_751)
        {
        }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(MinorFactionHideout), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(List<MinorFactionHideout>));
            base.ConstructContainerDefinition(typeof(Dictionary<MBGUID, MinorFactionHideout>));
            base.ConstructContainerDefinition(typeof(Dictionary<string, MinorFactionHideout>));
        }
    }
    public class MinorFactionHideout : SettlementComponent, ISpottable
    {


        public CampaignTime NextPossibleAttackTime
        {
            get => _nextPossibleAttackTime;
        }

        public CampaignTime NextPossibleActivationTime
        {
            get => _nextPossibleActivationTime;
        }

        [LoadInitializationCallback]
        private void OnLoad()
        {
            IMFManager.InitManagerIfNone();
            IMFManager.Current!.AddLoadedMFHideout(this);
        }

        public void ActivateHideoutFirstTime()
        {
            if (this.OwnerClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} Attempting to be activated with no owner clan!!!", Color.Black));
                return;
            }

            if (IMFManager.Current.GetActiveHideoutsOfClan(this.OwnerClan).Contains(this))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} Double Activated!!!!", Color.Black));
                return;
                // throw new System.Exception("double clan activation");
            }
            var notable1 = HeroCreator.CreateHeroAtOccupation(Occupation.Preacher, this.Settlement);
            var notable2 = HeroCreator.CreateHeroAtOccupation(Occupation.Preacher, this.Settlement);
            if (notable1 == null || notable2 == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} notable creation has failed!!! Please report this on Nexus mod page", Color.Black));
            } else
            {
                notable1!.IsMinorFactionHero = true;
                notable2!.IsMinorFactionHero = true;
            }

            ActivateHideout();
            base.Settlement.Militia = IMFModels.NumMilitiaFirstTime(this.OwnerClan);
            this.Hearth = 350;
        }

        private void ActivateHideout(List<Hero> newNotables, DeactivationReason reason)
        {
            // move notables to this hideout
            foreach (Hero notable in newNotables)
            {
                if (reason == DeactivationReason.Raid)
                    notable.VolunteerTypes = new CharacterObject[6];
                notable.StayingInSettlement = this.Settlement;
                notable.UpdateHomeSettlement();
            }
            if (reason == DeactivationReason.Raid)
                ScheduleHideoutActivation(IMFModels.HideoutActivationDelay(this.OwnerClan));
            else
                ActivateHideout();
        }

        private void ScheduleHideoutActivation(CampaignTime waitTime)
        {
            _nextPossibleActivationTime = CampaignTime.Now + waitTime;
            _activationScheduled = true;
        }

        private void ActivateHideout()
        {
            this._isActive = true;
            this._isSpotted = false;
            this._activationTime = CampaignTime.Now;

            // add lvl 3 militias
            int numLvl3 = IMFModels.NumLvl3Militia(this.OwnerClan);
            base.Settlement.Militia = numLvl3;
            UpgradeMilitia(numLvl3);
            UpgradeMilitia(numLvl3);

            int numLvl2 = IMFModels.NumLvl2Militia(this.OwnerClan);
            base.Settlement.Militia += numLvl2;
            UpgradeMilitia(numLvl2);

            base.Settlement.Militia = IMFModels.NumMilitiaPostRaid(this.OwnerClan);
            this.Hearth = 200;


            Helpers.CallPrivateMethod(this.OwnerClan, "set_HomeSettlement", new object[] { this.Settlement });
            foreach (Hero hero in this.OwnerClan.Heroes)
            {
                hero.UpdateHomeSettlement();
            }
        }

        public void MoveHideoutsPostRaid(MinorFactionHideout newHideout)
        {
            List<Hero> notables = this.Settlement.Notables.ToList();
            newHideout.ActivateHideout(notables, DeactivationReason.Raid);
            this.DeactivateHideout(true);
        }

        public void MoveHideoutsNomad(MinorFactionHideout newHideout)
        {
            List<Hero> notables = this.Settlement.Notables.ToList();
            newHideout.ActivateHideout(notables, DeactivationReason.NomadMovement);
            newHideout.Hearth = this.Hearth;
            // We will handle the militia party ourselves
            CopyMilitiaToHideout(newHideout);


            this.DeactivateHideout(true);
            if (PlayerEncounter.Current != null
                && PlayerEncounter.Current.IsPlayerWaiting
                && PlayerEncounter.Current.EncounterSettlementAux == this.Settlement)
            {
                GameMenu.SwitchToMenu("mf_hideout_nomads_left");
                // When we leave it should stop being visible
                this.Settlement.IsVisible = true;
            }
            InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=bTIQXD4cq}The {MINOR_FACTION} have moved their camp to a new location.")
                        .SetTextVariable("MINOR_FACTION", this.OwnerClan.Name).ToString()));
        }

        // Method counts how many militia of each tier there are and gives otherHideout that number of militia of that tier
        internal void CopyMilitiaToHideout(MinorFactionHideout otherHideout)
        {
            otherHideout.Settlement.Militia = 0;
            int[] tierCounts = new int[15];
            foreach (TroopRosterElement troopInfo in this.Settlement.MilitiaPartyComponent.Party.MemberRoster.GetTroopRoster())
            {
                int tier = troopInfo.Character.Tier;
                int numTroop = troopInfo.Number;
                tierCounts[tier] += numTroop;
            }
            for (int i = tierCounts.Length - 1; i >= 0; i--)
            {
                otherHideout.Settlement.Militia += tierCounts[i];
                for (int j = otherHideout.OwnerClan.BasicTroop.Tier; j < i; j++)
                {
                    otherHideout.UpgradeMilitia(tierCounts[i]);
                }
            }
        }

        internal void DeactivateHideout(bool createEvent)
        {
            if (createEvent)
            {
                CampaignEventDispatcher.Instance.OnHideoutDeactivated(this.Settlement);
            }

            this.Settlement.Notables.Clear();
            this._isActive = false;
            this._isSpotted = false;
            this.Settlement.IsVisible = false;
            base.Settlement.Militia = 0;
            this.Hearth = 0;
            this._activationScheduled = false;
        }

        public void UpgradeMilitia(int count)
        {
            var militiaParty = this.Settlement.MilitiaPartyComponent.Party;
            var militiaRoster = militiaParty.MemberRoster;
            count = MathF.Min(count, (int)this.Settlement.Militia);
            int loopCounter = 0;
            while (count > 0 && loopCounter < 20)
            {
                CharacterObject? troopToUpgrade = null;
                loopCounter++;

                // TODO: randomly choose troop to upgrade
                foreach (var troop in militiaRoster.GetTroopRoster())
                {
                    if (troop.Character.UpgradeTargets.Length != 0)
                        troopToUpgrade = troop.Character;
                }
                if (troopToUpgrade != null)
                {
                    int amountToUpgrade = MathF.Min(militiaRoster.GetTroopCount(troopToUpgrade), count);
                    var upgradeTarget = troopToUpgrade.UpgradeTargets[MBRandom.RandomInt(troopToUpgrade.UpgradeTargets.Length)];
                    militiaRoster.AddToCounts(troopToUpgrade, -amountToUpgrade);
                    militiaRoster.AddToCounts(upgradeTarget, amountToUpgrade);
                    count -= amountToUpgrade;
                }
                else
                {
                    break;
                }
            }

        }

        public void DailyTick()
        {
            if (_activationScheduled && _nextPossibleActivationTime.IsPast)
            {
                ActivateHideout();
                _activationScheduled = false;
            }

            if (!this.IsActive)
                return;
            float curMil = base.Settlement.Militia;
            float milChange = this.MilitiaChange.ResultNumber;
            if (curMil + milChange > IMFModels.MaxMilitia(this.OwnerClan) + 1)
            {
                base.Settlement.Militia = (curMil + milChange) - 1;
                this.UpgradeMilitia(1);
            }
            else
            {
                base.Settlement.Militia += this.MilitiaChange.ResultNumber;
            }

            // backwards compatibility
            if (base.Settlement.Militia < IMFModels.NumMilitiaPostRaid(this.OwnerClan))
            {
                base.Settlement.Militia = IMFModels.NumMilitiaPostRaid(this.OwnerClan);
                this.UpgradeMilitia(10);
            }

            // daily random chance for militia to upgrade
            if (IMFModels.UpgradeMilitiaRandom(this.OwnerClan))
                this.UpgradeMilitia(1);

            this.Hearth += this.HearthChange.ResultNumber;
        }

        protected override void OnInventoryUpdated(ItemRosterElement item, int count)
        {
            // TODO: implement if/when hideouts get an inventory
        }

        // TODO: make hideouts only attackable at night
        public void UpdateNextPossibleAttackTime()
        {
            _nextPossibleAttackTime = CampaignTime.Now + CampaignTime.Hours(12f);
        }

        public IEnumerable<PartyBase> GetDefenderParties(MapEvent.BattleTypes battleType)
        {
            yield return base.Settlement.Party;
            foreach (MobileParty mobileParty in base.Settlement.Parties)
            {

                // only minor faction members in hideout will defend
                if (mobileParty.MapFaction.Equals(Settlement.MapFaction) || mobileParty.IsMilitia)
                {
                    yield return mobileParty.Party;
                }
            }
            yield break;
        }

        // makes sure nomad camp does not migrate before this time
        public void ExtendNomadCampMigrationTimePastTime(CampaignTime time)
        {
            if (this.ActivationTime + IMFModels.NomadHideoutLifetime < time)
            {
                this.ActivationTime = time - IMFModels.NomadHideoutLifetime + CampaignTime.Days(3);
            }
        }

        public PartyBase? GetNextDefenderParty(ref int partyIndex, MapEvent.BattleTypes battleType)
        {
            partyIndex++;
            if (partyIndex == 0)
            {
                return base.Settlement.Party;
            }
            for (int i = partyIndex - 1; i < base.Settlement.Parties.Count; i++)
            {
                MobileParty mobileParty = base.Settlement.Parties[i];
                if (mobileParty.MapFaction.Equals(Settlement.MapFaction))
                {
                    partyIndex = i + 1;
                    return mobileParty.Party;
                }
            }
            return null;
        }

        public string SceneName { get; private set; }


        public IFaction MapFaction
        {
            get => _ownerclan!.MapFaction;
        }

        public bool IsSpotted
        {
            get => _isSpotted;
            set => _isSpotted = value;
        }

        public void SetScene(string sceneName)
        {
            this.SceneName = sceneName;
        }

        public MinorFactionHideout()
        {
            this._isSpotted = false;
        }

        public override void OnPartyEntered(MobileParty mobileParty)
        {
            base.OnPartyEntered(mobileParty);
        }


        public override void OnPartyLeft(MobileParty mobileParty)
        {
            base.OnPartyLeft(mobileParty);
        }

        public override void OnRelatedPartyRemoved(MobileParty mobileParty)
        {
            base.OnRelatedPartyRemoved(mobileParty);
        }

        public override void OnInit()
        {
            base.Settlement.IsVisible = false;
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.BackgroundCropPosition = float.Parse(node!.Attributes!.GetNamedItem("background_crop_position")!.Value!);
            base.BackgroundMeshName = node!.Attributes!.GetNamedItem("background_mesh")!.Value;
            base.WaitMeshName = node!.Attributes!.GetNamedItem("wait_mesh")!.Value;
            base.Deserialize(objectManager, node);
            if (node!.Attributes!.GetNamedItem("scene_name") != null)
            {
                this.SceneName = node!.Attributes!.GetNamedItem("scene_name")!.InnerText;
            }
        }

        public ExplainedNumber MilitiaChange
        {
            get
            {
                return IMFModels.GetMilitiaChange(this);
            }
        }

        public Clan OwnerClan
        {
            get => this._ownerclan;
            set => this._ownerclan = value;
        }

        public CultureObject Culture
        {
            get => this.Settlement.Culture;
        }

        public bool IsActive
        {
            get => this._isActive;
        }

        public CampaignTime ActivationTime
        {
            get => this._activationTime;
            set => this._activationTime = value;
        }

        public bool IsActiveOrScheduled
        {
            get => this.IsActive || this._activationScheduled;
        }
        public bool IsScheduledToBeActive
        {
            get => this._activationScheduled;
        }

        public ExplainedNumber HearthChange
        {
            get
            {
                return IMFModels.GetHearthChange(this, true);
            }
        }

        public bool IsNomad
        {
            get => this.OwnerClan.IsNomad;
        }

        public bool Equals(MinorFactionHideout mfh)
        {
            return (
                this.Hearth == mfh.Hearth 
                && this.Settlement == mfh.Settlement
                && this.Gold == mfh.Gold
                && this.OwnerClan == mfh.OwnerClan
                && this.IsActive == mfh.IsActive
                );
        }

        [SaveableField(101)]
        private bool _isSpotted;


        [SaveableField(102)]
        private CampaignTime _nextPossibleAttackTime;

        [SaveableField(103)]
        private Clan _ownerclan;

        [SaveableField(104)]
        public float Hearth;

        [SaveableField(105)]
        private bool _isActive;

        [SaveableField(106)]
        private CampaignTime _nextPossibleActivationTime;

        [SaveableField(107)]
        private bool _activationScheduled;

        [SaveableField(108)]
        private CampaignTime _activationTime;
    }

    public enum DeactivationReason
    {
        Raid,
        NomadMovement
    }
}

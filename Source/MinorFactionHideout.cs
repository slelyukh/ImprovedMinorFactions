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
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions
{

    public class MFHideoutTypeDefiner : SaveableTypeDefiner
    {
        public MFHideoutTypeDefiner() : base(1_346_751)
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

        [LoadInitializationCallback]
        private void OnLoad()
        {
            MFHideoutManager.initManagerIfNone();
            MFHideoutManager.Current.AddLoadedMFHideout(this);
        }

        // CreateHeroAtOccupation copypasta
        private void RenameHeroToNewOccupation(Hero heroToRename, Occupation occupation, uint seed)
        {
            IEnumerable<CharacterObject> enumerable = Enumerable.Where<CharacterObject>(
                this.Settlement.Culture.NotableAndWandererTemplates, 
                (CharacterObject x) => x.Occupation == occupation);
            int randNum = this.Settlement.RandomIntWithSeed(seed, enumerable.Count());
            int i = 0;
            CharacterObject template = null;
            foreach (CharacterObject characterObject in enumerable)
            {
                if (i == randNum)
                {
                    template = characterObject;
                    break;
                }
                i++;
            }
            if (template == null)
                throw new System.Exception("random number failure");
            Hero hero = (Hero) Helpers.callPrivateMethod(null, "CreateNewHero", new object[] { template, 25 }, typeof(HeroCreator));
            TextObject firstName;
            TextObject fullName;
            NameGenerator.Current.GenerateHeroNameAndHeroFullName(hero, out firstName, out fullName, false);
            heroToRename.SetName(fullName, firstName);
        }

        public void ActivateHideoutFirstTime()
        {
            var notable1 = HeroCreator.CreateHeroAtOccupation(Occupation.Preacher, this.Settlement);
            RenameHeroToNewOccupation(notable1, Occupation.GangLeader, 1);
            notable1.IsMinorFactionHero = true;
            var notable2 = HeroCreator.CreateHeroAtOccupation(Occupation.Preacher, this.Settlement);
            RenameHeroToNewOccupation(notable2, Occupation.GangLeader, 2);
            notable2.IsMinorFactionHero = true;

            ActivateHideout();
            base.Settlement.Militia += 5;
            this.Hearth = 300;
        }

        private void ActivateHideout(List<Hero> newNotables)
        {
            foreach (Hero notable in newNotables)
            {
                MoveNotableIn(notable);
            }

            ActivateHideout();
        }

        private void ActivateHideout()
        {
            if (!Helpers.IsMFClanInitialized(this.OwnerClan))
            {
                if (this.OwnerClan == null)
                { 
                    InformationManager.DisplayMessage(new InformationMessage("HIDEOUT HAS NO CLAN CANT ACTIVATE"));
                    return;
                }
                Helpers.DetermineBasicTroopsForMinorFactionsCopypasta();
            }
            
            this._isActive = true;
            this._isSpotted = false;

            // add 3 high tier militias
            base.Settlement.Militia = 3;
            UpgradeMilitia(3);
            UpgradeMilitia(3);

            // add 5 mid tier militias
            base.Settlement.Militia += 5;
            UpgradeMilitia(5);

            base.Settlement.Militia += 7;
            this.Hearth = 200;


            Helpers.callPrivateMethod(this.OwnerClan, "set_HomeSettlement", new object[] { this.Settlement });
            foreach (Hero hero in this.OwnerClan.Heroes)
            {
                hero.UpdateHomeSettlement();
            }
        }

        public void MoveHideouts(MinorFactionHideout newHideout)
        {
            // TODO: move hideout inventory
            List<Hero> notables = this.Settlement.Notables.ToList<Hero>();
            newHideout.ActivateHideout(notables);
            this.DeactivateHideout();
        }

        // updates notable info and removes notable from old settlement and adds to new settlement
        private void MoveNotableIn(Hero notable)
        {
            notable.VolunteerTypes = new CharacterObject[6];
            notable.StayingInSettlement = this.Settlement;
            notable.UpdateHomeSettlement();
        }

        internal void DeactivateHideout()
        {
            this._isActive = false;
            this._isSpotted = false;
            this.Settlement.IsVisible = false;
            base.Settlement.Militia = 0;
            this.Hearth = 0;
        }

        private void UpgradeMilitia(int count)
        {
            var militiaParty = this.Settlement.MilitiaPartyComponent.Party;
            var militiaRoster = militiaParty.MemberRoster;
            var troopList = militiaRoster.GetTroopRoster();
            CharacterObject troopToUpgrade = null;
            while (count > 0)
            {
                foreach (var troop in troopList)
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
            if (Helpers.IsMFClanInitialized(_ownerclan))
            {
                float curMil = base.Settlement.Militia;
                float milChange = this.MilitiaChange.ResultNumber;
                if (curMil + milChange > MFHideoutModels.GetMaxMilitiaInHideout() + 1) {
                    base.Settlement.Militia = (curMil + milChange) - 1;
                    this.UpgradeMilitia(1);
                } else {
                    base.Settlement.Militia += this.MilitiaChange.ResultNumber;
                }
                    
            }
                
            this.Hearth += this.HearthChange.ResultNumber;
        }

        //TODO: add AllMinorFactionHideouts if needed
        //public static MBReadOnlyList<Hideout> All
        //{
        //    get
        //    {
        //        return Campaign.Current.AllHideouts;
        //    }
        //}

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
            // List<MobileParty>.Enumerator enumerator = default(List<MobileParty>.Enumerator);
            yield break;
        }

        // Hideout copypasta
        public PartyBase GetNextDefenderParty(ref int partyIndex, MapEvent.BattleTypes battleType)
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
            get => _ownerclan.MapFaction;
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
            base.BackgroundCropPosition = float.Parse(node.Attributes.GetNamedItem("background_crop_position").Value);
            base.BackgroundMeshName = node.Attributes.GetNamedItem("background_mesh").Value;
            base.WaitMeshName = node.Attributes.GetNamedItem("wait_mesh").Value;
            base.Deserialize(objectManager, node);
            if (node.Attributes.GetNamedItem("scene_name") != null)
            {
                this.SceneName = node.Attributes.GetNamedItem("scene_name").InnerText;
            }
        }

        // TODO: make reasonable militia calcs
        public ExplainedNumber MilitiaChange
        { 
            get
            {
                return MFHideoutModels.GetMilitiaChange(this.Settlement);
            }
        }

        public Clan OwnerClan
        {
            get => this._ownerclan;
            set => this._ownerclan = value;
        }

        public bool IsActive
        {
            get => this._isActive;
        }

        // TODO: make reasonable hearth calcs
        public ExplainedNumber HearthChange
        {
            get
            {
                return MFHideoutModels.GetHearthChange(this.Settlement, true);
            }
        }


        [SaveableField(420)]
        private bool _isSpotted;


        [SaveableField(422)]
        private CampaignTime _nextPossibleAttackTime;

        [SaveableField(423)]
        private Clan _ownerclan;

        [SaveableField(424)]
        public float Hearth;

        [SaveableField(425)]
        private bool _isActive;
        
    }
}

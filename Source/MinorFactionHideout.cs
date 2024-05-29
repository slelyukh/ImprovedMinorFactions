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
using static TaleWorlds.CampaignSystem.CampaignTime;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

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
            IMFManager.Current.AddLoadedMFHideout(this);
        }

        // CreateHeroAtOccupation copypasta
        // TODO: rename hero to Vakken or Darshi
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
            // TODO: return null instead of throwing an exception
            if (template == null)
                if (Helpers.IsDebugMode)
                    throw new System.Exception($"ERROR: {this.Settlement.Culture} does not have template for {occupation}");
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"ERROR: {this.Settlement.Culture} does not have template for {occupation}", Colors.Red));
                    return;
                }
            Hero hero = (Hero) Helpers.CallPrivateMethod(null, "CreateNewHero", new object[] { template, 25 }, typeof(HeroCreator));
            TextObject firstName;
            TextObject fullName;
            NameGenerator.Current.GenerateHeroNameAndHeroFullName(hero, out firstName, out fullName, false);
            heroToRename.SetName(fullName, firstName);
        }

        // CreateHeroAtOccupation copypasta
        private Hero CreateNotable(Occupation neededOccupation, IMFModels.Gender gender, Settlement forcedHomeSettlement)
        {
            Settlement settlement = forcedHomeSettlement ?? SettlementHelper.GetRandomTown(null);
            IEnumerable<CharacterObject> enumerable;
            if (gender == IMFModels.Gender.Male)
            {
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation && !x.IsFemale);
            } else if (gender == IMFModels.Gender.Female)
            {
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation && x.IsFemale);
            }
            else
            {
                enumerable = Enumerable.Where<CharacterObject>(settlement.Culture.NotableAndWandererTemplates, (CharacterObject x) => x.Occupation == neededOccupation);
            }

            int num = 0;
            foreach (CharacterObject characterObject in enumerable)
            {
                int num2 = characterObject.GetTraitLevel(DefaultTraits.Frequency) * 10;
                num += ((num2 > 0) ? num2 : 100);
            }

            if (!Enumerable.Any(enumerable))
            {
                return null;
            }
            CharacterObject template = null;
            int num3 = settlement.RandomIntWithSeed((uint)settlement.Notables.Count, 1, num);
            foreach (CharacterObject characterObject2 in enumerable)
            {
                int num4 = characterObject2.GetTraitLevel(DefaultTraits.Frequency) * 10;
                num3 -= ((num4 > 0) ? num4 : 100);
                if (num3 < 0)
                {
                    template = characterObject2;
                    break;
                }
            }

            Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, -1);
            CultureObject hideoutCulture = forcedHomeSettlement.OwnerClan.Culture;
            // Give Darshi, Nord, and Vakken MFs correct notable names
            // todo check why ori is so popular
            if (Helpers.IsMinorCulture(hideoutCulture))
            {
                var nameList = new List<TextObject>();
                if (hero.IsFemale)
                   nameList = hideoutCulture.FemaleNameList;
                else
                    nameList = hideoutCulture.MaleNameList;
                
                TextObject firstName = nameList.GetRandomElement();
                TextObject fullName = (TextObject)Helpers.CallPrivateMethod(NameGenerator.Current, "GenerateHeroFullName", new object[] { hero, firstName, true });
                hero.SetName(fullName, firstName);
            }

            if (hero.HomeSettlement.IsVillage && hero.HomeSettlement.Village.Bound != null && hero.HomeSettlement.Village.Bound.IsCastle)
            {
                float value = MBRandom.RandomFloat * 20f;
                hero.AddPower(value);
            }
            if (neededOccupation != Occupation.Wanderer)
            {
                hero.ChangeState(Hero.CharacterStates.Active);
                EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
                int amount = 10000;
                GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);
            }
            CharacterObject template2 = hero.Template;
            if (((template2 != null) ? template2.HeroObject : null) != null && hero.Template.HeroObject.Clan != null && hero.Template.HeroObject.Clan.IsMinorFaction)
            {
                hero.SupporterOf = hero.Template.HeroObject.Clan;
            }
            else
            {
                hero.SupporterOf = HeroHelper.GetRandomClanForNotable(hero);
            }

            if (neededOccupation != Occupation.Wanderer)
            {
                Helpers.CallPrivateMethod(null, "AddRandomVarianceToTraits", new object[] {hero}, typeof(HeroCreator));
            }

            return hero;
        }

        

        public void ActivateHideoutFirstTime()
        {
            if (IMFManager.Current.GetActiveHideoutsOfClan(this.OwnerClan).Contains(this))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} Double Activated!!!!", Color.Black));
                throw new System.Exception("double clan activation");
            }

            // TODO: use mfData for gender info
            if (this.OwnerClan.StringId == "skolderbrotva")
            {
                var notable1 = CreateNotable(Occupation.GangLeader, IMFModels.Gender.Male, this.Settlement);
                var notable2 = CreateNotable(Occupation.GangLeader, IMFModels.Gender.Male, this.Settlement);
                notable1.IsMinorFactionHero = true;
                notable2.IsMinorFactionHero = true;
            } else
            {
                var notable1 = CreateNotable(Occupation.GangLeader, IMFModels.Gender.Any, this.Settlement);
                var notable2 = CreateNotable(Occupation.GangLeader, IMFModels.Gender.Any, this.Settlement);
                notable1.IsMinorFactionHero = true;
                notable2.IsMinorFactionHero = true;
            }

            ActivateHideout();
            base.Settlement.Militia = IMFManager.Current.GetNumMilitiaFirstTime(this.OwnerClan);
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

            var imfManager = IMFManager.Current;

            // add lvl 3 militias
            int numLvl3 = imfManager.GetNumLvl3Militia(this.OwnerClan);
            base.Settlement.Militia = numLvl3;
            UpgradeMilitia(numLvl3);
            UpgradeMilitia(numLvl3);

            int numLvl2 = imfManager.GetNumLvl2Militia(this.OwnerClan);
            base.Settlement.Militia += numLvl2;
            UpgradeMilitia(numLvl2);

            base.Settlement.Militia = imfManager.GetNumMilitiaPostRaid(this.OwnerClan);
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
            
            this._isActive = false;
            this._isSpotted = false;
            this.Settlement.IsVisible = false;
            base.Settlement.Militia = 0;
            this.Hearth = 0;
        }

        public void UpgradeMilitia(int count)
        {
            var militiaParty = this.Settlement.MilitiaPartyComponent.Party;
            var militiaRoster = militiaParty.MemberRoster;
            count = MathF.Min(count, (int) this.Settlement.Militia);
            int loopCounter = 0;
            while (count > 0 && loopCounter < 20)
            {
                CharacterObject troopToUpgrade = null;
                loopCounter++;
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
            // if (loopCounter >= 20)
                // InformationManager.DisplayMessage(new InformationMessage("UPGRADE MILITIA INFINITE LOOP"));
            
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
            if (curMil + milChange > IMFManager.Current.GetMaxMilitia(this.OwnerClan) + 1)
            {
                base.Settlement.Militia = (curMil + milChange) - 1;
                this.UpgradeMilitia(1);
            }
            else
            {
                base.Settlement.Militia += this.MilitiaChange.ResultNumber;
            }

            // backwards compatibility
            if (base.Settlement.Militia < IMFManager.Current.GetNumMilitiaPostRaid(this.OwnerClan))
            {
                base.Settlement.Militia = IMFManager.Current.GetNumMilitiaPostRaid(this.OwnerClan);
                this.UpgradeMilitia(10);
            }

            // 1/15 chance for a random militia to upgrade every day
            if (MBRandom.RandomInt(15) == 1)
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

        public ExplainedNumber MilitiaChange
        { 
            get
            {
                return IMFModels.GetMilitiaChange(this.Settlement);
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

        public CampaignTime ActivationTime
        {
            get => this._activationTime;
            set => this._activationTime = value;
        }

        public bool IsScheduledToBeActive
        {
            get => this._activationScheduled;
        }

        public ExplainedNumber HearthChange
        {
            get
            {
                return IMFModels.GetHearthChange(this.Settlement, true);
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

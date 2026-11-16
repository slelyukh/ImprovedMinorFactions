using System.Collections.Generic;
using HarmonyLib;
using ImprovedMinorFactions.Patches;
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

    /// <summary>
    /// Registers the camp component and its saved lists with the save system.
    /// </summary>
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
    /// <summary>
    /// The settlement component of a minor faction camp. Holds the camp's state (active, spotted, hearth, owner, saved position),
    /// handles activating it with notables and militia, moving to another camp after a raid or a nomad move, and the daily militia and hearth growth.
    /// </summary>
    // ISpottable is gone since 1.5.4 so we track IsSpotted ourselves
    public class MinorFactionHideout : SettlementComponent
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

        /// <summary>
        /// Activates the camp at the start of a campaign or when a clan first needs one: creates two preacher notables,
        /// sets up the militia and hearth. Does nothing if the camp has no owner, is already active or has no usable ground.
        /// </summary>
        public void ActivateHideoutFirstTime()
        {
            if (this.OwnerClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} Attempting to be activated with no owner clan!!!", Color.Black));
                return;
            }

            if (IMFManager.Current!.GetActiveHideoutsOfClan(this.OwnerClan).Contains(this))
            {
                InformationManager.DisplayMessage(new InformationMessage($"{this.Name} Double Activated!!!!", Color.Black));
                return;
                // throw new System.Exception("double clan activation");
            }

            // no usable ground = don't go live, parties would spawn off the navmesh
            if (HideoutPlacement.IsUnplaceable(this.Settlement, this.OwnerClan))
            {
                IMFDiagnostics.Info($"[Guard] {this.Settlement?.StringId} of {this.OwnerClan?.StringId} not activated: no valid ground for it on this map");
                return;
            }
            var notable1 = TryCreatePreacher("#1");
            var notable2 = TryCreatePreacher("#2");
            if (notable1 == null || notable2 == null)
            {
                IMFDiagnostics.Info($"{this.Settlement?.StringId} started without {(notable1 == null ? "notable#1 " : "")}{(notable2 == null ? "notable#2" : "")}");
            }

            ActivateHideout();
            base.Settlement.Militia = IMFModels.NumMilitiaFirstTime(this.OwnerClan!);
            this.Hearth = 350;
        }

        /// <summary>
        /// Creates a preacher notable in the camp, picking a culture that has preacher templates first.
        /// </summary>
        /// <param name="tag">label for the log, e.g. "#1"</param>
        /// <returns>the notable, or null if it couldn't be made</returns>
        // CreateNotable throws on a null template, so make sure there is one.
        private Hero? TryCreatePreacher(string tag)
        {
            try
            {
                EnsureNotableCulture();
                if (PreacherTemplateCount(this.Settlement?.Culture) == 0)
                {
                    IMFDiagnostics.Info($"[Culture] {this.Settlement?.StringId}: no Preacher templates, skipping notable{tag}");
                    return null;
                }
                return HeroCreator.CreateNotable(Occupation.Preacher, this.Settlement);
            }
            catch (Exception ex)
            {
                IMFDiagnostics.Log($"CreateNotable{tag} failed for {this.Settlement?.StringId} (culture={this.Settlement?.Culture?.StringId}):", ex);
                return null;
            }
        }

        /// <summary>
        /// Counts a culture's preacher notable templates.
        /// </summary>
        internal static int PreacherTemplateCount(CultureObject? culture)
        {
            if (culture == null || culture.NotableTemplates == null)
                return 0;
            return culture.NotableTemplates.Count(t => t != null && t.Occupation == Occupation.Preacher);
        }

        /// <summary>
        /// If the camp's culture can't make preachers, switches it to the owner's culture, the nearest town's, or any culture that can.
        /// </summary>
        // settlement culture, owner's culture, nearest town with preachers, then any culture with preachers
        private void EnsureNotableCulture()
        {
            Settlement? s = this.Settlement;
            if (s == null || PreacherTemplateCount(s.Culture) > 0)
                return;

            CultureObject? pick = PreacherTemplateCount(this.OwnerClan?.Culture) > 0 ? this.OwnerClan!.Culture : null;
            if (pick == null)
            {
                float best = float.MaxValue;
                Vec2 here = s.GetPosition2D;
                foreach (Settlement other in Settlement.All)
                {
                    if (other == null || other == s || !(other.IsTown || other.IsVillage || other.IsCastle))
                        continue;
                    if (PreacherTemplateCount(other.Culture) == 0)
                        continue;
                    float d = here.DistanceSquared(other.GetPosition2D);
                    if (d < best)
                    {
                        best = d;
                        pick = other.Culture;
                    }
                }
            }
            if (pick == null)
            {
                foreach (CultureObject c in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
                {
                    if (PreacherTemplateCount(c) > 0)
                    {
                        pick = c;
                        break;
                    }
                }
            }
            if (pick == null)
                return;
            IMFDiagnostics.Verbose($"[Culture] {s.StringId}: culture {s.Culture?.StringId} has no Preacher notable templates, using {pick.StringId}");
            s.Culture = pick;
        }

        /// <summary>
        /// Moves the notables of the old camp here and activates this camp. After a raid the activation waits for the usual delay.
        /// </summary>
        /// <param name="newNotables">notables from the camp being left</param>
        /// <param name="reason">why the old camp closed</param>
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

        /// <summary>
        /// Marks the camp to activate once the wait time has passed, DailyTick does the activation.
        /// </summary>
        private void ScheduleHideoutActivation(CampaignTime waitTime)
        {
            _nextPossibleActivationTime = CampaignTime.Now + waitTime;
            _activationScheduled = true;
        }

        /// <summary>
        /// Activates the camp: places it on the map, picks a scene for the ground, fills the militia and makes it the clan's home.
        /// </summary>
        private void ActivateHideout()
        {
            HideoutPlacement.PlaceOnActivation(this);
            this.EnsureTerrainFittingScene();
            this._isActive = true;
            if (Clan.PlayerClan == this.OwnerClan)
            {
                this.PlayerSpotHideout();
            }
            else
            {
                this._isSpotted = false;
            }
            this._activationTime = CampaignTime.Now;

            // Start with a strong garrison (103 militia) so it doesn't get wiped early.
            int startingMilitia = IMFModels.NumMilitiaFirstTime(this.OwnerClan);
            base.Settlement.Militia = startingMilitia;

            int numLvl3 = IMFModels.NumLvl3Militia(this.OwnerClan);
            int numLvl2 = IMFModels.NumLvl2Militia(this.OwnerClan);
            UpgradeMilitia(numLvl3);
            UpgradeMilitia(numLvl3);
            UpgradeMilitia(numLvl2);

            this.Hearth = 200;


            Helpers.CallPrivateMethod(this.OwnerClan, "set_HomeSettlement", new object[] { this.Settlement });
            foreach (Hero hero in this.OwnerClan.Heroes)
            {
                hero.UpdateHomeSettlement();
            }
        }

        /// <summary>
        /// After a raid, moves the notables to the clan's next camp and shuts this one.
        /// </summary>
        public void MoveHideoutsPostRaid(MinorFactionHideout newHideout)
        {
            List<Hero> notables = this.Settlement.Notables.ToList();
            newHideout.ActivateHideout(notables, DeactivationReason.Raid);
            this.DeactivateHideout(true);
        }

        /// <summary>
        /// Nowhere else to move, so this camp reopens after the delay. It is emptied now so the raid still counts.
        /// </summary>
        internal void RespawnAfterRaid()
        {
            List<Hero> notables = this.Settlement.Notables.ToList();
            this.DeactivateHideout(true);
            ActivateHideout(notables, DeactivationReason.Raid);
        }

        /// <summary>
        /// Moves a nomad camp: notables, hearth and militia go to the new camp and this one shuts. A player waiting at the old camp gets told they left.
        /// </summary>
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

        /// <summary>
        /// Gives the other camp the same number of militia of each tier as this one.
        /// </summary>
        // Method counts how many militia of each tier there are and gives otherHideout that number of militia of that tier
        internal void CopyMilitiaToHideout(MinorFactionHideout otherHideout)
        {
            if (this.Settlement.MilitiaPartyComponent?.Party?.MemberRoster == null)
                return;
            otherHideout.Settlement.Militia = 0;
            int[] tierCounts = new int[15];
            foreach (TroopRosterElement troopInfo in this.Settlement.MilitiaPartyComponent.Party.MemberRoster.GetTroopRoster())
            {
                int tier = Math.Max(0, Math.Min(troopInfo.Character.Tier, tierCounts.Length - 1));
                int numTroop = troopInfo.Number;
                tierCounts[tier] += numTroop;
            }
            int baseTier = Helpers.GetBasicTroop(otherHideout.OwnerClan)?.Tier ?? 1;
            for (int i = tierCounts.Length - 1; i >= 0; i--)
            {
                otherHideout.Settlement.Militia += tierCounts[i];
                for (int j = baseTier; j < i; j++)
                {
                    otherHideout.UpgradeMilitia(tierCounts[i]);
                }
            }
        }

        /// <summary>
        /// Shuts the camp: removes its notables, militia and hearth and hides it on the map.
        /// </summary>
        /// <param name="createEvent">fire the game's hideout deactivated event, which things like quests listen to</param>
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

        /// <summary>
        /// Upgrades up to count militia troops to one of their upgrade targets.
        /// </summary>
        public void UpgradeMilitia(int count)
        {
            if (this.Settlement.MilitiaPartyComponent?.Party == null)
                return;
            var militiaParty = this.Settlement.MilitiaPartyComponent.Party;
            var militiaRoster = militiaParty.MemberRoster;
            count = MathF.Min(count, (int)this.Settlement.Militia);
            int loopCounter = 0;
            while (count > 0 && loopCounter < 20)
            {
                CharacterObject? troopToUpgrade = null;
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

        }

        /// <summary>
        /// Marks the camp as found by the player and shows it on the map.
        /// </summary>
        public void PlayerSpotHideout()
        {
            this.IsSpotted = true;
            this.Settlement.IsVisible = true;
            CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, this.Settlement.Party);
        }

        /// <summary>
        /// Activates a scheduled camp once its time comes. For active camps, grows militia and hearth, and upgrades militia
        /// when they are over the cap or by a daily random chance.
        /// </summary>
        public void DailyTick()
        {
            if (_activationScheduled && _nextPossibleActivationTime.IsPast)
            {
                ActivateHideout();
                _activationScheduled = false;
            }

            if (!this.IsActive)
                return;
            if (this.OwnerClan == null)
                return;
            float curMil = base.Settlement.Militia;
            float milChange = this.MilitiaChange.ResultNumber;
            if (curMil + milChange > IMFModels.MaxMilitia(this.OwnerClan, this.Hearth) + 1)
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

        /// <summary>
        /// Gets the parties that defend the camp: the camp itself, its militia, and parties of the owner's faction inside it.
        /// </summary>
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

        /// <summary>
        /// Gets defenders one at a time, the camp first and then the owner's parties inside it.
        /// </summary>
        /// <param name="partyIndex">where the last call stopped, moved on by this call</param>
        /// <returns>the next defender, or null when there are no more</returns>
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


        public override IFaction MapFaction
        {
            get => _ownerclan?.MapFaction;
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

        private Vec2 _sceneCheckedAt = new Vec2(float.NaN, float.NaN);

        /// <summary>
        /// Swaps the camp's scene for one that fits the ground it is on now. Only checks again after the camp moved.
        /// </summary>
        internal void EnsureTerrainFittingScene()
        {
            try
            {
                Settlement? s = this.Settlement;
                if (s == null)
                    return;
                Vec2 here = s.GetPosition2D;
                if (here.X == _sceneCheckedAt.X && here.Y == _sceneCheckedAt.Y)
                    return;
                _sceneCheckedAt = here;

                string resolved = HideoutScenes.Resolve(this, out HideoutTerrain terrain);
                if (string.IsNullOrEmpty(resolved))
                    return;
                if (resolved == this.SceneName)
                {
                    IMFDiagnostics.Verbose($"[Scene] {s.StringId} ({this.OwnerClan?.StringId}) stands on {terrain}: scene '{this.SceneName}' fits");
                    return;
                }
                IMFDiagnostics.Verbose($"[Scene] {s.StringId} ({this.OwnerClan?.StringId}) stands on {terrain}: scene '{this.SceneName}' does not fit, using '{resolved}'");
                this.SceneName = resolved;
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MinorFactionHideout.EnsureTerrainFittingScene failed:", e);
            }
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

        /// <summary>
        /// Reads the camp's scene and background from its settlements.xml row.
        /// </summary>
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
            get => this.OwnerClan?.IsNomad ?? false;
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

        // settlement positions aren't saved, LoadXmlHideoutsPatch puts this back
        [SaveableField(109)]
        private bool _hasSavedPosition;

        [SaveableField(110)]
        private float _savedPosX;

        [SaveableField(111)]
        private float _savedPosY;

        internal bool HasSavedPosition => _hasSavedPosition;

        internal TaleWorlds.Library.Vec2 SavedPosition => new TaleWorlds.Library.Vec2(_savedPosX, _savedPosY);

        /// <summary>
        /// Stores the camp's position in the save, the game doesn't save settlement positions.
        /// </summary>
        internal void RememberPosition(TaleWorlds.Library.Vec2 position)
        {
            _savedPosX = position.X;
            _savedPosY = position.Y;
            _hasSavedPosition = true;
        }
    }

    /// <summary>
    /// Why a camp is being closed.
    /// </summary>
    public enum DeactivationReason
    {
        Raid,
        NomadMovement
    }
}

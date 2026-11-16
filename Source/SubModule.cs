
using System;
using System.Linq;
using HarmonyLib;
using ImprovedMinorFactions.Patches;
using ImprovedMinorFactions.Source;
using ImprovedMinorFactions.Source.CampaignBehaviors;
using ImprovedMinorFactions.Source.Patches;
using ImprovedMinorFactions.Source.Quests.MFHLordNeedsRecruits;
using ImprovedMinorFactions.Source.Quests.MFHNotableNeedsRecruits;
using ImprovedMinorFactions.Source.Quests.MFHNotableNeedsTroopsTrained;
using ImprovedMinorFactions.Source.Quests.NearbyHideout;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;


namespace ImprovedMinorFactions
{

    /// <summary>
    /// Entry point of the mod. Applies the Harmony patches, adds the campaign behaviors and game models,
    /// and registers the camp and MFData types with the object manager.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (Harmony.HasAnyPatches("ImprovedMinorFactions")) return;
            Harmony harmony = new Harmony("ImprovedMinorFactions");
            harmony.PatchCategory(typeof(SubModule).Assembly, "HarmonyStaticFixes"); // run this before other patches

            harmony.PatchAllUncategorized(typeof(SubModule).Assembly);
        }

        /// <summary>
        /// Adds the mod's campaign behaviors and wraps the game's models with ours. Throws if a model it wraps is missing.
        /// </summary>
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            IMFTexts.InitIMFTexts();

            CampaignGameStarter? starter = gameStarterObject as CampaignGameStarter;
            if (starter == null)
            {
                return;
            }
            IMFManager.ConvertGangLeaderMFNotablesToPreachers();
            starter.AddBehavior(new MFHideoutCampaignBehavior());
            starter.AddBehavior(new MFHideoutVisitorCampaignBehavior());
            starter.AddBehavior(new MFHNotablesCampaignBehavior());
            starter.AddBehavior(new MFHCampCrowdCampaignBehavior());
            starter.AddBehavior(new MFHNotableNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new MFHNotableNeedsTroopsTrainedIssueBehavior());
            starter.AddBehavior(new MFHLordNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new NearbyMFHideoutIssueBehavior());
            starter.AddBehavior(new NomadMFsCampaignBehavior());
            starter.AddBehavior(new MFOutlawWarCampaignBehavior());
            starter.AddBehavior(new MFDescriptionDialogCampaignBehavior());
            starter.AddBehavior(new PlayAsMFBehavior());
            starter.AddBehavior(new MFProvisionBehavior());
            starter.AddBehavior(new MFPlayerClanPartiesBehavior());

            var clanFinanceModel = GetGameModel<ClanFinanceModel>(starter);
            if (clanFinanceModel is null)
                throw new Exception("No default ClanFinanceModel found!");

            var targetScoreModel = GetGameModel<TargetScoreCalculatingModel>(starter);
            if (targetScoreModel is null)
                throw new Exception("No default TargetScoreCalculatingModel found!");

            var encounterMenuModel = GetGameModel<EncounterGameMenuModel>(starter);
            if (encounterMenuModel is null)
                throw new Exception("No default EncounterGameMenuModel found!");

            var encounterModel = GetGameModel<EncounterModel>(starter);
            if (encounterModel is null)
                throw new Exception("No default EncounterModel found!");

            var banditDensityModel = GetGameModel<BanditDensityModel>(starter);
            if (banditDensityModel is null)
                throw new Exception("No default BanditDensityModel found!");

            var partySizeLimitModel = GetGameModel<PartySizeLimitModel>(starter);
            if (partySizeLimitModel is null)
                throw new Exception("No default PartySizeLimitModel found!");

            starter.AddModel(new IMFClanFinanceModel(clanFinanceModel));
            starter.AddModel(new IMFTargetScoreCalculatingModel(targetScoreModel));
            starter.AddModel(new IMFEncounterGameMenuModel(encounterMenuModel));
            starter.AddModel(new IMFEncounterModel(encounterModel));
            starter.AddModel(new IMFBanditDensityModel(banditDensityModel));
            starter.AddModel(new IMFPartySizeLimitModel(partySizeLimitModel));
        }
        /// <summary>
        /// Clears everything kept between campaigns: the manager, captured XML, generated camp ids, config, logs and placement data.
        /// </summary>
        public override void OnGameEnd(Game game)
        {
            IMFManager.ClearManager();
            ImprovedMinorFactions.Patches.LoadXmlHideoutsPatch.Registry.Clear();
            GeneratedHideouts.Clear();
            ImprovedMinorFactions.Source.CampaignBehaviors.HordeHelper.ClearCache();
            ImprovedMinorFactions.ConfigLoader.Reset();
            IMFDiagnostics.ResetLogOnce();
            HideoutPlacement.ForgetAnchors();
            ImprovedMinorFactions.Patches.SettlementVisualMFHideoutMapEntityPatch.ForgetModels();
        }

        /// <summary>
        /// Registers the camp component and MFData types and loads mf_data.xml for campaign games.
        /// </summary>
        public override void BeginGameStart(Game game)
        {
            base.BeginGameStart(game);
            if (game.GameType is Campaign || game.GameType.GetType().Name == "CampaignStoryMode")
            {
                game.ObjectManager.RegisterType<MinorFactionHideout>("MinorFactionHideout", "Components", 99U);
                game.ObjectManager.RegisterType<MFData>("MFData", "MFDatas", 100U);
                MBObjectManager.Instance.LoadXML("MFDatas", false);
            }
        }
        /// <summary>
        /// Warns if another mod replaced one of our models without calling into it, then checks camp positions.
        /// </summary>
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (Campaign.Current == null)
                return;
            

            ValidateGameModel(Campaign.Current.Models.ClanFinanceModel);
            ValidateGameModel(Campaign.Current.Models.TargetScoreCalculatingModel);
            ValidateGameModel(Campaign.Current.Models.EncounterGameMenuModel);
            ValidateGameModel(Campaign.Current.Models.EncounterModel);
            ValidateGameModel(Campaign.Current.Models.BanditDensityModel);
            ValidateGameModel(Campaign.Current.Models.PartySizeLimitModel);

            HideoutPlacement.ValidateAllHideouts();
        }

        /// <summary>
        /// Shows a load order warning if a model of ours was replaced by one that doesn't build on the previous model.
        /// </summary>
        private void ValidateGameModel(GameModel model)
        {
            if (model.GetType().Assembly == GetType().Assembly) { return; }
            if (!model.GetType().BaseType.IsAbstract)
            {
                TextObject error = new($"Game Model Error: {model.ToString()}, Please move " + GetType().Assembly.GetName().Name + " below " + model.GetType().Assembly.GetName().Name + " in your load order to ensure mod compatibility");
                InformationManager.DisplayMessage(new InformationMessage(error.ToString(), Colors.Red));
            }
        }

        /// <summary>
        /// Gets the last added model of a type, so we wrap whatever other mods put there.
        /// </summary>
        private T? GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
        {
            var models = gameStarterObject.Models.ToArray();

            for (int index = models.Length - 1; index >= 0; --index)
            {
                if (models[index] is T gameModel1)
                    return gameModel1;
            }
            return default;
        }
    }
}
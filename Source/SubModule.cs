
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
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;


namespace ImprovedMinorFactions
{

    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (Harmony.HasAnyPatches("ImprovedMinorFactions")) return;
            var assembly = typeof(SubModule).Assembly;

            Harmony harmony = new Harmony("ImprovedMinorFactions");
            harmony.PatchCategory(assembly, "HarmonyStaticFixes"); // run this before other patches
            //if (Harmony.HasAnyPatches("BannerKings"))
                //harmony.PatchCategory(assembly, "BannerKingsPatches");

            harmony.PatchAllUncategorized(assembly);        
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            CampaignGameStarter? starter = gameStarterObject as CampaignGameStarter;
            if (starter == null)
            {
                return;
            }
            starter.AddBehavior(new MFHideoutCampaignBehavior());
            starter.AddBehavior(new MFHNotablesCampaignBehavior());
            starter.AddBehavior(new MFHNotableNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new MFHNotableNeedsTroopsTrainedIssueBehavior());
            starter.AddBehavior(new MFHLordNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new NearbyMFHideoutIssueBehavior());
            starter.AddBehavior(new NomadMFsCampaignBehavior());

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

            starter.AddModel(new IMFClanFinanceModel(clanFinanceModel));
            starter.AddModel(new IMFTargetScoreCalculatingModel(targetScoreModel));
            starter.AddModel(new IMFEncounterGameMenuModel(encounterMenuModel));
            starter.AddModel(new IMFEncounterModel(encounterModel));
            starter.AddModel(new IMFBanditDensityModel(banditDensityModel));
        }
        public override void OnGameEnd(Game game)
        {
            IMFManager.ClearManager();
        }

        public override void BeginGameStart(Game game)
        {
            base.BeginGameStart(game);
            if (game.GameType is Campaign || game.GameType is CampaignStoryMode)
            {
                game.ObjectManager.RegisterType<MinorFactionHideout>("MinorFactionHideout", "Components", 99U);
                game.ObjectManager.RegisterType<MFData>("MFData", "MFDatas", 100U);
                MBObjectManager.Instance.LoadXML("MFDatas", false);
            }
        }
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
        }

        private void ValidateGameModel(GameModel model)
        {
            if (model.GetType().Assembly == GetType().Assembly) { return; }
            if (!model.GetType().BaseType.IsAbstract)
            {
                TextObject error = new($"Game Model Error: {model.ToString()}, Please move " + GetType().Assembly.GetName().Name + " below " + model.GetType().Assembly.GetName().Name + " in your load order to ensure mod compatibility");
                InformationManager.DisplayMessage(new InformationMessage(error.ToString(), Colors.Red));
            }
        }

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
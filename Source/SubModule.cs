
using HarmonyLib;
using ImprovedMinorFactions.Source;
using ImprovedMinorFactions.Source.Quests.MFHLordNeedsRecruits;
using ImprovedMinorFactions.Source.Quests.MFHNotableNeedsRecruits;
using ImprovedMinorFactions.Source.Quests.MFHNotableNeedsTroopsTrained;
using ImprovedMinorFactions.Source.Quests.NearbyHideout;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;


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
            harmony.PatchAllUncategorized(assembly);        }

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
            starter.AddBehavior(new MFHideoutCampaignBehavior());
            starter.AddBehavior(new MFHNotablesCampaignBehavior());
            starter.AddBehavior(new MFHNotableNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new MFHNotableNeedsTroopsTrainedIssueBehavior());
            starter.AddBehavior(new MFHLordNeedsRecruitsIssueBehavior());
            starter.AddBehavior(new NearbyMFHideoutIssueBehavior());
        }

        public override void OnGameEnd(Game game)
        {
            MFHideoutManager.clearManager();
        }

        public override void BeginGameStart(Game game)
        {
            base.BeginGameStart(game);
            if (game.GameType is Campaign || game.GameType is CampaignStoryMode)
            {
                game.ObjectManager.RegisterType<MinorFactionHideout>("MinorFactionHideout", "Components", 99U, true);
            }
        }
    }
}
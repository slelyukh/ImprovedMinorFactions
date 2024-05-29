using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;

//namespace ImprovedMinorFactions.Source.Patches
//{
//    internal static class BannerKingPatchClasses
//    {
//        public static Type BKClanBehavior
//        {
//            get => Type.GetType("BannerKings.Behaviors.BKClanBehavior");
//        }
//    }


//    [HarmonyPatchCategory("BannerKingsPatches")]
//    [HarmonyPatch(BannerKingPatchClasses.BKClanBehavior)]
//    internal class SomePatch
//    {
//        [HarmonyPatch(nameof(GameTexts.FindText))]
//        [HarmonyPrefix]
//        private static void FindTextPrefix()
//        {
//            if (get_GameTextManager.GetValue(null) == null)
//            {
//                var gameTextManager = new GameTextManager();
//                gameTextManager.LoadGameTexts();
//                GameTexts.Initialize(gameTextManager);
//            }
//        }
//    }
//}

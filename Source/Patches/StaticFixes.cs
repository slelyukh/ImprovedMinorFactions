using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;

namespace ImprovedMinorFactions.Patches
{
    // taken from Bannerlord Co-op mod to prevent gametext crashes with some patches
    [HarmonyPatchCategory("HarmonyStaticFixes")]
    [HarmonyPatch(typeof(GameTexts))]
    internal class GameTextsPatches
    {
        private static readonly FieldInfo get_GameTextManager = typeof(GameTexts)!
            .GetField("_gameTextManager", BindingFlags.Static | BindingFlags.NonPublic)!;

        [HarmonyPatch(nameof(GameTexts.FindText))]
        [HarmonyPrefix]
        private static void FindTextPrefix()
        {
            if (get_GameTextManager.GetValue(null) == null)
            {
                var gameTextManager = new GameTextManager();
                gameTextManager.LoadGameTexts();
                GameTexts.Initialize(gameTextManager);
            }
        }
    }
}

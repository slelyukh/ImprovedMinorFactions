using HarmonyLib;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Patches
{
    // Brings back camp talk options other mods hid, their own conditions still apply.
    [HarmonyPatch(typeof(GameMenuOption), "GetConditionsHold")]
    public class MFHideoutTalkEnablePatch
    {
        static void Postfix(GameMenuOption __instance, TaleWorlds.CampaignSystem.GameState.MenuContext menuContext, ref bool __result)
        {
            try
            {
                if (__result || __instance.OnCondition == null)
                    return;
                if (!(Settlement.CurrentSettlement?.SettlementComponent is MinorFactionHideout))
                    return;
                var args = new MenuCallbackArgs(menuContext, __instance.Text);
                if (!__instance.OnCondition(args) || args.optionLeaveType != GameMenuOption.LeaveType.Conversation)
                    return;
                __instance.SetEnable(true);
                __result = true;
            }
            catch
            {
                // Another mod's condition threw, leave the option how the game had it.
            }
        }
    }
}

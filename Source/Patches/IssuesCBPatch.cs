using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using MathF = TaleWorlds.Library.MathF;
using TaleWorlds.CampaignSystem.Settlements;

namespace ImprovedMinorFactions.Source.Patches
{
    // this should allow mfHideout notables to have quests
    [HarmonyPatch(typeof(IssuesCampaignBehavior), "OnSessionLaunched")]
    public class NeedsSpecialWeaponsPatch
    {
        static void Postfix(IssuesCampaignBehavior __instance, ref Settlement[] ____settlements)
        {
            var newSettlementsList = ____settlements.ToList();
            newSettlementsList.AppendList(
                Enumerable.ToList(Enumerable.Where<Settlement>(
                    Settlement.All,
                    (Settlement x) => Helpers.isMFHideout(x))));
            Helpers.setPrivateField(__instance, "_settlements", newSettlementsList.ToArray());
        }
    }

    [HarmonyPatch(typeof(IssuesCampaignBehavior), "DailyTickClan")]
    public class DailyTickClanPatch
    {
        static void Postfix(IssuesCampaignBehavior __instance, Clan clan)
        {
            if (clan.Heroes.Count == 0 || !clan.IsMinorFaction || clan.IsBanditFaction)
                return;
            int numFreeIssues = Enumerable.Count(
                Campaign.Current.IssueManager.Issues,
                (KeyValuePair<Hero, IssueBase> x) => !x.Value.IsTriedToSolveBefore);
            int numOccupiedLords = Enumerable.Count<Hero>(
                clan.Heroes, (Hero x) => x.Issue != null);
            int numLords = Enumerable.Count<Hero>(
                clan.Heroes, (Hero x) => x.IsAlive && !x.IsChild && x.IsLord);
            int maxIssues = MathF.Ceiling((float)numLords * 0.1f);
            int minIssues = MathF.Floor((float)numLords * 0.25f);
            float issueGenerationChance = MathF.Pow((1f - ((float)numOccupiedLords / (float)minIssues)), 2f) * 0.3f;
            if (minIssues > 0 && 
                numOccupiedLords < minIssues && 
                (numOccupiedLords < maxIssues || MBRandom.RandomFloat < issueGenerationChance))
            {
                Helpers.callPrivateMethod(__instance, "CreateAnIssueForClanNobles", new object[] { clan, numFreeIssues + 1 });
            }
        }
    }
}

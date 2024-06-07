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
    // allows MFHideout notables to have quests by adding MFHideouts to list of settlements with notables
    [HarmonyPatch(typeof(IssuesCampaignBehavior), "OnSessionLaunched")]
    public class NeedsSpecialWeaponsPatch
    {
        static void Postfix(IssuesCampaignBehavior __instance, ref Settlement[] ____settlements)
        {
            var newSettlementsList = ____settlements.ToList();
            newSettlementsList.AppendList(
                Enumerable.ToList(Enumerable.Where<Settlement>(
                    Settlement.All,
                    (Settlement x) => Helpers.IsMFHideout(x))));
            Helpers.setPrivateField(__instance, "_settlements", newSettlementsList.ToArray());
        }
    }

    // mostly copypasta that allows MF lords to have quests by raising the maximum amount of quests in a clan to 25% instead of 20%
    // because MF Clans only have 4 members
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

            // the constant is 0.2f in the original method
            // TODO: change to 0.25
            int minIssues = MathF.Floor((float)numLords * 0.9f);
            float issueGenerationChance = MathF.Pow((1f - ((float)numOccupiedLords / (float)minIssues)), 2f) * 0.3f;
            if (minIssues > 0 
                && numOccupiedLords < minIssues 
                && (numOccupiedLords < maxIssues || MBRandom.RandomFloat < issueGenerationChance))
            {
                Helpers.CallPrivateMethod(__instance, "CreateAnIssueForClanNobles", new object[] { clan, numFreeIssues + 1 });
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    internal class MFHideoutVisitorCampaignBehavior : CampaignBehaviorBase
    {
        private const float EnterDistanceSquared = 9f;
        private const int MaxRefugeVisitorsPerClan = 2;

        internal static MFHideoutVisitorCampaignBehavior? Current { get; private set; }

        private Dictionary<MobileParty, VisitInfo> _visitors = new Dictionary<MobileParty, VisitInfo>();
        private Dictionary<Clan, CampaignTime> _nextVisitTimes = new Dictionary<Clan, CampaignTime>();

        public override void RegisterEvents()
        {
            Current = this;
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTick));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(OnHourlyTick));
            // the game walks the party in by itself, recruit when it gets there
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(OnSettlementEntered));
        }

        private void OnDailyTick()
        {
            try
            {
                SendNewVisitors();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFHideoutVisitorCampaignBehavior daily tick failed:", e);
            }
        }

        private void OnHourlyTick()
        {
            try
            {
                UpdateCurrentVisitors();
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFHideoutVisitorCampaignBehavior hourly tick failed:", e);
            }
        }

        private void OnSettlementEntered(MobileParty? party, Settlement? settlement, Hero? hero)
        {
            try
            {
                if (party == null || settlement == null)
                    return;
                if (!_visitors.TryGetValue(party, out VisitInfo? info) || info.Arrived || info.TargetSettlement != settlement)
                    return;
                Arrive(party, info, settlement);
            }
            catch (Exception e)
            {
                IMFDiagnostics.Log("MFHideoutVisitorCampaignBehavior arrival failed:", e);
            }
        }

        private void Arrive(MobileParty visitor, VisitInfo info, Settlement hideout)
        {
            info.Arrived = true;
            info.VisitStart = CampaignTime.Now;
            int taken = info.Kind == VisitKind.Recruit ? RecruitTroopsFromHideout(visitor, hideout) : 0;
            IMFDiagnostics.Verbose($"[Visit] {visitor.StringId} ({visitor.LeaderHero?.Name}) reached {hideout.StringId}: " +
                                (info.Kind == VisitKind.Recruit ? $"recruited {taken} from the camp" : info.Kind == VisitKind.Hide ? "sheltering" : "staying a while") +
                                $", planned stay {info.PlannedStayHours:F0}h");
        }

        private void UpdateCurrentVisitors()
        {
            if (_visitors.Count == 0)
                return;

            foreach (KeyValuePair<MobileParty, VisitInfo> entry in _visitors.ToList())
            {
                MobileParty visitor = entry.Key;
                VisitInfo info = entry.Value;

                if (visitor == null || !visitor.IsActive || visitor.MapEvent != null || visitor.IsDisbanding)
                {
                    End(visitor, info, leave: false, "the party is gone or fighting");
                    continue;
                }

                MinorFactionHideout? target = Helpers.GetMFHideout(info.TargetSettlement);
                if (target == null || !target.IsActive)
                {
                    End(visitor, info, leave: visitor.CurrentSettlement == info.TargetSettlement, "the camp is gone");
                    continue;
                }

                Settlement targetSettlement = target.Settlement;
                double sinceStart = (CampaignTime.Now - info.VisitStart).ToHours;

                if (visitor.CurrentSettlement == targetSettlement)
                {
                    if (!info.Arrived)
                        Arrive(visitor, info, targetSettlement);   // we missed the enter event (happens after a load)
                    else if (visitor.GetNumDaysForFoodToLast() < LeaveWhenFoodDays)
                        End(visitor, info, leave: true, "low on food, the camp has no market");
                    else if (ReadyToLeave(info.Kind, sinceStart, info.PlannedStayHours,
                                 visitor.Party.NumberOfWoundedTotalMembers, visitor.Party.NumberOfAllMembers, visitor.Morale))
                        End(visitor, info, leave: true, "the visit is over");
                    continue;
                }

                if (visitor.CurrentSettlement != null)
                {
                    End(visitor, info, leave: false, "the party went into another settlement");
                    continue;
                }
                // the food check before leaving assumed the planned trip, a detour can still eat through it
                if (visitor.Party.IsStarving || visitor.GetNumDaysForFoodToLast() < LeaveWhenFoodDays)
                {
                    End(visitor, info, leave: false, "running out of food on the way");
                    continue;
                }
                if (sinceStart > info.TravelBudgetHours)
                {
                    End(visitor, info, leave: false, "the trip took too long");
                    continue;
                }
                double distance = visitor.GetPosition2D.Distance(targetSettlement.GetPosition2D);
                if (distance < info.BestDistance - ProgressStep)
                {
                    info.BestDistance = distance;
                    info.LastProgress = CampaignTime.Now;
                }
                else if ((CampaignTime.Now - info.LastProgress).ToHours > StuckHours)
                {
                    End(visitor, info, leave: false, "stopped getting closer to the camp");
                    continue;
                }
                if (visitor.GetPosition2D.DistanceSquared(targetSettlement.GetPosition2D) < EnterDistanceSquared)
                {
                    EnterSettlementAction.ApplyForParty(visitor, targetSettlement);
                    if (!info.Arrived)
                        Arrive(visitor, info, targetSettlement);
                }
            }
        }

        private void End(MobileParty? visitor, VisitInfo info, bool leave, string why)
        {
            if (visitor != null)
            {
                _visitors.Remove(visitor);
                Release(visitor, leave);
                IMFDiagnostics.Verbose($"[Visit] {visitor.StringId} ({visitor.LeaderHero?.Name}) done with {info.TargetSettlement?.StringId}: {why}");
            }
        }

        private static void Release(MobileParty visitor, bool leave)
        {
            visitor.Ai.SetDoNotMakeNewDecisions(false);
            visitor.Ai.RethinkAtNextHourlyTick = true;
            if (leave && visitor.CurrentSettlement != null && visitor.MapEvent == null)
                LeaveSettlementAction.ApplyForParty(visitor);
            visitor.RecalculateShortTermBehavior();
        }

        private void SendNewVisitors()
        {
            if (IMFManager.Current == null)
                return;

            foreach (Clan clan in Clan.All.ToList())
            {
                if (clan == null || !clan.IsMinorFaction || clan.IsEliminated || clan.IsRebelClan)
                    continue;

                List<MinorFactionHideout> hideouts = IMFManager.Current.GetActiveHideoutsOfClan(clan);
                if (hideouts.Count == 0)
                    continue;

                List<MobileParty> free = FreePartiesOf(clan);
                if (free.Count == 0)
                    continue;

                int sheltering = _visitors.Count(v => v.Value.Kind == VisitKind.Hide && ClanOf(v.Key) == clan);
                foreach (MobileParty party in free)
                {
                    if (sheltering >= MaxRefugeVisitorsPerClan)
                        break;
                    // a starving party needs a market not a camp, vanilla AI handles that
                    if (party.Party.IsStarving
                        || !NeedsRefuge(party.Party.NumberOfWoundedTotalMembers, party.Party.NumberOfAllMembers, false, party.Morale))
                        continue;
                    MinorFactionHideout? camp = NearestCampInReach(party, hideouts, VisitKind.Hide);
                    if (camp == null || !TrySendPartyToHideout(party, camp.Settlement, VisitKind.Hide))
                        continue;
                    sheltering++;
                }

                if (_visitors.Any(v => v.Value.Kind != VisitKind.Hide && ClanOf(v.Key) == clan))
                    continue;
                if (!_nextVisitTimes.TryGetValue(clan, out CampaignTime nextTime))
                {
                    _nextVisitTimes[clan] = CampaignTime.Now + CampaignTime.Days((float)RandomBetween(InitialDelayMinDays, InitialDelayMaxDays));
                    continue;
                }
                if (CampaignTime.Now < nextTime)
                    continue;

                free = free.Where(p => !_visitors.ContainsKey(p)).ToList();
                if (!TrySendScheduledVisit(clan, free, hideouts))
                    _nextVisitTimes[clan] = CampaignTime.Now + CampaignTime.Days(1f);   // nobody can reach a camp today, try again tomorrow
            }
        }

        private bool TrySendScheduledVisit(Clan clan, List<MobileParty> free, List<MinorFactionHideout> hideouts)
        {
            IEnumerable<MobileParty> ordered = free.OrderBy(p => p.LeaderHero == clan.Leader ? 0 : 1).ThenBy(_ => MBRandom.RandomInt(1000));
            foreach (MobileParty party in ordered)
            {
                var reachable = hideouts.Where(h => WithinReach(VisitKind.Hang, DistanceTo(party, h)) && SameLand(party, h)).ToList();
                if (reachable.Count == 0)
                    continue;
                MinorFactionHideout camp = reachable[MBRandom.RandomInt(reachable.Count)];
                if (!TrySendPartyToHideout(party, camp.Settlement, ScheduledKind(MBRandom.RandomFloat)))
                    continue;
                _nextVisitTimes[clan] = CampaignTime.Now + CampaignTime.Days((float)RandomBetween(CooldownMinDays, CooldownMaxDays));
                return true;
            }
            return false;
        }

        private static double RandomBetween(double min, double max) => min + MBRandom.RandomFloat * (max - min);

        private static Clan? ClanOf(MobileParty party) => party.ActualClan ?? party.LeaderHero?.Clan;

        private static double DistanceTo(MobileParty party, MinorFactionHideout camp) =>
            party.GetPosition2D.Distance(camp.Settlement.GetPosition2D);

        private static bool SameLand(MobileParty party, MinorFactionHideout camp) =>
            HideoutPlacement.SameLandAs(party.Position, camp.Settlement.Position);

        private static MinorFactionHideout? NearestCampInReach(MobileParty party, List<MinorFactionHideout> camps, VisitKind kind)
        {
            var usable = camps.Where(c => SameLand(party, c)).ToList();
            int at = NearestWithinReach(kind, usable.Select(c => DistanceTo(party, c)).ToArray());
            return at < 0 ? null : usable[at];
        }

        private List<MobileParty> FreePartiesOf(Clan clan)
        {
            var found = new List<MobileParty>();
            void Add(MobileParty? p)
            {
                if (p != null && !found.Contains(p) && IsFree(p))
                    found.Add(p);
            }

            foreach (WarPartyComponent component in clan.WarPartyComponents)
                Add(component.MobileParty);
            Add(clan.Leader?.PartyBelongedTo);
            foreach (Hero hero in clan.Heroes)
                if (hero != clan.Leader)
                    Add(hero.PartyBelongedTo);
            return found;
        }

        private bool IsFree(MobileParty p)
        {
            if (!p.IsActive || p.IsMainParty || p.IsDisbanding || p.IsCurrentlyAtSea)
                return false;
            if (p.MapEvent != null || p.SiegeEvent != null || p.BesiegedSettlement != null || p.Army != null || p.AttachedTo != null)
                return false;
            if (p.CurrentSettlement != null || p.LeaderHero == null || p.MemberRoster.TotalManCount == 0)
                return false;
            if (p.Ai.DoNotMakeNewDecisions || p.Ai.IsDisabled)
                return false;
            return !_visitors.ContainsKey(p);
        }

        // camps have no market, so only go with food for the trip and the stay
        private bool TrySendPartyToHideout(MobileParty party, Settlement targetSettlement, VisitKind kind)
        {
            double distance = party.GetPosition2D.Distance(targetSettlement.GetPosition2D);
            double travel = PlanTravelHours(distance, party.Speed);
            double stay = PlanStayHours(kind, MBRandom.RandomFloat);
            if (!HasFoodFor(party.GetNumDaysForFoodToLast(), travel + stay))
                return false;
            party.Ai.SetDoNotMakeNewDecisions(true);
            try
            {
                party.SetMoveGoToSettlement(targetSettlement, party.NavigationCapability, false);
                party.RecalculateShortTermBehavior();
            }
            catch (Exception e)
            {
                // not tracked yet, so nothing else would ever unlock its AI
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.Ai.RethinkAtNextHourlyTick = true;
                IMFDiagnostics.Log($"sending {party.StringId} to {targetSettlement.StringId} failed:", e);
                return false;
            }
            _visitors[party] = new VisitInfo(CampaignTime.Now, targetSettlement, kind, stay, travel);
            IMFDiagnostics.Verbose($"[Visit] {party.StringId} ({party.LeaderHero?.Name}, {ClanOf(party)?.StringId}) heads for {targetSettlement.StringId} " +
                                $"({kind}, {distance:F0} units)");
            return true;
        }

        private int RecruitTroopsFromHideout(MobileParty visitor, Settlement hideout)
        {
            MilitiaPartyComponent? militiaComponent = hideout.MilitiaPartyComponent;
            if (militiaComponent == null)
                return 0;
            PartyBase militiaParty = militiaComponent.Party;
            if (militiaParty == null)
                return 0;
            TroopRoster militiaRoster = militiaParty.MemberRoster;
            if (militiaRoster == null || militiaRoster.TotalManCount < 3)
                return 0;

            CharacterObject? troop = null;
            foreach (TroopRosterElement element in militiaRoster.GetTroopRoster())
            {
                if (element.Character != null && element.Number > 0)
                {
                    troop = element.Character;
                    break;
                }
            }
            if (troop == null)
                return 0;

            int requested = MBRandom.RandomInt(1, 4);
            int available = militiaRoster.GetTroopCount(troop);
            int toTransfer = Math.Min(Math.Min(requested, available), militiaRoster.TotalManCount / 2);
            if (toTransfer <= 0)
                return 0;

            militiaRoster.AddToCounts(troop, -toTransfer);
            visitor.MemberRoster.AddToCounts(troop, toTransfer);
            militiaRoster.RemoveZeroCounts();
            return toTransfer;
        }

        internal string ForceVisit(string? clanId)
        {
            if (IMFManager.Current == null)
                return "IMF is not running in this campaign.";
            var sb = new StringBuilder();
            foreach (Clan clan in Clan.All.ToList())
            {
                if (clan == null || !clan.IsMinorFaction || clan.IsEliminated)
                    continue;
                if (!string.IsNullOrEmpty(clanId) && !string.Equals(clan.StringId, clanId, StringComparison.OrdinalIgnoreCase))
                    continue;
                List<MinorFactionHideout> hideouts = IMFManager.Current.GetActiveHideoutsOfClan(clan);
                if (hideouts.Count == 0)
                {
                    if (!string.IsNullOrEmpty(clanId)) sb.AppendLine($"{clan.StringId}: no active hideout.");
                    continue;
                }
                List<MobileParty> free = FreePartiesOf(clan);
                if (free.Count == 0)
                {
                    if (!string.IsNullOrEmpty(clanId)) sb.AppendLine($"{clan.StringId}: no free party.");
                    continue;
                }
                bool sent = TrySendScheduledVisit(clan, free, hideouts);
                if (sent || !string.IsNullOrEmpty(clanId))
                    sb.AppendLine($"{clan.StringId}: {(sent ? "visit sent" : "no party can reach a camp (too far or another landmass)")}");
            }
            return sb.Length == 0 ? "No minor faction could send a visit." : sb.ToString();
        }

        internal string Describe()
        {
            if (_visitors.Count == 0)
                return "No parties are on their way to a camp.";
            var sb = new StringBuilder();
            foreach (KeyValuePair<MobileParty, VisitInfo> v in _visitors)
            {
                double hours = (CampaignTime.Now - v.Value.VisitStart).ToHours;
                sb.AppendLine($"{v.Key.StringId} ({v.Key.LeaderHero?.Name}) -> {v.Value.TargetSettlement?.StringId}: {v.Value.Kind}, " +
                              (v.Value.Arrived ? $"inside {hours:F0}h of {v.Value.PlannedStayHours:F0}h" : $"travelling {hours:F0}h, about {v.Value.TravelBudgetHours:F0}h expected"));
            }
            return sb.ToString();
        }

        // Saved as parallel lists, the save can't take a dictionary of a private type.
        public override void SyncData(IDataStore dataStore)
        {
            List<MobileParty>? parties = null;
            List<Settlement>? targets = null;
            List<CampaignTime>? starts = null;
            List<bool>? arrived = null;
            List<int>? kinds = null;
            List<double>? stays = null;
            List<double>? budgets = null;
            if (dataStore.IsSaving)
            {
                List<KeyValuePair<MobileParty, VisitInfo>> live = _visitors.Where(e => e.Key != null && e.Value.TargetSettlement != null).ToList();
                parties = live.Select(e => e.Key).ToList();
                targets = live.Select(e => e.Value.TargetSettlement).ToList();
                starts = live.Select(e => e.Value.VisitStart).ToList();
                arrived = live.Select(e => e.Value.Arrived).ToList();
                kinds = live.Select(e => (int)e.Value.Kind).ToList();
                stays = live.Select(e => e.Value.PlannedStayHours).ToList();
                budgets = live.Select(e => e.Value.TravelBudgetHours).ToList();
            }
            dataStore.SyncData("_mfhvParties", ref parties);
            dataStore.SyncData("_mfhvTargets", ref targets);
            dataStore.SyncData("_mfhvStarts", ref starts);
            dataStore.SyncData("_mfhvArrived", ref arrived);
            dataStore.SyncData("_mfhvKinds", ref kinds);
            dataStore.SyncData("_mfhvStays", ref stays);
            dataStore.SyncData("_mfhvBudgets", ref budgets);
            dataStore.SyncData("_mfhvisitorNextTimes", ref _nextVisitTimes);

            if (dataStore.IsLoading)
            {
                _visitors = new Dictionary<MobileParty, VisitInfo>();
                _nextVisitTimes ??= new Dictionary<Clan, CampaignTime>();
                if (parties != null && targets != null && starts != null && arrived != null && kinds != null && stays != null && budgets != null)
                {
                    int n = new[] { parties.Count, targets.Count, starts.Count, arrived.Count, kinds.Count, stays.Count, budgets.Count }.Min();
                    for (int i = 0; i < n; i++)
                    {
                        if (parties[i] == null || targets[i] == null)
                            continue;
                        var kind = Enum.IsDefined(typeof(VisitKind), kinds[i]) ? (VisitKind)kinds[i] : VisitKind.Hang;
                        _visitors[parties[i]] = new VisitInfo(starts[i], targets[i], kind, stays[i], budgets[i]) { Arrived = arrived[i] };
                    }
                }
            }
        }

        private sealed class VisitInfo
        {
            public CampaignTime VisitStart;
            public readonly Settlement TargetSettlement;
            public bool Arrived;
            public readonly VisitKind Kind;
            public readonly double PlannedStayHours;
            public readonly double TravelBudgetHours;
            public double BestDistance = double.MaxValue;
            public CampaignTime LastProgress = CampaignTime.Now;

            public VisitInfo(CampaignTime visitStart, Settlement targetSettlement, VisitKind kind, double plannedStayHours, double travelBudgetHours)
            {
                VisitStart = visitStart;
                TargetSettlement = targetSettlement;
                Kind = kind;
                PlannedStayHours = plannedStayHours;
                TravelBudgetHours = travelBudgetHours;
            }
        }

        internal const double RecruitChance = 0.6;

        internal const double HideWoundedShare = 0.30;
        internal const float HideMorale = 35f;
        internal const double RecoveredWoundedShare = 0.10;
        internal const float RecoveredMorale = 50f;

        internal const double MinStayHours = 12;
        internal const double MaxStayHours = 96;

        // map units, one is about 15 minutes of walking
        internal const double MaxVisitDistance = 140;
        internal const double MaxRefugeDistance = 90;

        internal const double StuckHours = 24;
        internal const double ProgressStep = 0.5;

        internal const double LeaveWhenFoodDays = 1;

        internal const double InitialDelayMinDays = 1;
        internal const double InitialDelayMaxDays = 4;
        internal const double CooldownMinDays = 5;
        internal const double CooldownMaxDays = 10;

        internal static bool NeedsRefuge(int wounded, int total, bool starving, float morale)
        {
            if (total <= 0)
                return false;
            return starving || wounded >= HideWoundedShare * total || morale < HideMorale;
        }

        internal static bool Recovered(int wounded, int total, float morale)
        {
            return total <= 0 || (wounded <= RecoveredWoundedShare * total && morale >= RecoveredMorale);
        }

        internal static VisitKind ScheduledKind(double roll01)
        {
            return roll01 < RecruitChance ? VisitKind.Recruit : VisitKind.Hang;
        }

        internal static double PlanStayHours(VisitKind kind, double roll01)
        {
            double r = Math.Min(Math.Max(roll01, 0.0), 1.0);
            return kind == VisitKind.Hide ? 48 + r * 48 : 24 + r * 48;
        }

        internal static bool ReadyToLeave(VisitKind kind, double stayedHours, double plannedHours, int wounded, int total, float morale)
        {
            if (stayedHours < MinStayHours)
                return false;
            if (stayedHours >= MaxStayHours)
                return true;
            if (stayedHours < plannedHours)
                return false;
            return kind != VisitKind.Hide || Recovered(wounded, total, morale);
        }

        internal static bool HasFoodFor(double foodDays, double tripHours)
        {
            return foodDays * 24 >= tripHours + LeaveWhenFoodDays * 24;
        }

        internal static double PlanTravelHours(double distance, double speedUnitsPerHour)
        {
            double speed = Math.Max(0.5, speedUnitsPerHour);
            return Math.Min(120, Math.Max(24, distance / speed * 1.8 + 12));
        }

        internal static bool WithinReach(VisitKind kind, double distance)
        {
            return distance >= 0 && distance <= (kind == VisitKind.Hide ? MaxRefugeDistance : MaxVisitDistance);
        }

        internal static int NearestWithinReach(VisitKind kind, double[] distances)
        {
            int best = -1;
            if (distances == null)
                return best;
            for (int i = 0; i < distances.Length; i++)
                if (WithinReach(kind, distances[i]) && (best < 0 || distances[i] < distances[best]))
                    best = i;
            return best;
        }
    }

    internal static class ImfConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("visit_now", "imf")]
        public static string VisitNow(List<string> args)
        {
            MFHideoutVisitorCampaignBehavior? behavior = MFHideoutVisitorCampaignBehavior.Current;
            return behavior == null ? "No campaign is running." : behavior.ForceVisit(args != null && args.Count > 0 ? args[0] : null);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("visits", "imf")]
        public static string Visits(List<string> args)
        {
            MFHideoutVisitorCampaignBehavior? behavior = MFHideoutVisitorCampaignBehavior.Current;
            return behavior == null ? "No campaign is running." : behavior.Describe();
        }
    }

    internal enum VisitKind
    {
        Hang = 0,      // just stay a while
        Recruit = 1,   // take some of the camp's men
        Hide = 2       // hurt, starving or broken, stays until it has recovered
    }

}

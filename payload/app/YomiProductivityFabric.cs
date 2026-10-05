using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yomi.ProductShell
{
    internal sealed class ProductivityMission
    {
        public string Id;
        public string ParentId;
        public string RootId;
        public string Title;
        public string Objective;
        public string Status;
        public int Priority;
        public string ResearchCaseId;
        public string CreatedUtc;
        public string UpdatedUtc;
        public string LastOpenedUtc;
        public string ResumeSummary;
        public string ResumeNext;
        public string ResumeSavedUtc;
        public List<string> Tags = new List<string>();
        public List<string> EvidencePaths = new List<string>();
        public List<string> Notes = new List<string>();
        public override string ToString()
        {
            return (Status ?? "ACTIVE") + "   P" + Priority.ToString(CultureInfo.InvariantCulture) + "   " + (Title ?? Id ?? "mission");
        }
    }

    internal sealed class ProductivityTask
    {
        public string Id;
        public string MissionId;
        public string ParentTaskId;
        public string Title;
        public string Detail;
        public string Kind;
        public string Status;
        public int Priority;
        public int Value;
        public int Effort;
        public string ResearchCaseId;
        public string CreatedUtc;
        public string UpdatedUtc;
        public string CompletedUtc;
        public List<string> DependsOn = new List<string>();
        public List<string> EvidencePaths = new List<string>();
        public List<string> Notes = new List<string>();
        public override string ToString()
        {
            return (Status ?? "TODO") + "   P" + Priority.ToString(CultureInfo.InvariantCulture) + " V" + Value.ToString(CultureInfo.InvariantCulture) + " E" + Effort.ToString(CultureInfo.InvariantCulture) + "   " + (Title ?? Id ?? "task");
        }
    }

    internal sealed class ProductivityCapture
    {
        public string Id;
        public string Kind;
        public string Text;
        public string CreatedUtc;
        public string MissionId;
        public bool Processed;
        public string ProcessedUtc;
        public override string ToString()
        {
            return (Processed ? "DONE" : "INBOX") + "   " + (Kind ?? "THOUGHT") + "   " + (Text ?? "");
        }
    }

    internal sealed class ProductivityFocusSession
    {
        public string Id;
        public string MissionId;
        public string TaskId;
        public string Status;
        public string StartedUtc;
        public string LastHeartbeatUtc;
        public string EndedUtc;
        public long AccumulatedSeconds;
        public string ExitNote;
    }

    internal sealed class ProductivityLedgerRecord
    {
        public long Seq;
        public string Utc;
        public string Action;
        public string ObjectType;
        public string ObjectId;
        public string Detail;
        public string PrevHash;
        public string EntryHash;
    }

    internal sealed class ProductivityStore
    {
        public int Schema = 1;
        public string CurrentMissionId;
        public string CurrentTaskId;
        public List<ProductivityMission> Missions = new List<ProductivityMission>();
        public List<ProductivityTask> Tasks = new List<ProductivityTask>();
        public List<ProductivityCapture> Captures = new List<ProductivityCapture>();
        public List<ProductivityFocusSession> FocusSessions = new List<ProductivityFocusSession>();
    }

    internal sealed class ProductivityRecommendation
    {
        public ProductivityTask Task;
        public ProductivityMission Mission;
        public int Score;
        public int Unlocks;
        public bool Executable;
        public string Reason;
        public string BlockReason;
        public override string ToString()
        {
            string prefix = Executable ? "READY" : "BLOCKED";
            return prefix + "   " + Score.ToString("0000", CultureInfo.InvariantCulture) + "   U" + Unlocks.ToString(CultureInfo.InvariantCulture) + "   " + (Mission == null ? "" : Mission.Title + " › ") + (Task == null ? "" : Task.Title);
        }
    }

    internal static class ProductivityFabric
    {
        private static readonly Brush Bg = B("#080B0F");
        private static readonly Brush Surface = B("#111821");
        private static readonly Brush Raised = B("#192431");
        private static readonly Brush Border = B("#334557");
        private static readonly Brush Text = B("#F1F6F9");
        private static readonly Brush Muted = B("#9FAFBC");
        private static readonly Brush Accent = B("#69ECB6");
        private static readonly Brush Blue = B("#6FB9FF");
        private static readonly Brush Amber = B("#FFD36E");
        private static readonly Brush Violet = B("#C6A0FF");
        private static readonly Brush Danger = B("#FF7D89");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();
        private static DispatcherTimer FocusPulse;
        private static DeepSystemsContext FocusContext;
        private static Window FocusWindow;
        private static TextBlock FocusClock;

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            ProductivityStore store = Load(ctx);
            ReconcileInterruptedFocus(ctx, store);
            OpenHub(owner, ctx, store);
        }

        public static void OpenForResearchCase(Window owner, DeepSystemsContext ctx, string caseId, string title, string question)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(caseId)) { Open(owner, ctx); return; }
            ProductivityStore store = Load(ctx);
            ProductivityMission existing = store.Missions.FirstOrDefault(x => String.Equals(x.ResearchCaseId, caseId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                string now = Now();
                existing = new ProductivityMission
                {
                    Id = Id("mission"),
                    ParentId = "",
                    Title = String.IsNullOrWhiteSpace(title) ? "Research case " + caseId : title,
                    Objective = String.IsNullOrWhiteSpace(question) ? "Resolve the linked research case through evidence-backed investigation." : question,
                    Status = "ACTIVE",
                    Priority = 4,
                    ResearchCaseId = caseId,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    LastOpenedUtc = now
                };
                existing.RootId = existing.Id;
                store.Missions.Add(existing);
                ProductivityTask seed = NewTask(existing.Id, "Review linked research case and establish evidence frontier", "RESEARCH", 4, 5, 2);
                seed.ResearchCaseId = caseId;
                store.Tasks.Add(seed);
                store.CurrentMissionId = existing.Id;
                store.CurrentTaskId = seed.Id;
                Save(ctx, store);
                Ledger(ctx, "MISSION_CREATE_FROM_RESEARCH", "MISSION", existing.Id, "research_case_id=" + caseId);
            }
            OpenMission(owner, ctx, existing.Id);
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx, ProductivityStore store)
        {
            Window w = W(owner, "YOMI · PRODUCTIVITY FABRIC", 1180, 820);
            DockPanel root = new DockPanel { Margin = new Thickness(20) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("COGNITIVE PRODUCTIVITY FABRIC", 30, Text, FontWeights.Bold));
            head.Children.Add(T("Missions · dependency graphs · focus continuity · evidence-backed next actions · research debt · resumable deep work", 13, Muted, FontWeights.Normal));
            TextBlock law = T("The system may organize work aggressively. It may never confuse organizational metadata with playback, queue, scheduler, or research evidence authority.", 11.5, Amber, FontWeights.SemiBold);
            law.Margin = new Thickness(0, 6, 0, 14); head.Children.Add(law);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel p = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            sv.Content = p;

            Portal(p, "MISSION", "MISSION CONTROL", "Projects become recursive missions with objectives, child missions, linked cases, task graphs, evidence, notes and resume capsules.", delegate { OpenMissionControl(w, ctx); });
            Portal(p, "NEXT", "NEXT-ACTION REACTOR", "Ranks executable work from dependency readiness, leverage, priority, value, effort, staleness and current context.", delegate { OpenNextActionReactor(w, ctx); });
            Portal(p, "FOCUS", "DEEP-WORK COCKPIT", "Crash-aware focus sessions, heartbeat accounting, current objective, scratch notes and resumable work continuity.", delegate { OpenFocusCockpit(w, ctx); });
            Portal(p, "GRAPH", "WORK GRAPH / DEPENDENCY FRONTIER", "See executable, blocked, cyclical and high-leverage nodes. Descend into exactly what unlocks what.", delegate { OpenWorkGraph(w, ctx); });
            Portal(p, "DEBT", "RESEARCH DEBT OBSERVATORY", "Thin-evidence questions, stale missions, blocked chains, orphan captures, abandoned focus and unincorporated research cases.", delegate { OpenResearchDebt(w, ctx); });
            Portal(p, "INBOX", "CAPTURE INBOX", "Thoughts, questions, tasks and evidence fragments land here first, then get promoted into the right mission when you decide.", delegate { OpenCaptureInbox(w, ctx); });
            Portal(p, "RESUME", "RESUME CAPSULE VAULT", "Where was I? What was I trying to prove? What should I do next? Save and reopen cognitive state instead of reconstructing it from memory.", delegate { OpenResumeVault(w, ctx); });
            Portal(p, "CASES", "RESEARCH CASE BRIDGE", "Import persistent Research Workbench cases into missions without converting hypotheses or notes into authority.", delegate { OpenResearchCaseBridge(w, ctx); });
            Portal(p, "LEDGER", "PRODUCTIVITY FLIGHT RECORDER", "Append-only hash-chained record of organizational actions: creation, status changes, focus sessions and promotions.", delegate { OpenLedger(w, ctx); });
            Portal(p, "REVIEW", "REVIEW RADAR", "Staleness, incomplete dependency chains, missions without next actions, focus interruptions and unprocessed captures.", delegate { OpenReviewRadar(w, ctx); });
            Portal(p, "∞", "DEEP SYSTEMS ATLAS", "Leave productivity and descend into the larger live system world.", delegate { DeepSystems.OpenAtlasFromContext(w); });
            Portal(p, "R&D", "RESEARCH WORKBENCH", "Return to unexpected leads, hypotheses and persistent evidence cases.", delegate { ResearchWorkbench.Open(w, ctx); });
            Portal(p, "GRAPH+", "CAUSAL GRAPH LAB", "Follow evidence topology and identity wormholes when a productivity question becomes a systems question.", delegate { CausalGraphLab.Open(w, ctx); });
            Portal(p, "ΔT", "TEMPORAL OBSERVATORY", "Follow the same problem through time, epochs and historical evidence frames.", delegate { TemporalObservatory.Open(w, ctx); });

            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenMissionControl(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx);
            Window w = W(owner, "YOMI · MISSION CONTROL", 1120, 780);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("MISSION CONTROL", 28, Text, FontWeights.Bold));
            head.Children.Add(T("Recursive objectives with real dependency-bearing work beneath them", 12.5, Muted, FontWeights.Normal));
            WrapPanel actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 10) };
            actions.Children.Add(Btn("NEW ROOT MISSION", delegate
            {
                string title = Prompt(w, "NEW MISSION", "Mission title", ""); if (String.IsNullOrWhiteSpace(title)) return;
                string objective = Prompt(w, "MISSION OBJECTIVE", "What are you actually trying to accomplish?", "");
                ProductivityMission m = NewMission(title, objective, null);
                ProductivityStore s = Load(ctx); s.Missions.Add(m); s.CurrentMissionId = m.Id; Save(ctx, s); Ledger(ctx, "MISSION_CREATE", "MISSION", m.Id, title); OpenMission(w, ctx, m.Id);
            }));
            actions.Children.Add(Btn("NEXT ACTION", delegate { OpenNextActionReactor(w, ctx); }));
            actions.Children.Add(Btn("WORK GRAPH", delegate { OpenWorkGraph(w, ctx); }));
            actions.Children.Add(Btn("CAPTURE", delegate { QuickCapture(w, ctx, null); }));
            head.Children.Add(actions); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ListBox list = List();
            List<ProductivityMission> ordered = store.Missions.OrderBy(x => MissionDepth(store, x)).ThenByDescending(x => x.Priority).ThenByDescending(x => ParseUtc(x.UpdatedUtc)).ToList();
            foreach (ProductivityMission m in ordered)
            {
                int depth = MissionDepth(store, m);
                int open = store.Tasks.Count(t => t.MissionId == m.Id && !IsDone(t));
                int ready = store.Tasks.Count(t => t.MissionId == m.Id && IsExecutable(t, store));
                list.Items.Add(new ListBoxItem { Content = new TextBlock { Text = new string(' ', depth * 4) + "▸ " + m.ToString() + "   ·   " + ready.ToString(CultureInfo.InvariantCulture) + " ready / " + open.ToString(CultureInfo.InvariantCulture) + " open", Foreground = Text, FontFamily = new FontFamily("Cascadia Mono, Consolas") }, Tag = m.Id });
            }
            list.MouseDoubleClick += delegate { ListBoxItem item = list.SelectedItem as ListBoxItem; if (item != null) OpenMission(w, ctx, item.Tag as string); };
            root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenMission(Window owner, DeepSystemsContext ctx, string missionId)
        {
            ProductivityStore store = Load(ctx);
            ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == missionId); if (m == null) return;
            m.LastOpenedUtc = Now(); m.UpdatedUtc = m.LastOpenedUtc; store.CurrentMissionId = m.Id; Save(ctx, store);
            Window w = W(owner, "YOMI · MISSION · " + (m.Title ?? m.Id), 1120, 820);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("MISSION DOSSIER", 11, Accent, FontWeights.Bold));
            head.Children.Add(T(m.Title ?? m.Id, 29, Text, FontWeights.Bold));
            head.Children.Add(T(m.Objective ?? "", 13, Muted, FontWeights.Normal));
            head.Children.Add(T((m.Status ?? "ACTIVE") + "   ·   P" + m.Priority.ToString(CultureInfo.InvariantCulture) + "   ·   " + m.Id + (String.IsNullOrWhiteSpace(m.ResearchCaseId) ? "" : "   ·   case " + m.ResearchCaseId), 11.5, Blue, FontWeights.SemiBold));
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel body = new StackPanel(); sv.Content = body;

            WrapPanel nav = new WrapPanel();
            nav.Children.Add(Btn("ADD TASK", delegate { CreateTaskInteractive(w, ctx, m.Id, null); }));
            nav.Children.Add(Btn("CHILD MISSION", delegate { CreateChildMissionInteractive(w, ctx, m); }));
            nav.Children.Add(Btn("START FOCUS", delegate { OpenFocusCockpitFor(w, ctx, m.Id, BestTaskId(store, m.Id)); }));
            nav.Children.Add(Btn("SAVE RESUME CAPSULE", delegate { SaveResumeCapsuleInteractive(w, ctx, m.Id); }));
            nav.Children.Add(Btn("CAPTURE", delegate { QuickCapture(w, ctx, m.Id); }));
            nav.Children.Add(Btn("WORK GRAPH", delegate { OpenWorkGraphForMission(w, ctx, m.Id); }));
            if (!String.IsNullOrWhiteSpace(m.ResearchCaseId)) nav.Children.Add(Btn("OPEN LINKED CASE", delegate { ResearchWorkbench.OpenSessionById(w, ctx, m.ResearchCaseId); }));
            body.Children.Add(nav);

            List<ProductivityRecommendation> recs = Recommendations(store, m.Id);
            ProductivityRecommendation best = recs.FirstOrDefault(x => x.Executable);
            StackPanel nxa = new StackPanel(); nxa.Children.Add(T("NEXT BEST ACTION", 11, Accent, FontWeights.Bold));
            if (best == null) nxa.Children.Add(T("No executable task. Inspect blockers, create work, or close the mission.", 13, Amber, FontWeights.SemiBold));
            else
            {
                TextBlock row = T(best.Task.Title + "\n" + best.Reason, 13, Text, FontWeights.Normal); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenTask(w, ctx, best.Task.Id); }; nxa.Children.Add(row);
            }
            body.Children.Add(Card(nxa));

            StackPanel stats = new StackPanel(); stats.Children.Add(T("MISSION STATE", 11, Blue, FontWeights.Bold));
            List<ProductivityTask> tasks = store.Tasks.Where(x => x.MissionId == m.Id).ToList();
            int done = tasks.Count(IsDone); int ready = tasks.Count(x => IsExecutable(x, store)); int blocked = tasks.Count(x => !IsDone(x) && !IsExecutable(x, store));
            stats.Children.Add(T(tasks.Count.ToString(CultureInfo.InvariantCulture) + " tasks · " + done.ToString(CultureInfo.InvariantCulture) + " complete · " + ready.ToString(CultureInfo.InvariantCulture) + " executable · " + blocked.ToString(CultureInfo.InvariantCulture) + " blocked", 12.5, Text, FontWeights.Normal));
            if (!String.IsNullOrWhiteSpace(m.ResumeSummary)) stats.Children.Add(T("RESUME CAPSULE\n" + m.ResumeSummary + (String.IsNullOrWhiteSpace(m.ResumeNext) ? "" : "\nNEXT: " + m.ResumeNext), 12.5, Violet, FontWeights.Normal));
            body.Children.Add(Card(stats));

            StackPanel taskPanel = new StackPanel(); taskPanel.Children.Add(T("TASK GRAPH NODES", 11, Accent, FontWeights.Bold));
            foreach (ProductivityTask t in tasks.OrderBy(x => TaskDepth(store, x)).ThenByDescending(x => x.Priority).ThenBy(x => x.Title))
            {
                ProductivityTask captured = t; int td = TaskDepth(store, t); int depsDone = t.DependsOn.Count(d => { ProductivityTask dt = store.Tasks.FirstOrDefault(x => x.Id == d); return dt != null && IsDone(dt); });
                TextBlock row = T(new string(' ', td * 4) + (IsExecutable(t, store) ? "▶ " : IsDone(t) ? "✓ " : "× ") + t.ToString() + "   ·   deps " + depsDone.ToString(CultureInfo.InvariantCulture) + "/" + t.DependsOn.Count.ToString(CultureInfo.InvariantCulture), 12.5, IsDone(t) ? Muted : Text, FontWeights.Normal);
                row.Margin = new Thickness(0, 5, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenTask(w, ctx, captured.Id); }; taskPanel.Children.Add(row);
            }
            body.Children.Add(Card(taskPanel));

            StackPanel child = new StackPanel(); child.Children.Add(T("CHILD MISSIONS", 11, Violet, FontWeights.Bold));
            foreach (ProductivityMission cm in store.Missions.Where(x => x.ParentId == m.Id).OrderByDescending(x => x.Priority))
            {
                ProductivityMission captured = cm; TextBlock row = T("↳ " + cm.ToString(), 12.5, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenMission(w, ctx, captured.Id); }; child.Children.Add(row);
            }
            body.Children.Add(Card(child));

            StackPanel evidence = new StackPanel(); evidence.Children.Add(T("MISSION EVIDENCE · " + m.EvidencePaths.Count.ToString(CultureInfo.InvariantCulture), 11, Blue, FontWeights.Bold));
            foreach (string pth in m.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase).Take(30)) { string cp = pth; TextBlock row = T(Path.GetFileName(pth) + "   ·   " + pth, 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalFile(w, ctx, cp, "ATLAS › PRODUCTIVITY › MISSION › " + m.Id + " › EVIDENCE"); }; evidence.Children.Add(row); }
            evidence.Children.Add(Btn("ATTACH EVIDENCE PATH", delegate { AttachMissionEvidence(w, ctx, m.Id); }));
            body.Children.Add(Card(evidence));

            StackPanel notes = new StackPanel(); notes.Children.Add(T("MISSION NOTES", 11, Accent, FontWeights.Bold));
            foreach (string note in m.Notes.TakeLastCompat(24)) notes.Children.Add(T("• " + note, 12.2, Text, FontWeights.Normal));
            TextBox box = new TextBox { MinHeight = 34, Margin = new Thickness(0, 8, 0, 5), Padding = new Thickness(8), Background = Raised, Foreground = Text, BorderBrush = Border };
            notes.Children.Add(box); notes.Children.Add(Btn("ADD NOTE", delegate { string v = (box.Text ?? "").Trim(); if (v.Length == 0) return; ProductivityStore s = Load(ctx); ProductivityMission mm = s.Missions.FirstOrDefault(x => x.Id == missionId); if (mm == null) return; mm.Notes.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " · " + v); if (mm.Notes.Count > 128) mm.Notes = mm.Notes.Skip(mm.Notes.Count - 128).ToList(); Touch(mm); Save(ctx, s); Ledger(ctx, "MISSION_NOTE", "MISSION", mm.Id, Trunc(v, 180)); box.Clear(); }));
            body.Children.Add(Card(notes));

            WrapPanel status = new WrapPanel();
            status.Children.Add(Btn("MARK ACTIVE", delegate { SetMissionStatus(ctx, m.Id, "ACTIVE"); }));
            status.Children.Add(Btn("PAUSE", delegate { SetMissionStatus(ctx, m.Id, "PAUSED"); }));
            status.Children.Add(Btn("COMPLETE", delegate { SetMissionStatus(ctx, m.Id, "COMPLETE"); }));
            status.Children.Add(Btn("RAW DOSSIER", delegate { DeepSystems.OpenExternalValue(w, ctx, "PRODUCTIVITY MISSION // RAW", MissionMap(Load(ctx), m.Id), "ATLAS › PRODUCTIVITY › MISSION › " + m.Id + " › RAW"); }));
            body.Children.Add(status);

            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenTask(Window owner, DeepSystemsContext ctx, string taskId)
        {
            ProductivityStore store = Load(ctx); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (t == null) return; ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == t.MissionId);
            t.UpdatedUtc = Now(); store.CurrentTaskId = t.Id; if (m != null) store.CurrentMissionId = m.Id; Save(ctx, store);
            Window w = W(owner, "YOMI · TASK · " + (t.Title ?? t.Id), 1040, 780);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("TASK DOSSIER", 11, Accent, FontWeights.Bold)); h.Children.Add(T(t.Title ?? t.Id, 27, Text, FontWeights.Bold)); h.Children.Add(T(t.Detail ?? "", 12.5, Muted, FontWeights.Normal)); h.Children.Add(T((t.Status ?? "TODO") + "   ·   " + (t.Kind ?? "TASK") + "   ·   P" + t.Priority + " V" + t.Value + " E" + t.Effort + "   ·   " + t.Id, 11.5, Blue, FontWeights.SemiBold)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 12, 0, 0) }; StackPanel body = new StackPanel(); sv.Content = body;
            WrapPanel nav = new WrapPanel();
            nav.Children.Add(Btn("START FOCUS", delegate { OpenFocusCockpitFor(w, ctx, t.MissionId, t.Id); }));
            nav.Children.Add(Btn("ADD SUBTASK", delegate { CreateTaskInteractive(w, ctx, t.MissionId, t.Id); }));
            nav.Children.Add(Btn("ADD DEPENDENCY", delegate { AddDependencyInteractive(w, ctx, t.Id); }));
            nav.Children.Add(Btn("ATTACH EVIDENCE", delegate { AttachTaskEvidence(w, ctx, t.Id); }));
            if (!String.IsNullOrWhiteSpace(t.ResearchCaseId)) nav.Children.Add(Btn("OPEN RESEARCH CASE", delegate { ResearchWorkbench.OpenSessionById(w, ctx, t.ResearchCaseId); }));
            if (m != null) nav.Children.Add(Btn("MISSION", delegate { OpenMission(w, ctx, m.Id); }));
            body.Children.Add(nav);

            StackPanel deps = new StackPanel(); deps.Children.Add(T("DEPENDENCIES", 11, Amber, FontWeights.Bold));
            foreach (string depId in t.DependsOn)
            {
                ProductivityTask dep = store.Tasks.FirstOrDefault(x => x.Id == depId); if (dep == null) { deps.Children.Add(T("MISSING · " + depId, 12, Danger, FontWeights.SemiBold)); continue; }
                ProductivityTask captured = dep; TextBlock row = T((IsDone(dep) ? "✓ " : "× ") + dep.Title + "   ·   " + dep.Status, 12.5, IsDone(dep) ? Muted : Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenTask(w, ctx, captured.Id); }; deps.Children.Add(row);
            }
            if (t.DependsOn.Count == 0) deps.Children.Add(T("No prerequisites. This node is structurally executable unless explicitly blocked or completed.", 12, Muted, FontWeights.Normal));
            body.Children.Add(Card(deps));

            int unlocks = UnlockCount(store, t.Id);
            StackPanel leverage = new StackPanel(); leverage.Children.Add(T("LEVERAGE", 11, Violet, FontWeights.Bold)); leverage.Children.Add(T("Direct/indirect downstream tasks unlocked if this node completes: " + unlocks.ToString(CultureInfo.InvariantCulture) + "\nExecutable now: " + (IsExecutable(t, store) ? "YES" : "NO") + (IsExecutable(t, store) ? "" : "\nBlock reason: " + BlockReason(t, store)), 12.5, Text, FontWeights.Normal)); body.Children.Add(Card(leverage));

            StackPanel ev = new StackPanel(); ev.Children.Add(T("TASK EVIDENCE · " + t.EvidencePaths.Count.ToString(CultureInfo.InvariantCulture), 11, Blue, FontWeights.Bold));
            foreach (string pth in t.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase).Take(32)) { string cp = pth; TextBlock row = T(Path.GetFileName(pth) + "   ·   " + pth, 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalFile(w, ctx, cp, "ATLAS › PRODUCTIVITY › TASK › " + t.Id + " › EVIDENCE"); }; ev.Children.Add(row); }
            body.Children.Add(Card(ev));

            StackPanel notes = new StackPanel(); notes.Children.Add(T("WORK NOTES", 11, Accent, FontWeights.Bold)); foreach (string n in t.Notes.TakeLastCompat(24)) notes.Children.Add(T("• " + n, 12.2, Text, FontWeights.Normal)); TextBox note = new TextBox { Margin = new Thickness(0, 8, 0, 5), MinHeight = 34, Padding = new Thickness(8), Background = Raised, Foreground = Text, BorderBrush = Border }; notes.Children.Add(note); notes.Children.Add(Btn("ADD NOTE", delegate { AddTaskNote(ctx, t.Id, note.Text); note.Clear(); })); body.Children.Add(Card(notes));

            WrapPanel states = new WrapPanel();
            states.Children.Add(Btn("TODO", delegate { SetTaskStatus(ctx, t.Id, "TODO"); }));
            states.Children.Add(Btn("DOING", delegate { SetTaskStatus(ctx, t.Id, "DOING"); }));
            states.Children.Add(Btn("BLOCKED", delegate { SetTaskStatus(ctx, t.Id, "BLOCKED"); }));
            states.Children.Add(Btn("DONE", delegate { SetTaskStatus(ctx, t.Id, "DONE"); }));
            states.Children.Add(Btn("SOMEDAY", delegate { SetTaskStatus(ctx, t.Id, "SOMEDAY"); }));
            states.Children.Add(Btn("RAW DOSSIER", delegate { DeepSystems.OpenExternalValue(w, ctx, "PRODUCTIVITY TASK // RAW", TaskMap(Load(ctx), t.Id), "ATLAS › PRODUCTIVITY › TASK › " + t.Id + " › RAW"); }));
            body.Children.Add(states);
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenNextActionReactor(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · NEXT-ACTION REACTOR", 1120, 780);
            DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("NEXT-ACTION REACTOR", 28, Text, FontWeights.Bold)); h.Children.Add(T("Mechanical prioritization, not prophecy: dependencies + leverage + priority + value − effort + staleness + current-context bias", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            List<ProductivityRecommendation> recs = Recommendations(store, null); ListBox list = List(); foreach (ProductivityRecommendation r in recs) list.Items.Add(r); list.MouseDoubleClick += delegate { ProductivityRecommendation r = list.SelectedItem as ProductivityRecommendation; if (r != null && r.Task != null) OpenTask(w, ctx, r.Task.Id); };
            StackPanel side = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            ProductivityRecommendation best = recs.FirstOrDefault(x => x.Executable); if (best != null) side.Children.Add(Card(T("REACTOR NOMINATION\n" + best.Task.Title + "\n\n" + best.Reason + "\n\nThis rank is an organizational heuristic. It does not imply factual truth, causal certainty, or engine authority.", 12.5, Accent, FontWeights.Normal)));
            DockPanel.SetDock(side, Dock.Bottom); root.Children.Add(side); root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenWorkGraph(Window owner, DeepSystemsContext ctx)
        {
            OpenWorkGraphForMission(owner, ctx, null);
        }

        private static void OpenWorkGraphForMission(Window owner, DeepSystemsContext ctx, string missionId)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · WORK GRAPH", 1160, 800);
            DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("WORK GRAPH / DEPENDENCY FRONTIER", 28, Text, FontWeights.Bold)); h.Children.Add(T(missionId == null ? "All task nodes" : "Mission-scoped graph: " + missionId, 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            List<ProductivityTask> tasks = store.Tasks.Where(x => missionId == null || x.MissionId == missionId).ToList(); List<string> cycles = DetectCycles(store, tasks.Select(x => x.Id));
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 12, 0, 0) }; StackPanel body = new StackPanel(); sv.Content = body;
            StackPanel summary = new StackPanel(); summary.Children.Add(T("GRAPH HEALTH", 11, cycles.Count == 0 ? Accent : Danger, FontWeights.Bold)); summary.Children.Add(T(tasks.Count.ToString(CultureInfo.InvariantCulture) + " nodes · " + tasks.Sum(x => x.DependsOn.Count).ToString(CultureInfo.InvariantCulture) + " dependency edges · " + tasks.Count(x => IsExecutable(x, store)).ToString(CultureInfo.InvariantCulture) + " executable · " + cycles.Count.ToString(CultureInfo.InvariantCulture) + " cycle witnesses", 12.5, Text, FontWeights.Normal)); body.Children.Add(Card(summary));
            if (cycles.Count > 0)
            {
                StackPanel cp = new StackPanel(); cp.Children.Add(T("CYCLE WITNESSES", 11, Danger, FontWeights.Bold)); foreach (string c in cycles.Take(24)) cp.Children.Add(T(c, 12, Danger, FontWeights.Normal)); body.Children.Add(Card(cp));
            }
            foreach (ProductivityTask t in tasks.OrderByDescending(x => UnlockCount(store, x.Id)).ThenByDescending(x => x.Priority))
            {
                ProductivityTask captured = t; ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == t.MissionId); int u = UnlockCount(store, t.Id);
                string state = IsDone(t) ? "DONE" : IsExecutable(t, store) ? "EXECUTABLE" : "BLOCKED";
                Border c = Card(T(state + "   U" + u.ToString(CultureInfo.InvariantCulture) + "   " + (m == null ? "" : m.Title + " › ") + t.Title + "\n" + (t.DependsOn.Count == 0 ? "no dependencies" : "depends on: " + String.Join(", ", t.DependsOn.ToArray())) + (state == "BLOCKED" ? "\n" + BlockReason(t, store) : ""), 12.4, state == "BLOCKED" ? Amber : state == "DONE" ? Muted : Text, FontWeights.Normal)); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { OpenTask(w, ctx, captured.Id); }; body.Children.Add(c);
            }
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenFocusCockpit(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); string mission = store.CurrentMissionId; string task = store.CurrentTaskId; if (String.IsNullOrWhiteSpace(task)) task = BestTaskId(store, mission); OpenFocusCockpitFor(owner, ctx, mission, task);
        }

        private static void OpenFocusCockpitFor(Window owner, DeepSystemsContext ctx, string missionId, string taskId)
        {
            ProductivityStore store = Load(ctx); ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == missionId); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId);
            Window w = W(owner, "YOMI · DEEP-WORK COCKPIT", 1040, 760); DockPanel root = new DockPanel { Margin = new Thickness(20) };
            StackPanel head = new StackPanel(); head.Children.Add(T("DEEP-WORK COCKPIT", 29, Text, FontWeights.Bold)); head.Children.Add(T(m == null ? "No current mission" : m.Title, 14, Accent, FontWeights.SemiBold)); head.Children.Add(T(t == null ? "Select a task from Mission Control or Next-Action Reactor." : t.Title, 13, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            StackPanel body = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            ProductivityFocusSession active = ActiveFocus(store);
            TextBlock clock = T(active == null ? "00:00:00" : FormatSeconds(active.AccumulatedSeconds), 42, Text, FontWeights.Bold); clock.FontFamily = new FontFamily("Cascadia Mono, Consolas"); body.Children.Add(Card(clock));
            WrapPanel actions = new WrapPanel();
            if (active == null && t != null) actions.Children.Add(Btn("BEGIN FOCUS", delegate { StartFocus(ctx, m == null ? t.MissionId : m.Id, t.Id); w.Close(); OpenFocusCockpitFor(owner, ctx, m == null ? t.MissionId : m.Id, t.Id); }));
            if (active != null)
            {
                actions.Children.Add(Btn("END FOCUS", delegate { EndFocusInteractive(w, ctx, active.Id); w.Close(); OpenFocusCockpit(owner, ctx); }));
                actions.Children.Add(Btn("INTERRUPT", delegate { InterruptFocus(ctx, active.Id, "operator interruption"); w.Close(); OpenFocusCockpit(owner, ctx); }));
            }
            actions.Children.Add(Btn("NEXT ACTION", delegate { OpenNextActionReactor(w, ctx); })); actions.Children.Add(Btn("CAPTURE THOUGHT", delegate { QuickCapture(w, ctx, m == null ? null : m.Id); })); body.Children.Add(actions);
            if (t != null)
            {
                StackPanel objective = new StackPanel(); objective.Children.Add(T("CURRENT NODE", 11, Blue, FontWeights.Bold)); objective.Children.Add(T((m == null ? "" : m.Objective + "\n\n") + t.Detail + "\n\nDependencies: " + (t.DependsOn.Count == 0 ? "none" : String.Join(", ", t.DependsOn.ToArray())) + "\nExecutable: " + (IsExecutable(t, store) ? "YES" : "NO"), 12.5, Text, FontWeights.Normal)); body.Children.Add(Card(objective));
                TextBox scratch = new TextBox { MinHeight = 90, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Padding = new Thickness(9), Margin = new Thickness(0, 8, 0, 5), Background = Raised, Foreground = Text, BorderBrush = Border }; body.Children.Add(scratch); body.Children.Add(Btn("COMMIT SCRATCH NOTE TO TASK", delegate { AddTaskNote(ctx, t.Id, scratch.Text); scratch.Clear(); }));
            }
            if (active != null)
            {
                FocusContext = ctx; FocusWindow = w; FocusClock = clock; EnsureFocusPulse();
            }
            root.Children.Add(body); w.Content = root; w.Show();
        }

        private static void OpenResearchDebt(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · RESEARCH DEBT OBSERVATORY", 1120, 800); DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("RESEARCH DEBT OBSERVATORY", 28, Text, FontWeights.Bold)); h.Children.Add(T("Useful discomfort made visible: stale, blocked, thinly evidenced, orphaned and unincorporated work", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 12, 0, 0) }; StackPanel body = new StackPanel(); sv.Content = body;

            List<ProductivityTask> blocked = store.Tasks.Where(x => !IsDone(x) && !IsExecutable(x, store)).ToList();
            StackPanel bp = new StackPanel(); bp.Children.Add(T("BLOCKED CHAINS · " + blocked.Count.ToString(CultureInfo.InvariantCulture), 11, Amber, FontWeights.Bold)); foreach (ProductivityTask t in blocked.Take(60)) { ProductivityTask c = t; TextBlock row = T(t.Title + "   ·   " + BlockReason(t, store), 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenTask(w, ctx, c.Id); }; bp.Children.Add(row); } body.Children.Add(Card(bp));

            List<ProductivityTask> thin = store.Tasks.Where(x => !IsDone(x) && (String.Equals(x.Kind, "RESEARCH", StringComparison.OrdinalIgnoreCase) || String.Equals(x.Kind, "QUESTION", StringComparison.OrdinalIgnoreCase)) && x.EvidencePaths.Count == 0 && String.IsNullOrWhiteSpace(x.ResearchCaseId)).ToList();
            StackPanel tp = new StackPanel(); tp.Children.Add(T("THIN-EVIDENCE RESEARCH NODES · " + thin.Count.ToString(CultureInfo.InvariantCulture), 11, Danger, FontWeights.Bold)); foreach (ProductivityTask t in thin.Take(60)) { ProductivityTask c = t; TextBlock row = T(t.Title + "   ·   no attached evidence / no linked research case", 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenTask(w, ctx, c.Id); }; tp.Children.Add(row); } body.Children.Add(Card(tp));

            DateTime staleCut = DateTime.UtcNow.AddDays(-7); List<ProductivityMission> stale = store.Missions.Where(x => !String.Equals(x.Status, "COMPLETE", StringComparison.OrdinalIgnoreCase) && ParseUtc(x.UpdatedUtc) < staleCut).OrderBy(x => ParseUtc(x.UpdatedUtc)).ToList();
            StackPanel sp = new StackPanel(); sp.Children.Add(T("STALE MISSIONS · " + stale.Count.ToString(CultureInfo.InvariantCulture), 11, Violet, FontWeights.Bold)); foreach (ProductivityMission mm in stale.Take(48)) { ProductivityMission c = mm; TextBlock row = T(mm.Title + "   ·   last touched " + ParseUtc(mm.UpdatedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenMission(w, ctx, c.Id); }; sp.Children.Add(row); } body.Children.Add(Card(sp));

            List<ProductivityCapture> inbox = store.Captures.Where(x => !x.Processed).ToList(); body.Children.Add(Card(T("CAPTURE DEBT\n" + inbox.Count.ToString(CultureInfo.InvariantCulture) + " unprocessed inbox items", 12.5, inbox.Count > 0 ? Amber : Accent, FontWeights.Normal)));
            List<ResearchCaseLite> cases = ReadResearchCases(ctx); List<string> linked = store.Missions.Where(x => !String.IsNullOrWhiteSpace(x.ResearchCaseId)).Select(x => x.ResearchCaseId).ToList(); List<ResearchCaseLite> unlinked = cases.Where(x => !linked.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            StackPanel up = new StackPanel(); up.Children.Add(T("UNINCORPORATED RESEARCH CASES · " + unlinked.Count.ToString(CultureInfo.InvariantCulture), 11, Blue, FontWeights.Bold)); foreach (ResearchCaseLite rc in unlinked.Take(40)) { ResearchCaseLite c = rc; TextBlock row = T(rc.Title + "   ·   " + rc.Id, 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 4, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { OpenResearchCaseCandidate(w, ctx, c); }; up.Children.Add(row); } body.Children.Add(Card(up));

            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenCaptureInbox(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · CAPTURE INBOX", 1040, 740); DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("CAPTURE INBOX", 28, Text, FontWeights.Bold)); h.Children.Add(T("Capture first. Decide structure later.", 12.5, Muted, FontWeights.Normal)); WrapPanel acts = new WrapPanel { Margin = new Thickness(0, 8, 0, 10) }; acts.Children.Add(Btn("CAPTURE THOUGHT", delegate { QuickCapture(w, ctx, store.CurrentMissionId, "THOUGHT"); })); acts.Children.Add(Btn("CAPTURE QUESTION", delegate { QuickCapture(w, ctx, store.CurrentMissionId, "QUESTION"); })); acts.Children.Add(Btn("CAPTURE TASK", delegate { QuickCapture(w, ctx, store.CurrentMissionId, "TASK"); })); acts.Children.Add(Btn("CAPTURE EVIDENCE", delegate { QuickCapture(w, ctx, store.CurrentMissionId, "EVIDENCE"); })); h.Children.Add(acts); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ListBox list = List(); List<ProductivityCapture> ordered = store.Captures.OrderBy(x => x.Processed).ThenByDescending(x => ParseUtc(x.CreatedUtc)).ToList(); foreach (ProductivityCapture c in ordered) list.Items.Add(c); list.MouseDoubleClick += delegate { ProductivityCapture c = list.SelectedItem as ProductivityCapture; if (c != null) OpenCaptureDossier(w, ctx, c.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenCaptureDossier(Window owner, DeepSystemsContext ctx, string captureId)
        {
            ProductivityStore store = Load(ctx); ProductivityCapture c = store.Captures.FirstOrDefault(x => x.Id == captureId); if (c == null) return; Window w = W(owner, "YOMI · CAPTURE · " + c.Kind, 880, 560); StackPanel body = new StackPanel { Margin = new Thickness(18) }; body.Children.Add(T(c.Kind + " CAPTURE", 12, Accent, FontWeights.Bold)); body.Children.Add(T(c.Text, 22, Text, FontWeights.SemiBold)); body.Children.Add(T(c.Id + "   ·   " + c.CreatedUtc + (c.Processed ? "   ·   processed" : ""), 11.5, Muted, FontWeights.Normal)); WrapPanel nav = new WrapPanel { Margin = new Thickness(0, 12, 0, 10) };
            nav.Children.Add(Btn("PROMOTE TO TASK", delegate { PromoteCaptureToTask(w, ctx, c.Id); }));
            nav.Children.Add(Btn("PROMOTE TO MISSION", delegate { PromoteCaptureToMission(w, ctx, c.Id); }));
            nav.Children.Add(Btn("MARK PROCESSED", delegate { MarkCaptureProcessed(ctx, c.Id); }));
            nav.Children.Add(Btn("TRACE TEXT", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, c.Text, "ATLAS › PRODUCTIVITY › CAPTURE › " + c.Id + " › TRACE"); }));
            body.Children.Add(nav); body.Children.Add(Card(T("CAPTURE CONTRACT\nInbox text is organizational metadata. Promoting a capture creates work structure; it does not create evidence or factual authority.", 12.5, Muted, FontWeights.Normal))); w.Content = body; w.Show();
        }

        private static void OpenResumeVault(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · RESUME CAPSULE VAULT", 1060, 740); DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("RESUME CAPSULE VAULT", 28, Text, FontWeights.Bold)); h.Children.Add(T("Persist cognitive context so stopping work does not mean reconstructing work", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); List<ProductivityMission> ms = store.Missions.Where(x => !String.IsNullOrWhiteSpace(x.ResumeSummary)).OrderByDescending(x => ParseUtc(x.ResumeSavedUtc)).ToList(); foreach (ProductivityMission m in ms) list.Items.Add(new ListBoxItem { Content = T((m.ResumeSavedUtc ?? "") + "   ·   " + m.Title + "\n" + m.ResumeSummary + (String.IsNullOrWhiteSpace(m.ResumeNext) ? "" : "\nNEXT: " + m.ResumeNext), 12.5, Text, FontWeights.Normal), Tag = m.Id }); list.MouseDoubleClick += delegate { ListBoxItem i = list.SelectedItem as ListBoxItem; if (i != null) OpenMission(w, ctx, i.Tag as string); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenResearchCaseBridge(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · RESEARCH CASE BRIDGE", 1060, 740); DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("RESEARCH CASE BRIDGE", 28, Text, FontWeights.Bold)); h.Children.Add(T("Research cases stay epistemic records. Missions merely organize the work required to investigate them.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); List<ResearchCaseLite> cases = ReadResearchCases(ctx).OrderByDescending(x => ParseUtc(x.UpdatedUtc)).ToList(); foreach (ResearchCaseLite rc in cases) list.Items.Add(rc); list.MouseDoubleClick += delegate { ResearchCaseLite rc = list.SelectedItem as ResearchCaseLite; if (rc != null) OpenResearchCaseCandidate(w, ctx, rc); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenResearchCaseCandidate(Window owner, DeepSystemsContext ctx, ResearchCaseLite rc)
        {
            Window w = W(owner, "YOMI · RESEARCH CASE → MISSION", 920, 600); StackPanel body = new StackPanel { Margin = new Thickness(18) }; body.Children.Add(T("RESEARCH CASE", 11, Blue, FontWeights.Bold)); body.Children.Add(T(rc.Title ?? rc.Id, 25, Text, FontWeights.Bold)); body.Children.Add(T(rc.Question ?? "", 13, Muted, FontWeights.Normal)); body.Children.Add(T("Working hypothesis: " + (rc.Hypothesis ?? "Unresolved"), 12.5, Amber, FontWeights.Normal)); WrapPanel nav = new WrapPanel { Margin = new Thickness(0, 12, 0, 10) }; nav.Children.Add(Btn("OPEN CASE", delegate { ResearchWorkbench.OpenSessionById(w, ctx, rc.Id); })); nav.Children.Add(Btn("PROMOTE TO MISSION", delegate { OpenForResearchCase(w, ctx, rc.Id, rc.Title, rc.Question); })); nav.Children.Add(Btn("TRACE CASE ID", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, rc.Id, "ATLAS › PRODUCTIVITY › CASE BRIDGE › " + rc.Id); })); body.Children.Add(nav); body.Children.Add(Card(T("Promotion copies only organizational pointers and creates a review task. The case's hypothesis remains a hypothesis; its evidence remains evidence; the mission remains organizational metadata.", 12.5, Muted, FontWeights.Normal))); w.Content = body; w.Show();
        }

        private static void OpenReviewRadar(Window owner, DeepSystemsContext ctx)
        {
            ProductivityStore store = Load(ctx); Window w = W(owner, "YOMI · REVIEW RADAR", 1100, 780); ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; StackPanel body = new StackPanel { Margin = new Thickness(18) }; sv.Content = body; body.Children.Add(T("REVIEW RADAR", 28, Text, FontWeights.Bold)); body.Children.Add(T("Things likely to have fallen out of working memory", 12.5, Muted, FontWeights.Normal));
            List<ProductivityMission> noNext = store.Missions.Where(x => !String.Equals(x.Status, "COMPLETE", StringComparison.OrdinalIgnoreCase) && !store.Tasks.Any(t => t.MissionId == x.Id && IsExecutable(t, store))).ToList();
            StackPanel n = new StackPanel(); n.Children.Add(T("MISSIONS WITHOUT AN EXECUTABLE NEXT ACTION · " + noNext.Count.ToString(CultureInfo.InvariantCulture), 11, Amber, FontWeights.Bold)); foreach (ProductivityMission m in noNext.Take(50)) { ProductivityMission c = m; TextBlock r = T(m.Title, 12, Text, FontWeights.Normal); r.Cursor = Cursors.Hand; r.MouseLeftButtonUp += delegate { OpenMission(w, ctx, c.Id); }; n.Children.Add(r); } body.Children.Add(Card(n));
            List<ProductivityFocusSession> interrupted = store.FocusSessions.Where(x => String.Equals(x.Status, "INTERRUPTED", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => ParseUtc(x.LastHeartbeatUtc)).Take(40).ToList(); StackPanel f = new StackPanel(); f.Children.Add(T("INTERRUPTED FOCUS SESSIONS · " + interrupted.Count.ToString(CultureInfo.InvariantCulture), 11, Violet, FontWeights.Bold)); foreach (ProductivityFocusSession fs in interrupted) { ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == fs.TaskId); f.Children.Add(T((t == null ? fs.TaskId : t.Title) + "   ·   " + FormatSeconds(fs.AccumulatedSeconds) + " captured", 12, Text, FontWeights.Normal)); } body.Children.Add(Card(f));
            List<ProductivityCapture> cap = store.Captures.Where(x => !x.Processed && ParseUtc(x.CreatedUtc) < DateTime.UtcNow.AddDays(-2)).ToList(); body.Children.Add(Card(T("STALE CAPTURES\n" + cap.Count.ToString(CultureInfo.InvariantCulture) + " inbox items older than 48 hours", 12.5, cap.Count == 0 ? Accent : Amber, FontWeights.Normal)));
            List<string> cycles = DetectCycles(store, store.Tasks.Select(x => x.Id)); body.Children.Add(Card(T("DEPENDENCY CYCLES\n" + cycles.Count.ToString(CultureInfo.InvariantCulture) + " cycle witnesses", 12.5, cycles.Count == 0 ? Accent : Danger, FontWeights.Normal)));
            w.Content = sv; w.Show();
        }

        private static void OpenLedger(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · PRODUCTIVITY FLIGHT RECORDER", 1120, 780); DockPanel root = new DockPanel { Margin = new Thickness(18) }; StackPanel h = new StackPanel(); h.Children.Add(T("PRODUCTIVITY FLIGHT RECORDER", 28, Text, FontWeights.Bold)); string verify = VerifyLedger(ctx); h.Children.Add(T(verify, 12.5, verify.StartsWith("VALID", StringComparison.OrdinalIgnoreCase) ? Accent : Danger, FontWeights.SemiBold)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); foreach (ProductivityLedgerRecord r in ReadLedger(ctx).OrderByDescending(x => x.Seq).Take(400)) list.Items.Add("#" + r.Seq.ToString(CultureInfo.InvariantCulture) + "   " + r.Utc + "   " + r.Action + "   " + r.ObjectType + "/" + r.ObjectId + "   " + r.Detail); list.MouseDoubleClick += delegate { int i = list.SelectedIndex; List<ProductivityLedgerRecord> rs = ReadLedger(ctx).OrderByDescending(x => x.Seq).Take(400).ToList(); if (i >= 0 && i < rs.Count) DeepSystems.OpenExternalValue(w, ctx, "PRODUCTIVITY LEDGER RECORD", LedgerMap(rs[i]), "ATLAS › PRODUCTIVITY › LEDGER › " + rs[i].Seq.ToString(CultureInfo.InvariantCulture)); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static List<ProductivityRecommendation> Recommendations(ProductivityStore store, string missionId)
        {
            var result = new List<ProductivityRecommendation>(); DateTime now = DateTime.UtcNow;
            foreach (ProductivityTask t in store.Tasks.Where(x => !IsDone(x) && (missionId == null || x.MissionId == missionId)))
            {
                ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == t.MissionId); bool executable = IsExecutable(t, store); int unlocks = UnlockCount(store, t.Id); double ageHours = Math.Max(0, (now - ParseUtc(t.UpdatedUtc)).TotalHours); int stale = (int)Math.Min(120, ageHours / 3.0);
                int score = t.Priority * 100 + t.Value * 60 - t.Effort * 25 + Math.Min(200, unlocks * 35) + stale;
                if (m != null) score += m.Priority * 40;
                if (String.Equals(store.CurrentMissionId, t.MissionId, StringComparison.OrdinalIgnoreCase)) score += 50;
                if (String.Equals(store.CurrentTaskId, t.Id, StringComparison.OrdinalIgnoreCase)) score += 25;
                if (String.Equals(t.Kind, "RESEARCH", StringComparison.OrdinalIgnoreCase) && t.EvidencePaths.Count == 0 && String.IsNullOrWhiteSpace(t.ResearchCaseId)) score -= 35;
                if (!executable) score -= 1000;
                string reason = "score=" + score.ToString(CultureInfo.InvariantCulture) + " = priority " + t.Priority + ", value " + t.Value + ", effort " + t.Effort + ", unlocks " + unlocks + ", staleness " + stale + (m != null ? ", mission P" + m.Priority : "") + ".";
                result.Add(new ProductivityRecommendation { Task = t, Mission = m, Score = score, Unlocks = unlocks, Executable = executable, Reason = reason, BlockReason = executable ? "" : BlockReason(t, store) });
            }
            return result.OrderByDescending(x => x.Executable).ThenByDescending(x => x.Score).ThenByDescending(x => x.Unlocks).ThenBy(x => x.Task.Title).ToList();
        }

        private static bool IsExecutable(ProductivityTask t, ProductivityStore store)
        {
            if (t == null || IsDone(t) || String.Equals(t.Status, "BLOCKED", StringComparison.OrdinalIgnoreCase) || String.Equals(t.Status, "SOMEDAY", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (string depId in t.DependsOn)
            {
                ProductivityTask dep = store.Tasks.FirstOrDefault(x => x.Id == depId); if (dep == null || !IsDone(dep)) return false;
            }
            if (DetectCycleFrom(store, t.Id)) return false;
            return true;
        }

        private static string BlockReason(ProductivityTask t, ProductivityStore store)
        {
            if (t == null) return "missing task";
            if (String.Equals(t.Status, "BLOCKED", StringComparison.OrdinalIgnoreCase)) return "explicitly marked BLOCKED";
            if (String.Equals(t.Status, "SOMEDAY", StringComparison.OrdinalIgnoreCase)) return "parked in SOMEDAY";
            if (DetectCycleFrom(store, t.Id)) return "dependency cycle reaches this node";
            List<string> missing = new List<string>(); List<string> open = new List<string>();
            foreach (string d in t.DependsOn)
            {
                ProductivityTask dep = store.Tasks.FirstOrDefault(x => x.Id == d); if (dep == null) missing.Add(d); else if (!IsDone(dep)) open.Add(dep.Title ?? dep.Id);
            }
            if (missing.Count > 0) return "missing dependency: " + String.Join(", ", missing.ToArray());
            if (open.Count > 0) return "waiting on: " + String.Join(", ", open.ToArray());
            return "not executable under current state";
        }

        private static int UnlockCount(ProductivityStore store, string taskId)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Queue<string> q = new Queue<string>(); q.Enqueue(taskId);
            while (q.Count > 0)
            {
                string cur = q.Dequeue(); foreach (ProductivityTask t in store.Tasks.Where(x => x.DependsOn.Contains(cur, StringComparer.OrdinalIgnoreCase))) if (seen.Add(t.Id)) q.Enqueue(t.Id);
            }
            return seen.Count;
        }

        private static bool DetectCycleFrom(ProductivityStore store, string taskId)
        {
            HashSet<string> active = new HashSet<string>(StringComparer.OrdinalIgnoreCase); HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase); return CycleDfs(store, taskId, active, done, null);
        }

        private static bool CycleDfs(ProductivityStore store, string id, HashSet<string> active, HashSet<string> done, List<string> path)
        {
            if (active.Contains(id)) { if (path != null) path.Add(id); return true; }
            if (done.Contains(id)) return false; active.Add(id); if (path != null) path.Add(id); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == id);
            if (t != null) foreach (string d in t.DependsOn) if (CycleDfs(store, d, active, done, path)) return true;
            active.Remove(id); done.Add(id); if (path != null && path.Count > 0) path.RemoveAt(path.Count - 1); return false;
        }

        private static List<string> DetectCycles(ProductivityStore store, IEnumerable<string> ids)
        {
            List<string> result = new List<string>(); foreach (string id in ids.Distinct(StringComparer.OrdinalIgnoreCase)) { List<string> path = new List<string>(); if (CycleDfs(store, id, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase), path)) { string witness = String.Join(" → ", path.ToArray()); if (!result.Contains(witness, StringComparer.OrdinalIgnoreCase)) result.Add(witness); } } return result;
        }

        private static void StartFocus(DeepSystemsContext ctx, string missionId, string taskId)
        {
            ProductivityStore store = Load(ctx); ProductivityFocusSession active = ActiveFocus(store); if (active != null) return; string now = Now(); ProductivityFocusSession fs = new ProductivityFocusSession { Id = Id("focus"), MissionId = missionId ?? "", TaskId = taskId ?? "", Status = "ACTIVE", StartedUtc = now, LastHeartbeatUtc = now, AccumulatedSeconds = 0 }; store.FocusSessions.Add(fs); store.CurrentMissionId = missionId; store.CurrentTaskId = taskId; ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (t != null && !IsDone(t)) { t.Status = "DOING"; t.UpdatedUtc = now; } Save(ctx, store); Ledger(ctx, "FOCUS_BEGIN", "FOCUS", fs.Id, "mission=" + missionId + " task=" + taskId); FocusContext = ctx; EnsureFocusPulse();
        }

        private static void EnsureFocusPulse()
        {
            if (FocusPulse != null) return; FocusPulse = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; FocusPulse.Tick += delegate
            {
                try
                {
                    if (FocusContext == null) return; ProductivityStore store = Load(FocusContext); ProductivityFocusSession fs = ActiveFocus(store); if (fs == null) { if (FocusClock != null) FocusClock.Text = "00:00:00"; return; }
                    DateTime last = ParseUtc(fs.LastHeartbeatUtc); DateTime now = DateTime.UtcNow; double delta = (now - last).TotalSeconds; if (delta >= 25)
                    {
                        long add = (long)Math.Max(0, Math.Min(35, delta)); fs.AccumulatedSeconds += add; fs.LastHeartbeatUtc = now.ToString("o", CultureInfo.InvariantCulture); Save(FocusContext, store);
                    }
                    long live = fs.AccumulatedSeconds + (long)Math.Max(0, Math.Min(35, (now - ParseUtc(fs.LastHeartbeatUtc)).TotalSeconds)); if (FocusClock != null) FocusClock.Text = FormatSeconds(live);
                }
                catch { }
            }; FocusPulse.Start();
        }

        private static void EndFocusInteractive(Window owner, DeepSystemsContext ctx, string focusId)
        {
            string note = Prompt(owner, "END FOCUS", "Exit note / what changed?", ""); ProductivityStore store = Load(ctx); ProductivityFocusSession fs = store.FocusSessions.FirstOrDefault(x => x.Id == focusId); if (fs == null) return; AccumulateFocus(fs); fs.Status = "COMPLETE"; fs.EndedUtc = Now(); fs.ExitNote = note ?? ""; Save(ctx, store); Ledger(ctx, "FOCUS_END", "FOCUS", fs.Id, "seconds=" + fs.AccumulatedSeconds.ToString(CultureInfo.InvariantCulture) + " note=" + Trunc(note, 120));
        }

        private static void InterruptFocus(DeepSystemsContext ctx, string focusId, string reason)
        {
            ProductivityStore store = Load(ctx); ProductivityFocusSession fs = store.FocusSessions.FirstOrDefault(x => x.Id == focusId); if (fs == null) return; AccumulateFocus(fs); fs.Status = "INTERRUPTED"; fs.EndedUtc = Now(); fs.ExitNote = reason ?? "interrupted"; Save(ctx, store); Ledger(ctx, "FOCUS_INTERRUPTED", "FOCUS", fs.Id, reason ?? "");
        }

        private static void AccumulateFocus(ProductivityFocusSession fs)
        {
            DateTime now = DateTime.UtcNow; DateTime last = ParseUtc(fs.LastHeartbeatUtc); long add = (long)Math.Max(0, Math.Min(60, (now - last).TotalSeconds)); fs.AccumulatedSeconds += add; fs.LastHeartbeatUtc = now.ToString("o", CultureInfo.InvariantCulture);
        }

        private static void ReconcileInterruptedFocus(DeepSystemsContext ctx, ProductivityStore store)
        {
            ProductivityFocusSession active = ActiveFocus(store); if (active == null) return; DateTime hb = ParseUtc(active.LastHeartbeatUtc); if ((DateTime.UtcNow - hb).TotalSeconds <= 120) { FocusContext = ctx; EnsureFocusPulse(); return; }
            active.Status = "INTERRUPTED"; active.EndedUtc = active.LastHeartbeatUtc; active.ExitNote = "Controller heartbeat disappeared; downtime was not counted as focus time."; Save(ctx, store); Ledger(ctx, "FOCUS_CRASH_RECONCILE", "FOCUS", active.Id, "stale heartbeat -> INTERRUPTED; offline wall time excluded");
        }

        private static ProductivityFocusSession ActiveFocus(ProductivityStore store) { return store.FocusSessions.OrderByDescending(x => ParseUtc(x.StartedUtc)).FirstOrDefault(x => String.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)); }

        private static void CreateTaskInteractive(Window owner, DeepSystemsContext ctx, string missionId, string parentTaskId)
        {
            string title = Prompt(owner, "NEW TASK", "Task title", ""); if (String.IsNullOrWhiteSpace(title)) return; string detail = Prompt(owner, "TASK DETAIL", "What does done mean?", ""); ProductivityTask t = NewTask(missionId, title, "TASK", 3, 3, 2); t.Detail = detail; t.ParentTaskId = parentTaskId ?? ""; ProductivityStore store = Load(ctx); store.Tasks.Add(t); store.CurrentMissionId = missionId; store.CurrentTaskId = t.Id; TouchMission(store, missionId); Save(ctx, store); Ledger(ctx, "TASK_CREATE", "TASK", t.Id, "mission=" + missionId + " parent=" + parentTaskId); OpenTask(owner, ctx, t.Id);
        }

        private static void CreateChildMissionInteractive(Window owner, DeepSystemsContext ctx, ProductivityMission parent)
        {
            string title = Prompt(owner, "CHILD MISSION", "Mission title", ""); if (String.IsNullOrWhiteSpace(title)) return; string objective = Prompt(owner, "CHILD OBJECTIVE", "What does this branch accomplish?", ""); ProductivityMission m = NewMission(title, objective, parent); ProductivityStore store = Load(ctx); store.Missions.Add(m); TouchMission(store, parent.Id); Save(ctx, store); Ledger(ctx, "MISSION_BRANCH", "MISSION", m.Id, "parent=" + parent.Id); OpenMission(owner, ctx, m.Id);
        }

        private static void AddDependencyInteractive(Window owner, DeepSystemsContext ctx, string taskId)
        {
            ProductivityStore store = Load(ctx); ProductivityTask target = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (target == null) return; Window w = W(owner, "YOMI · SELECT DEPENDENCY", 980, 680); DockPanel root = new DockPanel { Margin = new Thickness(18) }; TextBlock h = T("SELECT A TASK THAT MUST COMPLETE FIRST", 22, Text, FontWeights.Bold); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); List<ProductivityTask> options = store.Tasks.Where(x => x.Id != target.Id && !target.DependsOn.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).OrderBy(x => x.MissionId).ThenBy(x => x.Title).ToList(); foreach (ProductivityTask t in options) list.Items.Add(t); list.MouseDoubleClick += delegate { ProductivityTask dep = list.SelectedItem as ProductivityTask; if (dep == null) return; ProductivityStore s = Load(ctx); ProductivityTask tt = s.Tasks.FirstOrDefault(x => x.Id == taskId); if (tt == null) return; tt.DependsOn.Add(dep.Id); Touch(tt); if (DetectCycleFrom(s, tt.Id)) { tt.DependsOn.Remove(dep.Id); MessageBox.Show(w, "Dependency rejected because it would create a cycle.", "YOMI · WORK GRAPH", MessageBoxButton.OK, MessageBoxImage.Warning); return; } Save(ctx, s); Ledger(ctx, "TASK_DEPENDENCY_ADD", "TASK", tt.Id, "depends_on=" + dep.Id); w.Close(); OpenTask(owner, ctx, tt.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void AttachTaskEvidence(Window owner, DeepSystemsContext ctx, string taskId)
        {
            string path = Prompt(owner, "ATTACH EVIDENCE", "Exact YOMI evidence file path", ""); if (String.IsNullOrWhiteSpace(path)) return; ProductivityStore store = Load(ctx); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (t == null) return; if (!t.EvidencePaths.Contains(path, StringComparer.OrdinalIgnoreCase)) t.EvidencePaths.Add(path); Touch(t); Save(ctx, store); Ledger(ctx, "TASK_EVIDENCE_ATTACH", "TASK", t.Id, Trunc(path, 220));
        }

        private static void AttachMissionEvidence(Window owner, DeepSystemsContext ctx, string missionId)
        {
            string path = Prompt(owner, "ATTACH EVIDENCE", "Exact YOMI evidence file path", ""); if (String.IsNullOrWhiteSpace(path)) return; ProductivityStore store = Load(ctx); ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == missionId); if (m == null) return; if (!m.EvidencePaths.Contains(path, StringComparer.OrdinalIgnoreCase)) m.EvidencePaths.Add(path); Touch(m); Save(ctx, store); Ledger(ctx, "MISSION_EVIDENCE_ATTACH", "MISSION", m.Id, Trunc(path, 220));
        }

        private static void QuickCapture(Window owner, DeepSystemsContext ctx, string missionId) { QuickCapture(owner, ctx, missionId, "THOUGHT"); }
        private static void QuickCapture(Window owner, DeepSystemsContext ctx, string missionId, string kind)
        {
            string text = Prompt(owner, "CAPTURE " + kind, "Capture it before it evaporates", ""); if (String.IsNullOrWhiteSpace(text)) return; ProductivityStore store = Load(ctx); ProductivityCapture c = new ProductivityCapture { Id = Id("capture"), Kind = kind ?? "THOUGHT", Text = text.Trim(), CreatedUtc = Now(), MissionId = missionId ?? "", Processed = false }; store.Captures.Add(c); if (store.Captures.Count > 512) store.Captures = store.Captures.Skip(store.Captures.Count - 512).ToList(); Save(ctx, store); Ledger(ctx, "CAPTURE", "CAPTURE", c.Id, c.Kind + " " + Trunc(c.Text, 160));
        }

        private static void PromoteCaptureToTask(Window owner, DeepSystemsContext ctx, string captureId)
        {
            ProductivityStore store = Load(ctx); ProductivityCapture c = store.Captures.FirstOrDefault(x => x.Id == captureId); if (c == null) return; string missionId = c.MissionId; if (String.IsNullOrWhiteSpace(missionId) || !store.Missions.Any(x => x.Id == missionId)) missionId = store.CurrentMissionId; if (String.IsNullOrWhiteSpace(missionId) || !store.Missions.Any(x => x.Id == missionId)) { MessageBox.Show(owner, "Create or select a mission first.", "YOMI · PRODUCTIVITY", MessageBoxButton.OK, MessageBoxImage.Information); return; } ProductivityTask t = NewTask(missionId, c.Text, String.Equals(c.Kind, "QUESTION", StringComparison.OrdinalIgnoreCase) ? "QUESTION" : "TASK", 3, 3, 2); store.Tasks.Add(t); c.Processed = true; c.ProcessedUtc = Now(); store.CurrentTaskId = t.Id; TouchMission(store, missionId); Save(ctx, store); Ledger(ctx, "CAPTURE_PROMOTE_TASK", "TASK", t.Id, "capture=" + c.Id); OpenTask(owner, ctx, t.Id);
        }

        private static void PromoteCaptureToMission(Window owner, DeepSystemsContext ctx, string captureId)
        {
            ProductivityStore store = Load(ctx); ProductivityCapture c = store.Captures.FirstOrDefault(x => x.Id == captureId); if (c == null) return; ProductivityMission m = NewMission(c.Text, "Clarify and complete the captured objective.", null); store.Missions.Add(m); c.Processed = true; c.ProcessedUtc = Now(); store.CurrentMissionId = m.Id; Save(ctx, store); Ledger(ctx, "CAPTURE_PROMOTE_MISSION", "MISSION", m.Id, "capture=" + c.Id); OpenMission(owner, ctx, m.Id);
        }

        private static void MarkCaptureProcessed(DeepSystemsContext ctx, string captureId)
        {
            ProductivityStore store = Load(ctx); ProductivityCapture c = store.Captures.FirstOrDefault(x => x.Id == captureId); if (c == null) return; c.Processed = true; c.ProcessedUtc = Now(); Save(ctx, store); Ledger(ctx, "CAPTURE_PROCESSED", "CAPTURE", c.Id, "manual");
        }

        private static void SaveResumeCapsuleInteractive(Window owner, DeepSystemsContext ctx, string missionId)
        {
            ProductivityStore store = Load(ctx); ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == missionId); if (m == null) return; string summary = Prompt(owner, "RESUME CAPSULE", "What were you doing / what matters when you return?", m.ResumeSummary ?? ""); if (summary == null) return; string next = Prompt(owner, "RESUME NEXT", "What is the first concrete move when you return?", m.ResumeNext ?? ""); m.ResumeSummary = summary; m.ResumeNext = next; m.ResumeSavedUtc = Now(); Touch(m); Save(ctx, store); Ledger(ctx, "RESUME_CAPSULE_SAVE", "MISSION", m.Id, Trunc(summary, 160));
        }

        private static void SetMissionStatus(DeepSystemsContext ctx, string missionId, string status)
        {
            ProductivityStore store = Load(ctx); ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == missionId); if (m == null) return; m.Status = status; Touch(m); Save(ctx, store); Ledger(ctx, "MISSION_STATUS", "MISSION", m.Id, status);
        }

        private static void SetTaskStatus(DeepSystemsContext ctx, string taskId, string status)
        {
            ProductivityStore store = Load(ctx); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (t == null) return; t.Status = status; Touch(t); if (String.Equals(status, "DONE", StringComparison.OrdinalIgnoreCase)) t.CompletedUtc = Now(); else t.CompletedUtc = ""; TouchMission(store, t.MissionId); Save(ctx, store); Ledger(ctx, "TASK_STATUS", "TASK", t.Id, status);
        }

        private static void AddTaskNote(DeepSystemsContext ctx, string taskId, string text)
        {
            string v = (text ?? "").Trim(); if (v.Length == 0) return; ProductivityStore store = Load(ctx); ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == taskId); if (t == null) return; t.Notes.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " · " + v); if (t.Notes.Count > 128) t.Notes = t.Notes.Skip(t.Notes.Count - 128).ToList(); Touch(t); TouchMission(store, t.MissionId); Save(ctx, store); Ledger(ctx, "TASK_NOTE", "TASK", t.Id, Trunc(v, 160));
        }

        private static ProductivityMission NewMission(string title, string objective, ProductivityMission parent)
        {
            string now = Now(); ProductivityMission m = new ProductivityMission { Id = Id("mission"), ParentId = parent == null ? "" : parent.Id, Title = title == null ? "Untitled mission" : title.Trim(), Objective = objective == null ? "" : objective.Trim(), Status = "ACTIVE", Priority = 3, CreatedUtc = now, UpdatedUtc = now, LastOpenedUtc = now }; m.RootId = parent == null ? m.Id : (String.IsNullOrWhiteSpace(parent.RootId) ? parent.Id : parent.RootId); return m;
        }

        private static ProductivityTask NewTask(string missionId, string title, string kind, int priority, int value, int effort)
        {
            string now = Now(); return new ProductivityTask { Id = Id("task"), MissionId = missionId ?? "", ParentTaskId = "", Title = title == null ? "Untitled task" : title.Trim(), Detail = "", Kind = kind ?? "TASK", Status = "TODO", Priority = Clamp(priority, 1, 5), Value = Clamp(value, 1, 5), Effort = Clamp(effort, 1, 5), CreatedUtc = now, UpdatedUtc = now, CompletedUtc = "" };
        }

        private static bool IsDone(ProductivityTask t) { return t != null && String.Equals(t.Status, "DONE", StringComparison.OrdinalIgnoreCase); }
        private static void Touch(ProductivityTask t) { if (t != null) t.UpdatedUtc = Now(); }
        private static void Touch(ProductivityMission m) { if (m != null) m.UpdatedUtc = Now(); }
        private static void TouchMission(ProductivityStore store, string id) { ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == id); Touch(m); }
        private static int Clamp(int x, int min, int max) { return Math.Max(min, Math.Min(max, x)); }
        private static string BestTaskId(ProductivityStore store, string missionId) { ProductivityRecommendation r = Recommendations(store, missionId).FirstOrDefault(x => x.Executable); return r == null || r.Task == null ? null : r.Task.Id; }

        private static int MissionDepth(ProductivityStore store, ProductivityMission m)
        {
            int d = 0; HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string p = m == null ? "" : m.ParentId; while (!String.IsNullOrWhiteSpace(p) && d < 24 && seen.Add(p)) { d++; ProductivityMission pm = store.Missions.FirstOrDefault(x => x.Id == p); p = pm == null ? "" : pm.ParentId; } return d;
        }

        private static int TaskDepth(ProductivityStore store, ProductivityTask t)
        {
            int d = 0; HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string p = t == null ? "" : t.ParentTaskId; while (!String.IsNullOrWhiteSpace(p) && d < 24 && seen.Add(p)) { d++; ProductivityTask pt = store.Tasks.FirstOrDefault(x => x.Id == p); p = pt == null ? "" : pt.ParentTaskId; } return d;
        }

        private sealed class ResearchCaseLite
        {
            public string Id; public string Title; public string Question; public string Hypothesis; public string UpdatedUtc;
            public override string ToString() { return (UpdatedUtc ?? "") + "   ·   " + (Title ?? Id ?? "case") + "   ·   " + (Id ?? ""); }
        }

        private static List<ResearchCaseLite> ReadResearchCases(DeepSystemsContext ctx)
        {
            List<ResearchCaseLite> result = new List<ResearchCaseLite>(); string path = Path.Combine(ctx.StateRoot, "controller-research-workbench.json"); if (!File.Exists(path)) return result; try { Dictionary<string, object> root = Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>; if (root == null) return result; object raw; if (!root.TryGetValue("sessions", out raw)) return result; IEnumerable arr = raw as IEnumerable; if (arr == null) return result; foreach (object item in arr) { Dictionary<string, object> m = item as Dictionary<string, object>; if (m == null) continue; ResearchCaseLite rc = new ResearchCaseLite { Id = S(m, "id"), Title = S(m, "title"), Question = S(m, "question"), Hypothesis = S(m, "working_hypothesis"), UpdatedUtc = S(m, "updated_utc") }; if (!String.IsNullOrWhiteSpace(rc.Id)) result.Add(rc); } } catch { } return result;
        }

        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-productivity-fabric.json"); }
        private static string LedgerPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-productivity-ledger.jsonl"); }

        private static ProductivityStore Load(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                ProductivityStore store = new ProductivityStore(); string path = StorePath(ctx); if (!File.Exists(path)) return store;
                try
                {
                    Dictionary<string, object> root = Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>; if (root == null) return store; store.Schema = I(root, "schema", 1); store.CurrentMissionId = S(root, "current_mission_id"); store.CurrentTaskId = S(root, "current_task_id");
                    foreach (Dictionary<string, object> x in Maps(root, "missions")) { ProductivityMission m = new ProductivityMission { Id = S(x, "id"), ParentId = S(x, "parent_id"), RootId = S(x, "root_id"), Title = S(x, "title"), Objective = S(x, "objective"), Status = S(x, "status"), Priority = I(x, "priority", 3), ResearchCaseId = S(x, "research_case_id"), CreatedUtc = S(x, "created_utc"), UpdatedUtc = S(x, "updated_utc"), LastOpenedUtc = S(x, "last_opened_utc"), ResumeSummary = S(x, "resume_summary"), ResumeNext = S(x, "resume_next"), ResumeSavedUtc = S(x, "resume_saved_utc"), Tags = Strings(x, "tags"), EvidencePaths = Strings(x, "evidence_paths"), Notes = Strings(x, "notes") }; if (!String.IsNullOrWhiteSpace(m.Id)) store.Missions.Add(m); }
                    foreach (Dictionary<string, object> x in Maps(root, "tasks")) { ProductivityTask t = new ProductivityTask { Id = S(x, "id"), MissionId = S(x, "mission_id"), ParentTaskId = S(x, "parent_task_id"), Title = S(x, "title"), Detail = S(x, "detail"), Kind = S(x, "kind"), Status = S(x, "status"), Priority = I(x, "priority", 3), Value = I(x, "value", 3), Effort = I(x, "effort", 2), ResearchCaseId = S(x, "research_case_id"), CreatedUtc = S(x, "created_utc"), UpdatedUtc = S(x, "updated_utc"), CompletedUtc = S(x, "completed_utc"), DependsOn = Strings(x, "depends_on"), EvidencePaths = Strings(x, "evidence_paths"), Notes = Strings(x, "notes") }; if (!String.IsNullOrWhiteSpace(t.Id)) store.Tasks.Add(t); }
                    foreach (Dictionary<string, object> x in Maps(root, "captures")) { ProductivityCapture c = new ProductivityCapture { Id = S(x, "id"), Kind = S(x, "kind"), Text = S(x, "text"), CreatedUtc = S(x, "created_utc"), MissionId = S(x, "mission_id"), Processed = Bool(x, "processed"), ProcessedUtc = S(x, "processed_utc") }; if (!String.IsNullOrWhiteSpace(c.Id)) store.Captures.Add(c); }
                    foreach (Dictionary<string, object> x in Maps(root, "focus_sessions")) { ProductivityFocusSession f = new ProductivityFocusSession { Id = S(x, "id"), MissionId = S(x, "mission_id"), TaskId = S(x, "task_id"), Status = S(x, "status"), StartedUtc = S(x, "started_utc"), LastHeartbeatUtc = S(x, "last_heartbeat_utc"), EndedUtc = S(x, "ended_utc"), AccumulatedSeconds = L(x, "accumulated_seconds", 0), ExitNote = S(x, "exit_note") }; if (!String.IsNullOrWhiteSpace(f.Id)) store.FocusSessions.Add(f); }
                }
                catch
                {
                    PreserveCorruptStore(path);
                    return new ProductivityStore();
                }
                return store;
            }
        }

        private static void Save(DeepSystemsContext ctx, ProductivityStore store)
        {
            lock (Gate)
            {
                Directory.CreateDirectory(ctx.StateRoot); Dictionary<string, object> root = new Dictionary<string, object>(); root["schema"] = 1; root["updated_utc"] = Now(); root["current_mission_id"] = store.CurrentMissionId ?? ""; root["current_task_id"] = store.CurrentTaskId ?? "";
                root["missions"] = store.Missions.Take(128).Select(MissionMap).ToArray(); root["tasks"] = store.Tasks.Take(2048).Select(TaskMap).ToArray(); root["captures"] = store.Captures.TakeLastCompat(512).Select(CaptureMap).ToArray(); root["focus_sessions"] = store.FocusSessions.TakeLastCompat(256).Select(FocusMap).ToArray();
                AtomicWrite(StorePath(ctx), Json.Serialize(root));
            }
        }

        private static Dictionary<string, object> MissionMap(ProductivityMission m) { return new Dictionary<string, object> { { "id", m.Id }, { "parent_id", m.ParentId }, { "root_id", m.RootId }, { "title", m.Title }, { "objective", m.Objective }, { "status", m.Status }, { "priority", m.Priority }, { "research_case_id", m.ResearchCaseId }, { "created_utc", m.CreatedUtc }, { "updated_utc", m.UpdatedUtc }, { "last_opened_utc", m.LastOpenedUtc }, { "resume_summary", m.ResumeSummary }, { "resume_next", m.ResumeNext }, { "resume_saved_utc", m.ResumeSavedUtc }, { "tags", m.Tags.Take(48).ToArray() }, { "evidence_paths", m.EvidencePaths.Take(96).ToArray() }, { "notes", m.Notes.TakeLastCompat(128).ToArray() } }; }
        private static Dictionary<string, object> MissionMap(ProductivityStore store, string id) { ProductivityMission m = store.Missions.FirstOrDefault(x => x.Id == id); return m == null ? new Dictionary<string, object>() : MissionMap(m); }
        private static Dictionary<string, object> TaskMap(ProductivityTask t) { return new Dictionary<string, object> { { "id", t.Id }, { "mission_id", t.MissionId }, { "parent_task_id", t.ParentTaskId }, { "title", t.Title }, { "detail", t.Detail }, { "kind", t.Kind }, { "status", t.Status }, { "priority", t.Priority }, { "value", t.Value }, { "effort", t.Effort }, { "research_case_id", t.ResearchCaseId }, { "created_utc", t.CreatedUtc }, { "updated_utc", t.UpdatedUtc }, { "completed_utc", t.CompletedUtc }, { "depends_on", t.DependsOn.Take(64).ToArray() }, { "evidence_paths", t.EvidencePaths.Take(96).ToArray() }, { "notes", t.Notes.TakeLastCompat(128).ToArray() } }; }
        private static Dictionary<string, object> TaskMap(ProductivityStore store, string id) { ProductivityTask t = store.Tasks.FirstOrDefault(x => x.Id == id); return t == null ? new Dictionary<string, object>() : TaskMap(t); }
        private static Dictionary<string, object> CaptureMap(ProductivityCapture c) { return new Dictionary<string, object> { { "id", c.Id }, { "kind", c.Kind }, { "text", c.Text }, { "created_utc", c.CreatedUtc }, { "mission_id", c.MissionId }, { "processed", c.Processed }, { "processed_utc", c.ProcessedUtc } }; }
        private static Dictionary<string, object> FocusMap(ProductivityFocusSession f) { return new Dictionary<string, object> { { "id", f.Id }, { "mission_id", f.MissionId }, { "task_id", f.TaskId }, { "status", f.Status }, { "started_utc", f.StartedUtc }, { "last_heartbeat_utc", f.LastHeartbeatUtc }, { "ended_utc", f.EndedUtc }, { "accumulated_seconds", f.AccumulatedSeconds }, { "exit_note", f.ExitNote } }; }

        private static void Ledger(DeepSystemsContext ctx, string action, string objectType, string objectId, string detail)
        {
            try
            {
                Directory.CreateDirectory(ctx.StateRoot); List<ProductivityLedgerRecord> records = ReadLedger(ctx); ProductivityLedgerRecord prev = records.OrderByDescending(x => x.Seq).FirstOrDefault(); ProductivityLedgerRecord r = new ProductivityLedgerRecord { Seq = prev == null ? 1 : prev.Seq + 1, Utc = Now(), Action = action ?? "", ObjectType = objectType ?? "", ObjectId = objectId ?? "", Detail = detail ?? "", PrevHash = prev == null ? new string('0', 64) : prev.EntryHash ?? new string('0', 64) }; r.EntryHash = Hash(LedgerCanonical(r)); File.AppendAllText(LedgerPath(ctx), Json.Serialize(LedgerMap(r)) + Environment.NewLine, new UTF8Encoding(false));
            }
            catch { }
        }

        private static List<ProductivityLedgerRecord> ReadLedger(DeepSystemsContext ctx)
        {
            List<ProductivityLedgerRecord> result = new List<ProductivityLedgerRecord>(); string path = LedgerPath(ctx); if (!File.Exists(path)) return result; try { foreach (string line in File.ReadLines(path, Encoding.UTF8).TakeLastCompat(2048)) { if (String.IsNullOrWhiteSpace(line)) continue; Dictionary<string, object> m = Json.DeserializeObject(line) as Dictionary<string, object>; if (m == null) continue; ProductivityLedgerRecord r = new ProductivityLedgerRecord { Seq = L(m, "seq", 0), Utc = S(m, "utc"), Action = S(m, "action"), ObjectType = S(m, "object_type"), ObjectId = S(m, "object_id"), Detail = S(m, "detail"), PrevHash = S(m, "prev_hash"), EntryHash = S(m, "entry_hash") }; result.Add(r); } } catch { } return result;
        }

        private static string VerifyLedger(DeepSystemsContext ctx)
        {
            List<ProductivityLedgerRecord> rs = ReadLedger(ctx).OrderBy(x => x.Seq).ToList();
            if (rs.Count == 0) return "VALID · empty ledger";
            string prev = rs[0].PrevHash ?? new string('0', 64);
            long expected = rs[0].Seq;
            foreach (ProductivityLedgerRecord r in rs) { if (r.Seq != expected) return "INVALID · sequence discontinuity near " + r.Seq; if (!String.Equals(r.PrevHash, prev, StringComparison.OrdinalIgnoreCase)) return "INVALID · ancestry mismatch near " + r.Seq; string h = Hash(LedgerCanonical(r)); if (!String.Equals(h, r.EntryHash, StringComparison.OrdinalIgnoreCase)) return "INVALID · content hash mismatch near " + r.Seq; prev = r.EntryHash; expected++; } return "VALID · " + rs.Count.ToString(CultureInfo.InvariantCulture) + " bounded tail records verify from durable anchor";
        }

        private static string LedgerCanonical(ProductivityLedgerRecord r) { return r.Seq.ToString(CultureInfo.InvariantCulture) + "\n" + (r.Utc ?? "") + "\n" + (r.Action ?? "") + "\n" + (r.ObjectType ?? "") + "\n" + (r.ObjectId ?? "") + "\n" + (r.Detail ?? "") + "\n" + (r.PrevHash ?? ""); }
        private static Dictionary<string, object> LedgerMap(ProductivityLedgerRecord r) { return new Dictionary<string, object> { { "seq", r.Seq }, { "utc", r.Utc }, { "action", r.Action }, { "object_type", r.ObjectType }, { "object_id", r.ObjectId }, { "detail", r.Detail }, { "prev_hash", r.PrevHash }, { "entry_hash", r.EntryHash } }; }

        private static void AtomicWrite(string path, string text)
        {
            string tmp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text, new UTF8Encoding(false)); if (File.Exists(path)) { string bak = path + ".bak"; try { File.Replace(tmp, path, bak, true); try { File.Delete(bak); } catch { } } catch { File.Copy(tmp, path, true); File.Delete(tmp); } } else File.Move(tmp, path);
        }

        private static void PreserveCorruptStore(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                byte[] bytes = File.ReadAllBytes(path);
                string digest; using (SHA256 sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                string copy = path + ".corrupt." + digest.Substring(0, 12) + ".json";
                if (!File.Exists(copy)) File.WriteAllBytes(copy, bytes);
            }
            catch { }
        }

        private static IEnumerable<Dictionary<string, object>> Maps(Dictionary<string, object> root, string key)
        {
            object raw; if (root == null || !root.TryGetValue(key, out raw)) yield break; IEnumerable arr = raw as IEnumerable; if (arr == null || raw is string) yield break; foreach (object o in arr) { Dictionary<string, object> m = o as Dictionary<string, object>; if (m != null) yield return m; }
        }

        private static List<string> Strings(Dictionary<string, object> m, string key)
        {
            List<string> r = new List<string>(); object raw; if (m == null || !m.TryGetValue(key, out raw) || raw == null || raw is string) return r; IEnumerable arr = raw as IEnumerable; if (arr == null) return r; foreach (object o in arr) if (o != null && !String.IsNullOrWhiteSpace(Convert.ToString(o, CultureInfo.InvariantCulture))) r.Add(Convert.ToString(o, CultureInfo.InvariantCulture)); return r;
        }

        private static string S(Dictionary<string, object> m, string key) { object v; if (m != null && m.TryGetValue(key, out v) && v != null) return Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""; return ""; }
        private static int I(Dictionary<string, object> m, string key, int fallback) { object v; int n; if (m != null && m.TryGetValue(key, out v) && v != null && Int32.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n; return fallback; }
        private static long L(Dictionary<string, object> m, string key, long fallback) { object v; long n; if (m != null && m.TryGetValue(key, out v) && v != null && Int64.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n; return fallback; }
        private static bool Bool(Dictionary<string, object> m, string key) { object v; bool b; if (m != null && m.TryGetValue(key, out v) && v != null && Boolean.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out b)) return b; return false; }
        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static DateTime ParseUtc(string s) { DateTime d; return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out d) ? d.ToUniversalTime() : DateTime.MinValue; }
        private static string Id(string prefix) { return (prefix ?? "id") + "-" + Guid.NewGuid().ToString("N").Substring(0, 12); }
        private static string Trunc(string s, int max) { if (String.IsNullOrEmpty(s)) return ""; return s.Length <= max ? s : s.Substring(0, max) + "…"; }
        private static string Hash(string s) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""))).Replace("-", "").ToLowerInvariant(); }
        private static string FormatSeconds(long seconds) { if (seconds < 0) seconds = 0; TimeSpan ts = TimeSpan.FromSeconds(seconds); return ((int)ts.TotalHours).ToString("00", CultureInfo.InvariantCulture) + ":" + ts.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + ts.Seconds.ToString("00", CultureInfo.InvariantCulture); }

        private static Window W(Window owner, string title, double width, double height)
        {
            Window w = new Window { Title = title, Width = width, Height = height, MinWidth = 760, MinHeight = 520, Background = Bg, Foreground = Text, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner, FontFamily = new FontFamily("Segoe UI") }; return w;
        }

        private static TextBlock T(string text, double size, Brush color, FontWeight weight) { return new TextBlock { Text = text ?? "", FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap }; }
        private static Border Card(UIElement child) { return new Border { Child = child, Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(12), Margin = new Thickness(0, 6, 0, 6) }; }
        private static Button Btn(string text, RoutedEventHandler click) { Button b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(11, 7, 11, 7), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; b.Click += click; return b; }
        private static ListBox List() { return new ListBox { Margin = new Thickness(0, 10, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), Padding = new Thickness(5) }; }
        private static void Portal(Panel panel, string badge, string title, string subtitle, Action action)
        {
            StackPanel s = new StackPanel(); TextBlock b = T(badge, 10.5, Accent, FontWeights.Bold); TextBlock t = T(title, 18, Text, FontWeights.SemiBold); TextBlock d = T(subtitle, 12, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(b); s.Children.Add(t); s.Children.Add(d); Border c = new Border { Child = s, Width = 348, MinHeight = 132, Margin = new Thickness(0, 0, 12, 12), Padding = new Thickness(14), Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Cursor = Cursors.Hand }; c.MouseLeftButtonUp += delegate { if (action != null) action(); }; panel.Children.Add(c);
        }

        private static string Prompt(Window owner, string title, string label, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 620, 280); w.ResizeMode = ResizeMode.NoResize; StackPanel p = new StackPanel { Margin = new Thickness(18) }; p.Children.Add(T(label, 13, Muted, FontWeights.Normal)); TextBox box = new TextBox { Text = initial ?? "", MinHeight = 78, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(9), Background = Raised, Foreground = Text, BorderBrush = Border }; p.Children.Add(box); string result = null; WrapPanel buttons = new WrapPanel(); Button ok = Btn("COMMIT", delegate { result = box.Text; w.DialogResult = true; w.Close(); }); Button cancel = Btn("CANCEL", delegate { w.DialogResult = false; w.Close(); }); buttons.Children.Add(ok); buttons.Children.Add(cancel); p.Children.Add(buttons); w.Content = p; box.Focus(); w.ShowDialog(); return result;
        }
    }

    internal static class ProductivityEnumerableCompat
    {
        public static IEnumerable<T> TakeLastCompat<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) yield break; Queue<T> q = new Queue<T>(); foreach (T item in source) { q.Enqueue(item); if (q.Count > count) q.Dequeue(); } foreach (T item in q) yield return item;
        }
    }
}

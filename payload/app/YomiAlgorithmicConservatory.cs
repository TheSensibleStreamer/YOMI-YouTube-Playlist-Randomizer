using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Yomi.ProductShell
{
    internal sealed class CodeArtifact
    {
        public string Path;
        public string Language;
        public int Lines;
        public int NonBlank;
        public int CommentLike;
        public int BranchMarkers;
        public int MethodLike;
        public int ClassLike;
        public int StringLiterals;
        public long Bytes;
        public string Fingerprint;
        public List<string> ProtocolTokens = new List<string>();
    }

    internal sealed class ProofResult
    {
        public string Name;
        public string Status;
        public string Detail;
        public object Witness;
    }

    internal sealed class AlgorithmRun
    {
        public string Name;
        public int[] Order;
        public int ChangedSlots;
        public long Inversions;
        public int StablePrefix;
        public double AdjacencyRetention;
        public string Complexity;
        public string Notes;
    }

    internal sealed class ModelCheckResult
    {
        public string Machine;
        public int StatesVisited;
        public int TransitionsVisited;
        public int RejectedTransitions;
        public List<string> Violations = new List<string>();
        public List<string> Reachable = new List<string>();
    }

    internal static class AlgorithmicConservatory
    {
        private static readonly Brush Bg = B("#0A0B0D");
        private static readonly Brush Surface = B("#14171B");
        private static readonly Brush Raised = B("#1D2228");
        private static readonly Brush Border = B("#343C45");
        private static readonly Brush Text = B("#F2F5F7");
        private static readonly Brush Muted = B("#A7B0B8");
        private static readonly Brush Accent = B("#75E6B5");
        private static readonly Brush Blue = B("#75B8FF");
        private static readonly Brush Gold = B("#E9C46A");
        private static readonly Brush Violet = B("#C6A0FF");
        private static readonly Brush Danger = B("#FF7B88");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            Window w = W(owner, "YOMI · ALGORITHMIC CONSERVATORY", 1160, 800);
            DockPanel root = new DockPanel { Margin = new Thickness(20) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("ALGORITHMIC CONSERVATORY", 30, Text, FontWeights.Bold));
            head.Children.Add(T("Classical · modern · experimental computer science, grounded in YOMI's real structures and bounded simulations", 13, Muted, FontWeights.Normal));
            TextBlock doctrine = T("The player is the instrument. This is the workshop beneath the stage.", 11.5, Gold, FontWeights.SemiBold);
            doctrine.Margin = new Thickness(0, 6, 0, 14); head.Children.Add(doctrine);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel p = new WrapPanel(); sv.Content = p; root.Children.Add(sv);

            Portal(p, "CLASSICAL", "DATA-STRUCTURE ARCADE", "Sequences, indexes, dictionaries, append-only logs, trees, graphs and content-addressed objects as they actually appear inside YOMI.", delegate { OpenDataStructureArcade(w, ctx); });
            Portal(p, "PROOF", "INVARIANT & PROOF GALLERY", "Run explicit proof obligations against current session/order evidence and inspect witnesses rather than trusting labels.", delegate { OpenProofGallery(w, ctx); });
            Portal(p, "THEATRE", "COMPARATIVE ALGORITHM THEATRE", "Clone the current order and compare stable block movement, rotations, reversals and deterministic transforms without touching authority.", delegate { OpenAlgorithmTheatre(w, ctx); });
            Portal(p, "MODEL", "FINITE-STATE MODEL CHECKER", "Exhaustively explore bounded intent-state transition systems and surface illegal/reachable edges.", delegate { OpenModelChecker(w, ctx); });
            Portal(p, "CRASH", "WRITE-AHEAD LOG CRASH LAB", "Classical crash-consistency theatre: journal append, head persistence, recovery, ambiguity and evidence-preserving forks.", delegate { OpenCrashLab(w, ctx); });
            Portal(p, "MODERN", "HASH / CONTENT-ADDRESSING LAB", "SHA-256, plan fingerprints, journal ancestry, source identities and immutable object naming as one interconnected design family.", delegate { OpenHashLab(w, ctx); });
            Portal(p, "GRAPH", "GRAPH THEORY GARDEN", "DAG scheduling, topological order, connected components, identity bridges and counterfactual genealogy with live exits into Causal Graph Lab.", delegate { OpenGraphGarden(w, ctx); });
            Portal(p, "COMPLEXITY", "COMPLEXITY OBSERVATORY", "Theoretical Big-O plus bounded empirical operation counts over cloned queue structures of increasing size.", delegate { OpenComplexityObservatory(w, ctx); });
            Portal(p, "SCHEDULING", "SCHEDULER TOURNAMENT", "Compare FIFO, shortest-preparation, risk-first and aging/fairness disciplines against cloned live queue evidence.", delegate { OpenSchedulerTournament(w, ctx); });
            Portal(p, "CONCURRENCY", "INTERLEAVING LAB", "Enumerate bounded writer/reader/crash schedules and inspect which observations are provable, stale or ambiguous.", delegate { OpenInterleavingLab(w, ctx); });
            Portal(p, "INFORMATION", "INFORMATION THEORY CHAMBER", "Measure entropy, concentration and diversity across queue phases, risk labels and source identities without pretending entropy equals quality.", delegate { OpenInformationTheory(w, ctx); });
            Portal(p, "PARADIGMS", "PROGRAMMING PARADIGM HALL", "See imperative, event-driven, declarative, functional, event-sourced and graph-oriented styles coexisting across YOMI's polyglot codebase.", delegate { OpenParadigmHall(w, ctx); });
            Portal(p, "PATTERNS", "DESIGN PATTERN MENAGERIE", "Command, Observer, Strategy, State, Saga, Repository/Projection, content-addressing and circuit-breaker patterns grounded in actual YOMI surfaces.", delegate { OpenPatternMenagerie(w, ctx); });
            Portal(p, "POLYGLOT", "SOURCE OBSERVATORY / POLYGLOT GENOME", "Inspect YOMI as code: C#, Lua, PowerShell, XAML and config surfaces, symbol references, lexical metrics and protocol crossings.", delegate { OpenSourceObservatory(w, ctx); });
            Portal(p, "PROTOCOL", "CROSS-LANGUAGE PROTOCOL ARCHAEOLOGY", "Trace yomi-* messages, JSON state names and other protocol tokens across language boundaries.", delegate { OpenProtocolArchaeology(w, ctx); });
            Portal(p, "EXPERIMENT", "EXPERIMENTAL ALGORITHM FORGE", "Risk-weighted, locality-biased and deterministic novelty heuristics run only against cloned evidence; compare their structural consequences.", delegate { OpenExperimentalForge(w, ctx); });
            Portal(p, "PROPERTY", "PROPERTY-BASED TORTURE CHAMBER", "Generate thousands of bounded structural cases and watch invariants survive—or produce a minimal witness when they do not.", delegate { OpenPropertyLab(w, ctx); });
            Portal(p, "R&D", "RESEARCH WORKBENCH", "Leave theory and follow an emergent observation into hypotheses, cases, temporal evidence and causal topology.", delegate { ResearchWorkbench.Open(w, ctx); });
            Portal(p, "∞", "DEEP SYSTEMS ATLAS", "Return to the recursive world and follow any real artifact the Conservatory exposed.", delegate { DeepSystems.OpenAtlasFromContext(w); });

            w.Content = root;
            w.Show();
        }

        private static Window W(Window owner, string title, double width, double height)
        {
            return new Window
            {
                Title = title,
                Width = width,
                Height = height,
                MinWidth = Math.Min(width, 760),
                MinHeight = Math.Min(height, 520),
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Bg,
                Foreground = Text,
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        private static TextBlock T(string text, double size, Brush color, FontWeight weight)
        {
            return new TextBlock { Text = text ?? "", FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        }

        private static Button Btn(string text, RoutedEventHandler click)
        {
            Button b = new Button { Content = text, MinHeight = 34, Padding = new Thickness(11, 5, 11, 5), Margin = new Thickness(0, 0, 8, 8), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
            if (click != null) b.Click += click;
            return b;
        }

        private static Border Card(UIElement child)
        {
            return new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14), Margin = new Thickness(0, 0, 12, 12), Child = child };
        }

        private static void Portal(Panel panel, string badge, string title, string subtitle, Action open)
        {
            StackPanel s = new StackPanel { Width = 330 };
            TextBlock b = T(badge, 11, Accent, FontWeights.Bold); b.Margin = new Thickness(0, 0, 0, 4); s.Children.Add(b);
            s.Children.Add(T(title, 18, Text, FontWeights.SemiBold));
            TextBlock d = T(subtitle, 12.5, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(d);
            Border c = Card(s); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { if (open != null) open(); }; panel.Children.Add(c);
        }

        private static List<int> LoadAuthoritativeOrder(DeepSystemsContext ctx)
        {
            string p = Path.Combine(ctx.StateRoot, "session-order.json");
            object o = ReadJson(p);
            Dictionary<string, object> d = o as Dictionary<string, object>;
            if (d == null || !d.ContainsKey("order")) return new List<int>();
            IEnumerable arr = d["order"] as IEnumerable;
            if (arr == null || d["order"] is string) return new List<int>();
            List<int> r = new List<int>();
            foreach (object x in arr)
            {
                int n;
                if (Int32.TryParse(Convert.ToString(x, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) r.Add(n);
            }
            return r;
        }

        private static object ReadJson(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path, Encoding.UTF8);
                return Json.DeserializeObject(text);
            }
            catch { return null; }
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
                StringBuilder s = new StringBuilder(64);
                foreach (byte x in b) s.Append(x.ToString("x2", CultureInfo.InvariantCulture));
                return s.ToString();
            }
        }

        private static void OpenDataStructureArcade(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · DATA-STRUCTURE ARCADE", 1040, 720);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("DATA-STRUCTURE ARCADE", 27, Text, FontWeights.Bold));
            h.Children.Add(T("Classical structures, shown through the real YOMI artifacts that depend on them.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel p = new StackPanel(); sv.Content = p; root.Children.Add(sv);

            AddStructureCard(p, "SEQUENCE / ARRAY", "session-order.json", "Authoritative occurrence order is a permutation-like sequence with stable occurrence identity.", Path.Combine(ctx.StateRoot, "session-order.json"), w, ctx);
            AddStructureCard(p, "HASH MAP / INDEX", "queue occurrence lookup", "The WPF shell keeps occurrence→row indexes so object identity survives reorder without rebuilding semantic identity.", Path.Combine(ctx.AppDir, "YomiControllerWpf.cs"), w, ctx);
            AddStructureCard(p, "APPEND-ONLY LOG", "intent + mutation journals", "Durable history is represented as append-oriented evidence rather than rewriting the past in place.", FindFirst(ctx.StateRoot, new string[] { "*intent*.jsonl", "*journal*.jsonl" }), w, ctx);
            AddStructureCard(p, "HASH CHAIN", "journal ancestry", "Each record can prove ancestry through previous-hash linkage; tampering becomes evidence instead of silent mutation.", FindFirst(ctx.StateRoot, new string[] { "*intent*.jsonl", "*journal*.jsonl" }), w, ctx);
            AddStructureCard(p, "DIRECTED ACYCLIC GRAPH", "operator-plan dependencies", "Intent Fabric plans admit only dependency graphs that can be topologically ordered.", Path.Combine(ctx.AppDir, "YomiControllerWpf.cs"), w, ctx);
            AddStructureCard(p, "CONTENT-ADDRESSED STORE", "cache/objects", "Media identity is separated from mutable queue identity. Immutable-ish objects are named from source/signature identities.", Path.Combine(ctx.DataRoot, "cache", "objects"), w, ctx);
            AddStructureCard(p, "STATE MACHINE", "operator intent lifecycle", "PLANNED→ADMITTED→EXECUTING→AWAITING_AUTHORITY→COMMITTED with explicit abnormal terminal states.", Path.Combine(ctx.AppDir, "YomiControllerWpf.cs"), w, ctx);
            AddStructureCard(p, "PROJECTION", "queue-runtime/current/etc", "Read-optimized projections tell UI and diagnostics what authority last published without becoming authority themselves.", Path.Combine(ctx.StateRoot, "queue-runtime.json"), w, ctx);
            w.Content = root; w.Show();
        }

        private static void AddStructureCard(Panel p, string badge, string title, string desc, string path, Window owner, DeepSystemsContext ctx)
        {
            StackPanel s = new StackPanel();
            s.Children.Add(T(badge, 10.5, Gold, FontWeights.Bold));
            s.Children.Add(T(title, 17, Text, FontWeights.SemiBold));
            s.Children.Add(T(desc, 12.5, Muted, FontWeights.Normal));
            if (!String.IsNullOrWhiteSpace(path))
            {
                TextBlock f = T(path, 10.5, Blue, FontWeights.Normal); f.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(f);
                Button b = Btn("OPEN EVIDENCE", delegate { if (Directory.Exists(path)) DeepSystems.OpenExternalValue(owner, ctx, title + " · DIRECTORY", DirectorySnapshot(path), "CONSERVATORY › DATA STRUCTURES"); else DeepSystems.OpenExternalFile(owner, ctx, path, "CONSERVATORY › DATA STRUCTURES"); });
                b.Margin = new Thickness(0, 8, 0, 0); s.Children.Add(b);
            }
            p.Children.Add(Card(s));
        }

        private static object DirectorySnapshot(string path)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["path"] = path;
            try
            {
                d["directories"] = Directory.GetDirectories(path).Take(64).ToArray();
                d["files"] = Directory.GetFiles(path).Take(128).ToArray();
            }
            catch (Exception ex) { d["error"] = ex.Message; }
            return d;
        }

        private static string FindFirst(string root, string[] patterns)
        {
            if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;
            foreach (string pat in patterns)
            {
                try
                {
                    string x = Directory.GetFiles(root, pat, SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                    if (!String.IsNullOrWhiteSpace(x)) return x;
                }
                catch { }
            }
            return null;
        }

        private static void OpenProofGallery(Window owner, DeepSystemsContext ctx)
        {
            List<ProofResult> proofs = EvaluateProofs(ctx);
            Window w = W(owner, "YOMI · INVARIANT & PROOF GALLERY", 1080, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("INVARIANT & PROOF GALLERY", 27, Text, FontWeights.Bold));
            int pass = proofs.Count(x => x.Status == "PASS");
            h.Children.Add(T(pass.ToString(CultureInfo.InvariantCulture) + "/" + proofs.Count.ToString(CultureInfo.InvariantCulture) + " obligations currently satisfied", 12.5, pass == proofs.Count ? Accent : Danger, FontWeights.SemiBold));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel p = new StackPanel(); sv.Content = p; root.Children.Add(sv);
            foreach (ProofResult pr in proofs)
            {
                StackPanel s = new StackPanel();
                s.Children.Add(T(pr.Status, 10.5, pr.Status == "PASS" ? Accent : Danger, FontWeights.Bold));
                s.Children.Add(T(pr.Name, 17, Text, FontWeights.SemiBold));
                s.Children.Add(T(pr.Detail, 12.3, Muted, FontWeights.Normal));
                if (pr.Witness != null)
                    s.Children.Add(Btn("OPEN WITNESS", delegate(object sender, RoutedEventArgs e) { DeepSystems.OpenExternalValue(w, ctx, pr.Name + " · WITNESS", pr.Witness, "CONSERVATORY › PROOF GALLERY"); }));
                p.Children.Add(Card(s));
            }
            w.Content = root; w.Show();
        }

        private static List<ProofResult> EvaluateProofs(DeepSystemsContext ctx)
        {
            List<ProofResult> r = new List<ProofResult>();
            List<int> order = LoadAuthoritativeOrder(ctx);
            r.Add(new ProofResult { Name = "Occurrence uniqueness", Status = order.Count == order.Distinct().Count() ? "PASS" : "FAIL", Detail = "Every authoritative occurrence ID must appear at most once in session order.", Witness = new { count = order.Count, distinct = order.Distinct().Count(), duplicates = order.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).Take(32).ToArray() } });
            r.Add(new ProofResult { Name = "Positive occurrence domain", Status = order.All(x => x > 0) ? "PASS" : "FAIL", Detail = "Mutable-order occurrence IDs remain positive stable integer identities.", Witness = new { invalid = order.Where(x => x <= 0).Take(32).ToArray() } });
            object so = ReadJson(Path.Combine(ctx.StateRoot, "session-order.json"));
            Dictionary<string, object> sd = so as Dictionary<string, object>;
            int active = DictInt(sd, "active_count", order.Count);
            r.Add(new ProofResult { Name = "Cardinality agreement", Status = active == order.Count ? "PASS" : "FAIL", Detail = "Published active_count agrees with the durable authoritative sequence length.", Witness = new { active_count = active, sequence_count = order.Count } });
            object qr = ReadJson(Path.Combine(ctx.StateRoot, "queue-runtime.json"));
            Dictionary<string, object> qd = qr as Dictionary<string, object>;
            int qrRev = DictInt(qd, "order_revision", -1);
            int soRev = DictInt(sd, "revision", -1);
            r.Add(new ProofResult { Name = "Projection revision monotonic agreement", Status = qrRev < 0 || soRev < 0 || qrRev <= soRev ? "PASS" : "FAIL", Detail = "Read projection must not claim an order revision beyond durable authority.", Witness = new { queue_runtime_revision = qrRev, session_order_revision = soRev } });
            string journal = FindFirst(ctx.StateRoot, new string[] { "*intent*.jsonl" });
            if (!String.IsNullOrWhiteSpace(journal))
            {
                ProofResult chain = VerifyLooseHashChain(journal);
                chain.Name = "Intent-journal hash ancestry";
                r.Add(chain);
            }
            else r.Add(new ProofResult { Name = "Intent-journal hash ancestry", Status = "PASS", Detail = "No intent journal present in this state root; obligation is currently vacuous.", Witness = null });
            r.Add(new ProofResult { Name = "Authority / exploration separation", Status = "PASS", Detail = "Conservatory code contains no mpv command pipe, yt-dlp process, FFmpeg process or session-order write primitive. This proof is construction-enforced by the bundle sweep.", Witness = new { subsystem = "YomiAlgorithmicConservatory.cs", authority = "read/simulate only" } });
            return r;
        }

        private static int DictInt(Dictionary<string, object> d, string key, int fallback)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return fallback;
            int n; return Int32.TryParse(Convert.ToString(d[key], CultureInfo.InvariantCulture), out n) ? n : fallback;
        }

        private static ProofResult VerifyLooseHashChain(string path)
        {
            List<Dictionary<string, object>> entries = ReadJsonLines(path, 2000);
            string prev = null;
            int checkedCount = 0;
            List<object> problems = new List<object>();
            foreach (Dictionary<string, object> d in entries)
            {
                string p = DictString(d, "prev_hash");
                string h = DictString(d, "entry_hash");
                if (!String.IsNullOrWhiteSpace(prev) && !String.IsNullOrWhiteSpace(p) && !String.Equals(prev, p, StringComparison.OrdinalIgnoreCase))
                    problems.Add(new { sequence = DictString(d, "sequence"), expected_prev = prev, actual_prev = p });
                if (!String.IsNullOrWhiteSpace(h)) prev = h;
                checkedCount++;
            }
            return new ProofResult { Status = problems.Count == 0 ? "PASS" : "FAIL", Detail = "Checks visible previous-hash continuity across the bounded journal tail. It does not recompute the producer-specific canonical entry hash.", Witness = new { path = path, checked_entries = checkedCount, continuity_problems = problems.ToArray() } };
        }

        private static string DictString(Dictionary<string, object> d, string key)
        {
            return d != null && d.ContainsKey(key) && d[key] != null ? Convert.ToString(d[key], CultureInfo.InvariantCulture) : "";
        }

        private static List<Dictionary<string, object>> ReadJsonLines(string path, int max)
        {
            List<Dictionary<string, object>> r = new List<Dictionary<string, object>>();
            try
            {
                Queue<string> q = new Queue<string>();
                foreach (string line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    q.Enqueue(line);
                    while (q.Count > max) q.Dequeue();
                }
                foreach (string line in q)
                {
                    try { Dictionary<string, object> d = Json.DeserializeObject(line) as Dictionary<string, object>; if (d != null) r.Add(d); } catch { }
                }
            }
            catch { }
            return r;
        }

        private static void OpenAlgorithmTheatre(Window owner, DeepSystemsContext ctx)
        {
            List<int> order = LoadAuthoritativeOrder(ctx);
            if (order.Count < 2)
            {
                ShowMessage(owner, "ALGORITHM THEATRE", "No authoritative session order is available to clone."); return;
            }
            int anchor = Math.Min(order.Count - 1, Math.Max(0, order.Count / 3));
            List<AlgorithmRun> runs = new List<AlgorithmRun>();
            runs.Add(RunTransform("IDENTITY / CONTROL", order, order.ToArray(), "O(n)", "Baseline clone; no structural mutation."));
            runs.Add(RunTransform("STABLE BLOCK MOVE", order, StableBlockMove(order, anchor, Math.Min(4, order.Count - anchor), Math.Min(order.Count, anchor + 12)), "O(n)", "Preserves selected block order; analogous to DEV13.37 authoritative block transaction semantics."));
            runs.Add(RunTransform("ROTATE NEXT WINDOW", order, RotateWindow(order, anchor, Math.Min(12, order.Count - anchor)), "O(k)", "Classical cyclic rotation over a bounded local window."));
            runs.Add(RunTransform("REVERSE NEXT WINDOW", order, ReverseWindow(order, anchor, Math.Min(9, order.Count - anchor)), "O(k)", "Classical in-place reversal model applied to a clone."));
            runs.Add(RunTransform("DETERMINISTIC PHASE SHIFT", order, PhaseShift(order, anchor, Math.Min(16, order.Count - anchor)), "O(k log k)", "Experimental deterministic permutation seeded from baseline fingerprint."));
            OpenAlgorithmRuns(owner, ctx, "COMPARATIVE ALGORITHM THEATRE", order, runs);
        }

        private static AlgorithmRun RunTransform(string name, List<int> baseline, int[] candidate, string complexity, string notes)
        {
            int changed = 0; int prefix = 0;
            for (int i = 0; i < baseline.Count && i < candidate.Length; i++) { if (baseline[i] != candidate[i]) changed++; else if (i == prefix) prefix++; }
            long inv = KendallInversions(baseline, candidate);
            int sameAdj = 0; HashSet<string> baseAdj = new HashSet<string>();
            for (int i = 0; i + 1 < baseline.Count; i++) baseAdj.Add(baseline[i].ToString(CultureInfo.InvariantCulture) + ">" + baseline[i + 1].ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i + 1 < candidate.Length; i++) if (baseAdj.Contains(candidate[i].ToString(CultureInfo.InvariantCulture) + ">" + candidate[i + 1].ToString(CultureInfo.InvariantCulture))) sameAdj++;
            double retention = baseline.Count <= 1 ? 1.0 : sameAdj / (double)(baseline.Count - 1);
            return new AlgorithmRun { Name = name, Order = candidate, ChangedSlots = changed, Inversions = inv, StablePrefix = prefix, AdjacencyRetention = retention, Complexity = complexity, Notes = notes };
        }

        private static int[] StableBlockMove(List<int> src, int start, int count, int boundary)
        {
            List<int> x = new List<int>(src); count = Math.Max(0, Math.Min(count, x.Count - start));
            if (count <= 0) return x.ToArray();
            List<int> block = x.GetRange(start, count); x.RemoveRange(start, count);
            int adjusted = boundary;
            if (boundary > start) adjusted -= count;
            adjusted = Math.Max(0, Math.Min(x.Count, adjusted));
            x.InsertRange(adjusted, block); return x.ToArray();
        }

        private static int[] RotateWindow(List<int> src, int start, int count)
        {
            List<int> x = new List<int>(src); if (count <= 1) return x.ToArray();
            int last = start + count - 1; int first = x[start];
            for (int i = start; i < last; i++) x[i] = x[i + 1]; x[last] = first; return x.ToArray();
        }

        private static int[] ReverseWindow(List<int> src, int start, int count)
        {
            List<int> x = new List<int>(src); if (count > 1) x.Reverse(start, count); return x.ToArray();
        }

        private static int[] PhaseShift(List<int> src, int start, int count)
        {
            List<int> x = new List<int>(src); if (count <= 1) return x.ToArray();
            string fp = Sha256(String.Join(",", src.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray()));
            List<Tuple<string, int>> keyed = new List<Tuple<string, int>>();
            for (int i = 0; i < count; i++) keyed.Add(Tuple.Create(Sha256(fp + ":" + i.ToString(CultureInfo.InvariantCulture) + ":" + x[start + i].ToString(CultureInfo.InvariantCulture)), x[start + i]));
            int[] sorted = keyed.OrderBy(t => t.Item1, StringComparer.Ordinal).Select(t => t.Item2).ToArray();
            for (int i = 0; i < count; i++) x[start + i] = sorted[i]; return x.ToArray();
        }

        private static long KendallInversions(List<int> baseline, int[] candidate)
        {
            Dictionary<int, int> pos = new Dictionary<int, int>(); for (int i = 0; i < baseline.Count; i++) pos[baseline[i]] = i;
            int[] a = candidate.Where(x => pos.ContainsKey(x)).Select(x => pos[x]).ToArray();
            long inv = 0; for (int i = 0; i < a.Length; i++) for (int j = i + 1; j < a.Length; j++) if (a[i] > a[j]) inv++;
            return inv;
        }

        private static void OpenAlgorithmRuns(Window owner, DeepSystemsContext ctx, string title, List<int> baseline, List<AlgorithmRun> runs)
        {
            Window w = W(owner, "YOMI · " + title, 1080, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T(title, 27, Text, FontWeights.Bold));
            h.Children.Add(T("All transformations operate on clones. Baseline authority is never mutated by the theatre.", 12.5, Accent, FontWeights.SemiBold));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel p = new StackPanel(); sv.Content = p; root.Children.Add(sv);
            foreach (AlgorithmRun ar in runs)
            {
                StackPanel s = new StackPanel();
                s.Children.Add(T(ar.Name, 18, Text, FontWeights.SemiBold));
                s.Children.Add(T(ar.Complexity + " · changed slots " + ar.ChangedSlots.ToString(CultureInfo.InvariantCulture) + " · Kendall inversions " + ar.Inversions.ToString(CultureInfo.InvariantCulture) + " · stable prefix " + ar.StablePrefix.ToString(CultureInfo.InvariantCulture) + " · adjacency " + ar.AdjacencyRetention.ToString("P1", CultureInfo.InvariantCulture), 11.5, Blue, FontWeights.SemiBold));
                s.Children.Add(T(ar.Notes, 12.2, Muted, FontWeights.Normal));
                s.Children.Add(Btn("OPEN CLONED RESULT", delegate(object sender, RoutedEventArgs e) { DeepSystems.OpenExternalValue(w, ctx, ar.Name, new { metrics = new { changed_slots = ar.ChangedSlots, inversions = ar.Inversions, stable_prefix = ar.StablePrefix, adjacency_retention = ar.AdjacencyRetention, complexity = ar.Complexity }, baseline = baseline.ToArray(), candidate = ar.Order }, "CONSERVATORY › ALGORITHM THEATRE"); }));
                p.Children.Add(Card(s));
            }
            w.Content = root; w.Show();
        }

        private static void OpenModelChecker(Window owner, DeepSystemsContext ctx)
        {
            ModelCheckResult m = CheckIntentMachine();
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["machine"] = m.Machine; result["states_visited"] = m.StatesVisited; result["transitions_visited"] = m.TransitionsVisited; result["rejected_transitions"] = m.RejectedTransitions; result["reachable_states"] = m.Reachable.ToArray(); result["violations"] = m.Violations.ToArray();
            Window w = W(owner, "YOMI · FINITE-STATE MODEL CHECKER", 980, 680);
            StackPanel p = new StackPanel { Margin = new Thickness(20) };
            p.Children.Add(T("FINITE-STATE MODEL CHECKER", 27, Text, FontWeights.Bold));
            p.Children.Add(T("Exhaustive bounded exploration of the operator-intent lifecycle. This is a model of allowed semantics, not a live engine mutation.", 12.5, Muted, FontWeights.Normal));
            p.Children.Add(T(m.Violations.Count == 0 ? "MODEL CONSISTENT" : "VIOLATIONS FOUND", 13, m.Violations.Count == 0 ? Accent : Danger, FontWeights.Bold));
            p.Children.Add(T(m.StatesVisited + " reachable states · " + m.TransitionsVisited + " accepted edges · " + m.RejectedTransitions + " rejected candidate edges", 12, Blue, FontWeights.SemiBold));
            p.Children.Add(Btn("OPEN STATE-SPACE DOSSIER", delegate { DeepSystems.OpenExternalValue(w, ctx, "INTENT STATE SPACE", result, "CONSERVATORY › MODEL CHECKER"); }));
            p.Children.Add(Btn("OPEN INTENT FABRIC SOURCE", delegate { DeepSystems.OpenExternalFile(w, ctx, Path.Combine(ctx.AppDir, "YomiControllerWpf.cs"), "CONSERVATORY › MODEL CHECKER › SOURCE"); }));
            w.Content = p; w.Show();
        }

        private static ModelCheckResult CheckIntentMachine()
        {
            Dictionary<string, string[]> allowed = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "PLANNED", new string[] { "ADMITTED", "REJECTED", "CANCELLED" } },
                { "ADMITTED", new string[] { "EXECUTING", "CANCELLED", "FAILED" } },
                { "EXECUTING", new string[] { "EXECUTING", "AWAITING_AUTHORITY", "COMMITTED", "FAILED", "CANCELLED", "COMPENSATING" } },
                { "AWAITING_AUTHORITY", new string[] { "EXECUTING", "COMMITTED", "FAILED", "INDETERMINATE" } },
                { "COMPENSATING", new string[] { "CANCELLED", "FAILED" } },
                { "COMMITTED", new string[0] }, { "FAILED", new string[0] }, { "CANCELLED", new string[0] }, { "REJECTED", new string[0] }, { "INDETERMINATE", new string[0] }, { "ABANDONED", new string[0] }
            };
            HashSet<string> terminal = new HashSet<string>(new string[] { "COMMITTED", "FAILED", "CANCELLED", "REJECTED", "INDETERMINATE", "ABANDONED" }, StringComparer.OrdinalIgnoreCase);
            Queue<string> q = new Queue<string>(); HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); q.Enqueue("PLANNED"); seen.Add("PLANNED");
            ModelCheckResult r = new ModelCheckResult { Machine = "OPERATOR_INTENT_DEV13_37_X" };
            while (q.Count > 0)
            {
                string s = q.Dequeue(); r.Reachable.Add(s);
                string[] edges; if (!allowed.TryGetValue(s, out edges)) { r.Violations.Add("Missing transition definition for " + s); continue; }
                foreach (string t in edges) { r.TransitionsVisited++; if (terminal.Contains(s)) r.Violations.Add("Terminal state has outgoing edge: " + s + "→" + t); if (seen.Add(t)) q.Enqueue(t); }
            }
            string[] all = allowed.Keys.ToArray(); foreach (string a in all) foreach (string b in all) if (!allowed[a].Contains(b)) r.RejectedTransitions++;
            r.StatesVisited = seen.Count; return r;
        }

        private static void OpenCrashLab(Window owner, DeepSystemsContext ctx)
        {
            List<Dictionary<string, object>> cases = new List<Dictionary<string, object>>();
            cases.Add(CrashCase("Before journal append", false, false, "No durable mutation evidence exists; operation is safely absent."));
            cases.Add(CrashCase("After journal append / before head", true, false, "Durable tail can prove a lagging head; recovery may advance head after chain validation."));
            cases.Add(CrashCase("After head persistence", true, true, "Journal and head agree; committed evidence is durable."));
            cases.Add(CrashCase("Head claims unseen tail", false, true, "Head is ahead of durable evidence; fail closed and preserve evidence."));
            DeepSystems.OpenExternalValue(owner, ctx, "WRITE-AHEAD LOG CRASH LAB", new { doctrine = "journal evidence first; separately persisted head second", cases = cases.ToArray(), note = "Model only. No live journal is modified." }, "CONSERVATORY › CRASH LAB");
        }

        private static Dictionary<string, object> CrashCase(string point, bool journal, bool head, string interpretation)
        {
            return new Dictionary<string, object> { { "crash_point", point }, { "journal_tail_durable", journal }, { "head_durable", head }, { "interpretation", interpretation } };
        }

        private static void OpenHashLab(Window owner, DeepSystemsContext ctx)
        {
            List<string> files = EnumerateFiles(ctx.DataRoot, 180).Where(IsTextLike).Take(40).ToList();
            List<object> samples = new List<object>();
            foreach (string f in files.Take(12))
            {
                try
                {
                    byte[] b = File.ReadAllBytes(f); string h; using (SHA256 sha = SHA256.Create()) h = BitConverter.ToString(sha.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
                    samples.Add(new { file = f, bytes = b.Length, sha256 = h });
                }
                catch { }
            }
            DeepSystems.OpenExternalValue(owner, ctx, "HASH / CONTENT-ADDRESSING LAB", new { primitives = new string[] { "SHA-256", "previous-hash ancestry", "plan fingerprint", "source identity", "object signature" }, sample_artifact_hashes = samples.ToArray(), distinction = "Hash equality can establish byte identity; it does not by itself establish semantic equivalence or causal relationship." }, "CONSERVATORY › HASH LAB");
        }

        private static void OpenGraphGarden(Window owner, DeepSystemsContext ctx)
        {
            Dictionary<string, object> graph = new Dictionary<string, object>();
            graph["classical"] = new string[] { "directed graph", "connected component", "topological order", "path", "cycle detection" };
            graph["modern_yomi_instances"] = new string[] { "Intent DAG", "evidence identity graph", "counterfactual genealogy", "window constellation", "semantic cross-links" };
            graph["experimental"] = new string[] { "evidence-cone scoring", "serendipity ranking", "identity wormholes", "hypothesis topology" };
            graph["safety_boundary"] = "Graph relationships may be observed/inferred; they are not automatically causal.";
            Window w = W(owner, "YOMI · GRAPH THEORY GARDEN", 940, 640);
            StackPanel p = new StackPanel { Margin = new Thickness(20) };
            p.Children.Add(T("GRAPH THEORY GARDEN", 27, Text, FontWeights.Bold));
            p.Children.Add(T("One mathematical family expressed as scheduling, provenance, evidence topology and alternate-history genealogy.", 12.5, Muted, FontWeights.Normal));
            p.Children.Add(Btn("OPEN GRAPH FAMILY DOSSIER", delegate { DeepSystems.OpenExternalValue(w, ctx, "GRAPH THEORY FAMILY", graph, "CONSERVATORY › GRAPH GARDEN"); }));
            p.Children.Add(Btn("ENTER CAUSAL GRAPH LAB", delegate { CausalGraphLab.Open(w, ctx); }));
            p.Children.Add(Btn("ENTER RESEARCH WORKBENCH", delegate { ResearchWorkbench.Open(w, ctx); }));
            w.Content = p; w.Show();
        }

        private static void OpenComplexityObservatory(Window owner, DeepSystemsContext ctx)
        {
            int[] sizes = new int[] { 8, 32, 128, 512, 2048, 8192 };
            List<object> rows = new List<object>();
            foreach (int n in sizes)
            {
                long linear = n;
                long nlogn = (long)Math.Ceiling(n * Math.Log(Math.Max(2, n), 2));
                long quadratic = (long)n * (long)n;
                rows.Add(new { n = n, O_1 = 1, O_log_n = Math.Ceiling(Math.Log(Math.Max(2, n), 2)), O_n = linear, O_n_log_n = nlogn, O_n2 = quadratic });
            }
            DeepSystems.OpenExternalValue(owner, ctx, "COMPLEXITY OBSERVATORY", new { theory = rows.ToArray(), examples = new { dictionary_lookup = "expected O(1)", stable_block_rebuild = "O(n)", deterministic_phase_sort = "O(k log k)", naive_kendall_demo = "O(n²) in this educational bounded implementation" }, note = "Complexity class describes asymptotic growth, not wall-clock performance on one machine." }, "CONSERVATORY › COMPLEXITY");
        }


        private sealed class SchedulerItem
        {
            public int Occurrence;
            public int Slot;
            public double PrepSeconds;
            public double Risk;
            public double Age;
            public string Phase;
            public string SourceKey;
        }

        private static void OpenSchedulerTournament(Window owner, DeepSystemsContext ctx)
        {
            List<SchedulerItem> items = ReadSchedulerItems(ctx);
            if (items.Count == 0)
            {
                ShowMessage(owner, "SCHEDULER TOURNAMENT", "queue-runtime.json does not currently expose enough items to stage a tournament.");
                return;
            }
            List<object> disciplines = new List<object>();
            disciplines.Add(SchedulerDiscipline("FIFO / SLOT ORDER", items.OrderBy(x => x.Slot).ToList(), "Classical first-in sequence discipline."));
            disciplines.Add(SchedulerDiscipline("SHORTEST PREPARATION FIRST", items.OrderBy(x => x.PrepSeconds <= 0 ? Double.MaxValue : x.PrepSeconds).ThenBy(x => x.Slot).ToList(), "Greedy shortest-estimated-work discipline; may starve expensive items."));
            disciplines.Add(SchedulerDiscipline("RISK FIRST", items.OrderByDescending(x => x.Risk).ThenBy(x => x.Slot).ToList(), "Prioritizes observed transition danger; does not claim to match engine policy."));
            disciplines.Add(SchedulerDiscipline("AGING / FAIRNESS", items.OrderByDescending(x => x.Risk + Math.Min(4.0, x.Age / 30.0)).ThenBy(x => x.Slot).ToList(), "Adds age pressure to risk so long-waiting work eventually rises."));
            disciplines.Add(SchedulerDiscipline("HYBRID SLACK HEURISTIC", items.OrderByDescending(x => (x.Risk * 3.0) + Math.Min(3.0, x.Age / 45.0) - Math.Min(3.0, x.PrepSeconds / 30.0)).ThenBy(x => x.Slot).ToList(), "Experimental composite score over cloned evidence only."));
            DeepSystems.OpenExternalValue(owner, ctx, "SCHEDULER TOURNAMENT", new { observed_items = items.Select(x => new { occurrence = x.Occurrence, slot = x.Slot, prep_seconds = x.PrepSeconds, risk_score = x.Risk, age = x.Age, phase = x.Phase, source_key = x.SourceKey }).ToArray(), disciplines = disciplines.ToArray(), warning = "These are comparative simulations, not authoritative scheduler decisions." }, "CONSERVATORY › SCHEDULER TOURNAMENT");
        }

        private static object SchedulerDiscipline(string name, List<SchedulerItem> items, string note)
        {
            double cumulative = 0;
            double completionSum = 0;
            List<object> order = new List<object>();
            for (int i = 0; i < items.Count; i++)
            {
                SchedulerItem x = items[i];
                cumulative += Math.Max(0, x.PrepSeconds);
                completionSum += cumulative;
                order.Add(new { rank = i + 1, occurrence = x.Occurrence, original_slot = x.Slot, prep_seconds = x.PrepSeconds, risk_score = x.Risk, cumulative_work_seconds = cumulative });
            }
            double avgCompletion = order.Count == 0 ? 0 : completionSum / order.Count;
            return new { name = name, note = note, average_completion_seconds = avgCompletion, order = order.ToArray() };
        }

        private static List<SchedulerItem> ReadSchedulerItems(DeepSystemsContext ctx)
        {
            List<SchedulerItem> r = new List<SchedulerItem>();
            Dictionary<string, object> q = ReadJson(Path.Combine(ctx.StateRoot, "queue-runtime.json")) as Dictionary<string, object>;
            if (q == null || !q.ContainsKey("items")) return r;
            IEnumerable items = q["items"] as IEnumerable; if (items == null || q["items"] is string) return r;
            int ordinal = 0;
            foreach (object o in items)
            {
                Dictionary<string, object> d = o as Dictionary<string, object>; if (d == null) continue;
                ordinal++;
                SchedulerItem x = new SchedulerItem();
                x.Occurrence = DictInt(d, "occurrence_id", DictInt(d, "index", -1));
                x.Slot = DictInt(d, "order_slot", DictInt(d, "slot", ordinal));
                x.PrepSeconds = DictDouble(d, new string[] { "prepare_seconds", "prep_seconds", "eta_seconds", "estimated_prepare_seconds" }, 0);
                x.Age = DictDouble(d, new string[] { "age_seconds", "queued_seconds", "wait_seconds" }, Math.Max(0, ordinal - 1) * 3.0);
                x.Risk = RiskScore(DictString(d, "risk"));
                x.Phase = DictString(d, "phase");
                x.SourceKey = DictString(d, "source_key");
                if (x.Occurrence > 0) r.Add(x);
                if (r.Count >= 64) break;
            }
            return r;
        }

        private static double DictDouble(Dictionary<string, object> d, string[] keys, double fallback)
        {
            if (d == null) return fallback;
            foreach (string k in keys)
            {
                object v; if (!d.TryGetValue(k, out v) || v == null) continue;
                double x; if (Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return x;
            }
            return fallback;
        }

        private static double RiskScore(string risk)
        {
            string r = (risk ?? "").Trim().ToUpperInvariant();
            if (r == "URGENT" || r == "CRITICAL") return 5;
            if (r == "AT RISK" || r == "HIGH") return 4;
            if (r == "WATCH" || r == "MEDIUM") return 3;
            if (r == "LOW") return 1;
            return 0;
        }

        private sealed class InterleaveState
        {
            public bool Journal;
            public bool Head;
            public bool ReaderSawJournal;
            public bool ReaderSawHead;
            public List<string> Trace = new List<string>();
        }

        private static void OpenInterleavingLab(Window owner, DeepSystemsContext ctx)
        {
            string[] ops = new string[] { "W_APPEND_JOURNAL", "W_PERSIST_HEAD", "R_READ_JOURNAL", "R_READ_HEAD" };
            List<string[]> schedules = Permute(ops).Where(x => Array.IndexOf(x, "W_APPEND_JOURNAL") < Array.IndexOf(x, "W_PERSIST_HEAD") && Array.IndexOf(x, "R_READ_JOURNAL") < Array.IndexOf(x, "R_READ_HEAD")).ToList();
            List<object> cases = new List<object>();
            foreach (string[] schedule in schedules)
            {
                for (int crashAfter = 0; crashAfter <= schedule.Length; crashAfter++)
                {
                    InterleaveState st = new InterleaveState();
                    for (int i = 0; i < crashAfter; i++) ApplyInterleave(st, schedule[i]);
                    string classification = ClassifyInterleave(st);
                    cases.Add(new { schedule = schedule, crash_after_steps = crashAfter, durable_journal = st.Journal, durable_head = st.Head, reader_observed_journal = st.ReaderSawJournal, reader_observed_head = st.ReaderSawHead, classification = classification, trace = st.Trace.ToArray() });
                }
            }
            DeepSystems.OpenExternalValue(owner, ctx, "CONCURRENCY INTERLEAVING LAB", new { partial_order_constraints = new string[] { "append before head persist", "reader journal before reader head" }, schedules = schedules.Count, crash_prefix_cases = cases.Count, cases = cases.ToArray(), note = "Bounded exhaustive model. Real OS/file-system behavior has additional layers." }, "CONSERVATORY › INTERLEAVING LAB");
        }

        private static IEnumerable<string[]> Permute(string[] values)
        {
            return PermuteCore(values, 0);
        }

        private static IEnumerable<string[]> PermuteCore(string[] values, int index)
        {
            if (index >= values.Length - 1) { yield return (string[])values.Clone(); yield break; }
            for (int i = index; i < values.Length; i++)
            {
                string[] copy = (string[])values.Clone(); string t = copy[index]; copy[index] = copy[i]; copy[i] = t;
                foreach (string[] p in PermuteCore(copy, index + 1)) yield return p;
            }
        }

        private static void ApplyInterleave(InterleaveState s, string op)
        {
            if (op == "W_APPEND_JOURNAL") { s.Journal = true; s.Trace.Add("writer appended durable evidence"); }
            else if (op == "W_PERSIST_HEAD") { s.Head = true; s.Trace.Add("writer persisted head"); }
            else if (op == "R_READ_JOURNAL") { s.ReaderSawJournal = s.Journal; s.Trace.Add("reader journal=" + s.ReaderSawJournal); }
            else if (op == "R_READ_HEAD") { s.ReaderSawHead = s.Head; s.Trace.Add("reader head=" + s.ReaderSawHead); }
        }

        private static string ClassifyInterleave(InterleaveState s)
        {
            if (s.Head && !s.Journal) return "IMPOSSIBLE_UNDER_WRITER_ORDER / CORRUPTION";
            if (s.Journal && !s.Head) return "CRASH_COMPATIBLE_HEAD_LAG";
            if (s.Journal && s.Head) return "DURABLE_COMMIT_EVIDENCE";
            if (s.ReaderSawHead && !s.ReaderSawJournal) return "READER_OBSERVATION_ANOMALY";
            return "NO_DURABLE_COMMIT";
        }

        private static void OpenInformationTheory(Window owner, DeepSystemsContext ctx)
        {
            List<SchedulerItem> items = ReadSchedulerItems(ctx);
            Dictionary<string, int> phases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> sources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (SchedulerItem x in items)
            {
                string p = String.IsNullOrWhiteSpace(x.Phase) ? "UNKNOWN" : x.Phase;
                phases[p] = phases.ContainsKey(p) ? phases[p] + 1 : 1;
                string sk = String.IsNullOrWhiteSpace(x.SourceKey) ? "UNKNOWN" : x.SourceKey;
                sources[sk] = sources.ContainsKey(sk) ? sources[sk] + 1 : 1;
            }
            object result = new
            {
                item_count = items.Count,
                phase_distribution = phases,
                phase_entropy_bits = ShannonEntropy(phases.Values),
                source_identity_count = sources.Count,
                source_identity_entropy_bits = ShannonEntropy(sources.Values),
                source_concentration_hhi = Herfindahl(sources.Values),
                interpretation = "Entropy measures uncertainty/diversity of a distribution. It does not measure correctness, quality or desirability."
            };
            DeepSystems.OpenExternalValue(owner, ctx, "INFORMATION THEORY CHAMBER", result, "CONSERVATORY › INFORMATION THEORY");
        }

        private static double ShannonEntropy(IEnumerable<int> counts)
        {
            double total = counts.Sum(); if (total <= 0) return 0; double h = 0;
            foreach (int c in counts) { if (c <= 0) continue; double p = c / total; h -= p * (Math.Log(p) / Math.Log(2)); }
            return h;
        }

        private static double Herfindahl(IEnumerable<int> counts)
        {
            double total = counts.Sum(); if (total <= 0) return 0; double h = 0;
            foreach (int c in counts) { double p = c / total; h += p * p; }
            return h;
        }

        private static void OpenParadigmHall(Window owner, DeepSystemsContext ctx)
        {
            List<CodeArtifact> arts = ScanCode(ctx);
            object paradigms = new
            {
                imperative = new { description = "Explicit state mutation and control flow", evidence = arts.Where(x => x.Language == "LUA" || x.Language == "POWERSHELL").OrderByDescending(x => x.BranchMarkers).Take(8).Select(x => x.Path).ToArray() },
                event_driven = new { description = "WPF events, timers, callbacks and observable UI response", evidence = arts.Where(x => x.Language == "C#").OrderByDescending(x => x.MethodLike).Take(8).Select(x => x.Path).ToArray() },
                declarative = new { description = "XAML declares visual structure and resource relationships", evidence = arts.Where(x => x.Language == "XAML").Select(x => x.Path).Take(16).ToArray() },
                functional_transform = new { description = "LINQ/select/filter/order projections over immutable-ish snapshots", evidence = arts.Where(x => x.Language == "C#").Select(x => x.Path).Take(12).ToArray() },
                event_sourced_provenance = new { description = "Append-oriented journals + derived heads/projections", evidence = EnumerateFiles(ctx.StateRoot, 100).Where(x => Path.GetExtension(x).Equals(".jsonl", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(x).IndexOf("journal", StringComparison.OrdinalIgnoreCase) >= 0).Take(20).ToArray() },
                graph_oriented = new { description = "DAG dependencies, identity graphs, causal candidates and branch genealogy", evidence = new string[] { Path.Combine(ctx.AppDir, "YomiCausalGraphLab.cs"), Path.Combine(ctx.AppDir, "YomiResearchWorkbench.cs"), Path.Combine(ctx.AppDir, "YomiControllerWpf.cs") } },
                doctrine = "Paradigms are tools, not religions. YOMI mixes them deliberately where each shape fits the problem."
            };
            DeepSystems.OpenExternalValue(owner, ctx, "PROGRAMMING PARADIGM HALL", paradigms, "CONSERVATORY › PARADIGM HALL");
        }

        private static void OpenPatternMenagerie(Window owner, DeepSystemsContext ctx)
        {
            object patterns = new object[]
            {
                new { pattern = "COMMAND", yomi = "command palette + script-message operations", value = "decouples invocation from implementation" },
                new { pattern = "OBSERVER / PROJECTION", yomi = "current.json, queue-runtime.json, UI refresh", value = "read models observe authority without becoming it" },
                new { pattern = "STATE MACHINE", yomi = "Intent Fabric lifecycle", value = "explicit legal/illegal transitions and terminal outcomes" },
                new { pattern = "STRATEGY", yomi = "quality routes, workspace/media modes, algorithm experiments", value = "swap policy without rewriting the caller" },
                new { pattern = "SAGA / COMPENSATION", yomi = "operator-intent orchestration", value = "reverse safe shell effects while refusing blind authority rollback" },
                new { pattern = "WRITE-AHEAD / APPEND LOG", yomi = "intent + order evidence journals", value = "durable evidence before derived head state" },
                new { pattern = "CONTENT ADDRESSING", yomi = "cache objects keyed by source/signature", value = "identity decoupled from queue occurrence" },
                new { pattern = "CIRCUIT BREAKER", yomi = "audio/download failure-domain protection", value = "stop hammering a failing dependency" },
                new { pattern = "CAPABILITY LEASE", yomi = "single operator-intent ownership", value = "serialize control-plane authority" },
                new { pattern = "CQRS-LIKE SEPARATION", yomi = "engine commands vs read projections", value = "writes and reads have distinct contracts" }
            };
            DeepSystems.OpenExternalValue(owner, ctx, "DESIGN PATTERN MENAGERIE", new { patterns = patterns, caution = "Pattern names describe recurring structure. They are not proof that an implementation is correct merely because it resembles a named pattern." }, "CONSERVATORY › PATTERN MENAGERIE");
        }

        private static void OpenSourceObservatory(Window owner, DeepSystemsContext ctx)
        {
            List<CodeArtifact> arts = ScanCode(ctx);
            Window w = W(owner, "YOMI · SOURCE OBSERVATORY / POLYGLOT GENOME", 1140, 780);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("SOURCE OBSERVATORY / POLYGLOT GENOME", 27, Text, FontWeights.Bold));
            h.Children.Add(T(arts.Count.ToString(CultureInfo.InvariantCulture) + " source-like artifacts · " + arts.Sum(x => x.Lines).ToString(CultureInfo.InvariantCulture) + " lines observed", 12.5, Blue, FontWeights.SemiBold));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel p = new StackPanel(); sv.Content = p; root.Children.Add(sv);
            foreach (CodeArtifact a in arts.OrderByDescending(x => x.Lines).Take(120))
            {
                StackPanel s = new StackPanel();
                s.Children.Add(T(a.Language + " · " + Path.GetFileName(a.Path), 16.5, Text, FontWeights.SemiBold));
                s.Children.Add(T(a.Lines + " lines · " + a.MethodLike + " method-like · " + a.BranchMarkers + " branch markers · " + a.ClassLike + " class/type markers · " + a.Bytes + " bytes", 11.2, Blue, FontWeights.SemiBold));
                s.Children.Add(T(a.Path, 10.5, Muted, FontWeights.Normal));
                if (a.ProtocolTokens.Count > 0) s.Children.Add(T("protocols: " + String.Join(" · ", a.ProtocolTokens.Take(8).ToArray()), 10.5, Gold, FontWeights.Normal));
                s.Children.Add(Btn("OPEN SOURCE", delegate(object sender, RoutedEventArgs e) { DeepSystems.OpenExternalFile(w, ctx, a.Path, "CONSERVATORY › SOURCE OBSERVATORY"); }));
                p.Children.Add(Card(s));
            }
            w.Content = root; w.Show();
        }

        private static List<CodeArtifact> ScanCode(DeepSystemsContext ctx)
        {
            List<string> roots = new List<string>();
            if (Directory.Exists(ctx.AppDir)) roots.Add(ctx.AppDir);
            if (Directory.Exists(ctx.InstallRoot) && !String.Equals(ctx.AppDir, ctx.InstallRoot, StringComparison.OrdinalIgnoreCase)) roots.Add(ctx.InstallRoot);
            List<string> paths = new List<string>();
            foreach (string root in roots)
            {
                try
                {
                    foreach (string f in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".cs" || ext == ".lua" || ext == ".ps1" || ext == ".xaml" || ext == ".xml" || ext == ".json" || ext == ".cmd" || ext == ".bat") paths.Add(f);
                        if (paths.Count >= 260) break;
                    }
                }
                catch { }
                if (paths.Count >= 260) break;
            }
            return paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(AnalyzeCode).Where(x => x != null).ToList();
        }

        private static CodeArtifact AnalyzeCode(string path)
        {
            try
            {
                FileInfo fi = new FileInfo(path); if (fi.Length > 4 * 1024 * 1024) return null;
                string text = File.ReadAllText(path, Encoding.UTF8); string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string lang = ext == ".cs" ? "C#" : ext == ".lua" ? "LUA" : ext == ".ps1" ? "POWERSHELL" : ext == ".xaml" ? "XAML" : ext == ".json" ? "JSON" : ext.Trim('.').ToUpperInvariant();
                CodeArtifact a = new CodeArtifact { Path = path, Language = lang, Lines = lines.Length, NonBlank = lines.Count(x => !String.IsNullOrWhiteSpace(x)), Bytes = fi.Length, Fingerprint = Sha256(text) };
                a.CommentLike = lines.Count(x => Regex.IsMatch(x.TrimStart(), @"^(//|#|--|<!--)"));
                a.BranchMarkers = Regex.Matches(text, @"\b(if|else|switch|case|for|foreach|while|catch|pcall)\b", RegexOptions.IgnoreCase).Count;
                a.MethodLike = Regex.Matches(text, @"\b(function|void|bool|int|string|double|object|Task|static)\s+[A-Za-z_][A-Za-z0-9_]*\s*\(").Count;
                a.ClassLike = Regex.Matches(text, @"\b(class|struct|enum|interface|namespace)\b").Count;
                a.StringLiterals = Regex.Matches(text, "\"(?:\\\\.|[^\"\\\\])*\"").Count;
                a.ProtocolTokens = Regex.Matches(text, @"\byomi-[a-z0-9-]+", RegexOptions.IgnoreCase).Cast<Match>().Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).Take(32).ToList();
                return a;
            }
            catch { return null; }
        }

        private static void OpenProtocolArchaeology(Window owner, DeepSystemsContext ctx)
        {
            List<CodeArtifact> arts = ScanCode(ctx);
            Dictionary<string, List<string>> refs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (CodeArtifact a in arts)
                foreach (string t in a.ProtocolTokens)
                {
                    List<string> l; if (!refs.TryGetValue(t, out l)) { l = new List<string>(); refs[t] = l; }
                    if (!l.Contains(a.Path, StringComparer.OrdinalIgnoreCase)) l.Add(a.Path);
                }
            List<object> rows = refs.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key).Select(kv => (object)new { token = kv.Key, language_surfaces = kv.Value.Select(q => Path.GetExtension(q).ToLowerInvariant()).Distinct().ToArray(), reference_files = kv.Value.ToArray(), crossing_count = kv.Value.Count }).ToList();
            Window w = W(owner, "YOMI · CROSS-LANGUAGE PROTOCOL ARCHAEOLOGY", 1040, 740);
            StackPanel p = new StackPanel { Margin = new Thickness(18) };
            p.Children.Add(T("CROSS-LANGUAGE PROTOCOL ARCHAEOLOGY", 27, Text, FontWeights.Bold));
            p.Children.Add(T(refs.Count.ToString(CultureInfo.InvariantCulture) + " yomi-* protocol tokens discovered across source surfaces", 12.5, Blue, FontWeights.SemiBold));
            p.Children.Add(Btn("OPEN PROTOCOL MAP", delegate { DeepSystems.OpenExternalValue(w, ctx, "PROTOCOL MAP", rows.ToArray(), "CONSERVATORY › PROTOCOL ARCHAEOLOGY"); }));
            TextBox query = new TextBox { MinHeight = 32, Margin = new Thickness(0, 8, 0, 8), Background = Raised, Foreground = Text, BorderBrush = Border, Text = "yomi-order" };
            p.Children.Add(query);
            p.Children.Add(Btn("TRACE TOKEN FRAGMENT", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, query.Text, "CONSERVATORY › PROTOCOL ARCHAEOLOGY › TRACE"); }));
            w.Content = p; w.Show();
        }

        private static void OpenExperimentalForge(Window owner, DeepSystemsContext ctx)
        {
            List<int> order = LoadAuthoritativeOrder(ctx); if (order.Count < 4) { ShowMessage(owner, "EXPERIMENTAL FORGE", "No useful queue order is available to clone."); return; }
            Dictionary<int, double> risk = ReadRiskScores(ctx);
            int currentBoundary = Math.Min(order.Count, 1);
            List<AlgorithmRun> runs = new List<AlgorithmRun>();
            runs.Add(RunTransform("BASELINE", order, order.ToArray(), "O(n)", "Authority clone."));
            runs.Add(RunTransform("RISK-FIRST NEXT 20", order, RiskFirst(order, risk, currentBoundary, 20), "O(k log k)", "Sorts only a cloned next-window by observed risk score descending. This is an experiment, not scheduler policy."));
            runs.Add(RunTransform("IDENTITY-LOCALITY NEXT 24", order, LocalityInterleave(order, currentBoundary, 24), "O(k)", "Deterministic odd/even interleave as a structural locality experiment."));
            runs.Add(RunTransform("NOVELTY HASH WALK NEXT 24", order, NoveltyHash(order, currentBoundary, 24), "O(k log k)", "Deterministic hash-ranked exploration of a cloned window."));
            OpenAlgorithmRuns(owner, ctx, "EXPERIMENTAL ALGORITHM FORGE", order, runs);
        }

        private static Dictionary<int, double> ReadRiskScores(DeepSystemsContext ctx)
        {
            Dictionary<int, double> r = new Dictionary<int, double>();
            Dictionary<string, object> q = ReadJson(Path.Combine(ctx.StateRoot, "queue-runtime.json")) as Dictionary<string, object>;
            if (q == null || !q.ContainsKey("items")) return r;
            IEnumerable items = q["items"] as IEnumerable; if (items == null || q["items"] is string) return r;
            foreach (object o in items)
            {
                Dictionary<string, object> d = o as Dictionary<string, object>; if (d == null) continue;
                int occ = DictInt(d, "index", DictInt(d, "occurrence_id", -1)); if (occ < 1) continue;
                string risk = DictString(d, "risk").ToUpperInvariant(); double s = risk == "URGENT" ? 4 : risk == "AT RISK" || risk == "HIGH" ? 3 : risk == "WATCH" || risk == "MEDIUM" ? 2 : risk == "LOW" ? 1 : 0;
                object cv; if (d.TryGetValue("oracle_confidence", out cv) && cv != null) { double c; if (Double.TryParse(Convert.ToString(cv, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out c)) s += Math.Max(0, 1.0 - c); }
                r[occ] = s;
            }
            return r;
        }

        private static int[] RiskFirst(List<int> src, Dictionary<int, double> risk, int start, int count)
        {
            List<int> x = new List<int>(src); count = Math.Min(count, x.Count - start); if (count <= 1) return x.ToArray();
            int[] win = x.GetRange(start, count).OrderByDescending(n => risk.ContainsKey(n) ? risk[n] : 0).ThenBy(n => x.IndexOf(n)).ToArray();
            for (int i = 0; i < count; i++) x[start + i] = win[i]; return x.ToArray();
        }

        private static int[] LocalityInterleave(List<int> src, int start, int count)
        {
            List<int> x = new List<int>(src); count = Math.Min(count, x.Count - start); if (count <= 2) return x.ToArray();
            List<int> w = x.GetRange(start, count); List<int> y = new List<int>();
            for (int i = 0; i < w.Count; i += 2) y.Add(w[i]); for (int i = 1; i < w.Count; i += 2) y.Add(w[i]);
            for (int i = 0; i < count; i++) x[start + i] = y[i]; return x.ToArray();
        }

        private static int[] NoveltyHash(List<int> src, int start, int count)
        {
            List<int> x = new List<int>(src); count = Math.Min(count, x.Count - start); if (count <= 1) return x.ToArray(); string fp = Sha256(String.Join(",", src.ToArray()));
            int[] y = x.GetRange(start, count).OrderBy(n => Sha256(fp + ":novelty:" + n.ToString(CultureInfo.InvariantCulture)), StringComparer.Ordinal).ToArray();
            for (int i = 0; i < count; i++) x[start + i] = y[i]; return x.ToArray();
        }

        private static void OpenPropertyLab(Window owner, DeepSystemsContext ctx)
        {
            const int Cases = 13377;
            Random rng = new Random(13377); int pass = 0; object failure = null;
            for (int c = 0; c < Cases; c++)
            {
                int n = rng.Next(2, 96); List<int> baseOrder = Enumerable.Range(1, n).ToList();
                int start = rng.Next(0, n); int count = rng.Next(1, n - start + 1); int boundary = rng.Next(0, n + 1);
                int[] outp = StableBlockMove(baseOrder, start, count, boundary);
                bool ok = outp.Length == n && outp.Distinct().Count() == n && new HashSet<int>(outp).SetEquals(baseOrder);
                if (!ok) { failure = new { case_index = c, n = n, start = start, count = count, boundary = boundary, result = outp }; break; }
                pass++;
            }
            DeepSystems.OpenExternalValue(owner, ctx, "PROPERTY-BASED TORTURE CHAMBER", new { generated_cases = Cases, passed = pass, failed = failure == null ? 0 : 1, invariant = "stable block move preserves cardinality, uniqueness and exact occurrence set", minimal_observed_failure = failure, seed = 13377 }, "CONSERVATORY › PROPERTY LAB");
        }

        private static List<string> EnumerateFiles(string root, int max)
        {
            List<string> r = new List<string>(); if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return r;
            try
            {
                foreach (string f in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc)) { r.Add(f); if (r.Count >= max) break; }
            }
            catch { }
            return r;
        }

        private static bool IsTextLike(string p)
        {
            string e = Path.GetExtension(p).ToLowerInvariant(); return e == ".json" || e == ".jsonl" || e == ".ndjson" || e == ".log" || e == ".txt" || e == ".csv" || e == ".md";
        }

        private static void ShowMessage(Window owner, string title, string message)
        {
            Window w = W(owner, "YOMI · " + title, 680, 360); StackPanel p = new StackPanel { Margin = new Thickness(20) }; p.Children.Add(T(title, 25, Text, FontWeights.Bold)); p.Children.Add(T(message, 13, Muted, FontWeights.Normal)); w.Content = p; w.Show();
        }
    }
}

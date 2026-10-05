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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yomi.ProductShell
{
    internal sealed class CausalEvidenceEvent
    {
        public DateTime Utc;
        public string Source;
        public string Kind;
        public string Summary;
        public string Raw;
        public object Value;
        public string Fingerprint;
        public Dictionary<string, string> Identity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public override string ToString()
        {
            string t = Utc == DateTime.MinValue ? "--:--:--.---" : Utc.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            return t + "   " + (Kind ?? "EVENT") + "   " + (Summary ?? "") + "   · " + Path.GetFileName(Source ?? "");
        }
    }

    internal sealed class CausalCluster
    {
        public string Id;
        public List<CausalEvidenceEvent> Events = new List<CausalEvidenceEvent>();
        public Dictionary<string, int> IdentityFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public DateTime FirstUtc;
        public DateTime LastUtc;
        public int SourceCount;
        public string DominantIdentity;
        public int DominantCount;
        public override string ToString()
        {
            string span = FirstUtc == DateTime.MinValue || LastUtc == DateTime.MinValue ? "time n/a" : Math.Max(0, (LastUtc - FirstUtc).TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture) + "s";
            return Id + "   ·   " + Events.Count.ToString(CultureInfo.InvariantCulture) + " events   ·   " + SourceCount.ToString(CultureInfo.InvariantCulture) + " streams   ·   " + span + "   ·   " + (DominantIdentity ?? "no dominant identity");
        }
    }

    internal sealed class CausalBranch
    {
        public string Id;
        public string ParentId;
        public string RootId;
        public int Generation;
        public string CreatedUtc;
        public string Name;
        public string ScenarioCode;
        public string BaselineSessionId;
        public int BaselineRevision;
        public int AnchorOccurrence;
        public List<int> BaselineOrder = new List<int>();
        public List<int> BranchOrder = new List<int>();
        public string Hypothesis;
        public string Notes;
        public string ParentFingerprint;
        public string BranchFingerprint;
        public int ChangedSlots;
        public int ParentChangedSlots;
        public double StructuralDistance;
        public int LongestStablePrefix;
        public double AdjacencyRetention;
        public override string ToString()
        {
            return "G" + Generation.ToString(CultureInfo.InvariantCulture) + "  " + (Name ?? "branch") + "  ·  Δ" + ChangedSlots.ToString(CultureInfo.InvariantCulture) + " slots  ·  d=" + StructuralDistance.ToString("0.000", CultureInfo.InvariantCulture) + "  ·  " + (Id ?? "");
        }
    }

    internal sealed class CausalPathHop
    {
        public CausalEvidenceEvent Event;
        public int Score;
        public string Why;
        public override string ToString()
        {
            return "[" + Score.ToString("000", CultureInfo.InvariantCulture) + "]  " + (Event == null ? "(missing)" : Event.ToString()) + "   ·   " + (Why ?? "");
        }
    }

    internal static class CausalGraphLab
    {
        private static readonly Brush Bg = B("#090D12");
        private static readonly Brush Surface = B("#131920");
        private static readonly Brush Raised = B("#1B242E");
        private static readonly Brush Border = B("#33414E");
        private static readonly Brush Text = B("#EEF5FA");
        private static readonly Brush Muted = B("#9EAEBB");
        private static readonly Brush Accent = B("#7CF2BE");
        private static readonly Brush Blue = B("#71B9FF");
        private static readonly Brush Amber = B("#FFD276");
        private static readonly Brush Danger = B("#FF8080");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static string BranchPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-counterfactual-branches.json"); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            OpenHub(owner, ctx);
        }

        internal static void OpenPathFromFingerprint(Window owner, DeepSystemsContext ctx, string fingerprint)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(fingerprint)) return;
            var events = LoadEvents(ctx, 320);
            var anchor = events.FirstOrDefault(x => String.Equals(x.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));
            if (anchor == null)
            {
                DeepSystems.OpenExternalValue(owner, ctx, "CAUSAL PATH ANCHOR NOT FOUND", new Dictionary<string, object> { { "fingerprint", fingerprint }, { "reason", "anchor is outside the current bounded evidence universe" } }, "ATLAS › CAUSAL GRAPH › PATH › MISSING");
                return;
            }
            OpenCandidatePath(owner, ctx, anchor, events);
        }

        internal static void OpenIdentityToken(Window owner, DeepSystemsContext ctx, string key, string value)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value)) return;
            OpenIdentityJourney(owner, ctx, IdentityToken(key, value), true);
        }

        internal static void OpenBranchUniverse(Window owner, DeepSystemsContext ctx, string branchId)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(branchId)) return;
            var b = LoadBranches(ctx).FirstOrDefault(x => String.Equals(x.Id, branchId, StringComparison.OrdinalIgnoreCase));
            if (b != null) OpenBranchDossier(owner, ctx, b);
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
            var b = new Button { Content = text, MinHeight = 34, Padding = new Thickness(11, 5, 11, 5), Margin = new Thickness(0, 0, 8, 8), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
            if (click != null) b.Click += click;
            return b;
        }

        private static Border Card(UIElement child)
        {
            return new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14), Margin = new Thickness(0, 0, 12, 12), Child = child };
        }

        private static void Portal(Panel panel, string badge, string title, string subtitle, Action open)
        {
            var s = new StackPanel();
            var b = T(badge, 11, Accent, FontWeights.Bold); b.Margin = new Thickness(0, 0, 0, 4); s.Children.Add(b);
            s.Children.Add(T(title, 18, Text, FontWeights.SemiBold));
            var d = T(subtitle, 12.5, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 6, 0, 12); s.Children.Add(d);
            var enter = Btn("ENTER SYSTEM  ›", delegate { open(); }); enter.HorizontalAlignment = HorizontalAlignment.Left; s.Children.Add(enter);
            panel.Children.Add(Card(s));
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx)
        {
            var w = W(owner, "YOMI · CAUSAL GRAPH LAB", 1160, 780);
            var root = new DockPanel { Margin = new Thickness(18) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            head.Children.Add(T("CAUSAL GRAPH LAB", 26, Text, FontWeights.SemiBold));
            head.Children.Add(T("Evidence topology, identity wormholes, branch genealogy and structural divergence. Connections are scored evidence relationships, never fabricated proof of causation.", 13, Muted, FontWeights.Normal));
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; root.Children.Add(scroll);
            var grid = new UniformGrid { Columns = 2 }; scroll.Content = grid;
            Portal(grid, "GENEALOGY", "SIMULATION ECOLOGY", "Counterfactual branches become a persistent ancestry graph with parent/child generations, fingerprints and structural-distance metrics.", delegate { OpenBranchEcology(w, ctx); });
            Portal(grid, "Δ MATRIX", "BRANCH DIVERGENCE MATRIX", "Compare recent alternate histories pairwise. Every matrix cell is a portal into exact slot-level divergence.", delegate { OpenBranchMatrix(w, ctx); });
            Portal(grid, "CLUSTERS", "EVIDENCE CHAIN CLUSTERS", "Discover connected components formed by strong shared identities, hash ancestry and local event continuity.", delegate { OpenClusters(w, ctx); });
            Portal(grid, "WORMHOLE", "IDENTITY WORMHOLE", "Pick an intent, occurrence, source key, session, fingerprint or revision and follow it across every bounded evidence stream.", delegate { OpenIdentityUniverse(w, ctx); });
            Portal(grid, "COUPLING", "DEPENDENCY HEATMAP", "Visualize which semantic identity dimensions repeatedly co-occur in the same evidence records. Coupling, not causation.", delegate { OpenDependencyHeatmap(w, ctx); });
            Portal(grid, "PATH", "CAUSAL-CANDIDATE PATH EXPLORER", "Choose an anchor event and let YOMI greedily construct a scored evidence path through its strongest neighboring relationships.", delegate { OpenPathAnchorIndex(w, ctx); });
            Portal(grid, "LIVE", "LIVE IDENTITY TRACKER", "Follow one identity while YOMI is running and watch new matching evidence records appear without mutating anything.", delegate { OpenIdentityUniverse(w, ctx); });
            Portal(grid, "RAW", "GRAPH DATA MICROSCOPE", "Open the computed evidence graph summary as a recursive Deep Systems object and tunnel into whatever looks interesting.", delegate { DeepSystems.OpenExternalValue(w, ctx, "CAUSAL GRAPH SUMMARY", BuildGraphSummary(ctx), "ATLAS › CAUSAL GRAPH › SUMMARY"); });
            w.Show();
        }

        // -----------------------------------------------------------------
        // Evidence ingestion / identity graph
        // -----------------------------------------------------------------
        private static List<string> EvidenceFiles(DeepSystemsContext ctx)
        {
            var files = new List<string>();
            foreach (string root in new[] { ctx.StateRoot, Path.Combine(ctx.DataRoot, "logs") })
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (string p in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(p).ToLowerInvariant();
                        string n = Path.GetFileName(p).ToLowerInvariant();
                        if (ext == ".jsonl" || ext == ".log" || ext == ".txt" || n.Contains("journal") || n.Contains("event") || n.Contains("history") || n.Contains("flight")) files.Add(p);
                        if (files.Count >= 96) break;
                    }
                }
                catch { }
                if (files.Count >= 96) break;
            }
            return files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> Tail(string path, int max)
        {
            var q = new Queue<string>(max + 1);
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    const long MaxTailBytes = 524288;
                    long start = Math.Max(0, fs.Length - MaxTailBytes);
                    fs.Seek(start, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs, Encoding.UTF8, true))
                    {
                        if (start > 0) sr.ReadLine(); // discard partial first line from bounded byte window
                        string line;
                        while ((line = sr.ReadLine()) != null) { q.Enqueue(line); if (q.Count > max) q.Dequeue(); }
                    }
                }
            }
            catch { }
            return q.ToList();
        }

        private static List<CausalEvidenceEvent> LoadEvents(DeepSystemsContext ctx, int perFile)
        {
            return LoadEvents(ctx, perFile, 96);
        }

        private static List<CausalEvidenceEvent> LoadEvents(DeepSystemsContext ctx, int perFile, int maxFiles)
        {
            var result = new List<CausalEvidenceEvent>();
            var paths = EvidenceFiles(ctx).OrderByDescending(p => { try { return File.GetLastWriteTimeUtc(p); } catch { return DateTime.MinValue; } }).Take(Math.Max(1, maxFiles)).ToList();
            foreach (string path in paths)
            {
                foreach (string line in Tail(path, perFile))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var e = Parse(path, line); if (e != null) result.Add(e);
                    if (result.Count >= 14000) break;
                }
                if (result.Count >= 14000) break;
            }
            return result.OrderBy(x => x.Utc == DateTime.MinValue ? DateTime.MaxValue : x.Utc).ThenBy(x => x.Source).ToList();
        }

        private static string EvidenceSignature(DeepSystemsContext ctx, int maxFiles)
        {
            var sb = new StringBuilder();
            foreach (string p in EvidenceFiles(ctx).OrderByDescending(x => { try { return File.GetLastWriteTimeUtc(x); } catch { return DateTime.MinValue; } }).Take(Math.Max(1, maxFiles)))
            {
                try { var f = new FileInfo(p); sb.Append(p).Append('|').Append(f.Length).Append('|').Append(f.LastWriteTimeUtc.Ticks).Append(';'); } catch { }
            }
            return Sha(sb.ToString());
        }

        private static CausalEvidenceEvent Parse(string source, string line)
        {
            object value = null; Dictionary<string, object> map = null;
            string s = (line ?? "").Trim();
            if (s.StartsWith("{") || s.StartsWith("[")) { try { value = Json.DeserializeObject(s); map = value as Dictionary<string, object>; } catch { } }
            var e = new CausalEvidenceEvent { Source = source, Raw = line, Value = value ?? line, Utc = DateTime.MinValue };
            if (map != null)
            {
                e.Utc = EventTime(map);
                e.Kind = First(map, new[] { "event", "type", "kind", "state", "action", "phase" }, "EVENT").ToUpperInvariant();
                e.Summary = First(map, new[] { "message", "reason", "detail", "decision_reason", "summary", "title" }, e.Kind);
                foreach (string key in IdentityKeys())
                {
                    object v; if (map.TryGetValue(key, out v) && v != null)
                    {
                        string text = Convert.ToString(v, CultureInfo.InvariantCulture);
                        if (!String.IsNullOrWhiteSpace(text)) e.Identity[key] = text;
                    }
                }
            }
            else
            {
                e.Kind = GuessKind(line); e.Summary = line == null ? "" : (line.Length > 220 ? line.Substring(0, 220) + "…" : line); e.Utc = TextTime(line);
            }
            e.Fingerprint = Sha((source ?? "") + "\n" + (line ?? ""));
            return e;
        }

        private static string[] IdentityKeys()
        {
            return new[] { "intent_id", "occurrence_id", "occurrence", "source_key", "session_id", "entry_hash", "prev_hash", "plan_fingerprint", "order_revision", "work_generation", "failure_domain", "route_label", "decision_reason", "transaction_id", "generation" };
        }

        private static string First(Dictionary<string, object> map, string[] keys, string fallback)
        {
            foreach (string k in keys) { object v; if (map.TryGetValue(k, out v) && v != null) { string s = Convert.ToString(v, CultureInfo.InvariantCulture); if (!String.IsNullOrWhiteSpace(s)) return s; } }
            return fallback;
        }

        private static DateTime EventTime(Dictionary<string, object> map)
        {
            foreach (string k in new[] { "utc", "timestamp_utc", "created_utc", "updated_utc", "time_utc", "timestamp", "time" })
            {
                object v; if (!map.TryGetValue(k, out v) || v == null) continue; DateTime d;
                if (DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out d)) return d;
            }
            foreach (string k in new[] { "unix", "created_unix", "updated_unix", "timestamp_unix" })
            {
                object v; if (!map.TryGetValue(k, out v) || v == null) continue; double d;
                if (Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) try { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(d); } catch { }
            }
            return DateTime.MinValue;
        }

        private static DateTime TextTime(string line)
        {
            if (String.IsNullOrWhiteSpace(line)) return DateTime.MinValue; string p = line.Substring(0, Math.Min(40, line.Length)).Trim(' ', '[', ']'); DateTime d;
            return DateTime.TryParse(p, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d) ? d.ToUniversalTime() : DateTime.MinValue;
        }

        private static string GuessKind(string line)
        {
            string u = (line ?? "").ToUpperInvariant(); foreach (string k in new[] { "ERROR", "FAIL", "WARN", "COMMIT", "START", "STOP", "READY", "ORACLE", "FREEZE", "REHEARS", "CACHE", "VIDEO", "AUDIO", "QUEUE", "INTENT", "RECOVERY" }) if (u.Contains(k)) return k; return "LOG";
        }

        private static string IdentityToken(string key, string value) { return (key ?? "").ToLowerInvariant() + "=" + (value ?? ""); }
        private static string EventText(CausalEvidenceEvent e) { return (e.Kind ?? "") + " " + (e.Summary ?? "") + " " + (e.Source ?? "") + " " + String.Join(" ", e.Identity.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + " " + (e.Raw ?? ""); }

        private static int LinkScore(CausalEvidenceEvent a, CausalEvidenceEvent b)
        {
            int score = 0;
            foreach (var kv in a.Identity)
            {
                string bv; if (b.Identity.TryGetValue(kv.Key, out bv) && String.Equals(kv.Value, bv, StringComparison.OrdinalIgnoreCase))
                {
                    switch (kv.Key.ToLowerInvariant())
                    {
                        case "intent_id": score += 100; break;
                        case "entry_hash": score += 95; break;
                        case "plan_fingerprint": score += 90; break;
                        case "source_key": score += 80; break;
                        case "occurrence_id": case "occurrence": score += 75; break;
                        case "transaction_id": score += 72; break;
                        case "order_revision": score += 45; break;
                        case "work_generation": score += 35; break;
                        case "session_id": score += 14; break;
                        default: score += 18; break;
                    }
                }
            }
            string prevA, entryB, prevB, entryA;
            if (a.Identity.TryGetValue("prev_hash", out prevA) && b.Identity.TryGetValue("entry_hash", out entryB) && String.Equals(prevA, entryB, StringComparison.OrdinalIgnoreCase)) score += 120;
            if (b.Identity.TryGetValue("prev_hash", out prevB) && a.Identity.TryGetValue("entry_hash", out entryA) && String.Equals(prevB, entryA, StringComparison.OrdinalIgnoreCase)) score += 120;
            if (a.Utc != DateTime.MinValue && b.Utc != DateTime.MinValue)
            {
                double dt = Math.Abs((a.Utc - b.Utc).TotalSeconds);
                if (dt <= 0.5) score += 20; else if (dt <= 2) score += 12; else if (dt <= 10) score += 5;
                if (String.Equals(a.Source, b.Source, StringComparison.OrdinalIgnoreCase) && dt <= 3) score += 10;
            }
            return score;
        }

        private static string LinkWhy(CausalEvidenceEvent a, CausalEvidenceEvent b)
        {
            var why = new List<string>();
            foreach (var kv in a.Identity) { string bv; if (b.Identity.TryGetValue(kv.Key, out bv) && String.Equals(kv.Value, bv, StringComparison.OrdinalIgnoreCase)) why.Add(kv.Key); }
            string pa, eb, pb, ea; if (a.Identity.TryGetValue("prev_hash", out pa) && b.Identity.TryGetValue("entry_hash", out eb) && String.Equals(pa, eb, StringComparison.OrdinalIgnoreCase)) why.Add("hash-parent"); if (b.Identity.TryGetValue("prev_hash", out pb) && a.Identity.TryGetValue("entry_hash", out ea) && String.Equals(pb, ea, StringComparison.OrdinalIgnoreCase)) why.Add("hash-child");
            if (a.Utc != DateTime.MinValue && b.Utc != DateTime.MinValue) { double d = Math.Abs((a.Utc - b.Utc).TotalSeconds); if (d <= 10) why.Add("Δt=" + d.ToString("0.000", CultureInfo.InvariantCulture) + "s"); }
            return String.Join(" + ", why.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }

        // -----------------------------------------------------------------
        // Identity wormholes / live tracker
        // -----------------------------------------------------------------
        private static Dictionary<string, List<CausalEvidenceEvent>> BuildIdentityIndex(List<CausalEvidenceEvent> events)
        {
            var index = new Dictionary<string, List<CausalEvidenceEvent>>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in events)
            {
                foreach (var kv in e.Identity)
                {
                    string token = IdentityToken(kv.Key, kv.Value); List<CausalEvidenceEvent> list;
                    if (!index.TryGetValue(token, out list)) { list = new List<CausalEvidenceEvent>(); index[token] = list; }
                    list.Add(e);
                }
            }
            return index;
        }

        private static void OpenIdentityUniverse(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 300); var index = BuildIdentityIndex(events);
            var w = W(owner, "YOMI · IDENTITY WORMHOLE", 1160, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; head.Children.Add(T("IDENTITY WORMHOLE", 23, Text, FontWeights.SemiBold)); head.Children.Add(T(index.Count.ToString(CultureInfo.InvariantCulture) + " semantic identities discovered across " + events.Count.ToString(CultureInfo.InvariantCulture) + " bounded evidence records. Double-click one and follow it through the system.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var search = new TextBox { Height = 32, Margin = new Thickness(0, 0, 0, 8), Background = Raised, Foreground = Text, BorderBrush = Border }; DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; root.Children.Add(list);
            Action refresh = delegate
            {
                string q = (search.Text ?? "").Trim(); list.Items.Clear();
                foreach (var kv in index.OrderByDescending(x => x.Value.Count).ThenBy(x => x.Key).Where(x => q.Length == 0 || x.Key.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(2500))
                    list.Items.Add(new ListBoxItem { Content = kv.Value.Count.ToString("0000", CultureInfo.InvariantCulture) + "  ·  " + kv.Key, Tag = kv.Key, Foreground = Text, Background = Bg });
            };
            search.TextChanged += delegate { refresh(); };
            list.MouseDoubleClick += delegate { var li = list.SelectedItem as ListBoxItem; string token = li == null ? null : li.Tag as string; if (!String.IsNullOrWhiteSpace(token)) OpenIdentityJourney(w, ctx, token, true); };
            refresh(); w.Show();
        }

        private static void OpenIdentityJourney(Window owner, DeepSystemsContext ctx, string token, bool liveCapable)
        {
            var w = W(owner, "YOMI · IDENTITY JOURNEY · " + token, 1180, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; head.Children.Add(T("IDENTITY JOURNEY", 23, Text, FontWeights.SemiBold)); head.Children.Add(T(token, 13, Accent, FontWeights.SemiBold)); head.Children.Add(T("Every bounded evidence record carrying this exact semantic identity. LIVE FOLLOW remains passive and re-scans only recent local evidence tails.", 12, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) }; DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            var live = new CheckBox { Content = "LIVE FOLLOW", Foreground = Text, Margin = new Thickness(0, 7, 14, 0), IsChecked = false }; if (liveCapable) tools.Children.Add(live);
            tools.Children.Add(Btn("DEEP EVIDENCE SEARCH", delegate { string value = token.Contains("=") ? token.Substring(token.IndexOf('=') + 1) : token; DeepSystems.OpenExternalEvidenceSearch(w, ctx, value, "ATLAS › CAUSAL GRAPH › IDENTITY › " + token); }));
            var status = T("", 11.5, Muted, FontWeights.Normal); tools.Children.Add(status);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; root.Children.Add(list);
            string[] parts = token.Split(new[] { '=' }, 2); string key = parts.Length > 0 ? parts[0] : ""; string val = parts.Length > 1 ? parts[1] : "";
            string lastLiveSignature = "";
            Action refresh = delegate
            {
                int fileCap = live.IsChecked == true ? 24 : 96;
                var events = LoadEvents(ctx, live.IsChecked == true ? 180 : 340, fileCap);
                var rows = events.Where(e => { string x; return e.Identity.TryGetValue(key, out x) && String.Equals(x, val, StringComparison.OrdinalIgnoreCase); }).OrderBy(e => e.Utc == DateTime.MinValue ? DateTime.MaxValue : e.Utc).ToList();
                string selected = null; var old = list.SelectedItem as CausalEvidenceEvent; if (old != null) selected = old.Fingerprint;
                list.ItemsSource = rows; if (selected != null) { var match = rows.FirstOrDefault(x => x.Fingerprint == selected); if (match != null) list.SelectedItem = match; }
                lastLiveSignature = EvidenceSignature(ctx, fileCap);
                status.Text = rows.Count.ToString(CultureInfo.InvariantCulture) + " records · refreshed " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            };
            list.MouseDoubleClick += delegate { var e = list.SelectedItem as CausalEvidenceEvent; if (e != null) OpenEvidenceDossier(w, ctx, e); };
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.1) };
            timer.Tick += delegate
            {
                if (live.IsChecked != true || !w.IsVisible || w.WindowState == WindowState.Minimized) return;
                string sig = EvidenceSignature(ctx, 24);
                if (!String.Equals(sig, lastLiveSignature, StringComparison.Ordinal)) refresh();
            };
            timer.Start(); w.Closed += delegate { timer.Stop(); };
            live.Checked += delegate { lastLiveSignature = ""; refresh(); }; refresh(); w.Show();
        }

        private static void OpenEvidenceDossier(Window owner, DeepSystemsContext ctx, CausalEvidenceEvent e)
        {
            var doc = new Dictionary<string, object>(); doc["utc"] = e.Utc == DateTime.MinValue ? "" : e.Utc.ToString("o", CultureInfo.InvariantCulture); doc["kind"] = e.Kind; doc["summary"] = e.Summary; doc["source"] = e.Source; doc["fingerprint"] = e.Fingerprint; doc["identity"] = e.Identity; doc["value"] = e.Value;
            DeepSystems.OpenExternalValue(owner, ctx, "CAUSAL EVIDENCE · " + (e.Kind ?? "EVENT"), doc, "ATLAS › CAUSAL GRAPH › EVIDENCE › " + e.Fingerprint.Substring(0, Math.Min(12, e.Fingerprint.Length)));
        }

        // -----------------------------------------------------------------
        // Evidence clustering
        // -----------------------------------------------------------------
        private sealed class UnionFind
        {
            private readonly int[] _p; private readonly byte[] _r;
            public UnionFind(int n) { _p = new int[n]; _r = new byte[n]; for (int i = 0; i < n; i++) _p[i] = i; }
            public int Find(int x) { while (_p[x] != x) { _p[x] = _p[_p[x]]; x = _p[x]; } return x; }
            public void Union(int a, int b) { a = Find(a); b = Find(b); if (a == b) return; if (_r[a] < _r[b]) _p[a] = b; else if (_r[a] > _r[b]) _p[b] = a; else { _p[b] = a; _r[a]++; } }
        }

        private static List<CausalCluster> BuildClusters(List<CausalEvidenceEvent> events)
        {
            int n = events.Count; var uf = new UnionFind(n);
            var strong = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var entry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var pendingPrev = new List<KeyValuePair<int, string>>();
            string[] keys = { "intent_id", "occurrence_id", "source_key", "plan_fingerprint", "transaction_id" };
            for (int i = 0; i < n; i++)
            {
                foreach (string k in keys)
                {
                    string v; if (!events[i].Identity.TryGetValue(k, out v) || String.IsNullOrWhiteSpace(v)) continue; string token = IdentityToken(k, v); int first;
                    if (strong.TryGetValue(token, out first)) uf.Union(i, first); else strong[token] = i;
                }
                string h; if (events[i].Identity.TryGetValue("entry_hash", out h) && !String.IsNullOrWhiteSpace(h)) { int first; if (entry.TryGetValue(h, out first)) uf.Union(i, first); else entry[h] = i; }
                string prev; if (events[i].Identity.TryGetValue("prev_hash", out prev) && !String.IsNullOrWhiteSpace(prev)) pendingPrev.Add(new KeyValuePair<int, string>(i, prev));
            }
            foreach (var p in pendingPrev) { int parent; if (entry.TryGetValue(p.Value, out parent)) uf.Union(p.Key, parent); }
            // Local same-stream continuity is intentionally weak: only records within 750ms are joined.
            var bySource = events.Select((e, i) => new { E = e, I = i }).Where(x => x.E.Utc != DateTime.MinValue).GroupBy(x => x.E.Source ?? "", StringComparer.OrdinalIgnoreCase);
            foreach (var g in bySource)
            {
                var xs = g.OrderBy(x => x.E.Utc).ToList(); for (int i = 1; i < xs.Count; i++) if ((xs[i].E.Utc - xs[i - 1].E.Utc).TotalMilliseconds <= 750) uf.Union(xs[i].I, xs[i - 1].I);
            }
            var groups = new Dictionary<int, List<CausalEvidenceEvent>>(); for (int i = 0; i < n; i++) { int r = uf.Find(i); List<CausalEvidenceEvent> g; if (!groups.TryGetValue(r, out g)) { g = new List<CausalEvidenceEvent>(); groups[r] = g; } g.Add(events[i]); }
            var result = new List<CausalCluster>(); int seq = 1;
            foreach (var g in groups.Values.Where(x => x.Count >= 2).OrderByDescending(x => x.Count))
            {
                var c = new CausalCluster(); c.Id = "CLUSTER-" + seq.ToString("0000", CultureInfo.InvariantCulture); seq++; c.Events = g.OrderBy(x => x.Utc == DateTime.MinValue ? DateTime.MaxValue : x.Utc).ToList();
                var timed = c.Events.Where(x => x.Utc != DateTime.MinValue).ToList(); c.FirstUtc = timed.Count == 0 ? DateTime.MinValue : timed.First().Utc; c.LastUtc = timed.Count == 0 ? DateTime.MinValue : timed.Last().Utc; c.SourceCount = c.Events.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                foreach (var e in c.Events) foreach (var kv in e.Identity) { string token = IdentityToken(kv.Key, kv.Value); int v; c.IdentityFrequency.TryGetValue(token, out v); c.IdentityFrequency[token] = v + 1; }
                var dom = c.IdentityFrequency.OrderByDescending(x => x.Value).ThenBy(x => x.Key).FirstOrDefault(); if (!String.IsNullOrWhiteSpace(dom.Key)) { c.DominantIdentity = dom.Key; c.DominantCount = dom.Value; }
                result.Add(c);
            }
            return result;
        }

        private static void OpenClusters(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 260); var clusters = BuildClusters(events);
            var w = W(owner, "YOMI · EVIDENCE CHAIN CLUSTERS", 1180, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; h.Children.Add(T("EVIDENCE CHAIN CLUSTERS", 23, Text, FontWeights.SemiBold)); h.Children.Add(T(clusters.Count.ToString(CultureInfo.InvariantCulture) + " connected evidence components. Edges come from strong shared identities, exact hash ancestry, or sub-second same-stream continuity.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = clusters.Take(1500).ToList(), Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var c = list.SelectedItem as CausalCluster; if (c != null) OpenClusterDossier(w, ctx, c); }; root.Children.Add(list); w.Show();
        }

        private static void OpenClusterDossier(Window owner, DeepSystemsContext ctx, CausalCluster c)
        {
            var w = W(owner, "YOMI · " + c.Id, 1180, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; head.Children.Add(T(c.Id, 23, Accent, FontWeights.SemiBold)); head.Children.Add(T(c.Events.Count.ToString(CultureInfo.InvariantCulture) + " events · " + c.SourceCount.ToString(CultureInfo.InvariantCulture) + " streams · dominant " + (c.DominantIdentity ?? "none"), 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) }; if (!String.IsNullOrWhiteSpace(c.DominantIdentity)) tools.Children.Add(Btn("FOLLOW DOMINANT IDENTITY", delegate { OpenIdentityJourney(w, ctx, c.DominantIdentity, true); })); tools.Children.Add(Btn("OPEN CLUSTER OBJECT", delegate { DeepSystems.OpenExternalValue(w, ctx, c.Id, c, "ATLAS › CAUSAL GRAPH › CLUSTER › " + c.Id); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            var list = new ListBox { ItemsSource = c.Events, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var e = list.SelectedItem as CausalEvidenceEvent; if (e != null) OpenEvidenceDossier(w, ctx, e); }; root.Children.Add(list); w.Show();
        }

        // -----------------------------------------------------------------
        // Co-observation heatmap
        // -----------------------------------------------------------------
        private static void OpenDependencyHeatmap(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 300); var keys = IdentityKeys().Where(k => events.Any(e => e.Identity.ContainsKey(k))).Take(11).ToList(); int n = keys.Count; int[,] counts = new int[n, n];
            foreach (var e in events) for (int i = 0; i < n; i++) if (e.Identity.ContainsKey(keys[i])) for (int j = i; j < n; j++) if (e.Identity.ContainsKey(keys[j])) { counts[i, j]++; if (i != j) counts[j, i]++; }
            int max = 1; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) max = Math.Max(max, counts[i, j]);
            var w = W(owner, "YOMI · SEMANTIC CO-OBSERVATION HEATMAP", 1240, 820); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; h.Children.Add(T("SEMANTIC CO-OBSERVATION HEATMAP", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("How often identity dimensions coexist in the same evidence record. Brightness means repeated coupling, not causal dependence. Click any cell to inspect matching evidence.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; root.Children.Add(scroll); var grid = new Grid { Background = Bg }; scroll.Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(130) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); for (int i = 0; i < n; i++) { grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) }); }
            for (int i = 0; i < n; i++)
            {
                var top = T(keys[i], 11, Muted, FontWeights.SemiBold); top.LayoutTransform = new RotateTransform(-52); top.HorizontalAlignment = HorizontalAlignment.Center; top.VerticalAlignment = VerticalAlignment.Bottom; Grid.SetColumn(top, i + 1); Grid.SetRow(top, 0); grid.Children.Add(top);
                var left = T(keys[i], 11.5, Muted, FontWeights.SemiBold); left.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(left, 0); Grid.SetRow(left, i + 1); grid.Children.Add(left);
                for (int j = 0; j < n; j++)
                {
                    int count = counts[i, j]; double ratio = Math.Sqrt((double)count / (double)max); byte r = (byte)(25 + 40 * ratio); byte g = (byte)(38 + 150 * ratio); byte b = (byte)(52 + 115 * ratio); var color = Color.FromRgb(r, g, b); string ka = keys[i], kb = keys[j];
                    var cell = new Button { Content = count.ToString(CultureInfo.InvariantCulture), Background = new SolidColorBrush(color), Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(1), Cursor = Cursors.Hand, ToolTip = ka + " ↔ " + kb + "\n" + count.ToString(CultureInfo.InvariantCulture) + " co-observations" };
                    cell.Click += delegate { OpenPairEvidence(w, ctx, ka, kb); }; Grid.SetColumn(cell, j + 1); Grid.SetRow(cell, i + 1); grid.Children.Add(cell);
                }
            }
            w.Show();
        }

        private static void OpenPairEvidence(Window owner, DeepSystemsContext ctx, string a, string b)
        {
            var rows = LoadEvents(ctx, 320).Where(e => e.Identity.ContainsKey(a) && e.Identity.ContainsKey(b)).ToList(); var w = W(owner, "YOMI · COUPLING · " + a + " ↔ " + b, 1160, 740); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; h.Children.Add(T(a + "  ↔  " + b, 22, Accent, FontWeights.SemiBold)); h.Children.Add(T(rows.Count.ToString(CultureInfo.InvariantCulture) + " evidence records contain both identity dimensions.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = rows, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var e = list.SelectedItem as CausalEvidenceEvent; if (e != null) OpenEvidenceDossier(w, ctx, e); }; root.Children.Add(list); w.Show();
        }

        // -----------------------------------------------------------------
        // Candidate path explorer
        // -----------------------------------------------------------------
        private static void OpenPathAnchorIndex(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 240).OrderByDescending(e => e.Utc == DateTime.MinValue ? DateTime.MinValue : e.Utc).Take(1800).ToList(); var w = W(owner, "YOMI · CAUSAL-CANDIDATE PATH ANCHORS", 1180, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; h.Children.Add(T("CAUSAL-CANDIDATE PATH EXPLORER", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("Choose an anchor. YOMI will greedily walk strongest unused evidence links outward in both temporal directions. This is an investigative path, not a proof graph.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = events, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var e = list.SelectedItem as CausalEvidenceEvent; if (e != null) OpenCandidatePath(w, ctx, e, events); }; root.Children.Add(list); w.Show();
        }

        private static void OpenCandidatePath(Window owner, DeepSystemsContext ctx, CausalEvidenceEvent anchor, List<CausalEvidenceEvent> universe)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase); used.Add(anchor.Fingerprint); var before = Walk(anchor, universe, used, -1, 12); var after = Walk(anchor, universe, used, 1, 12); before.Reverse(); var hops = new List<CausalPathHop>(); hops.AddRange(before); hops.Add(new CausalPathHop { Event = anchor, Score = 999, Why = "ANCHOR" }); hops.AddRange(after);
            var w = W(owner, "YOMI · CANDIDATE PATH", 1220, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; h.Children.Add(T("CAUSAL-CANDIDATE PATH", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("Greedy path through strongest evidence links. Edge score combines semantic identity, exact hash adjacency and temporal proximity.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = hops, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var hop = list.SelectedItem as CausalPathHop; if (hop != null && hop.Event != null) OpenEvidenceDossier(w, ctx, hop.Event); }; root.Children.Add(list); w.Show();
        }

        private static List<CausalPathHop> Walk(CausalEvidenceEvent start, List<CausalEvidenceEvent> universe, HashSet<string> used, int direction, int limit)
        {
            var result = new List<CausalPathHop>(); CausalEvidenceEvent cur = start;
            for (int step = 0; step < limit; step++)
            {
                IEnumerable<CausalEvidenceEvent> candidates = universe.Where(x => !used.Contains(x.Fingerprint));
                if (cur.Utc != DateTime.MinValue) candidates = direction < 0 ? candidates.Where(x => x.Utc != DateTime.MinValue && x.Utc <= cur.Utc) : candidates.Where(x => x.Utc != DateTime.MinValue && x.Utc >= cur.Utc);
                var best = candidates.Select(x => new { E = x, Score = LinkScore(cur, x) }).Where(x => x.Score >= 35).OrderByDescending(x => x.Score).ThenBy(x => cur.Utc == DateTime.MinValue || x.E.Utc == DateTime.MinValue ? Double.MaxValue : Math.Abs((cur.Utc - x.E.Utc).TotalSeconds)).FirstOrDefault();
                if (best == null) break; result.Add(new CausalPathHop { Event = best.E, Score = best.Score, Why = LinkWhy(cur, best.E) }); used.Add(best.E.Fingerprint); cur = best.E;
            }
            return result;
        }

        // -----------------------------------------------------------------
        // Branch genealogy / nested simulation universes
        // -----------------------------------------------------------------
        private static List<CausalBranch> LoadBranches(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                try
                {
                    if (!File.Exists(BranchPath(ctx))) return new List<CausalBranch>(); var root = ReadMap(BranchPath(ctx)); object raw; if (!root.TryGetValue("branches", out raw)) return new List<CausalBranch>(); var al = raw as IList; if (al == null) return new List<CausalBranch>(); var result = new List<CausalBranch>();
                    foreach (object o in al)
                    {
                        var m = o as Dictionary<string, object>; if (m == null) continue; var b = new CausalBranch(); b.Id = S(m, "id"); b.ParentId = S(m, "parent_branch_id"); b.RootId = S(m, "root_branch_id"); b.Generation = I(m, "generation"); b.CreatedUtc = S(m, "created_utc"); b.Name = S(m, "name"); b.ScenarioCode = S(m, "scenario_code"); b.BaselineSessionId = S(m, "baseline_session_id"); b.BaselineRevision = I(m, "baseline_revision"); b.AnchorOccurrence = I(m, "anchor_occurrence"); b.BaselineOrder = IntList(m, "baseline_order"); b.BranchOrder = IntList(m, "branch_order"); b.Hypothesis = S(m, "hypothesis"); b.Notes = S(m, "notes"); b.ParentFingerprint = S(m, "parent_fingerprint"); b.BranchFingerprint = S(m, "branch_fingerprint"); b.ChangedSlots = I(m, "changed_slots"); b.ParentChangedSlots = I(m, "parent_changed_slots"); b.StructuralDistance = D(m, "structural_distance"); b.LongestStablePrefix = I(m, "longest_stable_prefix"); b.AdjacencyRetention = D(m, "adjacency_retention");
                        result.Add(b);
                    }
                    foreach (var b in result) EnrichBranch(b, result.FirstOrDefault(x => String.Equals(x.Id, b.ParentId, StringComparison.OrdinalIgnoreCase)));
                    return result;
                }
                catch { return new List<CausalBranch>(); }
            }
        }

        private static void EnrichBranch(CausalBranch b, CausalBranch parent)
        {
            if (b.Generation <= 0) b.Generation = String.IsNullOrWhiteSpace(b.ParentId) ? 1 : (parent == null ? 2 : parent.Generation + 1);
            if (String.IsNullOrWhiteSpace(b.RootId)) b.RootId = String.IsNullOrWhiteSpace(b.ParentId) ? b.Id : (parent == null || String.IsNullOrWhiteSpace(parent.RootId) ? b.ParentId : parent.RootId);
            if (String.IsNullOrWhiteSpace(b.BranchFingerprint)) b.BranchFingerprint = OrderFingerprint(b.BranchOrder);
            if (b.ChangedSlots <= 0) b.ChangedSlots = ChangedSlots(b.BaselineOrder, b.BranchOrder);
            if (parent != null && b.ParentChangedSlots <= 0) b.ParentChangedSlots = ChangedSlots(parent.BranchOrder, b.BranchOrder);
            if (b.StructuralDistance <= 0 && b.BaselineOrder.Count == b.BranchOrder.Count && b.BaselineOrder.Count > 1) b.StructuralDistance = KendallDistance(b.BaselineOrder, b.BranchOrder);
            if (b.LongestStablePrefix <= 0) b.LongestStablePrefix = StablePrefix(b.BaselineOrder, b.BranchOrder);
            if (b.AdjacencyRetention <= 0) b.AdjacencyRetention = AdjacencyRetention(b.BaselineOrder, b.BranchOrder);
        }

        private static void SaveBranches(DeepSystemsContext ctx, List<CausalBranch> branches)
        {
            lock (Gate)
            {
                while (branches.Count > 64) branches.RemoveAt(0); var list = new List<object>(); foreach (var b in branches) list.Add(BranchMap(b)); var doc = new Dictionary<string, object>(); doc["schema"] = 2; doc["updated_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); doc["branches"] = list; Directory.CreateDirectory(ctx.StateRoot); AtomicWrite(BranchPath(ctx), Json.Serialize(doc));
            }
        }

        private static Dictionary<string, object> BranchMap(CausalBranch b)
        {
            var m = new Dictionary<string, object>(); m["id"] = b.Id; m["parent_branch_id"] = b.ParentId; m["root_branch_id"] = b.RootId; m["generation"] = b.Generation; m["created_utc"] = b.CreatedUtc; m["name"] = b.Name; m["scenario_code"] = b.ScenarioCode; m["baseline_session_id"] = b.BaselineSessionId; m["baseline_revision"] = b.BaselineRevision; m["anchor_occurrence"] = b.AnchorOccurrence; m["baseline_order"] = b.BaselineOrder; m["branch_order"] = b.BranchOrder; m["hypothesis"] = b.Hypothesis; m["notes"] = b.Notes; m["parent_fingerprint"] = b.ParentFingerprint; m["branch_fingerprint"] = b.BranchFingerprint; m["changed_slots"] = b.ChangedSlots; m["parent_changed_slots"] = b.ParentChangedSlots; m["structural_distance"] = b.StructuralDistance; m["longest_stable_prefix"] = b.LongestStablePrefix; m["adjacency_retention"] = b.AdjacencyRetention; return m;
        }

        private static void OpenBranchEcology(Window owner, DeepSystemsContext ctx)
        {
            var branches = LoadBranches(ctx); var w = W(owner, "YOMI · SIMULATION ECOLOGY", 1180, 780); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; head.Children.Add(T("SIMULATION ECOLOGY", 23, Text, FontWeights.SemiBold)); int roots = branches.Count(x => String.IsNullOrWhiteSpace(x.ParentId)); int maxGen = branches.Count == 0 ? 0 : branches.Max(x => x.Generation); head.Children.Add(T(branches.Count.ToString(CultureInfo.InvariantCulture) + " persisted universes · " + roots.ToString(CultureInfo.InvariantCulture) + " roots · max generation G" + maxGen.ToString(CultureInfo.InvariantCulture) + ". Every child is simulation-only and carries parent/root fingerprints.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) }; tools.Children.Add(Btn("OPEN DIVERGENCE MATRIX", delegate { OpenBranchMatrix(w, ctx); })); tools.Children.Add(Btn("OPEN RAW ECOLOGY", delegate { DeepSystems.OpenExternalValue(w, ctx, "SIMULATION ECOLOGY", branches, "ATLAS › CAUSAL GRAPH › BRANCH ECOLOGY"); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            var tree = new TreeView { Background = Bg, Foreground = Text, BorderBrush = Border }; root.Children.Add(tree);
            var byParent = branches.GroupBy(x => x.ParentId ?? "", StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreatedUtc).ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var b in branches.Where(x => String.IsNullOrWhiteSpace(x.ParentId)).OrderBy(x => x.CreatedUtc)) tree.Items.Add(BranchTreeItem(b, byParent, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            // Orphans remain visible instead of silently disappearing from genealogy.
            foreach (var b in branches.Where(x => !String.IsNullOrWhiteSpace(x.ParentId) && !branches.Any(p => String.Equals(p.Id, x.ParentId, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.CreatedUtc)) tree.Items.Add(BranchTreeItem(b, byParent, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            tree.MouseDoubleClick += delegate { var item = tree.SelectedItem as TreeViewItem; var b = item == null ? null : item.Tag as CausalBranch; if (b != null) OpenBranchDossier(w, ctx, b); }; w.Show();
        }

        private static TreeViewItem BranchTreeItem(CausalBranch b, Dictionary<string, List<CausalBranch>> byParent, HashSet<string> ancestry)
        {
            var item = new TreeViewItem { Header = b.ToString(), Tag = b, Foreground = Text, IsExpanded = b.Generation <= 2 };
            string id = b.Id ?? "";
            if (!ancestry.Add(id))
            {
                item.Items.Add(new TreeViewItem { Header = "LINEAGE CYCLE DETECTED · " + id, Foreground = Danger });
                return item;
            }
            List<CausalBranch> kids;
            if (byParent.TryGetValue(id, out kids))
                foreach (var child in kids) item.Items.Add(BranchTreeItem(child, byParent, new HashSet<string>(ancestry, StringComparer.OrdinalIgnoreCase)));
            return item;
        }

        private static void OpenBranchDossier(Window owner, DeepSystemsContext ctx, CausalBranch b)
        {
            var branches = LoadBranches(ctx); var parent = branches.FirstOrDefault(x => String.Equals(x.Id, b.ParentId, StringComparison.OrdinalIgnoreCase)); var w = W(owner, "YOMI · UNIVERSE " + b.Id, 1120, 760); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; head.Children.Add(T("UNIVERSE " + b.Id + " · G" + b.Generation.ToString(CultureInfo.InvariantCulture), 22, Accent, FontWeights.SemiBold)); head.Children.Add(T((b.Name ?? "branch") + " · Δ" + b.ChangedSlots.ToString(CultureInfo.InvariantCulture) + " root slots · structural distance " + b.StructuralDistance.ToString("0.000", CultureInfo.InvariantCulture) + " · adjacency " + (100.0 * b.AdjacencyRetention).ToString("0.0", CultureInfo.InvariantCulture) + "%", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) }; tools.Children.Add(Btn("SPAWN CHILD UNIVERSE", delegate { OpenSpawnMenu(w, ctx, b); })); if (parent != null) tools.Children.Add(Btn("COMPARE TO PARENT", delegate { OpenBranchDiff(w, ctx, parent, b); })); tools.Children.Add(Btn("COMPARE TO REAL BASELINE", delegate { OpenBranchAgainstBaseline(w, ctx, b); })); tools.Children.Add(Btn("OPEN RAW UNIVERSE", delegate { DeepSystems.OpenExternalValue(w, ctx, "COUNTERFACTUAL UNIVERSE " + b.Id, b, "ATLAS › CAUSAL GRAPH › BRANCH › " + b.Id); })); if (!String.IsNullOrWhiteSpace(b.Hypothesis)) tools.Children.Add(Btn("TRACE HYPOTHESIS", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, b.Hypothesis, "ATLAS › CAUSAL GRAPH › BRANCH › HYPOTHESIS"); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            var report = new TextBox { IsReadOnly = true, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), TextWrapping = TextWrapping.NoWrap, Text = BranchNarrative(b, parent) }; root.Children.Add(report); w.Show();
        }

        private static string BranchNarrative(CausalBranch b, CausalBranch parent)
        {
            var sb = new StringBuilder(); sb.AppendLine("id = " + b.Id); sb.AppendLine("parent = " + (String.IsNullOrWhiteSpace(b.ParentId) ? "REAL AUTHORITY BASELINE" : b.ParentId)); sb.AppendLine("root = " + (b.RootId ?? "")); sb.AppendLine("generation = G" + b.Generation); sb.AppendLine("scenario = " + (b.ScenarioCode ?? b.Name)); sb.AppendLine("anchor occurrence = " + b.AnchorOccurrence); sb.AppendLine("branch fingerprint = " + b.BranchFingerprint); sb.AppendLine("parent fingerprint = " + b.ParentFingerprint); sb.AppendLine(); sb.AppendLine("changed slots vs root = " + b.ChangedSlots); sb.AppendLine("changed slots vs parent = " + b.ParentChangedSlots); sb.AppendLine("Kendall structural distance vs root = " + b.StructuralDistance.ToString("0.000000", CultureInfo.InvariantCulture)); sb.AppendLine("longest stable prefix = " + b.LongestStablePrefix); sb.AppendLine("adjacency retention = " + (100.0 * b.AdjacencyRetention).ToString("0.00", CultureInfo.InvariantCulture) + "%"); sb.AppendLine(); sb.AppendLine("SIMULATION ONLY · branch state is controller-owned additive metadata · no engine command is implied"); if (parent != null) sb.AppendLine("parent universe exists and can be diffed exactly"); return sb.ToString();
        }

        private static void OpenSpawnMenu(Window owner, DeepSystemsContext ctx, CausalBranch parent)
        {
            var w = W(owner, "YOMI · SPAWN CHILD UNIVERSE", 820, 600); var root = new StackPanel { Margin = new Thickness(18) }; w.Content = root; root.Children.Add(T("SPAWN CHILD OF " + parent.Id, 22, Text, FontWeights.SemiBold)); var sub = T("Each scenario clones the parent universe in memory, applies one structural transformation, verifies exact occurrence-set preservation, then persists a new child with lineage/fingerprints.", 12.5, Muted, FontWeights.Normal); sub.Margin = new Thickness(0, 6, 0, 14); root.Children.Add(sub);
            Action<string, string, Func<List<int>, List<int>>> spawn = delegate(string name, string code, Func<List<int>, List<int>> transform) { SpawnBranch(ctx, parent, name, code, transform); w.Close(); OpenBranchEcology(owner, ctx); };
                        int anchor = parent.AnchorOccurrence != 0 ? parent.AnchorOccurrence : CurrentOccurrence(ctx);
            root.Children.Add(Btn("DEFER NEXT TO END", delegate { spawn("Defer next occurrence to end", "DEFER_NEXT_END", xs => DeferNext(xs, anchor)); }));
            root.Children.Add(Btn("ROTATE NEXT 7", delegate { spawn("Rotate next seven", "ROTATE_7", xs => Rotate(xs, anchor, 7)); }));
            root.Children.Add(Btn("REVERSE NEXT 9", delegate { spawn("Reverse next nine", "REVERSE_9", xs => Reverse(xs, anchor, 9)); }));
            root.Children.Add(Btn("INTERLEAVE NEXT 12", delegate { spawn("Interleave next twelve", "INTERLEAVE_12", xs => Interleave(xs, anchor, 12)); }));
            root.Children.Add(Btn("DETERMINISTIC PHASE SHIFT", delegate { spawn("Deterministic phase shift next sixteen", "PHASE_SHIFT_16", xs => PhaseShift(xs, anchor, 16, parent.BranchFingerprint)); }));
            w.Show();
        }

        private static void SpawnBranch(DeepSystemsContext ctx, CausalBranch parent, string name, string code, Func<List<int>, List<int>> transform)
        {
            var baseline = new List<int>(parent.BranchOrder); var branch = transform(new List<int>(parent.BranchOrder)); if (baseline.Count != branch.Count || !new HashSet<int>(baseline).SetEquals(branch) || branch.Count != branch.Distinct().Count()) return;
            var child = new CausalBranch(); child.Id = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(); child.ParentId = parent.Id; child.RootId = String.IsNullOrWhiteSpace(parent.RootId) ? parent.Id : parent.RootId; child.Generation = parent.Generation + 1; child.CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); child.Name = name; child.ScenarioCode = code; child.BaselineSessionId = parent.BaselineSessionId; child.BaselineRevision = parent.BaselineRevision; child.AnchorOccurrence = parent.AnchorOccurrence; child.BaselineOrder = new List<int>(parent.BaselineOrder); child.BranchOrder = branch; child.Hypothesis = name + " derived from universe " + parent.Id; child.ParentFingerprint = parent.BranchFingerprint; child.BranchFingerprint = OrderFingerprint(branch); child.ChangedSlots = ChangedSlots(child.BaselineOrder, branch); child.ParentChangedSlots = ChangedSlots(parent.BranchOrder, branch); child.StructuralDistance = KendallDistance(child.BaselineOrder, branch); child.LongestStablePrefix = StablePrefix(child.BaselineOrder, branch); child.AdjacencyRetention = AdjacencyRetention(child.BaselineOrder, branch);
            var all = LoadBranches(ctx); all.Add(child); SaveBranches(ctx, all);
        }

        private static void OpenBranchMatrix(Window owner, DeepSystemsContext ctx)
        {
            var branches = LoadBranches(ctx).OrderByDescending(x => x.CreatedUtc).Take(14).Reverse().ToList(); var w = W(owner, "YOMI · BRANCH DIVERGENCE MATRIX", 1320, 860); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 8) }; h.Children.Add(T("BRANCH DIVERGENCE MATRIX", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("Pairwise changed-slot distance across the 14 most recent universes. Click a cell to open an exact structural diff.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            if (branches.Count == 0) { root.Children.Add(T("No counterfactual universes exist yet.", 14, Muted, FontWeights.Normal)); w.Show(); return; }
            var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; root.Children.Add(scroll); var g = new Grid { Background = Bg }; scroll.Content = g; g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(135) }); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); for (int i = 0; i < branches.Count; i++) { g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) }); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) }); }
            int max = 1; for (int i = 0; i < branches.Count; i++) for (int j = 0; j < branches.Count; j++) max = Math.Max(max, ChangedSlots(branches[i].BranchOrder, branches[j].BranchOrder));
            for (int i = 0; i < branches.Count; i++)
            {
                string label = "G" + branches[i].Generation + "·" + branches[i].Id.Substring(0, Math.Min(6, branches[i].Id.Length)); var top = T(label, 10.5, Muted, FontWeights.SemiBold); top.LayoutTransform = new RotateTransform(-52); top.HorizontalAlignment = HorizontalAlignment.Center; top.VerticalAlignment = VerticalAlignment.Bottom; Grid.SetColumn(top, i + 1); Grid.SetRow(top, 0); g.Children.Add(top); var left = T(label, 10.5, Muted, FontWeights.SemiBold); left.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(left, 0); Grid.SetRow(left, i + 1); g.Children.Add(left);
                for (int j = 0; j < branches.Count; j++)
                {
                    int d = ChangedSlots(branches[i].BranchOrder, branches[j].BranchOrder); double ratio = (double)d / (double)max; var c = Color.FromRgb((byte)(24 + 165 * ratio), (byte)(42 + 70 * (1.0 - ratio)), (byte)(56 + 75 * (1.0 - ratio))); CausalBranch a = branches[i], b = branches[j]; var cell = new Button { Content = d.ToString(CultureInfo.InvariantCulture), Background = new SolidColorBrush(c), Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(1), ToolTip = a.Id + " ↔ " + b.Id, Cursor = Cursors.Hand }; cell.Click += delegate { OpenBranchDiff(w, ctx, a, b); }; Grid.SetColumn(cell, j + 1); Grid.SetRow(cell, i + 1); g.Children.Add(cell);
                }
            }
            w.Show();
        }

        private static void OpenBranchAgainstBaseline(Window owner, DeepSystemsContext ctx, CausalBranch b)
        {
            var real = new CausalBranch { Id = "REAL-R" + b.BaselineRevision.ToString(CultureInfo.InvariantCulture), Generation = 0, Name = "Authoritative baseline captured by root branch", AnchorOccurrence = b.AnchorOccurrence, BranchOrder = new List<int>(b.BaselineOrder), BaselineOrder = new List<int>(b.BaselineOrder), BranchFingerprint = OrderFingerprint(b.BaselineOrder) }; OpenBranchDiff(owner, ctx, real, b);
        }

        private static void OpenBranchDiff(Window owner, DeepSystemsContext ctx, CausalBranch a, CausalBranch b)
        {
            var rows = new List<Dictionary<string, object>>(); int n = Math.Min(a.BranchOrder.Count, b.BranchOrder.Count); for (int i = 0; i < n; i++) if (a.BranchOrder[i] != b.BranchOrder[i]) { var m = new Dictionary<string, object>(); m["slot"] = i + 1; m["universe_a_occurrence"] = a.BranchOrder[i]; m["universe_b_occurrence"] = b.BranchOrder[i]; rows.Add(m); }
            var summary = new Dictionary<string, object>(); summary["universe_a"] = a; summary["universe_b"] = b; summary["changed_slots"] = rows.Count; summary["kendall_distance"] = a.BranchOrder.Count == b.BranchOrder.Count && new HashSet<int>(a.BranchOrder).SetEquals(b.BranchOrder) ? KendallDistance(a.BranchOrder, b.BranchOrder) : -1.0; summary["adjacency_retention"] = AdjacencyRetention(a.BranchOrder, b.BranchOrder); summary["slot_diff"] = rows; summary["warning"] = "Comparison only. No branch is engine authority.";
            DeepSystems.OpenExternalValue(owner, ctx, "UNIVERSE DIFF · " + a.Id + " ↔ " + b.Id, summary, "ATLAS › CAUSAL GRAPH › UNIVERSE DIFF");
        }

        private static int CurrentOccurrence(DeepSystemsContext ctx)
        {
            var m = ReadMap(Path.Combine(ctx.StateRoot, "current.json")); foreach (string k in new[] { "occurrence_id", "occurrence", "index" }) { int v = I(m, k); if (v != 0) return v; } return 0;
        }

        private static List<int> DeferNext(List<int> order, int current)
        {
            int i = order.IndexOf(current); int idx = i >= 0 ? i + 1 : 0; if (idx < 0 || idx >= order.Count) return order; int x = order[idx]; order.RemoveAt(idx); order.Add(x); return order;
        }
        private static List<int> Rotate(List<int> order, int current, int count) { int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n > 1) { int x = order[start]; order.RemoveAt(start); order.Insert(start + n - 1, x); } return order; }
        private static List<int> Reverse(List<int> order, int current, int count) { int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n > 1) order.Reverse(start, n); return order; }
        private static List<int> Interleave(List<int> order, int current, int count)
        {
            int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n < 4) return order; var segment = order.GetRange(start, n); var next = new List<int>(); int mid = (segment.Count + 1) / 2; for (int x = 0; x < mid; x++) { next.Add(segment[x]); int y = x + mid; if (y < segment.Count) next.Add(segment[y]); } for (int x = 0; x < n; x++) order[start + x] = next[x]; return order;
        }
        private static List<int> PhaseShift(List<int> order, int current, int count, string seed)
        {
            int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n < 2) return order; var seg = order.GetRange(start, n); byte[] hash; using (var sha = SHA256.Create()) hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed ?? "phase")); for (int x = seg.Count - 1, h = 0; x > 0; x--, h++) { int j = (hash[h % hash.Length] + 257 * h) % (x + 1); int t = seg[x]; seg[x] = seg[j]; seg[j] = t; } for (int x = 0; x < n; x++) order[start + x] = seg[x]; return order;
        }

        private static int ChangedSlots(List<int> a, List<int> b) { int n = Math.Min(a == null ? 0 : a.Count, b == null ? 0 : b.Count); int c = Math.Abs((a == null ? 0 : a.Count) - (b == null ? 0 : b.Count)); for (int i = 0; i < n; i++) if (a[i] != b[i]) c++; return c; }
        private static int StablePrefix(List<int> a, List<int> b) { int n = Math.Min(a == null ? 0 : a.Count, b == null ? 0 : b.Count); int i = 0; while (i < n && a[i] == b[i]) i++; return i; }
        private static double AdjacencyRetention(List<int> a, List<int> b)
        {
            if (a == null || b == null || a.Count < 2 || b.Count < 2) return a != null && b != null && a.Count == b.Count ? 1.0 : 0.0; var edges = new HashSet<string>(); for (int i = 0; i + 1 < a.Count; i++) edges.Add(a[i].ToString(CultureInfo.InvariantCulture) + ">" + a[i + 1].ToString(CultureInfo.InvariantCulture)); int hit = 0; for (int i = 0; i + 1 < b.Count; i++) if (edges.Contains(b[i].ToString(CultureInfo.InvariantCulture) + ">" + b[i + 1].ToString(CultureInfo.InvariantCulture))) hit++; return (double)hit / (double)Math.Max(1, a.Count - 1);
        }
        private static double KendallDistance(List<int> baseline, List<int> order)
        {
            if (baseline == null || order == null || baseline.Count != order.Count || baseline.Count < 2) return 0.0; var pos = new Dictionary<int, int>(); for (int i = 0; i < baseline.Count; i++) pos[baseline[i]] = i + 1; var bit = new long[baseline.Count + 3]; long inv = 0; long seen = 0;
            foreach (int occ in order) { int p; if (!pos.TryGetValue(occ, out p)) return 1.0; long le = 0; for (int x = p; x > 0; x -= x & -x) le += bit[x]; inv += seen - le; for (int x = p; x < bit.Length; x += x & -x) bit[x]++; seen++; }
            double max = (double)baseline.Count * (baseline.Count - 1) / 2.0; return max <= 0 ? 0 : inv / max;
        }
        private static string OrderFingerprint(List<int> order) { return Sha(String.Join(",", (order ?? new List<int>()).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray())); }

        // -----------------------------------------------------------------
        // Computed summary / helpers
        // -----------------------------------------------------------------
        private static object BuildGraphSummary(DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 220); var ids = BuildIdentityIndex(events); var clusters = BuildClusters(events); var branches = LoadBranches(ctx); var summary = new Dictionary<string, object>(); summary["schema"] = 1; summary["generated_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); summary["evidence_records"] = events.Count; summary["evidence_streams"] = events.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count(); summary["semantic_identities"] = ids.Count; summary["connected_clusters"] = clusters.Count; summary["largest_cluster"] = clusters.Count == 0 ? 0 : clusters.Max(x => x.Events.Count); summary["counterfactual_universes"] = branches.Count; summary["max_generation"] = branches.Count == 0 ? 0 : branches.Max(x => x.Generation); summary["branch_roots"] = branches.Count(x => String.IsNullOrWhiteSpace(x.ParentId)); summary["top_identities"] = ids.OrderByDescending(x => x.Value.Count).Take(40).ToDictionary(x => x.Key, x => x.Value.Count); summary["note"] = "Computed passive projection. Evidence edges express observed linkage, not proven causality. Counterfactual branches are non-authoritative."; return summary;
        }

        private static Dictionary<string, object> ReadMap(string path)
        {
            try { if (!File.Exists(path)) return new Dictionary<string, object>(); return Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object> ?? new Dictionary<string, object>(); } catch { return new Dictionary<string, object>(); }
        }
        private static List<int> IntList(Dictionary<string, object> m, string key)
        {
            var r = new List<int>(); if (m == null) return r; object raw;
            if (!m.TryGetValue(key, out raw)) { var kv = m.FirstOrDefault(x => String.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)); raw = kv.Value; }
            var al = raw as IList; if (al == null) return r; foreach (object o in al) { int v; if (Int32.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), out v)) r.Add(v); } return r;
        }
        private static string S(Dictionary<string, object> m, string k)
        {
            if (m == null) return ""; object v; if (m.TryGetValue(k, out v) && v != null) return Convert.ToString(v, CultureInfo.InvariantCulture);
            foreach (var kv in m) if (String.Equals(kv.Key, k, StringComparison.OrdinalIgnoreCase) && kv.Value != null) return Convert.ToString(kv.Value, CultureInfo.InvariantCulture); return "";
        }
        private static int I(Dictionary<string, object> m, string k) { int v; return Int32.TryParse(S(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0; }
        private static double D(Dictionary<string, object> m, string k) { double v; return Double.TryParse(S(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0.0; }
        private static string Sha(string text) { using (var sha = SHA256.Create()) return String.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)).ToArray()); }
        private static void AtomicWrite(string path, string text)
        {
            string tmp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text, new UTF8Encoding(false)); if (File.Exists(path)) { string bak = path + ".bak"; try { File.Replace(tmp, path, bak, true); try { File.Delete(bak); } catch { } } catch { File.Copy(tmp, path, true); File.Delete(tmp); } } else File.Move(tmp, path);
        }
    }
}

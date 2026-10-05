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

namespace Yomi.ProductShell
{
    internal sealed class TemporalEvent
    {
        public DateTime Utc;
        public string Source;
        public string Kind;
        public string Summary;
        public string Raw;
        public object Value;
        public Dictionary<string, string> Identity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Fingerprint;
        public override string ToString()
        {
            string time = Utc == DateTime.MinValue ? "--:--:--" : Utc.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            return time + "   " + (Kind ?? "EVENT") + "   " + (Summary ?? "") + "   · " + Path.GetFileName(Source ?? "");
        }
    }

    internal sealed class TemporalEpoch
    {
        public string Id;
        public string CapturedUtc;
        public string SessionId;
        public int OrderRevision;
        public Dictionary<string, TemporalFingerprint> Files = new Dictionary<string, TemporalFingerprint>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class TemporalFingerprint
    {
        public long Bytes;
        public string ModifiedUtc;
        public string Sha256;
    }

    internal sealed class TemporalBranchRecord
    {
        public string Id;
        public string ParentBranchId;
        public string RootBranchId;
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
    }

    internal static class TemporalObservatory
    {
        private static readonly Brush Bg = B("#0D1014");
        private static readonly Brush Surface = B("#15191F");
        private static readonly Brush Raised = B("#1E242C");
        private static readonly Brush Border = B("#34404B");
        private static readonly Brush Text = B("#EFF4F8");
        private static readonly Brush Muted = B("#9EABB7");
        private static readonly Brush Accent = B("#69E6B4");
        private static readonly Brush Blue = B("#70B7FF");
        private static readonly Brush Amber = B("#FFCA69");
        private static readonly Brush Danger = B("#FF7B7B");
        private static readonly object VaultGate = new object();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            OpenHub(owner, ctx);
        }

        private static Window BaseWindow(Window owner, string title, double width, double height)
        {
            var w = new Window
            {
                Title = title,
                Width = width,
                Height = height,
                MinWidth = Math.Min(width, 780),
                MinHeight = Math.Min(height, 520),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                Background = Bg,
                Foreground = Text,
                FontFamily = new FontFamily("Segoe UI")
            };
            return w;
        }

        private static Border Card(UIElement child, Thickness margin)
        {
            return new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14), Margin = margin, Child = child };
        }

        private static Button Btn(string text, RoutedEventHandler click)
        {
            var b = new Button
            {
                Content = text,
                MinHeight = 34,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 8, 8),
                Background = Raised,
                Foreground = Text,
                BorderBrush = Border,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            if (click != null) b.Click += click;
            return b;
        }

        private static TextBlock T(string text, double size, Brush brush, FontWeight weight)
        {
            return new TextBlock { Text = text ?? "", FontSize = size, Foreground = brush, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx)
        {
            var w = BaseWindow(owner, "YOMI · TEMPORAL OBSERVATORY", 1120, 760);
            var root = new DockPanel { Margin = new Thickness(18) };
            w.Content = root;

            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            head.Children.Add(T("TEMPORAL OBSERVATORY", 26, Text, FontWeights.SemiBold));
            head.Children.Add(T("History, correlation, drift, reconstruction and simulation. Observation is passive; counterfactuals never become engine authority.", 13, Muted, FontWeights.Normal));
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            root.Children.Add(scroll);
            var grid = new UniformGrid { Columns = 2 };
            scroll.Content = grid;

            AddPortal(grid, "CAUSAL GRAPH LAB", "Evidence topology, identity wormholes, nested alternate-history genealogy, divergence matrices and scored candidate paths.", "GRAPH", delegate { CausalGraphLab.Open(w, ctx); });
            AddPortal(grid, "TIMELINE THEATRE", "Merge append-only event streams into one chronology. Search, filter, open dossiers, and follow evidence sideways.", "TIME", delegate { OpenTimeline(w, ctx, null); });
            AddPortal(grid, "CAUSALITY CANDIDATE ENGINE", "Rank evidence-linked event neighborhoods by shared identity, hash ancestry, order revision and temporal proximity.", "CONE", delegate { OpenCausalityIndex(w, ctx); });
            AddPortal(grid, "COUNTERFACTUAL CHAMBER", "Fork the current queue/order into an isolated branch, run structural scenarios, compare outcomes, and never mutate playback.", "WHAT IF", delegate { OpenCounterfactual(w, ctx, null); });
            AddPortal(grid, "EPOCH / DRIFT RADAR", "Fingerprint important control-plane artifacts, capture epochs, and discover exactly which subsystems drifted while you explored elsewhere.", "ΔT", delegate { OpenEpochVault(w, ctx); });
            AddPortal(grid, "CORRELATION CONSTELLATION", "Find hot identities spanning multiple artifacts: intents, occurrences, source keys, sessions, revisions and hashes.", "LINK", delegate { OpenCorrelationConstellation(w, ctx); });
            AddPortal(grid, "STATE FOSSIL LAB", "Browse historical snapshots, queue checkpoints, recovery chains and immutable evidence as archaeological strata.", "FOSSIL", delegate { OpenFossilLab(w, ctx); });
            AddPortal(grid, "BRANCH VAULT", "Reopen bounded counterfactual branches and descend into their baseline/branch order models and diffs.", "BRANCH", delegate { OpenBranchVault(w, ctx); });
            AddPortal(grid, "TEMPORAL DATA CATACOMBS", "Skip the curation. Enter every JSONL/log/history-like artifact Temporal Observatory can discover.", "∞", delegate { OpenTemporalFiles(w, ctx); });

            w.Show();
        }

        private static void AddPortal(Panel panel, string title, string subtitle, string badge, Action open)
        {
            var stack = new StackPanel();
            var badgeBlock = T(badge, 11, Accent, FontWeights.Bold); badgeBlock.Margin = new Thickness(0, 0, 0, 5); stack.Children.Add(badgeBlock);
            stack.Children.Add(T(title, 18, Text, FontWeights.SemiBold));
            var sub = T(subtitle, 12.5, Muted, FontWeights.Normal); sub.Margin = new Thickness(0, 6, 0, 12); stack.Children.Add(sub);
            var enter = Btn("ENTER SYSTEM  ›", delegate { open(); }); enter.HorizontalAlignment = HorizontalAlignment.Left; stack.Children.Add(enter);
            panel.Children.Add(Card(stack, new Thickness(0, 0, 12, 12)));
        }

        private static List<string> EventFiles(DeepSystemsContext ctx)
        {
            var files = new List<string>();
            string[] roots = { ctx.StateRoot, Path.Combine(ctx.DataRoot, "logs") };
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (string p in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(p).ToLowerInvariant();
                        string name = Path.GetFileName(p).ToLowerInvariant();
                        if (ext == ".jsonl" || ext == ".log" || ext == ".txt" || name.Contains("journal") || name.Contains("event") || name.Contains("history") || name.Contains("flight"))
                            files.Add(p);
                        if (files.Count >= 80) break;
                    }
                }
                catch { }
                if (files.Count >= 80) break;
            }
            return files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> TailLines(string path, int max)
        {
            var q = new Queue<string>(max + 1);
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        q.Enqueue(line);
                        if (q.Count > max) q.Dequeue();
                    }
                }
            }
            catch { }
            return q.ToList();
        }

        private static List<TemporalEvent> LoadEvents(DeepSystemsContext ctx, int perFile)
        {
            var all = new List<TemporalEvent>();
            foreach (string path in EventFiles(ctx))
            {
                foreach (string line in TailLines(path, perFile))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var ev = ParseEvent(path, line);
                    if (ev != null) all.Add(ev);
                    if (all.Count >= 12000) break;
                }
                if (all.Count >= 12000) break;
            }
            return all.OrderByDescending(x => x.Utc == DateTime.MinValue ? DateTime.MinValue : x.Utc).ThenBy(x => x.Source).ToList();
        }

        private static TemporalEvent ParseEvent(string source, string line)
        {
            object value = null;
            Dictionary<string, object> map = null;
            string trimmed = line.Trim();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                try { value = Json.DeserializeObject(trimmed); map = value as Dictionary<string, object>; } catch { }
            }
            var ev = new TemporalEvent { Source = source, Raw = line, Value = value ?? line, Utc = DateTime.MinValue };
            if (map != null)
            {
                ev.Utc = ExtractTime(map);
                ev.Kind = First(map, new[] { "event", "type", "kind", "state", "action", "phase" }, "EVENT").ToUpperInvariant();
                ev.Summary = First(map, new[] { "message", "reason", "detail", "decision_reason", "summary", "title" }, ev.Kind);
                foreach (string key in new[] { "intent_id", "occurrence_id", "occurrence", "source_key", "session_id", "entry_hash", "prev_hash", "plan_fingerprint", "order_revision", "work_generation", "failure_domain", "route_label" })
                {
                    object v; if (map.TryGetValue(key, out v) && v != null) ev.Identity[key] = Convert.ToString(v, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                ev.Utc = ExtractTextTime(line);
                ev.Kind = GuessKind(line);
                ev.Summary = line.Length > 220 ? line.Substring(0, 220) + "…" : line;
            }
            ev.Fingerprint = Sha256Hex((source ?? "") + "\n" + line);
            return ev;
        }

        private static string First(Dictionary<string, object> map, string[] keys, string fallback)
        {
            foreach (string k in keys)
            {
                object v; if (map.TryGetValue(k, out v) && v != null)
                {
                    string s = Convert.ToString(v, CultureInfo.InvariantCulture); if (!String.IsNullOrWhiteSpace(s)) return s;
                }
            }
            return fallback;
        }

        private static DateTime ExtractTime(Dictionary<string, object> map)
        {
            foreach (string k in new[] { "utc", "timestamp_utc", "created_utc", "updated_utc", "time_utc", "timestamp", "time" })
            {
                object v; if (!map.TryGetValue(k, out v) || v == null) continue;
                DateTime dt; if (DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt)) return dt;
            }
            foreach (string k in new[] { "unix", "created_unix", "updated_unix", "timestamp_unix" })
            {
                object v; if (!map.TryGetValue(k, out v) || v == null) continue;
                double d; if (Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                {
                    try { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(d); } catch { }
                }
            }
            return DateTime.MinValue;
        }

        private static DateTime ExtractTextTime(string line)
        {
            if (String.IsNullOrWhiteSpace(line)) return DateTime.MinValue;
            int max = Math.Min(line.Length, 40);
            string prefix = line.Substring(0, max).Trim(' ', '[', ']');
            DateTime dt; if (DateTime.TryParse(prefix, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) return dt.ToUniversalTime();
            return DateTime.MinValue;
        }

        private static string GuessKind(string line)
        {
            string u = (line ?? "").ToUpperInvariant();
            foreach (string k in new[] { "ERROR", "FAIL", "WARN", "COMMIT", "START", "STOP", "READY", "ORACLE", "FREEZE", "REHEARS", "CACHE", "VIDEO", "AUDIO", "QUEUE" }) if (u.Contains(k)) return k;
            return "LOG";
        }

        private static void OpenTimeline(Window owner, DeepSystemsContext ctx, string initialQuery)
        {
            var events = LoadEvents(ctx, 320);
            var w = BaseWindow(owner, "YOMI · TIMELINE THEATRE", 1220, 780);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            header.Children.Add(T("TIMELINE THEATRE", 23, Text, FontWeights.SemiBold));
            header.Children.Add(T(events.Count.ToString(CultureInfo.InvariantCulture) + " bounded recent records merged across " + EventFiles(ctx).Count.ToString(CultureInfo.InvariantCulture) + " evidence streams", 12, Muted, FontWeights.Normal));
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);

            var search = new TextBox { Height = 32, Margin = new Thickness(0, 0, 0, 8), Background = Raised, Foreground = Text, BorderBrush = Border, Text = initialQuery ?? "" };
            DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12.5 };
            root.Children.Add(list);

            Action refresh = delegate
            {
                string q = (search.Text ?? "").Trim();
                IEnumerable<TemporalEvent> view = events;
                if (q.Length > 0) view = view.Where(e => EventSearchText(e).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                list.ItemsSource = view.Take(2500).ToList();
            };
            search.TextChanged += delegate { refresh(); };
            list.MouseDoubleClick += delegate
            {
                var ev = list.SelectedItem as TemporalEvent; if (ev != null) OpenEventDossier(w, ctx, ev, events);
            };
            list.PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { var ev = list.SelectedItem as TemporalEvent; if (ev != null) OpenEventDossier(w, ctx, ev, events); e.Handled = true; }
            };
            refresh();
            w.Show();
        }

        private static string EventSearchText(TemporalEvent e)
        {
            return (e.Kind ?? "") + " " + (e.Summary ?? "") + " " + (e.Source ?? "") + " " + String.Join(" ", e.Identity.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + " " + (e.Raw ?? "");
        }

        private static void OpenEventDossier(Window owner, DeepSystemsContext ctx, TemporalEvent ev, List<TemporalEvent> universe)
        {
            var w = BaseWindow(owner, "YOMI · EVENT DOSSIER · " + (ev.Kind ?? "EVENT"), 980, 720);
            var root = new DockPanel { Margin = new Thickness(16) }; w.Content = root;
            var head = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            head.Children.Add(T(ev.Kind ?? "EVENT", 22, Accent, FontWeights.Bold));
            head.Children.Add(T(ev.Summary ?? "", 16, Text, FontWeights.SemiBold));
            head.Children.Add(T((ev.Utc == DateTime.MinValue ? "timestamp unavailable" : ev.Utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)) + "  ·  " + ev.Source, 11.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            actions.Children.Add(Btn("RAW MICROSCOPE", delegate { DeepSystems.OpenExternalValue(w, ctx, "TEMPORAL EVENT · " + ev.Kind, ev.Value, "ATLAS › TEMPORAL › EVENT › RAW"); }));
            actions.Children.Add(Btn("EVIDENCE CONE", delegate { OpenEvidenceCone(w, ctx, ev, universe); }));
            actions.Children.Add(Btn("CAUSAL PATH", delegate { CausalGraphLab.OpenPathFromFingerprint(w, ctx, ev.Fingerprint); }));
            actions.Children.Add(Btn("±10s EVENT HORIZON", delegate { OpenEventHorizon(w, ctx, ev, universe, 10.0); }));
            actions.Children.Add(Btn("REPLAY EVIDENCE FRAME", delegate { OpenEvidenceFrame(w, ctx, ev, universe); }));
            if (ev.Identity.ContainsKey("entry_hash") || ev.Identity.ContainsKey("prev_hash")) actions.Children.Add(Btn("HASH ANCESTRY", delegate { OpenHashAncestry(w, ctx, ev, universe); }));
            actions.Children.Add(Btn("COUNTERFACTUAL BRANCH", delegate { OpenCounterfactual(w, ctx, ev); }));
            bool firstIdentity = true;
            foreach (var kv in ev.Identity.Take(5))
            {
                string key = kv.Key, val = kv.Value;
                actions.Children.Add(Btn("TRACE " + key.ToUpperInvariant(), delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, val, "ATLAS › TEMPORAL › EVENT › " + key); }));
                if (firstIdentity)
                {
                    actions.Children.Add(Btn("GRAPH " + key.ToUpperInvariant(), delegate { CausalGraphLab.OpenIdentityToken(w, ctx, key, val); }));
                    firstIdentity = false;
                }
            }
            DockPanel.SetDock(actions, Dock.Top); root.Children.Add(actions);

            var tabs = new TabControl { Background = Bg, Foreground = Text, BorderBrush = Border };
            root.Children.Add(tabs);
            var idText = String.Join(Environment.NewLine, ev.Identity.Select(kv => kv.Key + " = " + kv.Value).ToArray());
            tabs.Items.Add(Tab("IDENTITY", new TextBox { Text = idText.Length == 0 ? "(no semantic identity fields recognized)" : idText, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Background = Surface, Foreground = Text, BorderThickness = new Thickness(0), Padding = new Thickness(12) }));
            tabs.Items.Add(Tab("RAW", new TextBox { Text = ev.Raw ?? "", IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Surface, Foreground = Text, BorderThickness = new Thickness(0), Padding = new Thickness(12), FontFamily = new FontFamily("Cascadia Mono, Consolas") }));
            tabs.Items.Add(Tab("FINGERPRINT", new TextBox { Text = "sha256 = " + ev.Fingerprint + Environment.NewLine + "source = " + ev.Source, IsReadOnly = true, Background = Surface, Foreground = Text, BorderThickness = new Thickness(0), Padding = new Thickness(12), FontFamily = new FontFamily("Cascadia Mono, Consolas") }));
            w.Show();
        }

        private static TabItem Tab(string title, UIElement content) { return new TabItem { Header = title, Content = content }; }

        private static void OpenEventHorizon(Window owner, DeepSystemsContext ctx, TemporalEvent center, List<TemporalEvent> universe, double seconds)
        {
            if (center.Utc == DateTime.MinValue) { OpenTimeline(owner, ctx, center.Fingerprint.Substring(0, 12)); return; }
            var rows = universe.Where(e => e.Utc != DateTime.MinValue && Math.Abs((e.Utc - center.Utc).TotalSeconds) <= seconds).OrderBy(e => e.Utc).ToList();
            var w = BaseWindow(owner, "YOMI · EVENT HORIZON ±" + seconds.ToString("0", CultureInfo.InvariantCulture) + "s", 1120, 720);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; h.Children.Add(T("EVENT HORIZON", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("All bounded evidence records within ±" + seconds.ToString("0", CultureInfo.InvariantCulture) + " seconds of the anchor event.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = rows, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            list.MouseDoubleClick += delegate { var ev = list.SelectedItem as TemporalEvent; if (ev != null) OpenEventDossier(w, ctx, ev, universe); };
            root.Children.Add(list); w.Show();
        }

        private static void OpenEvidenceFrame(Window owner, DeepSystemsContext ctx, TemporalEvent center, List<TemporalEvent> universe)
        {
            if (center.Utc == DateTime.MinValue) { DeepSystems.OpenExternalValue(owner, ctx, "EVIDENCE FRAME UNAVAILABLE", new Dictionary<string, object> { { "reason", "anchor event has no parseable timestamp" } }, "ATLAS › TEMPORAL › FRAME"); return; }
            var frame = new Dictionary<string, object>();
            frame["schema"] = 1;
            frame["frame_kind"] = "last-known evidence per source";
            frame["anchor_utc"] = center.Utc.ToString("o", CultureInfo.InvariantCulture);
            frame["warning"] = "Evidence reconstruction only. This is not a byte-perfect historical process-memory snapshot.";
            var sourceMap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in universe.Where(e => e.Utc != DateTime.MinValue && e.Utc <= center.Utc).GroupBy(e => e.Source ?? "(unknown)", StringComparer.OrdinalIgnoreCase))
            {
                var last = group.OrderByDescending(e => e.Utc).FirstOrDefault(); if (last == null) continue;
                var entry = new Dictionary<string, object>(); entry["utc"] = last.Utc.ToString("o", CultureInfo.InvariantCulture); entry["kind"] = last.Kind; entry["summary"] = last.Summary; entry["fingerprint"] = last.Fingerprint; entry["identity"] = last.Identity; entry["value"] = last.Value; sourceMap[Relative(ctx.DataRoot, group.Key)] = entry;
            }
            frame["sources"] = sourceMap;
            DeepSystems.OpenExternalValue(owner, ctx, "REPLAY EVIDENCE FRAME", frame, "ATLAS › TEMPORAL › REPLAY FRAME");
        }

        private static void OpenHashAncestry(Window owner, DeepSystemsContext ctx, TemporalEvent center, List<TemporalEvent> universe)
        {
            var byEntry = new Dictionary<string, TemporalEvent>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in universe) { string h; if (e.Identity.TryGetValue("entry_hash", out h) && !String.IsNullOrWhiteSpace(h) && !byEntry.ContainsKey(h)) byEntry[h] = e; }
            var chain = new List<TemporalEvent>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); TemporalEvent cur = center;
            for (int depth = 0; cur != null && depth < 512; depth++)
            {
                chain.Add(cur); string prev; if (!cur.Identity.TryGetValue("prev_hash", out prev) || String.IsNullOrWhiteSpace(prev) || !seen.Add(prev)) break;
                TemporalEvent parent; if (!byEntry.TryGetValue(prev, out parent)) break; cur = parent;
            }
            var w = BaseWindow(owner, "YOMI · HASH ANCESTRY", 1120, 720); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var hblock = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; hblock.Children.Add(T("HASH ANCESTRY", 23, Text, FontWeights.SemiBold)); hblock.Children.Add(T("Exact prev_hash → entry_hash ancestry discovered inside the currently loaded bounded evidence universe.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(hblock, Dock.Top); root.Children.Add(hblock);
            var list = new ListBox { ItemsSource = chain, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; list.MouseDoubleClick += delegate { var ev = list.SelectedItem as TemporalEvent; if (ev != null) OpenEventDossier(w, ctx, ev, universe); }; root.Children.Add(list); w.Show();
        }

        private static void OpenEvidenceCone(Window owner, DeepSystemsContext ctx, TemporalEvent center, List<TemporalEvent> universe)
        {
            var scored = universe.Where(x => !Object.ReferenceEquals(x, center)).Select(x => new { Event = x, Score = LinkScore(center, x), Why = LinkWhy(center, x) }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => TimeDistance(center, x.Event)).Take(500).ToList();
            var w = BaseWindow(owner, "YOMI · EVIDENCE CONE", 1120, 760);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            h.Children.Add(T("EVIDENCE CONE", 23, Text, FontWeights.SemiBold));
            h.Children.Add(T("Candidate relationships ranked by shared identity/hash lineage and time proximity. This is correlation evidence, not automatic proof of causation.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (var row in scored)
            {
                var item = new ListBoxItem { Content = row.Score.ToString("00", CultureInfo.InvariantCulture) + "  " + row.Why + "  ·  " + row.Event.ToString(), Tag = row.Event, Foreground = Text, Background = Bg };
                list.Items.Add(item);
            }
            list.MouseDoubleClick += delegate { var item = list.SelectedItem as ListBoxItem; var e = item == null ? null : item.Tag as TemporalEvent; if (e != null) OpenEventDossier(w, ctx, e, universe); };
            root.Children.Add(list); w.Show();
        }

        private static int LinkScore(TemporalEvent a, TemporalEvent b)
        {
            int score = 0;
            foreach (var kv in a.Identity)
            {
                string other; if (!b.Identity.TryGetValue(kv.Key, out other) || String.IsNullOrWhiteSpace(kv.Value) || kv.Value != other) continue;
                switch (kv.Key.ToLowerInvariant())
                {
                    case "entry_hash": case "prev_hash": score += 12; break;
                    case "intent_id": score += 10; break;
                    case "occurrence_id": case "occurrence": score += 8; break;
                    case "source_key": score += 8; break;
                    case "session_id": score += 4; break;
                    case "order_revision": score += 4; break;
                    case "plan_fingerprint": score += 7; break;
                    default: score += 2; break;
                }
            }
            double seconds = TimeDistance(a, b);
            if (seconds <= 0.25) score += 5; else if (seconds <= 1) score += 4; else if (seconds <= 5) score += 3; else if (seconds <= 20) score += 2; else if (seconds <= 120) score += 1;
            return score;
        }

        private static string LinkWhy(TemporalEvent a, TemporalEvent b)
        {
            var why = new List<string>();
            foreach (var kv in a.Identity) { string ov; if (b.Identity.TryGetValue(kv.Key, out ov) && kv.Value == ov && !String.IsNullOrWhiteSpace(kv.Value)) why.Add(kv.Key); }
            double s = TimeDistance(a, b); if (s < Double.MaxValue && s <= 120) why.Add("Δ" + s.ToString("0.###", CultureInfo.InvariantCulture) + "s");
            return why.Count == 0 ? "temporal" : String.Join("+", why.ToArray());
        }

        private static double TimeDistance(TemporalEvent a, TemporalEvent b)
        {
            if (a.Utc == DateTime.MinValue || b.Utc == DateTime.MinValue) return Double.MaxValue;
            return Math.Abs((a.Utc - b.Utc).TotalSeconds);
        }

        private static void OpenCausalityIndex(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 260);
            var anchors = events.Where(e => e.Identity.Count > 0).Take(900).ToList();
            var w = BaseWindow(owner, "YOMI · CAUSALITY CANDIDATE ENGINE", 1140, 760);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            h.Children.Add(T("CAUSALITY CANDIDATE ENGINE", 23, Text, FontWeights.SemiBold));
            h.Children.Add(T("Choose an anchor event. YOMI computes its highest-scoring evidence neighborhood without pretending statistical linkage proves causation.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { ItemsSource = anchors, Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            list.MouseDoubleClick += delegate { var ev = list.SelectedItem as TemporalEvent; if (ev != null) OpenEvidenceCone(w, ctx, ev, events); };
            root.Children.Add(list); w.Show();
        }

        private static void OpenCorrelationConstellation(Window owner, DeepSystemsContext ctx)
        {
            var events = LoadEvents(ctx, 240);
            var counts = new Dictionary<string, Tuple<string, int>>(StringComparer.Ordinal);
            foreach (var ev in events)
            {
                foreach (var kv in ev.Identity)
                {
                    if (String.IsNullOrWhiteSpace(kv.Value)) continue;
                    string id = kv.Key + "\0" + kv.Value;
                    Tuple<string, int> old;
                    counts[id] = counts.TryGetValue(id, out old) ? Tuple.Create(kv.Value, old.Item2 + 1) : Tuple.Create(kv.Value, 1);
                }
            }
            var rows = counts.Select(kv => new { Key = kv.Key.Split('\0')[0], Value = kv.Value.Item1, Count = kv.Value.Item2 }).Where(x => x.Count > 1).OrderByDescending(x => x.Count).ThenBy(x => x.Key).Take(1200).ToList();
            var w = BaseWindow(owner, "YOMI · CORRELATION CONSTELLATION", 1080, 740);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            h.Children.Add(T("CORRELATION CONSTELLATION", 23, Text, FontWeights.SemiBold));
            h.Children.Add(T("Repeated semantic identities spanning recent evidence streams. Double-click any identity to open a filtered chronology.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (var row in rows) list.Items.Add(new ListBoxItem { Content = row.Count.ToString("0000", CultureInfo.InvariantCulture) + "  " + row.Key + " = " + row.Value, Tag = row.Value, Foreground = Text, Background = Bg });
            list.MouseDoubleClick += delegate { var li = list.SelectedItem as ListBoxItem; if (li != null) OpenTimeline(w, ctx, Convert.ToString(li.Tag, CultureInfo.InvariantCulture)); };
            root.Children.Add(list); w.Show();
        }

        private static string VaultPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-temporal-vault.json"); }
        private static string BranchPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-counterfactual-branches.json"); }

        private static List<TemporalEpoch> LoadEpochs(DeepSystemsContext ctx)
        {
            lock (VaultGate)
            {
                try
                {
                    if (!File.Exists(VaultPath(ctx))) return new List<TemporalEpoch>();
                    var obj = Json.DeserializeObject(File.ReadAllText(VaultPath(ctx), Encoding.UTF8)) as Dictionary<string, object>;
                    if (obj == null) return new List<TemporalEpoch>();
                    object raw; if (!obj.TryGetValue("epochs", out raw)) return new List<TemporalEpoch>();
                    var list = raw as IList; if (list == null) return new List<TemporalEpoch>();
                    var result = new List<TemporalEpoch>();
                    foreach (object o in list)
                    {
                        var m = o as Dictionary<string, object>; if (m == null) continue;
                        var ep = new TemporalEpoch { Id = S(m, "id"), CapturedUtc = S(m, "captured_utc"), SessionId = S(m, "session_id"), OrderRevision = I(m, "order_revision") };
                        object fr; if (m.TryGetValue("files", out fr))
                        {
                            var fm = fr as Dictionary<string, object>;
                            if (fm != null) foreach (var kv in fm)
                            {
                                var f = kv.Value as Dictionary<string, object>; if (f == null) continue;
                                ep.Files[kv.Key] = new TemporalFingerprint { Bytes = L(f, "bytes"), ModifiedUtc = S(f, "modified_utc"), Sha256 = S(f, "sha256") };
                            }
                        }
                        result.Add(ep);
                    }
                    return result;
                }
                catch { return new List<TemporalEpoch>(); }
            }
        }

        private static void SaveEpochs(DeepSystemsContext ctx, List<TemporalEpoch> epochs)
        {
            lock (VaultGate)
            {
                Directory.CreateDirectory(ctx.StateRoot);
                while (epochs.Count > 24) epochs.RemoveAt(0);
                var serializedEpochs = new List<object>();
                foreach (var ep in epochs)
                {
                    var files = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in ep.Files)
                    {
                        var f = new Dictionary<string, object>(); f["bytes"] = kv.Value.Bytes; f["modified_utc"] = kv.Value.ModifiedUtc; f["sha256"] = kv.Value.Sha256; files[kv.Key] = f;
                    }
                    var e = new Dictionary<string, object>(); e["id"] = ep.Id; e["captured_utc"] = ep.CapturedUtc; e["session_id"] = ep.SessionId; e["order_revision"] = ep.OrderRevision; e["files"] = files; serializedEpochs.Add(e);
                }
                var doc = new Dictionary<string, object>(); doc["schema"] = 1; doc["updated_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); doc["epochs"] = serializedEpochs;
                AtomicWrite(VaultPath(ctx), Json.Serialize(doc));
            }
        }

        private static TemporalEpoch CaptureEpoch(DeepSystemsContext ctx)
        {
            var ep = new TemporalEpoch { Id = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(), CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
            var order = ReadMap(Path.Combine(ctx.StateRoot, "session-order.json")); ep.SessionId = S(order, "session_id"); ep.OrderRevision = I(order, "revision");
            foreach (string p in ImportantFiles(ctx))
            {
                try
                {
                    var fi = new FileInfo(p); if (!fi.Exists) continue;
                    ep.Files[Relative(ctx.DataRoot, p)] = new TemporalFingerprint { Bytes = fi.Length, ModifiedUtc = fi.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture), Sha256 = Sha256File(p) };
                }
                catch { }
            }
            return ep;
        }

        private static List<string> ImportantFiles(DeepSystemsContext ctx)
        {
            var names = new[] { "current.json", "queue-runtime.json", "session-order.json", "session.json", "runtime-instance.json", "runtime-lease.json", "controller-intent-head.json", "controller-intent-journal.jsonl", "session-journal.jsonl", "freeze-window.json", "video-capability.json", "controller-queue-checkpoints.json", "controller-library.json" };
            var files = names.Select(n => Path.Combine(ctx.StateRoot, n)).Where(File.Exists).ToList();
            return files;
        }

        private static void OpenEpochVault(Window owner, DeepSystemsContext ctx)
        {
            var w = BaseWindow(owner, "YOMI · EPOCH / DRIFT RADAR", 1080, 740);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            h.Children.Add(T("EPOCH / DRIFT RADAR", 23, Text, FontWeights.SemiBold));
            h.Children.Add(T("Capture bounded SHA-256 fingerprints of important control-plane artifacts. Compare epochs to discover exactly what drifted while you explored.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(actions, Dock.Top); root.Children.Add(actions);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            root.Children.Add(list);
            Action refresh = delegate
            {
                list.Items.Clear(); var eps = LoadEpochs(ctx);
                foreach (var ep in eps.OrderByDescending(x => x.CapturedUtc)) list.Items.Add(new ListBoxItem { Content = ep.CapturedUtc + "  ·  R" + ep.OrderRevision.ToString(CultureInfo.InvariantCulture) + "  ·  " + ep.Files.Count.ToString(CultureInfo.InvariantCulture) + " artifacts  ·  " + ep.Id, Tag = ep, Foreground = Text, Background = Bg });
            };
            actions.Children.Add(Btn("CAPTURE EPOCH", delegate { var eps = LoadEpochs(ctx); eps.Add(CaptureEpoch(ctx)); SaveEpochs(ctx, eps); refresh(); }));
            actions.Children.Add(Btn("DIFF TWO NEWEST", delegate { var eps = LoadEpochs(ctx).OrderBy(x => x.CapturedUtc).ToList(); if (eps.Count >= 2) OpenEpochDiff(w, ctx, eps[eps.Count - 2], eps[eps.Count - 1]); }));
            list.MouseDoubleClick += delegate { var li = list.SelectedItem as ListBoxItem; var ep = li == null ? null : li.Tag as TemporalEpoch; if (ep != null) DeepSystems.OpenExternalValue(w, ctx, "TEMPORAL EPOCH " + ep.Id, ep, "ATLAS › TEMPORAL › EPOCH › " + ep.Id); };
            refresh(); w.Show();
        }

        private static void OpenEpochDiff(Window owner, DeepSystemsContext ctx, TemporalEpoch a, TemporalEpoch b)
        {
            var keys = a.Files.Keys.Union(b.Files.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var rows = new List<Dictionary<string, object>>();
            foreach (string k in keys)
            {
                TemporalFingerprint fa, fb; a.Files.TryGetValue(k, out fa); b.Files.TryGetValue(k, out fb);
                string state = fa == null ? "ADDED" : fb == null ? "REMOVED" : fa.Sha256 == fb.Sha256 ? "UNCHANGED" : "CHANGED";
                var m = new Dictionary<string, object>(); m["artifact"] = k; m["state"] = state; m["before"] = fa; m["after"] = fb; rows.Add(m);
            }
            DeepSystems.OpenExternalValue(owner, ctx, "EPOCH DIFF " + a.Id + " → " + b.Id, rows, "ATLAS › TEMPORAL › EPOCH › DIFF");
        }

        private static void OpenCounterfactual(Window owner, DeepSystemsContext ctx, TemporalEvent anchor)
        {
            var orderMap = ReadMap(Path.Combine(ctx.StateRoot, "session-order.json"));
            var order = IntList(orderMap, "order");
            var current = ReadMap(Path.Combine(ctx.StateRoot, "current.json"));
            int currentOcc = FirstInt(current, new[] { "occurrence_id", "occurrence", "index" });
            var w = BaseWindow(owner, "YOMI · COUNTERFACTUAL CHAMBER", 1160, 780);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            h.Children.Add(T("COUNTERFACTUAL CHAMBER", 23, Text, FontWeights.SemiBold));
            h.Children.Add(T("Simulation-only branch. Baseline is copied from authoritative state; scenarios never write session-order.json or send engine commands.", 12.5, Muted, FontWeights.Normal));
            DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            DockPanel.SetDock(actions, Dock.Top); root.Children.Add(actions);
            var output = new TextBox { IsReadOnly = true, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), TextWrapping = TextWrapping.NoWrap };
            root.Children.Add(output);
            if (order.Count == 0) output.Text = "No authoritative order is materialized.";
            Action<string, Func<List<int>, List<int>>> run = delegate(string name, Func<List<int>, List<int>> transform)
            {
                var baseline = new List<int>(order); var branch = transform(new List<int>(order));
                var record = new TemporalBranchRecord { Id = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(), ParentBranchId = "", Generation = 1, CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), Name = name, ScenarioCode = name.ToUpperInvariant().Replace(' ', '_'), BaselineSessionId = S(orderMap, "session_id"), BaselineRevision = I(orderMap, "revision"), AnchorOccurrence = currentOcc, BaselineOrder = baseline, BranchOrder = branch, Hypothesis = anchor == null ? name : (name + " after evidence " + anchor.Fingerprint.Substring(0, 12)) };
                record.RootBranchId = record.Id;
                record.BranchFingerprint = Sha256Hex(String.Join(",", branch.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()));
                record.ChangedSlots = Enumerable.Range(0, Math.Min(baseline.Count, branch.Count)).Count(i => baseline[i] != branch[i]) + Math.Abs(baseline.Count - branch.Count);
                SaveBranch(ctx, record);
                output.Text = BranchReport(record, currentOcc);
            };
            actions.Children.Add(Btn("DEFER NEXT TO END", delegate { run("Defer next occurrence to end", xs => MoveOccurrence(xs, NextAfterCurrent(xs, currentOcc), xs.Count + 1)); }));
            actions.Children.Add(Btn("ROTATE NEXT 5", delegate { run("Rotate next five", xs => RotateUpcoming(xs, currentOcc, 5)); }));
            actions.Children.Add(Btn("REVERSE NEXT 5", delegate { run("Reverse next five", xs => ReverseUpcoming(xs, currentOcc, 5)); }));
            actions.Children.Add(Btn("MOVE FIRST RISKY BEHIND READY", delegate { run("Defer first risky occurrence behind ready candidate", xs => RiskAwareBranch(ctx, xs, currentOcc)); }));
            actions.Children.Add(Btn("OPEN BRANCH VAULT", delegate { OpenBranchVault(w, ctx); }));
            if (anchor != null) actions.Children.Add(Btn("ANCHOR EVIDENCE", delegate { OpenEventDossier(w, ctx, anchor, LoadEvents(ctx, 240)); }));
            output.Text = "Baseline session = " + S(orderMap, "session_id") + Environment.NewLine + "baseline revision = R" + I(orderMap, "revision") + Environment.NewLine + "occurrences = " + order.Count + Environment.NewLine + "current occurrence = " + currentOcc + Environment.NewLine + Environment.NewLine + "Choose a scenario. Each result is persisted as an additive controller-owned branch record.";
            w.Show();
        }

        private static int NextAfterCurrent(List<int> order, int current)
        {
            int i = order.IndexOf(current); if (i < 0) return order.Count > 0 ? order[0] : 0; return i + 1 < order.Count ? order[i + 1] : 0;
        }

        private static List<int> MoveOccurrence(List<int> order, int occurrence, int boundaryOneBased)
        {
            if (occurrence == 0 || !order.Contains(occurrence)) return order;
            int old = order.IndexOf(occurrence); order.RemoveAt(old);
            int idx = Math.Max(0, Math.Min(order.Count, boundaryOneBased - 1)); if (old < boundaryOneBased - 1) idx = Math.Max(0, idx - 1);
            order.Insert(idx, occurrence); return order;
        }

        private static List<int> RotateUpcoming(List<int> order, int current, int count)
        {
            int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n > 1) { int first = order[start]; order.RemoveAt(start); order.Insert(start + n - 1, first); } return order;
        }

        private static List<int> ReverseUpcoming(List<int> order, int current, int count)
        {
            int i = order.IndexOf(current); int start = i >= 0 ? i + 1 : 0; int n = Math.Min(count, order.Count - start); if (n > 1) order.Reverse(start, n); return order;
        }

        private static List<int> RiskAwareBranch(DeepSystemsContext ctx, List<int> order, int current)
        {
            var q = ReadMap(Path.Combine(ctx.StateRoot, "queue-runtime.json")); object raw; var items = new List<Dictionary<string, object>>();
            if (q.TryGetValue("items", out raw)) { var al = raw as IList; if (al != null) foreach (object o in al) { var m = o as Dictionary<string, object>; if (m != null) items.Add(m); } }
            var risky = items.Where(m => { string r = S(m, "risk").ToUpperInvariant(); return r == "URGENT" || r == "AT_RISK"; }).OrderBy(m => I(m, "relative")).FirstOrDefault();
            var ready = items.Where(m => I(m, "relative") > 0 && (S(m, "sync_ready").ToLowerInvariant() == "true" || S(m, "phase").ToUpperInvariant().Contains("READY"))).OrderBy(m => I(m, "relative")).FirstOrDefault();
            if (risky == null || ready == null) return order;
            int ro = FirstInt(risky, new[] { "occurrence_id", "occurrence", "index" }); int rd = FirstInt(ready, new[] { "occurrence_id", "occurrence", "index" });
            if (ro == 0 || rd == 0 || ro == rd) return order;
            int target = order.IndexOf(rd) + 2; return MoveOccurrence(order, ro, target);
        }

        private static string BranchReport(TemporalBranchRecord r, int current)
        {
            var sb = new StringBuilder(); sb.AppendLine("COUNTERFACTUAL BRANCH " + r.Id); sb.AppendLine("scenario = " + r.Name); sb.AppendLine("baseline = " + r.BaselineSessionId + " / R" + r.BaselineRevision); sb.AppendLine("current occurrence = " + current); sb.AppendLine();
            int changes = 0; int n = Math.Min(r.BaselineOrder.Count, r.BranchOrder.Count); for (int i = 0; i < n; i++) if (r.BaselineOrder[i] != r.BranchOrder[i]) changes++;
            sb.AppendLine("changed slots = " + changes); sb.AppendLine("cardinality preserved = " + (r.BaselineOrder.Count == r.BranchOrder.Count)); sb.AppendLine("occurrence set preserved = " + new HashSet<int>(r.BaselineOrder).SetEquals(r.BranchOrder)); sb.AppendLine();
            sb.AppendLine("SLOT    BASELINE    BRANCH");
            for (int i = 0; i < n; i++) if (r.BaselineOrder[i] != r.BranchOrder[i]) sb.AppendLine((i + 1).ToString("0000") + "    " + r.BaselineOrder[i].ToString("000000") + "      " + r.BranchOrder[i].ToString("000000"));
            if (changes == 0) sb.AppendLine("(scenario produced no structural change)");
            sb.AppendLine(); sb.AppendLine("SIMULATION ONLY · no engine command sent · no authoritative state changed"); return sb.ToString();
        }

        private static List<TemporalBranchRecord> LoadBranches(DeepSystemsContext ctx)
        {
            lock (VaultGate)
            {
                try
                {
                    if (!File.Exists(BranchPath(ctx))) return new List<TemporalBranchRecord>();
                    var root = Json.DeserializeObject(File.ReadAllText(BranchPath(ctx), Encoding.UTF8)) as Dictionary<string, object>; if (root == null) return new List<TemporalBranchRecord>();
                    object raw; if (!root.TryGetValue("branches", out raw)) return new List<TemporalBranchRecord>(); var al = raw as IList; if (al == null) return new List<TemporalBranchRecord>();
                    var result = new List<TemporalBranchRecord>(); foreach (object o in al)
                    {
                        var m = o as Dictionary<string, object>; if (m == null) continue;
                        result.Add(new TemporalBranchRecord { Id = S(m, "id"), ParentBranchId = S(m, "parent_branch_id"), RootBranchId = S(m, "root_branch_id"), Generation = I(m, "generation"), CreatedUtc = S(m, "created_utc"), Name = S(m, "name"), ScenarioCode = S(m, "scenario_code"), BaselineSessionId = S(m, "baseline_session_id"), BaselineRevision = I(m, "baseline_revision"), AnchorOccurrence = I(m, "anchor_occurrence"), BaselineOrder = IntList(m, "baseline_order"), BranchOrder = IntList(m, "branch_order"), Hypothesis = S(m, "hypothesis"), Notes = S(m, "notes"), ParentFingerprint = S(m, "parent_fingerprint"), BranchFingerprint = S(m, "branch_fingerprint"), ChangedSlots = I(m, "changed_slots"), ParentChangedSlots = I(m, "parent_changed_slots"), StructuralDistance = DoubleValue(m, "structural_distance"), LongestStablePrefix = I(m, "longest_stable_prefix"), AdjacencyRetention = DoubleValue(m, "adjacency_retention") });
                    }
                    return result;
                }
                catch { return new List<TemporalBranchRecord>(); }
            }
        }

        private static void SaveBranch(DeepSystemsContext ctx, TemporalBranchRecord record)
        {
            lock (VaultGate)
            {
                var branches = LoadBranches(ctx); branches.Add(record); while (branches.Count > 64) branches.RemoveAt(0);
                var serializedBranches = new List<object>();
                foreach (var b in branches)
                {
                    var m = new Dictionary<string, object>();
                    m["id"] = b.Id; m["parent_branch_id"] = b.ParentBranchId; m["root_branch_id"] = b.RootBranchId; m["generation"] = b.Generation;
                    m["created_utc"] = b.CreatedUtc; m["name"] = b.Name; m["scenario_code"] = b.ScenarioCode; m["baseline_session_id"] = b.BaselineSessionId; m["baseline_revision"] = b.BaselineRevision; m["anchor_occurrence"] = b.AnchorOccurrence;
                    m["baseline_order"] = b.BaselineOrder; m["branch_order"] = b.BranchOrder; m["hypothesis"] = b.Hypothesis; m["notes"] = b.Notes;
                    m["parent_fingerprint"] = b.ParentFingerprint; m["branch_fingerprint"] = b.BranchFingerprint; m["changed_slots"] = b.ChangedSlots; m["parent_changed_slots"] = b.ParentChangedSlots;
                    m["structural_distance"] = b.StructuralDistance; m["longest_stable_prefix"] = b.LongestStablePrefix; m["adjacency_retention"] = b.AdjacencyRetention;
                    serializedBranches.Add(m);
                }
                var doc = new Dictionary<string, object>(); doc["schema"] = 2; doc["updated_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); doc["branches"] = serializedBranches;
                Directory.CreateDirectory(ctx.StateRoot); AtomicWrite(BranchPath(ctx), Json.Serialize(doc));
            }
        }

        private static void OpenBranchVault(Window owner, DeepSystemsContext ctx)
        {
            var branches = LoadBranches(ctx).OrderByDescending(x => x.CreatedUtc).ToList();
            var w = BaseWindow(owner, "YOMI · COUNTERFACTUAL BRANCH VAULT", 1080, 720);
            var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; h.Children.Add(T("COUNTERFACTUAL BRANCH VAULT", 23, Text, FontWeights.SemiBold)); h.Children.Add(T("Bounded controller-owned simulation records. These are hypotheses, never session/order authority.", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (var b in branches) list.Items.Add(new ListBoxItem { Content = b.CreatedUtc + "  ·  " + b.Name + "  ·  R" + b.BaselineRevision + "  ·  " + b.Id, Tag = b, Foreground = Text, Background = Bg });
            list.MouseDoubleClick += delegate { var li = list.SelectedItem as ListBoxItem; var br = li == null ? null : li.Tag as TemporalBranchRecord; if (br != null) CausalGraphLab.OpenBranchUniverse(w, ctx, br.Id); };
            root.Children.Add(list); w.Show();
        }

        private static void OpenFossilLab(Window owner, DeepSystemsContext ctx)
        {
            var items = new List<string>();
            foreach (string p in new[] { Path.Combine(ctx.StateRoot, "controller-deep-snapshots.json"), Path.Combine(ctx.StateRoot, "controller-queue-checkpoints.json"), Path.Combine(ctx.StateRoot, "controller-intent-journal.jsonl"), Path.Combine(ctx.StateRoot, "controller-intent-recovery.jsonl"), Path.Combine(ctx.StateRoot, "session-journal.jsonl"), VaultPath(ctx), BranchPath(ctx) }) if (File.Exists(p)) items.Add(p);
            OpenFileIndex(owner, ctx, "STATE FOSSIL LAB", "Historical and append-only strata discovered in the control plane.", items);
        }

        private static void OpenTemporalFiles(Window owner, DeepSystemsContext ctx)
        {
            OpenFileIndex(owner, ctx, "TEMPORAL DATA CATACOMBS", "Every recent event/log/history-like stream discovered by the Temporal Observatory scanner.", EventFiles(ctx));
        }

        private static void OpenFileIndex(Window owner, DeepSystemsContext ctx, string title, string subtitle, List<string> paths)
        {
            var w = BaseWindow(owner, "YOMI · " + title, 1080, 720); var root = new DockPanel { Margin = new Thickness(14) }; w.Content = root;
            var h = new StackPanel { Margin = new Thickness(0, 0, 0, 10) }; h.Children.Add(T(title, 23, Text, FontWeights.SemiBold)); h.Children.Add(T(subtitle, 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            var list = new ListBox { Background = Bg, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (string p in paths) list.Items.Add(new ListBoxItem { Content = Relative(ctx.DataRoot, p) + "  ·  " + new FileInfo(p).Length.ToString("N0", CultureInfo.InvariantCulture) + " bytes", Tag = p, Foreground = Text, Background = Bg });
            list.MouseDoubleClick += delegate { var li = list.SelectedItem as ListBoxItem; string p = li == null ? null : li.Tag as string; if (!String.IsNullOrWhiteSpace(p)) DeepSystems.OpenExternalFile(w, ctx, p, "ATLAS › TEMPORAL › ARTIFACT"); };
            root.Children.Add(list); w.Show();
        }

        private static Dictionary<string, object> ReadMap(string path)
        {
            try { if (!File.Exists(path)) return new Dictionary<string, object>(); return Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object> ?? new Dictionary<string, object>(); } catch { return new Dictionary<string, object>(); }
        }

        private static List<int> IntList(Dictionary<string, object> map, string key)
        {
            var list = new List<int>(); object raw;
            if (!map.TryGetValue(key, out raw))
            {
                var kv = map.FirstOrDefault(x => String.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)); raw = kv.Value; if (raw == null) return list;
            }
            var al = raw as IList; if (al == null) return list;
            foreach (object o in al) { int v; if (Int32.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), out v)) list.Add(v); } return list;
        }

        private static int FirstInt(Dictionary<string, object> map, string[] keys) { foreach (string k in keys) { int v = I(map, k); if (v != 0) return v; } return 0; }
        private static string S(Dictionary<string, object> m, string k)
        {
            if (m == null) return ""; object v; if (m.TryGetValue(k, out v) && v != null) return Convert.ToString(v, CultureInfo.InvariantCulture);
            foreach (var kv in m) if (String.Equals(kv.Key, k, StringComparison.OrdinalIgnoreCase) && kv.Value != null) return Convert.ToString(kv.Value, CultureInfo.InvariantCulture);
            return "";
        }
        private static int I(Dictionary<string, object> m, string k) { int x; return Int32.TryParse(S(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out x) ? x : 0; }
        private static long L(Dictionary<string, object> m, string k) { long x; return Int64.TryParse(S(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out x) ? x : 0L; }
        private static double DoubleValue(Dictionary<string, object> m, string k) { double x; return Double.TryParse(S(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out x) ? x : 0.0; }

        private static string Relative(string root, string path)
        {
            try { var r = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar; return path.StartsWith(r, StringComparison.OrdinalIgnoreCase) ? path.Substring(r.Length) : path; } catch { return path; }
        }

        private static string Sha256Hex(string text)
        {
            using (var sha = SHA256.Create()) return String.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)).ToArray());
        }

        private static string Sha256File(string path)
        {
            using (var sha = SHA256.Create()) using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return String.Concat(sha.ComputeHash(fs).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)).ToArray());
        }

        private static void AtomicWrite(string path, string text)
        {
            string tmp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text, new UTF8Encoding(false));
            if (File.Exists(path)) { string bak = path + ".bak"; try { File.Replace(tmp, path, bak, true); try { File.Delete(bak); } catch { } } catch { File.Copy(tmp, path, true); File.Delete(tmp); } }
            else File.Move(tmp, path);
        }
    }
}

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
    internal sealed class ResearchArtifact
    {
        public string Path;
        public string Category;
        public long Bytes;
        public DateTime ModifiedUtc;
        public string Sample;
        public Dictionary<string, string> Identity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public int ErrorCount;
        public int WarningCount;
        public override string ToString()
        {
            return (Category ?? "OTHER") + "   " + System.IO.Path.GetFileName(Path ?? "") + "   ·   " + FormatBytes(Bytes) + "   ·   " + ModifiedUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }
        private static string FormatBytes(long value)
        {
            if (value >= 1024L * 1024L) return (value / (1024d * 1024d)).ToString("0.0", CultureInfo.InvariantCulture) + " MiB";
            if (value >= 1024L) return (value / 1024d).ToString("0.0", CultureInfo.InvariantCulture) + " KiB";
            return value.ToString(CultureInfo.InvariantCulture) + " B";
        }
    }

    internal sealed class ResearchSignal
    {
        public string Id;
        public string Kind;
        public string Title;
        public string Summary;
        public string Epistemic;
        public int Severity;
        public int Novelty;
        public int Confidence;
        public string PrimaryPath;
        public string IdentityKey;
        public string IdentityValue;
        public List<string> EvidencePaths = new List<string>();
        public Dictionary<string, object> Evidence = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public override string ToString()
        {
            return "[" + (Epistemic ?? "OBSERVED") + "]   S" + Severity.ToString(CultureInfo.InvariantCulture) + " N" + Novelty.ToString(CultureInfo.InvariantCulture) + " C" + Confidence.ToString(CultureInfo.InvariantCulture) + "   " + (Title ?? Kind ?? "signal");
        }
    }

    internal sealed class ResearchHypothesis
    {
        public string Id;
        public string Title;
        public string Proposition;
        public int Score;
        public int SupportingSignals;
        public int ContradictingSignals;
        public string IdentityKey;
        public string IdentityValue;
        public List<string> SignalIds = new List<string>();
        public List<string> EvidencePaths = new List<string>();
        public override string ToString()
        {
            return "H" + Score.ToString("000", CultureInfo.InvariantCulture) + "   " + (Title ?? "hypothesis") + "   ·   " + SupportingSignals.ToString(CultureInfo.InvariantCulture) + " support / " + ContradictingSignals.ToString(CultureInfo.InvariantCulture) + " counter";
        }
    }

    internal sealed class ResearchSessionRecord
    {
        public string Id;
        public string CreatedUtc;
        public string UpdatedUtc;
        public string Title;
        public string SeedKind;
        public string SeedFingerprint;
        public string Question;
        public string WorkingHypothesis;
        public List<string> EvidencePaths = new List<string>();
        public List<string> IdentityTokens = new List<string>();
        public List<string> Notes = new List<string>();
    }

    internal sealed class ResearchStore
    {
        public int Schema = 1;
        public List<ResearchSessionRecord> Sessions = new List<ResearchSessionRecord>();
        public List<string> ReviewedLeadFingerprints = new List<string>();
    }

    internal sealed class ResearchUniverse
    {
        public DateTime ScannedUtc;
        public List<ResearchArtifact> Artifacts = new List<ResearchArtifact>();
        public Dictionary<string, List<ResearchArtifact>> ByToken = new Dictionary<string, List<ResearchArtifact>>(StringComparer.OrdinalIgnoreCase);
        public List<ResearchSignal> Signals = new List<ResearchSignal>();
        public List<ResearchHypothesis> Hypotheses = new List<ResearchHypothesis>();
    }

    internal static class ResearchWorkbench
    {
        private static readonly Brush Bg = B("#080C10");
        private static readonly Brush Surface = B("#121820");
        private static readonly Brush Raised = B("#1A232D");
        private static readonly Brush Border = B("#33424F");
        private static readonly Brush Text = B("#F0F6FA");
        private static readonly Brush Muted = B("#9EAFBC");
        private static readonly Brush Accent = B("#78F0BE");
        private static readonly Brush Blue = B("#73B9FF");
        private static readonly Brush Amber = B("#FFD16E");
        private static readonly Brush Danger = B("#FF7E88");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();
        private static ResearchUniverse LastUniverse;

        private static readonly string[] SemanticKeys = new string[]
        {
            "intent_id","session_id","source_key","occurrence_id","order_revision","plan_fingerprint","entry_hash","prev_hash","transaction_id","work_generation","failure_domain","decision_reason","route_label","risk","health","buffer_health","service_level","governor","resource_mode","state","oracle_verdict","oracle_confidence","rehearsal","freeze"
        };

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-research-workbench.json"); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            ResearchUniverse u = ScanUniverse(ctx);
            LastUniverse = u;
            OpenHub(owner, ctx, u);
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
            StackPanel s = new StackPanel();
            TextBlock b = T(badge, 11, Accent, FontWeights.Bold); b.Margin = new Thickness(0, 0, 0, 4); s.Children.Add(b);
            s.Children.Add(T(title, 18, Text, FontWeights.SemiBold));
            TextBlock d = T(subtitle, 12.5, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(d);
            Border c = Card(s); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { if (open != null) open(); }; panel.Children.Add(c);
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx, ResearchUniverse u)
        {
            Window w = W(owner, "YOMI · RESEARCH WORKBENCH", 1100, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(20) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("RESEARCH WORKBENCH", 29, Text, FontWeights.Bold));
            head.Children.Add(T("Passive machine-assisted investigation · observation, inference and hypothesis remain distinct", 13, Muted, FontWeights.Normal));
            TextBlock scan = T("Universe " + u.Artifacts.Count.ToString(CultureInfo.InvariantCulture) + " artifacts · " + u.ByToken.Count.ToString(CultureInfo.InvariantCulture) + " identity tokens · " + u.Signals.Count.ToString(CultureInfo.InvariantCulture) + " leads · scanned " + u.ScannedUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture), 11.5, Blue, FontWeights.SemiBold);
            scan.Margin = new Thickness(0, 6, 0, 14); head.Children.Add(scan);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel p = new WrapPanel(); sv.Content = p; root.Children.Add(sv);

            Portal(p, "SERENDIPITY", "UNEXPECTED LEADS", "Ranked things YOMI thinks are unusually connected, contradictory, rare, risky or simply worth opening next.", delegate { OpenSignalList(w, ctx, u, SerendipityLeads(u), "UNEXPECTED LEADS", "Novel/risky/rare evidence selected without asserting causation."); });
            Portal(p, "OBSERVED", "ANOMALY OBSERVATORY", "Mechanically detected contradictions, stress markers, ambiguous outcomes, low-confidence decisions and evidence drift.", delegate { OpenSignalList(w, ctx, u, u.Signals.Where(x => String.Equals(x.Epistemic, "OBSERVED", StringComparison.OrdinalIgnoreCase)).ToList(), "ANOMALY OBSERVATORY", "Directly observed state and mechanically checkable contradictions."); });
            Portal(p, "INFERRED", "EMERGENT PATTERN MINER", "Rare bridges, convergence points and repeated cross-subsystem motifs synthesized from shared semantic identities.", delegate { OpenSignalList(w, ctx, u, u.Signals.Where(x => String.Equals(x.Epistemic, "INFERRED", StringComparison.OrdinalIgnoreCase)).ToList(), "EMERGENT PATTERN MINER", "Cross-artifact structure mechanically derived from observed identity relationships."); });
            Portal(p, "HYPOTHESIS", "HYPOTHESIS FORGE", "Ranked explanations generated from signals, each with supporting evidence and explicit uncertainty.", delegate { OpenHypothesisList(w, ctx, u); });
            Portal(p, "TOPOLOGY", "SYSTEM CARTOGRAPHER", "Synthesize a live map of artifact categories, identity bridges, convergence points and isolated evidence islands.", delegate { OpenTopology(w, ctx, u); });
            Portal(p, "QUERY", "LENS COMPOSER", "Build an ad-hoc research lens over paths, text, semantic identities and anomaly labels.", delegate { OpenLensComposer(w, ctx, u); });
            Portal(p, "CASES", "RESEARCH SESSIONS", "Persistent case files seeded from leads or hypotheses. Evidence remains additive and never becomes playback authority.", delegate { OpenResearchSessions(w, ctx, u); });
            Portal(p, "FRONTIER", "UNEXPLORED FRONTIER", "Leads not previously marked reviewed, biased toward rare cross-system identities and high novelty.", delegate { OpenSignalList(w, ctx, u, UnreviewedLeads(ctx, u), "UNEXPLORED FRONTIER", "Interesting leads that this controller profile has not marked reviewed yet."); });
            Portal(p, "GRAPH", "CAUSAL GRAPH LAB", "Leave the workbench through a discovered relationship and continue into evidence topology / simulation genealogy.", delegate { CausalGraphLab.Open(w, ctx); });
            Portal(p, "TIME", "TEMPORAL OBSERVATORY", "Follow a research lead into chronology, evidence frames and counterfactual history.", delegate { TemporalObservatory.Open(w, ctx); });
            Portal(p, "∞", "DEEP SYSTEMS ATLAS", "Return to the larger recursive world. Research artifacts remain available as cross-links.", delegate { DeepSystems.OpenAtlasFromContext(w); });

            w.Content = root;
            w.Show();
        }

        private static ResearchUniverse ScanUniverse(DeepSystemsContext ctx)
        {
            ResearchUniverse u = new ResearchUniverse { ScannedUtc = DateTime.UtcNow };
            List<string> files = new List<string>();
            try
            {
                if (Directory.Exists(ctx.DataRoot))
                {
                    files = Directory.EnumerateFiles(ctx.DataRoot, "*", SearchOption.AllDirectories)
                        .Where(IsResearchTextArtifact)
                        .Select(x => new FileInfo(x))
                        .OrderByDescending(x => x.LastWriteTimeUtc)
                        .ThenBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
                        .Take(260)
                        .Select(x => x.FullName).ToList();
                }
            }
            catch { }

            foreach (string path in files)
            {
                ResearchArtifact a = ReadArtifact(path);
                if (a == null) continue;
                u.Artifacts.Add(a);
                foreach (KeyValuePair<string, string> kv in a.Identity)
                {
                    string token = Token(kv.Key, kv.Value);
                    List<ResearchArtifact> list;
                    if (!u.ByToken.TryGetValue(token, out list)) { list = new List<ResearchArtifact>(); u.ByToken[token] = list; }
                    list.Add(a);
                }
            }

            BuildObservedSignals(ctx, u);
            BuildPatternSignals(u);
            BuildHypotheses(u);
            return u;
        }

        private static bool IsResearchTextArtifact(string path)
        {
            string e = Path.GetExtension(path ?? "").ToLowerInvariant();
            return e == ".json" || e == ".jsonl" || e == ".log" || e == ".txt" || e == ".ndjson" || e == ".csv";
        }

        private static ResearchArtifact ReadArtifact(string path)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                string sample = ReadTailText(path, 384 * 1024);
                ResearchArtifact a = new ResearchArtifact
                {
                    Path = path,
                    Category = CategoryFor(path),
                    Bytes = fi.Exists ? fi.Length : 0,
                    ModifiedUtc = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue,
                    Sample = sample ?? ""
                };
                a.ErrorCount = Regex.Matches(a.Sample, @"(?im)\b(error|failed|fatal|exception|indeterminate)\b").Count;
                a.WarningCount = Regex.Matches(a.Sample, @"(?im)\b(warn|warning|attention|at_risk|urgent|brownout|emergency)\b").Count;
                ExtractIdentity(a.Sample, a.Identity);
                return a;
            }
            catch { return null; }
        }

        private static string ReadTailText(string path, int maxBytes)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                long start = Math.Max(0, fs.Length - maxBytes);
                fs.Seek(start, SeekOrigin.Begin);
                byte[] buf = new byte[(int)Math.Min(maxBytes, fs.Length - start)];
                int got = fs.Read(buf, 0, buf.Length);
                string s = Encoding.UTF8.GetString(buf, 0, got);
                if (start > 0)
                {
                    int nl = s.IndexOf('\n');
                    if (nl >= 0 && nl + 1 < s.Length) s = s.Substring(nl + 1);
                }
                return s;
            }
        }

        private static void ExtractIdentity(string text, Dictionary<string, string> target)
        {
            if (String.IsNullOrEmpty(text) || target == null) return;
            foreach (string key in SemanticKeys)
            {
                MatchCollection matches = Regex.Matches(text, "[\\\"]?" + Regex.Escape(key) + "[\\\"]?\\s*[:=]\\s*[\\\"]?([^\\\"\\r\\n,} ]+)", RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    string v = matches[matches.Count - 1].Groups[1].Value.Trim();
                    if (v.Length > 0 && v.Length < 256) target[key] = v;
                }
            }
            Match h = Regex.Match(text, @"\b[a-fA-F0-9]{64}\b");
            if (h.Success && !target.ContainsKey("sha256")) target["sha256"] = h.Value.ToLowerInvariant();
            Match sk = Regex.Match(text, @"\b[a-fA-F0-9]{16}\b");
            if (sk.Success && !target.ContainsKey("source_key")) target["source_key"] = sk.Value.ToLowerInvariant();
        }

        private static string CategoryFor(string path)
        {
            string p = (path ?? "").Replace('\\', '/').ToLowerInvariant();
            string f = Path.GetFileName(p);
            if (p.Contains("intent") || f.Contains("operator")) return "INTENT";
            if (p.Contains("oracle")) return "ORACLE";
            if (p.Contains("queue") || p.Contains("session-order") || p.Contains("mutation")) return "QUEUE";
            if (p.Contains("runtime") || p.Contains("lease") || p.Contains("watchdog") || p.Contains("ready")) return "RUNTIME";
            if (p.Contains("freeze") || p.Contains("rehears")) return "BROADCAST";
            if (p.Contains("update") || p.Contains("install")) return "UPDATE";
            if (p.Contains("recover") || p.Contains("repair") || p.Contains("support")) return "RECOVERY";
            if (p.Contains("cache") || p.Contains("object")) return "CACHE";
            if (p.Contains("config") || p.Contains("setting")) return "CONFIG";
            if (p.Contains("log") || Path.GetExtension(p) == ".log") return "LOG";
            return "STATE";
        }

        private static void BuildObservedSignals(DeepSystemsContext ctx, ResearchUniverse u)
        {
            ResearchArtifact queue = FindArtifact(u, "queue-runtime.json");
            ResearchArtifact order = FindArtifact(u, "session-order.json");
            if (queue != null)
            {
                string s = queue.Sample ?? "";
                AddObservedIf(u, Regex.IsMatch(s, @"(?i)""?buffer_health""?\s*:\s*""?(critical|thin)"), "BUFFER PRESSURE", "Queue runtime reports thin/critical transition coverage.", 8, 5, 98, queue, "buffer_health", Value(queue, "buffer_health"));
                AddObservedIf(u, Regex.IsMatch(s, @"(?i)""?safe_mode""?\s*:\s*true"), "SAFE MODE ACTIVE", "Engine runtime projection reports Safe Mode.", 9, 4, 99, queue, "state", "SAFE_MODE");
                AddObservedIf(u, Regex.IsMatch(s, @"(?i)""?audio_circuit_open""?\s*:\s*true"), "AUDIO CIRCUIT OPEN", "Audio circuit breaker is open in the queue runtime projection.", 10, 7, 99, queue, "failure_domain", Value(queue, "failure_domain"));
                AddObservedIf(u, Regex.IsMatch(s, @"(?i)""?risk""?\s*:\s*""?(urgent|at_risk)"), "AT-RISK TRANSITION", "At least one bounded queue item is projected AT_RISK or URGENT.", 8, 6, 98, queue, "risk", "AT_RISK");
                bool freeze = Regex.IsMatch(s, @"(?i)""?freeze""?\s*:\s*\{[^}]*""?active""?\s*:\s*true");
                bool rehearse = Regex.IsMatch(s, @"(?i)""?rehearsal""?\s*:\s*\{[^}]*""?active""?\s*:\s*true");
                AddObservedIf(u, freeze && !rehearse, "FREEZE WITHOUT ACTIVE REHEARSAL", "Broadcast Freeze appears active while the runtime projection does not show an active rehearsal window.", 9, 9, 95, queue, "freeze", "active");
            }

            if (queue != null && order != null)
            {
                int qr = ParseIntIdentity(queue, "order_revision");
                int orr = ParseIntIdentity(order, "revision");
                if (orr < 0) orr = ParseIntIdentity(order, "order_revision");
                if (qr >= 0 && orr >= 0 && qr != orr)
                {
                    ResearchSignal sig = Signal("OBSERVED", "ORDER REVISION DRIFT", "Queue runtime and authoritative session order report different revisions.", 8, 7, 99, queue.Path, "order_revision", qr.ToString(CultureInfo.InvariantCulture));
                    sig.EvidencePaths.Add(order.Path); sig.Evidence["queue_runtime_revision"] = qr; sig.Evidence["session_order_revision"] = orr; u.Signals.Add(sig);
                }
            }

            foreach (ResearchArtifact a in u.Artifacts)
            {
                if (a.ErrorCount >= 5)
                {
                    ResearchSignal sig = Signal("OBSERVED", "ERROR-DENSE EVIDENCE STREAM", Path.GetFileName(a.Path) + " contains " + a.ErrorCount.ToString(CultureInfo.InvariantCulture) + " failure/error markers in the bounded read window.", Math.Min(10, 4 + a.ErrorCount / 3), 5, 92, a.Path, "artifact", Path.GetFileName(a.Path));
                    sig.Evidence["error_markers"] = a.ErrorCount; sig.Evidence["warning_markers"] = a.WarningCount; u.Signals.Add(sig);
                }
                if (Regex.IsMatch(a.Sample ?? "", @"(?i)\b(INDETERMINATE|ABANDONED)\b") && a.Category == "INTENT")
                {
                    ResearchSignal sig = Signal("OBSERVED", "AMBIGUOUS OPERATOR OUTCOME", "Intent evidence contains an INDETERMINATE or ABANDONED terminal state.", 10, 9, 98, a.Path, "intent_id", Value(a, "intent_id"));
                    u.Signals.Add(sig);
                }
                string oc = Value(a, "oracle_confidence");
                double conf;
                if (!String.IsNullOrWhiteSpace(oc) && Double.TryParse(oc.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out conf) && conf >= 0 && conf < 45)
                {
                    ResearchSignal sig = Signal("OBSERVED", "LOW ORACLE CONFIDENCE", "Oracle confidence is below 45% in " + Path.GetFileName(a.Path) + ".", 5, 6, 98, a.Path, "oracle_confidence", oc);
                    u.Signals.Add(sig);
                }
            }
        }

        private static void BuildPatternSignals(ResearchUniverse u)
        {
            foreach (KeyValuePair<string, List<ResearchArtifact>> kv in u.ByToken)
            {
                List<ResearchArtifact> unique = kv.Value.GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
                int cats = unique.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (unique.Count >= 2 && unique.Count <= 5 && cats >= 2)
                {
                    string[] parts = SplitToken(kv.Key);
                    ResearchSignal sig = Signal("INFERRED", "RARE CROSS-SYSTEM BRIDGE", parts[0] + "=" + parts[1] + " appears in only " + unique.Count.ToString(CultureInfo.InvariantCulture) + " recent artifacts but crosses " + cats.ToString(CultureInfo.InvariantCulture) + " subsystem categories.", 3 + Math.Min(4, cats), 10 - Math.Min(5, unique.Count), 88, unique[0].Path, parts[0], parts[1]);
                    foreach (ResearchArtifact a in unique) sig.EvidencePaths.Add(a.Path);
                    sig.Evidence["artifact_count"] = unique.Count; sig.Evidence["category_count"] = cats; u.Signals.Add(sig);
                }
                else if (unique.Count >= 6 && cats >= 3)
                {
                    string[] parts = SplitToken(kv.Key);
                    ResearchSignal sig = Signal("INFERRED", "IDENTITY CONVERGENCE POINT", parts[0] + "=" + parts[1] + " is independently visible across " + cats.ToString(CultureInfo.InvariantCulture) + " subsystem categories and " + unique.Count.ToString(CultureInfo.InvariantCulture) + " bounded artifacts.", Math.Min(8, 3 + cats), 6, 93, unique[0].Path, parts[0], parts[1]);
                    foreach (ResearchArtifact a in unique.Take(12)) sig.EvidencePaths.Add(a.Path);
                    sig.Evidence["artifact_count"] = unique.Count; sig.Evidence["category_count"] = cats; u.Signals.Add(sig);
                }
            }

            Dictionary<string, int> categoryErrors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (ResearchArtifact a in u.Artifacts)
            {
                int n; categoryErrors.TryGetValue(a.Category, out n); categoryErrors[a.Category] = n + a.ErrorCount + a.WarningCount / 2;
            }
            foreach (KeyValuePair<string, int> kv in categoryErrors.Where(x => x.Value >= 12).OrderByDescending(x => x.Value).Take(8))
            {
                ResearchArtifact a = u.Artifacts.FirstOrDefault(x => String.Equals(x.Category, kv.Key, StringComparison.OrdinalIgnoreCase));
                if (a == null) continue;
                ResearchSignal sig = Signal("INFERRED", "SUBSYSTEM STRESS CLUSTER", kv.Key + " evidence contains a concentrated density of recent warning/failure markers across multiple artifacts.", Math.Min(9, 4 + kv.Value / 10), 5, 80, a.Path, "category", kv.Key);
                sig.EvidencePaths.AddRange(u.Artifacts.Where(x => String.Equals(x.Category, kv.Key, StringComparison.OrdinalIgnoreCase) && (x.ErrorCount > 0 || x.WarningCount > 0)).Take(12).Select(x => x.Path));
                sig.Evidence["weighted_marker_count"] = kv.Value; u.Signals.Add(sig);
            }
        }

        private static void BuildHypotheses(ResearchUniverse u)
        {
            foreach (ResearchSignal s in u.Signals.Where(x => x.Severity >= 6).OrderByDescending(x => x.Severity * 10 + x.Novelty).Take(24))
            {
                ResearchHypothesis h = new ResearchHypothesis();
                h.Id = ShortHash((s.Id ?? "") + "|H");
                h.IdentityKey = s.IdentityKey; h.IdentityValue = s.IdentityValue;
                h.SignalIds.Add(s.Id); h.EvidencePaths.AddRange(s.EvidencePaths);
                h.SupportingSignals = 1;
                h.Score = Math.Min(99, 30 + s.Severity * 5 + s.Novelty * 2 + s.Confidence / 10);
                if (s.Title == "ORDER REVISION DRIFT") { h.Title = "PROJECTION LAG OR MID-COMMIT OBSERVATION"; h.Proposition = "The UI/runtime projection may have been sampled between an authoritative order commit and downstream projection refresh. Investigate mutation ledger timing before treating this as corruption."; }
                else if (s.Title == "BUFFER PRESSURE" || s.Title == "AT-RISK TRANSITION") { h.Title = "PREPARATION DEADLINE PRESSURE"; h.Proposition = "Upcoming transition preparation may be consuming its deadline slack faster than the current worker/resource envelope can replenish it. Inspect Oracle estimate, Aegis mode, jobs and failure domain."; }
                else if (s.Title == "AMBIGUOUS OPERATOR OUTCOME") { h.Title = "AUTHORITY ACKNOWLEDGEMENT GAP"; h.Proposition = "The controller likely lost observation continuity around an authority-bound intent. The operator journal can prove what was requested/sent but may deliberately refuse to infer whether the engine committed it."; }
                else if (s.Title == "FREEZE WITHOUT ACTIVE REHEARSAL") { h.Title = "BROADCAST WINDOW STATE DIVERGENCE"; h.Proposition = "Freeze lineage may have outlived or become temporarily desynchronized from the rehearsal projection. Follow freeze-window persistence, queue runtime and event chronology."; }
                else if (s.Title == "LOW ORACLE CONFIDENCE") { h.Title = "SPARSE OR CONTRADICTORY ORACLE EVIDENCE"; h.Proposition = "The prediction model may have insufficient or conflicting recent evidence for this route/source state. Inspect Oracle factors and route capability memory before trusting the ETA strongly."; }
                else if (s.Title == "IDENTITY CONVERGENCE POINT" || s.Title == "RARE CROSS-SYSTEM BRIDGE") { h.Title = "SHARED IDENTITY EXPLAINS CROSS-SYSTEM CLUSTER"; h.Proposition = "The apparent relationship is plausibly explained by a shared semantic identity rather than direct causal coupling. Trace the identity chronologically and look for a stronger authority edge."; }
                else { h.Title = "EVIDENCE CLUSTER REQUIRES DISAMBIGUATION"; h.Proposition = "Multiple observations cluster around this signal, but the current bounded evidence is insufficient to distinguish direct cause, projection lag, shared identity or coincidence. Follow the evidence paths and seek counterevidence."; }
                u.Hypotheses.Add(h);
            }
        }

        private static ResearchSignal Signal(string epistemic, string title, string summary, int severity, int novelty, int confidence, string path, string key, string value)
        {
            ResearchSignal s = new ResearchSignal();
            s.Epistemic = epistemic; s.Kind = title; s.Title = title; s.Summary = summary; s.Severity = Math.Max(0, Math.Min(10, severity)); s.Novelty = Math.Max(0, Math.Min(10, novelty)); s.Confidence = Math.Max(0, Math.Min(100, confidence)); s.PrimaryPath = path; s.IdentityKey = key; s.IdentityValue = value;
            if (!String.IsNullOrWhiteSpace(path)) s.EvidencePaths.Add(path);
            s.Id = ShortHash(epistemic + "|" + title + "|" + path + "|" + key + "|" + value);
            return s;
        }

        private static void AddObservedIf(ResearchUniverse u, bool condition, string title, string summary, int severity, int novelty, int confidence, ResearchArtifact a, string key, string value)
        {
            if (!condition || a == null) return;
            u.Signals.Add(Signal("OBSERVED", title, summary, severity, novelty, confidence, a.Path, key, value));
        }

        private static List<ResearchSignal> SerendipityLeads(ResearchUniverse u)
        {
            return u.Signals.OrderByDescending(x => x.Novelty * 7 + x.Severity * 5 + x.Confidence / 4).ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase).Take(48).ToList();
        }

        private static List<ResearchSignal> UnreviewedLeads(DeepSystemsContext ctx, ResearchUniverse u)
        {
            ResearchStore store = LoadStore(ctx);
            HashSet<string> reviewed = new HashSet<string>(store.ReviewedLeadFingerprints ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            return SerendipityLeads(u).Where(x => !reviewed.Contains(x.Id)).Take(48).ToList();
        }

        private static void OpenSignalList(Window owner, DeepSystemsContext ctx, ResearchUniverse u, List<ResearchSignal> signals, string title, string subtitle)
        {
            Window w = W(owner, "YOMI · " + title, 1120, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T(title, 27, Text, FontWeights.Bold)); h.Children.Add(T(subtitle, 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ListBox list = new ListBox { Margin = new Thickness(0, 14, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (ResearchSignal s in signals) list.Items.Add(s);
            list.MouseDoubleClick += delegate { ResearchSignal s = list.SelectedItem as ResearchSignal; if (s != null) OpenSignalDossier(w, ctx, u, s); };
            root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenSignalDossier(Window owner, DeepSystemsContext ctx, ResearchUniverse u, ResearchSignal s)
        {
            Window w = W(owner, "YOMI · LEAD · " + s.Title, 960, 720);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel top = new StackPanel();
            top.Children.Add(T(s.Epistemic + " LEAD", 11, s.Epistemic == "HYPOTHESIS" ? Amber : Accent, FontWeights.Bold));
            top.Children.Add(T(s.Title, 27, Text, FontWeights.Bold));
            top.Children.Add(T(s.Summary, 13, Muted, FontWeights.Normal));
            top.Children.Add(T("Severity " + s.Severity + "/10   ·   Novelty " + s.Novelty + "/10   ·   Confidence " + s.Confidence + "%   ·   lead " + s.Id, 11.5, Blue, FontWeights.SemiBold));
            WrapPanel actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) };
            actions.Children.Add(Btn("MARK REVIEWED", delegate { MarkReviewed(ctx, s.Id); }));
            actions.Children.Add(Btn("CREATE CASE FILE", delegate { CreateCaseFromSignal(w, ctx, s); }));
            if (!String.IsNullOrWhiteSpace(s.PrimaryPath)) actions.Children.Add(Btn("OPEN PRIMARY EVIDENCE", delegate { DeepSystems.OpenExternalFile(w, ctx, s.PrimaryPath, "ATLAS › RESEARCH › " + s.Title + " › PRIMARY"); }));
            if (!String.IsNullOrWhiteSpace(s.IdentityValue)) actions.Children.Add(Btn("TRACE IDENTITY", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, s.IdentityValue, "ATLAS › RESEARCH › " + s.Title + " › TRACE"); }));
            actions.Children.Add(Btn("CAUSAL NEIGHBORHOOD", delegate { if (!String.IsNullOrWhiteSpace(s.IdentityKey) && !String.IsNullOrWhiteSpace(s.IdentityValue)) CausalGraphLab.OpenIdentityToken(w, ctx, s.IdentityKey, s.IdentityValue); else CausalGraphLab.Open(w, ctx); }));
            top.Children.Add(actions); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; StackPanel body = new StackPanel(); sv.Content = body;
            body.Children.Add(Card(T("EPISTEMIC CONTRACT\n" + (s.Epistemic == "OBSERVED" ? "This lead is directly present or mechanically checkable in bounded local evidence." : "This lead is mechanically inferred from observed relationships. It is not causal proof."), 12.5, Muted, FontWeights.Normal)));
            foreach (string p in s.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase).Take(24))
            {
                string captured = p; Border c = Card(T(Path.GetFileName(p) + "\n" + p, 12.5, Text, FontWeights.Normal)); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalFile(w, ctx, captured, "ATLAS › RESEARCH › " + s.Title + " › EVIDENCE"); }; body.Children.Add(c);
            }
            if (s.Evidence.Count > 0)
            {
                Dictionary<string, object> map = new Dictionary<string, object>(s.Evidence, StringComparer.OrdinalIgnoreCase); map["lead_id"] = s.Id; map["epistemic"] = s.Epistemic; map["severity"] = s.Severity; map["novelty"] = s.Novelty; map["confidence"] = s.Confidence;
                Border c = Card(T("STRUCTURED LEAD RECORD\nOpen the machine-readable evidence dossier", 12.5, Blue, FontWeights.SemiBold)); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalValue(w, ctx, s.Title + " // LEAD RECORD", map, "ATLAS › RESEARCH › " + s.Title + " › RECORD"); }; body.Children.Add(c);
            }
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenHypothesisList(Window owner, DeepSystemsContext ctx, ResearchUniverse u)
        {
            Window w = W(owner, "YOMI · HYPOTHESIS FORGE", 1100, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("HYPOTHESIS FORGE", 28, Text, FontWeights.Bold)); h.Children.Add(T("Ranked explanations · useful questions, not authority", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ListBox list = new ListBox { Margin = new Thickness(0, 14, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (ResearchHypothesis x in u.Hypotheses.OrderByDescending(x => x.Score)) list.Items.Add(x);
            list.MouseDoubleClick += delegate { ResearchHypothesis x = list.SelectedItem as ResearchHypothesis; if (x != null) OpenHypothesisDossier(w, ctx, u, x); };
            root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenHypothesisDossier(Window owner, DeepSystemsContext ctx, ResearchUniverse u, ResearchHypothesis h)
        {
            Window w = W(owner, "YOMI · HYPOTHESIS · " + h.Title, 970, 720);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel top = new StackPanel(); top.Children.Add(T("HYPOTHESIS · SCORE " + h.Score.ToString(CultureInfo.InvariantCulture), 11, Amber, FontWeights.Bold)); top.Children.Add(T(h.Title, 27, Text, FontWeights.Bold)); top.Children.Add(T(h.Proposition, 13, Muted, FontWeights.Normal));
            WrapPanel a = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) }; a.Children.Add(Btn("CREATE CASE FILE", delegate { CreateCaseFromHypothesis(w, ctx, h); })); if (!String.IsNullOrWhiteSpace(h.IdentityValue)) a.Children.Add(Btn("TRACE IDENTITY", delegate { DeepSystems.OpenExternalEvidenceSearch(w, ctx, h.IdentityValue, "ATLAS › RESEARCH › HYPOTHESIS › TRACE"); })); top.Children.Add(a); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; StackPanel body = new StackPanel(); sv.Content = body;
            body.Children.Add(Card(T("THIS IS A HYPOTHESIS\nThe score ranks investigative usefulness from currently bounded evidence. It does not convert correlation into causal proof.", 12.5, Amber, FontWeights.SemiBold)));
            foreach (string sid in h.SignalIds)
            {
                ResearchSignal s = u.Signals.FirstOrDefault(x => x.Id == sid); if (s == null) continue; ResearchSignal captured = s; Border c = Card(T(s.ToString() + "\n" + s.Summary, 12, Text, FontWeights.Normal)); c.Cursor = Cursors.Hand; c.MouseLeftButtonUp += delegate { OpenSignalDossier(w, ctx, u, captured); }; body.Children.Add(c);
            }
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void OpenTopology(Window owner, DeepSystemsContext ctx, ResearchUniverse u)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            map["scanned_utc"] = u.ScannedUtc.ToString("o", CultureInfo.InvariantCulture);
            map["artifact_count"] = u.Artifacts.Count;
            map["identity_token_count"] = u.ByToken.Count;
            Dictionary<string, object> cats = new Dictionary<string, object>();
            foreach (IGrouping<string, ResearchArtifact> g in u.Artifacts.GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase).OrderByDescending(x => x.Count()))
                cats[g.Key] = new Dictionary<string, object> { { "artifacts", g.Count() }, { "errors", g.Sum(x => x.ErrorCount) }, { "warnings", g.Sum(x => x.WarningCount) }, { "bytes", g.Sum(x => x.Bytes) } };
            map["subsystem_categories"] = cats;
            List<object> bridges = new List<object>();
            foreach (KeyValuePair<string, List<ResearchArtifact>> kv in u.ByToken.OrderByDescending(x => x.Value.Select(a => a.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count()).ThenBy(x => x.Key).Take(80))
            {
                string[] p = SplitToken(kv.Key); bridges.Add(new Dictionary<string, object> { { "identity_key", p[0] }, { "identity_value", p[1] }, { "artifact_count", kv.Value.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() }, { "categories", kv.Value.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray() }, { "paths", kv.Value.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToArray() } });
            }
            map["strongest_identity_bridges"] = bridges;
            map["isolated_artifacts"] = u.Artifacts.Where(x => x.Identity.Count == 0).Take(80).Select(x => new Dictionary<string, object> { { "path", x.Path }, { "category", x.Category }, { "bytes", x.Bytes }, { "modified_utc", x.ModifiedUtc.ToString("o", CultureInfo.InvariantCulture) } }).ToArray();
            DeepSystems.OpenExternalValue(owner, ctx, "SYSTEM CARTOGRAPHER", map, "ATLAS › RESEARCH › SYSTEM CARTOGRAPHER");
        }

        private static void OpenLensComposer(Window owner, DeepSystemsContext ctx, ResearchUniverse u)
        {
            Window w = W(owner, "YOMI · LENS COMPOSER", 1000, 720);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel top = new StackPanel(); top.Children.Add(T("LENS COMPOSER", 27, Text, FontWeights.Bold)); top.Children.Add(T("Search bounded artifacts by text/path/identity. Examples: source_key:abcd…, category:ORACLE, risk:URGENT, error, intent_id", 12.5, Muted, FontWeights.Normal));
            TextBox box = new TextBox { Margin = new Thickness(0, 12, 0, 8), MinHeight = 36, Padding = new Thickness(10, 7, 10, 7), Background = Raised, Foreground = Text, BorderBrush = Border };
            top.Children.Add(box); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            ListBox list = new ListBox { Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; root.Children.Add(list);
            Action run = delegate
            {
                list.Items.Clear(); string q = (box.Text ?? "").Trim(); if (q.Length == 0) return;
                foreach (ResearchArtifact a in ApplyLens(u, q).Take(120)) list.Items.Add(a);
            };
            box.TextChanged += delegate { run(); };
            list.MouseDoubleClick += delegate { ResearchArtifact a = list.SelectedItem as ResearchArtifact; if (a != null) DeepSystems.OpenExternalFile(w, ctx, a.Path, "ATLAS › RESEARCH › LENS › " + box.Text); };
            w.Content = root; w.Show(); box.Focus();
        }

        private static IEnumerable<ResearchArtifact> ApplyLens(ResearchUniverse u, string query)
        {
            string q = query ?? ""; string key = ""; string value = ""; int colon = q.IndexOf(':');
            if (colon > 0) { key = q.Substring(0, colon).Trim(); value = q.Substring(colon + 1).Trim(); }
            foreach (ResearchArtifact a in u.Artifacts)
            {
                bool match;
                if (key.Equals("category", StringComparison.OrdinalIgnoreCase)) match = a.Category.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
                else if (key.Length > 0 && SemanticKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) { string v = Value(a, key); match = v.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
                else match = a.Path.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || (a.Sample ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || a.Identity.Any(x => x.Key.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || x.Value.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match) yield return a;
            }
        }

        private static void OpenResearchSessions(Window owner, DeepSystemsContext ctx, ResearchUniverse u)
        {
            ResearchStore store = LoadStore(ctx);
            Window w = W(owner, "YOMI · RESEARCH SESSIONS", 1050, 720);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel h = new StackPanel(); h.Children.Add(T("RESEARCH SESSIONS", 27, Text, FontWeights.Bold)); h.Children.Add(T("Persistent controller-owned case files · max 48", 12.5, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ListBox list = new ListBox { Margin = new Thickness(0, 12, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            foreach (ResearchSessionRecord s in store.Sessions.OrderByDescending(x => x.UpdatedUtc)) list.Items.Add((s.Title ?? "case") + "   ·   " + (s.UpdatedUtc ?? s.CreatedUtc ?? "") + "   ·   " + (s.Id ?? ""));
            list.MouseDoubleClick += delegate { int i = list.SelectedIndex; if (i < 0) return; List<ResearchSessionRecord> ordered = store.Sessions.OrderByDescending(x => x.UpdatedUtc).ToList(); if (i < ordered.Count) OpenSessionDossier(w, ctx, ordered[i]); };
            root.Children.Add(list); w.Content = root; w.Show();
        }

        internal static void OpenSessionById(Window owner, DeepSystemsContext ctx, string caseId)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(caseId)) { Open(owner, ctx); return; }
            ResearchStore store = LoadStore(ctx);
            ResearchSessionRecord s = store.Sessions.FirstOrDefault(x => String.Equals(x.Id, caseId, StringComparison.OrdinalIgnoreCase));
            if (s == null) { OpenResearchSessions(owner, ctx, ScanUniverse(ctx)); return; }
            OpenSessionDossier(owner, ctx, s);
        }

        private static void OpenSessionDossier(Window owner, DeepSystemsContext ctx, ResearchSessionRecord s)
        {
            Window w = W(owner, "YOMI · RESEARCH CASE · " + (s.Title ?? s.Id), 1050, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("RESEARCH CASE FILE", 11, Accent, FontWeights.Bold));
            head.Children.Add(T(s.Title ?? "Untitled case", 28, Text, FontWeights.Bold));
            head.Children.Add(T(s.Question ?? "", 13, Muted, FontWeights.Normal));
            head.Children.Add(T((s.Id ?? "") + "   ·   " + (s.SeedKind ?? "") + "   ·   updated " + (s.UpdatedUtc ?? s.CreatedUtc ?? ""), 11.5, Blue, FontWeights.SemiBold));
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 14, 0, 0) };
            StackPanel body = new StackPanel(); sv.Content = body;

            WrapPanel nav = new WrapPanel();
            nav.Children.Add(Btn("RAW CASE RECORD", delegate { DeepSystems.OpenExternalValue(w, ctx, "RESEARCH CASE // RAW", SessionMap(s), "ATLAS › RESEARCH › CASES › " + s.Id + " › RAW"); }));
            nav.Children.Add(Btn("TEMPORAL CONTEXT", delegate { TemporalObservatory.Open(w, ctx); }));
            nav.Children.Add(Btn("CAUSAL GRAPH", delegate { CausalGraphLab.Open(w, ctx); }));
            nav.Children.Add(Btn("SYSTEM ATLAS", delegate { DeepSystems.OpenAtlasFromContext(w); }));
            nav.Children.Add(Btn("PROMOTE TO MISSION", delegate { ProductivityFabric.OpenForResearchCase(w, ctx, s.Id, s.Title, s.Question); }));
            body.Children.Add(nav);

            StackPanel hypothesis = new StackPanel(); hypothesis.Children.Add(T("WORKING HYPOTHESIS", 11, Amber, FontWeights.Bold)); hypothesis.Children.Add(T(s.WorkingHypothesis ?? "Unresolved", 13, Text, FontWeights.Normal));
            Border hc = Card(hypothesis); hc.Cursor = Cursors.Hand; hc.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalValue(w, ctx, "WORKING HYPOTHESIS", new Dictionary<string, object> { { "case_id", s.Id }, { "question", s.Question }, { "hypothesis", s.WorkingHypothesis }, { "epistemic_status", "HYPOTHESIS" } }, "ATLAS › RESEARCH › CASES › " + s.Id + " › HYPOTHESIS"); }; body.Children.Add(hc);

            StackPanel ev = new StackPanel(); ev.Children.Add(T("EVIDENCE CABINET · " + s.EvidencePaths.Count.ToString(CultureInfo.InvariantCulture), 11, Accent, FontWeights.Bold));
            foreach (string pth in s.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase).Take(40))
            {
                string captured = pth; TextBlock row = T(Path.GetFileName(pth) + "   ·   " + pth, 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 5, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { DeepSystems.OpenExternalFile(w, ctx, captured, "ATLAS › RESEARCH › CASES › " + s.Id + " › EVIDENCE"); }; ev.Children.Add(row);
            }
            body.Children.Add(Card(ev));

            StackPanel ids = new StackPanel(); ids.Children.Add(T("IDENTITY WORMHOLES · " + s.IdentityTokens.Count.ToString(CultureInfo.InvariantCulture), 11, Blue, FontWeights.Bold));
            foreach (string tok in s.IdentityTokens.Distinct(StringComparer.OrdinalIgnoreCase).Take(40))
            {
                string[] parts = SplitToken(tok); string ck = parts[0], cv = parts[1]; TextBlock row = T(tok, 12, Text, FontWeights.Normal); row.Margin = new Thickness(0, 5, 0, 0); row.Cursor = Cursors.Hand; row.MouseLeftButtonUp += delegate { if (!String.IsNullOrWhiteSpace(ck) && !String.IsNullOrWhiteSpace(cv)) CausalGraphLab.OpenIdentityToken(w, ctx, ck, cv); else DeepSystems.OpenExternalEvidenceSearch(w, ctx, cv, "ATLAS › RESEARCH › CASES › " + s.Id + " › IDENTITY"); }; ids.Children.Add(row);
            }
            body.Children.Add(Card(ids));

            StackPanel notes = new StackPanel(); notes.Children.Add(T("CASE NOTES", 11, Accent, FontWeights.Bold));
            foreach (string note in s.Notes.Take(30)) { TextBlock n = T("• " + note, 12.5, Text, FontWeights.Normal); n.Margin = new Thickness(0, 4, 0, 0); notes.Children.Add(n); }
            TextBox noteBox = new TextBox { Margin = new Thickness(0, 10, 0, 6), MinHeight = 34, Padding = new Thickness(9, 6, 9, 6), Background = Raised, Foreground = Text, BorderBrush = Border };
            notes.Children.Add(noteBox);
            notes.Children.Add(Btn("ADD NOTE", delegate
            {
                string note = (noteBox.Text ?? "").Trim(); if (note.Length == 0) return; s.Notes.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " · " + note); if (s.Notes.Count > 128) s.Notes = s.Notes.Skip(s.Notes.Count - 128).ToList(); s.UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); SaveSession(ctx, s); w.Close(); OpenSessionDossier(owner, ctx, s);
            }));
            body.Children.Add(Card(notes));

            body.Children.Add(Card(T("CASE FILE CONTRACT\nThis file is controller-owned research metadata. Notes, hypotheses and evidence links can never become queue/session/playback authority. A case can point at truth; it cannot manufacture truth.", 12.5, Muted, FontWeights.Normal)));
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static Dictionary<string, object> SessionMap(ResearchSessionRecord s)
        {
            Dictionary<string, object> map = new Dictionary<string, object>(); map["id"] = s.Id; map["created_utc"] = s.CreatedUtc; map["updated_utc"] = s.UpdatedUtc; map["title"] = s.Title; map["seed_kind"] = s.SeedKind; map["seed_fingerprint"] = s.SeedFingerprint; map["question"] = s.Question; map["working_hypothesis"] = s.WorkingHypothesis; map["evidence_paths"] = s.EvidencePaths.ToArray(); map["identity_tokens"] = s.IdentityTokens.ToArray(); map["notes"] = s.Notes.ToArray(); return map;
        }

        private static void CreateCaseFromSignal(Window owner, DeepSystemsContext ctx, ResearchSignal s)
        {
            ResearchSessionRecord r = new ResearchSessionRecord(); r.Id = "case-" + ShortHash(Guid.NewGuid().ToString("N")); r.CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); r.UpdatedUtc = r.CreatedUtc; r.Title = s.Title; r.SeedKind = s.Epistemic + "_LEAD"; r.SeedFingerprint = s.Id; r.Question = "What best explains: " + s.Summary; r.WorkingHypothesis = "Unresolved — follow evidence before promoting an explanation."; r.EvidencePaths.AddRange(s.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase)); if (!String.IsNullOrWhiteSpace(s.IdentityValue)) r.IdentityTokens.Add(Token(s.IdentityKey, s.IdentityValue)); SaveSession(ctx, r); OpenSessionDossier(owner, ctx, r);
        }

        private static void CreateCaseFromHypothesis(Window owner, DeepSystemsContext ctx, ResearchHypothesis h)
        {
            ResearchSessionRecord r = new ResearchSessionRecord(); r.Id = "case-" + ShortHash(Guid.NewGuid().ToString("N")); r.CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); r.UpdatedUtc = r.CreatedUtc; r.Title = h.Title; r.SeedKind = "HYPOTHESIS"; r.SeedFingerprint = h.Id; r.Question = "What evidence would support or falsify this hypothesis?"; r.WorkingHypothesis = h.Proposition; r.EvidencePaths.AddRange(h.EvidencePaths.Distinct(StringComparer.OrdinalIgnoreCase)); if (!String.IsNullOrWhiteSpace(h.IdentityValue)) r.IdentityTokens.Add(Token(h.IdentityKey, h.IdentityValue)); SaveSession(ctx, r); OpenSessionDossier(owner, ctx, r);
        }

        private static void SaveSession(DeepSystemsContext ctx, ResearchSessionRecord r)
        {
            lock (Gate)
            {
                ResearchStore store = LoadStore(ctx); store.Sessions.RemoveAll(x => String.Equals(x.Id, r.Id, StringComparison.OrdinalIgnoreCase)); store.Sessions.Add(r); store.Sessions = store.Sessions.OrderByDescending(x => x.UpdatedUtc).Take(48).ToList(); SaveStore(ctx, store);
            }
        }

        private static void MarkReviewed(DeepSystemsContext ctx, string id)
        {
            if (String.IsNullOrWhiteSpace(id)) return;
            lock (Gate)
            {
                ResearchStore store = LoadStore(ctx); if (!store.ReviewedLeadFingerprints.Contains(id, StringComparer.OrdinalIgnoreCase)) store.ReviewedLeadFingerprints.Add(id); if (store.ReviewedLeadFingerprints.Count > 256) store.ReviewedLeadFingerprints = store.ReviewedLeadFingerprints.Skip(store.ReviewedLeadFingerprints.Count - 256).ToList(); SaveStore(ctx, store);
            }
        }

        private static ResearchStore LoadStore(DeepSystemsContext ctx)
        {
            ResearchStore store = new ResearchStore(); string path = StorePath(ctx); if (!File.Exists(path)) return store;
            try
            {
                object raw = Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)); Dictionary<string, object> m = raw as Dictionary<string, object>; if (m == null) return store;
                object sessionsRaw; if (m.TryGetValue("sessions", out sessionsRaw))
                {
                    foreach (object item in AsList(sessionsRaw))
                    {
                        Dictionary<string, object> x = item as Dictionary<string, object>; if (x == null) continue; ResearchSessionRecord r = new ResearchSessionRecord(); r.Id = S(x, "id"); r.CreatedUtc = S(x, "created_utc"); r.UpdatedUtc = S(x, "updated_utc"); r.Title = S(x, "title"); r.SeedKind = S(x, "seed_kind"); r.SeedFingerprint = S(x, "seed_fingerprint"); r.Question = S(x, "question"); r.WorkingHypothesis = S(x, "working_hypothesis"); r.EvidencePaths = Strings(x, "evidence_paths"); r.IdentityTokens = Strings(x, "identity_tokens"); r.Notes = Strings(x, "notes"); if (!String.IsNullOrWhiteSpace(r.Id)) store.Sessions.Add(r);
                    }
                }
                object rev; if (m.TryGetValue("reviewed_lead_fingerprints", out rev)) store.ReviewedLeadFingerprints = AsList(rev).Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)).Where(x => !String.IsNullOrWhiteSpace(x)).Take(256).ToList();
            }
            catch { }
            return store;
        }

        private static void SaveStore(DeepSystemsContext ctx, ResearchStore store)
        {
            try
            {
                Directory.CreateDirectory(ctx.StateRoot); Dictionary<string, object> root = new Dictionary<string, object>(); root["schema"] = 1; root["updated_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); root["reviewed_lead_fingerprints"] = store.ReviewedLeadFingerprints.Take(256).ToArray(); List<object> sessions = new List<object>();
                foreach (ResearchSessionRecord r in store.Sessions.Take(48)) sessions.Add(new Dictionary<string, object> { { "id", r.Id }, { "created_utc", r.CreatedUtc }, { "updated_utc", r.UpdatedUtc }, { "title", r.Title }, { "seed_kind", r.SeedKind }, { "seed_fingerprint", r.SeedFingerprint }, { "question", r.Question }, { "working_hypothesis", r.WorkingHypothesis }, { "evidence_paths", r.EvidencePaths.Take(64).ToArray() }, { "identity_tokens", r.IdentityTokens.Take(64).ToArray() }, { "notes", r.Notes.Take(128).ToArray() } }); root["sessions"] = sessions;
                AtomicWrite(StorePath(ctx), Json.Serialize(root));
            }
            catch { }
        }

        private static void AtomicWrite(string path, string text)
        {
            string tmp = path + ".tmp." + ProcessId().ToString(CultureInfo.InvariantCulture); File.WriteAllText(tmp, text ?? "{}", new UTF8Encoding(false)); if (File.Exists(path)) File.Replace(tmp, path, null); else File.Move(tmp, path);
        }

        private static int ProcessId() { try { return System.Diagnostics.Process.GetCurrentProcess().Id; } catch { return 0; } }

        private static ResearchArtifact FindArtifact(ResearchUniverse u, string fileName) { return u.Artifacts.FirstOrDefault(x => String.Equals(Path.GetFileName(x.Path), fileName, StringComparison.OrdinalIgnoreCase)); }
        private static string Value(ResearchArtifact a, string key) { if (a == null || a.Identity == null) return ""; string v; return a.Identity.TryGetValue(key, out v) ? v ?? "" : ""; }
        private static int ParseIntIdentity(ResearchArtifact a, string key) { string v = Value(a, key); int n; if (Int32.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n; Match m = Regex.Match(a == null ? "" : a.Sample ?? "", "[\\\"]?" + Regex.Escape(key) + "[\\\"]?\\s*:\\s*(-?[0-9]+)", RegexOptions.IgnoreCase); return m.Success && Int32.TryParse(m.Groups[1].Value, out n) ? n : -1; }
        private static string Token(string key, string value) { return (key ?? "").Trim().ToLowerInvariant() + "=" + (value ?? "").Trim(); }
        private static string[] SplitToken(string token) { int i = (token ?? "").IndexOf('='); return i < 0 ? new string[] { token ?? "", "" } : new string[] { token.Substring(0, i), token.Substring(i + 1) }; }
        private static string ShortHash(string text) { using (SHA256 h = SHA256.Create()) { byte[] b = h.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")); return BitConverter.ToString(b, 0, 6).Replace("-", "").ToLowerInvariant(); } }
        private static IList<object> AsList(object raw) { object[] arr = raw as object[]; if (arr != null) return arr.ToList(); ArrayList al = raw as ArrayList; if (al != null) return al.Cast<object>().ToList(); return new List<object>(); }
        private static string S(Dictionary<string, object> m, string key) { object v; return m != null && m.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : ""; }
        private static List<string> Strings(Dictionary<string, object> m, string key) { object raw; if (m == null || !m.TryGetValue(key, out raw)) return new List<string>(); return AsList(raw).Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)).Where(x => !String.IsNullOrWhiteSpace(x)).ToList(); }
    }
}

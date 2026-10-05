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

namespace Yomi.ProductShell
{
    internal sealed class AbyssDescentRecord
    {
        public string Id;
        public string ParentId;
        public string RootId;
        public string Title;
        public string SeedKind;
        public string SeedValue;
        public string SourcePath;
        public string JsonPath;
        public string Fingerprint;
        public string RouteFingerprint;
        public int Depth;
        public string Zone;
        public string StartedUtc;
        public string UpdatedUtc;
        public string LastOpenedUtc;
        public string Status;
        public long DwellSeconds;
        public int OpenCount;
        public string ResumeSummary;
        public string ResumeNext;
        public List<string> IdentityTokens = new List<string>();
        public List<string> VisitedExitIds = new List<string>();
        public List<string> Notes = new List<string>();
    }

    internal sealed class AbyssFossil
    {
        public string Id;
        public string DescentId;
        public string Title;
        public string SourcePath;
        public string JsonPath;
        public string CapturedUtc;
        public string Fingerprint;
        public string Preview;
        public List<string> IdentityTokens = new List<string>();
    }

    internal sealed class AbyssStore
    {
        public int Schema = 1;
        public string CurrentDescentId;
        public List<AbyssDescentRecord> Descents = new List<AbyssDescentRecord>();
        public List<AbyssFossil> Fossils = new List<AbyssFossil>();
    }

    internal sealed class AbyssArtifact
    {
        public string Path;
        public long Bytes;
        public DateTime ModifiedUtc;
        public string Kind;
        public string Text;
    }

    internal sealed class AbyssMatch
    {
        public string Path;
        public string Kind;
        public string Token;
        public string Sample;
        public DateTime ModifiedUtc;
        public override string ToString()
        {
            return (Kind ?? "EVIDENCE") + "   " + System.IO.Path.GetFileName(Path ?? "") + "   ·   " + ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "   ·   " + (Sample ?? "");
        }
    }

    internal sealed class AbyssGap
    {
        public string Kind;
        public string Id;
        public string Summary;
        public string Detail;
        public string EvidencePath;
        public int Severity;
        public override string ToString()
        {
            return "S" + Severity.ToString(CultureInfo.InvariantCulture) + "   " + (Kind ?? "GAP") + "   " + (Summary ?? "");
        }
    }

    internal sealed class AbyssDivergence
    {
        public string EntityKind;
        public string EntityId;
        public string Attribute;
        public List<string> Values = new List<string>();
        public List<string> Paths = new List<string>();
        public override string ToString()
        {
            return (EntityKind ?? "ENTITY") + " " + (EntityId ?? "") + "   ·   " + (Attribute ?? "field") + "   ·   " + Values.Count.ToString(CultureInfo.InvariantCulture) + " observed values";
        }
    }

    internal static class AbyssalKnowledgeEngine
    {
        private static readonly Brush Bg = B("#03070B");
        private static readonly Brush Surface = B("#09121A");
        private static readonly Brush Raised = B("#101E29");
        private static readonly Brush Border = B("#294254");
        private static readonly Brush Text = B("#EFF9FF");
        private static readonly Brush Muted = B("#8FA9B8");
        private static readonly Brush Accent = B("#72F1C4");
        private static readonly Brush Blue = B("#62B7FF");
        private static readonly Brush Amber = B("#FFD26F");
        private static readonly Brush Violet = B("#C5A3FF");
        private static readonly Brush Danger = B("#FF7D8C");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();

        private static readonly string[] IdentityKeys = new string[]
        {
            "intent_id","session_id","source_key","occurrence_id","order_revision","plan_fingerprint","entry_hash","prev_hash","transaction_id","work_generation","failure_domain","decision_reason","route_label","mission_id","task_id","research_case_id","branch_id","parent_branch_id","root_branch_id"
        };

        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-abyssal-depth.json"); }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            OpenHub(owner, ctx);
        }

        public static void Open(Window owner, string dataRoot, string installRoot, string appDir)
        {
            Open(owner, new DeepSystemsContext(dataRoot, installRoot, appDir));
        }

        internal static void DescendFromExternal(Window owner, DeepSystemsContext ctx, string title, object value, string sourcePath, string jsonPath, string route)
        {
            if (ctx == null) return; string seed = String.IsNullOrWhiteSpace(title) ? "Deep Systems object" : title; if (!String.IsNullOrWhiteSpace(route)) seed += " · " + route; StartDescent(owner, ctx, null, "DEEP_OBJECT", seed, sourcePath ?? "", String.IsNullOrWhiteSpace(jsonPath) ? "$" : jsonPath, value);
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx)
        {
            AbyssStore store = Load(ctx);
            Window w = W(owner, "YOMI · HADAL SYSTEMS", 1220, 850);
            DockPanel root = new DockPanel { Margin = new Thickness(20) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("HADAL SYSTEMS / ABYSSAL KNOWLEDGE ENGINE", 30, Text, FontWeights.Bold));
            head.Children.Add(T("Recursive operational archaeology · provenance · divergence · null space · consequence horizons · resumable descents", 13, Muted, FontWeights.Normal));
            TextBlock law = T("Depth is earned by new information. Repetition is not depth. Organizational metadata never becomes evidence or engine authority.", 11.5, Amber, FontWeights.SemiBold); law.Margin = new Thickness(0, 7, 0, 14); head.Children.Add(law);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel p = new WrapPanel(); sv.Content = p;
            Portal(p, "↓", "START HADAL DESCENT", "Choose an identity, path, question or arbitrary token and descend recursively through every relationship YOMI can mechanically prove around it.", delegate { PromptNewDescent(w, ctx, null); });
            Portal(p, "RESUME", "DESCENT CONTINUITY VAULT", "Reopen deep branches with route fingerprints, ancestry, identity encounters, unexplored exits, notes and next-descent capsules.", delegate { OpenContinuityVault(w, ctx); });
            Portal(p, "SPINE", "PROVENANCE SPINE", "Build a cross-system lineage from identity-bearing state, journals, queue evidence, productivity records and research artifacts.", delegate { PromptTrace(w, ctx, "PROVENANCE SPINE"); });
            Portal(p, "≠", "DIVERGENCE CHAMBER", "Find stable-identity fields whose observed values disagree across bounded evidence. Divergence is surfaced; causation is not invented.", delegate { OpenDivergenceChamber(w, ctx); });
            Portal(p, "∅", "NULL-SPACE OBSERVATORY", "Missing dependencies, orphaned parents, absent research links, broken branch genealogy and impossible organizational references.", delegate { OpenNullSpace(w, ctx); });
            Portal(p, "→∞", "CONSEQUENCE HORIZON", "Ask where an identity or value appears downstream and open every matching artifact as a new investigative branch.", delegate { PromptTrace(w, ctx, "CONSEQUENCE HORIZON"); });
            Portal(p, "?", "QUESTION HORIZON", "Mechanically derive unresolved questions from gaps, divergent observations, indeterminate intents, stale work and unsupported links.", delegate { OpenQuestionHorizon(w, ctx); });
            Portal(p, "FOSSIL", "SEDIMENT / FOSSIL ARCHIVE", "Immutable-ish controller-owned captures of interesting deep objects with fingerprints, identity tokens and origin routes.", delegate { OpenFossilArchive(w, ctx); });
            Portal(p, "XSEC", "SYSTEM CROSS-SECTION", "A bounded cross-section of the currently discoverable evidence universe: artifacts, sizes, freshness, identity density and gaps.", delegate { OpenSystemCrossSection(w, ctx); });
            Portal(p, "PRESSURE", "DEPTH PRESSURE PROFILE", "Measure branch complexity from evidence fan-out, identity density, divergence, unresolved exits and null-space observations.", delegate { OpenPressureProfile(w, ctx); });
            Portal(p, "BLACK BOX", "ABYSSAL FLIGHT RECORDER", "Verify and descend through the append-only SHA-256 record of how the exploration world itself changed.", delegate { OpenAbyssalLedger(w, ctx); });
            Portal(p, "WORK", "COGNITIVE PRODUCTIVITY FABRIC", "Turn a descent into durable work when the investigation needs tasks, dependencies, focus continuity and review.", delegate { ProductivityFabric.Open(w, ctx); });
            Portal(p, "R&D", "RESEARCH WORKBENCH", "Move from a deep object into explicit observations, inferences and hypotheses.", delegate { ResearchWorkbench.Open(w, ctx); });
            Portal(p, "GRAPH", "CAUSAL GRAPH LAB", "Follow identity topology and evidence neighborhoods without promoting correlation into causation.", delegate { CausalGraphLab.Open(w, ctx); });
            Portal(p, "ΔT", "TEMPORAL OBSERVATORY", "Descend into chronology, historical evidence frames and counterfactual branches.", delegate { TemporalObservatory.Open(w, ctx); });
            Portal(p, "ATLAS", "DEEP SYSTEMS ATLAS", "Return to the larger system world and enter another subsystem from a different direction.", delegate { DeepSystems.OpenAtlasFromContext(w); });

            TextBlock status = T(store.Descents.Count.ToString(CultureInfo.InvariantCulture) + " persisted descents · " + store.Fossils.Count.ToString(CultureInfo.InvariantCulture) + " fossils · deepest recorded depth " + (store.Descents.Count == 0 ? "0" : store.Descents.Max(x => x.Depth).ToString(CultureInfo.InvariantCulture)), 11.5, Blue, FontWeights.SemiBold);
            status.Margin = new Thickness(0, 10, 0, 0); DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
            root.Children.Add(sv); w.Content = root; w.Show();
        }

        private static void PromptNewDescent(Window owner, DeepSystemsContext ctx, AbyssDescentRecord parent)
        {
            string seed = Prompt(owner, "START DESCENT", "Identity, path, question or value to descend around", parent == null ? "" : (parent.SeedValue ?? ""));
            if (String.IsNullOrWhiteSpace(seed)) return;
            StartDescent(owner, ctx, parent, "TOKEN", seed.Trim(), "", "$", null);
        }

        private static void PromptTrace(Window owner, DeepSystemsContext ctx, string title)
        {
            string seed = Prompt(owner, title, "Exact identity/value/token", "");
            if (String.IsNullOrWhiteSpace(seed)) return;
            List<AbyssMatch> matches = FindMatches(ctx, seed.Trim(), 128);
            OpenMatchWorld(owner, ctx, title, seed.Trim(), matches, null);
        }

        private static void StartDescent(Window owner, DeepSystemsContext ctx, AbyssDescentRecord parent, string seedKind, string seedValue, string sourcePath, string jsonPath, object value)
        {
            AbyssStore store = Load(ctx);
            string valueText = value == null ? (seedValue ?? "") : SafeSerialize(value);
            string fp = Hash((seedKind ?? "") + "\n" + (seedValue ?? "") + "\n" + (sourcePath ?? "") + "\n" + (jsonPath ?? "$"));
            if (parent != null)
            {
                AbyssDescentRecord loop = Ancestors(store, parent).FirstOrDefault(x => String.Equals(x.Fingerprint, fp, StringComparison.OrdinalIgnoreCase));
                if (loop != null)
                {
                    OpenLoopWitness(owner, ctx, parent, loop, seedValue); return;
                }
            }
            int depth = parent == null ? 1 : parent.Depth + 1;
            string now = Now();
            AbyssDescentRecord d = new AbyssDescentRecord
            {
                Id = Id("descent"), ParentId = parent == null ? "" : parent.Id, RootId = parent == null ? "" : parent.RootId,
                Title = Trunc(String.IsNullOrWhiteSpace(seedValue) ? "Unnamed descent" : seedValue, 120), SeedKind = seedKind ?? "TOKEN", SeedValue = seedValue ?? "",
                SourcePath = sourcePath ?? "", JsonPath = String.IsNullOrWhiteSpace(jsonPath) ? "$" : jsonPath, Fingerprint = fp, Depth = depth,
                Zone = Zone(depth), StartedUtc = now, UpdatedUtc = now, LastOpenedUtc = now, Status = "ACTIVE"
            };
            if (parent == null) d.RootId = d.Id;
            d.IdentityTokens = ExtractIdentityTokens(value == null ? (object)seedValue : value).Take(64).ToList();
            if (d.IdentityTokens.Count == 0 && !String.IsNullOrWhiteSpace(seedValue)) d.IdentityTokens.Add("token=" + seedValue.Trim());
            d.RouteFingerprint = Hash((parent == null ? "ROOT" : parent.RouteFingerprint) + "\n" + fp + "\n" + depth.ToString(CultureInfo.InvariantCulture));
            store.Descents.Add(d); TrimStore(store); store.CurrentDescentId = d.Id; Save(ctx, store);
            Ledger(ctx, "DESCENT_START", d, "parent=" + d.ParentId + " depth=" + d.Depth.ToString(CultureInfo.InvariantCulture));
            OpenDescent(owner, ctx, d.Id, valueText);
        }

        private static void OpenDescent(Window owner, DeepSystemsContext ctx, string descentId, string explicitValue)
        {
            AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == descentId); if (d == null) return;
            d.LastOpenedUtc = Now(); d.UpdatedUtc = d.LastOpenedUtc; d.OpenCount++; store.CurrentDescentId = d.Id; Save(ctx, store);
            List<AbyssMatch> matches = FindMatches(ctx, PrimaryToken(d), 96);
            List<AbyssGap> gaps = AnalyzeGaps(ctx).Where(g => RelevantGap(g, d)).Take(32).ToList();
            List<AbyssDivergence> divergences = AnalyzeDivergence(ctx).Where(x => RelevantDivergence(x, d)).Take(32).ToList();
            int pressure = Pressure(d, matches.Count, gaps.Count, divergences.Count);
            int exits = Math.Max(0, 10 - d.VisitedExitIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            Window w = W(owner, "YOMI · HADAL DESCENT · D" + d.Depth.ToString(CultureInfo.InvariantCulture), 1260, 880);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("DEPTH " + d.Depth.ToString(CultureInfo.InvariantCulture) + " · " + d.Zone.ToUpperInvariant() + " · " + ApproxMeters(d.Depth).ToString("#,0", CultureInfo.InvariantCulture) + " m MODEL DEPTH", 11, Accent, FontWeights.Bold));
            head.Children.Add(T(d.Title, 28, Text, FontWeights.Bold));
            head.Children.Add(T("route " + Short(d.RouteFingerprint, 16) + " · object " + Short(d.Fingerprint, 16) + " · pressure " + pressure.ToString(CultureInfo.InvariantCulture) + " · " + exits.ToString(CultureInfo.InvariantCulture) + " modeled exits unexplored · visits " + d.OpenCount.ToString(CultureInfo.InvariantCulture) + " · dwell " + FormatDuration(d.DwellSeconds), 12, Muted, FontWeights.Normal));
            TextBlock boundary = T("OBSERVATION BOUNDARY: this descent reads evidence and controller-owned exploration metadata. It cannot mutate playback, queue authority, scheduler policy, Oracle or Aegis.", 11, Amber, FontWeights.SemiBold); boundary.Margin = new Thickness(0, 6, 0, 12); head.Children.Add(boundary);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; WrapPanel p = new WrapPanel(); sv.Content = p;
            Portal(p, "STRATUM", "CURRENT STRATUM", "Open the seed, source coordinates, identities, evidence fan-out and raw representation as another explorable object.", delegate { MarkExit(ctx, d.Id, "current"); OpenCurrentStratum(w, ctx, d, explicitValue); });
            Portal(p, "ID", "IDENTITY STRATA", d.IdentityTokens.Count.ToString(CultureInfo.InvariantCulture) + " semantic identities encountered. Any identity can become another descent.", delegate { MarkExit(ctx, d.Id, "identities"); OpenIdentityStrata(w, ctx, d); });
            Portal(p, "↑", "ANCESTRY SHAFT", "Walk parent descents all the way back toward the surface and inspect the exact route that produced this branch.", delegate { MarkExit(ctx, d.Id, "ancestry"); OpenAncestry(w, ctx, d); });
            Portal(p, "↓", "DESCENDANT TRENCHES", "Open every child investigation that has already grown beneath this one, or fork another sub-descent.", delegate { MarkExit(ctx, d.Id, "descendants"); OpenDescendants(w, ctx, d); });
            Portal(p, "≠", "DIVERGENCE PRESSURE", divergences.Count.ToString(CultureInfo.InvariantCulture) + " relevant divergence candidates around stable identities. Differences are observations, not accusations of corruption.", delegate { MarkExit(ctx, d.Id, "divergence"); OpenDivergenceList(w, ctx, d, divergences); });
            Portal(p, "∅", "NULL-SPACE POCKETS", gaps.Count.ToString(CultureInfo.InvariantCulture) + " relevant missing-link or impossible-reference observations around this branch.", delegate { MarkExit(ctx, d.Id, "nullspace"); OpenGapList(w, ctx, d, gaps); });
            Portal(p, "→∞", "CONSEQUENCE HORIZON", matches.Count.ToString(CultureInfo.InvariantCulture) + " bounded evidence matches for the primary identity/value. Every match can become another descent.", delegate { MarkExit(ctx, d.Id, "consequence"); OpenMatchWorld(w, ctx, "CONSEQUENCE HORIZON", PrimaryToken(d), matches, d); });
            Portal(p, "?", "UNANSWERED-QUESTION HORIZON", "Generate mechanically grounded questions from missing links, divergent observations, stale branches and unresolved authority states.", delegate { MarkExit(ctx, d.Id, "questions"); OpenQuestionsForDescent(w, ctx, d, gaps, divergences); });
            Portal(p, "FOSSIL", "FOSSILIZE THIS STRATUM", "Capture a bounded fingerprinted fossil with identity tokens and exact origin so the discovery can survive after live state changes.", delegate { MarkExit(ctx, d.Id, "fossil"); Fossilize(w, ctx, d, explicitValue); });
            Portal(p, "SUB", "SPAWN SUB-INVESTIGATION", "Open a new child descent around a token or question discovered here. Ancestor fingerprint loops are intercepted explicitly.", delegate { MarkExit(ctx, d.Id, "sub"); PromptNewDescent(w, ctx, d); });
            Portal(p, "WORK", "TURN INTO WORK", "Open the Productivity Fabric beside this descent so the investigation can become durable tasks without gaining evidence authority.", delegate { ProductivityFabric.Open(w, ctx); });
            Portal(p, "R&D", "TURN INTO RESEARCH CASE", "Open the Research Workbench to formalize observations, inferences and hypotheses around what you found.", delegate { ResearchWorkbench.Open(w, ctx); });
            Portal(p, "GRAPH", "CAUSAL NEIGHBORHOOD", "Enter the evidence graph from this depth and explore mechanically correlated identities and chains.", delegate { CausalGraphLab.Open(w, ctx); });
            Portal(p, "ΔT", "TEMPORAL SHAFT", "Follow this system through historical evidence and alternate structural worlds.", delegate { TemporalObservatory.Open(w, ctx); });
            Portal(p, "BLACK BOX", "DESCENT FLIGHT RECORDER", "Inspect the provenance chain generated by descent starts, fossils, notes, capsules and status changes.", delegate { OpenAbyssalLedger(w, ctx); });

            WrapPanel buttons = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            buttons.Children.Add(Btn("SAVE RESUME CAPSULE", delegate { SaveResumeCapsule(w, ctx, d.Id); }));
            buttons.Children.Add(Btn("ADD NOTE", delegate { AddNote(w, ctx, d.Id); }));
            buttons.Children.Add(Btn("MARK SATURATED", delegate { SetStatus(ctx, d.Id, "SATURATED"); w.Close(); OpenDescent(owner, ctx, d.Id, explicitValue); }));
            DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);
            root.Children.Add(sv); w.Content = root; DateTime openedUtc = DateTime.UtcNow; w.Closed += delegate { RecordDwell(ctx, d.Id, openedUtc); }; w.Show();
        }

        private static void OpenCurrentStratum(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d, string explicitValue)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            obj["descent_id"] = d.Id; obj["depth"] = d.Depth; obj["zone"] = d.Zone; obj["seed_kind"] = d.SeedKind; obj["seed_value"] = d.SeedValue; obj["source_path"] = d.SourcePath; obj["json_path"] = d.JsonPath; obj["fingerprint"] = d.Fingerprint; obj["route_fingerprint"] = d.RouteFingerprint; obj["identity_tokens"] = d.IdentityTokens.ToArray(); obj["explicit_value"] = explicitValue ?? "";
            DeepSystems.OpenExternalValue(owner, ctx, "HADAL CURRENT STRATUM", obj, "ATLAS › HADAL › D" + d.Depth.ToString(CultureInfo.InvariantCulture) + " › STRATUM");
        }

        private static void OpenIdentityStrata(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d)
        {
            Window w = W(owner, "YOMI · IDENTITY STRATA", 1060, 720); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("IDENTITY STRATA · DEPTH " + d.Depth.ToString(CultureInfo.InvariantCulture), 27, Text, FontWeights.Bold));
            root.Children.Add(T("Every recognized semantic identity is a possible wormhole into another system. Opening one starts a child descent.", 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (string token in d.IdentityTokens.Distinct(StringComparer.OrdinalIgnoreCase)) list.Items.Add(token); root.Children.Add(list);
            WrapPanel buttons = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            buttons.Children.Add(Btn("DESCEND INTO IDENTITY", delegate { string token = list.SelectedItem as string; if (!String.IsNullOrWhiteSpace(token)) StartDescent(w, ctx, d, "IDENTITY", token, "", "$", null); }));
            buttons.Children.Add(Btn("TRACE EVIDENCE", delegate { string token = list.SelectedItem as string; if (!String.IsNullOrWhiteSpace(token)) OpenMatchWorld(w, ctx, "IDENTITY EVIDENCE", token, FindMatches(ctx, TokenValue(token), 160), d); })); root.Children.Add(buttons); w.Content = root; w.Show();
        }

        private static void OpenAncestry(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d)
        {
            AbyssStore store = Load(ctx); List<AbyssDescentRecord> chain = Ancestors(store, d).Reverse<AbyssDescentRecord>().ToList(); chain.Add(d);
            Window w = W(owner, "YOMI · ANCESTRY SHAFT", 1080, 760); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("ANCESTRY SHAFT", 28, Text, FontWeights.Bold)); root.Children.Add(T("Surface-to-current lineage. Route fingerprints make the path itself an inspectable object.", 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssDescentRecord x in chain) list.Items.Add("D" + x.Depth.ToString(CultureInfo.InvariantCulture) + "   " + x.Zone.ToUpperInvariant() + "   " + x.Title + "   ·   " + Short(x.RouteFingerprint, 12)); root.Children.Add(list);
            Button open = Btn("OPEN SELECTED ANCESTOR", delegate { int i = list.SelectedIndex; if (i >= 0 && i < chain.Count) OpenDescent(w, ctx, chain[i].Id, null); }); root.Children.Add(open); w.Content = root; w.Show();
        }

        private static void OpenDescendants(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d)
        {
            AbyssStore store = Load(ctx); List<AbyssDescentRecord> children = store.Descents.Where(x => String.Equals(x.ParentId, d.Id, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.UpdatedUtc).ToList();
            Window w = W(owner, "YOMI · DESCENDANT TRENCHES", 1080, 760); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("DESCENDANT TRENCHES", 28, Text, FontWeights.Bold)); root.Children.Add(T(children.Count.ToString(CultureInfo.InvariantCulture) + " direct child descents beneath this stratum", 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssDescentRecord x in children) list.Items.Add("D" + x.Depth.ToString(CultureInfo.InvariantCulture) + "   " + x.Zone.ToUpperInvariant() + "   " + x.Status + "   " + x.Title); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN CHILD", delegate { int i = list.SelectedIndex; if (i >= 0 && i < children.Count) OpenDescent(w, ctx, children[i].Id, null); })); b.Children.Add(Btn("SPAWN NEW CHILD", delegate { PromptNewDescent(w, ctx, d); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenContinuityVault(Window owner, DeepSystemsContext ctx)
        {
            AbyssStore store = Load(ctx); List<AbyssDescentRecord> rows = store.Descents.OrderByDescending(x => x.Depth).ThenByDescending(x => x.UpdatedUtc).Take(256).ToList();
            Window w = W(owner, "YOMI · DESCENT CONTINUITY VAULT", 1160, 800); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("DESCENT CONTINUITY VAULT", 28, Text, FontWeights.Bold)); root.Children.Add(T("Persistent routes through the rabbit hole. Deepest branches sort first.", 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssDescentRecord d in rows) list.Items.Add("D" + d.Depth.ToString(CultureInfo.InvariantCulture) + "   " + d.Zone.ToUpperInvariant() + "   " + d.Status + "   " + d.Title + "   ·   " + (String.IsNullOrWhiteSpace(d.ResumeNext) ? "no capsule" : "resume: " + Trunc(d.ResumeNext, 72))); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("RESUME SELECTED", delegate { int i = list.SelectedIndex; if (i >= 0 && i < rows.Count) OpenDescent(w, ctx, rows[i].Id, null); })); b.Children.Add(Btn("OPEN RAW RECORD", delegate { int i = list.SelectedIndex; if (i >= 0 && i < rows.Count) DeepSystems.OpenExternalValue(w, ctx, "DESCENT RECORD", DescentMap(rows[i]), "ATLAS › HADAL › CONTINUITY › " + rows[i].Id); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenDivergenceChamber(Window owner, DeepSystemsContext ctx)
        {
            OpenDivergenceList(owner, ctx, null, AnalyzeDivergence(ctx));
        }

        private static void OpenDivergenceList(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d, List<AbyssDivergence> rows)
        {
            Window w = W(owner, "YOMI · DIVERGENCE CHAMBER", 1180, 820); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("DIVERGENCE CHAMBER", 28, Text, FontWeights.Bold));
            root.Children.Add(T("Same stable identity, different observed stable-field values. This is a divergence candidate, not automatically a defect: time, projection lag and legitimate evolution remain possible explanations.", 12, Amber, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssDivergence x in rows.Take(256)) list.Items.Add(x); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            b.Children.Add(Btn("OPEN EVIDENCE", delegate { AbyssDivergence x = list.SelectedItem as AbyssDivergence; if (x != null) DeepSystems.OpenExternalValue(w, ctx, "DIVERGENCE EVIDENCE", new { entity_kind = x.EntityKind, entity_id = x.EntityId, attribute = x.Attribute, values = x.Values.ToArray(), paths = x.Paths.ToArray(), epistemic_status = "OBSERVED_DIVERGENCE" }, "ATLAS › HADAL › DIVERGENCE"); }));
            b.Children.Add(Btn("DESCEND INTO ENTITY", delegate { AbyssDivergence x = list.SelectedItem as AbyssDivergence; if (x != null) StartDescent(w, ctx, d, "DIVERGENCE_ENTITY", x.EntityKind.ToLowerInvariant() + "=" + x.EntityId, "", "$", null); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenNullSpace(Window owner, DeepSystemsContext ctx)
        {
            OpenGapList(owner, ctx, null, AnalyzeGaps(ctx));
        }

        private static void OpenGapList(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d, List<AbyssGap> rows)
        {
            Window w = W(owner, "YOMI · NULL-SPACE OBSERVATORY", 1180, 820); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("NULL-SPACE OBSERVATORY", 28, Text, FontWeights.Bold)); root.Children.Add(T("The absence of an expected relationship is itself evidence about the organizational graph. Missing does not automatically mean broken; the exact witness remains inspectable.", 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssGap g in rows.OrderByDescending(x => x.Severity).Take(300)) list.Items.Add(g); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN GAP DOSSIER", delegate { AbyssGap g = list.SelectedItem as AbyssGap; if (g != null) DeepSystems.OpenExternalValue(w, ctx, "NULL-SPACE WITNESS", GapMap(g), "ATLAS › HADAL › NULL SPACE › " + g.Kind); })); b.Children.Add(Btn("DESCEND INTO GAP", delegate { AbyssGap g = list.SelectedItem as AbyssGap; if (g != null) StartDescent(w, ctx, d, "NULL_SPACE", g.Id + " " + g.Summary, g.EvidencePath, "$", GapMap(g)); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenQuestionHorizon(Window owner, DeepSystemsContext ctx)
        {
            OpenQuestionsForDescent(owner, ctx, null, AnalyzeGaps(ctx), AnalyzeDivergence(ctx));
        }

        private static void OpenQuestionsForDescent(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d, List<AbyssGap> gaps, List<AbyssDivergence> divergences)
        {
            List<Dictionary<string, object>> q = BuildQuestions(ctx, d, gaps, divergences);
            Window w = W(owner, "YOMI · QUESTION HORIZON", 1180, 820); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("UNANSWERED-QUESTION HORIZON", 28, Text, FontWeights.Bold)); root.Children.Add(T("Questions are mechanically generated from observed gaps, divergence, staleness or explicit unresolved state. They are invitations to investigate, not conclusions.", 12, Amber, FontWeights.Normal));
            ListBox list = List(); foreach (Dictionary<string, object> x in q) list.Items.Add(S(x, "question")); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN QUESTION EVIDENCE", delegate { int i = list.SelectedIndex; if (i >= 0 && i < q.Count) DeepSystems.OpenExternalValue(w, ctx, "QUESTION DOSSIER", q[i], "ATLAS › HADAL › QUESTION HORIZON"); })); b.Children.Add(Btn("DESCEND INTO QUESTION", delegate { int i = list.SelectedIndex; if (i >= 0 && i < q.Count) StartDescent(w, ctx, d, "QUESTION", S(q[i], "question"), S(q[i], "evidence_path"), "$", q[i]); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenMatchWorld(Window owner, DeepSystemsContext ctx, string title, string token, List<AbyssMatch> matches, AbyssDescentRecord parent)
        {
            Window w = W(owner, "YOMI · " + title, 1200, 820); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T(title, 28, Text, FontWeights.Bold)); root.Children.Add(T(matches.Count.ToString(CultureInfo.InvariantCulture) + " bounded exact textual evidence matches for: " + token, 12, Muted, FontWeights.Normal));
            ListBox list = List(); foreach (AbyssMatch m in matches) list.Items.Add(m); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN ARTIFACT", delegate { AbyssMatch m = list.SelectedItem as AbyssMatch; if (m != null) DeepSystems.OpenExternalFile(w, ctx, m.Path, "ATLAS › HADAL › EVIDENCE › " + Path.GetFileName(m.Path)); })); b.Children.Add(Btn("DESCEND INTO MATCH", delegate { AbyssMatch m = list.SelectedItem as AbyssMatch; if (m != null) StartDescent(w, ctx, parent, "EVIDENCE_MATCH", token, m.Path, "$", new { path = m.Path, sample = m.Sample, token = token }); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void OpenFossilArchive(Window owner, DeepSystemsContext ctx)
        {
            AbyssStore store = Load(ctx); List<AbyssFossil> rows = store.Fossils.OrderByDescending(x => x.CapturedUtc).ToList(); Window w = W(owner, "YOMI · FOSSIL ARCHIVE", 1120, 780); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("SEDIMENT / FOSSIL ARCHIVE", 28, Text, FontWeights.Bold)); root.Children.Add(T("Bounded controller-owned captures of discoveries. Fossils preserve what was seen; they do not claim the live system still has that value.", 12, Muted, FontWeights.Normal)); ListBox list = List(); foreach (AbyssFossil f in rows) list.Items.Add((f.CapturedUtc ?? "") + "   " + f.Title + "   ·   " + Short(f.Fingerprint, 12)); root.Children.Add(list);
            WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN FOSSIL", delegate { int i = list.SelectedIndex; if (i >= 0 && i < rows.Count) DeepSystems.OpenExternalValue(w, ctx, "ABYSSAL FOSSIL", FossilMap(rows[i]), "ATLAS › HADAL › FOSSIL › " + rows[i].Id); })); b.Children.Add(Btn("TRACE FOSSIL IDENTITIES", delegate { int i = list.SelectedIndex; if (i >= 0 && i < rows.Count && rows[i].IdentityTokens.Count > 0) OpenMatchWorld(w, ctx, "FOSSIL IDENTITY TRACE", TokenValue(rows[i].IdentityTokens[0]), FindMatches(ctx, TokenValue(rows[i].IdentityTokens[0]), 128), null); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static void Fossilize(Window owner, DeepSystemsContext ctx, AbyssDescentRecord d, string explicitValue)
        {
            AbyssStore store = Load(ctx); string preview = Trunc(explicitValue ?? d.SeedValue ?? "", 4096); AbyssFossil f = new AbyssFossil { Id = Id("fossil"), DescentId = d.Id, Title = d.Title, SourcePath = d.SourcePath, JsonPath = d.JsonPath, CapturedUtc = Now(), Fingerprint = Hash((d.Fingerprint ?? "") + "\n" + preview), Preview = preview, IdentityTokens = d.IdentityTokens.Take(64).ToList() }; store.Fossils.Add(f); TrimStore(store); Save(ctx, store); Ledger(ctx, "FOSSIL_CAPTURE", d, "fossil=" + f.Id); MessageBox.Show(owner, "Fossil captured: " + f.Id, "YOMI Hadal Systems", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void OpenSystemCrossSection(Window owner, DeepSystemsContext ctx)
        {
            List<AbyssArtifact> arts = ScanArtifacts(ctx); List<object> rows = arts.Select(a => (object)new { file = Path.GetFileName(a.Path), path = a.Path, kind = a.Kind, bytes = a.Bytes, modified_utc = a.ModifiedUtc.ToString("o", CultureInfo.InvariantCulture), identity_density = ExtractIdentityTokens(ParseBest(a)).Count }).ToList();
            DeepSystems.OpenExternalValue(owner, ctx, "HADAL SYSTEM CROSS-SECTION", new { scanned_utc = Now(), artifact_count = arts.Count, total_bytes = arts.Sum(x => x.Bytes), rows = rows.ToArray(), boundary = "Bounded passive scan; large/binary artifacts are not parsed as text." }, "ATLAS › HADAL › CROSS SECTION");
        }

        private static void OpenPressureProfile(Window owner, DeepSystemsContext ctx)
        {
            AbyssStore store = Load(ctx); List<AbyssGap> gaps = AnalyzeGaps(ctx); List<AbyssDivergence> div = AnalyzeDivergence(ctx); List<object> rows = new List<object>();
            foreach (AbyssDescentRecord d in store.Descents.OrderByDescending(x => x.Depth).Take(128)) { int matches = FindMatches(ctx, PrimaryToken(d), 64).Count; int dg = gaps.Count(x => RelevantGap(x, d)); int dv = div.Count(x => RelevantDivergence(x, d)); rows.Add(new { descent_id = d.Id, depth = d.Depth, zone = d.Zone, title = d.Title, evidence_matches = matches, gap_count = dg, divergence_count = dv, identity_count = d.IdentityTokens.Count, pressure = Pressure(d, matches, dg, dv), route_fingerprint = d.RouteFingerprint }); }
            DeepSystems.OpenExternalValue(owner, ctx, "DEPTH PRESSURE PROFILE", new { generated_utc = Now(), deepest = store.Descents.Count == 0 ? 0 : store.Descents.Max(x => x.Depth), descents = rows.ToArray(), interpretation = "Pressure is an exploration-complexity heuristic, not a health or risk probability." }, "ATLAS › HADAL › PRESSURE");
        }

        private static void OpenAbyssalLedger(Window owner, DeepSystemsContext ctx)
        {
            string path = Path.Combine(ctx.StateRoot, "controller-abyssal-ledger.jsonl"); List<Dictionary<string, object>> records = ReadAbyssLedger(path); string verdict = VerifyAbyssLedger(records); Window w = W(owner, "YOMI · ABYSSAL FLIGHT RECORDER", 1160, 820); StackPanel root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(T("ABYSSAL FLIGHT RECORDER", 28, Text, FontWeights.Bold)); root.Children.Add(T(verdict, 12.5, verdict.StartsWith("VALID", StringComparison.OrdinalIgnoreCase) ? Accent : Danger, FontWeights.SemiBold)); root.Children.Add(T("Bounded tail verification begins from the first loaded record's durable prev_hash anchor. It proves continuity inside the loaded window, not unobserved history before that anchor.", 11.5, Muted, FontWeights.Normal)); ListBox list = List(); foreach (Dictionary<string, object> r in records) list.Items.Add("#" + L(r, "seq", 0).ToString(CultureInfo.InvariantCulture) + "   " + S(r, "utc") + "   " + S(r, "action") + "   D" + Convert.ToString(r.ContainsKey("depth") ? r["depth"] : 0, CultureInfo.InvariantCulture) + "   " + S(r, "descent_id") + "   ·   " + Short(S(r, "entry_hash"), 12)); root.Children.Add(list); WrapPanel b = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) }; b.Children.Add(Btn("OPEN RECORD", delegate { int i = list.SelectedIndex; if (i >= 0 && i < records.Count) DeepSystems.OpenExternalValue(w, ctx, "ABYSSAL LEDGER RECORD", records[i], "ATLAS › HADAL › FLIGHT RECORDER › " + L(records[i], "seq", 0).ToString(CultureInfo.InvariantCulture)); })); b.Children.Add(Btn("OPEN RAW JOURNAL", delegate { DeepSystems.OpenExternalFile(w, ctx, path, "ATLAS › HADAL › FLIGHT RECORDER › RAW"); })); root.Children.Add(b); w.Content = root; w.Show();
        }

        private static List<Dictionary<string, object>> ReadAbyssLedger(string path)
        {
            List<Dictionary<string, object>> r = new List<Dictionary<string, object>>(); if (!File.Exists(path)) return r; foreach (string line in SplitLines(ReadBoundedText(path, 1024 * 1024)).TakeLastCompatAbyss(2048)) { if (String.IsNullOrWhiteSpace(line)) continue; try { Dictionary<string, object> m = Json.DeserializeObject(line) as Dictionary<string, object>; if (m != null) r.Add(m); } catch { } } return r.OrderBy(x => L(x, "seq", 0)).ToList();
        }

        private static string VerifyAbyssLedger(List<Dictionary<string, object>> records)
        {
            if (records == null || records.Count == 0) return "VALID · empty bounded ledger"; long expected = L(records[0], "seq", 0); string prev = S(records[0], "prev_hash"); if (expected <= 0 || String.IsNullOrWhiteSpace(prev)) return "INVALID · missing durable tail anchor"; foreach (Dictionary<string, object> r in records) { long seq = L(r, "seq", 0); if (seq != expected) return "INVALID · sequence discontinuity near #" + seq.ToString(CultureInfo.InvariantCulture); if (!String.Equals(S(r, "prev_hash"), prev, StringComparison.OrdinalIgnoreCase)) return "INVALID · ancestry mismatch near #" + seq.ToString(CultureInfo.InvariantCulture); string canonical = String.Join("\n", new string[] { seq.ToString(CultureInfo.InvariantCulture), S(r, "utc"), S(r, "action"), S(r, "descent_id"), Convert.ToString(r.ContainsKey("depth") ? r["depth"] : 0, CultureInfo.InvariantCulture), S(r, "route_fingerprint"), S(r, "detail"), S(r, "prev_hash") }); string h = Hash(canonical); if (!String.Equals(h, S(r, "entry_hash"), StringComparison.OrdinalIgnoreCase)) return "INVALID · content hash mismatch near #" + seq.ToString(CultureInfo.InvariantCulture); prev = S(r, "entry_hash"); expected++; } return "VALID · " + records.Count.ToString(CultureInfo.InvariantCulture) + " bounded records verify from durable tail anchor";
        }

        private static void OpenLoopWitness(Window owner, DeepSystemsContext ctx, AbyssDescentRecord parent, AbyssDescentRecord ancestor, string seed)
        {
            DeepSystems.OpenExternalValue(owner, ctx, "DESCENT LOOP WITNESS", new { requested_seed = seed ?? "", parent_descent = parent == null ? "" : parent.Id, ancestor_descent = ancestor == null ? "" : ancestor.Id, ancestor_depth = ancestor == null ? 0 : ancestor.Depth, ancestor_route_fingerprint = ancestor == null ? "" : ancestor.RouteFingerprint, action = "recursive descent suppressed", reason = "target fingerprint already exists in ancestry" }, "ATLAS › HADAL › LOOP WITNESS");
        }

        private static void SaveResumeCapsule(Window owner, DeepSystemsContext ctx, string id)
        {
            string summary = Prompt(owner, "RESUME CAPSULE", "What were you trying to understand at this depth?", ""); if (summary == null) return; string next = Prompt(owner, "RESUME CAPSULE", "First concrete thing to inspect when you return", ""); if (next == null) return; AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == id); if (d == null) return; d.ResumeSummary = Trunc(summary, 2000); d.ResumeNext = Trunc(next, 1000); d.UpdatedUtc = Now(); Save(ctx, store); Ledger(ctx, "RESUME_CAPSULE", d, "saved");
        }

        private static void AddNote(Window owner, DeepSystemsContext ctx, string id)
        {
            string note = Prompt(owner, "ABYSS NOTE", "Note for this descent", ""); if (String.IsNullOrWhiteSpace(note)) return; AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == id); if (d == null) return; d.Notes.Add(Now() + "  " + Trunc(note, 3000)); if (d.Notes.Count > 128) d.Notes = d.Notes.Skip(d.Notes.Count - 128).ToList(); d.UpdatedUtc = Now(); Save(ctx, store); Ledger(ctx, "NOTE_ADD", d, "note");
        }

        private static void SetStatus(DeepSystemsContext ctx, string id, string status)
        {
            AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == id); if (d == null) return; d.Status = status ?? "ACTIVE"; d.UpdatedUtc = Now(); Save(ctx, store); Ledger(ctx, "DESCENT_STATUS", d, d.Status);
        }

        private static void RecordDwell(DeepSystemsContext ctx, string id, DateTime openedUtc)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(id)) return; long seconds = Math.Max(0L, (long)(DateTime.UtcNow - openedUtc).TotalSeconds); if (seconds <= 0) return; AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == id); if (d == null) return; d.DwellSeconds = Math.Min(Int64.MaxValue - seconds, d.DwellSeconds) + seconds; d.UpdatedUtc = Now(); Save(ctx, store);
        }

        private static void MarkExit(DeepSystemsContext ctx, string id, string exit)
        {
            AbyssStore store = Load(ctx); AbyssDescentRecord d = store.Descents.FirstOrDefault(x => x.Id == id); if (d == null || String.IsNullOrWhiteSpace(exit)) return; if (!d.VisitedExitIds.Contains(exit, StringComparer.OrdinalIgnoreCase)) d.VisitedExitIds.Add(exit); d.UpdatedUtc = Now(); Save(ctx, store);
        }

        private static List<AbyssArtifact> ScanArtifacts(DeepSystemsContext ctx)
        {
            List<string> paths = new List<string>(); if (Directory.Exists(ctx.StateRoot)) paths.AddRange(Directory.EnumerateFiles(ctx.StateRoot, "*", SearchOption.TopDirectoryOnly)); string logRoot = Path.Combine(ctx.DataRoot, "logs"); if (Directory.Exists(logRoot)) paths.AddRange(Directory.EnumerateFiles(logRoot, "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => File.GetLastWriteTimeUtc(x)).Take(24));
            List<AbyssArtifact> result = new List<AbyssArtifact>(); foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(x => SafeWrite(x)).Take(128)) { try { FileInfo fi = new FileInfo(path); string ext = fi.Extension.ToLowerInvariant(); string kind = ext == ".json" ? "JSON" : ext == ".jsonl" ? "JSONL" : ext == ".log" || ext == ".txt" ? "TEXT" : "OTHER"; if (kind == "OTHER") continue; string text = ReadBoundedText(path, kind == "JSON" ? 2 * 1024 * 1024 : 768 * 1024); result.Add(new AbyssArtifact { Path = path, Bytes = fi.Length, ModifiedUtc = fi.LastWriteTimeUtc, Kind = kind, Text = text }); } catch { } } return result;
        }

        private static List<AbyssMatch> FindMatches(DeepSystemsContext ctx, string token, int max)
        {
            List<AbyssMatch> result = new List<AbyssMatch>(); if (String.IsNullOrWhiteSpace(token)) return result; foreach (AbyssArtifact a in ScanArtifacts(ctx)) { int idx = a.Text == null ? -1 : a.Text.IndexOf(token, StringComparison.OrdinalIgnoreCase); if (idx < 0) continue; int start = Math.Max(0, idx - 120); int len = Math.Min(360, a.Text.Length - start); string sample = OneLine(a.Text.Substring(start, len)); result.Add(new AbyssMatch { Path = a.Path, Kind = a.Kind, Token = token, Sample = sample, ModifiedUtc = a.ModifiedUtc }); if (result.Count >= max) break; } return result;
        }

        private static List<AbyssDivergence> AnalyzeDivergence(DeepSystemsContext ctx)
        {
            List<Dictionary<string, string>> obs = new List<Dictionary<string, string>>(); foreach (AbyssArtifact a in ScanArtifacts(ctx).Where(x => x.Kind == "JSON" || x.Kind == "JSONL")) CollectEntityObservations(a, obs); List<AbyssDivergence> result = new List<AbyssDivergence>();
            foreach (var g in obs.GroupBy(x => x["entity_kind"] + "\u001f" + x["entity_id"] + "\u001f" + x["attribute"], StringComparer.OrdinalIgnoreCase)) { List<string> vals = g.Select(x => x["value"]).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); if (vals.Count < 2) continue; string[] parts = g.Key.Split('\u001f'); result.Add(new AbyssDivergence { EntityKind = parts.Length > 0 ? parts[0] : "ENTITY", EntityId = parts.Length > 1 ? parts[1] : "", Attribute = parts.Length > 2 ? parts[2] : "field", Values = vals.Take(16).ToList(), Paths = g.Select(x => x["path"]).Distinct(StringComparer.OrdinalIgnoreCase).Take(32).ToList() }); }
            return result.OrderByDescending(x => x.Values.Count).ThenBy(x => x.EntityKind).Take(256).ToList();
        }

        private static void CollectEntityObservations(AbyssArtifact a, List<Dictionary<string, string>> obs)
        {
            if (a == null || String.IsNullOrWhiteSpace(a.Text)) return; if (a.Kind == "JSON") { try { object root = Json.DeserializeObject(a.Text); int budget = 6000; CollectEntityNode(root, "$", a.Path, obs, 0, ref budget); } catch { } }
            else { foreach (string line in SplitLines(a.Text).TakeLastCompatAbyss(1200)) { if (String.IsNullOrWhiteSpace(line)) continue; try { object root = Json.DeserializeObject(line); int budget = 500; CollectEntityNode(root, "$line", a.Path, obs, 0, ref budget); } catch { } } }
        }

        private static void CollectEntityNode(object node, string path, string file, List<Dictionary<string, string>> obs, int depth, ref int budget)
        {
            if (node == null || depth > 10 || budget-- <= 0) return; Dictionary<string, object> m = node as Dictionary<string, object>; if (m != null) { string occurrence = SInsensitive(m, "occurrence_id"); if (String.IsNullOrWhiteSpace(occurrence)) occurrence = SInsensitive(m, "occurrence"); if (!String.IsNullOrWhiteSpace(occurrence)) { Observe(obs, "OCCURRENCE", occurrence, "source_key", SInsensitive(m, "source_key"), file); Observe(obs, "OCCURRENCE", occurrence, "title", SInsensitive(m, "title"), file); Observe(obs, "OCCURRENCE", occurrence, "channel", SInsensitive(m, "channel"), file); }
                string intent = SInsensitive(m, "intent_id"); if (!String.IsNullOrWhiteSpace(intent)) Observe(obs, "INTENT", intent, "plan_fingerprint", SInsensitive(m, "plan_fingerprint"), file);
                string branch = SInsensitive(m, "branch_id"); if (String.IsNullOrWhiteSpace(branch) && path.IndexOf("branch", StringComparison.OrdinalIgnoreCase) >= 0) branch = SInsensitive(m, "id"); if (!String.IsNullOrWhiteSpace(branch)) { Observe(obs, "BRANCH", branch, "parent_branch_id", SInsensitive(m, "parent_branch_id"), file); Observe(obs, "BRANCH", branch, "root_branch_id", SInsensitive(m, "root_branch_id"), file); }
                string task = path.IndexOf("task", StringComparison.OrdinalIgnoreCase) >= 0 ? SInsensitive(m, "id") : ""; if (!String.IsNullOrWhiteSpace(task)) { Observe(obs, "TASK", task, "mission_id", SInsensitive(m, "mission_id"), file); Observe(obs, "TASK", task, "parent_task_id", SInsensitive(m, "parent_task_id"), file); Observe(obs, "TASK", task, "kind", SInsensitive(m, "kind"), file); }
                foreach (KeyValuePair<string, object> kv in m) CollectEntityNode(kv.Value, path + "." + kv.Key, file, obs, depth + 1, ref budget); return; }
            IEnumerable e = node as IEnumerable; if (e != null && !(node is string)) { int i = 0; foreach (object x in e) { CollectEntityNode(x, path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", file, obs, depth + 1, ref budget); if (++i > 1024 || budget <= 0) break; } }
        }

        private static void Observe(List<Dictionary<string, string>> obs, string kind, string id, string attr, string value, string path)
        {
            if (String.IsNullOrWhiteSpace(id) || String.IsNullOrWhiteSpace(value)) return; obs.Add(new Dictionary<string, string> { { "entity_kind", kind }, { "entity_id", id }, { "attribute", attr }, { "value", value }, { "path", path ?? "" } });
        }

        private static List<AbyssGap> AnalyzeGaps(DeepSystemsContext ctx)
        {
            List<AbyssGap> r = new List<AbyssGap>(); AnalyzeProductivityGaps(ctx, r); AnalyzeResearchGaps(ctx, r); AnalyzeBranchGaps(ctx, r); AnalyzeIntentGaps(ctx, r); return r.OrderByDescending(x => x.Severity).ThenBy(x => x.Kind).Take(512).ToList();
        }

        private static void AnalyzeProductivityGaps(DeepSystemsContext ctx, List<AbyssGap> r)
        {
            string path = Path.Combine(ctx.StateRoot, "controller-productivity-fabric.json"); Dictionary<string, object> root = ReadJsonMap(path); if (root == null) return; List<Dictionary<string, object>> tasks = Maps(root, "tasks").ToList(); HashSet<string> taskIds = new HashSet<string>(tasks.Select(x => S(x, "id")).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); foreach (Dictionary<string, object> t in tasks) foreach (string dep in Strings(t, "depends_on")) if (!taskIds.Contains(dep)) r.Add(new AbyssGap { Kind = "MISSING_TASK_DEPENDENCY", Id = S(t, "id"), Summary = "Task depends on absent task " + dep, Detail = "task=" + S(t, "title") + " dependency=" + dep, EvidencePath = path, Severity = 5 });
            List<Dictionary<string, object>> missions = Maps(root, "missions").ToList(); HashSet<string> missionIds = new HashSet<string>(missions.Select(x => S(x, "id")).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); foreach (Dictionary<string, object> m in missions) { string parent = S(m, "parent_id"); if (!String.IsNullOrWhiteSpace(parent) && !missionIds.Contains(parent)) r.Add(new AbyssGap { Kind = "ORPHAN_MISSION_PARENT", Id = S(m, "id"), Summary = "Mission references absent parent " + parent, Detail = S(m, "title"), EvidencePath = path, Severity = 4 }); }
            foreach (Dictionary<string, object> t in tasks) { string mission = S(t, "mission_id"); if (!String.IsNullOrWhiteSpace(mission) && !missionIds.Contains(mission)) r.Add(new AbyssGap { Kind = "ORPHAN_TASK_MISSION", Id = S(t, "id"), Summary = "Task references absent mission " + mission, Detail = S(t, "title"), EvidencePath = path, Severity = 5 }); }
        }

        private static void AnalyzeResearchGaps(DeepSystemsContext ctx, List<AbyssGap> r)
        {
            string pPath = Path.Combine(ctx.StateRoot, "controller-productivity-fabric.json"); string rPath = Path.Combine(ctx.StateRoot, "controller-research-workbench.json"); Dictionary<string, object> p = ReadJsonMap(pPath); Dictionary<string, object> rw = ReadJsonMap(rPath); if (p == null || rw == null) return; HashSet<string> cases = new HashSet<string>(Maps(rw, "sessions").Select(x => S(x, "id")).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); foreach (Dictionary<string, object> m in Maps(p, "missions")) { string id = S(m, "research_case_id"); if (!String.IsNullOrWhiteSpace(id) && !cases.Contains(id)) r.Add(new AbyssGap { Kind = "MISSING_RESEARCH_CASE", Id = S(m, "id"), Summary = "Mission points to absent research case " + id, Detail = S(m, "title"), EvidencePath = pPath, Severity = 4 }); } foreach (Dictionary<string, object> t in Maps(p, "tasks")) { string id = S(t, "research_case_id"); if (!String.IsNullOrWhiteSpace(id) && !cases.Contains(id)) r.Add(new AbyssGap { Kind = "MISSING_RESEARCH_CASE", Id = S(t, "id"), Summary = "Task points to absent research case " + id, Detail = S(t, "title"), EvidencePath = pPath, Severity = 4 }); }
        }

        private static void AnalyzeBranchGaps(DeepSystemsContext ctx, List<AbyssGap> r)
        {
            string path = Path.Combine(ctx.StateRoot, "controller-counterfactual-branches.json"); Dictionary<string, object> root = ReadJsonMap(path); if (root == null) return; List<Dictionary<string, object>> branches = Maps(root, "branches").ToList(); HashSet<string> ids = new HashSet<string>(branches.Select(x => S(x, "id")).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); foreach (Dictionary<string, object> b in branches) { string parent = S(b, "parent_branch_id"); string rootId = S(b, "root_branch_id"); if (!String.IsNullOrWhiteSpace(parent) && !ids.Contains(parent)) r.Add(new AbyssGap { Kind = "ORPHAN_COUNTERFACTUAL_PARENT", Id = S(b, "id"), Summary = "Counterfactual branch parent missing: " + parent, Detail = S(b, "name"), EvidencePath = path, Severity = 3 }); if (!String.IsNullOrWhiteSpace(rootId) && !ids.Contains(rootId) && !String.Equals(rootId, S(b, "id"), StringComparison.OrdinalIgnoreCase)) r.Add(new AbyssGap { Kind = "ORPHAN_COUNTERFACTUAL_ROOT", Id = S(b, "id"), Summary = "Counterfactual root missing: " + rootId, Detail = S(b, "name"), EvidencePath = path, Severity = 3 }); }
        }

        private static void AnalyzeIntentGaps(DeepSystemsContext ctx, List<AbyssGap> r)
        {
            string path = Path.Combine(ctx.StateRoot, "controller-intent-journal.jsonl"); if (!File.Exists(path)) return; string tail = ReadBoundedText(path, 768 * 1024); foreach (string line in SplitLines(tail).TakeLastCompatAbyss(800)) { if (line.IndexOf("INDETERMINATE", StringComparison.OrdinalIgnoreCase) < 0 && line.IndexOf("ABANDONED", StringComparison.OrdinalIgnoreCase) < 0) continue; try { Dictionary<string, object> m = Json.DeserializeObject(line) as Dictionary<string, object>; if (m == null) continue; string state = S(m, "state"); r.Add(new AbyssGap { Kind = "UNRESOLVED_INTENT_" + state.ToUpperInvariant(), Id = S(m, "intent_id"), Summary = "Operator intent ended in " + state, Detail = S(m, "reason"), EvidencePath = path, Severity = String.Equals(state, "INDETERMINATE", StringComparison.OrdinalIgnoreCase) ? 5 : 3 }); } catch { } }
        }

        private static List<Dictionary<string, object>> BuildQuestions(DeepSystemsContext ctx, AbyssDescentRecord d, List<AbyssGap> gaps, List<AbyssDivergence> div)
        {
            List<Dictionary<string, object>> q = new List<Dictionary<string, object>>(); foreach (AbyssGap g in gaps.Take(40)) q.Add(new Dictionary<string, object> { { "epistemic", "QUESTION" }, { "question", "What explains the observed " + g.Kind.ToLowerInvariant().Replace('_', ' ') + "?" }, { "witness", g.Summary }, { "evidence_path", g.EvidencePath ?? "" }, { "severity", g.Severity } }); foreach (AbyssDivergence x in div.Take(30)) q.Add(new Dictionary<string, object> { { "epistemic", "QUESTION" }, { "question", "Why does " + x.EntityKind + " " + x.EntityId + " expose multiple values for " + x.Attribute + "?" }, { "observed_values", x.Values.ToArray() }, { "evidence_paths", x.Paths.ToArray() }, { "evidence_path", x.Paths.FirstOrDefault() ?? "" } });
            if (d != null && d.Depth >= 5 && d.VisitedExitIds.Count < 4) q.Add(new Dictionary<string, object> { { "epistemic", "QUESTION" }, { "question", "Which unexplored exit at this depth carries the highest information gain?" }, { "descent_id", d.Id }, { "visited_exits", d.VisitedExitIds.ToArray() }, { "evidence_path", d.SourcePath ?? "" } });
            string runtime = Path.Combine(ctx.StateRoot, "queue-runtime.json"); Dictionary<string, object> qr = ReadJsonMap(runtime); if (qr != null) { string health = S(qr, "buffer_health"); if (!String.IsNullOrWhiteSpace(health) && !String.Equals(health, "healthy", StringComparison.OrdinalIgnoreCase) && !String.Equals(health, "good", StringComparison.OrdinalIgnoreCase)) q.Add(new Dictionary<string, object> { { "epistemic", "QUESTION" }, { "question", "What evidence explains the current buffer-health state '" + health + "'?" }, { "evidence_path", runtime } }); }
            return q.Take(128).ToList();
        }

        private static bool RelevantGap(AbyssGap g, AbyssDescentRecord d)
        {
            if (d == null) return true; string hay = ((g.Id ?? "") + " " + (g.Summary ?? "") + " " + (g.Detail ?? "") + " " + (g.EvidencePath ?? "")).ToLowerInvariant(); foreach (string token in d.IdentityTokens) { string v = TokenValue(token); if (v.Length >= 3 && hay.IndexOf(v.ToLowerInvariant(), StringComparison.Ordinal) >= 0) return true; } return d.SeedValue != null && d.SeedValue.Length >= 3 && hay.IndexOf(d.SeedValue.ToLowerInvariant(), StringComparison.Ordinal) >= 0;
        }

        private static bool RelevantDivergence(AbyssDivergence x, AbyssDescentRecord d)
        {
            if (d == null) return true; string hay = ((x.EntityKind ?? "") + "=" + (x.EntityId ?? "") + " " + (x.Attribute ?? "") + " " + String.Join(" ", x.Values.ToArray())).ToLowerInvariant(); foreach (string token in d.IdentityTokens) { string v = TokenValue(token); if (v.Length >= 3 && hay.IndexOf(v.ToLowerInvariant(), StringComparison.Ordinal) >= 0) return true; } return d.SeedValue != null && d.SeedValue.Length >= 3 && hay.IndexOf(d.SeedValue.ToLowerInvariant(), StringComparison.Ordinal) >= 0;
        }

        private static List<string> ExtractIdentityTokens(object value)
        {
            List<string> result = new List<string>(); int budget = 5000; ExtractIdentityNode(value, result, 0, ref budget); return result.Distinct(StringComparer.OrdinalIgnoreCase).Take(128).ToList();
        }

        private static void ExtractIdentityNode(object node, List<string> result, int depth, ref int budget)
        {
            if (node == null || depth > 10 || budget-- <= 0) return; Dictionary<string, object> m = node as Dictionary<string, object>; if (m != null) { foreach (KeyValuePair<string, object> kv in m) { if (IdentityKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) && IsScalar(kv.Value)) { string v = Convert.ToString(kv.Value, CultureInfo.InvariantCulture); if (!String.IsNullOrWhiteSpace(v) && v.Length <= 512) result.Add(kv.Key.ToLowerInvariant() + "=" + v); } ExtractIdentityNode(kv.Value, result, depth + 1, ref budget); } return; } IEnumerable e = node as IEnumerable; if (e != null && !(node is string)) { int i = 0; foreach (object x in e) { ExtractIdentityNode(x, result, depth + 1, ref budget); if (++i > 1024 || budget <= 0) break; } }
        }

        private static object ParseBest(AbyssArtifact a)
        {
            if (a == null) return null; if (a.Kind == "JSON") { try { return Json.DeserializeObject(a.Text); } catch { return a.Text; } } return a.Text;
        }

        private static string PrimaryToken(AbyssDescentRecord d)
        {
            if (d == null) return ""; if (d.IdentityTokens != null && d.IdentityTokens.Count > 0) return TokenValue(d.IdentityTokens[0]); return d.SeedValue ?? "";
        }

        private static int Pressure(AbyssDescentRecord d, int matches, int gaps, int divergences)
        {
            int visited = d == null ? 0 : d.VisitedExitIds.Distinct(StringComparer.OrdinalIgnoreCase).Count(); int ids = d == null ? 0 : d.IdentityTokens.Count; return Math.Min(999, (matches * 2) + (gaps * 9) + (divergences * 12) + (ids * 4) + Math.Max(0, 10 - visited) * 3 + (d == null ? 0 : d.Depth * 2));
        }

        private static int ApproxMeters(int depth) { return Math.Min(10984, Math.Max(0, depth) * 1098); }
        private static string Zone(int depth) { if (depth <= 1) return "bathyal gate"; if (depth <= 3) return "midnight descent"; if (depth <= 6) return "abyssal plain"; return "hadal trench"; }

        private static List<AbyssDescentRecord> Ancestors(AbyssStore store, AbyssDescentRecord d)
        {
            List<AbyssDescentRecord> result = new List<AbyssDescentRecord>(); HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string id = d == null ? "" : d.ParentId; while (!String.IsNullOrWhiteSpace(id) && seen.Add(id) && result.Count < 256) { AbyssDescentRecord x = store.Descents.FirstOrDefault(y => String.Equals(y.Id, id, StringComparison.OrdinalIgnoreCase)); if (x == null) break; result.Add(x); id = x.ParentId; } return result;
        }

        private static AbyssStore Load(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                AbyssStore store = new AbyssStore(); string path = StorePath(ctx); if (!File.Exists(path)) return store; try { Dictionary<string, object> root = Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>; if (root == null) return store; store.Schema = I(root, "schema", 1); store.CurrentDescentId = S(root, "current_descent_id"); foreach (Dictionary<string, object> m in Maps(root, "descents")) { AbyssDescentRecord d = new AbyssDescentRecord { Id = S(m, "id"), ParentId = S(m, "parent_id"), RootId = S(m, "root_id"), Title = S(m, "title"), SeedKind = S(m, "seed_kind"), SeedValue = S(m, "seed_value"), SourcePath = S(m, "source_path"), JsonPath = S(m, "json_path"), Fingerprint = S(m, "fingerprint"), RouteFingerprint = S(m, "route_fingerprint"), Depth = I(m, "depth", 1), Zone = S(m, "zone"), StartedUtc = S(m, "started_utc"), UpdatedUtc = S(m, "updated_utc"), LastOpenedUtc = S(m, "last_opened_utc"), Status = S(m, "status"), DwellSeconds = L(m, "dwell_seconds", 0), OpenCount = I(m, "open_count", 0), ResumeSummary = S(m, "resume_summary"), ResumeNext = S(m, "resume_next"), IdentityTokens = Strings(m, "identity_tokens"), VisitedExitIds = Strings(m, "visited_exit_ids"), Notes = Strings(m, "notes") }; if (!String.IsNullOrWhiteSpace(d.Id)) store.Descents.Add(d); } foreach (Dictionary<string, object> m in Maps(root, "fossils")) { AbyssFossil f = new AbyssFossil { Id = S(m, "id"), DescentId = S(m, "descent_id"), Title = S(m, "title"), SourcePath = S(m, "source_path"), JsonPath = S(m, "json_path"), CapturedUtc = S(m, "captured_utc"), Fingerprint = S(m, "fingerprint"), Preview = S(m, "preview"), IdentityTokens = Strings(m, "identity_tokens") }; if (!String.IsNullOrWhiteSpace(f.Id)) store.Fossils.Add(f); } } catch { PreserveCorrupt(StorePath(ctx)); } return store;
            }
        }

        private static void Save(DeepSystemsContext ctx, AbyssStore store)
        {
            lock (Gate) { Directory.CreateDirectory(ctx.StateRoot); TrimStore(store); Dictionary<string, object> root = new Dictionary<string, object>(); root["schema"] = 1; root["updated_utc"] = Now(); root["current_descent_id"] = store.CurrentDescentId ?? ""; root["descents"] = store.Descents.Select(DescentMap).ToArray(); root["fossils"] = store.Fossils.Select(FossilMap).ToArray(); AtomicWrite(StorePath(ctx), Json.Serialize(root)); }
        }

        private static void TrimStore(AbyssStore store)
        {
            if (store.Descents.Count > 512) store.Descents = store.Descents.OrderByDescending(x => x.UpdatedUtc).Take(512).ToList(); if (store.Fossils.Count > 256) store.Fossils = store.Fossils.OrderByDescending(x => x.CapturedUtc).Take(256).ToList();
        }

        private static Dictionary<string, object> DescentMap(AbyssDescentRecord d) { return new Dictionary<string, object> { { "id", d.Id }, { "parent_id", d.ParentId }, { "root_id", d.RootId }, { "title", d.Title }, { "seed_kind", d.SeedKind }, { "seed_value", d.SeedValue }, { "source_path", d.SourcePath }, { "json_path", d.JsonPath }, { "fingerprint", d.Fingerprint }, { "route_fingerprint", d.RouteFingerprint }, { "depth", d.Depth }, { "zone", d.Zone }, { "started_utc", d.StartedUtc }, { "updated_utc", d.UpdatedUtc }, { "last_opened_utc", d.LastOpenedUtc }, { "status", d.Status }, { "dwell_seconds", d.DwellSeconds }, { "open_count", d.OpenCount }, { "resume_summary", d.ResumeSummary }, { "resume_next", d.ResumeNext }, { "identity_tokens", d.IdentityTokens.Take(64).ToArray() }, { "visited_exit_ids", d.VisitedExitIds.Take(64).ToArray() }, { "notes", d.Notes.TakeLastCompatAbyss(128).ToArray() } }; }
        private static Dictionary<string, object> FossilMap(AbyssFossil f) { return new Dictionary<string, object> { { "id", f.Id }, { "descent_id", f.DescentId }, { "title", f.Title }, { "source_path", f.SourcePath }, { "json_path", f.JsonPath }, { "captured_utc", f.CapturedUtc }, { "fingerprint", f.Fingerprint }, { "preview", f.Preview }, { "identity_tokens", f.IdentityTokens.Take(64).ToArray() } }; }
        private static Dictionary<string, object> GapMap(AbyssGap g) { return new Dictionary<string, object> { { "kind", g.Kind }, { "id", g.Id }, { "summary", g.Summary }, { "detail", g.Detail }, { "evidence_path", g.EvidencePath }, { "severity", g.Severity }, { "epistemic", "OBSERVED_GAP" } }; }

        private static void Ledger(DeepSystemsContext ctx, string action, AbyssDescentRecord d, string detail)
        {
            try { string path = Path.Combine(ctx.StateRoot, "controller-abyssal-ledger.jsonl"); string prev = LastLedgerHash(path); long seq = LastLedgerSeq(path) + 1; Dictionary<string, object> core = new Dictionary<string, object> { { "seq", seq }, { "utc", Now() }, { "action", action ?? "" }, { "descent_id", d == null ? "" : d.Id }, { "depth", d == null ? 0 : d.Depth }, { "route_fingerprint", d == null ? "" : d.RouteFingerprint }, { "detail", Trunc(detail ?? "", 1000) }, { "prev_hash", prev } }; string canonical = String.Join("\n", new string[] { seq.ToString(CultureInfo.InvariantCulture), S(core, "utc"), S(core, "action"), S(core, "descent_id"), Convert.ToString(core["depth"], CultureInfo.InvariantCulture), S(core, "route_fingerprint"), S(core, "detail"), prev }); core["entry_hash"] = Hash(canonical); File.AppendAllText(path, Json.Serialize(core) + Environment.NewLine, new UTF8Encoding(false)); } catch { }
        }

        private static long LastLedgerSeq(string path) { long seq = 0; foreach (string line in ReadTailLines(path, 64)) { try { Dictionary<string, object> m = Json.DeserializeObject(line) as Dictionary<string, object>; if (m != null) seq = Math.Max(seq, L(m, "seq", 0)); } catch { } } return seq; }
        private static string LastLedgerHash(string path) { string hash = new string('0', 64); foreach (string line in ReadTailLines(path, 64)) { try { Dictionary<string, object> m = Json.DeserializeObject(line) as Dictionary<string, object>; string h = S(m, "entry_hash"); if (!String.IsNullOrWhiteSpace(h)) hash = h; } catch { } } return hash; }

        private static void AtomicWrite(string path, string text)
        {
            string tmp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text ?? "{}", new UTF8Encoding(false)); if (File.Exists(path)) { string bak = path + ".bak"; try { File.Replace(tmp, path, bak, true); try { File.Delete(bak); } catch { } } catch { File.Copy(tmp, path, true); File.Delete(tmp); } } else File.Move(tmp, path);
        }

        private static void PreserveCorrupt(string path)
        {
            try { if (!File.Exists(path)) return; byte[] b = File.ReadAllBytes(path); string h; using (SHA256 s = SHA256.Create()) h = BitConverter.ToString(s.ComputeHash(b)).Replace("-", "").ToLowerInvariant(); string copy = path + ".corrupt." + h.Substring(0, 12) + ".json"; if (!File.Exists(copy)) File.WriteAllBytes(copy, b); } catch { }
        }

        private static Dictionary<string, object> ReadJsonMap(string path)
        {
            try { if (!File.Exists(path) || new FileInfo(path).Length > 4 * 1024 * 1024) return null; return Json.DeserializeObject(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object>; } catch { return null; }
        }

        private static IEnumerable<Dictionary<string, object>> Maps(Dictionary<string, object> root, string key)
        {
            object raw; if (root == null || !TryGetInsensitive(root, key, out raw) || raw == null || raw is string) yield break; IEnumerable e = raw as IEnumerable; if (e == null) yield break; foreach (object x in e) { Dictionary<string, object> m = x as Dictionary<string, object>; if (m != null) yield return m; }
        }

        private static List<string> Strings(Dictionary<string, object> m, string key)
        {
            object raw; List<string> r = new List<string>(); if (m == null || !TryGetInsensitive(m, key, out raw) || raw == null || raw is string) return r; IEnumerable e = raw as IEnumerable; if (e == null) return r; foreach (object x in e) { string s = Convert.ToString(x, CultureInfo.InvariantCulture); if (!String.IsNullOrWhiteSpace(s)) r.Add(s); } return r;
        }

        private static bool TryGetInsensitive(Dictionary<string, object> m, string key, out object value)
        {
            value = null; if (m == null) return false; if (m.TryGetValue(key, out value)) return true; foreach (KeyValuePair<string, object> kv in m) if (String.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) { value = kv.Value; return true; } return false;
        }
        private static string SInsensitive(Dictionary<string, object> m, string key) { object v; return TryGetInsensitive(m, key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : ""; }
        private static string S(Dictionary<string, object> m, string key) { object v; return m != null && m.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : ""; }
        private static int I(Dictionary<string, object> m, string key, int fallback) { object v; int n; return m != null && m.TryGetValue(key, out v) && v != null && Int32.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : fallback; }
        private static long L(Dictionary<string, object> m, string key, long fallback) { object v; long n; return m != null && m.TryGetValue(key, out v) && v != null && Int64.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : fallback; }

        private static string SafeSerialize(object value) { try { return value is string ? (string)value : Json.Serialize(value); } catch { return Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; } }
        private static bool IsScalar(object value) { return value == null || value is string || value is bool || value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal || value is DateTime; }
        private static string TokenValue(string token) { int i = (token ?? "").IndexOf('='); return i >= 0 ? token.Substring(i + 1) : token ?? ""; }
        private static string ReadBoundedText(string path, int maxBytes) { try { using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { long len = fs.Length; int take = (int)Math.Min((long)Math.Max(1, maxBytes), len); if (len > take) fs.Seek(len - take, SeekOrigin.Begin); byte[] b = new byte[take]; int offset = 0; while (offset < take) { int n = fs.Read(b, offset, take - offset); if (n <= 0) break; offset += n; } return Encoding.UTF8.GetString(b, 0, offset); } } catch { return ""; } }
        private static IEnumerable<string> ReadTailLines(string path, int maxLines) { if (!File.Exists(path)) return new string[0]; return SplitLines(ReadBoundedText(path, 256 * 1024)).TakeLastCompatAbyss(maxLines); }
        private static IEnumerable<string> SplitLines(string s) { return (s ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'); }
        private static DateTime SafeWrite(string path) { try { return File.GetLastWriteTimeUtc(path); } catch { return DateTime.MinValue; } }
        private static string OneLine(string s) { return Trunc((s ?? "").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' '), 360); }
        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static string Id(string prefix) { return (prefix ?? "id") + "-" + Guid.NewGuid().ToString("N").Substring(0, 12); }
        private static string Hash(string s) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""))).Replace("-", "").ToLowerInvariant(); }
        private static string Short(string s, int n) { if (String.IsNullOrEmpty(s)) return "∅"; return s.Length <= n ? s : s.Substring(0, n); }
        private static string Trunc(string s, int n) { if (String.IsNullOrEmpty(s)) return ""; return s.Length <= n ? s : s.Substring(0, n) + "…"; }
        private static string FormatDuration(long seconds) { if (seconds < 0) seconds = 0; TimeSpan ts = TimeSpan.FromSeconds(seconds); if (ts.TotalHours >= 1) return ((int)ts.TotalHours).ToString(CultureInfo.InvariantCulture) + "h " + ts.Minutes.ToString(CultureInfo.InvariantCulture) + "m"; if (ts.TotalMinutes >= 1) return ((int)ts.TotalMinutes).ToString(CultureInfo.InvariantCulture) + "m " + ts.Seconds.ToString(CultureInfo.InvariantCulture) + "s"; return ts.Seconds.ToString(CultureInfo.InvariantCulture) + "s"; }

        private static Window W(Window owner, string title, double width, double height) { return new Window { Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 780), MinHeight = Math.Min(height, 540), Background = Bg, Foreground = Text, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner, FontFamily = new FontFamily("Segoe UI") }; }
        private static TextBlock T(string text, double size, Brush color, FontWeight weight) { return new TextBlock { Text = text ?? "", FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap }; }
        private static Button Btn(string text, RoutedEventHandler click) { Button b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(11, 7, 11, 7), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; b.Click += click; return b; }
        private static ListBox List() { return new ListBox { Margin = new Thickness(0, 10, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), Padding = new Thickness(5) }; }
        private static void Portal(Panel panel, string badge, string title, string subtitle, Action action) { StackPanel s = new StackPanel(); s.Children.Add(T(badge, 10.5, Accent, FontWeights.Bold)); s.Children.Add(T(title, 18, Text, FontWeights.SemiBold)); TextBlock d = T(subtitle, 12, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(d); Border c = new Border { Child = s, Width = 355, MinHeight = 138, Margin = new Thickness(0, 0, 12, 12), Padding = new Thickness(14), Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Cursor = Cursors.Hand }; c.MouseLeftButtonUp += delegate { if (action != null) action(); }; panel.Children.Add(c); }
        private static string Prompt(Window owner, string title, string label, string initial) { Window w = W(owner, "YOMI · " + title, 640, 290); w.ResizeMode = ResizeMode.NoResize; StackPanel p = new StackPanel { Margin = new Thickness(18) }; p.Children.Add(T(label, 13, Muted, FontWeights.Normal)); TextBox box = new TextBox { Text = initial ?? "", MinHeight = 82, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(9), Background = Raised, Foreground = Text, BorderBrush = Border }; p.Children.Add(box); string result = null; WrapPanel b = new WrapPanel(); b.Children.Add(Btn("DESCEND", delegate { result = box.Text; w.DialogResult = true; w.Close(); })); b.Children.Add(Btn("CANCEL", delegate { w.DialogResult = false; w.Close(); })); p.Children.Add(b); w.Content = p; box.Focus(); w.ShowDialog(); return result; }
    }

    internal static class AbyssEnumerableCompat
    {
        public static IEnumerable<T> TakeLastCompatAbyss<T>(this IEnumerable<T> source, int count) { if (source == null) yield break; Queue<T> q = new Queue<T>(); foreach (T item in source) { q.Enqueue(item); if (q.Count > count) q.Dequeue(); } foreach (T item in q) yield return item; }
    }
}

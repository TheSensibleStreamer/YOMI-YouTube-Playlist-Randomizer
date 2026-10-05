using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace Yomi.ProductShell
{
    internal sealed class GenomeProjectionReceipt
    {
        public string Id { get; set; }
        public string Civilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string CreatedUtc { get; set; }
        public string Rule { get; set; }
        public string CertificateHash { get; set; }
        public GenomeProjectionReceipt() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Civilization ?? "SOURCE") + "   ·   " + (SourceId ?? "∅") + "   ·   fp " + SoftwareGenomeKernel.Short(SourceFingerprint) + "   ·   " + (Rule ?? "projection"); }
    }

    internal sealed class GenomeBaseline
    {
        public string Id { get; set; }
        public string SnapshotId { get; set; }
        public string SnapshotFingerprint { get; set; }
        public string Name { get; set; }
        public string CreatedUtc { get; set; }
        public int Files { get; set; }
        public int Symbols { get; set; }
        public int Edges { get; set; }
        public int Cycles { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (Name ?? Id ?? "baseline") + "   ·   " + Files + " files   ·   " + Symbols + " symbols   ·   " + Edges + " edges   ·   fp " + SoftwareGenomeKernel.Short(SnapshotFingerprint); }
    }

    internal sealed class GenomeCapsule
    {
        public string Id { get; set; }
        public string SnapshotId { get; set; }
        public string Question { get; set; }
        public string Prediction { get; set; }
        public string Falsifier { get; set; }
        public string CreatedUtc { get; set; }
        public string SnapshotFingerprint { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (Question ?? "capsule") + "   ·   prediction " + (Prediction ?? "") + "   ·   fp " + SoftwareGenomeKernel.Short(CertificateHash); }
    }

    internal sealed class GenomeDossier
    {
        public string Id { get; set; }
        public string SnapshotId { get; set; }
        public string SubjectId { get; set; }
        public string Title { get; set; }
        public string CreatedUtc { get; set; }
        public List<string> Evidence { get; set; }
        public List<string> Implications { get; set; }
        public List<string> Uncertainties { get; set; }
        public string CertificateHash { get; set; }
        public GenomeDossier() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Evidence = new List<string>(); Implications = new List<string>(); Uncertainties = new List<string>(); }
        public override string ToString() { return (Title ?? Id ?? "dossier") + "   ·   evidence " + Evidence.Count + "   ·   implications " + Implications.Count + "   ·   uncertainties " + Uncertainties.Count + "   ·   " + SoftwareGenomeKernel.Short(CertificateHash); }
    }

    internal sealed class GenomeLedgerRecord
    {
        public int Schema { get; set; }
        public long Sequence { get; set; }
        public string Utc { get; set; }
        public string Event { get; set; }
        public string Subject { get; set; }
        public string Detail { get; set; }
        public string SnapshotFingerprint { get; set; }
        public string CertificateHash { get; set; }
        public string PrevHash { get; set; }
        public string EntryHash { get; set; }
        public override string ToString() { return Sequence.ToString("000000", CultureInfo.InvariantCulture) + "   ·   " + (Utc ?? "") + "   ·   " + (Event ?? "EVENT") + "   ·   " + (Subject ?? "") + "   ·   " + SoftwareGenomeKernel.Short(EntryHash); }
    }

    internal sealed class SoftwareGenomeStore
    {
        public int Schema { get; set; }
        public int Revision { get; set; }
        public string UpdatedUtc { get; set; }
        public List<GenomeSnapshot> Snapshots { get; set; }
        public List<GenomeImpactReport> Impacts { get; set; }
        public List<GenomeProjectionReceipt> Projections { get; set; }
        public List<GenomeBaseline> Baselines { get; set; }
        public List<GenomeCapsule> Capsules { get; set; }
        public List<GenomeDossier> Dossiers { get; set; }
        public SoftwareGenomeStore() { Schema = 1; Snapshots = new List<GenomeSnapshot>(); Impacts = new List<GenomeImpactReport>(); Projections = new List<GenomeProjectionReceipt>(); Baselines = new List<GenomeBaseline>(); Capsules = new List<GenomeCapsule>(); Dossiers = new List<GenomeDossier>(); }
    }

    internal static class SoftwareGenomeObservatory
    {
        private static readonly object Gate = new object();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly Brush Bg = B("#090C12"), Surface = B("#111822"), Raised = B("#182536"), Border = B("#30455F"), Text = B("#F0F7FF"), Muted = B("#91A7BB"), Accent = B("#78E0FF"), Green = B("#65E6A7"), Amber = B("#FFD166"), Danger = B("#FF738B"), Violet = B("#C09AFF");
        private static SoftwareGenomeStore Store;
        private static string LoadedRoot;
        private const int MaxSnapshots = 8;
        private const int MaxLedgerRead = 65536;

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return; Ensure(ctx); if (Store.Snapshots.Count == 0) SeedStarter(ctx);
            Window w = W(owner, "YOMI · DEV13.37.25 · SOFTWARE GENOME OBSERVATORY", 1540, 950);
            DockPanel root = Shell("SOFTWARE GENOME OBSERVATORY", "Turn a codebase into an explorable organism: files, symbols, dependencies, calls, state flow, cycles, architecture, history, ownership, change consequences and evidence. Analysis is static; the observed project is never executed.");
            TextBlock status = new TextBlock { Text = "GENOMEΩ   ·   DEV13.37.25   ·   " + Store.Snapshots.Count + " retained genomes   ·   " + Store.Impacts.Count + " impact reports   ·   " + Store.Projections.Count + " civilization projections   ·   Ctrl+Alt+G", Foreground = Accent, FontWeight = FontWeights.Bold, Margin = new Thickness(20, 0, 20, 14) }; DockPanel.SetDock(status, Dock.Top); root.Children.Add(status);
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; WrapPanel chambers = new WrapPanel { Margin = new Thickness(12) };
            Portal(chambers, "REG", "GENOME REGISTRY", "Retained codebase snapshots with source-root fingerprint, file/symbol graph and historical evidence.", delegate { OpenSnapshots(w, ctx); });
            Portal(chambers, "SCAN", "INGEST OBSERVATORY", "Explicitly choose a local source directory. Read text and metadata only; never build, import, run or load the analyzed project.", delegate { ScanRepository(w, ctx); });
            Portal(chambers, "FILE", "FILE ORGANISM", "Open every retained source file as a living organ: language, hash, lines, symbols, fan-in/out, mutations, boundaries and ownership.", delegate { OpenFiles(w); });
            Portal(chambers, "SYM", "SYMBOL MICROSCOPE", "Descend to classes, methods and functions with source position, body fingerprint, complexity, calls and state access.", delegate { OpenSymbols(w); });
            Portal(chambers, "GENE", "GENE FINDER", "Search retained symbol and file metadata by name without rescanning the source tree.", delegate { GeneFinder(w); });
            Portal(chambers, "MOD", "MODULE / NAMESPACE ATLAS", "Imports, modules and inferred directory layers reveal the large-scale anatomy of the repository.", delegate { OpenModules(w); });
            Portal(chambers, "DEP", "DEPENDENCY CONSTELLATION", "Resolved file dependency edges from imports and unique type references, with explicit heuristic confidence.", delegate { OpenEdges(w, "FILE_DEPENDS_ON", "DEPENDENCY CONSTELLATION"); });
            Portal(chambers, "REV", "REVERSE DEPENDENCY TELESCOPE", "Ask who depends on a selected file and keep descending outward toward consequences.", delegate { ReverseDependencies(w); });
            Portal(chambers, "CALL", "CALL GRAPH OBSERVATORY", "Resolved unique-symbol calls with caller, callee, line and evidence snippet.", delegate { OpenEdges(w, "CALLS", "CALL GRAPH OBSERVATORY"); });
            Portal(chambers, "FLOW", "DATA-FLOW RIVER MAP", "Static writes and emissions associated with their containing functions and state classifications.", delegate { OpenDataFlows(w); });
            Portal(chambers, "MUT", "STATE MUTATION MAP", "Assignment-heavy regions, shared/member state writes and potential mutation concentration.", delegate { OpenComputed(w, "STATE MUTATION MAP", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.MutationMap(s).Cast<object>().ToList(); }, "Lexical writes are evidence, not a runtime alias analysis."); });
            Portal(chambers, "BOUND", "EXTERNAL BOUNDARY MAP", "Process, network, filesystem, registry and database boundary signatures, without invoking any of them.", delegate { OpenComputed(w, "EXTERNAL BOUNDARY MAP", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.BoundaryMap(s).Cast<object>().ToList(); }, "Boundary detection is a fixed lexical vocabulary and intentionally conservative."); });
            Portal(chambers, "API", "PUBLIC SURFACE LAB", "Public/protected symbols and high fan-in entry points approximate the codebase contract surface.", delegate { PublicSurface(w); });
            Portal(chambers, "CFG", "CONFIG & SCHEMA SURFACE", "Configuration-shaped files, serialization contracts, XAML/XML/JSON and settings-heavy organs.", delegate { ConfigSurface(w); });
            Portal(chambers, "SCC", "CYCLE / SCC REACTOR", "Tarjan strongly connected components expose dependency cycles as explicit architectural knots.", delegate { OpenComputed(w, "CYCLE / SCC REACTOR", delegate(GenomeSnapshot s) { return s.Cycles.Cast<object>().ToList(); }, "Cycles are computed over resolved FILE_DEPENDS_ON edges only."); });
            Portal(chambers, "LAYER", "ARCHITECTURE LAYER LAB", "Directory-derived layers and cross-layer dependency counts reveal actual architecture versus intended architecture.", delegate { OpenComputed(w, "ARCHITECTURE LAYER LAB", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.ArchitectureLayers(s).Cast<object>().ToList(); }, "Layer names are inferred from the first repository-relative directory."); });
            Portal(chambers, "HUB", "CENTRALITY & HUBS", "Rank high fan-in/fan-out files and symbols: the organs through which disproportionate structure passes.", delegate { Hubs(w); });
            Portal(chambers, "ORPH", "ORPHAN / DEAD-CODE RADAR", "No resolved callers is a candidate signal, never a deletion instruction. Reflection and dynamic dispatch remain uncertainty.", delegate { OpenComputed(w, "ORPHAN / DEAD-CODE RADAR", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.OrphanCandidates(s).Cast<object>().ToList(); }, "Heuristic only: zero resolved callers does not prove dead code."); });
            Portal(chambers, "CLONE", "CLONE FINGERPRINT LAB", "Exact normalized symbol-body fingerprints reveal duplicate implementation bodies across the organism.", delegate { CloneLab(w); });
            Portal(chambers, "HOT", "HOTSPOT HEATMAP", "Combine complexity, dependency centrality, mutation density and history touches into a transparent hotspot ordering.", delegate { OpenComputed(w, "HOTSPOT HEATMAP", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.Hotspots(s).Cast<object>().ToList(); }, "The score is a prioritization heuristic; every component remains visible."); });
            Portal(chambers, "CX", "COMPLEXITY TOPOGRAPHY", "Rank files and symbols by lexical decision proxy and size without pretending it is semantic cyclomatic truth.", delegate { ComplexityLab(w); });
            Portal(chambers, "FAN", "FAN-IN / FAN-OUT MATRIX", "Find stable foundations, god objects, dependency magnets and sprawling callers.", delegate { FanMatrix(w); });
            Portal(chambers, "BLAST", "BLAST-RADIUS ENGINE", "Select a file or symbol and traverse reverse dependencies to estimate direct/transitive consequences and affected tests.", delegate { CreateImpact(w, ctx); });
            Portal(chambers, "CHANGE", "CHANGE SCENARIO CHAMBER", "Record MODIFY / DELETE / SIGNATURE / BEHAVIOR scenarios against a stable genome and produce consequence certificates.", delegate { ChangeScenario(w, ctx); });
            Portal(chambers, "TEST", "TEST COVERAGE TOPOLOGY", "Map test-shaped files to statically resolved production dependencies and expose blind regions.", delegate { OpenComputed(w, "TEST COVERAGE TOPOLOGY", delegate(GenomeSnapshot s) { return SoftwareGenomeKernel.TestMap(s).Cast<object>().ToList(); }, "This is dependency coverage, not executed line/branch coverage."); });
            Portal(chambers, "HIST", "HISTORY TIME MACHINE", "Optionally harvest up to 250 commits with a fixed read-only git log command; no hooks, checkout or project execution.", delegate { HarvestHistory(w, ctx); });
            Portal(chambers, "CHURN", "CHURN ARCHAEOLOGY", "Rank files by retained commit touches and cross them with structural complexity and centrality.", delegate { Churn(w); });
            Portal(chambers, "OWN", "OWNERSHIP ATLAS", "Historical author shares per file plus retained primary-owner evidence.", delegate { Ownership(w); });
            Portal(chambers, "CODEOWN", "CODEOWNERS TEMPLE", "Read repository CODEOWNERS rules directly when present and keep them separate from historical ownership.", delegate { CodeOwners(w); });
            Portal(chambers, "BUS", "BUS-FACTOR RADAR", "Surface files whose retained history is concentrated in one contributor.", delegate { BusFactor(w); });
            Portal(chambers, "CO", "CO-CHANGE GRAVITY", "Files repeatedly changing in the same commit reveal hidden coupling that static imports may miss.", delegate { CoChange(w); });
            Portal(chambers, "DRIFT", "ARCHITECTURE DRIFT RADAR", "Compare two retained genomes: files, symbols, hashes, dependencies, cycles and hotspot movement.", delegate { CompareSnapshots(w); });
            Portal(chambers, "BASE", "IMMUTABLE GENOME BASELINES", "Freeze exact scan fingerprints and structural counts before refactors or investigations.", delegate { Baselines(w, ctx); });
            Portal(chambers, "CAP", "PREREGISTRATION CAPSULES", "Write the question, prediction and falsifier before descending into the code evidence.", delegate { Capsules(w, ctx); });
            Portal(chambers, "DOS", "CHANGE DOSSIERS", "Bind subject, evidence, implications and uncertainty to the exact genome fingerprint.", delegate { Dossiers(w, ctx); });
            Portal(chambers, "PROJ", "CIVILIZATION PROJECTION INBOX", "Typed receipts from every earlier YOMI civilization preserve upstream identity and fingerprint without pretending models are source code.", delegate { OpenCollection(w, "CIVILIZATION PROJECTION INBOX", Store.Projections.Cast<object>().ToList(), "Projection receipts are contextual evidence only."); });
            Portal(chambers, "CERT", "EVIDENCE CERTIFICATE VAULT", "Snapshot, edge, cycle, impact, baseline, capsule and dossier fingerprints in one descendable evidence surface.", delegate { Certificates(w); });
            Portal(chambers, "LEDGER", "GENOME LEDGER", "Hash-chained structural events make scans, history harvests, baselines and impact reports tamper-evident.", delegate { Ledger(w, ctx); });
            Portal(chambers, "DESC", "HADAL CODE DESCENT", "Repository → file → symbol → callers → state writes → history → ownership → impact → dossier. Keep opening windows until the architecture stops being abstract.", delegate { HadalDescent(w, ctx); });
            Portal(chambers, "DATAΩ", "DATA FOUNDRY WORMHOLE", "Project a retained genome snapshot into provenance-bound structured data for cross-civilization analysis.", delegate { GenomeSnapshot s = ChooseSnapshot(w); if (s != null) DataFoundry.OpenFromSoftwareGenomeSnapshot(w, ctx, s); });
            Portal(chambers, "SECΩ", "SECURITY FORTRESS WORMHOLE", "Send the codebase structural surface to the defensive-security civilization as simulation evidence, not authority.", delegate { GenomeSnapshot s = ChooseSnapshot(w); if (s != null) AdversarialSecurityFortress.OpenFromSoftwareGenomeSnapshot(w, ctx, WorldKernel.ExternalIndex(ctx, false), s); });
            Portal(chambers, "DOCΩ", "LIVING DOCUMENT FACTORY", "Project a retained codebase genome into reports/specifications as fingerprinted architecture and change evidence.", delegate { GenomeSnapshot s = ChooseSnapshot(w); if (s != null) LivingDocumentIntelligenceFactory.OpenFromSoftwareGenomeSnapshot(w, ctx, s); });
            Portal(chambers, "EXPΩ", "EXPEDITION COMMAND", "Capture the retained codebase genome as a fingerprinted mission crossing with architecture, ownership and change evidence retained.", delegate { GenomeSnapshot s = ChooseSnapshot(w); if (s != null) GrandUnifiedExpeditionCommand.OpenFromSoftwareGenomeSnapshot(w, ctx, s); });
            Portal(chambers, "WORM", "RETURN WORMHOLES", "Cross directly into every preceding civilization without discarding the retained genome or its investigation state.", delegate { OpenReturnWormholes(w, ctx); });
            Portal(chambers, "ATLAS", "DEEP SYSTEMS ATLAS", "Return to the civilization lattice and cross into any other YOMI system.", delegate { DeepSystems.OpenAtlasFromContext(w); });
            scroll.Content = chambers; root.Children.Add(scroll); w.Content = root; w.Show();
        }

        public static void OpenFromWorldNode(Window owner, DeepSystemsContext ctx, WorldIndex index, string nodeId) { if (index == null || String.IsNullOrWhiteSpace(nodeId)) { Open(owner, ctx); return; } WorldNode source; if (!index.Nodes.TryGetValue(nodeId, out source)) { Open(owner, ctx); return; } Project(owner, ctx, "WORLD", source, nodeId, "World object supplied as contextual architecture evidence."); }
        public static void OpenFromCivilResolution(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilResolution source) { Project(owner, ctx, "CIVILIZATION", source, ReadId(source), "Civil resolution supplied as governance/context evidence."); }
        public static void OpenFromLogicTheory(Window owner, DeepSystemsContext ctx, WorldIndex index, LogicTheory source) { Project(owner, ctx, "EPISTEMIC_LOGIC", source, ReadId(source), "Theory supplied as assumptions/contract evidence."); }
        public static void OpenFromVerificationModel(Window owner, DeepSystemsContext ctx, WorldIndex index, VerificationModel source) { Project(owner, ctx, "VERIFICATION", source, ReadId(source), "Verification model supplied as behavioral-contract evidence."); }
        public static void OpenFromStrategyModel(Window owner, DeepSystemsContext ctx, WorldIndex index, StrategyModel source) { Project(owner, ctx, "STRATEGY", source, ReadId(source), "Strategy model supplied as change-scenario context."); }
        public static void OpenFromDiscoveryProtocol(Window owner, DeepSystemsContext ctx, WorldIndex index, DiscoveryProtocol source) { Project(owner, ctx, "SCIENTIFIC_DISCOVERY", source, ReadId(source), "Discovery protocol supplied as investigation context."); }
        public static void OpenFromEngineeringArchitecture(Window owner, DeepSystemsContext ctx, WorldIndex index, EngineeringArchitecture source) { Project(owner, ctx, "SYSTEMS_ENGINEERING", source, ReadId(source), "Engineering architecture supplied as intended-architecture evidence."); }
        public static void OpenFromCyberneticTwin(Window owner, DeepSystemsContext ctx, WorldIndex index, CyberTwinModel source) { Project(owner, ctx, "CYBERNETIC_TWIN", source, ReadId(source), "Digital twin supplied as runtime/control-context evidence."); }
        public static void OpenFromComplexSystem(Window owner, DeepSystemsContext ctx, WorldIndex index, ComplexSystemModel source) { Project(owner, ctx, "COMPLEX_SYSTEMS", source, ReadId(source), "Complex-system model supplied as emergence/context evidence."); }
        public static void OpenFromOperationsResearchModel(Window owner, DeepSystemsContext ctx, WorldIndex index, OperationsResearchModel source) { Project(owner, ctx, "OPERATIONS_RESEARCH", source, ReadId(source), "Optimization model supplied as algorithmic/context evidence."); }
        public static void OpenFromEconomicModel(Window owner, DeepSystemsContext ctx, WorldIndex index, EconomicModel source) { Project(owner, ctx, "ECONOMIC_CIVILIZATION", source, ReadId(source), "Economic model supplied as domain/data context."); }
        public static void OpenFromSpatialWorld(Window owner, DeepSystemsContext ctx, WorldIndex index, SpatialWorldModel source) { Project(owner, ctx, "SPATIAL_CARTOGRAPHY", source, ReadId(source), "Spatial model supplied as topology/context evidence."); }
        public static void OpenFromDistributedSystem(Window owner, DeepSystemsContext ctx, WorldIndex index, DistributedSystemModel source) { Project(owner, ctx, "DISTRIBUTED_SYSTEMS", source, ReadId(source), "Distributed-system model supplied as service/dependency context."); }
        public static void OpenFromSecurityModel(Window owner, DeepSystemsContext ctx, WorldIndex index, SecurityModel source) { Project(owner, ctx, "ADVERSARIAL_SECURITY", source, ReadId(source), "Security model supplied as trust-boundary/control evidence."); }
        public static void OpenFromDataDataset(Window owner, DeepSystemsContext ctx, DataDataset source) { Project(owner, ctx, "DATA_FOUNDRY", source, ReadId(source), "Dataset supplied as schema/data-contract context, not code."); }
        public static void OpenFromLivingDocumentVersion(Window owner, DeepSystemsContext ctx, LivingDocument document, LivingVersion source) { Project(owner, ctx, "LIVING_DOCUMENT", source, source == null ? "UNKNOWN" : source.Id, "Living report/specification supplied as requirements, assumptions and documentary change context; it is never interpreted as executable source."); }

        private static void Project(Window owner, DeepSystemsContext ctx, string civilization, object source, string sourceId, string rule)
        {
            if (ctx == null || source == null) return; ExpeditionWorkspace.Visit(ctx, "genome", civilization + " projection", rule); Ensure(ctx); string fp = HashObject(source); GenomeProjectionReceipt r = new GenomeProjectionReceipt { Id = SoftwareGenomeKernel.NewId("PROJ"), Civilization = civilization, SourceId = String.IsNullOrWhiteSpace(sourceId) ? "UNKNOWN" : sourceId, SourceFingerprint = fp, Rule = rule }; r.CertificateHash = SoftwareGenomeKernel.HashText(r.Civilization + "|" + r.SourceId + "|" + r.SourceFingerprint + "|" + r.Rule); Store.Projections.Insert(0, r); Trim(Store.Projections, 2048); Save(ctx); AppendLedger(ctx, "PROJECTION", r.SourceId, r.Civilization + " → GENOMEΩ", "", r.CertificateHash); Open(owner, ctx);
        }

        private static void ScanRepository(Window owner, DeepSystemsContext ctx)
        {
            using (Forms.FolderBrowserDialog dlg = new Forms.FolderBrowserDialog())
            {
                dlg.Description = "Choose a codebase root. YOMI will statically read source text and metadata only; it will not execute, build or load the project."; dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog() != Forms.DialogResult.OK || String.IsNullOrWhiteSpace(dlg.SelectedPath)) return; string name = Prompt(owner, "GENOME NAME", "Retained name for this codebase snapshot:", new DirectoryInfo(dlg.SelectedPath).Name); if (name == null) return;
                try { GenomeSnapshot s = SoftwareGenomeKernel.Scan(dlg.SelectedPath, name, new GenomeScanOptions()); Store.Snapshots.Insert(0, s); Trim(Store.Snapshots, MaxSnapshots); Save(ctx); AppendLedger(ctx, "SCAN", s.Id, s.Files.Count + " files; " + s.Symbols.Count + " symbols; " + s.Edges.Count + " edges; " + s.Cycles.Count + " cycles", s.RootFingerprint, ""); OpenSnapshot(owner, ctx, s); }
                catch (Exception ex) { MessageBox.Show(owner, ex.Message, "YOMI Software Genome · Scan failed", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private static void OpenSnapshots(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · GENOME REGISTRY", 1450, 880); DockPanel root = Shell("GENOME REGISTRY", "Double-click a retained codebase organism to descend into it."); ListBox list = List(); list.ItemsSource = Store.Snapshots; list.MouseDoubleClick += delegate { GenomeSnapshot s = list.SelectedItem as GenomeSnapshot; if (s != null) OpenSnapshot(w, ctx, s); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenSnapshot(Window owner, DeepSystemsContext ctx, GenomeSnapshot s)
        {
            Window w = W(owner, "YOMI · GENOME · " + s.Name, 1480, 900); DockPanel root = Shell(s.Name, s.RootPath ?? "Synthetic genome"); TextBlock meta = new TextBlock { Text = "id " + s.Id + "   ·   fp " + SoftwareGenomeKernel.Short(s.RootFingerprint) + "   ·   " + s.Files.Count + " files   ·   " + s.Symbols.Count + " symbols   ·   " + s.Edges.Count + " edges   ·   " + s.Cycles.Count + " cycles   ·   history " + s.History.Count + " commits", Foreground = Accent, Margin = new Thickness(20, 0, 20, 12), TextWrapping = TextWrapping.Wrap }; DockPanel.SetDock(meta, Dock.Top); root.Children.Add(meta); WrapPanel p = new WrapPanel { Margin = new Thickness(14) };
            SmallPortal(p, "FILES", delegate { OpenCollection(w, "FILES · " + s.Name, s.Files.Cast<object>().ToList(), s.RootFingerprint); }); SmallPortal(p, "SYMBOLS", delegate { OpenCollection(w, "SYMBOLS · " + s.Name, s.Symbols.Cast<object>().ToList(), s.RootFingerprint); }); SmallPortal(p, "DEPENDENCIES", delegate { OpenCollection(w, "DEPENDENCIES · " + s.Name, s.Edges.Where(x => x.Kind == "FILE_DEPENDS_ON").Cast<object>().ToList(), s.RootFingerprint); }); SmallPortal(p, "CALLS", delegate { OpenCollection(w, "CALLS · " + s.Name, s.Edges.Where(x => x.Kind == "CALLS").Cast<object>().ToList(), s.RootFingerprint); }); SmallPortal(p, "FINDINGS", delegate { OpenCollection(w, "FINDINGS · " + s.Name, s.Findings.Cast<object>().ToList(), s.RootFingerprint); }); SmallPortal(p, "IMPACT", delegate { CreateImpact(w, ctx, s); }); SmallPortal(p, "HISTORY", delegate { HarvestHistory(w, ctx, s); }); SmallPortal(p, "BASELINE", delegate { CreateBaseline(w, ctx, s); }); SmallPortal(p, "DATAΩ", delegate { DataFoundry.OpenFromSoftwareGenomeSnapshot(w, ctx, s); }); SmallPortal(p, "WORLD SPINE", delegate { WorldPassportSpine.CaptureAndOpen(w, ctx, s.Name, "SOFTWARE_GENOME", "GENOMEΩ", "genome", s.Id, s, s.RootPath, "$genome"); });
            root.Children.Add(new ScrollViewer { Content = p, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); w.Content = root; w.Show();
        }

        private static void OpenFiles(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, "FILE ORGANISM · " + s.Name, s.Files.OrderByDescending(x => x.FanIn + x.FanOut).Cast<object>().ToList(), "Every file is content-addressed by SHA-256."); }
        private static void OpenSymbols(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, "SYMBOL MICROSCOPE · " + s.Name, s.Symbols.OrderByDescending(x => x.FanIn + x.FanOut).Cast<object>().ToList(), "Symbol extraction is static and language-specific; dynamic/reflection edges may be absent."); }
        private static void GeneFinder(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; string q = Prompt(owner, "GENE FINDER", "Name/path fragment:", ""); if (q == null) return; List<object> rows = new List<object>(); rows.AddRange(s.Symbols.Where(x => Contains(x.Name, q) || Contains(x.QualifiedName, q)).Take(1000).Cast<object>()); rows.AddRange(s.Files.Where(x => Contains(x.RelativePath, q)).Take(500).Cast<object>()); OpenCollection(owner, "GENE FINDER · " + q, rows, "Metadata search over retained scan; source bytes are not rescanned."); }
        private static void OpenModules(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = new List<object>(); foreach (GenomeFile f in s.Files.OrderBy(x => x.Layer).ThenBy(x => x.RelativePath)) rows.Add(f.Layer + "   ·   " + f.RelativePath + "   ·   imports " + (f.Imports.Count == 0 ? "∅" : String.Join(", ", f.Imports.Take(12).ToArray()))); OpenCollection(owner, "MODULE / NAMESPACE ATLAS · " + s.Name, rows, "Imports are language-specific lexical evidence."); }
        private static void OpenEdges(Window owner, string kind, string title) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, title + " · " + s.Name, s.Edges.Where(x => x.Kind == kind).Cast<object>().ToList(), "Resolved edges disclose confidence and evidence."); }
        private static void ReverseDependencies(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; GenomeFile f = ChooseFile(owner, s); if (f == null) return; List<object> rows = s.Edges.Where(x => x.Kind == "FILE_DEPENDS_ON" && x.ToId == f.Id).Select(x => (object)x).ToList(); OpenCollection(owner, "REVERSE DEPENDENCIES · " + f.RelativePath, rows, "Direct reverse edges only. Use Blast Radius for transitive consequence traversal."); }
        private static void OpenDataFlows(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, "DATA-FLOW RIVER MAP · " + s.Name, s.DataFlows.Cast<object>().ToList(), "Static writes/emits only; this is not runtime taint tracking."); }
        private static void PublicSurface(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = s.Symbols.Where(x => String.Equals(x.Visibility, "public", StringComparison.OrdinalIgnoreCase) || String.Equals(x.Visibility, "protected", StringComparison.OrdinalIgnoreCase) || x.FanIn >= 5).OrderByDescending(x => x.FanIn).Cast<object>().ToList(); OpenCollection(owner, "PUBLIC SURFACE LAB · " + s.Name, rows, "Public/protected declarations and dependency magnets approximate the contract surface."); }
        private static void ConfigSurface(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = s.Files.Where(x => { string e = Path.GetExtension(x.RelativePath).ToLowerInvariant(); string p = x.RelativePath.ToLowerInvariant(); return e == ".json" || e == ".yaml" || e == ".yml" || e == ".toml" || e == ".ini" || e == ".config" || e == ".xaml" || e == ".xml" || p.Contains("config") || p.Contains("setting") || p.Contains("schema"); }).Cast<object>().ToList(); OpenCollection(owner, "CONFIG & SCHEMA SURFACE · " + s.Name, rows, "Configuration-shaped files are metadata candidates, not guaranteed runtime contracts."); }
        private static void Hubs(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = new List<object>(); rows.AddRange(s.Files.OrderByDescending(x => x.FanIn + x.FanOut).Take(500).Cast<object>()); rows.AddRange(s.Symbols.OrderByDescending(x => x.FanIn + x.FanOut).Take(1000).Cast<object>()); OpenCollection(owner, "CENTRALITY & HUBS · " + s.Name, rows, "Degree centrality over resolved edges."); }
        private static void CloneLab(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = s.Symbols.Where(x => x.Lines >= 5).GroupBy(x => x.BodyFingerprint).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).Select(g => (object)(g.Count() + " exact normalized bodies   ·   " + SoftwareGenomeKernel.Short(g.Key) + "   ·   " + String.Join(" | ", g.Take(8).Select(x => x.QualifiedName).ToArray()))).ToList(); OpenCollection(owner, "CLONE FINGERPRINT LAB · " + s.Name, rows, "Exact normalized-body matches only; near-clones are not claimed."); }
        private static void ComplexityLab(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = new List<object>(); rows.AddRange(s.Symbols.OrderByDescending(x => x.ComplexityProxy).Take(1500).Cast<object>()); rows.AddRange(s.Files.OrderByDescending(x => x.ComplexityProxy).Take(800).Cast<object>()); OpenCollection(owner, "COMPLEXITY TOPOGRAPHY · " + s.Name, rows, "Lexical branch/decision proxy, not semantic cyclomatic complexity."); }
        private static void FanMatrix(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = s.Files.OrderByDescending(x => x.FanIn * 2 + x.FanOut).Select(x => (object)(x.RelativePath + "   ·   fan-in " + x.FanIn + "   ·   fan-out " + x.FanOut + "   ·   ratio " + (x.FanOut == 0 ? "∞" : ((double)x.FanIn / x.FanOut).ToString("0.00", CultureInfo.InvariantCulture)))).ToList(); OpenCollection(owner, "FAN-IN / FAN-OUT MATRIX · " + s.Name, rows, "Resolved file dependency degree."); }

        private static void CreateImpact(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) CreateImpact(owner, ctx, s); }
        private static void CreateImpact(Window owner, DeepSystemsContext ctx, GenomeSnapshot s) { GenomeFile f = ChooseFile(owner, s); if (f == null) return; GenomeImpactReport r = SoftwareGenomeKernel.Impact(s, f.Id, "MODIFY"); Store.Impacts.Insert(0, r); Trim(Store.Impacts, 1024); Save(ctx); AppendLedger(ctx, "IMPACT", r.SubjectId, r.ToString(), s.RootFingerprint, r.CertificateHash); List<object> rows = new List<object> { r }; rows.AddRange(r.Reasons.Cast<object>()); rows.AddRange(r.Impacted.Cast<object>()); OpenCollection(owner, "BLAST RADIUS · " + f.RelativePath, rows, "Reverse-dependency consequence estimate. Dynamic dispatch/reflection/build-time generation can create additional edges."); }
        private static void ChangeScenario(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; GenomeFile f = ChooseFile(owner, s); if (f == null) return; string kind = Prompt(owner, "CHANGE SCENARIO", "Change kind: MODIFY, SIGNATURE, DELETE, BEHAVIOR, CONFIG", "MODIFY"); if (kind == null) return; GenomeImpactReport r = SoftwareGenomeKernel.Impact(s, f.Id, kind.Trim().ToUpperInvariant()); Store.Impacts.Insert(0, r); Trim(Store.Impacts, 1024); Save(ctx); AppendLedger(ctx, "CHANGE_SCENARIO", f.Id, kind, s.RootFingerprint, r.CertificateHash); OpenCollection(owner, "CHANGE SCENARIO · " + f.RelativePath, new List<object> { r }.Concat(r.Reasons.Cast<object>()).Concat(r.Impacted.Cast<object>()).ToList(), "Scenario is analytical only; no source file is edited."); }

        private static void HarvestHistory(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) HarvestHistory(owner, ctx, s); }
        private static void HarvestHistory(Window owner, DeepSystemsContext ctx, GenomeSnapshot s)
        {
            MessageBoxResult ok = MessageBox.Show(owner, "Harvest up to 250 commits using a fixed read-only git log command?\n\nYOMI will not checkout, reset, merge, build, execute project code, or invoke hooks.", "YOMI · History Time Machine", MessageBoxButton.OKCancel, MessageBoxImage.Information); if (ok != MessageBoxResult.OK) return;
            try { SoftwareGenomeKernel.HarvestGitHistory(s, 250); Save(ctx); AppendLedger(ctx, "HISTORY_HARVEST", s.Id, s.History.Count + " commits; " + s.Ownership.Count + " ownership records; " + s.CoChanges.Count + " co-change pairs", s.RootFingerprint, SoftwareGenomeKernel.HashText(String.Join("|", s.History.Select(x => x.Sha).ToArray()))); OpenCollection(owner, "HISTORY TIME MACHINE · " + s.Name, s.History.Cast<object>().ToList(), "Read-only git metadata only."); } catch (Exception ex) { MessageBox.Show(owner, ex.Message, "YOMI · History harvest", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        private static void Churn(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; OpenCollection(owner, "CHURN ARCHAEOLOGY · " + s.Name, s.Files.OrderByDescending(x => x.HistoryTouches).ThenByDescending(x => x.ComplexityProxy).Cast<object>().ToList(), s.History.Count == 0 ? "Harvest history first to populate touches." : "History touch count is commit presence, not line churn."); }
        private static void Ownership(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, "OWNERSHIP ATLAS · " + s.Name, s.Ownership.OrderBy(x => x.FilePath).ThenByDescending(x => x.Touches).Cast<object>().ToList(), s.History.Count == 0 ? "Harvest history first." : "Historical authorship is evidence, not organizational authority."); }
        private static void CodeOwners(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; try { OpenCollection(owner, "CODEOWNERS TEMPLE · " + s.Name, SoftwareGenomeKernel.CodeOwners(s).Cast<object>().ToList(), "Literal CODEOWNERS rules; matching precedence is not expanded in this chamber."); } catch (Exception ex) { MessageBox.Show(owner, ex.Message, "YOMI", MessageBoxButton.OK, MessageBoxImage.Warning); } }
        private static void BusFactor(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; List<object> rows = s.Ownership.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(g => g.OrderByDescending(x => x.Share).First()).Where(x => x.Share >= 0.75 && x.Touches >= 2).OrderByDescending(x => x.Share).ThenByDescending(x => x.Touches).Cast<object>().ToList(); OpenCollection(owner, "BUS-FACTOR RADAR · " + s.Name, rows, "A dominant historical author share is concentration evidence, not a judgment about maintainability or people."); }
        private static void CoChange(Window owner) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, "CO-CHANGE GRAVITY · " + s.Name, s.CoChanges.OrderByDescending(x => x.CommitsTogether).ThenByDescending(x => x.Jaccard).Cast<object>().ToList(), "Capped to 80 files per commit when constructing pair evidence."); }

        private static void CompareSnapshots(Window owner)
        {
            GenomeSnapshot a = ChooseSnapshot(owner, "Choose OLDER / BASE genome"); if (a == null) return; GenomeSnapshot b = ChooseSnapshot(owner, "Choose NEWER / COMPARE genome"); if (b == null) return; Dictionary<string, GenomeFile> af = a.Files.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase), bf = b.Files.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase); List<object> rows = new List<object>(); foreach (string p in bf.Keys.Except(af.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) rows.Add("ADDED FILE   ·   " + p); foreach (string p in af.Keys.Except(bf.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) rows.Add("REMOVED FILE   ·   " + p); foreach (string p in af.Keys.Intersect(bf.Keys, StringComparer.OrdinalIgnoreCase).Where(p => !String.Equals(af[p].Sha256, bf[p].Sha256, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x)) rows.Add("CHANGED FILE   ·   " + p + "   ·   " + SoftwareGenomeKernel.Short(af[p].Sha256) + " → " + SoftwareGenomeKernel.Short(bf[p].Sha256)); rows.Insert(0, "SUMMARY   ·   files " + a.Files.Count + " → " + b.Files.Count + "   ·   symbols " + a.Symbols.Count + " → " + b.Symbols.Count + "   ·   edges " + a.Edges.Count + " → " + b.Edges.Count + "   ·   SCCs " + a.Cycles.Count + " → " + b.Cycles.Count); OpenCollection(owner, "ARCHITECTURE DRIFT · " + a.Name + " → " + b.Name, rows, "Snapshot comparison is content-addressed; unchanged hashes remain unchanged evidence.");
        }

        private static void Baselines(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; MessageBoxResult r = MessageBox.Show(owner, "Create an immutable baseline for " + s.Name + "?", "YOMI · Genome Baseline", MessageBoxButton.YesNo, MessageBoxImage.Question); if (r == MessageBoxResult.Yes) CreateBaseline(owner, ctx, s); else OpenCollection(owner, "IMMUTABLE GENOME BASELINES", Store.Baselines.Cast<object>().ToList(), "Baselines bind counts to exact snapshot fingerprints."); }
        private static void CreateBaseline(Window owner, DeepSystemsContext ctx, GenomeSnapshot s) { GenomeBaseline b = new GenomeBaseline { Id = SoftwareGenomeKernel.NewId("BASE"), SnapshotId = s.Id, SnapshotFingerprint = s.RootFingerprint, Name = s.Name + " · " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture), CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), Files = s.Files.Count, Symbols = s.Symbols.Count, Edges = s.Edges.Count, Cycles = s.Cycles.Count }; b.CertificateHash = SoftwareGenomeKernel.HashText(b.SnapshotId + "|" + b.SnapshotFingerprint + "|" + b.Files + "|" + b.Symbols + "|" + b.Edges + "|" + b.Cycles); Store.Baselines.Insert(0, b); Trim(Store.Baselines, 512); Save(ctx); AppendLedger(ctx, "BASELINE", b.Id, b.ToString(), s.RootFingerprint, b.CertificateHash); OpenCollection(owner, "BASELINE CREATED", new List<object> { b }, "Immutable structural checkpoint."); }
        private static void Capsules(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; string q = Prompt(owner, "PREREGISTRATION", "Question to investigate:", "Which organ creates the largest change blast radius?"); if (q == null) return; string p = Prompt(owner, "PREREGISTRATION", "Prediction before examining evidence:", ""); if (p == null) return; string f = Prompt(owner, "PREREGISTRATION", "What observation would falsify or materially weaken the prediction?", ""); if (f == null) return; GenomeCapsule c = new GenomeCapsule { Id = SoftwareGenomeKernel.NewId("CAP"), SnapshotId = s.Id, Question = q, Prediction = p, Falsifier = f, CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), SnapshotFingerprint = s.RootFingerprint }; c.CertificateHash = SoftwareGenomeKernel.HashText(c.SnapshotId + "|" + c.SnapshotFingerprint + "|" + c.Question + "|" + c.Prediction + "|" + c.Falsifier); Store.Capsules.Insert(0, c); Trim(Store.Capsules, 1024); Save(ctx); AppendLedger(ctx, "CAPSULE", c.Id, c.Question, s.RootFingerprint, c.CertificateHash); OpenCollection(owner, "PREREGISTRATION CAPSULE", new List<object> { c }, "Question/prediction/falsifier frozen before further descent."); }
        private static void Dossiers(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; GenomeFile f = ChooseFile(owner, s); if (f == null) return; GenomeImpactReport impact = SoftwareGenomeKernel.Impact(s, f.Id, "DOSSIER"); GenomeDossier d = new GenomeDossier { Id = SoftwareGenomeKernel.NewId("DOS"), SnapshotId = s.Id, SubjectId = f.Id, Title = "Change dossier · " + f.RelativePath }; d.Evidence.Add(f.ToString()); d.Evidence.Add(impact.ToString()); d.Evidence.AddRange(s.Findings.Where(x => x.SubjectId == f.Id).Take(20).Select(x => x.ToString())); d.Implications.AddRange(impact.Reasons); d.Uncertainties.Add("Static analysis cannot fully resolve reflection, dynamic dispatch, generated code, runtime configuration or external build steps."); d.Uncertainties.Add("Git history, when harvested, records commit touches rather than semantic authorship or line-level blame."); d.CertificateHash = SoftwareGenomeKernel.HashText(s.RootFingerprint + "|" + d.SubjectId + "|" + String.Join("|", d.Evidence.ToArray()) + "|" + String.Join("|", d.Implications.ToArray())); Store.Dossiers.Insert(0, d); Trim(Store.Dossiers, 1024); Save(ctx); AppendLedger(ctx, "DOSSIER", d.Id, d.Title, s.RootFingerprint, d.CertificateHash); OpenCollection(owner, "CHANGE DOSSIER · " + f.RelativePath, new List<object> { d }.Concat(d.Evidence.Cast<object>()).Concat(d.Implications.Cast<object>()).Concat(d.Uncertainties.Cast<object>()).ToList(), "Evidence and uncertainty remain adjacent."); }
        private static void Certificates(Window owner) { List<object> rows = new List<object>(); foreach (GenomeSnapshot s in Store.Snapshots) rows.Add("SNAPSHOT   ·   " + s.Name + "   ·   " + s.RootFingerprint); rows.AddRange(Store.Impacts.Select(x => (object)("IMPACT   ·   " + x.Subject + "   ·   " + x.CertificateHash))); rows.AddRange(Store.Baselines.Select(x => (object)("BASELINE   ·   " + x.Name + "   ·   " + x.CertificateHash))); rows.AddRange(Store.Capsules.Select(x => (object)("CAPSULE   ·   " + x.Question + "   ·   " + x.CertificateHash))); rows.AddRange(Store.Dossiers.Select(x => (object)("DOSSIER   ·   " + x.Title + "   ·   " + x.CertificateHash))); OpenCollection(owner, "EVIDENCE CERTIFICATE VAULT", rows, "Content-addressed evidence roots for reproducible descent."); }
        private static void Ledger(Window owner, DeepSystemsContext ctx) { Ensure(ctx); List<object> rows = ReadLedger(ctx).Cast<object>().ToList(); OpenCollection(owner, "GENOME LEDGER", rows, VerifyLedger(rows.Cast<GenomeLedgerRecord>().ToList()) ? "HASH CHAIN: VERIFIED over retained ledger window." : "HASH CHAIN: ATTENTION — retained window does not verify."); }
        private static void HadalDescent(Window owner, DeepSystemsContext ctx) { GenomeSnapshot s = ChooseSnapshot(owner); if (s == null) return; GenomeFile f = ChooseFile(owner, s); if (f == null) return; List<object> rows = new List<object> { s, f }; rows.AddRange(s.Symbols.Where(x => x.FileId == f.Id).Cast<object>()); rows.AddRange(s.Edges.Where(x => x.FromId == f.Id || x.ToId == f.Id || s.Symbols.Where(z => z.FileId == f.Id).Select(z => z.Id).Contains(x.FromId) || s.Symbols.Where(z => z.FileId == f.Id).Select(z => z.Id).Contains(x.ToId)).Take(500).Cast<object>()); rows.AddRange(s.DataFlows.Where(x => x.FileId == f.Id).Take(500).Cast<object>()); rows.AddRange(s.Ownership.Where(x => String.Equals(x.FilePath, f.RelativePath, StringComparison.OrdinalIgnoreCase)).Cast<object>()); GenomeImpactReport impact = SoftwareGenomeKernel.Impact(s, f.Id, "HADAL_DESCENT"); rows.Add(impact); rows.AddRange(impact.Impacted.Take(100).Cast<object>()); OpenCollection(owner, "HADAL CODE DESCENT · " + f.RelativePath, rows, "One organ, all retained layers of evidence. Open parallel chambers to keep descending."); }

        private static void OpenReturnWormholes(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · GENOMEΩ RETURN WORMHOLES", 1180, 780); DockPanel root = Shell("RETURN WORMHOLES", "The Software Genome remains retained while you cross into another civilization."); WrapPanel p = new WrapPanel { Margin = new Thickness(14) };
            SmallPortal(p, "WORLD KERNEL", delegate { WorldKernel.Open(w, ctx); });
            SmallPortal(p, "CIVILIZATION LAYER", delegate { CivilizationLayer.Open(w, ctx); });
            SmallPortal(p, "EPISTEMIC LOGIC", delegate { EpistemicLogicFoundry.Open(w, ctx); });
            SmallPortal(p, "VERIFICATION REACTOR", delegate { AxiomaticVerificationReactor.Open(w, ctx); });
            SmallPortal(p, "COUNTERFACTUAL STRATEGY", delegate { CounterfactualStrategySuperstructure.Open(w, ctx); });
            SmallPortal(p, "SCIENTIFIC DISCOVERY", delegate { ScientificDiscoveryHyperstructure.Open(w, ctx); });
            SmallPortal(p, "SYSTEMS ENGINEERING", delegate { DysonSystemsEngineeringMegastructure.Open(w, ctx); });
            SmallPortal(p, "CYBERNETIC TWIN", delegate { CyberneticDigitalTwinMetastructure.Open(w, ctx); });
            SmallPortal(p, "COMPLEX SYSTEMS", delegate { ComplexSystemsLaboratory.Open(w, ctx); });
            SmallPortal(p, "OPERATIONS RESEARCH", delegate { OperationsResearchEmpire.Open(w, ctx); });
            SmallPortal(p, "ECONOMIC CIVILIZATION", delegate { EconomicCivilization.Open(w, ctx); });
            SmallPortal(p, "SPATIAL CARTOGRAPHY", delegate { SpatialWorldCartography.Open(w, ctx); });
            SmallPortal(p, "DISTRIBUTED SYSTEMS", delegate { DistributedSystemsPlanetarium.Open(w, ctx); });
            SmallPortal(p, "SECURITY FORTRESS", delegate { AdversarialSecurityFortress.Open(w, ctx); });
            SmallPortal(p, "DATA FOUNDRY", delegate { DataFoundry.Open(w, ctx); });
            root.Children.Add(new ScrollViewer { Content = p, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); w.Content = root; w.Show();
        }

        private static void OpenComputed(Window owner, string title, Func<GenomeSnapshot, List<object>> fn, string note) { GenomeSnapshot s = ChooseSnapshot(owner); if (s != null) OpenCollection(owner, title + " · " + s.Name, fn(s), note); }

        private static GenomeSnapshot ChooseSnapshot(Window owner, string title = "Choose genome") { EnsureLoaded(); if (Store.Snapshots.Count == 0) return null; return Choose<GenomeSnapshot>(owner, title, Store.Snapshots); }
        private static GenomeFile ChooseFile(Window owner, GenomeSnapshot s) { if (s == null || s.Files.Count == 0) return null; return Choose<GenomeFile>(owner, "Choose file organ", s.Files.OrderBy(x => x.RelativePath).ToList()); }
        private static T Choose<T>(Window owner, string title, IList<T> items) where T : class { Window w = W(owner, "YOMI · " + title.ToUpperInvariant(), 980, 680); DockPanel root = Shell(title.ToUpperInvariant(), "Double-click to select."); ListBox list = List(); list.ItemsSource = items; T selected = null; list.MouseDoubleClick += delegate { selected = list.SelectedItem as T; w.DialogResult = selected != null; w.Close(); }; root.Children.Add(list); w.Content = root; bool? r = w.ShowDialog(); return r == true ? selected : null; }
        private static bool Contains(string a, string q) { return !String.IsNullOrWhiteSpace(a) && a.IndexOf(q ?? "", StringComparison.OrdinalIgnoreCase) >= 0; }

        private static void SeedStarter(DeepSystemsContext ctx)
        {
            GenomeSnapshot s = new GenomeSnapshot { Id = SoftwareGenomeKernel.NewId("GENOME"), Name = "Helix Station", RootPath = "SYNTHETIC://HELIX-STATION" };
            string[] names = { "Core/Kernel.cs", "Domain/Model.cs", "Services/Planner.cs", "Persistence/Store.cs", "UI/Controller.cs", "Tests/PlannerTests.cs" }; foreach (string n in names) s.Files.Add(new GenomeFile { Id = "FILE-" + SoftwareGenomeKernel.HashText(n).Substring(0, 16).ToUpperInvariant(), RelativePath = n, Language = "C#", Sha256 = SoftwareGenomeKernel.HashText("starter|" + n), Lines = 80 + n.Length, NonBlankLines = 70, Layer = n.Split('/')[0].ToUpperInvariant(), IsTest = n.Contains("Tests"), ComplexityProxy = 8 + n.Length % 12, MutationSites = 4 + n.Length % 7 });
            for (int i = 0; i < s.Files.Count; i++) { GenomeFile f = s.Files[i]; GenomeSymbol sym = new GenomeSymbol { Id = "SYM-" + SoftwareGenomeKernel.HashText(f.Id + "|Run").Substring(0, 16).ToUpperInvariant(), FileId = f.Id, FilePath = f.RelativePath, Name = i == 2 ? "Plan" : "Run", QualifiedName = f.RelativePath + "::" + (i == 2 ? "Plan" : "Run"), Kind = "METHOD", Visibility = "public", StartLine = 12, EndLine = 42, Lines = 31, ComplexityProxy = 5 + i, BodyFingerprint = SoftwareGenomeKernel.HashText("starter-body-" + i) }; s.Symbols.Add(sym); f.SymbolCount = 1; }
            Action<int,int> edge = delegate(int a, int b) { GenomeFile x = s.Files[a], y = s.Files[b]; s.Edges.Add(new GenomeEdge { Id = SoftwareGenomeKernel.NewId("EDGE"), FromId = x.Id, ToId = y.Id, FromLabel = x.RelativePath, ToLabel = y.RelativePath, Kind = "FILE_DEPENDS_ON", Evidence = "synthetic starter dependency", Confidence = 1.0, CertificateHash = SoftwareGenomeKernel.HashText(x.Id + "|" + y.Id) }); x.FanOut++; y.FanIn++; }; edge(2,1); edge(2,0); edge(3,1); edge(4,2); edge(4,3); edge(5,2); s.RootFingerprint = SoftwareGenomeKernel.Fingerprint(s); Store.Snapshots.Add(s); Save(ctx); AppendLedger(ctx, "GENESIS", s.Id, "Helix Station starter genome", s.RootFingerprint, "");
        }

        private static void Ensure(DeepSystemsContext ctx) { lock (Gate) { string root = ctx.DataRoot ?? ""; if (Store != null && String.Equals(LoadedRoot, root, StringComparison.OrdinalIgnoreCase)) return; LoadedRoot = root; Store = Load(ctx); } }
        private static void EnsureLoaded() { if (Store == null) Store = new SoftwareGenomeStore(); }
        private static string StorePath(DeepSystemsContext ctx) { string d = Path.Combine(ctx.DataRoot, "deep-systems"); Directory.CreateDirectory(d); return Path.Combine(d, "controller-software-genome.json"); }
        private static string LedgerPath(DeepSystemsContext ctx) { string d = Path.Combine(ctx.DataRoot, "deep-systems"); Directory.CreateDirectory(d); return Path.Combine(d, "controller-software-genome-ledger.jsonl"); }
        private static SoftwareGenomeStore Load(DeepSystemsContext ctx) { string p = StorePath(ctx), prev = p + ".previous"; foreach (string q in new[] { p, prev }) { if (!File.Exists(q)) continue; try { SoftwareGenomeStore s = Json.Deserialize<SoftwareGenomeStore>(File.ReadAllText(q, Encoding.UTF8)); if (s != null && s.Schema <= 1) { Normalize(s); return s; } } catch { try { File.Copy(q, q + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), true); } catch { } } } return new SoftwareGenomeStore(); }
        private static void Normalize(SoftwareGenomeStore s) { if (s.Snapshots == null) s.Snapshots = new List<GenomeSnapshot>(); if (s.Impacts == null) s.Impacts = new List<GenomeImpactReport>(); if (s.Projections == null) s.Projections = new List<GenomeProjectionReceipt>(); if (s.Baselines == null) s.Baselines = new List<GenomeBaseline>(); if (s.Capsules == null) s.Capsules = new List<GenomeCapsule>(); if (s.Dossiers == null) s.Dossiers = new List<GenomeDossier>(); }
        private static void Save(DeepSystemsContext ctx) { lock (Gate) { Normalize(Store); Store.Revision++; Store.UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Trim(Store.Snapshots, MaxSnapshots); string p = StorePath(ctx), next = p + ".next", prev = p + ".previous"; File.WriteAllText(next, Json.Serialize(Store), new UTF8Encoding(false)); if (File.Exists(p)) { try { File.Copy(p, prev, true); } catch { } } if (File.Exists(p)) File.Delete(p); File.Move(next, p); } }
        private static void Trim<T>(List<T> xs, int n) { if (xs == null) return; while (xs.Count > n) xs.RemoveAt(xs.Count - 1); }

        private static void AppendLedger(DeepSystemsContext ctx, string ev, string subject, string detail, string snapshotFp, string cert)
        {
            lock (Gate) { List<GenomeLedgerRecord> old = ReadLedger(ctx); long seq = old.Count == 0 ? 1 : old[old.Count - 1].Sequence + 1; string prev = old.Count == 0 ? new string('0', 64) : old[old.Count - 1].EntryHash; GenomeLedgerRecord r = new GenomeLedgerRecord { Schema = 1, Sequence = seq, Utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), Event = ev, Subject = subject, Detail = detail, SnapshotFingerprint = snapshotFp, CertificateHash = cert, PrevHash = prev }; r.EntryHash = SoftwareGenomeKernel.HashText(r.Schema + "|" + r.Sequence + "|" + r.Utc + "|" + r.Event + "|" + r.Subject + "|" + r.Detail + "|" + r.SnapshotFingerprint + "|" + r.CertificateHash + "|" + r.PrevHash); File.AppendAllText(LedgerPath(ctx), Json.Serialize(r) + Environment.NewLine, new UTF8Encoding(false)); }
        }
        private static List<GenomeLedgerRecord> ReadLedger(DeepSystemsContext ctx) { List<GenomeLedgerRecord> r = new List<GenomeLedgerRecord>(); string p = LedgerPath(ctx); if (!File.Exists(p)) return r; foreach (string line in File.ReadLines(p).Tail(MaxLedgerRead)) { if (String.IsNullOrWhiteSpace(line)) continue; try { GenomeLedgerRecord x = Json.Deserialize<GenomeLedgerRecord>(line); if (x != null) r.Add(x); } catch { } } return r; }
        private static bool VerifyLedger(List<GenomeLedgerRecord> xs) { if (xs == null || xs.Count == 0) return true; for (int i = 0; i < xs.Count; i++) { GenomeLedgerRecord r = xs[i]; string expectedPrev = i == 0 ? r.PrevHash : xs[i - 1].EntryHash; if (i > 0 && !String.Equals(r.PrevHash, expectedPrev, StringComparison.OrdinalIgnoreCase)) return false; string h = SoftwareGenomeKernel.HashText(r.Schema + "|" + r.Sequence + "|" + r.Utc + "|" + r.Event + "|" + r.Subject + "|" + r.Detail + "|" + r.SnapshotFingerprint + "|" + r.CertificateHash + "|" + r.PrevHash); if (!String.Equals(h, r.EntryHash, StringComparison.OrdinalIgnoreCase)) return false; } return true; }

        private static string HashObject(object source) { try { return SoftwareGenomeKernel.HashText(Json.Serialize(source)); } catch { return SoftwareGenomeKernel.HashText(source == null ? "" : source.ToString()); } }
        private static string ReadId(object source) { if (source == null) return "UNKNOWN"; try { PropertyInfo p = source.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase); object v = p == null ? null : p.GetValue(source, null); return v == null ? source.GetType().Name : Convert.ToString(v, CultureInfo.InvariantCulture); } catch { return source.GetType().Name; } }

        private static Window W(Window owner, string title, double width, double height) { Window w = new Window { Title = title, Width = width, Height = height, Background = Bg, Foreground = Text, WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner, Owner = owner }; return w; }
        private static DockPanel Shell(string title, string subtitle) { DockPanel root = new DockPanel { Background = Bg }; StackPanel head = new StackPanel { Margin = new Thickness(20, 18, 20, 10) }; head.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 25, FontWeight = FontWeights.Bold }); head.Children.Add(new TextBlock { Text = subtitle ?? "", Foreground = Muted, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) }); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head); return root; }
        private static void Portal(WrapPanel panel, string badge, string title, string detail, RoutedEventHandler click) { Button b = new Button { Width = 285, MinHeight = 118, Margin = new Thickness(7), Padding = new Thickness(14), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), HorizontalContentAlignment = HorizontalAlignment.Stretch }; StackPanel p = new StackPanel(); p.Children.Add(new TextBlock { Text = badge + "   " + title, Foreground = Accent, FontWeight = FontWeights.Bold, FontSize = 13, TextWrapping = TextWrapping.Wrap }); p.Children.Add(new TextBlock { Text = detail, Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 0) }); b.Content = p; b.Click += click; panel.Children.Add(b); }
        private static void SmallPortal(Panel p, string title, RoutedEventHandler click) { Button b = new Button { Content = title, Margin = new Thickness(5), Padding = new Thickness(11, 7, 11, 7), Background = Raised, Foreground = Accent, BorderBrush = Border }; b.Click += click; p.Children.Add(b); }
        private static ListBox List() { return new ListBox { Margin = new Thickness(18), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), FontFamily = new FontFamily("Consolas"), FontSize = 12 }; }
        private static void OpenCollection(Window owner, string title, IList<object> items, string note) { Window w = W(owner, "YOMI · " + title, 1340, 820); DockPanel root = Shell(title, note + "   ·   " + (items == null ? 0 : items.Count) + " retained rows"); ListBox list = List(); list.ItemsSource = items ?? new List<object>(); root.Children.Add(list); w.Content = root; w.Show(); }
        private static string Prompt(Window owner, string title, string label, string initial) { Window w = W(owner, "YOMI · " + title, 720, 280); DockPanel root = Shell(title, label); TextBox box = new TextBox { Text = initial ?? "", Margin = new Thickness(20, 6, 20, 10), Padding = new Thickness(8), Background = Surface, Foreground = Text, BorderBrush = Border }; DockPanel.SetDock(box, Dock.Top); root.Children.Add(box); StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20) }; Button ok = new Button { Content = "COMMIT", Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(6), Background = Raised, Foreground = Green, BorderBrush = Border }; Button cancel = new Button { Content = "CANCEL", Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(6), Background = Raised, Foreground = Muted, BorderBrush = Border }; string value = null; ok.Click += delegate { value = box.Text; w.DialogResult = true; w.Close(); }; cancel.Click += delegate { w.DialogResult = false; w.Close(); }; buttons.Children.Add(cancel); buttons.Children.Add(ok); root.Children.Add(buttons); w.Content = root; bool? r = w.ShowDialog(); return r == true ? value : null; }
        private static Brush B(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
    }

    internal static class GenomeEnumerableCompatibility
    {
        public static IEnumerable<T> Tail<T>(this IEnumerable<T> source, int count) { if (source == null) yield break; Queue<T> q = new Queue<T>(); foreach (T x in source) { q.Enqueue(x); if (q.Count > count) q.Dequeue(); } foreach (T x in q) yield return x; }
    }
}

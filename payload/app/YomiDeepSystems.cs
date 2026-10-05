using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal sealed class DeepSystemsContext
    {
        public readonly string DataRoot;
        public readonly string StateRoot;
        public readonly string InstallRoot;
        public readonly string AppDir;
        public readonly JavaScriptSerializer Json;
        public DeepSystemsContext(string dataRoot, string installRoot, string appDir)
        {
            DataRoot = dataRoot;
            StateRoot = Path.Combine(dataRoot, "state");
            InstallRoot = installRoot;
            AppDir = appDir;
            Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        }
    }

    internal sealed class DeepNode
    {
        public string Title;
        public string Subtitle;
        public string Badge;
        public string FilePath;
        public string JsonPath;
        public object Value;
        public Func<DeepSystemsContext, List<DeepNode>> Children;
        public Action<DeepSystemsContext, Window> Action;
        public DeepNode(string title, string subtitle)
        {
            Title = title ?? "";
            Subtitle = subtitle ?? "";
            Badge = "PORTAL";
            JsonPath = "$";
        }
    }

    internal sealed class DeepBookmark
    {
        public string Title;
        public string Route;
        public string FilePath;
        public string JsonPath;
        public string Summary;
        public string SavedUtc;
    }

    internal sealed class DeepSnapshot
    {
        public string Id;
        public string Title;
        public string Route;
        public string FilePath;
        public string JsonPath;
        public string Json;
        public string CapturedUtc;
    }

    internal sealed class DeepWindowRecord
    {
        public WeakReference Window;
        public string Route;
        public int Depth;
        public DateTime OpenedUtc;
    }

    internal static class DeepSystems
    {
        private static readonly object Gate = new object();
        private static readonly List<DeepWindowRecord> OpenWindows = new List<DeepWindowRecord>();
        private static readonly List<DeepBookmark> Bookmarks = new List<DeepBookmark>();
        private static readonly List<DeepSnapshot> Snapshots = new List<DeepSnapshot>();
        private static DeepSystemsContext LastContext;

        private static SolidColorBrush Brush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        private static readonly Brush Bg = Brush("#101214");
        private static readonly Brush Surface = Brush("#171A1D");
        private static readonly Brush Raised = Brush("#202429");
        private static readonly Brush Border = Brush("#343A40");
        private static readonly Brush Text = Brush("#F0F3F5");
        private static readonly Brush Muted = Brush("#A3ADB7");
        private static readonly Brush Accent = Brush("#69E6B4");
        private static readonly Brush Blue = Brush("#70B7FF");
        private static readonly Brush Amber = Brush("#FFCA69");
        private static readonly Brush Danger = Brush("#FF7B7B");

        private static DeepSystemsContext CreateContext(string dataRoot, string installRoot, string appDir)
        {
            var ctx = new DeepSystemsContext(dataRoot, installRoot, appDir);
            LastContext = ctx;
            ExpeditionWorkspace.Initialize(ctx);
            return ctx;
        }

        internal static void AdoptContext(DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            LastContext = ctx;
            ExpeditionWorkspace.Initialize(ctx);
        }

        public static void OpenExpeditionWorkspace(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.Open(owner, ctx);
        }

        public static void OpenWorldPassportSpine(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            WorldPassportSpine.OpenHub(owner, ctx);
        }

        public static void OpenAtlas(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            LoadNotebook(ctx);
            LoadSnapshots(ctx);
            ExpeditionWorkspace.Visit(ctx, "atlas", "Controller / command", "System Atlas root");
            OpenPortal(owner, ctx, "SYSTEM ATLAS", "The deep map of YOMI's live control plane", BuildAtlas(ctx), new List<string> { "ATLAS" }, "ROOT");
        }

        public static void OpenAtlasFromContext(Window owner)
        {
            if (LastContext != null)
            {
                ExpeditionWorkspace.Visit(LastContext, "atlas", "Return / workspace", "System Atlas root");
                OpenPortal(owner, LastContext, "SYSTEM ATLAS", "The deep map of YOMI's live control plane", BuildAtlas(LastContext), new List<string> { "ATLAS" }, "ROOT");
            }
        }

        internal static void OpenExternalValue(Window owner, DeepSystemsContext ctx, string title, object value, string route)
        {
            if (ctx == null) ctx = LastContext;
            if (ctx == null) return;
            LastContext = ctx;
            ExpeditionWorkspace.TouchDetail(ctx, route ?? "external object", title ?? "OBJECT");
            OpenObjectWindow(owner, ctx, title ?? "OBJECT", "Opened from an adjacent exploration subsystem.", value, null, "$external", SplitRoute(route ?? "ATLAS › EXTERNAL"), false);
        }

        internal static void OpenExternalEvidenceSearch(Window owner, DeepSystemsContext ctx, string query, string route)
        {
            if (ctx == null) ctx = LastContext;
            if (ctx == null || String.IsNullOrWhiteSpace(query)) return;
            LastContext = ctx;
            ExpeditionWorkspace.TouchDetail(ctx, route ?? "external trace", "Evidence search: " + query);
            OpenEvidenceSearch(owner, ctx, query, SplitRoute(route ?? "ATLAS › EXTERNAL › TRACE"));
        }

        internal static void OpenExternalFile(Window owner, DeepSystemsContext ctx, string path, string route)
        {
            if (ctx == null) ctx = LastContext;
            if (ctx == null || String.IsNullOrWhiteSpace(path)) return;
            LastContext = ctx;
            ExpeditionWorkspace.TouchDetail(ctx, route ?? "external file", path);
            OpenFile(owner, ctx, path, "$", SplitRoute(route ?? "ATLAS › EXTERNAL › FILE"));
        }

        public static void OpenTemporalObservatory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "temporal", "Controller / command");
        }

        public static void OpenCausalGraphLab(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "causal", "Controller / command");
        }

        public static void OpenResearchWorkbench(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "research", "Controller / command");
        }

        public static void OpenAlgorithmicConservatory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "algorithmic", "Controller / command");
        }

        public static void OpenProductivityFabric(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "productivity", "Controller / command");
        }

        public static void OpenAbyssalKnowledgeEngine(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "abyssal", "Controller / command");
        }

        public static void OpenWorldKernel(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "world", "Controller / command");
        }

        public static void OpenCivilizationLayer(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "civilization", "Controller / command");
        }

        public static void OpenEpistemicLogicFoundry(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "logic", "Controller / command");
        }

        public static void OpenAxiomaticVerificationReactor(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "verification", "Controller / command");
        }

        public static void OpenCounterfactualStrategySuperstructure(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "strategy", "Controller / command");
        }

        public static void OpenScientificDiscoveryHyperstructure(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "science", "Controller / command");
        }

        public static void OpenDysonSystemsEngineeringMegastructure(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "engineering", "Controller / command");
        }

        public static void OpenCyberneticDigitalTwinMetastructure(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "cybernetic", "Controller / command");
        }

        public static void OpenComplexSystemsLaboratory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "complex", "Controller / command");
        }

        public static void OpenOperationsResearchEmpire(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "or", "Controller / command");
        }

        public static void OpenEconomicCivilization(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "economy", "Controller / command");
        }

        public static void OpenSpatialWorldCartography(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "spatial", "Controller / command");
        }

        public static void OpenDistributedSystemsPlanetarium(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "distributed", "Controller / command");
        }

        public static void OpenAdversarialSecurityFortress(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "security", "Controller / command");
        }

        public static void OpenDataFoundry(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "data", "Controller / command");
        }

        public static void OpenSoftwareGenomeObservatory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "genome", "Controller / command");
        }

        public static void OpenLivingDocumentIntelligenceFactory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "documents", "Controller / command");
        }

        public static void OpenGrandUnifiedExpeditionCommand(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "expedition", "Controller / command");
        }

        public static void OpenKnowledgeConstellationMetastructure(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "constellation", "Controller / command");
        }

        public static void OpenCivilizationalSynthesisReactor(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "synthesis", "Controller / command");
        }

        public static void OpenUnknownUnknownsObservatory(Window owner, string dataRoot, string installRoot, string appDir)
        {
            var ctx = CreateContext(dataRoot, installRoot, appDir);
            ExpeditionWorkspace.OpenRoute(owner, ctx, "void", "Controller / command");
        }

        private static List<DeepNode> BuildAtlas(DeepSystemsContext ctx)
        {
            var nodes = new List<DeepNode>();
            nodes.Add(new DeepNode("UNIVERSAL WORLD KERNEL / CONVERGENCE CORE", "Stable object passports, cross-system identity graph, World Query Language, schema cartography, relationship traversal, saved lenses and whole-world epochs.") { Badge = "KERNEL", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "world", "System Atlas"); } });
            nodes.Add(new DeepNode("CIVILIZATION LAYER / WORLD INSTITUTIONS", "Schema-2 institutions, constitutional memory, expeditions, sentinels, councils, resolutions, versioned Knowledge Commons, plurality forums, border cartography, treaty constellations, relay routes, civil compacts, census epochs and integrity-linked chronicles over the Universal World Kernel.") { Badge = "CIV", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "civilization", "System Atlas"); } });
            nodes.Add(new DeepNode("EPISTEMIC LOGIC FOUNDRY / PROOF CIVILIZATION", "Paraconsistent four-valued truth, modal worlds, proof DAGs, bounded unification, defeasible rules, contradiction cores, abduction, falsification, frozen theories and decision dominance over World Kernel evidence.") { Badge = "⊢4V", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "logic", "System Atlas"); } });
            nodes.Add(new DeepNode("AXIOMATIC VERIFICATION REACTOR / FINITE-STATE FORMAL METHODS", "Persistent Kripke structures, compositional CTL fixed points, state-space topology, shortest counterexamples, accepting-cycle witnesses, invariant mining, bisimulation quotienting, mutation adequacy and SHA-256 proof-carrying verification certificates.") { Badge = "⊨CTL", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "verification", "System Atlas"); } });
            nodes.Add(new DeepNode("COUNTERFACTUAL STRATEGY SUPERSTRUCTURE / DECISION SCIENCE CIVILIZATION", "Structural causal models, explicit do-operators, shared-exogenous twin worlds, low-discrepancy uncertainty manifolds, Pareto frontiers, lower-tail robustness, minimax regret, sensitivity tensors, information value and model-bound strategy dossiers.") { Badge = "do(X)", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "strategy", "System Atlas"); } });
            nodes.Add(new DeepNode("SCIENTIFIC DISCOVERY HYPERSTRUCTURE / EXPERIMENTAL REASONING CIVILIZATION", "Preregistered competing hypotheses, deterministic factorial/space-filling/D-optimal design synthesis, estimability and alias tribunals, expected information gain, sequential Bayesian updating, power, reproducibility and bounded research portfolios without experiment-execution authority.") { Badge = "I(H;Y)", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "science", "System Atlas"); } });
            nodes.Add(new DeepNode("DYSON SYSTEMS ENGINEERING MEGASTRUCTURE / PLANETARY-SCALE SYNTHESIS CIVILIZATION", "Requirements calculus, architecture decomposition, design-structure matrices, interface contracts, conservation budgets, PERT/CPM, reliability, fault trees, FMEA, max-flow/min-cut, Pareto trade studies, risk manifolds, safety cases and immutable configuration baselines.") { Badge = "ΣSYS", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "engineering", "System Atlas"); } });
            nodes.Add(new DeepNode("CYBERNETIC DIGITAL TWIN METASTRUCTURE / ESTIMATION & CONTROL CIVILIZATION", "Discrete and continuous state-space abstractions, controllability, observability, spectral stability, Kalman estimation, Riccati/LQR synthesis, constrained MPC, PID response design, reachability, robust stress worlds, sensor portfolios, actuator allocation, residual fault isolation and immutable twin baselines without live authority.") { Badge = "x̂|u", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "cybernetic", "System Atlas"); } });
            nodes.Add(new DeepNode("COMPLEX SYSTEMS LABORATORY / EMERGENCE CIVILIZATION", "Heterogeneous agent populations, procedural and explicit interaction networks, threshold/SIR/opinion/Ising/evolutionary/cascade/coupled-map/cellular dynamics, attractor basins, deterministic ensembles, finite-size phase transitions, resilience, avalanche criticality, sensitivity, immutable baselines and model-bound emergence dossiers.") { Badge = "ΣPOP", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "complex", "System Atlas"); } });
            nodes.Add(new DeepNode("OPERATIONS RESEARCH EMPIRE / OPTIMIZATION CIVILIZATION", "Shortest paths, capacitated vehicle routing, precedence/resource scheduling, Hungarian assignment, min-cost flow, facility location, warehouse slotting, workforce coverage, inventory policy, budgeted portfolios, scenario stress, Pareto trade spaces, sensitivity, immutable baselines and bounded decision dossiers.") { Badge = "ORΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "or", "System Atlas"); } });
            nodes.Add(new DeepNode("ECONOMIC CIVILIZATION / STOCK-FLOW WORLD", "Households, firms, goods, production recipes, labor, endogenous prices, double-entry transaction journals, contracts, banks, credit, defaults, governments, taxation, transfers, trade, national accounts, distribution, Leontief supply chains, market concentration, crisis worlds, policy counterfactuals, immutable baselines and bounded economic dossiers.") { Badge = "ECONΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "economy", "System Atlas"); } });
            nodes.Add(new DeepNode("SPATIAL WORLD CARTOGRAPHY / PLANETARY ATLAS", "Coordinate reference systems, stable places, nested interiors, polygonal territories, jurisdictions, multimodal topology, generalized-cost routes, isochrones, moving entities, congestion, infrastructure dependencies, accessibility criticality, hazard worlds, spatial indexes, immutable baselines and bounded geographic dossiers.") { Badge = "GEOΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "spatial", "System Atlas"); } });
            nodes.Add(new DeepNode("DISTRIBUTED SYSTEMS PLANETARIUM / NETWORKED COMPUTATION CIVILIZATION", "Fault domains, nodes, processes, impaired message fabrics, logical and vector clocks, Raft-like elections, replicated logs, quorum algebra, consistency histories, gossip convergence, distributed transactions, leases, fencing, failure detection, CAP worlds, topology survivability, chaos campaigns, immutable baselines and bounded resilience dossiers.") { Badge = "DISTΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "distributed", "System Atlas"); } });
            nodes.Add(new DeepNode("ADVERSARIAL SECURITY FORTRESS / DEFENSIVE ASSURANCE CIVILIZATION", "Assets, crown jewels, identities, group-closure authorization, explicit deny, opaque secret references, trust boundaries, synthetic threats, abstract attack graphs, privilege paths, blast radius, segmentation min-cuts, defense-in-depth coverage, residual risk, incident command, containment planning, adversarial campaigns and assurance cases—without live-system authority.") { Badge = "SECΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "security", "System Atlas"); } });
            nodes.Add(new DeepNode("DATA FOUNDRY / PROVENANCE & TRANSFORMATION CIVILIZATION", "Streaming local CSV/TSV/JSONL profiling, field semantics, bounded deterministic sampling, data-quality evidence, candidate relations, correlations, lineage DAGs, declarative cleaning recipes, YOMI-owned derived materialization, schema contracts, validation, drift, immutable baselines and evidence-bound decision dossiers.") { Badge = "DATAΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "data", "System Atlas"); } });
            nodes.Add(new DeepNode("SOFTWARE GENOME OBSERVATORY / CODEBASE ORGANISM CIVILIZATION", "Static codebase anatomy: content-addressed files, symbols, imports, dependencies, calls, state writes, strongly connected components, layers, hotspots, reverse-dependency blast radius, read-only Git history, ownership concentration, co-change gravity, architecture drift, immutable baselines and change dossiers without executing the observed project.") { Badge = "GENOMEΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "genome", "System Atlas"); } });
            nodes.Add(new DeepNode("LIVING DOCUMENT & INTELLIGENCE FACTORY / PROVENANCE-NATIVE PUBLICATION CIVILIZATION", "Reports, manuals, research dossiers and specifications whose paragraphs remain wired to claims, source fingerprints, calculations, models, assumptions, objections, requirements, decisions, staleness propagation, immutable version genealogy and reproducible publication seals.") { Badge = "DOCΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "documents", "System Atlas"); } });
            nodes.Add(new DeepNode("GRAND UNIFIED EXPEDITION COMMAND / MISSION WORLDLINE CIVILIZATION", "Question-first investigations that cross the entire civilization lattice while retaining fingerprinted source snapshots, findings, contradictions, branches, decisions, exact checkpoints, window-route constellations and resumable mission worldlines.") { Badge = "EXPΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "expedition", "System Atlas"); } });
            nodes.Add(new DeepNode("KNOWLEDGE CONSTELLATION METASTRUCTURE / CROSS-MISSION HYPERGRAPH", "A provenance-preserving universe above EXPΩ: every retained mission, branch, crossing, finding, question, contradiction, decision, checkpoint and source fingerprint becomes an addressable graph object with exact links kept separate from heuristic affinity, global frontiers, drift baselines and launchable follow-up expeditions.") { Badge = "METAΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "constellation", "System Atlas"); } });
            nodes.Add(new DeepNode("CIVILIZATIONAL SYNTHESIS REACTOR / TRANSFERABLE PATTERN FORGE", "A synthesis layer above METAΩ that mines cross-mission recurrences, motifs, failure signatures and invariant candidates while preserving counterexamples, source independence, scope limits and transfer risk; human promotion creates theorem candidates, never automatic truths.") { Badge = "SYNTHΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "synthesis", "System Atlas"); } });
            nodes.Add(new DeepNode("UNKNOWN UNKNOWNS OBSERVATORY / CIVILIZATION CLOSURE", "Final civilization-layer observatory: structural absence, source monoculture, neglected lenses, unchallenged assumptions, disagreement typing and theory-test debt. VOIDΩ can identify blind-spot candidates from retained structure; it cannot enumerate genuinely unimagined unknowns. This release also freezes the civilization layer behind an explicit YOMI relevance firewall.") { Badge = "VOIDΩ", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "void", "System Atlas"); } });
            nodes.Add(Portal("INVESTIGATION MISSIONS", "Enter by question instead of subsystem: transition autopsy, broadcast readiness, order mutation, media origin and recovery evidence.", "CASES", BuildInvestigations));
            nodes.Add(new DeepNode("TEMPORAL OBSERVATORY", "History, evidence correlation, epoch drift, counterfactual branches and time-oriented investigation surfaces.") { Badge = "ΔT", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "temporal", "System Atlas"); } });
            nodes.Add(new DeepNode("CAUSAL GRAPH LAB", "Evidence-chain clusters, identity wormholes, semantic coupling heatmaps and nested counterfactual-universe genealogy.") { Badge = "GRAPH", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "causal", "System Atlas"); } });
            nodes.Add(new DeepNode("RESEARCH WORKBENCH", "Self-discovering leads, anomaly observation, emergent motifs, hypotheses, topology synthesis, research lenses and persistent case files.") { Badge = "R&D", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "research", "System Atlas"); } });
            nodes.Add(new DeepNode("ALGORITHMIC CONSERVATORY", "Classical, modern and experimental coding theory made explorable through YOMI's real structures, source genome, proofs, simulations and protocol archaeology.") { Badge = "CS", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "algorithmic", "System Atlas"); } });
            nodes.Add(new DeepNode("COGNITIVE PRODUCTIVITY FABRIC", "Recursive missions, dependency-aware next actions, crash-aware focus continuity, research debt, capture processing and resumable deep work.") { Badge = "WORK", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "productivity", "System Atlas"); } });
            nodes.Add(new DeepNode("HADAL SYSTEMS / ABYSSAL KNOWLEDGE ENGINE", "Marianas-depth recursive descents through ancestry, consequence horizons, divergence, null space, unanswered questions, fossils and loop-proof cross-system wormholes.") { Badge = "HADAL", Action = delegate(DeepSystemsContext c, Window w) { ExpeditionWorkspace.OpenRoute(w, c, "abyssal", "System Atlas"); } });
            nodes.Add(Portal("QUEUE OBSERVATORY", "Authoritative order, occurrence identity, mutation lineage, checkpoints and queue-runtime evidence.", "ORDER", BuildQueue));
            nodes.Add(Portal("BROADCAST LAB", "Rehearsal, Freeze, service-level objectives, Oracle forecasts, scheduler decisions and Aegis pressure.", "LIVE", BuildBroadcast));
            nodes.Add(Portal("INTENT / PROVENANCE VAULT", "Operator intent plans, DAG steps, journal records, hash ancestry and crash-recovery lineage.", "BLACK BOX", BuildIntent));
            nodes.Add(Portal("RUNTIME TOPOLOGY", "Processes, leases, engine projections, readiness, contracts and the current data-plane state.", "RUNTIME", BuildRuntime));
            nodes.Add(Portal("MEDIA OBJECT UNIVERSE", "Content-addressed artwork/video/audio/visualizer objects, source identities and cache generations.", "CACHE", BuildMedia));
            nodes.Add(Portal("SESSION & IDENTITY LAB", "Session registry, occurrence cardinality, source metadata, favorites and controller-owned state.", "IDENTITY", BuildSession));
            nodes.Add(Portal("FORENSICS / EVENT HORIZON", "Events, logs, journals, failure evidence, recovery artifacts and text-search across state.", "EVIDENCE", BuildForensics));
            nodes.Add(Portal("TOOLS WITHIN TOOLS", "Operations Center, Session Explorer, Oracle Lab, Incident Replay, Recovery Points and other installed subsystems.", "LABS", BuildTools));
            nodes.Add(Portal("DISCOVERY NOTEBOOK", "Pinned discoveries collected while you wander through the system.", "NOTEBOOK", BuildNotebook));
            nodes.Add(Portal("TEMPORAL SNAPSHOT VAULT", "Captured subsystem states and before/after diffs from earlier points in your exploration.", "TIME", BuildSnapshotVault));
            nodes.Add(Portal("OPEN WINDOW CONSTELLATION", "Every deep-system window currently alive, including its depth and route through the world.", "MAP", BuildConstellation));
            nodes.Add(Portal("RAW STATE CATACOMBS", "Recursive entry into the entire YOMI data directory. No curated path. Follow whatever looks interesting.", "∞", delegate(DeepSystemsContext c) { return DirectoryNodes(c.DataRoot, 0); }));
            return nodes;
        }

        private static DeepNode Portal(string title, string subtitle, string badge, Func<DeepSystemsContext, List<DeepNode>> children)
        {
            var n = new DeepNode(title, subtitle);
            n.Badge = badge;
            n.Children = children;
            return n;
        }

        private static DeepNode FileNode(string title, string subtitle, string badge, string path)
        {
            var n = new DeepNode(title, subtitle);
            n.Badge = badge;
            n.FilePath = path;
            return n;
        }

        private static List<DeepNode> BuildInvestigations(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            n.Add(Portal("NEXT TRANSITION AUTOPSY", "Why is the next transition ready, thin, late, risky, or still building?", "WHY NEXT", BuildNextTransitionAutopsy));
            n.Add(Portal("BROADCAST READINESS CASE", "Correlate SLO, Oracle, Aegis, rehearsal, Freeze and the next scheduler decisions.", "GO/NO-GO", BuildBroadcastReadinessCase));
            n.Add(Portal("ORDER MUTATION AUTOPSY", "Trace the active order revision into ledger evidence, checkpoints, undo/redo and occurrence sequence.", "R#", BuildOrderMutationCase));
            n.Add(Portal("CURRENT MEDIA ORIGIN TRACE", "Start at Now Playing identity and tunnel toward queue projection, source key and cache-object evidence.", "ORIGIN", BuildMediaOriginCase));
            n.Add(Portal("RECOVERY / CRASH EVIDENCE CASE", "Walk runtime provenance, watchdog/preflight evidence, intent recovery chains and control-plane heads.", "FORENSICS", BuildRecoveryCase));
            n.Add(Portal("SCHEDULER WHY FIELD", "A wall of live scheduler decisions. Pick any occurrence and keep descending.", "WHY", BuildSchedulerField));
            return n;
        }

        private static List<DeepNode> BuildNextTransitionAutopsy(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            var q = AsMap(ReadJson(ctx, Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            var items = GetList(q, "items");
            Dictionary<string, object> next = null;
            foreach (object raw in items)
            {
                var m = AsMap(raw);
                int rel = GetIntAny(m, new string[] { "relative" }, 9999);
                if (rel == 1) { next = m; break; }
            }
            if (next != null)
            {
                n.Add(new DeepNode("NEXT OCCURRENCE DOSSIER", "The scheduler's complete projection for relative +1.") { Badge = GetString(next, "risk", "NEXT"), Value = next, JsonPath = "$.items[relative=1]" });
                string source = GetString(next, "source_key", "");
                if (!String.IsNullOrWhiteSpace(source))
                {
                    string captured = source;
                    n.Add(new DeepNode("TRACE NEXT SOURCE IDENTITY", captured) { Badge = "SOURCE", Action = delegate(DeepSystemsContext c, Window w) { OpenEvidenceSearch(w, c, captured, new List<string> { "ATLAS", "CASES", "NEXT", "SOURCE", captured }); } });
                }
            }
            else n.Add(Missing("No relative +1 queue item is currently projected."));
            AddMapBranch(n, q, "service_level", "SERVICE-LEVEL CONTEXT", "How much ready-time cushion exists around the next transition.", "SLO");
            AddMapBranch(n, q, "oracle", "ORACLE CONTEXT", "Aggregate confidence and attention/watch verdict.", "ORACLE");
            AddMapBranch(n, q, "resource", "RESOURCE CONTEXT", "Whether Aegis is suppressing or constraining preparation.", "AEGIS");
            n.Add(FileNode("FULL QUEUE RUNTIME", "If the case still doesn't make sense, descend into the entire projection.", "RAW", Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            return n;
        }

        private static List<DeepNode> BuildBroadcastReadinessCase(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            var q = AsMap(ReadJson(ctx, Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            AddMapBranch(n, q, "service_level", "1 · SERVICE LEVEL", "Continuity and ready-buffer evidence.", "SLO");
            AddMapBranch(n, q, "oracle", "2 · ORACLE", "Predictive confidence and urgency evidence.", "ORACLE");
            AddMapBranch(n, q, "resource", "3 · AEGIS", "Resource-governor constraints and pressure reason.", "AEGIS");
            AddMapBranch(n, q, "rehearsal", "4 · REHEARSAL", "Prepared transition-window state.", "REHEARSE");
            AddMapBranch(n, q, "freeze", "5 · FREEZE", "Whether the rehearsal commitments are protected.", "FREEZE");
            n.Add(Portal("6 · FUTURE DECISION FIELD", "Inspect each upcoming occurrence's ETA, risk, slack and Oracle confidence.", "WHY", BuildSchedulerField));
            n.Add(FileNode("7 · CURRENT ENGINE STATE", "Correlate broadcast readiness with the current playing occurrence.", "CURRENT", Path.Combine(ctx.StateRoot, "current.json")));
            return n;
        }

        private static List<DeepNode> BuildOrderMutationCase(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            n.Add(FileNode("ACTIVE ORDER", "Current mutable order revision and full occurrence sequence.", "R#", Path.Combine(ctx.StateRoot, "session-order.json")));
            n.Add(Portal("MUTATION LINEAGE", "Every journal/ledger artifact discovered around the order state.", "LEDGER", BuildOrderForensics));
            n.Add(FileNode("CHECKPOINT VAULT", "Controller snapshots that can propose a full-order restore through engine validation.", "SNAP", Path.Combine(ctx.StateRoot, "controller-queue-checkpoints.json")));
            n.Add(Portal("OCCURRENCE DOSSIERS", "Pick an occurrence and follow identity/readiness/media links.", "OCC", BuildOccurrenceDossiers));
            return n;
        }

        private static List<DeepNode> BuildMediaOriginCase(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            var current = AsMap(ReadJson(ctx, Path.Combine(ctx.StateRoot, "current.json")));
            n.Add(new DeepNode("NOW PLAYING IDENTITY", "The current engine projection that begins the trace.") { Badge = "CURRENT", Value = current, JsonPath = "$current" });
            string[] keys = { "source_key", "id", "url", "occurrence_id", "occurrence" };
            foreach (string key in keys)
            {
                string v = GetString(current, key, "");
                if (String.IsNullOrWhiteSpace(v)) continue;
                string captured = v; string capturedKey = key;
                n.Add(new DeepNode("TRACE " + key.ToUpperInvariant(), captured) { Badge = "IDENTITY", Action = delegate(DeepSystemsContext c, Window w) { OpenEvidenceSearch(w, c, captured, new List<string> { "ATLAS", "CASES", "MEDIA ORIGIN", capturedKey, captured }); } });
            }
            n.Add(Portal("CONTENT-ADDRESSED OBJECT STORE", "Continue into audio/artwork/video/visualizer object namespaces.", "CACHE", BuildMedia));
            n.Add(FileNode("QUEUE RUNTIME CROSS-CHECK", "Find how the same occurrence/source is represented by the scheduler.", "QUEUE", Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            return n;
        }

        private static List<DeepNode> BuildRecoveryCase(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string[] preferred = { "runtime-instance.json", "runtime-lease.json", "watchdog.json", "preflight.json", "controller-intent-head.json", "update-transaction.json", "deployment.json" };
            foreach (string f in preferred)
            {
                string p = Path.Combine(ctx.StateRoot, f);
                if (File.Exists(p)) n.Add(FileNode(f.ToUpperInvariant(), "Recovery/control-plane evidence.", "EVIDENCE", p));
            }
            n.Add(Portal("INTENT RECOVERY LINEAGES", "Recovered operator-control chains created instead of rewriting suspicious history.", "INTENT", BuildIntentRecoveryChains));
            n.Add(Portal("ORDER / LEDGER EVIDENCE", "Mutation-chain and ledger-head evidence around mutable session state.", "ORDER", BuildOrderForensics));
            n.Add(Portal("LOG CATACOMBS", "Operational logs and bounded traces around failures/restarts.", "LOG", delegate(DeepSystemsContext c) { return DirectoryNodes(Path.Combine(c.DataRoot, "logs"), 0); }));
            return n;
        }

        private static List<DeepNode> BuildQueue(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            n.Add(FileNode("AUTHORITATIVE SESSION ORDER", "Mutable occurrence sequence, undo/redo stacks, revision and inserted occurrences.", "R#", Path.Combine(ctx.StateRoot, "session-order.json")));
            n.Add(FileNode("QUEUE RUNTIME PROJECTION", "Readiness, phase, ETA, risk, Oracle, scheduler reason, jobs, workers and buffer health.", "LIVE", Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            n.Add(FileNode("SESSION REGISTRY", "Base source metadata and the session identity that the mutable order references.", "SESSION", Path.Combine(ctx.StateRoot, "session.json")));
            n.Add(Portal("OCCURRENCE DOSSIERS", "One portal per live queue occurrence. Duplicates remain separate occurrence identities.", "NODES", BuildOccurrenceDossiers));
            n.Add(Portal("ORDER REVISION FORENSICS", "Mutation ledger, ledger head and anything else carrying authoritative order evidence.", "LINEAGE", BuildOrderForensics));
            n.Add(FileNode("QUEUE CHECKPOINT VAULT", "Controller-owned full-sequence checkpoints for transactional restore.", "SNAP", Path.Combine(ctx.StateRoot, "controller-queue-checkpoints.json")));
            n.Add(new DeepNode("SEARCH AN OCCURRENCE / SOURCE", "Open a focused state search and follow every file that mentions an occurrence ID, source key or URL.")
            {
                Badge = "SEARCH",
                Action = delegate(DeepSystemsContext c, Window w) { OpenPromptSearch(w, c, "QUEUE EVIDENCE SEARCH", new List<string> { "ATLAS", "QUEUE", "SEARCH" }); }
            });
            return n;
        }

        private static List<DeepNode> BuildOccurrenceDossiers(DeepSystemsContext ctx)
        {
            var rows = new List<DeepNode>();
            object root = ReadJson(ctx, Path.Combine(ctx.StateRoot, "queue-runtime.json"));
            var map = AsMap(root);
            var items = GetList(map, "items");
            int count = 0;
            foreach (object raw in items)
            {
                var item = AsMap(raw);
                if (item.Count == 0) continue;
                int occ = GetIntAny(item, new string[] { "occurrence_id", "occurrence", "index" }, 0);
                string phase = GetString(item, "phase", "");
                string risk = GetString(item, "risk", "");
                string source = GetString(item, "source_key", "");
                string subtitle = "Occurrence " + occ.ToString(CultureInfo.InvariantCulture) + " · " + phase + (String.IsNullOrWhiteSpace(risk) ? "" : " · " + risk) + (String.IsNullOrWhiteSpace(source) ? "" : " · source " + source);
                var captured = item;
                rows.Add(new DeepNode("OCCURRENCE " + occ.ToString("0000", CultureInfo.InvariantCulture), subtitle)
                {
                    Badge = String.IsNullOrWhiteSpace(risk) ? "DOSSIER" : risk,
                    Value = captured,
                    JsonPath = "$.items[occurrence=" + occ.ToString(CultureInfo.InvariantCulture) + "]"
                });
                count++;
                if (count >= 800) break;
            }
            if (rows.Count == 0) rows.Add(Missing("No queue-runtime occurrence dossiers are currently available."));
            return rows;
        }

        private static List<DeepNode> BuildOrderForensics(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string[] candidates = { "session-journal.jsonl", "session-ledger.jsonl", "session-ledger-head.json", "session-journal-head.json", "mutation-ledger.jsonl", "mutation-ledger-head.json" };
            foreach (string name in candidates)
            {
                string p = Path.Combine(ctx.StateRoot, name);
                if (File.Exists(p)) n.Add(FileNode(name.ToUpperInvariant(), "Authoritative queue mutation evidence.", "EVIDENCE", p));
            }
            foreach (string p in SafeFiles(ctx.StateRoot, "*journal*", SearchOption.TopDirectoryOnly).Concat(SafeFiles(ctx.StateRoot, "*ledger*", SearchOption.TopDirectoryOnly)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!n.Any(x => String.Equals(x.FilePath, p, StringComparison.OrdinalIgnoreCase)))
                    n.Add(FileNode(Path.GetFileName(p).ToUpperInvariant(), "Discovered journal/ledger artifact.", "DISCOVERED", p));
            }
            if (n.Count == 0) n.Add(Missing("No mutation ledger files are materialized in the current state directory."));
            return n;
        }

        private static List<DeepNode> BuildBroadcast(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            object root = ReadJson(ctx, Path.Combine(ctx.StateRoot, "queue-runtime.json"));
            var q = AsMap(root);
            AddMapBranch(n, q, "service_level", "SERVICE LEVEL OBJECTIVE", "Continuity policy, ready buffer ratio and presentation degradation state.", "SLO");
            AddMapBranch(n, q, "oracle", "ORACLE FORECAST", "Aggregate confidence, future evaluation and attention/watch verdict.", "ORACLE");
            AddMapBranch(n, q, "resource", "AEGIS RESOURCE GOVERNOR", "Governor mode, pressure reason and effective/configured worker ceilings.", "AEGIS");
            AddMapBranch(n, q, "rehearsal", "REHEARSAL WINDOW", "Prepared broadcast transition window and synchronization progress.", "REHEARSE");
            AddMapBranch(n, q, "freeze", "BROADCAST FREEZE", "Protected rehearsal commitments for the exact active session.", "FREEZE");
            n.Add(Portal("SCHEDULER DECISION FIELD", "Every projected queue item with risk, ETA, slack, Oracle confidence and scheduling reason.", "WHY", BuildSchedulerField));
            n.Add(FileNode("RAW QUEUE-RUNTIME UNIVERSE", "Open the complete runtime projection and recursively descend from there.", "RAW", Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            n.Add(Portal("CAPABILITY / ROUTE MEMORY", "Discovered persistent route-intelligence and machine-calibration artifacts.", "MEMORY", BuildCapabilityMemory));
            return n;
        }

        private static List<DeepNode> BuildSchedulerField(DeepSystemsContext ctx)
        {
            var rows = new List<DeepNode>();
            var q = AsMap(ReadJson(ctx, Path.Combine(ctx.StateRoot, "queue-runtime.json")));
            var items = GetList(q, "items");
            int i = 0;
            foreach (object raw in items)
            {
                var m = AsMap(raw);
                int occ = GetIntAny(m, new string[] { "occurrence_id", "occurrence", "index" }, 0);
                string why = GetString(m, "decision_reason", "");
                string risk = GetString(m, "risk", "UNKNOWN");
                string conf = m.ContainsKey("oracle_confidence") ? Convert.ToString(m["oracle_confidence"], CultureInfo.InvariantCulture) + "%" : "—";
                string eta = m.ContainsKey("eta_seconds") ? Convert.ToString(m["eta_seconds"], CultureInfo.InvariantCulture) + "s" : "—";
                rows.Add(new DeepNode("OCC " + occ.ToString(CultureInfo.InvariantCulture) + " · " + risk, "ETA " + eta + " · ORACLE " + conf + (String.IsNullOrWhiteSpace(why) ? "" : " · WHY " + why)) { Badge = "DECISION", Value = m, JsonPath = "$.items[" + i.ToString(CultureInfo.InvariantCulture) + "]" });
                i++;
            }
            if (rows.Count == 0) rows.Add(Missing("No scheduler projection is available."));
            return rows;
        }

        private static List<DeepNode> BuildCapabilityMemory(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string[] roots = { Path.Combine(ctx.DataRoot, "cache", "capabilities"), ctx.StateRoot };
            foreach (string r in roots)
            {
                if (!Directory.Exists(r)) continue;
                foreach (string p in SafeFiles(r, "*", SearchOption.TopDirectoryOnly))
                {
                    string f = Path.GetFileName(p).ToLowerInvariant();
                    if (r != ctx.StateRoot || f.Contains("oracle") || f.Contains("route") || f.Contains("capab") || f.Contains("calibr") || f.Contains("performance"))
                        n.Add(FileNode(Path.GetFileName(p).ToUpperInvariant(), "Persistent scheduler/route/capability evidence.", "MEMORY", p));
                }
            }
            if (n.Count == 0) n.Add(Missing("No route/capability memory artifacts are currently materialized."));
            return n;
        }

        private static List<DeepNode> BuildIntent(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            n.Add(FileNode("INTENT HEAD", "Persisted journal head, chain selection, recovery reason and crash-reconciliation state.", "HEAD", Path.Combine(ctx.StateRoot, "controller-intent-head.json")));
            string journal = ResolveIntentJournal(ctx);
            n.Add(FileNode("ACTIVE INTENT JOURNAL", "Append-only hash-chained operator intent transitions.", "CHAIN", journal));
            n.Add(Portal("INTENT DOSSIERS", "One portal per journal transition. Follow plan fingerprint, state, step and hash ancestry.", "RECORDS", BuildIntentDossiers));
            n.Add(Portal("RECOVERY LINEAGES", "All recovered intent chains created after tamper/inconsistency/crash evidence.", "FORKS", BuildIntentRecoveryChains));
            n.Add(new DeepNode("SEARCH BY INTENT ID / HASH", "Paste an intent ID, fingerprint or hash fragment and chase it through every state artifact.")
            {
                Badge = "TRACE",
                Action = delegate(DeepSystemsContext c, Window w) { OpenPromptSearch(w, c, "PROVENANCE TRACE", new List<string> { "ATLAS", "INTENT", "TRACE" }); }
            });
            return n;
        }

        private static List<DeepNode> BuildIntentDossiers(DeepSystemsContext ctx)
        {
            string path = ResolveIntentJournal(ctx);
            return JsonLineNodes(ctx, path, 400, "INTENT TRANSITION");
        }

        private static List<DeepNode> BuildIntentRecoveryChains(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            foreach (string p in SafeFiles(ctx.StateRoot, "controller-intent-journal-recovery-*.jsonl", SearchOption.TopDirectoryOnly).OrderByDescending(x => x))
                n.Add(FileNode(Path.GetFileName(p), "Recovered provenance lineage. Prior evidence is preserved rather than rewritten.", "RECOVERY", p));
            if (n.Count == 0) n.Add(Missing("No recovery intent lineages have been created."));
            return n;
        }

        private static List<DeepNode> BuildRuntime(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string[] preferred = { "current.json", "engine-status.json", "runtime-instance.json", "runtime-lease.json", "queue-runtime.json", "preflight.json", "watchdog.json", "contracts.json", "deployment.json", "update-transaction.json" };
            foreach (string f in preferred)
            {
                string p = Path.Combine(ctx.StateRoot, f);
                if (File.Exists(p)) n.Add(FileNode(f.ToUpperInvariant(), "Live/runtime control-plane projection.", "STATE", p));
            }
            n.Add(Portal("DISCOVERED RUNTIME ARTIFACTS", "Every top-level state file not already represented above.", "DISCOVER", delegate(DeepSystemsContext c)
            {
                var all = new List<DeepNode>();
                foreach (string p in SafeFiles(c.StateRoot, "*", SearchOption.TopDirectoryOnly).OrderBy(x => x))
                {
                    if (preferred.Any(f => String.Equals(Path.GetFileName(p), f, StringComparison.OrdinalIgnoreCase))) continue;
                    all.Add(FileNode(Path.GetFileName(p), DescribeFile(p), "STATE", p));
                }
                return all;
            }));
            n.Add(Portal("PROCESS / BINARY PROVENANCE", "Runtime instance, PID, executable path, start epoch and binary fingerprint evidence.", "PROVENANCE", BuildProcessProvenance));
            return n;
        }

        private static List<DeepNode> BuildProcessProvenance(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string p = Path.Combine(ctx.StateRoot, "runtime-instance.json");
            if (File.Exists(p)) n.Add(FileNode("RUNTIME INSTANCE", "Exact process provenance projection.", "PID+SHA", p));
            foreach (string f in SafeFiles(ctx.StateRoot, "*.pid", SearchOption.TopDirectoryOnly)) n.Add(FileNode(Path.GetFileName(f), "PID sidecar. Correlate with runtime-instance provenance before trusting it.", "PID", f));
            if (n.Count == 0) n.Add(Missing("No process provenance files are currently materialized."));
            return n;
        }

        private static List<DeepNode> BuildMedia(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string objects = Path.Combine(ctx.DataRoot, "cache", "objects");
            if (Directory.Exists(objects))
            {
                foreach (string dir in SafeDirs(objects).OrderBy(x => x))
                {
                    string captured = dir;
                    n.Add(Portal(Path.GetFileName(dir).ToUpperInvariant() + " OBJECTS", "Content-addressed " + Path.GetFileName(dir) + " object namespace.", "OBJECTS", delegate(DeepSystemsContext c) { return DirectoryNodes(captured, 0); }));
                }
            }
            n.Add(Portal("CACHE ROOT", "Walk every cache namespace recursively, including capability memory and legacy projections.", "FILES", delegate(DeepSystemsContext c) { return DirectoryNodes(Path.Combine(c.DataRoot, "cache"), 0); }));
            n.Add(new DeepNode("SEARCH SOURCE KEY / SIGNATURE", "Search cache filenames and state evidence for a source-key or generation-signature fragment.")
            {
                Badge = "TRACE",
                Action = delegate(DeepSystemsContext c, Window w) { OpenPromptSearch(w, c, "MEDIA IDENTITY TRACE", new List<string> { "ATLAS", "MEDIA", "TRACE" }); }
            });
            return n;
        }

        private static List<DeepNode> BuildSession(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string[] files = { "session.json", "session-order.json", "current.json", "controller-library.json", "controller-queue-checkpoints.json", "controller-ui.json" };
            foreach (string f in files)
            {
                string p = Path.Combine(ctx.StateRoot, f);
                if (File.Exists(p)) n.Add(FileNode(f.ToUpperInvariant(), "Session/controller identity state.", "IDENTITY", p));
            }
            n.Add(Portal("SOURCE / OCCURRENCE CROSSWALK", "Follow runtime occurrences into source identity, queue state and cached media evidence.", "CROSSWALK", BuildOccurrenceDossiers));
            n.Add(Portal("FAVORITE SOURCE IDENTITIES", "Controller personal-library state keyed by source identity rather than occurrence identity.", "★", delegate(DeepSystemsContext c)
            {
                string p = Path.Combine(c.StateRoot, "controller-library.json");
                return ObjectChildren(c, ReadJson(c, p), "$", p);
            }));
            return n;
        }

        private static List<DeepNode> BuildForensics(DeepSystemsContext ctx)
        {
            var n = new List<DeepNode>();
            string events = Path.Combine(ctx.StateRoot, "events.jsonl");
            if (File.Exists(events)) n.Add(FileNode("FLIGHT RECORDER EVENTS", "Scheduler, lifecycle, preparation and incident events.", "EVENTS", events));
            n.Add(Portal("ALL JSONL JOURNALS", "Every append-only line-oriented evidence stream in YOMI data.", "JOURNALS", delegate(DeepSystemsContext c)
            {
                return SafeFiles(c.DataRoot, "*.jsonl", SearchOption.AllDirectories).Take(500).Select(p => FileNode(Path.GetFileName(p), Relative(c.DataRoot, p), "JSONL", p)).ToList();
            }));
            n.Add(Portal("LOG CATACOMBS", "Operational logs, bounded traces and any other textual runtime evidence.", "LOGS", delegate(DeepSystemsContext c)
            {
                string logs = Path.Combine(c.DataRoot, "logs");
                return DirectoryNodes(logs, 0);
            }));
            n.Add(new DeepNode("SEARCH ALL STATE TEXT", "Literal search across JSON, JSONL, TXT and LOG evidence under the YOMI data root.")
            {
                Badge = "GREP",
                Action = delegate(DeepSystemsContext c, Window w) { OpenPromptSearch(w, c, "GLOBAL EVIDENCE SEARCH", new List<string> { "ATLAS", "FORENSICS", "SEARCH" }); }
            });
            return n;
        }

        private static List<DeepNode> BuildTools(DeepSystemsContext ctx)
        {
            string[] scripts = { "operations-center.ps1", "session-explorer.ps1", "oracle-lab.ps1", "incident-replay.ps1", "recovery-point.ps1", "config-history.ps1", "doctor.ps1", "aegis-repair.ps1", "support-bundle.ps1", "self-test.ps1", "preflight.ps1", "reset-oracle-learning.ps1" };
            var n = new List<DeepNode>();
            foreach (string script in scripts)
            {
                string path = Path.Combine(ctx.AppDir, script);
                if (!File.Exists(path)) continue;
                string captured = path;
                n.Add(new DeepNode(Path.GetFileNameWithoutExtension(script).Replace('-', ' ').ToUpperInvariant(), "Launch installed subsystem · " + script)
                {
                    Badge = "TOOL",
                    Action = delegate(DeepSystemsContext c, Window w) { LaunchPowerShell(captured); }
                });
            }
            n.Add(Portal("INSTALLED APP DIRECTORY", "If there are tools we didn't curate, go discover them directly.", "FILES", delegate(DeepSystemsContext c) { return DirectoryNodes(c.AppDir, 0); }));
            return n;
        }

        private static List<DeepNode> BuildNotebook(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                if (Bookmarks.Count == 0) return new List<DeepNode> { Missing("The notebook is empty. Pin something from any deep-system window.") };
                var n = new List<DeepNode>();
                foreach (DeepBookmark b in Bookmarks.AsEnumerable().Reverse())
                {
                    DeepBookmark captured = b;
                    n.Add(new DeepNode(b.Title, b.Summary + " · saved " + b.SavedUtc)
                    {
                        Badge = "PIN",
                        FilePath = b.FilePath,
                        JsonPath = b.JsonPath,
                        Action = String.IsNullOrWhiteSpace(b.FilePath) ? (Action<DeepSystemsContext, Window>)delegate(DeepSystemsContext c, Window w)
                        {
                            OpenPortal(w, c, captured.Title, captured.Summary, new List<DeepNode> { Missing("This bookmark records route context only: " + captured.Route) }, SplitRoute(captured.Route), "PIN");
                        } : null
                    });
                }
                return n;
            }
        }

        private static List<DeepNode> BuildSnapshotVault(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                if (Snapshots.Count == 0) return new List<DeepNode> { Missing("No temporal snapshots yet. Use SNAPSHOT from an object microscope window.") };
                var n = new List<DeepNode>();
                foreach (DeepSnapshot s in Snapshots.AsEnumerable().Reverse())
                {
                    DeepSnapshot captured = s;
                    n.Add(new DeepNode(s.Title, "Captured " + s.CapturedUtc + " · " + s.Route)
                    {
                        Badge = "T-" + ShortId(s.Id),
                        Action = delegate(DeepSystemsContext c, Window w)
                        {
                            object oldValue = SafeDeserialize(c, captured.Json);
                            OpenObjectWindow(w, c, captured.Title + " // SNAPSHOT", "Historical captured object", oldValue, captured.FilePath, captured.JsonPath, SplitRoute(captured.Route).Concat(new string[] { "SNAPSHOT" }).ToList(), true);
                        }
                    });
                }
                return n;
            }
        }

        private static List<DeepNode> BuildConstellation(DeepSystemsContext ctx)
        {
            var result = new List<DeepNode>();
            lock (Gate)
            {
                for (int i = OpenWindows.Count - 1; i >= 0; i--)
                {
                    Window w = OpenWindows[i].Window.Target as Window;
                    if (w == null || !w.IsLoaded) { OpenWindows.RemoveAt(i); continue; }
                    Window captured = w;
                    DeepWindowRecord rec = OpenWindows[i];
                    result.Add(new DeepNode("DEPTH " + rec.Depth.ToString(CultureInfo.InvariantCulture) + " · " + ShortRoute(rec.Route), "Opened " + rec.OpenedUtc.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z · " + rec.Route)
                    {
                        Badge = "WINDOW",
                        Action = delegate(DeepSystemsContext c, Window owner) { try { if (captured.WindowState == WindowState.Minimized) captured.WindowState = WindowState.Normal; captured.Activate(); captured.Topmost = true; captured.Topmost = false; captured.Focus(); } catch { } }
                    });
                }
            }
            if (result.Count == 0) result.Add(Missing("No deep-system windows are alive."));
            return result;
        }

        internal static List<string> ExpeditionWindowRoutes()
        {
            var rows = new List<string>();
            lock (Gate)
            {
                for (int i = OpenWindows.Count - 1; i >= 0; i--)
                {
                    Window w = OpenWindows[i].Window.Target as Window;
                    if (w == null || !w.IsLoaded) { OpenWindows.RemoveAt(i); continue; }
                    DeepWindowRecord rec = OpenWindows[i];
                    rows.Add("DEPTH " + rec.Depth.ToString(CultureInfo.InvariantCulture) + " · " + rec.Route + " · " + (w.Title ?? "WINDOW"));
                }
            }
            return rows;
        }

        private static void AddMapBranch(List<DeepNode> target, Dictionary<string, object> map, string key, string title, string subtitle, string badge)
        {
            if (map == null || !map.ContainsKey(key))
            {
                target.Add(new DeepNode(title, subtitle + " · not currently projected") { Badge = "ABSENT", Value = new Dictionary<string, object> { { "projection", "not materialized" }, { "field", key } } });
                return;
            }
            target.Add(new DeepNode(title, subtitle) { Badge = badge, Value = map[key], JsonPath = "$." + key });
        }

        private static DeepNode Missing(string text)
        {
            return new DeepNode("NO MATERIALIZED STATE", text) { Badge = "EMPTY", Value = text };
        }

        private static void OpenNode(Window owner, DeepSystemsContext ctx, DeepNode node, List<string> route)
        {
            var nextRoute = new List<string>(route);
            nextRoute.Add(node.Title);
            if (node.Action != null)
            {
                node.Action(ctx, owner);
                return;
            }
            if (!String.IsNullOrWhiteSpace(node.FilePath))
            {
                OpenFile(owner, ctx, node.FilePath, String.IsNullOrWhiteSpace(node.JsonPath) ? "$" : node.JsonPath, nextRoute);
                return;
            }
            if (node.Value != null)
            {
                if (IsComplex(node.Value)) OpenObjectWindow(owner, ctx, node.Title, node.Subtitle, node.Value, null, node.JsonPath, nextRoute, false);
                else OpenScalarWindow(owner, ctx, node.Title, node.Subtitle, node.Value, null, node.JsonPath, nextRoute);
                return;
            }
            if (node.Children != null)
            {
                List<DeepNode> children;
                try { children = node.Children(ctx) ?? new List<DeepNode>(); }
                catch (Exception ex) { children = new List<DeepNode> { Missing("Portal failed to materialize: " + ex.Message) }; }
                OpenPortal(owner, ctx, node.Title, node.Subtitle, children, nextRoute, node.Badge);
            }
        }

        private static void OpenPortal(Window owner, DeepSystemsContext ctx, string title, string subtitle, List<DeepNode> nodes, List<string> route, string badge)
        {
            var window = MakeWindow(owner, title, subtitle, route, badge);
            var host = ((Border)window.Content).Child as DockPanel;
            if (host == null) return;
            var search = new TextBox { Margin = new Thickness(18, 8, 18, 8), Height = 34, Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), FontSize = 13, ToolTip = "Filter this layer only. Deeper layers remain untouched." };
            DockPanel.SetDock(search, Dock.Top);
            host.Children.Add(search);
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(12, 0, 12, 12) };
            var stack = new StackPanel();
            scroll.Content = stack;
            host.Children.Add(scroll);

            Action render = delegate
            {
                stack.Children.Clear();
                string q = (search.Text ?? "").Trim();
                IEnumerable<DeepNode> visible = nodes ?? new List<DeepNode>();
                if (q.Length > 0)
                    visible = visible.Where(x => (x.Title + " " + x.Subtitle + " " + x.Badge).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                int shown = 0;
                foreach (DeepNode node in visible.Take(1000))
                {
                    var card = MakeNodeCard(node);
                    card.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
                    {
                        if (e.ClickCount >= 1) OpenNode(window, ctx, node, route);
                    };
                    stack.Children.Add(card);
                    shown++;
                }
                if (shown == 0) stack.Children.Add(MakeEmpty("Nothing in this layer matches your filter."));
                if (visible.Skip(1000).Any()) stack.Children.Add(MakeEmpty("Layer truncated at 1000 portals. Narrow the filter to continue."));
            };
            search.TextChanged += delegate { render(); };
            render();
            ShowDeepWindow(window, route);
        }

        private static Border MakeNodeCard(DeepNode node)
        {
            var border = new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Margin = new Thickness(6), Padding = new Thickness(14), Cursor = Cursors.Hand };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var textStack = new StackPanel();
            var title = new TextBlock { Text = node.Title ?? "", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            var sub = new TextBlock { Text = node.Subtitle ?? "", Foreground = Muted, FontSize = 12, Margin = new Thickness(0, 5, 0, 0), TextWrapping = TextWrapping.Wrap };
            textStack.Children.Add(title); textStack.Children.Add(sub);
            Grid.SetColumn(textStack, 0); grid.Children.Add(textStack);
            var badge = new Border { Background = Raised, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            badge.Child = new TextBlock { Text = node.Badge ?? "PORTAL", Foreground = Accent, FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold };
            Grid.SetColumn(badge, 1); grid.Children.Add(badge);
            border.Child = grid;
            return border;
        }

        private static Window MakeWindow(Window owner, string title, string subtitle, List<string> route, string badge)
        {
            var w = new Window();
            w.Title = "YOMI // DEEP SYSTEMS // " + title;
            w.Width = 820;
            w.Height = 680;
            w.MinWidth = 620;
            w.MinHeight = 460;
            w.Background = Bg;
            w.Foreground = Text;
            w.WindowStartupLocation = WindowStartupLocation.Manual;
            if (owner != null)
            {
                try
                {
                    w.Owner = owner;
                    double offset = 26 + Math.Min(160, route.Count * 12);
                    w.Left = owner.Left + offset;
                    w.Top = owner.Top + 34;
                }
                catch { w.WindowStartupLocation = WindowStartupLocation.CenterScreen; }
            }
            else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var root = new Border { Background = Bg, BorderBrush = Border, BorderThickness = new Thickness(1) };
            var dock = new DockPanel(); root.Child = dock; w.Content = root;
            var header = new Border { Background = Raised, BorderBrush = Border, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(18, 14, 18, 12) };
            DockPanel.SetDock(header, Dock.Top); dock.Children.Add(header);
            var headerGrid = new Grid(); headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var left = new StackPanel();
            left.Children.Add(new TextBlock { Text = "YOMI / DEEP SYSTEMS / DEPTH " + Math.Max(0, route.Count - 1).ToString(CultureInfo.InvariantCulture), Foreground = Accent, FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold });
            left.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap });
            left.Children.Add(new TextBlock { Text = subtitle ?? "", Foreground = Muted, FontSize = 12, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            left.Children.Add(new TextBlock { Text = String.Join("  ›  ", route.ToArray()), Foreground = Blue, FontFamily = new FontFamily("Consolas"), FontSize = 10, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap });
            Grid.SetColumn(left, 0); headerGrid.Children.Add(left);
            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
            var pin = SmallButton("PIN"); pin.ToolTip = "Pin this route in the Discovery Notebook";
            pin.Click += delegate { PinRoute(LastContext, title, route, "", "$", subtitle ?? ""); };
            var constellation = SmallButton("MAP"); constellation.ToolTip = "Open window constellation"; constellation.Margin = new Thickness(6, 0, 0, 0);
            constellation.Click += delegate { if (LastContext != null) OpenPortal(w, LastContext, "OPEN WINDOW CONSTELLATION", "Every live Deep Systems window", BuildConstellation(LastContext), route.Concat(new string[] { "CONSTELLATION" }).ToList(), "MAP"); };
            right.Children.Add(pin); right.Children.Add(constellation);
            Grid.SetColumn(right, 1); headerGrid.Children.Add(right);
            header.Child = headerGrid;

            w.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { w.Close(); e.Handled = true; }
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.W) { w.Close(); e.Handled = true; }
            };
            return w;
        }

        private static Button SmallButton(string text)
        {
            return new Button { Content = text, Height = 30, MinWidth = 46, Padding = new Thickness(9, 3, 9, 3), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand };
        }

        private static Border MakeEmpty(string text)
        {
            return new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Margin = new Thickness(6), Padding = new Thickness(16), Child = new TextBlock { Text = text, Foreground = Muted, FontSize = 13, TextWrapping = TextWrapping.Wrap } };
        }

        private static void ShowDeepWindow(Window w, List<string> route)
        {
            var rec = new DeepWindowRecord { Window = new WeakReference(w), Route = String.Join(" / ", route.ToArray()), Depth = Math.Max(0, route.Count - 1), OpenedUtc = DateTime.UtcNow };
            lock (Gate) OpenWindows.Add(rec);
            w.Closed += delegate { lock (Gate) OpenWindows.Remove(rec); };
            w.Show();
        }

        private static void OpenFile(Window owner, DeepSystemsContext ctx, string path, string jsonPath, List<string> route)
        {
            if (String.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                OpenPortal(owner, ctx, "MISSING ARTIFACT", "The portal exists conceptually, but the backing artifact is not materialized right now.", new List<DeepNode> { Missing(path ?? "(null)") }, route, "MISSING");
                return;
            }
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".json")
            {
                object root = ReadJson(ctx, path);
                object value = ResolveJsonPath(root, jsonPath);
                OpenObjectWindow(owner, ctx, Path.GetFileName(path).ToUpperInvariant(), DescribeFile(path), value, path, jsonPath, route, false);
            }
            else if (ext == ".jsonl")
            {
                OpenPortal(owner, ctx, Path.GetFileName(path).ToUpperInvariant(), "Append-only line stream · " + DescribeFile(path), JsonLineNodes(ctx, path, 800, "RECORD"), route, "JSONL");
            }
            else if (IsBinaryExtension(ext))
            {
                OpenBinaryWindow(owner, ctx, path, route);
            }
            else
            {
                OpenTextWindow(owner, ctx, path, route);
            }
        }

        private static void OpenObjectWindow(Window owner, DeepSystemsContext ctx, string title, string subtitle, object value, string filePath, string jsonPath, List<string> route, bool historical)
        {
            var window = MakeWindow(owner, title, subtitle, route, historical ? "SNAPSHOT" : "OBJECT");
            var host = ((Border)window.Content).Child as DockPanel;
            object currentValue = value;
            List<DeepNode> children = BuildObjectLayer(ctx, currentValue, jsonPath, filePath);

            var tools = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 8, 18, 8) };
            DockPanel.SetDock(tools, Dock.Top); host.Children.Add(tools);
            var snapshot = SmallButton("SNAPSHOT"); snapshot.IsEnabled = !historical; snapshot.ToolTip = "Persist a bounded temporal capture of this exact object";
            snapshot.Click += delegate { SaveSnapshot(ctx, title, route, filePath, jsonPath, currentValue); };
            var pin = SmallButton("PIN DOSSIER"); pin.Margin = new Thickness(6, 0, 0, 0); pin.Click += delegate { PinRoute(ctx, title, route, filePath, jsonPath, Summarize(currentValue)); };
            var copy = SmallButton("COPY JSON"); copy.Margin = new Thickness(6, 0, 0, 0); copy.Click += delegate { TryCopy(ctx.Json.Serialize(currentValue)); };
            var diff = SmallButton("DIFF LATEST"); diff.Margin = new Thickness(6, 0, 0, 0); diff.ToolTip = "Compare this object to the newest matching temporal snapshot";
            diff.Click += delegate { OpenDiffAgainstLatest(window, ctx, title, route, filePath, jsonPath, currentValue); };
            var search = SmallButton("TRACE VALUE"); search.Margin = new Thickness(6, 0, 0, 0); search.Click += delegate { OpenPromptSearch(window, ctx, "TRACE FROM " + title, route.Concat(new string[] { "TRACE" }).ToList()); };
            var hadal = SmallButton("DESCEND FROM HERE"); hadal.Margin = new Thickness(6, 0, 0, 0); hadal.ToolTip = "Promote this exact object coordinate into a persistent Hadal descent"; hadal.Click += delegate { AbyssalKnowledgeEngine.DescendFromExternal(window, ctx, title, currentValue, filePath, jsonPath, String.Join(" › ", route.ToArray())); };
            var passport = SmallButton("WORLD PASSPORT"); passport.Margin = new Thickness(6, 0, 0, 0); passport.ToolTip = "Resolve this exact object against the Universal World Kernel"; passport.Click += delegate { WorldKernel.OpenFromExternal(window, ctx, title, currentValue, filePath, jsonPath, String.Join(" › ", route.ToArray())); };
            var spine = SmallButton("SAVE TO SPINE"); spine.Margin = new Thickness(6, 0, 0, 0); spine.ToolTip = "Persist this exact object fingerprint as a cross-civilization World Passport"; spine.Click += delegate { WorldPassportSpine.CaptureAndOpen(window, ctx, title, historical ? "SNAPSHOT" : "DEEP_OBJECT", "DEEP_SYSTEMS", "atlas", String.Join(" › ", route.ToArray()), currentValue, filePath, jsonPath); };
            var live = SmallButton("LIVE FOLLOW"); live.Margin = new Thickness(6, 0, 0, 0); live.IsEnabled = !historical && !String.IsNullOrWhiteSpace(filePath) && File.Exists(filePath) && String.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase);
            var liveState = new TextBlock { Text = "STATIC", Foreground = Muted, FontFamily = new FontFamily("Consolas"), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            tools.Children.Add(snapshot); tools.Children.Add(pin); tools.Children.Add(copy); tools.Children.Add(diff); tools.Children.Add(search); tools.Children.Add(hadal); tools.Children.Add(passport); tools.Children.Add(spine); tools.Children.Add(live); tools.Children.Add(liveState);

            var filter = new TextBox { Margin = new Thickness(18, 0, 18, 8), Height = 34, Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6), ToolTip = "Filter keys/indices in this object" };
            DockPanel.SetDock(filter, Dock.Top); host.Children.Add(filter);
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(12, 0, 12, 12) };
            var stack = new StackPanel(); scroll.Content = stack; host.Children.Add(scroll);
            Action render = delegate
            {
                stack.Children.Clear();
                string q = (filter.Text ?? "").Trim();
                IEnumerable<DeepNode> list = children;
                if (q.Length > 0) list = list.Where(x => (x.Title + " " + x.Subtitle).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                bool any = false;
                foreach (DeepNode node in list.Take(1200))
                {
                    any = true;
                    Border card = MakeNodeCard(node);
                    card.MouseLeftButtonDown += delegate { OpenNode(window, ctx, node, route); };
                    stack.Children.Add(card);
                }
                if (!any) stack.Children.Add(MakeEmpty("No children match."));
            };
            filter.TextChanged += delegate { render(); }; render();

            var followTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            DateTime lastWrite = DateTime.MinValue;
            string currentFingerprint = HashText(SafeSerialize(currentValue));
            bool following = false;
            followTimer.Tick += delegate
            {
                if (!following || String.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
                try
                {
                    DateTime write = File.GetLastWriteTimeUtc(filePath);
                    if (write == lastWrite) return;
                    lastWrite = write;
                    object root = ReadJson(ctx, filePath);
                    object next = ResolveJsonPath(root, jsonPath);
                    string nextFingerprint = HashText(SafeSerialize(next));
                    if (String.Equals(nextFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase)) return;
                    currentValue = next;
                    currentFingerprint = nextFingerprint;
                    children = BuildObjectLayer(ctx, currentValue, jsonPath, filePath);
                    liveState.Text = "LIVE · Δ " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " · " + ShortHash(currentFingerprint);
                    liveState.Foreground = Accent;
                    render();
                }
                catch
                {
                    liveState.Text = "LIVE · READ DEFERRED";
                    liveState.Foreground = Amber;
                }
            };
            live.Click += delegate
            {
                following = !following;
                live.Content = following ? "STOP FOLLOW" : "LIVE FOLLOW";
                liveState.Text = following ? "LIVE · WATCHING" : "STATIC";
                liveState.Foreground = following ? Accent : Muted;
                if (following)
                {
                    lastWrite = DateTime.MinValue;
                    followTimer.Start();
                }
                else followTimer.Stop();
            };
            window.Closed += delegate { followTimer.Stop(); };
            ShowDeepWindow(window, route);
        }

        private static List<DeepNode> BuildObjectLayer(DeepSystemsContext ctx, object value, string jsonPath, string filePath)
        {
            var children = ObjectChildren(ctx, value, jsonPath, filePath);
            var semantic = BuildSemanticLinks(ctx, value);
            if (semantic.Count > 0)
            {
                children.Insert(0, Portal("SEMANTIC CROSS-LINKS", "Auto-discovered doors based on identities and evidence inside this object.", "RELATED", delegate(DeepSystemsContext c) { return semantic; }));
            }
            string fingerprint = HashText(SafeSerialize(value));
            children.Insert(0, new DeepNode("OBJECT FINGERPRINT", "SHA-256 over this rendered object state. Trace it, pin it, or compare it later.") { Badge = "SHA256", Value = fingerprint, JsonPath = (jsonPath ?? "$") + ".@fingerprint" });
            children.Insert(1, new DeepNode("STRUCTURAL SHAPE", "A type/cardinality projection of this object without its values.") { Badge = "SCHEMA", Value = BuildShape(value, 0), JsonPath = (jsonPath ?? "$") + ".@shape" });
            return children;
        }

        private static void OpenScalarWindow(Window owner, DeepSystemsContext ctx, string title, string subtitle, object value, string filePath, string jsonPath, List<string> route)
        {
            var window = MakeWindow(owner, title, subtitle, route, "SCALAR");
            var host = ((Border)window.Content).Child as DockPanel;
            string text = value == null ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture);
            var tool = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 10, 18, 6) };
            DockPanel.SetDock(tool, Dock.Top); host.Children.Add(tool);
            var copy = SmallButton("COPY"); copy.Click += delegate { TryCopy(text); };
            var trace = SmallButton("FIND THIS VALUE"); trace.Margin = new Thickness(6, 0, 0, 0); trace.Click += delegate { OpenEvidenceSearch(window, ctx, text, route.Concat(new string[] { "VALUE TRACE" }).ToList()); };
            var pin = SmallButton("PIN"); pin.Margin = new Thickness(6, 0, 0, 0); pin.Click += delegate { PinRoute(ctx, title, route, filePath, jsonPath, text); };
            var hadal = SmallButton("DESCEND FROM HERE"); hadal.Margin = new Thickness(6, 0, 0, 0); hadal.Click += delegate { AbyssalKnowledgeEngine.DescendFromExternal(window, ctx, title, value, filePath, jsonPath, String.Join(" › ", route.ToArray())); };
            var passport = SmallButton("WORLD PASSPORT"); passport.Margin = new Thickness(6, 0, 0, 0); passport.Click += delegate { WorldKernel.OpenFromExternal(window, ctx, title, value, filePath, jsonPath, String.Join(" › ", route.ToArray())); };
            tool.Children.Add(copy); tool.Children.Add(trace); tool.Children.Add(pin); tool.Children.Add(hadal); tool.Children.Add(passport);
            var card = new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Margin = new Thickness(18), Padding = new Thickness(18) };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = jsonPath ?? "$", Foreground = Blue, FontFamily = new FontFamily("Consolas"), FontSize = 11, TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(new TextBlock { Text = text, Foreground = Text, FontFamily = new FontFamily("Consolas"), FontSize = 17, Margin = new Thickness(0, 14, 0, 0), TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(new TextBlock { Text = "TYPE  " + (value == null ? "null" : value.GetType().FullName), Foreground = Muted, FontFamily = new FontFamily("Consolas"), FontSize = 10, Margin = new Thickness(0, 16, 0, 0) });
            card.Child = stack; host.Children.Add(card);
            ShowDeepWindow(window, route);
        }

        private static List<DeepNode> ObjectChildren(DeepSystemsContext ctx, object value, string jsonPath, string filePath)
        {
            var result = new List<DeepNode>();
            var map = value as IDictionary<string, object>;
            if (map == null)
            {
                var nongeneric = value as IDictionary;
                if (nongeneric != null)
                {
                    foreach (DictionaryEntry de in nongeneric)
                    {
                        string key = Convert.ToString(de.Key, CultureInfo.InvariantCulture);
                        result.Add(ValueNode(key, de.Value, filePath, PathChild(jsonPath, key)));
                    }
                    return result;
                }
            }
            if (map != null)
            {
                foreach (var kv in map.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)) result.Add(ValueNode(kv.Key, kv.Value, filePath, PathChild(jsonPath, kv.Key)));
                return result;
            }
            var list = value as IEnumerable;
            if (list != null && !(value is string))
            {
                int index = 0;
                foreach (object item in list)
                {
                    result.Add(ValueNode("[" + index.ToString(CultureInfo.InvariantCulture) + "]", item, filePath, (jsonPath ?? "$") + "[" + index.ToString(CultureInfo.InvariantCulture) + "]"));
                    index++;
                    if (index >= 3000) break;
                }
                return result;
            }
            if (IsStructuredPoco(value))
            {
                var type = value.GetType();
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    object child = null; try { child = field.GetValue(value); } catch { }
                    result.Add(ValueNode(field.Name, child, filePath, PathChild(jsonPath, field.Name)));
                }
                foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Where(x => x.CanRead && x.GetIndexParameters().Length == 0).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (result.Any(x => String.Equals(x.Title, prop.Name, StringComparison.OrdinalIgnoreCase))) continue;
                    object child = null; try { child = prop.GetValue(value, null); } catch { }
                    result.Add(ValueNode(prop.Name, child, filePath, PathChild(jsonPath, prop.Name)));
                }
                if (result.Count > 0) return result;
            }
            result.Add(ValueNode("VALUE", value, filePath, jsonPath));
            return result;
        }

        private static DeepNode ValueNode(string key, object value, string filePath, string path)
        {
            bool complex = IsComplex(value);
            string summary = Summarize(value);
            return new DeepNode(key, summary)
            {
                Badge = complex ? (value is IDictionary ? "OBJECT" : "ARRAY") : "VALUE",
                Value = value,
                FilePath = filePath,
                JsonPath = path
            };
        }

        private static List<DeepNode> BuildSemanticLinks(DeepSystemsContext ctx, object value)
        {
            var result = new List<DeepNode>();
            var map = AsMap(value);
            if (map.Count == 0) return result;
            string[] identityKeys = { "source_key", "session_id", "occurrence_id", "occurrence", "entry_hash", "prev_hash", "plan_fingerprint", "route_label", "video_route_label", "failure_domain", "decision_reason", "order_revision", "work_generation", "intent_id" };
            foreach (string key in identityKeys)
            {
                if (!map.ContainsKey(key) || map[key] == null || IsComplex(map[key])) continue;
                string text = Convert.ToString(map[key], CultureInfo.InvariantCulture) ?? "";
                if (String.IsNullOrWhiteSpace(text)) continue;
                string captured = text;
                string capturedKey = key;
                result.Add(new DeepNode("TRACE " + key.ToUpperInvariant(), captured)
                {
                    Badge = "CROSS-LINK",
                    Action = delegate(DeepSystemsContext c, Window w) { OpenEvidenceSearch(w, c, captured, new List<string> { "ATLAS", "SEMANTIC", capturedKey, captured }); }
                });
            }
            foreach (var kv in map)
            {
                if (kv.Value == null || IsComplex(kv.Value)) continue;
                string text = Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? "";
                string key = kv.Key.ToLowerInvariant();
                if ((key.Contains("path") || key.Contains("file")) && File.Exists(text))
                {
                    result.Add(FileNode("OPEN REFERENCED FILE · " + kv.Key, text, "FILE LINK", text));
                }
                if ((key.Contains("url") || key == "source") && (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    string capturedUrl = text;
                    result.Add(new DeepNode("OPEN REFERENCED URL · " + kv.Key, capturedUrl)
                    {
                        Badge = "EXTERNAL",
                        Action = delegate(DeepSystemsContext c, Window w) { try { Process.Start(new ProcessStartInfo { FileName = capturedUrl, UseShellExecute = true }); } catch { } }
                    });
                }
            }
            return result;
        }

        private static object BuildShape(object value, int depth)
        {
            if (depth > 12) return "depth-limit";
            if (value == null) return "null";
            var map = AsMap(value);
            if (map.Count > 0)
            {
                var shape = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in map.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)) shape[kv.Key] = BuildShape(kv.Value, depth + 1);
                return shape;
            }
            var list = value as IEnumerable;
            if (list != null && !(value is string))
            {
                int count = 0; object first = null;
                foreach (object item in list) { if (count == 0) first = item; count++; if (count > 100000) break; }
                return new Dictionary<string, object> { { "type", "array" }, { "count", count }, { "item_shape", count > 0 ? BuildShape(first, depth + 1) : "empty" } };
            }
            return value.GetType().Name;
        }

        private static bool IsBinaryExtension(string ext)
        {
            string[] binary = { ".mp4", ".webm", ".mkv", ".m4a", ".mp3", ".ogg", ".opus", ".wav", ".flac", ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".ico", ".exe", ".dll", ".zip", ".7z", ".bin", ".dat" };
            return binary.Contains((ext ?? "").ToLowerInvariant());
        }

        private static void OpenBinaryWindow(Window owner, DeepSystemsContext ctx, string path, List<string> route)
        {
            var window = MakeWindow(owner, Path.GetFileName(path).ToUpperInvariant(), "Binary/media object dossier · " + DescribeFile(path), route, "BINARY");
            var host = ((Border)window.Content).Child as DockPanel;
            var toolbar = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 8, 18, 8) };
            DockPanel.SetDock(toolbar, Dock.Top); host.Children.Add(toolbar);
            var reveal = SmallButton("REVEAL"); reveal.Click += delegate { try { Process.Start("explorer.exe", "/select,\"" + path + "\""); } catch { } };
            var open = SmallButton("OPEN EXTERNAL"); open.Margin = new Thickness(6, 0, 0, 0); open.Click += delegate { try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { } };
            var trace = SmallButton("TRACE IDENTITY"); trace.Margin = new Thickness(6, 0, 0, 0); trace.Click += delegate
            {
                string stem = Path.GetFileNameWithoutExtension(path);
                string token = stem.Contains("-") ? stem.Split('-')[0] : stem;
                OpenEvidenceSearch(window, ctx, token, route.Concat(new string[] { "IDENTITY TRACE", token }).ToList());
            };
            var sha = SmallButton("COMPUTE SHA-256"); sha.Margin = new Thickness(6, 0, 0, 0);
            toolbar.Children.Add(reveal); toolbar.Children.Add(open); toolbar.Children.Add(trace); toolbar.Children.Add(sha);

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(18) };
            var stack = new StackPanel(); scroll.Content = stack; host.Children.Add(scroll);
            var info = new Dictionary<string, object>();
            try
            {
                var fi = new FileInfo(path);
                info["path"] = path;
                info["name"] = fi.Name;
                info["extension"] = fi.Extension;
                info["length_bytes"] = fi.Length;
                info["length_human"] = FormatBytes(fi.Length);
                info["created_utc"] = fi.CreationTimeUtc.ToString("o", CultureInfo.InvariantCulture);
                info["modified_utc"] = fi.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture);
                info["directory"] = fi.DirectoryName ?? "";
                info["first_256_bytes_hex"] = ReadHexPrefix(path, 256);
            }
            catch (Exception ex) { info["metadata_error"] = ex.Message; }

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".gif" || ext == ".bmp" || ext == ".ico")
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit(); bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.UriSource = new Uri(path); bmp.DecodePixelWidth = 720; bmp.EndInit(); bmp.Freeze();
                    var imageBorder = new Border { Background = Raised, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 12), MaxHeight = 360 };
                    imageBorder.Child = new Image { Source = bmp, Stretch = Stretch.Uniform, MaxHeight = 340 };
                    stack.Children.Add(imageBorder);
                    info["pixel_width"] = bmp.PixelWidth; info["pixel_height"] = bmp.PixelHeight;
                }
                catch (Exception ex) { info["preview_error"] = ex.Message; }
            }

            var metaCard = MakeNodeCard(new DeepNode("BINARY OBJECT METADATA", "Open the structured dossier and descend into individual fields.") { Badge = "META", Value = info });
            metaCard.MouseLeftButtonDown += delegate { OpenObjectWindow(window, ctx, "BINARY OBJECT METADATA", path, info, null, "$binary", route.Concat(new string[] { "METADATA" }).ToList(), false); };
            stack.Children.Add(metaCard);
            var shaText = new TextBlock { Text = "SHA-256  not computed (lazy by design)", Foreground = Muted, FontFamily = new FontFamily("Consolas"), FontSize = 11, Margin = new Thickness(4, 10, 4, 0), TextWrapping = TextWrapping.Wrap };
            stack.Children.Add(shaText);
            sha.Click += delegate
            {
                try
                {
                    string hash = HashFile(path);
                    shaText.Text = "SHA-256  " + hash;
                    shaText.Foreground = Accent;
                }
                catch (Exception ex) { shaText.Text = "SHA-256 FAILED  " + ex.Message; shaText.Foreground = Danger; }
            };
            ShowDeepWindow(window, route);
        }

        private static string ReadHexPrefix(string path, int count)
        {
            try
            {
                byte[] bytes = new byte[count]; int read;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) read = fs.Read(bytes, 0, bytes.Length);
                var sb = new StringBuilder(read * 3);
                for (int i = 0; i < read; i++) { if (i > 0) sb.Append(' '); sb.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture)); }
                return sb.ToString();
            }
            catch { return "unavailable"; }
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create()) using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] hash = sha.ComputeHash(fs); var sb = new StringBuilder(hash.Length * 2); foreach (byte b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); return sb.ToString();
            }
        }

        private static void OpenTextWindow(Window owner, DeepSystemsContext ctx, string path, List<string> route)
        {
            var window = MakeWindow(owner, Path.GetFileName(path).ToUpperInvariant(), DescribeFile(path), route, "TEXT");
            var host = ((Border)window.Content).Child as DockPanel;
            string text = ReadTextShared(path);
            bool truncated = false;
            if (text.Length > 1000000) { text = text.Substring(text.Length - 1000000); truncated = true; }
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 8, 18, 8) };
            DockPanel.SetDock(toolbar, Dock.Top); host.Children.Add(toolbar);
            var copy = SmallButton("COPY"); copy.Click += delegate { TryCopy(text); };
            var trace = SmallButton("SEARCH VALUE…"); trace.Margin = new Thickness(6, 0, 0, 0); trace.Click += delegate { OpenPromptSearch(window, ctx, "SEARCH FROM " + Path.GetFileName(path), route.Concat(new string[] { "SEARCH" }).ToList()); };
            toolbar.Children.Add(copy); toolbar.Children.Add(trace);
            var box = new TextBox { Text = (truncated ? "[VIEW TRUNCATED TO LAST 1,000,000 CHARACTERS]\r\n\r\n" : "") + text, Background = Bg, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(18), Padding = new Thickness(12), FontFamily = new FontFamily("Consolas"), FontSize = 11, IsReadOnly = true, AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            host.Children.Add(box);
            ShowDeepWindow(window, route);
        }

        private static List<DeepNode> JsonLineNodes(DeepSystemsContext ctx, string path, int limit, string label)
        {
            var result = new List<DeepNode>();
            if (!File.Exists(path)) return new List<DeepNode> { Missing("JSONL artifact is missing: " + path) };
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.UTF8); }
            catch { return new List<DeepNode> { Missing("Could not read JSONL artifact.") }; }
            int start = Math.Max(0, lines.Length - limit);
            for (int i = lines.Length - 1; i >= start; i--)
            {
                string line = lines[i]; if (String.IsNullOrWhiteSpace(line)) continue;
                object value = SafeDeserialize(ctx, line);
                var map = AsMap(value);
                string state = GetString(map, "state", GetString(map, "event", GetString(map, "type", "")));
                string id = GetString(map, "intent_id", GetString(map, "occurrence_id", ""));
                string hash = GetString(map, "entry_hash", "");
                string title = label + " #" + (i + 1).ToString(CultureInfo.InvariantCulture) + (String.IsNullOrWhiteSpace(state) ? "" : " · " + state);
                string subtitle = (String.IsNullOrWhiteSpace(id) ? "" : id + " · ") + (String.IsNullOrWhiteSpace(hash) ? "" : "hash " + ShortHash(hash));
                result.Add(new DeepNode(title, subtitle) { Badge = "LINE " + (i + 1).ToString(CultureInfo.InvariantCulture), Value = value, JsonPath = "$line[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]" });
            }
            if (result.Count == 0) result.Add(Missing("The JSONL stream is empty."));
            return result;
        }

        private static List<DeepNode> DirectoryNodes(string root, int depth)
        {
            var result = new List<DeepNode>();
            if (!Directory.Exists(root)) return new List<DeepNode> { Missing("Directory does not exist: " + root) };
            foreach (string d in SafeDirs(root).OrderBy(x => x).Take(500))
            {
                string captured = d;
                result.Add(Portal(Path.GetFileName(d).ToUpperInvariant(), "Directory · " + d, "DIR", delegate(DeepSystemsContext c) { return DirectoryNodes(captured, depth + 1); }));
            }
            foreach (string f in SafeFiles(root, "*", SearchOption.TopDirectoryOnly).OrderBy(x => x).Take(1000)) result.Add(FileNode(Path.GetFileName(f), DescribeFile(f), "FILE", f));
            if (result.Count == 0) result.Add(Missing("Directory is empty."));
            return result;
        }

        private static void OpenPromptSearch(Window owner, DeepSystemsContext ctx, string title, List<string> route)
        {
            var w = MakeWindow(owner, title, "Literal evidence search across YOMI state/cache/log text", route, "SEARCH");
            var host = ((Border)w.Content).Child as DockPanel;
            var box = new TextBox { Margin = new Thickness(18), Height = 36, Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(10, 6, 10, 6) };
            DockPanel.SetDock(box, Dock.Top); host.Children.Add(box);
            var go = new Button { Content = "TRACE THROUGH YOMI", Margin = new Thickness(18, 0, 18, 18), Height = 36, Background = Surface, Foreground = Accent, BorderBrush = Border, BorderThickness = new Thickness(1), FontWeight = FontWeights.Bold };
            DockPanel.SetDock(go, Dock.Top); host.Children.Add(go);
            host.Children.Add(MakeEmpty("Enter an occurrence ID, source key, session ID, hash fragment, filename, URL fragment, route label, error token, or literally anything you found down another rabbit hole."));
            go.Click += delegate { string q = (box.Text ?? "").Trim(); if (q.Length > 0) OpenEvidenceSearch(w, ctx, q, route.Concat(new string[] { q }).ToList()); };
            box.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { string q = (box.Text ?? "").Trim(); if (q.Length > 0) OpenEvidenceSearch(w, ctx, q, route.Concat(new string[] { q }).ToList()); e.Handled = true; } };
            ShowDeepWindow(w, route); box.Focus();
        }

        private static void OpenEvidenceSearch(Window owner, DeepSystemsContext ctx, string query, List<string> route)
        {
            var result = new List<DeepNode>();
            if (String.IsNullOrWhiteSpace(query)) return;
            string q = query.Trim();
            int filesSeen = 0; int matches = 0;
            foreach (string path in SafeFiles(ctx.DataRoot, "*", SearchOption.AllDirectories))
            {
                if (filesSeen++ > 2500 || matches >= 500) break;
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".json" && ext != ".jsonl" && ext != ".txt" && ext != ".log" && ext != ".pending" && ext != ".pid")
                {
                    if (Path.GetFileName(path).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(FileNode(Path.GetFileName(path), "FILENAME MATCH · " + Relative(ctx.DataRoot, path), "NAME", path)); matches++;
                    }
                    continue;
                }
                try
                {
                    FileInfo fi = new FileInfo(path);
                    if (fi.Length > 6 * 1024 * 1024) continue;
                    string text = ReadTextShared(path);
                    int pos = text.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                    if (pos < 0) continue;
                    int a = Math.Max(0, pos - 120); int b = Math.Min(text.Length, pos + q.Length + 180);
                    string snippet = text.Substring(a, b - a).Replace("\r", " ").Replace("\n", " ");
                    result.Add(FileNode(Path.GetFileName(path), Relative(ctx.DataRoot, path) + " · …" + snippet + "…", "HIT", path)); matches++;
                }
                catch { }
            }
            if (result.Count == 0) result.Add(Missing("No evidence matched: " + q));
            OpenPortal(owner, ctx, "EVIDENCE TRACE · " + q, matches.ToString(CultureInfo.InvariantCulture) + " matching artifacts · scanned " + Math.Min(filesSeen, 2500).ToString(CultureInfo.InvariantCulture) + " files", result, route, "TRACE");
        }

        private static void SaveSnapshot(DeepSystemsContext ctx, string title, List<string> route, string filePath, string jsonPath, object value)
        {
            try
            {
                string json = ctx.Json.Serialize(value);
                if (json.Length > 2097152)
                {
                    var sentinel = new Dictionary<string, object>();
                    sentinel["snapshot_omitted"] = true;
                    sentinel["reason"] = "serialized object exceeded 2 MiB Deep Systems snapshot ceiling";
                    sentinel["serialized_characters"] = json.Length;
                    sentinel["sha256"] = HashText(json);
                    json = ctx.Json.Serialize(sentinel);
                }
                var s = new DeepSnapshot { Id = Guid.NewGuid().ToString("N"), Title = title, Route = String.Join(" / ", route.ToArray()), FilePath = filePath ?? "", JsonPath = jsonPath ?? "$", Json = json, CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
                lock (Gate) { Snapshots.Add(s); while (Snapshots.Count > 12) Snapshots.RemoveAt(0); }
                PersistSnapshots(ctx);
            }
            catch { }
        }

        private static void OpenDiffAgainstLatest(Window owner, DeepSystemsContext ctx, string title, List<string> route, string filePath, string jsonPath, object current)
        {
            DeepSnapshot snap = null;
            lock (Gate)
            {
                for (int i = Snapshots.Count - 1; i >= 0; i--)
                {
                    DeepSnapshot s = Snapshots[i];
                    if (String.Equals(s.FilePath ?? "", filePath ?? "", StringComparison.OrdinalIgnoreCase) && String.Equals(s.JsonPath ?? "$", jsonPath ?? "$", StringComparison.Ordinal)) { snap = s; break; }
                }
            }
            if (snap == null)
            {
                OpenPortal(owner, ctx, "NO COMPARABLE SNAPSHOT", "Capture this object first, explore elsewhere, then come back and diff it.", new List<DeepNode> { Missing("No snapshot matches this file/path identity.") }, route.Concat(new string[] { "DIFF" }).ToList(), "TIME");
                return;
            }
            object old = SafeDeserialize(ctx, snap.Json);
            var diff = DiffObjects(old, current, "$", 0, 600);
            OpenPortal(owner, ctx, "TEMPORAL DIFF · " + title, "Snapshot " + snap.CapturedUtc + " → now · " + diff.Count.ToString(CultureInfo.InvariantCulture) + " changed paths", diff, route.Concat(new string[] { "DIFF" }).ToList(), "Δ");
        }

        private static List<DeepNode> DiffObjects(object oldValue, object newValue, string path, int depth, int budget)
        {
            var result = new List<DeepNode>();
            if (budget <= 0 || depth > 24) return result;
            string oldJson = SafeSerialize(oldValue); string newJson = SafeSerialize(newValue);
            if (String.Equals(oldJson, newJson, StringComparison.Ordinal)) return result;
            var a = AsMap(oldValue); var b = AsMap(newValue);
            if (a.Count > 0 || b.Count > 0)
            {
                var keys = new HashSet<string>(a.Keys, StringComparer.Ordinal); foreach (string k in b.Keys) keys.Add(k);
                foreach (string k in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    object av = a.ContainsKey(k) ? a[k] : null; object bv = b.ContainsKey(k) ? b[k] : null;
                    var inner = DiffObjects(av, bv, PathChild(path, k), depth + 1, budget - result.Count);
                    if (inner.Count == 0 && !String.Equals(SafeSerialize(av), SafeSerialize(bv), StringComparison.Ordinal))
                        result.Add(new DeepNode(PathChild(path, k), "OLD  " + Summarize(av) + "  →  NEW  " + Summarize(bv)) { Badge = "CHANGED", Value = new Dictionary<string, object> { { "old", av }, { "new", bv } } });
                    else result.AddRange(inner);
                    if (result.Count >= budget) break;
                }
                return result;
            }
            result.Add(new DeepNode(path, "OLD  " + Summarize(oldValue) + "  →  NEW  " + Summarize(newValue)) { Badge = "CHANGED", Value = new Dictionary<string, object> { { "old", oldValue }, { "new", newValue } } });
            return result;
        }

        private static void PinRoute(DeepSystemsContext ctx, string title, List<string> route, string filePath, string jsonPath, string summary)
        {
            if (ctx == null) return;
            var b = new DeepBookmark { Title = title ?? "Discovery", Route = String.Join(" / ", route.ToArray()), FilePath = filePath ?? "", JsonPath = jsonPath ?? "$", Summary = summary ?? "", SavedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
            lock (Gate) { Bookmarks.Add(b); while (Bookmarks.Count > 100) Bookmarks.RemoveAt(0); }
            PersistNotebook(ctx);
        }

        private static string NotebookPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-discovery-notebook.json"); }
        private static string SnapshotPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-deep-snapshots.json"); }

        private static void PersistNotebook(DeepSystemsContext ctx)
        {
            try
            {
                Directory.CreateDirectory(ctx.StateRoot);
                var rows = new List<Dictionary<string, object>>();
                lock (Gate) foreach (DeepBookmark b in Bookmarks) rows.Add(new Dictionary<string, object> { { "title", b.Title }, { "route", b.Route }, { "file_path", b.FilePath }, { "json_path", b.JsonPath }, { "summary", b.Summary }, { "saved_utc", b.SavedUtc } });
                var root = new Dictionary<string, object> { { "schema", 1 }, { "items", rows } };
                WriteAtomic(NotebookPath(ctx), ctx.Json.Serialize(root));
            }
            catch { }
        }

        private static void LoadNotebook(DeepSystemsContext ctx)
        {
            lock (Gate) Bookmarks.Clear();
            try
            {
                var root = AsMap(ReadJson(ctx, NotebookPath(ctx)));
                foreach (object raw in GetList(root, "items"))
                {
                    var m = AsMap(raw);
                    lock (Gate) Bookmarks.Add(new DeepBookmark { Title = GetString(m, "title", "Discovery"), Route = GetString(m, "route", ""), FilePath = GetString(m, "file_path", ""), JsonPath = GetString(m, "json_path", "$"), Summary = GetString(m, "summary", ""), SavedUtc = GetString(m, "saved_utc", "") });
                }
            }
            catch { }
        }

        private static void PersistSnapshots(DeepSystemsContext ctx)
        {
            try
            {
                Directory.CreateDirectory(ctx.StateRoot);
                var rows = new List<Dictionary<string, object>>();
                lock (Gate) foreach (DeepSnapshot s in Snapshots) rows.Add(new Dictionary<string, object> { { "id", s.Id }, { "title", s.Title }, { "route", s.Route }, { "file_path", s.FilePath }, { "json_path", s.JsonPath }, { "json", s.Json }, { "captured_utc", s.CapturedUtc } });
                var root = new Dictionary<string, object> { { "schema", 1 }, { "items", rows } };
                WriteAtomic(SnapshotPath(ctx), ctx.Json.Serialize(root));
            }
            catch { }
        }

        private static void LoadSnapshots(DeepSystemsContext ctx)
        {
            lock (Gate) Snapshots.Clear();
            try
            {
                var root = AsMap(ReadJson(ctx, SnapshotPath(ctx)));
                foreach (object raw in GetList(root, "items"))
                {
                    var m = AsMap(raw);
                    lock (Gate) Snapshots.Add(new DeepSnapshot { Id = GetString(m, "id", Guid.NewGuid().ToString("N")), Title = GetString(m, "title", "Snapshot"), Route = GetString(m, "route", ""), FilePath = GetString(m, "file_path", ""), JsonPath = GetString(m, "json_path", "$"), Json = GetString(m, "json", "null"), CapturedUtc = GetString(m, "captured_utc", "") });
                }
            }
            catch { }
        }

        private static object ReadJson(DeepSystemsContext ctx, string path)
        {
            try { if (!File.Exists(path)) return new Dictionary<string, object> { { "missing", path } }; return ctx.Json.DeserializeObject(ReadTextShared(path)); }
            catch (Exception ex) { return new Dictionary<string, object> { { "parse_error", ex.Message }, { "path", path } }; }
        }

        private static object SafeDeserialize(DeepSystemsContext ctx, string text)
        {
            try { return ctx.Json.DeserializeObject(text ?? "null"); } catch { return text; }
        }

        private static string SafeSerialize(object value)
        {
            try { return new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 128 }.Serialize(value); } catch { return Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; }
        }

        private static Dictionary<string, object> AsMap(object value)
        {
            var d = value as Dictionary<string, object>; if (d != null) return d;
            var generic = value as IDictionary<string, object>; if (generic != null) return new Dictionary<string, object>(generic, StringComparer.OrdinalIgnoreCase);
            var ng = value as IDictionary;
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (ng != null) foreach (DictionaryEntry de in ng) result[Convert.ToString(de.Key, CultureInfo.InvariantCulture)] = de.Value;
            return result;
        }

        private static List<object> GetList(Dictionary<string, object> map, string key)
        {
            var result = new List<object>(); if (map == null || !map.ContainsKey(key) || map[key] == null) return result;
            var e = map[key] as IEnumerable; if (e == null || map[key] is string) return result;
            foreach (object o in e) result.Add(o); return result;
        }

        private static string GetString(Dictionary<string, object> map, string key, string fallback)
        {
            if (map == null || !map.ContainsKey(key) || map[key] == null) return fallback; return Convert.ToString(map[key], CultureInfo.InvariantCulture) ?? fallback;
        }

        private static int GetIntAny(Dictionary<string, object> map, string[] keys, int fallback)
        {
            foreach (string key in keys)
            {
                if (map != null && map.ContainsKey(key) && map[key] != null)
                {
                    int v; if (Int32.TryParse(Convert.ToString(map[key], CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
                }
            }
            return fallback;
        }

        private static bool IsComplex(object value)
        {
            return value is IDictionary || (value is IEnumerable && !(value is string)) || IsStructuredPoco(value);
        }

        private static bool IsStructuredPoco(object value)
        {
            if (value == null || value is string) return false;
            Type t = value.GetType();
            if (t.IsPrimitive || t.IsEnum || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || value is TimeSpan) return false;
            string ns = t.Namespace ?? "";
            if (ns.StartsWith("System", StringComparison.Ordinal)) return false;
            return t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Length > 0 ||
                   t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Any(x => x.CanRead && x.GetIndexParameters().Length == 0);
        }

        private static string Summarize(object value)
        {
            if (value == null) return "null";
            var map = value as IDictionary; if (map != null) return "object · " + map.Count.ToString(CultureInfo.InvariantCulture) + " fields";
            var list = value as IList; if (list != null) return "array · " + list.Count.ToString(CultureInfo.InvariantCulture) + " items";
            if (IsStructuredPoco(value)) return "object · " + value.GetType().Name;
            string s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; s = s.Replace("\r", " ").Replace("\n", " "); if (s.Length > 180) s = s.Substring(0, 177) + "…"; return s;
        }

        private static object ResolveJsonPath(object root, string path)
        {
            // For persisted bookmarks we intentionally support conservative $.key[index] walking.
            if (root == null || String.IsNullOrWhiteSpace(path) || path == "$" || !path.StartsWith("$", StringComparison.Ordinal)) return root;
            object current = root; int i = 1;
            while (i < path.Length)
            {
                if (path[i] == '.')
                {
                    i++; int start = i; while (i < path.Length && path[i] != '.' && path[i] != '[') i++;
                    string key = path.Substring(start, i - start);
                    var m = AsMap(current); if (!m.ContainsKey(key)) return root; current = m[key];
                }
                else if (path[i] == '[')
                {
                    int close = path.IndexOf(']', i); if (close < 0) return root;
                    string token = path.Substring(i + 1, close - i - 1);
                    if (token.Length >= 2 && token[0] == '\'' && token[token.Length - 1] == '\'')
                    {
                        string key = token.Substring(1, token.Length - 2).Replace("\\'", "'");
                        var m = AsMap(current); if (!m.ContainsKey(key)) return root; current = m[key]; i = close + 1; continue;
                    }
                    int index; if (!Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return root;
                    var e = current as IEnumerable; if (e == null || current is string) return root;
                    int n = 0; object found = null; bool ok = false; foreach (object o in e) { if (n++ == index) { found = o; ok = true; break; } }
                    if (!ok) return root; current = found; i = close + 1;
                }
                else i++;
            }
            return current;
        }

        private static string PathChild(string parent, string key)
        {
            if (String.IsNullOrWhiteSpace(parent)) parent = "$";
            bool simple = key.All(ch => Char.IsLetterOrDigit(ch) || ch == '_' || ch == '-');
            return simple ? parent + "." + key : parent + "['" + key.Replace("'", "\\'") + "']";
        }

        private static string ResolveIntentJournal(DeepSystemsContext ctx)
        {
            try
            {
                var head = AsMap(ReadJson(ctx, Path.Combine(ctx.StateRoot, "controller-intent-head.json")));
                string f = GetString(head, "journal_file", "controller-intent-journal.jsonl");
                string candidate = Path.Combine(ctx.StateRoot, Path.GetFileName(f));
                return candidate;
            }
            catch { return Path.Combine(ctx.StateRoot, "controller-intent-journal.jsonl"); }
        }

        private static void LaunchPowerShell(string path)
        {
            try { Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + path + "\"", UseShellExecute = true }); } catch { }
        }

        private static string ReadTextShared(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (var sr = new StreamReader(fs, Encoding.UTF8, true)) return sr.ReadToEnd();
        }

        private static IEnumerable<string> SafeFiles(string root, string pattern, SearchOption option)
        {
            try { if (!Directory.Exists(root)) return new string[0]; return Directory.EnumerateFiles(root, pattern, option).ToArray(); } catch { return new string[0]; }
        }
        private static IEnumerable<string> SafeDirs(string root)
        {
            try { if (!Directory.Exists(root)) return new string[0]; return Directory.EnumerateDirectories(root).ToArray(); } catch { return new string[0]; }
        }

        private static string HashText(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static string DescribeFile(string path)
        {
            try { var fi = new FileInfo(path); return FormatBytes(fi.Length) + " · modified " + fi.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z · " + path; } catch { return path ?? ""; }
        }
        private static string FormatBytes(long n)
        {
            if (n >= 1073741824L) return (n / 1073741824.0).ToString("0.00", CultureInfo.InvariantCulture) + " GiB";
            if (n >= 1048576L) return (n / 1048576.0).ToString("0.00", CultureInfo.InvariantCulture) + " MiB";
            if (n >= 1024L) return (n / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KiB";
            return n.ToString(CultureInfo.InvariantCulture) + " B";
        }
        private static string Relative(string root, string path)
        {
            try { if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return path.Substring(root.Length).TrimStart('\\', '/'); } catch { } return path;
        }
        private static string ShortHash(string s) { if (String.IsNullOrWhiteSpace(s)) return "—"; return s.Length <= 12 ? s : s.Substring(0, 12); }
        private static string ShortId(string s) { if (String.IsNullOrWhiteSpace(s)) return "—"; return s.Length <= 6 ? s.ToUpperInvariant() : s.Substring(0, 6).ToUpperInvariant(); }
        private static string ShortRoute(string s) { if (String.IsNullOrWhiteSpace(s)) return "ATLAS"; return s.Length <= 64 ? s : "…" + s.Substring(s.Length - 61); }
        private static List<string> SplitRoute(string s) { return String.IsNullOrWhiteSpace(s) ? new List<string> { "ATLAS" } : s.Split(new string[] { " / ", " › " }, StringSplitOptions.RemoveEmptyEntries).ToList(); }
        private static void TryCopy(string text) { try { Clipboard.SetText(text ?? ""); } catch { } }

        private static void WriteAtomic(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp." + Guid.NewGuid().ToString("N");
            File.WriteAllText(tmp, text ?? "", new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(tmp, path, null, true); else File.Move(tmp, path);
        }
    }
}

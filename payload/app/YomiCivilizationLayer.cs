using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Yomi.ProductShell
{
    internal sealed class CivilInstitution
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Charter { get; set; }
        public string JurisdictionQuery { get; set; }
        public string ParentInstitutionId { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string LastOpenedUtc { get; set; }
        public string BaselineFingerprint { get; set; }
        public string Status { get; set; }
        public int CharterRevision { get; set; }
        public Dictionary<string, string> BaselineNodes { get; set; }
        public List<string> TrackedIdentities { get; set; }
        public List<CivilAmendment> Amendments { get; set; }
        public CivilInstitution()
        {
            BaselineNodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TrackedIdentities = new List<string>();
            Amendments = new List<CivilAmendment>();
        }
        public override string ToString()
        {
            return (Status ?? "ACTIVE") + "   ·   " + (Name ?? "INSTITUTION") + "   ·   " + (JurisdictionQuery ?? "STATS") + (String.IsNullOrWhiteSpace(ParentInstitutionId) ? "" : "   ·   CHILD");
        }
    }

    internal sealed class CivilAmendment
    {
        public string Id { get; set; }
        public string CreatedUtc { get; set; }
        public string Kind { get; set; }
        public string Summary { get; set; }
        public string BeforeHash { get; set; }
        public string AfterHash { get; set; }
        public string PreviousCharter { get; set; }
        public string PreviousJurisdictionQuery { get; set; }
        public override string ToString() { return (CreatedUtc ?? "") + "   ·   " + (Kind ?? "AMENDMENT") + "   ·   " + (Summary ?? ""); }
    }

    internal sealed class CivilExpedition
    {
        public string Id { get; set; }
        public string InstitutionId { get; set; }
        public string Name { get; set; }
        public string RootNodeId { get; set; }
        public string CurrentNodeId { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<string> VisitedNodeIds { get; set; }
        public List<string> FrontierNodeIds { get; set; }
        public List<string> Findings { get; set; }
        public CivilExpedition()
        {
            VisitedNodeIds = new List<string>();
            FrontierNodeIds = new List<string>();
            Findings = new List<string>();
        }
        public override string ToString()
        {
            return (Status ?? "ACTIVE") + "   ·   " + (Name ?? "EXPEDITION") + "   ·   visited " + VisitedNodeIds.Count.ToString(CultureInfo.InvariantCulture) + "   ·   frontier " + FrontierNodeIds.Count.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class CivilAlert
    {
        public string Id { get; set; }
        public string SentinelId { get; set; }
        public string InstitutionId { get; set; }
        public string CreatedUtc { get; set; }
        public string Severity { get; set; }
        public string Summary { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }
        public List<string> NodeIds { get; set; }
        public CivilAlert() { NodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "OPEN") + "   ·   " + (Severity ?? "INFO") + "   ·   " + (Summary ?? "ALERT"); }
    }

    internal sealed class CivilSentinel
    {
        public string Id { get; set; }
        public string InstitutionId { get; set; }
        public string Name { get; set; }
        public string Query { get; set; }
        public string CreatedUtc { get; set; }
        public string LastScanUtc { get; set; }
        public string LastFingerprint { get; set; }
        public Dictionary<string, string> LastNodes { get; set; }
        public CivilSentinel() { LastNodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return (Name ?? "SENTINEL") + "   ·   " + (Query ?? "STATS") + (String.IsNullOrWhiteSpace(LastScanUtc) ? "   ·   UNSCANNED" : "   ·   " + LastScanUtc); }
    }

    internal sealed class CivilResolution
    {
        public string Id { get; set; }
        public string InstitutionId { get; set; }
        public string Title { get; set; }
        public string Proposition { get; set; }
        public string EpistemicClass { get; set; }
        public string Status { get; set; }
        public string OriginAlertId { get; set; }
        public string OriginExpeditionId { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public List<string> Deliberation { get; set; }
        public CivilResolution()
        {
            EvidenceNodeIds = new List<string>();
            Deliberation = new List<string>();
        }
        public override string ToString() { return (Status ?? "DRAFT") + "   ·   " + (EpistemicClass ?? "QUESTION") + "   ·   " + (Title ?? "RESOLUTION"); }
    }

    internal sealed class CivilCompact
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string EvidenceFingerprint { get; set; }
        public string LastEvaluatedUtc { get; set; }
        public List<string> InstitutionIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CivilCompact()
        {
            InstitutionIds = new List<string>();
            EvidenceNodeIds = new List<string>();
        }
        public override string ToString() { return (Status ?? "PROPOSED") + "   ·   " + (Name ?? "COMPACT") + "   ·   " + InstitutionIds.Count.ToString(CultureInfo.InvariantCulture) + " institutions"; }
    }

    internal sealed class CivilCensusEpoch
    {
        public string Id { get; set; }
        public string CapturedUtc { get; set; }
        public string WorldFingerprint { get; set; }
        public string CoverageFingerprint { get; set; }
        public string TreatyFingerprint { get; set; }
        public int WorldNodes { get; set; }
        public int WorldEdges { get; set; }
        public int ClaimedNodes { get; set; }
        public int ActiveInstitutions { get; set; }
        public int TreatyBridges { get; set; }
        public int OpenAlerts { get; set; }
        public int ActiveExpeditions { get; set; }
        public int OpenResolutions { get; set; }
        public int RatifiedCompacts { get; set; }
        public int KnowledgeArticles { get; set; }
        public int PluralityObjects { get; set; }
        public Dictionary<string, string> JurisdictionFingerprints { get; set; }
        public CivilCensusEpoch() { JurisdictionFingerprints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return (CapturedUtc ?? "") + "   ·   coverage " + ClaimedNodes.ToString(CultureInfo.InvariantCulture) + "/" + WorldNodes.ToString(CultureInfo.InvariantCulture) + "   ·   " + ActiveInstitutions.ToString(CultureInfo.InvariantCulture) + " institutions"; }
    }

    internal sealed class CivilKnowledgeRevision
    {
        public string Id { get; set; }
        public string CreatedUtc { get; set; }
        public string PreviousTitle { get; set; }
        public string PreviousBody { get; set; }
        public string PreviousEpistemicClass { get; set; }
        public string BeforeHash { get; set; }
        public string AfterHash { get; set; }
        public override string ToString() { return (CreatedUtc ?? "") + "   ·   " + (PreviousEpistemicClass ?? "NOTE") + "   ·   " + (PreviousTitle ?? "REVISION"); }
    }

    internal sealed class CivilKnowledgeArticle
    {
        public string Id { get; set; }
        public string InstitutionId { get; set; }
        public string NodeId { get; set; }
        public string SourceNodeFingerprint { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string BodyHash { get; set; }
        public string EpistemicClass { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<string> Tags { get; set; }
        public List<CivilKnowledgeRevision> Revisions { get; set; }
        public CivilKnowledgeArticle()
        {
            Tags = new List<string>();
            Revisions = new List<CivilKnowledgeRevision>();
        }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (EpistemicClass ?? "INTERPRETATION") + "   ·   " + (Title ?? "ARTICLE") + "   ·   r" + (Revisions.Count + 1).ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class CivilizationStore
    {
        public int Schema { get; set; }
        public long Revision { get; set; }
        public string UpdatedUtc { get; set; }
        public string LastCommitId { get; set; }
        public List<CivilInstitution> Institutions { get; set; }
        public List<CivilExpedition> Expeditions { get; set; }
        public List<CivilSentinel> Sentinels { get; set; }
        public List<CivilAlert> Alerts { get; set; }
        public List<CivilResolution> Resolutions { get; set; }
        public List<CivilCompact> Compacts { get; set; }
        public List<CivilCensusEpoch> CensusEpochs { get; set; }
        public List<CivilKnowledgeArticle> KnowledgeArticles { get; set; }
        public CivilizationStore()
        {
            Schema = 2;
            Institutions = new List<CivilInstitution>();
            Expeditions = new List<CivilExpedition>();
            Sentinels = new List<CivilSentinel>();
            Alerts = new List<CivilAlert>();
            Resolutions = new List<CivilResolution>();
            Compacts = new List<CivilCompact>();
            CensusEpochs = new List<CivilCensusEpoch>();
            KnowledgeArticles = new List<CivilKnowledgeArticle>();
        }
    }

    internal sealed class CivilChronicleRecord
    {
        public int Schema { get; set; }
        public long Sequence { get; set; }
        public string Utc { get; set; }
        public string Event { get; set; }
        public string Subject { get; set; }
        public string Detail { get; set; }
        public string CorrelationId { get; set; }
        public string EvidenceHash { get; set; }
        public string PrevHash { get; set; }
        public string EntryHash { get; set; }
        public override string ToString() { return Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(6) + "   " + (Utc ?? "") + "   " + (Event ?? "") + "   ·   " + (Subject ?? ""); }
    }

    internal static class CivilizationLayer
    {
        private static readonly Brush Bg = B("#080B10");
        private static readonly Brush Surface = B("#101720");
        private static readonly Brush Raised = B("#172331");
        private static readonly Brush Border = B("#34465B");
        private static readonly Brush Text = B("#F3F7FB");
        private static readonly Brush Muted = B("#9AACBE");
        private static readonly Brush Accent = B("#72F0C1");
        private static readonly Brush Blue = B("#73BEFF");
        private static readonly Brush Amber = B("#FFD477");
        private static readonly Brush Violet = B("#C5A8FF");
        private static readonly Brush Danger = B("#FF8190");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();
        private static CivilizationStore Store;
        private static string LoadedRoot;
        private const int MaxInstitutions = 64;
        private const int MaxExpeditions = 96;
        private const int MaxSentinels = 96;
        private const int MaxAlerts = 512;
        private const int MaxResolutions = 256;
        private const int MaxCompacts = 128;
        private const int MaxCensusEpochs = 64;
        private const int MaxAmendmentsPerInstitution = 96;
        private const int MaxKnowledgeArticles = 512;
        private const int MaxKnowledgeRevisionsPerArticle = 32;
        private const int MaxShortText = 2048;
        private const int MaxLongText = 32768;

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            Load(ctx);
            WorldIndex index = WorldKernel.ExternalIndex(ctx, false);
            OpenHub(owner, ctx, index);
        }

        public static void OpenFromWorldNode(Window owner, DeepSystemsContext ctx, string nodeId)
        {
            if (ctx == null || String.IsNullOrWhiteSpace(nodeId)) return;
            Load(ctx);
            WorldIndex index = WorldKernel.ExternalIndex(ctx, false);
            WorldNode node;
            if (!index.Nodes.TryGetValue(nodeId, out node)) { Open(owner, ctx); return; }
            OpenWorldCivicGateway(owner, ctx, index, node);
        }

        private static void OpenWorldCivicGateway(Window owner, DeepSystemsContext ctx, WorldIndex index, WorldNode node)
        {
            Window w = W(owner, "YOMI · CIVIC GATEWAY · " + (node.Label ?? node.Kind), 960, 650); DockPanel root = Shell("WORLD PASSPORT → CIVILIZATION", (node.Kind ?? "OBJECT") + " · " + (node.Label ?? node.Id) + "\r\n" + node.Id); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 8, 0, 10) }; tools.Children.Add(Btn("FOUND INSTITUTION", delegate { string token = FirstIdentityValue(node); string query = String.IsNullOrWhiteSpace(token) ? "FIND " + (node.Label ?? node.Kind ?? "object") : "IDENTITY " + token; CreateInstitution(w, ctx, index, "Institute of " + Trunc(node.Label ?? node.Kind ?? "Object", 42), query, "Founded from World Passport " + node.Id, null); })); tools.Children.Add(Btn("AUTHOR COMMONS ARTICLE", delegate { CreateKnowledgePrompt(w, ctx, index, null, node); })); tools.Children.Add(Btn("LAUNCH EXPEDITION", delegate { string name = Prompt(w, "LAUNCH EXPEDITION", "Expedition name", "Expedition · " + Trunc(node.Label ?? node.Kind, 48)); if (!String.IsNullOrWhiteSpace(name)) LaunchExpedition(w, ctx, index, null, name.Trim(), node.Id); })); tools.Children.Add(Btn("OPEN PASSPORT", delegate { WorldKernel.ExternalOpenDossier(w, ctx, index, node.Id); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); List<CivilInstitution> jurisdictions = ActiveInstitutions().Where(i => WorldKernel.ExternalQuery(index, i.JurisdictionQuery).Nodes.Take(4096).Any(n => String.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase))).ToList(); ListBox list = List(); list.ItemsSource = jurisdictions; list.MouseDoubleClick += delegate { CivilInstitution i = list.SelectedItem as CivilInstitution; if (i != null) OpenInstitution(w, ctx, index, i); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenHub(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx);
            Window w = W(owner, "YOMI · CIVILIZATION LAYER", 1220, 820);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("CIVILIZATION LAYER / WORLD INSTITUTIONS", 28, Text, FontWeights.Bold));
            head.Children.Add(T("Persistent institutions, jurisdictions, expeditions, sentinels, councils and treaties built over the Universal World Kernel. Organization may observe and remember the world; it does not become engine authority.", 13, Muted, FontWeights.Normal));
            head.Children.Add(T(CensusLine(index), 11.5, Blue, FontWeights.SemiBold));
            WrapPanel top = new WrapPanel { Margin = new Thickness(0, 10, 0, 10) };
            top.Children.Add(Btn("FOUND INSTITUTION", delegate { CreateInstitutionPrompt(w, ctx, index, null); }));
            top.Children.Add(Btn("REBUILD WORLD", delegate { index = WorldKernel.ExternalIndex(ctx, true); MessageBox.Show(w, CensusLine(index), "YOMI · Civilization Census"); }));
            top.Children.Add(Btn("WORLD KERNEL", delegate { WorldKernel.Open(w, ctx); }));
            top.Children.Add(Btn("VERIFICATION REACTOR", delegate { AxiomaticVerificationReactor.Open(w, ctx); }));
            top.Children.Add(Btn("STRATEGY SUPERSTRUCTURE", delegate { CounterfactualStrategySuperstructure.Open(w, ctx); }));
            top.Children.Add(Btn("DISCOVERY HYPERSTRUCTURE", delegate { ScientificDiscoveryHyperstructure.Open(w, ctx); }));
            top.Children.Add(Btn("DYSON ENGINEERING", delegate { DysonSystemsEngineeringMegastructure.Open(w, ctx); }));
            top.Children.Add(Btn("COMPLEX SYSTEMS", delegate { ComplexSystemsLaboratory.Open(w, ctx); }));
            top.Children.Add(Btn("OPERATIONS RESEARCH", delegate { OperationsResearchEmpire.Open(w, ctx); }));
            top.Children.Add(Btn("ECONOMIC CIVILIZATION", delegate { EconomicCivilization.Open(w, ctx); }));
            top.Children.Add(Btn("SPATIAL CARTOGRAPHY", delegate { SpatialWorldCartography.Open(w, ctx); }));
            top.Children.Add(Btn("DISTRIBUTED PLANETARIUM", delegate { DistributedSystemsPlanetarium.Open(w, ctx); }));
            top.Children.Add(Btn("SYSTEM ATLAS", delegate { DeepSystems.OpenAtlasFromContext(w); }));
            head.Children.Add(top); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel cards = new WrapPanel();
            Portal(cards, "INST", "INSTITUTION REGISTRY", "Jurisdictions are live YQL lenses with charters, child institutions, baselines and graph territory.", delegate { OpenInstitutions(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "FRONTIER", "UNCLAIMED FRONTIER", "High-information graph objects not currently covered by any institution. Found new institutions directly from unexplored territory.", delegate { OpenFrontier(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "EXP", "EXPEDITION COMMAND", "Persistent graph traversals with visited territory, ranked frontier, findings and resumable route state.", delegate { OpenExpeditions(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "SENT", "SENTINEL NETWORK", "Manual, demand-driven jurisdiction scans that generate evidence-backed added/removed/changed alerts.", delegate { OpenSentinels(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "COUNCIL", "COUNCIL CHAMBER", "One review surface for open sentinel alerts, institutional drift and unfinished expeditions.", delegate { OpenCouncil(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "LAW", "RESOLUTION ASSEMBLY", "Draft evidence-anchored questions, hypotheses and organizational decisions; deliberate, adopt and retire them without promoting them into truth.", delegate { OpenResolutions(w, ctx, WorldKernel.ExternalIndex(ctx, false), null); });
            Portal(cards, "COMMONS", "KNOWLEDGE COMMONS", "Versioned evidence-addressed articles retain epistemic class, exact World Passport anchors, revisions and institutional scope.", delegate { OpenKnowledgeCommons(w, ctx, WorldKernel.ExternalIndex(ctx, false), null); });
            Portal(cards, "PLURAL", "PLURALITY FORUM", "Surface multiple institutional interpretations of the same universal object without collapsing difference into contradiction or forced consensus.", delegate { OpenPluralityForum(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "TREATY", "TREATY / BRIDGE ATLAS", "Compute actual cross-jurisdiction edges between institutions and inspect the evidence creating those bridges.", delegate { OpenTreaties(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "BORDER", "JURISDICTION BORDER ATLAS", "Inspect overlap, containment and contested graph territory between live institutional jurisdictions.", delegate { OpenBorders(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "CONST", "TREATY CONSTELLATIONS", "Discover connected institutional civilizations, isolated jurisdictions and bridge-central hubs from the current evidence topology.", delegate { OpenConstellations(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "ROUTE", "EVIDENCE RELAY ROUTES", "Traverse bounded shortest World Kernel paths between institutional territories while preserving every object passport along the route.", delegate { OpenRelayRoutes(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "PACT", "CIVIL COMPACTS", "Record explicit human organizational cooperation separately from computed graph treaties and reevaluate its supporting evidence on demand.", delegate { OpenCompacts(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "CENSUS", "CIVILIZATION CENSUS", "Coverage, institutions, active expeditions, sentinels, alerts and graph territory statistics.", delegate { OpenCensus(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "EPOCH", "CENSUS EPOCH LEDGER", "Capture bounded civilization-wide demographic epochs and compare coverage, treaty topology and jurisdiction fingerprints over time.", delegate { OpenCensusEpochs(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "LINEAGE", "INSTITUTION GENEALOGY", "Explore parent-child institution ancestry, descendants, archival state and explicit charter amendment lineages.", delegate { OpenGenealogy(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "AUDIT", "CONSTITUTIONAL AUDIT CHAMBER", "Read-only structural audit of civil identities, lineages, references, hashes, bounds, evidence drift and Chronicle continuity.", delegate { OpenConstitutionalAudit(w, ctx, WorldKernel.ExternalIndex(ctx, false)); });
            Portal(cards, "CHRON", "CIVILIZATION CHRONICLE", "Append-only SHA-256 chained institutional history: founding, baselines, scans, alerts, expeditions, reviews and treaties.", delegate { OpenChronicle(w, ctx); });
            Portal(cards, "⊨CTL", "AXIOMATIC VERIFICATION REACTOR", "Project organizational resolutions into explicit lifecycle automata and verify safety or liveness without confusing adoption with truth.", delegate { AxiomaticVerificationReactor.Open(w, ctx); });
            Portal(cards, "do(X)", "COUNTERFACTUAL STRATEGY SUPERSTRUCTURE", "Project resolutions into explicit intervention portfolios, causal hypotheses, uncertainty manifolds and model-bound decision dossiers without confusing adoption with truth or authority.", delegate { CounterfactualStrategySuperstructure.Open(w, ctx); });
            Portal(cards, "I(H;Y)", "SCIENTIFIC DISCOVERY HYPERSTRUCTURE", "Project resolutions into preregistered policy pilots with competing hypotheses, explicit outcomes, stopping rules, power plans and immutable design certificates without confusing adoption with efficacy.", delegate { ScientificDiscoveryHyperstructure.Open(w, ctx); });
            Portal(cards, "ΣSYS", "DYSON SYSTEMS ENGINEERING MEGASTRUCTURE", "Allocate adopted intent into traceable requirements, architecture boundaries, interface contracts, resource reserves, work packages, hazards, safety claims and controlled baselines.", delegate { DysonSystemsEngineeringMegastructure.Open(w, ctx); });
            Portal(cards, "ΣPOP", "COMPLEX SYSTEMS LABORATORY", "Model institutional populations, adoption networks, cascades, polarization, resilience and unintended collective behavior while keeping resolutions distinct from behavioral laws.", delegate { ComplexSystemsLaboratory.Open(w, ctx); });
            Portal(cards, "ORΩ", "OPERATIONS RESEARCH EMPIRE", "Translate civil implementation questions into explicit resource, scheduling, staffing, routing and portfolio models while preserving deliberation and human authority.", delegate { OperationsResearchEmpire.Open(w, ctx); });
            Portal(cards, "ECONΩ", "ECONOMIC CIVILIZATION", "Translate civil resolutions into explicit fiscal, labor, production, credit, trade and distribution worlds while preserving deliberation, dissent and human authority.", delegate { EconomicCivilization.Open(w, ctx); });
            Portal(cards, "GEOΩ", "SPATIAL WORLD CARTOGRAPHY", "Translate civil resolutions into explicit territories, jurisdictions, places, accessibility and infrastructure worlds while preserving deliberation, dissent and geographic authority boundaries.", delegate { SpatialWorldCartography.Open(w, ctx); });
            Portal(cards, "DISTΩ", "DISTRIBUTED SYSTEMS PLANETARIUM", "Translate institutional replication, voting, communication and continuity questions into synthetic consensus and failure worlds while preserving civil authority and dissent.", delegate { DistributedSystemsPlanetarium.Open(w, ctx); });
            Portal(cards, "WORLD", "CONVERGENCE CORE", "Drop back into the universal object graph, YQL, identity nexus, schema cartography and world epochs.", delegate { WorldKernel.Open(w, ctx); });
            Portal(cards, "HADAL", "HADAL ACCESS SHAFT", "Descend beneath an institution or graph object into persistent ancestry, null space, divergence and consequence horizons.", delegate { AbyssalKnowledgeEngine.Open(w, ctx); });
            scroll.Content = cards; root.Children.Add(scroll); w.Content = root; w.Show();
        }

        private static void OpenInstitutions(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · INSTITUTION REGISTRY", 1100, 740); DockPanel root = Shell("INSTITUTION REGISTRY", "Durable jurisdictions over the World Kernel. Double-click an institution to enter its internal world.");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 10) }; tools.Children.Add(Btn("FOUND INSTITUTION", delegate { CreateInstitutionPrompt(w, ctx, index, null); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            ListBox list = List(); Action refresh = delegate { list.ItemsSource = Store.Institutions.OrderBy(i => i.ParentInstitutionId ?? "").ThenBy(i => i.Name ?? "").ToList(); }; refresh();
            list.MouseDoubleClick += delegate { CivilInstitution i = list.SelectedItem as CivilInstitution; if (i != null) { i.LastOpenedUtc = Now(); Save(ctx); OpenInstitution(w, ctx, index, i); } }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenInstitution(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            WorldQueryResult result = WorldKernel.ExternalQuery(index, inst.JurisdictionQuery);
            Dictionary<string, string> now = Fingerprints(result.Nodes);
            Drift drift = Compare(inst.BaselineNodes, now);
            Window w = W(owner, "YOMI · INSTITUTION · " + (inst.Name ?? ""), 1160, 800);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel(); head.Children.Add(T("INSTITUTION · " + (inst.ParentInstitutionId == null ? "SOVEREIGN" : "CHILD") + " · " + (inst.Status ?? "ACTIVE") + " · CHARTER r" + Math.Max(1, inst.CharterRevision).ToString(CultureInfo.InvariantCulture), 12, Accent, FontWeights.Bold)); head.Children.Add(T(inst.Name ?? "INSTITUTION", 27, Text, FontWeights.Bold));
            head.Children.Add(T(inst.Charter ?? "No charter recorded.", 13, Muted, FontWeights.Normal)); head.Children.Add(T("JURISDICTION   " + (inst.JurisdictionQuery ?? "STATS"), 11.5, Blue, FontWeights.SemiBold));
            head.Children.Add(T("CURRENT " + result.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes   ·   Δ +" + drift.Added.Count.ToString(CultureInfo.InvariantCulture) + " / -" + drift.Removed.Count.ToString(CultureInfo.InvariantCulture) + " / ~" + drift.Changed.Count.ToString(CultureInfo.InvariantCulture), 11.5, drift.Total == 0 ? Accent : Amber, FontWeights.SemiBold));
            WrapPanel actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 8) };
            actions.Children.Add(Btn("ENTER JURISDICTION", delegate { OpenJurisdiction(w, ctx, index, inst); }));
            actions.Children.Add(Btn("REBASELINE", delegate { inst.BaselineNodes = Fingerprints(WorldKernel.ExternalQuery(WorldKernel.ExternalIndex(ctx, true), inst.JurisdictionQuery).Nodes); inst.BaselineFingerprint = HashMap(inst.BaselineNodes); inst.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "BASELINE", inst.Id, inst.Name + " · " + inst.BaselineNodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes"); MessageBox.Show(w, "Institution baseline updated.", "YOMI · Civilization"); }));
            actions.Children.Add(Btn("LAUNCH EXPEDITION", delegate { LaunchExpeditionPrompt(w, ctx, index, inst); }));
            actions.Children.Add(Btn("ADD SENTINEL", delegate { CreateSentinelPrompt(w, ctx, index, inst); }));
            actions.Children.Add(Btn("FOUND CHILD", delegate { CreateInstitutionPrompt(w, ctx, index, inst.Id); }));
            actions.Children.Add(Btn("COUNCIL", delegate { OpenInstitutionCouncil(w, ctx, index, inst); }));
            actions.Children.Add(Btn("RESOLUTIONS", delegate { OpenResolutions(w, ctx, index, inst); }));
            actions.Children.Add(Btn("KNOWLEDGE COMMONS", delegate { OpenKnowledgeCommons(w, ctx, index, inst); }));
            actions.Children.Add(Btn("AMEND CHARTER", delegate { AmendInstitutionPrompt(w, ctx, index, inst); }));
            actions.Children.Add(Btn("AMENDMENT LEDGER", delegate { OpenAmendments(w, ctx, inst); }));
            actions.Children.Add(Btn(String.Equals(inst.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "RESTORE" : "ARCHIVE", delegate { ToggleInstitutionLifecycle(w, ctx, inst); }));
            actions.Children.Add(Btn("HADAL DESCENT", delegate { AbyssalKnowledgeEngine.DescendFromExternal(w, ctx, inst.Name, inst, StorePath(ctx), "$institution:" + inst.Id, "CIVILIZATION › " + inst.Name); }));
            head.Children.Add(actions); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            Grid body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition());
            TextBox info = ReadOnly(DescribeInstitution(ctx, index, inst, result, drift)); Grid.SetColumn(info, 0); body.Children.Add(info);
            ListBox sample = List(); sample.ItemsSource = result.Nodes.Take(300).ToList(); sample.MouseDoubleClick += delegate { WorldNode n = sample.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; Grid.SetColumn(sample, 1); body.Children.Add(sample);
            root.Children.Add(body); w.Content = root; w.Show();
        }

        private static void OpenJurisdiction(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            WorldQueryResult r = WorldKernel.ExternalQuery(index, inst.JurisdictionQuery); Window w = W(owner, "YOMI · JURISDICTION · " + inst.Name, 1100, 740); DockPanel root = Shell(inst.Name + " · JURISDICTION", r.Explanation + " · " + r.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes");
            ListBox list = List(); list.ItemsSource = r.Nodes.OrderByDescending(n => WorldKernel.ExternalDegree(index, n.Id)).ToList(); list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenFrontier(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CivilInstitution i in ActiveInstitutions()) foreach (WorldNode n in WorldKernel.ExternalQuery(index, i.JurisdictionQuery).Nodes.Take(1200)) claimed.Add(n.Id);
            List<WorldNode> frontier = index.Nodes.Values.Where(n => n.Kind != "IDENTITY" && !claimed.Contains(n.Id)).OrderByDescending(n => FrontierScore(index, n)).Take(600).ToList();
            Window w = W(owner, "YOMI · UNCLAIMED FRONTIER", 1120, 760); DockPanel root = Shell("UNCLAIMED FRONTIER", frontier.Count.ToString(CultureInfo.InvariantCulture) + " high-information objects outside current institutional jurisdiction. Double-click for World Passport.");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; Button found = Btn("FOUND INSTITUTION FROM SELECTED", null); tools.Children.Add(found); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            ListBox list = List(); list.ItemsSource = frontier; found.Click += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n == null) return; string token = FirstIdentityValue(n); string query = String.IsNullOrWhiteSpace(token) ? "FIND " + (n.Label ?? n.Kind ?? "object") : "IDENTITY " + token; CreateInstitution(w, ctx, index, "Institute of " + Trunc(n.Label ?? n.Kind, 42), query, "Founded from unclaimed World Kernel frontier " + n.Id, null); };
            list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenExpeditions(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · EXPEDITION COMMAND", 1100, 740); DockPanel root = Shell("EXPEDITION COMMAND", "Persistent graph traversals. Every advance is recorded; frontier ranking favors connected novelty rather than blind depth."); ListBox list = List(); Action refresh = delegate { list.ItemsSource = Store.Expeditions.OrderByDescending(e => e.UpdatedUtc ?? e.CreatedUtc).ToList(); }; refresh(); list.MouseDoubleClick += delegate { CivilExpedition e = list.SelectedItem as CivilExpedition; if (e != null) OpenExpedition(w, ctx, index, e); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenExpedition(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilExpedition exp)
        {
            SurveyFrontier(index, exp); Save(ctx);
            Window w = W(owner, "YOMI · EXPEDITION · " + exp.Name, 1140, 800); DockPanel root = new DockPanel { Margin = new Thickness(18) };
            WorldNode current = index.Nodes.ContainsKey(exp.CurrentNodeId ?? "") ? index.Nodes[exp.CurrentNodeId] : null;
            StackPanel head = new StackPanel(); head.Children.Add(T("EXPEDITION · " + (exp.Status ?? "ACTIVE"), 12, Accent, FontWeights.Bold)); head.Children.Add(T(exp.Name ?? "EXPEDITION", 26, Text, FontWeights.Bold)); head.Children.Add(T("Visited " + exp.VisitedNodeIds.Count.ToString(CultureInfo.InvariantCulture) + " · frontier " + exp.FrontierNodeIds.Count.ToString(CultureInfo.InvariantCulture) + " · current " + (current == null ? "∅" : current.Label), 12, Muted, FontWeights.Normal));
            WrapPanel actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 8) };
            actions.Children.Add(Btn("ADVANCE BEST FRONTIER", delegate { AdvanceExpedition(w, ctx, index, exp); }));
            actions.Children.Add(Btn("SURVEY", delegate { SurveyFrontier(index, exp); exp.UpdatedUtc = Now(); Save(ctx); MessageBox.Show(w, "Frontier rebuilt: " + exp.FrontierNodeIds.Count.ToString(CultureInfo.InvariantCulture), "YOMI · Expedition"); }));
            actions.Children.Add(Btn("RECORD FINDING", delegate { string note = PromptLarge(w, "EXPEDITION FINDING", "Observation / question / finding", ""); if (!String.IsNullOrWhiteSpace(note)) { exp.Findings.Insert(0, Now() + " · " + note.Trim()); exp.Findings = exp.Findings.Take(80).ToList(); exp.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "FINDING", exp.Id, note.Trim()); } }));
            actions.Children.Add(Btn("DRAFT RESOLUTION", delegate { DraftResolutionFromExpedition(w, ctx, index, exp); }));
            actions.Children.Add(Btn("CURRENT PASSPORT", delegate { if (current != null) WorldKernel.ExternalOpenDossier(w, ctx, index, current.Id); }));
            actions.Children.Add(Btn("SPAWN SUB-EXPEDITION", delegate { if (current != null) LaunchExpedition(w, ctx, index, Institution(exp.InstitutionId), "Sub-expedition · " + Trunc(current.Label, 36), current.Id); }));
            actions.Children.Add(Btn("COMPLETE", delegate { exp.Status = "COMPLETE"; exp.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "EXPEDITION_COMPLETE", exp.Id, exp.Name); w.Close(); }));
            head.Children.Add(actions); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            Grid g = new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition());
            TextBox route = ReadOnly("ROUTE\r\n" + String.Join("\r\n", exp.VisitedNodeIds.Select((id, p) => (p + 1).ToString(CultureInfo.InvariantCulture).PadLeft(3) + "  " + (index.Nodes.ContainsKey(id) ? index.Nodes[id].ToString() : id)).ToArray()) + "\r\n\r\nFINDINGS\r\n" + String.Join("\r\n", exp.Findings.ToArray())); Grid.SetColumn(route, 0); g.Children.Add(route);
            ListBox front = List(); front.ItemsSource = exp.FrontierNodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); front.MouseDoubleClick += delegate { WorldNode n = front.SelectedItem as WorldNode; if (n != null) { exp.CurrentNodeId = n.Id; if (!exp.VisitedNodeIds.Contains(n.Id, StringComparer.OrdinalIgnoreCase)) exp.VisitedNodeIds.Add(n.Id); exp.FrontierNodeIds.RemoveAll(id => String.Equals(id, n.Id, StringComparison.OrdinalIgnoreCase)); SurveyFrontier(index, exp); exp.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "EXPEDITION_ADVANCE", exp.Id, n.Id); OpenExpedition(w, ctx, index, exp); } }; Grid.SetColumn(front, 1); g.Children.Add(front); root.Children.Add(g); w.Content = root; w.Show();
        }

        private static void OpenSentinels(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · SENTINEL NETWORK", 1120, 760); DockPanel root = Shell("SENTINEL NETWORK", "Demand-driven jurisdiction watchers. No background crawler: scans happen only when requested.");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 10) }; tools.Children.Add(Btn("SCAN ALL", delegate { index = WorldKernel.ExternalIndex(ctx, true); int created = 0; foreach (CivilSentinel s in Store.Sentinels) created += ScanSentinel(ctx, index, s); Save(ctx); MessageBox.Show(w, "Scan complete · " + created.ToString(CultureInfo.InvariantCulture) + " new alerts", "YOMI · Sentinel Network"); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            ListBox list = List(); list.ItemsSource = Store.Sentinels.OrderBy(s => s.Name).ToList(); list.MouseDoubleClick += delegate { CivilSentinel s = list.SelectedItem as CivilSentinel; if (s != null) OpenSentinel(w, ctx, WorldKernel.ExternalIndex(ctx, false), s); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenSentinel(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilSentinel s)
        {
            Window w = W(owner, "YOMI · SENTINEL · " + s.Name, 1080, 720); DockPanel root = Shell(s.Name ?? "SENTINEL", "QUERY   " + s.Query + "\r\nLast scan   " + (s.LastScanUtc ?? "never") + "   · baseline " + s.LastNodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 10) }; tools.Children.Add(Btn("SCAN NOW", delegate { index = WorldKernel.ExternalIndex(ctx, true); int n = ScanSentinel(ctx, index, s); Save(ctx); MessageBox.Show(w, n.ToString(CultureInfo.InvariantCulture) + " alert(s) created.", "YOMI · Sentinel"); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            ListBox alerts = List(); alerts.ItemsSource = Store.Alerts.Where(a => String.Equals(a.SentinelId, s.Id, StringComparison.OrdinalIgnoreCase)).OrderByDescending(a => a.CreatedUtc).ToList(); alerts.MouseDoubleClick += delegate { CivilAlert a = alerts.SelectedItem as CivilAlert; if (a != null) OpenAlert(w, ctx, index, a); }; root.Children.Add(alerts); w.Content = root; w.Show();
        }

        private static void OpenCouncil(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · COUNCIL CHAMBER", 1160, 780); DockPanel root = Shell("COUNCIL CHAMBER", "Open alerts plus institutional drift and unfinished expeditions. A review surface, not a truth engine.");
            List<object> rows = new List<object>(); rows.AddRange(Store.Alerts.Where(a => !String.Equals(a.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase)).Cast<object>());
            foreach (CivilExpedition e in Store.Expeditions.Where(e => String.Equals(e.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))) rows.Add(e);
            rows.AddRange(Store.Resolutions.Where(r => String.Equals(r.Status, "DRAFT", StringComparison.OrdinalIgnoreCase) || String.Equals(r.Status, "DELIBERATING", StringComparison.OrdinalIgnoreCase)).Cast<object>());
            ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { CivilAlert a = list.SelectedItem as CivilAlert; if (a != null) { OpenAlert(w, ctx, index, a); return; } CivilExpedition e = list.SelectedItem as CivilExpedition; if (e != null) { OpenExpedition(w, ctx, index, e); return; } CivilResolution r = list.SelectedItem as CivilResolution; if (r != null) OpenResolution(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenInstitutionCouncil(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            Window w = W(owner, "YOMI · COUNCIL · " + inst.Name, 1080, 720); DockPanel root = Shell(inst.Name + " · COUNCIL", "Alerts, active expeditions and unresolved resolutions scoped to this institution."); List<object> rows = new List<object>(); rows.AddRange(Store.Alerts.Where(a => a.InstitutionId == inst.Id && a.Status != "REVIEWED").Cast<object>()); rows.AddRange(Store.Expeditions.Where(e => e.InstitutionId == inst.Id && e.Status == "ACTIVE").Cast<object>()); rows.AddRange(Store.Resolutions.Where(r => r.InstitutionId == inst.Id && r.Status != "RETIRED" && r.Status != "ADOPTED").Cast<object>()); ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { CivilAlert a = list.SelectedItem as CivilAlert; if (a != null) { OpenAlert(w, ctx, index, a); return; } CivilExpedition e = list.SelectedItem as CivilExpedition; if (e != null) { OpenExpedition(w, ctx, index, e); return; } CivilResolution r = list.SelectedItem as CivilResolution; if (r != null) OpenResolution(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenAlert(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilAlert a)
        {
            Window w = W(owner, "YOMI · COUNCIL ALERT", 1050, 720); DockPanel root = Shell((a.Severity ?? "INFO") + " · " + (a.Summary ?? "ALERT"), a.Detail ?? "");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 10) }; tools.Children.Add(Btn("MARK REVIEWED", delegate { a.Status = "REVIEWED"; Save(ctx); AppendChronicle(ctx, "ALERT_REVIEWED", a.Id, a.Summary); w.Close(); })); tools.Children.Add(Btn("DRAFT RESOLUTION", delegate { DraftResolutionFromAlert(w, ctx, index, a); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            ListBox list = List(); list.ItemsSource = a.NodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenTreaties(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx);
            List<TreatyRow> rows = BuildTreaties(index);
            Window w = W(owner, "YOMI · TREATY / BRIDGE ATLAS", 1140, 760); DockPanel root = Shell("TREATY / BRIDGE ATLAS", "Cross-jurisdiction evidence edges computed from one reverse-membership index and one graph pass. Strong bridges show convergence; they do not imply causality or organizational dependency."); ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { TreatyRow r = list.SelectedItem as TreatyRow; if (r != null) OpenTreaty(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static Dictionary<string, List<CivilInstitution>> BuildMembership(WorldIndex index)
        {
            Dictionary<string, List<CivilInstitution>> membership = new Dictionary<string, List<CivilInstitution>>(StringComparer.OrdinalIgnoreCase);
            foreach (CivilInstitution inst in ActiveInstitutions())
            {
                foreach (WorldNode n in WorldKernel.ExternalQuery(index, inst.JurisdictionQuery).Nodes.Take(4096))
                {
                    List<CivilInstitution> owners; if (!membership.TryGetValue(n.Id, out owners)) { owners = new List<CivilInstitution>(); membership[n.Id] = owners; }
                    if (!owners.Any(x => String.Equals(x.Id, inst.Id, StringComparison.OrdinalIgnoreCase))) owners.Add(inst);
                }
            }
            return membership;
        }

        private static List<TreatyRow> BuildTreaties(WorldIndex index)
        {
            // One reverse jurisdiction index + one world-edge pass. Avoid O(institution² × edges).
            Dictionary<string, List<CivilInstitution>> membership = BuildMembership(index);
            Dictionary<string, TreatyRow> byPair = new Dictionary<string, TreatyRow>(StringComparer.OrdinalIgnoreCase);
            foreach (WorldEdge edge in index.Edges)
            {
                List<CivilInstitution> left, right; if (!membership.TryGetValue(edge.FromId, out left) || !membership.TryGetValue(edge.ToId, out right)) continue;
                HashSet<string> countedPairsForEdge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (CivilInstitution a0 in left) foreach (CivilInstitution b0 in right)
                {
                    if (String.Equals(a0.Id, b0.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    CivilInstitution a = String.Compare(a0.Id, b0.Id, StringComparison.OrdinalIgnoreCase) <= 0 ? a0 : b0;
                    CivilInstitution b = Object.ReferenceEquals(a, a0) ? b0 : a0;
                    string key = a.Id + "|" + b.Id; if (!countedPairsForEdge.Add(key)) continue;
                    TreatyRow row; if (!byPair.TryGetValue(key, out row)) { row = new TreatyRow { A = a, B = b, Count = 0, Edges = new List<WorldEdge>() }; byPair[key] = row; }
                    row.Count++; if (row.Edges.Count < 1000) row.Edges.Add(edge);
                }
            }
            return byPair.Values.Where(r => r.Count > 0).OrderByDescending(r => r.Count).ToList();
        }

        private static void OpenTreaty(Window owner, DeepSystemsContext ctx, WorldIndex index, TreatyRow r)
        {
            Window w = W(owner, "YOMI · BRIDGE · " + r.A.Name + " ↔ " + r.B.Name, 1080, 720); DockPanel root = Shell(r.A.Name + " ↔ " + r.B.Name, r.Count.ToString(CultureInfo.InvariantCulture) + " cross-jurisdiction graph edges"); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("OPEN RELAY ROUTE", delegate { OpenRelayRoute(w, ctx, index, r); })); tools.Children.Add(Btn("PROPOSE COMPACT", delegate { CreateCompactFromTreaty(w, ctx, index, r); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); ListBox list = List(); list.ItemsSource = r.Edges; list.MouseDoubleClick += delegate { WorldEdge e = list.SelectedItem as WorldEdge; if (e != null) { string id = index.Nodes.ContainsKey(e.FromId) ? e.FromId : e.ToId; WorldKernel.ExternalOpenDossier(w, ctx, index, id); } }; root.Children.Add(list); w.Content = root; w.Show(); AppendChronicle(ctx, "TREATY_INSPECT", r.A.Id + ":" + r.B.Id, r.Count.ToString(CultureInfo.InvariantCulture) + " bridges");
        }

        private static void OpenResolutions(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution scope)
        {
            Load(ctx); string scopeName = scope == null ? "CIVILIZATION" : scope.Name; Window w = W(owner, "YOMI · RESOLUTION ASSEMBLY", 1140, 760); DockPanel root = Shell(scopeName + " · RESOLUTION ASSEMBLY", "Questions, hypotheses and organizational decisions retain their epistemic class and evidence anchors. Adoption records a decision; it does not manufacture truth.");
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("DRAFT RESOLUTION", delegate { CreateResolutionPrompt(w, ctx, index, scope); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            IEnumerable<CivilResolution> source = Store.Resolutions; if (scope != null) source = source.Where(r => String.Equals(r.InstitutionId, scope.Id, StringComparison.OrdinalIgnoreCase)); ListBox list = List(); list.ItemsSource = source.OrderBy(r => ResolutionStatusRank(r.Status)).ThenByDescending(r => r.UpdatedUtc ?? r.CreatedUtc).ToList(); list.MouseDoubleClick += delegate { CivilResolution r = list.SelectedItem as CivilResolution; if (r != null) OpenResolution(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void CreateResolutionPrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution scope)
        {
            CivilInstitution inst = scope; if (inst == null && ActiveInstitutions().Count > 0) { string requested = Prompt(owner, "RESOLUTION SCOPE", "Institution name, or leave blank for civilization-wide", ActiveInstitutions()[0].Name); if (requested == null) return; if (!String.IsNullOrWhiteSpace(requested)) { inst = ActiveInstitutions().FirstOrDefault(i => String.Equals(i.Name, requested.Trim(), StringComparison.OrdinalIgnoreCase)); if (inst == null) { MessageBox.Show(owner, "No active institution has that exact name.", "YOMI · Resolution Assembly"); return; } } }
            string title = Prompt(owner, "DRAFT RESOLUTION", "Resolution title", "Investigate unresolved evidence"); if (String.IsNullOrWhiteSpace(title)) return; string epistemic = Choose(owner, "EPISTEMIC CLASS", "Classify what this resolution claims to be", new[] { "QUESTION", "HYPOTHESIS", "OBSERVATION NOTE", "ORGANIZATIONAL DECISION" }, "QUESTION"); if (String.IsNullOrWhiteSpace(epistemic)) return; string proposition = PromptLarge(owner, "PROPOSITION", "What should the council examine or decide?", "Review the cited evidence and record a bounded conclusion."); if (proposition == null) return; CreateResolution(owner, ctx, index, inst, title.Trim(), proposition.Trim(), epistemic, null, null, new List<string>());
        }

        private static void CreateResolution(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst, string title, string proposition, string epistemicClass, string alertId, string expeditionId, IEnumerable<string> evidence)
        {
            CivilResolution r = new CivilResolution { Id = Id("resolution"), InstitutionId = inst == null ? null : inst.Id, Title = title, Proposition = proposition, EpistemicClass = String.IsNullOrWhiteSpace(epistemicClass) ? "QUESTION" : epistemicClass, Status = "DRAFT", OriginAlertId = alertId, OriginExpeditionId = expeditionId, CreatedUtc = Now(), UpdatedUtc = Now() }; r.EvidenceNodeIds = (evidence ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(256).ToList(); Store.Resolutions.Insert(0, r); TrimStore(); Save(ctx); AppendChronicle(ctx, "RESOLUTION_DRAFTED", r.Id, r.Title + " · " + r.EpistemicClass, HashStrings(r.EvidenceNodeIds)); OpenResolution(owner, ctx, index, r);
        }

        private static void DraftResolutionFromAlert(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilAlert alert)
        {
            CivilInstitution inst = Institution(alert.InstitutionId); CreateResolution(owner, ctx, index, inst, "Resolve " + Trunc(alert.Summary, 72), "Review sentinel drift without assuming that change implies error or causality. " + (alert.Detail ?? ""), "QUESTION", alert.Id, null, alert.NodeIds);
        }

        private static void DraftResolutionFromExpedition(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilExpedition expedition)
        {
            string proposition = expedition.Findings.Count == 0 ? "Review the current expedition route and determine the next bounded investigation." : expedition.Findings[0]; CreateResolution(owner, ctx, index, Institution(expedition.InstitutionId), "Expedition council · " + Trunc(expedition.Name, 64), proposition, "HYPOTHESIS", null, expedition.Id, expedition.VisitedNodeIds.Concat(new[] { expedition.CurrentNodeId }));
        }

        private static void OpenResolution(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilResolution resolution)
        {
            CivilInstitution inst = Institution(resolution.InstitutionId); Window w = W(owner, "YOMI · RESOLUTION · " + resolution.Title, 1120, 780); DockPanel root = Shell((resolution.Status ?? "DRAFT") + " · " + (resolution.EpistemicClass ?? "QUESTION") + " · " + resolution.Title, "Scope " + (inst == null ? "CIVILIZATION" : inst.Name) + " · evidence anchors " + resolution.EvidenceNodeIds.Count.ToString(CultureInfo.InvariantCulture));
            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("BEGIN DELIBERATION", delegate { TransitionResolution(ctx, resolution, "DELIBERATING"); w.Close(); OpenResolution(owner, ctx, index, resolution); })); tools.Children.Add(Btn("ADOPT", delegate { TransitionResolution(ctx, resolution, "ADOPTED"); w.Close(); OpenResolution(owner, ctx, index, resolution); })); tools.Children.Add(Btn("RETIRE", delegate { TransitionResolution(ctx, resolution, "RETIRED"); w.Close(); })); tools.Children.Add(Btn("ADD DELIBERATION", delegate { string note = PromptLarge(w, "COUNCIL DELIBERATION", "Note, objection, question or bounded conclusion", ""); if (!String.IsNullOrWhiteSpace(note)) { resolution.Deliberation.Insert(0, Now() + " · " + note.Trim()); resolution.Deliberation = resolution.Deliberation.Take(128).ToList(); resolution.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "RESOLUTION_DELIBERATION", resolution.Id, note.Trim(), HashStrings(resolution.EvidenceNodeIds)); } })); tools.Children.Add(Btn("LOGIC FOUNDRY", delegate { EpistemicLogicFoundry.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("VERIFY LIFECYCLE", delegate { AxiomaticVerificationReactor.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("COUNTERFACTUAL STRATEGY", delegate { CounterfactualStrategySuperstructure.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("PREREGISTERED PILOT", delegate { ScientificDiscoveryHyperstructure.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("ENGINEER IMPLEMENTATION", delegate { DysonSystemsEngineeringMegastructure.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("CYBERNETIC TWIN", delegate { CyberneticDigitalTwinMetastructure.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("COMPLEX POPULATION", delegate { ComplexSystemsLaboratory.OpenFromCivilResolution(w, ctx, index, resolution); })); tools.Children.Add(Btn("OPTIMIZE IMPLEMENTATION", delegate { OperationsResearchEmpire.OpenFromCivilResolution(w, ctx, index, resolution); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools);
            tools.Children.Add(Btn("MODEL POLITICAL ECONOMY", delegate { EconomicCivilization.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("MAP CIVIC TERRITORY", delegate { SpatialWorldCartography.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("MODEL DISTRIBUTED INSTITUTION", delegate { DistributedSystemsPlanetarium.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("DEFEND INSTITUTION", delegate { AdversarialSecurityFortress.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("PROJECT TO DATA FOUNDRY", delegate { DataFoundry.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("SOFTWARE GENOME", delegate { SoftwareGenomeObservatory.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("LIVING DOCUMENT", delegate { LivingDocumentIntelligenceFactory.OpenFromCivilResolution(w, ctx, index, resolution); }));
            tools.Children.Add(Btn("EXPEDITION COMMAND", delegate { GrandUnifiedExpeditionCommand.OpenFromCivilResolution(w, ctx, index, resolution); }));
            Grid body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition()); StringBuilder detail = new StringBuilder(); detail.AppendLine("RESOLUTION ID   " + resolution.Id); detail.AppendLine("INSTITUTION     " + (inst == null ? "CIVILIZATION" : inst.Name)); detail.AppendLine("CREATED         " + resolution.CreatedUtc); detail.AppendLine("UPDATED         " + resolution.UpdatedUtc); detail.AppendLine("ORIGIN ALERT    " + (resolution.OriginAlertId ?? "∅")); detail.AppendLine("ORIGIN EXP      " + (resolution.OriginExpeditionId ?? "∅")); detail.AppendLine(); detail.AppendLine("PROPOSITION"); detail.AppendLine(resolution.Proposition ?? "∅"); detail.AppendLine(); detail.AppendLine("DELIBERATION"); detail.AppendLine(String.Join("\r\n", resolution.Deliberation.ToArray())); TextBox info = ReadOnly(detail.ToString()); Grid.SetColumn(info, 0); body.Children.Add(info); ListBox evidence = List(); evidence.ItemsSource = resolution.EvidenceNodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); evidence.MouseDoubleClick += delegate { WorldNode n = evidence.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; Grid.SetColumn(evidence, 1); body.Children.Add(evidence); root.Children.Add(body); w.Content = root; w.Show();
        }

        private static void TransitionResolution(DeepSystemsContext ctx, CivilResolution resolution, string status)
        {
            if (String.Equals(resolution.Status, status, StringComparison.OrdinalIgnoreCase)) return; string before = resolution.Status ?? "DRAFT"; resolution.Status = status; resolution.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "RESOLUTION_" + status, resolution.Id, before + " → " + status + " · " + resolution.Title, HashStrings(resolution.EvidenceNodeIds));
        }

        private static int ResolutionStatusRank(string status)
        {
            if (String.Equals(status, "DELIBERATING", StringComparison.OrdinalIgnoreCase)) return 0; if (String.Equals(status, "DRAFT", StringComparison.OrdinalIgnoreCase)) return 1; if (String.Equals(status, "ADOPTED", StringComparison.OrdinalIgnoreCase)) return 2; return 3;
        }

        private static void OpenKnowledgeCommons(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution scope)
        {
            Load(ctx); string scopeName = scope == null ? "CIVILIZATION" : scope.Name; Window w = W(owner, "YOMI · KNOWLEDGE COMMONS", 1140, 780); DockPanel root = Shell(scopeName + " · KNOWLEDGE COMMONS", "Versioned interpretations anchored to exact universal objects. Articles retain epistemic class and revisions; they remain downstream of evidence."); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("AUTHOR ARTICLE", delegate { CreateKnowledgePrompt(w, ctx, index, scope, null); })); tools.Children.Add(Btn("PLURALITY FORUM", delegate { OpenPluralityForum(w, ctx, index); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); IEnumerable<CivilKnowledgeArticle> source = Store.KnowledgeArticles; if (scope != null) source = source.Where(a => String.Equals(a.InstitutionId, scope.Id, StringComparison.OrdinalIgnoreCase)); ListBox list = List(); list.ItemsSource = source.OrderBy(a => String.Equals(a.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ThenByDescending(a => a.UpdatedUtc ?? a.CreatedUtc).ToList(); list.MouseDoubleClick += delegate { CivilKnowledgeArticle a = list.SelectedItem as CivilKnowledgeArticle; if (a != null) OpenKnowledgeArticle(w, ctx, index, a); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void CreateKnowledgePrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution scope, WorldNode anchoredNode)
        {
            CivilInstitution inst = scope; if (inst == null && ActiveInstitutions().Count > 0) { string requested = Prompt(owner, "ARTICLE SCOPE", "Institution name, or leave blank for civilization-wide", ActiveInstitutions()[0].Name); if (requested == null) return; if (!String.IsNullOrWhiteSpace(requested)) { inst = ActiveInstitutions().FirstOrDefault(i => String.Equals(i.Name, requested.Trim(), StringComparison.OrdinalIgnoreCase)); if (inst == null) { MessageBox.Show(owner, "No active institution has that exact name.", "YOMI · Knowledge Commons"); return; } } }
            WorldNode node = anchoredNode; if (node == null) { string query = Prompt(owner, "ARTICLE EVIDENCE", "Exact World URN, or YQL whose first high-degree result becomes the anchor", inst == null ? "STATS" : inst.JurisdictionQuery); if (String.IsNullOrWhiteSpace(query)) return; if (!index.Nodes.TryGetValue(query.Trim(), out node)) node = WorldKernel.ExternalQuery(index, query.Trim()).Nodes.OrderByDescending(n => WorldKernel.ExternalDegree(index, n.Id)).FirstOrDefault(); if (node == null) { MessageBox.Show(owner, "No World Kernel object resolved for that anchor.", "YOMI · Knowledge Commons"); return; } }
            string title = Prompt(owner, "AUTHOR ARTICLE", "Article title", Trunc(node.Label ?? node.Kind ?? "Universal Object", 72)); if (String.IsNullOrWhiteSpace(title)) return; string epistemic = Choose(owner, "EPISTEMIC CLASS", "Classify this article", new[] { "INTERPRETATION", "OBSERVATION NOTE", "HYPOTHESIS", "QUESTION", "DECISION RECORD" }, "INTERPRETATION"); if (String.IsNullOrWhiteSpace(epistemic)) return; string body = PromptLarge(owner, "ARTICLE BODY", "Bounded article text", "Describe what this object means to the institution and retain uncertainty explicitly."); if (body == null) return; string tags = Prompt(owner, "ARTICLE TAGS", "Comma-separated tags", node.Kind ?? "object"); if (tags == null) return; CivilKnowledgeArticle article = new CivilKnowledgeArticle { Id = Id("article"), InstitutionId = inst == null ? null : inst.Id, NodeId = node.Id, SourceNodeFingerprint = node.Fingerprint, Title = title.Trim(), Body = body.Trim(), EpistemicClass = epistemic, Status = "ACTIVE", CreatedUtc = Now(), UpdatedUtc = Now() }; article.Tags = tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(24).ToList(); article.BodyHash = KnowledgeHash(article.Title, article.Body, article.EpistemicClass); Store.KnowledgeArticles.Insert(0, article); TrimStore(); Save(ctx); AppendChronicle(ctx, "COMMONS_ARTICLE_CREATED", article.Id, article.Title + " · " + article.EpistemicClass + " · " + article.NodeId, article.BodyHash); OpenKnowledgeArticle(owner, ctx, index, article);
        }

        private static void OpenKnowledgeArticle(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilKnowledgeArticle article)
        {
            CivilInstitution inst = Institution(article.InstitutionId); WorldNode node; index.Nodes.TryGetValue(article.NodeId ?? "", out node); bool evidenceChanged = node != null && !String.Equals(node.Fingerprint ?? "", article.SourceNodeFingerprint ?? "", StringComparison.OrdinalIgnoreCase); Window w = W(owner, "YOMI · COMMONS · " + article.Title, 1120, 780); DockPanel root = Shell((article.Status ?? "ACTIVE") + " · " + (article.EpistemicClass ?? "INTERPRETATION") + " · " + article.Title, "Scope " + (inst == null ? "CIVILIZATION" : inst.Name) + " · source evidence " + (node == null ? "ABSENT" : evidenceChanged ? "CHANGED SINCE AUTHORING" : "UNCHANGED FINGERPRINT")); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("EDIT NEW REVISION", delegate { EditKnowledgePrompt(w, ctx, index, article); })); tools.Children.Add(Btn(String.Equals(article.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "RESTORE" : "ARCHIVE", delegate { article.Status = String.Equals(article.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "ARCHIVED"; article.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "COMMONS_ARTICLE_" + article.Status, article.Id, article.Title, article.BodyHash); w.Close(); })); tools.Children.Add(Btn("SOURCE PASSPORT", delegate { if (node != null) WorldKernel.ExternalOpenDossier(w, ctx, index, node.Id); })); tools.Children.Add(Btn("REVISION LEDGER", delegate { OpenKnowledgeRevisions(w, ctx, article); })); tools.Children.Add(Btn("LOGIC FOUNDRY", delegate { EpistemicLogicFoundry.OpenFromKnowledgeArticle(w, ctx, index, article); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); StringBuilder s = new StringBuilder(); s.AppendLine("ARTICLE ID       " + article.Id); s.AppendLine("INSTITUTION      " + (inst == null ? "CIVILIZATION" : inst.Name)); s.AppendLine("WORLD ADDRESS    " + article.NodeId); s.AppendLine("SOURCE HASH      " + article.SourceNodeFingerprint); s.AppendLine("ARTICLE HASH     " + article.BodyHash); s.AppendLine("CREATED          " + article.CreatedUtc); s.AppendLine("UPDATED          " + article.UpdatedUtc); s.AppendLine("REVISION         " + (article.Revisions.Count + 1).ToString(CultureInfo.InvariantCulture)); s.AppendLine("TAGS             " + String.Join(", ", article.Tags.ToArray())); s.AppendLine(); s.AppendLine(article.Body ?? ""); TextBox body = ReadOnly(s.ToString()); root.Children.Add(body); w.Content = root; w.Show();
        }

        private static void EditKnowledgePrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilKnowledgeArticle article)
        {
            string title = Prompt(owner, "REVISE ARTICLE", "Article title", article.Title); if (String.IsNullOrWhiteSpace(title)) return; string epistemic = Choose(owner, "REVISE EPISTEMIC CLASS", "Epistemic class", new[] { "INTERPRETATION", "OBSERVATION NOTE", "HYPOTHESIS", "QUESTION", "DECISION RECORD" }, article.EpistemicClass); if (String.IsNullOrWhiteSpace(epistemic)) return; string body = PromptLarge(owner, "REVISE ARTICLE BODY", "New bounded article text", article.Body); if (body == null) return; string after = KnowledgeHash(title.Trim(), body.Trim(), epistemic); if (String.Equals(after, article.BodyHash, StringComparison.OrdinalIgnoreCase)) return; CivilKnowledgeRevision revision = new CivilKnowledgeRevision { Id = Id("article-revision"), CreatedUtc = Now(), PreviousTitle = article.Title, PreviousBody = article.Body, PreviousEpistemicClass = article.EpistemicClass, BeforeHash = article.BodyHash, AfterHash = after }; article.Revisions.Insert(0, revision); article.Revisions = article.Revisions.Take(MaxKnowledgeRevisionsPerArticle).ToList(); article.Title = title.Trim(); article.Body = body.Trim(); article.EpistemicClass = epistemic; article.BodyHash = after; article.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "COMMONS_ARTICLE_REVISED", article.Id, article.Title + " · r" + (article.Revisions.Count + 1).ToString(CultureInfo.InvariantCulture), article.BodyHash); MessageBox.Show(owner, "New article revision recorded.", "YOMI · Knowledge Commons");
        }

        private static void OpenKnowledgeRevisions(Window owner, DeepSystemsContext ctx, CivilKnowledgeArticle article)
        {
            Window w = W(owner, "YOMI · COMMONS REVISIONS · " + article.Title, 1040, 700); DockPanel root = Shell(article.Title + " · REVISION LEDGER", "Prior article text and epistemic class remain inspectable after every revision."); ListBox list = List(); list.ItemsSource = article.Revisions.OrderByDescending(r => r.CreatedUtc).ToList(); list.MouseDoubleClick += delegate { CivilKnowledgeRevision r = list.SelectedItem as CivilKnowledgeRevision; if (r != null) DeepSystems.OpenExternalValue(w, ctx, "ARTICLE REVISION " + r.Id, r, "CIVILIZATION › COMMONS › REVISIONS"); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenPluralityForum(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<PluralityRow> rows = Store.KnowledgeArticles.Where(a => !String.Equals(a.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(a.NodeId)).GroupBy(a => a.NodeId, StringComparer.OrdinalIgnoreCase).Select(g => new PluralityRow { NodeId = g.Key, Articles = g.ToList(), DistinctInterpretations = g.Select(a => a.BodyHash ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count(), DistinctInstitutions = g.Select(a => a.InstitutionId ?? "CIVILIZATION").Distinct(StringComparer.OrdinalIgnoreCase).Count() }).Where(r => r.Articles.Count > 1 && (r.DistinctInterpretations > 1 || r.DistinctInstitutions > 1)).OrderByDescending(r => r.DistinctInstitutions).ThenByDescending(r => r.DistinctInterpretations).ToList(); Window w = W(owner, "YOMI · PLURALITY FORUM", 1120, 760); DockPanel root = Shell("PLURALITY FORUM", "Multiple institutional readings of the same universal address. Difference is surfaced as plurality; contradiction requires human interpretation of the actual claims."); ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { PluralityRow r = list.SelectedItem as PluralityRow; if (r != null) OpenPlurality(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenPlurality(Window owner, DeepSystemsContext ctx, WorldIndex index, PluralityRow row)
        {
            WorldNode node; index.Nodes.TryGetValue(row.NodeId ?? "", out node); Window w = W(owner, "YOMI · PLURALITY · " + (node == null ? row.NodeId : node.Label), 1080, 720); DockPanel root = Shell("PLURALITY · " + (node == null ? row.NodeId : node.Label), row.Articles.Count.ToString(CultureInfo.InvariantCulture) + " articles · " + row.DistinctInstitutions.ToString(CultureInfo.InvariantCulture) + " institutional scopes · " + row.DistinctInterpretations.ToString(CultureInfo.InvariantCulture) + " article fingerprints"); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("SOURCE PASSPORT", delegate { if (node != null) WorldKernel.ExternalOpenDossier(w, ctx, index, node.Id); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); ListBox list = List(); list.ItemsSource = row.Articles; list.MouseDoubleClick += delegate { CivilKnowledgeArticle a = list.SelectedItem as CivilKnowledgeArticle; if (a != null) OpenKnowledgeArticle(w, ctx, index, a); }; root.Children.Add(list); w.Content = root; w.Show(); AppendChronicle(ctx, "PLURALITY_INSPECT", row.NodeId, row.Articles.Count.ToString(CultureInfo.InvariantCulture) + " articles · " + row.DistinctInterpretations.ToString(CultureInfo.InvariantCulture) + " interpretations", HashStrings(row.Articles.Select(a => a.BodyHash)));
        }

        private static string KnowledgeHash(string title, string body, string epistemicClass) { return Sha((title ?? "") + "|" + (epistemicClass ?? "") + "|" + (body ?? "")); }
        private static int PluralityObjectCount() { return Store.KnowledgeArticles.Where(a => !String.Equals(a.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(a.NodeId)).GroupBy(a => a.NodeId, StringComparer.OrdinalIgnoreCase).Count(g => g.Count() > 1 && (g.Select(a => a.BodyHash ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1 || g.Select(a => a.InstitutionId ?? "CIVILIZATION").Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)); }

        private static void OpenBorders(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<CivilInstitution> institutions = ActiveInstitutions(); Dictionary<string, HashSet<string>> sets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); foreach (CivilInstitution i in institutions) sets[i.Id] = new HashSet<string>(WorldKernel.ExternalQuery(index, i.JurisdictionQuery).Nodes.Take(4096).Select(n => n.Id), StringComparer.OrdinalIgnoreCase); List<BorderRow> rows = new List<BorderRow>();
            for (int a = 0; a < institutions.Count; a++) for (int b = a + 1; b < institutions.Count; b++) { CivilInstitution ia = institutions[a], ib = institutions[b]; HashSet<string> sa = sets[ia.Id], sb = sets[ib.Id]; IEnumerable<string> smaller = sa.Count <= sb.Count ? sa : sb; List<string> sharedAll = smaller.Where(id => sa.Contains(id) && sb.Contains(id)).ToList(); int sharedCount = sharedAll.Count; int union = sa.Count + sb.Count - sharedCount; string relation = sharedCount == 0 ? "DISJOINT" : sharedCount == Math.Min(sa.Count, sb.Count) ? "CONTAINMENT" : "OVERLAP"; rows.Add(new BorderRow { A = ia, B = ib, ACount = sa.Count, BCount = sb.Count, SharedCount = sharedCount, Jaccard = union == 0 ? 0 : (double)sharedCount / union, Relation = relation, SharedNodeIds = sharedAll.Take(1024).ToList() }); }
            Window w = W(owner, "YOMI · JURISDICTION BORDER ATLAS", 1160, 780); DockPanel root = Shell("JURISDICTION BORDER ATLAS", "Pairwise territory overlap derived from live YQL membership. Overlap is co-jurisdiction, not consensus, ownership or causality."); ListBox list = List(); list.ItemsSource = rows.OrderByDescending(r => r.SharedCount).ThenByDescending(r => r.Jaccard).Take(1024).ToList(); list.MouseDoubleClick += delegate { BorderRow r = list.SelectedItem as BorderRow; if (r != null) OpenBorder(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenBorder(Window owner, DeepSystemsContext ctx, WorldIndex index, BorderRow row)
        {
            Window w = W(owner, "YOMI · BORDER · " + row.A.Name + " / " + row.B.Name, 1080, 720); DockPanel root = Shell(row.Relation + " · " + row.A.Name + " / " + row.B.Name, row.SharedCount.ToString(CultureInfo.InvariantCulture) + " shared objects · Jaccard " + row.Jaccard.ToString("0.0000", CultureInfo.InvariantCulture)); ListBox list = List(); list.ItemsSource = row.SharedNodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show(); AppendChronicle(ctx, "BORDER_INSPECT", row.A.Id + ":" + row.B.Id, row.Relation + " · " + row.SharedCount.ToString(CultureInfo.InvariantCulture), HashStrings(row.SharedNodeIds));
        }

        private static void OpenConstellations(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<CivilInstitution> institutions = ActiveInstitutions().Where(i => !String.IsNullOrWhiteSpace(i.Id)).GroupBy(i => i.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList(); List<TreatyRow> treaties = BuildTreaties(index); Dictionary<string, List<string>> adjacency = institutions.ToDictionary(i => i.Id, i => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (TreatyRow t in treaties) { if (!adjacency.ContainsKey(t.A.Id) || !adjacency.ContainsKey(t.B.Id)) continue; adjacency[t.A.Id].Add(t.B.Id); adjacency[t.B.Id].Add(t.A.Id); } HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); List<ConstellationRow> rows = new List<ConstellationRow>();
            foreach (CivilInstitution seed in institutions) { if (!seen.Add(seed.Id)) continue; Queue<string> q = new Queue<string>(); q.Enqueue(seed.Id); List<string> ids = new List<string>(); while (q.Count > 0) { string id = q.Dequeue(); ids.Add(id); foreach (string next in adjacency[id]) if (seen.Add(next)) q.Enqueue(next); } HashSet<string> component = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase); List<TreatyRow> inside = treaties.Where(t => component.Contains(t.A.Id) && component.Contains(t.B.Id)).ToList(); Dictionary<string, int> strength = ids.ToDictionary(id => id, id => inside.Where(t => t.A.Id == id || t.B.Id == id).Sum(t => t.Count), StringComparer.OrdinalIgnoreCase); string hubId = strength.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).FirstOrDefault(); rows.Add(new ConstellationRow { InstitutionIds = ids, BridgePairs = inside.Count, BridgeEdges = inside.Sum(t => t.Count), HubInstitutionId = hubId }); }
            Window w = W(owner, "YOMI · TREATY CONSTELLATIONS", 1120, 760); DockPanel root = Shell("TREATY CONSTELLATIONS", "Connected components in the current institutional treaty graph. An isolated jurisdiction is visible rather than erased; a central hub is topology, not political authority."); ListBox list = List(); list.ItemsSource = rows.OrderByDescending(r => r.InstitutionIds.Count).ThenByDescending(r => r.BridgeEdges).ToList(); list.MouseDoubleClick += delegate { ConstellationRow r = list.SelectedItem as ConstellationRow; if (r != null) OpenConstellation(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenConstellation(Window owner, DeepSystemsContext ctx, WorldIndex index, ConstellationRow row)
        {
            CivilInstitution hub = Institution(row.HubInstitutionId); Window w = W(owner, "YOMI · INSTITUTIONAL CONSTELLATION", 1040, 700); DockPanel root = Shell("INSTITUTIONAL CONSTELLATION", row.InstitutionIds.Count.ToString(CultureInfo.InvariantCulture) + " institutions · " + row.BridgePairs.ToString(CultureInfo.InvariantCulture) + " treaty pairs · " + row.BridgeEdges.ToString(CultureInfo.InvariantCulture) + " bridge edges · topology hub " + (hub == null ? "∅" : hub.Name)); ListBox list = List(); list.ItemsSource = row.InstitutionIds.Select(Institution).Where(i => i != null).ToList(); list.MouseDoubleClick += delegate { CivilInstitution i = list.SelectedItem as CivilInstitution; if (i != null) OpenInstitution(w, ctx, index, i); }; root.Children.Add(list); w.Content = root; w.Show(); AppendChronicle(ctx, "CONSTELLATION_INSPECT", row.HubInstitutionId, row.InstitutionIds.Count.ToString(CultureInfo.InvariantCulture) + " institutions · " + row.BridgeEdges.ToString(CultureInfo.InvariantCulture) + " edges", HashStrings(row.InstitutionIds));
        }

        private static void OpenRelayRoutes(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<TreatyRow> rows = BuildTreaties(index); Window w = W(owner, "YOMI · EVIDENCE RELAY ROUTES", 1120, 760); DockPanel root = Shell("EVIDENCE RELAY ROUTES", "Select a treaty pair to calculate a bounded shortest path between high-degree objects in its two jurisdictions. Routes prove traversability in the indexed graph, not causal transmission."); ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { TreatyRow r = list.SelectedItem as TreatyRow; if (r != null) OpenRelayRoute(w, ctx, index, r); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenRelayRoute(Window owner, DeepSystemsContext ctx, WorldIndex index, TreatyRow treaty)
        {
            List<WorldNode> left = WorldKernel.ExternalQuery(index, treaty.A.JurisdictionQuery).Nodes.Take(4096).ToList(); List<WorldNode> right = WorldKernel.ExternalQuery(index, treaty.B.JurisdictionQuery).Nodes.Take(4096).ToList(); WorldNode a = left.OrderByDescending(n => WorldKernel.ExternalDegree(index, n.Id)).FirstOrDefault(); WorldNode b = right.OrderByDescending(n => WorldKernel.ExternalDegree(index, n.Id)).FirstOrDefault(); List<string> path = a == null || b == null ? new List<string>() : WorldKernel.ExternalShortestPath(index, a.Id, b.Id, 24); if (path.Count == 0 && treaty.Edges.Count > 0) path = new List<string> { treaty.Edges[0].FromId, treaty.Edges[0].ToId };
            Window w = W(owner, "YOMI · RELAY · " + treaty.A.Name + " → " + treaty.B.Name, 1080, 720); DockPanel root = Shell("EVIDENCE RELAY ROUTE", treaty.A.Name + " → " + treaty.B.Name + " · " + Math.Max(0, path.Count - 1).ToString(CultureInfo.InvariantCulture) + " hops · bounded at 24"); ListBox list = List(); list.ItemsSource = path.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show(); AppendChronicle(ctx, "RELAY_ROUTE", treaty.A.Id + ":" + treaty.B.Id, Math.Max(0, path.Count - 1).ToString(CultureInfo.InvariantCulture) + " hops", HashStrings(path));
        }

        private static void OpenCompacts(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · CIVIL COMPACTS", 1120, 760); DockPanel root = Shell("CIVIL COMPACTS", "Explicit organizational cooperation is stored separately from computed graph treaties. Evidence can support a compact, drift away from it, or be absent without rewriting the compact's human meaning."); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("PROPOSE FROM STRONGEST BRIDGE", delegate { TreatyRow strongest = BuildTreaties(index).FirstOrDefault(); if (strongest == null) MessageBox.Show(w, "No cross-jurisdiction bridge exists in the current world.", "YOMI · Civil Compacts"); else CreateCompactFromTreaty(w, ctx, index, strongest); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); ListBox list = List(); list.ItemsSource = Store.Compacts.OrderByDescending(c => c.UpdatedUtc ?? c.CreatedUtc).ToList(); list.MouseDoubleClick += delegate { CivilCompact c = list.SelectedItem as CivilCompact; if (c != null) OpenCompact(w, ctx, index, c); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void CreateCompactFromTreaty(Window owner, DeepSystemsContext ctx, WorldIndex index, TreatyRow treaty)
        {
            string name = Prompt(owner, "PROPOSE CIVIL COMPACT", "Compact name", treaty.A.Name + " / " + treaty.B.Name + " Research Compact"); if (String.IsNullOrWhiteSpace(name)) return; string purpose = PromptLarge(owner, "COMPACT PURPOSE", "Human organizational purpose", "Coordinate investigation across both jurisdictions while preserving independent evidence standards."); if (purpose == null) return; CivilCompact compact = new CivilCompact { Id = Id("compact"), Name = name.Trim(), Purpose = purpose.Trim(), Status = "PROPOSED", CreatedUtc = Now(), UpdatedUtc = Now() }; compact.InstitutionIds.Add(treaty.A.Id); compact.InstitutionIds.Add(treaty.B.Id); EvaluateCompact(index, compact); Store.Compacts.Insert(0, compact); TrimStore(); Save(ctx); AppendChronicle(ctx, "COMPACT_PROPOSED", compact.Id, compact.Name, compact.EvidenceFingerprint); OpenCompact(owner, ctx, index, compact);
        }

        private static void EvaluateCompact(WorldIndex index, CivilCompact compact)
        {
            HashSet<string> parties = new HashSet<string>(compact.InstitutionIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase); List<TreatyRow> evidence = BuildTreaties(index).Where(t => parties.Contains(t.A.Id) && parties.Contains(t.B.Id)).ToList(); compact.EvidenceNodeIds = evidence.SelectMany(t => t.Edges).SelectMany(e => new[] { e.FromId, e.ToId }).Distinct(StringComparer.OrdinalIgnoreCase).Take(512).ToList(); compact.EvidenceFingerprint = Sha(String.Join("\n", evidence.OrderBy(t => t.A.Id).ThenBy(t => t.B.Id).Select(t => t.A.Id + "|" + t.B.Id + "|" + t.Count.ToString(CultureInfo.InvariantCulture) + "|" + HashEdges(t.Edges)).ToArray())); compact.LastEvaluatedUtc = Now(); compact.UpdatedUtc = Now();
        }

        private static void OpenCompact(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilCompact compact)
        {
            Window w = W(owner, "YOMI · COMPACT · " + compact.Name, 1100, 740); DockPanel root = Shell((compact.Status ?? "PROPOSED") + " · " + compact.Name, compact.Purpose ?? "No purpose recorded."); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("REEVALUATE EVIDENCE", delegate { string before = compact.EvidenceFingerprint; EvaluateCompact(index, compact); Save(ctx); AppendChronicle(ctx, "COMPACT_EVIDENCE", compact.Id, String.Equals(before, compact.EvidenceFingerprint, StringComparison.OrdinalIgnoreCase) ? "UNCHANGED" : "CHANGED", compact.EvidenceFingerprint); w.Close(); OpenCompact(owner, ctx, index, compact); })); tools.Children.Add(Btn("RATIFY", delegate { TransitionCompact(ctx, compact, "RATIFIED"); w.Close(); OpenCompact(owner, ctx, index, compact); })); tools.Children.Add(Btn("SUSPEND", delegate { TransitionCompact(ctx, compact, "SUSPENDED"); w.Close(); OpenCompact(owner, ctx, index, compact); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); Grid body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition()); StringBuilder text = new StringBuilder(); text.AppendLine("COMPACT ID       " + compact.Id); text.AppendLine("CREATED          " + compact.CreatedUtc); text.AppendLine("UPDATED          " + compact.UpdatedUtc); text.AppendLine("LAST EVALUATED   " + (compact.LastEvaluatedUtc ?? "never")); text.AppendLine("EVIDENCE HASH    " + (compact.EvidenceFingerprint ?? "∅")); text.AppendLine(); text.AppendLine("SIGNATORIES"); foreach (string id in compact.InstitutionIds) { CivilInstitution i = Institution(id); text.AppendLine("· " + (i == null ? id : i.Name)); } TextBox info = ReadOnly(text.ToString()); Grid.SetColumn(info, 0); body.Children.Add(info); ListBox evidence = List(); evidence.ItemsSource = compact.EvidenceNodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).ToList(); evidence.MouseDoubleClick += delegate { WorldNode n = evidence.SelectedItem as WorldNode; if (n != null) WorldKernel.ExternalOpenDossier(w, ctx, index, n.Id); }; Grid.SetColumn(evidence, 1); body.Children.Add(evidence); root.Children.Add(body); w.Content = root; w.Show();
        }

        private static void TransitionCompact(DeepSystemsContext ctx, CivilCompact compact, string status)
        {
            if (String.Equals(compact.Status, status, StringComparison.OrdinalIgnoreCase)) return; string before = compact.Status ?? "PROPOSED"; compact.Status = status; compact.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "COMPACT_" + status, compact.Id, before + " → " + status + " · " + compact.Name, compact.EvidenceFingerprint);
        }

        private static void OpenGenealogy(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<GenealogyRow> rows = BuildGenealogy(); Window w = W(owner, "YOMI · INSTITUTION GENEALOGY", 1080, 740); DockPanel root = Shell("INSTITUTION GENEALOGY", "Explicit parent-child lineage with cycle-safe traversal, archival state and charter revision depth."); ListBox list = List(); list.ItemsSource = rows; list.MouseDoubleClick += delegate { GenealogyRow r = list.SelectedItem as GenealogyRow; if (r != null && r.Institution != null) OpenInstitution(w, ctx, index, r.Institution); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static List<GenealogyRow> BuildGenealogy()
        {
            List<GenealogyRow> rows = new List<GenealogyRow>(); HashSet<string> emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (CivilInstitution root in Store.Institutions.Where(i => String.IsNullOrWhiteSpace(i.ParentInstitutionId) || Institution(i.ParentInstitutionId) == null).OrderBy(i => i.Name)) AppendGenealogy(root, 0, rows, emitted, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); foreach (CivilInstitution orphan in Store.Institutions.Where(i => !emitted.Contains(i.Id))) AppendGenealogy(orphan, 0, rows, emitted, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); return rows;
        }

        private static void AppendGenealogy(CivilInstitution inst, int depth, List<GenealogyRow> rows, HashSet<string> emitted, HashSet<string> ancestry)
        {
            if (inst == null || emitted.Contains(inst.Id)) return; bool loop = !ancestry.Add(inst.Id); rows.Add(new GenealogyRow { Institution = inst, Depth = depth, Loop = loop }); emitted.Add(inst.Id); if (loop || depth >= 32) return; foreach (CivilInstitution child in Store.Institutions.Where(i => String.Equals(i.ParentInstitutionId, inst.Id, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.Name)) AppendGenealogy(child, depth + 1, rows, emitted, new HashSet<string>(ancestry, StringComparer.OrdinalIgnoreCase));
        }

        private static void AmendInstitutionPrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            string charter = PromptLarge(owner, "AMEND CHARTER", "Charter", inst.Charter ?? ""); if (charter == null) return; string query = Prompt(owner, "AMEND JURISDICTION", "YQL jurisdiction; baseline remains unchanged until explicit rebaseline", inst.JurisdictionQuery ?? "STATS"); if (String.IsNullOrWhiteSpace(query)) return; WorldQueryResult preview = WorldKernel.ExternalQuery(index, query.Trim()); if (preview.Nodes.Count == 0 && !query.Trim().StartsWith("STATS", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(owner, "The amended jurisdiction currently resolves zero nodes. Amendment not recorded.", "YOMI · Institution"); return; } string before = Sha((inst.Charter ?? "") + "|" + (inst.JurisdictionQuery ?? "")); string after = Sha(charter.Trim() + "|" + query.Trim()); if (String.Equals(before, after, StringComparison.OrdinalIgnoreCase)) return; CivilAmendment amendment = new CivilAmendment { Id = Id("amendment"), CreatedUtc = Now(), Kind = !String.Equals(inst.JurisdictionQuery, query.Trim(), StringComparison.Ordinal) ? "CHARTER_AND_JURISDICTION" : "CHARTER", Summary = "Revision " + (Math.Max(1, inst.CharterRevision) + 1).ToString(CultureInfo.InvariantCulture), BeforeHash = before, AfterHash = after, PreviousCharter = inst.Charter, PreviousJurisdictionQuery = inst.JurisdictionQuery }; inst.Amendments.Insert(0, amendment); inst.Amendments = inst.Amendments.Take(MaxAmendmentsPerInstitution).ToList(); inst.Charter = charter.Trim(); inst.JurisdictionQuery = query.Trim(); inst.CharterRevision = Math.Max(1, inst.CharterRevision) + 1; inst.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "INSTITUTION_AMENDED", inst.Id, amendment.Kind + " · r" + inst.CharterRevision.ToString(CultureInfo.InvariantCulture), after); MessageBox.Show(owner, "Amendment recorded. The prior baseline was intentionally preserved so jurisdiction drift remains visible.", "YOMI · Institution");
        }

        private static void OpenAmendments(Window owner, DeepSystemsContext ctx, CivilInstitution inst)
        {
            Window w = W(owner, "YOMI · AMENDMENTS · " + inst.Name, 1040, 700); DockPanel root = Shell(inst.Name + " · AMENDMENT LEDGER", "Prior charter and jurisdiction text is retained as organizational provenance. Amendments do not rewrite the World Kernel."); ListBox list = List(); list.ItemsSource = inst.Amendments.OrderByDescending(a => a.CreatedUtc).ToList(); list.MouseDoubleClick += delegate { CivilAmendment a = list.SelectedItem as CivilAmendment; if (a != null) DeepSystems.OpenExternalValue(w, ctx, "AMENDMENT " + a.Id, a, "CIVILIZATION › " + inst.Name + " › AMENDMENTS"); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void ToggleInstitutionLifecycle(Window owner, DeepSystemsContext ctx, CivilInstitution inst)
        {
            string before = inst.Status ?? "ACTIVE"; inst.Status = String.Equals(before, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "ARCHIVED"; inst.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "INSTITUTION_" + inst.Status, inst.Id, before + " → " + inst.Status + " · " + inst.Name); MessageBox.Show(owner, "Institution is now " + inst.Status + ". Reopen the Institution Registry to refresh computed civilization topology.", "YOMI · Institution");
        }

        private static void OpenConstitutionalAudit(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); List<CivilAuditFinding> findings = BuildConstitutionalAudit(ctx, index); int failures = findings.Count(f => f.Severity == "FAIL"); int warnings = findings.Count(f => f.Severity == "WARN"); int info = findings.Count(f => f.Severity == "INFO"); Window w = W(owner, "YOMI · CONSTITUTIONAL AUDIT CHAMBER", 1180, 800); DockPanel root = Shell("CONSTITUTIONAL AUDIT CHAMBER", failures.ToString(CultureInfo.InvariantCulture) + " failures · " + warnings.ToString(CultureInfo.InvariantCulture) + " warnings · " + info.ToString(CultureInfo.InvariantCulture) + " informational observations · read-only audit"); ListBox list = List(); list.ItemsSource = findings.OrderBy(f => AuditSeverityRank(f.Severity)).ThenBy(f => f.Code).ThenBy(f => f.SubjectId).ToList(); list.MouseDoubleClick += delegate { CivilAuditFinding f = list.SelectedItem as CivilAuditFinding; if (f == null) return; if (!String.IsNullOrWhiteSpace(f.NodeId) && index.Nodes.ContainsKey(f.NodeId)) WorldKernel.ExternalOpenDossier(w, ctx, index, f.NodeId); else DeepSystems.OpenExternalValue(w, ctx, "AUDIT " + f.Code, f, "CIVILIZATION › CONSTITUTIONAL AUDIT"); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static List<CivilAuditFinding> BuildConstitutionalAudit(DeepSystemsContext ctx, WorldIndex index)
        {
            List<CivilAuditFinding> rows = new List<CivilAuditFinding>(); HashSet<string> globalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CivilInstitution i in Store.Institutions) AuditId(rows, globalIds, "INSTITUTION", i.Id); foreach (CivilExpedition e in Store.Expeditions) AuditId(rows, globalIds, "EXPEDITION", e.Id); foreach (CivilSentinel s in Store.Sentinels) AuditId(rows, globalIds, "SENTINEL", s.Id); foreach (CivilAlert a in Store.Alerts) AuditId(rows, globalIds, "ALERT", a.Id); foreach (CivilResolution r in Store.Resolutions) AuditId(rows, globalIds, "RESOLUTION", r.Id); foreach (CivilCompact c in Store.Compacts) AuditId(rows, globalIds, "COMPACT", c.Id); foreach (CivilCensusEpoch e in Store.CensusEpochs) AuditId(rows, globalIds, "CENSUS", e.Id); foreach (CivilKnowledgeArticle a in Store.KnowledgeArticles) AuditId(rows, globalIds, "ARTICLE", a.Id);
            foreach (CivilInstitution i in Store.Institutions)
            {
                if (!String.IsNullOrWhiteSpace(i.ParentInstitutionId) && Institution(i.ParentInstitutionId) == null) rows.Add(Audit("FAIL", "PARENT_MISSING", i.Id, i.ParentInstitutionId, null)); if (InstitutionCycle(i)) rows.Add(Audit("FAIL", "LINEAGE_CYCLE", i.Id, "Parent chain revisits an institution.", null)); string baselineHash = HashMap(i.BaselineNodes); if (!String.IsNullOrWhiteSpace(i.BaselineFingerprint) && !String.Equals(i.BaselineFingerprint, baselineHash, StringComparison.OrdinalIgnoreCase)) rows.Add(Audit("FAIL", "BASELINE_HASH", i.Id, "Stored baseline fingerprint does not match baseline map.", null)); WorldQueryResult current = WorldKernel.ExternalQuery(index, i.JurisdictionQuery); Drift drift = Compare(i.BaselineNodes, Fingerprints(current.Nodes)); if (drift.Total > 0) rows.Add(Audit("INFO", "JURISDICTION_DRIFT", i.Id, "+" + drift.Added.Count + " / -" + drift.Removed.Count + " / ~" + drift.Changed.Count, drift.Changed.Concat(drift.Added).FirstOrDefault()));
            }
            foreach (CivilSentinel s in Store.Sentinels) { if (!String.IsNullOrWhiteSpace(s.InstitutionId) && Institution(s.InstitutionId) == null) rows.Add(Audit("FAIL", "SENTINEL_SCOPE", s.Id, "Institution reference missing: " + s.InstitutionId, null)); string hash = HashMap(s.LastNodes); if (!String.IsNullOrWhiteSpace(s.LastFingerprint) && !String.Equals(hash, s.LastFingerprint, StringComparison.OrdinalIgnoreCase)) rows.Add(Audit("FAIL", "SENTINEL_HASH", s.Id, "Last-node fingerprint mismatch.", null)); }
            foreach (CivilAlert a in Store.Alerts) { if (!String.IsNullOrWhiteSpace(a.SentinelId) && !Store.Sentinels.Any(s => String.Equals(s.Id, a.SentinelId, StringComparison.OrdinalIgnoreCase))) rows.Add(Audit("WARN", "ALERT_SENTINEL_MISSING", a.Id, a.SentinelId, a.NodeIds.FirstOrDefault())); if (!String.IsNullOrWhiteSpace(a.InstitutionId) && Institution(a.InstitutionId) == null) rows.Add(Audit("WARN", "ALERT_SCOPE_MISSING", a.Id, a.InstitutionId, a.NodeIds.FirstOrDefault())); }
            foreach (CivilExpedition e in Store.Expeditions) { if (!String.IsNullOrWhiteSpace(e.InstitutionId) && Institution(e.InstitutionId) == null) rows.Add(Audit("WARN", "EXPEDITION_SCOPE", e.Id, e.InstitutionId, e.CurrentNodeId)); if (!String.IsNullOrWhiteSpace(e.CurrentNodeId) && !index.Nodes.ContainsKey(e.CurrentNodeId)) rows.Add(Audit("WARN", "EXPEDITION_CURRENT_ABSENT", e.Id, e.CurrentNodeId, e.CurrentNodeId)); if (!String.IsNullOrWhiteSpace(e.RootNodeId) && !index.Nodes.ContainsKey(e.RootNodeId)) rows.Add(Audit("INFO", "EXPEDITION_ROOT_ABSENT", e.Id, e.RootNodeId, e.RootNodeId)); }
            foreach (CivilResolution r in Store.Resolutions) { if (!String.IsNullOrWhiteSpace(r.InstitutionId) && Institution(r.InstitutionId) == null) rows.Add(Audit("WARN", "RESOLUTION_SCOPE", r.Id, r.InstitutionId, r.EvidenceNodeIds.FirstOrDefault())); int absent = r.EvidenceNodeIds.Count(id => !index.Nodes.ContainsKey(id)); if (absent > 0) rows.Add(Audit("INFO", "RESOLUTION_EVIDENCE_ABSENT", r.Id, absent.ToString(CultureInfo.InvariantCulture) + " evidence anchors absent from current bounded world.", r.EvidenceNodeIds.FirstOrDefault(id => !index.Nodes.ContainsKey(id)))); }
            foreach (CivilCompact c in Store.Compacts) { int parties = c.InstitutionIds.Distinct(StringComparer.OrdinalIgnoreCase).Count(); if (parties < 2) rows.Add(Audit("FAIL", "COMPACT_PARTIES", c.Id, "Compact has fewer than two distinct signatories.", null)); foreach (string id in c.InstitutionIds.Where(id => Institution(id) == null)) rows.Add(Audit("WARN", "COMPACT_SIGNATORY_MISSING", c.Id, id, null)); }
            foreach (CivilKnowledgeArticle a in Store.KnowledgeArticles) { if (!String.IsNullOrWhiteSpace(a.InstitutionId) && Institution(a.InstitutionId) == null) rows.Add(Audit("WARN", "ARTICLE_SCOPE", a.Id, a.InstitutionId, a.NodeId)); string bodyHash = KnowledgeHash(a.Title, a.Body, a.EpistemicClass); if (!String.Equals(bodyHash, a.BodyHash, StringComparison.OrdinalIgnoreCase)) rows.Add(Audit("FAIL", "ARTICLE_HASH", a.Id, "Article body fingerprint mismatch.", a.NodeId)); WorldNode n; if (!index.Nodes.TryGetValue(a.NodeId ?? "", out n)) rows.Add(Audit("INFO", "ARTICLE_SOURCE_ABSENT", a.Id, a.NodeId, a.NodeId)); else if (!String.Equals(n.Fingerprint ?? "", a.SourceNodeFingerprint ?? "", StringComparison.OrdinalIgnoreCase)) rows.Add(Audit("INFO", "ARTICLE_SOURCE_CHANGED", a.Id, "Current World fingerprint differs from authoring fingerprint.", a.NodeId)); }
            string chronicle = VerifyChronicle(ReadChronicle(ctx, 4096)); rows.Add(Audit(chronicle.StartsWith("CHAIN VERIFIED", StringComparison.OrdinalIgnoreCase) || chronicle == "EMPTY CHRONICLE" ? "INFO" : "FAIL", "CHRONICLE", "civilization", chronicle, null)); AuditBound(rows, "INSTITUTIONS", Store.Institutions.Count, MaxInstitutions); AuditBound(rows, "EXPEDITIONS", Store.Expeditions.Count, MaxExpeditions); AuditBound(rows, "SENTINELS", Store.Sentinels.Count, MaxSentinels); AuditBound(rows, "ALERTS", Store.Alerts.Count, MaxAlerts); AuditBound(rows, "RESOLUTIONS", Store.Resolutions.Count, MaxResolutions); AuditBound(rows, "COMPACTS", Store.Compacts.Count, MaxCompacts); AuditBound(rows, "CENSUS_EPOCHS", Store.CensusEpochs.Count, MaxCensusEpochs); AuditBound(rows, "KNOWLEDGE_ARTICLES", Store.KnowledgeArticles.Count, MaxKnowledgeArticles); if (rows.Count == 0) rows.Add(Audit("INFO", "EMPTY", "civilization", "No civil state exists yet.", null)); return rows;
        }

        private static void AuditId(List<CivilAuditFinding> rows, HashSet<string> ids, string kind, string id) { if (String.IsNullOrWhiteSpace(id)) rows.Add(Audit("FAIL", "IDENTITY_MISSING", kind, "Object has no stable ID.", null)); else if (!ids.Add(id)) rows.Add(Audit("FAIL", "IDENTITY_DUPLICATE", id, kind + " duplicates another civil object ID.", null)); }
        private static CivilAuditFinding Audit(string severity, string code, string subject, string detail, string nodeId) { return new CivilAuditFinding { Severity = severity, Code = code, SubjectId = subject, Detail = detail, NodeId = nodeId }; }
        private static void AuditBound(List<CivilAuditFinding> rows, string code, int count, int limit) { rows.Add(Audit(count <= limit ? "INFO" : "FAIL", "BOUND_" + code, code, count.ToString(CultureInfo.InvariantCulture) + "/" + limit.ToString(CultureInfo.InvariantCulture), null)); }
        private static int AuditSeverityRank(string severity) { return severity == "FAIL" ? 0 : severity == "WARN" ? 1 : 2; }
        private static bool InstitutionCycle(CivilInstitution start) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); CivilInstitution cursor = start; for (int depth = 0; cursor != null && depth <= MaxInstitutions; depth++) { if (!seen.Add(cursor.Id ?? "")) return true; cursor = Institution(cursor.ParentInstitutionId); } return cursor != null; }

        private static void OpenCensus(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); HashSet<string> claimed = ClaimedNodes(index); List<TreatyRow> treaties = BuildTreaties(index);
            StringBuilder s = new StringBuilder(); s.AppendLine("CIVILIZATION CENSUS"); s.AppendLine(); s.AppendLine("Store schema            " + Store.Schema); s.AppendLine("Store revision          " + Store.Revision); s.AppendLine("Active institutions     " + ActiveInstitutions().Count); s.AppendLine("Archived institutions   " + Store.Institutions.Count(i => String.Equals(i.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase))); s.AppendLine("Child institutions      " + Store.Institutions.Count(i => !String.IsNullOrWhiteSpace(i.ParentInstitutionId))); s.AppendLine("Active expeditions      " + Store.Expeditions.Count(e => e.Status == "ACTIVE")); s.AppendLine("Completed expeditions   " + Store.Expeditions.Count(e => e.Status == "COMPLETE")); s.AppendLine("Sentinels               " + Store.Sentinels.Count); s.AppendLine("Open alerts             " + Store.Alerts.Count(a => a.Status != "REVIEWED")); s.AppendLine("Open resolutions        " + Store.Resolutions.Count(r => r.Status == "DRAFT" || r.Status == "DELIBERATING")); s.AppendLine("Ratified compacts       " + Store.Compacts.Count(c => c.Status == "RATIFIED")); s.AppendLine("Commons articles        " + Store.KnowledgeArticles.Count(a => a.Status != "ARCHIVED")); s.AppendLine("Plurality objects       " + PluralityObjectCount()); s.AppendLine("Treaty pairs            " + treaties.Count); s.AppendLine("Treaty bridge edges     " + treaties.Sum(t => t.Count)); s.AppendLine("Census epochs           " + Store.CensusEpochs.Count); s.AppendLine("World nodes             " + index.Nodes.Count); s.AppendLine("Claimed jurisdiction    " + claimed.Count); s.AppendLine("Coverage                " + (index.Nodes.Count == 0 ? "0%" : ((100.0 * claimed.Count / index.Nodes.Count).ToString("0.00", CultureInfo.InvariantCulture) + "%"))); s.AppendLine("World edges             " + index.Edges.Count); s.AppendLine("World fingerprint       " + index.Fingerprint);
            Window w = W(owner, "YOMI · CIVILIZATION CENSUS", 940, 700); DockPanel root = Shell("CIVILIZATION CENSUS", "Demand-driven live census. Capture creates a bounded historical epoch; merely opening this surface does not persist a snapshot."); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("CAPTURE CENSUS EPOCH", delegate { CivilCensusEpoch epoch = CaptureCensusEpoch(ctx, index); MessageBox.Show(w, "Captured " + epoch.Id, "YOMI · Civilization Census"); })); tools.Children.Add(Btn("EPOCH LEDGER", delegate { OpenCensusEpochs(w, ctx, index); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); TextBox box = ReadOnly(s.ToString()); root.Children.Add(box); w.Content = root; w.Show();
        }

        private static HashSet<string> ClaimedNodes(WorldIndex index)
        {
            HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (CivilInstitution i in ActiveInstitutions()) foreach (WorldNode n in WorldKernel.ExternalQuery(index, i.JurisdictionQuery).Nodes.Take(4096)) claimed.Add(n.Id); return claimed;
        }

        private static CivilCensusEpoch CaptureCensusEpoch(DeepSystemsContext ctx, WorldIndex index)
        {
            HashSet<string> claimed = ClaimedNodes(index); List<TreatyRow> treaties = BuildTreaties(index); CivilCensusEpoch epoch = new CivilCensusEpoch { Id = Id("census"), CapturedUtc = Now(), WorldFingerprint = index.Fingerprint, CoverageFingerprint = HashStrings(claimed), TreatyFingerprint = Sha(String.Join("\n", treaties.OrderBy(t => t.A.Id).ThenBy(t => t.B.Id).Select(t => t.A.Id + "|" + t.B.Id + "|" + t.Count.ToString(CultureInfo.InvariantCulture) + "|" + HashEdges(t.Edges)).ToArray())), WorldNodes = index.Nodes.Count, WorldEdges = index.Edges.Count, ClaimedNodes = claimed.Count, ActiveInstitutions = ActiveInstitutions().Count, TreatyBridges = treaties.Sum(t => t.Count), OpenAlerts = Store.Alerts.Count(a => a.Status != "REVIEWED"), ActiveExpeditions = Store.Expeditions.Count(e => e.Status == "ACTIVE"), OpenResolutions = Store.Resolutions.Count(r => r.Status == "DRAFT" || r.Status == "DELIBERATING"), RatifiedCompacts = Store.Compacts.Count(c => c.Status == "RATIFIED"), KnowledgeArticles = Store.KnowledgeArticles.Count(a => a.Status != "ARCHIVED"), PluralityObjects = PluralityObjectCount() }; foreach (CivilInstitution i in ActiveInstitutions()) epoch.JurisdictionFingerprints[i.Id] = HashMap(Fingerprints(WorldKernel.ExternalQuery(index, i.JurisdictionQuery).Nodes)); Store.CensusEpochs.Insert(0, epoch); TrimStore(); Save(ctx); AppendChronicle(ctx, "CENSUS_EPOCH", epoch.Id, epoch.ClaimedNodes.ToString(CultureInfo.InvariantCulture) + "/" + epoch.WorldNodes.ToString(CultureInfo.InvariantCulture) + " covered · " + epoch.TreatyBridges.ToString(CultureInfo.InvariantCulture) + " bridges", epoch.CoverageFingerprint); return epoch;
        }

        private static void OpenCensusEpochs(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Load(ctx); Window w = W(owner, "YOMI · CENSUS EPOCH LEDGER", 1120, 760); DockPanel root = Shell("CENSUS EPOCH LEDGER", "Bounded civilization-wide snapshots preserve observed coverage and topology fingerprints. They are exact for the bounded index captured, not a claim about unindexed state."); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 8) }; tools.Children.Add(Btn("CAPTURE NOW", delegate { CaptureCensusEpoch(ctx, index); w.Close(); OpenCensusEpochs(owner, ctx, index); })); DockPanel.SetDock(tools, Dock.Top); root.Children.Add(tools); ListBox list = List(); list.ItemsSource = Store.CensusEpochs.OrderByDescending(e => e.CapturedUtc).ToList(); list.MouseDoubleClick += delegate { CivilCensusEpoch e = list.SelectedItem as CivilCensusEpoch; if (e != null) OpenCensusEpoch(w, ctx, e); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenCensusEpoch(Window owner, DeepSystemsContext ctx, CivilCensusEpoch epoch)
        {
            CivilCensusEpoch previous = Store.CensusEpochs.Where(e => String.Compare(e.CapturedUtc, epoch.CapturedUtc, StringComparison.Ordinal) < 0).OrderByDescending(e => e.CapturedUtc).FirstOrDefault(); StringBuilder s = new StringBuilder(); s.AppendLine("CENSUS EPOCH      " + epoch.Id); s.AppendLine("CAPTURED          " + epoch.CapturedUtc); s.AppendLine("WORLD             " + epoch.WorldNodes + " nodes / " + epoch.WorldEdges + " edges"); s.AppendLine("COVERAGE          " + epoch.ClaimedNodes + " / " + epoch.WorldNodes + " · " + (epoch.WorldNodes == 0 ? "0%" : (100.0 * epoch.ClaimedNodes / epoch.WorldNodes).ToString("0.00", CultureInfo.InvariantCulture) + "%")); s.AppendLine("INSTITUTIONS      " + epoch.ActiveInstitutions); s.AppendLine("TREATY BRIDGES    " + epoch.TreatyBridges); s.AppendLine("OPEN ALERTS       " + epoch.OpenAlerts); s.AppendLine("ACTIVE EXPEDITIONS " + epoch.ActiveExpeditions); s.AppendLine("OPEN RESOLUTIONS  " + epoch.OpenResolutions); s.AppendLine("RATIFIED COMPACTS " + epoch.RatifiedCompacts); s.AppendLine("COMMONS ARTICLES  " + epoch.KnowledgeArticles); s.AppendLine("PLURALITY OBJECTS " + epoch.PluralityObjects); s.AppendLine("WORLD HASH        " + epoch.WorldFingerprint); s.AppendLine("COVERAGE HASH     " + epoch.CoverageFingerprint); s.AppendLine("TREATY HASH       " + epoch.TreatyFingerprint); if (previous != null) { s.AppendLine(); s.AppendLine("DELTA FROM " + previous.CapturedUtc); s.AppendLine("WORLD NODES       " + Signed(epoch.WorldNodes - previous.WorldNodes)); s.AppendLine("WORLD EDGES       " + Signed(epoch.WorldEdges - previous.WorldEdges)); s.AppendLine("CLAIMED NODES     " + Signed(epoch.ClaimedNodes - previous.ClaimedNodes)); s.AppendLine("INSTITUTIONS      " + Signed(epoch.ActiveInstitutions - previous.ActiveInstitutions)); s.AppendLine("TREATY BRIDGES    " + Signed(epoch.TreatyBridges - previous.TreatyBridges)); s.AppendLine("OPEN ALERTS       " + Signed(epoch.OpenAlerts - previous.OpenAlerts)); s.AppendLine("RESOLUTIONS       " + Signed(epoch.OpenResolutions - previous.OpenResolutions)); s.AppendLine("COMPACTS          " + Signed(epoch.RatifiedCompacts - previous.RatifiedCompacts)); s.AppendLine("ARTICLES          " + Signed(epoch.KnowledgeArticles - previous.KnowledgeArticles)); s.AppendLine("PLURALITY         " + Signed(epoch.PluralityObjects - previous.PluralityObjects)); int changed = epoch.JurisdictionFingerprints.Count(kv => !previous.JurisdictionFingerprints.ContainsKey(kv.Key) || !String.Equals(previous.JurisdictionFingerprints[kv.Key], kv.Value, StringComparison.OrdinalIgnoreCase)); s.AppendLine("JURISDICTIONS Δ   " + changed.ToString(CultureInfo.InvariantCulture)); } Window w = W(owner, "YOMI · CENSUS EPOCH", 940, 700); TextBox box = ReadOnly(s.ToString()); box.Margin = new Thickness(18); w.Content = box; w.Show();
        }

        private static void OpenChronicle(Window owner, DeepSystemsContext ctx)
        {
            List<CivilChronicleRecord> rows = ReadChronicle(ctx, 2048); string verify = VerifyChronicle(rows); Window w = W(owner, "YOMI · CIVILIZATION CHRONICLE", 1120, 760); DockPanel root = Shell("CIVILIZATION CHRONICLE", verify + " · append-only SHA-256 institutional history"); ListBox list = List(); list.ItemsSource = rows.OrderByDescending(r => r.Sequence).ToList(); list.MouseDoubleClick += delegate { CivilChronicleRecord r = list.SelectedItem as CivilChronicleRecord; if (r != null) DeepSystems.OpenExternalValue(w, ctx, "CHRONICLE RECORD " + r.Sequence.ToString(CultureInfo.InvariantCulture), r, "CIVILIZATION › CHRONICLE"); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void CreateInstitutionPrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, string parentId)
        {
            string name = Prompt(owner, "FOUND INSTITUTION", "Institution name", parentId == null ? "New Institute" : "Child Office"); if (String.IsNullOrWhiteSpace(name)) return; string query = Prompt(owner, "JURISDICTION", "YQL jurisdiction", "STATS"); if (String.IsNullOrWhiteSpace(query)) return; string charter = PromptLarge(owner, "CHARTER", "What question / responsibility defines this institution?", "Investigate and organize this territory without claiming authority over it."); if (charter == null) return; CreateInstitution(owner, ctx, index, name.Trim(), query.Trim(), charter.Trim(), parentId);
        }

        private static void CreateInstitution(Window owner, DeepSystemsContext ctx, WorldIndex index, string name, string query, string charter, string parentId)
        {
            Load(ctx); if (Store.Institutions.Count >= MaxInstitutions) { MessageBox.Show(owner, "Institution cap reached (64).", "YOMI · Civilization"); return; } WorldQueryResult r = WorldKernel.ExternalQuery(index, query); if (r.Nodes.Count == 0 && !query.StartsWith("STATS", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(owner, "The jurisdiction currently resolves zero nodes. Institution not founded.", "YOMI · Civilization"); return; }
            CivilInstitution inst = new CivilInstitution { Id = Id("inst"), Name = name, JurisdictionQuery = query, Charter = charter, ParentInstitutionId = parentId, Status = "ACTIVE", CharterRevision = 1, CreatedUtc = Now(), UpdatedUtc = Now(), BaselineNodes = Fingerprints(r.Nodes) }; inst.BaselineFingerprint = HashMap(inst.BaselineNodes); Store.Institutions.Add(inst); TrimStore(); Save(ctx); AppendChronicle(ctx, "INSTITUTION_FOUNDED", inst.Id, inst.Name + " · " + inst.JurisdictionQuery, inst.BaselineFingerprint); OpenInstitution(owner, ctx, index, inst);
        }

        private static void LaunchExpeditionPrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            WorldQueryResult r = WorldKernel.ExternalQuery(index, inst.JurisdictionQuery); WorldNode root = r.Nodes.OrderByDescending(n => WorldKernel.ExternalDegree(index, n.Id)).FirstOrDefault(); if (root == null) return; string name = Prompt(owner, "LAUNCH EXPEDITION", "Expedition name", inst.Name + " · Frontier Survey"); if (String.IsNullOrWhiteSpace(name)) return; LaunchExpedition(owner, ctx, index, inst, name.Trim(), root.Id);
        }

        private static void LaunchExpedition(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst, string name, string rootId)
        {
            if (Store.Expeditions.Count >= MaxExpeditions) Store.Expeditions = Store.Expeditions.OrderByDescending(e => e.UpdatedUtc ?? e.CreatedUtc).Take(MaxExpeditions - 1).ToList(); CivilExpedition exp = new CivilExpedition { Id = Id("exp"), InstitutionId = inst == null ? null : inst.Id, Name = name, RootNodeId = rootId, CurrentNodeId = rootId, Status = "ACTIVE", CreatedUtc = Now(), UpdatedUtc = Now() }; exp.VisitedNodeIds.Add(rootId); SurveyFrontier(index, exp); Store.Expeditions.Add(exp); Save(ctx); AppendChronicle(ctx, "EXPEDITION_LAUNCH", exp.Id, exp.Name + " · " + rootId); OpenExpedition(owner, ctx, index, exp);
        }

        private static void AdvanceExpedition(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilExpedition exp)
        {
            SurveyFrontier(index, exp); WorldNode next = exp.FrontierNodeIds.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).OrderByDescending(n => ExpeditionScore(index, exp, n)).FirstOrDefault(); if (next == null) { MessageBox.Show(owner, "No unexplored frontier remains from the current route.", "YOMI · Expedition"); return; } exp.CurrentNodeId = next.Id; if (!exp.VisitedNodeIds.Contains(next.Id, StringComparer.OrdinalIgnoreCase)) exp.VisitedNodeIds.Add(next.Id); SurveyFrontier(index, exp); exp.UpdatedUtc = Now(); Save(ctx); AppendChronicle(ctx, "EXPEDITION_ADVANCE", exp.Id, next.Id); OpenExpedition(owner, ctx, index, exp);
        }

        private static void SurveyFrontier(WorldIndex index, CivilExpedition exp)
        {
            HashSet<string> visited = new HashSet<string>(exp.VisitedNodeIds, StringComparer.OrdinalIgnoreCase); HashSet<string> frontier = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string id in TakeLastLocal(exp.VisitedNodeIds, 64)) foreach (WorldNode n in WorldKernel.ExternalNeighbors(index, id)) if (!visited.Contains(n.Id)) frontier.Add(n.Id); exp.FrontierNodeIds = frontier.Where(id => index.Nodes.ContainsKey(id)).Select(id => index.Nodes[id]).OrderByDescending(n => ExpeditionScore(index, exp, n)).Take(256).Select(n => n.Id).ToList();
        }

        private static void CreateSentinelPrompt(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilInstitution inst)
        {
            string name = Prompt(owner, "NEW SENTINEL", "Sentinel name", inst.Name + " · Drift Watch"); if (String.IsNullOrWhiteSpace(name)) return; string q = Prompt(owner, "SENTINEL QUERY", "YQL query to watch", inst.JurisdictionQuery); if (String.IsNullOrWhiteSpace(q)) return; CivilSentinel s = new CivilSentinel { Id = Id("sent"), InstitutionId = inst.Id, Name = name.Trim(), Query = q.Trim(), CreatedUtc = Now() }; s.LastNodes = Fingerprints(WorldKernel.ExternalQuery(index, s.Query).Nodes); s.LastFingerprint = HashMap(s.LastNodes); s.LastScanUtc = Now(); Store.Sentinels.Add(s); TrimStore(); Save(ctx); AppendChronicle(ctx, "SENTINEL_CREATED", s.Id, s.Name + " · " + s.Query); OpenSentinel(owner, ctx, index, s);
        }

        private static int ScanSentinel(DeepSystemsContext ctx, WorldIndex index, CivilSentinel s)
        {
            Dictionary<string, string> now = Fingerprints(WorldKernel.ExternalQuery(index, s.Query).Nodes); Drift d = Compare(s.LastNodes, now); int count = 0; if (d.Total > 0)
            {
                CivilAlert a = new CivilAlert { Id = Id("alert"), SentinelId = s.Id, InstitutionId = s.InstitutionId, CreatedUtc = Now(), Severity = d.Changed.Count > 0 ? "WATCH" : "INFO", Summary = s.Name + " · jurisdiction drift", Detail = "+" + d.Added.Count.ToString(CultureInfo.InvariantCulture) + " added / -" + d.Removed.Count.ToString(CultureInfo.InvariantCulture) + " removed / ~" + d.Changed.Count.ToString(CultureInfo.InvariantCulture) + " changed", Status = "OPEN" };
                a.NodeIds = d.Added.Concat(d.Removed).Concat(d.Changed).Distinct(StringComparer.OrdinalIgnoreCase).Take(160).ToList(); Store.Alerts.Insert(0, a); count++; AppendChronicle(ctx, "SENTINEL_ALERT", a.Id, a.Summary + " · " + a.Detail);
            }
            s.LastNodes = now; s.LastFingerprint = HashMap(now); s.LastScanUtc = Now(); TrimStore(); return count;
        }

        private static string DescribeInstitution(DeepSystemsContext ctx, WorldIndex index, CivilInstitution i, WorldQueryResult r, Drift d)
        {
            StringBuilder s = new StringBuilder(); s.AppendLine("INSTITUTION ID      " + i.Id); s.AppendLine("STATUS              " + (i.Status ?? "ACTIVE")); s.AppendLine("PARENT              " + (i.ParentInstitutionId ?? "∅")); s.AppendLine("CHARTER REVISION    " + Math.Max(1, i.CharterRevision)); s.AppendLine("AMENDMENTS          " + i.Amendments.Count); s.AppendLine("CREATED             " + i.CreatedUtc); s.AppendLine("UPDATED             " + i.UpdatedUtc); s.AppendLine("BASELINE            " + (i.BaselineFingerprint ?? "∅")); s.AppendLine("BASELINE NODES      " + i.BaselineNodes.Count); s.AppendLine("CURRENT NODES       " + r.Nodes.Count); s.AppendLine("DRIFT               +" + d.Added.Count + " / -" + d.Removed.Count + " / ~" + d.Changed.Count); s.AppendLine("CHILD INSTITUTIONS  " + Store.Institutions.Count(x => x.ParentInstitutionId == i.Id)); s.AppendLine("SENTINELS           " + Store.Sentinels.Count(x => x.InstitutionId == i.Id)); s.AppendLine("OPEN ALERTS         " + Store.Alerts.Count(x => x.InstitutionId == i.Id && x.Status != "REVIEWED")); s.AppendLine("OPEN RESOLUTIONS    " + Store.Resolutions.Count(x => x.InstitutionId == i.Id && (x.Status == "DRAFT" || x.Status == "DELIBERATING"))); s.AppendLine("ACTIVE EXPEDITIONS  " + Store.Expeditions.Count(x => x.InstitutionId == i.Id && x.Status == "ACTIVE")); s.AppendLine(); s.AppendLine("CHARTER"); s.AppendLine(i.Charter ?? "∅"); return s.ToString();
        }

        private static string CensusLine(WorldIndex index)
        {
            return ActiveInstitutions().Count.ToString(CultureInfo.InvariantCulture) + " active institutions · " + Store.Expeditions.Count(e => e.Status == "ACTIVE").ToString(CultureInfo.InvariantCulture) + " active expeditions · " + Store.Sentinels.Count.ToString(CultureInfo.InvariantCulture) + " sentinels · " + Store.Alerts.Count(a => a.Status != "REVIEWED").ToString(CultureInfo.InvariantCulture) + " alerts · " + Store.Resolutions.Count(r => r.Status == "DRAFT" || r.Status == "DELIBERATING").ToString(CultureInfo.InvariantCulture) + " open resolutions · world " + index.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes / " + index.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges";
        }

        private static int FrontierScore(WorldIndex index, WorldNode n) { return WorldKernel.ExternalDegree(index, n.Id) * 8 + n.IdentityTokens.Count * 5 + Math.Min(40, n.Properties.Count); }
        private static int ExpeditionScore(WorldIndex index, CivilExpedition exp, WorldNode n)
        {
            HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string id in exp.VisitedNodeIds) { WorldNode x; if (index.Nodes.TryGetValue(id, out x)) foreach (string t in x.IdentityTokens) known.Add(t); } int novelty = n.IdentityTokens.Count(t => !known.Contains(t)); return WorldKernel.ExternalDegree(index, n.Id) * 5 + novelty * 17 + n.Properties.Count;
        }

        private static CivilInstitution Institution(string id) { return Store.Institutions.FirstOrDefault(i => String.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)); }
        private static List<CivilInstitution> ActiveInstitutions() { return Store.Institutions.Where(i => !String.Equals(i.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase)).ToList(); }
        private static Dictionary<string, string> Fingerprints(IEnumerable<WorldNode> nodes) { return nodes.Take(4096).ToDictionary(n => n.Id, n => n.Fingerprint ?? "", StringComparer.OrdinalIgnoreCase); }
        private static Drift Compare(Dictionary<string, string> before, Dictionary<string, string> after)
        {
            before = before ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); after = after ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); Drift d = new Drift(); foreach (string id in after.Keys) { if (!before.ContainsKey(id)) d.Added.Add(id); else if (!String.Equals(before[id] ?? "", after[id] ?? "", StringComparison.OrdinalIgnoreCase)) d.Changed.Add(id); } foreach (string id in before.Keys) if (!after.ContainsKey(id)) d.Removed.Add(id); return d;
        }

        private static string HashMap(Dictionary<string, string> map)
        {
            StringBuilder s = new StringBuilder(); foreach (KeyValuePair<string, string> kv in (map ?? new Dictionary<string, string>()).OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)) s.Append(kv.Key).Append('=').Append(kv.Value).Append('\n'); return Sha(s.ToString());
        }

        private static string HashStrings(IEnumerable<string> values) { return Sha(String.Join("\n", (values ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray())); }
        private static string HashEdges(IEnumerable<WorldEdge> edges) { return Sha(String.Join("\n", (edges ?? Enumerable.Empty<WorldEdge>()).OrderBy(e => e.Id ?? "", StringComparer.OrdinalIgnoreCase).Select(e => (e.Id ?? "") + "|" + (e.FromId ?? "") + "|" + (e.ToId ?? "") + "|" + (e.Kind ?? "") + "|" + (e.EvidenceClass ?? "") + "|" + e.Strength.ToString(CultureInfo.InvariantCulture)).ToArray())); }
        private static string Signed(int n) { return n > 0 ? "+" + n.ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture); }

        private static string FirstIdentityValue(WorldNode n)
        {
            if (n == null || n.IdentityTokens == null || n.IdentityTokens.Count == 0) return ""; string t = n.IdentityTokens[0] ?? ""; int at = t.IndexOf('='); return at >= 0 ? t.Substring(at + 1).Trim() : t;
        }

        private static void Load(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                if (Store != null && String.Equals(LoadedRoot, ctx.DataRoot, StringComparison.OrdinalIgnoreCase)) return; LoadedRoot = ctx.DataRoot; Store = new CivilizationStore(); string p = StorePath(ctx); try { if (File.Exists(p)) { CivilizationStore s = Json.Deserialize<CivilizationStore>(File.ReadAllText(p, Encoding.UTF8)); if (s != null && s.Schema <= 2) Store = s; else throw new InvalidDataException("Unsupported Civilization schema."); } } catch { PreserveCorrupt(p); Store = LoadPreviousStore(p) ?? new CivilizationStore(); } NormalizeStore();
            }
        }

        private static CivilizationStore LoadPreviousStore(string path)
        {
            try { string previous = path + ".previous"; if (!File.Exists(previous)) return null; CivilizationStore s = Json.Deserialize<CivilizationStore>(File.ReadAllText(previous, Encoding.UTF8)); return s != null && s.Schema <= 2 ? s : null; } catch { return null; }
        }

        private static void NormalizeStore()
        {
            if (Store == null) Store = new CivilizationStore(); Store.Schema = 2; if (Store.Institutions == null) Store.Institutions = new List<CivilInstitution>(); if (Store.Expeditions == null) Store.Expeditions = new List<CivilExpedition>(); if (Store.Sentinels == null) Store.Sentinels = new List<CivilSentinel>(); if (Store.Alerts == null) Store.Alerts = new List<CivilAlert>(); if (Store.Resolutions == null) Store.Resolutions = new List<CivilResolution>(); if (Store.Compacts == null) Store.Compacts = new List<CivilCompact>(); if (Store.CensusEpochs == null) Store.CensusEpochs = new List<CivilCensusEpoch>(); if (Store.KnowledgeArticles == null) Store.KnowledgeArticles = new List<CivilKnowledgeArticle>(); foreach (CivilInstitution i in Store.Institutions) { if (i.BaselineNodes == null) i.BaselineNodes = new Dictionary<string, string>(); if (i.TrackedIdentities == null) i.TrackedIdentities = new List<string>(); if (i.Amendments == null) i.Amendments = new List<CivilAmendment>(); if (String.IsNullOrWhiteSpace(i.Status)) i.Status = "ACTIVE"; if (i.CharterRevision < 1) i.CharterRevision = Math.Max(1, i.Amendments.Count + 1); } foreach (CivilSentinel s in Store.Sentinels) if (s.LastNodes == null) s.LastNodes = new Dictionary<string, string>(); foreach (CivilExpedition e in Store.Expeditions) { if (e.VisitedNodeIds == null) e.VisitedNodeIds = new List<string>(); if (e.FrontierNodeIds == null) e.FrontierNodeIds = new List<string>(); if (e.Findings == null) e.Findings = new List<string>(); } foreach (CivilAlert a in Store.Alerts) if (a.NodeIds == null) a.NodeIds = new List<string>(); foreach (CivilResolution r in Store.Resolutions) { if (r.EvidenceNodeIds == null) r.EvidenceNodeIds = new List<string>(); if (r.Deliberation == null) r.Deliberation = new List<string>(); if (String.IsNullOrWhiteSpace(r.Status)) r.Status = "DRAFT"; if (String.IsNullOrWhiteSpace(r.EpistemicClass)) r.EpistemicClass = "QUESTION"; } foreach (CivilCompact c in Store.Compacts) { if (c.InstitutionIds == null) c.InstitutionIds = new List<string>(); if (c.EvidenceNodeIds == null) c.EvidenceNodeIds = new List<string>(); if (String.IsNullOrWhiteSpace(c.Status)) c.Status = "PROPOSED"; } foreach (CivilCensusEpoch e in Store.CensusEpochs) if (e.JurisdictionFingerprints == null) e.JurisdictionFingerprints = new Dictionary<string, string>(); foreach (CivilKnowledgeArticle a in Store.KnowledgeArticles) { if (a.Tags == null) a.Tags = new List<string>(); if (a.Revisions == null) a.Revisions = new List<CivilKnowledgeRevision>(); if (String.IsNullOrWhiteSpace(a.Status)) a.Status = "ACTIVE"; if (String.IsNullOrWhiteSpace(a.EpistemicClass)) a.EpistemicClass = "INTERPRETATION"; if (String.IsNullOrWhiteSpace(a.BodyHash)) a.BodyHash = KnowledgeHash(a.Title, a.Body, a.EpistemicClass); } TrimStore();
        }

        private static void TrimStore()
        {
            Store.Institutions = Store.Institutions.Take(MaxInstitutions).ToList(); Store.Expeditions = Store.Expeditions.OrderByDescending(e => e.UpdatedUtc ?? e.CreatedUtc).Take(MaxExpeditions).ToList(); Store.Sentinels = Store.Sentinels.Take(MaxSentinels).ToList(); Store.Alerts = Store.Alerts.OrderByDescending(a => a.CreatedUtc).Take(MaxAlerts).ToList(); Store.Resolutions = Store.Resolutions.OrderByDescending(r => r.UpdatedUtc ?? r.CreatedUtc).Take(MaxResolutions).ToList(); Store.Compacts = Store.Compacts.OrderByDescending(c => c.UpdatedUtc ?? c.CreatedUtc).Take(MaxCompacts).ToList(); Store.CensusEpochs = Store.CensusEpochs.OrderByDescending(e => e.CapturedUtc).Take(MaxCensusEpochs).ToList(); Store.KnowledgeArticles = Store.KnowledgeArticles.OrderByDescending(a => a.UpdatedUtc ?? a.CreatedUtc).Take(MaxKnowledgeArticles).ToList(); foreach (CivilInstitution i in Store.Institutions) i.Amendments = i.Amendments.OrderByDescending(a => a.CreatedUtc).Take(MaxAmendmentsPerInstitution).ToList(); foreach (CivilKnowledgeArticle a in Store.KnowledgeArticles) a.Revisions = a.Revisions.OrderByDescending(r => r.CreatedUtc).Take(MaxKnowledgeRevisionsPerArticle).ToList();
        }

        private static void Save(DeepSystemsContext ctx)
        {
            lock (Gate) { NormalizeStore(); Store.Revision = Math.Max(0, Store.Revision) + 1; Store.UpdatedUtc = Now(); Store.LastCommitId = Id("civil-commit"); string p = StorePath(ctx); Directory.CreateDirectory(Path.GetDirectoryName(p)); AtomicWrite(p, Json.Serialize(Store)); }
        }

        private static void AtomicWrite(string p, string text)
        {
            string tmp = p + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text ?? "", new UTF8Encoding(false)); if (File.Exists(p)) { string prev = p + ".previous"; try { File.Replace(tmp, p, prev, true); return; } catch { try { File.Copy(p, prev, true); } catch { } } } if (File.Exists(p)) File.Delete(p); File.Move(tmp, p);
        }

        private static void PreserveCorrupt(string p)
        {
            try { if (!File.Exists(p)) return; byte[] b = File.ReadAllBytes(p); string q = p + ".corrupt." + ShaBytes(b).Substring(0, 16) + ".json"; if (!File.Exists(q)) File.Copy(p, q); } catch { }
        }

        private static IEnumerable<T> TakeLastLocal<T>(IEnumerable<T> source, int count)
        {
            if (source == null) yield break; Queue<T> q = new Queue<T>(); foreach (T item in source) { q.Enqueue(item); if (q.Count > count) q.Dequeue(); } foreach (T item in q) yield return item;
        }

        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-civilization.json"); }
        private static string ChroniclePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-civilization-chronicle.jsonl"); }

        private static void AppendChronicle(DeepSystemsContext ctx, string evt, string subject, string detail)
        {
            AppendChronicle(ctx, evt, subject, detail, "");
        }

        private static void AppendChronicle(DeepSystemsContext ctx, string evt, string subject, string detail, string evidenceHash)
        {
            lock (Gate) try
            {
                Directory.CreateDirectory(ctx.StateRoot); List<CivilChronicleRecord> prior = ReadChronicle(ctx, 4096); CivilChronicleRecord last = prior.OrderByDescending(q => q.Sequence).FirstOrDefault(); long seq = last == null ? 1 : last.Sequence + 1; string prev = last == null ? new string('0', 64) : last.EntryHash; CivilChronicleRecord r = new CivilChronicleRecord { Schema = 2, Sequence = seq, Utc = Now(), Event = evt, Subject = subject, Detail = detail, CorrelationId = Store == null ? null : Store.LastCommitId, EvidenceHash = evidenceHash ?? "", PrevHash = prev }; r.EntryHash = ChronicleHash(r); byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(r) + Environment.NewLine); using (FileStream stream = new FileStream(ChroniclePath(ctx), FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(); }
            }
            catch { }
        }

        private static List<CivilChronicleRecord> ReadChronicle(DeepSystemsContext ctx, int cap)
        {
            List<CivilChronicleRecord> rows = new List<CivilChronicleRecord>(); string p = ChroniclePath(ctx); if (!File.Exists(p)) return rows; try { foreach (string line in TakeLastLocal(File.ReadLines(p).Where(x => !String.IsNullOrWhiteSpace(x)), cap)) { try { CivilChronicleRecord r = Json.Deserialize<CivilChronicleRecord>(line); if (r != null) rows.Add(r); } catch { } } } catch { } return rows;
        }

        private static string VerifyChronicle(List<CivilChronicleRecord> rows)
        {
            if (rows == null || rows.Count == 0) return "EMPTY CHRONICLE"; rows = rows.OrderBy(r => r.Sequence).ToList(); string prev = rows[0].PrevHash; long expected = rows[0].Sequence; foreach (CivilChronicleRecord r in rows) { if (r.Sequence != expected) return "CHAIN GAP @ " + expected.ToString(CultureInfo.InvariantCulture); if (!String.Equals(r.PrevHash ?? "", prev ?? "", StringComparison.OrdinalIgnoreCase)) return "PREV HASH MISMATCH @ " + r.Sequence; if (!String.Equals(r.EntryHash ?? "", ChronicleHash(r), StringComparison.OrdinalIgnoreCase)) return "ENTRY HASH MISMATCH @ " + r.Sequence; prev = r.EntryHash; expected++; } return "CHAIN VERIFIED · " + rows.Count.ToString(CultureInfo.InvariantCulture) + " RECORDS";
        }

        private static string ChronicleHash(CivilChronicleRecord r) { if (r.Schema >= 2) return Sha(r.Schema.ToString(CultureInfo.InvariantCulture) + "|" + r.Sequence.ToString(CultureInfo.InvariantCulture) + "|" + (r.Utc ?? "") + "|" + (r.Event ?? "") + "|" + (r.Subject ?? "") + "|" + (r.Detail ?? "") + "|" + (r.CorrelationId ?? "") + "|" + (r.EvidenceHash ?? "") + "|" + (r.PrevHash ?? "")); return Sha(r.Sequence.ToString(CultureInfo.InvariantCulture) + "|" + (r.Utc ?? "") + "|" + (r.Event ?? "") + "|" + (r.Subject ?? "") + "|" + (r.Detail ?? "") + "|" + (r.PrevHash ?? "")); }
        private static string Sha(string s) { return ShaBytes(Encoding.UTF8.GetBytes(s ?? "")); }
        private static string ShaBytes(byte[] b) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(b ?? new byte[0])).Replace("-", "").ToLowerInvariant(); }
        private static string Id(string prefix) { return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8); }
        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static string Trunc(string s, int n) { s = s ?? ""; return s.Length <= n ? s : s.Substring(0, n - 1) + "…"; }

        private static Window W(Window owner, string title, double width, double height) { Window w = new Window { Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 820), MinHeight = Math.Min(height, 560), Background = Bg, Foreground = Text, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner, FontFamily = new FontFamily("Segoe UI") }; return w; }
        private static DockPanel Shell(string title, string subtitle) { DockPanel root = new DockPanel { Margin = new Thickness(16) }; StackPanel h = new StackPanel(); h.Children.Add(T(title, 24, Text, FontWeights.Bold)); h.Children.Add(T(subtitle, 12, Muted, FontWeights.Normal)); h.Margin = new Thickness(0, 0, 0, 10); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); return root; }
        private static TextBlock T(string text, double size, Brush fg, FontWeight weight) { return new TextBlock { Text = text ?? "", FontSize = size, Foreground = fg, FontWeight = weight, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) }; }
        private static Button Btn(string text, RoutedEventHandler click) { Button b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(12, 7, 12, 7), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), MinWidth = 112 }; if (click != null) b.Click += click; return b; }
        private static ListBox List() { return new ListBox { Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(0, 6, 0, 0), Padding = new Thickness(5), FontFamily = new FontFamily("Cascadia Mono, Consolas") }; }
        private static TextBox ReadOnly(string text) { return new TextBox { Text = text ?? "", IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(0, 6, 6, 0), FontFamily = new FontFamily("Cascadia Mono, Consolas") }; }
        private static void Portal(Panel p, string badge, string title, string desc, Action open) { Button b = new Button { Width = 350, Height = 116, Margin = new Thickness(0, 0, 10, 10), Padding = new Thickness(12), Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), HorizontalContentAlignment = HorizontalAlignment.Stretch }; StackPanel s = new StackPanel(); s.Children.Add(T(badge + "   " + title, 14, Accent, FontWeights.Bold)); s.Children.Add(T(desc, 11.5, Muted, FontWeights.Normal)); b.Content = s; b.Click += delegate { if (open != null) open(); }; p.Children.Add(b); }
        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static string Prompt(Window owner, string title, string label, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 620, 260); DockPanel root = new DockPanel { Margin = new Thickness(16) }; TextBlock l = T(label, 12, Muted, FontWeights.Normal); DockPanel.SetDock(l, Dock.Top); root.Children.Add(l); TextBox tb = new TextBox { Text = initial ?? "", MaxLength = MaxShortText, Margin = new Thickness(0, 8, 0, 10), Background = Surface, Foreground = Text, BorderBrush = Border, Padding = new Thickness(8) }; DockPanel.SetDock(tb, Dock.Top); root.Children.Add(tb); WrapPanel buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; string result = null; Button ok = Btn("OK", delegate { result = tb.Text; w.DialogResult = true; }); Button cancel = Btn("CANCEL", delegate { w.DialogResult = false; }); buttons.Children.Add(ok); buttons.Children.Add(cancel); root.Children.Add(buttons); w.Content = root; bool? yes = w.ShowDialog(); return yes == true ? result : null;
        }

        private static string PromptLarge(Window owner, string title, string label, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 720, 480); DockPanel root = new DockPanel { Margin = new Thickness(16) }; TextBlock l = T(label, 12, Muted, FontWeights.Normal); DockPanel.SetDock(l, Dock.Top); root.Children.Add(l); TextBox editor = new TextBox { Text = initial ?? "", MaxLength = MaxLongText, AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 8, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10), FontFamily = new FontFamily("Cascadia Mono, Consolas") }; WrapPanel buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) }; string result = null; Button ok = Btn("SAVE", delegate { result = editor.Text; w.DialogResult = true; }); Button cancel = Btn("CANCEL", delegate { w.DialogResult = false; }); buttons.Children.Add(ok); buttons.Children.Add(cancel); DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons); root.Children.Add(editor); w.Content = root; bool? yes = w.ShowDialog(); return yes == true ? result : null;
        }

        private static string Choose(Window owner, string title, string label, IEnumerable<string> choices, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 620, 270); DockPanel root = new DockPanel { Margin = new Thickness(16) }; TextBlock l = T(label, 12, Muted, FontWeights.Normal); DockPanel.SetDock(l, Dock.Top); root.Children.Add(l); ComboBox combo = new ComboBox { ItemsSource = (choices ?? Enumerable.Empty<string>()).ToList(), Margin = new Thickness(0, 10, 0, 12), Background = Surface, Foreground = Text, BorderBrush = Border, Padding = new Thickness(8), IsEditable = false }; combo.SelectedItem = combo.Items.Cast<object>().FirstOrDefault(x => String.Equals(Convert.ToString(x, CultureInfo.InvariantCulture), initial, StringComparison.OrdinalIgnoreCase)); if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0; DockPanel.SetDock(combo, Dock.Top); root.Children.Add(combo); WrapPanel buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right }; string result = null; Button ok = Btn("SELECT", delegate { result = Convert.ToString(combo.SelectedItem, CultureInfo.InvariantCulture); w.DialogResult = true; }); Button cancel = Btn("CANCEL", delegate { w.DialogResult = false; }); buttons.Children.Add(ok); buttons.Children.Add(cancel); root.Children.Add(buttons); w.Content = root; bool? yes = w.ShowDialog(); return yes == true ? result : null;
        }

        private sealed class Drift { public List<string> Added = new List<string>(); public List<string> Removed = new List<string>(); public List<string> Changed = new List<string>(); public int Total { get { return Added.Count + Removed.Count + Changed.Count; } } }
        private sealed class TreatyRow { public CivilInstitution A; public CivilInstitution B; public int Count; public List<WorldEdge> Edges; public override string ToString() { return A.Name + " ↔ " + B.Name + "   ·   " + Count.ToString(CultureInfo.InvariantCulture) + " bridges"; } }
        private sealed class BorderRow { public CivilInstitution A; public CivilInstitution B; public int ACount; public int BCount; public int SharedCount; public double Jaccard; public string Relation; public List<string> SharedNodeIds; public override string ToString() { return (Relation ?? "BORDER") + "   ·   " + A.Name + " / " + B.Name + "   ·   shared " + SharedCount.ToString(CultureInfo.InvariantCulture) + "   ·   J " + Jaccard.ToString("0.000", CultureInfo.InvariantCulture); } }
        private sealed class ConstellationRow { public List<string> InstitutionIds; public int BridgePairs; public int BridgeEdges; public string HubInstitutionId; public override string ToString() { return InstitutionIds.Count.ToString(CultureInfo.InvariantCulture) + " institutions   ·   " + BridgePairs.ToString(CultureInfo.InvariantCulture) + " treaty pairs   ·   " + BridgeEdges.ToString(CultureInfo.InvariantCulture) + " bridge edges"; } }
        private sealed class GenealogyRow { public CivilInstitution Institution; public int Depth; public bool Loop; public override string ToString() { return new string(' ', Math.Min(32, Math.Max(0, Depth)) * 2) + (Loop ? "↻ " : "└ ") + (Institution == null ? "∅" : Institution.Name) + "   ·   " + (Institution == null ? "" : Institution.Status) + "   ·   r" + (Institution == null ? "0" : Math.Max(1, Institution.CharterRevision).ToString(CultureInfo.InvariantCulture)); } }
        private sealed class PluralityRow { public string NodeId; public List<CivilKnowledgeArticle> Articles; public int DistinctInterpretations; public int DistinctInstitutions; public override string ToString() { return DistinctInstitutions.ToString(CultureInfo.InvariantCulture) + " scopes   ·   " + DistinctInterpretations.ToString(CultureInfo.InvariantCulture) + " interpretations   ·   " + Articles.Count.ToString(CultureInfo.InvariantCulture) + " articles   ·   " + (NodeId ?? "∅"); } }
        private sealed class CivilAuditFinding { public string Severity; public string Code; public string SubjectId; public string Detail; public string NodeId; public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Code ?? "AUDIT") + "   ·   " + (SubjectId ?? "∅") + "   ·   " + (Detail ?? ""); } }
    }

}

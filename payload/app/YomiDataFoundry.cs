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
using Microsoft.Win32;

namespace Yomi.ProductShell
{
    internal sealed class DataFoundryLedgerRecord
    {
        public int Schema { get; set; }
        public long Sequence { get; set; }
        public string Utc { get; set; }
        public string Event { get; set; }
        public string Subject { get; set; }
        public string Detail { get; set; }
        public string CorrelationId { get; set; }
        public string DatasetFingerprint { get; set; }
        public string CertificateHash { get; set; }
        public string PrevHash { get; set; }
        public string EntryHash { get; set; }
        public override string ToString() { return Sequence.ToString("000000", CultureInfo.InvariantCulture) + "   ·   " + (Utc ?? "") + "   ·   " + (Event ?? "EVENT") + "   ·   " + (Subject ?? "") + "   ·   " + DataFoundryKernel.Short(EntryHash); }
    }

    internal sealed class DataFoundryStore
    {
        public int Schema { get; set; }
        public int Revision { get; set; }
        public string UpdatedUtc { get; set; }
        public string LastCommitId { get; set; }
        public List<DataDataset> Datasets { get; set; }
        public List<DataRelation> Relations { get; set; }
        public List<DataCorrelation> Correlations { get; set; }
        public List<DataLineageNode> Lineage { get; set; }
        public List<DataRecipe> Recipes { get; set; }
        public List<DataSchemaContract> Contracts { get; set; }
        public List<DataContractResult> ContractResults { get; set; }
        public List<DataDriftReport> Drifts { get; set; }
        public List<DataProjectionReceipt> Projections { get; set; }
        public List<DataBaseline> Baselines { get; set; }
        public List<DataCapsule> Capsules { get; set; }
        public List<DataDossier> Dossiers { get; set; }
        public DataFoundryStore()
        {
            Schema = 1; Datasets = new List<DataDataset>(); Relations = new List<DataRelation>(); Correlations = new List<DataCorrelation>(); Lineage = new List<DataLineageNode>(); Recipes = new List<DataRecipe>(); Contracts = new List<DataSchemaContract>(); ContractResults = new List<DataContractResult>(); Drifts = new List<DataDriftReport>(); Projections = new List<DataProjectionReceipt>(); Baselines = new List<DataBaseline>(); Capsules = new List<DataCapsule>(); Dossiers = new List<DataDossier>();
        }
    }

    internal static class DataFoundry
    {
        private const int MaxDatasets = 512;
        private const int MaxArtifacts = 4096;
        private const int MaxLedgerRead = 65536;
        private static readonly object Gate = new object();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly Brush Bg = B("#0C1015"), Surface = B("#131B24"), Raised = B("#1B2835"), Border = B("#31485D"), Text = B("#EEF7FF"), Muted = B("#99AEC0"), Accent = B("#62D9FF"), Green = B("#63E6A2"), Amber = B("#FFD166"), Danger = B("#FF7185"), Violet = B("#B792FF");
        private static DataFoundryStore Store;
        private static string LoadedRoot;

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            Ensure(ctx);
            if (Store.Datasets.Count == 0) SeedStarter(ctx);
            Window w = W(owner, "YOMI · DEV13.37.24 · DATA FOUNDRY", 1500, 940);
            DockPanel root = Shell("DATA FOUNDRY", "Local-first data civilization · streaming profiles · provenance · field semantics · quality · relations · lineage · transformation recipes · contracts · drift · evidence. Source files are immutable inputs; derived materialization is confined to YOMI-owned state unless a later civilization explicitly expands authority.");
            TextBlock status = new TextBlock { Text = "DATAΩ   ·   DEV13.37.24   ·   " + Store.Datasets.Count.ToString(CultureInfo.InvariantCulture) + " datasets   ·   " + Store.Relations.Count.ToString(CultureInfo.InvariantCulture) + " relations   ·   " + Store.Recipes.Count.ToString(CultureInfo.InvariantCulture) + " recipes   ·   " + Store.Contracts.Count.ToString(CultureInfo.InvariantCulture) + " contracts   ·   Ctrl+Alt+D", Foreground = Accent, FontWeight = FontWeights.Bold, Margin = new Thickness(20, 0, 20, 14) };
            DockPanel.SetDock(status, Dock.Top); root.Children.Add(status);
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            WrapPanel chambers = new WrapPanel { Margin = new Thickness(12) };
            Portal(chambers, "REG", "DATASET REGISTRY", "Open persistent datasets, fingerprints, source origins and linked analytical rooms.", delegate { OpenDatasets(w, ctx); });
            Portal(chambers, "IMPORT", "IMPORT BAY", "Explicitly select a local CSV, TSV or JSON Lines file. Stream it; retain bounded samples and statistics rather than the full table in controller state.", delegate { OpenImportBay(w, ctx); });
            Portal(chambers, "ORIGIN", "SOURCE PROVENANCE", "Origin kind, locator, byte length, modification time, SHA-256, projection parent and transformation ancestry.", delegate { OpenOrigins(w); });
            Portal(chambers, "SCHEMA", "SCHEMA OBSERVATORY", "All fields, inferred scalar types, ordinals, key candidates and explanations derived from observed values.", delegate { OpenFieldInventory(w, "SCHEMA OBSERVATORY", delegate(DataDataset d) { return d.Fields.Cast<object>().ToList(); }); });
            Portal(chambers, "FIELD", "FIELD MICROSCOPE", "Descend into nullness, distinctness, examples, type evidence, ranges, means, variance and quality flags.", delegate { OpenFieldMicroscope(w); });
            Portal(chambers, "ROW", "ROW SAMPLE DECK", "Deterministic bounded samples from across large sources with stable row ranks and anomaly flags.", delegate { OpenFieldInventory(w, "ROW SAMPLE DECK", delegate(DataDataset d) { return d.Samples.Cast<object>().ToList(); }); });
            Portal(chambers, "CHUNK", "CHUNK TELEMETRY", "Streaming chunk boundaries, bytes, malformed rows and profiling throughput without retaining every row.", delegate { OpenFieldInventory(w, "CHUNK TELEMETRY", delegate(DataDataset d) { return d.Chunks.Cast<object>().ToList(); }); });
            Portal(chambers, "QUAL", "QUALITY TRIAGE", "Nullness, mixed types, malformed rows, blank rows, bounded-estimator warnings and dataset quality score.", delegate { OpenFieldInventory(w, "QUALITY TRIAGE", delegate(DataDataset d) { return d.Quality.Cast<object>().ToList(); }); });
            Portal(chambers, "NULL", "NULLNESS MAP", "Rank fields by missingness and expose where absence is structural rather than hiding it inside averages.", delegate { OpenNullness(w); });
            Portal(chambers, "TYPE", "TYPE INFERENCE LAB", "Inspect evidence behind INTEGER, NUMBER, BOOLEAN, DATETIME, TEXT, MIXED and NULL classifications.", delegate { OpenTypeInference(w); });
            Portal(chambers, "DIST", "DISTINCTNESS OBSERVATORY", "Approximate bounded distinctness, uniqueness ratios and candidate identifiers with cap disclosure.", delegate { OpenDistinctness(w); });
            Portal(chambers, "DISTR", "DISTRIBUTION LAB", "Numeric range, mean and standard deviation plus text-length distributions from the streaming profile.", delegate { OpenDistribution(w); });
            Portal(chambers, "CORR", "CORRELATION MATRIX", "Sample-bounded Pearson relationships for numeric fields, certificate-bound to the exact dataset fingerprint.", delegate { RunCorrelations(w, ctx); });
            Portal(chambers, "OUT", "OUTLIER RADAR", "Flag sample values beyond robust profile thresholds without pretending bounded samples are population truth.", delegate { OpenOutliers(w); });
            Portal(chambers, "DUP", "DUPLICATE LAB", "Inspect repeated sampled records and high-frequency values; distinguish sample evidence from full-population claims.", delegate { OpenDuplicates(w); });
            Portal(chambers, "KEY", "KEY CANDIDATE LAB", "Find fields with zero nulls and population-level uniqueness where the bounded distinct estimator remains exact.", delegate { OpenKeys(w); });
            Portal(chambers, "REL", "RELATIONSHIP FORGE", "Infer candidate joins across datasets from field-name/type compatibility and deterministic sample overlap.", delegate { RunRelations(w, ctx); });
            Portal(chambers, "JOIN", "JOIN CARDINALITY", "One-to-one, one-to-many and many-to-many hypotheses with explicit confidence and non-declarative status.", delegate { OpenCollection(w, "JOIN CARDINALITY", Store.Relations.Cast<object>().ToList(), "Relations are inferred candidates, never silently promoted to database constraints."); });
            Portal(chambers, "ENTITY", "ENTITY RESOLUTION LAB", "Compare candidate identity fields and overlapping sampled values while preserving ambiguity instead of forced merges.", delegate { OpenEntityResolution(w); });
            Portal(chambers, "LINEAGE", "LINEAGE DAG", "Source → projection → recipe → materialization → baseline ancestry with immutable parent fingerprints.", delegate { OpenCollection(w, "LINEAGE DAG", Store.Lineage.OrderByDescending(x => x.CreatedUtc).Cast<object>().ToList(), "Every derivation is represented as an explicit node; original sources are never rewritten."); });
            Portal(chambers, "RECIPE", "TRANSFORM RECIPE FORGE", "Build ordered TRIM, whitespace, case, null replacement, rename, drop, parse and filter steps without touching source bytes.", delegate { OpenRecipes(w, ctx); });
            Portal(chambers, "PREVIEW", "CLEANING PREVIEW", "Apply recipes only to retained samples first and inspect before/after/rejected evidence.", delegate { OpenRecipePreview(w); });
            Portal(chambers, "MATERIAL", "DERIVED MATERIALIZATION", "Explicitly materialize a CSV/TSV recipe into YOMI-owned derived storage; never overwrite or rename the origin.", delegate { OpenMaterialization(w, ctx); });
            Portal(chambers, "CONTRACT", "SCHEMA CONTRACTS", "Freeze field presence, compatible types, key expectations and bounded missingness guards against a source fingerprint.", delegate { OpenContracts(w, ctx); });
            Portal(chambers, "VALID", "VALIDATION GATE", "Evaluate contracts against any retained dataset and preserve pass/fail findings with certificate hashes.", delegate { RunContractValidation(w, ctx); });
            Portal(chambers, "DRIFT", "DRIFT RADAR", "Compare schema, type, missingness, numeric center and distinctness changes between dataset versions.", delegate { RunDrift(w, ctx); });
            Portal(chambers, "ARCH", "VERSION ARCHAEOLOGY", "Follow parent dataset IDs, source fingerprints, lineage nodes, baselines and historical derivations.", delegate { OpenVersionArchaeology(w); });
            Portal(chambers, "QUERY", "QUERY WORKBENCH", "Filter retained bounded samples by field/value and descend through matching rows without loading the full source.", delegate { OpenQueryWorkbench(w); });
            Portal(chambers, "PIVOT", "PIVOT / CUBE OBSERVATORY", "Group bounded samples by dimensions and measures to generate exploratory summaries with sampling caveats attached.", delegate { OpenPivot(w); });
            Portal(chambers, "NEXUS", "PROJECTION NEXUS", "Typed immutable projection receipts from every prior YOMI civilization, preserving source identity and fingerprint.", delegate { OpenCollection(w, "PROJECTION NEXUS", Store.Projections.Cast<object>().ToList(), "15 peer civilizations can project typed objects into bounded Data Foundry datasets without mutation."); });
            Portal(chambers, "EVID", "EVIDENCE TRACE", "Dataset fingerprint → field/profile evidence → relation/recipe/contract/drift artifacts → lineage and ledger records.", delegate { OpenEvidenceTrace(w); });
            Portal(chambers, "CAP", "PREREGISTRATION CAPSULES", "Freeze a data question, metrics, assumptions, bounds and exact dataset fingerprint before analysis.", delegate { OpenCapsules(w, ctx); });
            Portal(chambers, "BASE", "IMMUTABLE BASELINES", "Seal row/field counts, quality and dataset fingerprint for later drift and regression comparison.", delegate { OpenBaselines(w, ctx); });
            Portal(chambers, "DOS", "DECISION DOSSIERS", "Bind decisions to exact data fingerprints, evidence, caveats and retained artifacts rather than detached prose.", delegate { OpenDossiers(w, ctx); });
            Portal(chambers, "LEDGER", "TAMPER-EVIDENT FOUNDRY LEDGER", "Verify the append-only SHA-256 previous-hash chain for imports, projections, analyses and materializations.", delegate { OpenLedger(w, ctx); });
            Portal(chambers, "HADAL", "HADAL DATA DESCENT", "Dataset → origin → field → sample → quality → relation → recipe → derived child → contract → drift → evidence → ledger.", delegate { OpenHadal(w); });
            Portal(chambers, "GENOMEΩ", "SOFTWARE GENOME OBSERVATORY", "Project a retained dataset into codebase analysis as schema/data-contract context without pretending data is executable source.", delegate { DataDataset d = ChooseDataset(w); if (d != null) SoftwareGenomeObservatory.OpenFromDataDataset(w, ctx, d); });
            Portal(chambers, "DOCΩ", "LIVING DOCUMENT FACTORY", "Project a retained dataset into evidence-bound reports/specifications while preserving dataset fingerprint and lineage.", delegate { DataDataset d = ChooseDataset(w); if (d != null) LivingDocumentIntelligenceFactory.OpenFromDataDataset(w, ctx, d); });
            Portal(chambers, "EXPΩ", "EXPEDITION COMMAND", "Capture the retained dataset as a fingerprinted mission crossing with lineage and source identity retained.", delegate { DataDataset d = ChooseDataset(w); if (d != null) GrandUnifiedExpeditionCommand.OpenFromDataDataset(w, ctx, d); });
            Portal(chambers, "WORM", "RETURN WORMHOLES", "Jump back to every peer civilization while keeping Data Foundry state and source semantics intact.", delegate { OpenReturnWormholes(w, ctx); });
            scroll.Content = chambers; root.Children.Add(scroll); w.Content = root; w.Show();
        }

        public static void OpenFromWorldNode(Window owner, DeepSystemsContext ctx, WorldIndex index, string nodeId) { if (ctx == null || index == null || String.IsNullOrWhiteSpace(nodeId)) return; WorldNode source; if (!index.Nodes.TryGetValue(nodeId, out source)) { Open(owner, ctx); return; } ProjectTyped(owner, ctx, "WORLD", source, nodeId, "World Passport object projected as a one-row evidence dataset."); }
        public static void OpenFromCivilResolution(Window owner, DeepSystemsContext ctx, WorldIndex index, CivilResolution source) { ProjectTyped(owner, ctx, "CIVILIZATION", source, ReadId(source), "Civil resolution projected into field-addressable evidence."); }
        public static void OpenFromLogicTheory(Window owner, DeepSystemsContext ctx, WorldIndex index, LogicTheory source) { ProjectTyped(owner, ctx, "EPISTEMIC_LOGIC", source, ReadId(source), "Frozen theory projected without changing its logic status."); }
        public static void OpenFromVerificationModel(Window owner, DeepSystemsContext ctx, WorldIndex index, VerificationModel source) { ProjectTyped(owner, ctx, "VERIFICATION", source, ReadId(source), "Verification model projected into data provenance."); }
        public static void OpenFromStrategyModel(Window owner, DeepSystemsContext ctx, WorldIndex index, StrategyModel source) { ProjectTyped(owner, ctx, "STRATEGY", source, ReadId(source), "Counterfactual strategy projected into a bounded record."); }
        public static void OpenFromDiscoveryProtocol(Window owner, DeepSystemsContext ctx, WorldIndex index, DiscoveryProtocol source) { ProjectTyped(owner, ctx, "SCIENTIFIC_DISCOVERY", source, ReadId(source), "Discovery protocol projected with immutable fingerprint."); }
        public static void OpenFromEngineeringArchitecture(Window owner, DeepSystemsContext ctx, WorldIndex index, EngineeringArchitecture source) { ProjectTyped(owner, ctx, "SYSTEMS_ENGINEERING", source, ReadId(source), "Engineering architecture projected as structured evidence."); }
        public static void OpenFromCyberneticTwin(Window owner, DeepSystemsContext ctx, WorldIndex index, CyberTwinModel source) { ProjectTyped(owner, ctx, "CYBERNETIC_TWIN", source, ReadId(source), "Cybernetic twin projected without taking control authority."); }
        public static void OpenFromComplexSystem(Window owner, DeepSystemsContext ctx, WorldIndex index, ComplexSystemModel source) { ProjectTyped(owner, ctx, "COMPLEX_SYSTEMS", source, ReadId(source), "Complex-system model projected as bounded data evidence."); }
        public static void OpenFromOperationsResearchModel(Window owner, DeepSystemsContext ctx, WorldIndex index, OperationsResearchModel source) { ProjectTyped(owner, ctx, "OPERATIONS_RESEARCH", source, ReadId(source), "Optimization model projected without dispatch or execution authority."); }
        public static void OpenFromEconomicModel(Window owner, DeepSystemsContext ctx, WorldIndex index, EconomicModel source) { ProjectTyped(owner, ctx, "ECONOMIC_CIVILIZATION", source, ReadId(source), "Economic world projected into tabular evidence."); }
        public static void OpenFromSpatialWorld(Window owner, DeepSystemsContext ctx, WorldIndex index, SpatialWorldModel source) { ProjectTyped(owner, ctx, "SPATIAL_CARTOGRAPHY", source, ReadId(source), "Spatial world projected as structured data without altering geography."); }
        public static void OpenFromDistributedSystem(Window owner, DeepSystemsContext ctx, WorldIndex index, DistributedSystemModel source) { ProjectTyped(owner, ctx, "DISTRIBUTED_SYSTEMS", source, ReadId(source), "Distributed system projected into lineage-aware evidence."); }
        public static void OpenFromSecurityModel(Window owner, DeepSystemsContext ctx, WorldIndex index, SecurityModel source) { ProjectTyped(owner, ctx, "ADVERSARIAL_SECURITY", source, ReadId(source), "Simulation-only security model projected into Data Foundry without expanding security authority."); }
        public static void OpenFromSoftwareGenomeSnapshot(Window owner, DeepSystemsContext ctx, GenomeSnapshot source) { ProjectTyped(owner, ctx, "SOFTWARE_GENOME", source, source == null ? "UNKNOWN" : source.Id, "Codebase genome projected as bounded structural data with source fingerprint, counts, files, symbols and graph evidence."); }
        public static void OpenFromLivingDocumentVersion(Window owner, DeepSystemsContext ctx, LivingDocument document, LivingVersion source) { ProjectTyped(owner, ctx, "LIVING_DOCUMENT", source, source == null ? "UNKNOWN" : source.Id, "Living document version projected as structured traceability data: paragraph/claim/evidence graph, source fingerprints, calculations, requirements and immutable version root."); }
        public static void OpenFromExpeditionMission(Window owner, DeepSystemsContext ctx, ExpeditionMission source) { ProjectTyped(owner, ctx, "GRAND_UNIFIED_EXPEDITION", source, source == null ? "UNKNOWN" : source.Id, "Grand unified expedition mission projected as structured branch/step/finding/checkpoint provenance evidence."); }

        private static void ProjectTyped(Window owner, DeepSystemsContext ctx, string civilization, object source, string sourceId, string rule)
        {
            if (ctx == null || source == null) return; ExpeditionWorkspace.Visit(ctx, "data", civilization + " projection", rule); Ensure(ctx); string fingerprint = DataFoundryKernel.HashObject(source); string sid = String.IsNullOrWhiteSpace(sourceId) ? DataFoundryKernel.NewId("SOURCE") : sourceId; Dictionary<string, object> row = ReflectRecord(source); row["projection_source_civilization"] = civilization; row["projection_source_id"] = sid; row["projection_source_fingerprint"] = fingerprint; row["projection_rule"] = rule; DataDataset d = DataFoundryKernel.ProjectionDataset(civilization + " projection · " + sid, civilization, sid, fingerprint, row); Store.Datasets.Insert(0, d); DataProjectionReceipt receipt = new DataProjectionReceipt { Id = DataFoundryKernel.NewId("PROJ"), SourceCivilization = civilization, SourceId = sid, SourceFingerprint = fingerprint, DatasetId = d.Id, ProjectionRule = rule }; Store.Projections.Insert(0, receipt); Store.Lineage.Insert(0, new DataLineageNode { Id = DataFoundryKernel.NewId("LIN"), DatasetId = d.Id, Kind = "TYPED_PROJECTION", Operation = civilization + " → DATAΩ", Fingerprint = d.DataFingerprint, Detail = rule, InputDatasetIds = new List<string> { sid } }); Save(ctx); AppendLedger(ctx, "TYPED_PROJECTION", d.Id, civilization + " source " + sid, d.DataFingerprint, DataFoundryKernel.HashObject(receipt)); OpenDataset(owner, ctx, d);
        }

        private static Dictionary<string, object> ReflectRecord(object source)
        {
            Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); Type t = source.GetType(); row["source_type"] = t.FullName ?? t.Name; foreach (PropertyInfo p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(x => x.Name)) { if (!p.CanRead || p.GetIndexParameters().Length > 0) continue; object value = null; try { value = p.GetValue(source, null); } catch { continue; } if (value == null) { row[p.Name] = ""; continue; } Type vt = value.GetType(); if (IsScalar(vt)) row[p.Name] = Convert.ToString(value, CultureInfo.InvariantCulture); else { ICollection c = value as ICollection; if (c != null) row[p.Name] = "[collection count=" + c.Count.ToString(CultureInfo.InvariantCulture) + "] " + ClipJson(value, 1200); else row[p.Name] = ClipJson(value, 1200); } if (row.Count >= 192) break; } return row;
        }
        private static bool IsScalar(Type t) { return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid); }
        private static string ClipJson(object o, int max) { string s; try { s = Json.Serialize(o); } catch { s = Convert.ToString(o, CultureInfo.InvariantCulture); } return DataFoundryKernel.Clip(s, max); }
        private static string ReadId(object source) { if (source == null) return ""; PropertyInfo p = source.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public); if (p == null) return ""; try { return Convert.ToString(p.GetValue(source, null), CultureInfo.InvariantCulture); } catch { return ""; } }

        private static void SeedStarter(DeepSystemsContext ctx)
        {
            List<IDictionary<string, object>> customers = new List<IDictionary<string, object>>();
            customers.Add(R("customer_id", "C001", "name", "Orion Fabrication", "region", "North", "segment", "Industrial", "risk_band", "LOW", "annual_value", 125000));
            customers.Add(R("customer_id", "C002", "name", "Blue Mesa Foods", "region", "South", "segment", "Food", "risk_band", "MEDIUM", "annual_value", 84000));
            customers.Add(R("customer_id", "C003", "name", "Kestrel Transit", "region", "West", "segment", "Mobility", "risk_band", "LOW", "annual_value", 219000));
            customers.Add(R("customer_id", "C004", "name", "Northstar Clinic", "region", "East", "segment", "Health", "risk_band", "HIGH", "annual_value", 171000));
            customers.Add(R("customer_id", "C005", "name", "Atlas Cold Chain", "region", "North", "segment", "Logistics", "risk_band", "MEDIUM", "annual_value", 302000));
            List<IDictionary<string, object>> orders = new List<IDictionary<string, object>>();
            orders.Add(R("order_id", "O1001", "customer_id", "C001", "facility_id", "F1", "amount", 15300, "units", 41, "status", "SHIPPED"));
            orders.Add(R("order_id", "O1002", "customer_id", "C003", "facility_id", "F2", "amount", 42100, "units", 110, "status", "PROCESSING"));
            orders.Add(R("order_id", "O1003", "customer_id", "C001", "facility_id", "F1", "amount", 8100, "units", 19, "status", "SHIPPED"));
            orders.Add(R("order_id", "O1004", "customer_id", "C005", "facility_id", "F3", "amount", 66300, "units", 144, "status", "HELD"));
            orders.Add(R("order_id", "O1005", "customer_id", "C004", "facility_id", "F2", "amount", 27250, "units", 56, "status", "PROCESSING"));
            orders.Add(R("order_id", "O1006", "customer_id", "C002", "facility_id", "F3", "amount", 9300, "units", 25, "status", "SHIPPED"));
            List<IDictionary<string, object>> facilities = new List<IDictionary<string, object>>();
            facilities.Add(R("facility_id", "F1", "name", "Forge North", "capacity", 900, "utilization", 0.72, "region", "North"));
            facilities.Add(R("facility_id", "F2", "name", "Transit East", "capacity", 1100, "utilization", 0.81, "region", "East"));
            facilities.Add(R("facility_id", "F3", "name", "Cold South", "capacity", 700, "utilization", 0.93, "region", "South"));
            List<IDictionary<string, object>> sensors = new List<IDictionary<string, object>>();
            sensors.Add(R("sensor_id", "S1", "facility_id", "F1", "metric", "temperature", "value", 21.4, "unit", "C"));
            sensors.Add(R("sensor_id", "S2", "facility_id", "F2", "metric", "vibration", "value", 0.31, "unit", "g"));
            sensors.Add(R("sensor_id", "S3", "facility_id", "F3", "metric", "temperature", "value", -17.8, "unit", "C"));
            sensors.Add(R("sensor_id", "S4", "facility_id", "F3", "metric", "door_open_seconds", "value", 74, "unit", "s"));
            List<DataDataset> starter = new List<DataDataset> {
                DataFoundryKernel.SyntheticDataset("Atlas Data Furnace · customers", "Synthetic customer master for relationship and quality demonstrations.", customers, "STARTER_WORLD", "ATLAS-CUSTOMERS", "starter-133724"),
                DataFoundryKernel.SyntheticDataset("Atlas Data Furnace · orders", "Synthetic transactions connected to customers and facilities.", orders, "STARTER_WORLD", "ATLAS-ORDERS", "starter-133724"),
                DataFoundryKernel.SyntheticDataset("Atlas Data Furnace · facilities", "Synthetic facility registry.", facilities, "STARTER_WORLD", "ATLAS-FACILITIES", "starter-133724"),
                DataFoundryKernel.SyntheticDataset("Atlas Data Furnace · sensors", "Synthetic operational measurements connected to facilities.", sensors, "STARTER_WORLD", "ATLAS-SENSORS", "starter-133724")
            };
            foreach (DataDataset d in starter) { Store.Datasets.Add(d); Store.Lineage.Add(new DataLineageNode { Id = DataFoundryKernel.NewId("LIN"), DatasetId = d.Id, Kind = "STARTER_SOURCE", Operation = "SYNTHETIC_GENESIS", Fingerprint = d.DataFingerprint, Detail = d.Description }); }
            for (int i = 0; i < starter.Count; i++) for (int j = i + 1; j < starter.Count; j++) Store.Relations.AddRange(DataFoundryKernel.InferRelations(starter[i], starter[j]));
            Save(ctx); AppendLedger(ctx, "FOUNDRY_GENESIS", "ATLAS-DATA-FURNACE", "Starter world created: customers, orders, facilities and sensors.", DataFoundryKernel.HashText(String.Join("|", starter.Select(x => x.DataFingerprint).ToArray())), "");
        }

        private static IDictionary<string, object> R(params object[] items) { Dictionary<string, object> d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i + 1 < items.Length; i += 2) d[Convert.ToString(items[i], CultureInfo.InvariantCulture)] = items[i + 1]; return d; }

        private static void OpenImportBay(Window owner, DeepSystemsContext ctx)
        {
            OpenFileDialog dlg = new OpenFileDialog { Title = "YOMI Data Foundry · Choose local data source", Filter = "Data files (*.csv;*.tsv;*.jsonl;*.ndjson)|*.csv;*.tsv;*.jsonl;*.ndjson|CSV (*.csv)|*.csv|TSV (*.tsv)|*.tsv|JSON Lines (*.jsonl;*.ndjson)|*.jsonl;*.ndjson|All files (*.*)|*.*", Multiselect = false, CheckFileExists = true };
            bool? picked = dlg.ShowDialog(owner); if (picked != true) return; string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant(); DataImportOptions options = new DataImportOptions(); if (ext == ".jsonl" || ext == ".ndjson") options.Format = "JSONL"; else if (ext == ".tsv") { options.Format = "TSV"; options.Delimiter = '\t'; } else { options.Format = "CSV"; options.Delimiter = ','; }
            string name = Prompt(owner, "DATASET NAME", "Retained dataset name:", Path.GetFileNameWithoutExtension(dlg.FileName)); if (name == null) return;
            try { DataImportResult import = DataFoundryKernel.ImportFile(dlg.FileName, name, options); DataDataset d = import.Dataset; Store.Datasets.Insert(0, d); Store.Lineage.Insert(0, new DataLineageNode { Id = DataFoundryKernel.NewId("LIN"), DatasetId = d.Id, Kind = "LOCAL_SOURCE", Operation = "STREAM_PROFILE", Fingerprint = d.DataFingerprint, Detail = d.Origin == null ? "" : d.Origin.ToString() }); Save(ctx); AppendLedger(ctx, "DATA_IMPORT", d.Id, "Rows " + d.RowCount.ToString(CultureInfo.InvariantCulture) + "; fields " + d.Fields.Count.ToString(CultureInfo.InvariantCulture) + "; warnings " + import.Warnings.Count.ToString(CultureInfo.InvariantCulture), d.DataFingerprint, import.CertificateHash); OpenDataset(owner, ctx, d); }
            catch (Exception ex) { MessageBox.Show(owner, ex.Message, "YOMI Data Foundry · Import failed", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private static void OpenDatasets(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · DATASET REGISTRY", 1420, 880); DockPanel root = Shell("DATASET REGISTRY", "Persistent bounded profiles. Double-click a dataset to descend into its linked evidence rooms."); StackPanel bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 0, 18, 12) }; bar.Children.Add(Btn("IMPORT LOCAL DATA", delegate { OpenImportBay(w, ctx); })); bar.Children.Add(Btn("INFER ALL RELATIONS", delegate { RunRelations(w, ctx); })); DockPanel.SetDock(bar, Dock.Top); root.Children.Add(bar); ListBox list = List(); list.ItemsSource = Store.Datasets; list.MouseDoubleClick += delegate { DataDataset d = list.SelectedItem as DataDataset; if (d != null) OpenDataset(w, ctx, d); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenDataset(Window owner, DeepSystemsContext ctx, DataDataset d)
        {
            if (d == null) return; Window w = W(owner, "YOMI · DATASET · " + d.Name, 1460, 900); DockPanel root = Shell(d.Name, d.Description ?? "Dataset"); TextBlock meta = new TextBlock { Text = "id " + d.Id + "   ·   fp " + DataFoundryKernel.Short(d.DataFingerprint) + "   ·   " + d.RowCount.ToString("N0", CultureInfo.InvariantCulture) + " rows   ·   " + d.Fields.Count.ToString(CultureInfo.InvariantCulture) + " fields   ·   " + (d.Format ?? "DATA") + "   ·   quality " + d.QualityScore.ToString("P0", CultureInfo.InvariantCulture), Foreground = Accent, Margin = new Thickness(20, 0, 20, 12), TextWrapping = TextWrapping.Wrap }; DockPanel.SetDock(meta, Dock.Top); root.Children.Add(meta); WrapPanel actions = new WrapPanel { Margin = new Thickness(14) };
            SmallPortal(actions, "FIELDS", delegate { OpenCollection(w, "FIELDS · " + d.Name, d.Fields.Cast<object>().ToList(), "Inferred types and explanations are evidence-bound to the retained profile."); });
            SmallPortal(actions, "SAMPLES", delegate { OpenCollection(w, "SAMPLES · " + d.Name, d.Samples.Cast<object>().ToList(), "Bounded deterministic samples, not the entire table."); });
            SmallPortal(actions, "QUALITY", delegate { OpenCollection(w, "QUALITY · " + d.Name, d.Quality.Cast<object>().ToList(), "Quality findings disclose bounded estimator limits."); });
            SmallPortal(actions, "CHUNKS", delegate { OpenCollection(w, "CHUNKS · " + d.Name, d.Chunks.Cast<object>().ToList(), "Streaming telemetry by chunk."); });
            SmallPortal(actions, "CORRELATE", delegate { RunCorrelationsFor(w, ctx, d); });
            SmallPortal(actions, "CONTRACT", delegate { CreateContractFor(w, ctx, d); });
            SmallPortal(actions, "BASELINE", delegate { CreateBaselineFor(w, ctx, d); });
            SmallPortal(actions, "RECIPE", delegate { CreateRecipeFor(w, ctx, d); });
            SmallPortal(actions, "LINEAGE", delegate { OpenCollection(w, "LINEAGE · " + d.Name, Store.Lineage.Where(x => Eq(x.DatasetId, d.Id) || (x.InputDatasetIds != null && x.InputDatasetIds.Contains(d.Id))).Cast<object>().ToList(), "Parent and child derivations."); });
            SmallPortal(actions, "EVIDENCE", delegate { OpenEvidenceFor(w, d); });
            SmallPortal(actions, "WORLD SPINE", delegate { WorldPassportSpine.CaptureAndOpen(w, ctx, d.Name, "DATASET", "DATAΩ", "data", d.Id, d, d.Origin == null ? "" : d.Origin.Locator, "$dataset"); });
            root.Children.Add(new ScrollViewer { Content = actions, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); w.Content = root; w.Show();
        }

        private static void OpenOrigins(Window owner) { OpenCollection(owner, "SOURCE PROVENANCE", Store.Datasets.Where(x => x.Origin != null).Select(x => (object)(x.Name + "   ·   " + x.Origin.ToString() + "   ·   data fp " + DataFoundryKernel.Short(x.DataFingerprint))).ToList(), "Local-file origins retain full path and SHA-256. Typed projections retain source civilization, ID and upstream fingerprint. Source bytes themselves are not copied into controller JSON."); }
        private static void OpenFieldInventory(Window owner, string title, Func<DataDataset, List<object>> selector) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, title + " · " + d.Name, selector(d), "Dataset fingerprint " + DataFoundryKernel.Short(d.DataFingerprint)); }
        private static void OpenFieldMicroscope(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; DataFieldProfile f = ChooseField(owner, d); if (f == null) return; List<object> rows = new List<object> { f, f.Explanation, "Observed=" + f.Observed, "NullOrBlank=" + f.NullOrBlank, "ApproxDistinct=" + f.ApproxDistinct + (f.DistinctCapped ? "+ capped" : ""), "NumericCount=" + f.NumericCount, "IntegerCount=" + f.IntegerCount, "DateCount=" + f.DateCount, "BooleanCount=" + f.BooleanCount, "TextCount=" + f.TextCount, "Min/Max/Mean/StdDev=" + f.MinNumber.ToString("R", CultureInfo.InvariantCulture) + " / " + f.MaxNumber.ToString("R", CultureInfo.InvariantCulture) + " / " + f.MeanNumber.ToString("R", CultureInfo.InvariantCulture) + " / " + f.StdDevNumber.ToString("R", CultureInfo.InvariantCulture), "Examples=" + String.Join(" | ", f.ExampleValues.ToArray()), "Flags=" + String.Join(", ", f.QualityFlags.ToArray()) }; OpenCollection(owner, "FIELD MICROSCOPE · " + f.Name, rows, "Observed evidence only. Inference is descriptive and can be wrong for domain-specific encodings."); }
        private static void OpenNullness(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, "NULLNESS MAP · " + d.Name, d.Fields.OrderByDescending(x => x.NullRate).Select(x => (object)(x.Name + "   ·   " + x.NullRate.ToString("P2", CultureInfo.InvariantCulture) + "   ·   " + x.NullOrBlank.ToString("N0", CultureInfo.InvariantCulture) + "/" + x.Observed.ToString("N0", CultureInfo.InvariantCulture))).ToList(), "Missingness is preserved as evidence, never silently imputed by profiling."); }
        private static void OpenTypeInference(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, "TYPE INFERENCE · " + d.Name, d.Fields.OrderBy(x => x.Ordinal).Select(x => (object)(x.Name + "   ·   " + x.InferredType + "   ·   int/num/bool/date/text " + x.IntegerCount + "/" + x.NumericCount + "/" + x.BooleanCount + "/" + x.DateCount + "/" + x.TextCount + "   ·   " + x.Explanation)).ToList(), "Type inference is transparent, deterministic and based on observed scalar parses."); }
        private static void OpenDistinctness(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, "DISTINCTNESS · " + d.Name, d.Fields.OrderByDescending(x => x.UniqueRate).Select(x => (object)(x.Name + "   ·   distinct ≈" + x.ApproxDistinct.ToString("N0", CultureInfo.InvariantCulture) + (x.DistinctCapped ? "+" : "") + "   ·   unique " + x.UniqueRate.ToString("P1", CultureInfo.InvariantCulture) + (x.CandidateKey ? "   ·   CANDIDATE KEY" : ""))).ToList(), "If a field reaches its distinct-memory ceiling, the count is explicitly a lower bound and cannot establish uniqueness."); }
        private static void OpenDistribution(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, "DISTRIBUTIONS · " + d.Name, d.Fields.Select(x => (object)(x.Name + "   ·   " + x.InferredType + "   ·   num[min=" + x.MinNumber.ToString("0.###", CultureInfo.InvariantCulture) + ", max=" + x.MaxNumber.ToString("0.###", CultureInfo.InvariantCulture) + ", mean=" + x.MeanNumber.ToString("0.###", CultureInfo.InvariantCulture) + ", sd=" + x.StdDevNumber.ToString("0.###", CultureInfo.InvariantCulture) + "]   ·   len[min=" + x.MinLength.ToString("0", CultureInfo.InvariantCulture) + ", max=" + x.MaxLength.ToString("0", CultureInfo.InvariantCulture) + ", mean=" + x.MeanLength.ToString("0.0", CultureInfo.InvariantCulture) + "]")).ToList(), "Streaming Welford-style moments and bounded field statistics."); }

        private static void RunCorrelations(Window owner, DeepSystemsContext ctx) { DataDataset d = ChooseDataset(owner); if (d != null) RunCorrelationsFor(owner, ctx, d); }
        private static void RunCorrelationsFor(Window owner, DeepSystemsContext ctx, DataDataset d) { List<DataCorrelation> c = DataFoundryKernel.Correlations(d); Store.Correlations.RemoveAll(x => Eq(x.DatasetId, d.Id)); Store.Correlations.AddRange(c); Save(ctx); AppendLedger(ctx, "CORRELATION_ANALYSIS", d.Id, c.Count.ToString(CultureInfo.InvariantCulture) + " bounded sample correlations.", d.DataFingerprint, DataFoundryKernel.HashObject(c)); OpenCollection(owner, "CORRELATION MATRIX · " + d.Name, c.Cast<object>().ToList(), "Pearson r is computed only from retained paired numeric samples, never misrepresented as full-population inference."); }

        private static void OpenOutliers(Window owner)
        {
            DataDataset d = ChooseDataset(owner); if (d == null) return; List<object> rows = new List<object>(); foreach (DataFieldProfile f in d.Fields.Where(x => x.StdDevNumber > 0 && (x.InferredType == "NUMBER" || x.InferredType == "INTEGER"))) foreach (DataRowSample s in d.Samples) { string raw; double v; if (s.Values.TryGetValue(f.Name, out raw) && Double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out v)) { double z = Math.Abs((v - f.MeanNumber) / f.StdDevNumber); if (z >= 2.5) rows.Add(f.Name + "   ·   row " + s.RowNumber + "   ·   value " + v.ToString("R", CultureInfo.InvariantCulture) + "   ·   z≈" + z.ToString("0.00", CultureInfo.InvariantCulture)); } } OpenCollection(owner, "OUTLIER RADAR · " + d.Name, rows, "Exploratory sample flags using profile mean/standard deviation; not a claim that the population follows a normal distribution.");
        }

        private static void OpenDuplicates(Window owner)
        {
            DataDataset d = ChooseDataset(owner); if (d == null) return; List<object> rows = new List<object>(); Dictionary<string, List<DataRowSample>> groups = d.Samples.GroupBy(x => DataFoundryKernel.HashText(String.Join("|", x.Values.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value).ToArray()))).Where(g => g.Count() > 1).ToDictionary(g => g.Key, g => g.ToList()); foreach (KeyValuePair<string, List<DataRowSample>> g in groups) rows.Add("sample duplicate group " + DataFoundryKernel.Short(g.Key) + "   ·   rows " + String.Join(",", g.Value.Select(x => x.RowNumber.ToString(CultureInfo.InvariantCulture)).ToArray())); foreach (DataFieldProfile f in d.Fields.Where(x => x.UniqueRate < 0.25 && x.Observed > 0).OrderBy(x => x.UniqueRate)) rows.Add("low-distinct field   ·   " + f.Name + "   ·   unique≈" + f.UniqueRate.ToString("P1", CultureInfo.InvariantCulture)); OpenCollection(owner, "DUPLICATE LAB · " + d.Name, rows, "Exact duplicate evidence applies only to retained samples. Low distinctness is profile evidence, not full duplicate-row proof.");
        }
        private static void OpenKeys(Window owner) { DataDataset d = ChooseDataset(owner); if (d == null) return; OpenCollection(owner, "KEY CANDIDATES · " + d.Name, d.Fields.Where(x => x.CandidateKey || (x.NullRate == 0 && !x.DistinctCapped && x.UniqueRate > 0.95)).OrderByDescending(x => x.UniqueRate).Cast<object>().ToList(), "Candidate key requires exact bounded distinctness; capped estimates are never promoted to keys."); }

        private static void RunRelations(Window owner, DeepSystemsContext ctx)
        {
            List<DataRelation> relations = new List<DataRelation>(); for (int i = 0; i < Store.Datasets.Count; i++) for (int j = i + 1; j < Store.Datasets.Count; j++) relations.AddRange(DataFoundryKernel.InferRelations(Store.Datasets[i], Store.Datasets[j])); Store.Relations = relations.OrderByDescending(x => x.Confidence).Take(MaxArtifacts).ToList(); Save(ctx); AppendLedger(ctx, "RELATION_INFERENCE", "FOUNDRY", Store.Relations.Count.ToString(CultureInfo.InvariantCulture) + " candidate relations across " + Store.Datasets.Count.ToString(CultureInfo.InvariantCulture) + " datasets.", "", DataFoundryKernel.HashObject(Store.Relations)); OpenCollection(owner, "RELATIONSHIP FORGE", Store.Relations.Cast<object>().ToList(), "Candidates use deterministic samples, field names and inferred types. They are hypotheses, not database constraints.");
        }

        private static void OpenEntityResolution(Window owner) { OpenCollection(owner, "ENTITY RESOLUTION LAB", Store.Relations.Where(x => x.Confidence >= 0.55).Select(x => (object)(x.ToString() + "   ·   basis " + x.Basis)).ToList(), "Ambiguity remains explicit. Data Foundry does not merge identities or mutate source records."); }

        private static void OpenRecipes(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · TRANSFORM RECIPE FORGE", 1320, 840); DockPanel root = Shell("TRANSFORM RECIPE FORGE", "Recipes are declarative transformations bound to a source fingerprint. Creation does not alter source bytes."); StackPanel bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 0, 18, 12) }; bar.Children.Add(Btn("NEW RECIPE", delegate { DataDataset d = ChooseDataset(w); if (d != null) CreateRecipeFor(w, ctx, d); })); bar.Children.Add(Btn("ADD STEP TO SELECTED", delegate { DataRecipe r = ChooseRecipe(w); if (r != null) AddStep(w, ctx, r); })); DockPanel.SetDock(bar, Dock.Top); root.Children.Add(bar); ListBox list = List(); list.ItemsSource = Store.Recipes; list.MouseDoubleClick += delegate { DataRecipe r = list.SelectedItem as DataRecipe; if (r != null) OpenCollection(w, "RECIPE · " + r.Name, r.Steps.Cast<object>().ToList(), "Source " + r.SourceDatasetId + " / " + DataFoundryKernel.Short(r.SourceFingerprint)); }; root.Children.Add(list); w.Content = root; w.Show();
        }
        private static void CreateRecipeFor(Window owner, DeepSystemsContext ctx, DataDataset d) { string name = Prompt(owner, "RECIPE NAME", "Name:", d.Name + " · cleaning recipe"); if (name == null) return; DataRecipe r = new DataRecipe { Id = DataFoundryKernel.NewId("RECIPE"), Name = name, SourceDatasetId = d.Id, SourceFingerprint = d.DataFingerprint }; r.Assumptions.Add("Source bytes are immutable; preview precedes materialization."); Store.Recipes.Insert(0, r); Save(ctx); AppendLedger(ctx, "RECIPE_CREATED", r.Id, r.Name, d.DataFingerprint, DataFoundryKernel.HashObject(r)); AddStep(owner, ctx, r); }
        private static void AddStep(Window owner, DeepSystemsContext ctx, DataRecipe r) { string raw = Prompt(owner, "ADD TRANSFORM STEP", "operation | field | target/argument | argument2\nOperations: TRIM, NORMALIZE_WHITESPACE, LOWER, UPPER, REPLACE_NULL, REPLACE, RENAME, DROP, PARSE_NUMBER, PARSE_DATE, FILTER_EQUALS, FILTER_NOT_EQUALS", "TRIM | field_name | | "); if (raw == null) return; string[] f = raw.Split('|'); DataTransformStep s = new DataTransformStep { Id = DataFoundryKernel.NewId("STEP"), Order = r.Steps.Count + 1, Operation = At(f, 0, "TRIM").Trim().ToUpperInvariant(), Field = At(f, 1, "").Trim(), Enabled = true }; string third = At(f, 2, "").Trim(); if (s.Operation == "RENAME") s.TargetField = third; else s.Argument = third; s.Argument2 = At(f, 3, "").Trim(); r.Steps.Add(s); r.UpdatedUtc = Now(); Save(ctx); AppendLedger(ctx, "RECIPE_STEP", r.Id, s.ToString(), r.SourceFingerprint, DataFoundryKernel.HashObject(s)); OpenCollection(owner, "RECIPE · " + r.Name, r.Steps.Cast<object>().ToList(), "Declarative only; no source mutation."); }

        private static void OpenRecipePreview(Window owner) { DataRecipe r = ChooseRecipe(owner); if (r == null) return; DataDataset d = Store.Datasets.FirstOrDefault(x => Eq(x.Id, r.SourceDatasetId)); if (d == null) return; DataRecipePreview p = DataFoundryKernel.PreviewRecipe(d, r); List<object> rows = new List<object> { "input=" + p.InputSamples + " output=" + p.OutputSamples + " rejected=" + p.RejectedSamples + " cert=" + DataFoundryKernel.Short(p.CertificateHash) }; for (int i = 0; i < Math.Min(p.Before.Count, p.After.Count); i++) { rows.Add("BEFORE   " + p.Before[i].ToString()); rows.Add("AFTER    " + p.After[i].ToString()); } rows.AddRange(p.Warnings.Cast<object>()); OpenCollection(owner, "CLEANING PREVIEW · " + r.Name, rows, "Preview is restricted to retained samples and never changes source bytes."); }

        private static void OpenMaterialization(Window owner, DeepSystemsContext ctx)
        {
            DataRecipe r = ChooseRecipe(owner); if (r == null) return; DataDataset d = Store.Datasets.FirstOrDefault(x => Eq(x.Id, r.SourceDatasetId)); if (d == null) return; MessageBoxResult confirm = MessageBox.Show(owner, "Materialize this recipe into YOMI-owned Data Foundry derived storage?\n\nThe original source file will not be overwritten, renamed or deleted.", "YOMI Data Foundry", MessageBoxButton.OKCancel, MessageBoxImage.Information); if (confirm != MessageBoxResult.OK) return; string dir = Path.Combine(ctx.StateRoot, "data-foundry", "derived"); try { DataMaterializationResult m = DataFoundryKernel.MaterializeDelimited(d, r, dir); if (!m.Success) { MessageBox.Show(owner, String.Join(Environment.NewLine, m.Warnings.ToArray()), "Data Foundry", MessageBoxButton.OK, MessageBoxImage.Information); return; } DataImportOptions opts = new DataImportOptions { Format = "CSV", Delimiter = ',', HasHeader = true }; DataImportResult imported = DataFoundryKernel.ImportDelimited(m.OutputPath, d.Name + " · derived", opts); imported.Dataset.ParentDatasetId = d.Id; imported.Dataset.SourceKind = "DERIVED_RECIPE"; imported.Dataset.SourceId = r.Id; imported.Dataset.SourceFingerprint = d.DataFingerprint; imported.Dataset.Origin.ParentDatasetId = d.Id; imported.Dataset.Origin.ParentFingerprint = d.DataFingerprint; imported.Dataset.Origin.RecipeId = r.Id; imported.Dataset.DataFingerprint = DataFoundryKernel.Fingerprint(imported.Dataset); Store.Datasets.Insert(0, imported.Dataset); Store.Lineage.Insert(0, new DataLineageNode { Id = DataFoundryKernel.NewId("LIN"), DatasetId = imported.Dataset.Id, ParentNodeId = Store.Lineage.FirstOrDefault(x => Eq(x.DatasetId, d.Id)) == null ? "" : Store.Lineage.First(x => Eq(x.DatasetId, d.Id)).Id, Kind = "DERIVED_MATERIALIZATION", Operation = r.Name, Fingerprint = imported.Dataset.DataFingerprint, Detail = m.ToString(), InputDatasetIds = new List<string> { d.Id } }); Save(ctx); AppendLedger(ctx, "DERIVED_MATERIALIZATION", imported.Dataset.Id, m.ToString(), imported.Dataset.DataFingerprint, m.CertificateHash); OpenDataset(owner, ctx, imported.Dataset); } catch (Exception ex) { MessageBox.Show(owner, ex.Message, "Data Foundry materialization failed", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private static void OpenContracts(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · SCHEMA CONTRACTS", 1320, 840); DockPanel root = Shell("SCHEMA CONTRACTS", "Contracts freeze structural expectations against a source fingerprint. They do not rewrite data."); Button create = Btn("BUILD CONTRACT FROM DATASET", delegate { DataDataset d = ChooseDataset(w); if (d != null) CreateContractFor(w, ctx, d); }); DockPanel.SetDock(create, Dock.Top); root.Children.Add(create); ListBox list = List(); list.ItemsSource = Store.Contracts; list.MouseDoubleClick += delegate { DataSchemaContract c = list.SelectedItem as DataSchemaContract; if (c != null) OpenCollection(w, "CONTRACT · " + c.Name, c.Rules.Cast<object>().ToList(), "Certificate " + DataFoundryKernel.Short(c.CertificateHash)); }; root.Children.Add(list); w.Content = root; w.Show();
        }
        private static void CreateContractFor(Window owner, DeepSystemsContext ctx, DataDataset d) { string name = Prompt(owner, "CONTRACT NAME", "Name:", d.Name + " · schema contract"); if (name == null) return; DataSchemaContract c = DataFoundryKernel.BuildContract(d, name); Store.Contracts.Insert(0, c); Save(ctx); AppendLedger(ctx, "CONTRACT_SEALED", c.Id, c.Name, d.DataFingerprint, c.CertificateHash); OpenCollection(owner, "CONTRACT · " + c.Name, c.Rules.Cast<object>().ToList(), "Frozen against dataset " + d.Id + " / " + DataFoundryKernel.Short(d.DataFingerprint)); }

        private static void RunContractValidation(Window owner, DeepSystemsContext ctx) { DataSchemaContract c = ChooseContract(owner); if (c == null) return; DataDataset d = ChooseDataset(owner); if (d == null) return; DataContractResult r = DataFoundryKernel.ValidateContract(c, d); Store.ContractResults.Insert(0, r); Save(ctx); AppendLedger(ctx, "CONTRACT_VALIDATION", d.Id, r.ToString(), d.DataFingerprint, r.CertificateHash); List<object> rows = new List<object> { r }; rows.AddRange(r.Findings.Cast<object>()); OpenCollection(owner, "VALIDATION GATE · " + c.Name + " → " + d.Name, rows, "Contract validation is deterministic against retained profile fields and fingerprint."); }

        private static void RunDrift(Window owner, DeepSystemsContext ctx) { DataDataset before = ChooseDataset(owner, "CHOOSE BASE DATASET"); if (before == null) return; DataDataset after = ChooseDataset(owner, "CHOOSE COMPARISON DATASET"); if (after == null || Eq(before.Id, after.Id)) return; DataDriftReport r = DataFoundryKernel.Compare(before, after); Store.Drifts.Insert(0, r); Save(ctx); AppendLedger(ctx, "DRIFT_COMPARISON", before.Id + "→" + after.Id, r.ToString(), after.DataFingerprint, r.CertificateHash); List<object> rows = new List<object> { r }; rows.AddRange(r.Fields.Cast<object>()); OpenCollection(owner, "DRIFT RADAR · " + before.Name + " → " + after.Name, rows, "Schema and distribution drift are profile comparisons, not causal explanations."); }

        private static void OpenVersionArchaeology(Window owner) { List<object> rows = new List<object>(); foreach (DataDataset d in Store.Datasets.OrderByDescending(x => x.CreatedUtc)) rows.Add(d.Name + "   ·   id " + d.Id + "   ·   parent " + (d.ParentDatasetId ?? "∅") + "   ·   source " + (d.SourceKind ?? "") + ":" + (d.SourceId ?? "") + "   ·   fp " + DataFoundryKernel.Short(d.DataFingerprint)); rows.AddRange(Store.Baselines.OrderByDescending(x => x.CreatedUtc).Cast<object>()); OpenCollection(owner, "VERSION ARCHAEOLOGY", rows, "Derived children preserve parent dataset identity and parent fingerprint; historical objects are not rewritten in place."); }

        private static void OpenQueryWorkbench(Window owner)
        {
            DataDataset d = ChooseDataset(owner); if (d == null) return; string raw = Prompt(owner, "QUERY BOUNDED SAMPLES", "field | contains text\nThis searches only retained samples.", (d.Fields.FirstOrDefault() == null ? "field" : d.Fields.First().Name) + " | "); if (raw == null) return; string[] p = raw.Split('|'); string field = At(p, 0, "").Trim(), term = At(p, 1, "").Trim(); List<object> rows = d.Samples.Where(s => { string v; return s.Values.TryGetValue(field, out v) && (v ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0; }).Cast<object>().ToList(); OpenCollection(owner, "QUERY WORKBENCH · " + d.Name, rows, "Bounded sample query; full-source query execution is intentionally outside this development step.");
        }

        private static void OpenPivot(Window owner)
        {
            DataDataset d = ChooseDataset(owner); if (d == null) return; string raw = Prompt(owner, "PIVOT BOUNDED SAMPLES", "dimension field | numeric measure field", (d.Fields.FirstOrDefault() == null ? "dimension" : d.Fields.First().Name) + " | " + (d.Fields.FirstOrDefault(x => x.InferredType == "NUMBER" || x.InferredType == "INTEGER") == null ? "measure" : d.Fields.First(x => x.InferredType == "NUMBER" || x.InferredType == "INTEGER").Name)); if (raw == null) return; string[] p = raw.Split('|'); string dim = At(p, 0, "").Trim(), measure = At(p, 1, "").Trim(); Dictionary<string, List<double>> groups = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase); foreach (DataRowSample s in d.Samples) { string dv, mv; double n; if (!s.Values.TryGetValue(dim, out dv) || !s.Values.TryGetValue(measure, out mv) || !Double.TryParse(mv, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out n)) continue; List<double> list; if (!groups.TryGetValue(dv ?? "", out list)) { list = new List<double>(); groups[dv ?? ""] = list; } list.Add(n); } List<object> rows = groups.OrderByDescending(x => x.Value.Count).Select(x => (object)((String.IsNullOrWhiteSpace(x.Key) ? "(blank)" : x.Key) + "   ·   n=" + x.Value.Count + "   ·   sum=" + x.Value.Sum().ToString("0.###", CultureInfo.InvariantCulture) + "   ·   mean=" + x.Value.Average().ToString("0.###", CultureInfo.InvariantCulture))).ToList(); OpenCollection(owner, "PIVOT / CUBE · " + d.Name, rows, "Exploratory aggregation over retained samples only; every result carries this sampling caveat.");
        }

        private static void OpenEvidenceTrace(Window owner) { DataDataset d = ChooseDataset(owner); if (d != null) OpenEvidenceFor(owner, d); }
        private static void OpenEvidenceFor(Window owner, DataDataset d) { List<object> rows = new List<object> { "DATASET   ·   " + d.ToString(), "FINGERPRINT   ·   " + d.DataFingerprint, "SOURCE   ·   " + (d.Origin == null ? "∅" : d.Origin.ToString()) }; rows.AddRange(d.Quality.Cast<object>()); rows.AddRange(Store.Relations.Where(x => Eq(x.LeftDatasetId, d.Id) || Eq(x.RightDatasetId, d.Id)).Cast<object>()); rows.AddRange(Store.Correlations.Where(x => Eq(x.DatasetId, d.Id)).Cast<object>()); rows.AddRange(Store.Contracts.Where(x => Eq(x.DatasetId, d.Id)).Cast<object>()); rows.AddRange(Store.Drifts.Where(x => Eq(x.BeforeDatasetId, d.Id) || Eq(x.AfterDatasetId, d.Id)).Cast<object>()); rows.AddRange(Store.Lineage.Where(x => Eq(x.DatasetId, d.Id) || (x.InputDatasetIds != null && x.InputDatasetIds.Contains(d.Id))).Cast<object>()); OpenCollection(owner, "EVIDENCE TRACE · " + d.Name, rows, "Linked artifacts all descend from retained source/data fingerprints."); }

        private static void OpenCapsules(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · DATA PREREGISTRATION CAPSULES", 1320, 840); DockPanel root = Shell("PREREGISTRATION CAPSULES", "Freeze a question, metrics, assumptions, bounds and exact data fingerprint before analysis."); Button create = Btn("FREEZE NEW CAPSULE", delegate { DataDataset d = ChooseDataset(w); if (d == null) return; string raw = Prompt(w, "CAPSULE", "question | metrics | assumptions | bounds", "Which fields and relations are stable enough for downstream analysis? | nullness,types,join-confidence,drift | samples are bounded evidence | no population claim beyond retained profile"); if (raw == null) return; string[] f = raw.Split('|'); DataCapsule c = DataFoundryKernel.Capsule(d, At(f, 0, "Data question"), At(f, 1, ""), At(f, 2, ""), At(f, 3, "")); Store.Capsules.Insert(0, c); Save(ctx); AppendLedger(ctx, "CAPSULE_FROZEN", c.Id, c.Question, d.DataFingerprint, c.CertificateHash); OpenCollection(w, "PREREGISTRATION CAPSULE", new List<object> { c }, "Immutable evidence commitment."); }); DockPanel.SetDock(create, Dock.Top); root.Children.Add(create); ListBox list = List(); list.ItemsSource = Store.Capsules; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenBaselines(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · IMMUTABLE DATA BASELINES", 1320, 840); DockPanel root = Shell("IMMUTABLE BASELINES", "Seal dataset fingerprint, row/field counts and quality for later comparison."); Button create = Btn("SEAL BASELINE", delegate { DataDataset d = ChooseDataset(w); if (d != null) CreateBaselineFor(w, ctx, d); }); DockPanel.SetDock(create, Dock.Top); root.Children.Add(create); ListBox list = List(); list.ItemsSource = Store.Baselines; root.Children.Add(list); w.Content = root; w.Show();
        }
        private static void CreateBaselineFor(Window owner, DeepSystemsContext ctx, DataDataset d) { string label = Prompt(owner, "BASELINE LABEL", "Label:", d.Name + " · baseline"); if (label == null) return; DataBaseline b = DataFoundryKernel.Baseline(d, label); Store.Baselines.Insert(0, b); Save(ctx); AppendLedger(ctx, "BASELINE_SEALED", b.Id, b.ToString(), d.DataFingerprint, b.CertificateHash); OpenCollection(owner, "BASELINE · " + label, new List<object> { b, "dataset fingerprint " + b.DatasetFingerprint, "certificate " + b.CertificateHash }, "Immutable comparison anchor."); }

        private static void OpenDossiers(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · DATA DECISION DOSSIERS", 1320, 840); DockPanel root = Shell("DECISION DOSSIERS", "Bind a data-dependent decision to exact evidence and caveats."); Button create = Btn("CREATE DOSSIER", delegate { DataDataset d = ChooseDataset(w); if (d == null) return; string raw = Prompt(w, "DOSSIER", "title | decision | evidence | caveats", "Data readiness decision | READY FOR EXPLORATORY ANALYSIS | profile,quality,relations,contract | bounded samples; domain review still required"); if (raw == null) return; string[] f = raw.Split('|'); DataDossier x = DataFoundryKernel.Dossier(d, At(f, 0, "Data decision"), At(f, 1, "UNDECIDED"), At(f, 2, ""), At(f, 3, "")); Store.Dossiers.Insert(0, x); Save(ctx); AppendLedger(ctx, "DOSSIER_CREATED", x.Id, x.Decision, d.DataFingerprint, x.CertificateHash); OpenCollection(w, "DOSSIER · " + x.Title, new List<object> { x, "evidence: " + x.Evidence, "caveats: " + x.Caveats }, "Decision remains linked to dataset fingerprint " + DataFoundryKernel.Short(d.DataFingerprint)); }); DockPanel.SetDock(create, Dock.Top); root.Children.Add(create); ListBox list = List(); list.ItemsSource = Store.Dossiers; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenLedger(Window owner, DeepSystemsContext ctx) { List<DataFoundryLedgerRecord> rows = ReadLedger(ctx, MaxLedgerRead); string verdict = VerifyLedger(rows); List<object> view = new List<object> { verdict }; view.AddRange(rows.OrderByDescending(x => x.Sequence).Cast<object>()); OpenCollection(owner, "TAMPER-EVIDENT FOUNDRY LEDGER", view, "Each entry commits to the previous entry hash plus event, subject, correlation ID, dataset fingerprint and certificate."); }

        private static void OpenHadal(Window owner)
        {
            DataDataset d = ChooseDataset(owner); if (d == null) return; List<object> rows = new List<object>(); rows.Add("DATASET → " + d); rows.Add("ORIGIN → " + (d.Origin == null ? "∅" : d.Origin.ToString())); foreach (DataFieldProfile f in d.Fields.Take(12)) rows.Add("FIELD → " + f); foreach (DataRowSample s in d.Samples.Take(8)) rows.Add("SAMPLE → " + s); foreach (DataQualityFinding q in d.Quality.Take(8)) rows.Add("QUALITY → " + q); foreach (DataRelation r in Store.Relations.Where(x => Eq(x.LeftDatasetId, d.Id) || Eq(x.RightDatasetId, d.Id)).Take(8)) rows.Add("RELATION → " + r); foreach (DataRecipe r in Store.Recipes.Where(x => Eq(x.SourceDatasetId, d.Id)).Take(4)) rows.Add("RECIPE → " + r); foreach (DataSchemaContract c in Store.Contracts.Where(x => Eq(x.DatasetId, d.Id)).Take(4)) rows.Add("CONTRACT → " + c); foreach (DataDriftReport r in Store.Drifts.Where(x => Eq(x.BeforeDatasetId, d.Id) || Eq(x.AfterDatasetId, d.Id)).Take(4)) rows.Add("DRIFT → " + r); foreach (DataLineageNode l in Store.Lineage.Where(x => Eq(x.DatasetId, d.Id) || (x.InputDatasetIds != null && x.InputDatasetIds.Contains(d.Id))).Take(8)) rows.Add("LINEAGE → " + l); OpenCollection(owner, "HADAL DATA DESCENT · " + d.Name, rows, "A navigable vertical slice through source, structure, evidence, inference, transformation and provenance.");
        }

        private static void OpenReturnWormholes(Window owner, DeepSystemsContext ctx)
        {
            Window w = W(owner, "YOMI · DATA FOUNDRY RETURN WORMHOLES", 1180, 820); WrapPanel p = new WrapPanel { Background = Bg, Margin = new Thickness(18) };
            Return(p, "WORLD KERNEL", delegate { WorldKernel.Open(owner, ctx); });
            Return(p, "CIVILIZATION LAYER", delegate { CivilizationLayer.Open(owner, ctx); });
            Return(p, "EPISTEMIC LOGIC FOUNDRY", delegate { EpistemicLogicFoundry.Open(owner, ctx); });
            Return(p, "AXIOMATIC VERIFICATION", delegate { AxiomaticVerificationReactor.Open(owner, ctx); });
            Return(p, "COUNTERFACTUAL STRATEGY", delegate { CounterfactualStrategySuperstructure.Open(owner, ctx); });
            Return(p, "SCIENTIFIC DISCOVERY", delegate { ScientificDiscoveryHyperstructure.Open(owner, ctx); });
            Return(p, "DYSON SYSTEMS ENGINEERING", delegate { DysonSystemsEngineeringMegastructure.Open(owner, ctx); });
            Return(p, "CYBERNETIC DIGITAL TWIN", delegate { CyberneticDigitalTwinMetastructure.Open(owner, ctx); });
            Return(p, "COMPLEX SYSTEMS", delegate { ComplexSystemsLaboratory.Open(owner, ctx); });
            Return(p, "OPERATIONS RESEARCH", delegate { OperationsResearchEmpire.Open(owner, ctx); });
            Return(p, "ECONOMIC CIVILIZATION", delegate { EconomicCivilization.Open(owner, ctx); });
            Return(p, "SPATIAL CARTOGRAPHY", delegate { SpatialWorldCartography.Open(owner, ctx); });
            Return(p, "DISTRIBUTED SYSTEMS", delegate { DistributedSystemsPlanetarium.Open(owner, ctx); });
            Return(p, "ADVERSARIAL SECURITY", delegate { AdversarialSecurityFortress.Open(owner, ctx); });
            w.Content = new ScrollViewer { Content = p, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; w.Show();
        }

        private static DataDataset ChooseDataset(Window owner) { return ChooseDataset(owner, "CHOOSE DATASET"); }
        private static DataDataset ChooseDataset(Window owner, string title)
        {
            EnsureLoadedOnly(); if (Store == null || Store.Datasets.Count == 0) return null; Window w = W(owner, "YOMI · " + title, 1180, 760); DockPanel root = Shell(title, "Double-click a retained dataset."); ListBox list = List(); list.ItemsSource = Store.Datasets; DataDataset selected = null; bool done = false; list.MouseDoubleClick += delegate { selected = list.SelectedItem as DataDataset; done = true; w.Close(); }; root.Children.Add(list); w.Content = root; w.ShowDialog(); return done ? selected : null;
        }
        private static DataFieldProfile ChooseField(Window owner, DataDataset d) { if (d == null || d.Fields.Count == 0) return null; Window w = W(owner, "YOMI · CHOOSE FIELD", 1050, 720); DockPanel root = Shell("CHOOSE FIELD", d.Name); ListBox list = List(); list.ItemsSource = d.Fields; DataFieldProfile selected = null; list.MouseDoubleClick += delegate { selected = list.SelectedItem as DataFieldProfile; w.Close(); }; root.Children.Add(list); w.Content = root; w.ShowDialog(); return selected; }
        private static DataRecipe ChooseRecipe(Window owner) { if (Store.Recipes.Count == 0) { MessageBox.Show(owner, "Create a recipe first.", "YOMI Data Foundry", MessageBoxButton.OK, MessageBoxImage.Information); return null; } Window w = W(owner, "YOMI · CHOOSE RECIPE", 1050, 720); DockPanel root = Shell("CHOOSE RECIPE", "Double-click a recipe."); ListBox list = List(); list.ItemsSource = Store.Recipes; DataRecipe selected = null; list.MouseDoubleClick += delegate { selected = list.SelectedItem as DataRecipe; w.Close(); }; root.Children.Add(list); w.Content = root; w.ShowDialog(); return selected; }
        private static DataSchemaContract ChooseContract(Window owner) { if (Store.Contracts.Count == 0) { MessageBox.Show(owner, "Create a schema contract first.", "YOMI Data Foundry", MessageBoxButton.OK, MessageBoxImage.Information); return null; } Window w = W(owner, "YOMI · CHOOSE CONTRACT", 1050, 720); DockPanel root = Shell("CHOOSE CONTRACT", "Double-click a contract."); ListBox list = List(); list.ItemsSource = Store.Contracts; DataSchemaContract selected = null; list.MouseDoubleClick += delegate { selected = list.SelectedItem as DataSchemaContract; w.Close(); }; root.Children.Add(list); w.Content = root; w.ShowDialog(); return selected; }

        private static void OpenCollection(Window owner, string title, List<object> rows, string note)
        {
            Window w = W(owner, "YOMI · " + title, 1360, 850); DockPanel root = Shell(title, note); TextBlock count = new TextBlock { Text = (rows == null ? 0 : rows.Count).ToString("N0", CultureInfo.InvariantCulture) + " retained item(s)", Foreground = Accent, Margin = new Thickness(20, 0, 20, 10) }; DockPanel.SetDock(count, Dock.Top); root.Children.Add(count); ListBox list = List(); list.ItemsSource = rows ?? new List<object>(); root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void Ensure(DeepSystemsContext ctx)
        {
            if (ctx == null) return; lock (Gate) { string root = ctx.StateRoot ?? ""; if (Store != null && Eq(LoadedRoot, root)) return; LoadedRoot = root; Store = Load(ctx); NormalizeStore(); }
        }
        private static void EnsureLoadedOnly() { if (Store == null) Store = new DataFoundryStore(); }
        private static DataFoundryStore Load(DeepSystemsContext ctx)
        {
            string path = StorePath(ctx), previous = path + ".previous"; Directory.CreateDirectory(Path.GetDirectoryName(path)); foreach (string candidate in new[] { path, previous }) if (File.Exists(candidate)) try { DataFoundryStore s = Json.Deserialize<DataFoundryStore>(File.ReadAllText(candidate, Encoding.UTF8)); if (s != null && s.Schema <= 1) return s; } catch { try { File.Copy(candidate, candidate + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), true); } catch { } } return new DataFoundryStore();
        }
        private static void NormalizeStore()
        {
            if (Store == null) Store = new DataFoundryStore(); if (Store.Datasets == null) Store.Datasets = new List<DataDataset>(); if (Store.Relations == null) Store.Relations = new List<DataRelation>(); if (Store.Correlations == null) Store.Correlations = new List<DataCorrelation>(); if (Store.Lineage == null) Store.Lineage = new List<DataLineageNode>(); if (Store.Recipes == null) Store.Recipes = new List<DataRecipe>(); if (Store.Contracts == null) Store.Contracts = new List<DataSchemaContract>(); if (Store.ContractResults == null) Store.ContractResults = new List<DataContractResult>(); if (Store.Drifts == null) Store.Drifts = new List<DataDriftReport>(); if (Store.Projections == null) Store.Projections = new List<DataProjectionReceipt>(); if (Store.Baselines == null) Store.Baselines = new List<DataBaseline>(); if (Store.Capsules == null) Store.Capsules = new List<DataCapsule>(); if (Store.Dossiers == null) Store.Dossiers = new List<DataDossier>(); foreach (DataDataset d in Store.Datasets) DataFoundryKernel.Normalize(d);
        }
        private static void TrimStore() { Store.Datasets = Store.Datasets.Take(MaxDatasets).ToList(); Store.Relations = Store.Relations.Take(MaxArtifacts).ToList(); Store.Correlations = Store.Correlations.Take(MaxArtifacts).ToList(); Store.Lineage = Store.Lineage.Take(MaxArtifacts).ToList(); Store.Recipes = Store.Recipes.Take(MaxArtifacts).ToList(); Store.Contracts = Store.Contracts.Take(MaxArtifacts).ToList(); Store.ContractResults = Store.ContractResults.Take(MaxArtifacts).ToList(); Store.Drifts = Store.Drifts.Take(MaxArtifacts).ToList(); Store.Projections = Store.Projections.Take(MaxArtifacts).ToList(); Store.Baselines = Store.Baselines.Take(MaxArtifacts).ToList(); Store.Capsules = Store.Capsules.Take(MaxArtifacts).ToList(); Store.Dossiers = Store.Dossiers.Take(MaxArtifacts).ToList(); }
        private static void Save(DeepSystemsContext ctx) { lock (Gate) { TrimStore(); Store.Revision++; Store.UpdatedUtc = Now(); Store.LastCommitId = DataFoundryKernel.NewId("DATA-COMMIT"); AtomicWrite(StorePath(ctx), Json.Serialize(Store)); } }
        private static void AtomicWrite(string path, string text) { Directory.CreateDirectory(Path.GetDirectoryName(path)); string temp = path + ".next", previous = path + ".previous"; File.WriteAllText(temp, text ?? "", new UTF8Encoding(false)); if (File.Exists(path)) { if (File.Exists(previous)) File.Delete(previous); File.Move(path, previous); } File.Move(temp, path); }
        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-data-foundry.json"); }
        private static string LedgerPath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-data-foundry-ledger.jsonl"); }

        private static void AppendLedger(DeepSystemsContext ctx, string evt, string subject, string detail, string fingerprint, string certificate)
        {
            lock (Gate) try { Directory.CreateDirectory(ctx.StateRoot); List<DataFoundryLedgerRecord> prior = ReadLedger(ctx, MaxLedgerRead); DataFoundryLedgerRecord last = prior.OrderByDescending(x => x.Sequence).FirstOrDefault(); DataFoundryLedgerRecord r = new DataFoundryLedgerRecord { Schema = 1, Sequence = last == null ? 1 : last.Sequence + 1, Utc = Now(), Event = Clip(evt, 120), Subject = Clip(subject, 512), Detail = Clip(detail, 131072), CorrelationId = Store == null ? "" : Store.LastCommitId, DatasetFingerprint = Clip(fingerprint, 128), CertificateHash = Clip(certificate, 128), PrevHash = last == null ? new string('0', 64) : last.EntryHash }; r.EntryHash = LedgerHash(r); byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(r) + Environment.NewLine); using (FileStream stream = new FileStream(LedgerPath(ctx), FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(); } } catch { }
        }
        private static List<DataFoundryLedgerRecord> ReadLedger(DeepSystemsContext ctx, int limit) { List<DataFoundryLedgerRecord> rows = new List<DataFoundryLedgerRecord>(); string path = LedgerPath(ctx); if (!File.Exists(path)) return rows; try { foreach (string line in File.ReadLines(path).Reverse().Take(limit).Reverse()) if (!String.IsNullOrWhiteSpace(line)) { try { DataFoundryLedgerRecord r = Json.Deserialize<DataFoundryLedgerRecord>(line); if (r != null) rows.Add(r); } catch { } } } catch { } return rows; }
        private static string LedgerHash(DataFoundryLedgerRecord r) { return DataFoundryKernel.HashText(r.Schema + "|" + r.Sequence + "|" + r.Utc + "|" + r.Event + "|" + r.Subject + "|" + r.Detail + "|" + r.CorrelationId + "|" + r.DatasetFingerprint + "|" + r.CertificateHash + "|" + r.PrevHash); }
        private static string VerifyLedger(List<DataFoundryLedgerRecord> rows) { if (rows.Count == 0) return "EMPTY LEDGER"; string prev = new string('0', 64); long seq = 1; foreach (DataFoundryLedgerRecord r in rows.OrderBy(x => x.Sequence)) { if (r.Sequence != seq || !Eq(r.PrevHash, prev) || !Eq(r.EntryHash, LedgerHash(r))) return "LEDGER VERIFICATION FAILED AT SEQUENCE " + r.Sequence.ToString(CultureInfo.InvariantCulture); prev = r.EntryHash; seq++; } return "LEDGER VERIFIED   ·   " + rows.Count.ToString(CultureInfo.InvariantCulture) + " chained entries   ·   head " + DataFoundryKernel.Short(prev); }

        private static DockPanel Shell(string title, string subtitle) { DockPanel root = new DockPanel { Background = Bg }; StackPanel head = new StackPanel { Margin = new Thickness(20, 18, 20, 12) }; head.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 26, FontWeight = FontWeights.Bold }); head.Children.Add(new TextBlock { Text = subtitle ?? "", Foreground = Muted, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) }); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head); return root; }
        private static Window W(Window owner, string title, double width, double height) { Window w = new Window { Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 900), MinHeight = Math.Min(height, 600), Background = Bg, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner }; return w; }
        private static ListBox List() { return new ListBox { Background = Surface, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(18), Padding = new Thickness(8), FontFamily = new FontFamily("Consolas"), FontSize = 12, HorizontalContentAlignment = HorizontalAlignment.Stretch }; }
        private static Button Btn(string text, Action action) { Button b = new Button { Content = text, Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(4), Padding = new Thickness(12, 7, 12, 7), FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand }; b.Click += delegate { action(); }; return b; }
        private static void Portal(WrapPanel panel, string badge, string title, string detail, Action action) { Border card = new Border { Width = 330, Height = 154, Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Margin = new Thickness(7), Padding = new Thickness(13) }; StackPanel s = new StackPanel(); s.Children.Add(new TextBlock { Text = badge + "   " + title, Foreground = Accent, FontWeight = FontWeights.Bold, FontSize = 14 }); s.Children.Add(new TextBlock { Text = detail, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8), Height = 68 }); Button open = Btn("OPEN", action); open.HorizontalAlignment = HorizontalAlignment.Left; s.Children.Add(open); card.Child = s; panel.Children.Add(card); }
        private static void SmallPortal(WrapPanel panel, string text, Action action) { Button b = Btn(text, action); b.MinWidth = 150; b.Margin = new Thickness(6); panel.Children.Add(b); }
        private static void Return(WrapPanel panel, string title, Action action) { Border card = new Border { Width = 260, Height = 88, Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), Margin = new Thickness(7), Padding = new Thickness(10) }; Button b = Btn(title, action); b.Foreground = Green; card.Child = b; panel.Children.Add(card); }
        private static string Prompt(Window owner, string title, string label, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 760, 300); DockPanel root = Shell(title, label); TextBox box = new TextBox { Text = initial ?? "", Background = Surface, Foreground = Text, BorderBrush = Border, Margin = new Thickness(20), Padding = new Thickness(10), FontSize = 13, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; DockPanel.SetDock(box, Dock.Top); root.Children.Add(box); StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(20) }; string result = null; Button ok = Btn("OK", delegate { result = box.Text; w.DialogResult = true; }); Button cancel = Btn("CANCEL", delegate { w.DialogResult = false; }); buttons.Children.Add(ok); buttons.Children.Add(cancel); DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons); w.Content = root; bool? accepted = w.ShowDialog(); return accepted == true ? result : null;
        }
        private static Brush B(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }
        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static string At(string[] a, int i, string fallback) { return a != null && i >= 0 && i < a.Length ? a[i] : fallback; }
        private static string Clip(string s, int max) { return DataFoundryKernel.Clip(s, max); }
    }
}

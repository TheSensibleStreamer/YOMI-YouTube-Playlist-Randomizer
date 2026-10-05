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
    internal sealed class WorldNode
    {
        public string Id;
        public string Kind;
        public string Label;
        public string Summary;
        public string SourcePath;
        public string JsonPath;
        public string EvidenceClass;
        public string Fingerprint;
        public DateTime ModifiedUtc;
        public object Value;
        public Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<string> IdentityTokens = new List<string>();
        public override string ToString()
        {
            return (Kind ?? "OBJECT") + "   " + (Label ?? Id ?? "") + (String.IsNullOrWhiteSpace(Summary) ? "" : "   ·   " + Summary);
        }
    }

    internal sealed class WorldEdge
    {
        public string Id;
        public string FromId;
        public string ToId;
        public string Kind;
        public string Evidence;
        public string EvidenceClass;
        public int Strength;
        public override string ToString()
        {
            return (Kind ?? "RELATED") + "   " + Short(FromId) + " → " + Short(ToId) + "   ·   " + (Evidence ?? "");
        }
        private static string Short(string s) { return String.IsNullOrWhiteSpace(s) ? "∅" : (s.Length <= 18 ? s : s.Substring(0, 18)); }
    }

    internal sealed class WorldSourceStat
    {
        public string Path;
        public string Kind;
        public long Bytes;
        public DateTime ModifiedUtc;
        public int Nodes;
        public int Edges;
        public string Status;
        public override string ToString()
        {
            return (Kind ?? "SOURCE") + "   " + System.IO.Path.GetFileName(Path ?? "") + "   ·   " + Nodes.ToString(CultureInfo.InvariantCulture) + " nodes   ·   " + (Status ?? "OK");
        }
    }

    internal sealed class WorldIndex
    {
        public string BuiltUtc;
        public string Fingerprint;
        public string SourceSignature;
        public int SourcesScanned;
        public int SourcesSkipped;
        public bool Truncated;
        public Dictionary<string, WorldNode> Nodes = new Dictionary<string, WorldNode>(StringComparer.OrdinalIgnoreCase);
        public List<WorldEdge> Edges = new List<WorldEdge>();
        public HashSet<string> EdgeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, WorldEdge> EdgeById = new Dictionary<string, WorldEdge>(StringComparer.OrdinalIgnoreCase);
        public List<WorldSourceStat> Sources = new List<WorldSourceStat>();
        public Dictionary<string, List<string>> Out = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> In = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> IdentityNodeByToken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HashSet<string>> SchemaKinds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HashSet<string>> SchemaTypes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class WorldLens
    {
        public string Id;
        public string Name;
        public string Query;
        public string SavedUtc;
        public int OpenCount;
        public string LastOpenedUtc;
        public override string ToString() { return (Name ?? "LENS") + "   ·   " + (Query ?? ""); }
    }

    internal sealed class WorldEpoch
    {
        public string Id;
        public string CapturedUtc;
        public string Fingerprint;
        public int Nodes;
        public int Edges;
        public int Sources;
        public Dictionary<string, string> NodeFingerprints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public override string ToString()
        {
            return (CapturedUtc ?? "") + "   ·   " + Nodes.ToString(CultureInfo.InvariantCulture) + " nodes   ·   " + Edges.ToString(CultureInfo.InvariantCulture) + " edges   ·   " + Short(Fingerprint);
        }
        private static string Short(string s) { return String.IsNullOrWhiteSpace(s) ? "∅" : (s.Length <= 12 ? s : s.Substring(0, 12)); }
    }

    internal sealed class WorldKernelStore
    {
        public int Schema = 1;
        public List<WorldLens> Lenses = new List<WorldLens>();
        public List<string> QueryHistory = new List<string>();
        public List<WorldEpoch> Epochs = new List<WorldEpoch>();
    }

    internal sealed class WorldQueryResult
    {
        public string Query;
        public string Explanation;
        public List<WorldNode> Nodes = new List<WorldNode>();
        public List<WorldEdge> Edges = new List<WorldEdge>();
    }

    internal static class WorldKernel
    {
        private static readonly Brush Bg = B("#07090D");
        private static readonly Brush Surface = B("#0F141B");
        private static readonly Brush Raised = B("#18212B");
        private static readonly Brush Border = B("#33404D");
        private static readonly Brush Text = B("#F2F6FA");
        private static readonly Brush Muted = B("#98A7B7");
        private static readonly Brush Accent = B("#77F0BE");
        private static readonly Brush Blue = B("#6DBBFF");
        private static readonly Brush Amber = B("#FFD36E");
        private static readonly Brush Violet = B("#C7A6FF");
        private static readonly Brush Danger = B("#FF7D8A");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 256 };
        private static readonly object Gate = new object();
        private static WorldIndex Cached;
        private static string CachedContextRoot;
        private static WorldKernelStore Store;
        private static string StoreRoot;

        private const int MaxTextSources = 256;
        private const int MaxMediaObjects = 4096;
        private const int MaxNodes = 8192;
        private const int MaxEdges = 24576;
        private const int MaxEntityProperties = 40;
        private const int MaxJsonDepth = 18;
        private const int MaxJsonBytes = 4 * 1024 * 1024;
        private const int MaxJournalTailBytes = 768 * 1024;

        private static readonly string[] IdentityKeys = new string[]
        {
            "intent_id","session_id","source_key","occurrence_id","order_revision","entry_hash","prev_hash","plan_fingerprint",
            "branch_id","parent_branch_id","root_branch_id","mission_id","task_id","parent_task_id","parent_mission_id","case_id",
            "descent_id","parent_id","root_id","fingerprint","route_fingerprint","generation","work_generation","decision_reason","route_label"
        };

        private static readonly HashSet<string> IdentityKeySet = new HashSet<string>(IdentityKeys, StringComparer.OrdinalIgnoreCase);

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            LoadStore(ctx);
            WorldIndex index = GetIndex(ctx, false);
            OpenHub(owner, ctx, index);
        }

        public static void OpenFromExternal(Window owner, DeepSystemsContext ctx, string title, object value, string sourcePath, string jsonPath, string route)
        {
            if (ctx == null) return;
            LoadStore(ctx);
            WorldIndex index = GetIndex(ctx, false);
            WorldNode external = CreateExternalNode(index, title, value, sourcePath, jsonPath, route);
            OpenDossier(owner, ctx, index, external.Id);
        }

        // DEV13.37.25 cumulative Civilization, Epistemic Logic, Verification, Strategy, Scientific Discovery, Dyson Systems Engineering, Cybernetic Digital Twin, Complex Systems, Operations Research, Economic Civilization, Spatial World Cartography, Distributed Systems Planetarium, Adversarial Security Fortress, Data Foundry and Software Genome Observatory bridges. The Kernel remains the graph authority;
        // higher organizational layers receive read-only index/query/navigation capabilities only.
        internal static WorldIndex ExternalIndex(DeepSystemsContext ctx, bool force) { return GetIndex(ctx, force); }
        internal static WorldNode ExternalRegisterObject(DeepSystemsContext ctx, string title, object value, string sourcePath, string jsonPath, string route)
        {
            if (ctx == null) return null;
            LoadStore(ctx);
            WorldIndex index = GetIndex(ctx, false);
            return CreateExternalNode(index, title, value, sourcePath, jsonPath, route);
        }
        internal static WorldQueryResult ExternalQuery(WorldIndex index, string query) { return ExecuteQuery(index, query); }
        internal static void ExternalOpenDossier(Window owner, DeepSystemsContext ctx, WorldIndex index, string id) { OpenDossier(owner, ctx, index, id); }
        internal static List<WorldNode> ExternalNeighbors(WorldIndex index, string id) { return NeighborNodes(index, id).ToList(); }
        internal static int ExternalDegree(WorldIndex index, string id) { return Degree(index, id); }
        internal static List<string> ExternalShortestPath(WorldIndex index, string a, string b, int maxHops) { return ShortestPath(index, a, b, maxHops); }

        private static void OpenHub(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Window w = W(owner, "YOMI · UNIVERSAL WORLD KERNEL", 1180, 800);
            DockPanel root = new DockPanel { Margin = new Thickness(18) };
            StackPanel head = new StackPanel();
            head.Children.Add(T("UNIVERSAL WORLD KERNEL", 28, Text, FontWeights.Bold));
            head.Children.Add(T("Convergence core · one graph spanning YOMI's engine projections, deep worlds, research, productivity, provenance, temporal branches and media identities.", 13, Muted, FontWeights.Normal));
            TextBlock stat = T(IndexSummary(index), 11.5, Blue, FontWeights.SemiBold); stat.Margin = new Thickness(0, 8, 0, 10); head.Children.Add(stat);
            WrapPanel controls = new WrapPanel();
            controls.Children.Add(Btn("QUERY CONSOLE", delegate { OpenQueryConsole(w, ctx, GetIndex(ctx, false), null); }));
            controls.Children.Add(Btn("REBUILD WORLD INDEX", delegate { index = GetIndex(ctx, true); stat.Text = IndexSummary(index); }));
            controls.Children.Add(Btn("CAPTURE WORLD EPOCH", delegate { CaptureEpoch(w, ctx, GetIndex(ctx, true)); }));
            controls.Children.Add(Btn("DEEP SYSTEMS ATLAS", delegate { DeepSystems.OpenAtlasFromContext(w); }));
            head.Children.Add(controls);
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            WrapPanel cards = new WrapPanel();
            Portal(cards, "YQL", "QUERY CONSOLE", "Search the unified world graph by kind, identity, text, field, neighbor relation or shortest path. Save reusable lenses.", delegate { OpenQueryConsole(w, ctx, GetIndex(ctx, false), null); });
            Portal(cards, "CIV", "CIVILIZATION LAYER", "Build schema-2 institutions, constitutional memory, resolutions, Knowledge Commons, plurality forums, border cartography, treaty constellations, relay routes, compacts and census epochs over the universal world model.", delegate { CivilizationLayer.Open(w, ctx); });
            Portal(cards, "⊢4V", "EPISTEMIC LOGIC FOUNDRY", "Construct proof DAGs, contradiction-tolerant theories, modal counterfactuals, abductions, falsifiers and dominance analyses directly over World Passport evidence.", delegate { EpistemicLogicFoundry.Open(w, ctx); });
            Portal(cards, "⊨CTL", "AXIOMATIC VERIFICATION REACTOR", "Forge evidence-anchored finite-state models, check CTL obligations, minimize counterexamples and issue model-bound verification certificates.", delegate { AxiomaticVerificationReactor.Open(w, ctx); });
            Portal(cards, "do(X)", "COUNTERFACTUAL STRATEGY SUPERSTRUCTURE", "Forge evidence-bound structural causal hypotheses, compare explicit interventions across twin worlds and retain robust model-bound strategy dossiers.", delegate { CounterfactualStrategySuperstructure.Open(w, ctx); });
            Portal(cards, "I(H;Y)", "SCIENTIFIC DISCOVERY HYPERSTRUCTURE", "Project World passports into bounded measurement protocols, competing hypotheses and preregistered experiment designs without treating identity or provenance as randomized causes.", delegate { ScientificDiscoveryHyperstructure.Open(w, ctx); });
            Portal(cards, "ΣSYS", "DYSON SYSTEMS ENGINEERING MEGASTRUCTURE", "Transform evidence passports into bounded requirements, architecture components, interface contracts, budgets, hazards, verification links and configuration baselines without granting the projection runtime authority.", delegate { DysonSystemsEngineeringMegastructure.Open(w, ctx); });
            Portal(cards, "ΣPOP", "COMPLEX SYSTEMS LABORATORY", "Project World passports into heterogeneous populations and interaction networks for bounded emergence, cascade, resilience and phase-transition analysis without converting identity metadata into behavioral truth.", delegate { ComplexSystemsLaboratory.Open(w, ctx); });
            Portal(cards, "ORΩ", "OPERATIONS RESEARCH EMPIRE", "Project stable World passports into explicit locations, networks, demands, resources and bounded optimization models without inventing operational truth from metadata.", delegate { OperationsResearchEmpire.Open(w, ctx); });
            Portal(cards, "ECONΩ", "ECONOMIC CIVILIZATION", "Project stable World passports into fingerprint-bound households, firms, goods, production, credit, government, trade and distribution worlds without inventing measured economic truth.", delegate { EconomicCivilization.Open(w, ctx); });
            Portal(cards, "GEOΩ", "SPATIAL WORLD CARTOGRAPHY", "Project stable World passports into fingerprint-bound places, territories, infrastructure, routes and movement worlds without inventing surveyed geographic truth.", delegate { SpatialWorldCartography.Open(w, ctx); });
            Portal(cards, "DISTΩ", "DISTRIBUTED SYSTEMS PLANETARIUM", "Project stable World passports into fingerprint-bound nodes, services, messages, replicas, quorums and failure worlds without inventing production topology.", delegate { DistributedSystemsPlanetarium.Open(w, ctx); });
            Portal(cards, "OMNI", "UNIVERSAL OBJECT INDEX", "Every indexed object gets a stable passport, fingerprint, source coordinate and inbound/outbound relationship neighborhood.", delegate { OpenObjectIndex(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "ID", "IDENTITY NEXUS", "High-degree semantic identities connecting otherwise separate YOMI subsystems: occurrence, source, intent, mission, hash, branch, task and session.", delegate { OpenIdentityNexus(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "Σ", "SCHEMA CARTOGRAPHY", "Inferred entity kinds, field vocabularies and observed data types across the indexed evidence universe.", delegate { OpenSchemaRegistry(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "EDGE", "RELATIONSHIP TAXONOMY", "Count and inspect CONTAINS, HAS_IDENTITY, REFERENCES and object-store relationship families.", delegate { OpenEdgeTaxonomy(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "MAP", "CONNECTED WORLDS", "Connected components, high-degree hubs, isolated artifacts and cross-system convergence zones.", delegate { OpenTopology(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "Δ", "WORLD EPOCH VAULT", "Capture bounded fingerprints of the entire graph and compare epochs: added, removed and materially changed objects.", delegate { OpenEpochVault(w, ctx); });
            Portal(cards, "LENS", "SAVED LENS VAULT", "Persistent YQL lenses and query history. Reopen a recurring investigative perspective without reconstructing it.", delegate { OpenLensVault(w, ctx); });
            Portal(cards, "SRC", "SOURCE CATALOG", "See exactly which files and namespaces contributed to the current world model, including skipped/truncated evidence.", delegate { OpenSourceCatalog(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "DIAG", "CONVERGENCE DIAGNOSTICS", "Graph-integrity diagnostics: identity superhubs, orphans, self-reference, duplicate coordinates and unresolved references. Observations, not verdicts.", delegate { OpenDiagnostics(w, ctx, GetIndex(ctx, false)); });
            Portal(cards, "HADAL", "DESCENT INTERFACE", "Drop from the unified graph back into persistent Hadal investigations, consequence horizons, divergence, null space and fossils.", delegate { AbyssalKnowledgeEngine.Open(w, ctx); });
            Portal(cards, "R&D", "RESEARCH BRIDGE", "Turn graph observations into explicit research cases without allowing organization or hypothesis metadata to become evidence truth.", delegate { ResearchWorkbench.Open(w, ctx); });
            scroll.Content = cards; root.Children.Add(scroll); w.Content = root; w.Show();
        }

        private static WorldNode CreateExternalNode(WorldIndex index, string title, object value, string sourcePath, string jsonPath, string route)
        {
            string serialized = SafeSerialize(value);
            string coordinate = (sourcePath ?? "external") + "|" + (jsonPath ?? "$external");
            string id = "yomi://external/" + Hash(coordinate.ToLowerInvariant()).Substring(0, 24);
            RemoveNodeEdges(index, id);
            WorldNode n = new WorldNode
            {
                Id = id, Kind = "EXTERNAL_OBJECT", Label = title ?? "EXTERNAL OBJECT", Summary = route ?? "External deep-system coordinate",
                SourcePath = sourcePath, JsonPath = jsonPath ?? "$external", EvidenceClass = "OBSERVED", Fingerprint = Hash(serialized), Value = value,
                ModifiedUtc = SafeWrite(sourcePath)
            };
            ExtractIdentityTokens(value, n.IdentityTokens, 0);
            index.Nodes[id] = n;
            foreach (string token in n.IdentityTokens.Distinct(StringComparer.OrdinalIgnoreCase)) LinkIdentity(index, n, token, sourcePath ?? "external");
            BuildAdjacency(index); RecomputeFingerprint(index);
            return n;
        }

        private static WorldIndex GetIndex(DeepSystemsContext ctx, bool force)
        {
            lock (Gate)
            {
                string signature = ComputeSourceSignature(ctx);
                if (!force && Cached != null && String.Equals(CachedContextRoot, ctx.DataRoot, StringComparison.OrdinalIgnoreCase) && String.Equals(Cached.SourceSignature, signature, StringComparison.Ordinal)) return Cached;
                Cached = BuildIndex(ctx, signature);
                CachedContextRoot = ctx.DataRoot;
                return Cached;
            }
        }

        private static WorldIndex BuildIndex(DeepSystemsContext ctx, string sourceSignature)
        {
            WorldIndex index = new WorldIndex { BuiltUtc = Now(), SourceSignature = sourceSignature };
            List<string> sources = EnumerateTextSources(ctx).Take(MaxTextSources).ToList();
            index.SourcesScanned = sources.Count;
            foreach (string path in sources)
            {
                if (index.Nodes.Count >= MaxNodes || index.Edges.Count >= MaxEdges) { index.Truncated = true; break; }
                IngestSource(index, ctx, path);
            }
            IngestMediaObjects(index, ctx);
            BuildAdjacency(index);
            RecomputeFingerprint(index);
            return index;
        }

        private static void IngestSource(WorldIndex index, DeepSystemsContext ctx, string path)
        {
            WorldSourceStat stat = new WorldSourceStat { Path = path, Kind = ExtensionKind(path), ModifiedUtc = SafeWrite(path), Status = "OK" };
            try { stat.Bytes = new FileInfo(path).Length; } catch { }
            int beforeNodes = index.Nodes.Count, beforeEdges = index.Edges.Count;
            WorldNode file = AddFileNode(index, ctx, path);
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".json")
                {
                    if (stat.Bytes > MaxJsonBytes) { stat.Status = "SKIPPED · JSON > 4 MiB"; index.SourcesSkipped++; }
                    else
                    {
                        object root = Json.DeserializeObject(ReadShared(path));
                        IngestValue(index, file, root, path, "$", 0);
                    }
                }
                else if (ext == ".jsonl")
                {
                    string text = ReadTail(path, MaxJournalTailBytes);
                    int lineNo = 0;
                    foreach (string line in SplitLines(text))
                    {
                        if (String.IsNullOrWhiteSpace(line)) continue; lineNo++;
                        try { object row = Json.DeserializeObject(line); IngestValue(index, file, row, path, "$tail[" + lineNo.ToString(CultureInfo.InvariantCulture) + "]", 0); }
                        catch { }
                        if (index.Nodes.Count >= MaxNodes || index.Edges.Count >= MaxEdges) break;
                    }
                    if (stat.Bytes > MaxJournalTailBytes) stat.Status = "TAIL INDEX · " + FormatBytes(MaxJournalTailBytes);
                }
                else
                {
                    string text = ReadTail(path, Math.Min(MaxJournalTailBytes, 256 * 1024));
                    ExtractTextIdentities(index, file, text, path);
                }
            }
            catch (Exception ex) { stat.Status = "PARSE WARNING · " + Trunc(ex.Message, 90); }
            stat.Nodes = index.Nodes.Count - beforeNodes; stat.Edges = index.Edges.Count - beforeEdges; index.Sources.Add(stat);
        }

        private static WorldNode AddFileNode(WorldIndex index, DeepSystemsContext ctx, string path)
        {
            string rel = Relative(ctx.DataRoot, path);
            string id = "yomi://file/" + Hash(rel.ToLowerInvariant()).Substring(0, 24);
            WorldNode existing; if (index.Nodes.TryGetValue(id, out existing)) return existing;
            long bytes = 0; try { bytes = new FileInfo(path).Length; } catch { }
            WorldNode n = new WorldNode
            {
                Id = id, Kind = "FILE", Label = Path.GetFileName(path), Summary = rel, SourcePath = path, JsonPath = "$file", EvidenceClass = "OBSERVED",
                Fingerprint = Hash(rel + "|" + bytes.ToString(CultureInfo.InvariantCulture) + "|" + SafeWrite(path).Ticks.ToString(CultureInfo.InvariantCulture)), ModifiedUtc = SafeWrite(path)
            };
            n.Properties["relative_path"] = rel; n.Properties["bytes"] = bytes.ToString(CultureInfo.InvariantCulture); n.Properties["extension"] = Path.GetExtension(path).ToLowerInvariant();
            index.Nodes[id] = n; return n;
        }

        private static void IngestValue(WorldIndex index, WorldNode file, object value, string path, string jsonPath, int depth)
        {
            if (depth > MaxJsonDepth || index.Nodes.Count >= MaxNodes || index.Edges.Count >= MaxEdges || value == null) return;
            Dictionary<string, object> map = value as Dictionary<string, object>;
            if (map != null)
            {
                bool entity = depth == 0 || map.Keys.Any(k => IdentityKeySet.Contains(k)) || map.Keys.Any(k => k.EndsWith("_id", StringComparison.OrdinalIgnoreCase));
                WorldNode parent = file;
                if (entity)
                {
                    parent = AddEntityNode(index, file, map, path, jsonPath);
                    if (!String.Equals(parent.Id, file.Id, StringComparison.OrdinalIgnoreCase)) AddEdge(index, file.Id, parent.Id, "CONTAINS", jsonPath, "OBSERVED", 100);
                }
                int ordinal = 0;
                foreach (KeyValuePair<string, object> kv in map)
                {
                    if (++ordinal > 256) break;
                    object child = kv.Value;
                    string childPath = AppendPath(jsonPath, kv.Key);
                    if (IsScalar(child))
                    {
                        string scalar = Convert.ToString(child, CultureInfo.InvariantCulture) ?? "";
                        if (parent.Properties.Count < MaxEntityProperties) parent.Properties[kv.Key] = Trunc(scalar, 500);
                        if (IdentityKeySet.Contains(kv.Key) || kv.Key.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                        {
                            string semantic = NormalizeIdentityKey(parent.Kind, kv.Key);
                            string token = semantic + "=" + scalar;
                            if (!String.IsNullOrWhiteSpace(scalar)) { AddUnique(parent.IdentityTokens, token); LinkIdentity(index, parent, token, childPath); if (IsReferenceKey(kv.Key)) AddReferenceEdge(index, parent, token, kv.Key + " @ " + childPath); }
                        }
                    }
                    else
                    {
                        if (IsReferenceCollectionKey(kv.Key)) IngestReferenceCollection(index, parent, kv.Key, child, childPath);
                        IngestValue(index, parent, child, path, childPath, depth + 1);
                    }
                    if (index.Nodes.Count >= MaxNodes || index.Edges.Count >= MaxEdges) break;
                }
                RegisterSchema(index, parent);
                return;
            }
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                int i = 0;
                foreach (object child in enumerable)
                {
                    if (i >= 512) break;
                    IngestValue(index, file, child, path, jsonPath + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", depth + 1); i++;
                    if (index.Nodes.Count >= MaxNodes || index.Edges.Count >= MaxEdges) break;
                }
            }
        }

        private static WorldNode AddEntityNode(WorldIndex index, WorldNode file, Dictionary<string, object> map, string path, string jsonPath)
        {
            string kind = InferKind(map, path);
            List<string> identities = new List<string>();
            foreach (KeyValuePair<string, object> kv in map)
            {
                if (!IsScalar(kv.Value)) continue;
                if (IdentityKeySet.Contains(kv.Key) || kv.Key.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                {
                    string val = Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? "";
                    if (!String.IsNullOrWhiteSpace(val)) identities.Add(NormalizeIdentityKey(kind, kv.Key) + "=" + val);
                }
            }
            string coord = RelativeRoot(path) + "|" + jsonPath;
            string id = "yomi://" + kind.ToLowerInvariant() + "/" + Hash(coord).Substring(0, 24);
            WorldNode existing;
            if (index.Nodes.TryGetValue(id, out existing)) return existing;
            string label = BestLabel(map, kind, identities);
            string serialized = SafeSerialize(map);
            WorldNode n = new WorldNode
            {
                Id = id, Kind = kind, Label = label, Summary = Path.GetFileName(path) + " " + jsonPath, SourcePath = path, JsonPath = jsonPath,
                EvidenceClass = "OBSERVED", Fingerprint = Hash(serialized), ModifiedUtc = SafeWrite(path), Value = map
            };
            foreach (string token in identities) AddUnique(n.IdentityTokens, token);
            int count = 0;
            foreach (KeyValuePair<string, object> kv in map)
            {
                if (++count > MaxEntityProperties) break;
                if (IsScalar(kv.Value)) { n.Properties[kv.Key] = Trunc(Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? "", 500); RegisterFieldType(index, kind, kv.Key, kv.Value); }
            }
            index.Nodes[id] = n;
            foreach (string token in n.IdentityTokens) LinkIdentity(index, n, token, jsonPath);
            return n;
        }

        private static void AddReferenceEdge(WorldIndex index, WorldNode entity, string token, string evidence)
        {
            string identityId; if (!index.IdentityNodeByToken.TryGetValue(token, out identityId)) { LinkIdentity(index, entity, token, evidence); index.IdentityNodeByToken.TryGetValue(token, out identityId); } if (!String.IsNullOrWhiteSpace(identityId)) AddEdge(index, entity.Id, identityId, "REFERENCES", evidence, "OBSERVED", 100);
        }

        private static void IngestReferenceCollection(WorldIndex index, WorldNode entity, string key, object raw, string evidence)
        {
            IEnumerable seq = raw as IEnumerable; if (seq == null || raw is string) return; string semantic = ReferenceCollectionIdentity(entity.Kind, key); if (String.IsNullOrWhiteSpace(semantic)) return; int n = 0; foreach (object x in seq) { if (n++ >= 128) break; if (!IsScalar(x)) continue; string value = Convert.ToString(x, CultureInfo.InvariantCulture) ?? ""; if (String.IsNullOrWhiteSpace(value)) continue; string token = semantic + "=" + value; AddReferenceEdge(index, entity, token, key + " @ " + evidence); }
        }

        private static bool IsReferenceKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key)) return false; return key.StartsWith("parent_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("root_", StringComparison.OrdinalIgnoreCase) || key.EndsWith("_ref", StringComparison.OrdinalIgnoreCase) || key.EndsWith("_reference", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReferenceCollectionKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key)) return false; string k = key.ToLowerInvariant(); return k.Contains("depend") || k.Contains("prereq") || k == "parents" || k == "children" || k.EndsWith("_ids");
        }

        private static string ReferenceCollectionIdentity(string kind, string key)
        {
            string k = (key ?? "").ToLowerInvariant(); if (k.Contains("depend") || k.Contains("prereq")) return "task_id"; if (k.Contains("mission")) return "mission_id"; if (k.Contains("branch")) return "branch_id"; if (k.Contains("case")) return "case_id"; if (k.Contains("descent")) return "descent_id"; if ((kind ?? "").IndexOf("TASK", StringComparison.OrdinalIgnoreCase) >= 0) return "task_id"; return "id";
        }

        private static void LinkIdentity(WorldIndex index, WorldNode entity, string token, string evidence)
        {
            if (String.IsNullOrWhiteSpace(token) || index.Edges.Count >= MaxEdges) return;
            string identityId;
            if (!index.IdentityNodeByToken.TryGetValue(token, out identityId))
            {
                string key = TokenKey(token), value = TokenValue(token);
                identityId = "yomi://identity/" + key.ToLowerInvariant() + "/" + Hash(value).Substring(0, 24);
                index.IdentityNodeByToken[token] = identityId;
                WorldNode idNode = new WorldNode { Id = identityId, Kind = "IDENTITY", Label = token, Summary = "Semantic identity hub", EvidenceClass = "INFERRED", Fingerprint = Hash(token), ModifiedUtc = entity.ModifiedUtc };
                idNode.Properties["identity_key"] = key; idNode.Properties["identity_value"] = value; idNode.IdentityTokens.Add(token); index.Nodes[identityId] = idNode;
            }
            AddEdge(index, entity.Id, identityId, "HAS_IDENTITY", evidence, "OBSERVED", 100);
        }

        private static void AddEdge(WorldIndex index, string from, string to, string kind, string evidence, string evidenceClass, int strength)
        {
            if (String.IsNullOrWhiteSpace(from) || String.IsNullOrWhiteSpace(to) || index.Edges.Count >= MaxEdges) return;
            string key = from + "|" + to + "|" + kind;
            string id = "edge:" + Hash(key).Substring(0, 24);
            if (!index.EdgeIds.Add(id)) return;
            WorldEdge edge = new WorldEdge { Id = id, FromId = from, ToId = to, Kind = kind, Evidence = evidence, EvidenceClass = evidenceClass, Strength = strength };
            index.Edges.Add(edge); index.EdgeById[id] = edge;
        }

        private static void ExtractTextIdentities(WorldIndex index, WorldNode file, string text, string path)
        {
            if (String.IsNullOrEmpty(text)) return;
            foreach (string key in IdentityKeys)
            {
                int start = 0, hits = 0;
                while (hits < 24)
                {
                    int at = text.IndexOf(key, start, StringComparison.OrdinalIgnoreCase); if (at < 0) break;
                    int eq = text.IndexOfAny(new char[] { '=', ':', ' ' }, at + key.Length); if (eq < 0) break;
                    int p = eq + 1; while (p < text.Length && (Char.IsWhiteSpace(text[p]) || text[p] == '"' || text[p] == '\'' || text[p] == '=' || text[p] == ':')) p++;
                    int q = p; while (q < text.Length && q - p < 160 && !Char.IsWhiteSpace(text[q]) && text[q] != '"' && text[q] != '\'' && text[q] != ',' && text[q] != '}' && text[q] != ']') q++;
                    if (q > p)
                    {
                        string val = text.Substring(p, q - p).Trim(); if (val.Length >= 2) { string token = key + "=" + val; AddUnique(file.IdentityTokens, token); LinkIdentity(index, file, token, path); }
                    }
                    start = Math.Max(q, at + key.Length); hits++;
                }
            }
        }

        private static void IngestMediaObjects(WorldIndex index, DeepSystemsContext ctx)
        {
            string root = Path.Combine(ctx.DataRoot, "cache", "objects");
            if (!Directory.Exists(root)) return;
            int count = 0;
            foreach (string path in SafeFiles(root, "*", SearchOption.AllDirectories).OrderByDescending(SafeWrite))
            {
                if (count++ >= MaxMediaObjects || index.Nodes.Count >= MaxNodes) { index.Truncated = true; break; }
                string rel = Relative(ctx.DataRoot, path); string id = "yomi://media/" + Hash(rel.ToLowerInvariant()).Substring(0, 24);
                if (index.Nodes.ContainsKey(id)) continue;
                FileInfo fi; try { fi = new FileInfo(path); } catch { continue; }
                WorldNode n = new WorldNode { Id = id, Kind = "MEDIA_OBJECT", Label = fi.Name, Summary = Relative(root, path), SourcePath = path, JsonPath = "$binary", EvidenceClass = "OBSERVED", Fingerprint = Hash(rel + "|" + fi.Length.ToString(CultureInfo.InvariantCulture) + "|" + fi.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)), ModifiedUtc = fi.LastWriteTimeUtc };
                n.Properties["bytes"] = fi.Length.ToString(CultureInfo.InvariantCulture); n.Properties["extension"] = fi.Extension.ToLowerInvariant(); n.Properties["namespace"] = Relative(root, fi.DirectoryName ?? root);
                index.Nodes[id] = n;
                string sourceKey = GuessSourceKey(fi.Name); if (!String.IsNullOrWhiteSpace(sourceKey)) { string token = "source_key=" + sourceKey; n.IdentityTokens.Add(token); LinkIdentity(index, n, token, path); }
            }
        }

        private static void RegisterSchema(WorldIndex index, WorldNode n)
        {
            HashSet<string> fields; if (!index.SchemaKinds.TryGetValue(n.Kind ?? "OBJECT", out fields)) { fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase); index.SchemaKinds[n.Kind ?? "OBJECT"] = fields; }
            foreach (string k in n.Properties.Keys) fields.Add(k);
        }

        private static void RegisterFieldType(WorldIndex index, string kind, string field, object value)
        {
            string key = (kind ?? "OBJECT") + "." + (field ?? "field"); HashSet<string> types; if (!index.SchemaTypes.TryGetValue(key, out types)) { types = new HashSet<string>(StringComparer.OrdinalIgnoreCase); index.SchemaTypes[key] = types; } types.Add(TypeName(value));
        }

        private static string TypeName(object value)
        {
            if (value == null) return "null"; if (value is string) return "string"; if (value is bool) return "bool"; if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong) return "integer"; if (value is float || value is double || value is decimal) return "number"; if (value is DateTime) return "datetime"; return value.GetType().Name;
        }

        private static void RemoveNodeEdges(WorldIndex index, string nodeId)
        {
            if (String.IsNullOrWhiteSpace(nodeId)) return; List<WorldEdge> remove = index.Edges.Where(e => String.Equals(e.FromId, nodeId, StringComparison.OrdinalIgnoreCase) || String.Equals(e.ToId, nodeId, StringComparison.OrdinalIgnoreCase)).ToList(); foreach (WorldEdge e in remove) { index.Edges.Remove(e); index.EdgeIds.Remove(e.Id); index.EdgeById.Remove(e.Id); }
        }

        private static void RecomputeFingerprint(WorldIndex index)
        {
            index.Fingerprint = Hash(String.Join("\n", index.Nodes.Values.OrderBy(n => n.Id, StringComparer.OrdinalIgnoreCase).Select(n => n.Id + "|" + (n.Fingerprint ?? "")).Concat(index.Edges.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).Select(e => e.Id))));
        }

        private static void BuildAdjacency(WorldIndex index)
        {
            index.Out.Clear(); index.In.Clear();
            foreach (WorldEdge e in index.Edges)
            {
                List<string> list;
                if (!index.Out.TryGetValue(e.FromId, out list)) { list = new List<string>(); index.Out[e.FromId] = list; } list.Add(e.Id);
                if (!index.In.TryGetValue(e.ToId, out list)) { list = new List<string>(); index.In[e.ToId] = list; } list.Add(e.Id);
            }
        }

        private static void OpenQueryConsole(Window owner, DeepSystemsContext ctx, WorldIndex index, string initial)
        {
            Window w = W(owner, "YOMI · WORLD QUERY CONSOLE", 1120, 760);
            Grid root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); root.RowDefinitions.Add(new RowDefinition());
            StackPanel head = new StackPanel(); head.Children.Add(T("WORLD QUERY LANGUAGE / YQL", 24, Text, FontWeights.Bold)); head.Children.Add(T("Commands: FIND text · KIND kind · IDENTITY key=value · FIELD key=value · NEIGHBORS node-id · PATH node-id -> node-id · STATS", 12, Muted, FontWeights.Normal)); Grid.SetRow(head, 0); root.Children.Add(head);
            DockPanel bar = new DockPanel { Margin = new Thickness(0, 12, 0, 10) };
            TextBox q = new TextBox { Text = initial ?? "FIND source_key", MinHeight = 38, Padding = new Thickness(9), Background = Raised, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") };
            Button run = Btn("EXECUTE", null); Button save = Btn("SAVE LENS", null); Button rebuild = Btn("REBUILD", null); DockPanel.SetDock(run, Dock.Right); DockPanel.SetDock(save, Dock.Right); DockPanel.SetDock(rebuild, Dock.Right); bar.Children.Add(rebuild); bar.Children.Add(save); bar.Children.Add(run); bar.Children.Add(q); Grid.SetRow(bar, 1); root.Children.Add(bar);
            Grid body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ListBox results = List(); Grid.SetColumn(results, 0); body.Children.Add(results);
            TextBox detail = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(10, 10, 0, 0), Padding = new Thickness(10), Background = Surface, Foreground = Muted, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; Grid.SetColumn(detail, 1); body.Children.Add(detail); Grid.SetRow(body, 2); root.Children.Add(body);
            WorldQueryResult current = null;
            Action execute = delegate
            {
                current = ExecuteQuery(index, q.Text); results.ItemsSource = current.Nodes; detail.Text = current.Explanation + "\r\n\r\n" + current.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes · " + current.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges"; RememberQuery(ctx, q.Text);
            };
            run.Click += delegate { execute(); };
            rebuild.Click += delegate { index = GetIndex(ctx, true); execute(); };
            save.Click += delegate { string name = Prompt(w, "SAVE WORLD LENS", "Lens name", q.Text.Length > 42 ? q.Text.Substring(0, 42) : q.Text); if (!String.IsNullOrWhiteSpace(name)) SaveLens(ctx, name, q.Text); };
            q.KeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) { execute(); e.Handled = true; } };
            results.SelectionChanged += delegate { WorldNode n = results.SelectedItem as WorldNode; if (n != null) detail.Text = DossierText(index, n); };
            results.MouseDoubleClick += delegate { WorldNode n = results.SelectedItem as WorldNode; if (n != null) OpenDossier(w, ctx, index, n.Id); };
            w.Content = root; w.Show(); execute(); q.Focus(); q.SelectAll();
        }

        private static WorldQueryResult ExecuteQuery(WorldIndex index, string query)
        {
            WorldQueryResult r = new WorldQueryResult { Query = query ?? "" };
            string q = (query ?? "").Trim(); if (q.Length == 0) q = "STATS";
            if (q.StartsWith("STATS", StringComparison.OrdinalIgnoreCase))
            {
                r.Nodes = index.Nodes.Values.OrderByDescending(n => Degree(index, n.Id)).Take(100).ToList(); r.Explanation = IndexSummary(index) + "\r\nTop nodes ordered by graph degree."; return r;
            }
            if (q.StartsWith("KIND ", StringComparison.OrdinalIgnoreCase))
            {
                string kind = q.Substring(5).Trim(); r.Nodes = index.Nodes.Values.Where(n => (n.Kind ?? "").IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(n => Degree(index, n.Id)).Take(500).ToList(); r.Explanation = "Kind selector · " + kind; return r;
            }
            if (q.StartsWith("IDENTITY ", StringComparison.OrdinalIgnoreCase))
            {
                string token = q.Substring(9).Trim(); r.Nodes = index.Nodes.Values.Where(n => n.IdentityTokens.Any(t => t.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) || (n.Kind == "IDENTITY" && (n.Label ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)).OrderByDescending(n => Degree(index, n.Id)).Take(500).ToList(); r.Explanation = "Identity selector · " + token; return r;
            }
            if (q.StartsWith("FIELD ", StringComparison.OrdinalIgnoreCase))
            {
                string expr = q.Substring(6).Trim(); int at = expr.IndexOf('='); string key = at >= 0 ? expr.Substring(0, at).Trim() : expr; string val = at >= 0 ? expr.Substring(at + 1).Trim() : "";
                r.Nodes = index.Nodes.Values.Where(n => n.Properties.Any(kv => kv.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 && (val.Length == 0 || (kv.Value ?? "").IndexOf(val, StringComparison.OrdinalIgnoreCase) >= 0))).OrderByDescending(n => Degree(index, n.Id)).Take(500).ToList(); r.Explanation = "Field selector · " + key + (val.Length > 0 ? " = " + val : ""); return r;
            }
            if (q.StartsWith("NEIGHBORS ", StringComparison.OrdinalIgnoreCase))
            {
                string id = q.Substring(10).Trim(); WorldNode center = ResolveNode(index, id); if (center == null) { r.Explanation = "No node resolved for: " + id; return r; }
                r.Nodes.Add(center); r.Nodes.AddRange(NeighborNodes(index, center.Id).Where(n => n.Id != center.Id).Take(500)); r.Edges = NeighborEdges(index, center.Id).Take(1000).ToList(); r.Explanation = "Neighborhood · " + center.Id + " · degree " + Degree(index, center.Id).ToString(CultureInfo.InvariantCulture); return r;
            }
            if (q.StartsWith("PATH ", StringComparison.OrdinalIgnoreCase))
            {
                string expr = q.Substring(5).Trim(); int arrow = expr.IndexOf("->", StringComparison.Ordinal); if (arrow < 0) { r.Explanation = "PATH syntax: PATH <node-or-fragment> -> <node-or-fragment>"; return r; }
                WorldNode a = ResolveNode(index, expr.Substring(0, arrow).Trim()), b = ResolveNode(index, expr.Substring(arrow + 2).Trim()); if (a == null || b == null) { r.Explanation = "Could not resolve one or both path endpoints."; return r; }
                List<string> path = ShortestPath(index, a.Id, b.Id, 12); foreach (string id in path) { WorldNode n; if (index.Nodes.TryGetValue(id, out n)) r.Nodes.Add(n); } r.Explanation = path.Count == 0 ? "No path found within 12 hops." : "Shortest undirected evidence path · " + (path.Count - 1).ToString(CultureInfo.InvariantCulture) + " hops"; return r;
            }
            string term = q.StartsWith("FIND ", StringComparison.OrdinalIgnoreCase) ? q.Substring(5).Trim() : q;
            r.Nodes = index.Nodes.Values.Where(n => SearchBlob(n).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(n => Degree(index, n.Id)).Take(500).ToList(); r.Explanation = "Full-world text search · " + term; return r;
        }

        private static void OpenDossier(Window owner, DeepSystemsContext ctx, WorldIndex index, string id)
        {
            WorldNode n; if (!index.Nodes.TryGetValue(id, out n)) return;
            Window w = W(owner, "YOMI · WORLD PASSPORT · " + Trunc(n.Label, 72), 1060, 760);
            DockPanel root = new DockPanel { Margin = new Thickness(16) };
            StackPanel head = new StackPanel(); head.Children.Add(T((n.Kind ?? "OBJECT") + " · WORLD PASSPORT", 12, Accent, FontWeights.Bold)); head.Children.Add(T(n.Label ?? n.Id, 25, Text, FontWeights.Bold)); head.Children.Add(T(n.Id, 11, Blue, FontWeights.Normal)); head.Children.Add(T(n.Summary ?? "", 12, Muted, FontWeights.Normal));
            WrapPanel actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 6) };
            actions.Children.Add(Btn("NEIGHBORHOOD", delegate { OpenNeighborhood(w, ctx, index, n); }));
            actions.Children.Add(Btn("CIVILIZATION", delegate { CivilizationLayer.OpenFromWorldNode(w, ctx, n.Id); }));
            actions.Children.Add(Btn("LOGIC FOUNDRY", delegate { EpistemicLogicFoundry.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("VERIFY MODEL", delegate { AxiomaticVerificationReactor.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("STRATEGY MODEL", delegate { CounterfactualStrategySuperstructure.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("DISCOVERY PROTOCOL", delegate { ScientificDiscoveryHyperstructure.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("ENGINEER SYSTEM", delegate { DysonSystemsEngineeringMegastructure.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("CYBERNETIC TWIN", delegate { CyberneticDigitalTwinMetastructure.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("COMPLEX POPULATION", delegate { ComplexSystemsLaboratory.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("OPTIMIZE OPERATIONS", delegate { OperationsResearchEmpire.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("ECONOMIC CIVILIZATION", delegate { EconomicCivilization.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("SPATIAL CARTOGRAPHY", delegate { SpatialWorldCartography.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("DISTRIBUTED PLANETARIUM", delegate { DistributedSystemsPlanetarium.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("SECURITY FORTRESS", delegate { AdversarialSecurityFortress.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("DATA FOUNDRY", delegate { DataFoundry.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("SOFTWARE GENOME", delegate { SoftwareGenomeObservatory.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("LIVING DOCUMENT", delegate { LivingDocumentIntelligenceFactory.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("EXPEDITION COMMAND", delegate { GrandUnifiedExpeditionCommand.OpenFromWorldNode(w, ctx, index, n.Id); }));
            actions.Children.Add(Btn("SAVE TO SPINE", delegate { WorldPassportSpine.CaptureWorldNode(w, ctx, n); }));
            actions.Children.Add(Btn("FIND PATH…", delegate { PromptPath(w, ctx, index, n); }));
            actions.Children.Add(Btn("HADAL DESCENT", delegate { AbyssalKnowledgeEngine.DescendFromExternal(w, ctx, n.Label, n.Value ?? (object)n.Properties, n.SourcePath, n.JsonPath, "WORLD KERNEL › " + n.Kind + " › " + n.Label); }));
            actions.Children.Add(Btn("TRACE EVIDENCE", delegate { string token = n.IdentityTokens.FirstOrDefault(); if (!String.IsNullOrWhiteSpace(token)) DeepSystems.OpenExternalEvidenceSearch(w, ctx, TokenValue(token), "WORLD KERNEL › TRACE › " + n.Label); }));
            actions.Children.Add(Btn("RAW / MICROSCOPE", delegate { if (n.Value != null) DeepSystems.OpenExternalValue(w, ctx, n.Label, n.Value, "WORLD KERNEL › RAW"); else if (!String.IsNullOrWhiteSpace(n.SourcePath)) DeepSystems.OpenExternalFile(w, ctx, n.SourcePath, "WORLD KERNEL › FILE"); }));
            actions.Children.Add(Btn("TEMPORAL", delegate { TemporalObservatory.Open(w, ctx); }));
            actions.Children.Add(Btn("CAUSAL", delegate { CausalGraphLab.Open(w, ctx); }));
            actions.Children.Add(Btn("RESEARCH", delegate { ResearchWorkbench.Open(w, ctx); }));
            head.Children.Add(actions); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            Grid body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition());
            TextBox passport = new TextBox { IsReadOnly = true, Text = DossierText(index, n), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(10), Margin = new Thickness(0, 6, 6, 0), Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas") }; Grid.SetColumn(passport, 0); body.Children.Add(passport);
            ListBox neighbors = List(); neighbors.ItemsSource = NeighborNodes(index, n.Id).Take(400).ToList(); neighbors.MouseDoubleClick += delegate { WorldNode x = neighbors.SelectedItem as WorldNode; if (x != null) OpenDossier(w, ctx, index, x.Id); }; Grid.SetColumn(neighbors, 1); body.Children.Add(neighbors); root.Children.Add(body); w.Content = root; w.Show();
        }

        private static string DossierText(WorldIndex index, WorldNode n)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine("URN              " + n.Id); s.AppendLine("KIND             " + n.Kind); s.AppendLine("EVIDENCE CLASS   " + n.EvidenceClass); s.AppendLine("FINGERPRINT      " + n.Fingerprint); s.AppendLine("SOURCE           " + (n.SourcePath ?? "∅")); s.AppendLine("JSON PATH        " + (n.JsonPath ?? "∅")); s.AppendLine("MODIFIED UTC     " + (n.ModifiedUtc == DateTime.MinValue ? "∅" : n.ModifiedUtc.ToString("o", CultureInfo.InvariantCulture))); s.AppendLine("GRAPH DEGREE     " + Degree(index, n.Id).ToString(CultureInfo.InvariantCulture));
            s.AppendLine(); s.AppendLine("IDENTITIES"); foreach (string token in n.IdentityTokens.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) s.AppendLine("  " + token);
            s.AppendLine(); s.AppendLine("PROPERTIES"); foreach (KeyValuePair<string, string> kv in n.Properties.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)) s.AppendLine("  " + kv.Key + " = " + kv.Value);
            s.AppendLine(); s.AppendLine("RELATIONSHIPS"); foreach (WorldEdge e in NeighborEdges(index, n.Id).Take(80)) s.AppendLine("  " + e.Kind + "  " + (String.Equals(e.FromId, n.Id, StringComparison.OrdinalIgnoreCase) ? "→ " + e.ToId : "← " + e.FromId) + "  [" + e.EvidenceClass + "]");
            return s.ToString();
        }

        private static void OpenNeighborhood(Window owner, DeepSystemsContext ctx, WorldIndex index, WorldNode center)
        {
            Window w = W(owner, "YOMI · WORLD NEIGHBORHOOD", 1080, 720); DockPanel root = new DockPanel { Margin = new Thickness(16) };
            StackPanel h = new StackPanel(); h.Children.Add(T("NEIGHBORHOOD", 24, Text, FontWeights.Bold)); h.Children.Add(T(center.Label + " · degree " + Degree(index, center.Id).ToString(CultureInfo.InvariantCulture), 12, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h);
            ListBox list = List(); List<WorldNode> nodes = NeighborNodes(index, center.Id).OrderByDescending(n => Degree(index, n.Id)).ToList(); list.ItemsSource = nodes; list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) OpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void PromptPath(Window owner, DeepSystemsContext ctx, WorldIndex index, WorldNode from)
        {
            string target = Prompt(owner, "WORLD PATHFINDER", "Destination node ID, label fragment or identity value", ""); if (String.IsNullOrWhiteSpace(target)) return; WorldNode to = ResolveNode(index, target); if (to == null) { MessageBox.Show(owner, "No destination resolved.", "YOMI · World Pathfinder"); return; } List<string> path = ShortestPath(index, from.Id, to.Id, 18); if (path.Count == 0) { MessageBox.Show(owner, "No evidence path found within 18 hops.", "YOMI · World Pathfinder"); return; }
            Window w = W(owner, "YOMI · WORLD PATH · " + (path.Count - 1).ToString(CultureInfo.InvariantCulture) + " HOPS", 1000, 680); ListBox list = List(); list.Margin = new Thickness(16); list.ItemsSource = path.Select(id => index.Nodes.ContainsKey(id) ? index.Nodes[id] : null).Where(n => n != null).ToList(); list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) OpenDossier(w, ctx, index, n.Id); }; w.Content = list; w.Show();
        }

        private static void OpenObjectIndex(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            OpenNodeList(owner, ctx, index, "UNIVERSAL OBJECT INDEX", index.Nodes.Values.OrderByDescending(n => Degree(index, n.Id)).ThenBy(n => n.Kind).Take(4000).ToList(), "Objects ordered by graph connectivity. Double-click for World Passport.");
        }

        private static void OpenIdentityNexus(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            List<WorldNode> ids = index.Nodes.Values.Where(n => n.Kind == "IDENTITY").OrderByDescending(n => Degree(index, n.Id)).Take(2000).ToList(); OpenNodeList(owner, ctx, index, "IDENTITY NEXUS", ids, "Semantic identity hubs. High degree means the same identity joins many otherwise separate evidence objects.");
        }

        private static void OpenNodeList(Window owner, DeepSystemsContext ctx, WorldIndex index, string title, List<WorldNode> nodes, string subtitle)
        {
            Window w = W(owner, "YOMI · " + title, 1080, 720); DockPanel root = new DockPanel { Margin = new Thickness(16) }; StackPanel h = new StackPanel(); h.Children.Add(T(title, 24, Text, FontWeights.Bold)); h.Children.Add(T(subtitle, 12, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); list.ItemsSource = nodes; list.MouseDoubleClick += delegate { WorldNode n = list.SelectedItem as WorldNode; if (n != null) OpenDossier(w, ctx, index, n.Id); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenSchemaRegistry(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Window w = W(owner, "YOMI · SCHEMA CARTOGRAPHY", 1080, 720); DockPanel root = new DockPanel { Margin = new Thickness(16) }; root.Children.Add(T("SCHEMA CARTOGRAPHY", 24, Text, FontWeights.Bold)); ListBox list = List();
            List<string> rows = new List<string>(); foreach (KeyValuePair<string, HashSet<string>> kv in index.SchemaKinds.OrderByDescending(kv => index.Nodes.Values.Count(n => String.Equals(n.Kind, kv.Key, StringComparison.OrdinalIgnoreCase)))) { List<string> fields = new List<string>(); foreach (string field in kv.Value.OrderBy(x => x).Take(24)) { HashSet<string> types; string typeKey = kv.Key + "." + field; fields.Add(field + ":" + (index.SchemaTypes.TryGetValue(typeKey, out types) ? String.Join("/", types.OrderBy(x => x).ToArray()) : "?")); } rows.Add(kv.Key + "   ·   " + index.Nodes.Values.Count(n => String.Equals(n.Kind, kv.Key, StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture) + " objects   ·   " + String.Join(", ", fields.ToArray())); } list.ItemsSource = rows; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenEdgeTaxonomy(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            List<string> rows = index.Edges.GroupBy(e => e.Kind ?? "RELATED", StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Select(g => g.Key + "   ·   " + g.Count().ToString(CultureInfo.InvariantCulture) + " edges").ToList(); SimpleList(owner, "RELATIONSHIP TAXONOMY", "Observed/inferred edge families in the current world graph.", rows);
        }

        private static void OpenTopology(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            List<List<string>> components = Components(index); List<string> rows = new List<string>(); rows.Add("COMPONENTS   " + components.Count.ToString(CultureInfo.InvariantCulture)); rows.Add("LARGEST      " + (components.Count == 0 ? "0" : components.Max(c => c.Count).ToString(CultureInfo.InvariantCulture))); rows.Add(""); int i = 0; foreach (List<string> c in components.OrderByDescending(x => x.Count).Take(120)) { i++; WorldNode hub = c.Select(id => index.Nodes.ContainsKey(id) ? index.Nodes[id] : null).Where(n => n != null).OrderByDescending(n => Degree(index, n.Id)).FirstOrDefault(); rows.Add("#" + i.ToString(CultureInfo.InvariantCulture) + "   " + c.Count.ToString(CultureInfo.InvariantCulture) + " nodes   ·   hub " + (hub == null ? "∅" : hub.Label)); } SimpleList(owner, "CONNECTED WORLDS", "Connected components are structural graph neighborhoods, not claims of causal relationship.", rows);
        }

        private static void OpenSourceCatalog(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            Window w = W(owner, "YOMI · WORLD SOURCE CATALOG", 1080, 720); ListBox list = List(); list.Margin = new Thickness(16); list.ItemsSource = index.Sources.OrderByDescending(s => s.ModifiedUtc).ToList(); list.MouseDoubleClick += delegate { WorldSourceStat s = list.SelectedItem as WorldSourceStat; if (s != null) DeepSystems.OpenExternalFile(w, ctx, s.Path, "WORLD KERNEL › SOURCE CATALOG"); }; w.Content = list; w.Show();
        }

        private static void OpenDiagnostics(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            List<string> rows = new List<string>();
            List<WorldNode> super = index.Nodes.Values.Where(n => n.Kind == "IDENTITY" && Degree(index, n.Id) >= 12).OrderByDescending(n => Degree(index, n.Id)).Take(60).ToList();
            foreach (WorldNode n in super) rows.Add("SUPERHUB   degree=" + Degree(index, n.Id).ToString(CultureInfo.InvariantCulture) + "   " + n.Label);
            foreach (WorldNode n in index.Nodes.Values.Where(n => n.Kind != "FILE" && Degree(index, n.Id) == 0).Take(80)) rows.Add("ORPHAN   " + n.Kind + "   " + n.Label);
            foreach (WorldEdge e in index.Edges.Where(e => String.Equals(e.FromId, e.ToId, StringComparison.OrdinalIgnoreCase)).Take(40)) rows.Add("SELF-REFERENCE   " + e.FromId + "   " + e.Kind);
            if (rows.Count == 0) rows.Add("No convergence diagnostics triggered in the bounded current index.");
            rows.Insert(0, "These are mechanically observed graph patterns. They are not corruption or causal verdicts."); SimpleList(owner, "CONVERGENCE DIAGNOSTICS", "High-connectivity and structural oddities worth investigating.", rows);
        }

        private static void CaptureEpoch(Window owner, DeepSystemsContext ctx, WorldIndex index)
        {
            LoadStore(ctx); WorldEpoch e = new WorldEpoch { Id = "epoch-" + Guid.NewGuid().ToString("N").Substring(0, 12), CapturedUtc = Now(), Fingerprint = index.Fingerprint, Nodes = index.Nodes.Count, Edges = index.Edges.Count, Sources = index.SourcesScanned };
            foreach (WorldNode n in index.Nodes.Values.OrderByDescending(n => Degree(index, n.Id)).Take(6000)) e.NodeFingerprints[n.Id] = n.Fingerprint ?? "";
            Store.Epochs.Add(e); while (Store.Epochs.Count > 12) Store.Epochs.RemoveAt(0); SaveStore(ctx); MessageBox.Show(owner, "World epoch captured.\n\n" + e.ToString(), "YOMI · World Kernel");
        }

        private static void OpenEpochVault(Window owner, DeepSystemsContext ctx)
        {
            LoadStore(ctx); Window w = W(owner, "YOMI · WORLD EPOCH VAULT", 1040, 700); DockPanel root = new DockPanel { Margin = new Thickness(16) }; StackPanel head = new StackPanel(); head.Children.Add(T("WORLD EPOCH VAULT", 24, Text, FontWeights.Bold)); head.Children.Add(T("Bounded graph fingerprints. Compare two captures to see added, removed and materially changed universal objects.", 12, Muted, FontWeights.Normal)); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head); ListBox list = List(); list.ItemsSource = Store.Epochs.OrderByDescending(e => e.CapturedUtc).ToList(); list.MouseDoubleClick += delegate { WorldEpoch selected = list.SelectedItem as WorldEpoch; if (selected != null) OpenEpochDiff(w, ctx, selected); }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenEpochDiff(Window owner, DeepSystemsContext ctx, WorldEpoch older)
        {
            WorldIndex current = GetIndex(ctx, true); Dictionary<string, string> now = current.Nodes.Values.OrderByDescending(n => Degree(current, n.Id)).Take(6000).ToDictionary(n => n.Id, n => n.Fingerprint ?? "", StringComparer.OrdinalIgnoreCase); List<string> rows = new List<string>(); int added = 0, removed = 0, changed = 0;
            foreach (KeyValuePair<string, string> kv in now) { string old; if (!older.NodeFingerprints.TryGetValue(kv.Key, out old)) { added++; if (rows.Count < 1000) rows.Add("ADDED     " + kv.Key); } else if (!String.Equals(old, kv.Value, StringComparison.Ordinal)) { changed++; if (rows.Count < 1000) rows.Add("CHANGED   " + kv.Key); } }
            foreach (string id in older.NodeFingerprints.Keys) if (!now.ContainsKey(id)) { removed++; if (rows.Count < 1000) rows.Add("REMOVED   " + id); }
            rows.Insert(0, "Δ added=" + added.ToString(CultureInfo.InvariantCulture) + " changed=" + changed.ToString(CultureInfo.InvariantCulture) + " removed=" + removed.ToString(CultureInfo.InvariantCulture)); SimpleList(owner, "WORLD EPOCH DELTA", older.CapturedUtc + " → NOW", rows);
        }

        private static void OpenLensVault(Window owner, DeepSystemsContext ctx)
        {
            LoadStore(ctx); Window w = W(owner, "YOMI · SAVED WORLD LENSES", 1040, 700); DockPanel root = new DockPanel { Margin = new Thickness(16) }; StackPanel head = new StackPanel(); head.Children.Add(T("SAVED WORLD LENSES", 24, Text, FontWeights.Bold)); WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) }; tools.Children.Add(Btn("QUERY HISTORY", delegate { OpenQueryHistory(w, ctx); })); head.Children.Add(tools); DockPanel.SetDock(head, Dock.Top); root.Children.Add(head); ListBox list = List(); list.ItemsSource = Store.Lenses.OrderByDescending(l => l.LastOpenedUtc ?? l.SavedUtc).ToList(); list.MouseDoubleClick += delegate { WorldLens lens = list.SelectedItem as WorldLens; if (lens != null) { lens.OpenCount++; lens.LastOpenedUtc = Now(); SaveStore(ctx); OpenQueryConsole(w, ctx, GetIndex(ctx, false), lens.Query); } }; root.Children.Add(list); w.Content = root; w.Show();
        }

        private static void OpenQueryHistory(Window owner, DeepSystemsContext ctx)
        {
            LoadStore(ctx); Window w = W(owner, "YOMI · WORLD QUERY HISTORY", 980, 660); ListBox list = List(); list.Margin = new Thickness(16); list.ItemsSource = Store.QueryHistory.ToList(); list.MouseDoubleClick += delegate { string q = list.SelectedItem as string; if (!String.IsNullOrWhiteSpace(q)) OpenQueryConsole(w, ctx, GetIndex(ctx, false), q); }; w.Content = list; w.Show();
        }

        private static void SaveLens(DeepSystemsContext ctx, string name, string query)
        {
            LoadStore(ctx); Store.Lenses.Add(new WorldLens { Id = "lens-" + Guid.NewGuid().ToString("N").Substring(0, 12), Name = name.Trim(), Query = query ?? "", SavedUtc = Now() }); while (Store.Lenses.Count > 32) Store.Lenses.RemoveAt(0); SaveStore(ctx);
        }

        private static void RememberQuery(DeepSystemsContext ctx, string query)
        {
            if (String.IsNullOrWhiteSpace(query)) return; LoadStore(ctx); Store.QueryHistory.RemoveAll(x => String.Equals(x, query, StringComparison.OrdinalIgnoreCase)); Store.QueryHistory.Insert(0, query); while (Store.QueryHistory.Count > 64) Store.QueryHistory.RemoveAt(Store.QueryHistory.Count - 1); SaveStore(ctx);
        }

        private static void LoadStore(DeepSystemsContext ctx)
        {
            lock (Gate)
            {
                if (Store != null && String.Equals(StoreRoot, ctx.DataRoot, StringComparison.OrdinalIgnoreCase)) return; Store = null; StoreRoot = ctx.DataRoot; string path = StorePath(ctx); try { if (File.Exists(path)) Store = Json.Deserialize<WorldKernelStore>(File.ReadAllText(path, Encoding.UTF8)); } catch { PreserveCorrupt(path); Store = null; } if (Store == null) Store = new WorldKernelStore(); if (Store.Lenses == null) Store.Lenses = new List<WorldLens>(); if (Store.QueryHistory == null) Store.QueryHistory = new List<string>(); if (Store.Epochs == null) Store.Epochs = new List<WorldEpoch>();
            }
        }

        private static void SaveStore(DeepSystemsContext ctx)
        {
            lock (Gate) { Directory.CreateDirectory(ctx.StateRoot); AtomicWrite(StorePath(ctx), Json.Serialize(Store ?? new WorldKernelStore())); }
        }

        private static string StorePath(DeepSystemsContext ctx) { return Path.Combine(ctx.StateRoot, "controller-world-kernel.json"); }

        private static string ComputeSourceSignature(DeepSystemsContext ctx)
        {
            StringBuilder s = new StringBuilder(); int count = 0; foreach (string p in EnumerateTextSources(ctx).Take(MaxTextSources)) { if (count++ > MaxTextSources) break; try { FileInfo fi = new FileInfo(p); s.Append(Relative(ctx.DataRoot, p)).Append('|').Append(fi.Length).Append('|').Append(fi.LastWriteTimeUtc.Ticks).Append('\n'); } catch { } }
            string media = Path.Combine(ctx.DataRoot, "cache", "objects"); try { DirectoryInfo di = new DirectoryInfo(media); s.Append("MEDIA|").Append(di.Exists ? di.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture) : "0"); if (di.Exists) { int d = 0; foreach (DirectoryInfo child in di.EnumerateDirectories().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)) { if (d++ >= 64) break; s.Append('|').Append(child.Name).Append(':').Append(child.LastWriteTimeUtc.Ticks); } } } catch { }
            return Hash(s.ToString());
        }

        private static IEnumerable<string> EnumerateTextSources(DeepSystemsContext ctx)
        {
            List<string> paths = new List<string>();
            List<string> recursiveRoots = new List<string> { ctx.StateRoot, Path.Combine(ctx.DataRoot, "logs"), Path.Combine(ctx.DataRoot, "recovery"), Path.Combine(ctx.DataRoot, "support"), Path.Combine(ctx.DataRoot, "diagnostics") };
            string[] patterns = new string[] { "*.json", "*.jsonl", "*.log", "*.txt" };
            foreach (string root in recursiveRoots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(root)) continue;
                foreach (string pattern in patterns) foreach (string p in SafeFiles(root, pattern, SearchOption.AllDirectories)) AddSourcePath(paths, p);
            }
            if (Directory.Exists(ctx.DataRoot)) foreach (string pattern in patterns) foreach (string p in SafeFiles(ctx.DataRoot, pattern, SearchOption.TopDirectoryOnly)) AddSourcePath(paths, p);
            return paths.OrderByDescending(SafeWrite);
        }

        private static void AddSourcePath(List<string> paths, string p)
        {
            if (String.IsNullOrWhiteSpace(p)) return; if (String.Equals(Path.GetFileName(p), "controller-world-kernel.json", StringComparison.OrdinalIgnoreCase)) return; if (!paths.Contains(p, StringComparer.OrdinalIgnoreCase)) paths.Add(p);
        }

        private static string InferKind(Dictionary<string, object> map, string path)
        {
            if (Has(map, "intent_id")) return Has(map, "state") || Has(map, "step") ? "INTENT_EVENT" : "INTENT";
            if (Has(map, "occurrence_id")) return "OCCURRENCE";
            if (Has(map, "mission_id") || Has(map, "parent_mission_id")) return "MISSION_OBJECT";
            if (Has(map, "task_id") || Has(map, "parent_task_id")) return "TASK_OBJECT";
            if (Has(map, "branch_id") || Has(map, "parent_branch_id")) return "COUNTERFACTUAL_BRANCH";
            if (Has(map, "descent_id") || ((path ?? "").IndexOf("abyss", StringComparison.OrdinalIgnoreCase) >= 0 && Has(map, "depth"))) return "HADAL_DESCENT";
            if (Has(map, "entry_hash") || Has(map, "prev_hash")) return "LEDGER_RECORD";
            if (Has(map, "source_key")) return "SOURCE_OBJECT";
            if (Has(map, "session_id")) return "SESSION_OBJECT";
            if (Has(map, "id")) return "TYPED_RECORD";
            return "JSON_OBJECT";
        }

        private static string BestLabel(Dictionary<string, object> map, string kind, List<string> identities)
        {
            foreach (string k in new string[] { "title", "name", "label", "summary", "action", "state", "kind", "phase" }) { string v = Scalar(map, k); if (!String.IsNullOrWhiteSpace(v)) return Trunc(v, 96); }
            if (identities.Count > 0) return Trunc(identities[0], 96); return kind;
        }

        private static string NormalizeIdentityKey(string kind, string key)
        {
            if (String.Equals(key, "parent_branch_id", StringComparison.OrdinalIgnoreCase) || String.Equals(key, "root_branch_id", StringComparison.OrdinalIgnoreCase)) return "branch_id";
            if (String.Equals(key, "parent_task_id", StringComparison.OrdinalIgnoreCase)) return "task_id";
            if (String.Equals(key, "parent_mission_id", StringComparison.OrdinalIgnoreCase)) return "mission_id";
            if (String.Equals(key, "parent_id", StringComparison.OrdinalIgnoreCase))
            {
                if ((kind ?? "").IndexOf("HADAL", StringComparison.OrdinalIgnoreCase) >= 0) return "descent_id";
                if ((kind ?? "").IndexOf("MISSION", StringComparison.OrdinalIgnoreCase) >= 0) return "mission_id";
            }
            return key.ToLowerInvariant();
        }

        private static WorldNode ResolveNode(WorldIndex index, string fragment)
        {
            if (String.IsNullOrWhiteSpace(fragment)) return null; WorldNode exact; if (index.Nodes.TryGetValue(fragment, out exact)) return exact;
            return index.Nodes.Values.OrderByDescending(n => Degree(index, n.Id)).FirstOrDefault(n => (n.Id ?? "").IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0 || (n.Label ?? "").IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0 || n.IdentityTokens.Any(t => t.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static IEnumerable<WorldEdge> NeighborEdges(WorldIndex index, string id)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); List<string> list; if (index.Out.TryGetValue(id, out list)) foreach (string x in list) ids.Add(x); if (index.In.TryGetValue(id, out list)) foreach (string x in list) ids.Add(x);
            foreach (string edgeId in ids) { WorldEdge edge; if (index.EdgeById.TryGetValue(edgeId, out edge)) yield return edge; }
        }

        private static IEnumerable<WorldNode> NeighborNodes(WorldIndex index, string id)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (WorldEdge e in NeighborEdges(index, id)) { if (!String.Equals(e.FromId, id, StringComparison.OrdinalIgnoreCase)) ids.Add(e.FromId); if (!String.Equals(e.ToId, id, StringComparison.OrdinalIgnoreCase)) ids.Add(e.ToId); } foreach (string x in ids) { WorldNode n; if (index.Nodes.TryGetValue(x, out n)) yield return n; }
        }

        private static int Degree(WorldIndex index, string id) { int n = 0; List<string> x; if (index.Out.TryGetValue(id, out x)) n += x.Count; if (index.In.TryGetValue(id, out x)) n += x.Count; return n; }

        private static List<string> ShortestPath(WorldIndex index, string from, string to, int maxDepth)
        {
            if (String.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return new List<string> { from };
            Queue<string> q = new Queue<string>(); Dictionary<string, string> prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); Dictionary<string, int> depth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); q.Enqueue(from); prev[from] = null; depth[from] = 0;
            while (q.Count > 0)
            {
                string cur = q.Dequeue(); int d = depth[cur]; if (d >= maxDepth) continue;
                foreach (WorldNode n in NeighborNodes(index, cur))
                {
                    if (prev.ContainsKey(n.Id)) continue; prev[n.Id] = cur; depth[n.Id] = d + 1; if (String.Equals(n.Id, to, StringComparison.OrdinalIgnoreCase)) { List<string> path = new List<string>(); string x = to; while (x != null) { path.Add(x); x = prev[x]; } path.Reverse(); return path; } q.Enqueue(n.Id);
                }
            }
            return new List<string>();
        }

        private static List<List<string>> Components(WorldIndex index)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); List<List<string>> result = new List<List<string>>();
            foreach (string start in index.Nodes.Keys)
            {
                if (seen.Contains(start)) continue; List<string> comp = new List<string>(); Queue<string> q = new Queue<string>(); q.Enqueue(start); seen.Add(start);
                while (q.Count > 0 && comp.Count < 20000) { string cur = q.Dequeue(); comp.Add(cur); foreach (WorldNode n in NeighborNodes(index, cur)) if (seen.Add(n.Id)) q.Enqueue(n.Id); }
                result.Add(comp);
            }
            return result;
        }

        private static void ExtractIdentityTokens(object value, List<string> tokens, int depth)
        {
            if (value == null || depth > 12 || tokens.Count >= 128) return;
            Dictionary<string, object> map = value as Dictionary<string, object>; if (map != null) { foreach (KeyValuePair<string, object> kv in map) { if (IsScalar(kv.Value) && (IdentityKeySet.Contains(kv.Key) || kv.Key.EndsWith("_id", StringComparison.OrdinalIgnoreCase))) { string v = Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? ""; if (!String.IsNullOrWhiteSpace(v)) AddUnique(tokens, kv.Key.ToLowerInvariant() + "=" + v); } else ExtractIdentityTokens(kv.Value, tokens, depth + 1); } return; }
            IEnumerable e = value as IEnumerable; if (e != null && !(value is string)) foreach (object x in e) { ExtractIdentityTokens(x, tokens, depth + 1); if (tokens.Count >= 128) break; }
        }

        private static string SearchBlob(WorldNode n)
        {
            StringBuilder s = new StringBuilder(); s.Append(n.Id).Append(' ').Append(n.Kind).Append(' ').Append(n.Label).Append(' ').Append(n.Summary).Append(' ').Append(n.SourcePath).Append(' ').Append(n.JsonPath); foreach (string t in n.IdentityTokens) s.Append(' ').Append(t); foreach (KeyValuePair<string, string> kv in n.Properties) s.Append(' ').Append(kv.Key).Append('=').Append(kv.Value); return s.ToString();
        }

        private static string IndexSummary(WorldIndex i)
        {
            return i.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes · " + i.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges · " + i.SourcesScanned.ToString(CultureInfo.InvariantCulture) + " text sources · fingerprint " + Short(i.Fingerprint, 16) + (i.Truncated ? " · BOUNDED/TRUNCATED" : "");
        }

        private static void SimpleList(Window owner, string title, string subtitle, IEnumerable<string> rows)
        {
            Window w = W(owner, "YOMI · " + title, 1040, 700); DockPanel root = new DockPanel { Margin = new Thickness(16) }; StackPanel h = new StackPanel(); h.Children.Add(T(title, 24, Text, FontWeights.Bold)); h.Children.Add(T(subtitle, 12, Muted, FontWeights.Normal)); DockPanel.SetDock(h, Dock.Top); root.Children.Add(h); ListBox list = List(); list.ItemsSource = rows.ToList(); root.Children.Add(list); w.Content = root; w.Show();
        }

        private static string Prompt(Window owner, string title, string label, string initial)
        {
            Window w = W(owner, "YOMI · " + title, 660, 280); w.ResizeMode = ResizeMode.NoResize; StackPanel p = new StackPanel { Margin = new Thickness(18) }; p.Children.Add(T(label, 13, Muted, FontWeights.Normal)); TextBox box = new TextBox { Text = initial ?? "", MinHeight = 70, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(9), Background = Raised, Foreground = Text, BorderBrush = Border }; p.Children.Add(box); string result = null; WrapPanel buttons = new WrapPanel(); buttons.Children.Add(Btn("OK", delegate { result = box.Text; w.DialogResult = true; w.Close(); })); buttons.Children.Add(Btn("CANCEL", delegate { w.DialogResult = false; w.Close(); })); p.Children.Add(buttons); w.Content = p; box.Focus(); w.ShowDialog(); return result;
        }

        private static Window W(Window owner, string title, double width, double height)
        {
            return new Window { Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 760), MinHeight = Math.Min(height, 520), Background = Bg, Foreground = Text, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = owner, FontFamily = new FontFamily("Segoe UI") };
        }
        private static TextBlock T(string text, double size, Brush color, FontWeight weight) { return new TextBlock { Text = text ?? "", FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap }; }
        private static Button Btn(string text, RoutedEventHandler click) { Button b = new Button { Content = text, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(11, 7, 11, 7), Background = Raised, Foreground = Text, BorderBrush = Border, BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; if (click != null) b.Click += click; return b; }
        private static ListBox List() { return new ListBox { Margin = new Thickness(0, 10, 0, 0), Background = Surface, Foreground = Text, BorderBrush = Border, FontFamily = new FontFamily("Cascadia Mono, Consolas"), Padding = new Thickness(5) }; }
        private static void Portal(Panel panel, string badge, string title, string subtitle, Action action) { StackPanel s = new StackPanel(); s.Children.Add(T(badge, 10.5, Accent, FontWeights.Bold)); s.Children.Add(T(title, 18, Text, FontWeights.SemiBold)); TextBlock d = T(subtitle, 12, Muted, FontWeights.Normal); d.Margin = new Thickness(0, 5, 0, 0); s.Children.Add(d); Border c = new Border { Child = s, Width = 350, MinHeight = 142, Margin = new Thickness(0, 0, 12, 12), Padding = new Thickness(14), Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Cursor = Cursors.Hand }; c.MouseLeftButtonUp += delegate { if (action != null) action(); }; panel.Children.Add(c); }
        private static Brush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }

        private static bool Has(Dictionary<string, object> m, string key) { object v; return TryGetInsensitive(m, key, out v); }
        private static string Scalar(Dictionary<string, object> m, string key) { object v; return TryGetInsensitive(m, key, out v) && v != null && IsScalar(v) ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : ""; }
        private static bool TryGetInsensitive(Dictionary<string, object> m, string key, out object value) { value = null; if (m == null) return false; if (m.TryGetValue(key, out value)) return true; foreach (KeyValuePair<string, object> kv in m) if (String.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) { value = kv.Value; return true; } return false; }
        private static bool IsScalar(object value) { return value == null || value is string || value is bool || value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal || value is DateTime; }
        private static string SafeSerialize(object value) { try { return value is string ? (string)value : Json.Serialize(value); } catch { return Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; } }
        private static string AppendPath(string path, string key) { if (key.All(ch => Char.IsLetterOrDigit(ch) || ch == '_')) return path + "." + key; return path + "['" + key.Replace("\\", "\\\\").Replace("'", "\\'") + "']"; }
        private static string RelativeRoot(string path) { return (path ?? "").ToLowerInvariant(); }
        private static string Relative(string root, string path) { try { if (!String.IsNullOrWhiteSpace(root) && !String.IsNullOrWhiteSpace(path) && path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return path.Substring(root.Length).TrimStart('\\', '/'); } catch { } return path ?? ""; }
        private static string ExtensionKind(string path) { string e = Path.GetExtension(path).ToLowerInvariant(); if (e == ".jsonl") return "JOURNAL"; if (e == ".json") return "JSON"; if (e == ".log") return "LOG"; if (e == ".txt") return "TEXT"; return "FILE"; }
        private static string GuessSourceKey(string name) { string b = Path.GetFileNameWithoutExtension(name ?? ""); if (b.Length >= 16) { for (int i = 0; i <= b.Length - 16; i++) { string x = b.Substring(i, 16); if (x.All(IsHex)) return x.ToLowerInvariant(); } } return ""; }
        private static bool IsHex(char c) { return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'); }
        private static string TokenKey(string token) { int at = (token ?? "").IndexOf('='); return at < 0 ? token ?? "" : token.Substring(0, at); }
        private static string TokenValue(string token) { int at = (token ?? "").IndexOf('='); return at < 0 ? token ?? "" : token.Substring(at + 1); }
        private static void AddUnique(List<string> list, string value) { if (String.IsNullOrWhiteSpace(value)) return; if (!list.Any(x => String.Equals(x, value, StringComparison.OrdinalIgnoreCase))) list.Add(value); }
        private static string ReadShared(string path) { using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true)) return sr.ReadToEnd(); }
        private static string ReadTail(string path, int maxBytes) { try { using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { long len = fs.Length; int take = (int)Math.Min((long)Math.Max(1, maxBytes), len); if (len > take) fs.Seek(len - take, SeekOrigin.Begin); byte[] b = new byte[take]; int offset = 0; while (offset < take) { int n = fs.Read(b, offset, take - offset); if (n <= 0) break; offset += n; } return Encoding.UTF8.GetString(b, 0, offset); } } catch { return ""; } }
        private static IEnumerable<string> SplitLines(string s) { return (s ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'); }
        private static IEnumerable<string> SafeFiles(string root, string pattern, SearchOption option) { try { if (!Directory.Exists(root)) return new string[0]; return Directory.EnumerateFiles(root, pattern, option).ToArray(); } catch { return new string[0]; } }
        private static DateTime SafeWrite(string path) { try { return !String.IsNullOrWhiteSpace(path) && File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue; } catch { return DateTime.MinValue; } }
        private static string Now() { return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static string Hash(string s) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""))).Replace("-", "").ToLowerInvariant(); }
        private static string Short(string s, int n) { if (String.IsNullOrWhiteSpace(s)) return "∅"; return s.Length <= n ? s : s.Substring(0, n); }
        private static string Trunc(string s, int n) { if (String.IsNullOrEmpty(s)) return ""; return s.Length <= n ? s : s.Substring(0, n) + "…"; }
        private static string FormatBytes(long n) { if (n >= 1073741824L) return (n / 1073741824.0).ToString("0.00", CultureInfo.InvariantCulture) + " GiB"; if (n >= 1048576L) return (n / 1048576.0).ToString("0.00", CultureInfo.InvariantCulture) + " MiB"; if (n >= 1024L) return (n / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KiB"; return n.ToString(CultureInfo.InvariantCulture) + " B"; }
        private static string StoreDir(string path) { return Path.GetDirectoryName(path) ?? "."; }

        private static void AtomicWrite(string path, string text)
        {
            Directory.CreateDirectory(StoreDir(path)); string tmp = path + ".tmp." + Guid.NewGuid().ToString("N"); File.WriteAllText(tmp, text ?? "{}", new UTF8Encoding(false)); if (File.Exists(path)) { string bak = path + ".bak"; try { File.Replace(tmp, path, bak, true); try { File.Delete(bak); } catch { } } catch { File.Copy(tmp, path, true); File.Delete(tmp); } } else File.Move(tmp, path);
        }
        private static void PreserveCorrupt(string path) { try { if (!File.Exists(path)) return; byte[] b = File.ReadAllBytes(path); string h; using (SHA256 s = SHA256.Create()) h = BitConverter.ToString(s.ComputeHash(b)).Replace("-", "").ToLowerInvariant(); string copy = path + ".corrupt." + h.Substring(0, 12) + ".json"; if (!File.Exists(copy)) File.WriteAllBytes(copy, b); } catch { } }
    }
}

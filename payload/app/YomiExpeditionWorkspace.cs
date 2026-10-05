
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Yomi.ProductShell
{
    internal sealed class ExpeditionNavEntry
    {
        public string Route;
        public string Title;
        public string Badge;
        public string Category;
        public string Origin;
        public string Detail;
        public string VisitedUtc;
        public override string ToString()
        {
            string badge = String.IsNullOrWhiteSpace(Badge) ? "EXP" : Badge;
            string title = String.IsNullOrWhiteSpace(Title) ? Route : Title;
            string origin = String.IsNullOrWhiteSpace(Origin) ? "" : "  ·  " + Origin;
            return badge + "  " + title + origin;
        }
    }

    internal sealed class ExpeditionWorkspaceState
    {
        public int Schema = 1;
        public ExpeditionNavEntry Current;
        public List<ExpeditionNavEntry> Back = new List<ExpeditionNavEntry>();
        public List<ExpeditionNavEntry> Forward = new List<ExpeditionNavEntry>();
        public List<ExpeditionNavEntry> Recent = new List<ExpeditionNavEntry>();
        public List<ExpeditionNavEntry> Pins = new List<ExpeditionNavEntry>();
        public string UpdatedUtc;
    }

    internal sealed class ExpeditionRoute
    {
        public string Id;
        public string Title;
        public string Badge;
        public string Category;
        public string Description;
        public Action<Window, DeepSystemsContext> Open;
        public override string ToString()
        {
            return (Badge ?? "EXP") + "  " + (Title ?? Id) + "   ·   " + (Category ?? "SYSTEM");
        }
    }

    internal static class ExpeditionWorkspace
    {
        private static readonly object Gate = new object();
        private static ExpeditionWorkspaceState State = new ExpeditionWorkspaceState();
        private static string LoadedPath;
        private static List<ExpeditionRoute> RouteCache;
        private static WeakReference WorkspaceWindow;

        private static SolidColorBrush B(string hex) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        private static readonly Brush Bg = B("#101417");
        private static readonly Brush Surface = B("#171D21");
        private static readonly Brush Raised = B("#20272D");
        private static readonly Brush Border = B("#354048");
        private static readonly Brush Text = B("#F1F4F6");
        private static readonly Brush Muted = B("#9EABB5");
        private static readonly Brush Accent = B("#69E6B4");
        private static readonly Brush Blue = B("#70B7FF");
        private static readonly Brush Amber = B("#FFCA69");

        private static string WorkspacePath(DeepSystemsContext ctx)
        {
            return Path.Combine(ctx.DataRoot, "expedition", "workspace-shell.json");
        }

        private static ExpeditionNavEntry Clone(ExpeditionNavEntry e)
        {
            if (e == null) return null;
            return new ExpeditionNavEntry
            {
                Route = e.Route,
                Title = e.Title,
                Badge = e.Badge,
                Category = e.Category,
                Origin = e.Origin,
                Detail = e.Detail,
                VisitedUtc = e.VisitedUtc
            };
        }

        private static ExpeditionRoute R(string id, string badge, string category, string title, string description, Action<Window, DeepSystemsContext> open)
        {
            return new ExpeditionRoute { Id = id, Badge = badge, Category = category, Title = title, Description = description, Open = open };
        }

        private static List<ExpeditionRoute> Routes()
        {
            if (RouteCache != null) return RouteCache;
            RouteCache = new List<ExpeditionRoute>
            {
                R("atlas","ATLAS","FOUNDATION","System Atlas","The deep map of YOMI's live control plane and all civilization portals.", delegate(Window w, DeepSystemsContext c){ DeepSystems.OpenAtlasFromContext(w); }),
                R("temporal","ΔT","FOUNDATION","Temporal Observatory","History, epoch drift, state correlation, branches and counterfactual timelines.", delegate(Window w, DeepSystemsContext c){ TemporalObservatory.Open(w,c); }),
                R("causal","GRAPH","FOUNDATION","Causal Graph Lab","Causal structure, evidence chains, interventions and counterfactual relationship traversal.", delegate(Window w, DeepSystemsContext c){ CausalGraphLab.Open(w,c); }),
                R("research","R&D","FOUNDATION","Research Workbench","Leads, hypotheses, anomalies, evidence synthesis and persistent research case files.", delegate(Window w, DeepSystemsContext c){ ResearchWorkbench.Open(w,c); }),
                R("algorithmic","CS","FOUNDATION","Algorithmic Conservatory","Algorithms and coding theory explored through real structures, proofs and simulations.", delegate(Window w, DeepSystemsContext c){ AlgorithmicConservatory.Open(w,c); }),
                R("productivity","WORK","FOUNDATION","Productivity Fabric","Recursive missions, dependency-aware actions, research debt and resumable deep work.", delegate(Window w, DeepSystemsContext c){ ProductivityFabric.Open(w,c); }),
                R("abyssal","HADAL","FOUNDATION","Abyssal Knowledge Engine","Recursive descents through ancestry, consequences, divergence, null space and unanswered questions.", delegate(Window w, DeepSystemsContext c){ AbyssalKnowledgeEngine.Open(w,c); }),
                R("world","KERNEL","CIVILIZATION","Universal World Kernel","Stable object passports, cross-system identity graph, schema cartography and whole-world epochs.", delegate(Window w, DeepSystemsContext c){ WorldKernel.Open(w,c); }),
                R("civilization","CIV","CIVILIZATION","Civilization Layer / World Institutions","Institutions, constitutions, councils, resolutions, commons, treaties and chronicles.", delegate(Window w, DeepSystemsContext c){ CivilizationLayer.Open(w,c); }),
                R("logic","⊢4V","CIVILIZATION","Epistemic Logic Foundry","Paraconsistent truth, proof DAGs, defeasible rules, contradiction cores and falsification.", delegate(Window w, DeepSystemsContext c){ EpistemicLogicFoundry.Open(w,c); }),
                R("verification","⊨CTL","CIVILIZATION","Axiomatic Verification Reactor","Finite-state models, CTL, invariants, counterexamples, bisimulation and proof certificates.", delegate(Window w, DeepSystemsContext c){ AxiomaticVerificationReactor.Open(w,c); }),
                R("strategy","do(X)","CIVILIZATION","Counterfactual Strategy Superstructure","Structural causal models, interventions, Pareto frontiers, robustness and regret.", delegate(Window w, DeepSystemsContext c){ CounterfactualStrategySuperstructure.Open(w,c); }),
                R("science","I(H;Y)","CIVILIZATION","Scientific Discovery Hyperstructure","Competing hypotheses, experimental design, Bayesian updating and research portfolios.", delegate(Window w, DeepSystemsContext c){ ScientificDiscoveryHyperstructure.Open(w,c); }),
                R("engineering","ΣSYS","CIVILIZATION","Dyson Systems Engineering Megastructure","Requirements, architecture, interfaces, reliability, FMEA, trade studies and safety cases.", delegate(Window w, DeepSystemsContext c){ DysonSystemsEngineeringMegastructure.Open(w,c); }),
                R("cybernetic","x̂|u","CIVILIZATION","Cybernetic Digital Twin Metastructure","State estimation, controllability, observability, LQR, MPC, reachability and fault isolation.", delegate(Window w, DeepSystemsContext c){ CyberneticDigitalTwinMetastructure.Open(w,c); }),
                R("complex","ΣPOP","CONCRETE","Complex Systems Laboratory","Agent populations, interaction networks, emergence, attractors, criticality and resilience.", delegate(Window w, DeepSystemsContext c){ ComplexSystemsLaboratory.Open(w,c); }),
                R("or","ORΩ","CONCRETE","Operations Research Empire","Routing, scheduling, assignment, flow, staffing, facility location and portfolio optimization.", delegate(Window w, DeepSystemsContext c){ OperationsResearchEmpire.Open(w,c); }),
                R("economy","ECONΩ","CONCRETE","Economic Civilization","Firms, workers, goods, prices, contracts, banks, credit, taxation, trade and crises.", delegate(Window w, DeepSystemsContext c){ EconomicCivilization.Open(w,c); }),
                R("spatial","GEOΩ","CONCRETE","Spatial World Cartography","Places, territories, topology, routes, accessibility, hazards and infrastructure dependencies.", delegate(Window w, DeepSystemsContext c){ SpatialWorldCartography.Open(w,c); }),
                R("distributed","DISTΩ","CONCRETE","Distributed Systems Planetarium","Nodes, clocks, elections, replication, quorums, partitions, transactions and chaos campaigns.", delegate(Window w, DeepSystemsContext c){ DistributedSystemsPlanetarium.Open(w,c); }),
                R("security","SECΩ","CONCRETE","Adversarial Security Fortress","Simulation-only defensive assurance, trust boundaries, blast radius, segmentation and containment.", delegate(Window w, DeepSystemsContext c){ AdversarialSecurityFortress.Open(w,c); }),
                R("data","DATAΩ","CONCRETE","Data Foundry","Dataset profiling, field semantics, provenance, lineage, cleaning, contracts, validation and drift.", delegate(Window w, DeepSystemsContext c){ DataFoundry.Open(w,c); }),
                R("genome","GENOMEΩ","CONCRETE","Software Genome Observatory","Codebase anatomy, symbols, dependencies, blast radius, ownership, co-change and architecture drift.", delegate(Window w, DeepSystemsContext c){ SoftwareGenomeObservatory.Open(w,c); }),
                R("documents","DOCΩ","CONCRETE","Living Document & Intelligence Factory","Evidence-wired reports, manuals, dossiers and specifications with immutable version genealogy.", delegate(Window w, DeepSystemsContext c){ LivingDocumentIntelligenceFactory.Open(w,c); }),
                R("expedition","EXPΩ","META","Grand Unified Expedition Command","Question-first cross-system investigations with branches, findings, checkpoints and exact resume.", delegate(Window w, DeepSystemsContext c){ GrandUnifiedExpeditionCommand.Open(w,c); }),
                R("constellation","METAΩ","META","Knowledge Constellation Metastructure","Cross-mission provenance hypergraph, contradictions, affinities, frontiers and follow-up expeditions.", delegate(Window w, DeepSystemsContext c){ KnowledgeConstellationMetastructure.Open(w,c); }),
                R("synthesis","SYNTHΩ","META","Civilizational Synthesis Reactor","Cross-mission recurrences, motifs, counterexamples, theorem candidates and transfer risk.", delegate(Window w, DeepSystemsContext c){ CivilizationalSynthesisReactor.Open(w,c); }),
                R("void","VOIDΩ","META","Unknown Unknowns Observatory","Structural absence, monoculture, neglected lenses, unchallenged assumptions and theory-test debt.", delegate(Window w, DeepSystemsContext c){ UnknownUnknownsObservatory.Open(w,c); })
            };
            return RouteCache;
        }

        private static ExpeditionRoute Resolve(string id)
        {
            if (String.IsNullOrWhiteSpace(id)) return null;
            return Routes().FirstOrDefault(x => String.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static void Initialize(DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            string path = WorkspacePath(ctx);
            lock (Gate)
            {
                if (String.Equals(LoadedPath, path, StringComparison.OrdinalIgnoreCase)) return;
                LoadedPath = path;
                State = new ExpeditionWorkspaceState();
                try
                {
                    if (File.Exists(path))
                    {
                        ExpeditionWorkspaceState parsed = ctx.Json.Deserialize<ExpeditionWorkspaceState>(File.ReadAllText(path));
                        if (parsed != null && parsed.Schema <= 1)
                        {
                            if (parsed.Back == null) parsed.Back = new List<ExpeditionNavEntry>();
                            if (parsed.Forward == null) parsed.Forward = new List<ExpeditionNavEntry>();
                            if (parsed.Recent == null) parsed.Recent = new List<ExpeditionNavEntry>();
                            if (parsed.Pins == null) parsed.Pins = new List<ExpeditionNavEntry>();
                            State = parsed;
                        }
                    }
                }
                catch
                {
                    State = new ExpeditionWorkspaceState();
                }
                Normalize();
            }
        }

        private static void Normalize()
        {
            if (State == null) State = new ExpeditionWorkspaceState();
            if (State.Back == null) State.Back = new List<ExpeditionNavEntry>();
            if (State.Forward == null) State.Forward = new List<ExpeditionNavEntry>();
            if (State.Recent == null) State.Recent = new List<ExpeditionNavEntry>();
            if (State.Pins == null) State.Pins = new List<ExpeditionNavEntry>();
            State.Back = State.Back.Where(x => x != null && Resolve(x.Route) != null).ToList();
            if (State.Back.Count > 64) State.Back = State.Back.Skip(State.Back.Count - 64).ToList();
            State.Forward = State.Forward.Where(x => x != null && Resolve(x.Route) != null).ToList();
            if (State.Forward.Count > 64) State.Forward = State.Forward.Skip(State.Forward.Count - 64).ToList();
            State.Recent = State.Recent.Where(x => x != null && Resolve(x.Route) != null).Take(128).ToList();
            State.Pins = State.Pins.Where(x => x != null && Resolve(x.Route) != null)
                                   .GroupBy(x => x.Route, StringComparer.OrdinalIgnoreCase)
                                   .Select(g => g.First()).Take(32).ToList();
            if (State.Current != null && Resolve(State.Current.Route) == null) State.Current = null;
        }

        private static void Save(DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            string path = WorkspacePath(ctx);
            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            State.Schema = 1;
            State.UpdatedUtc = DateTime.UtcNow.ToString("o");
            string json = ctx.Json.Serialize(State);
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temp, json, new System.Text.UTF8Encoding(false));
            if (File.Exists(path))
            {
                string bak = path + ".previous";
                try { File.Replace(temp, path, bak, true); }
                catch { File.Copy(temp, path, true); File.Delete(temp); }
            }
            else File.Move(temp, path);
        }

        private static ExpeditionNavEntry Entry(ExpeditionRoute route, string origin, string detail)
        {
            return new ExpeditionNavEntry
            {
                Route = route.Id,
                Title = route.Title,
                Badge = route.Badge,
                Category = route.Category,
                Origin = origin ?? "",
                Detail = detail ?? route.Description,
                VisitedUtc = DateTime.UtcNow.ToString("o")
            };
        }

        public static void Visit(DeepSystemsContext ctx, string routeId, string origin, string detail)
        {
            if (ctx == null) return;
            Initialize(ctx);
            ExpeditionRoute route = Resolve(routeId);
            if (route == null) return;
            lock (Gate)
            {
                ExpeditionNavEntry next = Entry(route, origin, detail);
                if (State.Current != null && !String.Equals(State.Current.Route, next.Route, StringComparison.OrdinalIgnoreCase))
                {
                    State.Back.Add(Clone(State.Current));
                    if (State.Back.Count > 64) State.Back.RemoveRange(0, State.Back.Count - 64);
                    State.Forward.Clear();
                }
                State.Current = next;
                State.Recent.RemoveAll(x => String.Equals(x.Route, next.Route, StringComparison.OrdinalIgnoreCase));
                State.Recent.Insert(0, Clone(next));
                if (State.Recent.Count > 128) State.Recent.RemoveRange(128, State.Recent.Count - 128);
                Save(ctx);
            }
        }

        private static void OpenRaw(Window owner, DeepSystemsContext ctx, ExpeditionNavEntry target)
        {
            ExpeditionRoute route = Resolve(target == null ? null : target.Route);
            if (route == null || route.Open == null) return;
            route.Open(owner, ctx);
        }

        public static void OpenRoute(Window owner, DeepSystemsContext ctx, string routeId, string origin)
        {
            if (ctx == null) return;
            Initialize(ctx);
            ExpeditionRoute route = Resolve(routeId);
            if (route == null) return;
            ExpeditionWorkspaceState before;
            lock (Gate)
            {
                before = ctx.Json.Deserialize<ExpeditionWorkspaceState>(ctx.Json.Serialize(State));
                Visit(ctx, route.Id, origin, route.Description);
            }
            try { OpenRaw(owner, ctx, State.Current); }
            catch
            {
                lock (Gate)
                {
                    State = before ?? new ExpeditionWorkspaceState();
                    Normalize();
                    Save(ctx);
                }
                throw;
            }
        }

        public static bool CanBack(DeepSystemsContext ctx) { Initialize(ctx); lock (Gate) return State.Back.Count > 0; }
        public static bool CanForward(DeepSystemsContext ctx) { Initialize(ctx); lock (Gate) return State.Forward.Count > 0; }

        public static void Back(Window owner, DeepSystemsContext ctx)
        {
            Initialize(ctx);
            ExpeditionNavEntry target = null;
            ExpeditionWorkspaceState before = null;
            lock (Gate)
            {
                if (State.Back.Count == 0) return;
                before = ctx.Json.Deserialize<ExpeditionWorkspaceState>(ctx.Json.Serialize(State));
                if (State.Current != null)
                {
                    State.Forward.Add(Clone(State.Current));
                    if (State.Forward.Count > 64) State.Forward.RemoveRange(0, State.Forward.Count - 64);
                }
                target = Clone(State.Back[State.Back.Count - 1]);
                State.Back.RemoveAt(State.Back.Count - 1);
                State.Current = Clone(target);
                State.Recent.RemoveAll(x => String.Equals(x.Route, target.Route, StringComparison.OrdinalIgnoreCase));
                State.Recent.Insert(0, Clone(target));
                Save(ctx);
            }
            try { OpenRaw(owner, ctx, target); }
            catch
            {
                lock (Gate) { State = before ?? new ExpeditionWorkspaceState(); Normalize(); Save(ctx); }
                throw;
            }
        }

        public static void Forward(Window owner, DeepSystemsContext ctx)
        {
            Initialize(ctx);
            ExpeditionNavEntry target = null;
            ExpeditionWorkspaceState before = null;
            lock (Gate)
            {
                if (State.Forward.Count == 0) return;
                before = ctx.Json.Deserialize<ExpeditionWorkspaceState>(ctx.Json.Serialize(State));
                if (State.Current != null)
                {
                    State.Back.Add(Clone(State.Current));
                    if (State.Back.Count > 64) State.Back.RemoveRange(0, State.Back.Count - 64);
                }
                target = Clone(State.Forward[State.Forward.Count - 1]);
                State.Forward.RemoveAt(State.Forward.Count - 1);
                State.Current = Clone(target);
                State.Recent.RemoveAll(x => String.Equals(x.Route, target.Route, StringComparison.OrdinalIgnoreCase));
                State.Recent.Insert(0, Clone(target));
                Save(ctx);
            }
            try { OpenRaw(owner, ctx, target); }
            catch
            {
                lock (Gate) { State = before ?? new ExpeditionWorkspaceState(); Normalize(); Save(ctx); }
                throw;
            }
        }

        public static void Resume(Window owner, DeepSystemsContext ctx)
        {
            Initialize(ctx);
            ExpeditionNavEntry current;
            lock (Gate) current = Clone(State.Current);
            if (current == null) OpenRoute(owner, ctx, "expedition", "workspace first resume");
            else OpenRaw(owner, ctx, current);
        }


        public static void TouchDetail(DeepSystemsContext ctx, string origin, string detail)
        {
            Initialize(ctx);
            lock (Gate)
            {
                if (State.Current == null) return;
                State.Current.Origin = origin ?? State.Current.Origin;
                State.Current.Detail = detail ?? State.Current.Detail;
                State.Current.VisitedUtc = DateTime.UtcNow.ToString("o");
                State.Recent.RemoveAll(x => String.Equals(x.Route, State.Current.Route, StringComparison.OrdinalIgnoreCase));
                State.Recent.Insert(0, Clone(State.Current));
                if (State.Recent.Count > 128) State.Recent.RemoveRange(128, State.Recent.Count - 128);
                Save(ctx);
            }
        }

        public static void PinCurrent(DeepSystemsContext ctx)
        {
            Initialize(ctx);
            lock (Gate)
            {
                if (State.Current == null) return;
                State.Pins.RemoveAll(x => String.Equals(x.Route, State.Current.Route, StringComparison.OrdinalIgnoreCase));
                State.Pins.Insert(0, Clone(State.Current));
                if (State.Pins.Count > 32) State.Pins.RemoveRange(32, State.Pins.Count - 32);
                Save(ctx);
            }
        }


        public static void PinRoute(DeepSystemsContext ctx, string routeId, string origin)
        {
            Initialize(ctx);
            ExpeditionRoute route = Resolve(routeId);
            if (route == null) return;
            lock (Gate)
            {
                ExpeditionNavEntry e = Entry(route, origin, route.Description);
                State.Pins.RemoveAll(x => String.Equals(x.Route, e.Route, StringComparison.OrdinalIgnoreCase));
                State.Pins.Insert(0, e);
                if (State.Pins.Count > 32) State.Pins.RemoveRange(32, State.Pins.Count - 32);
                Save(ctx);
            }
        }

        public static void Unpin(DeepSystemsContext ctx, string routeId)
        {
            Initialize(ctx);
            lock (Gate)
            {
                State.Pins.RemoveAll(x => String.Equals(x.Route, routeId, StringComparison.OrdinalIgnoreCase));
                Save(ctx);
            }
        }

        public static string TrailText(DeepSystemsContext ctx)
        {
            Initialize(ctx);
            lock (Gate)
            {
                List<string> lines = new List<string>();
                lines.Add("YOMI EXPEDITION WORKSPACE TRAIL");
                lines.Add("Generated: " + DateTime.UtcNow.ToString("u"));
                lines.Add("Current: " + (State.Current == null ? "∅" : State.Current.ToString()));
                lines.Add("");
                lines.Add("RECENT");
                foreach (ExpeditionNavEntry e in State.Recent.Take(64)) lines.Add((e.VisitedUtc ?? "") + "  " + e.ToString());
                lines.Add("");
                lines.Add("PINS");
                foreach (ExpeditionNavEntry e in State.Pins) lines.Add(e.ToString());
                return String.Join(Environment.NewLine, lines.ToArray());
            }
        }

        private static Button Btn(string text, Action action)
        {
            Button b = new Button
            {
                Content = text,
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 8, 0),
                Background = Raised,
                Foreground = Text,
                BorderBrush = Border,
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.SemiBold
            };
            b.Click += delegate { action(); };
            return b;
        }

        private static TextBlock T(string text, double size, Brush color, FontWeight weight)
        {
            return new TextBlock { Text = text ?? "", FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        }

        private static Border Card(UIElement child)
        {
            return new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(12), Child = child };
        }

        public static void Open(Window owner, DeepSystemsContext ctx)
        {
            if (ctx == null) return;
            Initialize(ctx);
            lock (Gate)
            {
                if (WorkspaceWindow != null && WorkspaceWindow.IsAlive)
                {
                    Window existing = WorkspaceWindow.Target as Window;
                    if (existing != null)
                    {
                        if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
                        existing.Show();
                        existing.Activate();
                        existing.Topmost = true;
                        existing.Topmost = false;
                        existing.Focus();
                        return;
                    }
                }
            }

            Window w = new Window
            {
                Owner = owner,
                Title = "YOMI · EXPEDITION WORKSPACE",
                Width = 1380,
                Height = 860,
                MinWidth = 1080,
                MinHeight = 680,
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Background = Bg,
                Foreground = Text
            };

            lock (Gate) WorkspaceWindow = new WeakReference(w);
            w.Closed += delegate
            {
                lock (Gate)
                {
                    if (WorkspaceWindow != null && Object.ReferenceEquals(WorkspaceWindow.Target, w))
                        WorkspaceWindow = null;
                }
            };

            DockPanel root = new DockPanel { Margin = new Thickness(16) };
            w.Content = root;

            StackPanel header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            header.Children.Add(T("EXPEDITION WORKSPACE", 12, Accent, FontWeights.Bold));
            header.Children.Add(T("One navigation spine across every recovered YOMI civilization. Browser-style history is local workspace state; EXPΩ remains the authoritative deep mission/checkpoint journal.", 13, Muted, FontWeights.Normal));

            WrapPanel tools = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            header.Children.Add(tools);

            Button back = Btn("← BACK", delegate { Back(w, ctx); });
            Button forward = Btn("FORWARD →", delegate { Forward(w, ctx); });
            Button resume = Btn("RESUME CURRENT", delegate { Resume(w, ctx); });
            Button pin = Btn("PIN CURRENT", delegate { PinCurrent(ctx); });
            Button exp = Btn("ENTER EXPΩ", delegate { OpenRoute(w, ctx, "expedition", "workspace"); });
            Button spine = Btn("OBJECT SPINE", delegate { WorldPassportSpine.OpenHub(w, ctx); });
            Button trail = Btn("COPY TRAIL", delegate { try { Clipboard.SetText(TrailText(ctx)); } catch { } });
            tools.Children.Add(back); tools.Children.Add(forward); tools.Children.Add(resume); tools.Children.Add(pin); tools.Children.Add(exp); tools.Children.Add(spine); tools.Children.Add(trail);

            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
            root.Children.Add(body);

            Grid left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170) });
            Grid.SetColumn(left, 0); body.Children.Add(Card(left));

            TextBlock recentTitle = T("RECENT WORLDLINE", 11, Accent, FontWeights.Bold);
            recentTitle.Margin = new Thickness(0, 0, 0, 8);
            left.Children.Add(recentTitle);
            ListBox recent = new ListBox { Background = Surface, Foreground = Text, BorderThickness = new Thickness(0) };
            Grid.SetRow(recent, 1); left.Children.Add(recent);
            TextBlock pinTitle = T("PINNED PORTALS", 11, Blue, FontWeights.Bold);
            pinTitle.Margin = new Thickness(0, 10, 0, 6); Grid.SetRow(pinTitle, 2); left.Children.Add(pinTitle);
            ListBox pins = new ListBox { Background = Surface, Foreground = Text, BorderThickness = new Thickness(0) };
            Grid.SetRow(pins, 3); left.Children.Add(pins);

            Grid center = new Grid();
            center.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            center.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(center, 2); body.Children.Add(Card(center));
            TextBox search = new TextBox { Height = 34, Margin = new Thickness(0,0,0,8), Background = Raised, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10,6,10,6), Text = "" };
            center.Children.Add(search);
            ListBox catalog = new ListBox { Background = Surface, Foreground = Text, BorderThickness = new Thickness(0) };
            Grid.SetRow(catalog, 1); center.Children.Add(catalog);

            StackPanel right = new StackPanel();
            Grid.SetColumn(right, 4); body.Children.Add(Card(right));
            TextBlock detailBadge = T("EXPΩ", 11, Accent, FontWeights.Bold);
            TextBlock detailTitle = T("Select a civilization", 19, Text, FontWeights.Bold);
            detailTitle.Margin = new Thickness(0, 6, 0, 6);
            TextBlock detailCategory = T("SYSTEM", 11, Blue, FontWeights.Bold);
            TextBlock detailDescription = T("Choose any recovered subsystem from the catalog. Open it, pin it, navigate away, then come back through the workspace history.", 13, Muted, FontWeights.Normal);
            detailDescription.Margin = new Thickness(0, 10, 0, 14);
            right.Children.Add(detailBadge); right.Children.Add(detailTitle); right.Children.Add(detailCategory); right.Children.Add(detailDescription);
            WrapPanel detailButtons = new WrapPanel();
            Button open = Btn("OPEN", delegate { ExpeditionRoute r = catalog.SelectedItem as ExpeditionRoute; if (r != null) { OpenRoute(w, ctx, r.Id, "workspace catalog"); } });
            Button pinSelected = Btn("PIN", delegate { ExpeditionRoute r = catalog.SelectedItem as ExpeditionRoute; if (r != null) { PinRoute(ctx,r.Id,"workspace catalog"); } });
            detailButtons.Children.Add(open); detailButtons.Children.Add(pinSelected); right.Children.Add(detailButtons);

            Border law = new Border { Margin = new Thickness(0,18,0,0), Padding = new Thickness(10), Background = Raised, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
            law.Child = T("RESOURCE LAW\nClosed workspace: 0 workers · 0 downloaders · 0 decoders · 0 pollers · 0 network requests. Navigation writes only tiny local JSON at semantic transitions.", 11, Amber, FontWeights.Normal);
            right.Children.Add(law);

            TextBlock footer = T("", 11, Muted, FontWeights.Normal);
            footer.Margin = new Thickness(0, 10, 0, 0);
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            Action updateDetails = delegate
            {
                ExpeditionRoute r = catalog.SelectedItem as ExpeditionRoute;
                if (r == null) return;
                detailBadge.Text = r.Badge;
                detailTitle.Text = r.Title;
                detailCategory.Text = r.Category;
                detailDescription.Text = r.Description;
            };

            Action refreshCatalog = delegate
            {
                string q = (search.Text ?? "").Trim();
                IEnumerable<ExpeditionRoute> rows = Routes();
                if (q.Length > 0)
                {
                    rows = rows.Where(r =>
                        (r.Title ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.Id ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.Badge ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.Category ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.Description ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                catalog.ItemsSource = rows.OrderBy(r => r.Category).ThenBy(r => r.Title).ToList();
                if (catalog.Items.Count > 0 && catalog.SelectedIndex < 0) catalog.SelectedIndex = 0;
            };

            Action refresh = delegate
            {
                Initialize(ctx);
                lock (Gate)
                {
                    recent.ItemsSource = State.Recent.Select(Clone).ToList();
                    pins.ItemsSource = State.Pins.Select(Clone).ToList();
                    back.IsEnabled = State.Back.Count > 0;
                    forward.IsEnabled = State.Forward.Count > 0;
                    resume.IsEnabled = State.Current != null;
                    pin.IsEnabled = State.Current != null;
                    footer.Text = "workspace schema 1   ·   current " + (State.Current == null ? "∅" : State.Current.Badge + " / " + State.Current.Title) +
                                  "   ·   back " + State.Back.Count + "   ·   forward " + State.Forward.Count +
                                  "   ·   recent " + State.Recent.Count + "   ·   pins " + State.Pins.Count +
                                  "   ·   " + WorkspacePath(ctx);
                }
            };

            search.TextChanged += delegate { refreshCatalog(); };
            catalog.SelectionChanged += delegate { updateDetails(); };
            catalog.MouseDoubleClick += delegate { ExpeditionRoute r = catalog.SelectedItem as ExpeditionRoute; if (r != null) { OpenRoute(w, ctx, r.Id, "workspace double-click"); refresh(); } };
            recent.MouseDoubleClick += delegate { ExpeditionNavEntry e = recent.SelectedItem as ExpeditionNavEntry; if (e != null) { OpenRoute(w, ctx, e.Route, "recent worldline"); refresh(); } };
            pins.MouseDoubleClick += delegate { ExpeditionNavEntry e = pins.SelectedItem as ExpeditionNavEntry; if (e != null) { OpenRoute(w, ctx, e.Route, "pinned portal"); refresh(); } };
            pinSelected.Click += delegate { refresh(); };
            pins.PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Delete) { ExpeditionNavEntry x = pins.SelectedItem as ExpeditionNavEntry; if (x != null) { Unpin(ctx,x.Route); refresh(); e.Handled=true; } } };
            w.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.Key == Key.Left) { Back(w,ctx); refresh(); e.Handled=true; }
                else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.Key == Key.Right) { Forward(w,ctx); refresh(); e.Handled=true; }
                else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.P) { PinCurrent(ctx); refresh(); e.Handled=true; }
                else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.R) { Resume(w,ctx); refresh(); e.Handled=true; }
            };

            // Replace handlers that need refresh after state mutation.
            pin.Click += delegate { refresh(); };
            exp.Click += delegate { refresh(); };
            resume.Click += delegate { refresh(); };
            back.Click += delegate { refresh(); };
            forward.Click += delegate { refresh(); };

            refreshCatalog();
            refresh();
            updateDetails();
            w.Show();
        }
    }
}

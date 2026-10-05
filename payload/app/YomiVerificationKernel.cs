using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class VerificationState
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public List<string> Labels { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public VerificationState() { Labels = new List<string>(); Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Name ?? Id ?? "STATE") + "   ·   " + String.Join(", ", Labels.Take(8).ToArray()) + (Labels.Count > 8 ? " …" : ""); }
    }

    internal sealed class VerificationTransition
    {
        public string Id { get; set; }
        public string FromStateId { get; set; }
        public string ToStateId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Guard { get; set; }
        public string Action { get; set; }
        public string FairnessGroup { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public VerificationTransition() { EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? "TRANSITION") + "   ·   " + (FromStateId ?? "∅") + " → " + (ToStateId ?? "∅") + "   ·   " + (Kind ?? "STEP"); }
    }

    internal sealed class VerificationModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public int Revision { get; set; }
        public List<string> InitialStateIds { get; set; }
        public List<VerificationState> States { get; set; }
        public List<VerificationTransition> Transitions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public VerificationModel() { InitialStateIds = new List<string>(); States = new List<VerificationState>(); Transitions = new List<VerificationTransition>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? "MODEL") + "   ·   " + States.Count.ToString(CultureInfo.InvariantCulture) + " states / " + Transitions.Count.ToString(CultureInfo.InvariantCulture) + " transitions   ·   r" + Math.Max(1, Revision).ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class VerificationProperty
    {
        public string Id { get; set; }
        public string ModelId { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }
        public string Kind { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Kind ?? "CTL") + "   ·   " + (Severity ?? "REQUIRED") + "   ·   " + (Name ?? "PROPERTY") + "   ·   " + (Formula ?? "∅"); }
    }

    internal sealed class VerificationRun
    {
        public string Id { get; set; }
        public string ModelId { get; set; }
        public string PropertyId { get; set; }
        public string PropertyName { get; set; }
        public string Formula { get; set; }
        public string NormalizedFormula { get; set; }
        public string Outcome { get; set; }
        public string CheckedUtc { get; set; }
        public string ModelFingerprint { get; set; }
        public string CertificateHash { get; set; }
        public string Diagnostic { get; set; }
        public int ReachableStates { get; set; }
        public int SatisfyingStates { get; set; }
        public int FixedPointIterations { get; set; }
        public List<string> TraceStateIds { get; set; }
        public List<string> TraceTransitionIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public VerificationRun() { TraceStateIds = new List<string>(); TraceTransitionIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Outcome ?? "UNKNOWN") + "   ·   " + (PropertyName ?? Formula ?? "PROPERTY") + "   ·   states " + ReachableStates.ToString(CultureInfo.InvariantCulture) + "   ·   " + (CheckedUtc ?? ""); }
    }

    internal sealed class VerificationInvariantCandidate
    {
        public string Kind;
        public string Formula;
        public int SupportStates;
        public int TotalStates;
        public double StructuralScore;
        public override string ToString() { return (Kind ?? "INVARIANT").PadRight(18) + "   ·   " + StructuralScore.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   " + (Formula ?? "∅") + "   ·   support " + SupportStates.ToString(CultureInfo.InvariantCulture) + "/" + TotalStates.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class VerificationScc
    {
        public int Index;
        public List<string> StateIds = new List<string>();
        public int InternalTransitions;
        public int ExitTransitions;
        public bool Cyclic;
        public bool Terminal;
        public override string ToString() { return "SCC " + Index.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   " + (Cyclic ? "CYCLIC" : "ACYCLIC") + "   ·   " + (Terminal ? "TERMINAL" : "HAS EXIT") + "   ·   " + StateIds.Count.ToString(CultureInfo.InvariantCulture) + " states / " + InternalTransitions.ToString(CultureInfo.InvariantCulture) + " internal / " + ExitTransitions.ToString(CultureInfo.InvariantCulture) + " exits"; }
    }

    internal sealed class VerificationBisimulationClass
    {
        public int Index;
        public string LabelSignature;
        public List<string> StateIds = new List<string>();
        public List<int> SuccessorClasses = new List<int>();
        public override string ToString() { return "≈" + Index.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   " + StateIds.Count.ToString(CultureInfo.InvariantCulture) + " equivalent states   ·   successors {" + String.Join(",", SuccessorClasses.ToArray()) + "}   ·   " + (LabelSignature ?? "∅"); }
    }

    internal sealed class VerificationTopology
    {
        public int TotalStates;
        public int ReachableStates;
        public int UnreachableStates;
        public int TotalTransitions;
        public int Deadlocks;
        public int NondeterministicStates;
        public int StrongComponents;
        public int CyclicComponents;
        public int TerminalComponents;
        public int Diameter;
        public double MeanBranching;
        public double BranchingEntropy;
        public List<string> ReachableStateIds = new List<string>();
        public List<string> UnreachableStateIds = new List<string>();
        public List<string> DeadlockStateIds = new List<string>();
        public List<VerificationScc> Components = new List<VerificationScc>();
    }

    internal sealed class VerificationCheckResult
    {
        public bool Parsed;
        public bool Passed;
        public string Error;
        public string NormalizedFormula;
        public HashSet<string> SatisfyingStateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<string> TraceStateIds = new List<string>();
        public List<string> TraceTransitionIds = new List<string>();
        public string TraceKind;
        public int FixedPointIterations;
        public string ModelFingerprint;
        public string CertificateHash;
    }

    internal sealed class VerificationKernelLimits
    {
        public int MaxStates = 4096;
        public int MaxTransitions = 16384;
        public int MaxFormulaDepth = 96;
        public int MaxFixedPointIterations = 8192;
        public int MaxTraceStates = 4096;
        public int MaxInvariantLabels = 128;
        public int MaxInvariantCandidates = 1024;
        public int MaxExactlyOneFamily = 8;
        public int MaxDiameterSources = 256;
    }

    internal enum TemporalOperator { Atom, True, False, Not, And, Or, Implies, EX, AX, EF, AF, EG, AG, EU, AU }

    internal sealed class TemporalFormula
    {
        public TemporalOperator Operator;
        public string Atom;
        public TemporalFormula Left;
        public TemporalFormula Right;
        public string Display
        {
            get
            {
                switch (Operator)
                {
                    case TemporalOperator.Atom: return Atom ?? "";
                    case TemporalOperator.True: return "TRUE";
                    case TemporalOperator.False: return "FALSE";
                    case TemporalOperator.Not: return "!(" + Left.Display + ")";
                    case TemporalOperator.And: return "(" + Left.Display + " & " + Right.Display + ")";
                    case TemporalOperator.Or: return "(" + Left.Display + " | " + Right.Display + ")";
                    case TemporalOperator.Implies: return "(" + Left.Display + " -> " + Right.Display + ")";
                    case TemporalOperator.EU: return "EU(" + Left.Display + ", " + Right.Display + ")";
                    case TemporalOperator.AU: return "AU(" + Left.Display + ", " + Right.Display + ")";
                    default: return Operator.ToString() + "(" + (Left == null ? "" : Left.Display) + ")";
                }
            }
        }
    }

    internal static class VerificationKernel
    {
        public static VerificationCheckResult Check(VerificationModel model, string formulaText, VerificationKernelLimits limits)
        {
            limits = limits ?? new VerificationKernelLimits(); VerificationCheckResult result = new VerificationCheckResult(); ModelGraph graph = BuildGraph(model, limits); result.ModelFingerprint = Fingerprint(model); TemporalFormula formula; string error; if (!TemporalFormulaParser.TryParse(formulaText, limits.MaxFormulaDepth, out formula, out error)) { result.Error = error; return result; } result.NormalizedFormula = formula.Display; if (graph.States.Count == 0) { result.Error = "Model defines no states."; return result; } if (graph.Initials.Count == 0) { result.Error = "Model defines no valid initial state."; return result; } result.Parsed = true; EvalContext context = new EvalContext { Graph = graph, Limits = limits }; result.SatisfyingStateIds = EvaluateFormula(formula, context); result.FixedPointIterations = context.FixedPointIterations; result.Passed = graph.Initials.All(result.SatisfyingStateIds.Contains); BuildDiagnosticTrace(formula, result.Passed, context, result); string certificatePayload = result.ModelFingerprint + "|" + result.NormalizedFormula + "|" + result.Passed + "|" + String.Join(",", result.SatisfyingStateIds.OrderBy(x => x).ToArray()) + "|" + String.Join(",", result.TraceStateIds.ToArray()) + "|" + String.Join(",", result.TraceTransitionIds.ToArray()); result.CertificateHash = Sha(certificatePayload); return result;
        }

        public static VerificationTopology AnalyzeTopology(VerificationModel model, VerificationKernelLimits limits)
        {
            limits = limits ?? new VerificationKernelLimits(); ModelGraph graph = BuildGraph(model, limits); VerificationTopology report = new VerificationTopology { TotalStates = graph.States.Count, TotalTransitions = graph.Transitions.Count }; HashSet<string> reachable = Reachable(graph, graph.Initials); report.ReachableStateIds = reachable.OrderBy(x => x).ToList(); report.UnreachableStateIds = graph.States.Keys.Where(id => !reachable.Contains(id)).OrderBy(x => x).ToList(); report.ReachableStates = reachable.Count; report.UnreachableStates = report.UnreachableStateIds.Count; report.DeadlockStateIds = reachable.Where(id => RealSuccessors(graph, id).Count == 0).OrderBy(x => x).ToList(); report.Deadlocks = report.DeadlockStateIds.Count; report.NondeterministicStates = reachable.Count(id => RealSuccessors(graph, id).Select(t => t.ToStateId).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1); report.MeanBranching = reachable.Count == 0 ? 0 : reachable.Average(id => RealSuccessors(graph, id).Count); report.BranchingEntropy = BranchingEntropy(graph, reachable); report.Components = StrongComponents(graph, reachable); report.StrongComponents = report.Components.Count; report.CyclicComponents = report.Components.Count(c => c.Cyclic); report.TerminalComponents = report.Components.Count(c => c.Terminal); report.Diameter = Diameter(graph, reachable, limits.MaxDiameterSources); return report;
        }

        public static List<VerificationInvariantCandidate> SynthesizeInvariants(VerificationModel model, VerificationKernelLimits limits)
        {
            limits = limits ?? new VerificationKernelLimits(); ModelGraph graph = BuildGraph(model, limits); HashSet<string> reachable = Reachable(graph, graph.Initials); List<VerificationInvariantCandidate> result = new List<VerificationInvariantCandidate>(); if (reachable.Count == 0) return result; Dictionary<string, HashSet<string>> supports = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); foreach (string id in reachable) foreach (string label in LabelsOf(graph.States[id]).Where(RepresentableAtom)) { HashSet<string> ids; if (!supports.TryGetValue(label, out ids)) { ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); supports[label] = ids; } ids.Add(id); } List<string> labels = supports.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key).Take(limits.MaxInvariantLabels).Select(kv => kv.Key).ToList(); foreach (string label in labels.Where(l => supports[l].Count == reachable.Count)) result.Add(Invariant("UNIVERSAL LABEL", "AG " + QuoteAtom(label), reachable.Count, reachable.Count, 1));
            foreach (string key in reachable.SelectMany(id => graph.States[id].Properties.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Take(limits.MaxInvariantLabels)) { string value; VerificationState representative = graph.States[reachable.First()]; if (!representative.Properties.TryGetValue(key, out value) || !RepresentableAtom(key + "=" + value)) continue; if (reachable.All(id => graph.States[id].Properties.ContainsKey(key) && String.Equals(graph.States[id].Properties[key] ?? "", value ?? "", StringComparison.OrdinalIgnoreCase))) result.Add(Invariant("CONSTANT FIELD", "AG " + QuoteAtom(key + "=" + value), reachable.Count, reachable.Count, 0.98)); }
            for (int i = 0; i < labels.Count; i++) for (int j = i + 1; j < labels.Count; j++) { string a = labels[i], b = labels[j]; HashSet<string> intersection = new HashSet<string>(supports[a], StringComparer.OrdinalIgnoreCase); intersection.IntersectWith(supports[b]); if (intersection.Count == 0 && supports[a].Count > 0 && supports[b].Count > 0) result.Add(Invariant("MUTUAL EXCLUSION", "AG !(" + QuoteAtom(a) + " & " + QuoteAtom(b) + ")", supports[a].Count + supports[b].Count, reachable.Count, 0.85)); if (supports[a].Count >= 2 && supports[a].IsSubsetOf(supports[b]) && supports[a].Count < reachable.Count) result.Add(Invariant("IMPLICATION", "AG (" + QuoteAtom(a) + " -> " + QuoteAtom(b) + ")", supports[a].Count, reachable.Count, 0.75 + 0.2 * supports[a].Count / reachable.Count)); if (supports[b].Count >= 2 && supports[b].IsSubsetOf(supports[a]) && supports[b].Count < reachable.Count) result.Add(Invariant("IMPLICATION", "AG (" + QuoteAtom(b) + " -> " + QuoteAtom(a) + ")", supports[b].Count, reachable.Count, 0.75 + 0.2 * supports[b].Count / reachable.Count)); if (result.Count >= limits.MaxInvariantCandidates * 2) break; }
            foreach (IGrouping<string, string> group in labels.Where(x => x.Contains(":")).GroupBy(x => x.Substring(0, x.IndexOf(':')), StringComparer.OrdinalIgnoreCase)) { List<string> members = group.ToList(); if (members.Count < 2 || members.Count > limits.MaxExactlyOneFamily) continue; bool exactlyOne = reachable.All(id => members.Count(label => supports[label].Contains(id)) == 1); if (exactlyOne) result.Add(Invariant("EXACTLY ONE", ExactOneFormula(members), reachable.Count, reachable.Count, 0.99)); }
            return result.OrderByDescending(x => x.StructuralScore).ThenBy(x => x.Kind).ThenBy(x => x.Formula).Take(limits.MaxInvariantCandidates).ToList();
        }

        public static List<VerificationBisimulationClass> MinimizeBisimulation(VerificationModel model, VerificationKernelLimits limits)
        {
            limits = limits ?? new VerificationKernelLimits(); ModelGraph graph = BuildGraph(model, limits); HashSet<string> reachable = Reachable(graph, graph.Initials); List<List<string>> partitions = reachable.GroupBy(id => LabelSignature(graph.States[id]), StringComparer.OrdinalIgnoreCase).Select(g => g.OrderBy(x => x).ToList()).ToList(); bool changed = true; for (int round = 0; changed && round < graph.States.Count + 1; round++) { changed = false; Dictionary<string, int> block = BlockMap(partitions); List<List<string>> refined = new List<List<string>>(); foreach (List<string> partition in partitions) { List<List<string>> splits = partition.GroupBy(id => SuccessorBlockSignature(graph, id, block), StringComparer.OrdinalIgnoreCase).Select(g => g.ToList()).ToList(); refined.AddRange(splits); if (splits.Count > 1) changed = true; } partitions = refined; }
            Dictionary<string, int> finalBlock = BlockMap(partitions); List<VerificationBisimulationClass> result = new List<VerificationBisimulationClass>(); for (int i = 0; i < partitions.Count; i++) { string representative = partitions[i][0]; result.Add(new VerificationBisimulationClass { Index = i, LabelSignature = LabelSignature(graph.States[representative]), StateIds = partitions[i], SuccessorClasses = TotalSuccessorIds(graph, representative).Select(id => finalBlock[id]).Distinct().OrderBy(x => x).ToList() }); } return result.OrderByDescending(x => x.StateIds.Count).ThenBy(x => x.Index).ToList();
        }

        public static string Fingerprint(VerificationModel model)
        {
            if (model == null) return Sha("null-model"); StringBuilder s = new StringBuilder(); s.Append("M:").Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.Name).Append('|').Append(model.Status).Append('|').Append(model.Revision).Append('\n'); foreach (string initial in model.InitialStateIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) s.Append("I:").Append(initial).Append('\n'); foreach (VerificationState state in model.States.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) { s.Append("S:").Append(state.Id).Append('|').Append(state.Name).Append('|').Append(state.Status).Append('|').Append(LabelSignature(state)).Append('|'); foreach (KeyValuePair<string, string> p in state.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) s.Append(p.Key).Append('=').Append(p.Value).Append(';'); s.Append('\n'); } foreach (VerificationTransition transition in model.Transitions.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("T:").Append(transition.Id).Append('|').Append(transition.FromStateId).Append('|').Append(transition.ToStateId).Append('|').Append(transition.Name).Append('|').Append(transition.Kind).Append('|').Append(transition.Status).Append('|').Append(transition.Guard).Append('|').Append(transition.Action).Append('|').Append(transition.FairnessGroup).Append('\n'); return Sha(s.ToString());
        }

        private static HashSet<string> EvaluateFormula(TemporalFormula f, EvalContext c)
        {
            HashSet<string> cached; string key = f.Display; if (c.Cache.TryGetValue(key, out cached)) return new HashSet<string>(cached, StringComparer.OrdinalIgnoreCase); HashSet<string> universe = new HashSet<string>(c.Graph.States.Keys, StringComparer.OrdinalIgnoreCase), result; switch (f.Operator)
            {
                case TemporalOperator.True: result = universe; break;
                case TemporalOperator.False: result = new HashSet<string>(StringComparer.OrdinalIgnoreCase); break;
                case TemporalOperator.Atom: { string atom = Unquote(f.Atom); if (String.Equals(atom, "initial", StringComparison.OrdinalIgnoreCase)) result = new HashSet<string>(c.Graph.Initials, StringComparer.OrdinalIgnoreCase); else if (String.Equals(atom, "deadlock", StringComparison.OrdinalIgnoreCase)) result = new HashSet<string>(c.Graph.States.Keys.Where(id => RealSuccessors(c.Graph, id).Count == 0), StringComparer.OrdinalIgnoreCase); else if (String.Equals(atom, "reachable", StringComparison.OrdinalIgnoreCase)) result = Reachable(c.Graph, c.Graph.Initials); else result = new HashSet<string>(c.Graph.States.Values.Where(s => HasAtom(s, atom)).Select(s => s.Id), StringComparer.OrdinalIgnoreCase); break; }
                case TemporalOperator.Not: result = universe; result.ExceptWith(EvaluateFormula(f.Left, c)); break;
                case TemporalOperator.And: result = EvaluateFormula(f.Left, c); result.IntersectWith(EvaluateFormula(f.Right, c)); break;
                case TemporalOperator.Or: result = EvaluateFormula(f.Left, c); result.UnionWith(EvaluateFormula(f.Right, c)); break;
                case TemporalOperator.Implies: result = universe; result.ExceptWith(EvaluateFormula(f.Left, c)); result.UnionWith(EvaluateFormula(f.Right, c)); break;
                case TemporalOperator.EX: result = PreExists(c.Graph, EvaluateFormula(f.Left, c)); break;
                case TemporalOperator.AX: result = PreAll(c.Graph, EvaluateFormula(f.Left, c)); break;
                case TemporalOperator.EF: result = LeastFixedPoint(EvaluateFormula(f.Left, c), delegate(HashSet<string> z) { HashSet<string> n = PreExists(c.Graph, z); n.UnionWith(EvaluateFormula(f.Left, c)); return n; }, c); break;
                case TemporalOperator.AF: result = LeastFixedPoint(EvaluateFormula(f.Left, c), delegate(HashSet<string> z) { HashSet<string> n = PreAll(c.Graph, z); n.UnionWith(EvaluateFormula(f.Left, c)); return n; }, c); break;
                case TemporalOperator.EG: result = GreatestFixedPoint(universe, delegate(HashSet<string> z) { HashSet<string> n = PreExists(c.Graph, z); n.IntersectWith(EvaluateFormula(f.Left, c)); return n; }, c); break;
                case TemporalOperator.AG: result = GreatestFixedPoint(universe, delegate(HashSet<string> z) { HashSet<string> n = PreAll(c.Graph, z); n.IntersectWith(EvaluateFormula(f.Left, c)); return n; }, c); break;
                case TemporalOperator.EU: { HashSet<string> left = EvaluateFormula(f.Left, c), right = EvaluateFormula(f.Right, c); result = LeastFixedPoint(right, delegate(HashSet<string> z) { HashSet<string> n = PreExists(c.Graph, z); n.IntersectWith(left); n.UnionWith(right); return n; }, c); break; }
                case TemporalOperator.AU: { HashSet<string> left = EvaluateFormula(f.Left, c), right = EvaluateFormula(f.Right, c); result = LeastFixedPoint(right, delegate(HashSet<string> z) { HashSet<string> n = PreAll(c.Graph, z); n.IntersectWith(left); n.UnionWith(right); return n; }, c); break; }
                default: result = new HashSet<string>(StringComparer.OrdinalIgnoreCase); break;
            }
            c.Cache[key] = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase); return result;
        }

        private static HashSet<string> LeastFixedPoint(HashSet<string> seed, Func<HashSet<string>, HashSet<string>> step, EvalContext c) { HashSet<string> current = new HashSet<string>(seed, StringComparer.OrdinalIgnoreCase); for (int i = 0; i < c.Limits.MaxFixedPointIterations; i++) { c.FixedPointIterations++; HashSet<string> next = step(current); if (next.SetEquals(current)) return current; current = next; } return current; }
        private static HashSet<string> GreatestFixedPoint(HashSet<string> seed, Func<HashSet<string>, HashSet<string>> step, EvalContext c) { HashSet<string> current = new HashSet<string>(seed, StringComparer.OrdinalIgnoreCase); for (int i = 0; i < c.Limits.MaxFixedPointIterations; i++) { c.FixedPointIterations++; HashSet<string> next = step(current); if (next.SetEquals(current)) return current; current = next; } return current; }
        private static HashSet<string> PreExists(ModelGraph graph, HashSet<string> target) { return new HashSet<string>(graph.States.Keys.Where(id => TotalSuccessorIds(graph, id).Any(target.Contains)), StringComparer.OrdinalIgnoreCase); }
        private static HashSet<string> PreAll(ModelGraph graph, HashSet<string> target) { return new HashSet<string>(graph.States.Keys.Where(id => TotalSuccessorIds(graph, id).All(target.Contains)), StringComparer.OrdinalIgnoreCase); }

        private static void BuildDiagnosticTrace(TemporalFormula formula, bool passed, EvalContext context, VerificationCheckResult result)
        {
            List<string> initials = context.Graph.Initials.Count > 0 ? context.Graph.Initials : context.Graph.States.Keys.Take(1).ToList(); HashSet<string> goal = null; string kind = passed ? "WITNESS" : "COUNTEREXAMPLE"; if (formula.Operator == TemporalOperator.AG && !passed) { HashSet<string> good = EvaluateFormula(formula.Left, context); goal = new HashSet<string>(context.Graph.States.Keys.Where(id => !good.Contains(id)), StringComparer.OrdinalIgnoreCase); } else if (formula.Operator == TemporalOperator.EF && passed) goal = EvaluateFormula(formula.Left, context); else if (formula.Operator == TemporalOperator.EX && passed) goal = EvaluateFormula(formula.Left, context); else if (formula.Operator == TemporalOperator.AX && !passed) { HashSet<string> good = EvaluateFormula(formula.Left, context); goal = new HashSet<string>(context.Graph.States.Keys.Where(id => !good.Contains(id)), StringComparer.OrdinalIgnoreCase); } else if (formula.Operator == TemporalOperator.AF && !passed) { HashSet<string> good = EvaluateFormula(formula.Left, context); VerificationScc cycle = StrongComponents(context.Graph, new HashSet<string>(context.Graph.States.Keys.Where(id => !good.Contains(id)), StringComparer.OrdinalIgnoreCase)).FirstOrDefault(c => c.Cyclic && c.Terminal); if (cycle != null) goal = new HashSet<string>(cycle.StateIds, StringComparer.OrdinalIgnoreCase); } else if (formula.Operator == TemporalOperator.EG && passed) { HashSet<string> good = EvaluateFormula(formula.Left, context); VerificationScc cycle = StrongComponents(context.Graph, good).FirstOrDefault(c => c.Cyclic); if (cycle != null) goal = new HashSet<string>(cycle.StateIds, StringComparer.OrdinalIgnoreCase); } if (goal == null) goal = passed ? result.SatisfyingStateIds : new HashSet<string>(context.Graph.States.Keys.Where(id => !result.SatisfyingStateIds.Contains(id)), StringComparer.OrdinalIgnoreCase); Trace trace = ShortestTrace(context.Graph, initials, goal, context.Limits.MaxTraceStates); result.TraceStateIds = trace.StateIds; result.TraceTransitionIds = trace.TransitionIds; result.TraceKind = kind + (trace.CycleClosed ? " · LASSO" : " · SHORTEST PREFIX");
        }

        private static Trace ShortestTrace(ModelGraph graph, IEnumerable<string> starts, HashSet<string> goals, int cap)
        {
            Trace empty = new Trace(); Queue<string> queue = new Queue<string>(); Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); Dictionary<string, string> via = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); foreach (string start in starts.Where(graph.States.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase)) { queue.Enqueue(start); parent[start] = null; } string found = null; while (queue.Count > 0 && parent.Count <= cap) { string current = queue.Dequeue(); if (goals.Contains(current)) { found = current; break; } foreach (VerificationTransition transition in RealSuccessors(graph, current)) if (!parent.ContainsKey(transition.ToStateId)) { parent[transition.ToStateId] = current; via[transition.ToStateId] = transition.Id; queue.Enqueue(transition.ToStateId); } } if (found == null) return empty; List<string> states = new List<string>(); List<string> transitions = new List<string>(); string cursor = found; while (cursor != null) { states.Add(cursor); string previous = parent[cursor]; if (previous != null) transitions.Add(via[cursor]); cursor = previous; } states.Reverse(); transitions.Reverse(); empty.StateIds = states; empty.TransitionIds = transitions; VerificationTransition loop = RealSuccessors(graph, found).FirstOrDefault(t => goals.Contains(t.ToStateId) && states.Contains(t.ToStateId, StringComparer.OrdinalIgnoreCase)); if (loop != null) { empty.TransitionIds.Add(loop.Id); empty.StateIds.Add(loop.ToStateId); empty.CycleClosed = true; } return empty;
        }

        private static ModelGraph BuildGraph(VerificationModel model, VerificationKernelLimits limits)
        {
            ModelGraph graph = new ModelGraph(); if (model == null) return graph; foreach (VerificationState state in (model.States ?? new List<VerificationState>()).Where(s => s != null && !String.IsNullOrWhiteSpace(s.Id)).Take(limits.MaxStates)) if (!graph.States.ContainsKey(state.Id)) graph.States[state.Id] = state; foreach (VerificationTransition transition in (model.Transitions ?? new List<VerificationTransition>()).Where(t => t != null && !String.Equals(t.Status, "DISABLED", StringComparison.OrdinalIgnoreCase) && graph.States.ContainsKey(t.FromStateId ?? "") && graph.States.ContainsKey(t.ToStateId ?? "")).Take(limits.MaxTransitions)) { graph.Transitions.Add(transition); List<VerificationTransition> outgoing; if (!graph.Out.TryGetValue(transition.FromStateId, out outgoing)) { outgoing = new List<VerificationTransition>(); graph.Out[transition.FromStateId] = outgoing; } outgoing.Add(transition); List<VerificationTransition> incoming; if (!graph.In.TryGetValue(transition.ToStateId, out incoming)) { incoming = new List<VerificationTransition>(); graph.In[transition.ToStateId] = incoming; } incoming.Add(transition); } graph.Initials = (model.InitialStateIds ?? new List<string>()).Where(graph.States.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); return graph;
        }

        private static HashSet<string> Reachable(ModelGraph graph, IEnumerable<string> roots) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Queue<string> queue = new Queue<string>(); foreach (string root in roots.Where(graph.States.ContainsKey)) if (seen.Add(root)) queue.Enqueue(root); while (queue.Count > 0) { string id = queue.Dequeue(); foreach (string next in TotalSuccessorIds(graph, id)) if (seen.Add(next)) queue.Enqueue(next); } return seen; }
        private static List<VerificationTransition> RealSuccessors(ModelGraph graph, string id) { List<VerificationTransition> rows; return graph.Out.TryGetValue(id ?? "", out rows) ? rows : new List<VerificationTransition>(); }
        private static IEnumerable<string> TotalSuccessorIds(ModelGraph graph, string id) { List<VerificationTransition> rows = RealSuccessors(graph, id); return rows.Count == 0 ? new[] { id } : rows.Select(t => t.ToStateId).Distinct(StringComparer.OrdinalIgnoreCase); }

        private static List<VerificationScc> StrongComponents(ModelGraph graph, HashSet<string> universe)
        {
            List<List<string>> raw = new List<List<string>>(); Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> onStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int cursor = 0; Action<string> visit = null; visit = delegate(string vertex) { indices[vertex] = cursor; low[vertex] = cursor++; stack.Push(vertex); onStack.Add(vertex); foreach (string next in TotalSuccessorIds(graph, vertex).Where(universe.Contains)) { if (!indices.ContainsKey(next)) { visit(next); low[vertex] = Math.Min(low[vertex], low[next]); } else if (onStack.Contains(next)) low[vertex] = Math.Min(low[vertex], indices[next]); } if (low[vertex] != indices[vertex]) return; List<string> component = new List<string>(); string member; do { member = stack.Pop(); onStack.Remove(member); component.Add(member); } while (!String.Equals(member, vertex, StringComparison.OrdinalIgnoreCase)); raw.Add(component); }; foreach (string id in universe.OrderBy(x => x)) if (!indices.ContainsKey(id)) visit(id); List<VerificationScc> result = new List<VerificationScc>(); for (int i = 0; i < raw.Count; i++) { HashSet<string> members = new HashSet<string>(raw[i], StringComparer.OrdinalIgnoreCase); List<VerificationTransition> from = graph.Transitions.Where(t => members.Contains(t.FromStateId)).ToList(); int internalCount = from.Count(t => members.Contains(t.ToStateId)), exitCount = from.Count(t => !members.Contains(t.ToStateId) && universe.Contains(t.ToStateId)); bool self = raw[i].Count == 1 && from.Any(t => String.Equals(t.FromStateId, t.ToStateId, StringComparison.OrdinalIgnoreCase)); result.Add(new VerificationScc { Index = i, StateIds = raw[i].OrderBy(x => x).ToList(), InternalTransitions = internalCount, ExitTransitions = exitCount, Cyclic = raw[i].Count > 1 || self || (raw[i].Count == 1 && RealSuccessors(graph, raw[i][0]).Count == 0), Terminal = exitCount == 0 }); } return result.OrderByDescending(x => x.StateIds.Count).ThenBy(x => x.Index).ToList();
        }

        private static int Diameter(ModelGraph graph, HashSet<string> universe, int maxSources) { int diameter = 0; foreach (string source in universe.OrderBy(x => x).Take(maxSources)) { Dictionary<string, int> distance = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { source, 0 } }; Queue<string> queue = new Queue<string>(); queue.Enqueue(source); while (queue.Count > 0) { string current = queue.Dequeue(); foreach (string next in TotalSuccessorIds(graph, current).Where(universe.Contains)) if (!distance.ContainsKey(next)) { distance[next] = distance[current] + 1; diameter = Math.Max(diameter, distance[next]); queue.Enqueue(next); } } } return diameter; }
        private static double BranchingEntropy(ModelGraph graph, HashSet<string> universe) { if (universe.Count == 0) return 0; Dictionary<int, int> histogram = universe.GroupBy(id => RealSuccessors(graph, id).Count).ToDictionary(g => g.Key, g => g.Count()); double total = universe.Count, entropy = 0; foreach (int count in histogram.Values) { double p = count / total; entropy -= p * Math.Log(p, 2); } return entropy; }
        private static bool HasAtom(VerificationState state, string atom) { if (state == null) return false; atom = Unquote(atom); if (String.Equals(atom, "initial", StringComparison.OrdinalIgnoreCase)) return false; if (String.Equals(atom, "active", StringComparison.OrdinalIgnoreCase)) return !String.Equals(state.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) && !String.Equals(state.Status, "DISABLED", StringComparison.OrdinalIgnoreCase) && !String.Equals(state.Status, "RETRACTED", StringComparison.OrdinalIgnoreCase); if ((state.Labels ?? new List<string>()).Contains(atom, StringComparer.OrdinalIgnoreCase)) return true; int equals = atom.IndexOf('='); if (equals > 0) { string key = atom.Substring(0, equals).Trim(), expected = atom.Substring(equals + 1).Trim(), actual; return state.Properties != null && state.Properties.TryGetValue(key, out actual) && String.Equals(actual ?? "", expected, StringComparison.OrdinalIgnoreCase); } return false; }
        private static IEnumerable<string> LabelsOf(VerificationState state) { foreach (string label in state.Labels ?? new List<string>()) if (!String.IsNullOrWhiteSpace(label)) yield return label.Trim(); foreach (KeyValuePair<string, string> p in state.Properties ?? new Dictionary<string, string>()) yield return p.Key + "=" + p.Value; }
        private static string LabelSignature(VerificationState state) { List<string> labels = LabelsOf(state).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); labels.Add("@active=" + HasAtom(state, "active").ToString(CultureInfo.InvariantCulture)); return String.Join("|", labels.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()); }
        private static Dictionary<string, int> BlockMap(List<List<string>> partitions) { Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < partitions.Count; i++) foreach (string id in partitions[i]) result[id] = i; return result; }
        private static string SuccessorBlockSignature(ModelGraph graph, string stateId, Dictionary<string, int> blocks) { List<VerificationTransition> transitions = RealSuccessors(graph, stateId); if (transitions.Count == 0) return "τ:" + blocks[stateId].ToString(CultureInfo.InvariantCulture); return String.Join(",", transitions.Where(t => blocks.ContainsKey(t.ToStateId)).Select(t => (t.Kind ?? "STEP") + ":" + (t.Name ?? "") + "→" + blocks[t.ToStateId].ToString(CultureInfo.InvariantCulture)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()); }
        private static VerificationInvariantCandidate Invariant(string kind, string formula, int support, int total, double score) { return new VerificationInvariantCandidate { Kind = kind, Formula = formula, SupportStates = support, TotalStates = total, StructuralScore = Math.Max(0, Math.Min(1, score)) }; }
        private static string ExactOneFormula(List<string> atoms) { string atLeastOne = "(" + String.Join(" | ", atoms.Select(QuoteAtom).ToArray()) + ")"; List<string> exclusions = new List<string>(); for (int i = 0; i < atoms.Count; i++) for (int j = i + 1; j < atoms.Count; j++) exclusions.Add("!(" + QuoteAtom(atoms[i]) + " & " + QuoteAtom(atoms[j]) + ")"); return "AG (" + atLeastOne + " & " + String.Join(" & ", exclusions.ToArray()) + ")"; }
        private static bool RepresentableAtom(string atom) { atom = atom ?? ""; return !(atom.Contains("'") && atom.Contains("\"")); }
        private static string QuoteAtom(string atom) { atom = atom ?? ""; bool quote = atom.Any(c => Char.IsWhiteSpace(c) || "()!&|,->'\"".IndexOf(c) >= 0); if (!quote) return atom; return atom.Contains("'") ? "\"" + atom + "\"" : "'" + atom + "'"; }
        private static string Unquote(string atom) { atom = (atom ?? "").Trim(); return atom.Length >= 2 && ((atom[0] == '\'' && atom[atom.Length - 1] == '\'') || (atom[0] == '"' && atom[atom.Length - 1] == '"')) ? atom.Substring(1, atom.Length - 2) : atom; }
        private static string Sha(string text) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "").ToLowerInvariant(); }

        private sealed class ModelGraph
        {
            public Dictionary<string, VerificationState> States = new Dictionary<string, VerificationState>(StringComparer.OrdinalIgnoreCase);
            public List<VerificationTransition> Transitions = new List<VerificationTransition>();
            public Dictionary<string, List<VerificationTransition>> Out = new Dictionary<string, List<VerificationTransition>>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<VerificationTransition>> In = new Dictionary<string, List<VerificationTransition>>(StringComparer.OrdinalIgnoreCase);
            public List<string> Initials = new List<string>();
        }

        private sealed class EvalContext
        {
            public ModelGraph Graph;
            public VerificationKernelLimits Limits;
            public int FixedPointIterations;
            public Dictionary<string, HashSet<string>> Cache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class Trace { public List<string> StateIds = new List<string>(); public List<string> TransitionIds = new List<string>(); public bool CycleClosed; }
    }

    internal static class TemporalFormulaParser
    {
        public static bool TryParse(string text, int maxDepth, out TemporalFormula formula, out string error)
        {
            formula = null; error = null; try { Parser parser = new Parser(text, maxDepth); formula = parser.Parse(); return true; } catch (Exception ex) { error = ex.Message; return false; }
        }

        private sealed class Parser
        {
            private readonly List<string> Tokens;
            private readonly int MaxDepth;
            private int Position;
            public Parser(string text, int maxDepth) { Tokens = Tokenize(text); MaxDepth = Math.Max(8, maxDepth); }
            public TemporalFormula Parse() { if (Tokens.Count == 0) throw new FormatException("Formula is empty."); TemporalFormula f = ParseImplies(0); if (Position != Tokens.Count) throw new FormatException("Unexpected token '" + Tokens[Position] + "'."); return f; }
            private TemporalFormula ParseImplies(int depth) { TemporalFormula left = ParseOr(depth + 1); if (Take("->")) return Binary(TemporalOperator.Implies, left, ParseImplies(depth + 1)); return left; }
            private TemporalFormula ParseOr(int depth) { TemporalFormula left = ParseAnd(depth + 1); while (Take("|") || TakeWord("OR")) left = Binary(TemporalOperator.Or, left, ParseAnd(depth + 1)); return left; }
            private TemporalFormula ParseAnd(int depth) { TemporalFormula left = ParseUnary(depth + 1); while (Take("&") || TakeWord("AND")) left = Binary(TemporalOperator.And, left, ParseUnary(depth + 1)); return left; }
            private TemporalFormula ParseUnary(int depth)
            {
                GuardDepth(depth); if (Take("!") || TakeWord("NOT")) return Unary(TemporalOperator.Not, ParseUnary(depth + 1)); foreach (TemporalOperator op in new[] { TemporalOperator.EX, TemporalOperator.AX, TemporalOperator.EF, TemporalOperator.AF, TemporalOperator.EG, TemporalOperator.AG }) if (TakeWord(op.ToString())) { bool grouped = Take("("); TemporalFormula child = grouped ? ParseImplies(depth + 1) : ParseUnary(depth + 1); if (grouped) Require(")"); return Unary(op, child); } if (TakeWord("EU")) return ParseUntil(TemporalOperator.EU, depth); if (TakeWord("AU")) return ParseUntil(TemporalOperator.AU, depth); if (Take("(")) { TemporalFormula nested = ParseImplies(depth + 1); Require(")"); return nested; } string token = Next(); if (String.Equals(token, "TRUE", StringComparison.OrdinalIgnoreCase)) return new TemporalFormula { Operator = TemporalOperator.True }; if (String.Equals(token, "FALSE", StringComparison.OrdinalIgnoreCase)) return new TemporalFormula { Operator = TemporalOperator.False }; return new TemporalFormula { Operator = TemporalOperator.Atom, Atom = token };
            }
            private TemporalFormula ParseUntil(TemporalOperator op, int depth) { Require("("); TemporalFormula left = ParseImplies(depth + 1); Require(","); TemporalFormula right = ParseImplies(depth + 1); Require(")"); return Binary(op, left, right); }
            private TemporalFormula Unary(TemporalOperator op, TemporalFormula child) { return new TemporalFormula { Operator = op, Left = child }; }
            private TemporalFormula Binary(TemporalOperator op, TemporalFormula left, TemporalFormula right) { return new TemporalFormula { Operator = op, Left = left, Right = right }; }
            private void GuardDepth(int depth) { if (depth > MaxDepth) throw new FormatException("Formula nesting exceeds " + MaxDepth.ToString(CultureInfo.InvariantCulture) + "."); }
            private bool Take(string token) { if (Position < Tokens.Count && String.Equals(Tokens[Position], token, StringComparison.OrdinalIgnoreCase)) { Position++; return true; } return false; }
            private bool TakeWord(string word) { return Take(word); }
            private void Require(string token) { if (!Take(token)) throw new FormatException("Expected '" + token + "' at token " + Position.ToString(CultureInfo.InvariantCulture) + "."); }
            private string Next() { if (Position >= Tokens.Count) throw new FormatException("Unexpected end of formula."); string token = Tokens[Position++]; if (token == "(" || token == ")" || token == "," || token == "!" || token == "&" || token == "|" || token == "->") throw new FormatException("Expected proposition at token " + (Position - 1).ToString(CultureInfo.InvariantCulture) + "."); return token; }
            private static List<string> Tokenize(string text)
            {
                List<string> tokens = new List<string>(); StringBuilder current = new StringBuilder(); char quote = '\0'; Action flush = delegate { if (current.Length > 0) { tokens.Add(current.ToString()); current.Length = 0; } }; for (int i = 0; i < (text ?? "").Length; i++) { char c = text[i]; if (quote != '\0') { current.Append(c); if (c == quote) { quote = '\0'; flush(); } continue; } if (c == '\'' || c == '"') { flush(); quote = c; current.Append(c); continue; } if (Char.IsWhiteSpace(c)) { flush(); continue; } if (c == '-' && i + 1 < text.Length && text[i + 1] == '>') { flush(); tokens.Add("->"); i++; continue; } if ("()!&|,".IndexOf(c) >= 0) { flush(); tokens.Add(c.ToString()); continue; } current.Append(c); } if (quote != '\0') throw new FormatException("Unterminated quoted proposition."); flush(); return tokens;
            }
        }
    }
}

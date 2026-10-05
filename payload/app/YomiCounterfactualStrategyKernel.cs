using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    // DEV13.37.14 pure strategy kernel.  This file deliberately owns no WPF,
    // filesystem, process, playback, scheduler, Oracle or Aegis dependency.
    internal sealed class StrategyVariable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public double Baseline { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Intercept { get; set; }
        public bool Uncertain { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public StrategyVariable() { Kind = "ENDOGENOUS"; Status = "ACTIVE"; Minimum = -1000000; Maximum = 1000000; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Kind ?? "VARIABLE").PadRight(11) + "   ·   " + (Name ?? Id ?? "VARIABLE") + "   ·   base " + Baseline.ToString("0.####", CultureInfo.InvariantCulture) + " " + (Unit ?? "") + (Uncertain ? "   ·   UNCERTAIN" : ""); }
    }

    internal sealed class StrategyEdge
    {
        public string Id { get; set; }
        public string FromVariableId { get; set; }
        public string ToVariableId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Transform { get; set; }
        public double Coefficient { get; set; }
        public double Threshold { get; set; }
        public int Lag { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public StrategyEdge() { Kind = "CAUSAL"; Status = "ACTIVE"; Transform = "LINEAR"; Coefficient = 1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Kind ?? "EDGE") + "   ·   " + (FromVariableId ?? "∅") + " → " + (ToVariableId ?? "∅") + "   ·   β=" + Coefficient.ToString("0.####", CultureInfo.InvariantCulture) + (Lag > 0 ? "   lag " + Lag.ToString(CultureInfo.InvariantCulture) : ""); }
    }

    internal sealed class StrategyIntervention
    {
        public string Id { get; set; }
        public string VariableId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public double Value { get; set; }
        public int StartPeriod { get; set; }
        public int EndPeriod { get; set; }
        public string Rationale { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public StrategyIntervention() { Kind = "DO"; EndPeriod = Int32.MaxValue; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Kind ?? "DO") + "(" + (VariableId ?? "∅") + "=" + Value.ToString("0.####", CultureInfo.InvariantCulture) + ")   ·   t" + StartPeriod.ToString(CultureInfo.InvariantCulture) + "…" + (EndPeriod == Int32.MaxValue ? "∞" : EndPeriod.ToString(CultureInfo.InvariantCulture)); }
    }

    internal sealed class StrategyObjective
    {
        public string Id { get; set; }
        public string VariableId { get; set; }
        public string Name { get; set; }
        public string Direction { get; set; }
        public string Status { get; set; }
        public double Weight { get; set; }
        public double Aspiration { get; set; }
        public override string ToString() { return (Direction ?? "MAXIMIZE").PadRight(8) + "   ·   w=" + Weight.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Name ?? VariableId ?? "OBJECTIVE") + "   ·   aspiration " + Aspiration.ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class StrategyConstraint
    {
        public string Id { get; set; }
        public string VariableId { get; set; }
        public string Name { get; set; }
        public string Operator { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public double Threshold { get; set; }
        public double Penalty { get; set; }
        public override string ToString() { return (Severity ?? "HARD").PadRight(8) + "   ·   " + (Name ?? VariableId ?? "CONSTRAINT") + "   ·   " + (VariableId ?? "∅") + " " + (Operator ?? ">=") + " " + Threshold.ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class StrategyPolicy
    {
        public string Id { get; set; }
        public string ParentPolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<StrategyIntervention> Interventions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public StrategyPolicy() { Status = "CANDIDATE"; Interventions = new List<StrategyIntervention>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "CANDIDATE") + "   ·   " + (Name ?? Id ?? "POLICY") + "   ·   " + Interventions.Count.ToString(CultureInfo.InvariantCulture) + " interventions"; }
    }

    internal sealed class StrategyModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Name { get; set; }
        public string Charter { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public int Revision { get; set; }
        public int Horizon { get; set; }
        public List<StrategyVariable> Variables { get; set; }
        public List<StrategyEdge> Edges { get; set; }
        public List<StrategyPolicy> Policies { get; set; }
        public List<StrategyObjective> Objectives { get; set; }
        public List<StrategyConstraint> Constraints { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public StrategyModel() { Status = "DRAFT"; Revision = 1; Horizon = 1; Variables = new List<StrategyVariable>(); Edges = new List<StrategyEdge>(); Policies = new List<StrategyPolicy>(); Objectives = new List<StrategyObjective>(); Constraints = new List<StrategyConstraint>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "DRAFT") + "   ·   " + (Name ?? Id ?? "MODEL") + "   ·   " + Variables.Count.ToString(CultureInfo.InvariantCulture) + "V / " + Edges.Count.ToString(CultureInfo.InvariantCulture) + "E / " + Policies.Count.ToString(CultureInfo.InvariantCulture) + " policies   ·   r" + Math.Max(1, Revision).ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class StrategyUncertaintyCase
    {
        public int Index { get; set; }
        public string Id { get; set; }
        public Dictionary<string, double> ExogenousValues { get; set; }
        public StrategyUncertaintyCase() { ExogenousValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return "CASE " + Index.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   " + String.Join("   ", ExogenousValues.Take(6).Select(x => x.Key + "=" + x.Value.ToString("0.###", CultureInfo.InvariantCulture)).ToArray()); }
    }

    internal sealed class StrategySimulationResult
    {
        public string ModelId { get; set; }
        public string PolicyId { get; set; }
        public string CaseId { get; set; }
        public string ModelFingerprint { get; set; }
        public string PolicyFingerprint { get; set; }
        public bool Converged { get; set; }
        public int Iterations { get; set; }
        public bool Feasible { get; set; }
        public double SoftPenalty { get; set; }
        public Dictionary<string, double> FinalValues { get; set; }
        public Dictionary<string, double> ObjectiveValues { get; set; }
        public List<string> Violations { get; set; }
        public List<Dictionary<string, double>> Timeline { get; set; }
        public StrategySimulationResult() { Converged = true; Feasible = true; FinalValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); ObjectiveValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Violations = new List<string>(); Timeline = new List<Dictionary<string, double>>(); }
    }

    internal sealed class StrategyPolicyEvaluation
    {
        public string PolicyId { get; set; }
        public string PolicyName { get; set; }
        public int Cases { get; set; }
        public int FeasibleCases { get; set; }
        public double Feasibility { get; set; }
        public double MeanUtility { get; set; }
        public double MedianUtility { get; set; }
        public double P05Utility { get; set; }
        public double WorstUtility { get; set; }
        public double LowerTailUtility { get; set; }
        public double UtilityDeviation { get; set; }
        public double MeanRegret { get; set; }
        public double MaximumRegret { get; set; }
        public double Robustness { get; set; }
        public bool ParetoEfficient { get; set; }
        public Dictionary<string, double> MeanObjectives { get; set; }
        public List<double> CaseUtilities { get; set; }
        public List<double> CaseRegrets { get; set; }
        public StrategyPolicyEvaluation() { MeanObjectives = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); CaseUtilities = new List<double>(); CaseRegrets = new List<double>(); }
        public override string ToString() { return (ParetoEfficient ? "PARETO" : "      ") + "   ·   " + (PolicyName ?? PolicyId ?? "POLICY") + "   ·   robust " + Robustness.ToString("P1", CultureInfo.InvariantCulture) + "   ·   μ " + MeanUtility.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   worst " + WorstUtility.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   regret≤" + MaximumRegret.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   feasible " + Feasibility.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class StrategyPortfolioAnalysis
    {
        public string ModelId { get; set; }
        public string ModelFingerprint { get; set; }
        public string AnalysisFingerprint { get; set; }
        public string CertificateHash { get; set; }
        public int CaseCount { get; set; }
        public string RecommendedPolicyId { get; set; }
        public string RecommendationRule { get; set; }
        public List<StrategyUncertaintyCase> Cases { get; set; }
        public List<StrategyPolicyEvaluation> Policies { get; set; }
        public List<StrategySimulationResult> Simulations { get; set; }
        public StrategyPortfolioAnalysis() { Cases = new List<StrategyUncertaintyCase>(); Policies = new List<StrategyPolicyEvaluation>(); Simulations = new List<StrategySimulationResult>(); }
    }

    internal sealed class StrategySensitivity
    {
        public string InputVariableId { get; set; }
        public string OutcomeVariableId { get; set; }
        public double BaselineInput { get; set; }
        public double Delta { get; set; }
        public double LocalEffect { get; set; }
        public double Elasticity { get; set; }
        public string Sign { get; set; }
        public override string ToString() { return (InputVariableId ?? "INPUT").PadRight(28) + " → " + (OutcomeVariableId ?? "OUTCOME").PadRight(28) + "   ·   ∂y/∂x≈" + LocalEffect.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   ε≈" + Elasticity.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   " + (Sign ?? "NEUTRAL"); }
    }

    internal sealed class StrategyCausalPath
    {
        public List<string> VariableIds { get; set; }
        public List<string> EdgeIds { get; set; }
        public double PathCoefficient { get; set; }
        public StrategyCausalPath() { VariableIds = new List<string>(); EdgeIds = new List<string>(); }
        public override string ToString() { return PathCoefficient.ToString("+0.######;-0.######;0", CultureInfo.InvariantCulture).PadLeft(14) + "   ·   " + String.Join(" → ", VariableIds.ToArray()); }
    }

    internal sealed class StrategyGraphDiagnostics
    {
        public int Variables { get; set; }
        public int Edges { get; set; }
        public int Exogenous { get; set; }
        public int Decisions { get; set; }
        public int Outcomes { get; set; }
        public int Components { get; set; }
        public int CyclicComponents { get; set; }
        public bool Acyclic { get; set; }
        public List<List<string>> StrongComponents { get; set; }
        public List<string> TopologicalOrder { get; set; }
        public List<string> Colliders { get; set; }
        public List<string> OrphanVariables { get; set; }
        public List<string> Audit { get; set; }
        public StrategyGraphDiagnostics() { StrongComponents = new List<List<string>>(); TopologicalOrder = new List<string>(); Colliders = new List<string>(); OrphanVariables = new List<string>(); Audit = new List<string>(); }
    }

    internal sealed class StrategyInformationValue
    {
        public string VariableId { get; set; }
        public string VariableName { get; set; }
        public double PerfectInformationValue { get; set; }
        public double ThreeBinInformationValue { get; set; }
        public int Cases { get; set; }
        public override string ToString() { return (VariableName ?? VariableId ?? "VARIABLE").PadRight(34) + "   ·   EVPI " + PerfectInformationValue.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   3-bin EVSI≈" + ThreeBinInformationValue.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12); }
    }

    internal sealed class StrategyAttribution
    {
        public string InterventionId { get; set; }
        public string VariableId { get; set; }
        public double ShapleyValue { get; set; }
        public int Coalitions { get; set; }
        public override string ToString() { return (VariableId ?? InterventionId ?? "INTERVENTION").PadRight(34) + "   ·   φ=" + ShapleyValue.ToString("+0.######;-0.######;0", CultureInfo.InvariantCulture).PadLeft(14) + "   ·   " + Coalitions.ToString(CultureInfo.InvariantCulture) + " marginal coalitions"; }
    }

    internal sealed class StrategyDSeparationResult
    {
        public bool Supported { get; set; }
        public bool Separated { get; set; }
        public bool BackdoorGraph { get; set; }
        public string TreatmentId { get; set; }
        public string OutcomeId { get; set; }
        public List<string> ConditionedIds { get; set; }
        public List<string> AncestralIds { get; set; }
        public List<string> ActiveMoralPath { get; set; }
        public string Diagnostic { get; set; }
        public StrategyDSeparationResult() { ConditionedIds = new List<string>(); AncestralIds = new List<string>(); ActiveMoralPath = new List<string>(); }
        public override string ToString() { return !Supported ? "UNSUPPORTED   ·   " + (Diagnostic ?? "∅") : (Separated ? "D-SEPARATED" : "D-CONNECTED") + "   ·   " + (TreatmentId ?? "X") + " ⫫ " + (OutcomeId ?? "Y") + " | {" + String.Join(",", ConditionedIds.ToArray()) + "}" + (BackdoorGraph ? "   ·   G_under_X" : ""); }
    }

    internal sealed class StrategyAdjustmentSet
    {
        public List<string> VariableIds { get; set; }
        public bool Minimal { get; set; }
        public int CandidateUniverse { get; set; }
        public StrategyAdjustmentSet() { VariableIds = new List<string>(); }
        public override string ToString() { return (Minimal ? "MINIMAL" : "VALID") + "   ·   Z={" + String.Join(", ", VariableIds.ToArray()) + "}   ·   searched universe " + CandidateUniverse.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class StrategyKernelLimits
    {
        public int MaxVariables = 512;
        public int MaxEdges = 4096;
        public int MaxPolicies = 256;
        public int MaxInterventionsPerPolicy = 128;
        public int MaxObjectives = 64;
        public int MaxConstraints = 256;
        public int MaxHorizon = 128;
        public int MaxSolverIterations = 512;
        public double ConvergenceEpsilon = 0.0000001;
        public double Damping = 0.65;
        public int MaxCases = 2048;
        public int MaxSensitivityPairs = 4096;
        public int MaxPaths = 1024;
        public int MaxPathDepth = 16;
        public int MaxExactShapleyInterventions = 10;
        public int MaxAdjustmentCandidates = 12;
        public int MaxAdjustmentSets = 256;
    }

    internal static class CounterfactualStrategyKernel
    {
        public static StrategySimulationResult Simulate(StrategyModel model, StrategyPolicy policy, StrategyUncertaintyCase uncertainty, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits();
            StrategySimulationResult result = new StrategySimulationResult { ModelId = model == null ? "" : model.Id, PolicyId = policy == null ? "baseline" : policy.Id, CaseId = uncertainty == null ? "base" : uncertainty.Id, ModelFingerprint = Fingerprint(model), PolicyFingerprint = PolicyFingerprint(policy) };
            if (model == null) { result.Converged = false; result.Feasible = false; result.Violations.Add("MODEL_MISSING"); return result; }
            Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits);
            List<StrategyEdge> edges = ActiveEdges(model, variables, limits);
            if (variables.Count == 0) { result.Converged = false; result.Feasible = false; result.Violations.Add("VARIABLE_SET_EMPTY"); return result; }
            int horizon = Bound(model.Horizon, 1, limits.MaxHorizon);
            Dictionary<string, double> previous = variables.ToDictionary(x => x.Key, x => Clamp(x.Value.Baseline, x.Value.Minimum, x.Value.Maximum), StringComparer.OrdinalIgnoreCase);
            if (uncertainty != null) foreach (KeyValuePair<string, double> row in uncertainty.ExogenousValues) if (variables.ContainsKey(row.Key)) previous[row.Key] = Clamp(row.Value, variables[row.Key].Minimum, variables[row.Key].Maximum);
            result.Timeline.Add(Copy(previous));
            for (int period = 0; period < horizon; period++)
            {
                Dictionary<string, double> current = Copy(previous); bool converged = false; int used = 0;
                for (int iteration = 0; iteration < limits.MaxSolverIterations; iteration++)
                {
                    used++; double maximumDelta = 0; Dictionary<string, double> next = Copy(current);
                    foreach (StrategyVariable variable in variables.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        double interventionValue; bool intervened = InterventionValue(policy, variable.Id, period, out interventionValue);
                        if (intervened) next[variable.Id] = Clamp(interventionValue, variable.Minimum, variable.Maximum);
                        else if (IsKind(variable, "EXOGENOUS") || IsKind(variable, "DECISION"))
                        {
                            double external; if (uncertainty != null && uncertainty.ExogenousValues.TryGetValue(variable.Id, out external)) next[variable.Id] = Clamp(external, variable.Minimum, variable.Maximum); else next[variable.Id] = Clamp(variable.Baseline, variable.Minimum, variable.Maximum);
                        }
                        else
                        {
                            double structural = variable.Intercept;
                            foreach (StrategyEdge edge in edges.Where(x => Eq(x.ToVariableId, variable.Id)))
                            {
                                double parent = ValueForLag(edge, period, current, result.Timeline, variables);
                                structural += EdgeContribution(edge, parent);
                            }
                            double candidate = Clamp(structural, variable.Minimum, variable.Maximum);
                            double old = current.ContainsKey(variable.Id) ? current[variable.Id] : candidate;
                            next[variable.Id] = Clamp(old + limits.Damping * (candidate - old), variable.Minimum, variable.Maximum);
                        }
                        maximumDelta = Math.Max(maximumDelta, Math.Abs(next[variable.Id] - current[variable.Id]));
                    }
                    current = next;
                    if (maximumDelta <= limits.ConvergenceEpsilon) { converged = true; break; }
                }
                result.Iterations += used; if (!converged) result.Converged = false; previous = current; result.Timeline.Add(Copy(previous));
            }
            result.FinalValues = Copy(previous);
            foreach (StrategyObjective objective in (model.Objectives ?? new List<StrategyObjective>()).Where(x => Active(x.Status)).Take(limits.MaxObjectives)) { double value; if (result.FinalValues.TryGetValue(objective.VariableId ?? "", out value)) result.ObjectiveValues[objective.Id ?? objective.VariableId] = value; }
            EvaluateConstraints(model, result, limits);
            return result;
        }

        public static StrategyPortfolioAnalysis AnalyzePortfolio(StrategyModel model, int requestedCases, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); StrategyPortfolioAnalysis analysis = new StrategyPortfolioAnalysis { ModelId = model == null ? "" : model.Id, ModelFingerprint = Fingerprint(model) }; if (model == null) return analysis;
            List<StrategyPolicy> policies = (model.Policies ?? new List<StrategyPolicy>()).Where(x => Active(x.Status)).Take(limits.MaxPolicies).ToList();
            if (policies.Count == 0) policies.Add(new StrategyPolicy { Id = "baseline", Name = "Baseline / no intervention", Status = "CANDIDATE" });
            analysis.Cases = BuildCases(model, requestedCases, limits); analysis.CaseCount = analysis.Cases.Count;
            Dictionary<string, List<StrategySimulationResult>> byPolicy = new Dictionary<string, List<StrategySimulationResult>>(StringComparer.OrdinalIgnoreCase);
            foreach (StrategyPolicy policy in policies)
            {
                List<StrategySimulationResult> runs = new List<StrategySimulationResult>(); foreach (StrategyUncertaintyCase c in analysis.Cases) runs.Add(Simulate(model, policy, c, limits)); byPolicy[policy.Id] = runs; analysis.Simulations.AddRange(runs);
            }
            Dictionary<string, Dictionary<string, Range>> objectiveRanges = ObjectiveRanges(model, byPolicy);
            foreach (StrategyPolicy policy in policies)
            {
                StrategyPolicyEvaluation evaluation = new StrategyPolicyEvaluation { PolicyId = policy.Id, PolicyName = policy.Name, Cases = analysis.CaseCount };
                List<StrategySimulationResult> runs = byPolicy[policy.Id]; foreach (StrategySimulationResult run in runs) { double utility = Utility(model, run, objectiveRanges); if (!run.Feasible) utility -= 1 + run.SoftPenalty; evaluation.CaseUtilities.Add(utility); if (run.Feasible) evaluation.FeasibleCases++; }
                evaluation.Feasibility = evaluation.Cases == 0 ? 0 : (double)evaluation.FeasibleCases / evaluation.Cases; Summarize(evaluation); foreach (StrategyObjective objective in model.Objectives.Where(x => Active(x.Status))) { List<double> values = runs.Select(x => Value(x.FinalValues, objective.VariableId)).ToList(); evaluation.MeanObjectives[objective.Id ?? objective.VariableId] = values.Count == 0 ? 0 : values.Average(); } analysis.Policies.Add(evaluation);
            }
            for (int c = 0; c < analysis.CaseCount; c++)
            {
                double best = analysis.Policies.Max(x => x.CaseUtilities[c]); foreach (StrategyPolicyEvaluation policy in analysis.Policies) policy.CaseRegrets.Add(Math.Max(0, best - policy.CaseUtilities[c]));
            }
            foreach (StrategyPolicyEvaluation evaluation in analysis.Policies) { evaluation.MeanRegret = evaluation.CaseRegrets.Count == 0 ? 0 : evaluation.CaseRegrets.Average(); evaluation.MaximumRegret = evaluation.CaseRegrets.Count == 0 ? 0 : evaluation.CaseRegrets.Max(); evaluation.Robustness = Clamp01(0.45 * evaluation.Feasibility + 0.35 * Clamp01((evaluation.WorstUtility + 1) / 2) + 0.20 * Clamp01(1 - evaluation.MaximumRegret)); }
            MarkPareto(model, analysis.Policies); StrategyPolicyEvaluation recommended = analysis.Policies.OrderByDescending(x => x.Feasibility >= 0.999 ? 1 : 0).ThenBy(x => x.MaximumRegret).ThenByDescending(x => x.LowerTailUtility).ThenByDescending(x => x.MeanUtility).ThenBy(x => x.PolicyId).FirstOrDefault(); analysis.RecommendedPolicyId = recommended == null ? "" : recommended.PolicyId; analysis.RecommendationRule = "LEXICOGRAPHIC(feasible, minimax-regret, lower-tail, mean-utility)";
            analysis.AnalysisFingerprint = AnalysisFingerprint(analysis); analysis.CertificateHash = Sha(analysis.ModelFingerprint + "|" + analysis.AnalysisFingerprint + "|" + analysis.RecommendedPolicyId + "|" + analysis.RecommendationRule); return analysis;
        }

        public static List<StrategySensitivity> Sensitivity(StrategyModel model, StrategyPolicy policy, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); List<StrategySensitivity> result = new List<StrategySensitivity>(); if (model == null) return result; Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyVariable> inputs = variables.Values.Where(x => IsKind(x, "EXOGENOUS") || IsKind(x, "DECISION") || x.Uncertain).ToList(); List<StrategyVariable> outcomes = variables.Values.Where(x => IsKind(x, "OUTCOME") || model.Objectives.Any(o => Eq(o.VariableId, x.Id))).ToList(); StrategyUncertaintyCase baselineCase = new StrategyUncertaintyCase { Id = "sensitivity-base" }; StrategySimulationResult baseline = Simulate(model, policy, baselineCase, limits);
            foreach (StrategyVariable input in inputs) foreach (StrategyVariable outcome in outcomes)
            {
                if (result.Count >= limits.MaxSensitivityPairs) return result; double span = Math.Abs(input.Maximum - input.Minimum); double delta = span > 0 && span < 1000000 ? Math.Max(span * 0.001, 0.000001) : Math.Max(Math.Abs(input.Baseline) * 0.001, 0.000001); StrategyUncertaintyCase plus = new StrategyUncertaintyCase { Id = "sens-plus" }, minus = new StrategyUncertaintyCase { Id = "sens-minus" }; plus.ExogenousValues[input.Id] = Clamp(input.Baseline + delta, input.Minimum, input.Maximum); minus.ExogenousValues[input.Id] = Clamp(input.Baseline - delta, input.Minimum, input.Maximum); double actualDelta = plus.ExogenousValues[input.Id] - minus.ExogenousValues[input.Id]; if (Math.Abs(actualDelta) < 1e-15) continue; double yp = Value(Simulate(model, policy, plus, limits).FinalValues, outcome.Id); double ym = Value(Simulate(model, policy, minus, limits).FinalValues, outcome.Id); double effect = (yp - ym) / actualDelta; double y0 = Value(baseline.FinalValues, outcome.Id); double elasticity = Math.Abs(y0) < 1e-15 ? 0 : effect * input.Baseline / y0; result.Add(new StrategySensitivity { InputVariableId = input.Id, OutcomeVariableId = outcome.Id, BaselineInput = input.Baseline, Delta = actualDelta / 2, LocalEffect = effect, Elasticity = elasticity, Sign = effect > 1e-12 ? "AMPLIFYING" : effect < -1e-12 ? "INHIBITING" : "NEUTRAL" });
            }
            return result.OrderByDescending(x => Math.Abs(x.Elasticity)).ThenByDescending(x => Math.Abs(x.LocalEffect)).ToList();
        }

        public static StrategyGraphDiagnostics Diagnose(StrategyModel model, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); StrategyGraphDiagnostics report = new StrategyGraphDiagnostics(); if (model == null) { report.Audit.Add("ERROR · MODEL_MISSING"); return report; } Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyEdge> edges = ActiveEdges(model, variables, limits); report.Variables = variables.Count; report.Edges = edges.Count; report.Exogenous = variables.Values.Count(x => IsKind(x, "EXOGENOUS")); report.Decisions = variables.Values.Count(x => IsKind(x, "DECISION")); report.Outcomes = variables.Values.Count(x => IsKind(x, "OUTCOME")); report.StrongComponents = StrongComponents(variables.Keys, edges); report.Components = report.StrongComponents.Count; report.CyclicComponents = report.StrongComponents.Count(c => c.Count > 1 || edges.Any(e => Eq(e.FromVariableId, e.ToVariableId) && c.Any(x => Eq(x, e.FromVariableId)))); report.Acyclic = report.CyclicComponents == 0; report.TopologicalOrder = TopologicalOrder(variables.Keys, edges); report.Colliders = variables.Keys.Where(id => edges.Where(e => Eq(e.ToVariableId, id)).Select(e => e.FromVariableId).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1).OrderBy(x => x).ToList(); report.OrphanVariables = variables.Keys.Where(id => !edges.Any(e => Eq(e.FromVariableId, id) || Eq(e.ToVariableId, id))).OrderBy(x => x).ToList();
            foreach (StrategyEdge edge in (model.Edges ?? new List<StrategyEdge>()).Take(limits.MaxEdges)) { if (!variables.ContainsKey(edge.FromVariableId ?? "")) report.Audit.Add("ERROR · EDGE_ORPHAN_ORIGIN · " + (edge.Id ?? "∅")); if (!variables.ContainsKey(edge.ToVariableId ?? "")) report.Audit.Add("ERROR · EDGE_ORPHAN_DESTINATION · " + (edge.Id ?? "∅")); if (!Finite(edge.Coefficient)) report.Audit.Add("ERROR · EDGE_NONFINITE_COEFFICIENT · " + (edge.Id ?? "∅")); }
            foreach (StrategyVariable variable in variables.Values) { if (!Finite(variable.Baseline) || !Finite(variable.Minimum) || !Finite(variable.Maximum) || variable.Minimum > variable.Maximum) report.Audit.Add("ERROR · VARIABLE_DOMAIN_INVALID · " + variable.Id); if (IsKind(variable, "ENDOGENOUS") && !edges.Any(e => Eq(e.ToVariableId, variable.Id)) && Math.Abs(variable.Intercept) < 1e-15) report.Audit.Add("WARN  · ENDOGENOUS_WITHOUT_PARENTS · " + variable.Id); }
            foreach (StrategyPolicy policy in model.Policies ?? new List<StrategyPolicy>()) foreach (StrategyIntervention intervention in policy.Interventions ?? new List<StrategyIntervention>()) if (!variables.ContainsKey(intervention.VariableId ?? "")) report.Audit.Add("ERROR · INTERVENTION_ORPHAN_VARIABLE · " + (intervention.Id ?? "∅"));
            if (report.CyclicComponents > 0) report.Audit.Add("INFO  · CYCLIC_STRUCTURAL_SYSTEM · damped fixed-point semantics apply"); if (report.Audit.Count == 0) report.Audit.Add("PASS  · STRUCTURAL_REFERENCES_AND_NUMERIC_DOMAINS_COHERENT"); return report;
        }

        public static List<StrategyCausalPath> EnumeratePaths(StrategyModel model, string fromVariableId, string toVariableId, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); List<StrategyCausalPath> result = new List<StrategyCausalPath>(); if (model == null || String.IsNullOrWhiteSpace(fromVariableId) || String.IsNullOrWhiteSpace(toVariableId)) return result; Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyEdge> edges = ActiveEdges(model, variables, limits); if (!variables.ContainsKey(fromVariableId) || !variables.ContainsKey(toVariableId)) return result; PathDfs(fromVariableId, toVariableId, edges, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new List<string>(), new List<string>(), 1, result, limits); return result.OrderByDescending(x => Math.Abs(x.PathCoefficient)).ThenBy(x => x.VariableIds.Count).Take(limits.MaxPaths).ToList();
        }

        public static List<string> BackdoorAdjustmentCandidates(StrategyModel model, string treatmentId, string outcomeId, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); if (model == null) return new List<string>(); Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyEdge> edges = ActiveEdges(model, variables, limits); HashSet<string> ancestorsTreatment = Ancestors(treatmentId, edges); HashSet<string> ancestorsOutcome = Ancestors(outcomeId, edges); HashSet<string> descendantsTreatment = Descendants(treatmentId, edges); ancestorsTreatment.IntersectWith(ancestorsOutcome); ancestorsTreatment.ExceptWith(descendantsTreatment); ancestorsTreatment.Remove(treatmentId); ancestorsTreatment.Remove(outcomeId); return ancestorsTreatment.Where(variables.ContainsKey).OrderBy(x => x).ToList();
        }

        public static StrategyDSeparationResult DSeparation(StrategyModel model, string treatmentId, string outcomeId, IEnumerable<string> conditionedIds, bool backdoorGraph, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); StrategyDSeparationResult result = new StrategyDSeparationResult { TreatmentId = treatmentId, OutcomeId = outcomeId, BackdoorGraph = backdoorGraph }; if (model == null) { result.Diagnostic = "MODEL_MISSING"; return result; } Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyEdge> edges = ActiveEdges(model, variables, limits); if (!variables.ContainsKey(treatmentId ?? "") || !variables.ContainsKey(outcomeId ?? "")) { result.Diagnostic = "ENDPOINT_MISSING"; return result; } StrategyGraphDiagnostics graph = Diagnose(model, limits); if (!graph.Acyclic) { result.Diagnostic = "D_SEPARATION_REQUIRES_ACYCLIC_GRAPH"; return result; } if (backdoorGraph) edges = edges.Where(e => !Eq(e.FromVariableId, treatmentId)).ToList(); result.ConditionedIds = (conditionedIds ?? Enumerable.Empty<string>()).Where(variables.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(); if (result.ConditionedIds.Contains(treatmentId, StringComparer.OrdinalIgnoreCase) || result.ConditionedIds.Contains(outcomeId, StringComparer.OrdinalIgnoreCase)) { result.Supported = true; result.Separated = true; result.Diagnostic = "ENDPOINT_CONDITIONED"; return result; }
            HashSet<string> ancestral = new HashSet<string>(StringComparer.OrdinalIgnoreCase); ancestral.Add(treatmentId); ancestral.Add(outcomeId); foreach (string z in result.ConditionedIds) ancestral.Add(z); Queue<string> queue = new Queue<string>(ancestral); while (queue.Count > 0) { string child = queue.Dequeue(); foreach (string parent in edges.Where(e => Eq(e.ToVariableId, child)).Select(e => e.FromVariableId)) if (ancestral.Add(parent)) queue.Enqueue(parent); } result.AncestralIds = ancestral.OrderBy(x => x).ToList();
            Dictionary<string, HashSet<string>> moral = ancestral.ToDictionary(x => x, x => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase); foreach (StrategyEdge edge in edges.Where(e => ancestral.Contains(e.FromVariableId) && ancestral.Contains(e.ToVariableId))) { moral[edge.FromVariableId].Add(edge.ToVariableId); moral[edge.ToVariableId].Add(edge.FromVariableId); } foreach (string child in ancestral) { List<string> parents = edges.Where(e => Eq(e.ToVariableId, child) && ancestral.Contains(e.FromVariableId)).Select(e => e.FromVariableId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); for (int i = 0; i < parents.Count; i++) for (int j = i + 1; j < parents.Count; j++) { moral[parents[i]].Add(parents[j]); moral[parents[j]].Add(parents[i]); } }
            HashSet<string> removed = new HashSet<string>(result.ConditionedIds, StringComparer.OrdinalIgnoreCase); Dictionary<string, string> prior = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Queue<string> frontier = new Queue<string>(); if (!removed.Contains(treatmentId)) { frontier.Enqueue(treatmentId); seen.Add(treatmentId); } while (frontier.Count > 0) { string current = frontier.Dequeue(); if (Eq(current, outcomeId)) break; foreach (string next in moral[current].Where(x => !removed.Contains(x)).OrderBy(x => x)) if (seen.Add(next)) { prior[next] = current; frontier.Enqueue(next); } } result.Supported = true; result.Separated = !seen.Contains(outcomeId); result.Diagnostic = result.Separated ? "NO_PATH_IN_CONDITIONED_ANCESTRAL_MORAL_GRAPH" : "ACTIVE_PATH_IN_CONDITIONED_ANCESTRAL_MORAL_GRAPH"; if (!result.Separated) { string current = outcomeId; result.ActiveMoralPath.Add(current); while (!Eq(current, treatmentId) && prior.ContainsKey(current)) { current = prior[current]; result.ActiveMoralPath.Add(current); } result.ActiveMoralPath.Reverse(); } return result;
        }

        public static List<StrategyAdjustmentSet> MinimalBackdoorAdjustmentSets(StrategyModel model, string treatmentId, string outcomeId, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); List<StrategyAdjustmentSet> result = new List<StrategyAdjustmentSet>(); if (model == null) return result; StrategyGraphDiagnostics graph = Diagnose(model, limits); if (!graph.Acyclic) return result; Dictionary<string, StrategyVariable> variables = ActiveVariables(model, limits); List<StrategyEdge> edges = ActiveEdges(model, variables, limits); HashSet<string> candidates = Ancestors(treatmentId, edges); candidates.UnionWith(Ancestors(outcomeId, edges)); candidates.ExceptWith(Descendants(treatmentId, edges)); candidates.Remove(treatmentId); candidates.Remove(outcomeId); List<string> universe = candidates.Where(variables.ContainsKey).OrderBy(x => x).Take(limits.MaxAdjustmentCandidates).ToList(); int coalitions = 1 << universe.Count; List<HashSet<string>> minimal = new List<HashSet<string>>(); foreach (int mask in Enumerable.Range(0, coalitions).OrderBy(PopCount).ThenBy(x => x)) { if (result.Count >= limits.MaxAdjustmentSets) break; HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < universe.Count; i++) if ((mask & (1 << i)) != 0) set.Add(universe[i]); if (minimal.Any(existing => existing.IsSubsetOf(set))) continue; StrategyDSeparationResult check = DSeparation(model, treatmentId, outcomeId, set, true, limits); if (check.Supported && check.Separated) { minimal.Add(set); result.Add(new StrategyAdjustmentSet { VariableIds = set.OrderBy(x => x).ToList(), Minimal = true, CandidateUniverse = universe.Count }); } } return result;
        }

        public static List<StrategyInformationValue> ValueOfInformation(StrategyModel model, StrategyPortfolioAnalysis analysis)
        {
            List<StrategyInformationValue> result = new List<StrategyInformationValue>(); if (model == null || analysis == null || analysis.Cases.Count == 0 || analysis.Policies.Count == 0) return result; double fixedBest = analysis.Policies.Max(p => p.MeanUtility); double omniscient = Enumerable.Range(0, analysis.CaseCount).Average(i => analysis.Policies.Max(p => p.CaseUtilities[i])); foreach (StrategyVariable variable in model.Variables.Where(x => x.Uncertain && Active(x.Status)))
            {
                List<double> values = analysis.Cases.Select(c => Value(c.ExogenousValues, variable.Id)).OrderBy(x => x).ToList(); if (values.Count == 0) continue; double q1 = Quantile(values, 1.0 / 3), q2 = Quantile(values, 2.0 / 3); double conditional = 0; int counted = 0; for (int bin = 0; bin < 3; bin++) { List<int> ids = Enumerable.Range(0, analysis.CaseCount).Where(i => { double v = Value(analysis.Cases[i].ExogenousValues, variable.Id); return bin == 0 ? v <= q1 : bin == 1 ? v > q1 && v <= q2 : v > q2; }).ToList(); if (ids.Count == 0) continue; double binBest = analysis.Policies.Max(p => ids.Average(i => p.CaseUtilities[i])); conditional += binBest * ids.Count; counted += ids.Count; } if (counted > 0) conditional /= counted; result.Add(new StrategyInformationValue { VariableId = variable.Id, VariableName = variable.Name, PerfectInformationValue = Math.Max(0, omniscient - fixedBest), ThreeBinInformationValue = Math.Max(0, conditional - fixedBest), Cases = analysis.CaseCount }); }
            return result.OrderByDescending(x => x.ThreeBinInformationValue).ThenByDescending(x => x.PerfectInformationValue).ToList();
        }

        public static List<StrategyAttribution> ShapleyAttribution(StrategyModel model, StrategyPolicy policy, string outcomeVariableId, StrategyUncertaintyCase uncertainty, StrategyKernelLimits limits)
        {
            limits = limits ?? new StrategyKernelLimits(); List<StrategyAttribution> result = new List<StrategyAttribution>(); if (model == null || policy == null) return result; List<StrategyIntervention> interventions = (policy.Interventions ?? new List<StrategyIntervention>()).Take(limits.MaxExactShapleyInterventions).ToList(); int n = interventions.Count; if (n == 0) return result; int coalitions = 1 << n; double[] values = new double[coalitions]; for (int mask = 0; mask < coalitions; mask++) { StrategyPolicy subset = new StrategyPolicy { Id = "subset-" + mask, Name = "subset" }; for (int i = 0; i < n; i++) if ((mask & (1 << i)) != 0) subset.Interventions.Add(interventions[i]); values[mask] = Value(Simulate(model, subset, uncertainty, limits).FinalValues, outcomeVariableId); }
            double factorialN = Factorial(n); for (int i = 0; i < n; i++) { double phi = 0; int count = 0; for (int mask = 0; mask < coalitions; mask++) { if ((mask & (1 << i)) != 0) continue; int size = PopCount(mask); double weight = Factorial(size) * Factorial(n - size - 1) / factorialN; phi += weight * (values[mask | (1 << i)] - values[mask]); count++; } result.Add(new StrategyAttribution { InterventionId = interventions[i].Id, VariableId = interventions[i].VariableId, ShapleyValue = phi, Coalitions = count }); } return result.OrderByDescending(x => Math.Abs(x.ShapleyValue)).ToList();
        }

        public static string Fingerprint(StrategyModel model)
        {
            if (model == null) return Sha("null-strategy-model"); StringBuilder s = new StringBuilder(); s.Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.Name).Append('|').Append(model.Status).Append('|').Append(model.Revision).Append('|').Append(model.Horizon).Append('\n');
            foreach (StrategyVariable v in (model.Variables ?? new List<StrategyVariable>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("V|").Append(v.Id).Append('|').Append(v.Name).Append('|').Append(v.Kind).Append('|').Append(v.Status).Append('|').Append(F(v.Baseline)).Append('|').Append(F(v.Minimum)).Append('|').Append(F(v.Maximum)).Append('|').Append(F(v.Intercept)).Append('|').Append(v.Uncertain).Append('\n');
            foreach (StrategyEdge e in (model.Edges ?? new List<StrategyEdge>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("E|").Append(e.Id).Append('|').Append(e.FromVariableId).Append('|').Append(e.ToVariableId).Append('|').Append(e.Kind).Append('|').Append(e.Status).Append('|').Append(e.Transform).Append('|').Append(F(e.Coefficient)).Append('|').Append(F(e.Threshold)).Append('|').Append(e.Lag).Append('\n');
            foreach (StrategyObjective o in (model.Objectives ?? new List<StrategyObjective>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("O|").Append(o.Id).Append('|').Append(o.VariableId).Append('|').Append(o.Direction).Append('|').Append(F(o.Weight)).Append('|').Append(F(o.Aspiration)).Append('|').Append(o.Status).Append('\n');
            foreach (StrategyConstraint c in (model.Constraints ?? new List<StrategyConstraint>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("C|").Append(c.Id).Append('|').Append(c.VariableId).Append('|').Append(c.Operator).Append('|').Append(c.Severity).Append('|').Append(F(c.Threshold)).Append('|').Append(F(c.Penalty)).Append('|').Append(c.Status).Append('\n');
            foreach (StrategyPolicy p in (model.Policies ?? new List<StrategyPolicy>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append("P|").Append(PolicyFingerprint(p)).Append('\n'); foreach (string a in (model.Assumptions ?? new List<string>()).OrderBy(x => x, StringComparer.Ordinal)) s.Append("A|").Append(a).Append('\n'); return Sha(s.ToString());
        }

        public static string PolicyFingerprint(StrategyPolicy policy)
        {
            if (policy == null) return Sha("baseline-policy"); StringBuilder s = new StringBuilder(); s.Append(policy.Id).Append('|').Append(policy.ParentPolicyId).Append('|').Append(policy.Name).Append('|').Append(policy.Status).Append('\n'); foreach (StrategyIntervention i in (policy.Interventions ?? new List<StrategyIntervention>()).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)) s.Append(i.Id).Append('|').Append(i.VariableId).Append('|').Append(i.Kind).Append('|').Append(F(i.Value)).Append('|').Append(i.StartPeriod).Append('|').Append(i.EndPeriod).Append('\n'); return Sha(s.ToString());
        }

        private static Dictionary<string, StrategyVariable> ActiveVariables(StrategyModel model, StrategyKernelLimits limits) { return (model == null ? new List<StrategyVariable>() : model.Variables ?? new List<StrategyVariable>()).Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id) && Active(x.Status)).Take(limits.MaxVariables).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase); }
        private static List<StrategyEdge> ActiveEdges(StrategyModel model, Dictionary<string, StrategyVariable> variables, StrategyKernelLimits limits) { return (model.Edges ?? new List<StrategyEdge>()).Where(x => x != null && Active(x.Status) && variables.ContainsKey(x.FromVariableId ?? "") && variables.ContainsKey(x.ToVariableId ?? "")).Take(limits.MaxEdges).ToList(); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETRACTED") && !Eq(status, "REJECTED"); }
        private static bool IsKind(StrategyVariable variable, string kind) { return variable != null && Eq(variable.Kind, kind); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static int Bound(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        private static double Clamp(double value, double min, double max) { if (!Finite(value)) return Finite(min) ? min : 0; if (!Finite(min)) min = -Double.MaxValue; if (!Finite(max)) max = Double.MaxValue; if (min > max) { double t = min; min = max; max = t; } return Math.Max(min, Math.Min(max, value)); }
        private static double Clamp01(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static Dictionary<string, double> Copy(Dictionary<string, double> source) { return new Dictionary<string, double>(source ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase); }
        private static double Value(Dictionary<string, double> values, string key) { double v; return values != null && values.TryGetValue(key ?? "", out v) ? v : 0; }
        private static bool InterventionValue(StrategyPolicy policy, string variableId, int period, out double value) { value = 0; if (policy == null) return false; StrategyIntervention row = (policy.Interventions ?? new List<StrategyIntervention>()).LastOrDefault(x => Eq(x.VariableId, variableId) && period >= Math.Max(0, x.StartPeriod) && period <= Math.Max(x.StartPeriod, x.EndPeriod)); if (row == null) return false; value = row.Value; return true; }
        private static double ValueForLag(StrategyEdge edge, int period, Dictionary<string, double> current, List<Dictionary<string, double>> timeline, Dictionary<string, StrategyVariable> variables) { if (edge.Lag <= 0) return Value(current, edge.FromVariableId); int index = timeline.Count - edge.Lag; if (index >= 0 && index < timeline.Count) return Value(timeline[index], edge.FromVariableId); StrategyVariable variable; return variables.TryGetValue(edge.FromVariableId ?? "", out variable) ? variable.Baseline : 0; }
        private static double EdgeContribution(StrategyEdge edge, double parent) { string transform = (edge.Transform ?? "LINEAR").ToUpperInvariant(); double signal = parent; if (transform == "LOGISTIC") signal = 1.0 / (1.0 + Math.Exp(-Clamp(parent - edge.Threshold, -60, 60))); else if (transform == "SATURATING") signal = parent / (1 + Math.Abs(parent)); else if (transform == "THRESHOLD") signal = parent >= edge.Threshold ? 1 : 0; else if (transform == "HINGE") signal = Math.Max(0, parent - edge.Threshold); else if (transform == "INVERSE") signal = Math.Abs(parent) < 1e-12 ? 0 : 1.0 / parent; return edge.Coefficient * signal; }
        private static void EvaluateConstraints(StrategyModel model, StrategySimulationResult result, StrategyKernelLimits limits) { foreach (StrategyConstraint c in (model.Constraints ?? new List<StrategyConstraint>()).Where(x => Active(x.Status)).Take(limits.MaxConstraints)) { double value; if (!result.FinalValues.TryGetValue(c.VariableId ?? "", out value)) { result.Feasible = false; result.Violations.Add("CONSTRAINT_VARIABLE_MISSING · " + (c.Id ?? c.VariableId)); continue; } bool pass = Compare(value, c.Operator, c.Threshold); if (pass) continue; string violation = (c.Id ?? c.Name ?? "constraint") + " · " + value.ToString("0.######", CultureInfo.InvariantCulture) + " " + (c.Operator ?? ">=") + " " + c.Threshold.ToString("0.######", CultureInfo.InvariantCulture); result.Violations.Add(violation); if (Eq(c.Severity, "HARD")) result.Feasible = false; else result.SoftPenalty += Math.Max(0, c.Penalty <= 0 ? 0.1 : c.Penalty); } }
        private static bool Compare(double value, string op, double threshold) { op = (op ?? ">=").Trim(); if (op == ">") return value > threshold; if (op == "<=") return value <= threshold; if (op == "<") return value < threshold; if (op == "==" || op == "=") return Math.Abs(value - threshold) <= 1e-9; if (op == "!=") return Math.Abs(value - threshold) > 1e-9; return value >= threshold; }
        private static List<StrategyUncertaintyCase> BuildCases(StrategyModel model, int requested, StrategyKernelLimits limits) { int count = Bound(requested <= 0 ? 128 : requested, 1, limits.MaxCases); List<StrategyVariable> uncertain = model.Variables.Where(x => Active(x.Status) && x.Uncertain && Finite(x.Minimum) && Finite(x.Maximum) && x.Maximum > x.Minimum).Take(32).ToList(); List<StrategyUncertaintyCase> cases = new List<StrategyUncertaintyCase>(); int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131 }; for (int i = 0; i < count; i++) { StrategyUncertaintyCase c = new StrategyUncertaintyCase { Index = i, Id = "halton-" + i.ToString("D6", CultureInfo.InvariantCulture) }; for (int d = 0; d < uncertain.Count; d++) { double q = i == 0 ? Clamp01((uncertain[d].Baseline - uncertain[d].Minimum) / (uncertain[d].Maximum - uncertain[d].Minimum)) : Halton(i, primes[d]); c.ExogenousValues[uncertain[d].Id] = uncertain[d].Minimum + q * (uncertain[d].Maximum - uncertain[d].Minimum); } cases.Add(c); } return cases; }
        private static double Halton(int index, int radix) { double f = 1, r = 0; int i = Math.Max(1, index); while (i > 0) { f /= radix; r += f * (i % radix); i /= radix; } return r; }
        private sealed class Range { public double Min = Double.PositiveInfinity; public double Max = Double.NegativeInfinity; }
        private static Dictionary<string, Dictionary<string, Range>> ObjectiveRanges(StrategyModel model, Dictionary<string, List<StrategySimulationResult>> runs) { Dictionary<string, Dictionary<string, Range>> outer = new Dictionary<string, Dictionary<string, Range>>(StringComparer.OrdinalIgnoreCase); Dictionary<string, Range> shared = new Dictionary<string, Range>(StringComparer.OrdinalIgnoreCase); foreach (StrategyObjective o in model.Objectives.Where(x => Active(x.Status))) shared[o.Id ?? o.VariableId] = new Range(); foreach (List<StrategySimulationResult> list in runs.Values) foreach (StrategySimulationResult run in list) foreach (StrategyObjective o in model.Objectives.Where(x => Active(x.Status))) { string id = o.Id ?? o.VariableId; double v = Value(run.FinalValues, o.VariableId); shared[id].Min = Math.Min(shared[id].Min, v); shared[id].Max = Math.Max(shared[id].Max, v); } foreach (string policy in runs.Keys) outer[policy] = shared; return outer; }
        private static double Utility(StrategyModel model, StrategySimulationResult run, Dictionary<string, Dictionary<string, Range>> ranges) { List<StrategyObjective> objectives = model.Objectives.Where(x => Active(x.Status)).ToList(); if (objectives.Count == 0) return 0; Dictionary<string, Range> shared = ranges.Values.FirstOrDefault() ?? new Dictionary<string, Range>(); double sum = 0, weights = 0; foreach (StrategyObjective o in objectives) { string id = o.Id ?? o.VariableId; Range range; if (!shared.TryGetValue(id, out range)) continue; double v = Value(run.FinalValues, o.VariableId); double normalized = range.Max - range.Min < 1e-15 ? 0.5 : (v - range.Min) / (range.Max - range.Min); if (Eq(o.Direction, "MINIMIZE") || Eq(o.Direction, "MIN")) normalized = 1 - normalized; double weight = Math.Max(0, o.Weight); sum += weight * Clamp01(normalized); weights += weight; } return weights <= 0 ? 0 : sum / weights; }
        private static void Summarize(StrategyPolicyEvaluation e) { List<double> sorted = e.CaseUtilities.OrderBy(x => x).ToList(); if (sorted.Count == 0) return; e.MeanUtility = sorted.Average(); e.MedianUtility = Quantile(sorted, 0.5); e.P05Utility = Quantile(sorted, 0.05); e.WorstUtility = sorted[0]; int tail = Math.Max(1, (int)Math.Ceiling(sorted.Count * 0.10)); e.LowerTailUtility = sorted.Take(tail).Average(); e.UtilityDeviation = Math.Sqrt(sorted.Average(x => (x - e.MeanUtility) * (x - e.MeanUtility))); }
        private static double Quantile(List<double> sorted, double q) { if (sorted == null || sorted.Count == 0) return 0; q = Clamp01(q); double position = (sorted.Count - 1) * q; int lower = (int)Math.Floor(position), upper = (int)Math.Ceiling(position); if (lower == upper) return sorted[lower]; return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]); }
        private static void MarkPareto(StrategyModel model, List<StrategyPolicyEvaluation> policies) { foreach (StrategyPolicyEvaluation candidate in policies) { bool dominated = false; foreach (StrategyPolicyEvaluation other in policies) { if (Object.ReferenceEquals(candidate, other)) continue; bool all = other.Feasibility >= candidate.Feasibility - 1e-12; bool strict = other.Feasibility > candidate.Feasibility + 1e-12; foreach (StrategyObjective objective in model.Objectives.Where(x => Active(x.Status))) { double a = Value(candidate.MeanObjectives, objective.Id ?? objective.VariableId), b = Value(other.MeanObjectives, objective.Id ?? objective.VariableId); bool min = Eq(objective.Direction, "MINIMIZE") || Eq(objective.Direction, "MIN"); all &= min ? b <= a + 1e-12 : b >= a - 1e-12; strict |= min ? b < a - 1e-12 : b > a + 1e-12; } if (all && strict) { dominated = true; break; } } candidate.ParetoEfficient = !dominated; } }
        private static List<List<string>> StrongComponents(IEnumerable<string> ids, List<StrategyEdge> edges) { Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> on = new HashSet<string>(StringComparer.OrdinalIgnoreCase); List<List<string>> result = new List<List<string>>(); int next = 0; foreach (string id in ids) if (!index.ContainsKey(id)) Tarjan(id, edges, index, low, stack, on, result, ref next); return result.OrderByDescending(x => x.Count).ThenBy(x => x.FirstOrDefault()).ToList(); }
        private static void Tarjan(string v, List<StrategyEdge> edges, Dictionary<string, int> index, Dictionary<string, int> low, Stack<string> stack, HashSet<string> on, List<List<string>> result, ref int next) { index[v] = next; low[v] = next; next++; stack.Push(v); on.Add(v); foreach (string w in edges.Where(e => Eq(e.FromVariableId, v)).Select(e => e.ToVariableId).Distinct(StringComparer.OrdinalIgnoreCase)) { if (!index.ContainsKey(w)) { Tarjan(w, edges, index, low, stack, on, result, ref next); low[v] = Math.Min(low[v], low[w]); } else if (on.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> component = new List<string>(); string w; do { w = stack.Pop(); on.Remove(w); component.Add(w); } while (!Eq(w, v)); result.Add(component.OrderBy(x => x).ToList()); } }
        private static List<string> TopologicalOrder(IEnumerable<string> ids, List<StrategyEdge> edges) { Dictionary<string, int> indegree = ids.ToDictionary(x => x, x => 0, StringComparer.OrdinalIgnoreCase); foreach (StrategyEdge e in edges) indegree[e.ToVariableId]++; SortedSet<string> ready = new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.OrdinalIgnoreCase); List<string> result = new List<string>(); while (ready.Count > 0) { string v = ready.Min; ready.Remove(v); result.Add(v); foreach (StrategyEdge e in edges.Where(x => Eq(x.FromVariableId, v))) { indegree[e.ToVariableId]--; if (indegree[e.ToVariableId] == 0) ready.Add(e.ToVariableId); } } foreach (string id in indegree.Keys.Where(x => !result.Contains(x, StringComparer.OrdinalIgnoreCase)).OrderBy(x => x)) result.Add(id); return result; }
        private static void PathDfs(string current, string target, List<StrategyEdge> edges, HashSet<string> visited, List<string> variables, List<string> edgeIds, double coefficient, List<StrategyCausalPath> result, StrategyKernelLimits limits) { if (result.Count >= limits.MaxPaths || variables.Count >= limits.MaxPathDepth) return; visited.Add(current); variables.Add(current); if (Eq(current, target)) result.Add(new StrategyCausalPath { VariableIds = new List<string>(variables), EdgeIds = new List<string>(edgeIds), PathCoefficient = coefficient }); else foreach (StrategyEdge edge in edges.Where(x => Eq(x.FromVariableId, current)).OrderBy(x => x.Id)) if (!visited.Contains(edge.ToVariableId)) { edgeIds.Add(edge.Id); PathDfs(edge.ToVariableId, target, edges, visited, variables, edgeIds, coefficient * edge.Coefficient, result, limits); edgeIds.RemoveAt(edgeIds.Count - 1); } variables.RemoveAt(variables.Count - 1); visited.Remove(current); }
        private static HashSet<string> Ancestors(string id, List<StrategyEdge> edges) { HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase), frontier = new HashSet<string>(StringComparer.OrdinalIgnoreCase); if (!String.IsNullOrWhiteSpace(id)) frontier.Add(id); while (frontier.Count > 0) { string x = frontier.First(); frontier.Remove(x); foreach (string p in edges.Where(e => Eq(e.ToVariableId, x)).Select(e => e.FromVariableId)) if (result.Add(p)) frontier.Add(p); } return result; }
        private static HashSet<string> Descendants(string id, List<StrategyEdge> edges) { HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase), frontier = new HashSet<string>(StringComparer.OrdinalIgnoreCase); if (!String.IsNullOrWhiteSpace(id)) frontier.Add(id); while (frontier.Count > 0) { string x = frontier.First(); frontier.Remove(x); foreach (string p in edges.Where(e => Eq(e.FromVariableId, x)).Select(e => e.ToVariableId)) if (result.Add(p)) frontier.Add(p); } return result; }
        private static string AnalysisFingerprint(StrategyPortfolioAnalysis analysis) { StringBuilder s = new StringBuilder(); s.Append(analysis.ModelFingerprint).Append('|').Append(analysis.CaseCount).Append('\n'); foreach (StrategyPolicyEvaluation p in analysis.Policies.OrderBy(x => x.PolicyId)) s.Append(p.PolicyId).Append('|').Append(F(p.Feasibility)).Append('|').Append(F(p.MeanUtility)).Append('|').Append(F(p.WorstUtility)).Append('|').Append(F(p.MaximumRegret)).Append('|').Append(F(p.Robustness)).Append('|').Append(p.ParetoEfficient).Append('\n'); return Sha(s.ToString()); }
        private static int PopCount(int value) { int count = 0; while (value != 0) { value &= value - 1; count++; } return count; }
        private static double Factorial(int n) { double value = 1; for (int i = 2; i <= n; i++) value *= i; return value; }
        private static string F(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static string Sha(string value) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

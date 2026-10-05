using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class ComplexAgentSpec
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Kind { get; set; }
        public string Archetype { get; set; }
        public string InitialMode { get; set; }
        public double InitialValue { get; set; }
        public double Threshold { get; set; }
        public double Susceptibility { get; set; }
        public double Capacity { get; set; }
        public double InitialLoad { get; set; }
        public double Noise { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> Traits { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public ComplexAgentSpec() { Status = "ACTIVE"; Kind = "ACTOR"; Archetype = "DEFAULT"; InitialMode = "0"; Threshold = 0.5; Susceptibility = 1; Capacity = 1; Traits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Label ?? Id ?? "AGENT") + "   ·   " + (Kind ?? "ACTOR") + "/" + (Archetype ?? "DEFAULT") + "   ·   mode " + (InitialMode ?? "0") + "   ·   value " + InitialValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   θ " + Threshold.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexInteractionEdge
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public string Kind { get; set; }
        public double Weight { get; set; }
        public bool Bidirectional { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public ComplexInteractionEdge() { Kind = "INTERACTION"; Weight = 1; Bidirectional = true; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (SourceId ?? "?") + (Bidirectional ? " ⇄ " : " → ") + (TargetId ?? "?") + "   ·   " + (Kind ?? "INTERACTION") + "   ·   w " + Weight.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexRuleSpec
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string StateFrom { get; set; }
        public string StateTo { get; set; }
        public string NeighborState { get; set; }
        public double MinimumNeighborFraction { get; set; }
        public double Probability { get; set; }
        public double ScalarDelta { get; set; }
        public double Coupling { get; set; }
        public double Threshold { get; set; }
        public string ParameterKey { get; set; }
        public bool Enabled { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public ComplexRuleSpec() { Kind = "STATE_TRANSITION"; StateFrom = "0"; StateTo = "1"; NeighborState = "1"; Probability = 1; Coupling = 1; Enabled = true; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Enabled ? "ACTIVE" : "DISABLED") + "   ·   " + (Name ?? Id ?? "RULE") + "   ·   " + (Kind ?? "STATE_TRANSITION") + "   ·   " + (StateFrom ?? "*") + " → " + (StateTo ?? "*") + "   ·   neighbor≥" + MinimumNeighborFraction.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexInterventionSpec
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Step { get; set; }
        public int Duration { get; set; }
        public string Kind { get; set; }
        public string TargetKind { get; set; }
        public string TargetValue { get; set; }
        public double Magnitude { get; set; }
        public double Fraction { get; set; }
        public bool Enabled { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public ComplexInterventionSpec() { Duration = 1; Kind = "SEED_STATE"; TargetKind = "ALL"; TargetValue = "1"; Magnitude = 1; Fraction = 0.05; Enabled = true; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Enabled ? "ACTIVE" : "DISABLED") + "   ·   t=" + Step.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Name ?? Kind ?? "INTERVENTION") + "   ·   " + (TargetKind ?? "ALL") + "   ·   magnitude " + Magnitude.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   fraction " + Fraction.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexSystemModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Revision { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string PopulationKind { get; set; }
        public int PopulationSize { get; set; }
        public string TopologyKind { get; set; }
        public string DynamicsKind { get; set; }
        public string UpdateMode { get; set; }
        public int Seed { get; set; }
        public int Steps { get; set; }
        public int MeanDegree { get; set; }
        public double LinkProbability { get; set; }
        public double RewireProbability { get; set; }
        public int CommunityCount { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<ComplexAgentSpec> Agents { get; set; }
        public List<ComplexInteractionEdge> Edges { get; set; }
        public List<ComplexRuleSpec> Rules { get; set; }
        public List<ComplexInterventionSpec> Interventions { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public ComplexSystemModel()
        {
            Status = "ACTIVE"; Revision = 1; PopulationKind = "ACTORS"; PopulationSize = 128; TopologyKind = "SMALL_WORLD"; DynamicsKind = "THRESHOLD_CONTAGION"; UpdateMode = "SYNCHRONOUS"; Seed = 1337; Steps = 128; MeanDegree = 6; LinkProbability = 0.04; RewireProbability = 0.08; CommunityCount = 4;
            Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Agents = new List<ComplexAgentSpec>(); Edges = new List<ComplexInteractionEdge>(); Rules = new List<ComplexRuleSpec>(); Interventions = new List<ComplexInterventionSpec>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>();
        }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "COMPLEX SYSTEM") + "   ·   N=" + PopulationSize.ToString(CultureInfo.InvariantCulture) + "   ·   " + (TopologyKind ?? "TOPOLOGY") + " / " + (DynamicsKind ?? "DYNAMICS") + "   ·   rev " + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexNetworkMetrics
    {
        public int Nodes { get; set; }
        public int DirectedArcs { get; set; }
        public int Components { get; set; }
        public int LargestComponent { get; set; }
        public int Isolates { get; set; }
        public double MeanDegree { get; set; }
        public double DegreeVariance { get; set; }
        public double Density { get; set; }
        public double Clustering { get; set; }
        public double MeanPathLength { get; set; }
        public double Diameter { get; set; }
        public double DegreeAssortativity { get; set; }
        public double SpectralRadius { get; set; }
        public double CommunityModularityProxy { get; set; }
        public List<string> Audit { get; set; }
        public ComplexNetworkMetrics() { Audit = new List<string>(); }
        public override string ToString() { return "NETWORK   ·   N " + Nodes.ToString(CultureInfo.InvariantCulture) + "   ·   arcs " + DirectedArcs.ToString(CultureInfo.InvariantCulture) + "   ·   k̄ " + MeanDegree.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   C " + Clustering.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   L " + MeanPathLength.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   ρ " + SpectralRadius.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   giant " + LargestComponent.ToString(CultureInfo.InvariantCulture) + "/" + Nodes.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexMacrostate
    {
        public int Step { get; set; }
        public double ActiveFraction { get; set; }
        public double RemovedFraction { get; set; }
        public double MeanValue { get; set; }
        public double Variance { get; set; }
        public double OrderParameter { get; set; }
        public double Polarization { get; set; }
        public double StateEntropy { get; set; }
        public double ValueGini { get; set; }
        public double MeanLoadRatio { get; set; }
        public double Performance { get; set; }
        public int ChangedAgents { get; set; }
        public int LargestActiveComponent { get; set; }
        public string StateHash { get; set; }
        public override string ToString() { return Step.ToString(CultureInfo.InvariantCulture).PadLeft(6) + "   ·   active " + ActiveFraction.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   order " + OrderParameter.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   H " + StateEntropy.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   pol " + Polarization.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   perf " + Performance.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   Δ " + ChangedAgents.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexAgentTerminal
    {
        public string AgentId { get; set; }
        public string Archetype { get; set; }
        public int Mode { get; set; }
        public double Value { get; set; }
        public double Load { get; set; }
        public double Capacity { get; set; }
        public int Degree { get; set; }
        public override string ToString() { return (AgentId ?? "AGENT") + "   ·   " + (Archetype ?? "DEFAULT") + "   ·   mode " + Mode.ToString(CultureInfo.InvariantCulture) + "   ·   value " + Value.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   load/cap " + Load.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Capacity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   degree " + Degree.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexSimulationResult
    {
        public string ModelFingerprint { get; set; }
        public int Seed { get; set; }
        public string DynamicsKind { get; set; }
        public string UpdateMode { get; set; }
        public int StepsRequested { get; set; }
        public int StepsCompleted { get; set; }
        public bool AttractorDetected { get; set; }
        public int TransientLength { get; set; }
        public int AttractorPeriod { get; set; }
        public string TerminalHash { get; set; }
        public ComplexNetworkMetrics Network { get; set; }
        public List<ComplexMacrostate> Trace { get; set; }
        public List<ComplexAgentTerminal> TerminalSample { get; set; }
        public List<string> Audit { get; set; }
        public ComplexSimulationResult() { Trace = new List<ComplexMacrostate>(); TerminalSample = new List<ComplexAgentTerminal>(); Audit = new List<string>(); }
        public ComplexMacrostate Terminal { get { return Trace == null || Trace.Count == 0 ? null : Trace[Trace.Count - 1]; } }
        public override string ToString() { ComplexMacrostate t = Terminal; return "SIMULATION   ·   seed " + Seed.ToString(CultureInfo.InvariantCulture) + "   ·   steps " + StepsCompleted.ToString(CultureInfo.InvariantCulture) + "   ·   " + (AttractorDetected ? "attractor period " + AttractorPeriod.ToString(CultureInfo.InvariantCulture) : "no bounded attractor") + "   ·   terminal active " + (t == null ? "—" : t.ActiveFraction.ToString("0.######", CultureInfo.InvariantCulture)) + "   ·   " + Short(TerminalHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(16, x.Length)); }
    }

    internal sealed class ComplexEnsembleResult
    {
        public string ModelFingerprint { get; set; }
        public int Replicates { get; set; }
        public double MeanTerminalActive { get; set; }
        public double StandardDeviationTerminalActive { get; set; }
        public double Q05TerminalActive { get; set; }
        public double MedianTerminalActive { get; set; }
        public double Q95TerminalActive { get; set; }
        public double MeanTerminalOrder { get; set; }
        public double MeanPeakActivity { get; set; }
        public double CollapseProbability { get; set; }
        public double AttractorDetectionRate { get; set; }
        public Dictionary<string, int> TerminalBasins { get; set; }
        public List<int> Seeds { get; set; }
        public List<string> Audit { get; set; }
        public ComplexEnsembleResult() { TerminalBasins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Seeds = new List<int>(); Audit = new List<string>(); }
        public override string ToString() { return "ENSEMBLE   ·   n " + Replicates.ToString(CultureInfo.InvariantCulture) + "   ·   active μ±σ " + MeanTerminalActive.ToString("0.######", CultureInfo.InvariantCulture) + " ± " + StandardDeviationTerminalActive.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   q05/q50/q95 " + Q05TerminalActive.ToString("0.###", CultureInfo.InvariantCulture) + "/" + MedianTerminalActive.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Q95TerminalActive.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   collapse p " + CollapseProbability.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   basins " + TerminalBasins.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexPhasePoint
    {
        public double ParameterValue { get; set; }
        public double MeanOrder { get; set; }
        public double VarianceOrder { get; set; }
        public double Susceptibility { get; set; }
        public double BinderCumulant { get; set; }
        public double MeanActivity { get; set; }
        public double NumericalDerivative { get; set; }
        public override string ToString() { return "λ " + ParameterValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   m " + MeanOrder.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   χ " + Susceptibility.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   U₄ " + BinderCumulant.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   dm/dλ " + NumericalDerivative.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexPhaseSweepResult
    {
        public string ParameterKey { get; set; }
        public List<ComplexPhasePoint> Points { get; set; }
        public double CriticalCandidate { get; set; }
        public double PeakSusceptibility { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public ComplexPhaseSweepResult() { Points = new List<ComplexPhasePoint>(); Audit = new List<string>(); }
        public override string ToString() { return "PHASE SWEEP   ·   " + (ParameterKey ?? "parameter") + "   ·   candidate λc " + CriticalCandidate.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   peak χ " + PeakSusceptibility.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   points " + Points.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexResilienceResult
    {
        public int ShockStep { get; set; }
        public double MaximumDisruption { get; set; }
        public double IntegralPerformanceLoss { get; set; }
        public int RecoveryStep { get; set; }
        public double RecoveryFraction { get; set; }
        public double AdaptiveGain { get; set; }
        public List<double> Divergence { get; set; }
        public List<string> Audit { get; set; }
        public ComplexResilienceResult() { RecoveryStep = -1; Divergence = new List<double>(); Audit = new List<string>(); }
        public override string ToString() { return "RESILIENCE   ·   shock t=" + ShockStep.ToString(CultureInfo.InvariantCulture) + "   ·   max loss " + MaximumDisruption.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   integral " + IntegralPerformanceLoss.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   recovery " + (RecoveryStep < 0 ? "not observed" : RecoveryStep.ToString(CultureInfo.InvariantCulture)) + "   ·   adaptation " + AdaptiveGain.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexCriticalityResult
    {
        public int AvalancheCount { get; set; }
        public double MeanAvalancheSize { get; set; }
        public double MaximumAvalancheSize { get; set; }
        public double BranchingRatio { get; set; }
        public double LogLogTailSlope { get; set; }
        public double LagOneAutocorrelation { get; set; }
        public double VarianceInflation { get; set; }
        public List<int> AvalancheSizes { get; set; }
        public List<string> Audit { get; set; }
        public ComplexCriticalityResult() { AvalancheSizes = new List<int>(); Audit = new List<string>(); }
        public override string ToString() { return "CRITICALITY   ·   avalanches " + AvalancheCount.ToString(CultureInfo.InvariantCulture) + "   ·   mean/max " + MeanAvalancheSize.ToString("0.###", CultureInfo.InvariantCulture) + "/" + MaximumAvalancheSize.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   branching " + BranchingRatio.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   tail slope " + LogLogTailSlope.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   AC₁ " + LagOneAutocorrelation.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexSensitivityEffect
    {
        public string ParameterKey { get; set; }
        public double BaselineValue { get; set; }
        public double MinusMetric { get; set; }
        public double PlusMetric { get; set; }
        public double ElementaryEffect { get; set; }
        public double Elasticity { get; set; }
        public override string ToString() { return (ParameterKey ?? "parameter") + "   ·   base " + BaselineValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   −/+ " + MinusMetric.ToString("0.######", CultureInfo.InvariantCulture) + "/" + PlusMetric.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   effect " + ElementaryEffect.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   elasticity " + Elasticity.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexSensitivityResult
    {
        public List<ComplexSensitivityEffect> Effects { get; set; }
        public string DominantParameter { get; set; }
        public double ConditionProxy { get; set; }
        public List<string> Audit { get; set; }
        public ComplexSensitivityResult() { Effects = new List<ComplexSensitivityEffect>(); Audit = new List<string>(); }
        public override string ToString() { return "SENSITIVITY   ·   dominant " + (DominantParameter ?? "∅") + "   ·   condition proxy " + ConditionProxy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   dimensions " + Effects.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexPercolationPoint
    {
        public double RemovedFraction { get; set; }
        public double RandomGiantFraction { get; set; }
        public double TargetedGiantFraction { get; set; }
        public double RandomComponents { get; set; }
        public double TargetedComponents { get; set; }
        public override string ToString() { return "removed " + RemovedFraction.ToString("P1", CultureInfo.InvariantCulture).PadLeft(7) + "   ·   giant random/targeted " + RandomGiantFraction.ToString("0.000000", CultureInfo.InvariantCulture) + "/" + TargetedGiantFraction.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   components " + RandomComponents.ToString("0", CultureInfo.InvariantCulture) + "/" + TargetedComponents.ToString("0", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexPercolationResult
    {
        public List<ComplexPercolationPoint> Points { get; set; }
        public double RandomRobustnessArea { get; set; }
        public double TargetedRobustnessArea { get; set; }
        public double RandomHalfCollapseFraction { get; set; }
        public double TargetedHalfCollapseFraction { get; set; }
        public double AttackFragilityRatio { get; set; }
        public List<string> Audit { get; set; }
        public ComplexPercolationResult() { Points = new List<ComplexPercolationPoint>(); Audit = new List<string>(); RandomHalfCollapseFraction = 1; TargetedHalfCollapseFraction = 1; }
        public override string ToString() { return "PERCOLATION   ·   robustness random/targeted " + RandomRobustnessArea.ToString("0.######", CultureInfo.InvariantCulture) + "/" + TargetedRobustnessArea.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   half-collapse " + RandomHalfCollapseFraction.ToString("P1", CultureInfo.InvariantCulture) + "/" + TargetedHalfCollapseFraction.ToString("P1", CultureInfo.InvariantCulture) + "   ·   fragility ratio " + AttackFragilityRatio.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexInformationDynamicsResult
    {
        public double SymbolEntropy { get; set; }
        public double EntropyRate { get; set; }
        public double LagOneMutualInformation { get; set; }
        public double ExcessEntropyProxy { get; set; }
        public double PermutationEntropy { get; set; }
        public double LempelZivComplexity { get; set; }
        public double PredictiveEfficiency { get; set; }
        public int ChangePointStep { get; set; }
        public double ChangePointStrength { get; set; }
        public double EarlyVariance { get; set; }
        public double LateVariance { get; set; }
        public double EarlyAutocorrelation { get; set; }
        public double LateAutocorrelation { get; set; }
        public List<string> Audit { get; set; }
        public ComplexInformationDynamicsResult() { ChangePointStep = -1; Audit = new List<string>(); }
        public override string ToString() { return "INFORMATION DYNAMICS   ·   H " + SymbolEntropy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   hμ " + EntropyRate.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   I₁ " + LagOneMutualInformation.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   E≈ " + ExcessEntropyProxy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   Hperm " + PermutationEntropy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   LZ " + LempelZivComplexity.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   change t=" + ChangePointStep.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class ComplexUpdateSemanticsResult
    {
        public ComplexSimulationResult Synchronous { get; set; }
        public ComplexSimulationResult Asynchronous { get; set; }
        public double MaximumActivityDivergence { get; set; }
        public double TerminalActivityDivergence { get; set; }
        public double TerminalOrderDivergence { get; set; }
        public double TerminalPerformanceDivergence { get; set; }
        public bool SameTerminalBasin { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public ComplexUpdateSemanticsResult() { Audit = new List<string>(); }
        public override string ToString() { return "UPDATE SEMANTICS   ·   max Δactivity " + MaximumActivityDivergence.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   terminal Δactivity/order/perf " + TerminalActivityDivergence.ToString("0.######", CultureInfo.InvariantCulture) + "/" + TerminalOrderDivergence.ToString("0.######", CultureInfo.InvariantCulture) + "/" + TerminalPerformanceDivergence.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + (SameTerminalBasin ? "same basin" : "divergent basins"); }
    }

    internal static class ComplexSystemsKernel
    {
        private const double Epsilon = 1e-12;
        private const int MaximumPopulation = 4096;
        private const int MaximumSteps = 8192;
        private const int MaximumEdges = 262144;

        private sealed class RuntimeAgent
        {
            public string Id;
            public string Archetype;
            public int Mode;
            public double Value;
            public double Threshold;
            public double Susceptibility;
            public double Capacity;
            public double Load;
            public double Noise;
            public bool Removed;
        }

        private sealed class RuntimeGraph
        {
            public List<int>[] Neighbors;
            public List<double>[] Weights;
            public int[] Community;
        }

        private sealed class StableRandom
        {
            private ulong _state;
            public StableRandom(int seed) { _state = ((ulong)(uint)seed << 1) | 1UL; for (int i = 0; i < 8; i++) NextUInt64(); }
            private ulong NextUInt64() { _state += 0x9E3779B97F4A7C15UL; ulong z = _state; z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL; z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL; return z ^ (z >> 31); }
            public double NextDouble() { return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0); }
            public int Next(int maximum) { return maximum <= 1 ? 0 : (int)(NextDouble() * maximum); }
            public double Normal() { double u1 = Math.Max(Epsilon, NextDouble()), u2 = NextDouble(); return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2); }
        }

        public static ComplexNetworkMetrics AnalyzeNetwork(ComplexSystemModel model)
        {
            List<RuntimeAgent> agents; RuntimeGraph graph; BuildRuntime(model, model == null ? 0 : model.Seed, out agents, out graph); return NetworkMetrics(graph, agents);
        }

        public static ComplexSimulationResult Simulate(ComplexSystemModel model, int seed)
        {
            ComplexSimulationResult result = new ComplexSimulationResult { ModelFingerprint = Fingerprint(model), Seed = seed, DynamicsKind = model == null ? "" : model.DynamicsKind, UpdateMode = model == null ? "" : model.UpdateMode, StepsRequested = model == null ? 0 : model.Steps };
            result.Audit.AddRange(Audit(model));
            if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;

            StableRandom random = new StableRandom(seed); List<RuntimeAgent> agents; RuntimeGraph graph; BuildRuntime(model, seed, out agents, out graph); result.Network = NetworkMetrics(graph, agents);
            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            ComplexMacrostate initial = Measure(model, 0, agents, graph, agents.Count); result.Trace.Add(initial); seen[initial.StateHash] = 0;
            int steps = Math.Max(1, Math.Min(MaximumSteps, model.Steps));
            for (int step = 1; step <= steps; step++)
            {
                ApplyInterventions(model, agents, graph, step, random);
                int changed = Advance(model, agents, graph, random);
                ComplexMacrostate macro = Measure(model, step, agents, graph, changed); result.Trace.Add(macro); result.StepsCompleted = step;
                int prior;
                if (seen.TryGetValue(macro.StateHash, out prior)) { result.AttractorDetected = true; result.TransientLength = prior; result.AttractorPeriod = step - prior; if (result.AttractorPeriod <= 32 || changed == 0) break; }
                else seen[macro.StateHash] = step;
            }
            ComplexMacrostate terminal = result.Terminal; result.TerminalHash = terminal == null ? "" : terminal.StateHash;
            result.TerminalSample = agents.Take(256).Select((a, i) => new ComplexAgentTerminal { AgentId = a.Id, Archetype = a.Archetype, Mode = a.Mode, Value = a.Value, Load = a.Load, Capacity = a.Capacity, Degree = graph.Neighbors[i].Count }).ToList();
            if (!result.AttractorDetected) result.Audit.Add("BOUNDARY · no repeated aggregate microstate hash was found inside the bounded horizon");
            return result;
        }

        public static ComplexEnsembleResult RunEnsemble(ComplexSystemModel model, int replicates, int seedOffset)
        {
            ComplexEnsembleResult result = new ComplexEnsembleResult { ModelFingerprint = Fingerprint(model), Replicates = Math.Max(1, Math.Min(512, replicates)) }; result.Audit.AddRange(Audit(model));
            if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            List<double> active = new List<double>(), order = new List<double>(), peaks = new List<double>(); int collapses = 0, attractors = 0;
            for (int i = 0; i < result.Replicates; i++)
            {
                int seed = unchecked(model.Seed + seedOffset + i * 104729); result.Seeds.Add(seed); ComplexSimulationResult run = Simulate(model, seed); ComplexMacrostate terminal = run.Terminal; if (terminal == null) continue;
                active.Add(terminal.ActiveFraction); order.Add(terminal.OrderParameter); peaks.Add(run.Trace.Max(x => x.ActiveFraction)); if (terminal.Performance < 0.2 || terminal.RemovedFraction > 0.8) collapses++; if (run.AttractorDetected) attractors++;
                string basin = String.IsNullOrWhiteSpace(run.TerminalHash) ? "UNRESOLVED" : run.TerminalHash.Substring(0, Math.Min(24, run.TerminalHash.Length)); int count; result.TerminalBasins[basin] = result.TerminalBasins.TryGetValue(basin, out count) ? count + 1 : 1;
            }
            active.Sort(); result.MeanTerminalActive = Mean(active); result.StandardDeviationTerminalActive = StandardDeviation(active); result.Q05TerminalActive = Quantile(active, 0.05); result.MedianTerminalActive = Quantile(active, 0.5); result.Q95TerminalActive = Quantile(active, 0.95); result.MeanTerminalOrder = Mean(order); result.MeanPeakActivity = Mean(peaks); result.CollapseProbability = result.Replicates == 0 ? 0 : collapses / (double)result.Replicates; result.AttractorDetectionRate = result.Replicates == 0 ? 0 : attractors / (double)result.Replicates;
            result.Audit.Add("BOUNDARY · ensemble frequencies describe the declared deterministic seed family, not calibrated real-world probabilities"); return result;
        }

        public static ComplexPhaseSweepResult SweepParameter(ComplexSystemModel model, string parameterKey, double minimum, double maximum, int points, int replicates)
        {
            ComplexPhaseSweepResult result = new ComplexPhaseSweepResult { ParameterKey = parameterKey ?? "coupling" }; result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            points = Math.Max(3, Math.Min(129, points)); replicates = Math.Max(2, Math.Min(128, replicates)); double original = Parameter(model, result.ParameterKey, 0); bool existed = model.Parameters != null && model.Parameters.ContainsKey(result.ParameterKey);
            try
            {
                for (int p = 0; p < points; p++)
                {
                    double value = minimum + (maximum - minimum) * p / (points - 1.0); model.Parameters[result.ParameterKey] = value; List<double> orders = new List<double>(), activities = new List<double>();
                    for (int r = 0; r < replicates; r++) { ComplexSimulationResult run = Simulate(model, unchecked(model.Seed + p * 1000003 + r * 7919)); if (run.Terminal != null) { orders.Add(run.Terminal.OrderParameter); activities.Add(run.Terminal.ActiveFraction); } }
                    double mean = Mean(orders), variance = Variance(orders), second = Mean(orders.Select(x => x * x)), fourth = Mean(orders.Select(x => x * x * x * x)); result.Points.Add(new ComplexPhasePoint { ParameterValue = value, MeanOrder = mean, VarianceOrder = variance, Susceptibility = Math.Max(1, model.PopulationSize) * variance, BinderCumulant = second <= Epsilon ? 0 : 1 - fourth / (3 * second * second), MeanActivity = Mean(activities) });
                }
            }
            finally { if (existed) model.Parameters[result.ParameterKey] = original; else model.Parameters.Remove(result.ParameterKey); }
            for (int i = 0; i < result.Points.Count; i++) { int lo = Math.Max(0, i - 1), hi = Math.Min(result.Points.Count - 1, i + 1); double dx = result.Points[hi].ParameterValue - result.Points[lo].ParameterValue; result.Points[i].NumericalDerivative = Math.Abs(dx) <= Epsilon ? 0 : (result.Points[hi].MeanOrder - result.Points[lo].MeanOrder) / dx; }
            ComplexPhasePoint critical = result.Points.OrderByDescending(x => x.Susceptibility).ThenByDescending(x => Math.Abs(x.NumericalDerivative)).FirstOrDefault(); if (critical != null) { result.CriticalCandidate = critical.ParameterValue; result.PeakSusceptibility = critical.Susceptibility; }
            result.CertificateHash = HashText(Fingerprint(model) + "|" + result.ParameterKey + "|" + minimum.ToString("R", CultureInfo.InvariantCulture) + "|" + maximum.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join(";", result.Points.Select(x => x.ParameterValue.ToString("R", CultureInfo.InvariantCulture) + ":" + x.MeanOrder.ToString("R", CultureInfo.InvariantCulture) + ":" + x.Susceptibility.ToString("R", CultureInfo.InvariantCulture)).ToArray()));
            result.Audit.Add("BOUNDARY · susceptibility peaks and Binder behavior are finite-size criticality candidates, not thermodynamic-limit proofs"); return result;
        }

        public static ComplexResilienceResult AnalyzeResilience(ComplexSystemModel model, int shockStep, double shockFraction, double shockMagnitude)
        {
            ComplexResilienceResult result = new ComplexResilienceResult { ShockStep = Math.Max(1, shockStep) }; result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            List<ComplexInterventionSpec> original = model.Interventions; model.Interventions = new List<ComplexInterventionSpec>(); ComplexSimulationResult baseline;
            try { baseline = Simulate(model, model.Seed); string shockKind = Eq(model.DynamicsKind, "CASCADE_FAILURE") ? "ADD_LOAD" : (Eq(model.DynamicsKind, "BOUNDED_CONFIDENCE") || Eq(model.DynamicsKind, "COUPLED_MAP") ? "SHOCK_VALUE" : "SEED_STATE"); model.Interventions.Add(new ComplexInterventionSpec { Id = "resilience-shock", Name = "Bounded resilience shock", Step = result.ShockStep, Duration = 1, Kind = shockKind, TargetKind = "ALL", TargetValue = "1", Magnitude = shockMagnitude, Fraction = Clamp(shockFraction, 0, 1), Enabled = true }); ComplexSimulationResult shocked = Simulate(model, model.Seed); int count = Math.Min(baseline.Trace.Count, shocked.Trace.Count); double tolerance = 0.02; for (int i = 0; i < count; i++) { double divergence = Math.Abs(baseline.Trace[i].Performance - shocked.Trace[i].Performance); result.Divergence.Add(divergence); if (i >= result.ShockStep) { result.MaximumDisruption = Math.Max(result.MaximumDisruption, divergence); result.IntegralPerformanceLoss += Math.Max(0, baseline.Trace[i].Performance - shocked.Trace[i].Performance); if (result.RecoveryStep < 0 && i > result.ShockStep && divergence <= tolerance) result.RecoveryStep = i; } } if (result.RecoveryStep >= 0) result.RecoveryFraction = 1; else if (result.MaximumDisruption > Epsilon && result.Divergence.Count > 0) result.RecoveryFraction = Clamp(1 - result.Divergence[result.Divergence.Count - 1] / result.MaximumDisruption, 0, 1); if (count > 0) result.AdaptiveGain = shocked.Trace[count - 1].Performance - baseline.Trace[count - 1].Performance; }
            finally { model.Interventions = original ?? new List<ComplexInterventionSpec>(); }
            result.Audit.Add("BOUNDARY · resilience is divergence from a same-seed declared baseline under one explicit synthetic shock"); return result;
        }

        public static ComplexCriticalityResult AnalyzeCriticality(ComplexSystemModel model, int replicates)
        {
            ComplexCriticalityResult result = new ComplexCriticalityResult(); result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            replicates = Math.Max(1, Math.Min(128, replicates)); List<double> activitySeries = new List<double>(); double parentEvents = 0, childEvents = 0;
            for (int r = 0; r < replicates; r++)
            {
                ComplexSimulationResult run = Simulate(model, unchecked(model.Seed + 32452843 * r)); List<int> avalanche = new List<int>();
                foreach (ComplexMacrostate m in run.Trace) { activitySeries.Add(m.ChangedAgents); if (m.ChangedAgents > 0) avalanche.Add(m.ChangedAgents); else if (avalanche.Count > 0) { result.AvalancheSizes.Add(avalanche.Sum()); for (int i = 1; i < avalanche.Count; i++) { parentEvents += avalanche[i - 1]; childEvents += avalanche[i]; } avalanche.Clear(); } }
                if (avalanche.Count > 0) result.AvalancheSizes.Add(avalanche.Sum());
            }
            result.AvalancheCount = result.AvalancheSizes.Count; result.MeanAvalancheSize = Mean(result.AvalancheSizes.Select(x => (double)x)); result.MaximumAvalancheSize = result.AvalancheSizes.Count == 0 ? 0 : result.AvalancheSizes.Max(); result.BranchingRatio = parentEvents <= Epsilon ? 0 : childEvents / parentEvents; result.LogLogTailSlope = TailSlope(result.AvalancheSizes); result.LagOneAutocorrelation = Autocorrelation(activitySeries, 1); result.VarianceInflation = Mean(activitySeries) <= Epsilon ? 0 : Variance(activitySeries) / Mean(activitySeries);
            result.Audit.Add("BOUNDARY · avalanche tails, branching ratio and critical slowing proxies require independent scale tests before a criticality claim"); return result;
        }

        public static ComplexSensitivityResult AnalyzeSensitivity(ComplexSystemModel model, IEnumerable<string> parameterKeys, double relativeDelta)
        {
            ComplexSensitivityResult result = new ComplexSensitivityResult(); result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            relativeDelta = Clamp(Math.Abs(relativeDelta), 0.0001, 0.9); List<string> keys = (parameterKeys ?? model.Parameters.Keys).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(64).ToList();
            foreach (string key in keys)
            {
                bool existed = model.Parameters.ContainsKey(key); double baseline = Parameter(model, key, 0), delta = Math.Max(1e-6, Math.Abs(baseline) * relativeDelta); if (Math.Abs(baseline) <= Epsilon) delta = relativeDelta;
                double minus, plus; try { model.Parameters[key] = baseline - delta; ComplexSimulationResult low = Simulate(model, model.Seed); minus = low.Terminal == null ? 0 : low.Terminal.OrderParameter; model.Parameters[key] = baseline + delta; ComplexSimulationResult high = Simulate(model, model.Seed); plus = high.Terminal == null ? 0 : high.Terminal.OrderParameter; }
                finally { if (existed) model.Parameters[key] = baseline; else model.Parameters.Remove(key); }
                double effect = (plus - minus) / (2 * delta); result.Effects.Add(new ComplexSensitivityEffect { ParameterKey = key, BaselineValue = baseline, MinusMetric = minus, PlusMetric = plus, ElementaryEffect = effect, Elasticity = Math.Abs(baseline) <= Epsilon ? effect : effect * baseline / Math.Max(Epsilon, Math.Abs((plus + minus) * 0.5)) });
            }
            ComplexSensitivityEffect dominant = result.Effects.OrderByDescending(x => Math.Abs(x.ElementaryEffect)).FirstOrDefault(); result.DominantParameter = dominant == null ? null : dominant.ParameterKey; double min = result.Effects.Count == 0 ? 0 : result.Effects.Min(x => Math.Abs(x.ElementaryEffect)), max = result.Effects.Count == 0 ? 0 : result.Effects.Max(x => Math.Abs(x.ElementaryEffect)); result.ConditionProxy = min <= Epsilon ? (max <= Epsilon ? 0 : Double.MaxValue) : max / min; result.Audit.Add("BOUNDARY · one-at-a-time local elementary effects do not identify interactions or global causal sensitivity"); return result;
        }

        public static ComplexPercolationResult AnalyzePercolation(ComplexSystemModel model, int points, int randomReplicates)
        {
            ComplexPercolationResult result = new ComplexPercolationResult(); result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result;
            points = Math.Max(3, Math.Min(101, points)); randomReplicates = Math.Max(1, Math.Min(64, randomReplicates)); List<RuntimeAgent> sourceAgents; RuntimeGraph graph; BuildRuntime(model, model.Seed, out sourceAgents, out graph); int n = sourceAgents.Count; List<int> targetedOrder = Enumerable.Range(0, n).OrderByDescending(i => graph.Neighbors[i].Count).ThenBy(i => i).ToList();
            for (int p = 0; p < points; p++)
            {
                double fraction = p / (points - 1.0); int remove = Math.Min(n, (int)Math.Round(fraction * n)); double targetedGiant, targetedComponents; PercolationMeasure(graph, n, new HashSet<int>(targetedOrder.Take(remove)), out targetedGiant, out targetedComponents); List<double> randomGiants = new List<double>(), randomComponents = new List<double>();
                for (int r = 0; r < randomReplicates; r++) { List<int> order = Enumerable.Range(0, n).ToList(); StableRandom rng = new StableRandom(unchecked(model.Seed + p * 15485863 + r * 32452843)); for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1), t = order[i]; order[i] = order[j]; order[j] = t; } double giant, components; PercolationMeasure(graph, n, new HashSet<int>(order.Take(remove)), out giant, out components); randomGiants.Add(giant); randomComponents.Add(components); }
                result.Points.Add(new ComplexPercolationPoint { RemovedFraction = fraction, RandomGiantFraction = Mean(randomGiants), TargetedGiantFraction = targetedGiant, RandomComponents = Mean(randomComponents), TargetedComponents = targetedComponents });
            }
            for (int i = 1; i < result.Points.Count; i++) { double dx = result.Points[i].RemovedFraction - result.Points[i - 1].RemovedFraction; result.RandomRobustnessArea += dx * (result.Points[i].RandomGiantFraction + result.Points[i - 1].RandomGiantFraction) * 0.5; result.TargetedRobustnessArea += dx * (result.Points[i].TargetedGiantFraction + result.Points[i - 1].TargetedGiantFraction) * 0.5; }
            ComplexPercolationPoint randomHalf = result.Points.FirstOrDefault(x => x.RandomGiantFraction <= 0.5), targetedHalf = result.Points.FirstOrDefault(x => x.TargetedGiantFraction <= 0.5); if (randomHalf != null) result.RandomHalfCollapseFraction = randomHalf.RemovedFraction; if (targetedHalf != null) result.TargetedHalfCollapseFraction = targetedHalf.RemovedFraction; result.AttackFragilityRatio = result.TargetedRobustnessArea <= Epsilon ? (result.RandomRobustnessArea <= Epsilon ? 0 : Double.MaxValue) : result.RandomRobustnessArea / result.TargetedRobustnessArea; result.Audit.Add("BOUNDARY · targeted attack uses static initial degree order; adaptive attacks and real failure dependence require separate models"); return result;
        }

        public static ComplexInformationDynamicsResult AnalyzeInformationDynamics(ComplexSystemModel model, int seed)
        {
            ComplexInformationDynamicsResult result = new ComplexInformationDynamicsResult(); ComplexSimulationResult run = Simulate(model, seed); result.Audit.AddRange(run.Audit); List<double> series = run.Trace.Select(x => x.ActiveFraction).ToList(); if (series.Count < 4) { result.Audit.Add("BLOCKER · TRACE_TOO_SHORT_FOR_INFORMATION_DYNAMICS"); return result; }
            List<int> symbols = Symbolize(series, 8); result.SymbolEntropy = DiscreteEntropy(symbols); result.LagOneMutualInformation = DiscreteMutualInformation(symbols, 1); double pairEntropy = TupleEntropy(symbols, 2); result.EntropyRate = Math.Max(0, pairEntropy - result.SymbolEntropy); double excess = 0; for (int lag = 1; lag <= Math.Min(16, symbols.Count / 4); lag++) excess += DiscreteMutualInformation(symbols, lag); result.ExcessEntropyProxy = excess; result.PermutationEntropy = PermutationEntropy(series, 3); result.LempelZivComplexity = LempelZivComplexity(symbols); result.PredictiveEfficiency = result.SymbolEntropy <= Epsilon ? 0 : Clamp(result.LagOneMutualInformation / result.SymbolEntropy, 0, 1);
            int half = series.Count / 2; result.EarlyVariance = Variance(series.Take(half)); result.LateVariance = Variance(series.Skip(half)); result.EarlyAutocorrelation = Autocorrelation(series.Take(half).ToList(), 1); result.LateAutocorrelation = Autocorrelation(series.Skip(half).ToList(), 1); double mean = series.Average(), cumulative = 0, best = 0; for (int i = 0; i < series.Count; i++) { cumulative += series[i] - mean; if (Math.Abs(cumulative) > best) { best = Math.Abs(cumulative); result.ChangePointStep = i; } } result.ChangePointStrength = best / Math.Max(Epsilon, StandardDeviation(series) * Math.Sqrt(series.Count)); result.Audit.Add("BOUNDARY · aggregate information metrics depend on symbolization, horizon and selected observable; they are descriptive, not consciousness or causal-emergence measures"); return result;
        }

        public static ComplexUpdateSemanticsResult CompareUpdateSemantics(ComplexSystemModel model, int seed)
        {
            ComplexUpdateSemanticsResult result = new ComplexUpdateSemanticsResult(); result.Audit.AddRange(Audit(model)); if (model == null || result.Audit.Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase))) return result; string original = model.UpdateMode;
            try { model.UpdateMode = "SYNCHRONOUS"; result.Synchronous = Simulate(model, seed); model.UpdateMode = "ASYNCHRONOUS"; result.Asynchronous = Simulate(model, seed); }
            finally { model.UpdateMode = original; }
            int count = Math.Min(result.Synchronous.Trace.Count, result.Asynchronous.Trace.Count); for (int i = 0; i < count; i++) result.MaximumActivityDivergence = Math.Max(result.MaximumActivityDivergence, Math.Abs(result.Synchronous.Trace[i].ActiveFraction - result.Asynchronous.Trace[i].ActiveFraction)); ComplexMacrostate a = result.Synchronous.Terminal, b = result.Asynchronous.Terminal; if (a != null && b != null) { result.TerminalActivityDivergence = Math.Abs(a.ActiveFraction - b.ActiveFraction); result.TerminalOrderDivergence = Math.Abs(a.OrderParameter - b.OrderParameter); result.TerminalPerformanceDivergence = Math.Abs(a.Performance - b.Performance); result.SameTerminalBasin = Eq(result.Synchronous.TerminalHash, result.Asynchronous.TerminalHash); }
            result.CertificateHash = HashText(Fingerprint(model) + "|UPDATE_SEMANTICS|" + seed.ToString(CultureInfo.InvariantCulture) + "|" + (result.Synchronous.TerminalHash ?? "") + "|" + (result.Asynchronous.TerminalHash ?? "") + "|" + result.MaximumActivityDivergence.ToString("R", CultureInfo.InvariantCulture)); result.Audit.Add("BOUNDARY · divergence is an exact within-model ordering effect for one seed, not evidence that either scheduling regime describes reality"); return result;
        }

        public static List<string> Audit(ComplexSystemModel model)
        {
            List<string> findings = new List<string>(); if (model == null) { findings.Add("BLOCKER · NULL_MODEL"); return findings; }
            if (String.IsNullOrWhiteSpace(model.Id)) findings.Add("WARN · MISSING_ID"); if (String.IsNullOrWhiteSpace(model.Name)) findings.Add("BLOCKER · MISSING_NAME");
            if (model.PopulationSize < 2 || model.PopulationSize > MaximumPopulation) findings.Add("BLOCKER · POPULATION_RANGE · expected 2.." + MaximumPopulation.ToString(CultureInfo.InvariantCulture));
            if (model.Steps < 1 || model.Steps > MaximumSteps) findings.Add("BLOCKER · STEP_RANGE · expected 1.." + MaximumSteps.ToString(CultureInfo.InvariantCulture));
            if (model.MeanDegree < 0 || model.MeanDegree >= Math.Max(2, model.PopulationSize)) findings.Add("BLOCKER · MEAN_DEGREE_RANGE");
            if (!InUnit(model.LinkProbability) || !InUnit(model.RewireProbability)) findings.Add("BLOCKER · TOPOLOGY_PROBABILITY_RANGE");
            if (model.CommunityCount < 1 || model.CommunityCount > Math.Max(1, model.PopulationSize)) findings.Add("BLOCKER · COMMUNITY_COUNT_RANGE");
            if (!KnownTopology(model.TopologyKind)) findings.Add("BLOCKER · UNKNOWN_TOPOLOGY · " + (model.TopologyKind ?? "∅")); if (!KnownDynamics(model.DynamicsKind)) findings.Add("BLOCKER · UNKNOWN_DYNAMICS · " + (model.DynamicsKind ?? "∅"));
            if (!Eq(model.UpdateMode, "SYNCHRONOUS") && !Eq(model.UpdateMode, "ASYNCHRONOUS")) findings.Add("BLOCKER · UNKNOWN_UPDATE_MODE");
            List<ComplexAgentSpec> agents = model.Agents ?? new List<ComplexAgentSpec>(); DuplicateFindings(findings, agents.Where(x => x != null).Select(x => x.Id), "AGENT"); foreach (ComplexAgentSpec a in agents.Where(x => x != null)) { if (!Finite(a.InitialValue) || !Finite(a.Threshold) || !Finite(a.Susceptibility) || !Finite(a.Capacity) || !Finite(a.InitialLoad) || !Finite(a.Noise)) findings.Add("BLOCKER · NONFINITE_AGENT · " + (a.Id ?? "∅")); if (a.Capacity <= 0) findings.Add("BLOCKER · NONPOSITIVE_CAPACITY · " + (a.Id ?? "∅")); }
            List<ComplexInteractionEdge> edges = model.Edges ?? new List<ComplexInteractionEdge>(); if (edges.Count > MaximumEdges) findings.Add("BLOCKER · EDGE_CAPACITY"); DuplicateFindings(findings, edges.Where(x => x != null).Select(x => x.Id), "EDGE"); HashSet<string> explicitIds = new HashSet<string>(agents.Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (ComplexInteractionEdge e in edges.Where(x => x != null && Active(x.Status))) { if (String.IsNullOrWhiteSpace(e.SourceId) || String.IsNullOrWhiteSpace(e.TargetId)) findings.Add("BLOCKER · EDGE_ENDPOINT_MISSING · " + (e.Id ?? "∅")); if (explicitIds.Count > 0 && (!explicitIds.Contains(e.SourceId ?? "") || !explicitIds.Contains(e.TargetId ?? ""))) findings.Add("BLOCKER · EDGE_ENDPOINT_UNKNOWN · " + (e.Id ?? "∅")); if (!Finite(e.Weight)) findings.Add("BLOCKER · NONFINITE_EDGE_WEIGHT · " + (e.Id ?? "∅")); }
            DuplicateFindings(findings, (model.Rules ?? new List<ComplexRuleSpec>()).Where(x => x != null).Select(x => x.Id), "RULE"); foreach (ComplexRuleSpec r in (model.Rules ?? new List<ComplexRuleSpec>()).Where(x => x != null && x.Enabled)) if (!InUnit(r.Probability) || !InUnit(r.MinimumNeighborFraction)) findings.Add("BLOCKER · RULE_PROBABILITY_RANGE · " + (r.Id ?? "∅"));
            foreach (ComplexInterventionSpec x in (model.Interventions ?? new List<ComplexInterventionSpec>()).Where(x => x != null && x.Enabled)) { if (x.Step < 0 || x.Step > model.Steps || x.Duration < 1 || !InUnit(x.Fraction) || !Finite(x.Magnitude)) findings.Add("BLOCKER · INTERVENTION_RANGE · " + (x.Id ?? "∅")); }
            foreach (KeyValuePair<string, double> p in model.Parameters ?? new Dictionary<string, double>()) if (String.IsNullOrWhiteSpace(p.Key) || !Finite(p.Value)) findings.Add("BLOCKER · INVALID_PARAMETER · " + (p.Key ?? "∅"));
            if ((model.Assumptions ?? new List<string>()).Count == 0) findings.Add("WARN · NO_EXPLICIT_ASSUMPTIONS"); if ((model.EvidenceNodeIds ?? new List<string>()).Count == 0) findings.Add("INFO · NO_WORLD_EVIDENCE_ANCHORS");
            findings.Add("BOUNDARY · simulation artifacts are conditional consequences of declared rules, topology, parameters and seed; they are not observations, forecasts or deployment authority"); return findings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string Fingerprint(ComplexSystemModel model)
        {
            if (model == null) return HashText("NULL_COMPLEX_MODEL"); StringBuilder s = new StringBuilder();
            s.Append("COMPLEX-SYSTEM-V1|").Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.Name).Append('|').Append(model.Description).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.PopulationKind).Append('|').Append(model.PopulationSize).Append('|').Append(model.TopologyKind).Append('|').Append(model.DynamicsKind).Append('|').Append(model.UpdateMode).Append('|').Append(model.Seed).Append('|').Append(model.Steps).Append('|').Append(model.MeanDegree).Append('|').Append(model.LinkProbability.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(model.RewireProbability.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(model.CommunityCount);
            foreach (KeyValuePair<string, double> p in (model.Parameters ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append("|P:").Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture));
            foreach (ComplexAgentSpec a in (model.Agents ?? new List<ComplexAgentSpec>()).Where(x => x != null).OrderBy(x => x.Id)) { s.Append("|A:").Append(a.Id).Append(':').Append(a.Label).Append(':').Append(a.Kind).Append(':').Append(a.Archetype).Append(':').Append(a.InitialMode).Append(':').Append(a.InitialValue.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.Threshold.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.Susceptibility.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.InitialLoad.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.Noise.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(a.Status); foreach (KeyValuePair<string, double> t in (a.Traits ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append(':').Append(t.Key).Append('=').Append(t.Value.ToString("R", CultureInfo.InvariantCulture)); AppendEvidence(s, a.EvidenceNodeIds); }
            foreach (ComplexInteractionEdge e in (model.Edges ?? new List<ComplexInteractionEdge>()).Where(x => x != null).OrderBy(x => x.Id)) { s.Append("|E:").Append(e.Id).Append(':').Append(e.SourceId).Append(':').Append(e.TargetId).Append(':').Append(e.Kind).Append(':').Append(e.Weight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(e.Bidirectional).Append(':').Append(e.Status); AppendEvidence(s, e.EvidenceNodeIds); }
            foreach (ComplexRuleSpec r in (model.Rules ?? new List<ComplexRuleSpec>()).Where(x => x != null).OrderBy(x => x.Id)) { s.Append("|R:").Append(r.Id).Append(':').Append(r.Name).Append(':').Append(r.Kind).Append(':').Append(r.StateFrom).Append(':').Append(r.StateTo).Append(':').Append(r.NeighborState).Append(':').Append(r.MinimumNeighborFraction.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(r.Probability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(r.ScalarDelta.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(r.Coupling.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(r.Threshold.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(r.ParameterKey).Append(':').Append(r.Enabled); AppendEvidence(s, r.EvidenceNodeIds); }
            foreach (ComplexInterventionSpec x in (model.Interventions ?? new List<ComplexInterventionSpec>()).Where(v => v != null).OrderBy(v => v.Id)) { s.Append("|I:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Step).Append(':').Append(x.Duration).Append(':').Append(x.Kind).Append(':').Append(x.TargetKind).Append(':').Append(x.TargetValue).Append(':').Append(x.Magnitude.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Fraction.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Enabled); AppendEvidence(s, x.EvidenceNodeIds); }
            foreach (string a in (model.Assumptions ?? new List<string>()).OrderBy(x => x)) s.Append("|ASSUME:").Append(a); AppendEvidence(s, model.EvidenceNodeIds); return HashText(s.ToString());
        }

        private static void BuildRuntime(ComplexSystemModel model, int seed, out List<RuntimeAgent> agents, out RuntimeGraph graph)
        {
            int n = model == null ? 0 : Math.Max(2, Math.Min(MaximumPopulation, model.PopulationSize)); StableRandom random = new StableRandom(seed); agents = new List<RuntimeAgent>(n); List<ComplexAgentSpec> specs = model == null ? new List<ComplexAgentSpec>() : (model.Agents ?? new List<ComplexAgentSpec>()).Where(x => x != null && Active(x.Status)).ToList();
            for (int i = 0; i < n; i++) { ComplexAgentSpec spec = i < specs.Count ? specs[i] : null; double initialActive = Parameter(model, "initial_active_fraction", 0.05); int mode = spec == null ? (random.NextDouble() < initialActive ? 1 : 0) : ParseMode(spec.InitialMode); agents.Add(new RuntimeAgent { Id = spec == null || String.IsNullOrWhiteSpace(spec.Id) ? "agent-" + i.ToString("D6", CultureInfo.InvariantCulture) : spec.Id, Archetype = spec == null ? "DEFAULT" : spec.Archetype, Mode = mode, Value = spec == null ? (mode == 1 ? 1 : 0) : FiniteOrZero(spec.InitialValue), Threshold = Clamp(spec == null ? Parameter(model, "threshold", 0.5) : spec.Threshold, 0, 1), Susceptibility = Math.Max(0, spec == null ? Parameter(model, "susceptibility", 1) : spec.Susceptibility), Capacity = Math.Max(Epsilon, spec == null ? Parameter(model, "capacity", 1) : spec.Capacity), Load = Math.Max(0, spec == null ? Parameter(model, "initial_load", 0.4) : spec.InitialLoad), Noise = Math.Max(0, spec == null ? Parameter(model, "noise", 0) : spec.Noise), Removed = mode == 2 }); }
            graph = BuildGraph(model, agents, random);
        }

        private static RuntimeGraph BuildGraph(ComplexSystemModel model, List<RuntimeAgent> agents, StableRandom random)
        {
            int n = agents.Count; RuntimeGraph graph = new RuntimeGraph { Neighbors = new List<int>[n], Weights = new List<double>[n], Community = new int[n] }; for (int i = 0; i < n; i++) { graph.Neighbors[i] = new List<int>(); graph.Weights[i] = new List<double>(); graph.Community[i] = Math.Min(Math.Max(1, model.CommunityCount) - 1, i * Math.Max(1, model.CommunityCount) / Math.Max(1, n)); }
            Dictionary<string, int> ids = agents.Select((a, i) => new { a.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index, StringComparer.OrdinalIgnoreCase); List<ComplexInteractionEdge> explicitEdges = (model.Edges ?? new List<ComplexInteractionEdge>()).Where(x => x != null && Active(x.Status)).ToList();
            if (explicitEdges.Count > 0) { foreach (ComplexInteractionEdge edge in explicitEdges.Take(MaximumEdges)) { int a, b; if (!ids.TryGetValue(edge.SourceId ?? "", out a) || !ids.TryGetValue(edge.TargetId ?? "", out b) || a == b) continue; AddArc(graph, a, b, edge.Weight); if (edge.Bidirectional) AddArc(graph, b, a, edge.Weight); } return graph; }
            if (Eq(model.TopologyKind, "EXPLICIT")) return graph;
            int k = Math.Max(0, Math.Min(n - 1, model.MeanDegree)); if ((k & 1) == 1) k--;
            if (Eq(model.TopologyKind, "COMPLETE")) { for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) if (i != j) AddArc(graph, i, j, 1); }
            else if (Eq(model.TopologyKind, "ERDOS_RENYI") || Eq(model.TopologyKind, "RANDOM")) { double p = Clamp(model.LinkProbability, 0, 1); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (random.NextDouble() < p) { AddArc(graph, i, j, 1); AddArc(graph, j, i, 1); } }
            else if (Eq(model.TopologyKind, "SCALE_FREE"))
            {
                int m = Math.Max(1, Math.Min(16, Math.Max(1, k / 2))); int seedNodes = Math.Min(n, m + 1); for (int i = 0; i < seedNodes; i++) for (int j = i + 1; j < seedNodes; j++) { AddArc(graph, i, j, 1); AddArc(graph, j, i, 1); }
                for (int node = seedNodes; node < n; node++) { HashSet<int> chosen = new HashSet<int>(); int guard = 0; while (chosen.Count < Math.Min(m, node) && guard++ < node * 32) { double total = 0; for (int i = 0; i < node; i++) total += graph.Neighbors[i].Count + 1; double draw = random.NextDouble() * total, cumulative = 0; int pick = node - 1; for (int i = 0; i < node; i++) { cumulative += graph.Neighbors[i].Count + 1; if (draw <= cumulative) { pick = i; break; } } chosen.Add(pick); } foreach (int target in chosen) { AddArc(graph, node, target, 1); AddArc(graph, target, node, 1); } }
            }
            else
            {
                for (int i = 0; i < n; i++) for (int d = 1; d <= Math.Max(1, k / 2); d++) { int j = (i + d) % n; AddArc(graph, i, j, 1); AddArc(graph, j, i, 1); }
                if (Eq(model.TopologyKind, "SMALL_WORLD")) { double beta = Clamp(model.RewireProbability, 0, 1); for (int i = 0; i < n; i++) for (int e = graph.Neighbors[i].Count - 1; e >= 0; e--) if (graph.Neighbors[i][e] > i && random.NextDouble() < beta) { int old = graph.Neighbors[i][e], replacement = random.Next(n), guard = 0; while ((replacement == i || graph.Neighbors[i].Contains(replacement)) && guard++ < n * 2) replacement = random.Next(n); if (replacement != i && !graph.Neighbors[i].Contains(replacement)) { RemoveArc(graph, i, old); RemoveArc(graph, old, i); AddArc(graph, i, replacement, 1); AddArc(graph, replacement, i, 1); } } }
                if (Eq(model.TopologyKind, "COMMUNITY")) { double inside = Math.Max(model.LinkProbability, 0.1), outside = Math.Min(inside, Math.Max(0.001, model.LinkProbability * model.RewireProbability)); for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (!graph.Neighbors[i].Contains(j) && random.NextDouble() < (graph.Community[i] == graph.Community[j] ? inside : outside)) { AddArc(graph, i, j, 1); AddArc(graph, j, i, 1); } }
            }
            return graph;
        }

        private static int Advance(ComplexSystemModel model, List<RuntimeAgent> agents, RuntimeGraph graph, StableRandom random)
        {
            int n = agents.Count, changed = 0; RuntimeAgent[] previous = agents.Select(CloneAgent).ToArray(); int[] order = Enumerable.Range(0, n).ToArray(); if (Eq(model.UpdateMode, "ASYNCHRONOUS")) for (int i = n - 1; i > 0; i--) { int j = random.Next(i + 1), t = order[i]; order[i] = order[j]; order[j] = t; }
            foreach (int i in order)
            {
                RuntimeAgent source = Eq(model.UpdateMode, "ASYNCHRONOUS") ? agents[i] : previous[i]; RuntimeAgent next = CloneAgent(source); List<int> neighbors = graph.Neighbors[i]; int active = 0, alive = 0; double weight = 0, activeWeight = 0, neighborMean = 0, localSpin = 0;
                for (int q = 0; q < neighbors.Count; q++) { int j = neighbors[q]; RuntimeAgent other = Eq(model.UpdateMode, "ASYNCHRONOUS") ? agents[j] : previous[j]; double w = graph.Weights[i][q]; if (!other.Removed) alive++; if (other.Mode == 1) { active++; activeWeight += Math.Abs(w); } weight += Math.Abs(w); neighborMean += w * other.Value; localSpin += w * (other.Mode == 1 ? 1 : -1); }
                double activeFraction = weight <= Epsilon ? 0 : activeWeight / weight; if (Math.Abs(weight) > Epsilon) neighborMean /= weight;
                string dynamics = model.DynamicsKind ?? "THRESHOLD_CONTAGION";
                if (Eq(dynamics, "THRESHOLD_CONTAGION"))
                {
                    if (source.Mode == 0) { double spontaneous = Clamp(Parameter(model, "spontaneous_rate", 0), 0, 1), probability = Clamp(spontaneous + Parameter(model, "transmission", 0.2) * source.Susceptibility * activeFraction, 0, 1); if (activeFraction >= source.Threshold || random.NextDouble() < probability) { next.Mode = 1; next.Value = 1; } }
                    else if (source.Mode == 1 && random.NextDouble() < Clamp(Parameter(model, "recovery", 0.02), 0, 1)) { next.Mode = 0; next.Value = 0; }
                }
                else if (Eq(dynamics, "SIR"))
                {
                    if (source.Mode == 0 && random.NextDouble() < 1 - Math.Pow(1 - Clamp(Parameter(model, "transmission", 0.15), 0, 1), active)) { next.Mode = 1; next.Value = 1; }
                    else if (source.Mode == 1 && random.NextDouble() < Clamp(Parameter(model, "recovery", 0.05), 0, 1)) { next.Mode = 2; next.Value = 0; next.Removed = true; }
                }
                else if (Eq(dynamics, "BOUNDED_CONFIDENCE"))
                {
                    double epsilon = Math.Max(0, Parameter(model, "confidence_bound", 0.3)), rate = Clamp(Parameter(model, "adaptation_rate", 0.25), 0, 1); if (neighbors.Count > 0 && Math.Abs(neighborMean - source.Value) <= epsilon) next.Value = Clamp(source.Value + rate * (neighborMean - source.Value) + source.Noise * random.Normal(), -1, 1); next.Mode = next.Value >= 0 ? 1 : 0;
                }
                else if (Eq(dynamics, "VOTER"))
                {
                    if (neighbors.Count > 0) { RuntimeAgent exemplar = Eq(model.UpdateMode, "ASYNCHRONOUS") ? agents[neighbors[random.Next(neighbors.Count)]] : previous[neighbors[random.Next(neighbors.Count)]]; if (random.NextDouble() >= Clamp(Parameter(model, "mutation", 0), 0, 1)) { next.Mode = exemplar.Mode; next.Value = exemplar.Value; } else { next.Mode = source.Mode == 1 ? 0 : 1; next.Value = next.Mode; } }
                }
                else if (Eq(dynamics, "ISING"))
                {
                    double temperature = Math.Max(Epsilon, Parameter(model, "temperature", 1)), coupling = Parameter(model, "coupling", 1), field = Parameter(model, "external_field", 0), energy = 2 * (source.Mode == 1 ? 1 : -1) * (coupling * localSpin + field), flip = 1 / (1 + Math.Exp(Clamp(energy / temperature, -60, 60))); if (random.NextDouble() < flip) next.Mode = source.Mode == 1 ? 0 : 1; next.Value = next.Mode == 1 ? 1 : -1;
                }
                else if (Eq(dynamics, "EVOLUTIONARY_GAME"))
                {
                    if (neighbors.Count > 0) { int j = neighbors[random.Next(neighbors.Count)]; RuntimeAgent other = Eq(model.UpdateMode, "ASYNCHRONOUS") ? agents[j] : previous[j]; double own = Payoff(i, source.Mode, previous, graph, model), theirs = Payoff(j, other.Mode, previous, graph, model), beta = Math.Max(0, Parameter(model, "selection_intensity", 1)); double imitate = 1 / (1 + Math.Exp(Clamp(-beta * (theirs - own), -60, 60))); if (random.NextDouble() < imitate) next.Mode = other.Mode; if (random.NextDouble() < Clamp(Parameter(model, "mutation", 0.001), 0, 1)) next.Mode = next.Mode == 1 ? 0 : 1; next.Value = next.Mode; }
                }
                else if (Eq(dynamics, "CASCADE_FAILURE"))
                {
                    double incoming = 0; for (int q = 0; q < neighbors.Count; q++) { RuntimeAgent other = previous[neighbors[q]]; if (other.Removed) incoming += Math.Max(0, other.Load) / Math.Max(1, graph.Neighbors[neighbors[q]].Count); } next.Load = Math.Max(0, source.Load + Parameter(model, "load_growth", 0) + Parameter(model, "redistribution", 1) * incoming); if (next.Load > next.Capacity) { next.Mode = 2; next.Removed = true; next.Value = 0; } else { next.Mode = 1; next.Value = Clamp(1 - next.Load / next.Capacity, 0, 1); }
                }
                else if (Eq(dynamics, "COUPLED_MAP"))
                {
                    double r = Parameter(model, "map_rate", 3.7), coupling = Clamp(Parameter(model, "coupling", 0.1), 0, 1), local = Clamp(r * source.Value * (1 - source.Value), 0, 1), neighborhood = neighbors.Count == 0 ? local : Clamp(r * neighborMean * (1 - neighborMean), 0, 1); next.Value = Clamp((1 - coupling) * local + coupling * neighborhood + source.Noise * random.Normal(), 0, 1); next.Mode = next.Value >= source.Threshold ? 1 : 0;
                }
                else if (Eq(dynamics, "CELLULAR_AUTOMATON"))
                {
                    int birthMask = (int)Parameter(model, "birth_mask", 8), survivalMask = (int)Parameter(model, "survival_mask", 12); bool aliveNow = source.Mode == 1, aliveNext = aliveNow ? ((survivalMask & (1 << Math.Min(30, active))) != 0) : ((birthMask & (1 << Math.Min(30, active))) != 0); next.Mode = aliveNext ? 1 : 0; next.Value = next.Mode;
                }
                ApplyGenericRules(model, source, next, activeFraction, random);
                if (Different(source, next)) changed++; agents[i] = next;
            }
            return changed;
        }

        private static void ApplyGenericRules(ComplexSystemModel model, RuntimeAgent source, RuntimeAgent next, double activeFraction, StableRandom random)
        {
            foreach (ComplexRuleSpec rule in (model.Rules ?? new List<ComplexRuleSpec>()).Where(x => x != null && x.Enabled))
            {
                if (!String.IsNullOrWhiteSpace(rule.StateFrom) && rule.StateFrom != "*" && ParseMode(rule.StateFrom) != source.Mode) continue; if (activeFraction + Epsilon < rule.MinimumNeighborFraction || random.NextDouble() > Clamp(rule.Probability, 0, 1)) continue;
                double parameter = String.IsNullOrWhiteSpace(rule.ParameterKey) ? 1 : Parameter(model, rule.ParameterKey, 1); if (Eq(rule.Kind, "STATE_TRANSITION")) { next.Mode = ParseMode(rule.StateTo); next.Value = next.Mode; next.Removed = next.Mode == 2; } else if (Eq(rule.Kind, "VALUE_DELTA")) next.Value = FiniteOrZero(next.Value + rule.ScalarDelta * parameter + rule.Coupling * activeFraction); else if (Eq(rule.Kind, "FAILURE") && next.Load / Math.Max(Epsilon, next.Capacity) >= rule.Threshold) { next.Mode = 2; next.Removed = true; } else if (Eq(rule.Kind, "RECOVERY") && next.Removed) { next.Removed = false; next.Mode = ParseMode(rule.StateTo); next.Load = Math.Max(0, next.Load - Math.Abs(rule.ScalarDelta * parameter)); }
            }
        }

        private static void ApplyInterventions(ComplexSystemModel model, List<RuntimeAgent> agents, RuntimeGraph graph, int step, StableRandom random)
        {
            foreach (ComplexInterventionSpec intervention in (model.Interventions ?? new List<ComplexInterventionSpec>()).Where(x => x != null && x.Enabled && step >= x.Step && step < x.Step + Math.Max(1, x.Duration)))
            {
                List<int> candidates = Enumerable.Range(0, agents.Count).Where(i => Eq(intervention.TargetKind, "ALL") || Eq(agents[i].Archetype, intervention.TargetValue) || Eq(agents[i].Id, intervention.TargetValue)).ToList(); int count = Math.Max(0, Math.Min(candidates.Count, (int)Math.Ceiling(candidates.Count * Clamp(intervention.Fraction, 0, 1))));
                for (int i = candidates.Count - 1; i > 0; i--) { int j = random.Next(i + 1), t = candidates[i]; candidates[i] = candidates[j]; candidates[j] = t; }
                foreach (int i in candidates.Take(count)) { RuntimeAgent a = agents[i]; if (Eq(intervention.Kind, "SEED_STATE")) { a.Mode = ParseMode(intervention.TargetValue); a.Value = intervention.Magnitude; a.Removed = a.Mode == 2; } else if (Eq(intervention.Kind, "SHOCK_VALUE")) a.Value = FiniteOrZero(a.Value + intervention.Magnitude); else if (Eq(intervention.Kind, "ADD_LOAD")) a.Load = Math.Max(0, a.Load + intervention.Magnitude); else if (Eq(intervention.Kind, "REMOVE_NODE")) { a.Mode = 2; a.Removed = true; } else if (Eq(intervention.Kind, "CAPACITY_MULTIPLIER")) a.Capacity = Math.Max(Epsilon, a.Capacity * intervention.Magnitude); }
            }
        }

        private static ComplexMacrostate Measure(ComplexSystemModel model, int step, List<RuntimeAgent> agents, RuntimeGraph graph, int changed)
        {
            int n = agents.Count, active = agents.Count(x => x.Mode == 1 && !x.Removed), removed = agents.Count(x => x.Removed || x.Mode == 2); List<double> values = agents.Select(x => x.Value).ToList(); double mean = Mean(values), variance = Variance(values), positive = Mean(values.Where(x => x >= 0)), negative = Mean(values.Where(x => x < 0)); double polarization = values.Any(x => x >= 0) && values.Any(x => x < 0) ? Math.Abs(positive - negative) : 0; Dictionary<int, int> modes = agents.GroupBy(x => x.Mode).ToDictionary(x => x.Key, x => x.Count()); double entropy = 0; foreach (int count in modes.Values) { double p = count / (double)Math.Max(1, n); if (p > 0) entropy -= p * Math.Log(p, 2); }
            double order = Math.Abs(agents.Sum(x => x.Mode == 1 ? 1.0 : (x.Mode == 0 ? -1.0 : 0)) / Math.Max(1, n)), gini = Gini(values.Select(Math.Abs).ToList()), loadRatio = Mean(agents.Select(x => x.Load / Math.Max(Epsilon, x.Capacity))), targetActivity = Clamp(Parameter(model, "performance_target_activity", 0), 0, 1), performance = Clamp(1 - Math.Abs(active / (double)Math.Max(1, n) - targetActivity), 0, 1) * Clamp(1 - removed / (double)Math.Max(1, n), 0, 1); if (Eq(model.DynamicsKind, "CASCADE_FAILURE")) performance *= Clamp(1 - loadRatio, 0, 1);
            return new ComplexMacrostate { Step = step, ActiveFraction = active / (double)Math.Max(1, n), RemovedFraction = removed / (double)Math.Max(1, n), MeanValue = mean, Variance = variance, OrderParameter = order, Polarization = polarization, StateEntropy = entropy, ValueGini = gini, MeanLoadRatio = loadRatio, Performance = performance, ChangedAgents = changed, LargestActiveComponent = LargestComponent(graph, agents, true), StateHash = StateHash(agents) };
        }

        private static ComplexNetworkMetrics NetworkMetrics(RuntimeGraph graph, List<RuntimeAgent> agents)
        {
            int n = agents.Count, arcs = graph.Neighbors.Sum(x => x.Count); List<double> degrees = graph.Neighbors.Select(x => (double)x.Count).ToList(); List<int> distances = new List<int>(); int diameter = 0, components = 0, isolates = graph.Neighbors.Count(x => x.Count == 0), largest = 0; bool[] visited = new bool[n];
            for (int root = 0; root < n; root++) if (!visited[root]) { components++; Queue<int> q = new Queue<int>(); q.Enqueue(root); visited[root] = true; int size = 0; while (q.Count > 0) { int u = q.Dequeue(); size++; foreach (int v in graph.Neighbors[u]) if (!visited[v]) { visited[v] = true; q.Enqueue(v); } } largest = Math.Max(largest, size); }
            int samples = Math.Min(n, 128); for (int s = 0; s < samples; s++) { int source = samples == n ? s : (int)((long)s * n / samples); int[] d = Enumerable.Repeat(-1, n).ToArray(); Queue<int> q = new Queue<int>(); d[source] = 0; q.Enqueue(source); while (q.Count > 0) { int u = q.Dequeue(); foreach (int v in graph.Neighbors[u]) if (d[v] < 0) { d[v] = d[u] + 1; q.Enqueue(v); } } for (int i = 0; i < n; i++) if (i != source && d[i] >= 0) { distances.Add(d[i]); diameter = Math.Max(diameter, d[i]); } }
            double triangles = 0, triples = 0; for (int i = 0; i < n; i++) { HashSet<int> set = new HashSet<int>(graph.Neighbors[i]); int k = set.Count; triples += k * (k - 1) / 2.0; int closed = 0; int[] list = set.ToArray(); for (int a = 0; a < list.Length; a++) for (int b = a + 1; b < list.Length; b++) if (graph.Neighbors[list[a]].Contains(list[b])) closed++; triangles += closed; }
            List<double> edgeX = new List<double>(), edgeY = new List<double>(); for (int i = 0; i < n; i++) foreach (int j in graph.Neighbors[i]) { edgeX.Add(graph.Neighbors[i].Count); edgeY.Add(graph.Neighbors[j].Count); }
            double assortativity = Correlation(edgeX, edgeY), spectral = SpectralRadius(graph), modularity = 0; if (arcs > 0) { double m = arcs; for (int i = 0; i < n; i++) foreach (int j in graph.Neighbors[i]) if (graph.Community[i] == graph.Community[j]) modularity += 1 - graph.Neighbors[i].Count * graph.Neighbors[j].Count / m; modularity /= m; }
            return new ComplexNetworkMetrics { Nodes = n, DirectedArcs = arcs, Components = components, LargestComponent = largest, Isolates = isolates, MeanDegree = Mean(degrees), DegreeVariance = Variance(degrees), Density = n <= 1 ? 0 : arcs / (double)(n * (n - 1)), Clustering = triples <= Epsilon ? 0 : triangles / triples, MeanPathLength = Mean(distances.Select(x => (double)x)), Diameter = diameter, DegreeAssortativity = assortativity, SpectralRadius = spectral, CommunityModularityProxy = modularity };
        }

        private static void PercolationMeasure(RuntimeGraph graph, int n, HashSet<int> removed, out double giantFraction, out double componentCount)
        {
            bool[] seen = new bool[n]; int largest = 0, components = 0, remaining = n - (removed == null ? 0 : removed.Count); for (int root = 0; root < n; root++) { if (seen[root] || (removed != null && removed.Contains(root))) continue; components++; int size = 0; Queue<int> queue = new Queue<int>(); queue.Enqueue(root); seen[root] = true; while (queue.Count > 0) { int u = queue.Dequeue(); size++; foreach (int v in graph.Neighbors[u]) if (!seen[v] && (removed == null || !removed.Contains(v))) { seen[v] = true; queue.Enqueue(v); } } largest = Math.Max(largest, size); } giantFraction = remaining <= 0 ? 0 : largest / (double)n; componentCount = components;
        }

        private static double SpectralRadius(RuntimeGraph graph)
        {
            int n = graph.Neighbors.Length; if (n == 0) return 0; double[] x = Enumerable.Repeat(1.0 / Math.Sqrt(n), n).ToArray(); double lambda = 0;
            for (int iteration = 0; iteration < 256; iteration++) { double[] y = new double[n]; for (int i = 0; i < n; i++) for (int q = 0; q < graph.Neighbors[i].Count; q++) y[i] += Math.Abs(graph.Weights[i][q]) * x[graph.Neighbors[i][q]]; double norm = Math.Sqrt(y.Sum(v => v * v)); if (norm <= Epsilon) return 0; for (int i = 0; i < n; i++) y[i] /= norm; double next = 0; for (int i = 0; i < n; i++) { double ax = 0; for (int q = 0; q < graph.Neighbors[i].Count; q++) ax += Math.Abs(graph.Weights[i][q]) * y[graph.Neighbors[i][q]]; next += y[i] * ax; } if (Math.Abs(next - lambda) < 1e-10) { lambda = next; break; } lambda = next; x = y; }
            return Math.Abs(lambda);
        }

        private static double Payoff(int index, int strategy, RuntimeAgent[] agents, RuntimeGraph graph, ComplexSystemModel model)
        {
            double r = Parameter(model, "payoff_reward", 1), s = Parameter(model, "payoff_sucker", 0), t = Parameter(model, "payoff_temptation", 1.5), p = Parameter(model, "payoff_punishment", 0.1), total = 0; List<int> neighbors = graph.Neighbors[index]; if (neighbors.Count == 0) return 0; foreach (int j in neighbors) { int other = agents[j].Mode; total += strategy == 1 ? (other == 1 ? r : s) : (other == 1 ? t : p); } return total / neighbors.Count;
        }

        private static int LargestComponent(RuntimeGraph graph, List<RuntimeAgent> agents, bool activeOnly)
        {
            int n = agents.Count, largest = 0; bool[] seen = new bool[n]; for (int root = 0; root < n; root++) { if (seen[root] || (activeOnly && agents[root].Mode != 1)) continue; int size = 0; Queue<int> q = new Queue<int>(); q.Enqueue(root); seen[root] = true; while (q.Count > 0) { int u = q.Dequeue(); size++; foreach (int v in graph.Neighbors[u]) if (!seen[v] && (!activeOnly || agents[v].Mode == 1)) { seen[v] = true; q.Enqueue(v); } } largest = Math.Max(largest, size); } return largest;
        }

        private static string StateHash(List<RuntimeAgent> agents) { StringBuilder s = new StringBuilder(); foreach (RuntimeAgent a in agents) s.Append(a.Mode).Append(':').Append(Math.Round(a.Value, 9).ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(Math.Round(a.Load, 9).ToString("R", CultureInfo.InvariantCulture)).Append('|'); return HashText(s.ToString()); }
        private static RuntimeAgent CloneAgent(RuntimeAgent a) { return new RuntimeAgent { Id = a.Id, Archetype = a.Archetype, Mode = a.Mode, Value = a.Value, Threshold = a.Threshold, Susceptibility = a.Susceptibility, Capacity = a.Capacity, Load = a.Load, Noise = a.Noise, Removed = a.Removed }; }
        private static bool Different(RuntimeAgent a, RuntimeAgent b) { return a.Mode != b.Mode || a.Removed != b.Removed || Math.Abs(a.Value - b.Value) > 1e-12 || Math.Abs(a.Load - b.Load) > 1e-12 || Math.Abs(a.Capacity - b.Capacity) > 1e-12; }
        private static void AddArc(RuntimeGraph graph, int source, int target, double weight) { if (source == target || graph.Neighbors[source].Contains(target)) return; graph.Neighbors[source].Add(target); graph.Weights[source].Add(FiniteOrZero(weight)); }
        private static void RemoveArc(RuntimeGraph graph, int source, int target) { int at = graph.Neighbors[source].IndexOf(target); if (at < 0) return; graph.Neighbors[source].RemoveAt(at); graph.Weights[source].RemoveAt(at); }
        private static int ParseMode(string value) { int mode; if (Int32.TryParse(value ?? "", NumberStyles.Integer, CultureInfo.InvariantCulture, out mode)) return mode; if (Eq(value, "ACTIVE") || Eq(value, "INFECTED") || Eq(value, "COOPERATE") || Eq(value, "ON") || Eq(value, "ALIVE")) return 1; if (Eq(value, "REMOVED") || Eq(value, "FAILED") || Eq(value, "RECOVERED")) return 2; return 0; }
        private static double Parameter(ComplexSystemModel model, string key, double fallback) { double value; return model != null && model.Parameters != null && model.Parameters.TryGetValue(key ?? "", out value) && Finite(value) ? value : fallback; }
        private static double Parameter(ComplexSystemModel model, string key, int fallback) { return Parameter(model, key, (double)fallback); }
        private static bool KnownTopology(string value) { return new[] { "RING", "LATTICE", "SMALL_WORLD", "ERDOS_RENYI", "RANDOM", "SCALE_FREE", "COMMUNITY", "COMPLETE", "EXPLICIT" }.Any(x => Eq(x, value)); }
        private static bool KnownDynamics(string value) { return new[] { "THRESHOLD_CONTAGION", "SIR", "BOUNDED_CONFIDENCE", "VOTER", "ISING", "EVOLUTIONARY_GAME", "CASCADE_FAILURE", "COUPLED_MAP", "CELLULAR_AUTOMATON", "GENERIC_RULES" }.Any(x => Eq(x, value)); }
        private static void DuplicateFindings(List<string> findings, IEnumerable<string> ids, string kind) { foreach (IGrouping<string, string> g in (ids ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.OrdinalIgnoreCase)) if (g.Count() > 1) findings.Add("BLOCKER · DUPLICATE_" + kind + "_ID · " + g.Key); }
        private static void AppendEvidence(StringBuilder s, IEnumerable<string> evidence) { foreach (string id in (evidence ?? Enumerable.Empty<string>()).OrderBy(x => x)) s.Append(":EVIDENCE:").Append(id); }
        private static double TailSlope(List<int> values) { List<double> sorted = (values ?? new List<int>()).Where(x => x > 0).Distinct().OrderBy(x => x).Select(x => (double)x).ToList(); if (sorted.Count < 3) return 0; List<double> xs = new List<double>(), ys = new List<double>(); foreach (double x in sorted) { int tail = values.Count(v => v >= x); if (tail > 0) { xs.Add(Math.Log(x)); ys.Add(Math.Log(tail / (double)values.Count)); } } return RegressionSlope(xs, ys); }
        private static List<int> Symbolize(IList<double> values, int bins) { List<int> result = new List<int>(); if (values == null || values.Count == 0) return result; bins = Math.Max(2, Math.Min(32, bins)); double minimum = values.Min(), maximum = values.Max(), width = Math.Max(Epsilon, maximum - minimum); foreach (double value in values) result.Add(Math.Min(bins - 1, Math.Max(0, (int)Math.Floor((value - minimum) / width * bins)))); return result; }
        private static double DiscreteEntropy(IList<int> values) { if (values == null || values.Count == 0) return 0; double entropy = 0; foreach (IGrouping<int, int> group in values.GroupBy(x => x)) { double p = group.Count() / (double)values.Count; entropy -= p * Math.Log(p, 2); } return entropy; }
        private static double TupleEntropy(IList<int> values, int length) { if (values == null || values.Count < length || length < 1) return 0; Dictionary<string, int> counts = new Dictionary<string, int>(); int total = values.Count - length + 1; for (int i = 0; i < total; i++) { string key = String.Join(",", values.Skip(i).Take(length).ToArray()); int count; counts[key] = counts.TryGetValue(key, out count) ? count + 1 : 1; } double entropy = 0; foreach (int count in counts.Values) { double p = count / (double)total; entropy -= p * Math.Log(p, 2); } return entropy; }
        private static double DiscreteMutualInformation(IList<int> values, int lag) { if (values == null || lag < 1 || values.Count <= lag) return 0; Dictionary<int, int> left = new Dictionary<int, int>(), right = new Dictionary<int, int>(); Dictionary<string, int> joint = new Dictionary<string, int>(); int total = values.Count - lag; for (int i = lag; i < values.Count; i++) { int a = values[i - lag], b = values[i], count; left[a] = left.TryGetValue(a, out count) ? count + 1 : 1; right[b] = right.TryGetValue(b, out count) ? count + 1 : 1; string key = a + ":" + b; joint[key] = joint.TryGetValue(key, out count) ? count + 1 : 1; } double information = 0; foreach (KeyValuePair<string, int> pair in joint) { string[] parts = pair.Key.Split(':'); int a = Int32.Parse(parts[0], CultureInfo.InvariantCulture), b = Int32.Parse(parts[1], CultureInfo.InvariantCulture); double p = pair.Value / (double)total, pa = left[a] / (double)total, pb = right[b] / (double)total; information += p * Math.Log(p / (pa * pb), 2); } return Math.Max(0, information); }
        private static double PermutationEntropy(IList<double> values, int dimension) { if (values == null || values.Count < dimension || dimension < 2) return 0; Dictionary<string, int> patterns = new Dictionary<string, int>(); int total = values.Count - dimension + 1; for (int i = 0; i < total; i++) { string key = String.Join(",", Enumerable.Range(0, dimension).OrderBy(j => values[i + j]).ThenBy(j => j).ToArray()); int count; patterns[key] = patterns.TryGetValue(key, out count) ? count + 1 : 1; } double entropy = 0; foreach (int count in patterns.Values) { double p = count / (double)total; entropy -= p * Math.Log(p, 2); } double maximum = Math.Log(Factorial(dimension), 2); return maximum <= Epsilon ? 0 : entropy / maximum; }
        private static double LempelZivComplexity(IList<int> values) { if (values == null || values.Count == 0) return 0; HashSet<string> dictionary = new HashSet<string>(); int at = 0, phrases = 0; while (at < values.Count) { int length = 1; while (at + length <= values.Count && dictionary.Contains(String.Join(",", values.Skip(at).Take(length).ToArray()))) length++; dictionary.Add(String.Join(",", values.Skip(at).Take(Math.Min(length, values.Count - at)).ToArray())); phrases++; at += Math.Max(1, Math.Min(length, values.Count - at)); } return phrases * Math.Log(Math.Max(2, values.Count), 2) / Math.Max(1, values.Count); }
        private static int Factorial(int value) { int result = 1; for (int i = 2; i <= Math.Min(10, value); i++) result *= i; return result; }
        private static double Autocorrelation(IList<double> values, int lag) { if (values == null || values.Count <= lag) return 0; double mean = values.Average(), numerator = 0, denominator = 0; for (int i = lag; i < values.Count; i++) numerator += (values[i] - mean) * (values[i - lag] - mean); for (int i = 0; i < values.Count; i++) denominator += (values[i] - mean) * (values[i] - mean); return denominator <= Epsilon ? 0 : numerator / denominator; }
        private static double RegressionSlope(IList<double> x, IList<double> y) { int n = Math.Min(x == null ? 0 : x.Count, y == null ? 0 : y.Count); if (n < 2) return 0; double mx = x.Take(n).Average(), my = y.Take(n).Average(), numerator = 0, denominator = 0; for (int i = 0; i < n; i++) { numerator += (x[i] - mx) * (y[i] - my); denominator += (x[i] - mx) * (x[i] - mx); } return denominator <= Epsilon ? 0 : numerator / denominator; }
        private static double Correlation(IList<double> x, IList<double> y) { int n = Math.Min(x == null ? 0 : x.Count, y == null ? 0 : y.Count); if (n < 2) return 0; double mx = x.Take(n).Average(), my = y.Take(n).Average(), numerator = 0, xx = 0, yy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; numerator += a * b; xx += a * a; yy += b * b; } return xx <= Epsilon || yy <= Epsilon ? 0 : numerator / Math.Sqrt(xx * yy); }
        private static double Gini(List<double> values) { values = (values ?? new List<double>()).Where(Finite).Select(Math.Abs).OrderBy(x => x).ToList(); if (values.Count == 0 || values.Sum() <= Epsilon) return 0; double weighted = 0; for (int i = 0; i < values.Count; i++) weighted += (i + 1) * values[i]; return Clamp(2 * weighted / (values.Count * values.Sum()) - (values.Count + 1.0) / values.Count, 0, 1); }
        private static double Mean(IEnumerable<double> values) { List<double> list = (values ?? Enumerable.Empty<double>()).Where(Finite).ToList(); return list.Count == 0 ? 0 : list.Average(); }
        private static double Variance(IEnumerable<double> values) { List<double> list = (values ?? Enumerable.Empty<double>()).Where(Finite).ToList(); if (list.Count < 2) return 0; double mean = list.Average(); return list.Sum(x => (x - mean) * (x - mean)) / (list.Count - 1); }
        private static double StandardDeviation(IEnumerable<double> values) { return Math.Sqrt(Math.Max(0, Variance(values))); }
        private static double Quantile(IList<double> sorted, double q) { if (sorted == null || sorted.Count == 0) return 0; double at = Clamp(q, 0, 1) * (sorted.Count - 1); int lo = (int)Math.Floor(at), hi = (int)Math.Ceiling(at); return lo == hi ? sorted[lo] : sorted[lo] * (hi - at) + sorted[hi] * (at - lo); }
        private static bool InUnit(double value) { return Finite(value) && value >= 0 && value <= 1; }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static double FiniteOrZero(double value) { return Finite(value) ? value : 0; }
        private static double Clamp(double value, double minimum, double maximum) { if (maximum < minimum) { double t = minimum; minimum = maximum; maximum = t; } return Math.Max(minimum, Math.Min(maximum, FiniteOrZero(value))); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETIRED") && !Eq(status, "REJECTED"); }
        private static string HashText(string value) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

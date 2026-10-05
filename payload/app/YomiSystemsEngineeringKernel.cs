using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    // DEV13.37.16 pure systems-engineering kernel. It owns no WPF, filesystem,
    // process, network, clock, playback, scheduler, experiment, or deployment authority.
    internal sealed class EngineeringRequirement
    {
        public string Id { get; set; }
        public string ParentRequirementId { get; set; }
        public string Name { get; set; }
        public string Statement { get; set; }
        public string Kind { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string MetricKey { get; set; }
        public string Operator { get; set; }
        public string Unit { get; set; }
        public string VerificationMethod { get; set; }
        public double Threshold { get; set; }
        public double Tolerance { get; set; }
        public List<string> SatisfiedByComponentIds { get; set; }
        public List<string> DerivedFromRequirementIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringRequirement() { Kind = "FUNCTIONAL"; Priority = "SHALL"; Status = "DRAFT"; Operator = ">="; VerificationMethod = "ANALYSIS"; SatisfiedByComponentIds = new List<string>(); DerivedFromRequirementIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Priority ?? "SHALL").PadRight(7) + "   ·   " + (Kind ?? "REQUIREMENT").PadRight(12) + "   ·   " + (Name ?? Id ?? "REQUIREMENT") + (String.IsNullOrWhiteSpace(MetricKey) ? "" : "   ·   " + MetricKey + " " + (Operator ?? ">=") + " " + Threshold.ToString("0.######", CultureInfo.InvariantCulture) + " " + (Unit ?? "")); }
    }

    internal sealed class EngineeringComponent
    {
        public string Id { get; set; }
        public string ParentComponentId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public int TechnologyReadinessLevel { get; set; }
        public double Mass { get; set; }
        public double Power { get; set; }
        public double Compute { get; set; }
        public double Heat { get; set; }
        public double Capacity { get; set; }
        public double CostOptimistic { get; set; }
        public double CostMostLikely { get; set; }
        public double CostPessimistic { get; set; }
        public double FailureRatePerHour { get; set; }
        public double RepairRatePerHour { get; set; }
        public Dictionary<string, double> Attributes { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringComponent() { Kind = "SUBSYSTEM"; Status = "ACTIVE"; TechnologyReadinessLevel = 1; CostOptimistic = 0; CostMostLikely = 0; CostPessimistic = 0; Attributes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Kind ?? "COMPONENT") + "   ·   " + (Name ?? Id ?? "COMPONENT") + "   ·   TRL " + TechnologyReadinessLevel.ToString(CultureInfo.InvariantCulture) + "   ·   mass " + Mass.ToString("0.###", CultureInfo.InvariantCulture) + " · power " + Power.ToString("0.###", CultureInfo.InvariantCulture) + " · compute " + Compute.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringInterface
    {
        public string Id { get; set; }
        public string FromComponentId { get; set; }
        public string ToComponentId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Medium { get; set; }
        public string Protocol { get; set; }
        public string Contract { get; set; }
        public string Precondition { get; set; }
        public string Postcondition { get; set; }
        public double Capacity { get; set; }
        public double Latency { get; set; }
        public double Availability { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringInterface() { Kind = "DATA"; Status = "ACTIVE"; Availability = 1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Kind ?? "INTERFACE").PadRight(10) + "   ·   " + (Name ?? Id ?? "INTERFACE") + "   ·   " + (FromComponentId ?? "∅") + " → " + (ToComponentId ?? "∅") + "   ·   cap " + Capacity.ToString("0.###", CultureInfo.InvariantCulture) + " · latency " + Latency.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringResourceBudget
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double Available { get; set; }
        public double RequiredReserve { get; set; }
        public bool Hard { get; set; }
        public Dictionary<string, double> Allocations { get; set; }
        public EngineeringResourceBudget() { Status = "ACTIVE"; Hard = true; Allocations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return (Hard ? "HARD" : "SOFT").PadRight(5) + "   ·   " + (Name ?? Id ?? "BUDGET") + "   ·   available " + Available.ToString("0.###", CultureInfo.InvariantCulture) + " " + (Unit ?? "") + " · reserve " + RequiredReserve.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringTask
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ComponentId { get; set; }
        public string RequirementId { get; set; }
        public string Status { get; set; }
        public double OptimisticDuration { get; set; }
        public double MostLikelyDuration { get; set; }
        public double PessimisticDuration { get; set; }
        public double Cost { get; set; }
        public double ResourceDemand { get; set; }
        public List<string> PredecessorTaskIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringTask() { Status = "PLANNED"; OptimisticDuration = 1; MostLikelyDuration = 1; PessimisticDuration = 1; PredecessorTaskIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "PLANNED") + "   ·   " + (Name ?? Id ?? "TASK") + "   ·   PERT [" + OptimisticDuration.ToString("0.##", CultureInfo.InvariantCulture) + ", " + MostLikelyDuration.ToString("0.##", CultureInfo.InvariantCulture) + ", " + PessimisticDuration.ToString("0.##", CultureInfo.InvariantCulture) + "]   ·   predecessors " + PredecessorTaskIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringFailureMode
    {
        public string Id { get; set; }
        public string ComponentId { get; set; }
        public string Name { get; set; }
        public string Cause { get; set; }
        public string Effect { get; set; }
        public string Mitigation { get; set; }
        public string Status { get; set; }
        public int Severity { get; set; }
        public int Occurrence { get; set; }
        public int Detectability { get; set; }
        public double Probability { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringFailureMode() { Status = "OPEN"; Severity = 1; Occurrence = 1; Detectability = 1; EvidenceNodeIds = new List<string>(); }
        public int RiskPriorityNumber { get { return Math.Max(1, Severity) * Math.Max(1, Occurrence) * Math.Max(1, Detectability); } }
        public override string ToString() { return (Status ?? "OPEN") + "   ·   RPN " + RiskPriorityNumber.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   " + (Name ?? Id ?? "FAILURE MODE") + "   ·   P=" + Probability.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   " + (Effect ?? ""); }
    }

    internal sealed class EngineeringFaultGate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public int RequiredCount { get; set; }
        public List<string> InputIds { get; set; }
        public EngineeringFaultGate() { Kind = "OR"; Status = "ACTIVE"; RequiredCount = 1; InputIds = new List<string>(); }
        public override string ToString() { return (Kind ?? "OR") + (String.Equals(Kind, "KOFN", StringComparison.OrdinalIgnoreCase) ? "(" + RequiredCount + "/" + InputIds.Count + ")" : "") + "   ·   " + (Name ?? Id ?? "FAULT GATE") + "   ·   " + InputIds.Count.ToString(CultureInfo.InvariantCulture) + " inputs"; }
    }

    internal sealed class EngineeringCriterion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Direction { get; set; }
        public string Status { get; set; }
        public double Weight { get; set; }
        public double Threshold { get; set; }
        public bool Hard { get; set; }
        public override string ToString() { return (Direction ?? "MAXIMIZE").PadRight(8) + "   ·   w=" + Weight.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Name ?? Id ?? "CRITERION") + (Hard ? "   ·   HARD " + Threshold.ToString("0.###", CultureInfo.InvariantCulture) : ""); }
    }

    internal sealed class EngineeringAlternative
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
        public List<string> SelectedComponentIds { get; set; }
        public List<string> Assumptions { get; set; }
        public EngineeringAlternative() { Status = "CANDIDATE"; Metrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); SelectedComponentIds = new List<string>(); Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "CANDIDATE") + "   ·   " + (Name ?? Id ?? "ALTERNATIVE") + "   ·   " + Metrics.Count.ToString(CultureInfo.InvariantCulture) + " metrics / " + SelectedComponentIds.Count.ToString(CultureInfo.InvariantCulture) + " components"; }
    }

    internal sealed class EngineeringVerificationLink
    {
        public string Id { get; set; }
        public string RequirementId { get; set; }
        public string ArtifactKind { get; set; }
        public string ArtifactId { get; set; }
        public string Status { get; set; }
        public string Method { get; set; }
        public string CertificateHash { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringVerificationLink() { Status = "PLANNED"; Method = "ANALYSIS"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "PLANNED") + "   ·   " + (Method ?? "ANALYSIS") + "   ·   requirement " + (RequirementId ?? "∅") + " → " + (ArtifactKind ?? "ARTIFACT") + ":" + (ArtifactId ?? "∅"); }
    }

    internal sealed class EngineeringSafetyClaim
    {
        public string Id { get; set; }
        public string ParentClaimId { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Statement { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public List<string> VerificationLinkIds { get; set; }
        public EngineeringSafetyClaim() { Kind = "GOAL"; Status = "OPEN"; EvidenceNodeIds = new List<string>(); VerificationLinkIds = new List<string>(); }
        public override string ToString() { return (Status ?? "OPEN") + "   ·   " + (Kind ?? "GOAL").PadRight(12) + "   ·   " + (Statement ?? Id ?? "SAFETY CLAIM"); }
    }

    internal sealed class EngineeringArchitecture
    {
        public string Id { get; set; }
        public string ParentArchitectureId { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Name { get; set; }
        public string Charter { get; set; }
        public string Status { get; set; }
        public string LifecyclePhase { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public int Revision { get; set; }
        public double MissionHours { get; set; }
        public List<EngineeringRequirement> Requirements { get; set; }
        public List<EngineeringComponent> Components { get; set; }
        public List<EngineeringInterface> Interfaces { get; set; }
        public List<EngineeringResourceBudget> Budgets { get; set; }
        public List<EngineeringTask> Tasks { get; set; }
        public List<EngineeringFailureMode> FailureModes { get; set; }
        public List<EngineeringFaultGate> FaultGates { get; set; }
        public string TopFaultGateId { get; set; }
        public List<EngineeringCriterion> Criteria { get; set; }
        public List<EngineeringAlternative> Alternatives { get; set; }
        public List<EngineeringVerificationLink> VerificationLinks { get; set; }
        public List<EngineeringSafetyClaim> SafetyClaims { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EngineeringArchitecture() { Status = "CONCEPT"; LifecyclePhase = "CONCEPT"; Revision = 1; MissionHours = 8760; Requirements = new List<EngineeringRequirement>(); Components = new List<EngineeringComponent>(); Interfaces = new List<EngineeringInterface>(); Budgets = new List<EngineeringResourceBudget>(); Tasks = new List<EngineeringTask>(); FailureModes = new List<EngineeringFailureMode>(); FaultGates = new List<EngineeringFaultGate>(); Criteria = new List<EngineeringCriterion>(); Alternatives = new List<EngineeringAlternative>(); VerificationLinks = new List<EngineeringVerificationLink>(); SafetyClaims = new List<EngineeringSafetyClaim>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "CONCEPT") + "   ·   " + (Name ?? Id ?? "ARCHITECTURE") + "   ·   " + Components.Count.ToString(CultureInfo.InvariantCulture) + " components / " + Interfaces.Count.ToString(CultureInfo.InvariantCulture) + " interfaces / " + Requirements.Count.ToString(CultureInfo.InvariantCulture) + " requirements   ·   r" + Math.Max(1, Revision).ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringRequirementAnalysis
    {
        public int Requirements { get; set; }
        public int Allocated { get; set; }
        public int VerificationPlanned { get; set; }
        public int Verified { get; set; }
        public double AllocationCoverage { get; set; }
        public double VerificationCoverage { get; set; }
        public List<string> OrphanRequirementIds { get; set; }
        public List<string> OrphanVerificationLinkIds { get; set; }
        public List<string> Conflicts { get; set; }
        public List<string> CyclicDerivations { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringRequirementAnalysis() { OrphanRequirementIds = new List<string>(); OrphanVerificationLinkIds = new List<string>(); Conflicts = new List<string>(); CyclicDerivations = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "ALLOCATION " + AllocationCoverage.ToString("P1", CultureInfo.InvariantCulture) + "   ·   VERIFICATION " + VerificationCoverage.ToString("P1", CultureInfo.InvariantCulture) + "   ·   " + Conflicts.Count.ToString(CultureInfo.InvariantCulture) + " conflicts   ·   " + CyclicDerivations.Count.ToString(CultureInfo.InvariantCulture) + " derivation cycles"; }
    }

    internal sealed class EngineeringTopologyAnalysis
    {
        public int Components { get; set; }
        public int Interfaces { get; set; }
        public int StrongComponents { get; set; }
        public int CyclicComponents { get; set; }
        public double Density { get; set; }
        public double ModularityProxy { get; set; }
        public List<List<string>> ComponentsByScc { get; set; }
        public List<string> CondensationOrder { get; set; }
        public List<EngineeringCentralityRow> Centrality { get; set; }
        public List<string> OrphanComponentIds { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringTopologyAnalysis() { ComponentsByScc = new List<List<string>>(); CondensationOrder = new List<string>(); Centrality = new List<EngineeringCentralityRow>(); OrphanComponentIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return Components.ToString(CultureInfo.InvariantCulture) + " components / " + Interfaces.ToString(CultureInfo.InvariantCulture) + " interfaces   ·   " + StrongComponents.ToString(CultureInfo.InvariantCulture) + " SCCs / " + CyclicComponents.ToString(CultureInfo.InvariantCulture) + " cyclic   ·   density " + Density.ToString("0.0000", CultureInfo.InvariantCulture) + "   ·   modularity≈" + ModularityProxy.ToString("0.0000", CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringCentralityRow
    {
        public string ComponentId { get; set; }
        public string ComponentName { get; set; }
        public double PageRank { get; set; }
        public double Betweenness { get; set; }
        public int ReachableImpact { get; set; }
        public int InDegree { get; set; }
        public int OutDegree { get; set; }
        public override string ToString() { return (ComponentName ?? ComponentId ?? "COMPONENT").PadRight(34) + "   ·   PR " + PageRank.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   BC " + Betweenness.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   impact " + ReachableImpact.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   in/out " + InDegree + "/" + OutDegree; }
    }

    internal sealed class EngineeringBudgetRow
    {
        public string BudgetId { get; set; }
        public string Name { get; set; }
        public double Available { get; set; }
        public double Allocated { get; set; }
        public double RequiredReserve { get; set; }
        public double Margin { get; set; }
        public double MarginFraction { get; set; }
        public bool Compliant { get; set; }
        public List<string> UnknownComponentIds { get; set; }
        public EngineeringBudgetRow() { UnknownComponentIds = new List<string>(); }
        public override string ToString() { return (Compliant ? "PASS" : "FAIL") + "   ·   " + (Name ?? BudgetId ?? "BUDGET").PadRight(28) + "   ·   available " + Available.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   allocated " + Allocated.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   margin " + Margin.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture).PadLeft(12) + " (" + MarginFraction.ToString("P1", CultureInfo.InvariantCulture) + ")"; }
    }

    internal sealed class EngineeringScheduleRow
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public double ExpectedDuration { get; set; }
        public double Variance { get; set; }
        public double EarliestStart { get; set; }
        public double EarliestFinish { get; set; }
        public double LatestStart { get; set; }
        public double LatestFinish { get; set; }
        public double Slack { get; set; }
        public bool Critical { get; set; }
        public override string ToString() { return (Critical ? "CRITICAL" : "        ") + "   ·   " + (TaskName ?? TaskId ?? "TASK").PadRight(34) + "   ·   ES " + EarliestStart.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(9) + " · EF " + EarliestFinish.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(9) + " · slack " + Slack.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(9); }
    }

    internal sealed class EngineeringScheduleAnalysis
    {
        public bool Acyclic { get; set; }
        public double ExpectedProjectDuration { get; set; }
        public double CriticalPathVariance { get; set; }
        public List<string> CriticalTaskIds { get; set; }
        public List<EngineeringScheduleRow> Rows { get; set; }
        public List<string> MissingPredecessorIds { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringScheduleAnalysis() { CriticalTaskIds = new List<string>(); Rows = new List<EngineeringScheduleRow>(); MissingPredecessorIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return (Acyclic ? "DAG" : "CYCLIC") + "   ·   expected duration " + ExpectedProjectDuration.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   critical variance " + CriticalPathVariance.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + CriticalTaskIds.Count.ToString(CultureInfo.InvariantCulture) + " critical tasks"; }
    }

    internal sealed class EngineeringReliabilityRow
    {
        public string ComponentId { get; set; }
        public string ComponentName { get; set; }
        public double Reliability { get; set; }
        public double Availability { get; set; }
        public double MtbfHours { get; set; }
        public double MttrHours { get; set; }
        public override string ToString() { return (ComponentName ?? ComponentId ?? "COMPONENT").PadRight(34) + "   ·   R(t) " + Reliability.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   A " + Availability.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   MTBF " + MtbfHours.ToString("0.###", CultureInfo.InvariantCulture) + "h   ·   MTTR " + MttrHours.ToString("0.###", CultureInfo.InvariantCulture) + "h"; }
    }

    internal sealed class EngineeringFaultTreeAnalysis
    {
        public string TopGateId { get; set; }
        public double TopEventProbability { get; set; }
        public List<List<string>> MinimalCutSets { get; set; }
        public List<string> Warnings { get; set; }
        public EngineeringFaultTreeAnalysis() { MinimalCutSets = new List<List<string>>(); Warnings = new List<string>(); }
        public override string ToString() { return "TOP EVENT P=" + TopEventProbability.ToString("0.000000000", CultureInfo.InvariantCulture) + "   ·   " + MinimalCutSets.Count.ToString(CultureInfo.InvariantCulture) + " minimal cut sets   ·   gate " + (TopGateId ?? "∅"); }
    }

    internal sealed class EngineeringReliabilityAnalysis
    {
        public double MissionHours { get; set; }
        public double SeriesReliability { get; set; }
        public double SeriesAvailability { get; set; }
        public List<EngineeringReliabilityRow> Components { get; set; }
        public List<EngineeringFailureMode> Fmea { get; set; }
        public EngineeringFaultTreeAnalysis FaultTree { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringReliabilityAnalysis() { Components = new List<EngineeringReliabilityRow>(); Fmea = new List<EngineeringFailureMode>(); Audit = new List<string>(); }
        public override string ToString() { return "MISSION " + MissionHours.ToString("0.###", CultureInfo.InvariantCulture) + "h   ·   SERIES R " + SeriesReliability.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   SERIES A " + SeriesAvailability.ToString("0.000000", CultureInfo.InvariantCulture) + (FaultTree == null ? "" : "   ·   TOP P " + FaultTree.TopEventProbability.ToString("0.000000", CultureInfo.InvariantCulture)); }
    }

    internal sealed class EngineeringFlowAnalysis
    {
        public string SourceComponentId { get; set; }
        public string SinkComponentId { get; set; }
        public double MaximumFlow { get; set; }
        public List<string> SourceSideComponentIds { get; set; }
        public List<string> SinkSideComponentIds { get; set; }
        public List<string> BottleneckInterfaceIds { get; set; }
        public Dictionary<string, double> InterfaceFlows { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringFlowAnalysis() { SourceSideComponentIds = new List<string>(); SinkSideComponentIds = new List<string>(); BottleneckInterfaceIds = new List<string>(); InterfaceFlows = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Audit = new List<string>(); }
        public override string ToString() { return "MAX FLOW " + MaximumFlow.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + (SourceComponentId ?? "∅") + " → " + (SinkComponentId ?? "∅") + "   ·   " + BottleneckInterfaceIds.Count.ToString(CultureInfo.InvariantCulture) + " min-cut interfaces"; }
    }

    internal sealed class EngineeringTradeEvaluation
    {
        public string AlternativeId { get; set; }
        public string AlternativeName { get; set; }
        public bool Feasible { get; set; }
        public bool ParetoEfficient { get; set; }
        public double WeightedUtility { get; set; }
        public double TopsisCloseness { get; set; }
        public double MaximumRegret { get; set; }
        public int OutrankingWins { get; set; }
        public List<string> Violations { get; set; }
        public Dictionary<string, double> NormalizedMetrics { get; set; }
        public EngineeringTradeEvaluation() { Feasible = true; Violations = new List<string>(); NormalizedMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return (ParetoEfficient ? "PARETO" : "      ") + "   ·   " + (Feasible ? "FEASIBLE" : "VIOLATES").PadRight(8) + "   ·   " + (AlternativeName ?? AlternativeId ?? "ALTERNATIVE") + "   ·   utility " + WeightedUtility.ToString("0.000000", CultureInfo.InvariantCulture) + " · TOPSIS " + TopsisCloseness.ToString("0.000000", CultureInfo.InvariantCulture) + " · regret≤" + MaximumRegret.ToString("0.000000", CultureInfo.InvariantCulture) + " · outranks " + OutrankingWins; }
    }

    internal sealed class EngineeringRiskEnvelope
    {
        public int Scenarios { get; set; }
        public double ScheduleP05 { get; set; }
        public double ScheduleP50 { get; set; }
        public double ScheduleP95 { get; set; }
        public double CostP05 { get; set; }
        public double CostP50 { get; set; }
        public double CostP95 { get; set; }
        public double JointUpperTailProbability { get; set; }
        public string Fingerprint { get; set; }
        public List<string> Audit { get; set; }
        public EngineeringRiskEnvelope() { Audit = new List<string>(); }
        public override string ToString() { return Scenarios.ToString(CultureInfo.InvariantCulture) + " scenarios   ·   schedule P05/P50/P95 " + ScheduleP05.ToString("0.###", CultureInfo.InvariantCulture) + "/" + ScheduleP50.ToString("0.###", CultureInfo.InvariantCulture) + "/" + ScheduleP95.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cost P05/P50/P95 " + CostP05.ToString("0.##", CultureInfo.InvariantCulture) + "/" + CostP50.ToString("0.##", CultureInfo.InvariantCulture) + "/" + CostP95.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EngineeringSafetyCaseAnalysis
    {
        public int Claims { get; set; }
        public int Goals { get; set; }
        public int Solutions { get; set; }
        public int SupportedGoals { get; set; }
        public double Coverage { get; set; }
        public bool Acyclic { get; set; }
        public List<string> UnsupportedGoalIds { get; set; }
        public List<string> OrphanClaimIds { get; set; }
        public List<string> Warnings { get; set; }
        public EngineeringSafetyCaseAnalysis() { UnsupportedGoalIds = new List<string>(); OrphanClaimIds = new List<string>(); Warnings = new List<string>(); }
        public override string ToString() { return (Acyclic ? "ACYCLIC" : "CYCLIC") + "   ·   safety coverage " + Coverage.ToString("P1", CultureInfo.InvariantCulture) + "   ·   " + SupportedGoals.ToString(CultureInfo.InvariantCulture) + "/" + Goals.ToString(CultureInfo.InvariantCulture) + " goals supported   ·   " + OrphanClaimIds.Count.ToString(CultureInfo.InvariantCulture) + " orphan claims"; }
    }

    internal static class SystemsEngineeringKernel
    {
        private const double Epsilon = 1e-12;

        public static EngineeringRequirementAnalysis AnalyzeRequirements(EngineeringArchitecture a)
        {
            EngineeringRequirementAnalysis r = new EngineeringRequirementAnalysis(); if (a == null) { r.Audit.Add("BLOCKER · NULL_ARCHITECTURE"); return r; } List<EngineeringRequirement> reqs = (a.Requirements ?? new List<EngineeringRequirement>()).Where(x => Active(x.Status)).ToList(); HashSet<string> componentIds = new HashSet<string>((a.Components ?? new List<EngineeringComponent>()).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); HashSet<string> reqIds = new HashSet<string>(reqs.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); r.Requirements = reqs.Count;
            foreach (EngineeringRequirement q in reqs) { q.SatisfiedByComponentIds = q.SatisfiedByComponentIds ?? new List<string>(); List<string> valid = q.SatisfiedByComponentIds.Where(componentIds.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); if (valid.Count > 0) r.Allocated++; else r.OrphanRequirementIds.Add(q.Id); foreach (string missing in q.SatisfiedByComponentIds.Where(x => !componentIds.Contains(x))) r.Audit.Add("MISSING COMPONENT · " + q.Id + " → " + missing); }
            foreach (EngineeringVerificationLink link in a.VerificationLinks ?? new List<EngineeringVerificationLink>()) { if (!reqIds.Contains(link.RequirementId ?? "")) { r.OrphanVerificationLinkIds.Add(link.Id); continue; } r.VerificationPlanned++; if (Eq(link.Status, "VERIFIED") || Eq(link.Status, "PASS") || Eq(link.Status, "ACCEPTED")) r.Verified++; }
            r.AllocationCoverage = r.Requirements == 0 ? 0 : (double)r.Allocated / r.Requirements; r.VerificationCoverage = r.Requirements == 0 ? 0 : (double)reqs.Count(q => (a.VerificationLinks ?? new List<EngineeringVerificationLink>()).Any(v => Eq(v.RequirementId, q.Id))) / r.Requirements;
            foreach (IGrouping<string, EngineeringRequirement> group in reqs.Where(x => !String.IsNullOrWhiteSpace(x.MetricKey)).GroupBy(x => x.MetricKey, StringComparer.OrdinalIgnoreCase)) { double lower = Double.NegativeInfinity, upper = Double.PositiveInfinity; EngineeringRequirement lowerR = null, upperR = null; foreach (EngineeringRequirement q in group) { if (q.Operator == ">=" || q.Operator == ">" || q.Operator == "==") if (q.Threshold > lower) { lower = q.Threshold; lowerR = q; } if (q.Operator == "<=" || q.Operator == "<" || q.Operator == "==") if (q.Threshold < upper) { upper = q.Threshold; upperR = q; } } if (lower > upper + Math.Max(lowerR == null ? 0 : lowerR.Tolerance, upperR == null ? 0 : upperR.Tolerance)) r.Conflicts.Add(group.Key + " · lower " + lower.ToString("R", CultureInfo.InvariantCulture) + " from " + (lowerR == null ? "∅" : lowerR.Id) + " exceeds upper " + upper.ToString("R", CultureInfo.InvariantCulture) + " from " + (upperR == null ? "∅" : upperR.Id)); }
            Dictionary<string, List<string>> graph = reqs.ToDictionary(x => x.Id, x => (x.DerivedFromRequirementIds ?? new List<string>()).Where(reqIds.Contains).ToList(), StringComparer.OrdinalIgnoreCase); foreach (List<string> scc in Tarjan(graph).Where(x => x.Count > 1 || (x.Count == 1 && graph[x[0]].Any(y => Eq(y, x[0]))))) r.CyclicDerivations.Add(String.Join(" ↔ ", scc.ToArray())); if (r.Requirements == 0) r.Audit.Add("BLOCKER · NO REQUIREMENTS"); if (r.AllocationCoverage < 1) r.Audit.Add("WARN · UNALLOCATED REQUIREMENTS"); if (r.VerificationCoverage < 1) r.Audit.Add("WARN · INCOMPLETE VERIFICATION CROSSWALK"); return r;
        }

        public static EngineeringTopologyAnalysis AnalyzeTopology(EngineeringArchitecture a)
        {
            EngineeringTopologyAnalysis result = new EngineeringTopologyAnalysis(); if (a == null) return result; List<EngineeringComponent> components = (a.Components ?? new List<EngineeringComponent>()).Where(x => Active(x.Status)).ToList(); HashSet<string> ids = new HashSet<string>(components.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); List<EngineeringInterface> edges = (a.Interfaces ?? new List<EngineeringInterface>()).Where(x => Active(x.Status) && ids.Contains(x.FromComponentId ?? "") && ids.Contains(x.ToComponentId ?? "")).ToList(); Dictionary<string, List<string>> graph = components.ToDictionary(x => x.Id, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (EngineeringInterface e in edges) if (!graph[e.FromComponentId].Contains(e.ToComponentId, StringComparer.OrdinalIgnoreCase)) graph[e.FromComponentId].Add(e.ToComponentId); result.Components = components.Count; result.Interfaces = edges.Count; result.Density = components.Count <= 1 ? 0 : (double)edges.Count / (components.Count * (components.Count - 1)); result.ComponentsByScc = Tarjan(graph); result.StrongComponents = result.ComponentsByScc.Count; result.CyclicComponents = result.ComponentsByScc.Count(x => x.Count > 1 || (x.Count == 1 && graph[x[0]].Any(y => Eq(y, x[0]))));
            Dictionary<string, int> sccBy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < result.ComponentsByScc.Count; i++) foreach (string id in result.ComponentsByScc[i]) sccBy[id] = i; int internalEdges = edges.Count(e => sccBy[e.FromComponentId] == sccBy[e.ToComponentId]); result.ModularityProxy = edges.Count == 0 ? 1 : (double)internalEdges / edges.Count; result.CondensationOrder = CondensationOrder(result.ComponentsByScc, graph, sccBy);
            Dictionary<string, double> pr = PageRank(graph); Dictionary<string, double> bc = Betweenness(graph); foreach (EngineeringComponent c in components) { int incoming = edges.Count(e => Eq(e.ToComponentId, c.Id)), outgoing = edges.Count(e => Eq(e.FromComponentId, c.Id)); int impact = Reachable(graph, c.Id).Count - 1; result.Centrality.Add(new EngineeringCentralityRow { ComponentId = c.Id, ComponentName = c.Name, PageRank = Val(pr, c.Id), Betweenness = Val(bc, c.Id), ReachableImpact = Math.Max(0, impact), InDegree = incoming, OutDegree = outgoing }); if (incoming + outgoing == 0) result.OrphanComponentIds.Add(c.Id); } result.Centrality = result.Centrality.OrderByDescending(x => x.PageRank + x.Betweenness).ThenByDescending(x => x.ReachableImpact).ToList(); if (result.CyclicComponents > 0) result.Audit.Add("Cyclic interface components require explicit feedback-control and initialization semantics."); if (result.OrphanComponentIds.Count > 0) result.Audit.Add("Orphan components are outside every active interface path."); return result;
        }

        public static List<EngineeringBudgetRow> AnalyzeBudgets(EngineeringArchitecture a)
        {
            List<EngineeringBudgetRow> rows = new List<EngineeringBudgetRow>(); if (a == null) return rows; HashSet<string> componentIds = new HashSet<string>((a.Components ?? new List<EngineeringComponent>()).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (EngineeringResourceBudget b in (a.Budgets ?? new List<EngineeringResourceBudget>()).Where(x => Active(x.Status))) { double allocated = (b.Allocations ?? new Dictionary<string, double>()).Where(x => componentIds.Contains(x.Key)).Sum(x => x.Value); double margin = b.Available - b.RequiredReserve - allocated; EngineeringBudgetRow row = new EngineeringBudgetRow { BudgetId = b.Id, Name = b.Name, Available = b.Available, Allocated = allocated, RequiredReserve = b.RequiredReserve, Margin = margin, MarginFraction = Math.Abs(b.Available) <= Epsilon ? 0 : margin / b.Available, Compliant = !b.Hard || margin >= -Epsilon }; row.UnknownComponentIds.AddRange((b.Allocations ?? new Dictionary<string, double>()).Keys.Where(x => !componentIds.Contains(x))); rows.Add(row); } return rows.OrderBy(x => x.Compliant ? 1 : 0).ThenBy(x => x.MarginFraction).ToList();
        }

        public static EngineeringScheduleAnalysis AnalyzeSchedule(EngineeringArchitecture a, IDictionary<string, double> durationOverride)
        {
            EngineeringScheduleAnalysis result = new EngineeringScheduleAnalysis(); if (a == null) return result; List<EngineeringTask> tasks = (a.Tasks ?? new List<EngineeringTask>()).Where(x => Active(x.Status)).ToList(); Dictionary<string, EngineeringTask> by = tasks.Where(x => !String.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> successors = by.Keys.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase); Dictionary<string, int> indegree = by.Keys.ToDictionary(x => x, x => 0, StringComparer.OrdinalIgnoreCase);
            foreach (EngineeringTask t in tasks) foreach (string pred in t.PredecessorTaskIds ?? new List<string>()) { if (!by.ContainsKey(pred)) { result.MissingPredecessorIds.Add(pred); continue; } successors[pred].Add(t.Id); indegree[t.Id]++; }
            Queue<string> ready = new Queue<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key).OrderBy(x => x)); List<string> order = new List<string>(); while (ready.Count > 0) { string id = ready.Dequeue(); order.Add(id); foreach (string next in successors[id].OrderBy(x => x)) { indegree[next]--; if (indegree[next] == 0) ready.Enqueue(next); } } result.Acyclic = order.Count == tasks.Count; if (!result.Acyclic) { result.Audit.Add("BLOCKER · TASK DEPENDENCY CYCLE · critical-path timing is undefined."); return result; }
            Dictionary<string, EngineeringScheduleRow> rows = new Dictionary<string, EngineeringScheduleRow>(StringComparer.OrdinalIgnoreCase); foreach (string id in order) { EngineeringTask t = by[id]; double expected = durationOverride != null && durationOverride.ContainsKey(id) ? Math.Max(0, durationOverride[id]) : Math.Max(0, (t.OptimisticDuration + 4 * t.MostLikelyDuration + t.PessimisticDuration) / 6.0); double variance = Math.Pow(Math.Max(0, t.PessimisticDuration - t.OptimisticDuration) / 6.0, 2); double es = (t.PredecessorTaskIds ?? new List<string>()).Where(rows.ContainsKey).Select(x => rows[x].EarliestFinish).DefaultIfEmpty(0).Max(); rows[id] = new EngineeringScheduleRow { TaskId = id, TaskName = t.Name, ExpectedDuration = expected, Variance = variance, EarliestStart = es, EarliestFinish = es + expected }; }
            result.ExpectedProjectDuration = rows.Values.Select(x => x.EarliestFinish).DefaultIfEmpty(0).Max(); foreach (string id in order.AsEnumerable().Reverse()) { EngineeringScheduleRow row = rows[id]; List<string> next = successors[id]; row.LatestFinish = next.Count == 0 ? result.ExpectedProjectDuration : next.Min(x => rows[x].LatestStart); row.LatestStart = row.LatestFinish - row.ExpectedDuration; row.Slack = Math.Max(0, row.LatestStart - row.EarliestStart); row.Critical = row.Slack <= 1e-8; if (row.Critical) { result.CriticalTaskIds.Add(id); result.CriticalPathVariance += row.Variance; } } result.Rows = order.Select(x => rows[x]).ToList(); return result;
        }

        public static EngineeringReliabilityAnalysis AnalyzeReliability(EngineeringArchitecture a)
        {
            EngineeringReliabilityAnalysis r = new EngineeringReliabilityAnalysis(); if (a == null) return r; r.MissionHours = Math.Max(0, a.MissionHours); r.SeriesReliability = 1; r.SeriesAvailability = 1; foreach (EngineeringComponent c in (a.Components ?? new List<EngineeringComponent>()).Where(x => Active(x.Status))) { double lambda = Math.Max(0, c.FailureRatePerHour), mu = Math.Max(0, c.RepairRatePerHour), reliability = Math.Exp(-lambda * r.MissionHours), availability = lambda + mu <= Epsilon ? (lambda <= Epsilon ? 1 : 0) : mu / (lambda + mu); EngineeringReliabilityRow row = new EngineeringReliabilityRow { ComponentId = c.Id, ComponentName = c.Name, Reliability = reliability, Availability = availability, MtbfHours = lambda <= Epsilon ? Double.MaxValue : 1 / lambda, MttrHours = mu <= Epsilon ? Double.MaxValue : 1 / mu }; r.Components.Add(row); r.SeriesReliability *= reliability; r.SeriesAvailability *= availability; } r.Fmea = (a.FailureModes ?? new List<EngineeringFailureMode>()).Where(x => Active(x.Status)).OrderByDescending(x => x.RiskPriorityNumber).ThenByDescending(x => x.Probability).ToList(); r.FaultTree = AnalyzeFaultTree(a); if (r.Components.Count == 0) r.Audit.Add("No active components exist for reliability analysis."); if (r.Components.Any(x => x.MttrHours >= Double.MaxValue / 2)) r.Audit.Add("One or more components declare no repair rate; steady-state availability needs interpretation."); return r;
        }

        public static EngineeringFaultTreeAnalysis AnalyzeFaultTree(EngineeringArchitecture a)
        {
            EngineeringFaultTreeAnalysis result = new EngineeringFaultTreeAnalysis { TopGateId = a == null ? "" : a.TopFaultGateId }; if (a == null || String.IsNullOrWhiteSpace(a.TopFaultGateId)) { result.Warnings.Add("No top fault gate is declared."); return result; } Dictionary<string, EngineeringFailureMode> modes = (a.FailureModes ?? new List<EngineeringFailureMode>()).Where(x => Active(x.Status)).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); Dictionary<string, EngineeringFaultGate> gates = (a.FaultGates ?? new List<EngineeringFaultGate>()).Where(x => Active(x.Status)).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); HashSet<string> stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase); result.TopEventProbability = FaultProbability(a.TopFaultGateId, modes, gates, stack, result.Warnings); stack.Clear(); result.MinimalCutSets = Absorb(CutSets(a.TopFaultGateId, modes, gates, stack, result.Warnings, 2048), 2048); return result;
        }

        public static EngineeringFlowAnalysis AnalyzeFlow(EngineeringArchitecture a, string sourceId, string sinkId)
        {
            EngineeringFlowAnalysis result = new EngineeringFlowAnalysis { SourceComponentId = sourceId, SinkComponentId = sinkId }; if (a == null || Eq(sourceId, sinkId)) { result.Audit.Add("Distinct source and sink components are required."); return result; } List<EngineeringComponent> components = (a.Components ?? new List<EngineeringComponent>()).Where(x => Active(x.Status)).ToList(); Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < components.Count; i++) index[components[i].Id] = i; if (!index.ContainsKey(sourceId ?? "") || !index.ContainsKey(sinkId ?? "")) { result.Audit.Add("Source or sink component does not exist."); return result; } int n = components.Count; double[,] capacity = new double[n, n], flow = new double[n, n]; Dictionary<string, List<EngineeringInterface>> edgeMap = new Dictionary<string, List<EngineeringInterface>>(StringComparer.OrdinalIgnoreCase); foreach (EngineeringInterface e in (a.Interfaces ?? new List<EngineeringInterface>()).Where(x => Active(x.Status) && index.ContainsKey(x.FromComponentId ?? "") && index.ContainsKey(x.ToComponentId ?? "") && x.Capacity > 0)) { int u = index[e.FromComponentId], v = index[e.ToComponentId]; capacity[u, v] += e.Capacity; string key = u + ">" + v; if (!edgeMap.ContainsKey(key)) edgeMap[key] = new List<EngineeringInterface>(); edgeMap[key].Add(e); }
            int s = index[sourceId], t = index[sinkId]; while (true) { int[] parent = Enumerable.Repeat(-1, n).ToArray(); parent[s] = s; Queue<int> q = new Queue<int>(); q.Enqueue(s); while (q.Count > 0 && parent[t] < 0) { int u = q.Dequeue(); for (int v = 0; v < n; v++) if (parent[v] < 0 && capacity[u, v] - flow[u, v] > Epsilon) { parent[v] = u; q.Enqueue(v); } } if (parent[t] < 0) break; double aug = Double.PositiveInfinity; for (int v = t; v != s; v = parent[v]) aug = Math.Min(aug, capacity[parent[v], v] - flow[parent[v], v]); for (int v = t; v != s; v = parent[v]) { int u = parent[v]; flow[u, v] += aug; flow[v, u] -= aug; } result.MaximumFlow += aug; }
            HashSet<int> reachable = new HashSet<int> { s }; Queue<int> visit = new Queue<int>(); visit.Enqueue(s); while (visit.Count > 0) { int u = visit.Dequeue(); for (int v = 0; v < n; v++) if (!reachable.Contains(v) && capacity[u, v] - flow[u, v] > Epsilon) { reachable.Add(v); visit.Enqueue(v); } } for (int i = 0; i < n; i++) if (reachable.Contains(i)) result.SourceSideComponentIds.Add(components[i].Id); else result.SinkSideComponentIds.Add(components[i].Id);
            foreach (KeyValuePair<string, List<EngineeringInterface>> pair in edgeMap) { string[] uv = pair.Key.Split('>'); int u = Int32.Parse(uv[0], CultureInfo.InvariantCulture), v = Int32.Parse(uv[1], CultureInfo.InvariantCulture); double aggregateFlow = Math.Max(0, flow[u, v]), aggregateCapacity = pair.Value.Sum(x => x.Capacity); foreach (EngineeringInterface e in pair.Value) result.InterfaceFlows[e.Id] = aggregateCapacity <= Epsilon ? 0 : aggregateFlow * e.Capacity / aggregateCapacity; if (reachable.Contains(u) && !reachable.Contains(v)) result.BottleneckInterfaceIds.AddRange(pair.Value.Select(x => x.Id)); } return result;
        }

        public static List<EngineeringTradeEvaluation> AnalyzeTrades(EngineeringArchitecture a)
        {
            List<EngineeringTradeEvaluation> rows = new List<EngineeringTradeEvaluation>(); if (a == null) return rows; List<EngineeringCriterion> criteria = (a.Criteria ?? new List<EngineeringCriterion>()).Where(x => Active(x.Status) && x.Weight >= 0).ToList(); List<EngineeringAlternative> alternatives = (a.Alternatives ?? new List<EngineeringAlternative>()).Where(x => Active(x.Status)).ToList(); if (criteria.Count == 0 || alternatives.Count == 0) return rows; double weightSum = criteria.Sum(x => x.Weight); if (weightSum <= Epsilon) weightSum = criteria.Count;
            Dictionary<string, double> min = criteria.ToDictionary(c => c.Id, c => alternatives.Select(x => Metric(x, c.Id)).DefaultIfEmpty(0).Min(), StringComparer.OrdinalIgnoreCase), max = criteria.ToDictionary(c => c.Id, c => alternatives.Select(x => Metric(x, c.Id)).DefaultIfEmpty(0).Max(), StringComparer.OrdinalIgnoreCase);
            foreach (EngineeringAlternative alt in alternatives) { EngineeringTradeEvaluation row = new EngineeringTradeEvaluation { AlternativeId = alt.Id, AlternativeName = alt.Name }; foreach (EngineeringCriterion c in criteria) { double raw = Metric(alt, c.Id), span = max[c.Id] - min[c.Id], normalized = Math.Abs(span) <= Epsilon ? 1 : (Eq(c.Direction, "MINIMIZE") ? (max[c.Id] - raw) / span : (raw - min[c.Id]) / span); row.NormalizedMetrics[c.Id] = normalized; row.WeightedUtility += normalized * (c.Weight <= Epsilon ? 1 : c.Weight) / weightSum; if (c.Hard && !Meets(raw, c.Direction, c.Threshold)) { row.Feasible = false; row.Violations.Add(c.Name + " · " + raw.ToString("R", CultureInfo.InvariantCulture) + " violates " + c.Direction + " " + c.Threshold.ToString("R", CultureInfo.InvariantCulture)); } } rows.Add(row); }
            foreach (EngineeringTradeEvaluation row in rows) { bool dominated = rows.Any(other => other != row && Dominates(other, row, criteria)); row.ParetoEfficient = !dominated; double positive = 0, negative = 0; foreach (EngineeringCriterion c in criteria) { double w = (c.Weight <= Epsilon ? 1 : c.Weight) / weightSum, x = row.NormalizedMetrics[c.Id]; positive += w * (1 - x) * (1 - x); negative += w * x * x; } positive = Math.Sqrt(positive); negative = Math.Sqrt(negative); row.TopsisCloseness = positive + negative <= Epsilon ? 0 : negative / (positive + negative); row.MaximumRegret = criteria.Max(c => rows.Max(x => x.NormalizedMetrics[c.Id]) - row.NormalizedMetrics[c.Id]); row.OutrankingWins = rows.Count(other => other != row && Outranks(row, other, criteria, weightSum)); }
            return rows.OrderBy(x => x.Feasible ? 0 : 1).ThenByDescending(x => x.ParetoEfficient).ThenByDescending(x => x.TopsisCloseness).ThenByDescending(x => x.WeightedUtility).ToList();
        }

        public static EngineeringRiskEnvelope AnalyzeRisk(EngineeringArchitecture a, int scenarios, int seed)
        {
            EngineeringRiskEnvelope r = new EngineeringRiskEnvelope(); if (a == null) return r; scenarios = Math.Max(16, Math.Min(8192, scenarios)); r.Scenarios = scenarios; List<EngineeringTask> tasks = (a.Tasks ?? new List<EngineeringTask>()).Where(x => Active(x.Status)).ToList(); List<EngineeringComponent> components = (a.Components ?? new List<EngineeringComponent>()).Where(x => Active(x.Status)).ToList(); List<double> durations = new List<double>(), costs = new List<double>(); int skip = 31 + Math.Abs(seed % 7919);
            for (int s = 0; s < scenarios; s++) { Dictionary<string, double> overrides = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < tasks.Count; i++) { EngineeringTask t = tasks[i]; double u = RadicalInverse(skip + s + 1, Prime(i + 1)); overrides[t.Id] = TriangularQuantile(u, t.OptimisticDuration, t.MostLikelyDuration, t.PessimisticDuration); } EngineeringScheduleAnalysis schedule = AnalyzeSchedule(a, overrides); durations.Add(schedule.Acyclic ? schedule.ExpectedProjectDuration : 0); double cost = tasks.Sum(x => x.Cost); for (int i = 0; i < components.Count; i++) cost += TriangularQuantile(RadicalInverse(skip + s + 1, Prime(tasks.Count + i + 1)), components[i].CostOptimistic, components[i].CostMostLikely, components[i].CostPessimistic); costs.Add(cost); }
            List<double> sortedDurations = durations.OrderBy(x => x).ToList(), sortedCosts = costs.OrderBy(x => x).ToList(); r.ScheduleP05 = Quantile(sortedDurations, 0.05); r.ScheduleP50 = Quantile(sortedDurations, 0.50); r.ScheduleP95 = Quantile(sortedDurations, 0.95); r.CostP05 = Quantile(sortedCosts, 0.05); r.CostP50 = Quantile(sortedCosts, 0.50); r.CostP95 = Quantile(sortedCosts, 0.95); int joint = 0; for (int i = 0; i < scenarios; i++) if (durations[i] >= r.ScheduleP95 && costs[i] >= r.CostP95) joint++; r.JointUpperTailProbability = (double)joint / scenarios; r.Fingerprint = HashText(Fingerprint(a) + "|" + scenarios + "|" + seed + "|" + r.ScheduleP95.ToString("R", CultureInfo.InvariantCulture) + "|" + r.CostP95.ToString("R", CultureInfo.InvariantCulture)); r.Audit.Add("Low-discrepancy triangular scenario envelope over declared task-duration and component-cost ranges; it is not a calibrated forecast."); return r;
        }

        public static EngineeringSafetyCaseAnalysis AnalyzeSafetyCase(EngineeringArchitecture a)
        {
            EngineeringSafetyCaseAnalysis r = new EngineeringSafetyCaseAnalysis(); if (a == null) return r; List<EngineeringSafetyClaim> claims = (a.SafetyClaims ?? new List<EngineeringSafetyClaim>()).Where(x => Active(x.Status)).ToList(); Dictionary<string, EngineeringSafetyClaim> by = claims.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> children = claims.ToDictionary(x => x.Id, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (EngineeringSafetyClaim c in claims) { if (!String.IsNullOrWhiteSpace(c.ParentClaimId) && by.ContainsKey(c.ParentClaimId)) children[c.ParentClaimId].Add(c.Id); else if (!String.IsNullOrWhiteSpace(c.ParentClaimId)) r.OrphanClaimIds.Add(c.Id); } r.Claims = claims.Count; r.Goals = claims.Count(x => Eq(x.Kind, "GOAL")); r.Solutions = claims.Count(x => Eq(x.Kind, "SOLUTION")); Dictionary<string, List<string>> graph = children; r.Acyclic = !Tarjan(graph).Any(x => x.Count > 1 || (x.Count == 1 && graph[x[0]].Contains(x[0], StringComparer.OrdinalIgnoreCase)));
            HashSet<string> verifiedLinks = new HashSet<string>((a.VerificationLinks ?? new List<EngineeringVerificationLink>()).Where(x => Eq(x.Status, "VERIFIED") || Eq(x.Status, "PASS") || Eq(x.Status, "ACCEPTED")).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (EngineeringSafetyClaim goal in claims.Where(x => Eq(x.Kind, "GOAL"))) { bool supported = Descendants(goal.Id, children).Select(id => by[id]).Any(x => Eq(x.Kind, "SOLUTION") && ((x.EvidenceNodeIds ?? new List<string>()).Count > 0 || (x.VerificationLinkIds ?? new List<string>()).Any(verifiedLinks.Contains))); if (supported) r.SupportedGoals++; else r.UnsupportedGoalIds.Add(goal.Id); } r.Coverage = r.Goals == 0 ? 0 : (double)r.SupportedGoals / r.Goals; if (!r.Acyclic) r.Warnings.Add("Safety-claim graph contains a cycle; circular argumentation cannot discharge a goal."); if (r.UnsupportedGoalIds.Count > 0) r.Warnings.Add("Open goals lack a descendant solution bound to evidence or accepted verification."); return r;
        }

        public static List<string> Impact(EngineeringArchitecture a, string componentId)
        {
            if (a == null) return new List<string>(); HashSet<string> ids = new HashSet<string>((a.Components ?? new List<EngineeringComponent>()).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> graph = ids.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (EngineeringInterface e in a.Interfaces ?? new List<EngineeringInterface>()) if (Active(e.Status) && ids.Contains(e.FromComponentId ?? "") && ids.Contains(e.ToComponentId ?? "")) graph[e.FromComponentId].Add(e.ToComponentId); return Reachable(graph, componentId).ToList();
        }

        public static List<string> Audit(EngineeringArchitecture a)
        {
            List<string> f = new List<string>(); if (a == null) { f.Add("BLOCKER · NULL_ARCHITECTURE"); return f; } HashSet<string> componentIds = new HashSet<string>((a.Components ?? new List<EngineeringComponent>()).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); HashSet<string> reqIds = new HashSet<string>((a.Requirements ?? new List<EngineeringRequirement>()).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); if (componentIds.Count == 0) f.Add("BLOCKER · NO_COMPONENTS"); if (reqIds.Count == 0) f.Add("BLOCKER · NO_REQUIREMENTS"); foreach (EngineeringInterface e in a.Interfaces ?? new List<EngineeringInterface>()) { if (!componentIds.Contains(e.FromComponentId ?? "")) f.Add("BLOCKER · INTERFACE_FROM_MISSING · " + e.Id); if (!componentIds.Contains(e.ToComponentId ?? "")) f.Add("BLOCKER · INTERFACE_TO_MISSING · " + e.Id); if (e.Capacity < 0 || e.Latency < 0) f.Add("BLOCKER · NEGATIVE_INTERFACE_METRIC · " + e.Id); if (String.IsNullOrWhiteSpace(e.Contract)) f.Add("WARN · UNDECLARED_INTERFACE_CONTRACT · " + e.Id); } foreach (EngineeringComponent c in a.Components ?? new List<EngineeringComponent>()) { if (c.TechnologyReadinessLevel < 1 || c.TechnologyReadinessLevel > 9) f.Add("WARN · TRL_OUT_OF_RANGE · " + c.Id); if (c.CostOptimistic > c.CostMostLikely || c.CostMostLikely > c.CostPessimistic) f.Add("BLOCKER · INVALID_COST_RANGE · " + c.Id); if (c.FailureRatePerHour < 0 || c.RepairRatePerHour < 0) f.Add("BLOCKER · NEGATIVE_RELIABILITY_RATE · " + c.Id); } foreach (EngineeringTask t in a.Tasks ?? new List<EngineeringTask>()) if (t.OptimisticDuration > t.MostLikelyDuration || t.MostLikelyDuration > t.PessimisticDuration || t.OptimisticDuration < 0) f.Add("BLOCKER · INVALID_TASK_RANGE · " + t.Id); foreach (EngineeringFailureMode m in a.FailureModes ?? new List<EngineeringFailureMode>()) if (m.Probability < 0 || m.Probability > 1) f.Add("BLOCKER · INVALID_FAILURE_PROBABILITY · " + m.Id); EngineeringRequirementAnalysis req = AnalyzeRequirements(a); f.AddRange(req.Conflicts.Select(x => "BLOCKER · REQUIREMENT_CONFLICT · " + x)); EngineeringScheduleAnalysis schedule = AnalyzeSchedule(a, null); if (!schedule.Acyclic) f.Add("BLOCKER · CYCLIC_TASK_NETWORK"); f.AddRange(AnalyzeBudgets(a).Where(x => !x.Compliant).Select(x => "BLOCKER · RESOURCE_OVERCOMMITMENT · " + x.Name)); if (f.Count == 0) f.Add("PASS · STRUCTURAL_ENGINEERING_AUDIT · References, ranges, budgets, dependencies and declared authority boundaries are internally coherent."); return f;
        }

        public static string Fingerprint(EngineeringArchitecture a)
        {
            if (a == null) return HashText("NULL_ARCHITECTURE"); StringBuilder b = new StringBuilder(); b.Append("SYSTEMS_ENGINEERING_V1|").Append(a.Id).Append('|').Append(a.ParentArchitectureId).Append('|').Append(a.SourceKind).Append('|').Append(a.SourceId).Append('|').Append(a.SourceFingerprint).Append('|').Append(a.Name).Append('|').Append(a.Charter).Append('|').Append(a.Status).Append('|').Append(a.LifecyclePhase).Append('|').Append(a.Revision).Append('|').Append(a.MissionHours.ToString("R", CultureInfo.InvariantCulture));
            foreach (EngineeringRequirement q in (a.Requirements ?? new List<EngineeringRequirement>()).OrderBy(x => x.Id)) b.Append("|REQ:").Append(q.Id).Append(':').Append(q.ParentRequirementId).Append(':').Append(q.Name).Append(':').Append(q.Statement).Append(':').Append(q.Kind).Append(':').Append(q.Priority).Append(':').Append(q.Status).Append(':').Append(q.MetricKey).Append(':').Append(q.Operator).Append(':').Append(q.Threshold.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(q.Tolerance.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(String.Join(",", q.SatisfiedByComponentIds.OrderBy(x => x).ToArray()));
            foreach (EngineeringComponent c in (a.Components ?? new List<EngineeringComponent>()).OrderBy(x => x.Id)) { b.Append("|CMP:").Append(c.Id).Append(':').Append(c.ParentComponentId).Append(':').Append(c.Name).Append(':').Append(c.Kind).Append(':').Append(c.Status).Append(':').Append(c.TechnologyReadinessLevel).Append(':').Append(c.Mass.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Power.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Compute.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Heat.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.CostOptimistic.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.CostMostLikely.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.CostPessimistic.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.FailureRatePerHour.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.RepairRatePerHour.ToString("R", CultureInfo.InvariantCulture)); foreach (KeyValuePair<string, double> x in c.Attributes.OrderBy(x => x.Key)) b.Append(':').Append(x.Key).Append('=').Append(x.Value.ToString("R", CultureInfo.InvariantCulture)); }
            foreach (EngineeringInterface e in (a.Interfaces ?? new List<EngineeringInterface>()).OrderBy(x => x.Id)) b.Append("|IF:").Append(e.Id).Append(':').Append(e.FromComponentId).Append(':').Append(e.ToComponentId).Append(':').Append(e.Kind).Append(':').Append(e.Status).Append(':').Append(e.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(e.Latency.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(e.Availability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(e.Contract).Append(':').Append(e.Precondition).Append(':').Append(e.Postcondition);
            foreach (EngineeringResourceBudget x in (a.Budgets ?? new List<EngineeringResourceBudget>()).OrderBy(x => x.Id)) { b.Append("|BUD:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Unit).Append(':').Append(x.Available.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.RequiredReserve.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Hard); foreach (KeyValuePair<string, double> y in x.Allocations.OrderBy(y => y.Key)) b.Append(':').Append(y.Key).Append('=').Append(y.Value.ToString("R", CultureInfo.InvariantCulture)); }
            foreach (EngineeringTask t in (a.Tasks ?? new List<EngineeringTask>()).OrderBy(x => x.Id)) b.Append("|TASK:").Append(t.Id).Append(':').Append(t.Name).Append(':').Append(t.Status).Append(':').Append(t.OptimisticDuration.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(t.MostLikelyDuration.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(t.PessimisticDuration.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(String.Join(",", t.PredecessorTaskIds.OrderBy(x => x).ToArray()));
            foreach (EngineeringFailureMode m in (a.FailureModes ?? new List<EngineeringFailureMode>()).OrderBy(x => x.Id)) b.Append("|FM:").Append(m.Id).Append(':').Append(m.ComponentId).Append(':').Append(m.Name).Append(':').Append(m.Status).Append(':').Append(m.Severity).Append(':').Append(m.Occurrence).Append(':').Append(m.Detectability).Append(':').Append(m.Probability.ToString("R", CultureInfo.InvariantCulture)); foreach (EngineeringFaultGate g in (a.FaultGates ?? new List<EngineeringFaultGate>()).OrderBy(x => x.Id)) b.Append("|GATE:").Append(g.Id).Append(':').Append(g.Kind).Append(':').Append(g.RequiredCount).Append(':').Append(String.Join(",", g.InputIds.OrderBy(x => x).ToArray()));
            foreach (EngineeringCriterion c in (a.Criteria ?? new List<EngineeringCriterion>()).OrderBy(x => x.Id)) b.Append("|CRIT:").Append(c.Id).Append(':').Append(c.Name).Append(':').Append(c.Direction).Append(':').Append(c.Weight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Threshold.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Hard); foreach (EngineeringAlternative x in (a.Alternatives ?? new List<EngineeringAlternative>()).OrderBy(x => x.Id)) { b.Append("|ALT:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Status); foreach (KeyValuePair<string, double> y in x.Metrics.OrderBy(y => y.Key)) b.Append(':').Append(y.Key).Append('=').Append(y.Value.ToString("R", CultureInfo.InvariantCulture)); }
            foreach (EngineeringVerificationLink v in (a.VerificationLinks ?? new List<EngineeringVerificationLink>()).OrderBy(x => x.Id)) b.Append("|VER:").Append(v.Id).Append(':').Append(v.RequirementId).Append(':').Append(v.ArtifactKind).Append(':').Append(v.ArtifactId).Append(':').Append(v.Status).Append(':').Append(v.Method).Append(':').Append(v.CertificateHash); foreach (EngineeringSafetyClaim c in (a.SafetyClaims ?? new List<EngineeringSafetyClaim>()).OrderBy(x => x.Id)) b.Append("|SAFE:").Append(c.Id).Append(':').Append(c.ParentClaimId).Append(':').Append(c.Kind).Append(':').Append(c.Status).Append(':').Append(c.Statement); foreach (string x in (a.Assumptions ?? new List<string>()).OrderBy(x => x)) b.Append("|A:").Append(x); foreach (string x in (a.EvidenceNodeIds ?? new List<string>()).OrderBy(x => x)) b.Append("|E:").Append(x); return HashText(b.ToString());
        }

        private static double FaultProbability(string id, Dictionary<string, EngineeringFailureMode> modes, Dictionary<string, EngineeringFaultGate> gates, HashSet<string> stack, List<string> warnings)
        {
            EngineeringFailureMode mode; if (modes.TryGetValue(id ?? "", out mode)) return Clamp(mode.Probability, 0, 1); EngineeringFaultGate gate; if (!gates.TryGetValue(id ?? "", out gate)) { warnings.Add("Unknown fault-tree input " + (id ?? "∅")); return 0; } if (!stack.Add(id)) { warnings.Add("Fault-tree cycle at " + id); return 0; } List<double> p = (gate.InputIds ?? new List<string>()).Select(x => FaultProbability(x, modes, gates, stack, warnings)).ToList(); stack.Remove(id); if (Eq(gate.Kind, "AND")) return p.Aggregate(1.0, (x, y) => x * y); if (Eq(gate.Kind, "KOFN")) { int k = Math.Max(1, Math.Min(p.Count, gate.RequiredCount)); double[] dist = new double[p.Count + 1]; dist[0] = 1; foreach (double q in p) for (int j = p.Count; j >= 0; j--) dist[j] = dist[j] * (1 - q) + (j > 0 ? dist[j - 1] * q : 0); return Clamp(dist.Skip(k).Sum(), 0, 1); } return Clamp(1 - p.Aggregate(1.0, (x, y) => x * (1 - y)), 0, 1);
        }

        private static List<List<string>> CutSets(string id, Dictionary<string, EngineeringFailureMode> modes, Dictionary<string, EngineeringFaultGate> gates, HashSet<string> stack, List<string> warnings, int cap)
        {
            if (modes.ContainsKey(id ?? "")) return new List<List<string>> { new List<string> { id } }; EngineeringFaultGate gate; if (!gates.TryGetValue(id ?? "", out gate)) return new List<List<string>>(); if (!stack.Add(id)) { warnings.Add("Cut-set recursion cycle at " + id); return new List<List<string>>(); } List<List<List<string>>> child = (gate.InputIds ?? new List<string>()).Select(x => CutSets(x, modes, gates, stack, warnings, cap)).ToList(); stack.Remove(id); List<List<string>> result = new List<List<string>>(); if (Eq(gate.Kind, "OR")) foreach (List<List<string>> sets in child) result.AddRange(sets); else if (Eq(gate.Kind, "KOFN")) { int k = Math.Max(1, Math.Min(child.Count, gate.RequiredCount)); foreach (List<int> combination in Combinations(child.Count, k, cap)) { List<List<string>> product = new List<List<string>> { new List<string>() }; foreach (int i in combination) product = CartesianUnion(product, child[i], cap); result.AddRange(product); if (result.Count >= cap) break; } } else { result.Add(new List<string>()); foreach (List<List<string>> sets in child) result = CartesianUnion(result, sets, cap); } return result.Take(cap).ToList();
        }

        private static List<List<string>> CartesianUnion(List<List<string>> a, List<List<string>> b, int cap) { List<List<string>> r = new List<List<string>>(); foreach (List<string> x in a) foreach (List<string> y in b) { r.Add(x.Concat(y).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(z => z).ToList()); if (r.Count >= cap) return r; } return r; }
        private static List<List<string>> Absorb(List<List<string>> source, int cap) { List<List<string>> unique = source.Select(x => x.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(y => y).ToList()).GroupBy(x => String.Join("|", x.ToArray()), StringComparer.OrdinalIgnoreCase).Select(x => x.First()).OrderBy(x => x.Count).ToList(); List<List<string>> r = new List<List<string>>(); foreach (List<string> set in unique) if (!r.Any(existing => existing.All(x => set.Contains(x, StringComparer.OrdinalIgnoreCase)))) { r.Add(set); if (r.Count >= cap) break; } return r; }
        private static IEnumerable<List<int>> Combinations(int n, int k, int cap) { int yielded = 0; int[] c = Enumerable.Range(0, k).ToArray(); if (k <= 0 || k > n) yield break; while (true) { yield return c.ToList(); yielded++; if (yielded >= cap) yield break; int i = k - 1; while (i >= 0 && c[i] == n - k + i) i--; if (i < 0) yield break; c[i]++; for (int j = i + 1; j < k; j++) c[j] = c[j - 1] + 1; } }

        private static List<List<string>> Tarjan(Dictionary<string, List<string>> graph) { List<List<string>> result = new List<List<string>>(); Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> on = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int next = 0; foreach (string v in graph.Keys.OrderBy(x => x)) if (!index.ContainsKey(v)) TarjanVisit(v, graph, index, low, stack, on, ref next, result); return result; }
        private static void TarjanVisit(string v, Dictionary<string, List<string>> graph, Dictionary<string, int> index, Dictionary<string, int> low, Stack<string> stack, HashSet<string> on, ref int next, List<List<string>> result) { index[v] = low[v] = next++; stack.Push(v); on.Add(v); foreach (string w in graph[v]) { if (!graph.ContainsKey(w)) continue; if (!index.ContainsKey(w)) { TarjanVisit(w, graph, index, low, stack, on, ref next, result); low[v] = Math.Min(low[v], low[w]); } else if (on.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> c = new List<string>(); string w; do { w = stack.Pop(); on.Remove(w); c.Add(w); } while (!Eq(w, v)); result.Add(c.OrderBy(x => x).ToList()); } }
        private static List<string> CondensationOrder(List<List<string>> scc, Dictionary<string, List<string>> graph, Dictionary<string, int> by) { Dictionary<int, HashSet<int>> edges = Enumerable.Range(0, scc.Count).ToDictionary(x => x, x => new HashSet<int>()); int[] indegree = new int[scc.Count]; foreach (KeyValuePair<string, List<string>> row in graph) foreach (string to in row.Value) if (by.ContainsKey(to) && by[row.Key] != by[to] && edges[by[row.Key]].Add(by[to])) indegree[by[to]]++; Queue<int> q = new Queue<int>(Enumerable.Range(0, scc.Count).Where(x => indegree[x] == 0)); List<string> r = new List<string>(); while (q.Count > 0) { int i = q.Dequeue(); r.Add(String.Join(" + ", scc[i].ToArray())); foreach (int j in edges[i]) if (--indegree[j] == 0) q.Enqueue(j); } return r; }
        private static Dictionary<string, double> PageRank(Dictionary<string, List<string>> graph) { int n = graph.Count; Dictionary<string, double> p = graph.Keys.ToDictionary(x => x, x => n == 0 ? 0 : 1.0 / n, StringComparer.OrdinalIgnoreCase); for (int round = 0; round < 80 && n > 0; round++) { double sink = graph.Where(x => x.Value.Count == 0).Sum(x => p[x.Key]), baseValue = (1 - 0.85) / n + 0.85 * sink / n; Dictionary<string, double> next = graph.Keys.ToDictionary(x => x, x => baseValue, StringComparer.OrdinalIgnoreCase); foreach (KeyValuePair<string, List<string>> row in graph) if (row.Value.Count > 0) foreach (string to in row.Value) if (next.ContainsKey(to)) next[to] += 0.85 * p[row.Key] / row.Value.Count; double delta = graph.Keys.Sum(x => Math.Abs(next[x] - p[x])); p = next; if (delta < 1e-12) break; } return p; }
        private static Dictionary<string, double> Betweenness(Dictionary<string, List<string>> graph) { Dictionary<string, double> cb = graph.Keys.ToDictionary(x => x, x => 0.0, StringComparer.OrdinalIgnoreCase); foreach (string s in graph.Keys) { Stack<string> stack = new Stack<string>(); Dictionary<string, List<string>> pred = graph.Keys.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase); Dictionary<string, double> sigma = graph.Keys.ToDictionary(x => x, x => 0.0, StringComparer.OrdinalIgnoreCase); Dictionary<string, int> dist = graph.Keys.ToDictionary(x => x, x => -1, StringComparer.OrdinalIgnoreCase); sigma[s] = 1; dist[s] = 0; Queue<string> q = new Queue<string>(); q.Enqueue(s); while (q.Count > 0) { string v = q.Dequeue(); stack.Push(v); foreach (string w in graph[v]) if (dist.ContainsKey(w)) { if (dist[w] < 0) { q.Enqueue(w); dist[w] = dist[v] + 1; } if (dist[w] == dist[v] + 1) { sigma[w] += sigma[v]; pred[w].Add(v); } } } Dictionary<string, double> delta = graph.Keys.ToDictionary(x => x, x => 0.0, StringComparer.OrdinalIgnoreCase); while (stack.Count > 0) { string w = stack.Pop(); foreach (string v in pred[w]) if (sigma[w] > Epsilon) delta[v] += sigma[v] / sigma[w] * (1 + delta[w]); if (!Eq(w, s)) cb[w] += delta[w]; } } double norm = graph.Count <= 2 ? 1 : (graph.Count - 1.0) * (graph.Count - 2.0); foreach (string id in graph.Keys.ToList()) cb[id] /= norm; return cb; }
        private static HashSet<string> Reachable(Dictionary<string, List<string>> graph, string start) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); if (!graph.ContainsKey(start ?? "")) return seen; Queue<string> q = new Queue<string>(); q.Enqueue(start); seen.Add(start); while (q.Count > 0) foreach (string next in graph[q.Dequeue()]) if (graph.ContainsKey(next) && seen.Add(next)) q.Enqueue(next); return seen; }
        private static HashSet<string> Descendants(string start, Dictionary<string, List<string>> graph) { return Reachable(graph, start); }

        private static bool Dominates(EngineeringTradeEvaluation a, EngineeringTradeEvaluation b, List<EngineeringCriterion> criteria) { bool strict = false; foreach (EngineeringCriterion c in criteria) { double x = a.NormalizedMetrics[c.Id], y = b.NormalizedMetrics[c.Id]; if (x + Epsilon < y) return false; if (x > y + Epsilon) strict = true; } return strict && (a.Feasible || !b.Feasible); }
        private static bool Outranks(EngineeringTradeEvaluation a, EngineeringTradeEvaluation b, List<EngineeringCriterion> criteria, double weightSum) { double concordance = 0, veto = 0; foreach (EngineeringCriterion c in criteria) { double w = (c.Weight <= Epsilon ? 1 : c.Weight) / weightSum, delta = a.NormalizedMetrics[c.Id] - b.NormalizedMetrics[c.Id]; if (delta >= -0.05) concordance += w; if (delta < -0.6) veto += w; } return concordance >= 0.65 && veto < 0.25; }
        private static bool Meets(double value, string direction, double threshold) { return Eq(direction, "MINIMIZE") ? value <= threshold + Epsilon : value >= threshold - Epsilon; }
        private static double Metric(EngineeringAlternative a, string id) { double v; return a.Metrics != null && a.Metrics.TryGetValue(id ?? "", out v) ? v : 0; }
        private static double Val(Dictionary<string, double> map, string id) { double v; return map.TryGetValue(id ?? "", out v) ? v : 0; }
        private static double TriangularQuantile(double u, double low, double mode, double high) { if (high < low) { double t = low; low = high; high = t; } mode = Clamp(mode, low, high); if (high - low <= Epsilon) return low; double f = (mode - low) / (high - low); return u < f ? low + Math.Sqrt(u * (high - low) * (mode - low)) : high - Math.Sqrt((1 - u) * (high - low) * (high - mode)); }
        private static double Quantile(List<double> sorted, double q) { if (sorted == null || sorted.Count == 0) return 0; double x = Clamp(q, 0, 1) * (sorted.Count - 1); int lo = (int)Math.Floor(x), hi = (int)Math.Ceiling(x); return lo == hi ? sorted[lo] : sorted[lo] * (hi - x) + sorted[hi] * (x - lo); }
        private static double RadicalInverse(int n, int b) { double value = 0, f = 1.0 / b; while (n > 0) { value += f * (n % b); n /= b; f /= b; } return value; }
        private static int Prime(int index) { int[] p = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293 }; return p[Math.Max(0, Math.Min(p.Length - 1, index - 1))]; }
        private static double Clamp(double x, double low, double high) { return Math.Max(low, Math.Min(high, x)); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETIRED") && !Eq(status, "REJECTED"); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static string HashText(string value) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

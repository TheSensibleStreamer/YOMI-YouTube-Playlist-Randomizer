using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    // DEV13.37.17 pure cybernetics kernel. It has no WPF, filesystem, process,
    // network, clock, playback, scheduler, actuator, experiment, or deployment authority.
    internal sealed class CyberStateVariable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double InitialValue { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double ProcessNoiseVariance { get; set; }
        public double RegulationWeight { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CyberStateVariable() { Status = "ACTIVE"; Minimum = -1000000; Maximum = 1000000; ProcessNoiseVariance = 0.0001; RegulationWeight = 1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "STATE") + "   ·   x₀ " + InitialValue.ToString("0.######", CultureInfo.InvariantCulture) + " " + (Unit ?? "") + "   ·   [" + Minimum.ToString("0.###", CultureInfo.InvariantCulture) + ", " + Maximum.ToString("0.###", CultureInfo.InvariantCulture) + "]   ·   q " + ProcessNoiseVariance.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberControlInput
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double MinimumSlew { get; set; }
        public double MaximumSlew { get; set; }
        public double EffortWeight { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CyberControlInput() { Status = "ACTIVE"; Minimum = -1; Maximum = 1; MinimumSlew = -1; MaximumSlew = 1; EffortWeight = 0.1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "INPUT") + "   ·   [" + Minimum.ToString("0.###", CultureInfo.InvariantCulture) + ", " + Maximum.ToString("0.###", CultureInfo.InvariantCulture) + "] " + (Unit ?? "") + "   ·   Δ[" + MinimumSlew.ToString("0.###", CultureInfo.InvariantCulture) + ", " + MaximumSlew.ToString("0.###", CultureInfo.InvariantCulture) + "]   ·   R " + EffortWeight.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberMeasuredOutput
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Target { get; set; }
        public double MeasurementNoiseVariance { get; set; }
        public double TrackingWeight { get; set; }
        public double SensorCost { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CyberMeasuredOutput() { Status = "ACTIVE"; Minimum = -1000000; Maximum = 1000000; MeasurementNoiseVariance = 0.01; TrackingWeight = 1; SensorCost = 1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "OUTPUT") + "   ·   target " + Target.ToString("0.######", CultureInfo.InvariantCulture) + " " + (Unit ?? "") + "   ·   r " + MeasurementNoiseVariance.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   cost " + SensorCost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberConstraint
    {
        public string Id { get; set; }
        public string VariableKind { get; set; }
        public string VariableId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public bool Hard { get; set; }
        public double Penalty { get; set; }
        public CyberConstraint() { VariableKind = "STATE"; Status = "ACTIVE"; Minimum = -1000000; Maximum = 1000000; Hard = true; Penalty = 1000; }
        public override string ToString() { return (Hard ? "HARD" : "SOFT") + "   ·   " + (VariableKind ?? "STATE") + ":" + (VariableId ?? "∅") + "   ·   [" + Minimum.ToString("0.######", CultureInfo.InvariantCulture) + ", " + Maximum.ToString("0.######", CultureInfo.InvariantCulture) + "]   ·   " + (Name ?? Id ?? "CONSTRAINT"); }
    }

    internal sealed class CyberFaultSignature
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public double PriorWeight { get; set; }
        public Dictionary<string, double> ResidualMeans { get; set; }
        public Dictionary<string, double> ResidualVariances { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CyberFaultSignature() { Status = "ACTIVE"; PriorWeight = 1; ResidualMeans = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); ResidualVariances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "FAULT") + "   ·   prior " + PriorWeight.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + ResidualMeans.Count.ToString(CultureInfo.InvariantCulture) + " residual coordinates"; }
    }

    internal sealed class CyberTwinModel
    {
        public string Id { get; set; }
        public string ParentTwinId { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Name { get; set; }
        public string Charter { get; set; }
        public string Status { get; set; }
        public string DynamicsKind { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public int Revision { get; set; }
        public double SamplePeriod { get; set; }
        public double ParameterUncertaintyFraction { get; set; }
        public List<CyberStateVariable> States { get; set; }
        public List<CyberControlInput> Inputs { get; set; }
        public List<CyberMeasuredOutput> Outputs { get; set; }
        public List<CyberConstraint> Constraints { get; set; }
        public List<CyberFaultSignature> FaultSignatures { get; set; }
        public double[][] A { get; set; }
        public double[][] B { get; set; }
        public double[][] C { get; set; }
        public double[][] D { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public CyberTwinModel() { Status = "CONCEPT"; DynamicsKind = "DISCRETE"; Revision = 1; SamplePeriod = 1; ParameterUncertaintyFraction = 0.05; States = new List<CyberStateVariable>(); Inputs = new List<CyberControlInput>(); Outputs = new List<CyberMeasuredOutput>(); Constraints = new List<CyberConstraint>(); FaultSignatures = new List<CyberFaultSignature>(); A = new double[0][]; B = new double[0][]; C = new double[0][]; D = new double[0][]; Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "CONCEPT") + "   ·   " + (Name ?? Id ?? "DIGITAL TWIN") + "   ·   " + States.Count.ToString(CultureInfo.InvariantCulture) + "x / " + Inputs.Count.ToString(CultureInfo.InvariantCulture) + "u / " + Outputs.Count.ToString(CultureInfo.InvariantCulture) + "y   ·   Δt " + SamplePeriod.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   r" + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberStructuralAnalysis
    {
        public int States { get; set; }
        public int Inputs { get; set; }
        public int Outputs { get; set; }
        public int ControllabilityRank { get; set; }
        public int ObservabilityRank { get; set; }
        public bool Controllable { get; set; }
        public bool Observable { get; set; }
        public bool Stable { get; set; }
        public double SpectralRadius { get; set; }
        public double ControllabilityGramianLogDet { get; set; }
        public double ObservabilityGramianLogDet { get; set; }
        public double ControllabilityCondition { get; set; }
        public double ObservabilityCondition { get; set; }
        public List<double> PoleMagnitudes { get; set; }
        public List<string> WeakStateIds { get; set; }
        public List<string> Audit { get; set; }
        public CyberStructuralAnalysis() { PoleMagnitudes = new List<double>(); WeakStateIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return (Stable ? "STABLE" : "UNSTABLE") + "   ·   ρ(A) " + SpectralRadius.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   controllability " + ControllabilityRank + "/" + States + "   ·   observability " + ObservabilityRank + "/" + States + "   ·   logdet Wc/Wo " + ControllabilityGramianLogDet.ToString("0.###", CultureInfo.InvariantCulture) + "/" + ObservabilityGramianLogDet.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberSimulationStep
    {
        public int Step { get; set; }
        public double Time { get; set; }
        public double[] State { get; set; }
        public double[] Input { get; set; }
        public double[] Output { get; set; }
        public double StageCost { get; set; }
        public List<string> Violations { get; set; }
        public CyberSimulationStep() { State = new double[0]; Input = new double[0]; Output = new double[0]; Violations = new List<string>(); }
        public override string ToString() { return "k=" + Step.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   t=" + Time.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(10) + "   ·   x=[" + Join(State) + "]   ·   u=[" + Join(Input) + "]   ·   y=[" + Join(Output) + "]   ·   J " + StageCost.ToString("0.######", CultureInfo.InvariantCulture) + (Violations.Count == 0 ? "" : "   ·   VIOLATIONS " + Violations.Count); }
        private static string Join(IEnumerable<double> x) { return String.Join(", ", (x ?? Enumerable.Empty<double>()).Select(v => v.ToString("0.######", CultureInfo.InvariantCulture)).ToArray()); }
    }

    internal sealed class CyberSimulationResult
    {
        public string TwinFingerprint { get; set; }
        public string ControllerKind { get; set; }
        public double TotalCost { get; set; }
        public int ViolationCount { get; set; }
        public List<CyberSimulationStep> Steps { get; set; }
        public List<string> Audit { get; set; }
        public CyberSimulationResult() { Steps = new List<CyberSimulationStep>(); Audit = new List<string>(); }
        public override string ToString() { return (ControllerKind ?? "OPEN_LOOP") + "   ·   " + Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps   ·   cost " + TotalCost.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + ViolationCount.ToString(CultureInfo.InvariantCulture) + " constraint violations"; }
    }

    internal sealed class CyberKalmanStep
    {
        public int Step { get; set; }
        public double[] PriorState { get; set; }
        public double[] PosteriorState { get; set; }
        public double[] Innovation { get; set; }
        public double InnovationMahalanobis { get; set; }
        public double CovarianceTrace { get; set; }
        public CyberKalmanStep() { PriorState = new double[0]; PosteriorState = new double[0]; Innovation = new double[0]; }
        public override string ToString() { return "k=" + Step.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   innovation χ² " + InnovationMahalanobis.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   tr(P) " + CovarianceTrace.ToString("0.######", CultureInfo.InvariantCulture).PadLeft(12) + "   ·   x̂=[" + String.Join(", ", PosteriorState.Select(x => x.ToString("0.######", CultureInfo.InvariantCulture)).ToArray()) + "]"; }
    }

    internal sealed class CyberKalmanResult
    {
        public string TwinFingerprint { get; set; }
        public double LogLikelihood { get; set; }
        public double FinalCovarianceTrace { get; set; }
        public List<CyberKalmanStep> Steps { get; set; }
        public List<string> Audit { get; set; }
        public CyberKalmanResult() { Steps = new List<CyberKalmanStep>(); Audit = new List<string>(); }
        public override string ToString() { return Steps.Count.ToString(CultureInfo.InvariantCulture) + " filter steps   ·   log L " + LogLikelihood.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   final tr(P) " + FinalCovarianceTrace.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberControllerSynthesis
    {
        public string Kind { get; set; }
        public bool Converged { get; set; }
        public int Iterations { get; set; }
        public double[][] Gain { get; set; }
        public double ClosedLoopSpectralRadius { get; set; }
        public double RiccatiResidual { get; set; }
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }
        public double ProcessGain { get; set; }
        public double TimeConstant { get; set; }
        public double DeadTime { get; set; }
        public List<string> Audit { get; set; }
        public CyberControllerSynthesis() { Gain = new double[0][]; Audit = new List<string>(); }
        public override string ToString() { return (Kind ?? "CONTROLLER") + "   ·   " + (Converged ? "CONVERGED" : "BOUNDED") + "   ·   iterations " + Iterations.ToString(CultureInfo.InvariantCulture) + "   ·   ρ(A−BK) " + ClosedLoopSpectralRadius.ToString("0.000000", CultureInfo.InvariantCulture) + (String.Equals(Kind, "PID_IMC", StringComparison.OrdinalIgnoreCase) ? "   ·   Kp/Ki/Kd " + Kp.ToString("0.######", CultureInfo.InvariantCulture) + "/" + Ki.ToString("0.######", CultureInfo.InvariantCulture) + "/" + Kd.ToString("0.######", CultureInfo.InvariantCulture) : ""); }
    }

    internal sealed class CyberMpcResult
    {
        public bool Converged { get; set; }
        public int Horizon { get; set; }
        public int Iterations { get; set; }
        public double InitialCost { get; set; }
        public double FinalCost { get; set; }
        public double GradientNorm { get; set; }
        public List<double[]> ControlSequence { get; set; }
        public CyberSimulationResult Prediction { get; set; }
        public List<string> Audit { get; set; }
        public CyberMpcResult() { ControlSequence = new List<double[]>(); Audit = new List<string>(); }
        public override string ToString() { return "MPC   ·   H=" + Horizon + "   ·   " + Iterations + " iterations   ·   J " + InitialCost.ToString("0.######", CultureInfo.InvariantCulture) + " → " + FinalCost.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   ||∇J|| " + GradientNorm.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberReachabilityStep
    {
        public int Step { get; set; }
        public double[] Center { get; set; }
        public double[] Radius { get; set; }
        public bool Safe { get; set; }
        public List<string> Violations { get; set; }
        public CyberReachabilityStep() { Center = new double[0]; Radius = new double[0]; Safe = true; Violations = new List<string>(); }
        public override string ToString() { return (Safe ? "SAFE" : "ESCAPES") + "   ·   k=" + Step.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   center [" + String.Join(", ", Center.Select(x => x.ToString("0.####", CultureInfo.InvariantCulture)).ToArray()) + "]   ·   radius [" + String.Join(", ", Radius.Select(x => x.ToString("0.####", CultureInfo.InvariantCulture)).ToArray()) + "]"; }
    }

    internal sealed class CyberReachabilityResult
    {
        public int Horizon { get; set; }
        public bool InvariantWithinHorizon { get; set; }
        public int FirstUnsafeStep { get; set; }
        public List<CyberReachabilityStep> Steps { get; set; }
        public List<string> Audit { get; set; }
        public CyberReachabilityResult() { InvariantWithinHorizon = true; FirstUnsafeStep = -1; Steps = new List<CyberReachabilityStep>(); Audit = new List<string>(); }
        public override string ToString() { return (InvariantWithinHorizon ? "BOUNDED SAFE ENVELOPE" : "ENVELOPE ESCAPE @ " + FirstUnsafeStep) + "   ·   horizon " + Horizon.ToString(CultureInfo.InvariantCulture) + "   ·   interval over-approximation"; }
    }

    internal sealed class CyberRobustnessCase
    {
        public int Case { get; set; }
        public double SpectralRadius { get; set; }
        public bool Stable { get; set; }
        public double Cost { get; set; }
        public int Violations { get; set; }
        public double MaximumStateExcursion { get; set; }
        public override string ToString() { return "CASE " + Case.ToString("D4", CultureInfo.InvariantCulture) + "   ·   " + (Stable ? "STABLE" : "UNSTABLE") + "   ·   ρ " + SpectralRadius.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   cost " + Cost.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   violations " + Violations + "   ·   excursion " + MaximumStateExcursion.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberRobustnessEnvelope
    {
        public int Cases { get; set; }
        public double StableFraction { get; set; }
        public double ConstraintCleanFraction { get; set; }
        public double CostP05 { get; set; }
        public double CostP50 { get; set; }
        public double CostP95 { get; set; }
        public double WorstSpectralRadius { get; set; }
        public string Fingerprint { get; set; }
        public List<CyberRobustnessCase> RetainedCases { get; set; }
        public List<string> Audit { get; set; }
        public CyberRobustnessEnvelope() { RetainedCases = new List<CyberRobustnessCase>(); Audit = new List<string>(); }
        public override string ToString() { return Cases + " cases   ·   stable " + StableFraction.ToString("P1", CultureInfo.InvariantCulture) + "   ·   constraint-clean " + ConstraintCleanFraction.ToString("P1", CultureInfo.InvariantCulture) + "   ·   cost P05/P50/P95 " + CostP05.ToString("0.###", CultureInfo.InvariantCulture) + "/" + CostP50.ToString("0.###", CultureInfo.InvariantCulture) + "/" + CostP95.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   worst ρ " + WorstSpectralRadius.ToString("0.000000", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberSensorSelection
    {
        public double Budget { get; set; }
        public double UsedBudget { get; set; }
        public int ObservabilityRank { get; set; }
        public double InformationLogDet { get; set; }
        public List<string> OutputIds { get; set; }
        public List<string> MarginalTrace { get; set; }
        public CyberSensorSelection() { OutputIds = new List<string>(); MarginalTrace = new List<string>(); }
        public override string ToString() { return OutputIds.Count + " sensors   ·   budget " + UsedBudget.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Budget.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   rank " + ObservabilityRank + "   ·   logdet information " + InformationLogDet.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberActuatorAllocation
    {
        public bool Converged { get; set; }
        public int Iterations { get; set; }
        public double[] Input { get; set; }
        public double[] AchievedStateDelta { get; set; }
        public double ResidualNorm { get; set; }
        public List<string> SaturatedInputIds { get; set; }
        public List<string> Audit { get; set; }
        public CyberActuatorAllocation() { Input = new double[0]; AchievedStateDelta = new double[0]; SaturatedInputIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return (Converged ? "CONVERGED" : "BOUNDED") + "   ·   residual " + ResidualNorm.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   u=[" + String.Join(", ", Input.Select(x => x.ToString("0.######", CultureInfo.InvariantCulture)).ToArray()) + "]   ·   saturated " + SaturatedInputIds.Count; }
    }

    internal sealed class CyberFaultScore
    {
        public string FaultId { get; set; }
        public string FaultName { get; set; }
        public double LogPosterior { get; set; }
        public double Posterior { get; set; }
        public double Mahalanobis { get; set; }
        public override string ToString() { return (FaultName ?? FaultId ?? "FAULT") + "   ·   posterior " + Posterior.ToString("P3", CultureInfo.InvariantCulture) + "   ·   χ² " + Mahalanobis.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   log score " + LogPosterior.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class CyberFaultDiagnosis
    {
        public string RecommendedFaultId { get; set; }
        public double ResidualEnergy { get; set; }
        public List<CyberFaultScore> Scores { get; set; }
        public List<string> Audit { get; set; }
        public CyberFaultDiagnosis() { Scores = new List<CyberFaultScore>(); Audit = new List<string>(); }
        public override string ToString() { return "FAULT POSTERIOR · " + (RecommendedFaultId ?? "UNRESOLVED") + "   ·   residual energy " + ResidualEnergy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   " + Scores.Count + " signatures"; }
    }

    internal static class CyberneticsKernel
    {
        private const double Epsilon = 1e-10;

        public static CyberStructuralAnalysis AnalyzeStructure(CyberTwinModel model)
        {
            CyberStructuralAnalysis r = new CyberStructuralAnalysis();
            if (model == null) { r.Audit.Add("BLOCKER · NULL_TWIN"); return r; }
            int n = ActiveStates(model).Count, m = ActiveInputs(model).Count, p = ActiveOutputs(model).Count;
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d);
            r.States = n; r.Inputs = m; r.Outputs = p;
            double[,] controllability = Controllability(a, b);
            double[,] observability = Observability(a, c);
            r.ControllabilityRank = Rank(controllability);
            r.ObservabilityRank = Rank(observability);
            r.Controllable = n > 0 && r.ControllabilityRank == n;
            r.Observable = n > 0 && r.ObservabilityRank == n;
            r.PoleMagnitudes = EigenMagnitudes(a);
            r.SpectralRadius = r.PoleMagnitudes.DefaultIfEmpty(0).Max();
            r.Stable = r.SpectralRadius < 1 - 1e-8;
            double[,] wc = Gramian(a, b, false, 256), wo = Gramian(a, c, true, 256);
            r.ControllabilityGramianLogDet = LogDetRegularized(wc, 1e-12);
            r.ObservabilityGramianLogDet = LogDetRegularized(wo, 1e-12);
            r.ControllabilityCondition = ConditionEstimate(wc);
            r.ObservabilityCondition = ConditionEstimate(wo);
            List<CyberStateVariable> states = ActiveStates(model);
            for (int i = 0; i < n; i++) if (wc[i, i] < 1e-8 || wo[i, i] < 1e-8) r.WeakStateIds.Add(states[i].Id);
            if (n == 0) r.Audit.Add("BLOCKER · NO ACTIVE STATES");
            if (m == 0) r.Audit.Add("BLOCKER · NO ACTIVE CONTROL INPUTS");
            if (p == 0) r.Audit.Add("BLOCKER · NO ACTIVE MEASURED OUTPUTS");
            if (!r.Controllable) r.Audit.Add("WARN · UNCONTROLLABLE SUBSPACE DIMENSION " + Math.Max(0, n - r.ControllabilityRank));
            if (!r.Observable) r.Audit.Add("WARN · UNOBSERVABLE SUBSPACE DIMENSION " + Math.Max(0, n - r.ObservabilityRank));
            if (!r.Stable) r.Audit.Add("WARN · OPEN-LOOP DISCRETE SPECTRAL RADIUS IS NOT INSIDE THE UNIT CIRCLE");
            if (r.WeakStateIds.Count > 0) r.Audit.Add("WARN · WEAK GRAMIAN DIAGONALS · " + String.Join(", ", r.WeakStateIds.ToArray()));
            r.Audit.Add("Structural ranks and Gramian metrics are numerical properties of the declared linear abstraction, not physical proof.");
            return r;
        }

        public static CyberSimulationResult Simulate(CyberTwinModel model, double[] initial, IList<double[]> controls)
        {
            CyberSimulationResult result = new CyberSimulationResult { TwinFingerprint = Fingerprint(model), ControllerKind = "DECLARED_SEQUENCE" };
            if (model == null) { result.Audit.Add("BLOCKER · NULL_TWIN"); return result; }
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d);
            int n = a.GetLength(0), m = b.GetLength(1), horizon = controls == null ? 0 : controls.Count;
            double[] x = Vector(initial, n, ActiveStates(model).Select(s => s.InitialValue).ToArray()), previous = new double[m];
            for (int k = 0; k < horizon; k++)
            {
                double[] u = ClampInput(model, Vector(controls[k], m, null), previous);
                double[] y = Add(Multiply(c, x), Multiply(d, u));
                CyberSimulationStep step = Step(model, k, x, u, y);
                result.Steps.Add(step); result.TotalCost += step.StageCost; result.ViolationCount += step.Violations.Count;
                x = Add(Multiply(a, x), Multiply(b, u)); previous = u;
            }
            double[] terminalInput = new double[m], finalY = Add(Multiply(c, x), Multiply(d, terminalInput));
            CyberSimulationStep terminal = Step(model, horizon, x, terminalInput, finalY);
            result.Steps.Add(terminal); result.TotalCost += terminal.StageCost; result.ViolationCount += terminal.Violations.Count;
            result.Audit.Add("Simulation is deterministic propagation of the declared linear abstraction; no live plant or actuator was contacted.");
            return result;
        }

        public static CyberKalmanResult Estimate(CyberTwinModel model, IList<double[]> controls, IList<double[]> observations, double[] initialEstimate, double covarianceScale)
        {
            CyberKalmanResult result = new CyberKalmanResult { TwinFingerprint = Fingerprint(model) };
            if (model == null) { result.Audit.Add("BLOCKER · NULL_TWIN"); return result; }
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d);
            int n = a.GetLength(0), m = b.GetLength(1), p = c.GetLength(0), horizon = observations == null ? 0 : observations.Count;
            double[] x = Vector(initialEstimate, n, ActiveStates(model).Select(s => s.InitialValue).ToArray());
            double[,] covariance = Scale(Identity(n), Math.Max(Epsilon, covarianceScale));
            double[,] q = Diagonal(ActiveStates(model).Select(s => Math.Max(Epsilon, s.ProcessNoiseVariance)).ToArray());
            double[,] r = Diagonal(ActiveOutputs(model).Select(y => Math.Max(Epsilon, y.MeasurementNoiseVariance)).ToArray());
            for (int k = 0; k < horizon; k++)
            {
                double[] u = controls != null && k < controls.Count ? Vector(controls[k], m, null) : new double[m];
                double[] prior = Add(Multiply(a, x), Multiply(b, u));
                double[,] priorP = Add(Multiply(Multiply(a, covariance), Transpose(a)), q);
                double[] observed = Vector(observations[k], p, null);
                double[] innovation = Subtract(observed, Add(Multiply(c, prior), Multiply(d, u)));
                double[,] innovationCovariance = Add(Multiply(Multiply(c, priorP), Transpose(c)), r);
                double[,] inverseS; if (!TryInverse(innovationCovariance, out inverseS)) { inverseS = PseudoInverseRegularized(innovationCovariance, 1e-8); result.Audit.Add("Innovation covariance required regularized inverse at step " + k); }
                double[,] gain = Multiply(Multiply(priorP, Transpose(c)), inverseS);
                x = Add(prior, Multiply(gain, innovation));
                double[,] joseph = Subtract(Identity(n), Multiply(gain, c));
                covariance = Add(Multiply(Multiply(joseph, priorP), Transpose(joseph)), Multiply(Multiply(gain, r), Transpose(gain)));
                covariance = Scale(Add(covariance, Transpose(covariance)), 0.5);
                double mahal = Dot(innovation, Multiply(inverseS, innovation));
                double logDet = LogDetRegularized(innovationCovariance, 1e-12);
                result.LogLikelihood += -0.5 * (p * Math.Log(2 * Math.PI) + logDet + mahal);
                result.Steps.Add(new CyberKalmanStep { Step = k, PriorState = prior, PosteriorState = (double[])x.Clone(), Innovation = innovation, InnovationMahalanobis = mahal, CovarianceTrace = Trace(covariance) });
            }
            result.FinalCovarianceTrace = Trace(covariance);
            result.Audit.Add("Kalman estimates assume the declared linear-Gaussian process and measurement covariances; they are not observed truth.");
            return result;
        }

        public static CyberControllerSynthesis SynthesizeLqr(CyberTwinModel model, int maximumIterations, double tolerance)
        {
            CyberControllerSynthesis result = new CyberControllerSynthesis { Kind = "DISCRETE_LQR" };
            if (model == null) { result.Audit.Add("BLOCKER · NULL_TWIN"); return result; }
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d);
            int n = a.GetLength(0), m = b.GetLength(1); maximumIterations = Math.Max(8, Math.Min(4096, maximumIterations)); tolerance = Math.Max(1e-14, tolerance);
            double[,] q = Diagonal(ActiveStates(model).Select(s => Math.Max(Epsilon, s.RegulationWeight)).ToArray());
            double[,] r = Diagonal(ActiveInputs(model).Select(u => Math.Max(Epsilon, u.EffortWeight)).ToArray());
            double[,] p = Copy(q), kGain = new double[m, n];
            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                double[,] inverse; double[,] normal = Add(r, Multiply(Multiply(Transpose(b), p), b));
                if (!TryInverse(normal, out inverse)) inverse = PseudoInverseRegularized(normal, 1e-9);
                kGain = Multiply(Multiply(Multiply(inverse, Transpose(b)), p), a);
                double[,] next = Add(q, Subtract(Multiply(Multiply(Transpose(a), p), a), Multiply(Multiply(Multiply(Multiply(Transpose(a), p), b), inverse), Multiply(Transpose(b), Multiply(p, a)))));
                result.RiccatiResidual = Frobenius(Subtract(next, p)); result.Iterations = iteration + 1; p = Scale(Add(next, Transpose(next)), 0.5);
                if (result.RiccatiResidual <= tolerance) { result.Converged = true; break; }
            }
            result.Gain = Jagged(kGain);
            result.ClosedLoopSpectralRadius = EigenMagnitudes(Subtract(a, Multiply(b, kGain))).DefaultIfEmpty(0).Max();
            if (m == 0 || n == 0) result.Audit.Add("LQR synthesis requires active states and control inputs.");
            if (result.ClosedLoopSpectralRadius >= 1) result.Audit.Add("Synthesized gain does not stabilize every declared discrete mode.");
            result.Audit.Add("LQR gain is a model-bound design artifact and has no actuator authority.");
            return result;
        }

        public static CyberMpcResult SolveMpc(CyberTwinModel model, double[] initial, int horizon, int maximumIterations)
        {
            CyberMpcResult result = new CyberMpcResult();
            if (model == null) { result.Audit.Add("BLOCKER · NULL_TWIN"); return result; }
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d);
            int n = a.GetLength(0), m = b.GetLength(1), p = c.GetLength(0); horizon = Math.Max(1, Math.Min(256, horizon)); maximumIterations = Math.Max(1, Math.Min(2048, maximumIterations)); result.Horizon = horizon;
            double[] x0 = Vector(initial, n, ActiveStates(model).Select(s => s.InitialValue).ToArray());
            List<CyberControlInput> inputs = ActiveInputs(model); List<CyberMeasuredOutput> outputs = ActiveOutputs(model);
            double[][] u = Enumerable.Range(0, horizon).Select(k => new double[m]).ToArray();
            double initialCost; double[][] initialGradient; MpcCostGradient(model, a, b, c, d, x0, u, out initialCost, out initialGradient); result.InitialCost = initialCost;
            double previousCost = initialCost, step = 0.08;
            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                double cost; double[][] gradient; MpcCostGradient(model, a, b, c, d, x0, u, out cost, out gradient); double norm = Math.Sqrt(gradient.Sum(row => row.Sum(v => v * v))); result.GradientNorm = norm; result.Iterations = iteration + 1;
                if (norm < 1e-7) { result.Converged = true; previousCost = cost; break; }
                double[][] candidate = u.Select(row => (double[])row.Clone()).ToArray();
                for (int k = 0; k < horizon; k++) for (int j = 0; j < m; j++) candidate[k][j] -= step * gradient[k][j];
                ProjectControlSequence(model, candidate, inputs);
                double candidateCost; double[][] ignored; MpcCostGradient(model, a, b, c, d, x0, candidate, out candidateCost, out ignored);
                if (candidateCost <= cost) { u = candidate; previousCost = candidateCost; step = Math.Min(0.5, step * 1.04); if (Math.Abs(cost - candidateCost) < 1e-9) { result.Converged = true; break; } }
                else step *= 0.5;
                if (step < 1e-12) break;
            }
            result.FinalCost = previousCost; result.ControlSequence = u.Select(row => (double[])row.Clone()).ToList(); result.Prediction = Simulate(model, x0, result.ControlSequence); result.Prediction.ControllerKind = "CONSTRAINED_MPC_PREDICTION";
            result.Audit.Add("Projected-gradient MPC respects declared input and slew boxes; plant execution remains outside this subsystem.");
            return result;
        }

        public static CyberControllerSynthesis TunePid(CyberTwinModel model, int inputIndex, int outputIndex, int steps)
        {
            CyberControllerSynthesis result = new CyberControllerSynthesis { Kind = "PID_IMC" };
            if (model == null) { result.Audit.Add("BLOCKER · NULL_TWIN"); return result; }
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d); int n = a.GetLength(0), m = b.GetLength(1), p = c.GetLength(0); steps = Math.Max(16, Math.Min(4096, steps));
            if (inputIndex < 0 || inputIndex >= m || outputIndex < 0 || outputIndex >= p) { result.Audit.Add("Input/output index is outside the active twin coordinates."); return result; }
            double[] x = new double[n], u = new double[m]; u[inputIndex] = 1; double[] response = new double[steps];
            for (int k = 0; k < steps; k++) { response[k] = Add(Multiply(c, x), Multiply(d, u))[outputIndex]; x = Add(Multiply(a, x), Multiply(b, u)); }
            double baseline = response[0], steady = response.Skip(Math.Max(0, steps - Math.Max(4, steps / 10))).Average(), gain = steady - baseline; result.ProcessGain = gain;
            if (Math.Abs(gain) <= Epsilon) { result.Audit.Add("Step response has negligible steady gain; PID identification is unresolved."); return result; }
            double y28 = baseline + 0.283 * gain, y63 = baseline + 0.632 * gain; int k28 = FirstCrossing(response, y28, gain > 0), k63 = FirstCrossing(response, y63, gain > 0); double dt = Math.Max(Epsilon, model.SamplePeriod); double tau = Math.Max(dt, 1.5 * (k63 - k28) * dt), dead = Math.Max(0, (k63 * dt) - tau); double lambda = Math.Max(dt, Math.Max(dead, tau * 0.5));
            result.TimeConstant = tau; result.DeadTime = dead; result.Kp = tau / (gain * (lambda + dead)); result.Ki = result.Kp / Math.Max(dt, tau + dead * 0.5); result.Kd = result.Kp * (tau * dead / Math.Max(dt, 2 * tau + dead)); result.Converged = true; result.Iterations = steps;
            result.Audit.Add("PID values use a bounded FOPDT/IMC approximation of one declared model channel; validate independently before any implementation.");
            return result;
        }

        public static CyberReachabilityResult AnalyzeReachability(CyberTwinModel model, int horizon)
        {
            CyberReachabilityResult result = new CyberReachabilityResult(); if (model == null) return result;
            double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d); List<CyberStateVariable> states = ActiveStates(model); List<CyberControlInput> inputs = ActiveInputs(model); int n = states.Count, m = inputs.Count; horizon = Math.Max(1, Math.Min(2048, horizon)); result.Horizon = horizon;
            double[] center = states.Select(s => s.InitialValue).ToArray(), radius = states.Select(s => Math.Max(Epsilon, Math.Min(Math.Abs(s.Maximum - s.InitialValue), Math.Abs(s.InitialValue - s.Minimum)) * 0.01)).ToArray();
            double[] inputCenter = inputs.Select(u => (u.Minimum + u.Maximum) * 0.5).ToArray(), inputRadius = inputs.Select(u => Math.Max(0, (u.Maximum - u.Minimum) * 0.5)).ToArray(); double[,] absA = Absolute(a), absB = Absolute(b);
            for (int k = 0; k <= horizon; k++)
            {
                CyberReachabilityStep row = new CyberReachabilityStep { Step = k, Center = (double[])center.Clone(), Radius = (double[])radius.Clone() };
                for (int i = 0; i < n; i++) { double declaredLow, declaredHigh; HardBounds(model, "STATE", states[i].Id, states[i].Minimum, states[i].Maximum, out declaredLow, out declaredHigh); double low = center[i] - radius[i], high = center[i] + radius[i]; if (low < declaredLow - Epsilon || high > declaredHigh + Epsilon) { row.Safe = false; row.Violations.Add(states[i].Id + " envelope [" + low.ToString("R", CultureInfo.InvariantCulture) + "," + high.ToString("R", CultureInfo.InvariantCulture) + "] outside [" + declaredLow.ToString("R", CultureInfo.InvariantCulture) + "," + declaredHigh.ToString("R", CultureInfo.InvariantCulture) + "]"); } }
                result.Steps.Add(row); if (!row.Safe && result.FirstUnsafeStep < 0) { result.FirstUnsafeStep = k; result.InvariantWithinHorizon = false; }
                center = Add(Multiply(a, center), Multiply(b, inputCenter)); radius = Add(Multiply(absA, radius), Multiply(absB, inputRadius));
            }
            result.Audit.Add("Interval propagation is a conservative box over-approximation and may include unreachable combinations."); return result;
        }

        public static CyberRobustnessEnvelope AnalyzeRobustness(CyberTwinModel model, int cases, int horizon, int seed)
        {
            CyberRobustnessEnvelope result = new CyberRobustnessEnvelope(); if (model == null) return result; cases = Math.Max(16, Math.Min(4096, cases)); horizon = Math.Max(4, Math.Min(512, horizon)); result.Cases = cases;
            double[,] nominalA, nominalB, c, d; Canonical(model, out nominalA, out nominalB, out c, out d); CyberControllerSynthesis synthesis = SynthesizeLqr(model, 1024, 1e-10); double[,] gain = Rectangular(synthesis.Gain, nominalB.GetLength(1), nominalA.GetLength(0)); double uncertainty = Math.Max(0, Math.Min(1, model.ParameterUncertaintyFraction)); List<CyberStateVariable> states = ActiveStates(model); List<double> costs = new List<double>(); int stable = 0, clean = 0; double worstSpectralRadius = 0;
            for (int scenario = 0; scenario < cases; scenario++)
            {
                double[,] a = Copy(nominalA), b = Copy(nominalB); int coordinate = 1;
                for (int i = 0; i < a.GetLength(0); i++) for (int j = 0; j < a.GetLength(1); j++) a[i, j] *= 1 + uncertainty * (2 * RadicalInverse(37 + scenario + Math.Abs(seed % 97), Prime(coordinate++)) - 1);
                for (int i = 0; i < b.GetLength(0); i++) for (int j = 0; j < b.GetLength(1); j++) b[i, j] *= 1 + uncertainty * (2 * RadicalInverse(37 + scenario + Math.Abs(seed % 97), Prime(coordinate++)) - 1);
                double[,] closed = Subtract(a, Multiply(b, gain)); double rho = EigenMagnitudes(closed).DefaultIfEmpty(0).Max(); worstSpectralRadius = Math.Max(worstSpectralRadius, rho); if (rho < 1) stable++;
                double[] x = new double[states.Count]; for (int i = 0; i < x.Length; i++) { double span = Math.Max(Epsilon, states[i].Maximum - states[i].Minimum); x[i] = Clamp(states[i].InitialValue + (RadicalInverse(37 + scenario, Prime(coordinate++)) - 0.5) * span * 0.2, states[i].Minimum, states[i].Maximum); }
                double cost = 0, maxExcursion = 0; int violations = 0; double[] previousInput = new double[nominalB.GetLength(1)];
                for (int k = 0; k < horizon; k++) { double[] u = Scale(Multiply(gain, x), -1); u = ClampInput(model, u, previousInput); double[] y = Add(Multiply(c, x), Multiply(d, u)); CyberSimulationStep row = Step(model, k, x, u, y); cost += row.StageCost; violations += row.Violations.Count; maxExcursion = Math.Max(maxExcursion, x.Select(Math.Abs).DefaultIfEmpty(0).Max()); x = Add(Multiply(a, x), Multiply(b, u)); previousInput = u; }
                if (violations == 0) clean++; costs.Add(cost); CyberRobustnessCase retained = new CyberRobustnessCase { Case = scenario, SpectralRadius = rho, Stable = rho < 1, Cost = cost, Violations = violations, MaximumStateExcursion = maxExcursion }; if (scenario < 64 || !retained.Stable || violations > 0) result.RetainedCases.Add(retained);
            }
            List<double> sorted = costs.OrderBy(x => x).ToList(); result.StableFraction = (double)stable / cases; result.ConstraintCleanFraction = (double)clean / cases; result.CostP05 = Quantile(sorted, 0.05); result.CostP50 = Quantile(sorted, 0.50); result.CostP95 = Quantile(sorted, 0.95); result.WorstSpectralRadius = worstSpectralRadius; result.Fingerprint = HashText(Fingerprint(model) + "|" + cases + "|" + horizon + "|" + seed + "|" + result.CostP95.ToString("R", CultureInfo.InvariantCulture)); result.Audit.Add("Robustness cases are deterministic stress coordinates over declared parameter and initial-state boxes, not calibrated probabilities."); return result;
        }

        public static CyberSensorSelection SelectSensors(CyberTwinModel model, double budget)
        {
            CyberSensorSelection result = new CyberSensorSelection { Budget = Math.Max(0, budget) }; if (model == null) return result; double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d); List<CyberMeasuredOutput> outputs = ActiveOutputs(model); List<int> chosen = new List<int>(); double remaining = result.Budget;
            while (true)
            {
                int best = -1, bestRank = -1; double bestLogDet = Double.NegativeInfinity;
                for (int candidate = 0; candidate < outputs.Count; candidate++)
                {
                    if (chosen.Contains(candidate) || outputs[candidate].SensorCost > remaining + Epsilon) continue; List<int> trial = new List<int>(chosen) { candidate }; double[,] selectedC = SelectRows(c, trial); double[,] o = Observability(a, selectedC); int rank = Rank(o); double logdet = LogDetRegularized(Multiply(Transpose(o), o), 1e-10); if (rank > bestRank || (rank == bestRank && logdet > bestLogDet)) { best = candidate; bestRank = rank; bestLogDet = logdet; }
                }
                if (best < 0) break; chosen.Add(best); remaining -= outputs[best].SensorCost; result.MarginalTrace.Add(outputs[best].Name + " · rank " + bestRank + " · logdet " + bestLogDet.ToString("0.######", CultureInfo.InvariantCulture)); if (bestRank >= a.GetLength(0) && remaining <= outputs.Where((x, i) => !chosen.Contains(i)).Select(x => x.SensorCost).DefaultIfEmpty(Double.MaxValue).Min()) break;
            }
            result.OutputIds = chosen.Select(i => outputs[i].Id).ToList(); result.UsedBudget = result.Budget - remaining; double[,] finalO = Observability(a, SelectRows(c, chosen)); result.ObservabilityRank = Rank(finalO); result.InformationLogDet = LogDetRegularized(Multiply(Transpose(finalO), finalO), 1e-10); return result;
        }

        public static CyberActuatorAllocation AllocateActuators(CyberTwinModel model, double[] desiredStateDelta, int maximumIterations)
        {
            CyberActuatorAllocation result = new CyberActuatorAllocation(); if (model == null) return result; double[,] a, b, c, d; Canonical(model, out a, out b, out c, out d); int n = b.GetLength(0), m = b.GetLength(1); double[] target = Vector(desiredStateDelta, n, null), u = new double[m]; List<CyberControlInput> inputs = ActiveInputs(model); maximumIterations = Math.Max(8, Math.Min(4096, maximumIterations)); double lipschitz = 2 * Math.Max(Epsilon, Frobenius(b) * Frobenius(b) + 0.001), alpha = 1 / lipschitz;
            for (int iteration = 0; iteration < maximumIterations; iteration++) { double[] residual = Subtract(Multiply(b, u), target); double[] gradient = Add(Scale(Multiply(Transpose(b), residual), 2), Scale(u, 0.002)); double norm = Norm(gradient); result.Iterations = iteration + 1; if (norm < 1e-8) { result.Converged = true; break; } for (int j = 0; j < m; j++) u[j] = Clamp(u[j] - alpha * gradient[j], inputs[j].Minimum, inputs[j].Maximum); }
            result.Input = u; result.AchievedStateDelta = Multiply(b, u); result.ResidualNorm = Norm(Subtract(result.AchievedStateDelta, target)); for (int j = 0; j < m; j++) if (Math.Abs(u[j] - inputs[j].Minimum) < 1e-8 || Math.Abs(u[j] - inputs[j].Maximum) < 1e-8) result.SaturatedInputIds.Add(inputs[j].Id); result.Audit.Add("Allocation is a bounded least-squares design calculation and sends no actuator command."); return result;
        }

        public static CyberFaultDiagnosis DiagnoseFault(CyberTwinModel model, IDictionary<string, double> residuals)
        {
            CyberFaultDiagnosis result = new CyberFaultDiagnosis(); if (model == null) return result; List<CyberMeasuredOutput> outputs = ActiveOutputs(model); double energy = 0; foreach (CyberMeasuredOutput y in outputs) { double value = Value(residuals, y.Id); energy += value * value / Math.Max(Epsilon, y.MeasurementNoiseVariance); } result.ResidualEnergy = energy; List<CyberFaultSignature> signatures = (model.FaultSignatures ?? new List<CyberFaultSignature>()).Where(x => Active(x.Status)).ToList(); if (signatures.Count == 0) { result.Audit.Add("No active fault signatures are declared."); return result; }
            double maxLog = Double.NegativeInfinity; foreach (CyberFaultSignature signature in signatures) { double mahal = 0, logNormalizer = 0; foreach (CyberMeasuredOutput y in outputs) { double mean = Value(signature.ResidualMeans, y.Id), variance = Math.Max(Epsilon, Value(signature.ResidualVariances, y.Id, y.MeasurementNoiseVariance)), delta = Value(residuals, y.Id) - mean; mahal += delta * delta / variance; logNormalizer += Math.Log(2 * Math.PI * variance); } double score = Math.Log(Math.Max(Epsilon, signature.PriorWeight)) - 0.5 * (mahal + logNormalizer); result.Scores.Add(new CyberFaultScore { FaultId = signature.Id, FaultName = signature.Name, LogPosterior = score, Mahalanobis = mahal }); maxLog = Math.Max(maxLog, score); }
            double normalizer = result.Scores.Sum(x => Math.Exp(x.LogPosterior - maxLog)); foreach (CyberFaultScore score in result.Scores) score.Posterior = Math.Exp(score.LogPosterior - maxLog) / Math.Max(Epsilon, normalizer); result.Scores = result.Scores.OrderByDescending(x => x.Posterior).ToList(); result.RecommendedFaultId = result.Scores.First().FaultId; result.Audit.Add("Fault posteriors are conditional on declared residual signatures, priors, and diagonal covariance assumptions."); return result;
        }

        public static List<string> Audit(CyberTwinModel model)
        {
            List<string> findings = new List<string>(); if (model == null) { findings.Add("BLOCKER · NULL_TWIN"); return findings; } List<CyberStateVariable> states = ActiveStates(model); List<CyberControlInput> inputs = ActiveInputs(model); List<CyberMeasuredOutput> outputs = ActiveOutputs(model); int n = states.Count, m = inputs.Count, p = outputs.Count;
            if (n == 0) findings.Add("BLOCKER · NO_ACTIVE_STATES"); if (m == 0) findings.Add("BLOCKER · NO_ACTIVE_INPUTS"); if (p == 0) findings.Add("BLOCKER · NO_ACTIVE_OUTPUTS"); if (model.SamplePeriod <= 0) findings.Add("BLOCKER · NONPOSITIVE_SAMPLE_PERIOD");
            AuditMatrix(findings, "A", model.A, n, n); AuditMatrix(findings, "B", model.B, n, m); AuditMatrix(findings, "C", model.C, p, n); AuditMatrix(findings, "D", model.D, p, m);
            foreach (CyberStateVariable state in states) { if (state.Minimum > state.Maximum) findings.Add("BLOCKER · INVALID_STATE_RANGE · " + state.Id); if (state.ProcessNoiseVariance < 0) findings.Add("BLOCKER · NEGATIVE_PROCESS_VARIANCE · " + state.Id); }
            foreach (CyberControlInput input in inputs) { if (input.Minimum > input.Maximum || input.MinimumSlew > input.MaximumSlew) findings.Add("BLOCKER · INVALID_INPUT_RANGE · " + input.Id); if (Math.Max(input.Minimum, input.MinimumSlew) > Math.Min(input.Maximum, input.MaximumSlew)) findings.Add("BLOCKER · EMPTY_INITIAL_INPUT_SLEW_INTERSECTION · " + input.Id); if (input.EffortWeight < 0) findings.Add("BLOCKER · NEGATIVE_INPUT_WEIGHT · " + input.Id); }
            foreach (CyberMeasuredOutput output in outputs) { if (output.Minimum > output.Maximum) findings.Add("BLOCKER · INVALID_OUTPUT_RANGE · " + output.Id); if (output.MeasurementNoiseVariance <= 0) findings.Add("BLOCKER · NONPOSITIVE_MEASUREMENT_VARIANCE · " + output.Id); }
            HashSet<string> ids = new HashSet<string>(states.Select(x => x.Id).Concat(inputs.Select(x => x.Id)).Concat(outputs.Select(x => x.Id)), StringComparer.OrdinalIgnoreCase); foreach (CyberConstraint c in model.Constraints ?? new List<CyberConstraint>()) { if (!ids.Contains(c.VariableId ?? "")) findings.Add("WARN · ORPHAN_CONSTRAINT · " + c.Id); if (c.Minimum > c.Maximum) findings.Add("BLOCKER · INVALID_CONSTRAINT_RANGE · " + c.Id); double low, high; if (Eq(c.VariableKind, "INPUT")) { CyberControlInput u = inputs.FirstOrDefault(x => Eq(x.Id, c.VariableId)); if (u != null) { HardBounds(model, "INPUT", u.Id, u.Minimum, u.Maximum, out low, out high); if (low > high) findings.Add("BLOCKER · EMPTY_HARD_CONSTRAINT_INTERSECTION · " + c.Id); } } else if (Eq(c.VariableKind, "OUTPUT")) { CyberMeasuredOutput y = outputs.FirstOrDefault(x => Eq(x.Id, c.VariableId)); if (y != null) { HardBounds(model, "OUTPUT", y.Id, y.Minimum, y.Maximum, out low, out high); if (low > high) findings.Add("BLOCKER · EMPTY_HARD_CONSTRAINT_INTERSECTION · " + c.Id); } } else { CyberStateVariable x = states.FirstOrDefault(q => Eq(q.Id, c.VariableId)); if (x != null) { HardBounds(model, "STATE", x.Id, x.Minimum, x.Maximum, out low, out high); if (low > high) findings.Add("BLOCKER · EMPTY_HARD_CONSTRAINT_INTERSECTION · " + c.Id); } } }
            CyberStructuralAnalysis structure = AnalyzeStructure(model); if (!structure.Controllable) findings.Add("WARN · TWIN_NOT_FULLY_CONTROLLABLE"); if (!structure.Observable) findings.Add("WARN · TWIN_NOT_FULLY_OBSERVABLE"); if (findings.Count == 0) findings.Add("PASS · STRUCTURAL_CYBERNETICS_AUDIT · dimensions, ranges, covariances, references, and authority boundaries are internally coherent."); return findings;
        }

        public static string Fingerprint(CyberTwinModel model)
        {
            if (model == null) return HashText("NULL_CYBER_TWIN"); StringBuilder s = new StringBuilder(); s.Append("CYBERNETIC_TWIN_V1|").Append(model.Id).Append('|').Append(model.ParentTwinId).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.Name).Append('|').Append(model.Charter).Append('|').Append(model.Status).Append('|').Append(model.DynamicsKind).Append('|').Append(model.Revision).Append('|').Append(model.SamplePeriod.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(model.ParameterUncertaintyFraction.ToString("R", CultureInfo.InvariantCulture));
            foreach (CyberStateVariable x in (model.States ?? new List<CyberStateVariable>()).OrderBy(x => x.Id)) { s.Append("|X:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Unit).Append(':').Append(x.Status).Append(':').Append(x.InitialValue.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Minimum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Maximum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ProcessNoiseVariance.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.RegulationWeight.ToString("R", CultureInfo.InvariantCulture)); AppendEvidence(s, x.EvidenceNodeIds); }
            foreach (CyberControlInput u in (model.Inputs ?? new List<CyberControlInput>()).OrderBy(x => x.Id)) { s.Append("|U:").Append(u.Id).Append(':').Append(u.Name).Append(':').Append(u.Unit).Append(':').Append(u.Status).Append(':').Append(u.Minimum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(u.Maximum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(u.MinimumSlew.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(u.MaximumSlew.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(u.EffortWeight.ToString("R", CultureInfo.InvariantCulture)); AppendEvidence(s, u.EvidenceNodeIds); }
            foreach (CyberMeasuredOutput y in (model.Outputs ?? new List<CyberMeasuredOutput>()).OrderBy(x => x.Id)) { s.Append("|Y:").Append(y.Id).Append(':').Append(y.Name).Append(':').Append(y.Unit).Append(':').Append(y.Status).Append(':').Append(y.Minimum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(y.Maximum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(y.Target.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(y.MeasurementNoiseVariance.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(y.TrackingWeight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(y.SensorCost.ToString("R", CultureInfo.InvariantCulture)); AppendEvidence(s, y.EvidenceNodeIds); }
            AppendMatrix(s, "A", model.A); AppendMatrix(s, "B", model.B); AppendMatrix(s, "C", model.C); AppendMatrix(s, "D", model.D);
            foreach (CyberConstraint c in (model.Constraints ?? new List<CyberConstraint>()).OrderBy(x => x.Id)) s.Append("|K:").Append(c.Id).Append(':').Append(c.Name).Append(':').Append(c.Status).Append(':').Append(c.VariableKind).Append(':').Append(c.VariableId).Append(':').Append(c.Minimum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Maximum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(c.Hard).Append(':').Append(c.Penalty.ToString("R", CultureInfo.InvariantCulture));
            foreach (CyberFaultSignature f in (model.FaultSignatures ?? new List<CyberFaultSignature>()).OrderBy(x => x.Id)) { s.Append("|F:").Append(f.Id).Append(':').Append(f.Name).Append(':').Append(f.Status).Append(':').Append(f.Description).Append(':').Append(f.PriorWeight.ToString("R", CultureInfo.InvariantCulture)); foreach (KeyValuePair<string, double> x in (f.ResidualMeans ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append(":MEAN:").Append(x.Key).Append('=').Append(x.Value.ToString("R", CultureInfo.InvariantCulture)); foreach (KeyValuePair<string, double> x in (f.ResidualVariances ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append(":VAR:").Append(x.Key).Append('=').Append(x.Value.ToString("R", CultureInfo.InvariantCulture)); foreach (string e in (f.EvidenceNodeIds ?? new List<string>()).OrderBy(x => x)) s.Append(":EVIDENCE:").Append(e); }
            foreach (string a in (model.Assumptions ?? new List<string>()).OrderBy(x => x)) s.Append("|ASSUME:").Append(a); foreach (string e in (model.EvidenceNodeIds ?? new List<string>()).OrderBy(x => x)) s.Append("|EVIDENCE:").Append(e); return HashText(s.ToString());
        }

        private static void Canonical(CyberTwinModel model, out double[,] a, out double[,] b, out double[,] c, out double[,] d)
        {
            int n = ActiveStates(model).Count, m = ActiveInputs(model).Count, p = ActiveOutputs(model).Count; a = Rectangular(model == null ? null : model.A, n, n); b = Rectangular(model == null ? null : model.B, n, m); c = Rectangular(model == null ? null : model.C, p, n); d = Rectangular(model == null ? null : model.D, p, m);
            if (model != null && String.Equals(model.DynamicsKind, "CONTINUOUS", StringComparison.OrdinalIgnoreCase)) { double dt = Math.Max(Epsilon, model.SamplePeriod); a = Add(Identity(n), Scale(a, dt)); b = Scale(b, dt); }
        }

        private static CyberSimulationStep Step(CyberTwinModel model, int k, double[] x, double[] u, double[] y)
        {
            CyberSimulationStep row = new CyberSimulationStep { Step = k, Time = k * Math.Max(Epsilon, model.SamplePeriod), State = (double[])x.Clone(), Input = (double[])u.Clone(), Output = (double[])y.Clone() }; List<CyberStateVariable> states = ActiveStates(model); List<CyberControlInput> inputs = ActiveInputs(model); List<CyberMeasuredOutput> outputs = ActiveOutputs(model);
            for (int i = 0; i < x.Length; i++) { double error = x[i] - states[i].InitialValue; row.StageCost += Math.Max(Epsilon, states[i].RegulationWeight) * error * error; if (x[i] < states[i].Minimum - Epsilon || x[i] > states[i].Maximum + Epsilon) row.Violations.Add("STATE " + states[i].Id); }
            for (int i = 0; i < u.Length; i++) { row.StageCost += Math.Max(Epsilon, inputs[i].EffortWeight) * u[i] * u[i]; if (u[i] < inputs[i].Minimum - Epsilon || u[i] > inputs[i].Maximum + Epsilon) row.Violations.Add("INPUT " + inputs[i].Id); }
            for (int i = 0; i < y.Length; i++) { double error = y[i] - outputs[i].Target; row.StageCost += Math.Max(Epsilon, outputs[i].TrackingWeight) * error * error; if (y[i] < outputs[i].Minimum - Epsilon || y[i] > outputs[i].Maximum + Epsilon) row.Violations.Add("OUTPUT " + outputs[i].Id); }
            foreach (CyberConstraint constraint in (model.Constraints ?? new List<CyberConstraint>()).Where(c => Active(c.Status))) { double value; if (!ConstraintValue(constraint, states, inputs, outputs, x, u, y, out value)) continue; double lowViolation = Math.Max(0, constraint.Minimum - value), highViolation = Math.Max(0, value - constraint.Maximum); if (lowViolation > 0 || highViolation > 0) { row.StageCost += Math.Max(Epsilon, constraint.Penalty) * (lowViolation * lowViolation + highViolation * highViolation); row.Violations.Add((constraint.Hard ? "HARD " : "SOFT ") + "CONSTRAINT " + (constraint.Id ?? constraint.VariableId)); } }
            return row;
        }

        private static void MpcCostGradient(CyberTwinModel model, double[,] a, double[,] b, double[,] c, double[,] d, double[] initial, double[][] controls, out double cost, out double[][] gradient)
        {
            int h = controls.Length, n = a.GetLength(0), m = b.GetLength(1), p = c.GetLength(0); double[][] states = new double[h + 1][], outputs = new double[h][], statePenaltyGradients = Enumerable.Range(0, h + 1).Select(k => new double[n]).ToArray(); states[0] = (double[])initial.Clone(); cost = 0; List<CyberStateVariable> stateDefinitions = ActiveStates(model); List<CyberControlInput> inputDefinitions = ActiveInputs(model); List<CyberMeasuredOutput> outputDefinitions = ActiveOutputs(model); double[] target = outputDefinitions.Select(x => x.Target).ToArray();
            double[][] outputPenaltyGradients = Enumerable.Range(0, h).Select(k => new double[p]).ToArray(), inputPenaltyGradients = Enumerable.Range(0, h).Select(k => new double[m]).ToArray();
            for (int k = 0; k < h; k++) { outputs[k] = Add(Multiply(c, states[k]), Multiply(d, controls[k])); double[] outputError = Subtract(outputs[k], target); for (int i = 0; i < p; i++) cost += Math.Max(Epsilon, outputDefinitions[i].TrackingWeight) * outputError[i] * outputError[i]; for (int j = 0; j < m; j++) cost += Math.Max(Epsilon, inputDefinitions[j].EffortWeight) * controls[k][j] * controls[k][j]; states[k + 1] = Add(Multiply(a, states[k]), Multiply(b, controls[k])); for (int i = 0; i < n; i++) { double lowViolation = Math.Max(0, stateDefinitions[i].Minimum - states[k + 1][i]), highViolation = Math.Max(0, states[k + 1][i] - stateDefinitions[i].Maximum); cost += 1000 * (lowViolation * lowViolation + highViolation * highViolation); statePenaltyGradients[k + 1][i] = lowViolation > 0 ? -2000 * lowViolation : (highViolation > 0 ? 2000 * highViolation : 0); } foreach (CyberConstraint constraint in (model.Constraints ?? new List<CyberConstraint>()).Where(q => Active(q.Status))) { int coordinate = ConstraintIndex(constraint, stateDefinitions, inputDefinitions, outputDefinitions); if (coordinate < 0) continue; double value = Eq(constraint.VariableKind, "INPUT") ? controls[k][coordinate] : (Eq(constraint.VariableKind, "OUTPUT") ? outputs[k][coordinate] : states[k + 1][coordinate]); double lowViolation = Math.Max(0, constraint.Minimum - value), highViolation = Math.Max(0, value - constraint.Maximum), weight = Math.Max(Epsilon, constraint.Penalty); cost += weight * (lowViolation * lowViolation + highViolation * highViolation); double derivative = lowViolation > 0 ? -2 * weight * lowViolation : (highViolation > 0 ? 2 * weight * highViolation : 0); if (Eq(constraint.VariableKind, "INPUT")) inputPenaltyGradients[k][coordinate] += derivative; else if (Eq(constraint.VariableKind, "OUTPUT")) outputPenaltyGradients[k][coordinate] += derivative; else statePenaltyGradients[k + 1][coordinate] += derivative; } }
            gradient = Enumerable.Range(0, h).Select(k => new double[m]).ToArray(); double[] lambda = new double[n];
            for (int k = h - 1; k >= 0; k--) { double[] outputError = Subtract(outputs[k], target), weightedOutputError = new double[p]; for (int i = 0; i < p; i++) weightedOutputError[i] = 2 * Math.Max(Epsilon, outputDefinitions[i].TrackingWeight) * outputError[i] + outputPenaltyGradients[k][i]; lambda = Add(lambda, statePenaltyGradients[k + 1]); double[] controlGradient = Add(Add(Add(Multiply(Transpose(b), lambda), Multiply(Transpose(d), weightedOutputError)), controls[k].Select((value, j) => 2 * Math.Max(Epsilon, inputDefinitions[j].EffortWeight) * value).ToArray()), inputPenaltyGradients[k]); gradient[k] = controlGradient; lambda = Add(Multiply(Transpose(a), lambda), Multiply(Transpose(c), weightedOutputError)); }
        }

        private static void ProjectControlSequence(CyberTwinModel model, double[][] controls, List<CyberControlInput> inputs)
        {
            double[] previous = new double[inputs.Count]; for (int k = 0; k < controls.Length; k++) for (int j = 0; j < inputs.Count; j++) { double declaredLow, declaredHigh; HardBounds(model, "INPUT", inputs[j].Id, inputs[j].Minimum, inputs[j].Maximum, out declaredLow, out declaredHigh); double low = Math.Max(declaredLow, previous[j] + inputs[j].MinimumSlew), high = Math.Min(declaredHigh, previous[j] + inputs[j].MaximumSlew); controls[k][j] = Clamp(controls[k][j], low, high); previous[j] = controls[k][j]; }
        }

        private static double[] ClampInput(CyberTwinModel model, double[] input, double[] previous)
        {
            List<CyberControlInput> definitions = ActiveInputs(model); double[] result = Vector(input, definitions.Count, null), prior = Vector(previous, definitions.Count, null); for (int i = 0; i < result.Length; i++) { double declaredLow, declaredHigh; HardBounds(model, "INPUT", definitions[i].Id, definitions[i].Minimum, definitions[i].Maximum, out declaredLow, out declaredHigh); result[i] = Clamp(result[i], Math.Max(declaredLow, prior[i] + definitions[i].MinimumSlew), Math.Min(declaredHigh, prior[i] + definitions[i].MaximumSlew)); } return result;
        }

        private static bool ConstraintValue(CyberConstraint constraint, List<CyberStateVariable> states, List<CyberControlInput> inputs, List<CyberMeasuredOutput> outputs, double[] x, double[] u, double[] y, out double value) { int index = ConstraintIndex(constraint, states, inputs, outputs); value = 0; if (index < 0) return false; if (Eq(constraint.VariableKind, "INPUT")) value = u[index]; else if (Eq(constraint.VariableKind, "OUTPUT")) value = y[index]; else value = x[index]; return true; }
        private static int ConstraintIndex(CyberConstraint constraint, List<CyberStateVariable> states, List<CyberControlInput> inputs, List<CyberMeasuredOutput> outputs) { if (constraint == null) return -1; if (Eq(constraint.VariableKind, "INPUT")) return inputs.FindIndex(x => Eq(x.Id, constraint.VariableId)); if (Eq(constraint.VariableKind, "OUTPUT")) return outputs.FindIndex(x => Eq(x.Id, constraint.VariableId)); return states.FindIndex(x => Eq(x.Id, constraint.VariableId)); }
        private static void HardBounds(CyberTwinModel model, string kind, string id, double fallbackLow, double fallbackHigh, out double low, out double high) { low = fallbackLow; high = fallbackHigh; foreach (CyberConstraint constraint in (model == null ? new List<CyberConstraint>() : model.Constraints ?? new List<CyberConstraint>()).Where(c => Active(c.Status) && c.Hard && Eq(c.VariableKind, kind) && Eq(c.VariableId, id))) { low = Math.Max(low, constraint.Minimum); high = Math.Min(high, constraint.Maximum); } }

        private static List<CyberStateVariable> ActiveStates(CyberTwinModel m) { return (m == null ? new List<CyberStateVariable>() : m.States ?? new List<CyberStateVariable>()).Where(x => Active(x.Status)).ToList(); }
        private static List<CyberControlInput> ActiveInputs(CyberTwinModel m) { return (m == null ? new List<CyberControlInput>() : m.Inputs ?? new List<CyberControlInput>()).Where(x => Active(x.Status)).ToList(); }
        private static List<CyberMeasuredOutput> ActiveOutputs(CyberTwinModel m) { return (m == null ? new List<CyberMeasuredOutput>() : m.Outputs ?? new List<CyberMeasuredOutput>()).Where(x => Active(x.Status)).ToList(); }

        private static double[,] Controllability(double[,] a, double[,] b) { int n = a.GetLength(0), m = b.GetLength(1); double[,] r = new double[n, n * m], power = Identity(n); for (int block = 0; block < n; block++) { double[,] value = Multiply(power, b); for (int i = 0; i < n; i++) for (int j = 0; j < m; j++) r[i, block * m + j] = value[i, j]; power = Multiply(power, a); } return r; }
        private static double[,] Observability(double[,] a, double[,] c) { int n = a.GetLength(0), p = c.GetLength(0); double[,] r = new double[n * p, n], power = Identity(n); for (int block = 0; block < n; block++) { double[,] value = Multiply(c, power); for (int i = 0; i < p; i++) for (int j = 0; j < n; j++) r[block * p + i, j] = value[i, j]; power = Multiply(power, a); } return r; }
        private static double[,] Gramian(double[,] a, double[,] io, bool observability, int terms) { int n = a.GetLength(0); double[,] result = new double[n, n], power = Identity(n); for (int k = 0; k < terms; k++) { double[,] term = observability ? Multiply(Multiply(Transpose(power), Multiply(Transpose(io), io)), power) : Multiply(Multiply(power, Multiply(io, Transpose(io))), Transpose(power)); result = Add(result, term); power = Multiply(power, a); if (Frobenius(term) < 1e-14) break; if (Frobenius(power) > 1e12) break; } return result; }

        private static List<double> EigenMagnitudes(double[,] matrix)
        {
            int n = matrix.GetLength(0); if (n == 0) return new List<double>(); double[,] h = Copy(matrix); for (int iteration = 0; iteration < 256; iteration++) { double[,] q, r; Qr(h, out q, out r); h = Multiply(r, q); if (LowerNorm(h) < 1e-10) break; } List<double> values = new List<double>(); int i = 0; while (i < n) { if (i + 1 < n && Math.Abs(h[i + 1, i]) > 1e-7) { double trace = h[i, i] + h[i + 1, i + 1], determinant = h[i, i] * h[i + 1, i + 1] - h[i, i + 1] * h[i + 1, i], discriminant = trace * trace - 4 * determinant; if (discriminant >= 0) { double root = Math.Sqrt(Math.Max(0, Finite(discriminant))); values.Add(Math.Abs(Finite((trace + root) / 2))); values.Add(Math.Abs(Finite((trace - root) / 2))); } else { double magnitude = Math.Sqrt(Math.Abs(Finite(determinant))); values.Add(magnitude); values.Add(magnitude); } i += 2; } else { values.Add(Math.Abs(Finite(h[i, i]))); i++; } } return values;
        }

        private static void Qr(double[,] a, out double[,] q, out double[,] r)
        {
            int rows = a.GetLength(0), cols = a.GetLength(1); q = new double[rows, cols]; r = new double[cols, cols]; for (int j = 0; j < cols; j++) { double[] v = Column(a, j); for (int i = 0; i < j; i++) { double projection = Dot(Column(q, i), v); r[i, j] = projection; v = Subtract(v, Scale(Column(q, i), projection)); } double norm = Norm(v); if (norm <= Epsilon) { v = new double[rows]; if (j < rows) v[j] = 1; for (int i = 0; i < j; i++) v = Subtract(v, Scale(Column(q, i), Dot(Column(q, i), v))); norm = Math.Max(Epsilon, Norm(v)); } r[j, j] = norm; for (int row = 0; row < rows; row++) q[row, j] = v[row] / norm; }
        }

        private static int Rank(double[,] source) { double[,] a = Copy(source); int rows = a.GetLength(0), cols = a.GetLength(1), rank = 0; double scale = Math.Max(1, Frobenius(a)), tolerance = 1e-9 * scale; for (int col = 0; col < cols && rank < rows; col++) { int pivot = rank; for (int row = rank + 1; row < rows; row++) if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row; if (Math.Abs(a[pivot, col]) <= tolerance) continue; SwapRows(a, rank, pivot); double value = a[rank, col]; for (int j = col; j < cols; j++) a[rank, j] /= value; for (int row = 0; row < rows; row++) if (row != rank) { double factor = a[row, col]; if (Math.Abs(factor) <= tolerance) continue; for (int j = col; j < cols; j++) a[row, j] -= factor * a[rank, j]; } rank++; } return rank; }
        private static bool TryInverse(double[,] source, out double[,] inverse) { int n = source.GetLength(0); inverse = new double[n, n]; if (n != source.GetLength(1)) return false; double[,] a = Copy(source); inverse = Identity(n); for (int col = 0; col < n; col++) { int pivot = col; for (int row = col + 1; row < n; row++) if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row; if (Math.Abs(a[pivot, col]) <= 1e-14) return false; SwapRows(a, col, pivot); SwapRows(inverse, col, pivot); double divisor = a[col, col]; for (int j = 0; j < n; j++) { a[col, j] /= divisor; inverse[col, j] /= divisor; } for (int row = 0; row < n; row++) if (row != col) { double factor = a[row, col]; for (int j = 0; j < n; j++) { a[row, j] -= factor * a[col, j]; inverse[row, j] -= factor * inverse[col, j]; } } } return true; }
        private static double[,] PseudoInverseRegularized(double[,] a, double lambda) { if (a.GetLength(0) == a.GetLength(1)) { double[,] inverse; if (TryInverse(Add(a, Scale(Identity(a.GetLength(0)), lambda)), out inverse)) return inverse; } double[,] at = Transpose(a), normal = Add(Multiply(at, a), Scale(Identity(a.GetLength(1)), lambda)), normalInverse; if (!TryInverse(normal, out normalInverse)) normalInverse = Identity(normal.GetLength(0)); return Multiply(normalInverse, at); }
        private static double ConditionEstimate(double[,] a) { double[,] inverse; return TryInverse(Add(a, Scale(Identity(a.GetLength(0)), 1e-12)), out inverse) ? Frobenius(a) * Frobenius(inverse) : Double.MaxValue; }
        private static double LogDetRegularized(double[,] source, double regularizer) { int n = source.GetLength(0); if (n == 0) return 0; double[,] a = Add(source, Scale(Identity(n), regularizer)); double result = 0; for (int col = 0; col < n; col++) { int pivot = col; for (int row = col + 1; row < n; row++) if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col])) pivot = row; double value = Math.Abs(a[pivot, col]); if (value <= Epsilon) return Math.Log(Epsilon) * n; SwapRows(a, col, pivot); result += Math.Log(value); for (int row = col + 1; row < n; row++) { double factor = a[row, col] / a[col, col]; for (int j = col; j < n; j++) a[row, j] -= factor * a[col, j]; } } return result; }

        private static double[,] Rectangular(double[][] source, int rows, int cols) { double[,] r = new double[Math.Max(0, rows), Math.Max(0, cols)]; if (source == null) return r; for (int i = 0; i < Math.Min(rows, source.Length); i++) if (source[i] != null) for (int j = 0; j < Math.Min(cols, source[i].Length); j++) r[i, j] = Finite(source[i][j]); return r; }
        private static double[][] Jagged(double[,] source) { int rows = source.GetLength(0), cols = source.GetLength(1); double[][] r = new double[rows][]; for (int i = 0; i < rows; i++) { r[i] = new double[cols]; for (int j = 0; j < cols; j++) r[i][j] = source[i, j]; } return r; }
        private static double[,] Identity(int n) { double[,] r = new double[n, n]; for (int i = 0; i < n; i++) r[i, i] = 1; return r; }
        private static double[,] Diagonal(double[] values) { values = values ?? new double[0]; double[,] r = new double[values.Length, values.Length]; for (int i = 0; i < values.Length; i++) r[i, i] = values[i]; return r; }
        private static double[,] Copy(double[,] a) { return (double[,])a.Clone(); }
        private static double[,] Add(double[,] a, double[,] b) { int rows = a.GetLength(0), cols = a.GetLength(1); double[,] r = new double[rows, cols]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[i, j] = a[i, j] + b[i, j]; return r; }
        private static double[,] Subtract(double[,] a, double[,] b) { int rows = a.GetLength(0), cols = a.GetLength(1); double[,] r = new double[rows, cols]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[i, j] = a[i, j] - b[i, j]; return r; }
        private static double[,] Scale(double[,] a, double value) { int rows = a.GetLength(0), cols = a.GetLength(1); double[,] r = new double[rows, cols]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[i, j] = a[i, j] * value; return r; }
        private static double[,] Absolute(double[,] a) { int rows = a.GetLength(0), cols = a.GetLength(1); double[,] r = new double[rows, cols]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[i, j] = Math.Abs(a[i, j]); return r; }
        private static double[,] Transpose(double[,] a) { int rows = a.GetLength(0), cols = a.GetLength(1); double[,] r = new double[cols, rows]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[j, i] = a[i, j]; return r; }
        private static double[,] Multiply(double[,] a, double[,] b) { int rows = a.GetLength(0), inner = a.GetLength(1), cols = b.GetLength(1); double[,] r = new double[rows, cols]; for (int i = 0; i < rows; i++) for (int k = 0; k < inner; k++) { double x = a[i, k]; if (Math.Abs(x) <= Epsilon) continue; for (int j = 0; j < cols; j++) r[i, j] += x * b[k, j]; } return r; }
        private static double[] Multiply(double[,] a, double[] x) { int rows = a.GetLength(0), cols = a.GetLength(1); double[] r = new double[rows]; for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) r[i] += a[i, j] * (j < x.Length ? x[j] : 0); return r; }
        private static double[] Add(double[] a, double[] b) { int n = Math.Max(a.Length, b.Length); double[] r = new double[n]; for (int i = 0; i < n; i++) r[i] = (i < a.Length ? a[i] : 0) + (i < b.Length ? b[i] : 0); return r; }
        private static double[] Subtract(double[] a, double[] b) { int n = Math.Max(a.Length, b.Length); double[] r = new double[n]; for (int i = 0; i < n; i++) r[i] = (i < a.Length ? a[i] : 0) - (i < b.Length ? b[i] : 0); return r; }
        private static double[] Scale(double[] a, double value) { return a.Select(x => x * value).ToArray(); }
        private static double Dot(double[] a, double[] b) { double r = 0; for (int i = 0; i < Math.Min(a.Length, b.Length); i++) r += a[i] * b[i]; return r; }
        private static double Norm(double[] x) { return Math.Sqrt(Dot(x, x)); }
        private static double Frobenius(double[,] a) { double sum = 0; foreach (double x in a) sum += x * x; return Math.Sqrt(sum); }
        private static double Trace(double[,] a) { double r = 0; for (int i = 0; i < Math.Min(a.GetLength(0), a.GetLength(1)); i++) r += a[i, i]; return r; }
        private static double LowerNorm(double[,] a) { double sum = 0; for (int i = 1; i < a.GetLength(0); i++) for (int j = 0; j < Math.Min(i, a.GetLength(1)); j++) sum += a[i, j] * a[i, j]; return Math.Sqrt(sum); }
        private static double[] Column(double[,] a, int col) { double[] r = new double[a.GetLength(0)]; for (int i = 0; i < r.Length; i++) r[i] = a[i, col]; return r; }
        private static void SwapRows(double[,] a, int x, int y) { if (x == y) return; for (int j = 0; j < a.GetLength(1); j++) { double t = a[x, j]; a[x, j] = a[y, j]; a[y, j] = t; } }
        private static double[,] SelectRows(double[,] a, IList<int> rows) { rows = rows ?? new List<int>(); double[,] r = new double[rows.Count, a.GetLength(1)]; for (int i = 0; i < rows.Count; i++) for (int j = 0; j < a.GetLength(1); j++) r[i, j] = a[rows[i], j]; return r; }
        private static double[] Vector(double[] source, int length, double[] fallback) { double[] r = new double[Math.Max(0, length)]; for (int i = 0; i < r.Length; i++) r[i] = source != null && i < source.Length ? Finite(source[i]) : (fallback != null && i < fallback.Length ? Finite(fallback[i]) : 0); return r; }
        private static int FirstCrossing(double[] series, double target, bool increasing) { for (int i = 0; i < series.Length; i++) if (increasing ? series[i] >= target : series[i] <= target) return i; return Math.Max(0, series.Length - 1); }
        private static void AuditMatrix(List<string> findings, string name, double[][] matrix, int rows, int cols) { if (matrix == null || matrix.Length != rows || matrix.Any(row => row == null || row.Length != cols)) findings.Add("BLOCKER · MATRIX_DIMENSION_" + name + " · expected " + rows + "x" + cols); else if (matrix.Any(row => row.Any(x => Double.IsNaN(x) || Double.IsInfinity(x)))) findings.Add("BLOCKER · NONFINITE_MATRIX_" + name); }
        private static void AppendMatrix(StringBuilder s, string name, double[][] matrix) { s.Append('|').Append(name).Append(':'); foreach (double[] row in matrix ?? new double[0][]) { foreach (double value in row ?? new double[0]) s.Append(Finite(value).ToString("R", CultureInfo.InvariantCulture)).Append(','); s.Append(';'); } }
        private static void AppendEvidence(StringBuilder s, IEnumerable<string> evidence) { foreach (string id in (evidence ?? Enumerable.Empty<string>()).OrderBy(x => x)) s.Append(":EVIDENCE:").Append(id); }
        private static double Value(IDictionary<string, double> map, string key, double fallback) { double value; return map != null && map.TryGetValue(key ?? "", out value) ? Finite(value) : fallback; }
        private static double Value(IDictionary<string, double> map, string key) { return Value(map, key, 0); }
        private static double Finite(double x) { return Double.IsNaN(x) || Double.IsInfinity(x) ? 0 : x; }
        private static double Clamp(double x, double low, double high) { if (high < low) { double t = low; low = high; high = t; } return Math.Max(low, Math.Min(high, Finite(x))); }
        private static double Quantile(List<double> sorted, double q) { if (sorted == null || sorted.Count == 0) return 0; double x = Clamp(q, 0, 1) * (sorted.Count - 1); int low = (int)Math.Floor(x), high = (int)Math.Ceiling(x); return low == high ? sorted[low] : sorted[low] * (high - x) + sorted[high] * (x - low); }
        private static double RadicalInverse(int n, int b) { double value = 0, factor = 1.0 / b; while (n > 0) { value += factor * (n % b); n /= b; factor /= b; } return value; }
        private static int Prime(int index) { int[] p = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293 }; return p[Math.Max(0, Math.Min(p.Length - 1, index - 1))]; }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static bool Active(string status) { return !String.Equals(status ?? "", "ARCHIVED", StringComparison.OrdinalIgnoreCase) && !String.Equals(status ?? "", "DISABLED", StringComparison.OrdinalIgnoreCase) && !String.Equals(status ?? "", "RETIRED", StringComparison.OrdinalIgnoreCase) && !String.Equals(status ?? "", "REJECTED", StringComparison.OrdinalIgnoreCase); }
        private static string HashText(string value) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

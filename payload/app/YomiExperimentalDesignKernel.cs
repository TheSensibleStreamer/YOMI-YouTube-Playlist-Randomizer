using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    // DEV13.37.15 pure scientific-discovery kernel.  It is deliberately incapable of
    // touching WPF, files, processes, clocks, playback, networks or experiment hardware.
    // Every result is a deterministic transformation of an explicit protocol and seed.
    internal sealed class DiscoveryFactor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public int Levels { get; set; }
        public double PerRunCost { get; set; }
        public double PerRunRisk { get; set; }
        public bool Controllable { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryFactor() { Kind = "CONTINUOUS"; Status = "ACTIVE"; Minimum = -1; Maximum = 1; Levels = 2; Controllable = true; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Controllable ? "CONTROL" : "OBSERVE").PadRight(8) + "   ·   " + (Name ?? Id ?? "FACTOR") + "   ·   [" + Minimum.ToString("0.####", CultureInfo.InvariantCulture) + ", " + Maximum.ToString("0.####", CultureInfo.InvariantCulture) + "] " + (Unit ?? "") + "   ·   " + Math.Max(2, Levels).ToString(CultureInfo.InvariantCulture) + " levels"; }
    }

    internal sealed class DiscoveryOutcome
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Scale { get; set; }
        public string Direction { get; set; }
        public string Status { get; set; }
        public double NoiseSigma { get; set; }
        public double RelevantEffect { get; set; }
        public double Alpha { get; set; }
        public double TargetPower { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryOutcome() { Scale = "CONTINUOUS"; Direction = "TWO_SIDED"; Status = "ACTIVE"; NoiseSigma = 1; RelevantEffect = 1; Alpha = 0.05; TargetPower = 0.8; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Scale ?? "CONTINUOUS").PadRight(11) + "   ·   " + (Name ?? Id ?? "OUTCOME") + "   ·   σ=" + NoiseSigma.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   MDE=" + RelevantEffect.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   power " + TargetPower.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryPrediction
    {
        public string Id { get; set; }
        public string OutcomeId { get; set; }
        public double Intercept { get; set; }
        public double Sigma { get; set; }
        public string Distribution { get; set; }
        public Dictionary<string, double> Terms { get; set; }
        public string Rationale { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryPrediction() { Distribution = "GAUSSIAN"; Sigma = 1; Terms = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Distribution ?? "GAUSSIAN") + "   ·   outcome " + (OutcomeId ?? "∅") + "   ·   μ=" + Intercept.ToString("0.####", CultureInfo.InvariantCulture) + " + " + Terms.Count.ToString(CultureInfo.InvariantCulture) + " terms   ·   σ=" + Sigma.ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryHypothesis
    {
        public string Id { get; set; }
        public string ParentHypothesisId { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string Status { get; set; }
        public string Statement { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public double PriorWeight { get; set; }
        public List<DiscoveryPrediction> Predictions { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> Falsifiers { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryHypothesis() { Family = "COMPETING_MODEL"; Status = "ACTIVE"; PriorWeight = 1; Predictions = new List<DiscoveryPrediction>(); Assumptions = new List<string>(); Falsifiers = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "HYPOTHESIS") + "   ·   prior weight " + PriorWeight.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   " + Predictions.Count.ToString(CultureInfo.InvariantCulture) + " predictions"; }
    }

    internal sealed class DiscoveryProtocol
    {
        public string Id { get; set; }
        public string ParentProtocolId { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Name { get; set; }
        public string Charter { get; set; }
        public string Status { get; set; }
        public string DesignMethod { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public int Revision { get; set; }
        public int RequestedRuns { get; set; }
        public int Replicates { get; set; }
        public int CandidatePool { get; set; }
        public int Seed { get; set; }
        public double Budget { get; set; }
        public double RiskCeiling { get; set; }
        public string RandomizationRule { get; set; }
        public string BlockingRule { get; set; }
        public string StoppingRule { get; set; }
        public string AnalysisPlan { get; set; }
        public string ExclusionRule { get; set; }
        public List<DiscoveryFactor> Factors { get; set; }
        public List<DiscoveryOutcome> Outcomes { get; set; }
        public List<DiscoveryHypothesis> Hypotheses { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryProtocol() { Status = "DRAFT"; DesignMethod = "D_OPTIMAL"; Revision = 1; RequestedRuns = 16; Replicates = 1; CandidatePool = 256; Seed = 1337; Budget = Double.MaxValue; RiskCeiling = Double.MaxValue; RandomizationRule = "DETERMINISTIC_SEEDED_ORDER"; BlockingRule = "NONE"; StoppingRule = "FIXED_SAMPLE"; AnalysisPlan = "Estimate preregistered main effects; compare explicit hypothesis likelihoods; report uncertainty and all exclusions."; ExclusionRule = "NO POST-HOC EXCLUSIONS"; Factors = new List<DiscoveryFactor>(); Outcomes = new List<DiscoveryOutcome>(); Hypotheses = new List<DiscoveryHypothesis>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "DRAFT") + "   ·   " + (Name ?? Id ?? "PROTOCOL") + "   ·   " + (DesignMethod ?? "DESIGN") + "   ·   " + RequestedRuns.ToString(CultureInfo.InvariantCulture) + " runs   ·   " + Hypotheses.Count.ToString(CultureInfo.InvariantCulture) + " hypotheses   ·   r" + Math.Max(1, Revision).ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryRun
    {
        public int Sequence { get; set; }
        public int Block { get; set; }
        public int Replicate { get; set; }
        public string Id { get; set; }
        public double Cost { get; set; }
        public double Risk { get; set; }
        public Dictionary<string, double> Levels { get; set; }
        public DiscoveryRun() { Block = 1; Replicate = 1; Levels = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return "RUN " + Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(4) + "   ·   B" + Block.ToString(CultureInfo.InvariantCulture) + "/R" + Replicate.ToString(CultureInfo.InvariantCulture) + "   ·   cost " + Cost.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   risk " + Risk.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + String.Join("   ", Levels.Take(8).Select(x => x.Key + "=" + x.Value.ToString("0.###", CultureInfo.InvariantCulture)).ToArray()); }
    }

    internal sealed class DiscoveryAlias
    {
        public string LeftTerm { get; set; }
        public string RightTerm { get; set; }
        public double Correlation { get; set; }
        public override string ToString() { return Math.Abs(Correlation).ToString("0.000000", CultureInfo.InvariantCulture).PadLeft(10) + "   ·   " + (LeftTerm ?? "∅") + "  ↔  " + (RightTerm ?? "∅") + (Math.Abs(Correlation) > 0.999999 ? "   ·   EXACT ALIAS" : ""); }
    }

    internal sealed class DiscoveryDesignDiagnostics
    {
        public int Runs { get; set; }
        public int Parameters { get; set; }
        public int Rank { get; set; }
        public double LogDeterminant { get; set; }
        public double ConditionNumberBound { get; set; }
        public double MaxAbsoluteCorrelation { get; set; }
        public double DEfficiency { get; set; }
        public double GEfficiency { get; set; }
        public bool Estimable { get; set; }
        public List<string> Terms { get; set; }
        public List<DiscoveryAlias> Aliases { get; set; }
        public List<string> Warnings { get; set; }
        public DiscoveryDesignDiagnostics() { Terms = new List<string>(); Aliases = new List<DiscoveryAlias>(); Warnings = new List<string>(); }
        public override string ToString() { return (Estimable ? "ESTIMABLE" : "RANK DEFICIENT") + "   ·   rank " + Rank.ToString(CultureInfo.InvariantCulture) + "/" + Parameters.ToString(CultureInfo.InvariantCulture) + "   ·   log|X'X| " + LogDeterminant.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   κ≤" + ConditionNumberBound.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   D-eff " + DEfficiency.ToString("P1", CultureInfo.InvariantCulture) + "   ·   max |ρ| " + MaxAbsoluteCorrelation.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryHypothesisPosterior
    {
        public string HypothesisId { get; set; }
        public string HypothesisName { get; set; }
        public double Prior { get; set; }
        public double LogLikelihood { get; set; }
        public double Posterior { get; set; }
        public double LogBayesFactorVsBestAlternative { get; set; }
        public override string ToString() { return (HypothesisName ?? HypothesisId ?? "HYPOTHESIS").PadRight(38) + "   ·   prior " + Prior.ToString("P3", CultureInfo.InvariantCulture).PadLeft(10) + "   ·   posterior " + Posterior.ToString("P3", CultureInfo.InvariantCulture).PadLeft(10) + "   ·   log BF " + LogBayesFactorVsBestAlternative.ToString("+0.000000;-0.000000;0", CultureInfo.InvariantCulture).PadLeft(12); }
    }

    internal sealed class DiscoveryObservation
    {
        public string Id { get; set; }
        public string RunId { get; set; }
        public string OutcomeId { get; set; }
        public double Value { get; set; }
        public double StandardError { get; set; }
        public string Status { get; set; }
        public string RecordedUtc { get; set; }
        public string Provenance { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DiscoveryObservation() { Status = "OBSERVED"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "OBSERVED") + "   ·   " + (RunId ?? "RUN") + " / " + (OutcomeId ?? "OUTCOME") + "   ·   y=" + Value.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   SE=" + StandardError.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryDiscrimination
    {
        public double PriorEntropyBits { get; set; }
        public double ExpectedPosteriorEntropyBits { get; set; }
        public double ExpectedInformationGainBits { get; set; }
        public double MinimumPairwiseSeparation { get; set; }
        public double MeanPairwiseSeparation { get; set; }
        public string StrongestRunId { get; set; }
        public List<string> Audit { get; set; }
        public DiscoveryDiscrimination() { Audit = new List<string>(); }
        public override string ToString() { return "EIG " + ExpectedInformationGainBits.ToString("0.000000", CultureInfo.InvariantCulture) + " bits   ·   H₀ " + PriorEntropyBits.ToString("0.000000", CultureInfo.InvariantCulture) + " → E[H₁] " + ExpectedPosteriorEntropyBits.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   min separation " + MinimumPairwiseSeparation.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   strongest " + (StrongestRunId ?? "∅"); }
    }

    internal sealed class DiscoveryPowerPlan
    {
        public string OutcomeId { get; set; }
        public int TotalSample { get; set; }
        public int PerArmSample { get; set; }
        public double Alpha { get; set; }
        public double Power { get; set; }
        public double Sigma { get; set; }
        public double Effect { get; set; }
        public string Assumption { get; set; }
        public override string ToString() { return (OutcomeId ?? "OUTCOME").PadRight(30) + "   ·   n=" + TotalSample.ToString(CultureInfo.InvariantCulture).PadLeft(7) + " total / " + PerArmSample.ToString(CultureInfo.InvariantCulture).PadLeft(6) + " per arm   ·   α=" + Alpha.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   power=" + Power.ToString("P1", CultureInfo.InvariantCulture) + "   ·   |δ|/σ=" + (Math.Abs(Effect) / Math.Max(1e-12, Sigma)).ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryReproducibilityProfile
    {
        public int Observations { get; set; }
        public int UniqueCells { get; set; }
        public int ReplicatedCells { get; set; }
        public double ReplicationCoverage { get; set; }
        public double PooledWithinCellSigma { get; set; }
        public double IntraclassCorrelation { get; set; }
        public double SignAgreement { get; set; }
        public List<string> Warnings { get; set; }
        public DiscoveryReproducibilityProfile() { Warnings = new List<string>(); }
        public override string ToString() { return "REPLICATION COVERAGE " + ReplicationCoverage.ToString("P1", CultureInfo.InvariantCulture) + "   ·   within-cell σ " + PooledWithinCellSigma.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   ICC " + IntraclassCorrelation.ToString("0.000000", CultureInfo.InvariantCulture) + "   ·   sign agreement " + SignAgreement.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryExperimentCandidate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double InformationValue { get; set; }
        public double Cost { get; set; }
        public double Risk { get; set; }
        public bool Mandatory { get; set; }
        public List<string> RequiresIds { get; set; }
        public List<string> ExcludesIds { get; set; }
        public DiscoveryExperimentCandidate() { RequiresIds = new List<string>(); ExcludesIds = new List<string>(); }
        public override string ToString() { return (Mandatory ? "MANDATORY" : "CANDIDATE").PadRight(9) + "   ·   " + (Name ?? Id ?? "EXPERIMENT") + "   ·   EIG " + InformationValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   cost " + Cost.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   risk " + Risk.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class DiscoveryPortfolio
    {
        public List<string> SelectedIds { get; set; }
        public double InformationValue { get; set; }
        public double Cost { get; set; }
        public double Risk { get; set; }
        public int ExploredStates { get; set; }
        public bool GloballyOptimalWithinBound { get; set; }
        public string Fingerprint { get; set; }
        public List<string> Audit { get; set; }
        public DiscoveryPortfolio() { SelectedIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return (GloballyOptimalWithinBound ? "BOUNDED OPTIMUM" : "HEURISTIC") + "   ·   " + SelectedIds.Count.ToString(CultureInfo.InvariantCulture) + " experiments   ·   EIG " + InformationValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   cost " + Cost.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   risk " + Risk.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + ExploredStates.ToString(CultureInfo.InvariantCulture) + " states"; }
    }

    internal static class ExperimentalDesignKernel
    {
        private const double Epsilon = 1e-10;

        public static List<DiscoveryRun> Generate(DiscoveryProtocol protocol)
        {
            if (protocol == null) return new List<DiscoveryRun>();
            List<DiscoveryFactor> factors = ActiveFactors(protocol);
            int requested = Math.Max(1, Math.Min(4096, protocol.RequestedRuns));
            string method = (protocol.DesignMethod ?? "D_OPTIMAL").ToUpperInvariant();
            List<DiscoveryRun> runs;
            if (method == "FULL_FACTORIAL") runs = FullFactorial(factors, requested);
            else if (method == "FRACTIONAL_FACTORIAL") runs = FractionalFactorial(factors, requested, protocol.Seed);
            else if (method == "LATIN_HYPERCUBE") runs = LatinHypercube(factors, requested, protocol.Seed);
            else if (method == "HALTON") runs = HaltonDesign(factors, requested, protocol.Seed);
            else runs = DOptimal(factors, requested, Math.Max(requested, Math.Min(8192, protocol.CandidatePool)), protocol.Seed);
            ApplyReplicationAndEconomics(protocol, factors, runs);
            double budget = SafePositive(protocol.Budget, Double.MaxValue), riskCeiling = SafePositive(protocol.RiskCeiling, Double.MaxValue), usedCost = 0, usedRisk = 0; List<DiscoveryRun> admitted = new List<DiscoveryRun>();
            foreach (DiscoveryRun run in runs.Take(requested * Math.Max(1, protocol.Replicates))) { if (usedCost + run.Cost > budget || usedRisk + run.Risk > riskCeiling) continue; admitted.Add(run); usedCost += run.Cost; usedRisk += run.Risk; }
            Resequence(admitted); return admitted;
        }

        public static DiscoveryDesignDiagnostics Diagnose(DiscoveryProtocol protocol, IList<DiscoveryRun> runs)
        {
            DiscoveryDesignDiagnostics d = new DiscoveryDesignDiagnostics();
            List<DiscoveryFactor> factors = ActiveFactors(protocol); List<string> terms = Terms(factors, true); d.Terms = terms;
            double[,] x = Matrix(runs, factors, terms); d.Runs = x.GetLength(0); d.Parameters = x.GetLength(1); d.Rank = Rank(x); d.Estimable = d.Rank == d.Parameters;
            double[,] info = CrossProduct(x); d.LogDeterminant = LogDeterminant(info); double[,] inverse;
            if (TryInverse(info, out inverse)) { d.ConditionNumberBound = Frobenius(info) * Frobenius(inverse); double maxLeverage = 0; for (int r = 0; r < x.GetLength(0); r++) maxLeverage = Math.Max(maxLeverage, QuadraticRow(x, r, inverse)); d.GEfficiency = maxLeverage <= Epsilon ? 0 : Math.Min(1, d.Parameters / (Math.Max(1, d.Runs) * maxLeverage)); }
            else { d.ConditionNumberBound = Double.PositiveInfinity; d.GEfficiency = 0; }
            d.DEfficiency = d.Parameters == 0 || Double.IsNegativeInfinity(d.LogDeterminant) ? 0 : Math.Min(1, Math.Exp(d.LogDeterminant / Math.Max(1, d.Parameters)) / Math.Max(1, d.Runs));
            d.Aliases = Aliases(x, terms); d.MaxAbsoluteCorrelation = d.Aliases.Count == 0 ? 0 : d.Aliases.Max(a => Math.Abs(a.Correlation));
            if (d.Runs < d.Parameters) d.Warnings.Add("Run count is below the requested model dimension; coefficients cannot all be identified.");
            if (!d.Estimable) d.Warnings.Add("Design matrix is rank deficient under the preregistered main-effect and interaction model.");
            if (d.MaxAbsoluteCorrelation > 0.95) d.Warnings.Add("Severe term aliasing is present; interpretation requires a reduced model or additional runs.");
            if (Double.IsInfinity(d.ConditionNumberBound) || d.ConditionNumberBound > 1000000) d.Warnings.Add("Information matrix is numerically ill-conditioned; small measurement errors may dominate coefficient estimates.");
            if (factors.Any(f => !f.Controllable)) d.Warnings.Add("Observed factors are represented in diagnostics but must not be interpreted as randomized interventions.");
            return d;
        }

        public static DiscoveryDiscrimination Discriminate(DiscoveryProtocol protocol, IList<DiscoveryRun> runs)
        {
            DiscoveryDiscrimination result = new DiscoveryDiscrimination(); List<DiscoveryHypothesis> hypotheses = ActiveHypotheses(protocol); double[] prior = Priors(hypotheses); result.PriorEntropyBits = Entropy(prior);
            if (hypotheses.Count < 2 || runs == null || runs.Count == 0) { result.ExpectedPosteriorEntropyBits = result.PriorEntropyBits; result.Audit.Add("At least two active hypotheses and one design run are required for discrimination."); return result; }
            double weightedEntropy = 0, totalSeparation = 0, minimum = Double.PositiveInfinity, strongest = Double.NegativeInfinity; int pairs = 0;
            foreach (DiscoveryRun run in runs)
            {
                double runGain = 0;
                for (int truth = 0; truth < hypotheses.Count; truth++)
                {
                    List<double> logWeights = new List<double>();
                    for (int candidate = 0; candidate < hypotheses.Count; candidate++) logWeights.Add(Math.Log(Math.Max(Epsilon, prior[candidate])) + LogPredictiveAgreement(hypotheses[truth], hypotheses[candidate], protocol, run));
                    double[] posterior = NormalizeLogs(logWeights); double entropy = Entropy(posterior); weightedEntropy += prior[truth] * entropy / Math.Max(1, runs.Count); runGain += prior[truth] * (result.PriorEntropyBits - entropy);
                }
                if (runGain > strongest) { strongest = runGain; result.StrongestRunId = run.Id; }
                for (int i = 0; i < hypotheses.Count; i++) for (int j = i + 1; j < hypotheses.Count; j++) { double sep = PairwiseSeparation(hypotheses[i], hypotheses[j], protocol, run); minimum = Math.Min(minimum, sep); totalSeparation += sep; pairs++; }
            }
            result.ExpectedPosteriorEntropyBits = Math.Max(0, weightedEntropy); result.ExpectedInformationGainBits = Math.Max(0, result.PriorEntropyBits - result.ExpectedPosteriorEntropyBits); result.MinimumPairwiseSeparation = Double.IsPositiveInfinity(minimum) ? 0 : minimum; result.MeanPairwiseSeparation = pairs == 0 ? 0 : totalSeparation / pairs;
            result.Audit.Add("EIG uses prior-predictive synthetic outcomes at each declared hypothesis mean; it is a deterministic design-comparison approximation, not observed evidence.");
            result.Audit.Add("Pairwise separation is the sum of equal-variance Gaussian Bhattacharyya distances over declared outcomes and runs.");
            return result;
        }

        public static List<DiscoveryHypothesisPosterior> Update(DiscoveryProtocol protocol, IList<DiscoveryRun> runs, IList<DiscoveryObservation> observations)
        {
            List<DiscoveryHypothesis> hypotheses = ActiveHypotheses(protocol); double[] prior = Priors(hypotheses); Dictionary<string, DiscoveryRun> byRun = (runs ?? new List<DiscoveryRun>()).Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase); List<double> logs = new List<double>();
            for (int h = 0; h < hypotheses.Count; h++)
            {
                double log = Math.Log(Math.Max(Epsilon, prior[h]));
                foreach (DiscoveryObservation o in observations ?? new List<DiscoveryObservation>()) { DiscoveryRun run; if (o == null || !byRun.TryGetValue(o.RunId ?? "", out run)) continue; DiscoveryPrediction p = Prediction(hypotheses[h], o.OutcomeId); if (p == null) continue; double sigma = Math.Sqrt(Math.Max(Epsilon, p.Sigma * p.Sigma + o.StandardError * o.StandardError)); double z = (o.Value - Predict(p, run)) / sigma; log += -0.5 * Math.Log(2 * Math.PI * sigma * sigma) - 0.5 * z * z; }
                logs.Add(log);
            }
            double[] posterior = NormalizeLogs(logs); List<DiscoveryHypothesisPosterior> rows = new List<DiscoveryHypothesisPosterior>();
            for (int i = 0; i < hypotheses.Count; i++) { double bestOther = Double.NegativeInfinity; for (int j = 0; j < logs.Count; j++) if (j != i) bestOther = Math.Max(bestOther, logs[j]); rows.Add(new DiscoveryHypothesisPosterior { HypothesisId = hypotheses[i].Id, HypothesisName = hypotheses[i].Name, Prior = prior[i], LogLikelihood = logs[i] - Math.Log(Math.Max(Epsilon, prior[i])), Posterior = posterior[i], LogBayesFactorVsBestAlternative = logs.Count < 2 ? 0 : logs[i] - bestOther }); }
            return rows.OrderByDescending(x => x.Posterior).ToList();
        }

        public static List<DiscoveryPowerPlan> PowerPlans(DiscoveryProtocol protocol)
        {
            List<DiscoveryPowerPlan> plans = new List<DiscoveryPowerPlan>(); foreach (DiscoveryOutcome outcome in (protocol == null ? new List<DiscoveryOutcome>() : protocol.Outcomes ?? new List<DiscoveryOutcome>()).Where(x => Active(x.Status)))
            {
                double alpha = Clamp(outcome.Alpha, 1e-8, 0.5), power = Clamp(outcome.TargetPower, 0.500001, 0.999999), sigma = Math.Max(Epsilon, Math.Abs(outcome.NoiseSigma)), effect = Math.Max(Epsilon, Math.Abs(outcome.RelevantEffect)); double zAlpha = InverseNormal(1 - alpha / (String.Equals(outcome.Direction, "ONE_SIDED", StringComparison.OrdinalIgnoreCase) ? 1 : 2)); double zPower = InverseNormal(power); int perArm = (int)Math.Ceiling(2 * Math.Pow((zAlpha + zPower) * sigma / effect, 2)); plans.Add(new DiscoveryPowerPlan { OutcomeId = outcome.Id, PerArmSample = Math.Max(2, perArm), TotalSample = Math.Max(4, 2 * perArm), Alpha = alpha, Power = power, Sigma = sigma, Effect = effect, Assumption = "Two independent balanced Gaussian arms, known common variance, fixed sample and " + (outcome.Direction ?? "TWO_SIDED") + " z approximation." });
            } return plans;
        }

        public static DiscoveryReproducibilityProfile Reproducibility(DiscoveryProtocol protocol, IList<DiscoveryRun> runs, IList<DiscoveryObservation> observations)
        {
            DiscoveryReproducibilityProfile p = new DiscoveryReproducibilityProfile(); Dictionary<string, DiscoveryRun> byRun = (runs ?? new List<DiscoveryRun>()).Where(x => x != null).ToDictionary(x => x.Id ?? x.Sequence.ToString(CultureInfo.InvariantCulture), x => x, StringComparer.OrdinalIgnoreCase); Dictionary<string, List<double>> cells = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
            foreach (DiscoveryObservation o in observations ?? new List<DiscoveryObservation>()) { DiscoveryRun run; if (o == null || !byRun.TryGetValue(o.RunId ?? "", out run)) continue; string cell = CellKey(run) + "|" + (o.OutcomeId ?? ""); List<double> values; if (!cells.TryGetValue(cell, out values)) { values = new List<double>(); cells[cell] = values; } values.Add(o.Value); p.Observations++; }
            p.UniqueCells = cells.Count; p.ReplicatedCells = cells.Count(x => x.Value.Count > 1); p.ReplicationCoverage = p.UniqueCells == 0 ? 0 : (double)p.ReplicatedCells / p.UniqueCells;
            double withinSS = 0, betweenSS = 0, grand = cells.SelectMany(x => x.Value).DefaultIfEmpty(0).Average(); int withinDf = 0; foreach (List<double> values in cells.Values) { double mean = values.Average(); withinSS += values.Sum(v => (v - mean) * (v - mean)); withinDf += Math.Max(0, values.Count - 1); betweenSS += values.Count * (mean - grand) * (mean - grand); } double withinVar = withinDf == 0 ? 0 : withinSS / withinDf; double betweenVar = Math.Max(0, cells.Count <= 1 ? 0 : betweenSS / Math.Max(1, cells.Count - 1) - withinVar); p.PooledWithinCellSigma = Math.Sqrt(Math.Max(0, withinVar)); p.IntraclassCorrelation = betweenVar + withinVar <= Epsilon ? 0 : betweenVar / (betweenVar + withinVar);
            int signPairs = 0, signAgree = 0; foreach (List<double> values in cells.Values.Where(x => x.Count > 1)) for (int i = 1; i < values.Count; i++) { signPairs++; if (Math.Sign(values[i]) == Math.Sign(values[0])) signAgree++; } p.SignAgreement = signPairs == 0 ? 0 : (double)signAgree / signPairs;
            if (p.ReplicatedCells == 0) p.Warnings.Add("No replicated factor/outcome cells exist; within-cell reproducibility is unidentified."); if (p.ReplicationCoverage < 0.5) p.Warnings.Add("Fewer than half of observed cells are replicated."); return p;
        }

        public static DiscoveryPortfolio OptimizePortfolio(IList<DiscoveryExperimentCandidate> candidates, double budget, double riskCeiling)
        {
            List<DiscoveryExperimentCandidate> c = (candidates ?? new List<DiscoveryExperimentCandidate>()).Where(x => x != null).Take(24).ToList(); DiscoveryPortfolio best = new DiscoveryPortfolio(); int explored = 0; bool exhaustive = c.Count <= 22; if (exhaustive) SearchPortfolio(c, 0, new List<DiscoveryExperimentCandidate>(), 0, 0, 0, Math.Max(0, budget), Math.Max(0, riskCeiling), ref best, ref explored); else { foreach (DiscoveryExperimentCandidate x in c.OrderByDescending(x => x.InformationValue / Math.Max(Epsilon, x.Cost + x.Risk)).ThenBy(x => x.Id)) { if (best.Cost + x.Cost <= budget && best.Risk + x.Risk <= riskCeiling && Compatible(x, best.SelectedIds, c)) { best.SelectedIds.Add(x.Id); best.Cost += x.Cost; best.Risk += x.Risk; best.InformationValue += x.InformationValue; } } explored = c.Count; }
            best.ExploredStates = explored; best.GloballyOptimalWithinBound = exhaustive; best.Fingerprint = HashText(String.Join("|", best.SelectedIds.OrderBy(x => x).ToArray()) + "|" + best.Cost.ToString("R", CultureInfo.InvariantCulture) + "|" + best.Risk.ToString("R", CultureInfo.InvariantCulture)); best.Audit.Add(exhaustive ? "Complete subset search over the bounded candidate set." : "Deterministic information-per-burden greedy selection because candidate count exceeded exact-search bound."); return best;
        }

        public static string ProtocolFingerprint(DiscoveryProtocol p)
        {
            if (p == null) return HashText("NULL_PROTOCOL"); StringBuilder b = new StringBuilder(); b.Append("DISCOVERY_PROTOCOL_V1|").Append(p.Id).Append('|').Append(p.ParentProtocolId).Append('|').Append(p.SourceKind).Append('|').Append(p.SourceId).Append('|').Append(p.SourceFingerprint).Append('|').Append(p.Name).Append('|').Append(p.Charter).Append('|').Append(p.Status).Append('|').Append(p.DesignMethod).Append('|').Append(p.Revision).Append('|').Append(p.RequestedRuns).Append('|').Append(p.Replicates).Append('|').Append(p.CandidatePool).Append('|').Append(p.Seed).Append('|').Append(p.Budget.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(p.RiskCeiling.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(p.RandomizationRule).Append('|').Append(p.BlockingRule).Append('|').Append(p.StoppingRule).Append('|').Append(p.AnalysisPlan).Append('|').Append(p.ExclusionRule);
            foreach (DiscoveryFactor f in (p.Factors ?? new List<DiscoveryFactor>()).OrderBy(x => x.Id)) b.Append("|F:").Append(f.Id).Append(':').Append(f.Name).Append(':').Append(f.Kind).Append(':').Append(f.Status).Append(':').Append(f.Minimum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.Maximum.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.Levels).Append(':').Append(f.PerRunCost.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.PerRunRisk.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.Controllable);
            foreach (DiscoveryOutcome o in (p.Outcomes ?? new List<DiscoveryOutcome>()).OrderBy(x => x.Id)) b.Append("|O:").Append(o.Id).Append(':').Append(o.Name).Append(':').Append(o.Scale).Append(':').Append(o.Direction).Append(':').Append(o.NoiseSigma.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(o.RelevantEffect.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(o.Alpha.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(o.TargetPower.ToString("R", CultureInfo.InvariantCulture));
            foreach (DiscoveryHypothesis h in (p.Hypotheses ?? new List<DiscoveryHypothesis>()).OrderBy(x => x.Id)) { b.Append("|H:").Append(h.Id).Append(':').Append(h.Name).Append(':').Append(h.Family).Append(':').Append(h.Status).Append(':').Append(h.PriorWeight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(h.Statement); foreach (DiscoveryPrediction q in (h.Predictions ?? new List<DiscoveryPrediction>()).OrderBy(x => x.Id)) { b.Append("|P:").Append(q.Id).Append(':').Append(q.OutcomeId).Append(':').Append(q.Distribution).Append(':').Append(q.Intercept.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(q.Sigma.ToString("R", CultureInfo.InvariantCulture)); foreach (KeyValuePair<string, double> term in q.Terms.OrderBy(x => x.Key)) b.Append(':').Append(term.Key).Append('=').Append(term.Value.ToString("R", CultureInfo.InvariantCulture)); } }
            foreach (string a in (p.Assumptions ?? new List<string>()).OrderBy(x => x)) b.Append("|A:").Append(a); foreach (string e in (p.EvidenceNodeIds ?? new List<string>()).OrderBy(x => x)) b.Append("|E:").Append(e); return HashText(b.ToString());
        }

        public static string PreregistrationFingerprint(DiscoveryProtocol p)
        {
            return HashText("PREREGISTRATION_V1|" + ProtocolFingerprint(p) + "|" + (p == null ? "" : p.StoppingRule) + "|" + (p == null ? "" : p.AnalysisPlan) + "|" + (p == null ? "" : p.ExclusionRule) + "|" + (p == null ? "" : p.RandomizationRule) + "|" + (p == null ? "" : p.BlockingRule));
        }

        public static List<string> Audit(DiscoveryProtocol p)
        {
            List<string> a = new List<string>(); if (p == null) { a.Add("BLOCKER · NULL_PROTOCOL · No protocol exists."); return a; } List<DiscoveryFactor> factors = ActiveFactors(p); List<DiscoveryOutcome> outcomes = (p.Outcomes ?? new List<DiscoveryOutcome>()).Where(x => Active(x.Status)).ToList(); List<DiscoveryHypothesis> hypotheses = ActiveHypotheses(p);
            if (factors.Count == 0) a.Add("BLOCKER · NO_FACTORS · A design cannot vary or observe any coordinates."); if (outcomes.Count == 0) a.Add("BLOCKER · NO_OUTCOMES · No preregistered response variable exists."); if (hypotheses.Count < 2) a.Add("WARN · NO_COMPETITION · Fewer than two active hypotheses prevents model discrimination."); if (hypotheses.Sum(x => Math.Max(0, x.PriorWeight)) <= 0) a.Add("BLOCKER · INVALID_PRIORS · Active hypothesis prior weights sum to zero.");
            foreach (DiscoveryFactor f in factors) { if (!(f.Maximum > f.Minimum)) a.Add("BLOCKER · DEGENERATE_FACTOR · " + (f.Name ?? f.Id)); if (f.PerRunCost < 0 || f.PerRunRisk < 0) a.Add("BLOCKER · NEGATIVE_BURDEN · " + (f.Name ?? f.Id)); }
            foreach (DiscoveryOutcome o in outcomes) { if (!(o.NoiseSigma > 0)) a.Add("BLOCKER · INVALID_NOISE · " + (o.Name ?? o.Id)); if (!(o.RelevantEffect > 0)) a.Add("WARN · NONPOSITIVE_MDE · " + (o.Name ?? o.Id)); }
            foreach (DiscoveryHypothesis h in hypotheses) foreach (DiscoveryPrediction q in h.Predictions ?? new List<DiscoveryPrediction>()) { if (!outcomes.Any(o => Eq(o.Id, q.OutcomeId))) a.Add("BLOCKER · ORPHAN_PREDICTION · " + (h.Name ?? h.Id) + " → " + (q.OutcomeId ?? "∅")); foreach (string term in q.Terms.Keys) if (!TermResolvable(term, factors)) a.Add("WARN · UNKNOWN_PREDICTION_TERM · " + (h.Name ?? h.Id) + " → " + term); }
            if (String.IsNullOrWhiteSpace(p.StoppingRule)) a.Add("BLOCKER · NO_STOPPING_RULE · Preregistration boundary is incomplete."); if (String.IsNullOrWhiteSpace(p.AnalysisPlan)) a.Add("BLOCKER · NO_ANALYSIS_PLAN · Preregistration boundary is incomplete."); if (String.IsNullOrWhiteSpace(p.ExclusionRule)) a.Add("BLOCKER · NO_EXCLUSION_RULE · Preregistration boundary is incomplete."); if (a.Count == 0) a.Add("PASS · STRUCTURAL_AUDIT · Explicit factors, outcomes, priors and preregistration fields are internally coherent."); return a;
        }

        private static List<DiscoveryRun> FullFactorial(List<DiscoveryFactor> factors, int cap)
        {
            List<DiscoveryRun> runs = new List<DiscoveryRun> { NewRun(1) }; foreach (DiscoveryFactor f in factors) { List<DiscoveryRun> expanded = new List<DiscoveryRun>(); int levels = Math.Max(2, Math.Min(32, f.Levels)); foreach (DiscoveryRun parent in runs) for (int level = 0; level < levels; level++) { DiscoveryRun child = CloneRun(parent); child.Levels[f.Id] = Denormalize(f, levels == 1 ? 0.5 : (double)level / (levels - 1)); expanded.Add(child); if (expanded.Count >= cap) break; } runs = expanded; if (runs.Count >= cap) break; } Resequence(runs); return runs;
        }

        private static List<DiscoveryRun> FractionalFactorial(List<DiscoveryFactor> factors, int count, int seed)
        {
            int n = Math.Max(2, count); List<DiscoveryRun> runs = new List<DiscoveryRun>(); for (int r = 0; r < n; r++) { DiscoveryRun run = NewRun(r + 1); for (int j = 0; j < factors.Count; j++) { int bit = Parity((r + Math.Abs(seed)) & ((1 << Math.Min(30, j + 1)) - 1)); if (j >= 30) bit = Parity((r + 1) * (j + 3) + seed); run.Levels[factors[j].Id] = bit == 0 ? factors[j].Minimum : factors[j].Maximum; } runs.Add(run); } return runs;
        }

        private static List<DiscoveryRun> LatinHypercube(List<DiscoveryFactor> factors, int count, int seed)
        {
            List<DiscoveryRun> runs = Enumerable.Range(0, count).Select(i => NewRun(i + 1)).ToList(); for (int j = 0; j < factors.Count; j++) { int step = CoprimeStep(count, 2 * j + 1 + Math.Abs(seed % Math.Max(1, count))); int offset = PositiveMod(seed * 31 + j * 17, count); for (int r = 0; r < count; r++) { int cell = PositiveMod(offset + r * step, count); double u = (cell + RadicalInverse(r + 1 + Math.Abs(seed), Prime(j + 1))) / count; runs[r].Levels[factors[j].Id] = Denormalize(factors[j], u); } } return runs;
        }

        private static List<DiscoveryRun> HaltonDesign(List<DiscoveryFactor> factors, int count, int seed)
        {
            List<DiscoveryRun> runs = new List<DiscoveryRun>(); int skip = 17 + Math.Abs(seed % 4093); for (int r = 0; r < count; r++) { DiscoveryRun run = NewRun(r + 1); for (int j = 0; j < factors.Count; j++) run.Levels[factors[j].Id] = Denormalize(factors[j], RadicalInverse(skip + r + 1, Prime(j + 1))); runs.Add(run); } return runs;
        }

        private static List<DiscoveryRun> DOptimal(List<DiscoveryFactor> factors, int count, int poolSize, int seed)
        {
            List<DiscoveryRun> pool = HaltonDesign(factors, Math.Max(count, poolSize), seed); List<string> terms = Terms(factors, true); List<DiscoveryRun> chosen = new List<DiscoveryRun>(); HashSet<int> used = new HashSet<int>();
            for (int slot = 0; slot < count && slot < pool.Count; slot++) { int best = -1; double bestScore = Double.NegativeInfinity; for (int i = 0; i < pool.Count; i++) { if (used.Contains(i)) continue; List<DiscoveryRun> trial = new List<DiscoveryRun>(chosen); trial.Add(pool[i]); double score = RegularizedLogDet(Matrix(trial, factors, terms), 1e-8); if (score > bestScore + Epsilon || (Math.Abs(score - bestScore) <= Epsilon && i < best)) { best = i; bestScore = score; } } if (best < 0) break; used.Add(best); chosen.Add(pool[best]); }
            // Deterministic Fedorov-style point exchange converges to a local log-det optimum.
            for (int pass = 0; pass < 6; pass++) { bool improved = false; for (int s = 0; s < chosen.Count; s++) { double baseline = RegularizedLogDet(Matrix(chosen, factors, terms), 1e-8); int replacement = -1; double best = baseline; for (int i = 0; i < pool.Count; i++) { if (used.Contains(i)) continue; DiscoveryRun old = chosen[s]; chosen[s] = pool[i]; double score = RegularizedLogDet(Matrix(chosen, factors, terms), 1e-8); chosen[s] = old; if (score > best + 1e-9) { best = score; replacement = i; } } if (replacement >= 0) { int oldIndex = pool.IndexOf(chosen[s]); used.Remove(oldIndex); chosen[s] = pool[replacement]; used.Add(replacement); improved = true; } } if (!improved) break; }
            Resequence(chosen); return chosen;
        }

        private static void ApplyReplicationAndEconomics(DiscoveryProtocol p, List<DiscoveryFactor> factors, List<DiscoveryRun> source)
        {
            int reps = Math.Max(1, Math.Min(64, p.Replicates)); List<DiscoveryRun> all = new List<DiscoveryRun>(); foreach (DiscoveryRun run in source) for (int replicate = 1; replicate <= reps; replicate++) { DiscoveryRun copy = CloneRun(run); copy.Replicate = replicate; copy.Cost = factors.Sum(f => Math.Max(0, f.PerRunCost)); copy.Risk = factors.Sum(f => Math.Max(0, f.PerRunRisk)); all.Add(copy); } source.Clear(); source.AddRange(all); Resequence(source);
        }

        private static List<string> Terms(List<DiscoveryFactor> factors, bool interactions) { List<string> terms = new List<string> { "1" }; terms.AddRange(factors.Select(x => x.Id)); if (interactions && factors.Count <= 24) for (int i = 0; i < factors.Count; i++) for (int j = i + 1; j < factors.Count; j++) terms.Add(factors[i].Id + "*" + factors[j].Id); return terms; }
        private static double[,] Matrix(IList<DiscoveryRun> runs, List<DiscoveryFactor> factors, List<string> terms) { int n = runs == null ? 0 : runs.Count, p = terms.Count; double[,] x = new double[n, p]; Dictionary<string, DiscoveryFactor> byId = factors.ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase); for (int r = 0; r < n; r++) for (int c = 0; c < p; c++) x[r, c] = TermValue(terms[c], runs[r], byId); return x; }
        private static double TermValue(string term, DiscoveryRun run, Dictionary<string, DiscoveryFactor> factors) { if (term == "1") return 1; double value = 1; foreach (string id in term.Split('*')) { DiscoveryFactor f; double raw; if (!factors.TryGetValue(id, out f) || !run.Levels.TryGetValue(id, out raw)) return 0; value *= Normalize(f, raw); } return value; }
        private static double Predict(DiscoveryPrediction p, DiscoveryRun run) { double mean = p.Intercept; foreach (KeyValuePair<string, double> term in p.Terms) { double value = 1; foreach (string id in term.Key.Split('*')) { double x; if (!run.Levels.TryGetValue(id, out x)) { value = 0; break; } value *= x; } mean += term.Value * value; } return mean; }
        private static DiscoveryPrediction Prediction(DiscoveryHypothesis h, string outcome) { return (h.Predictions ?? new List<DiscoveryPrediction>()).FirstOrDefault(x => Eq(x.OutcomeId, outcome)); }
        private static double LogPredictiveAgreement(DiscoveryHypothesis truth, DiscoveryHypothesis candidate, DiscoveryProtocol p, DiscoveryRun run) { double log = 0; foreach (DiscoveryOutcome o in p.Outcomes.Where(x => Active(x.Status))) { DiscoveryPrediction a = Prediction(truth, o.Id), b = Prediction(candidate, o.Id); if (a == null || b == null) continue; double variance = Math.Max(Epsilon, a.Sigma * a.Sigma + b.Sigma * b.Sigma); double delta = Predict(a, run) - Predict(b, run); log += -0.5 * delta * delta / variance - 0.5 * Math.Log(2 * Math.PI * variance); } return log; }
        private static double PairwiseSeparation(DiscoveryHypothesis a, DiscoveryHypothesis b, DiscoveryProtocol p, DiscoveryRun run) { double d = 0; foreach (DiscoveryOutcome o in p.Outcomes.Where(x => Active(x.Status))) { DiscoveryPrediction x = Prediction(a, o.Id), y = Prediction(b, o.Id); if (x == null || y == null) continue; double sx = Math.Max(Epsilon, x.Sigma), sy = Math.Max(Epsilon, y.Sigma), v = 0.5 * (sx * sx + sy * sy), delta = Predict(x, run) - Predict(y, run); d += 0.125 * delta * delta / v + 0.5 * Math.Log(v / (sx * sy)); } return d; }

        private static List<DiscoveryAlias> Aliases(double[,] x, List<string> terms) { List<DiscoveryAlias> rows = new List<DiscoveryAlias>(); int n = x.GetLength(0), p = x.GetLength(1); for (int i = 1; i < p; i++) for (int j = i + 1; j < p; j++) { double mi = 0, mj = 0; for (int r = 0; r < n; r++) { mi += x[r, i]; mj += x[r, j]; } mi /= Math.Max(1, n); mj /= Math.Max(1, n); double cross = 0, vi = 0, vj = 0; for (int r = 0; r < n; r++) { double di = x[r, i] - mi, dj = x[r, j] - mj; cross += di * dj; vi += di * di; vj += dj * dj; } double corr = vi <= Epsilon || vj <= Epsilon ? 0 : cross / Math.Sqrt(vi * vj); if (Math.Abs(corr) >= 0.25) rows.Add(new DiscoveryAlias { LeftTerm = terms[i], RightTerm = terms[j], Correlation = corr }); } return rows.OrderByDescending(a => Math.Abs(a.Correlation)).Take(1024).ToList(); }
        private static double[,] CrossProduct(double[,] x) { int n = x.GetLength(0), p = x.GetLength(1); double[,] a = new double[p, p]; for (int i = 0; i < p; i++) for (int j = i; j < p; j++) { double s = 0; for (int r = 0; r < n; r++) s += x[r, i] * x[r, j]; a[i, j] = a[j, i] = s; } return a; }
        private static int Rank(double[,] source) { int n = source.GetLength(0), p = source.GetLength(1), rank = 0; double[,] a = (double[,])source.Clone(); for (int col = 0; col < p && rank < n; col++) { int pivot = rank; for (int r = rank + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r; if (Math.Abs(a[pivot, col]) <= Epsilon) continue; SwapRows(a, rank, pivot); double div = a[rank, col]; for (int c = col; c < p; c++) a[rank, c] /= div; for (int r = 0; r < n; r++) if (r != rank) { double f = a[r, col]; for (int c = col; c < p; c++) a[r, c] -= f * a[rank, c]; } rank++; } return rank; }
        private static double LogDeterminant(double[,] source) { int n = source.GetLength(0); if (n == 0) return Double.NegativeInfinity; double[,] a = (double[,])source.Clone(); double log = 0; for (int col = 0; col < n; col++) { int pivot = col; for (int r = col + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r; double v = Math.Abs(a[pivot, col]); if (v <= Epsilon) return Double.NegativeInfinity; SwapRows(a, col, pivot); log += Math.Log(v); for (int r = col + 1; r < n; r++) { double f = a[r, col] / a[col, col]; for (int c = col + 1; c < n; c++) a[r, c] -= f * a[col, c]; } } return log; }
        private static double RegularizedLogDet(double[,] x, double ridge) { double[,] a = CrossProduct(x); for (int i = 0; i < a.GetLength(0); i++) a[i, i] += ridge; return LogDeterminant(a); }
        private static bool TryInverse(double[,] source, out double[,] inverse) { int n = source.GetLength(0); inverse = new double[n, n]; double[,] a = (double[,])source.Clone(); for (int i = 0; i < n; i++) inverse[i, i] = 1; for (int col = 0; col < n; col++) { int pivot = col; for (int r = col + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r; if (Math.Abs(a[pivot, col]) <= Epsilon) return false; SwapRows(a, col, pivot); SwapRows(inverse, col, pivot); double div = a[col, col]; for (int c = 0; c < n; c++) { a[col, c] /= div; inverse[col, c] /= div; } for (int r = 0; r < n; r++) if (r != col) { double f = a[r, col]; for (int c = 0; c < n; c++) { a[r, c] -= f * a[col, c]; inverse[r, c] -= f * inverse[col, c]; } } } return true; }
        private static void SwapRows(double[,] a, int x, int y) { if (x == y) return; for (int c = 0; c < a.GetLength(1); c++) { double t = a[x, c]; a[x, c] = a[y, c]; a[y, c] = t; } }
        private static double Frobenius(double[,] a) { double s = 0; foreach (double x in a) s += x * x; return Math.Sqrt(s); }
        private static double QuadraticRow(double[,] x, int row, double[,] a) { int p = x.GetLength(1); double s = 0; for (int i = 0; i < p; i++) for (int j = 0; j < p; j++) s += x[row, i] * a[i, j] * x[row, j]; return s; }

        private static void SearchPortfolio(List<DiscoveryExperimentCandidate> c, int index, List<DiscoveryExperimentCandidate> selected, double value, double cost, double risk, double budget, double ceiling, ref DiscoveryPortfolio best, ref int explored)
        { explored++; if (index >= c.Count) { List<string> ids = selected.Select(q => q.Id).ToList(); if (!AllRequirements(selected, ids) || !AllMandatory(c, ids)) return; if (value > best.InformationValue + Epsilon || (Math.Abs(value - best.InformationValue) <= Epsilon && cost < best.Cost)) { best.SelectedIds = ids; best.InformationValue = value; best.Cost = cost; best.Risk = risk; } return; } DiscoveryExperimentCandidate x = c[index]; if (!x.Mandatory) SearchPortfolio(c, index + 1, selected, value, cost, risk, budget, ceiling, ref best, ref explored); if (cost + x.Cost <= budget && risk + x.Risk <= ceiling && !selected.Any(s => (x.ExcludesIds ?? new List<string>()).Any(e => Eq(e, s.Id)) || (s.ExcludesIds ?? new List<string>()).Any(e => Eq(e, x.Id)))) { selected.Add(x); SearchPortfolio(c, index + 1, selected, value + Math.Max(0, x.InformationValue), cost + Math.Max(0, x.Cost), risk + Math.Max(0, x.Risk), budget, ceiling, ref best, ref explored); selected.RemoveAt(selected.Count - 1); } }
        private static bool AllRequirements(List<DiscoveryExperimentCandidate> selected, List<string> ids) { return selected.All(x => (x.RequiresIds ?? new List<string>()).All(r => ids.Any(id => Eq(id, r)))); }
        private static bool AllMandatory(List<DiscoveryExperimentCandidate> all, List<string> ids) { return all.Where(x => x.Mandatory).All(x => ids.Any(id => Eq(id, x.Id))); }
        private static bool Compatible(DiscoveryExperimentCandidate x, List<string> ids, List<DiscoveryExperimentCandidate> all) { if ((x.ExcludesIds ?? new List<string>()).Any(e => ids.Any(id => Eq(id, e)))) return false; if ((x.RequiresIds ?? new List<string>()).Any(r => !ids.Any(id => Eq(id, r)))) return false; return !all.Where(a => ids.Any(id => Eq(id, a.Id))).Any(a => (a.ExcludesIds ?? new List<string>()).Any(e => Eq(e, x.Id))); }

        private static List<DiscoveryFactor> ActiveFactors(DiscoveryProtocol p) { return p == null ? new List<DiscoveryFactor>() : (p.Factors ?? new List<DiscoveryFactor>()).Where(x => Active(x.Status)).Take(64).ToList(); }
        private static List<DiscoveryHypothesis> ActiveHypotheses(DiscoveryProtocol p) { return p == null ? new List<DiscoveryHypothesis>() : (p.Hypotheses ?? new List<DiscoveryHypothesis>()).Where(x => Active(x.Status)).Take(128).ToList(); }
        private static double[] Priors(List<DiscoveryHypothesis> hypotheses) { double sum = hypotheses.Sum(x => Math.Max(0, x.PriorWeight)); if (sum <= Epsilon) return hypotheses.Select(x => hypotheses.Count == 0 ? 0 : 1.0 / hypotheses.Count).ToArray(); return hypotheses.Select(x => Math.Max(0, x.PriorWeight) / sum).ToArray(); }
        private static double Entropy(double[] p) { double h = 0; foreach (double x in p) if (x > Epsilon) h -= x * Math.Log(x, 2); return h; }
        private static double[] NormalizeLogs(List<double> logs) { if (logs == null || logs.Count == 0) return new double[0]; double max = logs.Max(); double[] w = logs.Select(x => Math.Exp(x - max)).ToArray(); double sum = w.Sum(); return w.Select(x => x / Math.Max(Epsilon, sum)).ToArray(); }
        private static bool TermResolvable(string term, List<DiscoveryFactor> factors) { return (term ?? "").Split('*').All(id => factors.Any(f => Eq(f.Id, id))); }
        private static string CellKey(DiscoveryRun run) { return String.Join("|", (run.Levels ?? new Dictionary<string, double>()).OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value.ToString("R", CultureInfo.InvariantCulture)).ToArray()); }
        private static DiscoveryRun NewRun(int sequence) { return new DiscoveryRun { Sequence = sequence, Id = "design-run-" + sequence.ToString("D5", CultureInfo.InvariantCulture) }; }
        private static DiscoveryRun CloneRun(DiscoveryRun source) { DiscoveryRun r = new DiscoveryRun { Sequence = source.Sequence, Id = source.Id, Block = source.Block, Replicate = source.Replicate, Cost = source.Cost, Risk = source.Risk }; foreach (KeyValuePair<string, double> x in source.Levels) r.Levels[x.Key] = x.Value; return r; }
        private static void Resequence(List<DiscoveryRun> runs) { for (int i = 0; i < runs.Count; i++) { runs[i].Sequence = i + 1; runs[i].Id = "design-run-" + (i + 1).ToString("D5", CultureInfo.InvariantCulture); } }
        private static double Normalize(DiscoveryFactor f, double value) { double span = f.Maximum - f.Minimum; return Math.Abs(span) <= Epsilon ? 0 : 2 * (value - f.Minimum) / span - 1; }
        private static double Denormalize(DiscoveryFactor f, double u) { u = Clamp(u, 0, 1); if (String.Equals(f.Kind, "CATEGORICAL", StringComparison.OrdinalIgnoreCase) || String.Equals(f.Kind, "ORDINAL", StringComparison.OrdinalIgnoreCase)) { int levels = Math.Max(2, f.Levels); u = Math.Round(u * (levels - 1)) / (levels - 1); } return f.Minimum + u * (f.Maximum - f.Minimum); }
        private static int Parity(int x) { uint v = unchecked((uint)x); v ^= v >> 16; v ^= v >> 8; v ^= v >> 4; v &= 0xf; return (0x6996 >> (int)v) & 1; }
        private static int PositiveMod(int x, int n) { if (n <= 0) return 0; int r = x % n; return r < 0 ? r + n : r; }
        private static int CoprimeStep(int n, int start) { if (n <= 1) return 1; int x = Math.Max(1, PositiveMod(start, n)); while (Gcd(x, n) != 1) x = x % (n - 1) + 1; return x; }
        private static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return Math.Abs(a); }
        private static int Prime(int index) { int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293 }; return primes[Math.Max(0, Math.Min(primes.Length - 1, index - 1))]; }
        private static double RadicalInverse(int n, int b) { double value = 0, f = 1.0 / b; while (n > 0) { value += f * (n % b); n /= b; f /= b; } return value; }
        private static double SafePositive(double value, double fallback) { return Double.IsNaN(value) || value < 0 ? fallback : value; }
        private static double Clamp(double value, double min, double max) { return Math.Max(min, Math.Min(max, value)); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETRACTED") && !Eq(status, "REJECTED"); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static string HashText(string value) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
        private static double InverseNormal(double p) { p = Clamp(p, 1e-12, 1 - 1e-12); double[] a = { -39.69683028665376, 220.9460984245205, -275.9285104469687, 138.3577518672690, -30.66479806614716, 2.506628277459239 }; double[] b = { -54.47609879822406, 161.5858368580409, -155.6989798598866, 66.80131188771972, -13.28068155288572 }; double[] c = { -0.007784894002430293, -0.3223964580411365, -2.400758277161838, -2.549732539343734, 4.374664141464968, 2.938163982698783 }; double[] d = { 0.007784695709041462, 0.3224671290700398, 2.445134137142996, 3.754408661907416 }; double q, r; if (p < 0.02425) { q = Math.Sqrt(-2 * Math.Log(p)); return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1); } if (p > 0.97575) { q = Math.Sqrt(-2 * Math.Log(1 - p)); return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1); } q = p - 0.5; r = q * q; return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q / (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1); }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class LogicFact
    {
        public string Id { get; set; }
        public string WorldId { get; set; }
        public string Literal { get; set; }
        public string EpistemicClass { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public double Weight { get; set; }
        public string SourceFingerprint { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public LogicFact() { EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (EpistemicClass ?? "ASSERTION") + "   ·   " + (Literal ?? "∅") + "   ·   w=" + Weight.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class LogicRule
    {
        public string Id { get; set; }
        public string WorldId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<string> Premises { get; set; }
        public string Consequent { get; set; }
        public List<string> Defeaters { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public LogicRule() { Premises = new List<string>(); Defeaters = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Kind ?? "DEDUCTIVE") + "   ·   " + (Name ?? "RULE") + "   ·   " + String.Join(" & ", Premises.ToArray()) + " ⇒ " + (Consequent ?? "∅"); }
    }

    internal sealed class LogicWorld
    {
        public string Id { get; set; }
        public string ParentWorldId { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string Charter { get; set; }
        public List<string> ExcludedFactIds { get; set; }
        public LogicWorld() { ExcludedFactIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Kind ?? "ACTUAL") + "   ·   " + (Name ?? "WORLD") + (String.IsNullOrWhiteSpace(ParentWorldId) ? "" : "   ·   CHILD"); }
    }

    internal sealed class LogicTheory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string WorldId { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<string> FactIds { get; set; }
        public List<string> RuleIds { get; set; }
        public List<string> Questions { get; set; }
        public LogicTheory() { FactIds = new List<string>(); RuleIds = new List<string>(); Questions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? "THEORY") + "   ·   " + FactIds.Count.ToString(CultureInfo.InvariantCulture) + " facts / " + RuleIds.Count.ToString(CultureInfo.InvariantCulture) + " rules"; }
    }

    internal sealed class LogicHypothesis
    {
        public string Id { get; set; }
        public string WorldId { get; set; }
        public string Name { get; set; }
        public string TargetLiteral { get; set; }
        public string Status { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public double StructuralScore { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> Falsifiers { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public List<string> Notes { get; set; }
        public LogicHypothesis() { Assumptions = new List<string>(); Falsifiers = new List<string>(); EvidenceNodeIds = new List<string>(); Notes = new List<string>(); }
        public override string ToString() { return (Status ?? "OPEN") + "   ·   " + (Name ?? "HYPOTHESIS") + "   ·   " + (TargetLiteral ?? "∅") + "   ·   assumptions " + Assumptions.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class LogicSavedProof
    {
        public string Id { get; set; }
        public string WorldId { get; set; }
        public string QueryLiteral { get; set; }
        public string TruthStatus { get; set; }
        public string CreatedUtc { get; set; }
        public string KernelFingerprint { get; set; }
        public string PositiveProof { get; set; }
        public string NegativeProof { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public LogicSavedProof() { EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (TruthStatus ?? "NEITHER") + "   ·   " + (QueryLiteral ?? "∅") + "   ·   " + (CreatedUtc ?? ""); }
    }

    internal sealed class LogicLiteral
    {
        public bool Negated;
        public string Predicate;
        public List<string> Arguments = new List<string>();
        public string CanonicalAtom { get { return (Predicate ?? "").ToLowerInvariant() + "(" + String.Join(",", Arguments.Select(NormalizeTerm).ToArray()) + ")"; } }
        public string SignedKey { get { return (Negated ? "!" : "+") + CanonicalAtom; } }
        public string OppositeKey { get { return (Negated ? "+" : "!") + CanonicalAtom; } }
        public string Display { get { return (Negated ? "!" : "") + (Predicate ?? "") + "(" + String.Join(", ", Arguments.ToArray()) + ")"; } }
        public bool IsGround { get { return Arguments.All(a => !IsVariable(a)); } }

        public static LogicLiteral Parse(string text)
        {
            string s = (text ?? "").Trim(); bool neg = false; if (s.StartsWith("!", StringComparison.Ordinal)) { neg = true; s = s.Substring(1).Trim(); } else if (s.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase)) { neg = true; s = s.Substring(4).Trim(); }
            int open = s.IndexOf('('); int close = s.LastIndexOf(')'); LogicLiteral lit = new LogicLiteral { Negated = neg };
            if (open <= 0 || close < open) { lit.Predicate = NormalizeSymbol(s); return lit; }
            lit.Predicate = NormalizeSymbol(s.Substring(0, open)); foreach (string part in SplitArguments(s.Substring(open + 1, close - open - 1))) lit.Arguments.Add(CleanTerm(part)); return lit;
        }

        public LogicLiteral Instantiate(Dictionary<string, string> binding)
        {
            LogicLiteral next = new LogicLiteral { Negated = Negated, Predicate = Predicate }; foreach (string arg in Arguments) { string value; next.Arguments.Add(IsVariable(arg) && binding != null && binding.TryGetValue(arg, out value) ? value : arg); } return next;
        }

        public static bool IsVariable(string term) { return !String.IsNullOrWhiteSpace(term) && term.TrimStart().StartsWith("?", StringComparison.Ordinal); }
        private static string NormalizeSymbol(string s) { return (s ?? "").Trim().Replace(" ", "_").ToLowerInvariant(); }
        private static string NormalizeTerm(string s) { return CleanTerm(s).ToLowerInvariant(); }
        private static string CleanTerm(string s) { s = (s ?? "").Trim(); if (s.Length >= 2 && ((s[0] == '\'' && s[s.Length - 1] == '\'') || (s[0] == '"' && s[s.Length - 1] == '"'))) s = s.Substring(1, s.Length - 2); return s.Trim(); }
        private static List<string> SplitArguments(string s)
        {
            List<string> rows = new List<string>(); StringBuilder current = new StringBuilder(); char quote = '\0'; foreach (char c in s ?? "") { if ((c == '\'' || c == '"')) { if (quote == '\0') quote = c; else if (quote == c) quote = '\0'; current.Append(c); continue; } if (c == ',' && quote == '\0') { rows.Add(current.ToString()); current.Length = 0; } else current.Append(c); } if (current.Length > 0 || !String.IsNullOrEmpty(s)) rows.Add(current.ToString()); return rows;
        }
    }

    internal sealed class LogicDerivation
    {
        public string SignedLiteral;
        public string DisplayLiteral;
        public string FactId;
        public string RuleId;
        public string RuleName;
        public string Kind;
        public int Depth;
        public double Weight;
        public List<string> PremiseKeys = new List<string>();
        public List<string> EvidenceNodeIds = new List<string>();
    }

    internal sealed class LogicEvaluation
    {
        public string WorldId;
        public string KernelFingerprint;
        public int Rounds;
        public int SeedFacts;
        public int DerivedLiterals;
        public bool Truncated;
        public Dictionary<string, List<LogicDerivation>> Derivations = new Dictionary<string, List<LogicDerivation>>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PositiveAtoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> NegativeAtoms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasSigned(string key) { return Derivations.ContainsKey(key); }
        public string TruthOf(string query)
        {
            LogicLiteral q = LogicLiteral.Parse(query); bool forQuery = HasSigned(q.SignedKey); bool againstQuery = HasSigned(q.OppositeKey); if (forQuery && againstQuery) return "BOTH"; if (forQuery) return "TRUE_ONLY"; if (againstQuery) return "FALSE_ONLY"; return "NEITHER";
        }
        public LogicDerivation Best(string key) { List<LogicDerivation> rows; return Derivations.TryGetValue(key, out rows) ? rows.OrderBy(d => d.Depth).ThenByDescending(d => d.Weight).FirstOrDefault() : null; }
        public List<string> EvidenceFor(string query) { LogicLiteral q = LogicLiteral.Parse(query); List<string> ids = new List<string>(); CollectEvidence(q.SignedKey, ids, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); CollectEvidence(q.OppositeKey, ids, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); return ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList(); }
        private void CollectEvidence(string key, List<string> ids, HashSet<string> seen) { if (!seen.Add(key)) return; LogicDerivation d = Best(key); if (d == null) return; ids.AddRange(d.EvidenceNodeIds); foreach (string p in d.PremiseKeys) CollectEvidence(p, ids, seen); }
    }

    internal sealed class LogicContradiction
    {
        public string Atom;
        public LogicDerivation Positive;
        public LogicDerivation Negative;
        public int CombinedDepth;
        public List<string> EvidenceNodeIds = new List<string>();
        public override string ToString() { return "BOTH   ·   " + (Atom ?? "∅") + "   ·   proof depth " + CombinedDepth.ToString(CultureInfo.InvariantCulture) + "   ·   evidence " + EvidenceNodeIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class LogicAbductionCandidate
    {
        public string RuleId;
        public string RuleName;
        public string QueryLiteral;
        public double StructuralScore;
        public int SupportedPremises;
        public int ContradictedPremises;
        public List<string> Assumptions = new List<string>();
        public List<string> EvidenceNodeIds = new List<string>();
        public override string ToString() { return StructuralScore.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   " + (RuleName ?? "ABDUCTION") + "   ·   assume " + Assumptions.Count.ToString(CultureInfo.InvariantCulture) + "   ·   contradicted " + ContradictedPremises.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class LogicKernelLimits
    {
        public int MaxRounds = 32;
        public int MaxSignedLiterals = 8192;
        public int MaxBindingsPerRule = 4096;
        public int MaxDerivationsPerLiteral = 4;
        public int MaxWorldDepth = 64;
    }

    internal static class LogicInferenceKernel
    {
        public static LogicEvaluation Evaluate(IEnumerable<LogicFact> facts, IEnumerable<LogicRule> rules, IEnumerable<LogicWorld> worlds, string worldId, LogicKernelLimits limits)
        {
            limits = limits ?? new LogicKernelLimits(); List<LogicWorld> worldRows = (worlds ?? Enumerable.Empty<LogicWorld>()).ToList(); HashSet<string> visibleWorlds; HashSet<string> excludedFacts; ResolveWorld(worldRows, worldId, limits.MaxWorldDepth, out visibleWorlds, out excludedFacts); LogicEvaluation result = new LogicEvaluation { WorldId = worldId };
            foreach (LogicFact fact in (facts ?? Enumerable.Empty<LogicFact>()).Where(f => Active(f.Status) && !excludedFacts.Contains(f.Id ?? "") && (String.IsNullOrWhiteSpace(f.WorldId) || visibleWorlds.Contains(f.WorldId))))
            {
                LogicLiteral literal = LogicLiteral.Parse(fact.Literal); if (!literal.IsGround || String.IsNullOrWhiteSpace(literal.Predicate)) continue; LogicDerivation d = new LogicDerivation { SignedLiteral = literal.SignedKey, DisplayLiteral = literal.Display, FactId = fact.Id, Kind = "FACT", Depth = 0, Weight = Clamp01(fact.Weight <= 0 ? 1 : fact.Weight), EvidenceNodeIds = (fact.EvidenceNodeIds ?? new List<string>()).Take(256).ToList() }; AddDerivation(result, d, limits.MaxDerivationsPerLiteral); result.SeedFacts++;
            }
            List<LogicRule> activeRules = (rules ?? Enumerable.Empty<LogicRule>()).Where(r => Active(r.Status) && (String.IsNullOrWhiteSpace(r.WorldId) || visibleWorlds.Contains(r.WorldId))).OrderByDescending(r => r.Priority).ThenBy(r => r.Id).ToList();
            for (int round = 1; round <= limits.MaxRounds; round++)
            {
                bool changed = false; Dictionary<string, List<LogicLiteral>> literalIndex = BuildLiteralIndex(result); foreach (LogicRule rule in activeRules)
                {
                    List<LogicLiteral> premises = (rule.Premises ?? new List<string>()).Select(LogicLiteral.Parse).ToList(); LogicLiteral consequent = LogicLiteral.Parse(rule.Consequent); if (String.IsNullOrWhiteSpace(consequent.Predicate)) continue; List<BindingProof> bindings = MatchAll(premises, literalIndex, result, limits.MaxBindingsPerRule); foreach (BindingProof proof in bindings)
                    {
                        if (String.Equals(rule.Kind, "DEFEASIBLE", StringComparison.OrdinalIgnoreCase) && IsDefeated(rule, proof.Binding, result)) continue; LogicLiteral grounded = consequent.Instantiate(proof.Binding); if (!grounded.IsGround) continue; LogicDerivation d = new LogicDerivation { SignedLiteral = grounded.SignedKey, DisplayLiteral = grounded.Display, RuleId = rule.Id, RuleName = rule.Name, Kind = rule.Kind ?? "DEDUCTIVE", Depth = proof.Depth + 1, Weight = Math.Min(proof.Weight, RuleWeight(rule)), PremiseKeys = proof.PremiseKeys.ToList(), EvidenceNodeIds = (rule.EvidenceNodeIds ?? new List<string>()).Concat(proof.EvidenceNodeIds).Distinct(StringComparer.OrdinalIgnoreCase).Take(256).ToList() }; if (AddDerivation(result, d, limits.MaxDerivationsPerLiteral)) changed = true; if (result.Derivations.Count >= limits.MaxSignedLiterals) { result.Truncated = true; break; }
                    }
                    if (result.Truncated) break;
                }
                result.Rounds = round; if (result.Truncated || !changed) break;
            }
            foreach (string key in result.Derivations.Keys) { if (key.StartsWith("!", StringComparison.Ordinal)) result.NegativeAtoms.Add(key.Substring(1)); else if (key.StartsWith("+", StringComparison.Ordinal)) result.PositiveAtoms.Add(key.Substring(1)); } result.DerivedLiterals = Math.Max(0, result.Derivations.Count - result.SeedFacts); result.KernelFingerprint = Fingerprint(result, activeRules); return result;
        }

        public static List<LogicContradiction> Contradictions(LogicEvaluation evaluation, int cap)
        {
            List<LogicContradiction> rows = new List<LogicContradiction>(); if (evaluation == null) return rows; foreach (string atom in evaluation.PositiveAtoms.Intersect(evaluation.NegativeAtoms, StringComparer.OrdinalIgnoreCase)) { LogicDerivation p = evaluation.Best("+" + atom), n = evaluation.Best("!" + atom); LogicContradiction c = new LogicContradiction { Atom = atom, Positive = p, Negative = n, CombinedDepth = (p == null ? 0 : p.Depth) + (n == null ? 0 : n.Depth) }; c.EvidenceNodeIds = evaluation.EvidenceFor(atom).Take(512).ToList(); rows.Add(c); } return rows.OrderBy(c => c.CombinedDepth).ThenBy(c => c.Atom).Take(Math.Max(1, cap)).ToList();
        }

        public static List<LogicAbductionCandidate> Abduce(string query, IEnumerable<LogicRule> rules, LogicEvaluation evaluation, int cap)
        {
            LogicLiteral target = LogicLiteral.Parse(query); List<LogicAbductionCandidate> rows = new List<LogicAbductionCandidate>(); foreach (LogicRule rule in (rules ?? Enumerable.Empty<LogicRule>()).Where(r => Active(r.Status)))
            {
                LogicLiteral consequent = LogicLiteral.Parse(rule.Consequent); Dictionary<string, string> binding = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); if (!Unify(consequent, target, binding)) continue; LogicAbductionCandidate c = new LogicAbductionCandidate { RuleId = rule.Id, RuleName = rule.Name, QueryLiteral = target.Display }; bool valid = true; foreach (string raw in rule.Premises ?? new List<string>()) { LogicLiteral premise = LogicLiteral.Parse(raw).Instantiate(binding); if (!premise.IsGround) { valid = false; break; } if (evaluation.HasSigned(premise.SignedKey)) { c.SupportedPremises++; c.EvidenceNodeIds.AddRange(evaluation.EvidenceFor(premise.Display)); } else { c.Assumptions.Add(premise.Display); if (evaluation.HasSigned(premise.OppositeKey)) c.ContradictedPremises++; } } if (!valid) continue; int total = Math.Max(1, c.SupportedPremises + c.Assumptions.Count); c.StructuralScore = Clamp01((0.65 * c.SupportedPremises / total) + (0.25 / (1 + c.Assumptions.Count)) - (0.25 * c.ContradictedPremises / total) + (0.10 * Clamp01(rule.Priority / 100.0))); rows.Add(c);
            }
            return rows.OrderByDescending(c => c.StructuralScore).ThenBy(c => c.Assumptions.Count).Take(Math.Max(1, cap)).ToList();
        }

        public static string ProofText(LogicEvaluation evaluation, string query, bool opposite)
        {
            if (evaluation == null) return "NO EVALUATION"; LogicLiteral q = LogicLiteral.Parse(query); string key = opposite ? q.OppositeKey : q.SignedKey; StringBuilder s = new StringBuilder(); WriteProof(evaluation, key, s, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); return s.Length == 0 ? "NO PROOF" : s.ToString();
        }

        private static void WriteProof(LogicEvaluation evaluation, string key, StringBuilder s, int depth, HashSet<string> route)
        {
            string pad = new string(' ', Math.Min(40, depth) * 2); if (!route.Add(key)) { s.AppendLine(pad + "↻ " + key + " · proof cycle witness"); return; } LogicDerivation d = evaluation.Best(key); if (d == null) { s.AppendLine(pad + "∅ " + key); return; } s.AppendLine(pad + (depth == 0 ? "⊢ " : "├ ") + d.DisplayLiteral + "   [" + (d.Kind ?? "DERIVATION") + "]   depth=" + d.Depth.ToString(CultureInfo.InvariantCulture) + " weight=" + d.Weight.ToString("0.000", CultureInfo.InvariantCulture) + (String.IsNullOrWhiteSpace(d.RuleName) ? "" : "   via " + d.RuleName)); foreach (string premise in d.PremiseKeys) WriteProof(evaluation, premise, s, depth + 1, new HashSet<string>(route, StringComparer.OrdinalIgnoreCase));
        }

        private static Dictionary<string, List<LogicLiteral>> BuildLiteralIndex(LogicEvaluation result)
        {
            Dictionary<string, List<LogicLiteral>> index = new Dictionary<string, List<LogicLiteral>>(StringComparer.OrdinalIgnoreCase); foreach (string key in result.Derivations.Keys) { LogicLiteral lit = LogicLiteral.Parse((key.StartsWith("!", StringComparison.Ordinal) ? "!" : "") + key.Substring(1)); string bucket = (lit.Negated ? "!" : "+") + lit.Predicate + "/" + lit.Arguments.Count.ToString(CultureInfo.InvariantCulture); List<LogicLiteral> rows; if (!index.TryGetValue(bucket, out rows)) { rows = new List<LogicLiteral>(); index[bucket] = rows; } rows.Add(lit); } return index;
        }

        private static List<BindingProof> MatchAll(List<LogicLiteral> premises, Dictionary<string, List<LogicLiteral>> index, LogicEvaluation evaluation, int cap)
        {
            List<BindingProof> frontier = new List<BindingProof> { new BindingProof() }; foreach (LogicLiteral premise in premises) { List<BindingProof> next = new List<BindingProof>(); string bucket = (premise.Negated ? "!" : "+") + premise.Predicate + "/" + premise.Arguments.Count.ToString(CultureInfo.InvariantCulture); List<LogicLiteral> candidates; if (!index.TryGetValue(bucket, out candidates)) return next; foreach (BindingProof prior in frontier) foreach (LogicLiteral candidate in candidates) { Dictionary<string, string> binding = new Dictionary<string, string>(prior.Binding, StringComparer.OrdinalIgnoreCase); if (!Unify(premise, candidate, binding)) continue; LogicDerivation d = evaluation.Best(candidate.SignedKey); BindingProof b = new BindingProof { Binding = binding, Depth = Math.Max(prior.Depth, d == null ? 0 : d.Depth), Weight = Math.Min(prior.Weight, d == null ? 1 : d.Weight), PremiseKeys = prior.PremiseKeys.Concat(new[] { candidate.SignedKey }).ToList(), EvidenceNodeIds = prior.EvidenceNodeIds.Concat(d == null ? Enumerable.Empty<string>() : d.EvidenceNodeIds).Distinct(StringComparer.OrdinalIgnoreCase).Take(256).ToList() }; next.Add(b); if (next.Count >= cap) break; } frontier = DeduplicateBindings(next, cap); if (frontier.Count == 0) break; } return frontier;
        }

        private static bool Unify(LogicLiteral pattern, LogicLiteral candidate, Dictionary<string, string> binding)
        {
            if (pattern == null || candidate == null || pattern.Negated != candidate.Negated || !String.Equals(pattern.Predicate, candidate.Predicate, StringComparison.OrdinalIgnoreCase) || pattern.Arguments.Count != candidate.Arguments.Count) return false; for (int i = 0; i < pattern.Arguments.Count; i++) { string p = pattern.Arguments[i], c = candidate.Arguments[i]; if (LogicLiteral.IsVariable(p)) { string existing; if (binding.TryGetValue(p, out existing)) { if (!String.Equals(existing, c, StringComparison.OrdinalIgnoreCase)) return false; } else binding[p] = c; } else if (!String.Equals(p, c, StringComparison.OrdinalIgnoreCase)) return false; } return true;
        }

        private static List<BindingProof> DeduplicateBindings(List<BindingProof> rows, int cap)
        {
            Dictionary<string, BindingProof> unique = new Dictionary<string, BindingProof>(StringComparer.OrdinalIgnoreCase); foreach (BindingProof row in rows) { string key = String.Join("|", row.Binding.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value).ToArray()); BindingProof old; if (!unique.TryGetValue(key, out old) || row.Depth < old.Depth || (row.Depth == old.Depth && row.Weight > old.Weight)) unique[key] = row; } return unique.Values.Take(cap).ToList();
        }

        private static bool IsDefeated(LogicRule rule, Dictionary<string, string> binding, LogicEvaluation result) { foreach (string raw in rule.Defeaters ?? new List<string>()) { LogicLiteral d = LogicLiteral.Parse(raw).Instantiate(binding); if (d.IsGround && result.HasSigned(d.SignedKey)) return true; } return false; }
        private static double RuleWeight(LogicRule rule) { return Clamp01(0.5 + Math.Max(-100, Math.Min(100, rule.Priority)) / 250.0); }
        private static bool Active(string status) { return !String.Equals(status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) && !String.Equals(status, "DISABLED", StringComparison.OrdinalIgnoreCase) && !String.Equals(status, "RETRACTED", StringComparison.OrdinalIgnoreCase); }
        private static double Clamp01(double n) { return Math.Max(0, Math.Min(1, n)); }

        private static bool AddDerivation(LogicEvaluation result, LogicDerivation d, int cap)
        {
            List<LogicDerivation> rows; if (!result.Derivations.TryGetValue(d.SignedLiteral, out rows)) { rows = new List<LogicDerivation>(); result.Derivations[d.SignedLiteral] = rows; } string signature = (d.FactId ?? "") + "|" + (d.RuleId ?? "") + "|" + String.Join(",", d.PremiseKeys.ToArray()); if (rows.Any(x => String.Equals((x.FactId ?? "") + "|" + (x.RuleId ?? "") + "|" + String.Join(",", x.PremiseKeys.ToArray()), signature, StringComparison.OrdinalIgnoreCase))) return false; rows.Add(d); rows.Sort(delegate(LogicDerivation a, LogicDerivation b) { int c = a.Depth.CompareTo(b.Depth); return c != 0 ? c : -a.Weight.CompareTo(b.Weight); }); if (rows.Count > cap) rows.RemoveRange(cap, rows.Count - cap); return rows.Contains(d);
        }

        private static void ResolveWorld(List<LogicWorld> worlds, string worldId, int maxDepth, out HashSet<string> visible, out HashSet<string> excluded)
        {
            visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase); excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Dictionary<string, LogicWorld> byId = worlds.Where(w => !String.IsNullOrWhiteSpace(w.Id)).GroupBy(w => w.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase); string cursor = worldId; for (int depth = 0; !String.IsNullOrWhiteSpace(cursor) && depth < maxDepth; depth++) { if (!visible.Add(cursor)) break; LogicWorld w; if (!byId.TryGetValue(cursor, out w)) break; foreach (string id in w.ExcludedFactIds ?? new List<string>()) excluded.Add(id); cursor = w.ParentWorldId; }
        }

        private static string Fingerprint(LogicEvaluation evaluation, List<LogicRule> rules)
        {
            string payload = String.Join("\n", evaluation.Derivations.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()) + "\n--RULES--\n" + String.Join("\n", rules.Select(r => (r.Id ?? "") + "|" + (r.UpdatedUtc ?? r.CreatedUtc ?? "")).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()); using (System.Security.Cryptography.SHA256 h = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLowerInvariant();
        }

        private sealed class BindingProof
        {
            public Dictionary<string, string> Binding = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<string> PremiseKeys = new List<string>();
            public List<string> EvidenceNodeIds = new List<string>();
            public int Depth;
            public double Weight = 1;
        }
    }
}

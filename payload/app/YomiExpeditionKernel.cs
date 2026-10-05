using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class ExpeditionSourceSnapshot
    {
        public string Civilization { get; set; }
        public string SourceType { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string SourceJson { get; set; }
        public string Label { get; set; }
        public string CapturedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionSourceSnapshot() { CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Civilization ?? "SOURCE") + "   ·   " + (Label ?? SourceId ?? "∅") + "   ·   " + ExpeditionKernel.Short(SourceFingerprint); }
    }

    internal sealed class ExpeditionFinding
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string StepId { get; set; }
        public string Text { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public double Confidence { get; set; }
        public List<string> SourceStepIds { get; set; }
        public List<string> Challenges { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionFinding() { Kind = "FINDING"; Status = "ACTIVE"; Confidence = 0.5; SourceStepIds = new List<string>(); Challenges = new List<string>(); CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Kind ?? "FINDING") + "   ·   " + Confidence.ToString("P0", CultureInfo.InvariantCulture) + "   ·   " + (Status ?? "ACTIVE") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class ExpeditionQuestion
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string ParentQuestionId { get; set; }
        public string Text { get; set; }
        public string Status { get; set; }
        public string ResolutionFindingId { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionQuestion() { Status = "OPEN"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return "QUESTION   ·   " + (Status ?? "OPEN") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class ExpeditionAssumption
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string Text { get; set; }
        public string Status { get; set; }
        public List<string> SourceStepIds { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionAssumption() { Status = "ACTIVE"; SourceStepIds = new List<string>(); }
        public override string ToString() { return "ASSUMPTION   ·   " + (Status ?? "ACTIVE") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class ExpeditionContradiction
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string LeftFindingId { get; set; }
        public string RightFindingId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionContradiction() { Status = "OPEN"; }
        public override string ToString() { return "CONTRADICTION   ·   " + (Status ?? "OPEN") + "   ·   " + (Description ?? Id); }
    }

    internal sealed class ExpeditionArtifact
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string StepId { get; set; }
        public string Kind { get; set; }
        public string Label { get; set; }
        public string Civilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Note { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionArtifact() { Kind = "EVIDENCE"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Kind ?? "ARTIFACT") + "   ·   " + (Label ?? SourceId ?? Id) + "   ·   " + (Civilization ?? "LOCAL") + "   ·   " + ExpeditionKernel.Short(SourceFingerprint); }
    }

    internal sealed class ExpeditionStep
    {
        public string Id { get; set; }
        public long Sequence { get; set; }
        public string BranchId { get; set; }
        public string ParentStepId { get; set; }
        public string Civilization { get; set; }
        public string Chamber { get; set; }
        public string QuestionAtEntry { get; set; }
        public string Intent { get; set; }
        public string FindingSummary { get; set; }
        public string NextQuestion { get; set; }
        public string Route { get; set; }
        public string EnteredUtc { get; set; }
        public string LeftUtc { get; set; }
        public string Status { get; set; }
        public ExpeditionSourceSnapshot Source { get; set; }
        public List<string> WindowRoutes { get; set; }
        public List<string> FindingIds { get; set; }
        public List<string> ArtifactIds { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionStep() { Status = "ACTIVE"; EnteredUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); WindowRoutes = new List<string>(); FindingIds = new List<string>(); ArtifactIds = new List<string>(); }
        public override string ToString() { return Sequence.ToString("0000", CultureInfo.InvariantCulture) + "   ·   " + (Civilization ?? "LOCAL") + "   ·   " + (Chamber ?? "ENTRY") + "   ·   " + (Source == null ? "∅" : (Source.Label ?? Source.SourceId)) + "   ·   " + (Status ?? "ACTIVE"); }
    }

    internal sealed class ExpeditionBranch
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentBranchId { get; set; }
        public string ForkStepId { get; set; }
        public string Question { get; set; }
        public string Hypothesis { get; set; }
        public string Status { get; set; }
        public string ActiveStepId { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionBranch() { Status = "ACTIVE"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Name ?? Id) + "   ·   " + (Status ?? "ACTIVE") + "   ·   question: " + (Question ?? "∅") + "   ·   active " + (ActiveStepId ?? "∅"); }
    }

    internal sealed class ExpeditionCheckpoint
    {
        public string Id { get; set; }
        public string MissionId { get; set; }
        public string BranchId { get; set; }
        public string StepId { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public string MissionFingerprint { get; set; }
        public string SourceFingerprint { get; set; }
        public List<string> WindowRoutes { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionCheckpoint() { WindowRoutes = new List<string>(); CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return "CHECKPOINT   ·   " + (Name ?? Id) + "   ·   branch " + (BranchId ?? "∅") + "   ·   step " + (StepId ?? "∅") + "   ·   mission " + ExpeditionKernel.Short(MissionFingerprint); }
    }

    internal sealed class ExpeditionDecision
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string StepId { get; set; }
        public string Decision { get; set; }
        public string Rationale { get; set; }
        public List<string> Alternatives { get; set; }
        public List<string> FindingIds { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionDecision() { Alternatives = new List<string>(); FindingIds = new List<string>(); CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return "DECISION   ·   " + (Decision ?? Id) + "   ·   " + (Rationale ?? ""); }
    }

    internal sealed class ExpeditionSeal
    {
        public string Id { get; set; }
        public string MissionId { get; set; }
        public string MissionFingerprint { get; set; }
        public int Branches { get; set; }
        public int Steps { get; set; }
        public int Findings { get; set; }
        public int OpenQuestions { get; set; }
        public int OpenContradictions { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return "EXPEDITION SEAL   ·   steps " + Steps + "   ·   findings " + Findings + "   ·   open questions " + OpenQuestions + "   ·   fp " + ExpeditionKernel.Short(MissionFingerprint); }
    }

    internal sealed class ExpeditionMission
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Question { get; set; }
        public string Objective { get; set; }
        public string Status { get; set; }
        public string ActiveBranchId { get; set; }
        public string ActiveStepId { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public List<ExpeditionBranch> Branches { get; set; }
        public List<ExpeditionStep> Steps { get; set; }
        public List<ExpeditionFinding> Findings { get; set; }
        public List<ExpeditionQuestion> Questions { get; set; }
        public List<ExpeditionAssumption> Assumptions { get; set; }
        public List<ExpeditionContradiction> Contradictions { get; set; }
        public List<ExpeditionArtifact> Artifacts { get; set; }
        public List<ExpeditionCheckpoint> Checkpoints { get; set; }
        public List<ExpeditionDecision> Decisions { get; set; }
        public List<ExpeditionSeal> Seals { get; set; }
        public List<string> Tags { get; set; }
        public string RootFingerprint { get; set; }
        public ExpeditionMission()
        {
            Status = "ACTIVE"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); UpdatedUtc = CreatedUtc;
            Branches = new List<ExpeditionBranch>(); Steps = new List<ExpeditionStep>(); Findings = new List<ExpeditionFinding>(); Questions = new List<ExpeditionQuestion>(); Assumptions = new List<ExpeditionAssumption>(); Contradictions = new List<ExpeditionContradiction>(); Artifacts = new List<ExpeditionArtifact>(); Checkpoints = new List<ExpeditionCheckpoint>(); Decisions = new List<ExpeditionDecision>(); Seals = new List<ExpeditionSeal>(); Tags = new List<string>();
        }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Title ?? Id) + "   ·   branches " + Branches.Count + "   ·   steps " + Steps.Count + "   ·   findings " + Findings.Count + "   ·   " + (Question ?? "∅"); }
    }

    internal sealed class ExpeditionRouteSuggestion
    {
        public string Civilization { get; set; }
        public int Score { get; set; }
        public string Reason { get; set; }
        public bool Visited { get; set; }
        public override string ToString() { return Score.ToString("000", CultureInfo.InvariantCulture) + "   ·   " + (Civilization ?? "SYSTEM") + (Visited ? "   ·   VISITED" : "   ·   FRONTIER") + "   ·   " + (Reason ?? ""); }
    }

    internal sealed class ExpeditionBranchDiff
    {
        public string LeftBranchId { get; set; }
        public string RightBranchId { get; set; }
        public List<string> OnlyLeftCivilizations { get; set; }
        public List<string> OnlyRightCivilizations { get; set; }
        public List<string> OnlyLeftFindings { get; set; }
        public List<string> OnlyRightFindings { get; set; }
        public string CertificateHash { get; set; }
        public ExpeditionBranchDiff() { OnlyLeftCivilizations = new List<string>(); OnlyRightCivilizations = new List<string>(); OnlyLeftFindings = new List<string>(); OnlyRightFindings = new List<string>(); }
        public override string ToString() { return "BRANCH DIFF   ·   left-only systems " + OnlyLeftCivilizations.Count + "   ·   right-only systems " + OnlyRightCivilizations.Count + "   ·   finding delta " + (OnlyLeftFindings.Count + OnlyRightFindings.Count); }
    }

    internal sealed class ExpeditionValidationFinding
    {
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Message { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "CHECK") + "   ·   " + (SubjectId ?? "∅") + "   ·   " + (Message ?? ""); }
    }

    internal static class ExpeditionKernel
    {
        public static readonly string[] Civilizations = new[] { "WORLD", "CIVILIZATION", "EPISTEMIC_LOGIC", "VERIFICATION", "STRATEGY", "SCIENTIFIC_DISCOVERY", "SYSTEMS_ENGINEERING", "CYBERNETIC_TWIN", "COMPLEX_SYSTEMS", "OPERATIONS_RESEARCH", "ECONOMIC_CIVILIZATION", "SPATIAL_CARTOGRAPHY", "DISTRIBUTED_SYSTEMS", "ADVERSARIAL_SECURITY", "DATA_FOUNDRY", "SOFTWARE_GENOME", "LIVING_DOCUMENT" };

        public static string NewId(string prefix) { return (prefix ?? "EXP") + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(); }
        public static string HashText(string text) { using (SHA256 sha = SHA256.Create()) { byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")); StringBuilder b = new StringBuilder(h.Length * 2); foreach (byte x in h) b.Append(x.ToString("x2", CultureInfo.InvariantCulture)); return b.ToString(); } }
        public static string Short(string hash) { return String.IsNullOrWhiteSpace(hash) ? "∅" : hash.Substring(0, Math.Min(12, hash.Length)); }

        public static ExpeditionMission Starter(string question)
        {
            string q = String.IsNullOrWhiteSpace(question) ? "What is happening, why does it matter, and what evidence would change the conclusion?" : question.Trim();
            ExpeditionMission m = new ExpeditionMission { Id = NewId("MISSION"), Title = TrimTitle(q), Question = q, Objective = "Travel across YOMI civilizations while preserving every source fingerprint, branch, finding, unknown and exact resume checkpoint." };
            ExpeditionBranch b = new ExpeditionBranch { Id = NewId("BRANCH"), Name = "main", Question = q, Hypothesis = "Undetermined until cross-civilization evidence is collected." };
            ExpeditionQuestion eq = new ExpeditionQuestion { Id = NewId("Q"), BranchId = b.Id, Text = q }; eq.CertificateHash = HashText(eq.Id + "|" + eq.BranchId + "|" + eq.Text);
            b.CertificateHash = HashText(b.Id + "|" + b.Name + "|" + b.Question + "|" + b.Hypothesis); m.Branches.Add(b); m.Questions.Add(eq); m.ActiveBranchId = b.Id; m.RootFingerprint = Fingerprint(m); return m;
        }

        public static string FingerprintSource(ExpeditionSourceSnapshot s)
        {
            if (s == null) return HashText("");
            return HashText((s.Civilization ?? "") + "|" + (s.SourceType ?? "") + "|" + (s.SourceId ?? "") + "|" + (s.SourceFingerprint ?? "") + "|" + HashText(s.SourceJson ?? ""));
        }

        public static string FingerprintStep(ExpeditionStep s)
        {
            if (s == null) return HashText("");
            return HashText(s.Id + "|" + s.Sequence + "|" + s.BranchId + "|" + s.ParentStepId + "|" + s.Civilization + "|" + s.Chamber + "|" + s.QuestionAtEntry + "|" + s.Intent + "|" + s.FindingSummary + "|" + s.NextQuestion + "|" + s.Route + "|" + (s.Source == null ? "" : FingerprintSource(s.Source)) + "|" + Join(s.WindowRoutes) + "|" + Join(s.FindingIds) + "|" + Join(s.ArtifactIds));
        }

        public static string Fingerprint(ExpeditionMission m)
        {
            if (m == null) return HashText("");
            List<string> parts = new List<string> { "MISSION|" + m.Id + "|" + m.Title + "|" + m.Question + "|" + m.Objective + "|" + m.Status + "|" + m.ActiveBranchId + "|" + m.ActiveStepId };
            foreach (ExpeditionBranch b in m.Branches.OrderBy(x => x.Id)) parts.Add("B|" + b.Id + "|" + b.ParentBranchId + "|" + b.ForkStepId + "|" + b.Name + "|" + b.Question + "|" + b.Hypothesis + "|" + b.Status + "|" + b.ActiveStepId);
            foreach (ExpeditionStep s in m.Steps.OrderBy(x => x.Sequence)) parts.Add("S|" + FingerprintStep(s));
            foreach (ExpeditionFinding f in m.Findings.OrderBy(x => x.Id)) parts.Add("F|" + f.Id + "|" + f.BranchId + "|" + f.StepId + "|" + f.Text + "|" + f.Kind + "|" + f.Status + "|" + f.Confidence.ToString("R", CultureInfo.InvariantCulture) + "|" + Join(f.SourceStepIds) + "|" + Join(f.Challenges));
            foreach (ExpeditionQuestion q in m.Questions.OrderBy(x => x.Id)) parts.Add("Q|" + q.Id + "|" + q.BranchId + "|" + q.ParentQuestionId + "|" + q.Text + "|" + q.Status + "|" + q.ResolutionFindingId);
            foreach (ExpeditionAssumption a in m.Assumptions.OrderBy(x => x.Id)) parts.Add("A|" + a.Id + "|" + a.BranchId + "|" + a.Text + "|" + a.Status + "|" + Join(a.SourceStepIds));
            foreach (ExpeditionContradiction c in m.Contradictions.OrderBy(x => x.Id)) parts.Add("C|" + c.Id + "|" + c.BranchId + "|" + c.LeftFindingId + "|" + c.RightFindingId + "|" + c.Description + "|" + c.Status + "|" + c.Resolution);
            foreach (ExpeditionArtifact a in m.Artifacts.OrderBy(x => x.Id)) parts.Add("R|" + a.Id + "|" + a.BranchId + "|" + a.StepId + "|" + a.Kind + "|" + a.Label + "|" + a.Civilization + "|" + a.SourceId + "|" + a.SourceFingerprint + "|" + a.Note);
            foreach (ExpeditionCheckpoint c in m.Checkpoints.OrderBy(x => x.Id)) parts.Add("P|" + c.Id + "|" + c.BranchId + "|" + c.StepId + "|" + c.Name + "|" + c.Note + "|" + c.SourceFingerprint + "|" + Join(c.WindowRoutes));
            foreach (ExpeditionDecision d in m.Decisions.OrderBy(x => x.Id)) parts.Add("D|" + d.Id + "|" + d.BranchId + "|" + d.StepId + "|" + d.Decision + "|" + d.Rationale + "|" + Join(d.Alternatives) + "|" + Join(d.FindingIds));
            return HashText(String.Join("\n", parts.ToArray()));
        }

        public static List<ExpeditionRouteSuggestion> SuggestRoutes(ExpeditionMission m)
        {
            string text = ((m == null ? "" : m.Question) + " " + (m == null ? "" : String.Join(" ", m.Questions.Where(x => String.Equals(x.Status, "OPEN", StringComparison.OrdinalIgnoreCase)).Select(x => x.Text).ToArray()))).ToLowerInvariant();
            HashSet<string> visited = new HashSet<string>(m == null ? new string[0] : m.Steps.Select(x => x.Civilization), StringComparer.OrdinalIgnoreCase);
            Dictionary<string,string[]> words = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase) {
                {"WORLD",new[]{"entity","object","identity","relation","world"}}, {"CIVILIZATION",new[]{"institution","governance","resolution","organization","people"}},
                {"EPISTEMIC_LOGIC",new[]{"claim","logic","assumption","contradiction","truth","uncertain"}}, {"VERIFICATION",new[]{"verify","proof","state","invariant","counterexample","property"}},
                {"STRATEGY",new[]{"strategy","counterfactual","option","decision","scenario","adversary"}}, {"SCIENTIFIC_DISCOVERY",new[]{"experiment","hypothesis","science","measure","causal","research"}},
                {"SYSTEMS_ENGINEERING",new[]{"requirement","architecture","reliability","risk","design","schedule"}}, {"CYBERNETIC_TWIN",new[]{"control","telemetry","sensor","dynamic","feedback","stability"}},
                {"COMPLEX_SYSTEMS",new[]{"emergence","population","network","cascade","resilience","critical"}}, {"OPERATIONS_RESEARCH",new[]{"optimize","route","schedule","allocate","inventory","staff"}},
                {"ECONOMIC_CIVILIZATION",new[]{"economy","price","firm","market","money","trade","labor"}}, {"SPATIAL_CARTOGRAPHY",new[]{"where","location","route","distance","place","region","map"}},
                {"DISTRIBUTED_SYSTEMS",new[]{"distributed","consensus","replica","partition","latency","network","service"}}, {"ADVERSARIAL_SECURITY",new[]{"security","threat","access","attack","risk","identity","control"}},
                {"DATA_FOUNDRY",new[]{"data","dataset","field","schema","quality","correlation","transform"}}, {"SOFTWARE_GENOME",new[]{"code","software","function","dependency","file","change","bug"}},
                {"LIVING_DOCUMENT",new[]{"report","document","manual","specification","write","publication","evidence"}}
            };
            List<ExpeditionRouteSuggestion> rows = new List<ExpeditionRouteSuggestion>();
            foreach (string civ in Civilizations) { int score = visited.Contains(civ) ? 5 : 20; List<string> hits = new List<string>(); foreach (string w in words[civ]) if (text.Contains(w)) { score += 15; hits.Add(w); } rows.Add(new ExpeditionRouteSuggestion { Civilization = civ, Score = score, Visited = visited.Contains(civ), Reason = hits.Count == 0 ? (visited.Contains(civ) ? "Previously visited; revisit only if the evidence changed." : "Unvisited evidence frontier.") : "Question matches: " + String.Join(", ", hits.ToArray()) }); }
            return rows.OrderByDescending(x => x.Score).ThenBy(x => x.Civilization).ToList();
        }

        public static ExpeditionBranchDiff DiffBranches(ExpeditionMission m, string leftId, string rightId)
        {
            ExpeditionBranchDiff d = new ExpeditionBranchDiff { LeftBranchId = leftId, RightBranchId = rightId }; if (m == null) return d;
            HashSet<string> lc = new HashSet<string>(m.Steps.Where(x => x.BranchId == leftId).Select(x => x.Civilization), StringComparer.OrdinalIgnoreCase); HashSet<string> rc = new HashSet<string>(m.Steps.Where(x => x.BranchId == rightId).Select(x => x.Civilization), StringComparer.OrdinalIgnoreCase);
            d.OnlyLeftCivilizations.AddRange(lc.Except(rc, StringComparer.OrdinalIgnoreCase).OrderBy(x => x)); d.OnlyRightCivilizations.AddRange(rc.Except(lc, StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
            HashSet<string> lf = new HashSet<string>(m.Findings.Where(x => x.BranchId == leftId).Select(x => x.Text ?? x.Id), StringComparer.Ordinal); HashSet<string> rf = new HashSet<string>(m.Findings.Where(x => x.BranchId == rightId).Select(x => x.Text ?? x.Id), StringComparer.Ordinal);
            d.OnlyLeftFindings.AddRange(lf.Except(rf).OrderBy(x => x)); d.OnlyRightFindings.AddRange(rf.Except(lf).OrderBy(x => x)); d.CertificateHash = HashText(leftId + "|" + rightId + "|" + Join(d.OnlyLeftCivilizations) + "|" + Join(d.OnlyRightCivilizations) + "|" + Join(d.OnlyLeftFindings) + "|" + Join(d.OnlyRightFindings)); return d;
        }

        public static List<ExpeditionValidationFinding> Validate(ExpeditionMission m)
        {
            List<ExpeditionValidationFinding> r = new List<ExpeditionValidationFinding>(); if (m == null) { r.Add(V("ERROR","MISSION_NULL","","Mission is null.")); return r; }
            HashSet<string> branchIds = Set(m.Branches.Select(x => x.Id)); HashSet<string> stepIds = Set(m.Steps.Select(x => x.Id)); HashSet<string> findingIds = Set(m.Findings.Select(x => x.Id));
            Duplicates(m.Branches.Select(x => x.Id), "branch", r); Duplicates(m.Steps.Select(x => x.Id), "step", r); Duplicates(m.Findings.Select(x => x.Id), "finding", r); Duplicates(m.Checkpoints.Select(x => x.Id), "checkpoint", r);
            if (!branchIds.Contains(m.ActiveBranchId ?? "")) r.Add(V("ERROR","ACTIVE_BRANCH",m.Id,"Active branch does not exist."));
            if (!String.IsNullOrWhiteSpace(m.ActiveStepId) && !stepIds.Contains(m.ActiveStepId)) r.Add(V("ERROR","ACTIVE_STEP",m.Id,"Active step does not exist."));
            foreach (ExpeditionBranch b in m.Branches) { if (!String.IsNullOrWhiteSpace(b.ParentBranchId) && !branchIds.Contains(b.ParentBranchId)) r.Add(V("ERROR","BRANCH_PARENT",b.Id,"Parent branch is missing.")); if (!String.IsNullOrWhiteSpace(b.ForkStepId) && !stepIds.Contains(b.ForkStepId)) r.Add(V("ERROR","BRANCH_FORK",b.Id,"Fork step is missing.")); }
            foreach (ExpeditionStep s in m.Steps) { if (!branchIds.Contains(s.BranchId ?? "")) r.Add(V("ERROR","STEP_BRANCH",s.Id,"Step references missing branch.")); if (!String.IsNullOrWhiteSpace(s.ParentStepId) && !stepIds.Contains(s.ParentStepId)) r.Add(V("ERROR","STEP_PARENT",s.Id,"Step parent is missing.")); if (s.Source != null && !String.Equals(s.Source.SourceFingerprint, HashText(s.Source.SourceJson ?? ""), StringComparison.OrdinalIgnoreCase)) r.Add(V("WARN","SOURCE_FINGERPRINT",s.Id,"Captured source JSON no longer matches its stored fingerprint.")); }
            foreach (ExpeditionFinding f in m.Findings) { if (!branchIds.Contains(f.BranchId ?? "")) r.Add(V("ERROR","FINDING_BRANCH",f.Id,"Finding references missing branch.")); foreach (string id in f.SourceStepIds) if (!stepIds.Contains(id)) r.Add(V("ERROR","FINDING_SOURCE",f.Id,"Finding references missing step " + id + ".")); }
            foreach (ExpeditionCheckpoint c in m.Checkpoints) { if (!branchIds.Contains(c.BranchId ?? "")) r.Add(V("ERROR","CHECKPOINT_BRANCH",c.Id,"Checkpoint references missing branch.")); if (!String.IsNullOrWhiteSpace(c.StepId) && !stepIds.Contains(c.StepId)) r.Add(V("ERROR","CHECKPOINT_STEP",c.Id,"Checkpoint references missing step.")); }
            foreach (ExpeditionContradiction c in m.Contradictions) { if (!String.IsNullOrWhiteSpace(c.LeftFindingId) && !findingIds.Contains(c.LeftFindingId)) r.Add(V("ERROR","CONTRADICTION_LEFT",c.Id,"Left finding missing.")); if (!String.IsNullOrWhiteSpace(c.RightFindingId) && !findingIds.Contains(c.RightFindingId)) r.Add(V("ERROR","CONTRADICTION_RIGHT",c.Id,"Right finding missing.")); }
            if (m.Questions.Count(x => String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase)) == 0) r.Add(V("INFO","QUESTION_FRONTIER",m.Id,"No open question remains; mission may be ready for synthesis or closure."));
            return r;
        }

        public static ExpeditionSeal Seal(ExpeditionMission m)
        {
            string fp = Fingerprint(m); ExpeditionSeal s = new ExpeditionSeal { Id = NewId("EXPSEAL"), MissionId = m.Id, MissionFingerprint = fp, Branches = m.Branches.Count, Steps = m.Steps.Count, Findings = m.Findings.Count, OpenQuestions = m.Questions.Count(x => String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase)), OpenContradictions = m.Contradictions.Count(x => String.Equals(x.Status,"OPEN",StringComparison.OrdinalIgnoreCase)), CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }; s.CertificateHash = HashText(s.Id + "|" + s.MissionId + "|" + s.MissionFingerprint + "|" + s.Branches + "|" + s.Steps + "|" + s.Findings + "|" + s.OpenQuestions + "|" + s.OpenContradictions); return s;
        }

        private static string TrimTitle(string q) { if (String.IsNullOrWhiteSpace(q)) return "Untitled Expedition"; string x = q.Trim().Replace("\r"," ").Replace("\n"," "); return x.Length <= 90 ? x : x.Substring(0, 87) + "..."; }
        private static HashSet<string> Set(IEnumerable<string> xs) { return new HashSet<string>((xs ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.Ordinal); }
        private static void Duplicates(IEnumerable<string> xs, string kind, List<ExpeditionValidationFinding> r) { foreach (IGrouping<string,string> g in (xs ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1)) r.Add(V("ERROR","DUPLICATE_ID",g.Key,kind + " id appears " + g.Count() + " times.")); }
        private static ExpeditionValidationFinding V(string sev, string kind, string id, string msg) { return new ExpeditionValidationFinding { Severity = sev, Kind = kind, SubjectId = id, Message = msg }; }
        private static string Join(IEnumerable<string> xs) { return String.Join(",", (xs ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray()); }
    }
}

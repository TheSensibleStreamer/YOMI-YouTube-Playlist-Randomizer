using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class LivingEvidenceAnchor
    {
        public string Id { get; set; }
        public string SourceCivilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Locator { get; set; }
        public string Label { get; set; }
        public string Excerpt { get; set; }
        public string Authority { get; set; }
        public double Confidence { get; set; }
        public string CapturedUtc { get; set; }
        public List<string> Tags { get; set; }
        public string CertificateHash { get; set; }
        public LivingEvidenceAnchor() { Tags = new List<string>(); CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Confidence = 1.0; }
        public override string ToString() { return "EVIDENCE   ·   " + (Label ?? SourceId ?? Id) + "   ·   " + (SourceCivilization ?? "LOCAL") + "   ·   fp " + LivingDocumentKernel.Short(SourceFingerprint) + "   ·   " + Confidence.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class LivingCalculation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Expression { get; set; }
        public List<string> Inputs { get; set; }
        public string Output { get; set; }
        public string Units { get; set; }
        public string Explanation { get; set; }
        public List<string> EvidenceIds { get; set; }
        public string CertificateHash { get; set; }
        public LivingCalculation() { Inputs = new List<string>(); EvidenceIds = new List<string>(); }
        public override string ToString() { return "CALC   ·   " + (Name ?? Id) + "   ·   " + (Expression ?? "") + " = " + (Output ?? "∅") + (String.IsNullOrWhiteSpace(Units) ? "" : " " + Units); }
    }

    internal sealed class LivingModelBinding
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ModelKind { get; set; }
        public string SourceCivilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> Outputs { get; set; }
        public List<string> Limitations { get; set; }
        public string CertificateHash { get; set; }
        public LivingModelBinding() { Assumptions = new List<string>(); Outputs = new List<string>(); Limitations = new List<string>(); }
        public override string ToString() { return "MODEL   ·   " + (Name ?? Id) + "   ·   " + (ModelKind ?? "MODEL") + "   ·   " + (SourceCivilization ?? "LOCAL") + "   ·   fp " + LivingDocumentKernel.Short(SourceFingerprint); }
    }

    internal sealed class LivingAssumption
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Status { get; set; }
        public string Rationale { get; set; }
        public List<string> EvidenceIds { get; set; }
        public string CertificateHash { get; set; }
        public LivingAssumption() { Status = "OPEN"; EvidenceIds = new List<string>(); }
        public override string ToString() { return "ASSUMPTION   ·   " + (Status ?? "OPEN") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class LivingObjection
    {
        public string Id { get; set; }
        public string TargetClaimId { get; set; }
        public string Position { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
        public List<string> EvidenceIds { get; set; }
        public string CertificateHash { get; set; }
        public LivingObjection() { Severity = "MATERIAL"; Status = "OPEN"; EvidenceIds = new List<string>(); }
        public override string ToString() { return "OBJECTION   ·   " + (Severity ?? "MATERIAL") + "   ·   " + (Status ?? "OPEN") + "   ·   claim " + (TargetClaimId ?? "∅") + "   ·   " + (Position ?? Id); }
    }

    internal sealed class LivingClaim
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public double Confidence { get; set; }
        public List<string> EvidenceIds { get; set; }
        public List<string> CalculationIds { get; set; }
        public List<string> ModelIds { get; set; }
        public List<string> AssumptionIds { get; set; }
        public List<string> ObjectionIds { get; set; }
        public List<string> ParentClaimIds { get; set; }
        public string CertificateHash { get; set; }
        public LivingClaim()
        {
            Kind = "ASSERTION"; Status = "ACTIVE"; Confidence = 0.5;
            EvidenceIds = new List<string>(); CalculationIds = new List<string>(); ModelIds = new List<string>();
            AssumptionIds = new List<string>(); ObjectionIds = new List<string>(); ParentClaimIds = new List<string>();
        }
        public override string ToString() { return "CLAIM   ·   " + (Kind ?? "ASSERTION") + "   ·   " + Confidence.ToString("P0", CultureInfo.InvariantCulture) + "   ·   " + (Status ?? "ACTIVE") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class LivingParagraph
    {
        public string Id { get; set; }
        public string Heading { get; set; }
        public string Text { get; set; }
        public string Role { get; set; }
        public int Revision { get; set; }
        public string PreviousParagraphId { get; set; }
        public List<string> ClaimIds { get; set; }
        public List<string> EvidenceIds { get; set; }
        public List<string> CalculationIds { get; set; }
        public List<string> ModelIds { get; set; }
        public List<string> ObjectionIds { get; set; }
        public List<string> DependsOnParagraphIds { get; set; }
        public bool Stale { get; set; }
        public List<string> StaleReasons { get; set; }
        public string CertificateHash { get; set; }
        public LivingParagraph()
        {
            Role = "BODY"; Revision = 1; ClaimIds = new List<string>(); EvidenceIds = new List<string>();
            CalculationIds = new List<string>(); ModelIds = new List<string>(); ObjectionIds = new List<string>();
            DependsOnParagraphIds = new List<string>(); StaleReasons = new List<string>();
        }
        public override string ToString() { return (Stale ? "STALE   ·   " : "PARA   ·   ") + (Role ?? "BODY") + "   ·   r" + Revision + "   ·   " + (Heading ?? Id) + "   ·   claims " + ClaimIds.Count + " / evidence " + EvidenceIds.Count; }
    }

    internal sealed class LivingSection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public List<string> ParagraphIds { get; set; }
        public LivingSection() { ParagraphIds = new List<string>(); }
        public override string ToString() { return Order.ToString("000", CultureInfo.InvariantCulture) + "   ·   " + (Title ?? Id) + "   ·   " + ParagraphIds.Count + " paragraphs"; }
    }

    internal sealed class LivingDecisionRecord
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Decision { get; set; }
        public List<string> Alternatives { get; set; }
        public List<string> ClaimIds { get; set; }
        public List<string> EvidenceIds { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public LivingDecisionRecord() { Alternatives = new List<string>(); ClaimIds = new List<string>(); EvidenceIds = new List<string>(); CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return "DECISION   ·   " + (Title ?? Id) + "   ·   " + (Decision ?? "UNDECIDED") + "   ·   alternatives " + Alternatives.Count; }
    }

    internal sealed class LivingRequirement
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Level { get; set; }
        public string Status { get; set; }
        public List<string> ClaimIds { get; set; }
        public List<string> EvidenceIds { get; set; }
        public List<string> VerificationLinks { get; set; }
        public string CertificateHash { get; set; }
        public LivingRequirement() { Level = "SHALL"; Status = "OPEN"; ClaimIds = new List<string>(); EvidenceIds = new List<string>(); VerificationLinks = new List<string>(); }
        public override string ToString() { return "REQ   ·   " + (Level ?? "SHALL") + "   ·   " + (Status ?? "OPEN") + "   ·   " + (Text ?? Id); }
    }

    internal sealed class LivingDefinition
    {
        public string Id { get; set; }
        public string Term { get; set; }
        public string Meaning { get; set; }
        public List<string> EvidenceIds { get; set; }
        public string CertificateHash { get; set; }
        public LivingDefinition() { EvidenceIds = new List<string>(); }
        public override string ToString() { return (Term ?? Id) + "   =   " + (Meaning ?? ""); }
    }

    internal sealed class LivingVersion
    {
        public string Id { get; set; }
        public string DocumentId { get; set; }
        public int Number { get; set; }
        public string ParentVersionId { get; set; }
        public string Branch { get; set; }
        public string CreatedUtc { get; set; }
        public string ChangeNote { get; set; }
        public bool Frozen { get; set; }
        public List<LivingSection> Sections { get; set; }
        public List<LivingParagraph> Paragraphs { get; set; }
        public List<LivingClaim> Claims { get; set; }
        public List<LivingEvidenceAnchor> Evidence { get; set; }
        public List<LivingCalculation> Calculations { get; set; }
        public List<LivingModelBinding> Models { get; set; }
        public List<LivingAssumption> Assumptions { get; set; }
        public List<LivingObjection> Objections { get; set; }
        public List<LivingDecisionRecord> Decisions { get; set; }
        public List<LivingRequirement> Requirements { get; set; }
        public List<LivingDefinition> Definitions { get; set; }
        public string RootFingerprint { get; set; }
        public LivingVersion()
        {
            Branch = "main"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Sections = new List<LivingSection>(); Paragraphs = new List<LivingParagraph>(); Claims = new List<LivingClaim>();
            Evidence = new List<LivingEvidenceAnchor>(); Calculations = new List<LivingCalculation>(); Models = new List<LivingModelBinding>();
            Assumptions = new List<LivingAssumption>(); Objections = new List<LivingObjection>(); Decisions = new List<LivingDecisionRecord>();
            Requirements = new List<LivingRequirement>(); Definitions = new List<LivingDefinition>();
        }
        public override string ToString() { return "v" + Number + "   ·   " + (Branch ?? "main") + "   ·   " + (CreatedUtc ?? "") + "   ·   " + Paragraphs.Count + " paragraphs   ·   fp " + LivingDocumentKernel.Short(RootFingerprint) + (Frozen ? "   ·   FROZEN" : ""); }
    }

    internal sealed class LivingDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Kind { get; set; }
        public string Objective { get; set; }
        public string Audience { get; set; }
        public string CreatedUtc { get; set; }
        public string CurrentVersionId { get; set; }
        public string ActiveBranch { get; set; }
        public List<LivingVersion> Versions { get; set; }
        public LivingDocument() { Kind = "REPORT"; ActiveBranch = "main"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Versions = new List<LivingVersion>(); }
        public override string ToString() { return (Kind ?? "DOCUMENT") + "   ·   " + (Title ?? Id) + "   ·   versions " + Versions.Count + "   ·   branch " + (ActiveBranch ?? "main"); }
    }

    internal sealed class LivingProjectionReceipt
    {
        public string Id { get; set; }
        public string Civilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string Rule { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public LivingProjectionReceipt() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Civilization ?? "SOURCE") + "   ·   " + (SourceId ?? "∅") + "   ·   fp " + LivingDocumentKernel.Short(SourceFingerprint) + "   ·   " + (Rule ?? "projection"); }
    }

    internal sealed class LivingValidationFinding
    {
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string SubjectId { get; set; }
        public string Message { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "CHECK") + "   ·   " + (SubjectId ?? "") + "   ·   " + (Message ?? ""); }
    }

    internal sealed class LivingImpactReport
    {
        public string Id { get; set; }
        public string VersionId { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string CreatedUtc { get; set; }
        public List<string> Claims { get; set; }
        public List<string> Paragraphs { get; set; }
        public List<string> Reasons { get; set; }
        public string CertificateHash { get; set; }
        public LivingImpactReport() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Claims = new List<string>(); Paragraphs = new List<string>(); Reasons = new List<string>(); }
        public override string ToString() { return "IMPACT   ·   " + (SourceKind ?? "SOURCE") + ":" + (SourceId ?? "∅") + "   ·   claims " + Claims.Count + "   ·   paragraphs " + Paragraphs.Count; }
    }

    internal sealed class LivingVersionDiff
    {
        public string LeftId { get; set; }
        public string RightId { get; set; }
        public List<string> AddedParagraphs { get; set; }
        public List<string> RemovedParagraphs { get; set; }
        public List<string> ChangedParagraphs { get; set; }
        public List<string> AddedClaims { get; set; }
        public List<string> RemovedClaims { get; set; }
        public List<string> ChangedClaims { get; set; }
        public LivingVersionDiff() { AddedParagraphs = new List<string>(); RemovedParagraphs = new List<string>(); ChangedParagraphs = new List<string>(); AddedClaims = new List<string>(); RemovedClaims = new List<string>(); ChangedClaims = new List<string>(); }
        public override string ToString() { return "DIFF   ·   " + (LeftId ?? "∅") + " → " + (RightId ?? "∅") + "   ·   paragraphs +" + AddedParagraphs.Count + "/-" + RemovedParagraphs.Count + "/~" + ChangedParagraphs.Count + "   ·   claims +" + AddedClaims.Count + "/-" + RemovedClaims.Count + "/~" + ChangedClaims.Count; }
    }

    internal sealed class LivingBaseline
    {
        public string Id { get; set; }
        public string DocumentId { get; set; }
        public string VersionId { get; set; }
        public string VersionFingerprint { get; set; }
        public string Name { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return "BASELINE   ·   " + (Name ?? Id) + "   ·   " + (VersionId ?? "∅") + "   ·   fp " + LivingDocumentKernel.Short(VersionFingerprint); }
    }

    internal sealed class LivingPublicationSeal
    {
        public string Id { get; set; }
        public string DocumentId { get; set; }
        public string VersionId { get; set; }
        public string VersionFingerprint { get; set; }
        public string RenderedFingerprint { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return "PUBLICATION SEAL   ·   " + (VersionId ?? "∅") + "   ·   errors " + ErrorCount + "   ·   warnings " + WarningCount + "   ·   rendered " + LivingDocumentKernel.Short(RenderedFingerprint); }
    }

    internal static class LivingDocumentKernel
    {
        public static string NewId(string prefix) { return (prefix ?? "DOC") + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(); }
        public static string HashText(string text) { using (SHA256 sha = SHA256.Create()) { byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")); StringBuilder b = new StringBuilder(h.Length * 2); foreach (byte x in h) b.Append(x.ToString("x2", CultureInfo.InvariantCulture)); return b.ToString(); } }
        public static string Short(string hash) { return String.IsNullOrWhiteSpace(hash) ? "∅" : hash.Substring(0, Math.Min(12, hash.Length)); }

        public static LivingDocument Starter(string title, string kind)
        {
            LivingDocument d = new LivingDocument { Id = NewId("DOC"), Title = String.IsNullOrWhiteSpace(title) ? "Atlas Intelligence Dossier" : title.Trim(), Kind = String.IsNullOrWhiteSpace(kind) ? "RESEARCH_DOSSIER" : kind.Trim().ToUpperInvariant(), Objective = "Keep every important paragraph connected to inspectable evidence, calculations, models, objections and earlier versions.", Audience = "operator / reviewer / future investigator" };
            LivingVersion v = new LivingVersion { Id = NewId("VER"), DocumentId = d.Id, Number = 1, Branch = "main", ChangeNote = "Genesis version" };
            LivingEvidenceAnchor e1 = new LivingEvidenceAnchor { Id = NewId("EVID"), SourceCivilization = "DOCΩ", SourceId = "GENESIS", SourceFingerprint = HashText("GENESIS|" + d.Id), Locator = "internal://genesis", Label = "Factory genesis contract", Excerpt = "A living document must preserve the chain from prose to its supporting and challenging intelligence.", Authority = "SYSTEM_CONTRACT", Confidence = 1.0 };
            e1.CertificateHash = HashText(e1.Id + "|" + e1.SourceFingerprint + "|" + e1.Excerpt);
            LivingCalculation calc = new LivingCalculation { Id = NewId("CALC"), Name = "Trace coverage", Expression = "supported_claims / total_assertive_claims", Output = "1.00", Units = "ratio", Explanation = "Starter demonstration calculation; future versions recompute coverage from retained graph state." }; calc.EvidenceIds.Add(e1.Id); calc.CertificateHash = HashText(calc.Id + "|" + calc.Expression + "|" + calc.Output + "|" + e1.Id);
            LivingModelBinding model = new LivingModelBinding { Id = NewId("MODEL"), Name = "Paragraph provenance graph", ModelKind = "DIRECTED_EVIDENCE_GRAPH", SourceCivilization = "DOCΩ", SourceId = d.Id, SourceFingerprint = e1.SourceFingerprint }; model.Assumptions.Add("Explicit links are more trustworthy than inferred prose similarity."); model.Outputs.Add("claim/evidence dependency graph"); model.Limitations.Add("The factory cannot infer truth merely from the existence of a citation."); model.CertificateHash = HashText(model.Id + "|" + model.SourceFingerprint + "|" + String.Join("|", model.Assumptions.ToArray()));
            LivingAssumption assumption = new LivingAssumption { Id = NewId("ASM"), Text = "A reviewer should be able to inspect why a paragraph exists without reverse-engineering the author from scratch.", Status = "ACTIVE", Rationale = "Core design assumption" }; assumption.EvidenceIds.Add(e1.Id); assumption.CertificateHash = HashText(assumption.Id + "|" + assumption.Text + "|" + e1.Id);
            LivingClaim c1 = new LivingClaim { Id = NewId("CLAIM"), Text = "Paragraph-level intelligence becomes auditable when claims and dependencies remain explicit across revisions.", Kind = "THESIS", Confidence = 0.95 }; c1.EvidenceIds.Add(e1.Id); c1.CalculationIds.Add(calc.Id); c1.ModelIds.Add(model.Id); c1.AssumptionIds.Add(assumption.Id); c1.CertificateHash = FingerprintClaim(c1);
            LivingObjection objection = new LivingObjection { Id = NewId("OBJ"), TargetClaimId = c1.Id, Position = "Explicit linkage can create false confidence if low-quality evidence is merely well organized.", Severity = "MATERIAL", Status = "OPEN" }; objection.EvidenceIds.Add(e1.Id); objection.CertificateHash = HashText(objection.Id + "|" + objection.TargetClaimId + "|" + objection.Position); c1.ObjectionIds.Add(objection.Id); c1.CertificateHash = FingerprintClaim(c1);
            LivingParagraph p1 = new LivingParagraph { Id = NewId("PARA"), Heading = "Mission", Role = "EXECUTIVE", Text = "This dossier is alive: its prose is only the visible surface. Underneath each important paragraph is a traversable graph of claims, evidence, calculations, models, assumptions, objections and prior versions." }; p1.ClaimIds.Add(c1.Id); p1.EvidenceIds.Add(e1.Id); p1.CalculationIds.Add(calc.Id); p1.ModelIds.Add(model.Id); p1.ObjectionIds.Add(objection.Id); p1.CertificateHash = FingerprintParagraph(p1);
            LivingSection s1 = new LivingSection { Id = NewId("SEC"), Title = "Executive intelligence", Order = 1 }; s1.ParagraphIds.Add(p1.Id);
            LivingRequirement req = new LivingRequirement { Id = NewId("REQ"), Text = "Every assertive paragraph SHALL expose its supporting claim graph and source lineage.", Level = "SHALL", Status = "ACTIVE" }; req.ClaimIds.Add(c1.Id); req.EvidenceIds.Add(e1.Id); req.VerificationLinks.Add("TRACE_COVERAGE>=1.0"); req.CertificateHash = HashText(req.Id + "|" + req.Text + "|" + c1.Id);
            LivingDefinition def = new LivingDefinition { Id = NewId("DEF"), Term = "Living paragraph", Meaning = "A paragraph whose current text, claims, evidence dependencies, objections and revision lineage remain inspectably connected." }; def.EvidenceIds.Add(e1.Id); def.CertificateHash = HashText(def.Id + "|" + def.Term + "|" + def.Meaning);
            v.Sections.Add(s1); v.Paragraphs.Add(p1); v.Claims.Add(c1); v.Evidence.Add(e1); v.Calculations.Add(calc); v.Models.Add(model); v.Assumptions.Add(assumption); v.Objections.Add(objection); v.Requirements.Add(req); v.Definitions.Add(def); v.RootFingerprint = Fingerprint(v);
            d.Versions.Add(v); d.CurrentVersionId = v.Id; return d;
        }

        public static LivingVersion Current(LivingDocument d)
        {
            if (d == null || d.Versions == null || d.Versions.Count == 0) return null;
            LivingVersion v = d.Versions.FirstOrDefault(x => String.Equals(x.Id, d.CurrentVersionId, StringComparison.Ordinal));
            return v ?? d.Versions.OrderByDescending(x => x.Number).FirstOrDefault();
        }

        public static string FingerprintClaim(LivingClaim c)
        {
            if (c == null) return HashText("");
            return HashText((c.Text ?? "") + "|" + (c.Kind ?? "") + "|" + c.Confidence.ToString("R", CultureInfo.InvariantCulture) + "|" + Join(c.EvidenceIds) + "|" + Join(c.CalculationIds) + "|" + Join(c.ModelIds) + "|" + Join(c.AssumptionIds) + "|" + Join(c.ObjectionIds) + "|" + Join(c.ParentClaimIds));
        }
        public static string FingerprintParagraph(LivingParagraph p)
        {
            if (p == null) return HashText("");
            return HashText((p.Heading ?? "") + "|" + (p.Text ?? "") + "|" + (p.Role ?? "") + "|" + p.Revision + "|" + (p.PreviousParagraphId ?? "") + "|" + Join(p.ClaimIds) + "|" + Join(p.EvidenceIds) + "|" + Join(p.CalculationIds) + "|" + Join(p.ModelIds) + "|" + Join(p.ObjectionIds) + "|" + Join(p.DependsOnParagraphIds));
        }
        public static string Fingerprint(LivingVersion v)
        {
            if (v == null) return HashText("");
            List<string> parts = new List<string>(); parts.Add(v.DocumentId ?? ""); parts.Add(v.Number.ToString(CultureInfo.InvariantCulture)); parts.Add(v.ParentVersionId ?? ""); parts.Add(v.Branch ?? "");
            foreach (LivingSection s in v.Sections.OrderBy(x => x.Order).ThenBy(x => x.Id)) parts.Add("S|" + s.Id + "|" + s.Title + "|" + s.Order + "|" + Join(s.ParagraphIds));
            foreach (LivingParagraph p in v.Paragraphs.OrderBy(x => x.Id)) { p.CertificateHash = FingerprintParagraph(p); parts.Add("P|" + p.Id + "|" + p.CertificateHash); }
            foreach (LivingClaim c in v.Claims.OrderBy(x => x.Id)) { c.CertificateHash = FingerprintClaim(c); parts.Add("C|" + c.Id + "|" + c.CertificateHash); }
            foreach (LivingEvidenceAnchor e in v.Evidence.OrderBy(x => x.Id)) parts.Add("E|" + e.Id + "|" + e.SourceFingerprint + "|" + e.CertificateHash);
            foreach (LivingCalculation c in v.Calculations.OrderBy(x => x.Id)) parts.Add("K|" + c.Id + "|" + c.CertificateHash);
            foreach (LivingModelBinding m in v.Models.OrderBy(x => x.Id)) parts.Add("M|" + m.Id + "|" + m.CertificateHash);
            foreach (LivingAssumption a in v.Assumptions.OrderBy(x => x.Id)) parts.Add("A|" + a.Id + "|" + a.CertificateHash);
            foreach (LivingObjection o in v.Objections.OrderBy(x => x.Id)) parts.Add("O|" + o.Id + "|" + o.CertificateHash);
            foreach (LivingDecisionRecord d in v.Decisions.OrderBy(x => x.Id)) parts.Add("D|" + d.Id + "|" + d.CertificateHash);
            foreach (LivingRequirement r in v.Requirements.OrderBy(x => x.Id)) parts.Add("R|" + r.Id + "|" + r.CertificateHash);
            foreach (LivingDefinition d in v.Definitions.OrderBy(x => x.Id)) parts.Add("F|" + d.Id + "|" + d.CertificateHash);
            return HashText(String.Join("\n", parts.ToArray()));
        }

        public static List<LivingValidationFinding> Validate(LivingVersion v)
        {
            List<LivingValidationFinding> r = new List<LivingValidationFinding>(); if (v == null) { r.Add(F("ERROR", "VERSION", "", "No document version selected.")); return r; }
            HashSet<string> pids = Set(v.Paragraphs.Select(x => x.Id)); HashSet<string> cids = Set(v.Claims.Select(x => x.Id)); HashSet<string> eids = Set(v.Evidence.Select(x => x.Id)); HashSet<string> kids = Set(v.Calculations.Select(x => x.Id)); HashSet<string> mids = Set(v.Models.Select(x => x.Id)); HashSet<string> aids = Set(v.Assumptions.Select(x => x.Id)); HashSet<string> oids = Set(v.Objections.Select(x => x.Id));
            Duplicates(v.Paragraphs.Select(x => x.Id), "PARAGRAPH", r); Duplicates(v.Claims.Select(x => x.Id), "CLAIM", r); Duplicates(v.Evidence.Select(x => x.Id), "EVIDENCE", r);
            foreach (LivingSection s in v.Sections) foreach (string id in s.ParagraphIds) if (!pids.Contains(id)) r.Add(F("ERROR", "BROKEN_SECTION_LINK", s.Id, "Section references missing paragraph " + id + "."));
            foreach (LivingParagraph p in v.Paragraphs)
            {
                if (String.IsNullOrWhiteSpace(p.Text)) r.Add(F("WARN", "EMPTY_PARAGRAPH", p.Id, "Paragraph has no prose."));
                CheckRefs(p.Id, "paragraph claim", p.ClaimIds, cids, r); CheckRefs(p.Id, "paragraph evidence", p.EvidenceIds, eids, r); CheckRefs(p.Id, "paragraph calculation", p.CalculationIds, kids, r); CheckRefs(p.Id, "paragraph model", p.ModelIds, mids, r); CheckRefs(p.Id, "paragraph objection", p.ObjectionIds, oids, r); CheckRefs(p.Id, "paragraph dependency", p.DependsOnParagraphIds, pids, r);
                if (p.Stale) r.Add(F("WARN", "STALE_PARAGRAPH", p.Id, p.StaleReasons.Count == 0 ? "Paragraph is marked stale." : String.Join("; ", p.StaleReasons.ToArray())));
            }
            foreach (LivingClaim c in v.Claims)
            {
                CheckRefs(c.Id, "claim evidence", c.EvidenceIds, eids, r); CheckRefs(c.Id, "claim calculation", c.CalculationIds, kids, r); CheckRefs(c.Id, "claim model", c.ModelIds, mids, r); CheckRefs(c.Id, "claim assumption", c.AssumptionIds, aids, r); CheckRefs(c.Id, "claim objection", c.ObjectionIds, oids, r); CheckRefs(c.Id, "claim parent", c.ParentClaimIds, cids, r);
                bool assertive = !String.Equals(c.Kind, "QUESTION", StringComparison.OrdinalIgnoreCase) && !String.Equals(c.Kind, "NOTE", StringComparison.OrdinalIgnoreCase);
                if (assertive && c.EvidenceIds.Count + c.CalculationIds.Count + c.ModelIds.Count == 0) r.Add(F("WARN", "UNSUPPORTED_CLAIM", c.Id, "Assertive claim has no evidence, calculation or model dependency."));
            }
            foreach (LivingEvidenceAnchor e in v.Evidence) { if (String.IsNullOrWhiteSpace(e.SourceFingerprint)) r.Add(F("ERROR", "UNFINGERPRINTED_EVIDENCE", e.Id, "Evidence source fingerprint is missing.")); if (String.IsNullOrWhiteSpace(e.CertificateHash)) r.Add(F("WARN", "UNCERTIFIED_EVIDENCE", e.Id, "Evidence certificate hash is missing.")); }
            foreach (LivingCalculation c in v.Calculations) { CheckRefs(c.Id, "calculation evidence", c.EvidenceIds, eids, r); if (String.IsNullOrWhiteSpace(c.Expression)) r.Add(F("WARN", "CALCULATION_EXPRESSION", c.Id, "Calculation expression is empty.")); if (String.IsNullOrWhiteSpace(c.Output)) r.Add(F("WARN", "CALCULATION_OUTPUT", c.Id, "Calculation output is empty.")); }
            foreach (LivingModelBinding m in v.Models) { if (String.IsNullOrWhiteSpace(m.SourceFingerprint)) r.Add(F("WARN", "MODEL_PROVENANCE", m.Id, "Model binding lacks a source fingerprint.")); if (m.Limitations.Count == 0) r.Add(F("WARN", "MODEL_LIMITATIONS", m.Id, "Model binding has no retained limitations.")); }
            foreach (LivingAssumption a in v.Assumptions) CheckRefs(a.Id, "assumption evidence", a.EvidenceIds, eids, r);
            foreach (LivingObjection o in v.Objections) { if (!cids.Contains(o.TargetClaimId ?? "")) r.Add(F("ERROR", "OBJECTION_TARGET", o.Id, "Objection targets a missing claim.")); CheckRefs(o.Id, "objection evidence", o.EvidenceIds, eids, r); if (String.Equals(o.Status, "OPEN", StringComparison.OrdinalIgnoreCase)) r.Add(F("INFO", "OPEN_OBJECTION", o.Id, "Material objection remains open by design.")); }
            foreach (LivingRequirement q in v.Requirements) { CheckRefs(q.Id, "requirement claim", q.ClaimIds, cids, r); CheckRefs(q.Id, "requirement evidence", q.EvidenceIds, eids, r); if (q.VerificationLinks.Count == 0) r.Add(F("WARN", "UNVERIFIED_REQUIREMENT", q.Id, "Requirement has no verification link.")); }
            if (HasCycle(v.Paragraphs)) r.Add(F("WARN", "PARAGRAPH_DEPENDENCY_CYCLE", v.Id, "Paragraph dependency graph contains a cycle. Cycles are retained but should be intentional."));
            string recomputed = Fingerprint(v); if (!String.IsNullOrWhiteSpace(v.RootFingerprint) && !String.Equals(v.RootFingerprint, recomputed, StringComparison.OrdinalIgnoreCase)) r.Add(F("WARN", "ROOT_FINGERPRINT_DRIFT", v.Id, "Retained version fingerprint differs from recomputed graph fingerprint."));
            if (r.Count == 0) r.Add(F("PASS", "TRACE_GRAPH", v.Id, "No structural traceability defects detected.")); return r;
        }

        public static Dictionary<string,double> Coverage(LivingVersion v)
        {
            Dictionary<string,double> d = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase); if (v == null) return d;
            List<LivingClaim> assertive = v.Claims.Where(x => !String.Equals(x.Kind, "QUESTION", StringComparison.OrdinalIgnoreCase) && !String.Equals(x.Kind, "NOTE", StringComparison.OrdinalIgnoreCase)).ToList();
            int supported = assertive.Count(x => x.EvidenceIds.Count + x.CalculationIds.Count + x.ModelIds.Count > 0);
            int challenged = assertive.Count(x => x.ObjectionIds.Count > 0);
            int paragraphsWithClaims = v.Paragraphs.Count(x => x.ClaimIds.Count > 0);
            int current = v.Paragraphs.Count(x => !x.Stale);
            d["ASSERTIVE_CLAIM_SUPPORT"] = Ratio(supported, assertive.Count); d["ASSERTIVE_CLAIM_CHALLENGE"] = Ratio(challenged, assertive.Count); d["PARAGRAPH_CLAIM_TRACE"] = Ratio(paragraphsWithClaims, v.Paragraphs.Count); d["PARAGRAPH_FRESHNESS"] = Ratio(current, v.Paragraphs.Count); d["REQUIREMENT_VERIFICATION"] = Ratio(v.Requirements.Count(x => x.VerificationLinks.Count > 0), v.Requirements.Count); return d;
        }

        public static LivingImpactReport Impact(LivingVersion v, string sourceKind, string sourceId)
        {
            LivingImpactReport report = new LivingImpactReport { Id = NewId("IMPACT"), VersionId = v == null ? "" : v.Id, SourceKind = sourceKind, SourceId = sourceId }; if (v == null || String.IsNullOrWhiteSpace(sourceId)) return report;
            HashSet<string> claims = new HashSet<string>(StringComparer.Ordinal); string k = (sourceKind ?? "").ToUpperInvariant();
            foreach (LivingClaim c in v.Claims)
            {
                bool hit = false;
                if (k == "EVIDENCE") hit = c.EvidenceIds.Contains(sourceId);
                else if (k == "CALCULATION") hit = c.CalculationIds.Contains(sourceId);
                else if (k == "MODEL") hit = c.ModelIds.Contains(sourceId);
                else if (k == "ASSUMPTION") hit = c.AssumptionIds.Contains(sourceId);
                else if (k == "OBJECTION") hit = c.ObjectionIds.Contains(sourceId);
                else if (k == "CLAIM") hit = c.Id == sourceId || c.ParentClaimIds.Contains(sourceId);
                if (hit) claims.Add(c.Id);
            }
            bool grew = true; while (grew) { grew = false; foreach (LivingClaim c in v.Claims) if (!claims.Contains(c.Id) && c.ParentClaimIds.Any(x => claims.Contains(x))) { claims.Add(c.Id); grew = true; } }
            report.Claims.AddRange(claims.OrderBy(x => x)); HashSet<string> paragraphs = new HashSet<string>(v.Paragraphs.Where(p => p.ClaimIds.Any(x => claims.Contains(x)) || (k == "EVIDENCE" && p.EvidenceIds.Contains(sourceId)) || (k == "CALCULATION" && p.CalculationIds.Contains(sourceId)) || (k == "MODEL" && p.ModelIds.Contains(sourceId)) || (k == "OBJECTION" && p.ObjectionIds.Contains(sourceId))).Select(x => x.Id), StringComparer.Ordinal);
            grew = true; while (grew) { grew = false; foreach (LivingParagraph p in v.Paragraphs) if (!paragraphs.Contains(p.Id) && p.DependsOnParagraphIds.Any(x => paragraphs.Contains(x))) { paragraphs.Add(p.Id); grew = true; } }
            report.Paragraphs.AddRange(paragraphs.OrderBy(x => x)); report.Reasons.Add("Dependency walk follows explicit typed links only; no semantic similarity edge is invented."); report.Reasons.Add("Paragraph-to-paragraph dependencies propagate consequence transitively."); report.CertificateHash = HashText(report.VersionId + "|" + k + "|" + sourceId + "|" + Join(report.Claims) + "|" + Join(report.Paragraphs)); return report;
        }

        public static int MarkSourceFingerprintChanged(LivingVersion v, string civilization, string sourceId, string oldFingerprint, string newFingerprint)
        {
            if (v == null || String.IsNullOrWhiteSpace(sourceId) || String.IsNullOrWhiteSpace(oldFingerprint) || String.Equals(oldFingerprint, newFingerprint, StringComparison.OrdinalIgnoreCase)) return 0;

            HashSet<string> evidence = new HashSet<string>(v.Evidence.Where(x =>
                String.Equals(x.SourceCivilization, civilization, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(x.SourceFingerprint, oldFingerprint, StringComparison.OrdinalIgnoreCase)).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> calculations = new HashSet<string>(v.Calculations.Where(x => x.EvidenceIds.Any(id => evidence.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> models = new HashSet<string>(v.Models.Where(x =>
                (String.Equals(x.SourceCivilization, civilization, StringComparison.OrdinalIgnoreCase) && String.Equals(x.SourceId, sourceId, StringComparison.OrdinalIgnoreCase) && String.Equals(x.SourceFingerprint, oldFingerprint, StringComparison.OrdinalIgnoreCase))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> assumptions = new HashSet<string>(v.Assumptions.Where(x => x.EvidenceIds.Any(id => evidence.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> objections = new HashSet<string>(v.Objections.Where(x => x.EvidenceIds.Any(id => evidence.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> requirements = new HashSet<string>(v.Requirements.Where(x => x.EvidenceIds.Any(id => evidence.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> decisions = new HashSet<string>(v.Decisions.Where(x => x.EvidenceIds.Any(id => evidence.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);
            HashSet<string> claims = new HashSet<string>(v.Claims.Where(x =>
                x.EvidenceIds.Any(id => evidence.Contains(id)) ||
                x.CalculationIds.Any(id => calculations.Contains(id)) ||
                x.ModelIds.Any(id => models.Contains(id)) ||
                x.AssumptionIds.Any(id => assumptions.Contains(id)) ||
                x.ObjectionIds.Any(id => objections.Contains(id))).Select(x => x.Id), StringComparer.Ordinal);

            bool claimGrew = true;
            while (claimGrew)
            {
                claimGrew = false;
                foreach (LivingClaim c in v.Claims)
                    if (!claims.Contains(c.Id) && c.ParentClaimIds.Any(id => claims.Contains(id))) { claims.Add(c.Id); claimGrew = true; }
            }

            HashSet<string> requirementClaims = new HashSet<string>(v.Requirements.Where(x => requirements.Contains(x.Id)).SelectMany(x => x.ClaimIds), StringComparer.Ordinal);
            HashSet<string> decisionClaims = new HashSet<string>(v.Decisions.Where(x => decisions.Contains(x.Id)).SelectMany(x => x.ClaimIds), StringComparer.Ordinal);
            foreach (string id in requirementClaims) claims.Add(id);
            foreach (string id in decisionClaims) claims.Add(id);

            string reason = "Upstream " + civilization + " source " + sourceId + " fingerprint changed " + Short(oldFingerprint) + " → " + Short(newFingerprint) + ".";
            int changed = 0;
            foreach (LivingParagraph p in v.Paragraphs)
            {
                bool affected = p.EvidenceIds.Any(id => evidence.Contains(id)) ||
                                p.CalculationIds.Any(id => calculations.Contains(id)) ||
                                p.ModelIds.Any(id => models.Contains(id)) ||
                                p.ObjectionIds.Any(id => objections.Contains(id)) ||
                                p.ClaimIds.Any(id => claims.Contains(id));
                if (!affected) continue;
                if (!p.Stale) changed++;
                p.Stale = true;
                if (!p.StaleReasons.Contains(reason)) p.StaleReasons.Add(reason);
            }

            bool grew = true;
            while (grew)
            {
                grew = false;
                HashSet<string> stale = new HashSet<string>(v.Paragraphs.Where(x => x.Stale).Select(x => x.Id), StringComparer.Ordinal);
                foreach (LivingParagraph p in v.Paragraphs)
                    if (!p.Stale && p.DependsOnParagraphIds.Any(id => stale.Contains(id))) { p.Stale = true; p.StaleReasons.Add("Depends on a stale paragraph."); changed++; grew = true; }
            }
            v.RootFingerprint = Fingerprint(v);
            return changed;
        }

        public static LivingVersionDiff Diff(LivingVersion a, LivingVersion b)
        {
            LivingVersionDiff d = new LivingVersionDiff { LeftId = a == null ? "" : a.Id, RightId = b == null ? "" : b.Id }; if (a == null || b == null) return d;
            Dictionary<string,LivingParagraph> ap = a.Paragraphs.ToDictionary(x => x.Id, StringComparer.Ordinal), bp = b.Paragraphs.ToDictionary(x => x.Id, StringComparer.Ordinal); foreach (string id in bp.Keys.Except(ap.Keys)) d.AddedParagraphs.Add(id); foreach (string id in ap.Keys.Except(bp.Keys)) d.RemovedParagraphs.Add(id); foreach (string id in ap.Keys.Intersect(bp.Keys)) if (!String.Equals(FingerprintParagraph(ap[id]), FingerprintParagraph(bp[id]), StringComparison.OrdinalIgnoreCase)) d.ChangedParagraphs.Add(id);
            Dictionary<string,LivingClaim> ac = a.Claims.ToDictionary(x => x.Id, StringComparer.Ordinal), bc = b.Claims.ToDictionary(x => x.Id, StringComparer.Ordinal); foreach (string id in bc.Keys.Except(ac.Keys)) d.AddedClaims.Add(id); foreach (string id in ac.Keys.Except(bc.Keys)) d.RemovedClaims.Add(id); foreach (string id in ac.Keys.Intersect(bc.Keys)) if (!String.Equals(FingerprintClaim(ac[id]), FingerprintClaim(bc[id]), StringComparison.OrdinalIgnoreCase)) d.ChangedClaims.Add(id); return d;
        }

        public static List<string> ParagraphGenealogy(LivingDocument d, string paragraphId)
        {
            List<string> rows = new List<string>(); if (d == null || String.IsNullOrWhiteSpace(paragraphId)) return rows;
            foreach (LivingVersion v in d.Versions.OrderBy(x => x.Number)) { LivingParagraph p = v.Paragraphs.FirstOrDefault(x => x.Id == paragraphId || x.PreviousParagraphId == paragraphId); if (p != null) rows.Add("v" + v.Number + "   ·   " + v.Id + "   ·   " + p.Id + "   ·   previous " + (p.PreviousParagraphId ?? "∅") + "   ·   fp " + Short(FingerprintParagraph(p)) + "   ·   " + (p.Heading ?? "")); }
            return rows;
        }

        public static string RenderMarkdown(LivingDocument d, LivingVersion v)
        {
            if (d == null || v == null) return ""; StringBuilder b = new StringBuilder(); b.AppendLine("# " + (d.Title ?? "Untitled")); b.AppendLine(); b.AppendLine("**Kind:** " + (d.Kind ?? "DOCUMENT") + "  "); b.AppendLine("**Version:** " + v.Number + "  "); b.AppendLine("**Fingerprint:** `" + (v.RootFingerprint ?? Fingerprint(v)) + "`"); b.AppendLine(); if (!String.IsNullOrWhiteSpace(d.Objective)) { b.AppendLine("## Objective"); b.AppendLine(); b.AppendLine(d.Objective); b.AppendLine(); }
            Dictionary<string,LivingParagraph> pmap = v.Paragraphs.ToDictionary(x => x.Id, StringComparer.Ordinal); foreach (LivingSection s in v.Sections.OrderBy(x => x.Order)) { b.AppendLine("## " + (s.Title ?? "Section")); b.AppendLine(); foreach (string id in s.ParagraphIds) { LivingParagraph p; if (!pmap.TryGetValue(id, out p)) continue; if (!String.IsNullOrWhiteSpace(p.Heading)) { b.AppendLine("### " + p.Heading); b.AppendLine(); } b.AppendLine(p.Text ?? ""); b.AppendLine(); b.AppendLine("<!-- DOCΩ para=" + p.Id + " fp=" + FingerprintParagraph(p) + " claims=" + Join(p.ClaimIds) + " evidence=" + Join(p.EvidenceIds) + " -->"); b.AppendLine(); } }
            b.AppendLine("---"); b.AppendLine(); b.AppendLine("## Traceability appendix"); b.AppendLine(); foreach (LivingClaim c in v.Claims) b.AppendLine("- **" + c.Id + "** " + (c.Text ?? "") + " — evidence: " + Join(c.EvidenceIds) + "; calculations: " + Join(c.CalculationIds) + "; models: " + Join(c.ModelIds) + "; objections: " + Join(c.ObjectionIds)); return b.ToString();
        }

        private static LivingValidationFinding F(string sev, string kind, string id, string msg) { return new LivingValidationFinding { Severity = sev, Kind = kind, SubjectId = id, Message = msg }; }
        private static HashSet<string> Set(IEnumerable<string> ids) { return new HashSet<string>((ids ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.Ordinal); }
        private static void CheckRefs(string subject, string kind, IEnumerable<string> refs, HashSet<string> valid, List<LivingValidationFinding> r) { foreach (string id in refs ?? Enumerable.Empty<string>()) if (!valid.Contains(id)) r.Add(F("ERROR", "BROKEN_REFERENCE", subject, kind + " references missing id " + id + ".")); }
        private static void Duplicates(IEnumerable<string> ids, string kind, List<LivingValidationFinding> r) { foreach (IGrouping<string,string> g in (ids ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1)) r.Add(F("ERROR", "DUPLICATE_ID", g.Key, kind + " id appears " + g.Count() + " times.")); }
        private static bool HasCycle(List<LivingParagraph> paragraphs) { Dictionary<string,LivingParagraph> map = paragraphs.ToDictionary(x => x.Id, StringComparer.Ordinal); HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal), done = new HashSet<string>(StringComparer.Ordinal); foreach (string id in map.Keys) if (Cycle(id, map, visiting, done)) return true; return false; }
        private static bool Cycle(string id, Dictionary<string,LivingParagraph> map, HashSet<string> visiting, HashSet<string> done) { if (done.Contains(id)) return false; if (!visiting.Add(id)) return true; LivingParagraph p; if (map.TryGetValue(id, out p)) foreach (string dep in p.DependsOnParagraphIds) if (map.ContainsKey(dep) && Cycle(dep, map, visiting, done)) return true; visiting.Remove(id); done.Add(id); return false; }
        private static double Ratio(int n, int d) { return d <= 0 ? 1.0 : (double)n / (double)d; }
        private static string Join(IEnumerable<string> xs) { return String.Join(",", (xs ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray()); }
    }
}

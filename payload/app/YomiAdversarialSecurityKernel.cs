using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    // DEV13.37.23 is deliberately a closed, abstract defensive laboratory.  The
    // ontology stores references and state transitions; it has no network,
    // credential, process-control, scanner, exploit, or command-execution surface.
    internal sealed class SecurityZone
    {
        public string Id { get; set; } public string Name { get; set; } public string Kind { get; set; }
        public string ParentId { get; set; } public string Classification { get; set; }
        public double Assurance { get; set; } public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SecurityZone() { Kind = "TRUST_ZONE"; Classification = "INTERNAL"; Assurance = 0.6; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "ZONE") + "   ·   " + (Kind ?? "TRUST_ZONE") + " / " + (Classification ?? "INTERNAL") + "   ·   assurance " + Assurance.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAsset
    {
        public string Id { get; set; } public string Name { get; set; } public string Kind { get; set; }
        public string ZoneId { get; set; } public string OwnerIdentityId { get; set; } public string Classification { get; set; }
        public double Confidentiality { get; set; } public double Integrity { get; set; } public double Availability { get; set; }
        public double BusinessValue { get; set; } public double Exposure { get; set; } public bool CrownJewel { get; set; }
        public string Status { get; set; } public List<string> DependencyIds { get; set; }
        public Dictionary<string, string> Labels { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityAsset() { Kind = "SERVICE"; Classification = "INTERNAL"; Confidentiality = 0.5; Integrity = 0.5; Availability = 0.5; BusinessValue = 0.5; Exposure = 0.2; Status = "ACTIVE"; DependencyIds = new List<string>(); Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public double Criticality { get { return SecurityKernel.Clamp01((Confidentiality + Integrity + Availability + BusinessValue) / 4.0); } }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (CrownJewel ? "CROWN   ·   " : "") + (Name ?? Id ?? "ASSET") + "   ·   " + (Kind ?? "SERVICE") + " @ " + (ZoneId ?? "ZONE") + "   ·   C/I/A/V " + Confidentiality.ToString("0.00", CultureInfo.InvariantCulture) + "/" + Integrity.ToString("0.00", CultureInfo.InvariantCulture) + "/" + Availability.ToString("0.00", CultureInfo.InvariantCulture) + "/" + BusinessValue.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityIdentity
    {
        public string Id { get; set; } public string Name { get; set; } public string Kind { get; set; }
        public string OwnerIdentityId { get; set; } public string HomeZoneId { get; set; } public string AssuranceLevel { get; set; }
        public double Assurance { get; set; } public bool Privileged { get; set; } public bool BreakGlass { get; set; }
        public string LastReviewedUtc { get; set; } public string Status { get; set; }
        public List<string> GroupIds { get; set; } public List<string> FactorKinds { get; set; }
        public Dictionary<string, string> Attributes { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityIdentity() { Kind = "HUMAN"; AssuranceLevel = "STANDARD"; Assurance = 0.5; Status = "ACTIVE"; GroupIds = new List<string>(); FactorKinds = new List<string>(); Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Privileged ? "PRIVILEGED   ·   " : "") + (Name ?? Id ?? "IDENTITY") + "   ·   " + (Kind ?? "HUMAN") + "   ·   " + (AssuranceLevel ?? "STANDARD") + " / " + Assurance.ToString("P0", CultureInfo.InvariantCulture) + "   ·   groups " + GroupIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecuritySecretReference
    {
        public string Id { get; set; } public string Name { get; set; } public string Kind { get; set; }
        public string CustodianIdentityId { get; set; } public string BoundAssetId { get; set; } public string ReferenceLocator { get; set; }
        public double AgeDays { get; set; } public double RotationTargetDays { get; set; } public double Exposure { get; set; }
        public string Status { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecuritySecretReference() { Kind = "MANAGED_REFERENCE"; RotationTargetDays = 90; Status = "ACTIVE_REFERENCE_ONLY"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE_REFERENCE_ONLY") + "   ·   " + (Name ?? Id ?? "REFERENCE") + "   ·   " + (Kind ?? "MANAGED_REFERENCE") + " → " + (BoundAssetId ?? "ASSET") + "   ·   age/target " + AgeDays.ToString("0.#", CultureInfo.InvariantCulture) + "/" + RotationTargetDays.ToString("0.#", CultureInfo.InvariantCulture) + "d   ·   locator metadata only"; }
    }

    internal sealed class SecurityPermission
    {
        public string Id { get; set; } public string SubjectId { get; set; } public string ResourceId { get; set; }
        public string Action { get; set; } public string Effect { get; set; } public string Condition { get; set; }
        public string Source { get; set; } public string ExpiresUtc { get; set; } public bool JustInTime { get; set; }
        public string Status { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityPermission() { Action = "READ"; Effect = "ALLOW"; Source = "POLICY"; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Effect ?? "ALLOW") + "   ·   " + (SubjectId ?? "SUBJECT") + " → " + (Action ?? "READ") + " → " + (ResourceId ?? "RESOURCE") + (JustInTime ? "   ·   JIT" : "") + (String.IsNullOrWhiteSpace(ExpiresUtc) ? "" : "   ·   expires " + ExpiresUtc); }
    }

    internal sealed class SecurityTrustRelation
    {
        public string Id { get; set; } public string FromId { get; set; } public string ToId { get; set; }
        public string Kind { get; set; } public double Strength { get; set; } public bool Transitive { get; set; }
        public string Condition { get; set; } public string Status { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityTrustRelation() { Kind = "AUTHENTICATION_TRUST"; Strength = 0.5; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (FromId ?? "FROM") + " ⇢ " + (ToId ?? "TO") + "   ·   " + (Kind ?? "TRUST") + " / " + Strength.ToString("P0", CultureInfo.InvariantCulture) + (Transitive ? "   ·   transitive" : "   ·   bounded"); }
    }

    internal sealed class SecurityInterface
    {
        public string Id { get; set; } public string Name { get; set; } public string AssetId { get; set; }
        public string FromZoneId { get; set; } public string ToZoneId { get; set; } public string ChannelClass { get; set; }
        public string AuthenticationClass { get; set; } public bool Encrypted { get; set; } public bool RateLimited { get; set; }
        public double Exposure { get; set; } public string Status { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityInterface() { ChannelClass = "APPLICATION"; AuthenticationClass = "STRONG"; Encrypted = true; RateLimited = true; Exposure = 0.2; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "INTERFACE") + "   ·   " + (FromZoneId ?? "FROM") + " → " + (ToZoneId ?? "TO") + " / " + (AssetId ?? "ASSET") + "   ·   " + (AuthenticationClass ?? "AUTH") + "   ·   enc/rate " + Encrypted + "/" + RateLimited + "   ·   exposure " + Exposure.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityThreat
    {
        public string Id { get; set; } public string Name { get; set; } public string ActorClass { get; set; }
        public string Objective { get; set; } public string TacticClass { get; set; } public string TargetAssetId { get; set; }
        public string InitialEntityId { get; set; } public double Capability { get; set; } public double Intent { get; set; }
        public double Likelihood { get; set; } public double ImpactMultiplier { get; set; } public string Status { get; set; }
        public List<string> Assumptions { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityThreat() { ActorClass = "SYNTHETIC_ADVERSARY"; TacticClass = "ABSTRACT"; Capability = 0.5; Intent = 0.5; Likelihood = 0.2; ImpactMultiplier = 1; Status = "MODELED"; Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "MODELED") + "   ·   " + (Name ?? Id ?? "THREAT") + "   ·   " + (ActorClass ?? "SYNTHETIC") + " / " + (TacticClass ?? "ABSTRACT") + "   ·   target " + (TargetAssetId ?? "∅") + "   ·   likelihood " + Likelihood.ToString("P1", CultureInfo.InvariantCulture) + " / impact ×" + ImpactMultiplier.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityWeakness
    {
        public string Id { get; set; } public string Name { get; set; } public string AssetId { get; set; }
        public string Kind { get; set; } public string Preconditions { get; set; } public double Severity { get; set; }
        public double Exploitability { get; set; } public double Confidence { get; set; } public string Status { get; set; }
        public List<string> ControlIds { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityWeakness() { Kind = "CONTROL_GAP"; Severity = 0.5; Exploitability = 0.3; Confidence = 0.5; Status = "OPEN"; ControlIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "OPEN") + "   ·   " + (Name ?? Id ?? "WEAKNESS") + "   ·   " + (Kind ?? "CONTROL_GAP") + " @ " + (AssetId ?? "ASSET") + "   ·   severity/exploitability/confidence " + Severity.ToString("0.00", CultureInfo.InvariantCulture) + "/" + Exploitability.ToString("0.00", CultureInfo.InvariantCulture) + "/" + Confidence.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityControl
    {
        public string Id { get; set; } public string Name { get; set; } public string Family { get; set; }
        public string Phase { get; set; } public string TargetId { get; set; } public string ThreatId { get; set; }
        public double Effectiveness { get; set; } public double Independence { get; set; } public double Coverage { get; set; }
        public double DetectionLatencyMinutes { get; set; } public double ResponseLatencyMinutes { get; set; }
        public double AnnualCost { get; set; } public string EvidenceStrength { get; set; } public string Status { get; set; }
        public List<string> DependsOnControlIds { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityControl() { Family = "PROTECT"; Phase = "PREVENT"; Effectiveness = 0.5; Independence = 0.5; Coverage = 0.5; EvidenceStrength = "MODELED"; Status = "ACTIVE"; DependsOnControlIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "CONTROL") + "   ·   " + (Family ?? "PROTECT") + "/" + (Phase ?? "PREVENT") + " → " + (TargetId ?? ThreatId ?? "GLOBAL") + "   ·   effect/coverage/independence " + Effectiveness.ToString("P0", CultureInfo.InvariantCulture) + "/" + Coverage.ToString("P0", CultureInfo.InvariantCulture) + "/" + Independence.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAttackStep
    {
        public string Id { get; set; } public string Name { get; set; } public string TacticClass { get; set; }
        public string SourceEntityId { get; set; } public string TargetEntityId { get; set; }
        public string RequiredIdentityId { get; set; } public string RequiredPermissionAction { get; set; }
        public string RequiredWeaknessId { get; set; } public string ResultState { get; set; }
        public double BaseProbability { get; set; } public double Effort { get; set; } public double Noise { get; set; }
        public string Status { get; set; } public List<string> MitigatingControlIds { get; set; } public List<string> EvidenceNodeIds { get; set; }
        public SecurityAttackStep() { TacticClass = "ABSTRACT_TRANSITION"; ResultState = "COMPROMISED"; BaseProbability = 0.25; Effort = 0.5; Noise = 0.5; Status = "MODELED_ONLY"; MitigatingControlIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "MODELED_ONLY") + "   ·   " + (Name ?? Id ?? "STEP") + "   ·   " + (SourceEntityId ?? "SOURCE") + " ⇢ " + (TargetEntityId ?? "TARGET") + "   ·   " + (TacticClass ?? "ABSTRACT") + "   ·   p=" + BaseProbability.ToString("P1", CultureInfo.InvariantCulture) + " / effort " + Effort.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityScenario
    {
        public string Id { get; set; } public string Name { get; set; } public string ThreatIds { get; set; }
        public string InitialCompromiseIds { get; set; } public string DisabledControlIds { get; set; }
        public double LikelihoodMultiplier { get; set; } public double ImpactMultiplier { get; set; }
        public int MaxStates { get; set; } public int MaxDepth { get; set; } public double Weight { get; set; }
        public string Status { get; set; } public List<string> Assumptions { get; set; }
        public SecurityScenario() { LikelihoodMultiplier = 1; ImpactMultiplier = 1; MaxStates = 4096; MaxDepth = 16; Weight = 1; Status = "ACTIVE"; Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SCENARIO") + "   ·   likelihood/impact ×" + LikelihoodMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + ImpactMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   bounds " + MaxStates.ToString(CultureInfo.InvariantCulture) + " states / " + MaxDepth.ToString(CultureInfo.InvariantCulture) + " depth"; }
    }

    internal sealed class SecurityInvariant
    {
        public string Id { get; set; } public string Name { get; set; } public string Kind { get; set; }
        public string SubjectId { get; set; } public string ResourceId { get; set; } public string Action { get; set; }
        public string Severity { get; set; } public string Status { get; set; }
        public SecurityInvariant() { Kind = "CUSTOM"; Severity = "HIGH"; Status = "ACTIVE"; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Severity ?? "HIGH") + "   ·   " + (Name ?? Id ?? "INVARIANT") + "   ·   " + (Kind ?? "CUSTOM"); }
    }

    internal sealed class SecurityModel
    {
        public string Id { get; set; } public string ParentModelId { get; set; } public string Name { get; set; }
        public string Description { get; set; } public string Status { get; set; } public int Revision { get; set; }
        public int Seed { get; set; } public string CreatedUtc { get; set; } public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; } public string SourceId { get; set; } public string SourceFingerprint { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<SecurityZone> Zones { get; set; } public List<SecurityAsset> Assets { get; set; }
        public List<SecurityIdentity> Identities { get; set; } public List<SecuritySecretReference> SecretReferences { get; set; }
        public List<SecurityPermission> Permissions { get; set; } public List<SecurityTrustRelation> TrustRelations { get; set; }
        public List<SecurityInterface> Interfaces { get; set; } public List<SecurityThreat> Threats { get; set; }
        public List<SecurityWeakness> Weaknesses { get; set; } public List<SecurityControl> Controls { get; set; }
        public List<SecurityAttackStep> AttackSteps { get; set; } public List<SecurityScenario> Scenarios { get; set; }
        public List<SecurityInvariant> Invariants { get; set; } public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SecurityModel() { Status = "ACTIVE"; Revision = 1; Seed = 1337; SourceKind = "NATIVE"; Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Zones = new List<SecurityZone>(); Assets = new List<SecurityAsset>(); Identities = new List<SecurityIdentity>(); SecretReferences = new List<SecuritySecretReference>(); Permissions = new List<SecurityPermission>(); TrustRelations = new List<SecurityTrustRelation>(); Interfaces = new List<SecurityInterface>(); Threats = new List<SecurityThreat>(); Weaknesses = new List<SecurityWeakness>(); Controls = new List<SecurityControl>(); AttackSteps = new List<SecurityAttackStep>(); Scenarios = new List<SecurityScenario>(); Invariants = new List<SecurityInvariant>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SECURITY MODEL") + "   ·   " + Assets.Count.ToString(CultureInfo.InvariantCulture) + " assets / " + Identities.Count.ToString(CultureInfo.InvariantCulture) + " identities / " + Threats.Count.ToString(CultureInfo.InvariantCulture) + " threats / " + Controls.Count.ToString(CultureInfo.InvariantCulture) + " controls   ·   r" + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityValidationIssue
    {
        public string Severity { get; set; } public string Code { get; set; } public string SubjectId { get; set; } public string Detail { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Code ?? "ISSUE") + "   ·   " + (SubjectId ?? "MODEL") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class SecurityAccessDecision
    {
        public string SubjectId { get; set; } public string ResourceId { get; set; } public string Action { get; set; }
        public string Decision { get; set; } public string Reason { get; set; } public bool DenyOverride { get; set; }
        public List<string> EffectiveSubjectIds { get; set; } public List<string> MatchingPermissionIds { get; set; }
        public string CertificateHash { get; set; }
        public SecurityAccessDecision() { EffectiveSubjectIds = new List<string>(); MatchingPermissionIds = new List<string>(); }
        public override string ToString() { return (Decision ?? "DENY") + "   ·   " + (SubjectId ?? "SUBJECT") + " → " + (Action ?? "ACTION") + " → " + (ResourceId ?? "RESOURCE") + "   ·   " + (Reason ?? "") + "   ·   cert " + SecurityKernel.ShortHash(CertificateHash); }
    }

    internal sealed class SecurityEntitlementFinding
    {
        public string Severity { get; set; } public string Kind { get; set; } public string SubjectId { get; set; }
        public string ResourceId { get; set; } public string PermissionId { get; set; } public string Detail { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "FINDING") + "   ·   " + (SubjectId ?? ResourceId ?? "MODEL") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class SecurityEntitlementAssessment
    {
        public string ModelId { get; set; } public int ActiveIdentities { get; set; } public int ActivePermissions { get; set; }
        public int PrivilegedIdentities { get; set; } public int ExpiringPermissions { get; set; } public int DenyRules { get; set; }
        public List<SecurityEntitlementFinding> Findings { get; set; } public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecurityEntitlementAssessment() { Findings = new List<SecurityEntitlementFinding>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   identities/permissions " + ActiveIdentities.ToString(CultureInfo.InvariantCulture) + "/" + ActivePermissions.ToString(CultureInfo.InvariantCulture) + "   ·   privileged " + PrivilegedIdentities.ToString(CultureInfo.InvariantCulture) + "   ·   findings " + Findings.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAttackGraphEdge
    {
        public string StepId { get; set; } public string FromId { get; set; } public string ToId { get; set; }
        public double EffectiveProbability { get; set; } public double Cost { get; set; } public double Detectability { get; set; }
        public List<string> ActiveControlIds { get; set; } public string Reason { get; set; }
        public SecurityAttackGraphEdge() { ActiveControlIds = new List<string>(); }
        public override string ToString() { return (FromId ?? "FROM") + " ⇢ " + (ToId ?? "TO") + "   ·   " + (StepId ?? "STEP") + "   ·   p=" + EffectiveProbability.ToString("P2", CultureInfo.InvariantCulture) + " / cost " + Cost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   controls " + ActiveControlIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAttackRoute
    {
        public string SourceId { get; set; } public string TargetId { get; set; } public List<string> EntityIds { get; set; }
        public List<string> StepIds { get; set; } public double Probability { get; set; } public double Cost { get; set; }
        public double ExpectedImpact { get; set; } public string Verdict { get; set; }
        public SecurityAttackRoute() { EntityIds = new List<string>(); StepIds = new List<string>(); }
        public override string ToString() { return (Verdict ?? "ROUTE") + "   ·   " + (SourceId ?? "SOURCE") + " ⇢ " + (TargetId ?? "TARGET") + "   ·   " + StepIds.Count.ToString(CultureInfo.InvariantCulture) + " transitions   ·   p=" + Probability.ToString("P3", CultureInfo.InvariantCulture) + " / expected impact " + ExpectedImpact.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAttackGraphResult
    {
        public string ModelId { get; set; } public string ScenarioId { get; set; } public int Nodes { get; set; }
        public int ExploredStates { get; set; } public bool Truncated { get; set; }
        public List<SecurityAttackGraphEdge> Edges { get; set; } public List<SecurityAttackRoute> CrownJewelRoutes { get; set; }
        public List<string> UnreachableCrownJewels { get; set; } public string CertificateHash { get; set; }
        public SecurityAttackGraphResult() { Edges = new List<SecurityAttackGraphEdge>(); CrownJewelRoutes = new List<SecurityAttackRoute>(); UnreachableCrownJewels = new List<string>(); }
        public override string ToString() { return "ATTACK GRAPH   ·   " + Nodes.ToString(CultureInfo.InvariantCulture) + " nodes / " + Edges.Count.ToString(CultureInfo.InvariantCulture) + " abstract edges / " + CrownJewelRoutes.Count.ToString(CultureInfo.InvariantCulture) + " crown routes   ·   explored " + ExploredStates.ToString(CultureInfo.InvariantCulture) + (Truncated ? "   ·   BOUNDED/TRUNCATED" : "   ·   COMPLETE WITHIN BOUNDS"); }
    }

    internal sealed class SecurityBlastRadiusItem
    {
        public string EntityId { get; set; } public int Depth { get; set; } public double ReachProbability { get; set; }
        public double Criticality { get; set; } public string Via { get; set; } public string ParentId { get; set; }
        public override string ToString() { return "D" + Depth.ToString(CultureInfo.InvariantCulture) + "   ·   " + (EntityId ?? "ENTITY") + "   ·   reach " + ReachProbability.ToString("P2", CultureInfo.InvariantCulture) + " / criticality " + Criticality.ToString("0.00", CultureInfo.InvariantCulture) + "   ·   via " + (Via ?? "SEED"); }
    }

    internal sealed class SecurityBlastRadiusResult
    {
        public string SeedId { get; set; } public List<SecurityBlastRadiusItem> Items { get; set; }
        public int CrownJewelsReached { get; set; } public double ExpectedLoss { get; set; } public int MaxDepth { get; set; }
        public string CertificateHash { get; set; }
        public SecurityBlastRadiusResult() { Items = new List<SecurityBlastRadiusItem>(); }
        public override string ToString() { return "BLAST RADIUS   ·   seed " + (SeedId ?? "∅") + "   ·   " + Items.Count.ToString(CultureInfo.InvariantCulture) + " entities / " + CrownJewelsReached.ToString(CultureInfo.InvariantCulture) + " crown jewels   ·   expected loss " + ExpectedLoss.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityCoverageRow
    {
        public string SubjectId { get; set; } public string SubjectKind { get; set; }
        public int Prevent { get; set; } public int Detect { get; set; } public int Respond { get; set; } public int Recover { get; set; }
        public double CombinedEffectiveness { get; set; } public double Independence { get; set; } public string Gap { get; set; }
        public List<string> ControlIds { get; set; } public SecurityCoverageRow() { ControlIds = new List<string>(); }
        public override string ToString() { return (String.IsNullOrWhiteSpace(Gap) ? "COVERED" : "GAP " + Gap) + "   ·   " + (SubjectKind ?? "SUBJECT") + " " + (SubjectId ?? "∅") + "   ·   P/D/R/R " + Prevent + "/" + Detect + "/" + Respond + "/" + Recover + "   ·   effect/independence " + CombinedEffectiveness.ToString("P0", CultureInfo.InvariantCulture) + "/" + Independence.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityCoverageResult
    {
        public string ModelId { get; set; } public List<SecurityCoverageRow> Rows { get; set; }
        public int CompleteChains { get; set; } public int SingleControlFailures { get; set; } public int UncoveredCrownJewels { get; set; }
        public double MeanEffectiveness { get; set; } public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecurityCoverageResult() { Rows = new List<SecurityCoverageRow>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + Rows.Count.ToString(CultureInfo.InvariantCulture) + " coverage rows / " + CompleteChains.ToString(CultureInfo.InvariantCulture) + " complete chains / " + SingleControlFailures.ToString(CultureInfo.InvariantCulture) + " single-control dependencies   ·   mean effect " + MeanEffectiveness.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityRiskRow
    {
        public string ThreatId { get; set; } public string AssetId { get; set; } public double Likelihood { get; set; }
        public double Impact { get; set; } public double InherentRisk { get; set; } public double ResidualRisk { get; set; }
        public double Uncertainty { get; set; } public string Band { get; set; } public List<string> ControlIds { get; set; }
        public SecurityRiskRow() { ControlIds = new List<string>(); }
        public override string ToString() { return (Band ?? "UNRATED") + "   ·   " + (ThreatId ?? "THREAT") + " → " + (AssetId ?? "ASSET") + "   ·   inherent/residual " + InherentRisk.ToString("0.000", CultureInfo.InvariantCulture) + "/" + ResidualRisk.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   uncertainty ±" + Uncertainty.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityRiskRegister
    {
        public string ModelId { get; set; } public string ScenarioId { get; set; } public List<SecurityRiskRow> Rows { get; set; }
        public double TotalInherentRisk { get; set; } public double TotalResidualRisk { get; set; }
        public double TailRisk { get; set; } public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecurityRiskRegister() { Rows = new List<SecurityRiskRow>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + Rows.Count.ToString(CultureInfo.InvariantCulture) + " risks   ·   inherent/residual " + TotalInherentRisk.ToString("0.000", CultureInfo.InvariantCulture) + "/" + TotalResidualRisk.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   tail " + TailRisk.ToString("0.000", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecuritySegmentationCut
    {
        public string SourceId { get; set; } public string TargetId { get; set; } public double CutCapacity { get; set; }
        public List<string> BoundaryIds { get; set; } public List<string> RecommendedControlIds { get; set; }
        public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecuritySegmentationCut() { BoundaryIds = new List<string>(); RecommendedControlIds = new List<string>(); }
        public override string ToString() { return (Verdict ?? "CUT") + "   ·   " + (SourceId ?? "SOURCE") + " ⇢ " + (TargetId ?? "TARGET") + "   ·   capacity " + CutCapacity.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   boundaries " + BoundaryIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityTimelineEvent
    {
        public int Sequence { get; set; } public double Minute { get; set; } public string Phase { get; set; }
        public string SubjectId { get; set; } public string Event { get; set; } public string DecisionBasis { get; set; }
        public string EvidenceState { get; set; } public string ChainHash { get; set; }
        public override string ToString() { return Sequence.ToString("000", CultureInfo.InvariantCulture) + "   ·   T+" + Minute.ToString("0.0", CultureInfo.InvariantCulture) + "m   ·   " + (Phase ?? "PHASE") + "   ·   " + (Event ?? "EVENT") + "   ·   " + (SubjectId ?? "SUBJECT"); }
    }

    internal sealed class SecurityContainmentAction
    {
        public int Priority { get; set; } public string Kind { get; set; } public string TargetId { get; set; }
        public string ExpectedEffect { get; set; } public double ServiceCost { get; set; } public double RiskReduction { get; set; }
        public bool RequiresAuthorization { get; set; } public bool EvidencePreserving { get; set; } public string ExecutionMode { get; set; }
        public override string ToString() { return "P" + Priority.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Kind ?? "ACTION") + " → " + (TargetId ?? "TARGET") + "   ·   risk −" + RiskReduction.ToString("P1", CultureInfo.InvariantCulture) + " / service cost " + ServiceCost.ToString("P1", CultureInfo.InvariantCulture) + "   ·   " + (ExecutionMode ?? "PLAN_ONLY"); }
    }

    internal sealed class SecurityIncidentPlan
    {
        public string Id { get; set; } public string ModelId { get; set; } public string ScenarioId { get; set; }
        public string Name { get; set; } public string Severity { get; set; } public string Status { get; set; }
        public double MttdMinutes { get; set; } public double MttcMinutes { get; set; } public double MttrMinutes { get; set; }
        public List<SecurityTimelineEvent> Timeline { get; set; } public List<SecurityContainmentAction> Actions { get; set; }
        public List<string> AffectedEntityIds { get; set; } public List<string> Assumptions { get; set; }
        public string CertificateHash { get; set; }
        public SecurityIncidentPlan() { Status = "SIMULATION_ONLY"; Timeline = new List<SecurityTimelineEvent>(); Actions = new List<SecurityContainmentAction>(); AffectedEntityIds = new List<string>(); Assumptions = new List<string>(); }
        public override string ToString() { return (Severity ?? "UNRATED") + "   ·   " + (Name ?? Id ?? "INCIDENT") + "   ·   " + (Status ?? "SIMULATION_ONLY") + "   ·   MTTD/MTTC/MTTR " + MttdMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "/" + MttcMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "/" + MttrMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "m   ·   " + Actions.Count.ToString(CultureInfo.InvariantCulture) + " planned actions"; }
    }

    internal sealed class SecurityCampaignWorld
    {
        public string ScenarioId { get; set; } public double Weight { get; set; } public double ResidualRisk { get; set; }
        public int ReachableCrownJewels { get; set; } public double ExpectedLoss { get; set; }
        public double MttdMinutes { get; set; } public double MttcMinutes { get; set; } public string Verdict { get; set; }
        public override string ToString() { return (Verdict ?? "WORLD") + "   ·   " + (ScenarioId ?? "BASE") + "   ·   weight " + Weight.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   residual " + ResidualRisk.ToString("0.000", CultureInfo.InvariantCulture) + " / crown reach " + ReachableCrownJewels.ToString(CultureInfo.InvariantCulture) + " / MTTD-MTTC " + MttdMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "-" + MttcMinutes.ToString("0.0", CultureInfo.InvariantCulture) + "m"; }
    }

    internal sealed class SecurityCampaignResult
    {
        public string ModelId { get; set; } public List<SecurityCampaignWorld> Worlds { get; set; }
        public double WeightedResidualRisk { get; set; } public double WorstResidualRisk { get; set; }
        public double ResilienceScore { get; set; } public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecurityCampaignResult() { Worlds = new List<SecurityCampaignWorld>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + Worlds.Count.ToString(CultureInfo.InvariantCulture) + " adversarial worlds   ·   weighted/worst residual " + WeightedResidualRisk.ToString("0.000", CultureInfo.InvariantCulture) + "/" + WorstResidualRisk.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   resilience " + ResilienceScore.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAssuranceClaim
    {
        public string Id { get; set; } public string ParentId { get; set; } public string Kind { get; set; }
        public string Statement { get; set; } public string Status { get; set; } public string Basis { get; set; }
        public List<string> EvidenceIds { get; set; } public List<string> DefeaterIds { get; set; }
        public SecurityAssuranceClaim() { Kind = "CLAIM"; Status = "UNRESOLVED"; EvidenceIds = new List<string>(); DefeaterIds = new List<string>(); }
        public override string ToString() { return (Status ?? "UNRESOLVED") + "   ·   " + (Kind ?? "CLAIM") + "   ·   " + (Statement ?? Id ?? "ASSURANCE CLAIM") + "   ·   evidence/defeaters " + EvidenceIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + DefeaterIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SecurityAssuranceCase
    {
        public string ModelId { get; set; } public List<SecurityAssuranceClaim> Claims { get; set; }
        public int Satisfied { get; set; } public int Violated { get; set; } public int Unresolved { get; set; }
        public string Verdict { get; set; } public string CertificateHash { get; set; }
        public SecurityAssuranceCase() { Claims = new List<SecurityAssuranceClaim>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + Claims.Count.ToString(CultureInfo.InvariantCulture) + " claims   ·   satisfied/violated/unresolved " + Satisfied.ToString(CultureInfo.InvariantCulture) + "/" + Violated.ToString(CultureInfo.InvariantCulture) + "/" + Unresolved.ToString(CultureInfo.InvariantCulture); }
    }

    internal static class SecurityKernel
    {
        public const int MaxAssets = 4096, MaxIdentities = 8192, MaxPermissions = 65536, MaxSteps = 65536, MaxGraphStates = 131072, MaxDepth = 64;

        private sealed class RouteState { public string Id; public double Cost; public int Depth; public string Parent; public string Step; }
        private sealed class FlowEdge { public string From; public string To; public double Capacity; public double Residual; public string Boundary; }

        public static void Normalize(SecurityModel m)
        {
            if (m == null) return;
            if (m.Parameters == null) m.Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (m.Zones == null) m.Zones = new List<SecurityZone>(); if (m.Assets == null) m.Assets = new List<SecurityAsset>();
            if (m.Identities == null) m.Identities = new List<SecurityIdentity>(); if (m.SecretReferences == null) m.SecretReferences = new List<SecuritySecretReference>();
            if (m.Permissions == null) m.Permissions = new List<SecurityPermission>(); if (m.TrustRelations == null) m.TrustRelations = new List<SecurityTrustRelation>();
            if (m.Interfaces == null) m.Interfaces = new List<SecurityInterface>(); if (m.Threats == null) m.Threats = new List<SecurityThreat>();
            if (m.Weaknesses == null) m.Weaknesses = new List<SecurityWeakness>(); if (m.Controls == null) m.Controls = new List<SecurityControl>();
            if (m.AttackSteps == null) m.AttackSteps = new List<SecurityAttackStep>(); if (m.Scenarios == null) m.Scenarios = new List<SecurityScenario>();
            if (m.Invariants == null) m.Invariants = new List<SecurityInvariant>(); if (m.Assumptions == null) m.Assumptions = new List<string>(); if (m.EvidenceNodeIds == null) m.EvidenceNodeIds = new List<string>();
            foreach (SecurityAsset x in m.Assets.Where(x => x != null)) { if (x.DependencyIds == null) x.DependencyIds = new List<string>(); if (x.Labels == null) x.Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); if (x.EvidenceNodeIds == null) x.EvidenceNodeIds = new List<string>(); }
            foreach (SecurityIdentity x in m.Identities.Where(x => x != null)) { if (x.GroupIds == null) x.GroupIds = new List<string>(); if (x.FactorKinds == null) x.FactorKinds = new List<string>(); if (x.Attributes == null) x.Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); if (x.EvidenceNodeIds == null) x.EvidenceNodeIds = new List<string>(); }
            foreach (SecurityControl x in m.Controls.Where(x => x != null)) { if (x.DependsOnControlIds == null) x.DependsOnControlIds = new List<string>(); if (x.EvidenceNodeIds == null) x.EvidenceNodeIds = new List<string>(); }
            foreach (SecurityAttackStep x in m.AttackSteps.Where(x => x != null)) { if (x.MitigatingControlIds == null) x.MitigatingControlIds = new List<string>(); if (x.EvidenceNodeIds == null) x.EvidenceNodeIds = new List<string>(); }
        }

        public static string Fingerprint(SecurityModel m)
        {
            if (m == null) return HashText("NULL_SECURITY_MODEL"); Normalize(m); StringBuilder b = new StringBuilder();
            b.Append(m.Id).Append('|').Append(m.ParentModelId).Append('|').Append(m.Name).Append('|').Append(m.Revision).Append('|').Append(m.Seed).Append('|').Append(m.SourceKind).Append('|').Append(m.SourceId).Append('|').Append(m.SourceFingerprint);
            foreach (KeyValuePair<string, double> p in m.Parameters.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) b.Append("|P:").Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture));
            foreach (SecurityZone x in m.Zones.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|Z:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.ParentId).Append(':').Append(x.Classification).Append(':').Append(x.Assurance.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SecurityAsset x in m.Assets.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|A:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.ZoneId).Append(':').Append(x.OwnerIdentityId).Append(':').Append(x.Confidentiality.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Integrity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Availability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.BusinessValue.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Exposure.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.CrownJewel).Append(':').Append(String.Join(",", x.DependencyIds.OrderBy(y => y).ToArray()));
            foreach (SecurityIdentity x in m.Identities.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|I:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.OwnerIdentityId).Append(':').Append(x.Assurance.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Privileged).Append(':').Append(x.BreakGlass).Append(':').Append(x.Status).Append(':').Append(String.Join(",", x.GroupIds.OrderBy(y => y).ToArray()));
            foreach (SecuritySecretReference x in m.SecretReferences.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|S:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.CustodianIdentityId).Append(':').Append(x.BoundAssetId).Append(':').Append(x.ReferenceLocator).Append(':').Append(x.AgeDays.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SecurityPermission x in m.Permissions.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|R:").Append(x.Id).Append(':').Append(x.SubjectId).Append(':').Append(x.ResourceId).Append(':').Append(x.Action).Append(':').Append(x.Effect).Append(':').Append(x.Condition).Append(':').Append(x.ExpiresUtc).Append(':').Append(x.Status);
            foreach (SecurityTrustRelation x in m.TrustRelations.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|T:").Append(x.Id).Append(':').Append(x.FromId).Append(':').Append(x.ToId).Append(':').Append(x.Kind).Append(':').Append(x.Strength.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Transitive).Append(':').Append(x.Status);
            foreach (SecurityThreat x in m.Threats.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|H:").Append(x.Id).Append(':').Append(x.ActorClass).Append(':').Append(x.TargetAssetId).Append(':').Append(x.InitialEntityId).Append(':').Append(x.Likelihood.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ImpactMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SecurityWeakness x in m.Weaknesses.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|W:").Append(x.Id).Append(':').Append(x.AssetId).Append(':').Append(x.Kind).Append(':').Append(x.Severity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Exploitability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SecurityControl x in m.Controls.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|C:").Append(x.Id).Append(':').Append(x.Family).Append(':').Append(x.Phase).Append(':').Append(x.TargetId).Append(':').Append(x.ThreatId).Append(':').Append(x.Effectiveness.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Independence.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Coverage.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SecurityAttackStep x in m.AttackSteps.Where(x => x != null).OrderBy(x => x.Id)) b.Append("|E:").Append(x.Id).Append(':').Append(x.SourceEntityId).Append(':').Append(x.TargetEntityId).Append(':').Append(x.RequiredIdentityId).Append(':').Append(x.RequiredWeaknessId).Append(':').Append(x.BaseProbability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Effort.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status).Append(':').Append(String.Join(",", x.MitigatingControlIds.OrderBy(y => y).ToArray()));
            return HashText(b.ToString());
        }

        public static List<SecurityValidationIssue> Validate(SecurityModel m)
        {
            List<SecurityValidationIssue> issues = new List<SecurityValidationIssue>(); if (m == null) { Issue(issues, "FATAL", "MODEL_NULL", "MODEL", "A security model is required."); return issues; } Normalize(m);
            if (String.IsNullOrWhiteSpace(m.Id)) Issue(issues, "ERROR", "MODEL_ID_EMPTY", "MODEL", "Model identifier is required.");
            if (m.Assets.Count > MaxAssets) Issue(issues, "FATAL", "ASSET_BOUND", m.Id, "Asset bound exceeded."); if (m.Identities.Count > MaxIdentities) Issue(issues, "FATAL", "IDENTITY_BOUND", m.Id, "Identity bound exceeded."); if (m.Permissions.Count > MaxPermissions) Issue(issues, "FATAL", "PERMISSION_BOUND", m.Id, "Permission bound exceeded."); if (m.AttackSteps.Count > MaxSteps) Issue(issues, "FATAL", "STEP_BOUND", m.Id, "Attack-transition bound exceeded.");
            HashSet<string> all = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Action<string, string> add = delegate(string id, string kind) { if (String.IsNullOrWhiteSpace(id)) Issue(issues, "ERROR", "EMPTY_ID", kind, "Every modeled object requires a stable identifier."); else if (!all.Add(id)) Issue(issues, "ERROR", "DUPLICATE_ID", id, "Identifier appears more than once in the model."); };
            foreach (SecurityZone x in m.Zones.Where(x => x != null)) add(x.Id, "ZONE"); foreach (SecurityAsset x in m.Assets.Where(x => x != null)) add(x.Id, "ASSET"); foreach (SecurityIdentity x in m.Identities.Where(x => x != null)) add(x.Id, "IDENTITY"); foreach (SecuritySecretReference x in m.SecretReferences.Where(x => x != null)) add(x.Id, "SECRET_REFERENCE"); foreach (SecurityPermission x in m.Permissions.Where(x => x != null)) add(x.Id, "PERMISSION"); foreach (SecurityThreat x in m.Threats.Where(x => x != null)) add(x.Id, "THREAT"); foreach (SecurityWeakness x in m.Weaknesses.Where(x => x != null)) add(x.Id, "WEAKNESS"); foreach (SecurityControl x in m.Controls.Where(x => x != null)) add(x.Id, "CONTROL"); foreach (SecurityAttackStep x in m.AttackSteps.Where(x => x != null)) add(x.Id, "ATTACK_STEP");
            HashSet<string> zones = Set(m.Zones.Select(x => x == null ? null : x.Id)); HashSet<string> assets = Set(m.Assets.Select(x => x == null ? null : x.Id)); HashSet<string> identities = Set(m.Identities.Select(x => x == null ? null : x.Id)); HashSet<string> entities = new HashSet<string>(assets, StringComparer.OrdinalIgnoreCase); entities.UnionWith(identities); HashSet<string> weaknesses = Set(m.Weaknesses.Select(x => x == null ? null : x.Id)); HashSet<string> controls = Set(m.Controls.Select(x => x == null ? null : x.Id));
            foreach (SecurityAsset x in m.Assets.Where(x => x != null)) { if (!zones.Contains(x.ZoneId)) Issue(issues, "ERROR", "ASSET_ZONE_MISSING", x.Id, "Asset zone does not exist."); foreach (string d in x.DependencyIds) if (!assets.Contains(d)) Issue(issues, "ERROR", "DEPENDENCY_MISSING", x.Id, "Dependency " + d + " does not exist."); }
            foreach (SecurityPermission x in m.Permissions.Where(x => x != null)) { if (!identities.Contains(x.SubjectId) && !Eq(x.SubjectId, "*")) Issue(issues, "ERROR", "PERMISSION_SUBJECT_MISSING", x.Id, "Permission subject does not exist."); if (!assets.Contains(x.ResourceId) && !Eq(x.ResourceId, "*")) Issue(issues, "ERROR", "PERMISSION_RESOURCE_MISSING", x.Id, "Permission resource does not exist."); if (!Eq(x.Effect, "ALLOW") && !Eq(x.Effect, "DENY")) Issue(issues, "ERROR", "PERMISSION_EFFECT_INVALID", x.Id, "Effect must be ALLOW or DENY."); }
            foreach (SecuritySecretReference x in m.SecretReferences.Where(x => x != null)) { if (!assets.Contains(x.BoundAssetId)) Issue(issues, "ERROR", "SECRET_ASSET_MISSING", x.Id, "Reference target does not exist."); if ((x.ReferenceLocator ?? "").Length > 512) Issue(issues, "ERROR", "REFERENCE_LOCATOR_BOUND", x.Id, "Reference locator exceeds metadata bound."); if (LooksLikeMaterial(x.ReferenceLocator)) Issue(issues, "FATAL", "SECRET_MATERIAL_REJECTED", x.Id, "The Fortress accepts opaque locators only, never credential or secret material."); }
            foreach (SecurityAttackStep x in m.AttackSteps.Where(x => x != null)) { if (!entities.Contains(x.SourceEntityId)) Issue(issues, "ERROR", "STEP_SOURCE_MISSING", x.Id, "Abstract transition source does not exist."); if (!entities.Contains(x.TargetEntityId)) Issue(issues, "ERROR", "STEP_TARGET_MISSING", x.Id, "Abstract transition target does not exist."); if (!String.IsNullOrWhiteSpace(x.RequiredWeaknessId) && !weaknesses.Contains(x.RequiredWeaknessId)) Issue(issues, "ERROR", "STEP_WEAKNESS_MISSING", x.Id, "Required weakness does not exist."); foreach (string c in x.MitigatingControlIds) if (!controls.Contains(c)) Issue(issues, "ERROR", "STEP_CONTROL_MISSING", x.Id, "Mitigating control " + c + " does not exist."); }
            foreach (SecurityIdentity x in m.Identities.Where(x => x != null && x.Privileged && Eq(x.Status, "ACTIVE"))) if (x.FactorKinds.Count < 2 && !x.BreakGlass) Issue(issues, "HIGH", "PRIVILEGED_FACTOR_GAP", x.Id, "Privileged identity lacks two independent modeled factors.");
            foreach (SecurityAsset x in m.Assets.Where(x => x != null && x.CrownJewel)) if (!m.Controls.Any(c => c != null && Active(c.Status) && (Eq(c.TargetId, x.Id) || Eq(c.TargetId, "*")) && Eq(c.Phase, "DETECT"))) Issue(issues, "HIGH", "CROWN_DETECTION_GAP", x.Id, "Crown jewel has no active modeled detection control.");
            return issues;
        }

        public static SecurityAccessDecision DecideAccess(SecurityModel m, string subjectId, string resourceId, string action)
        {
            Normalize(m); SecurityAccessDecision d = new SecurityAccessDecision { SubjectId = subjectId, ResourceId = resourceId, Action = action, Decision = "DENY", Reason = "Default deny." };
            SecurityIdentity identity = m.Identities.FirstOrDefault(x => x != null && Eq(x.Id, subjectId)); SecurityAsset asset = m.Assets.FirstOrDefault(x => x != null && Eq(x.Id, resourceId));
            if (identity == null || !Active(identity.Status)) { d.Reason = "Unknown or inactive identity."; return Certify(d); } if (asset == null || !Active(asset.Status)) { d.Reason = "Unknown or inactive resource."; return Certify(d); }
            HashSet<string> subjects = EffectiveSubjects(m, identity); d.EffectiveSubjectIds.AddRange(subjects.OrderBy(x => x)); DateTime now = DateTime.UtcNow;
            List<SecurityPermission> match = m.Permissions.Where(p => p != null && Active(p.Status) && subjects.Contains(p.SubjectId) && (Eq(p.ResourceId, resourceId) || Eq(p.ResourceId, "*")) && (Eq(p.Action, action) || Eq(p.Action, "*")) && !Expired(p.ExpiresUtc, now)).OrderBy(p => p.Id).ToList();
            d.MatchingPermissionIds.AddRange(match.Select(x => x.Id)); if (match.Any(x => Eq(x.Effect, "DENY"))) { d.Decision = "DENY"; d.DenyOverride = true; d.Reason = "Explicit deny overrides all allows."; }
            else if (match.Any(x => Eq(x.Effect, "ALLOW"))) { d.Decision = "ALLOW"; d.Reason = match.Any(x => x.JustInTime) ? "Active just-in-time grant." : "Active explicit grant."; }
            else d.Reason = "No active matching grant; zero-trust default deny.";
            return Certify(d);
        }

        public static SecurityEntitlementAssessment AssessEntitlements(SecurityModel m)
        {
            Normalize(m); SecurityEntitlementAssessment r = new SecurityEntitlementAssessment { ModelId = m.Id };
            r.ActiveIdentities = m.Identities.Count(x => x != null && Active(x.Status)); r.ActivePermissions = m.Permissions.Count(x => x != null && Active(x.Status)); r.PrivilegedIdentities = m.Identities.Count(x => x != null && Active(x.Status) && x.Privileged); r.DenyRules = m.Permissions.Count(x => x != null && Active(x.Status) && Eq(x.Effect, "DENY")); DateTime now = DateTime.UtcNow;
            foreach (SecurityPermission p in m.Permissions.Where(x => x != null && Active(x.Status))) { if (Expired(p.ExpiresUtc, now)) Finding(r, "HIGH", "EXPIRED_GRANT", p.SubjectId, p.ResourceId, p.Id, "Permission remains active after its declared expiry."); else if (!String.IsNullOrWhiteSpace(p.ExpiresUtc)) r.ExpiringPermissions++; if (Eq(p.Effect, "ALLOW") && (Eq(p.ResourceId, "*") || Eq(p.Action, "*"))) Finding(r, "HIGH", "WILDCARD_PRIVILEGE", p.SubjectId, p.ResourceId, p.Id, "Broad wildcard grant expands blast radius."); SecurityIdentity i = m.Identities.FirstOrDefault(x => x != null && Eq(x.Id, p.SubjectId)); if (i != null && !Active(i.Status) && Eq(p.Effect, "ALLOW")) Finding(r, "CRITICAL", "INACTIVE_IDENTITY_GRANT", p.SubjectId, p.ResourceId, p.Id, "Inactive identity retains an allow grant."); }
            foreach (SecurityIdentity i in m.Identities.Where(x => x != null && Active(x.Status))) { List<SecurityPermission> p = m.Permissions.Where(x => x != null && Active(x.Status) && EffectiveSubjects(m, i).Contains(x.SubjectId) && Eq(x.Effect, "ALLOW")).ToList(); bool writes = p.Any(x => Eq(x.Action, "WRITE") || Eq(x.Action, "CHANGE")); bool approves = p.Any(x => Eq(x.Action, "APPROVE") || Eq(x.Action, "RELEASE")); if (writes && approves && !i.BreakGlass) Finding(r, "HIGH", "TOXIC_COMBINATION", i.Id, "*", "", "Identity can both change and approve/release, violating separation of duties."); if (i.Privileged && i.FactorKinds.Count < 2 && !i.BreakGlass) Finding(r, "HIGH", "ASSURANCE_GAP", i.Id, "*", "", "Privileged identity lacks two independent factors."); }
            foreach (SecuritySecretReference s in m.SecretReferences.Where(x => x != null && Active(x.Status))) { if (s.RotationTargetDays > 0 && s.AgeDays > s.RotationTargetDays) Finding(r, "HIGH", "ROTATION_OVERDUE", s.CustodianIdentityId, s.BoundAssetId, s.Id, "Opaque secret reference exceeds its rotation target."); if (s.Exposure > 0.5) Finding(r, "HIGH", "REFERENCE_EXPOSURE", s.CustodianIdentityId, s.BoundAssetId, s.Id, "Reference metadata indicates elevated exposure."); }
            r.Verdict = r.Findings.Any(x => Eq(x.Severity, "CRITICAL")) ? "CRITICAL ENTITLEMENT RISK" : r.Findings.Any(x => Eq(x.Severity, "HIGH")) ? "REMEDIATION REQUIRED" : r.Findings.Count == 0 ? "LEAST-PRIVILEGE BASELINE SATISFIED" : "REVIEW REQUIRED";
            r.CertificateHash = HashText(Fingerprint(m) + "|ENTITLEMENTS|" + String.Join("|", r.Findings.Select(x => x.Kind + ":" + x.SubjectId + ":" + x.ResourceId).ToArray())); return r;
        }

        public static SecurityAttackGraphResult BuildAttackGraph(SecurityModel m, SecurityScenario scenario)
        {
            Normalize(m); SecurityAttackGraphResult r = new SecurityAttackGraphResult { ModelId = m.Id, ScenarioId = scenario == null ? "BASE" : scenario.Id };
            HashSet<string> disabled = Tokens(scenario == null ? "" : scenario.DisabledControlIds); HashSet<string> allowedThreats = Tokens(scenario == null ? "" : scenario.ThreatIds); int cap = Math.Min(MaxGraphStates, Math.Max(16, scenario == null ? 4096 : scenario.MaxStates)); int depthCap = Math.Min(MaxDepth, Math.Max(1, scenario == null ? 16 : scenario.MaxDepth)); double scenarioMultiplier = scenario == null ? 1 : Math.Max(0, scenario.LikelihoodMultiplier);
            Dictionary<string, SecurityControl> controlMap = m.Controls.Where(x => x != null).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase); Dictionary<string, SecurityWeakness> weaknessMap = m.Weaknesses.Where(x => x != null).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            foreach (SecurityAttackStep s in m.AttackSteps.Where(x => x != null && Active(x.Status))) { SecurityWeakness w; if (!String.IsNullOrWhiteSpace(s.RequiredWeaknessId) && (!weaknessMap.TryGetValue(s.RequiredWeaknessId, out w) || !OpenWeakness(w.Status))) continue; SecurityAttackGraphEdge e = new SecurityAttackGraphEdge { StepId = s.Id, FromId = s.SourceEntityId, ToId = s.TargetEntityId, Cost = Math.Max(0.000001, s.Effort), Detectability = Clamp01(s.Noise), Reason = "Abstract transition; no executable procedure is represented." }; double survival = 1; foreach (string id in s.MitigatingControlIds.Distinct(StringComparer.OrdinalIgnoreCase)) { SecurityControl c; if (!controlMap.TryGetValue(id, out c) || !Active(c.Status) || disabled.Contains(id)) continue; e.ActiveControlIds.Add(id); double independentEffect = Clamp01(c.Effectiveness) * Clamp01(c.Coverage) * (0.25 + 0.75 * Clamp01(c.Independence)); survival *= 1 - independentEffect; } e.EffectiveProbability = Clamp01(s.BaseProbability * scenarioMultiplier * survival); if (e.EffectiveProbability > 0) r.Edges.Add(e); }
            HashSet<string> sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (SecurityThreat t in m.Threats.Where(x => x != null && Active(x.Status) && (allowedThreats.Count == 0 || allowedThreats.Contains(x.Id)))) if (!String.IsNullOrWhiteSpace(t.InitialEntityId)) sources.Add(t.InitialEntityId); foreach (string x in Tokens(scenario == null ? "" : scenario.InitialCompromiseIds)) sources.Add(x); if (sources.Count == 0) foreach (SecurityThreat t in m.Threats.Where(x => x != null && Active(x.Status)).Take(1)) sources.Add(t.InitialEntityId);
            HashSet<string> nodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (SecurityAttackGraphEdge e in r.Edges) { nodes.Add(e.FromId); nodes.Add(e.ToId); } r.Nodes = nodes.Count;
            foreach (SecurityAsset crown in m.Assets.Where(x => x != null && x.CrownJewel && Active(x.Status))) { SecurityAttackRoute best = null; foreach (string source in sources) { SecurityAttackRoute route = BestRoute(r.Edges, source, crown.Id, cap, depthCap); if (route != null && (best == null || route.Probability > best.Probability)) best = route; } if (best == null) r.UnreachableCrownJewels.Add(crown.Id); else { best.ExpectedImpact = best.Probability * crown.Criticality * (scenario == null ? 1 : Math.Max(0, scenario.ImpactMultiplier)); best.Verdict = best.Probability >= 0.25 ? "MATERIAL PATH" : best.Probability >= 0.05 ? "CONTAINED PATH" : "LOW-PROBABILITY PATH"; r.CrownJewelRoutes.Add(best); } }
            r.ExploredStates = Math.Min(cap, r.Nodes * Math.Max(1, sources.Count)); r.Truncated = r.Nodes * Math.Max(1, sources.Count) >= cap; r.CertificateHash = HashText(Fingerprint(m) + "|ATTACK_GRAPH|" + r.ScenarioId + "|" + String.Join("|", r.Edges.Select(x => x.StepId + ":" + x.EffectiveProbability.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static SecurityBlastRadiusResult BlastRadius(SecurityModel m, string seedId, SecurityScenario scenario)
        {
            SecurityAttackGraphResult graph = BuildAttackGraph(m, scenario); SecurityBlastRadiusResult r = new SecurityBlastRadiusResult { SeedId = seedId }; int cap = Math.Min(MaxGraphStates, Math.Max(32, scenario == null ? 4096 : scenario.MaxStates)); int depthCap = Math.Min(MaxDepth, Math.Max(1, scenario == null ? 12 : scenario.MaxDepth)); Dictionary<string, SecurityBlastRadiusItem> reached = new Dictionary<string, SecurityBlastRadiusItem>(StringComparer.OrdinalIgnoreCase); Queue<string> q = new Queue<string>(); reached[seedId] = new SecurityBlastRadiusItem { EntityId = seedId, Depth = 0, ReachProbability = 1, Via = "SEED" }; q.Enqueue(seedId);
            while (q.Count > 0 && reached.Count < cap) { string at = q.Dequeue(); SecurityBlastRadiusItem parent = reached[at]; if (parent.Depth >= depthCap) continue; List<SecurityAttackGraphEdge> outgoing = graph.Edges.Where(x => Eq(x.FromId, at)).ToList(); SecurityAsset asset = m.Assets.FirstOrDefault(x => x != null && Eq(x.Id, at)); if (asset != null) foreach (string dependency in asset.DependencyIds) outgoing.Add(new SecurityAttackGraphEdge { StepId = "DEPENDENCY", FromId = at, ToId = dependency, EffectiveProbability = 0.7, Cost = 0.3, Reason = "Modeled operational dependency." }); foreach (SecurityTrustRelation trust in m.TrustRelations.Where(x => x != null && Active(x.Status) && Eq(x.FromId, at))) outgoing.Add(new SecurityAttackGraphEdge { StepId = "TRUST:" + trust.Id, FromId = at, ToId = trust.ToId, EffectiveProbability = Clamp01(trust.Strength * (trust.Transitive ? 0.8 : 0.5)), Cost = 1 - Clamp01(trust.Strength), Reason = "Modeled trust reachability." });
                foreach (SecurityAttackGraphEdge e in outgoing) { if (String.IsNullOrWhiteSpace(e.ToId)) continue; double p = parent.ReachProbability * Clamp01(e.EffectiveProbability); SecurityBlastRadiusItem prior; if (reached.TryGetValue(e.ToId, out prior) && prior.ReachProbability >= p) continue; SecurityAsset target = m.Assets.FirstOrDefault(x => x != null && Eq(x.Id, e.ToId)); reached[e.ToId] = new SecurityBlastRadiusItem { EntityId = e.ToId, Depth = parent.Depth + 1, ReachProbability = p, Criticality = target == null ? 0.2 : target.Criticality, Via = e.StepId, ParentId = at }; q.Enqueue(e.ToId); }
            }
            r.Items = reached.Values.OrderBy(x => x.Depth).ThenByDescending(x => x.ReachProbability).ToList(); r.MaxDepth = r.Items.Count == 0 ? 0 : r.Items.Max(x => x.Depth); HashSet<string> crowns = Set(m.Assets.Where(x => x != null && x.CrownJewel).Select(x => x.Id)); r.CrownJewelsReached = r.Items.Count(x => crowns.Contains(x.EntityId) && x.Depth > 0); r.ExpectedLoss = r.Items.Where(x => x.Depth > 0).Sum(x => x.ReachProbability * x.Criticality); r.CertificateHash = HashText(Fingerprint(m) + "|BLAST|" + seedId + "|" + String.Join("|", r.Items.Select(x => x.EntityId + ":" + x.ReachProbability.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static SecurityCoverageResult AssessCoverage(SecurityModel m)
        {
            Normalize(m); SecurityCoverageResult r = new SecurityCoverageResult { ModelId = m.Id };
            foreach (SecurityThreat t in m.Threats.Where(x => x != null && Active(x.Status))) AddCoverageRow(m, r, t.Id, "THREAT"); foreach (SecurityAsset a in m.Assets.Where(x => x != null && x.CrownJewel && Active(x.Status))) AddCoverageRow(m, r, a.Id, "CROWN_JEWEL");
            r.CompleteChains = r.Rows.Count(x => x.Prevent > 0 && x.Detect > 0 && x.Respond > 0 && x.Recover > 0); r.SingleControlFailures = r.Rows.Count(x => x.ControlIds.Count == 1 || x.Independence < 0.35); r.UncoveredCrownJewels = r.Rows.Count(x => Eq(x.SubjectKind, "CROWN_JEWEL") && (x.Prevent == 0 || x.Detect == 0)); r.MeanEffectiveness = r.Rows.Count == 0 ? 0 : r.Rows.Average(x => x.CombinedEffectiveness); r.Verdict = r.UncoveredCrownJewels > 0 ? "CROWN JEWEL COVERAGE GAP" : r.SingleControlFailures > 0 ? "DEFENSE-IN-DEPTH REINFORCEMENT REQUIRED" : r.CompleteChains == r.Rows.Count ? "LAYERED COVERAGE ESTABLISHED" : "LIFECYCLE COVERAGE GAP"; r.CertificateHash = HashText(Fingerprint(m) + "|COVERAGE|" + String.Join("|", r.Rows.Select(x => x.SubjectId + ":" + x.Gap + ":" + x.CombinedEffectiveness.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static SecurityRiskRegister AssessRisk(SecurityModel m, SecurityScenario scenario)
        {
            Normalize(m); SecurityRiskRegister r = new SecurityRiskRegister { ModelId = m.Id, ScenarioId = scenario == null ? "BASE" : scenario.Id }; double lm = scenario == null ? 1 : Math.Max(0, scenario.LikelihoodMultiplier), im = scenario == null ? 1 : Math.Max(0, scenario.ImpactMultiplier); HashSet<string> threatFilter = Tokens(scenario == null ? "" : scenario.ThreatIds); HashSet<string> disabled = Tokens(scenario == null ? "" : scenario.DisabledControlIds);
            foreach (SecurityThreat t in m.Threats.Where(x => x != null && Active(x.Status) && (threatFilter.Count == 0 || threatFilter.Contains(x.Id)))) { List<SecurityAsset> targets = String.IsNullOrWhiteSpace(t.TargetAssetId) || Eq(t.TargetAssetId, "*") ? m.Assets.Where(x => x != null && x.CrownJewel).ToList() : m.Assets.Where(x => x != null && Eq(x.Id, t.TargetAssetId)).ToList(); foreach (SecurityAsset a in targets) { SecurityRiskRow row = new SecurityRiskRow { ThreatId = t.Id, AssetId = a.Id, Likelihood = Clamp01(t.Likelihood * (0.4 + 0.6 * Clamp01(t.Capability)) * (0.5 + 0.5 * Clamp01(t.Intent)) * lm), Impact = Clamp01(a.Criticality * t.ImpactMultiplier * im), Uncertainty = Clamp01(0.15 + 0.25 * (1 - Math.Min(Clamp01(t.Capability), Clamp01(t.Intent)))) }; row.InherentRisk = row.Likelihood * row.Impact; List<SecurityControl> controls = RelevantControls(m, t.Id, a.Id).Where(x => !disabled.Contains(x.Id)).ToList(); row.ControlIds.AddRange(controls.Select(x => x.Id)); double survival = 1; foreach (SecurityControl c in controls) survival *= 1 - Clamp01(c.Effectiveness) * Clamp01(c.Coverage) * (0.25 + 0.75 * Clamp01(c.Independence)); row.ResidualRisk = row.InherentRisk * Clamp01(survival); row.Band = row.ResidualRisk >= 0.35 ? "CRITICAL" : row.ResidualRisk >= 0.18 ? "HIGH" : row.ResidualRisk >= 0.07 ? "MODERATE" : "LOW"; r.Rows.Add(row); } }
            r.Rows = r.Rows.OrderByDescending(x => x.ResidualRisk).ToList(); r.TotalInherentRisk = r.Rows.Sum(x => x.InherentRisk); r.TotalResidualRisk = r.Rows.Sum(x => x.ResidualRisk); r.TailRisk = r.Rows.Count == 0 ? 0 : r.Rows.OrderByDescending(x => x.ResidualRisk).Take(Math.Max(1, (int)Math.Ceiling(r.Rows.Count * 0.1))).Average(x => x.ResidualRisk); r.Verdict = r.Rows.Any(x => Eq(x.Band, "CRITICAL")) ? "CRITICAL RESIDUAL RISK" : r.Rows.Any(x => Eq(x.Band, "HIGH")) ? "HIGH RESIDUAL RISK" : r.Rows.Count == 0 ? "NO MODELED RISK" : "RISK WITHIN MODELED TOLERANCE"; r.CertificateHash = HashText(Fingerprint(m) + "|RISK|" + r.ScenarioId + "|" + String.Join("|", r.Rows.Select(x => x.ThreatId + ":" + x.AssetId + ":" + x.ResidualRisk.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static SecuritySegmentationCut FindSegmentationCut(SecurityModel m, string sourceId, string targetId)
        {
            Normalize(m); List<FlowEdge> edges = new List<FlowEdge>(); foreach (SecurityAttackStep s in m.AttackSteps.Where(x => x != null && Active(x.Status))) edges.Add(new FlowEdge { From = s.SourceEntityId, To = s.TargetEntityId, Capacity = Math.Max(0.001, Clamp01(s.BaseProbability)), Residual = Math.Max(0.001, Clamp01(s.BaseProbability)), Boundary = s.Id }); foreach (SecurityTrustRelation t in m.TrustRelations.Where(x => x != null && Active(x.Status))) edges.Add(new FlowEdge { From = t.FromId, To = t.ToId, Capacity = Math.Max(0.001, Clamp01(t.Strength)), Residual = Math.Max(0.001, Clamp01(t.Strength)), Boundary = t.Id });
            double flow = 0; int guard = 0; while (guard++ < MaxGraphStates) { Dictionary<string, FlowEdge> parent = new Dictionary<string, FlowEdge>(StringComparer.OrdinalIgnoreCase); Queue<string> q = new Queue<string>(); HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourceId }; q.Enqueue(sourceId); while (q.Count > 0 && !seen.Contains(targetId)) { string at = q.Dequeue(); foreach (FlowEdge e in edges.Where(x => Eq(x.From, at) && x.Residual > 1e-9)) if (seen.Add(e.To)) { parent[e.To] = e; q.Enqueue(e.To); } } if (!seen.Contains(targetId)) break; double add = Double.MaxValue; string p = targetId; while (!Eq(p, sourceId)) { FlowEdge e = parent[p]; add = Math.Min(add, e.Residual); p = e.From; } p = targetId; while (!Eq(p, sourceId)) { FlowEdge e = parent[p]; e.Residual -= add; FlowEdge reverse = edges.FirstOrDefault(x => Eq(x.From, e.To) && Eq(x.To, e.From) && Eq(x.Boundary, "REV:" + e.Boundary)); if (reverse == null) { reverse = new FlowEdge { From = e.To, To = e.From, Capacity = 0, Residual = 0, Boundary = "REV:" + e.Boundary }; edges.Add(reverse); } reverse.Residual += add; p = e.From; } flow += add; }
            HashSet<string> reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourceId }; Queue<string> walk = new Queue<string>(); walk.Enqueue(sourceId); while (walk.Count > 0) { string at = walk.Dequeue(); foreach (FlowEdge e in edges.Where(x => Eq(x.From, at) && x.Residual > 1e-9)) if (reachable.Add(e.To)) walk.Enqueue(e.To); }
            SecuritySegmentationCut r = new SecuritySegmentationCut { SourceId = sourceId, TargetId = targetId, CutCapacity = flow }; foreach (FlowEdge e in edges.Where(x => x.Capacity > 0 && reachable.Contains(x.From) && !reachable.Contains(x.To) && !x.Boundary.StartsWith("REV:", StringComparison.OrdinalIgnoreCase))) r.BoundaryIds.Add(e.Boundary); foreach (string b in r.BoundaryIds) r.RecommendedControlIds.Add("PLAN_SEGMENT_OR_GATE:" + b); r.Verdict = r.BoundaryIds.Count == 0 ? "NO REACHABLE PATH OR EMPTY CUT" : r.BoundaryIds.Count == 1 ? "SINGLE CRITICAL BOUNDARY" : "DEFENSIBLE MULTI-BOUNDARY CUT"; r.CertificateHash = HashText(Fingerprint(m) + "|MINCUT|" + sourceId + "|" + targetId + "|" + String.Join(",", r.BoundaryIds.OrderBy(x => x).ToArray())); return r;
        }

        public static SecurityIncidentPlan PlanIncident(SecurityModel m, SecurityScenario scenario)
        {
            Normalize(m); SecurityAttackGraphResult graph = BuildAttackGraph(m, scenario); SecurityRiskRegister risks = AssessRisk(m, scenario); SecurityIncidentPlan p = new SecurityIncidentPlan { Id = "incident-" + HashText(Fingerprint(m) + "|" + (scenario == null ? "BASE" : scenario.Id)).Substring(0, 16), ModelId = m.Id, ScenarioId = scenario == null ? "BASE" : scenario.Id, Name = (scenario == null ? "Base adversarial world" : scenario.Name) + " incident exercise", Severity = risks.Rows.Count == 0 ? "LOW" : risks.Rows[0].Band };
            SecurityAttackRoute route = graph.CrownJewelRoutes.OrderByDescending(x => x.ExpectedImpact).FirstOrDefault(); if (route != null) p.AffectedEntityIds.AddRange(route.EntityIds); else foreach (SecurityThreat t in m.Threats.Take(1)) p.AffectedEntityIds.Add(t.TargetAssetId);
            List<SecurityControl> detects = m.Controls.Where(x => x != null && Active(x.Status) && Eq(x.Phase, "DETECT")).OrderBy(x => x.DetectionLatencyMinutes).ToList(); List<SecurityControl> responds = m.Controls.Where(x => x != null && Active(x.Status) && Eq(x.Phase, "RESPOND")).OrderBy(x => x.ResponseLatencyMinutes).ToList(); p.MttdMinutes = detects.Count == 0 ? 240 : Math.Max(0.1, detects.Take(3).Average(x => Math.Max(0.1, x.DetectionLatencyMinutes)) / Math.Max(0.25, detects.Take(3).Average(x => Clamp01(x.Effectiveness)))); p.MttcMinutes = p.MttdMinutes + (responds.Count == 0 ? 180 : Math.Max(1, responds.Take(3).Average(x => Math.Max(1, x.ResponseLatencyMinutes)))); p.MttrMinutes = p.MttcMinutes + Math.Max(30, Param(m, "recovery_target_minutes", 240));
            AddTimeline(p, 1, 0, "PREPARATION", m.Id, "Exercise world instantiated from immutable model fingerprint.", "Scenario and bounds frozen.", "PRESERVED"); AddTimeline(p, 2, p.MttdMinutes, "DETECTION", First(p.AffectedEntityIds), "Modeled signal crosses detection threshold.", detects.Count == 0 ? "No active detection; conservative fallback." : "Earliest effective detection layer.", "CAPTURED"); AddTimeline(p, 3, p.MttdMinutes + 1, "TRIAGE", First(p.AffectedEntityIds), "Severity and scope classified from attack graph and risk register.", risks.Verdict, "SEALED");
            p.Actions = PlanContainment(m, p.AffectedEntityIds, risks).ToList(); double at = p.MttdMinutes + 2; int seq = 4; foreach (SecurityContainmentAction a in p.Actions.Take(12)) { AddTimeline(p, seq++, at, "CONTAINMENT", a.TargetId, a.Kind + " proposed; no external execution authority.", a.ExpectedEffect, a.EvidencePreserving ? "PRESERVED" : "REVIEW"); at += 2 + a.ServiceCost * 10; } AddTimeline(p, seq++, p.MttcMinutes, "CONTAINMENT", m.Id, "Modeled propagation bounded.", "Containment plan reaches estimated control point.", "SEALED"); AddTimeline(p, seq++, p.MttrMinutes - 10, "RECOVERY", m.Id, "Known-good recovery state and dependency order evaluated.", "Recovery target and crown-jewel priorities.", "VERIFIED_REFERENCE"); AddTimeline(p, seq, p.MttrMinutes, "LESSONS", m.Id, "Exercise dossier ready for control and policy improvement.", "Evidence, assumptions, limitations and residual risk retained.", "SEALED"); p.Assumptions.Add("Simulation-only plan: every action requires independent human authorization and an external approved runbook."); p.Assumptions.Add("No real credential, secret, endpoint, exploit, command or network operation exists in this model."); p.CertificateHash = HashText(Fingerprint(m) + "|INCIDENT|" + p.ScenarioId + "|" + String.Join("|", p.Timeline.Select(x => x.ChainHash).ToArray())); return p;
        }

        public static IList<SecurityContainmentAction> PlanContainment(SecurityModel m, IEnumerable<string> affectedIds, SecurityRiskRegister risks)
        {
            Normalize(m); HashSet<string> affected = Set(affectedIds); List<SecurityContainmentAction> rows = new List<SecurityContainmentAction>(); int priority = 1;
            foreach (string id in affected) { SecurityIdentity identity = m.Identities.FirstOrDefault(x => x != null && Eq(x.Id, id)); SecurityAsset asset = m.Assets.FirstOrDefault(x => x != null && Eq(x.Id, id)); if (identity != null) { rows.Add(Action(priority++, "PLAN_SESSION_REVOCATION", id, "Bound active authorization paths in the modeled world.", identity.Privileged ? 0.2 : 0.08, identity.Privileged ? 0.7 : 0.45, true)); foreach (SecuritySecretReference s in m.SecretReferences.Where(x => x != null && Eq(x.CustodianIdentityId, id))) rows.Add(Action(priority++, "PLAN_REFERENCE_ROTATION", s.Id, "Invalidate dependent secret reference after evidence capture.", 0.12, 0.4, true)); } if (asset != null) { rows.Add(Action(priority++, "PLAN_SEGMENT_ISOLATION", id, "Bound modeled trust, interface and transition edges.", asset.Availability * 0.45, 0.65, true)); rows.Add(Action(priority++, "PRESERVE_EVIDENCE", id, "Retain event, decision and provenance state before eradication.", 0.03, 0.1, true)); } }
            foreach (SecurityPermission p in m.Permissions.Where(x => x != null && affected.Contains(x.SubjectId) && Eq(x.Effect, "ALLOW"))) rows.Add(Action(priority++, "PLAN_GRANT_SUSPENSION", p.Id, "Remove one modeled entitlement edge pending review.", 0.06, 0.25, true)); foreach (SecurityAsset crown in m.Assets.Where(x => x != null && x.CrownJewel && !affected.Contains(x.Id))) rows.Add(Action(priority++, "VERIFY_CROWN_INTEGRITY", crown.Id, "Confirm modeled integrity, provenance and recovery readiness.", 0.02, 0.2, true));
            rows = rows.OrderByDescending(x => (x.RiskReduction + 0.01) / (x.ServiceCost + 0.05)).ThenBy(x => x.Priority).Take(128).ToList(); for (int i = 0; i < rows.Count; i++) rows[i].Priority = i + 1; return rows;
        }

        public static SecurityCampaignResult RunCampaign(SecurityModel m)
        {
            Normalize(m); SecurityCampaignResult r = new SecurityCampaignResult { ModelId = m.Id }; List<SecurityScenario> scenarios = m.Scenarios.Where(x => x != null && Active(x.Status)).ToList(); if (scenarios.Count == 0) scenarios.Add(new SecurityScenario { Id = "BASE", Name = "Base modeled world", Weight = 1 }); double totalWeight = scenarios.Sum(x => Math.Max(0, x.Weight)); if (totalWeight <= 0) totalWeight = scenarios.Count;
            foreach (SecurityScenario s in scenarios.Take(256)) { SecurityRiskRegister risks = AssessRisk(m, s); SecurityAttackGraphResult graph = BuildAttackGraph(m, s); SecurityIncidentPlan incident = PlanIncident(m, s); double residual = risks.TotalResidualRisk; SecurityCampaignWorld w = new SecurityCampaignWorld { ScenarioId = s.Id, Weight = Math.Max(0, s.Weight) / totalWeight, ResidualRisk = residual, ReachableCrownJewels = graph.CrownJewelRoutes.Count, ExpectedLoss = graph.CrownJewelRoutes.Sum(x => x.ExpectedImpact), MttdMinutes = incident.MttdMinutes, MttcMinutes = incident.MttcMinutes, Verdict = residual >= 0.5 || graph.CrownJewelRoutes.Any(x => x.Probability >= 0.4) ? "BREACHED OBJECTIVE" : residual >= 0.2 ? "STRESSED" : "RESILIENT" }; r.Worlds.Add(w); }
            r.WeightedResidualRisk = r.Worlds.Sum(x => x.Weight * x.ResidualRisk); r.WorstResidualRisk = r.Worlds.Count == 0 ? 0 : r.Worlds.Max(x => x.ResidualRisk); double reachPenalty = r.Worlds.Count == 0 ? 0 : r.Worlds.Sum(x => x.Weight * Math.Min(1, x.ReachableCrownJewels / 3.0)); r.ResilienceScore = Clamp01(1 - Math.Min(1, r.WeightedResidualRisk) * 0.65 - reachPenalty * 0.35); r.Verdict = r.ResilienceScore >= 0.8 ? "FORTRESS RESILIENT" : r.ResilienceScore >= 0.55 ? "FORTRESS STRESSED" : "FORTRESS REINFORCEMENT REQUIRED"; r.CertificateHash = HashText(Fingerprint(m) + "|CAMPAIGN|" + String.Join("|", r.Worlds.Select(x => x.ScenarioId + ":" + x.ResidualRisk.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static SecurityAssuranceCase BuildAssuranceCase(SecurityModel m)
        {
            Normalize(m); SecurityAssuranceCase r = new SecurityAssuranceCase { ModelId = m.Id }; List<SecurityValidationIssue> validation = Validate(m); SecurityEntitlementAssessment entitlement = AssessEntitlements(m); SecurityCoverageResult coverage = AssessCoverage(m); SecurityCampaignResult campaign = RunCampaignShallow(m);
            Claim(r, "claim-model", "GOAL", "The defensive model is bounded, internally referential and free of secret material.", validation.Any(x => Eq(x.Severity, "FATAL") || Eq(x.Severity, "ERROR")) ? "VIOLATED" : "SATISFIED", "Validation certificate", validation.Select(x => x.Code)); Claim(r, "claim-access", "GOAL", "Access policy is default-deny with explicit deny override and reviewed privilege.", entitlement.Findings.Any(x => Eq(x.Severity, "CRITICAL")) ? "VIOLATED" : entitlement.Findings.Any(x => Eq(x.Severity, "HIGH")) ? "UNRESOLVED" : "SATISFIED", entitlement.Verdict, entitlement.Findings.Select(x => x.Kind)); Claim(r, "claim-defense", "GOAL", "Crown jewels have independent prevent, detect, respond and recover layers.", coverage.UncoveredCrownJewels > 0 ? "VIOLATED" : coverage.SingleControlFailures > 0 ? "UNRESOLVED" : "SATISFIED", coverage.Verdict, coverage.Rows.Where(x => !String.IsNullOrWhiteSpace(x.Gap)).Select(x => x.SubjectId)); Claim(r, "claim-resilience", "GOAL", "Bounded adversarial worlds remain within declared residual-risk tolerance.", campaign.ResilienceScore >= Param(m, "minimum_resilience", 0.65) ? "SATISFIED" : "UNRESOLVED", campaign.Verdict, campaign.Worlds.Where(x => !Eq(x.Verdict, "RESILIENT")).Select(x => x.ScenarioId));
            foreach (SecurityInvariant i in m.Invariants.Where(x => x != null && Active(x.Status))) { string status = EvaluateInvariant(m, i); Claim(r, i.Id, "INVARIANT", i.Name ?? i.Kind, status, i.Kind, new string[0]); }
            r.Satisfied = r.Claims.Count(x => Eq(x.Status, "SATISFIED")); r.Violated = r.Claims.Count(x => Eq(x.Status, "VIOLATED")); r.Unresolved = r.Claims.Count - r.Satisfied - r.Violated; r.Verdict = r.Violated > 0 ? "ASSURANCE CASE DEFEATED" : r.Unresolved > 0 ? "ASSURANCE CASE OPEN" : "ASSURANCE CASE SUPPORTED"; r.CertificateHash = HashText(Fingerprint(m) + "|ASSURANCE|" + String.Join("|", r.Claims.Select(x => x.Id + ":" + x.Status).ToArray())); return r;
        }

        private static SecurityCampaignResult RunCampaignShallow(SecurityModel m)
        {
            SecurityCampaignResult r = new SecurityCampaignResult { ModelId = m.Id }; List<SecurityScenario> worlds = m.Scenarios.Where(x => x != null && Active(x.Status)).Take(64).ToList(); if (worlds.Count == 0) worlds.Add(new SecurityScenario { Id = "BASE", Weight = 1 }); double sum = worlds.Sum(x => Math.Max(0.001, x.Weight)); foreach (SecurityScenario s in worlds) { SecurityRiskRegister risk = AssessRisk(m, s); SecurityAttackGraphResult graph = BuildAttackGraph(m, s); SecurityCampaignWorld w = new SecurityCampaignWorld { ScenarioId = s.Id, Weight = Math.Max(0.001, s.Weight) / sum, ResidualRisk = risk.TotalResidualRisk, ReachableCrownJewels = graph.CrownJewelRoutes.Count, ExpectedLoss = graph.CrownJewelRoutes.Sum(x => x.ExpectedImpact), Verdict = risk.TotalResidualRisk >= 0.5 ? "BREACHED OBJECTIVE" : risk.TotalResidualRisk >= 0.2 ? "STRESSED" : "RESILIENT" }; r.Worlds.Add(w); } r.WeightedResidualRisk = r.Worlds.Sum(x => x.Weight * x.ResidualRisk); r.WorstResidualRisk = r.Worlds.Max(x => x.ResidualRisk); r.ResilienceScore = Clamp01(1 - Math.Min(1, r.WeightedResidualRisk)); r.Verdict = r.ResilienceScore >= 0.8 ? "FORTRESS RESILIENT" : r.ResilienceScore >= 0.55 ? "FORTRESS STRESSED" : "FORTRESS REINFORCEMENT REQUIRED"; return r;
        }

        private static SecurityAttackRoute BestRoute(List<SecurityAttackGraphEdge> edges, string source, string target, int cap, int depthCap)
        {
            if (Eq(source, target)) return new SecurityAttackRoute { SourceId = source, TargetId = target, Probability = 1, Cost = 0, EntityIds = new List<string> { source } }; Dictionary<string, RouteState> best = new Dictionary<string, RouteState>(StringComparer.OrdinalIgnoreCase); List<RouteState> open = new List<RouteState>(); RouteState start = new RouteState { Id = source, Cost = 0, Depth = 0 }; best[source] = start; open.Add(start); int explored = 0;
            while (open.Count > 0 && explored++ < cap) { RouteState at = open.OrderBy(x => x.Cost).ThenBy(x => x.Id).First(); open.Remove(at); if (Eq(at.Id, target)) break; if (at.Depth >= depthCap) continue; foreach (SecurityAttackGraphEdge e in edges.Where(x => Eq(x.FromId, at.Id)).OrderBy(x => x.StepId)) { double p = Math.Max(1e-12, Math.Min(1, e.EffectiveProbability)); double cost = at.Cost - Math.Log(p); RouteState prior; if (best.TryGetValue(e.ToId, out prior) && prior.Cost <= cost) continue; RouteState next = new RouteState { Id = e.ToId, Cost = cost, Depth = at.Depth + 1, Parent = at.Id, Step = e.StepId }; best[e.ToId] = next; open.Add(next); } }
            RouteState end; if (!best.TryGetValue(target, out end)) return null; SecurityAttackRoute r = new SecurityAttackRoute { SourceId = source, TargetId = target, Cost = end.Cost, Probability = Math.Exp(-end.Cost) }; string cursor = target; int guard = 0; while (!Eq(cursor, source) && guard++ <= depthCap + 1) { RouteState s = best[cursor]; r.EntityIds.Add(cursor); r.StepIds.Add(s.Step); cursor = s.Parent; } r.EntityIds.Add(source); r.EntityIds.Reverse(); r.StepIds.Reverse(); return r;
        }

        private static void AddCoverageRow(SecurityModel m, SecurityCoverageResult result, string subjectId, string kind)
        {
            List<SecurityControl> controls = RelevantControls(m, Eq(kind, "THREAT") ? subjectId : null, Eq(kind, "CROWN_JEWEL") ? subjectId : null); SecurityCoverageRow row = new SecurityCoverageRow { SubjectId = subjectId, SubjectKind = kind }; row.ControlIds.AddRange(controls.Select(x => x.Id)); row.Prevent = controls.Count(x => Eq(x.Phase, "PREVENT")); row.Detect = controls.Count(x => Eq(x.Phase, "DETECT")); row.Respond = controls.Count(x => Eq(x.Phase, "RESPOND")); row.Recover = controls.Count(x => Eq(x.Phase, "RECOVER")); double survival = 1; foreach (SecurityControl c in controls) survival *= 1 - Clamp01(c.Effectiveness) * Clamp01(c.Coverage) * (0.25 + 0.75 * Clamp01(c.Independence)); row.CombinedEffectiveness = Clamp01(1 - survival); row.Independence = controls.Count == 0 ? 0 : controls.Average(x => Clamp01(x.Independence)); List<string> gaps = new List<string>(); if (row.Prevent == 0) gaps.Add("PREVENT"); if (row.Detect == 0) gaps.Add("DETECT"); if (row.Respond == 0) gaps.Add("RESPOND"); if (row.Recover == 0) gaps.Add("RECOVER"); row.Gap = String.Join(",", gaps.ToArray()); result.Rows.Add(row);
        }

        private static List<SecurityControl> RelevantControls(SecurityModel m, string threatId, string assetId) { return m.Controls.Where(c => c != null && Active(c.Status) && (Eq(c.TargetId, "*") || Eq(c.TargetId, assetId) || Eq(c.TargetId, threatId) || Eq(c.ThreatId, threatId))).GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList(); }
        private static HashSet<string> EffectiveSubjects(SecurityModel m, SecurityIdentity identity) { HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { identity.Id, "*" }; Queue<string> q = new Queue<string>(); foreach (string g in identity.GroupIds) if (set.Add(g)) q.Enqueue(g); int guard = 0; while (q.Count > 0 && guard++ < MaxIdentities) { string id = q.Dequeue(); SecurityIdentity group = m.Identities.FirstOrDefault(x => x != null && Eq(x.Id, id)); if (group == null) continue; foreach (string parent in group.GroupIds) if (set.Add(parent)) q.Enqueue(parent); } return set; }
        private static SecurityAccessDecision Certify(SecurityAccessDecision d) { d.CertificateHash = HashText((d.SubjectId ?? "") + "|" + (d.ResourceId ?? "") + "|" + (d.Action ?? "") + "|" + (d.Decision ?? "") + "|" + String.Join(",", d.MatchingPermissionIds.OrderBy(x => x).ToArray())); return d; }
        private static void Finding(SecurityEntitlementAssessment r, string severity, string kind, string subject, string resource, string permission, string detail) { r.Findings.Add(new SecurityEntitlementFinding { Severity = severity, Kind = kind, SubjectId = subject, ResourceId = resource, PermissionId = permission, Detail = detail }); }
        private static SecurityContainmentAction Action(int priority, string kind, string target, string effect, double cost, double reduction, bool preserve) { return new SecurityContainmentAction { Priority = priority, Kind = kind, TargetId = target, ExpectedEffect = effect, ServiceCost = Clamp01(cost), RiskReduction = Clamp01(reduction), RequiresAuthorization = true, EvidencePreserving = preserve, ExecutionMode = "SIMULATION_PLAN_ONLY" }; }
        private static void AddTimeline(SecurityIncidentPlan p, int sequence, double minute, string phase, string subject, string evt, string basis, string evidence) { string prior = p.Timeline.Count == 0 ? new string('0', 64) : p.Timeline[p.Timeline.Count - 1].ChainHash; SecurityTimelineEvent x = new SecurityTimelineEvent { Sequence = sequence, Minute = minute, Phase = phase, SubjectId = subject, Event = evt, DecisionBasis = basis, EvidenceState = evidence }; x.ChainHash = HashText(prior + "|" + sequence + "|" + minute.ToString("R", CultureInfo.InvariantCulture) + "|" + phase + "|" + subject + "|" + evt + "|" + basis + "|" + evidence); p.Timeline.Add(x); }
        private static void Claim(SecurityAssuranceCase r, string id, string kind, string statement, string status, string basis, IEnumerable<string> evidence) { SecurityAssuranceClaim c = new SecurityAssuranceClaim { Id = id, Kind = kind, Statement = statement, Status = status, Basis = basis }; c.EvidenceIds.AddRange((evidence ?? new string[0]).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)); r.Claims.Add(c); }
        private static string EvaluateInvariant(SecurityModel m, SecurityInvariant i) { if (Eq(i.Kind, "DENY_OVERRIDES")) return "SATISFIED"; if (Eq(i.Kind, "NO_DISABLED_GRANTS")) return m.Permissions.Any(p => p != null && Active(p.Status) && Eq(p.Effect, "ALLOW") && m.Identities.Any(x => x != null && Eq(x.Id, p.SubjectId) && !Active(x.Status))) ? "VIOLATED" : "SATISFIED"; if (Eq(i.Kind, "SEPARATION_OF_DUTIES")) return AssessEntitlements(m).Findings.Any(x => Eq(x.Kind, "TOXIC_COMBINATION") && (String.IsNullOrWhiteSpace(i.SubjectId) || Eq(x.SubjectId, i.SubjectId))) ? "VIOLATED" : "SATISFIED"; if (Eq(i.Kind, "CROWN_DETECTION")) return AssessCoverage(m).UncoveredCrownJewels > 0 ? "VIOLATED" : "SATISFIED"; if (Eq(i.Kind, "ACCESS_DENIED")) return Eq(DecideAccess(m, i.SubjectId, i.ResourceId, i.Action).Decision, "DENY") ? "SATISFIED" : "VIOLATED"; return "UNRESOLVED"; }
        private static bool LooksLikeMaterial(string value) { value = value ?? ""; if (value.Length == 0) return false; string v = value.ToLowerInvariant(); return v.Contains("-----begin") || v.Contains("password=") || v.Contains("secret=") || v.Contains("token=") || v.Contains("private_key") || (value.Length > 80 && value.IndexOf(' ') < 0 && value.IndexOf('/') < 0 && value.IndexOf(':') < 0); }
        private static bool Expired(string value, DateTime now) { DateTime dt; return !String.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt) && dt <= now; }
        private static bool Active(string status) { return String.IsNullOrWhiteSpace(status) || Eq(status, "ACTIVE") || Eq(status, "MODELED") || Eq(status, "MODELED_ONLY") || Eq(status, "ACTIVE_REFERENCE_ONLY") || Eq(status, "OPEN"); }
        private static bool OpenWeakness(string status) { return Active(status) && !Eq(status, "MITIGATED") && !Eq(status, "ACCEPTED_CLOSED"); }
        private static string First(IEnumerable<string> values) { return (values ?? new string[0]).FirstOrDefault() ?? "MODEL"; }
        private static HashSet<string> Set(IEnumerable<string> values) { return new HashSet<string>((values ?? new string[0]).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); }
        private static HashSet<string> Tokens(string raw) { return new HashSet<string>((raw ?? "").Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0), StringComparer.OrdinalIgnoreCase); }
        private static void Issue(List<SecurityValidationIssue> rows, string severity, string code, string subject, string detail) { rows.Add(new SecurityValidationIssue { Severity = severity, Code = code, SubjectId = subject, Detail = detail }); }
        private static double Param(SecurityModel m, string key, double fallback) { double v; return m.Parameters != null && m.Parameters.TryGetValue(key, out v) && !Double.IsNaN(v) && !Double.IsInfinity(v) ? v : fallback; }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        public static double Clamp01(double value) { return Double.IsNaN(value) || Double.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(1, value)); }
        public static string ShortHash(string hash) { return String.IsNullOrWhiteSpace(hash) ? "∅" : hash.Substring(0, Math.Min(12, hash.Length)); }
        private static string HashText(string value) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

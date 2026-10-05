using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Yomi.ProductShell
{
    internal sealed class DataOrigin
    {
        public string Kind { get; set; }
        public string Locator { get; set; }
        public string DisplayName { get; set; }
        public long LengthBytes { get; set; }
        public string ModifiedUtc { get; set; }
        public string CapturedUtc { get; set; }
        public string Sha256 { get; set; }
        public string ParentDatasetId { get; set; }
        public string ParentFingerprint { get; set; }
        public string RecipeId { get; set; }
        public string Notes { get; set; }
        public DataOrigin() { Kind = "LOCAL_FILE"; CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Kind ?? "SOURCE") + "   ·   " + (DisplayName ?? Locator ?? "origin") + "   ·   " + LengthBytes.ToString("N0", CultureInfo.InvariantCulture) + " bytes   ·   sha " + DataFoundryKernel.Short(Sha256); }
    }

    internal sealed class DataFieldProfile
    {
        public string Name { get; set; }
        public int Ordinal { get; set; }
        public string InferredType { get; set; }
        public string Explanation { get; set; }
        public long Observed { get; set; }
        public long NullOrBlank { get; set; }
        public long ParseFailures { get; set; }
        public long NumericCount { get; set; }
        public long DateCount { get; set; }
        public long BooleanCount { get; set; }
        public long IntegerCount { get; set; }
        public long TextCount { get; set; }
        public double MinNumber { get; set; }
        public double MaxNumber { get; set; }
        public double MeanNumber { get; set; }
        public double StdDevNumber { get; set; }
        public double MinLength { get; set; }
        public double MaxLength { get; set; }
        public double MeanLength { get; set; }
        public long ApproxDistinct { get; set; }
        public bool DistinctCapped { get; set; }
        public double NullRate { get; set; }
        public double UniqueRate { get; set; }
        public bool CandidateKey { get; set; }
        public List<string> ExampleValues { get; set; }
        public List<string> QualityFlags { get; set; }
        public DataFieldProfile() { InferredType = "UNKNOWN"; ExampleValues = new List<string>(); QualityFlags = new List<string>(); }
        public override string ToString() { return Ordinal.ToString(CultureInfo.InvariantCulture) + ". " + (Name ?? "field") + "   ·   " + InferredType + "   ·   null " + NullRate.ToString("P1", CultureInfo.InvariantCulture) + "   ·   distinct ≈" + ApproxDistinct.ToString("N0", CultureInfo.InvariantCulture) + (CandidateKey ? "   ·   KEY?" : ""); }
    }

    internal sealed class DataChunkProfile
    {
        public int ChunkIndex { get; set; }
        public long FirstRow { get; set; }
        public long LastRow { get; set; }
        public long BytesRead { get; set; }
        public long MalformedRows { get; set; }
        public long BlankRows { get; set; }
        public double RowsPerSecond { get; set; }
        public string Fingerprint { get; set; }
        public override string ToString() { return "chunk " + ChunkIndex.ToString(CultureInfo.InvariantCulture) + "   ·   rows " + FirstRow.ToString("N0", CultureInfo.InvariantCulture) + "–" + LastRow.ToString("N0", CultureInfo.InvariantCulture) + "   ·   malformed " + MalformedRows.ToString("N0", CultureInfo.InvariantCulture) + "   ·   " + RowsPerSecond.ToString("N0", CultureInfo.InvariantCulture) + " rows/s"; }
    }

    internal sealed class DataRowSample
    {
        public long RowNumber { get; set; }
        public string StableRank { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public List<string> Flags { get; set; }
        public DataRowSample() { Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); Flags = new List<string>(); }
        public override string ToString() { return "row " + RowNumber.ToString("N0", CultureInfo.InvariantCulture) + "   ·   " + String.Join("   |   ", Values.Take(5).Select(x => x.Key + "=" + DataFoundryKernel.Clip(x.Value, 54)).ToArray()); }
    }

    internal sealed class DataQualityFinding
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string FieldName { get; set; }
        public string Severity { get; set; }
        public string Kind { get; set; }
        public string Message { get; set; }
        public long AffectedEstimate { get; set; }
        public double Rate { get; set; }
        public string Evidence { get; set; }
        public DataQualityFinding() { Severity = "INFO"; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Kind ?? "QUALITY") + "   ·   " + (FieldName ?? "dataset") + "   ·   " + (Message ?? "") + (Rate > 0 ? "   ·   " + Rate.ToString("P1", CultureInfo.InvariantCulture) : ""); }
    }

    internal sealed class DataRelation
    {
        public string Id { get; set; }
        public string LeftDatasetId { get; set; }
        public string LeftField { get; set; }
        public string RightDatasetId { get; set; }
        public string RightField { get; set; }
        public string Kind { get; set; }
        public string Cardinality { get; set; }
        public double Confidence { get; set; }
        public double SampleOverlap { get; set; }
        public double NameSimilarity { get; set; }
        public string Basis { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (Kind ?? "RELATION") + "   ·   " + LeftDatasetId + "." + LeftField + " ⇄ " + RightDatasetId + "." + RightField + "   ·   " + (Cardinality ?? "?") + "   ·   confidence " + Confidence.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class DataCorrelation
    {
        public string DatasetId { get; set; }
        public string FieldA { get; set; }
        public string FieldB { get; set; }
        public int PairedSamples { get; set; }
        public double Pearson { get; set; }
        public string Strength { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return FieldA + " ⇄ " + FieldB + "   ·   r=" + Pearson.ToString("0.000", CultureInfo.InvariantCulture) + "   ·   " + (Strength ?? "") + "   ·   n=" + PairedSamples.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DataLineageNode
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string ParentNodeId { get; set; }
        public string Kind { get; set; }
        public string Operation { get; set; }
        public string Fingerprint { get; set; }
        public string CreatedUtc { get; set; }
        public string Detail { get; set; }
        public List<string> InputDatasetIds { get; set; }
        public DataLineageNode() { Kind = "SOURCE"; CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); InputDatasetIds = new List<string>(); }
        public override string ToString() { return (Kind ?? "LINEAGE") + "   ·   " + (Operation ?? "OBSERVE") + "   ·   " + (DatasetId ?? "dataset") + "   ·   " + DataFoundryKernel.Short(Fingerprint); }
    }

    internal sealed class DataTransformStep
    {
        public string Id { get; set; }
        public int Order { get; set; }
        public string Operation { get; set; }
        public string Field { get; set; }
        public string TargetField { get; set; }
        public string Argument { get; set; }
        public string Argument2 { get; set; }
        public bool Enabled { get; set; }
        public string Rationale { get; set; }
        public DataTransformStep() { Enabled = true; Operation = "TRIM"; }
        public override string ToString() { return Order.ToString(CultureInfo.InvariantCulture) + ". " + (Enabled ? "ON" : "OFF") + "   ·   " + (Operation ?? "STEP") + "   ·   " + (Field ?? "*") + (String.IsNullOrWhiteSpace(TargetField) ? "" : " → " + TargetField) + (String.IsNullOrWhiteSpace(Argument) ? "" : "   ·   " + DataFoundryKernel.Clip(Argument, 80)); }
    }

    internal sealed class DataRecipe
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SourceDatasetId { get; set; }
        public string SourceFingerprint { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string Status { get; set; }
        public List<DataTransformStep> Steps { get; set; }
        public List<string> Assumptions { get; set; }
        public DataRecipe() { CreatedUtc = UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Status = "DRAFT"; Steps = new List<DataTransformStep>(); Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "DRAFT") + "   ·   " + (Name ?? Id ?? "recipe") + "   ·   " + Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps   ·   source " + (SourceDatasetId ?? "∅"); }
    }

    internal sealed class DataValidationRule
    {
        public string Id { get; set; }
        public string Field { get; set; }
        public string Rule { get; set; }
        public string Argument { get; set; }
        public string Severity { get; set; }
        public string Rationale { get; set; }
        public DataValidationRule() { Severity = "ERROR"; }
        public override string ToString() { return (Severity ?? "ERROR") + "   ·   " + (Field ?? "dataset") + "   ·   " + (Rule ?? "RULE") + (String.IsNullOrWhiteSpace(Argument) ? "" : "   ·   " + Argument); }
    }

    internal sealed class DataSchemaContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DatasetId { get; set; }
        public string SourceFingerprint { get; set; }
        public string CreatedUtc { get; set; }
        public string Status { get; set; }
        public List<DataValidationRule> Rules { get; set; }
        public string CertificateHash { get; set; }
        public DataSchemaContract() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Status = "ACTIVE"; Rules = new List<DataValidationRule>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "contract") + "   ·   " + Rules.Count.ToString(CultureInfo.InvariantCulture) + " rules   ·   " + DataFoundryKernel.Short(CertificateHash); }
    }

    internal sealed class DataContractFinding
    {
        public string RuleId { get; set; }
        public string Severity { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }
        public bool Pass { get; set; }
        public override string ToString() { return (Pass ? "PASS" : (Severity ?? "FAIL")) + "   ·   " + (Field ?? "dataset") + "   ·   " + (Message ?? ""); }
    }

    internal sealed class DataContractResult
    {
        public string ContractId { get; set; }
        public string DatasetId { get; set; }
        public string DatasetFingerprint { get; set; }
        public string Verdict { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public List<DataContractFinding> Findings { get; set; }
        public string CertificateHash { get; set; }
        public DataContractResult() { Findings = new List<DataContractFinding>(); }
        public override string ToString() { return (Verdict ?? "UNKNOWN") + "   ·   " + Passed.ToString(CultureInfo.InvariantCulture) + " pass / " + Failed.ToString(CultureInfo.InvariantCulture) + " fail   ·   " + DataFoundryKernel.Short(CertificateHash); }
    }

    internal sealed class DataDriftField
    {
        public string Field { get; set; }
        public string Kind { get; set; }
        public string BeforeType { get; set; }
        public string AfterType { get; set; }
        public double NullRateDelta { get; set; }
        public double MeanDeltaNormalized { get; set; }
        public double DistinctRateDelta { get; set; }
        public double Severity { get; set; }
        public override string ToString() { return (Kind ?? "DRIFT") + "   ·   " + (Field ?? "field") + "   ·   severity " + Severity.ToString("P0", CultureInfo.InvariantCulture) + "   ·   null Δ " + NullRateDelta.ToString("P1", CultureInfo.InvariantCulture) + "   ·   mean σΔ " + MeanDeltaNormalized.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    internal sealed class DataDriftReport
    {
        public string Id { get; set; }
        public string BeforeDatasetId { get; set; }
        public string AfterDatasetId { get; set; }
        public string BeforeFingerprint { get; set; }
        public string AfterFingerprint { get; set; }
        public string Verdict { get; set; }
        public double SchemaDrift { get; set; }
        public double DistributionDrift { get; set; }
        public List<DataDriftField> Fields { get; set; }
        public string CertificateHash { get; set; }
        public string CreatedUtc { get; set; }
        public DataDriftReport() { Fields = new List<DataDriftField>(); CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Verdict ?? "DRIFT") + "   ·   schema " + SchemaDrift.ToString("P0", CultureInfo.InvariantCulture) + "   ·   distribution " + DistributionDrift.ToString("P0", CultureInfo.InvariantCulture) + "   ·   " + Fields.Count.ToString(CultureInfo.InvariantCulture) + " field deltas"; }
    }

    internal sealed class DataProjectionReceipt
    {
        public string Id { get; set; }
        public string SourceCivilization { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string DatasetId { get; set; }
        public string CreatedUtc { get; set; }
        public string ProjectionRule { get; set; }
        public string Status { get; set; }
        public DataProjectionReceipt() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); Status = "IMMUTABLE_PROJECTION"; }
        public override string ToString() { return (SourceCivilization ?? "SOURCE") + "   ·   " + (SourceId ?? "∅") + " → " + (DatasetId ?? "∅") + "   ·   " + DataFoundryKernel.Short(SourceFingerprint); }
    }

    internal sealed class DataBaseline
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string DatasetFingerprint { get; set; }
        public string Label { get; set; }
        public string CreatedUtc { get; set; }
        public long RowCount { get; set; }
        public int FieldCount { get; set; }
        public double QualityScore { get; set; }
        public string CertificateHash { get; set; }
        public DataBaseline() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Label ?? Id ?? "baseline") + "   ·   rows " + RowCount.ToString("N0", CultureInfo.InvariantCulture) + "   ·   fields " + FieldCount.ToString(CultureInfo.InvariantCulture) + "   ·   quality " + QualityScore.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class DataCapsule
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string DatasetFingerprint { get; set; }
        public string Question { get; set; }
        public string Metrics { get; set; }
        public string Assumptions { get; set; }
        public string Bounds { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public DataCapsule() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return "SEALED   ·   " + (Question ?? Id ?? "capsule") + "   ·   " + DataFoundryKernel.Short(CertificateHash); }
    }

    internal sealed class DataDossier
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string Title { get; set; }
        public string Decision { get; set; }
        public string Evidence { get; set; }
        public string Caveats { get; set; }
        public string CreatedUtc { get; set; }
        public string CertificateHash { get; set; }
        public DataDossier() { CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        public override string ToString() { return (Title ?? Id ?? "dossier") + "   ·   " + DataFoundryKernel.Clip(Decision, 100) + "   ·   " + DataFoundryKernel.Short(CertificateHash); }
    }

    internal sealed class DataDataset
    {
        public string Id { get; set; }
        public string ParentDatasetId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Format { get; set; }
        public string Delimiter { get; set; }
        public bool HasHeader { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string DataFingerprint { get; set; }
        public long RowCount { get; set; }
        public long ByteCount { get; set; }
        public long MalformedRows { get; set; }
        public long BlankRows { get; set; }
        public double QualityScore { get; set; }
        public bool ProfileTruncated { get; set; }
        public int SampleCapacity { get; set; }
        public int DistinctCapacityPerField { get; set; }
        public DataOrigin Origin { get; set; }
        public List<DataFieldProfile> Fields { get; set; }
        public List<DataChunkProfile> Chunks { get; set; }
        public List<DataRowSample> Samples { get; set; }
        public List<DataQualityFinding> Quality { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DataDataset() { Status = "PROFILED"; Format = "SYNTHETIC"; Delimiter = ","; HasHeader = true; CreatedUtc = UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture); SourceKind = "NATIVE"; SampleCapacity = 256; DistinctCapacityPerField = 4096; Origin = new DataOrigin(); Fields = new List<DataFieldProfile>(); Chunks = new List<DataChunkProfile>(); Samples = new List<DataRowSample>(); Quality = new List<DataQualityFinding>(); Tags = new List<string>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "PROFILED") + "   ·   " + (Name ?? Id ?? "DATASET") + "   ·   " + RowCount.ToString("N0", CultureInfo.InvariantCulture) + " rows / " + Fields.Count.ToString(CultureInfo.InvariantCulture) + " fields   ·   quality " + QualityScore.ToString("P0", CultureInfo.InvariantCulture) + "   ·   " + (Format ?? "DATA"); }
    }

    internal sealed class DataImportOptions
    {
        public string Format { get; set; }
        public char Delimiter { get; set; }
        public bool HasHeader { get; set; }
        public int SampleCapacity { get; set; }
        public int DistinctCapacityPerField { get; set; }
        public int ChunkRows { get; set; }
        public int MaxProfileFields { get; set; }
        public Encoding Encoding { get; set; }
        public DataImportOptions() { Format = "CSV"; Delimiter = ','; HasHeader = true; SampleCapacity = 256; DistinctCapacityPerField = 4096; ChunkRows = 50000; MaxProfileFields = 512; Encoding = new UTF8Encoding(false, true); }
    }

    internal sealed class DataImportResult
    {
        public DataDataset Dataset { get; set; }
        public List<string> Warnings { get; set; }
        public string CertificateHash { get; set; }
        public DataImportResult() { Warnings = new List<string>(); }
    }

    internal sealed class DataRecipePreview
    {
        public string DatasetId { get; set; }
        public string RecipeId { get; set; }
        public int InputSamples { get; set; }
        public int OutputSamples { get; set; }
        public int RejectedSamples { get; set; }
        public List<DataRowSample> Before { get; set; }
        public List<DataRowSample> After { get; set; }
        public List<string> Warnings { get; set; }
        public string CertificateHash { get; set; }
        public DataRecipePreview() { Before = new List<DataRowSample>(); After = new List<DataRowSample>(); Warnings = new List<string>(); }
    }

    internal sealed class DataMaterializationResult
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
        public long InputRows { get; set; }
        public long OutputRows { get; set; }
        public long RejectedRows { get; set; }
        public string Sha256 { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Warnings { get; set; }
        public DataMaterializationResult() { Warnings = new List<string>(); }
        public override string ToString() { return (Success ? "MATERIALIZED" : "NOT MATERIALIZED") + "   ·   " + OutputRows.ToString("N0", CultureInfo.InvariantCulture) + " rows   ·   " + (OutputPath ?? "") + "   ·   " + DataFoundryKernel.Short(Sha256); }
    }

    internal sealed class DataValidationIssue
    {
        public string Severity { get; set; }
        public string Message { get; set; }
        public DataValidationIssue() { Severity = "ERROR"; }
        public override string ToString() { return (Severity ?? "ERROR") + "   ·   " + (Message ?? ""); }
    }

    internal static class DataFoundryKernel
    {
        public const int Schema = 1;
        public const int MaxProfileFields = 512;
        public const int MaxSamples = 1024;
        public const int MaxDistinctPerField = 16384;
        public const int MaxCorrelationFields = 48;
        public const int MaxRelationsPerPair = 128;

        private sealed class RunningField
        {
            public DataFieldProfile Profile;
            public HashSet<string> Distinct;
            public double M2;
            public double LengthM2;
            public RunningField(DataFieldProfile p, int distinctCap) { Profile = p; Distinct = new HashSet<string>(StringComparer.Ordinal); DistinctCap = Math.Max(8, Math.Min(MaxDistinctPerField, distinctCap)); }
            public int DistinctCap;
        }

        public static string NewId(string prefix) { return (prefix ?? "DATA") + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(); }
        public static string Clip(string value, int max) { string s = value ?? ""; if (s.Length <= max) return s; return s.Substring(0, Math.Max(0, max - 1)) + "…"; }
        public static string Short(string hash) { string h = hash ?? ""; return h.Length <= 12 ? h : h.Substring(0, 12); }
        public static double Clamp01(double x) { if (Double.IsNaN(x) || Double.IsInfinity(x)) return 0; return Math.Max(0, Math.Min(1, x)); }
        public static string HashText(string text) { using (SHA256 sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "").ToLowerInvariant(); } }
        public static string HashFile(string path) { using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024 * 1024, FileOptions.SequentialScan)) using (SHA256 sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); } }
        public static string HashObject(object value) { JavaScriptSerializer json = new JavaScriptSerializer(); json.MaxJsonLength = Int32.MaxValue; json.RecursionLimit = 256; return HashText(json.Serialize(value)); }

        public static void Normalize(DataDataset d)
        {
            if (d == null) return;
            if (String.IsNullOrWhiteSpace(d.Id)) d.Id = NewId("DS");
            if (String.IsNullOrWhiteSpace(d.Name)) d.Name = d.Id;
            if (String.IsNullOrWhiteSpace(d.Status)) d.Status = "PROFILED";
            if (String.IsNullOrWhiteSpace(d.CreatedUtc)) d.CreatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            d.UpdatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            if (d.Origin == null) d.Origin = new DataOrigin();
            if (d.Fields == null) d.Fields = new List<DataFieldProfile>();
            if (d.Chunks == null) d.Chunks = new List<DataChunkProfile>();
            if (d.Samples == null) d.Samples = new List<DataRowSample>();
            if (d.Quality == null) d.Quality = new List<DataQualityFinding>();
            if (d.Tags == null) d.Tags = new List<string>();
            if (d.Assumptions == null) d.Assumptions = new List<string>();
            if (d.EvidenceNodeIds == null) d.EvidenceNodeIds = new List<string>();
            d.SampleCapacity = Math.Max(16, Math.Min(MaxSamples, d.SampleCapacity <= 0 ? 256 : d.SampleCapacity));
            d.DistinctCapacityPerField = Math.Max(32, Math.Min(MaxDistinctPerField, d.DistinctCapacityPerField <= 0 ? 4096 : d.DistinctCapacityPerField));
            foreach (DataFieldProfile f in d.Fields) { if (f == null) continue; if (f.ExampleValues == null) f.ExampleValues = new List<string>(); if (f.QualityFlags == null) f.QualityFlags = new List<string>(); }
            foreach (DataRowSample s in d.Samples) { if (s == null) continue; if (s.Values == null) s.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); if (s.Flags == null) s.Flags = new List<string>(); }
            if (String.IsNullOrWhiteSpace(d.DataFingerprint)) d.DataFingerprint = Fingerprint(d);
        }

        public static string Fingerprint(DataDataset d)
        {
            if (d == null) return HashText("NULL_DATASET");
            StringBuilder b = new StringBuilder();
            b.Append(d.Id).Append('|').Append(d.ParentDatasetId).Append('|').Append(d.Name).Append('|').Append(d.Format).Append('|').Append(d.Delimiter).Append('|').Append(d.HasHeader).Append('|').Append(d.RowCount).Append('|').Append(d.ByteCount).Append('|').Append(d.MalformedRows).Append('|').Append(d.BlankRows).Append('|').Append(d.SourceKind).Append('|').Append(d.SourceId).Append('|').Append(d.SourceFingerprint).Append('|');
            if (d.Origin != null) b.Append(d.Origin.Kind).Append('|').Append(d.Origin.Sha256).Append('|').Append(d.Origin.ParentFingerprint).Append('|').Append(d.Origin.RecipeId).Append('|');
            foreach (DataFieldProfile f in (d.Fields ?? new List<DataFieldProfile>()).OrderBy(x => x == null ? Int32.MaxValue : x.Ordinal)) if (f != null) b.Append(f.Ordinal).Append(':').Append(f.Name).Append(':').Append(f.InferredType).Append(':').Append(f.Observed).Append(':').Append(f.NullOrBlank).Append(':').Append(f.ApproxDistinct).Append(':').Append(f.MinNumber.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.MaxNumber.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(f.MeanNumber.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            foreach (DataRowSample s in (d.Samples ?? new List<DataRowSample>()).OrderBy(x => x == null ? Int64.MaxValue : x.RowNumber)) if (s != null) { b.Append("S:").Append(s.RowNumber).Append(':'); foreach (KeyValuePair<string, string> kv in s.Values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) b.Append(kv.Key).Append('=').Append(kv.Value).Append(';'); b.Append('|'); }
            return HashText(b.ToString());
        }

        public static List<DataValidationIssue> Validate(DataDataset d)
        {
            List<DataValidationIssue> issues = new List<DataValidationIssue>();
            if (d == null) { issues.Add(new DataValidationIssue { Message = "Dataset is null." }); return issues; }
            Normalize(d);
            if (d.Fields.Count == 0) issues.Add(new DataValidationIssue { Severity = "WARN", Message = "Dataset has no profiled fields." });
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataFieldProfile f in d.Fields) { if (f == null || String.IsNullOrWhiteSpace(f.Name)) issues.Add(new DataValidationIssue { Message = "A field has no name." }); else if (!names.Add(f.Name)) issues.Add(new DataValidationIssue { Message = "Duplicate field name: " + f.Name }); }
            if (d.RowCount < 0) issues.Add(new DataValidationIssue { Message = "Row count cannot be negative." });
            if (d.QualityScore < 0 || d.QualityScore > 1) issues.Add(new DataValidationIssue { Message = "Quality score must be within [0,1]." });
            if (!String.IsNullOrWhiteSpace(d.Origin.Sha256) && d.Origin.Sha256.Length != 64) issues.Add(new DataValidationIssue { Message = "Origin SHA-256 is malformed." });
            return issues;
        }

        public static DataImportResult ImportFile(string path, string datasetName, DataImportOptions options)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("A source file path is required.", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("Data Foundry source file was not found.", path);
            options = options ?? new DataImportOptions();
            string fmt = (options.Format ?? "CSV").Trim().ToUpperInvariant();
            if (fmt == "JSONL" || fmt == "JSON LINES" || fmt == "NDJSON") return ImportJsonLines(path, datasetName, options);
            return ImportDelimited(path, datasetName, options);
        }

        public static DataImportResult ImportDelimited(string path, string datasetName, DataImportOptions options)
        {
            options = options ?? new DataImportOptions();
            int sampleCap = Math.Max(16, Math.Min(MaxSamples, options.SampleCapacity));
            int distinctCap = Math.Max(32, Math.Min(MaxDistinctPerField, options.DistinctCapacityPerField));
            int chunkRows = Math.Max(1000, options.ChunkRows);
            DataImportResult result = new DataImportResult();
            FileInfo info = new FileInfo(path);
            DataDataset d = new DataDataset { Id = NewId("DS"), Name = String.IsNullOrWhiteSpace(datasetName) ? Path.GetFileNameWithoutExtension(path) : datasetName, Description = "Streaming local delimited-file profile. Source bytes remain immutable; controller state retains bounded statistics and samples.", Format = options.Delimiter == '\t' ? "TSV" : "CSV", Delimiter = options.Delimiter.ToString(), HasHeader = options.HasHeader, ByteCount = info.Length, SampleCapacity = sampleCap, DistinctCapacityPerField = distinctCap, SourceKind = "LOCAL_FILE" };
            d.Origin = new DataOrigin { Kind = "LOCAL_FILE", Locator = Path.GetFullPath(path), DisplayName = info.Name, LengthBytes = info.Length, ModifiedUtc = info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture), CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
            d.Origin.Sha256 = HashFile(path); d.SourceFingerprint = d.Origin.Sha256;
            List<string> headers = null; List<RunningField> running = null; List<DataRowSample> sample = new List<DataRowSample>(); long row = 0, bytesApprox = 0, malformed = 0, blank = 0; long chunkFirst = 1, chunkMalformed = 0, chunkBlank = 0, chunkBytes = 0; int chunkIndex = 0; DateTime chunkStart = DateTime.UtcNow; bool profileTruncated = false; int profileFieldCap = Math.Max(1, Math.Min(MaxProfileFields, options.MaxProfileFields));
            using (StreamReader reader = new StreamReader(path, options.Encoding ?? new UTF8Encoding(false, true), true, 1024 * 1024))
            {
                string line; bool first = true;
                while ((line = reader.ReadLine()) != null)
                {
                    long lineBytes = (reader.CurrentEncoding ?? Encoding.UTF8).GetByteCount(line) + 1; bytesApprox += lineBytes; chunkBytes += lineBytes;
                    if (first && options.HasHeader)
                    {
                        first = false; bool ok; List<string> parsed = ParseDelimitedLine(line, options.Delimiter, out ok); profileTruncated = parsed.Count > profileFieldCap; headers = MakeHeaders(parsed, options.MaxProfileFields); running = CreateRunning(headers, distinctCap); if (!ok) result.Warnings.Add("Header contains an unterminated quote; parsed using the bounded single-line dialect."); continue;
                    }
                    first = false; row++;
                    if (String.IsNullOrWhiteSpace(line)) { blank++; chunkBlank++; }
                    bool parseOk; List<string> cells = ParseDelimitedLine(line, options.Delimiter, out parseOk);
                    if (headers == null) { profileTruncated = cells.Count > profileFieldCap; int count = Math.Min(Math.Max(1, cells.Count), profileFieldCap); headers = Enumerable.Range(1, count).Select(x => "field_" + x.ToString(CultureInfo.InvariantCulture)).ToList(); running = CreateRunning(headers, distinctCap); }
                    if (!parseOk || cells.Count != headers.Count) { malformed++; chunkMalformed++; }
                    Dictionary<string, string> rowMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    int profiled = Math.Min(headers.Count, running.Count);
                    for (int i = 0; i < profiled; i++) { string value = i < cells.Count ? cells[i] : ""; rowMap[headers[i]] = value; Observe(running[i], value); }
                    ConsiderSample(sample, sampleCap, row, rowMap, parseOk && cells.Count == headers.Count ? null : "ROW_SHAPE");
                    if (row % chunkRows == 0) { d.Chunks.Add(FinishChunk(chunkIndex++, chunkFirst, row, chunkBytes, chunkMalformed, chunkBlank, chunkStart, d.Origin.Sha256)); chunkFirst = row + 1; chunkMalformed = chunkBlank = chunkBytes = 0; chunkStart = DateTime.UtcNow; }
                }
            }
            if (row >= chunkFirst || d.Chunks.Count == 0) d.Chunks.Add(FinishChunk(chunkIndex, chunkFirst, row, chunkBytes, chunkMalformed, chunkBlank, chunkStart, d.Origin.Sha256));
            d.RowCount = row; d.MalformedRows = malformed; d.BlankRows = blank; d.Samples = sample.OrderBy(x => x.RowNumber).ToList(); d.Fields = FinalizeFields(running ?? new List<RunningField>(), row); d.ProfileTruncated = profileTruncated; BuildQuality(d); d.DataFingerprint = Fingerprint(d); d.Origin.Notes = "Full source SHA-256 computed by streaming pass. CSV parser is RFC-4180-like for escaped quotes inside one physical line; multiline quoted fields are intentionally not reconstructed in DEV13.37.24."; result.Dataset = d; result.CertificateHash = HashText(d.Origin.Sha256 + "|IMPORT|" + d.DataFingerprint + "|" + row.ToString(CultureInfo.InvariantCulture));
            if (d.ProfileTruncated) result.Warnings.Add("Field profiling reached the configured field ceiling; unprofiled tail columns remain in source bytes.");
            if (malformed > 0) result.Warnings.Add(malformed.ToString("N0", CultureInfo.InvariantCulture) + " row(s) had quote/shape anomalies and were retained as quality evidence.");
            return result;
        }

        public static DataImportResult ImportJsonLines(string path, string datasetName, DataImportOptions options)
        {
            options = options ?? new DataImportOptions(); int sampleCap = Math.Max(16, Math.Min(MaxSamples, options.SampleCapacity)); int distinctCap = Math.Max(32, Math.Min(MaxDistinctPerField, options.DistinctCapacityPerField)); int chunkRows = Math.Max(1000, options.ChunkRows); int fieldCap = Math.Max(1, Math.Min(MaxProfileFields, options.MaxProfileFields));
            FileInfo info = new FileInfo(path); DataImportResult result = new DataImportResult(); DataDataset d = new DataDataset { Id = NewId("DS"), Name = String.IsNullOrWhiteSpace(datasetName) ? Path.GetFileNameWithoutExtension(path) : datasetName, Description = "Streaming JSON Lines profile with flattened top-level scalar fields.", Format = "JSONL", Delimiter = "", HasHeader = false, ByteCount = info.Length, SampleCapacity = sampleCap, DistinctCapacityPerField = distinctCap, SourceKind = "LOCAL_FILE" };
            d.Origin = new DataOrigin { Kind = "LOCAL_FILE", Locator = Path.GetFullPath(path), DisplayName = info.Name, LengthBytes = info.Length, ModifiedUtc = info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture), CapturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }; d.Origin.Sha256 = HashFile(path); d.SourceFingerprint = d.Origin.Sha256;
            JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 128 }; List<string> headers = new List<string>(); Dictionary<string, RunningField> runningMap = new Dictionary<string, RunningField>(StringComparer.OrdinalIgnoreCase); List<DataRowSample> sample = new List<DataRowSample>(); long row = 0, malformed = 0, blank = 0, chunkFirst = 1, chunkMalformed = 0, chunkBlank = 0, chunkBytes = 0; int chunkIndex = 0; DateTime chunkStart = DateTime.UtcNow; bool profileTruncated = false;
            using (StreamReader reader = new StreamReader(path, options.Encoding ?? new UTF8Encoding(false, true), true, 1024 * 1024))
            {
                string line; while ((line = reader.ReadLine()) != null)
                {
                    row++; chunkBytes += (reader.CurrentEncoding ?? Encoding.UTF8).GetByteCount(line) + 1; Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); bool ok = true;
                    if (String.IsNullOrWhiteSpace(line)) { blank++; chunkBlank++; ok = false; }
                    else
                    {
                        try
                        {
                            object root = json.DeserializeObject(line); IDictionary<string, object> obj = root as IDictionary<string, object>; if (obj == null) throw new InvalidDataException("JSON line is not an object.");
                            foreach (KeyValuePair<string, object> kv in obj)
                            {
                                string key = SafeFieldName(kv.Key); if (String.IsNullOrWhiteSpace(key)) continue; string value = ScalarText(kv.Value, json); map[key] = value;
                                RunningField rf; if (!runningMap.TryGetValue(key, out rf)) { if (headers.Count < fieldCap) { DataFieldProfile p = new DataFieldProfile { Name = key, Ordinal = headers.Count, Observed = row - 1, NullOrBlank = row - 1 }; rf = new RunningField(p, distinctCap); runningMap[key] = rf; headers.Add(key); } else profileTruncated = true; }
                            }
                        }
                        catch { malformed++; chunkMalformed++; ok = false; }
                    }
                    foreach (string h in headers) { RunningField rf = runningMap[h]; string value; map.TryGetValue(h, out value); Observe(rf, value ?? ""); }
                    ConsiderSample(sample, sampleCap, row, map, ok ? null : "JSON_PARSE_OR_SHAPE");
                    if (row % chunkRows == 0) { d.Chunks.Add(FinishChunk(chunkIndex++, chunkFirst, row, chunkBytes, chunkMalformed, chunkBlank, chunkStart, d.Origin.Sha256)); chunkFirst = row + 1; chunkMalformed = chunkBlank = chunkBytes = 0; chunkStart = DateTime.UtcNow; }
                }
            }
            if (row >= chunkFirst || d.Chunks.Count == 0) d.Chunks.Add(FinishChunk(chunkIndex, chunkFirst, row, chunkBytes, chunkMalformed, chunkBlank, chunkStart, d.Origin.Sha256));
            d.RowCount = row; d.MalformedRows = malformed; d.BlankRows = blank; d.Samples = sample.OrderBy(x => x.RowNumber).ToList(); d.Fields = FinalizeFields(headers.Select(x => runningMap[x]).ToList(), row); d.ProfileTruncated = profileTruncated; BuildQuality(d); d.DataFingerprint = Fingerprint(d); d.Origin.Notes = "Top-level JSON objects are streamed line by line. Nested arrays/objects are retained in samples as compact JSON text but not recursively exploded into unlimited columns."; result.Dataset = d; result.CertificateHash = HashText(d.Origin.Sha256 + "|JSONL_IMPORT|" + d.DataFingerprint);
            if (d.ProfileTruncated) result.Warnings.Add("JSON field discovery reached the configured field ceiling."); if (malformed > 0) result.Warnings.Add(malformed.ToString("N0", CultureInfo.InvariantCulture) + " line(s) were malformed or non-object JSON."); return result;
        }

        private static List<string> MakeHeaders(List<string> raw, int max)
        {
            int cap = Math.Max(1, Math.Min(MaxProfileFields, max)); List<string> result = new List<string>(); Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); int n = Math.Min(cap, Math.Max(1, raw.Count));
            for (int i = 0; i < n; i++) { string h = SafeFieldName(i < raw.Count ? raw[i] : ""); if (String.IsNullOrWhiteSpace(h)) h = "field_" + (i + 1).ToString(CultureInfo.InvariantCulture); int count; if (seen.TryGetValue(h, out count)) { count++; seen[h] = count; h = h + "_" + count.ToString(CultureInfo.InvariantCulture); } else seen[h] = 1; result.Add(h); }
            return result;
        }

        private static string SafeFieldName(string s) { string x = (s ?? "").Trim().TrimStart('\uFEFF'); return Clip(x, 256); }
        private static List<RunningField> CreateRunning(List<string> headers, int distinctCap) { List<RunningField> r = new List<RunningField>(); for (int i = 0; i < headers.Count; i++) r.Add(new RunningField(new DataFieldProfile { Name = headers[i], Ordinal = i }, distinctCap)); return r; }

        internal static List<string> ParseDelimitedLine(string line, char delimiter, out bool ok)
        {
            List<string> cells = new List<string>(); StringBuilder b = new StringBuilder(); bool quoted = false; ok = true; string s = line ?? "";
            for (int i = 0; i < s.Length; i++) { char c = s[i]; if (c == '"') { if (quoted && i + 1 < s.Length && s[i + 1] == '"') { b.Append('"'); i++; } else quoted = !quoted; } else if (c == delimiter && !quoted) { cells.Add(b.ToString()); b.Length = 0; } else b.Append(c); }
            cells.Add(b.ToString()); if (quoted) ok = false; return cells;
        }

        private static void Observe(RunningField rf, string value)
        {
            DataFieldProfile p = rf.Profile; p.Observed++; string s = value == null ? "" : value.Trim(); if (String.IsNullOrWhiteSpace(s) || String.Equals(s, "null", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "na", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "n/a", StringComparison.OrdinalIgnoreCase)) { p.NullOrBlank++; return; }
            if (p.ExampleValues.Count < 8 && !p.ExampleValues.Contains(s)) p.ExampleValues.Add(Clip(s, 160)); if (rf.Distinct.Count < rf.DistinctCap) rf.Distinct.Add(s); else p.DistinctCapped = true;
            double len = s.Length; double lengthDelta = len - p.MeanLength; long nonNull = p.Observed - p.NullOrBlank; p.MeanLength += lengthDelta / Math.Max(1, nonNull); rf.LengthM2 += lengthDelta * (len - p.MeanLength); if (nonNull == 1) { p.MinLength = p.MaxLength = len; } else { p.MinLength = Math.Min(p.MinLength, len); p.MaxLength = Math.Max(p.MaxLength, len); }
            long integer; double number; DateTime date; bool boolean;
            if (Int64.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) { p.IntegerCount++; p.NumericCount++; number = integer; ObserveNumber(rf, number); }
            else if (Double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out number)) { p.NumericCount++; ObserveNumber(rf, number); }
            else if (Boolean.TryParse(s, out boolean) || String.Equals(s, "yes", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "no", StringComparison.OrdinalIgnoreCase) || s == "0" || s == "1") p.BooleanCount++;
            else if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out date)) p.DateCount++;
            else p.TextCount++;
        }

        private static void ObserveNumber(RunningField rf, double x)
        {
            DataFieldProfile p = rf.Profile; long n = p.NumericCount; if (n == 1) { p.MinNumber = p.MaxNumber = p.MeanNumber = x; rf.M2 = 0; } else { p.MinNumber = Math.Min(p.MinNumber, x); p.MaxNumber = Math.Max(p.MaxNumber, x); double delta = x - p.MeanNumber; p.MeanNumber += delta / n; rf.M2 += delta * (x - p.MeanNumber); }
        }

        private static List<DataFieldProfile> FinalizeFields(List<RunningField> running, long rows)
        {
            List<DataFieldProfile> result = new List<DataFieldProfile>(); foreach (RunningField rf in running) { DataFieldProfile p = rf.Profile; long nonNull = Math.Max(0, p.Observed - p.NullOrBlank); p.ApproxDistinct = rf.Distinct.Count; p.NullRate = p.Observed == 0 ? 0 : (double)p.NullOrBlank / p.Observed; p.UniqueRate = nonNull == 0 ? 0 : Math.Min(1, (double)p.ApproxDistinct / nonNull); p.StdDevNumber = p.NumericCount > 1 ? Math.Sqrt(rf.M2 / (p.NumericCount - 1)) : 0; long dominant = Math.Max(Math.Max(p.NumericCount, p.DateCount), Math.Max(p.BooleanCount, p.TextCount)); if (nonNull == 0) p.InferredType = "NULL"; else if (p.IntegerCount == nonNull) p.InferredType = "INTEGER"; else if (p.NumericCount >= nonNull * 0.95) p.InferredType = "NUMBER"; else if (p.BooleanCount >= nonNull * 0.95) p.InferredType = "BOOLEAN"; else if (p.DateCount >= nonNull * 0.9) p.InferredType = "DATETIME"; else if (dominant < nonNull * 0.8) p.InferredType = "MIXED"; else p.InferredType = "TEXT"; p.CandidateKey = rows > 0 && p.NullRate == 0 && !p.DistinctCapped && p.ApproxDistinct == rows; p.Explanation = ExplainField(p); if (p.NullRate > 0.5) p.QualityFlags.Add("HIGH_NULLNESS"); if (p.InferredType == "MIXED") p.QualityFlags.Add("MIXED_TYPES"); if (p.CandidateKey) p.QualityFlags.Add("CANDIDATE_KEY"); result.Add(p); } return result;
        }

        public static string ExplainField(DataFieldProfile p)
        {
            if (p == null) return "No field profile."; string type = p.InferredType ?? "UNKNOWN"; string detail = type == "INTEGER" || type == "NUMBER" ? "Numeric values span " + p.MinNumber.ToString("0.###", CultureInfo.InvariantCulture) + " to " + p.MaxNumber.ToString("0.###", CultureInfo.InvariantCulture) + " with mean " + p.MeanNumber.ToString("0.###", CultureInfo.InvariantCulture) + "." : type == "DATETIME" ? "Values overwhelmingly parse as dates/times." : type == "BOOLEAN" ? "Values overwhelmingly behave like binary/boolean states." : type == "MIXED" ? "No single scalar type dominates; this field deserves normalization or domain review." : "Values behave primarily as text."; return (p.Name ?? "Field") + " is inferred as " + type + ". " + detail + " Missingness is " + p.NullRate.ToString("P1", CultureInfo.InvariantCulture) + "; observed distinctness is approximately " + p.ApproxDistinct.ToString("N0", CultureInfo.InvariantCulture) + (p.DistinctCapped ? "+ (capped)" : "") + "." + (p.CandidateKey ? " It is a candidate key in the profiled population." : "");
        }

        private static void BuildQuality(DataDataset d)
        {
            d.Quality.Clear(); long rows = Math.Max(1, d.RowCount); if (d.MalformedRows > 0) d.Quality.Add(new DataQualityFinding { Id = NewId("Q"), DatasetId = d.Id, Severity = d.MalformedRows > rows * 0.02 ? "ERROR" : "WARN", Kind = "ROW_SHAPE", Message = "Rows have inconsistent shape or parse anomalies.", AffectedEstimate = d.MalformedRows, Rate = (double)d.MalformedRows / rows }); if (d.BlankRows > 0) d.Quality.Add(new DataQualityFinding { Id = NewId("Q"), DatasetId = d.Id, Severity = "WARN", Kind = "BLANK_ROWS", Message = "Blank physical rows were observed.", AffectedEstimate = d.BlankRows, Rate = (double)d.BlankRows / rows });
            foreach (DataFieldProfile f in d.Fields) { if (f.NullRate >= 0.5) d.Quality.Add(new DataQualityFinding { Id = NewId("Q"), DatasetId = d.Id, FieldName = f.Name, Severity = f.NullRate >= 0.9 ? "ERROR" : "WARN", Kind = "NULLNESS", Message = "High missingness.", AffectedEstimate = f.NullOrBlank, Rate = f.NullRate }); if (f.InferredType == "MIXED") d.Quality.Add(new DataQualityFinding { Id = NewId("Q"), DatasetId = d.Id, FieldName = f.Name, Severity = "WARN", Kind = "TYPE_MIX", Message = "Mixed scalar representation may require normalization.", AffectedEstimate = f.Observed - f.NullOrBlank }); if (f.DistinctCapped) d.Quality.Add(new DataQualityFinding { Id = NewId("Q"), DatasetId = d.Id, FieldName = f.Name, Severity = "INFO", Kind = "DISTINCT_CAP", Message = "Distinct estimator reached its bounded memory cap; reported distinctness is a lower bound." }); }
            double penalty = Clamp01((double)d.MalformedRows / rows * 4.0 + (double)d.BlankRows / rows * 0.5 + d.Fields.Sum(x => x.NullRate) / Math.Max(1, d.Fields.Count) * 0.35 + d.Fields.Count(x => x.InferredType == "MIXED") / (double)Math.Max(1, d.Fields.Count) * 0.35); d.QualityScore = Clamp01(1 - penalty);
        }

        private static DataChunkProfile FinishChunk(int index, long first, long last, long bytes, long malformed, long blank, DateTime started, string sourceHash)
        {
            double seconds = Math.Max(0.001, (DateTime.UtcNow - started).TotalSeconds); long rows = Math.Max(0, last - first + 1); return new DataChunkProfile { ChunkIndex = index, FirstRow = first, LastRow = last, BytesRead = bytes, MalformedRows = malformed, BlankRows = blank, RowsPerSecond = rows / seconds, Fingerprint = HashText((sourceHash ?? "") + "|" + index.ToString(CultureInfo.InvariantCulture) + "|" + first.ToString(CultureInfo.InvariantCulture) + "|" + last.ToString(CultureInfo.InvariantCulture) + "|" + bytes.ToString(CultureInfo.InvariantCulture) + "|" + malformed.ToString(CultureInfo.InvariantCulture)) };
        }

        private static void ConsiderSample(List<DataRowSample> samples, int cap, long row, Dictionary<string, string> values, string flag)
        {
            string rank = HashText(row.ToString(CultureInfo.InvariantCulture) + "|" + String.Join("|", values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key + "=" + x.Value).ToArray())); DataRowSample candidate = new DataRowSample { RowNumber = row, StableRank = rank, Values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase) }; if (!String.IsNullOrWhiteSpace(flag)) candidate.Flags.Add(flag); if (samples.Count < cap) { samples.Add(candidate); return; } int worst = 0; for (int i = 1; i < samples.Count; i++) if (String.CompareOrdinal(samples[i].StableRank, samples[worst].StableRank) > 0) worst = i; if (String.CompareOrdinal(rank, samples[worst].StableRank) < 0) samples[worst] = candidate;
        }

        private static string ScalarText(object value, JavaScriptSerializer json)
        {
            if (value == null) return ""; string s = value as string; if (s != null) return s; if (value is bool) return ((bool)value) ? "true" : "false"; if (value is DateTime) return ((DateTime)value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture); if (value is IConvertible && !(value is IDictionary<string, object>) && !(value is object[])) return Convert.ToString(value, CultureInfo.InvariantCulture); return json.Serialize(value);
        }

        public static List<DataRelation> InferRelations(DataDataset left, DataDataset right)
        {
            List<DataRelation> result = new List<DataRelation>(); if (left == null || right == null) return result; Normalize(left); Normalize(right); Dictionary<string, HashSet<string>> lv = SampleSets(left), rv = SampleSets(right); foreach (DataFieldProfile a in left.Fields) foreach (DataFieldProfile b in right.Fields) { HashSet<string> asv, bsv; if (!lv.TryGetValue(a.Name, out asv) || !rv.TryGetValue(b.Name, out bsv) || asv.Count == 0 || bsv.Count == 0) continue; int intersect = asv.Count(x => bsv.Contains(x)); double overlap = (double)intersect / Math.Max(1, Math.Min(asv.Count, bsv.Count)); double name = NameSimilarity(a.Name, b.Name); double type = String.Equals(a.InferredType, b.InferredType, StringComparison.OrdinalIgnoreCase) ? 1 : ((a.InferredType == "INTEGER" || a.InferredType == "NUMBER") && (b.InferredType == "INTEGER" || b.InferredType == "NUMBER")) ? 0.8 : 0.2; double confidence = Clamp01(overlap * 0.55 + name * 0.3 + type * 0.15); if (confidence < 0.45) continue; string card = Cardinality(a, b); DataRelation r = new DataRelation { Id = NewId("REL"), LeftDatasetId = left.Id, LeftField = a.Name, RightDatasetId = right.Id, RightField = b.Name, Kind = confidence >= 0.75 ? "LIKELY_JOIN" : "CANDIDATE_JOIN", Cardinality = card, Confidence = confidence, SampleOverlap = overlap, NameSimilarity = name, Basis = "Bounded deterministic samples + field-name/type compatibility; not a foreign-key declaration." }; r.CertificateHash = HashText(Fingerprint(left) + "|" + Fingerprint(right) + "|" + r.LeftField + "|" + r.RightField + "|" + overlap.ToString("R", CultureInfo.InvariantCulture)); result.Add(r); }
            return result.OrderByDescending(x => x.Confidence).ThenBy(x => x.LeftField).Take(MaxRelationsPerPair).ToList();
        }

        private static Dictionary<string, HashSet<string>> SampleSets(DataDataset d)
        {
            Dictionary<string, HashSet<string>> r = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); foreach (DataFieldProfile f in d.Fields) r[f.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (DataRowSample s in d.Samples) foreach (KeyValuePair<string, string> kv in s.Values) if (!String.IsNullOrWhiteSpace(kv.Value) && r.ContainsKey(kv.Key) && r[kv.Key].Count < 2048) r[kv.Key].Add(kv.Value.Trim()); return r;
        }

        private static double NameSimilarity(string a, string b)
        {
            string x = Canon(a), y = Canon(b); if (x == y) return 1; if (x.EndsWith("id") && y.EndsWith("id") && (x.Contains(y) || y.Contains(x))) return 0.9; HashSet<string> ax = new HashSet<string>(TokensForName(a)); HashSet<string> by = new HashSet<string>(TokensForName(b)); int inter = ax.Count(z => by.Contains(z)); return (double)inter / Math.Max(1, ax.Union(by).Count());
        }
        private static string Canon(string s) { return new string((s ?? "").ToLowerInvariant().Where(Char.IsLetterOrDigit).ToArray()); }
        private static IEnumerable<string> TokensForName(string s) { return (s ?? "").ToLowerInvariant().Replace("_", " ").Replace("-", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); }
        private static string Cardinality(DataFieldProfile a, DataFieldProfile b) { bool au = a.CandidateKey || a.UniqueRate > 0.95, bu = b.CandidateKey || b.UniqueRate > 0.95; if (au && bu) return "ONE_TO_ONE?"; if (au) return "ONE_TO_MANY?"; if (bu) return "MANY_TO_ONE?"; return "MANY_TO_MANY?"; }

        public static List<DataCorrelation> Correlations(DataDataset d)
        {
            List<DataCorrelation> result = new List<DataCorrelation>(); if (d == null) return result; List<string> numeric = d.Fields.Where(x => x != null && (x.InferredType == "INTEGER" || x.InferredType == "NUMBER")).OrderByDescending(x => x.NumericCount).Take(MaxCorrelationFields).Select(x => x.Name).ToList(); for (int i = 0; i < numeric.Count; i++) for (int j = i + 1; j < numeric.Count; j++) { List<double> a = new List<double>(), b = new List<double>(); foreach (DataRowSample s in d.Samples) { string sa, sb; double da, db; if (s.Values.TryGetValue(numeric[i], out sa) && s.Values.TryGetValue(numeric[j], out sb) && Double.TryParse(sa, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out da) && Double.TryParse(sb, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out db)) { a.Add(da); b.Add(db); } } if (a.Count < 4) continue; double r = Pearson(a, b); DataCorrelation c = new DataCorrelation { DatasetId = d.Id, FieldA = numeric[i], FieldB = numeric[j], PairedSamples = a.Count, Pearson = r, Strength = Math.Abs(r) >= 0.8 ? "STRONG" : Math.Abs(r) >= 0.5 ? "MODERATE" : Math.Abs(r) >= 0.25 ? "WEAK" : "MINIMAL" }; c.CertificateHash = HashText(Fingerprint(d) + "|CORR|" + c.FieldA + "|" + c.FieldB + "|" + r.ToString("R", CultureInfo.InvariantCulture)); result.Add(c); } return result.OrderByDescending(x => Math.Abs(x.Pearson)).ToList();
        }
        private static double Pearson(List<double> a, List<double> b) { int n = Math.Min(a.Count, b.Count); double ma = a.Take(n).Average(), mb = b.Take(n).Average(), cov = 0, va = 0, vb = 0; for (int i = 0; i < n; i++) { double da = a[i] - ma, db = b[i] - mb; cov += da * db; va += da * da; vb += db * db; } if (va <= 0 || vb <= 0) return 0; return Math.Max(-1, Math.Min(1, cov / Math.Sqrt(va * vb))); }

        public static DataDriftReport Compare(DataDataset before, DataDataset after)
        {
            if (before == null || after == null) throw new ArgumentNullException(before == null ? "before" : "after"); Normalize(before); Normalize(after); DataDriftReport r = new DataDriftReport { Id = NewId("DRIFT"), BeforeDatasetId = before.Id, AfterDatasetId = after.Id, BeforeFingerprint = Fingerprint(before), AfterFingerprint = Fingerprint(after) }; Dictionary<string, DataFieldProfile> a = before.Fields.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase), b = after.Fields.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase); HashSet<string> names = new HashSet<string>(a.Keys, StringComparer.OrdinalIgnoreCase); names.UnionWith(b.Keys); int structural = 0; double dist = 0;
            foreach (string name in names.OrderBy(x => x)) { DataFieldProfile x, y; bool hx = a.TryGetValue(name, out x), hy = b.TryGetValue(name, out y); DataDriftField f = new DataDriftField { Field = name }; if (!hx) { f.Kind = "ADDED_FIELD"; f.Severity = 0.7; structural++; } else if (!hy) { f.Kind = "REMOVED_FIELD"; f.Severity = 1; structural++; } else { f.BeforeType = x.InferredType; f.AfterType = y.InferredType; f.NullRateDelta = y.NullRate - x.NullRate; double scale = Math.Max(Math.Max(Math.Abs(x.MeanNumber), Math.Abs(y.MeanNumber)), Math.Max(x.StdDevNumber, 1e-9)); f.MeanDeltaNormalized = (y.MeanNumber - x.MeanNumber) / scale; double xd = before.RowCount == 0 ? 0 : (double)x.ApproxDistinct / before.RowCount, yd = after.RowCount == 0 ? 0 : (double)y.ApproxDistinct / after.RowCount; f.DistinctRateDelta = yd - xd; bool typeChanged = !String.Equals(x.InferredType, y.InferredType, StringComparison.OrdinalIgnoreCase); f.Kind = typeChanged ? "TYPE_DRIFT" : "DISTRIBUTION_DRIFT"; f.Severity = Clamp01((typeChanged ? 0.65 : 0) + Math.Abs(f.NullRateDelta) * 0.8 + Math.Min(1, Math.Abs(f.MeanDeltaNormalized)) * 0.35 + Math.Abs(f.DistinctRateDelta) * 0.35); if (typeChanged) structural++; dist += f.Severity; } if (f.Severity >= 0.05) r.Fields.Add(f); }
            r.SchemaDrift = Clamp01((double)structural / Math.Max(1, names.Count)); r.DistributionDrift = Clamp01(dist / Math.Max(1, names.Count)); double total = Math.Max(r.SchemaDrift, r.DistributionDrift); r.Verdict = total >= 0.6 ? "MAJOR DRIFT" : total >= 0.3 ? "MATERIAL DRIFT" : total >= 0.1 ? "MINOR DRIFT" : "STABLE PROFILE"; r.CertificateHash = HashText(r.BeforeFingerprint + "|DRIFT|" + r.AfterFingerprint + "|" + String.Join("|", r.Fields.Select(x => x.Field + ":" + x.Kind + ":" + x.Severity.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return r;
        }

        public static DataSchemaContract BuildContract(DataDataset d, string name)
        {
            Normalize(d); DataSchemaContract c = new DataSchemaContract { Id = NewId("CONTRACT"), Name = String.IsNullOrWhiteSpace(name) ? (d.Name + " schema contract") : name, DatasetId = d.Id, SourceFingerprint = Fingerprint(d) }; foreach (DataFieldProfile f in d.Fields) { c.Rules.Add(new DataValidationRule { Id = NewId("RULE"), Field = f.Name, Rule = "FIELD_PRESENT", Severity = "ERROR", Rationale = "Field existed in the contract source profile." }); if (f.InferredType != "MIXED" && f.InferredType != "NULL" && f.InferredType != "UNKNOWN") c.Rules.Add(new DataValidationRule { Id = NewId("RULE"), Field = f.Name, Rule = "TYPE_COMPATIBLE", Argument = f.InferredType, Severity = "ERROR", Rationale = "Inferred type compatibility." }); if (f.CandidateKey) { c.Rules.Add(new DataValidationRule { Id = NewId("RULE"), Field = f.Name, Rule = "NULL_RATE_MAX", Argument = "0", Severity = "ERROR", Rationale = "Candidate key was complete in source profile." }); c.Rules.Add(new DataValidationRule { Id = NewId("RULE"), Field = f.Name, Rule = "UNIQUE_RATE_MIN", Argument = "0.95", Severity = "WARN", Rationale = "Candidate key uniqueness guard." }); } else if (f.NullRate < 0.2) c.Rules.Add(new DataValidationRule { Id = NewId("RULE"), Field = f.Name, Rule = "NULL_RATE_MAX", Argument = Math.Min(0.5, f.NullRate + 0.15).ToString("R", CultureInfo.InvariantCulture), Severity = "WARN", Rationale = "Bounded missingness drift guard." }); }
            c.CertificateHash = HashText(c.SourceFingerprint + "|CONTRACT|" + String.Join("|", c.Rules.Select(x => x.Field + ":" + x.Rule + ":" + x.Argument).ToArray())); return c;
        }

        public static DataContractResult ValidateContract(DataSchemaContract c, DataDataset d)
        {
            if (c == null || d == null) throw new ArgumentNullException(c == null ? "contract" : "dataset"); Normalize(d); DataContractResult r = new DataContractResult { ContractId = c.Id, DatasetId = d.Id, DatasetFingerprint = Fingerprint(d) }; Dictionary<string, DataFieldProfile> fields = d.Fields.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase); foreach (DataValidationRule rule in c.Rules ?? new List<DataValidationRule>()) { DataFieldProfile f; bool exists = fields.TryGetValue(rule.Field ?? "", out f); bool pass = true; string message = "Rule satisfied."; double arg; if (rule.Rule == "FIELD_PRESENT") { pass = exists; message = pass ? "Field is present." : "Required field is missing."; } else if (!exists) { pass = false; message = "Field is missing; rule cannot be evaluated."; } else if (rule.Rule == "TYPE_COMPATIBLE") { pass = TypeCompatible(rule.Argument, f.InferredType); message = pass ? "Inferred type is compatible with " + rule.Argument + "." : "Type changed from " + rule.Argument + " to " + f.InferredType + "."; } else if (rule.Rule == "NULL_RATE_MAX" && Double.TryParse(rule.Argument, NumberStyles.Float, CultureInfo.InvariantCulture, out arg)) { pass = f.NullRate <= arg; message = "Null rate " + f.NullRate.ToString("P1", CultureInfo.InvariantCulture) + " <= " + arg.ToString("P1", CultureInfo.InvariantCulture) + "."; } else if (rule.Rule == "UNIQUE_RATE_MIN" && Double.TryParse(rule.Argument, NumberStyles.Float, CultureInfo.InvariantCulture, out arg)) { pass = f.UniqueRate >= arg; message = "Unique rate " + f.UniqueRate.ToString("P1", CultureInfo.InvariantCulture) + " >= " + arg.ToString("P1", CultureInfo.InvariantCulture) + "."; } DataContractFinding finding = new DataContractFinding { RuleId = rule.Id, Severity = rule.Severity, Field = rule.Field, Pass = pass, Message = message }; r.Findings.Add(finding); if (pass) r.Passed++; else r.Failed++; }
            bool hard = r.Findings.Any(x => !x.Pass && String.Equals(x.Severity, "ERROR", StringComparison.OrdinalIgnoreCase)); r.Verdict = hard ? "CONTRACT VIOLATED" : r.Failed > 0 ? "CONTRACT WARNINGS" : "CONTRACT SATISFIED"; r.CertificateHash = HashText((c.CertificateHash ?? HashObject(c)) + "|VALIDATE|" + r.DatasetFingerprint + "|" + String.Join("|", r.Findings.Select(x => x.RuleId + ":" + x.Pass).ToArray())); return r;
        }
        private static bool TypeCompatible(string expected, string actual) { if (String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) return true; bool en = expected == "INTEGER" || expected == "NUMBER", an = actual == "INTEGER" || actual == "NUMBER"; return en && an; }

        public static DataRecipePreview PreviewRecipe(DataDataset d, DataRecipe recipe)
        {
            if (d == null || recipe == null) throw new ArgumentNullException(d == null ? "dataset" : "recipe"); DataRecipePreview p = new DataRecipePreview { DatasetId = d.Id, RecipeId = recipe.Id }; foreach (DataRowSample source in d.Samples.Take(256)) { DataRowSample before = CloneSample(source), after; bool keep = ApplyRecipeToSample(source, recipe, out after, p.Warnings); p.Before.Add(before); p.InputSamples++; if (keep) { p.After.Add(after); p.OutputSamples++; } else p.RejectedSamples++; } p.CertificateHash = HashText(Fingerprint(d) + "|PREVIEW|" + HashObject(recipe) + "|" + HashObject(p.After)); return p;
        }

        private static DataRowSample CloneSample(DataRowSample s) { DataRowSample x = new DataRowSample { RowNumber = s.RowNumber, StableRank = s.StableRank, Values = new Dictionary<string, string>(s.Values, StringComparer.OrdinalIgnoreCase) }; x.Flags.AddRange(s.Flags); return x; }

        private static bool ApplyRecipeToSample(DataRowSample source, DataRecipe recipe, out DataRowSample output, List<string> warnings)
        {
            output = CloneSample(source); foreach (DataTransformStep step in (recipe.Steps ?? new List<DataTransformStep>()).Where(x => x != null && x.Enabled).OrderBy(x => x.Order)) { string op = (step.Operation ?? "").Trim().ToUpperInvariant(); string value; if (op == "DROP") { output.Values.Remove(step.Field ?? ""); continue; } if (op == "RENAME") { if (output.Values.TryGetValue(step.Field ?? "", out value)) { output.Values.Remove(step.Field); output.Values[String.IsNullOrWhiteSpace(step.TargetField) ? step.Field : step.TargetField] = value; } continue; } if (op == "FILTER_EQUALS" || op == "FILTER_NOT_EQUALS") { output.Values.TryGetValue(step.Field ?? "", out value); bool eq = String.Equals(value ?? "", step.Argument ?? "", StringComparison.OrdinalIgnoreCase); if ((op == "FILTER_EQUALS" && !eq) || (op == "FILTER_NOT_EQUALS" && eq)) return false; continue; } if (!output.Values.TryGetValue(step.Field ?? "", out value)) continue; if (op == "TRIM") output.Values[step.Field] = (value ?? "").Trim(); else if (op == "NORMALIZE_WHITESPACE") output.Values[step.Field] = NormalizeWhitespace(value); else if (op == "LOWER") output.Values[step.Field] = (value ?? "").ToLowerInvariant(); else if (op == "UPPER") output.Values[step.Field] = (value ?? "").ToUpperInvariant(); else if (op == "REPLACE_NULL") { if (String.IsNullOrWhiteSpace(value) || String.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) output.Values[step.Field] = step.Argument ?? ""; } else if (op == "REPLACE") output.Values[step.Field] = (value ?? "").Replace(step.Argument ?? "", step.Argument2 ?? ""); else if (op == "PARSE_NUMBER") { double n; if (Double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out n)) output.Values[step.Field] = n.ToString("R", CultureInfo.InvariantCulture); else output.Flags.Add("PARSE_NUMBER:" + step.Field); } else if (op == "PARSE_DATE") { DateTime dt; if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out dt)) output.Values[step.Field] = dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture); else output.Flags.Add("PARSE_DATE:" + step.Field); } else { string warning = "Unsupported recipe operation: " + op; if (!warnings.Contains(warning)) warnings.Add(warning); } }
            return true;
        }
        private static string NormalizeWhitespace(string s) { return String.Join(" ", (s ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries)); }

        public static DataMaterializationResult MaterializeDelimited(DataDataset source, DataRecipe recipe, string outputDirectory)
        {
            DataMaterializationResult r = new DataMaterializationResult(); if (source == null || recipe == null) { r.Warnings.Add("Source dataset and recipe are required."); return r; } if (source.Origin == null || !String.Equals(source.Origin.Kind, "LOCAL_FILE", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(source.Origin.Locator) || !File.Exists(source.Origin.Locator)) { r.Warnings.Add("Materialization requires a retained local-file origin that still exists."); return r; } if (!(source.Format == "CSV" || source.Format == "TSV")) { r.Warnings.Add("DEV13.37.24 materialization currently supports CSV/TSV origins; JSONL remains profile/preview-only."); return r; } Directory.CreateDirectory(outputDirectory); string safe = new string((source.Name ?? "dataset").Select(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray()); if (String.IsNullOrWhiteSpace(safe)) safe = "dataset"; string path = Path.Combine(outputDirectory, safe + "-derived-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv"); char delimiter = String.IsNullOrEmpty(source.Delimiter) ? ',' : source.Delimiter[0]; DataImportOptions options = new DataImportOptions { Delimiter = delimiter, HasHeader = source.HasHeader };
            using (StreamReader reader = new StreamReader(source.Origin.Locator, new UTF8Encoding(false, true), true, 1024 * 1024)) using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                string line; List<string> headers = null; bool first = true; long row = 0; while ((line = reader.ReadLine()) != null) { if (first && source.HasHeader) { first = false; bool hok; headers = MakeHeaders(ParseDelimitedLine(line, delimiter, out hok), MaxProfileFields); List<string> transformedHeaders = TransformHeaders(headers, recipe); writer.WriteLine(String.Join(",", transformedHeaders.Select(EscapeCsv).ToArray())); continue; } first = false; row++; r.InputRows++; bool ok; List<string> cells = ParseDelimitedLine(line, delimiter, out ok); if (headers == null) headers = Enumerable.Range(1, cells.Count).Select(x => "field_" + x.ToString(CultureInfo.InvariantCulture)).ToList(); Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < headers.Count; i++) values[headers[i]] = i < cells.Count ? cells[i] : ""; DataRowSample src = new DataRowSample { RowNumber = row, Values = values }, dst; if (!ApplyRecipeToSample(src, recipe, out dst, r.Warnings)) { r.RejectedRows++; continue; } List<string> outHeaders = TransformHeaders(headers, recipe); writer.WriteLine(String.Join(",", outHeaders.Select(h => { string v; return EscapeCsv(dst.Values.TryGetValue(h, out v) ? v : ""); }).ToArray())); r.OutputRows++; }
            }
            r.OutputPath = path; r.Sha256 = HashFile(path); r.Success = true; r.CertificateHash = HashText(Fingerprint(source) + "|MATERIALIZE|" + HashObject(recipe) + "|" + r.Sha256 + "|" + r.OutputRows.ToString(CultureInfo.InvariantCulture)); return r;
        }

        private static List<string> TransformHeaders(List<string> headers, DataRecipe recipe)
        {
            List<string> result = new List<string>(headers); foreach (DataTransformStep step in (recipe.Steps ?? new List<DataTransformStep>()).Where(x => x != null && x.Enabled).OrderBy(x => x.Order)) { string op = (step.Operation ?? "").ToUpperInvariant(); if (op == "DROP") result.RemoveAll(x => String.Equals(x, step.Field, StringComparison.OrdinalIgnoreCase)); else if (op == "RENAME") { int i = result.FindIndex(x => String.Equals(x, step.Field, StringComparison.OrdinalIgnoreCase)); if (i >= 0 && !String.IsNullOrWhiteSpace(step.TargetField)) result[i] = step.TargetField; } } return result;
        }
        private static string EscapeCsv(string s) { string x = s ?? ""; if (x.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0) return "\"" + x.Replace("\"", "\"\"") + "\""; return x; }

        public static DataDataset SyntheticDataset(string name, string description, IList<IDictionary<string, object>> rows, string sourceKind, string sourceId, string sourceFingerprint)
        {
            DataDataset d = new DataDataset { Id = NewId("DS"), Name = name, Description = description, Format = "SYNTHETIC_TABLE", SourceKind = sourceKind ?? "SYNTHETIC", SourceId = sourceId, SourceFingerprint = sourceFingerprint, RowCount = rows == null ? 0 : rows.Count, Origin = new DataOrigin { Kind = "SYNTHETIC_OR_PROJECTION", DisplayName = name, ParentFingerprint = sourceFingerprint, Notes = "No external file was read. Values are retained bounded synthetic/projection records." } }; List<string> headers = new List<string>(); if (rows != null) foreach (IDictionary<string, object> row in rows) foreach (string key in row.Keys) if (!headers.Contains(key, StringComparer.OrdinalIgnoreCase) && headers.Count < MaxProfileFields) headers.Add(key); List<RunningField> running = CreateRunning(headers, 4096); long n = 0; if (rows != null) foreach (IDictionary<string, object> row in rows) { n++; Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < headers.Count; i++) { object obj; row.TryGetValue(headers[i], out obj); string value = obj == null ? "" : Convert.ToString(obj, CultureInfo.InvariantCulture); map[headers[i]] = value; Observe(running[i], value); } ConsiderSample(d.Samples, 256, n, map, null); } d.Fields = FinalizeFields(running, d.RowCount); BuildQuality(d); d.Origin.Sha256 = HashText(HashObject(rows)); d.DataFingerprint = Fingerprint(d); return d;
        }

        public static DataDataset ProjectionDataset(string name, string sourceCivilization, string sourceId, string sourceFingerprint, IDictionary<string, object> record)
        {
            List<IDictionary<string, object>> rows = new List<IDictionary<string, object>>(); rows.Add(record ?? new Dictionary<string, object>()); DataDataset d = SyntheticDataset(name, "Typed immutable projection from " + sourceCivilization + ". The upstream object remains authoritative and unmodified.", rows, "PROJECTION:" + sourceCivilization, sourceId, sourceFingerprint); d.Tags.Add("PROJECTION"); d.Tags.Add(sourceCivilization); return d;
        }

        public static DataBaseline Baseline(DataDataset d, string label) { Normalize(d); DataBaseline b = new DataBaseline { Id = NewId("BASE"), DatasetId = d.Id, DatasetFingerprint = Fingerprint(d), Label = String.IsNullOrWhiteSpace(label) ? d.Name + " baseline" : label, RowCount = d.RowCount, FieldCount = d.Fields.Count, QualityScore = d.QualityScore }; b.CertificateHash = HashText(b.DatasetFingerprint + "|BASELINE|" + b.Label + "|" + b.RowCount.ToString(CultureInfo.InvariantCulture)); return b; }
        public static DataCapsule Capsule(DataDataset d, string question, string metrics, string assumptions, string bounds) { Normalize(d); DataCapsule c = new DataCapsule { Id = NewId("CAP"), DatasetId = d.Id, DatasetFingerprint = Fingerprint(d), Question = question, Metrics = metrics, Assumptions = assumptions, Bounds = bounds }; c.CertificateHash = HashText(c.DatasetFingerprint + "|CAPSULE|" + c.Question + "|" + c.Metrics + "|" + c.Assumptions + "|" + c.Bounds); return c; }
        public static DataDossier Dossier(DataDataset d, string title, string decision, string evidence, string caveats) { Normalize(d); DataDossier x = new DataDossier { Id = NewId("DOS"), DatasetId = d.Id, Title = title, Decision = decision, Evidence = evidence, Caveats = caveats }; x.CertificateHash = HashText(Fingerprint(d) + "|DOSSIER|" + title + "|" + decision + "|" + evidence + "|" + caveats); return x; }
    }
}

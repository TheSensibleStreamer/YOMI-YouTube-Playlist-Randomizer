using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class DistributedFaultDomain
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string ParentId { get; set; }
        public string Region { get; set; }
        public double Capacity { get; set; }
        public double Correlation { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedFaultDomain() { Kind = "ZONE"; Capacity = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "DOMAIN") + "   ·   " + (Kind ?? "ZONE") + "   ·   " + (Region ?? "NO REGION") + "   ·   correlation " + Correlation.ToString("P0", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string FaultDomainId { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string Rack { get; set; }
        public double CpuCores { get; set; }
        public double MemoryGb { get; set; }
        public double DiskGb { get; set; }
        public double ClockOffsetMs { get; set; }
        public double ClockDriftPpm { get; set; }
        public double Reliability { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedNode() { Kind = "SERVER"; CpuCores = 4; MemoryGb = 16; DiskGb = 256; Reliability = 0.999; Status = "UP"; Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "UP") + "   ·   " + (Name ?? Id ?? "NODE") + "   ·   " + (Kind ?? "SERVER") + "   ·   " + (Region ?? "REGION") + "/" + (Zone ?? "ZONE") + "/" + (Rack ?? "RACK") + "   ·   " + CpuCores.ToString("0.#", CultureInfo.InvariantCulture) + "c " + MemoryGb.ToString("0.#", CultureInfo.InvariantCulture) + "GB   ·   clock " + ClockOffsetMs.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture) + "ms/" + ClockDriftPpm.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "ppm"; }
    }

    internal sealed class DistributedProcess
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Service { get; set; }
        public string NodeId { get; set; }
        public string GroupId { get; set; }
        public string Role { get; set; }
        public long Term { get; set; }
        public long Epoch { get; set; }
        public long LastLogIndex { get; set; }
        public long CommitIndex { get; set; }
        public long AppliedIndex { get; set; }
        public int Priority { get; set; }
        public bool Voting { get; set; }
        public bool AutoRestart { get; set; }
        public double RestartDelayMs { get; set; }
        public string Health { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedProcess() { Role = "FOLLOWER"; Priority = 1; Voting = true; AutoRestart = true; RestartDelayMs = 5000; Health = "HEALTHY"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Health ?? "HEALTHY") + "   ·   " + (Name ?? Id ?? "PROCESS") + "   ·   " + (Service ?? "SERVICE") + " @ " + (NodeId ?? "NODE") + "   ·   " + (Role ?? "FOLLOWER") + " term " + Term.ToString(CultureInfo.InvariantCulture) + "   ·   log/commit/apply " + LastLogIndex.ToString(CultureInfo.InvariantCulture) + "/" + CommitIndex.ToString(CultureInfo.InvariantCulture) + "/" + AppliedIndex.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedLink
    {
        public string Id { get; set; }
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public bool Bidirectional { get; set; }
        public double BaseLatencyMs { get; set; }
        public double JitterMs { get; set; }
        public double BandwidthMbps { get; set; }
        public double LossProbability { get; set; }
        public double DuplicationProbability { get; set; }
        public double ReorderProbability { get; set; }
        public double Reliability { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedLink() { Bidirectional = true; BaseLatencyMs = 10; BandwidthMbps = 1000; Reliability = 0.9999; Status = "UP"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "UP") + "   ·   " + (FromNodeId ?? "FROM") + (Bidirectional ? " ⇄ " : " → ") + (ToNodeId ?? "TO") + "   ·   " + BaseLatencyMs.ToString("0.###", CultureInfo.InvariantCulture) + "±" + JitterMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   " + BandwidthMbps.ToString("0.##", CultureInfo.InvariantCulture) + "Mbps   ·   loss/dup/reorder " + LossProbability.ToString("P2", CultureInfo.InvariantCulture) + "/" + DuplicationProbability.ToString("P2", CultureInfo.InvariantCulture) + "/" + ReorderProbability.ToString("P2", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedDataset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string ConsistencyModel { get; set; }
        public int ReplicationFactor { get; set; }
        public int ReadQuorum { get; set; }
        public int WriteQuorum { get; set; }
        public int ShardCount { get; set; }
        public bool SynchronousCommit { get; set; }
        public double RetentionSeconds { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedDataset() { Kind = "LOG"; ConsistencyModel = "LINEARIZABLE"; ReplicationFactor = 3; ReadQuorum = 2; WriteQuorum = 2; ShardCount = 1; SynchronousCommit = true; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "DATASET") + "   ·   " + (Kind ?? "LOG") + " / " + (ConsistencyModel ?? "LINEARIZABLE") + "   ·   N/R/W " + ReplicationFactor.ToString(CultureInfo.InvariantCulture) + "/" + ReadQuorum.ToString(CultureInfo.InvariantCulture) + "/" + WriteQuorum.ToString(CultureInfo.InvariantCulture) + "   ·   " + ShardCount.ToString(CultureInfo.InvariantCulture) + " shards"; }
    }

    internal sealed class DistributedShard
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string KeyRangeStart { get; set; }
        public string KeyRangeEnd { get; set; }
        public string LeaderReplicaId { get; set; }
        public long Epoch { get; set; }
        public string Status { get; set; }
        public DistributedShard() { Status = "ACTIVE"; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Id ?? "SHARD") + "   ·   " + (DatasetId ?? "DATASET") + "   ·   [" + (KeyRangeStart ?? "−∞") + ", " + (KeyRangeEnd ?? "+∞") + ")   ·   epoch " + Epoch.ToString(CultureInfo.InvariantCulture) + "   ·   leader " + (LeaderReplicaId ?? "∅"); }
    }

    internal sealed class DistributedReplica
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public string ShardId { get; set; }
        public string NodeId { get; set; }
        public string ProcessId { get; set; }
        public string Role { get; set; }
        public long Term { get; set; }
        public long LogIndex { get; set; }
        public long CommitIndex { get; set; }
        public long AppliedIndex { get; set; }
        public long Version { get; set; }
        public string StateHash { get; set; }
        public string VectorClock { get; set; }
        public double DurableThroughMs { get; set; }
        public string Status { get; set; }
        public DistributedReplica() { Role = "FOLLOWER"; StateHash = "GENESIS"; VectorClock = ""; Status = "ONLINE"; }
        public override string ToString() { return (Status ?? "ONLINE") + "   ·   " + (Id ?? "REPLICA") + "   ·   " + (ShardId ?? "SHARD") + " @ " + (NodeId ?? "NODE") + "   ·   " + (Role ?? "FOLLOWER") + " term " + Term.ToString(CultureInfo.InvariantCulture) + "   ·   log/commit/apply " + LogIndex.ToString(CultureInfo.InvariantCulture) + "/" + CommitIndex.ToString(CultureInfo.InvariantCulture) + "/" + AppliedIndex.ToString(CultureInfo.InvariantCulture) + "   ·   v" + Version.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedWorkloadOperation
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string DatasetId { get; set; }
        public string ShardId { get; set; }
        public string Kind { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public double AtMs { get; set; }
        public double DeadlineMs { get; set; }
        public string Consistency { get; set; }
        public string CorrelationId { get; set; }
        public int RetryLimit { get; set; }
        public string Status { get; set; }
        public DistributedWorkloadOperation() { Kind = "READ"; DeadlineMs = 1000; Consistency = "DECLARED"; RetryLimit = 2; Status = "PLANNED"; }
        public override string ToString() { return (Status ?? "PLANNED") + "   ·   t+" + AtMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   " + (Kind ?? "READ") + " " + (DatasetId ?? "DATASET") + "/" + (Key ?? "KEY") + "   ·   client " + (ClientId ?? "CLIENT") + "   ·   deadline " + DeadlineMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms"; }
    }

    internal sealed class DistributedFault
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string TargetId { get; set; }
        public string SecondaryTargetId { get; set; }
        public double StartMs { get; set; }
        public double DurationMs { get; set; }
        public double Magnitude { get; set; }
        public bool Recoverable { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedFault() { Kind = "NODE_CRASH"; DurationMs = 10000; Magnitude = 1; Recoverable = true; Status = "ARMED"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ARMED") + "   ·   " + (Name ?? Id ?? "FAULT") + "   ·   " + (Kind ?? "NODE_CRASH") + " @ " + (TargetId ?? "TARGET") + (String.IsNullOrWhiteSpace(SecondaryTargetId) ? "" : " ↔ " + SecondaryTargetId) + "   ·   t[" + StartMs.ToString("0.###", CultureInfo.InvariantCulture) + "," + (StartMs + DurationMs).ToString("0.###", CultureInfo.InvariantCulture) + "]   ·   magnitude " + Magnitude.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Weight { get; set; }
        public double DurationMs { get; set; }
        public int MaxEvents { get; set; }
        public double TrafficMultiplier { get; set; }
        public double LatencyMultiplier { get; set; }
        public double LossAddition { get; set; }
        public double ClockSkewMultiplier { get; set; }
        public string EnabledFaultIds { get; set; }
        public string Status { get; set; }
        public List<string> Assumptions { get; set; }
        public DistributedScenario() { Weight = 1; DurationMs = 60000; MaxEvents = 50000; TrafficMultiplier = 1; LatencyMultiplier = 1; ClockSkewMultiplier = 1; Status = "ACTIVE"; Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SCENARIO") + "   ·   " + DurationMs.ToString("0", CultureInfo.InvariantCulture) + "ms / " + MaxEvents.ToString(CultureInfo.InvariantCulture) + " events   ·   traffic/latency/skew × " + TrafficMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + LatencyMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + ClockSkewMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   loss +" + LossAddition.ToString("P2", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedSystemModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Revision { get; set; }
        public int Seed { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string ProtocolFamily { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<DistributedFaultDomain> FaultDomains { get; set; }
        public List<DistributedNode> Nodes { get; set; }
        public List<DistributedProcess> Processes { get; set; }
        public List<DistributedLink> Links { get; set; }
        public List<DistributedDataset> Datasets { get; set; }
        public List<DistributedShard> Shards { get; set; }
        public List<DistributedReplica> Replicas { get; set; }
        public List<DistributedWorkloadOperation> Workload { get; set; }
        public List<DistributedFault> Faults { get; set; }
        public List<DistributedScenario> Scenarios { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public DistributedSystemModel() { Status = "ACTIVE"; Revision = 1; Seed = 1337; ProtocolFamily = "RAFT_LIKE"; Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); FaultDomains = new List<DistributedFaultDomain>(); Nodes = new List<DistributedNode>(); Processes = new List<DistributedProcess>(); Links = new List<DistributedLink>(); Datasets = new List<DistributedDataset>(); Shards = new List<DistributedShard>(); Replicas = new List<DistributedReplica>(); Workload = new List<DistributedWorkloadOperation>(); Faults = new List<DistributedFault>(); Scenarios = new List<DistributedScenario>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "DISTRIBUTED SYSTEM") + "   ·   " + Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes / " + Processes.Count.ToString(CultureInfo.InvariantCulture) + " processes / " + Links.Count.ToString(CultureInfo.InvariantCulture) + " links / " + Replicas.Count.ToString(CultureInfo.InvariantCulture) + " replicas   ·   " + (ProtocolFamily ?? "PROTOCOL") + "   ·   r" + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedValidationIssue
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string SubjectId { get; set; }
        public string Detail { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Code ?? "ISSUE") + "   ·   " + (SubjectId ?? "SYSTEM") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class DistributedQuorumAssessment
    {
        public string DatasetId { get; set; }
        public int ReplicationFactor { get; set; }
        public int ReadQuorum { get; set; }
        public int WriteQuorum { get; set; }
        public int VotingReplicas { get; set; }
        public int ToleratedCrashFaults { get; set; }
        public bool ReadWriteIntersection { get; set; }
        public bool WriteWriteIntersection { get; set; }
        public bool MajorityAvailable { get; set; }
        public int SurvivingFaultDomains { get; set; }
        public string Verdict { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + (DatasetId ?? "DATASET") + "   ·   N/R/W " + ReplicationFactor.ToString(CultureInfo.InvariantCulture) + "/" + ReadQuorum.ToString(CultureInfo.InvariantCulture) + "/" + WriteQuorum.ToString(CultureInfo.InvariantCulture) + "   ·   f=" + ToleratedCrashFaults.ToString(CultureInfo.InvariantCulture) + "   ·   RW/WW intersection " + ReadWriteIntersection + "/" + WriteWriteIntersection; }
    }

    internal sealed class DistributedTopologyResult
    {
        public int ActiveNodes { get; set; }
        public int ActiveLinks { get; set; }
        public int WeakComponents { get; set; }
        public int StrongComponents { get; set; }
        public double Density { get; set; }
        public List<string> ArticulationNodeIds { get; set; }
        public List<string> BridgeLinkIds { get; set; }
        public List<string> SingleRegionDatasetIds { get; set; }
        public List<string> SingleDomainDatasetIds { get; set; }
        public List<string> QuorumLossDomainIds { get; set; }
        public List<string> Audit { get; set; }
        public string CertificateHash { get; set; }
        public DistributedTopologyResult() { ArticulationNodeIds = new List<string>(); BridgeLinkIds = new List<string>(); SingleRegionDatasetIds = new List<string>(); SingleDomainDatasetIds = new List<string>(); QuorumLossDomainIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return ActiveNodes.ToString(CultureInfo.InvariantCulture) + " nodes / " + ActiveLinks.ToString(CultureInfo.InvariantCulture) + " links   ·   weak/strong " + WeakComponents.ToString(CultureInfo.InvariantCulture) + "/" + StrongComponents.ToString(CultureInfo.InvariantCulture) + "   ·   articulation/bridges " + ArticulationNodeIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + BridgeLinkIds.Count.ToString(CultureInfo.InvariantCulture) + "   ·   quorum-risk domains " + QuorumLossDomainIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedClockStamp
    {
        public string ActorId { get; set; }
        public long Lamport { get; set; }
        public string VectorClock { get; set; }
        public double PhysicalMs { get; set; }
        public double ObservedMs { get; set; }
        public override string ToString() { return (ActorId ?? "ACTOR") + "   ·   L=" + Lamport.ToString(CultureInfo.InvariantCulture) + "   ·   V={" + (VectorClock ?? "") + "}   ·   physical/observed " + PhysicalMs.ToString("0.###", CultureInfo.InvariantCulture) + "/" + ObservedMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms"; }
    }

    internal sealed class DistributedTraceEvent
    {
        public long Sequence { get; set; }
        public double AtMs { get; set; }
        public string Kind { get; set; }
        public string ActorId { get; set; }
        public string PeerId { get; set; }
        public string SubjectId { get; set; }
        public string CorrelationId { get; set; }
        public long Term { get; set; }
        public long LogIndex { get; set; }
        public string Detail { get; set; }
        public string Outcome { get; set; }
        public long Lamport { get; set; }
        public string VectorClock { get; set; }
        public string EventHash { get; set; }
        public override string ToString() { return Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(6) + "   ·   t+" + AtMs.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(10) + "ms   ·   " + (Kind ?? "EVENT") + "   ·   " + (ActorId ?? "∅") + (String.IsNullOrWhiteSpace(PeerId) ? "" : " → " + PeerId) + "   ·   term/index " + Term.ToString(CultureInfo.InvariantCulture) + "/" + LogIndex.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Outcome ?? "") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class DistributedInvariantFinding
    {
        public string Severity { get; set; }
        public string Invariant { get; set; }
        public string SubjectId { get; set; }
        public double AtMs { get; set; }
        public string Detail { get; set; }
        public string WitnessHash { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Invariant ?? "INVARIANT") + "   ·   " + (SubjectId ?? "SYSTEM") + " @ " + AtMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   " + (Detail ?? "") + "   ·   " + Short(WitnessHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(16, x.Length)); }
    }

    internal sealed class DistributedConsistencyObservation
    {
        public string OperationId { get; set; }
        public string ClientId { get; set; }
        public string DatasetId { get; set; }
        public string Key { get; set; }
        public string Kind { get; set; }
        public long ObservedVersion { get; set; }
        public long RequiredVersion { get; set; }
        public double StartedMs { get; set; }
        public double CompletedMs { get; set; }
        public string Verdict { get; set; }
        public string Anomaly { get; set; }
        public override string ToString() { return (Verdict ?? "UNKNOWN") + "   ·   " + (OperationId ?? "OP") + "   ·   " + (Kind ?? "READ") + " " + (DatasetId ?? "DATASET") + "/" + (Key ?? "KEY") + "   ·   observed/required v" + ObservedVersion.ToString(CultureInfo.InvariantCulture) + "/v" + RequiredVersion.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Anomaly ?? "NONE"); }
    }

    internal sealed class DistributedConsistencyReport
    {
        public string DatasetId { get; set; }
        public int Reads { get; set; }
        public int Writes { get; set; }
        public int AcknowledgedWrites { get; set; }
        public int StaleReads { get; set; }
        public int MonotonicReadViolations { get; set; }
        public int ReadYourWritesViolations { get; set; }
        public int LostAcknowledgedWrites { get; set; }
        public int ConcurrentVersions { get; set; }
        public List<DistributedConsistencyObservation> Observations { get; set; }
        public List<string> Audit { get; set; }
        public string Verdict { get; set; }
        public string CertificateHash { get; set; }
        public DistributedConsistencyReport() { Observations = new List<DistributedConsistencyObservation>(); Audit = new List<string>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + (DatasetId ?? "ALL DATASETS") + "   ·   R/W/ACK " + Reads.ToString(CultureInfo.InvariantCulture) + "/" + Writes.ToString(CultureInfo.InvariantCulture) + "/" + AcknowledgedWrites.ToString(CultureInfo.InvariantCulture) + "   ·   stale/monotonic/RYW/lost " + StaleReads.ToString(CultureInfo.InvariantCulture) + "/" + MonotonicReadViolations.ToString(CultureInfo.InvariantCulture) + "/" + ReadYourWritesViolations.ToString(CultureInfo.InvariantCulture) + "/" + LostAcknowledgedWrites.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedSimulationResult
    {
        public string ModelId { get; set; }
        public string ScenarioId { get; set; }
        public string ModelFingerprint { get; set; }
        public double RequestedDurationMs { get; set; }
        public double SimulatedThroughMs { get; set; }
        public int EventLimit { get; set; }
        public int EventsProcessed { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public int MessagesDropped { get; set; }
        public int MessagesDuplicated { get; set; }
        public int Elections { get; set; }
        public int LeaderChanges { get; set; }
        public int SplitVotes { get; set; }
        public double LeaderlessMs { get; set; }
        public int OperationsStarted { get; set; }
        public int OperationsCompleted { get; set; }
        public int OperationsTimedOut { get; set; }
        public int AcknowledgedWrites { get; set; }
        public int MaximumReplicaLag { get; set; }
        public double Availability { get; set; }
        public double MeanLatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public double RecoveryTimeMs { get; set; }
        public double DataLossWindowMs { get; set; }
        public bool Truncated { get; set; }
        public List<DistributedTraceEvent> Trace { get; set; }
        public List<DistributedInvariantFinding> Invariants { get; set; }
        public List<DistributedConsistencyReport> Consistency { get; set; }
        public List<DistributedProcess> FinalProcesses { get; set; }
        public List<DistributedReplica> FinalReplicas { get; set; }
        public List<string> Audit { get; set; }
        public string TraceRootHash { get; set; }
        public string CertificateHash { get; set; }
        public DistributedSimulationResult() { Trace = new List<DistributedTraceEvent>(); Invariants = new List<DistributedInvariantFinding>(); Consistency = new List<DistributedConsistencyReport>(); FinalProcesses = new List<DistributedProcess>(); FinalReplicas = new List<DistributedReplica>(); Audit = new List<string>(); }
        public override string ToString() { return (Truncated ? "TRUNCATED" : "COMPLETE") + "   ·   " + (ScenarioId ?? "BASE") + "   ·   " + EventsProcessed.ToString(CultureInfo.InvariantCulture) + " events / " + MessagesDelivered.ToString(CultureInfo.InvariantCulture) + " delivered / " + MessagesDropped.ToString(CultureInfo.InvariantCulture) + " dropped   ·   availability " + Availability.ToString("P3", CultureInfo.InvariantCulture) + "   ·   p95 " + P95LatencyMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   elections/leaders " + Elections.ToString(CultureInfo.InvariantCulture) + "/" + LeaderChanges.ToString(CultureInfo.InvariantCulture) + "   ·   invariant findings " + Invariants.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedGossipRound
    {
        public int Round { get; set; }
        public int Messages { get; set; }
        public int DistinctViews { get; set; }
        public double KnowledgeRatio { get; set; }
        public string StateHash { get; set; }
        public override string ToString() { return "round " + Round.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   messages " + Messages.ToString(CultureInfo.InvariantCulture) + "   ·   distinct views " + DistinctViews.ToString(CultureInfo.InvariantCulture) + "   ·   knowledge " + KnowledgeRatio.ToString("P2", CultureInfo.InvariantCulture) + "   ·   " + (StateHash ?? "∅"); }
    }

    internal sealed class DistributedGossipResult
    {
        public string ScenarioId { get; set; }
        public int Members { get; set; }
        public int RequestedRounds { get; set; }
        public int ConvergedRound { get; set; }
        public int TotalMessages { get; set; }
        public double FinalKnowledgeRatio { get; set; }
        public List<DistributedGossipRound> Rounds { get; set; }
        public List<string> IsolatedMemberIds { get; set; }
        public string CertificateHash { get; set; }
        public DistributedGossipResult() { Rounds = new List<DistributedGossipRound>(); IsolatedMemberIds = new List<string>(); }
        public override string ToString() { return Members.ToString(CultureInfo.InvariantCulture) + " members   ·   " + RequestedRounds.ToString(CultureInfo.InvariantCulture) + " rounds   ·   converged " + (ConvergedRound < 0 ? "NO" : "@" + ConvergedRound.ToString(CultureInfo.InvariantCulture)) + "   ·   messages " + TotalMessages.ToString(CultureInfo.InvariantCulture) + "   ·   knowledge " + FinalKnowledgeRatio.ToString("P2", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedTransactionStep
    {
        public int Sequence { get; set; }
        public double AtMs { get; set; }
        public string ParticipantId { get; set; }
        public string Phase { get; set; }
        public string Vote { get; set; }
        public string State { get; set; }
        public string Detail { get; set; }
        public override string ToString() { return Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   t+" + AtMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   " + (ParticipantId ?? "COORDINATOR") + "   ·   " + (Phase ?? "PHASE") + " / " + (Vote ?? "−") + " / " + (State ?? "STATE") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class DistributedTransactionResult
    {
        public string TransactionId { get; set; }
        public string ScenarioId { get; set; }
        public string CoordinatorId { get; set; }
        public string Outcome { get; set; }
        public int Participants { get; set; }
        public int Prepared { get; set; }
        public int InDoubt { get; set; }
        public int HeuristicOutcomes { get; set; }
        public double CompletedMs { get; set; }
        public List<DistributedTransactionStep> Steps { get; set; }
        public List<string> WaitForEdges { get; set; }
        public List<string> DeadlockCycles { get; set; }
        public string CertificateHash { get; set; }
        public DistributedTransactionResult() { Steps = new List<DistributedTransactionStep>(); WaitForEdges = new List<string>(); DeadlockCycles = new List<string>(); }
        public override string ToString() { return (Outcome ?? "UNKNOWN") + "   ·   " + (TransactionId ?? "TX") + "   ·   participants/prepared/in-doubt " + Participants.ToString(CultureInfo.InvariantCulture) + "/" + Prepared.ToString(CultureInfo.InvariantCulture) + "/" + InDoubt.ToString(CultureInfo.InvariantCulture) + "   ·   deadlocks " + DeadlockCycles.Count.ToString(CultureInfo.InvariantCulture) + "   ·   t=" + CompletedMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms"; }
    }

    internal sealed class DistributedLeaseResult
    {
        public string ResourceId { get; set; }
        public string HolderId { get; set; }
        public long FencingToken { get; set; }
        public double GrantedAtMs { get; set; }
        public double ExpiresAtMs { get; set; }
        public double WorstClockErrorMs { get; set; }
        public double SafeUsableThroughMs { get; set; }
        public bool OverlapRisk { get; set; }
        public string Verdict { get; set; }
        public List<string> Audit { get; set; }
        public string CertificateHash { get; set; }
        public DistributedLeaseResult() { Audit = new List<string>(); }
        public override string ToString() { return (Verdict ?? "UNASSESSED") + "   ·   " + (ResourceId ?? "RESOURCE") + " → " + (HolderId ?? "HOLDER") + "   ·   fence " + FencingToken.ToString(CultureInfo.InvariantCulture) + "   ·   lease [" + GrantedAtMs.ToString("0.###", CultureInfo.InvariantCulture) + "," + ExpiresAtMs.ToString("0.###", CultureInfo.InvariantCulture) + "]   ·   safe through " + SafeUsableThroughMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms"; }
    }

    internal sealed class DistributedFailureDetectorRecord
    {
        public string ObserverId { get; set; }
        public string SubjectId { get; set; }
        public double MeanHeartbeatMs { get; set; }
        public double SilenceMs { get; set; }
        public double Phi { get; set; }
        public string Verdict { get; set; }
        public override string ToString() { return (Verdict ?? "ALIVE") + "   ·   " + (ObserverId ?? "OBSERVER") + " → " + (SubjectId ?? "SUBJECT") + "   ·   mean/silence " + MeanHeartbeatMs.ToString("0.###", CultureInfo.InvariantCulture) + "/" + SilenceMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms   ·   φ=" + Phi.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedChaosWorldResult
    {
        public string ScenarioId { get; set; }
        public double Weight { get; set; }
        public double Availability { get; set; }
        public double P95LatencyMs { get; set; }
        public double RecoveryTimeMs { get; set; }
        public double LeaderlessMs { get; set; }
        public double DataLossWindowMs { get; set; }
        public int InvariantViolations { get; set; }
        public int ConsistencyAnomalies { get; set; }
        public int QuorumLosses { get; set; }
        public string ResilienceClass { get; set; }
        public string SimulationHash { get; set; }
        public override string ToString() { return (ResilienceClass ?? "UNASSESSED") + "   ·   " + (ScenarioId ?? "SCENARIO") + "   ·   availability " + Availability.ToString("P3", CultureInfo.InvariantCulture) + "   ·   p95/recovery/leaderless " + P95LatencyMs.ToString("0.##", CultureInfo.InvariantCulture) + "/" + RecoveryTimeMs.ToString("0.##", CultureInfo.InvariantCulture) + "/" + LeaderlessMs.ToString("0.##", CultureInfo.InvariantCulture) + "ms   ·   invariant/anomaly/quorum " + InvariantViolations.ToString(CultureInfo.InvariantCulture) + "/" + ConsistencyAnomalies.ToString(CultureInfo.InvariantCulture) + "/" + QuorumLosses.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class DistributedChaosCampaignResult
    {
        public int RequestedWorlds { get; set; }
        public int CompletedWorlds { get; set; }
        public double WeightedAvailability { get; set; }
        public double WorstRecoveryTimeMs { get; set; }
        public string WorstScenarioId { get; set; }
        public List<DistributedChaosWorldResult> Worlds { get; set; }
        public List<string> Recommendations { get; set; }
        public string CertificateHash { get; set; }
        public DistributedChaosCampaignResult() { Worlds = new List<DistributedChaosWorldResult>(); Recommendations = new List<string>(); }
        public override string ToString() { return CompletedWorlds.ToString(CultureInfo.InvariantCulture) + "/" + RequestedWorlds.ToString(CultureInfo.InvariantCulture) + " worlds   ·   weighted availability " + WeightedAvailability.ToString("P3", CultureInfo.InvariantCulture) + "   ·   worst recovery " + WorstRecoveryTimeMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms @ " + (WorstScenarioId ?? "∅") + "   ·   recommendations " + Recommendations.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal static class DistributedSystemsKernel
    {
        private const int MaxNodes = 2048;
        private const int MaxProcesses = 8192;
        private const int MaxLinks = 32768;
        private const int MaxReplicas = 16384;
        private const int MaxWorkload = 100000;
        private const int AbsoluteEventCap = 250000;

        private sealed class RuntimeProcess
        {
            public DistributedProcess Source;
            public string Role;
            public long Term;
            public long LogIndex;
            public long CommitIndex;
            public long AppliedIndex;
            public string VotedFor;
            public bool Up;
            public double ElectionDeadline;
            public double LastHeartbeat;
            public HashSet<string> Votes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<long, HashSet<string>> Acks = new Dictionary<long, HashSet<string>>();
        }

        private sealed class RuntimeReplica
        {
            public DistributedReplica Source;
            public long Version;
            public long LogIndex;
            public long CommitIndex;
            public long AppliedIndex;
            public string StateHash;
            public string VectorClock;
            public bool Online;
        }

        private sealed class ScheduledEvent
        {
            public long Sequence;
            public double AtMs;
            public string Kind;
            public string ActorId;
            public string PeerId;
            public string SubjectId;
            public string CorrelationId;
            public long Term;
            public long LogIndex;
            public string Payload;
            public int Attempt;
        }

        private sealed class OperationState
        {
            public DistributedWorkloadOperation Source;
            public double Started;
            public double Completed;
            public long RequiredVersion;
            public long ObservedVersion;
            public string Outcome;
            public string ProcessId;
        }

        private sealed class Runtime
        {
            public DistributedSystemModel Model;
            public DistributedScenario Scenario;
            public DistributedSimulationResult Result;
            public Dictionary<string, DistributedNode> Nodes = new Dictionary<string, DistributedNode>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, RuntimeProcess> Processes = new Dictionary<string, RuntimeProcess>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, RuntimeReplica> Replicas = new Dictionary<string, RuntimeReplica>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, OperationState> Operations = new Dictionary<string, OperationState>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, long> Lamport = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Dictionary<string, long>> Vectors = new Dictionary<string, Dictionary<string, long>>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DownNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DownProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DownLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> ActivePartitions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> LatencyFaults = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> LossFaults = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public List<ScheduledEvent> Queue = new List<ScheduledEvent>();
            public List<double> Latencies = new List<double>();
            public Dictionary<string, double> LastClientVersion = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> LatestLeaderByGroup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public long QueueSequence;
            public long TraceSequence;
            public double Now;
            public double LastLeaderPresentAt;
            public double LastFaultStartedAt = -1;
            public double LastRecoveryAt = -1;
            public string PriorEventHash = new string('0', 64);
        }

        public static string Fingerprint(DistributedSystemModel model)
        {
            if (model == null) return HashText("NULL_DISTRIBUTED_SYSTEM");
            StringBuilder b = new StringBuilder();
            b.Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.Name).Append('|').Append(model.Revision).Append('|').Append(model.Seed).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.ProtocolFamily);
            foreach (KeyValuePair<string, double> p in (model.Parameters ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) b.Append("|P:").Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture));
            foreach (DistributedFaultDomain x in (model.FaultDomains ?? new List<DistributedFaultDomain>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|FDM:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.ParentId).Append(':').Append(x.Region).Append(':').Append(x.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Correlation.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (DistributedNode x in (model.Nodes ?? new List<DistributedNode>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|N:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.FaultDomainId).Append(':').Append(x.Region).Append(':').Append(x.Zone).Append(':').Append(x.Rack).Append(':').Append(x.ClockOffsetMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ClockDriftPpm.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (DistributedProcess x in (model.Processes ?? new List<DistributedProcess>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|PROC:").Append(x.Id).Append(':').Append(x.Service).Append(':').Append(x.NodeId).Append(':').Append(x.GroupId).Append(':').Append(x.Role).Append(':').Append(x.Term).Append(':').Append(x.LastLogIndex).Append(':').Append(x.CommitIndex).Append(':').Append(x.AppliedIndex).Append(':').Append(x.Voting).Append(':').Append(x.Health);
            foreach (DistributedLink x in (model.Links ?? new List<DistributedLink>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|L:").Append(x.Id).Append(':').Append(x.FromNodeId).Append('>').Append(x.ToNodeId).Append(':').Append(x.Bidirectional).Append(':').Append(x.BaseLatencyMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.JitterMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.BandwidthMbps.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.LossProbability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.DuplicationProbability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ReorderProbability.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (DistributedDataset x in (model.Datasets ?? new List<DistributedDataset>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|D:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.ConsistencyModel).Append(':').Append(x.ReplicationFactor).Append(':').Append(x.ReadQuorum).Append(':').Append(x.WriteQuorum).Append(':').Append(x.ShardCount).Append(':').Append(x.SynchronousCommit).Append(':').Append(x.Status);
            foreach (DistributedShard x in (model.Shards ?? new List<DistributedShard>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|S:").Append(x.Id).Append(':').Append(x.DatasetId).Append(':').Append(x.KeyRangeStart).Append(':').Append(x.KeyRangeEnd).Append(':').Append(x.LeaderReplicaId).Append(':').Append(x.Epoch).Append(':').Append(x.Status);
            foreach (DistributedReplica x in (model.Replicas ?? new List<DistributedReplica>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|R:").Append(x.Id).Append(':').Append(x.DatasetId).Append(':').Append(x.ShardId).Append(':').Append(x.NodeId).Append(':').Append(x.ProcessId).Append(':').Append(x.Role).Append(':').Append(x.Term).Append(':').Append(x.LogIndex).Append(':').Append(x.CommitIndex).Append(':').Append(x.AppliedIndex).Append(':').Append(x.Version).Append(':').Append(x.StateHash).Append(':').Append(x.VectorClock).Append(':').Append(x.Status);
            foreach (DistributedWorkloadOperation x in (model.Workload ?? new List<DistributedWorkloadOperation>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|W:").Append(x.Id).Append(':').Append(x.ClientId).Append(':').Append(x.DatasetId).Append(':').Append(x.ShardId).Append(':').Append(x.Kind).Append(':').Append(x.Key).Append(':').Append(x.Value).Append(':').Append(x.AtMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.DeadlineMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Consistency).Append(':').Append(x.RetryLimit);
            foreach (DistributedFault x in (model.Faults ?? new List<DistributedFault>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|F:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.TargetId).Append(':').Append(x.SecondaryTargetId).Append(':').Append(x.StartMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.DurationMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Magnitude.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Recoverable).Append(':').Append(x.Status);
            foreach (DistributedScenario x in (model.Scenarios ?? new List<DistributedScenario>()).OrderBy(x => x == null ? "" : x.Id)) if (x != null) b.Append("|SC:").Append(x.Id).Append(':').Append(x.Weight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.DurationMs.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.MaxEvents).Append(':').Append(x.TrafficMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.LatencyMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.LossAddition.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ClockSkewMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.EnabledFaultIds).Append(':').Append(x.Status);
            foreach (string x in (model.Assumptions ?? new List<string>()).OrderBy(x => x)) b.Append("|A:").Append(x);
            foreach (string x in (model.EvidenceNodeIds ?? new List<string>()).OrderBy(x => x)) b.Append("|E:").Append(x);
            return HashText(b.ToString());
        }

        public static List<DistributedValidationIssue> Validate(DistributedSystemModel model)
        {
            List<DistributedValidationIssue> issues = new List<DistributedValidationIssue>();
            if (model == null) { Issue(issues, "FATAL", "NULL_MODEL", "SYSTEM", "Distributed system model is null."); return issues; }
            CheckCap(issues, "NODE_CAP", model.Nodes, MaxNodes); CheckCap(issues, "PROCESS_CAP", model.Processes, MaxProcesses); CheckCap(issues, "LINK_CAP", model.Links, MaxLinks); CheckCap(issues, "REPLICA_CAP", model.Replicas, MaxReplicas); CheckCap(issues, "WORKLOAD_CAP", model.Workload, MaxWorkload);
            CheckIds(issues, "FAULT_DOMAIN", (model.FaultDomains ?? new List<DistributedFaultDomain>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "NODE", (model.Nodes ?? new List<DistributedNode>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "PROCESS", (model.Processes ?? new List<DistributedProcess>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "LINK", (model.Links ?? new List<DistributedLink>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "DATASET", (model.Datasets ?? new List<DistributedDataset>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "SHARD", (model.Shards ?? new List<DistributedShard>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "REPLICA", (model.Replicas ?? new List<DistributedReplica>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "WORKLOAD", (model.Workload ?? new List<DistributedWorkloadOperation>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "FAULT", (model.Faults ?? new List<DistributedFault>()).Where(x => x != null).Select(x => x.Id));
            CheckIds(issues, "SCENARIO", (model.Scenarios ?? new List<DistributedScenario>()).Where(x => x != null).Select(x => x.Id));
            HashSet<string> domains = IdSet((model.FaultDomains ?? new List<DistributedFaultDomain>()).Select(x => x == null ? null : x.Id));
            HashSet<string> nodes = IdSet((model.Nodes ?? new List<DistributedNode>()).Select(x => x == null ? null : x.Id));
            HashSet<string> processes = IdSet((model.Processes ?? new List<DistributedProcess>()).Select(x => x == null ? null : x.Id));
            HashSet<string> datasets = IdSet((model.Datasets ?? new List<DistributedDataset>()).Select(x => x == null ? null : x.Id));
            HashSet<string> shards = IdSet((model.Shards ?? new List<DistributedShard>()).Select(x => x == null ? null : x.Id));
            foreach (DistributedFaultDomain x in model.FaultDomains ?? new List<DistributedFaultDomain>()) if (x != null && !String.IsNullOrWhiteSpace(x.ParentId) && !domains.Contains(x.ParentId)) Issue(issues, "ERROR", "UNKNOWN_PARENT_DOMAIN", x.Id, x.ParentId);
            DetectParentCycles((model.FaultDomains ?? new List<DistributedFaultDomain>()).Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).ToDictionary(x => x.Id, x => x.ParentId ?? "", StringComparer.OrdinalIgnoreCase), issues);
            foreach (DistributedNode x in model.Nodes ?? new List<DistributedNode>()) if (x != null) { if (!String.IsNullOrWhiteSpace(x.FaultDomainId) && !domains.Contains(x.FaultDomainId)) Issue(issues, "ERROR", "UNKNOWN_FAULT_DOMAIN", x.Id, x.FaultDomainId); if (!Finite(x.ClockOffsetMs) || !Finite(x.ClockDriftPpm)) Issue(issues, "ERROR", "NONFINITE_CLOCK", x.Id, "Clock offset and drift must be finite."); if (x.CpuCores < 0 || x.MemoryGb < 0 || x.DiskGb < 0) Issue(issues, "ERROR", "NEGATIVE_CAPACITY", x.Id, "Node capacity cannot be negative."); }
            foreach (DistributedProcess x in model.Processes ?? new List<DistributedProcess>()) if (x != null) { if (!nodes.Contains(x.NodeId ?? "")) Issue(issues, "ERROR", "UNKNOWN_PROCESS_NODE", x.Id, x.NodeId); if (x.AppliedIndex > x.CommitIndex || x.CommitIndex > x.LastLogIndex) Issue(issues, "ERROR", "LOG_INDEX_ORDER", x.Id, "Required applied ≤ commit ≤ last log."); }
            foreach (DistributedLink x in model.Links ?? new List<DistributedLink>()) if (x != null) { if (!nodes.Contains(x.FromNodeId ?? "") || !nodes.Contains(x.ToNodeId ?? "")) Issue(issues, "ERROR", "UNKNOWN_LINK_ENDPOINT", x.Id, (x.FromNodeId ?? "") + " → " + (x.ToNodeId ?? "")); if (Eq(x.FromNodeId, x.ToNodeId)) Issue(issues, "WARN", "SELF_LINK", x.Id, "Self-link contributes no inter-node reachability."); if (!Probability(x.LossProbability) || !Probability(x.DuplicationProbability) || !Probability(x.ReorderProbability) || !Probability(x.Reliability)) Issue(issues, "ERROR", "INVALID_PROBABILITY", x.Id, "Network probabilities must be in [0,1]."); if (x.BaseLatencyMs < 0 || x.JitterMs < 0 || x.BandwidthMbps <= 0) Issue(issues, "ERROR", "INVALID_NETWORK_METRIC", x.Id, "Latency/jitter must be nonnegative and bandwidth positive."); }
            foreach (DistributedDataset x in model.Datasets ?? new List<DistributedDataset>()) if (x != null) { if (x.ReplicationFactor < 1 || x.ReadQuorum < 1 || x.WriteQuorum < 1 || x.ReadQuorum > x.ReplicationFactor || x.WriteQuorum > x.ReplicationFactor) Issue(issues, "ERROR", "INVALID_QUORUM", x.Id, "Require 1 ≤ R,W ≤ N."); if (x.ShardCount < 1) Issue(issues, "ERROR", "INVALID_SHARD_COUNT", x.Id, "Shard count must be positive."); int actual = (model.Shards ?? new List<DistributedShard>()).Count(s => s != null && Eq(s.DatasetId, x.Id)); if (actual != x.ShardCount) Issue(issues, "WARN", "SHARD_COUNT_DRIFT", x.Id, "Declared " + x.ShardCount + ", retained " + actual + "."); }
            foreach (DistributedShard x in model.Shards ?? new List<DistributedShard>()) if (x != null && !datasets.Contains(x.DatasetId ?? "")) Issue(issues, "ERROR", "UNKNOWN_SHARD_DATASET", x.Id, x.DatasetId);
            foreach (DistributedReplica x in model.Replicas ?? new List<DistributedReplica>()) if (x != null) { if (!datasets.Contains(x.DatasetId ?? "") || !shards.Contains(x.ShardId ?? "") || !nodes.Contains(x.NodeId ?? "") || !processes.Contains(x.ProcessId ?? "")) Issue(issues, "ERROR", "ORPHAN_REPLICA", x.Id, "Dataset/shard/node/process reference is unresolved."); if (x.AppliedIndex > x.CommitIndex || x.CommitIndex > x.LogIndex) Issue(issues, "ERROR", "REPLICA_INDEX_ORDER", x.Id, "Required applied ≤ commit ≤ log."); }
            foreach (DistributedWorkloadOperation x in model.Workload ?? new List<DistributedWorkloadOperation>()) if (x != null) { if (!datasets.Contains(x.DatasetId ?? "")) Issue(issues, "ERROR", "UNKNOWN_OPERATION_DATASET", x.Id, x.DatasetId); if (!String.IsNullOrWhiteSpace(x.ShardId) && !shards.Contains(x.ShardId)) Issue(issues, "ERROR", "UNKNOWN_OPERATION_SHARD", x.Id, x.ShardId); if (x.AtMs < 0 || x.DeadlineMs <= 0) Issue(issues, "ERROR", "INVALID_OPERATION_TIME", x.Id, "Operation start must be nonnegative and deadline positive."); }
            foreach (DistributedFault x in model.Faults ?? new List<DistributedFault>()) if (x != null && (x.StartMs < 0 || x.DurationMs < 0 || !Finite(x.Magnitude))) Issue(issues, "ERROR", "INVALID_FAULT_WINDOW", x.Id, "Fault start/duration/magnitude must be finite and nonnegative where applicable.");
            foreach (DistributedScenario x in model.Scenarios ?? new List<DistributedScenario>()) if (x != null && (x.DurationMs <= 0 || x.MaxEvents < 1 || x.MaxEvents > AbsoluteEventCap || x.TrafficMultiplier < 0 || x.LatencyMultiplier < 0 || !Finite(x.LossAddition))) Issue(issues, "ERROR", "INVALID_SCENARIO_BOUND", x.Id, "Scenario horizon, event bound and multipliers are invalid.");
            foreach (IGrouping<string, DistributedReplica> group in (model.Replicas ?? new List<DistributedReplica>()).Where(x => x != null).GroupBy(x => x.ShardId ?? "", StringComparer.OrdinalIgnoreCase)) { DistributedShard shard = (model.Shards ?? new List<DistributedShard>()).FirstOrDefault(x => x != null && Eq(x.Id, group.Key)); DistributedDataset dataset = shard == null ? null : (model.Datasets ?? new List<DistributedDataset>()).FirstOrDefault(x => x != null && Eq(x.Id, shard.DatasetId)); if (dataset != null && group.Count() < dataset.ReplicationFactor) Issue(issues, "WARN", "UNDER_REPLICATED_SHARD", group.Key, group.Count() + " < N=" + dataset.ReplicationFactor); if (group.Select(x => x.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != group.Count()) Issue(issues, "WARN", "COLOCATED_REPLICAS", group.Key, "Multiple replicas occupy one node."); }
            return issues;
        }

        public static List<DistributedQuorumAssessment> AssessQuorums(DistributedSystemModel model, DistributedScenario scenario)
        {
            List<DistributedQuorumAssessment> rows = new List<DistributedQuorumAssessment>();
            if (model == null) return rows;
            HashSet<string> unavailable = UnavailableNodes(model, scenario);
            foreach (DistributedDataset dataset in model.Datasets ?? new List<DistributedDataset>())
            {
                if (dataset == null || !Active(dataset.Status)) continue;
                List<DistributedReplica> replicas = (model.Replicas ?? new List<DistributedReplica>()).Where(x => x != null && Eq(x.DatasetId, dataset.Id) && Active(x.Status)).ToList();
                int voting = replicas.Count(x => !unavailable.Contains(x.NodeId ?? ""));
                HashSet<string> domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DistributedReplica r in replicas.Where(x => !unavailable.Contains(x.NodeId ?? ""))) { DistributedNode n = (model.Nodes ?? new List<DistributedNode>()).FirstOrDefault(x => x != null && Eq(x.Id, r.NodeId)); if (n != null) domains.Add(n.FaultDomainId ?? n.Zone ?? n.Region ?? n.Id); }
                DistributedQuorumAssessment q = new DistributedQuorumAssessment { DatasetId = dataset.Id, ReplicationFactor = dataset.ReplicationFactor, ReadQuorum = dataset.ReadQuorum, WriteQuorum = dataset.WriteQuorum, VotingReplicas = voting, ToleratedCrashFaults = Math.Max(0, dataset.ReplicationFactor - dataset.WriteQuorum), ReadWriteIntersection = dataset.ReadQuorum + dataset.WriteQuorum > dataset.ReplicationFactor, WriteWriteIntersection = dataset.WriteQuorum * 2 > dataset.ReplicationFactor, MajorityAvailable = voting >= dataset.WriteQuorum, SurvivingFaultDomains = domains.Count };
                q.Verdict = !q.MajorityAvailable ? "UNAVAILABLE" : (!q.ReadWriteIntersection || !q.WriteWriteIntersection ? "ANOMALY_RISK" : domains.Count < 2 ? "CORRELATED_FAILURE_RISK" : "QUORUM_SAFE");
                q.CertificateHash = HashText(q.DatasetId + "|" + q.ReplicationFactor + "|" + q.ReadQuorum + "|" + q.WriteQuorum + "|" + q.VotingReplicas + "|" + q.ReadWriteIntersection + "|" + q.WriteWriteIntersection + "|" + q.SurvivingFaultDomains + "|" + q.Verdict);
                rows.Add(q);
            }
            return rows;
        }

        public static DistributedTopologyResult AnalyzeTopology(DistributedSystemModel model)
        {
            DistributedTopologyResult result = new DistributedTopologyResult();
            if (model == null) return SealTopology(result);
            List<string> nodes = (model.Nodes ?? new List<DistributedNode>()).Where(x => x != null && Active(x.Status)).Select(x => x.Id).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxNodes).ToList();
            List<DistributedLink> links = (model.Links ?? new List<DistributedLink>()).Where(x => x != null && Active(x.Status) && nodes.Contains(x.FromNodeId, StringComparer.OrdinalIgnoreCase) && nodes.Contains(x.ToNodeId, StringComparer.OrdinalIgnoreCase)).Take(MaxLinks).ToList();
            result.ActiveNodes = nodes.Count; result.ActiveLinks = links.Count; result.Density = nodes.Count < 2 ? 0 : links.Sum(x => x.Bidirectional ? 2 : 1) / (double)(nodes.Count * (nodes.Count - 1));
            Dictionary<string, List<string>> directed = nodes.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, HashSet<string>> undirected = nodes.ToDictionary(x => x, x => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            foreach (DistributedLink link in links) { directed[link.FromNodeId].Add(link.ToNodeId); undirected[link.FromNodeId].Add(link.ToNodeId); undirected[link.ToNodeId].Add(link.FromNodeId); if (link.Bidirectional) directed[link.ToNodeId].Add(link.FromNodeId); }
            result.WeakComponents = Components(nodes, undirected); result.StrongComponents = StrongComponents(nodes, directed).Count;
            Dictionary<string, int> discovery = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); int time = 0;
            foreach (string node in nodes) if (!discovery.ContainsKey(node)) ArticulationDfs(node, null, undirected, links, discovery, low, parent, ref time, result.ArticulationNodeIds, result.BridgeLinkIds);
            result.ArticulationNodeIds = result.ArticulationNodeIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(); result.BridgeLinkIds = result.BridgeLinkIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            foreach (DistributedDataset dataset in model.Datasets ?? new List<DistributedDataset>())
            {
                if (dataset == null) continue; List<DistributedNode> homes = (from r in model.Replicas ?? new List<DistributedReplica>() join n in model.Nodes ?? new List<DistributedNode>() on r.NodeId equals n.Id where Eq(r.DatasetId, dataset.Id) select n).ToList();
                if (homes.Select(x => x.Region ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2) result.SingleRegionDatasetIds.Add(dataset.Id);
                if (homes.Select(x => x.FaultDomainId ?? x.Zone ?? x.Region ?? x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2) result.SingleDomainDatasetIds.Add(dataset.Id);
            }
            foreach (DistributedFaultDomain domain in model.FaultDomains ?? new List<DistributedFaultDomain>())
            {
                if (domain == null) continue; HashSet<string> lostNodes = IdSet((model.Nodes ?? new List<DistributedNode>()).Where(x => x != null && Eq(x.FaultDomainId, domain.Id)).Select(x => x.Id)); bool loses = false;
                foreach (DistributedDataset dataset in model.Datasets ?? new List<DistributedDataset>()) if (dataset != null) { int survivors = (model.Replicas ?? new List<DistributedReplica>()).Count(x => x != null && Eq(x.DatasetId, dataset.Id) && !lostNodes.Contains(x.NodeId ?? "")); if (survivors < dataset.WriteQuorum) { loses = true; result.Audit.Add("Domain " + domain.Id + " removes quorum for " + dataset.Id + "."); } }
                if (loses) result.QuorumLossDomainIds.Add(domain.Id);
            }
            result.Audit.Add("Directed density=" + result.Density.ToString("0.######", CultureInfo.InvariantCulture) + "."); result.Audit.Add("Strong connectivity is required for symmetric request/response reachability; weak connectivity alone is insufficient.");
            return SealTopology(result);
        }

        public static DistributedSimulationResult Simulate(DistributedSystemModel model, DistributedScenario scenario, double durationMs, int eventLimit)
        {
            DistributedSimulationResult empty = new DistributedSimulationResult { ModelId = model == null ? null : model.Id, ScenarioId = scenario == null ? "BASE" : scenario.Id, RequestedDurationMs = Math.Max(1, durationMs), EventLimit = Math.Max(1, Math.Min(AbsoluteEventCap, eventLimit)) };
            if (model == null) { empty.Audit.Add("NULL MODEL"); return SealSimulation(empty); }
            Runtime rt = BuildRuntime(model, scenario, durationMs, eventLimit);
            ScheduleInitial(rt);
            while (rt.Queue.Count > 0 && rt.Result.EventsProcessed < rt.Result.EventLimit)
            {
                ScheduledEvent ev = Pop(rt.Queue); if (ev == null || ev.AtMs > rt.Result.RequestedDurationMs) break; rt.Now = ev.AtMs; rt.Result.SimulatedThroughMs = ev.AtMs; rt.Result.EventsProcessed++;
                ProcessEvent(rt, ev);
                MeasureLeaderless(rt);
            }
            rt.Result.Truncated = rt.Queue.Count > 0 && rt.Result.EventsProcessed >= rt.Result.EventLimit;
            if (!rt.Result.Truncated) rt.Result.SimulatedThroughMs = Math.Min(rt.Result.RequestedDurationMs, Math.Max(rt.Result.SimulatedThroughMs, rt.Result.RequestedDurationMs));
            FinalizeSimulation(rt);
            return SealSimulation(rt.Result);
        }

        public static DistributedGossipResult SimulateGossip(DistributedSystemModel model, DistributedScenario scenario, int rounds, int fanout)
        {
            DistributedGossipResult result = new DistributedGossipResult { ScenarioId = scenario == null ? "BASE" : scenario.Id, RequestedRounds = Math.Max(1, Math.Min(1000, rounds)), ConvergedRound = -1 };
            if (model == null) return SealGossip(result);
            List<string> members = (model.Processes ?? new List<DistributedProcess>()).Where(x => x != null && Active(x.Health)).Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxProcesses).ToList(); result.Members = members.Count;
            Dictionary<string, HashSet<string>> knowledge = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); foreach (string member in members) knowledge[member] = new HashSet<string>(new[] { member }, StringComparer.OrdinalIgnoreCase);
            fanout = Math.Max(1, Math.Min(Math.Max(1, members.Count - 1), fanout)); HashSet<string> unavailable = UnavailableProcesses(model, scenario);
            for (int round = 1; round <= result.RequestedRounds && members.Count > 0; round++)
            {
                Dictionary<string, HashSet<string>> next = knowledge.ToDictionary(x => x.Key, x => new HashSet<string>(x.Value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase); int messages = 0;
                foreach (string sender in members.Where(x => !unavailable.Contains(x)))
                {
                    List<string> peers = members.Where(x => !Eq(x, sender) && !unavailable.Contains(x)).OrderBy(x => StableUnit(model.Seed, "gossip|" + round + "|" + sender + "|" + x)).Take(fanout).ToList();
                    foreach (string peer in peers) { if (!CanProcessesCommunicate(model, scenario, sender, peer)) continue; next[peer].UnionWith(knowledge[sender]); next[sender].UnionWith(knowledge[peer]); messages++; }
                }
                knowledge = next; result.TotalMessages += messages; int distinct = knowledge.Values.Select(x => HashText(String.Join(",", x.OrderBy(v => v).ToArray()))).Distinct().Count(); double ratio = members.Count == 0 ? 1 : knowledge.Values.Average(x => x.Count / (double)members.Count); string state = HashText(String.Join("|", knowledge.OrderBy(x => x.Key).Select(x => x.Key + ":" + String.Join(",", x.Value.OrderBy(v => v).ToArray())).ToArray())); result.Rounds.Add(new DistributedGossipRound { Round = round, Messages = messages, DistinctViews = distinct, KnowledgeRatio = ratio, StateHash = state });
                if (ratio >= 0.999999 && distinct == 1) { result.ConvergedRound = round; break; }
            }
            result.FinalKnowledgeRatio = result.Rounds.Count == 0 ? 0 : result.Rounds[result.Rounds.Count - 1].KnowledgeRatio; result.IsolatedMemberIds.AddRange(members.Where(x => unavailable.Contains(x) || knowledge[x].Count <= 1).OrderBy(x => x)); return SealGossip(result);
        }

        public static DistributedTransactionResult SimulateTwoPhaseCommit(DistributedSystemModel model, DistributedScenario scenario, string coordinatorId, IEnumerable<string> participantIds, double timeoutMs)
        {
            DistributedTransactionResult result = new DistributedTransactionResult { TransactionId = "tx-" + HashText((model == null ? "NULL" : Fingerprint(model)) + "|" + (scenario == null ? "BASE" : scenario.Id) + "|" + coordinatorId + "|" + timeoutMs.ToString("R", CultureInfo.InvariantCulture)).Substring(0, 16), ScenarioId = scenario == null ? "BASE" : scenario.Id, CoordinatorId = coordinatorId };
            List<string> participants = (participantIds ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(256).ToList(); result.Participants = participants.Count; double now = 0; int seq = 0; bool coordinatorUp = model != null && ProcessAvailable(model, scenario, coordinatorId);
            result.Steps.Add(new DistributedTransactionStep { Sequence = ++seq, AtMs = now, ParticipantId = coordinatorId, Phase = "BEGIN", State = coordinatorUp ? "ACTIVE" : "UNAVAILABLE", Detail = "Coordinator opens a bounded two-phase transaction." });
            Dictionary<string, bool> prepared = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (string participant in participants)
            {
                double delay = model == null ? timeoutMs : ProcessDelay(model, scenario, coordinatorId, participant, 512, "2pc-prepare-" + participant); now = Math.Max(now, delay); bool reachable = coordinatorUp && ProcessAvailable(model, scenario, participant) && CanProcessesCommunicate(model, scenario, coordinatorId, participant) && delay <= timeoutMs; prepared[participant] = reachable; result.Steps.Add(new DistributedTransactionStep { Sequence = ++seq, AtMs = Math.Min(delay, timeoutMs), ParticipantId = participant, Phase = "PREPARE", Vote = reachable ? "YES" : "NO_RESPONSE", State = reachable ? "PREPARED" : "UNKNOWN", Detail = reachable ? "Durable intent recorded in the model transaction log." : "No prepare acknowledgement inside the declared timeout." }); if (reachable) result.Prepared++; }
            bool commit = coordinatorUp && result.Prepared == result.Participants; now = Math.Min(timeoutMs * 2, Math.Max(now, timeoutMs)); result.Steps.Add(new DistributedTransactionStep { Sequence = ++seq, AtMs = now, ParticipantId = coordinatorId, Phase = "DECIDE", Vote = commit ? "COMMIT" : "ABORT", State = commit ? "COMMITTING" : "ABORTING", Detail = commit ? "All declared participants voted yes." : "Unanimous prepare was not obtained." });
            foreach (string participant in participants)
            {
                bool deliver = coordinatorUp && ProcessAvailable(model, scenario, participant) && CanProcessesCommunicate(model, scenario, coordinatorId, participant); string state; if (!prepared[participant]) state = "ABORT_OR_UNKNOWN"; else if (!deliver) { state = "IN_DOUBT"; result.InDoubt++; } else state = commit ? "COMMITTED" : "ABORTED";
                result.Steps.Add(new DistributedTransactionStep { Sequence = ++seq, AtMs = now + (deliver ? ProcessDelay(model, scenario, coordinatorId, participant, 256, "2pc-decision-" + participant) : timeoutMs), ParticipantId = participant, Phase = "DECISION", Vote = commit ? "COMMIT" : "ABORT", State = state, Detail = state == "IN_DOUBT" ? "Prepared participant cannot learn coordinator decision." : "Participant reaches a terminal modeled state." });
            }
            result.Outcome = result.InDoubt > 0 ? "IN_DOUBT" : commit ? "COMMITTED" : "ABORTED"; result.CompletedMs = result.Steps.Max(x => x.AtMs);
            foreach (string participant in participants.Where(x => prepared[x])) result.WaitForEdges.Add(participant + "→" + coordinatorId); result.DeadlockCycles.AddRange(DeadlockCycles(result.WaitForEdges)); result.CertificateHash = HashText(result.TransactionId + "|" + result.Outcome + "|" + result.Participants + "|" + result.Prepared + "|" + result.InDoubt + "|" + String.Join("|", result.Steps.Select(x => x.ToString()).ToArray()) + "|" + String.Join(",", result.DeadlockCycles.ToArray())); return result;
        }

        public static DistributedLeaseResult AnalyzeLease(DistributedSystemModel model, string resourceId, string holderId, long priorFencingToken, double grantedAtMs, double leaseDurationMs, double networkUncertaintyMs)
        {
            DistributedLeaseResult result = new DistributedLeaseResult { ResourceId = resourceId, HolderId = holderId, FencingToken = Math.Max(0, priorFencingToken) + 1, GrantedAtMs = Math.Max(0, grantedAtMs), ExpiresAtMs = Math.Max(0, grantedAtMs) + Math.Max(0, leaseDurationMs) };
            DistributedProcess process = model == null ? null : (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, holderId)); DistributedNode node = process == null || model == null ? null : (model.Nodes ?? new List<DistributedNode>()).FirstOrDefault(x => x != null && Eq(x.Id, process.NodeId)); double drift = node == null ? 0 : Math.Abs(node.ClockDriftPpm) * Math.Max(0, leaseDurationMs) / 1000000.0; double offset = node == null ? 0 : Math.Abs(node.ClockOffsetMs); result.WorstClockErrorMs = offset + drift + Math.Max(0, networkUncertaintyMs); result.SafeUsableThroughMs = Math.Max(result.GrantedAtMs, result.ExpiresAtMs - result.WorstClockErrorMs); result.OverlapRisk = result.WorstClockErrorMs * 2 >= Math.Max(0, leaseDurationMs); result.Verdict = leaseDurationMs <= 0 ? "INVALID" : result.OverlapRisk ? "UNSAFE_WITHOUT_FENCING" : "BOUNDED_SAFE_WINDOW"; result.Audit.Add("Fencing token is monotonically advanced from " + priorFencingToken + " to " + result.FencingToken + "."); result.Audit.Add("Safe usable window subtracts absolute offset, accumulated drift and declared network uncertainty."); result.Audit.Add("Storage recipients must reject tokens lower than the highest token already observed."); result.CertificateHash = HashText(result.ResourceId + "|" + result.HolderId + "|" + result.FencingToken + "|" + result.GrantedAtMs.ToString("R", CultureInfo.InvariantCulture) + "|" + result.ExpiresAtMs.ToString("R", CultureInfo.InvariantCulture) + "|" + result.WorstClockErrorMs.ToString("R", CultureInfo.InvariantCulture) + "|" + result.SafeUsableThroughMs.ToString("R", CultureInfo.InvariantCulture) + "|" + result.Verdict); return result;
        }

        public static List<DistributedFailureDetectorRecord> AnalyzeFailureDetector(DistributedSystemModel model, DistributedSimulationResult simulation, double threshold)
        {
            List<DistributedFailureDetectorRecord> rows = new List<DistributedFailureDetectorRecord>(); if (model == null || simulation == null) return rows; threshold = Math.Max(0.1, Math.Min(100, threshold));
            foreach (DistributedProcess observer in model.Processes ?? new List<DistributedProcess>()) foreach (DistributedProcess subject in model.Processes ?? new List<DistributedProcess>())
            {
                if (observer == null || subject == null || Eq(observer.Id, subject.Id) || !Eq(observer.GroupId, subject.GroupId)) continue;
                List<double> beats = simulation.Trace.Where(x => x != null && Eq(x.Kind, "HEARTBEAT_DELIVERED") && Eq(x.ActorId, subject.Id) && Eq(x.PeerId, observer.Id)).Select(x => x.AtMs).OrderBy(x => x).ToList(); List<double> intervals = new List<double>(); for (int i = 1; i < beats.Count; i++) intervals.Add(Math.Max(0.001, beats[i] - beats[i - 1])); double mean = intervals.Count == 0 ? Parameter(model, "heartbeat_ms", 800) : intervals.Average(); double last = beats.Count == 0 ? 0 : beats[beats.Count - 1]; double silence = Math.Max(0, simulation.SimulatedThroughMs - last); double phi = silence <= 0 ? 0 : silence / Math.Max(0.001, mean) * Math.Log10(Math.E); rows.Add(new DistributedFailureDetectorRecord { ObserverId = observer.Id, SubjectId = subject.Id, MeanHeartbeatMs = mean, SilenceMs = silence, Phi = phi, Verdict = phi >= threshold ? "SUSPECT" : "ALIVE" });
            }
            return rows.OrderByDescending(x => x.Phi).ThenBy(x => x.ObserverId).ThenBy(x => x.SubjectId).Take(4096).ToList();
        }

        public static DistributedChaosCampaignResult RunChaosCampaign(DistributedSystemModel model, int eventLimitPerWorld)
        {
            DistributedChaosCampaignResult campaign = new DistributedChaosCampaignResult(); if (model == null) return SealCampaign(campaign); List<DistributedScenario> scenarios = (model.Scenarios ?? new List<DistributedScenario>()).Where(x => x != null && Active(x.Status)).Take(128).ToList(); if (scenarios.Count == 0) scenarios.Add(new DistributedScenario { Id = "base", Name = "Base", DurationMs = 60000, MaxEvents = eventLimitPerWorld }); campaign.RequestedWorlds = scenarios.Count; double weighted = 0, weights = 0;
            foreach (DistributedScenario scenario in scenarios)
            {
                int cap = Math.Max(100, Math.Min(Math.Min(AbsoluteEventCap, eventLimitPerWorld), scenario.MaxEvents)); DistributedSimulationResult sim = Simulate(model, scenario, scenario.DurationMs, cap); List<DistributedQuorumAssessment> quorum = AssessQuorums(model, scenario); int anomalies = sim.Consistency.Sum(x => x.StaleReads + x.MonotonicReadViolations + x.ReadYourWritesViolations + x.LostAcknowledgedWrites); int violations = sim.Invariants.Count(x => Eq(x.Severity, "ERROR") || Eq(x.Severity, "FATAL")); int quorumLoss = quorum.Count(x => !x.MajorityAvailable); string cls = quorumLoss > 0 || violations > 0 ? "FRAGILE" : sim.Availability < 0.99 ? "DEGRADED" : anomalies > 0 ? "CONSISTENCY_RISK" : sim.RecoveryTimeMs > Parameter(model, "rto_target_ms", 15000) ? "RTO_BREACH" : "RESILIENT"; DistributedChaosWorldResult world = new DistributedChaosWorldResult { ScenarioId = scenario.Id, Weight = Math.Max(0, scenario.Weight), Availability = sim.Availability, P95LatencyMs = sim.P95LatencyMs, RecoveryTimeMs = sim.RecoveryTimeMs, LeaderlessMs = sim.LeaderlessMs, DataLossWindowMs = sim.DataLossWindowMs, InvariantViolations = violations, ConsistencyAnomalies = anomalies, QuorumLosses = quorumLoss, ResilienceClass = cls, SimulationHash = sim.CertificateHash }; campaign.Worlds.Add(world); campaign.CompletedWorlds++; weighted += world.Availability * world.Weight; weights += world.Weight; if (world.RecoveryTimeMs >= campaign.WorstRecoveryTimeMs) { campaign.WorstRecoveryTimeMs = world.RecoveryTimeMs; campaign.WorstScenarioId = world.ScenarioId; }
            }
            campaign.WeightedAvailability = weights <= 0 ? 0 : weighted / weights; if (campaign.Worlds.Any(x => x.QuorumLosses > 0)) campaign.Recommendations.Add("Redistribute voting replicas across independent fault domains until every declared domain-loss world retains W quorum."); if (campaign.Worlds.Any(x => x.InvariantViolations > 0)) campaign.Recommendations.Add("Treat invariant witnesses as blocking design evidence; inspect term, log and fencing histories before changing timeouts."); if (campaign.Worlds.Any(x => x.ConsistencyAnomalies > 0)) campaign.Recommendations.Add("Align declared consistency with quorum intersection, session guarantees and replica-read routing."); if (campaign.WorstRecoveryTimeMs > Parameter(model, "rto_target_ms", 15000)) campaign.Recommendations.Add("Recovery exceeds modeled RTO; reduce detection/restart/election delay or add survivable capacity."); if (campaign.Worlds.Any(x => x.P95LatencyMs > Parameter(model, "p95_target_ms", 250))) campaign.Recommendations.Add("Tail latency exceeds modeled SLO; inspect cross-region quorum paths, queuing and retry amplification."); return SealCampaign(campaign);
        }

        public static string CompareVectorClocks(string left, string right)
        {
            Dictionary<string, long> a = ParseVector(left), b = ParseVector(right); bool aLess = false, bLess = false; foreach (string key in a.Keys.Union(b.Keys, StringComparer.OrdinalIgnoreCase)) { long av = a.ContainsKey(key) ? a[key] : 0, bv = b.ContainsKey(key) ? b[key] : 0; if (av < bv) aLess = true; if (bv < av) bLess = true; } return aLess && bLess ? "CONCURRENT" : aLess ? "BEFORE" : bLess ? "AFTER" : "EQUAL";
        }

        public static string MergeVectorClocks(string left, string right, string incrementActor)
        {
            Dictionary<string, long> a = ParseVector(left), b = ParseVector(right); foreach (KeyValuePair<string, long> pair in b) if (!a.ContainsKey(pair.Key) || a[pair.Key] < pair.Value) a[pair.Key] = pair.Value; if (!String.IsNullOrWhiteSpace(incrementActor)) a[incrementActor] = (a.ContainsKey(incrementActor) ? a[incrementActor] : 0) + 1; return FormatVector(a);
        }

        private static Runtime BuildRuntime(DistributedSystemModel model, DistributedScenario scenario, double durationMs, int eventLimit)
        {
            Runtime rt = new Runtime { Model = model, Scenario = scenario }; rt.Result = new DistributedSimulationResult { ModelId = model.Id, ScenarioId = scenario == null ? "BASE" : scenario.Id, ModelFingerprint = Fingerprint(model), RequestedDurationMs = Math.Max(1, Math.Min(86400000, durationMs)), EventLimit = Math.Max(1, Math.Min(AbsoluteEventCap, eventLimit)) };
            foreach (DistributedNode node in model.Nodes ?? new List<DistributedNode>()) if (node != null && !String.IsNullOrWhiteSpace(node.Id) && !rt.Nodes.ContainsKey(node.Id)) rt.Nodes[node.Id] = node;
            foreach (DistributedProcess p in model.Processes ?? new List<DistributedProcess>()) if (p != null && !String.IsNullOrWhiteSpace(p.Id) && !rt.Processes.ContainsKey(p.Id)) rt.Processes[p.Id] = new RuntimeProcess { Source = p, Role = p.Role ?? "FOLLOWER", Term = p.Term, LogIndex = p.LastLogIndex, CommitIndex = p.CommitIndex, AppliedIndex = p.AppliedIndex, Up = Active(p.Health), ElectionDeadline = ElectionDeadline(model, p, 0), LastHeartbeat = 0 };
            foreach (DistributedReplica r in model.Replicas ?? new List<DistributedReplica>()) if (r != null && !String.IsNullOrWhiteSpace(r.Id) && !rt.Replicas.ContainsKey(r.Id)) rt.Replicas[r.Id] = new RuntimeReplica { Source = r, Version = r.Version, LogIndex = r.LogIndex, CommitIndex = r.CommitIndex, AppliedIndex = r.AppliedIndex, StateHash = r.StateHash ?? "GENESIS", VectorClock = r.VectorClock ?? "", Online = Active(r.Status) };
            foreach (RuntimeProcess p in rt.Processes.Values) if (Eq(p.Role, "LEADER") && p.Up) rt.LatestLeaderByGroup[p.Source.GroupId ?? "default"] = p.Source.Id;
            rt.Result.Audit.Add("Deterministic discrete-event world; seed=" + model.Seed + ", horizon=" + rt.Result.RequestedDurationMs.ToString("0.###", CultureInfo.InvariantCulture) + "ms, cap=" + rt.Result.EventLimit + "."); rt.Result.Audit.Add("Simulation authority is synthetic and model-bound; no packet, process, storage, clock or service is touched outside this retained world."); return rt;
        }

        private static void ScheduleInitial(Runtime rt)
        {
            foreach (RuntimeProcess p in rt.Processes.Values) if (p.Up) Schedule(rt, p.ElectionDeadline, "ELECTION_TIMEOUT", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, null, 0);
            foreach (RuntimeProcess leader in rt.Processes.Values.Where(x => x.Up && Eq(x.Role, "LEADER"))) Schedule(rt, 0, "HEARTBEAT_TICK", leader.Source.Id, null, leader.Source.GroupId, null, leader.Term, leader.LogIndex, null, 0);
            double traffic = rt.Scenario == null ? 1 : Math.Max(0, rt.Scenario.TrafficMultiplier); foreach (DistributedWorkloadOperation op in (rt.Model.Workload ?? new List<DistributedWorkloadOperation>()).Where(x => x != null && Active(x.Status))) { double at = traffic <= 0 ? rt.Result.RequestedDurationMs + 1 : op.AtMs / traffic; Schedule(rt, at, "CLIENT_OPERATION", op.ClientId, null, op.Id, op.CorrelationId ?? op.Id, 0, 0, null, 0); Schedule(rt, at + Math.Max(1, op.DeadlineMs), "OPERATION_DEADLINE", op.ClientId, null, op.Id, op.CorrelationId ?? op.Id, 0, 0, null, 0); }
            HashSet<string> enabled = EnabledFaults(rt.Model, rt.Scenario); foreach (DistributedFault fault in (rt.Model.Faults ?? new List<DistributedFault>()).Where(x => x != null && Active(x.Status) && enabled.Contains(x.Id))) { Schedule(rt, fault.StartMs, "FAULT_START", fault.TargetId, fault.SecondaryTargetId, fault.Id, fault.Id, 0, 0, fault.Kind, 0); if (fault.Recoverable && fault.DurationMs >= 0) Schedule(rt, fault.StartMs + fault.DurationMs, "FAULT_END", fault.TargetId, fault.SecondaryTargetId, fault.Id, fault.Id, 0, 0, fault.Kind, 0); }
        }

        private static void ProcessEvent(Runtime rt, ScheduledEvent ev)
        {
            if (Eq(ev.Kind, "VOTE_REQUEST") || Eq(ev.Kind, "VOTE_RESPONSE") || Eq(ev.Kind, "HEARTBEAT_DELIVERY") || Eq(ev.Kind, "APPEND_DELIVERY") || Eq(ev.Kind, "APPEND_ACK") || Eq(ev.Kind, "READ_DELIVERY")) rt.Result.MessagesDelivered++;
            if (Eq(ev.Kind, "ELECTION_TIMEOUT")) ElectionTimeout(rt, ev); else if (Eq(ev.Kind, "VOTE_REQUEST")) VoteRequest(rt, ev); else if (Eq(ev.Kind, "VOTE_RESPONSE")) VoteResponse(rt, ev); else if (Eq(ev.Kind, "HEARTBEAT_TICK")) HeartbeatTick(rt, ev); else if (Eq(ev.Kind, "HEARTBEAT_DELIVERY")) HeartbeatDelivery(rt, ev); else if (Eq(ev.Kind, "APPEND_DELIVERY")) AppendDelivery(rt, ev); else if (Eq(ev.Kind, "APPEND_ACK")) AppendAck(rt, ev); else if (Eq(ev.Kind, "CLIENT_OPERATION")) ClientOperation(rt, ev); else if (Eq(ev.Kind, "READ_DELIVERY")) ReadDelivery(rt, ev); else if (Eq(ev.Kind, "OPERATION_DEADLINE")) OperationDeadline(rt, ev); else if (Eq(ev.Kind, "FAULT_START")) FaultChange(rt, ev, true); else if (Eq(ev.Kind, "FAULT_END")) FaultChange(rt, ev, false); else if (Eq(ev.Kind, "PROCESS_RESTART")) RestartProcess(rt, ev);
        }

        private static void ElectionTimeout(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess p; if (!rt.Processes.TryGetValue(ev.ActorId ?? "", out p) || !Available(rt, p) || ev.Term != p.Term || Eq(p.Role, "LEADER") || rt.Now + 0.0001 < p.ElectionDeadline) return;
            p.Term++; p.Role = "CANDIDATE"; p.VotedFor = p.Source.Id; p.Votes.Clear(); p.Votes.Add(p.Source.Id); p.ElectionDeadline = ElectionDeadline(rt.Model, p.Source, rt.Now + p.Term * 0.001); rt.Result.Elections++; Trace(rt, "ELECTION_STARTED", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, "Candidate self-votes and solicits its voting group.", "STARTED"); Schedule(rt, p.ElectionDeadline, "ELECTION_TIMEOUT", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, null, 0);
            List<RuntimeProcess> peers = Group(rt, p.Source.GroupId).Where(x => !Eq(x.Source.Id, p.Source.Id) && x.Source.Voting).ToList(); foreach (RuntimeProcess peer in peers) Send(rt, p.Source.Id, peer.Source.Id, "VOTE_REQUEST", p.Source.GroupId, "vote|" + p.Term, p.Term, p.LogIndex, p.Source.Id, 0);
            if (VotingCount(rt, p.Source.GroupId) <= 1) BecomeLeader(rt, p);
        }

        private static void VoteRequest(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess voter; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out voter) || !Available(rt, voter)) return; bool grant = false; if (ev.Term > voter.Term) { voter.Term = ev.Term; voter.Role = "FOLLOWER"; voter.VotedFor = null; } if (ev.Term == voter.Term && (String.IsNullOrWhiteSpace(voter.VotedFor) || Eq(voter.VotedFor, ev.ActorId)) && ev.LogIndex >= voter.LogIndex) { grant = true; voter.VotedFor = ev.ActorId; voter.ElectionDeadline = ElectionDeadline(rt.Model, voter.Source, rt.Now); }
            Trace(rt, "VOTE_REQUEST_DELIVERED", ev.ActorId, voter.Source.Id, ev.SubjectId, ev.CorrelationId, ev.Term, ev.LogIndex, grant ? "Vote granted." : "Vote rejected by term, prior vote or stale log.", grant ? "GRANTED" : "REJECTED"); Send(rt, voter.Source.Id, ev.ActorId, "VOTE_RESPONSE", ev.SubjectId, ev.CorrelationId, voter.Term, voter.LogIndex, grant ? "YES" : "NO", 0);
        }

        private static void VoteResponse(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess candidate; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out candidate) || !Available(rt, candidate) || !Eq(candidate.Role, "CANDIDATE") || candidate.Term != ev.Term) return; if (Eq(ev.Payload, "YES")) candidate.Votes.Add(ev.ActorId); Trace(rt, "VOTE_RESPONSE_DELIVERED", ev.ActorId, candidate.Source.Id, ev.SubjectId, ev.CorrelationId, ev.Term, ev.LogIndex, "Candidate now has " + candidate.Votes.Count + " votes.", ev.Payload); int quorum = Majority(VotingCount(rt, candidate.Source.GroupId)); if (candidate.Votes.Count >= quorum) BecomeLeader(rt, candidate);
        }

        private static void BecomeLeader(Runtime rt, RuntimeProcess candidate)
        {
            string group = candidate.Source.GroupId ?? "default"; string old; rt.LatestLeaderByGroup.TryGetValue(group, out old); foreach (RuntimeProcess p in Group(rt, group)) if (p.Term <= candidate.Term && !Eq(p.Source.Id, candidate.Source.Id) && Eq(p.Role, "LEADER")) p.Role = "FOLLOWER"; candidate.Role = "LEADER"; candidate.VotedFor = candidate.Source.Id; rt.LatestLeaderByGroup[group] = candidate.Source.Id; if (!Eq(old, candidate.Source.Id)) rt.Result.LeaderChanges++; Trace(rt, "LEADER_ELECTED", candidate.Source.Id, null, group, null, candidate.Term, candidate.LogIndex, "Voting quorum=" + Majority(VotingCount(rt, group)) + ", votes=" + candidate.Votes.Count + ".", "LEADER"); Schedule(rt, rt.Now, "HEARTBEAT_TICK", candidate.Source.Id, null, group, null, candidate.Term, candidate.LogIndex, null, 0);
        }

        private static void HeartbeatTick(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess leader; if (!rt.Processes.TryGetValue(ev.ActorId ?? "", out leader) || !Available(rt, leader) || !Eq(leader.Role, "LEADER") || leader.Term != ev.Term) return; foreach (RuntimeProcess peer in Group(rt, leader.Source.GroupId).Where(x => !Eq(x.Source.Id, leader.Source.Id))) Send(rt, leader.Source.Id, peer.Source.Id, "HEARTBEAT_DELIVERY", leader.Source.GroupId, "hb|" + leader.Term + "|" + rt.Now.ToString("R", CultureInfo.InvariantCulture), leader.Term, leader.CommitIndex, null, 0); double interval = Math.Max(1, Parameter(rt.Model, "heartbeat_ms", 800)); Schedule(rt, rt.Now + interval, "HEARTBEAT_TICK", leader.Source.Id, null, leader.Source.GroupId, null, leader.Term, leader.LogIndex, null, 0);
        }

        private static void HeartbeatDelivery(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess follower; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out follower) || !Available(rt, follower)) return; if (ev.Term >= follower.Term) { follower.Term = ev.Term; follower.Role = "FOLLOWER"; follower.VotedFor = null; follower.LastHeartbeat = rt.Now; follower.ElectionDeadline = ElectionDeadline(rt.Model, follower.Source, rt.Now); Schedule(rt, follower.ElectionDeadline, "ELECTION_TIMEOUT", follower.Source.Id, null, follower.Source.GroupId, null, follower.Term, follower.LogIndex, null, 0); follower.CommitIndex = Math.Max(follower.CommitIndex, Math.Min(follower.LogIndex, ev.LogIndex)); follower.AppliedIndex = Math.Max(follower.AppliedIndex, follower.CommitIndex); }
            Trace(rt, "HEARTBEAT_DELIVERED", ev.ActorId, follower.Source.Id, ev.SubjectId, ev.CorrelationId, ev.Term, ev.LogIndex, "Follower election deadline reset.", "DELIVERED");
        }

        private static void ClientOperation(Runtime rt, ScheduledEvent ev)
        {
            DistributedWorkloadOperation op = (rt.Model.Workload ?? new List<DistributedWorkloadOperation>()).FirstOrDefault(x => x != null && Eq(x.Id, ev.SubjectId)); if (op == null) return; OperationState state = new OperationState { Source = op, Started = rt.Now, Outcome = "PENDING" }; rt.Operations[op.Id] = state; rt.Result.OperationsStarted++; string processId = RouteOperation(rt, op); state.ProcessId = processId; RuntimeProcess process; if (String.IsNullOrWhiteSpace(processId) || !rt.Processes.TryGetValue(processId, out process) || !Available(rt, process)) { Trace(rt, "OPERATION_REJECTED", op.ClientId, processId, op.Id, op.CorrelationId, 0, 0, "No reachable eligible process.", "UNAVAILABLE"); return; }
            if (Eq(op.Kind, "WRITE") || Eq(op.Kind, "PUT") || Eq(op.Kind, "DELETE"))
            {
                RuntimeProcess leader = Eq(process.Role, "LEADER") ? process : Leader(rt, process.Source.GroupId); if (leader == null || !Available(rt, leader)) { Trace(rt, "WRITE_REJECTED", op.ClientId, process.Source.Id, op.Id, op.CorrelationId, process.Term, process.LogIndex, "No leader is available for the write.", "NO_LEADER"); return; }
                leader.LogIndex++; long index = leader.LogIndex; state.RequiredVersion = index; leader.Acks[index] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { leader.Source.Id }; Trace(rt, "WRITE_ACCEPTED", op.ClientId, leader.Source.Id, op.Id, op.CorrelationId, leader.Term, index, "Leader appended an uncommitted log entry.", "APPENDED"); foreach (RuntimeProcess peer in Group(rt, leader.Source.GroupId).Where(x => !Eq(x.Source.Id, leader.Source.Id))) Send(rt, leader.Source.Id, peer.Source.Id, "APPEND_DELIVERY", op.Id, op.CorrelationId, leader.Term, index, op.DatasetId + "|" + op.ShardId + "|" + op.Key + "|" + op.Value, 0); if (Majority(VotingCount(rt, leader.Source.GroupId)) <= 1) Commit(rt, leader, op, index);
            }
            else
            {
                rt.Result.MessagesSent++; Schedule(rt, rt.Now + Math.Max(0.1, Parameter(rt.Model, "client_ingress_ms", 2)), "READ_DELIVERY", op.ClientId, process.Source.Id, op.Id, op.CorrelationId, process.Term, process.CommitIndex, op.DatasetId + "|" + op.ShardId + "|" + op.Key, 0);
            }
        }

        private static void AppendDelivery(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess follower; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out follower) || !Available(rt, follower)) return; bool accept = ev.Term >= follower.Term && ev.LogIndex <= follower.LogIndex + 1; if (ev.Term > follower.Term) { follower.Term = ev.Term; follower.Role = "FOLLOWER"; follower.VotedFor = null; } if (accept) { follower.LogIndex = Math.Max(follower.LogIndex, ev.LogIndex); UpdateReplica(rt, follower.Source.Id, ev, false); } Trace(rt, "APPEND_DELIVERED", ev.ActorId, follower.Source.Id, ev.SubjectId, ev.CorrelationId, ev.Term, ev.LogIndex, accept ? "Follower durably modeled the entry." : "Follower rejected a noncontiguous or stale append.", accept ? "ACK" : "REJECT"); Send(rt, follower.Source.Id, ev.ActorId, "APPEND_ACK", ev.SubjectId, ev.CorrelationId, follower.Term, follower.LogIndex, accept ? "ACK" : "REJECT", 0);
        }

        private static void AppendAck(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess leader; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out leader) || !Available(rt, leader) || !Eq(leader.Role, "LEADER") || leader.Term != ev.Term || !Eq(ev.Payload, "ACK")) return; HashSet<string> acks; if (!leader.Acks.TryGetValue(ev.LogIndex, out acks)) { acks = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { leader.Source.Id }; leader.Acks[ev.LogIndex] = acks; } acks.Add(ev.ActorId); Trace(rt, "APPEND_ACK_DELIVERED", ev.ActorId, leader.Source.Id, ev.SubjectId, ev.CorrelationId, ev.Term, ev.LogIndex, "Acknowledgements=" + acks.Count + ".", "ACK"); DistributedWorkloadOperation op = (rt.Model.Workload ?? new List<DistributedWorkloadOperation>()).FirstOrDefault(x => x != null && Eq(x.Id, ev.SubjectId)); if (op != null && acks.Count >= Majority(VotingCount(rt, leader.Source.GroupId)) && leader.CommitIndex < ev.LogIndex) Commit(rt, leader, op, ev.LogIndex);
        }

        private static void Commit(Runtime rt, RuntimeProcess leader, DistributedWorkloadOperation op, long index)
        {
            leader.CommitIndex = Math.Max(leader.CommitIndex, index); leader.AppliedIndex = Math.Max(leader.AppliedIndex, leader.CommitIndex); UpdateReplica(rt, leader.Source.Id, new ScheduledEvent { ActorId = leader.Source.Id, SubjectId = op.Id, CorrelationId = op.CorrelationId, Term = leader.Term, LogIndex = index, Payload = op.DatasetId + "|" + op.ShardId + "|" + op.Key + "|" + op.Value }, true); OperationState state; if (rt.Operations.TryGetValue(op.Id, out state) && Eq(state.Outcome, "PENDING")) { state.Outcome = "ACKNOWLEDGED"; state.Completed = rt.Now; state.ObservedVersion = index; rt.Result.OperationsCompleted++; rt.Result.AcknowledgedWrites++; rt.Latencies.Add(Math.Max(0, rt.Now - state.Started)); rt.LastClientVersion[(op.ClientId ?? "CLIENT") + "|" + (op.DatasetId ?? "DATASET") + "|" + (op.Key ?? "KEY")] = index; } Trace(rt, "WRITE_COMMITTED", leader.Source.Id, op.ClientId, op.Id, op.CorrelationId, leader.Term, index, "Quorum commit and client acknowledgement are co-recorded.", "ACKNOWLEDGED");
        }

        private static void ReadDelivery(Runtime rt, ScheduledEvent ev)
        {
            RuntimeProcess process; OperationState state; if (!rt.Processes.TryGetValue(ev.PeerId ?? "", out process) || !Available(rt, process) || !rt.Operations.TryGetValue(ev.SubjectId ?? "", out state) || !Eq(state.Outcome, "PENDING")) return; long version = process.AppliedIndex; RuntimeReplica replica = rt.Replicas.Values.FirstOrDefault(x => Eq(x.Source.ProcessId, process.Source.Id) && (String.IsNullOrWhiteSpace(state.Source.ShardId) || Eq(x.Source.ShardId, state.Source.ShardId))); if (replica != null) version = Math.Max(version, replica.AppliedIndex); state.ObservedVersion = version; state.RequiredVersion = (long)(rt.LastClientVersion.ContainsKey((state.Source.ClientId ?? "CLIENT") + "|" + (state.Source.DatasetId ?? "DATASET") + "|" + (state.Source.Key ?? "KEY")) ? rt.LastClientVersion[(state.Source.ClientId ?? "CLIENT") + "|" + (state.Source.DatasetId ?? "DATASET") + "|" + (state.Source.Key ?? "KEY")] : 0); state.Outcome = "COMPLETED"; state.Completed = rt.Now; rt.Result.OperationsCompleted++; rt.Latencies.Add(Math.Max(0, rt.Now - state.Started)); Trace(rt, "READ_COMPLETED", process.Source.Id, state.Source.ClientId, state.Source.Id, state.Source.CorrelationId, process.Term, version, "Observed version=" + version + ", session-required=" + state.RequiredVersion + ".", version < state.RequiredVersion ? "STALE" : "VALUE");
        }

        private static void OperationDeadline(Runtime rt, ScheduledEvent ev)
        {
            OperationState state; if (!rt.Operations.TryGetValue(ev.SubjectId ?? "", out state) || !Eq(state.Outcome, "PENDING")) return; state.Outcome = "TIMEOUT"; state.Completed = rt.Now; rt.Result.OperationsTimedOut++; Trace(rt, "OPERATION_TIMEOUT", state.Source.ClientId, state.ProcessId, state.Source.Id, state.Source.CorrelationId, 0, state.ObservedVersion, "Deadline elapsed before a terminal acknowledgement/value.", "TIMEOUT");
        }

        private static void FaultChange(Runtime rt, ScheduledEvent ev, bool start)
        {
            DistributedFault fault = (rt.Model.Faults ?? new List<DistributedFault>()).FirstOrDefault(x => x != null && Eq(x.Id, ev.SubjectId)); if (fault == null) return; string kind = fault.Kind ?? "NODE_CRASH"; if (start) rt.LastFaultStartedAt = rt.Now; else rt.LastRecoveryAt = rt.Now;
            if (Eq(kind, "NODE_CRASH") || Eq(kind, "NODE_PAUSE") || Eq(kind, "DISK_FAILURE")) { if (start) rt.DownNodes.Add(fault.TargetId ?? ""); else rt.DownNodes.Remove(fault.TargetId ?? ""); foreach (RuntimeProcess p in rt.Processes.Values.Where(x => Eq(x.Source.NodeId, fault.TargetId))) { p.Up = !start && Active(p.Source.Health); if (start) { if (Eq(p.Role, "LEADER")) rt.LatestLeaderByGroup.Remove(p.Source.GroupId ?? "default"); p.Role = "FOLLOWER"; } else { p.ElectionDeadline = ElectionDeadline(rt.Model, p.Source, rt.Now); Schedule(rt, p.ElectionDeadline, "ELECTION_TIMEOUT", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, null, 0); } } }
            else if (Eq(kind, "PROCESS_CRASH") || Eq(kind, "PROCESS_PAUSE")) { if (start) rt.DownProcesses.Add(fault.TargetId ?? ""); else rt.DownProcesses.Remove(fault.TargetId ?? ""); RuntimeProcess p; if (rt.Processes.TryGetValue(fault.TargetId ?? "", out p)) { p.Up = !start && Active(p.Source.Health); if (start) { if (Eq(p.Role, "LEADER")) rt.LatestLeaderByGroup.Remove(p.Source.GroupId ?? "default"); p.Role = "FOLLOWER"; } else { p.ElectionDeadline = ElectionDeadline(rt.Model, p.Source, rt.Now); Schedule(rt, p.ElectionDeadline, "ELECTION_TIMEOUT", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, null, 0); } } }
            else if (Eq(kind, "LINK_DOWN")) { if (start) rt.DownLinks.Add(fault.TargetId ?? ""); else rt.DownLinks.Remove(fault.TargetId ?? ""); }
            else if (Eq(kind, "PARTITION") || Eq(kind, "ASYMMETRIC_PARTITION")) { string key = PairKey(fault.TargetId, fault.SecondaryTargetId, Eq(kind, "PARTITION")); if (start) rt.ActivePartitions.Add(key); else rt.ActivePartitions.Remove(key); }
            else if (Eq(kind, "LATENCY_SPIKE")) { if (start) rt.LatencyFaults[fault.TargetId ?? "*"] = Math.Max(1, fault.Magnitude); else rt.LatencyFaults.Remove(fault.TargetId ?? "*"); }
            else if (Eq(kind, "PACKET_LOSS")) { if (start) rt.LossFaults[fault.TargetId ?? "*"] = Clamp01(fault.Magnitude); else rt.LossFaults.Remove(fault.TargetId ?? "*"); }
            else if (Eq(kind, "CLOCK_SKEW")) { DistributedNode n; if (rt.Nodes.TryGetValue(fault.TargetId ?? "", out n)) n.ClockOffsetMs += (start ? 1 : -1) * fault.Magnitude; }
            Trace(rt, start ? "FAULT_ACTIVATED" : "FAULT_RECOVERED", fault.TargetId, fault.SecondaryTargetId, fault.Id, fault.Id, 0, 0, kind + " magnitude=" + fault.Magnitude.ToString("R", CultureInfo.InvariantCulture) + ".", start ? "ACTIVE" : "RECOVERED");
        }

        private static void RestartProcess(Runtime rt, ScheduledEvent ev) { RuntimeProcess p; if (!rt.Processes.TryGetValue(ev.ActorId ?? "", out p) || rt.DownNodes.Contains(p.Source.NodeId ?? "")) return; rt.DownProcesses.Remove(p.Source.Id); p.Up = true; p.Role = "FOLLOWER"; p.ElectionDeadline = ElectionDeadline(rt.Model, p.Source, rt.Now); Schedule(rt, p.ElectionDeadline, "ELECTION_TIMEOUT", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, null, 0); Trace(rt, "PROCESS_RESTARTED", p.Source.Id, null, p.Source.GroupId, null, p.Term, p.LogIndex, "Process rejoined as follower.", "UP"); }

        private static void Send(Runtime rt, string actor, string peer, string kind, string subject, string correlation, long term, long index, string payload, int attempt)
        {
            rt.Result.MessagesSent++; RuntimeProcess from, to; if (!rt.Processes.TryGetValue(actor ?? "", out from) || !rt.Processes.TryGetValue(peer ?? "", out to) || !Available(rt, from) || !Available(rt, to)) { rt.Result.MessagesDropped++; Trace(rt, "MESSAGE_DROPPED", actor, peer, subject, correlation, term, index, "Sender or receiver unavailable.", "ENDPOINT_DOWN"); return; }
            DistributedLink link = FindLink(rt.Model, from.Source.NodeId, to.Source.NodeId); if (link == null || !LinkAvailable(rt, link, from.Source.NodeId, to.Source.NodeId)) { rt.Result.MessagesDropped++; Trace(rt, "MESSAGE_DROPPED", actor, peer, subject, correlation, term, index, "No active directionally reachable link.", "PARTITIONED"); return; }
            double loss = Clamp01(link.LossProbability + (rt.Scenario == null ? 0 : rt.Scenario.LossAddition) + FaultValue(rt.LossFaults, link.Id)); if (StableUnit(rt.Model.Seed, "loss|" + rt.QueueSequence + "|" + actor + "|" + peer + "|" + correlation) < loss) { rt.Result.MessagesDropped++; Trace(rt, "MESSAGE_DROPPED", actor, peer, subject, correlation, term, index, "Deterministic loss draw inside declared probability.", "NETWORK_LOSS"); return; }
            double delay = NetworkDelay(rt, link, actor, peer, payload, correlation); Schedule(rt, rt.Now + delay, kind, actor, peer, subject, correlation, term, index, payload, attempt); if (StableUnit(rt.Model.Seed, "dup|" + rt.QueueSequence + "|" + correlation + "|" + peer) < Clamp01(link.DuplicationProbability)) { rt.Result.MessagesDuplicated++; Schedule(rt, rt.Now + delay + Math.Max(0.001, link.JitterMs * StableUnit(rt.Model.Seed, "dupdelay|" + correlation)), kind, actor, peer, subject, correlation, term, index, payload, attempt + 1); }
        }

        private static double NetworkDelay(Runtime rt, DistributedLink link, string actor, string peer, string payload, string correlation)
        {
            double latency = Math.Max(0, link.BaseLatencyMs); double jitter = (StableUnit(rt.Model.Seed, "jitter|" + rt.QueueSequence + "|" + actor + "|" + peer + "|" + correlation) * 2 - 1) * Math.Max(0, link.JitterMs); int bytes = Encoding.UTF8.GetByteCount(payload ?? "") + 128; double serialize = bytes * 8.0 / Math.Max(0.001, link.BandwidthMbps * 1000000.0) * 1000.0; double scenario = rt.Scenario == null ? 1 : Math.Max(0, rt.Scenario.LatencyMultiplier); double spike = Math.Max(1, Math.Max(FaultValue(rt.LatencyFaults, link.Id), Math.Max(FaultValue(rt.LatencyFaults, actor), FaultValue(rt.LatencyFaults, peer)))); double delay = Math.Max(0.001, (latency + jitter + serialize) * scenario * spike); if (StableUnit(rt.Model.Seed, "reorder|" + rt.QueueSequence + "|" + correlation) < Clamp01(link.ReorderProbability)) delay += Math.Max(1, latency + Math.Abs(jitter)); return delay;
        }

        private static void Schedule(Runtime rt, double at, string kind, string actor, string peer, string subject, string correlation, long term, long index, string payload, int attempt)
        {
            if (rt.Queue.Count >= AbsoluteEventCap * 2) { rt.Result.Truncated = true; return; } rt.Queue.Add(new ScheduledEvent { Sequence = ++rt.QueueSequence, AtMs = Math.Max(rt.Now, at), Kind = kind, ActorId = actor, PeerId = peer, SubjectId = subject, CorrelationId = correlation, Term = term, LogIndex = index, Payload = payload, Attempt = attempt });
        }

        private static ScheduledEvent Pop(List<ScheduledEvent> queue) { if (queue == null || queue.Count == 0) return null; int best = 0; for (int i = 1; i < queue.Count; i++) if (queue[i].AtMs < queue[best].AtMs || (Math.Abs(queue[i].AtMs - queue[best].AtMs) < 0.0000001 && queue[i].Sequence < queue[best].Sequence)) best = i; ScheduledEvent value = queue[best]; queue.RemoveAt(best); return value; }

        private static void Trace(Runtime rt, string kind, string actor, string peer, string subject, string correlation, long term, long index, string detail, string outcome)
        {
            TouchClock(rt, actor, peer); DistributedTraceEvent row = new DistributedTraceEvent { Sequence = ++rt.TraceSequence, AtMs = rt.Now, Kind = kind, ActorId = actor, PeerId = peer, SubjectId = subject, CorrelationId = correlation, Term = term, LogIndex = index, Detail = detail, Outcome = outcome, Lamport = Clock(rt, actor), VectorClock = Vector(rt, actor) }; row.EventHash = HashText(rt.PriorEventHash + "|" + row.Sequence + "|" + row.AtMs.ToString("R", CultureInfo.InvariantCulture) + "|" + row.Kind + "|" + row.ActorId + "|" + row.PeerId + "|" + row.SubjectId + "|" + row.CorrelationId + "|" + row.Term + "|" + row.LogIndex + "|" + row.Outcome + "|" + row.Detail + "|" + row.Lamport + "|" + row.VectorClock); rt.PriorEventHash = row.EventHash; rt.Result.Trace.Add(row);
        }

        private static void TouchClock(Runtime rt, string actor, string peer)
        {
            if (String.IsNullOrWhiteSpace(actor)) actor = "SYSTEM"; long local = Clock(rt, actor), remote = Clock(rt, peer); rt.Lamport[actor] = Math.Max(local, remote) + 1; Dictionary<string, long> av = VectorMap(rt, actor), pv = VectorMap(rt, peer); foreach (KeyValuePair<string, long> pair in pv) if (!av.ContainsKey(pair.Key) || av[pair.Key] < pair.Value) av[pair.Key] = pair.Value; av[actor] = (av.ContainsKey(actor) ? av[actor] : 0) + 1;
        }

        private static long Clock(Runtime rt, string actor) { if (String.IsNullOrWhiteSpace(actor)) actor = "SYSTEM"; long value; return rt.Lamport.TryGetValue(actor, out value) ? value : 0; }
        private static Dictionary<string, long> VectorMap(Runtime rt, string actor) { if (String.IsNullOrWhiteSpace(actor)) actor = "SYSTEM"; Dictionary<string, long> value; if (!rt.Vectors.TryGetValue(actor, out value)) { value = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase); rt.Vectors[actor] = value; } return value; }
        private static string Vector(Runtime rt, string actor) { return FormatVector(VectorMap(rt, actor)); }

        private static void UpdateReplica(Runtime rt, string processId, ScheduledEvent ev, bool committed)
        {
            string[] fields = (ev.Payload ?? "").Split('|'); string dataset = fields.Length > 0 ? fields[0] : null, shard = fields.Length > 1 ? fields[1] : null; foreach (RuntimeReplica r in rt.Replicas.Values.Where(x => Eq(x.Source.ProcessId, processId) && (String.IsNullOrWhiteSpace(dataset) || Eq(x.Source.DatasetId, dataset)) && (String.IsNullOrWhiteSpace(shard) || Eq(x.Source.ShardId, shard)))) { r.LogIndex = Math.Max(r.LogIndex, ev.LogIndex); r.Version = Math.Max(r.Version, ev.LogIndex); r.VectorClock = MergeVectorClocks(r.VectorClock, Vector(rt, processId), processId); r.StateHash = HashText((r.StateHash ?? "GENESIS") + "|" + ev.LogIndex + "|" + ev.Payload); if (committed) { r.CommitIndex = Math.Max(r.CommitIndex, ev.LogIndex); r.AppliedIndex = Math.Max(r.AppliedIndex, r.CommitIndex); } }
        }

        private static void MeasureLeaderless(Runtime rt)
        {
            bool hasLeader = rt.Processes.Values.Any(x => Available(rt, x) && Eq(x.Role, "LEADER")); if (hasLeader) rt.LastLeaderPresentAt = rt.Now; else { double next = rt.Queue.Count == 0 ? rt.Result.RequestedDurationMs : Math.Min(rt.Result.RequestedDurationMs, rt.Queue.Min(x => x.AtMs)); rt.Result.LeaderlessMs += Math.Max(0, next - rt.Now); }
        }

        private static void FinalizeSimulation(Runtime rt)
        {
            foreach (OperationState op in rt.Operations.Values.Where(x => Eq(x.Outcome, "PENDING"))) { op.Outcome = "TIMEOUT"; op.Completed = rt.Result.SimulatedThroughMs; rt.Result.OperationsTimedOut++; }
            int terminal = rt.Result.OperationsCompleted + rt.Result.OperationsTimedOut; rt.Result.Availability = terminal == 0 ? (rt.Processes.Values.Any(x => Available(rt, x)) ? 1 : 0) : rt.Result.OperationsCompleted / (double)terminal; rt.Latencies.Sort(); rt.Result.MeanLatencyMs = rt.Latencies.Count == 0 ? 0 : rt.Latencies.Average(); rt.Result.P95LatencyMs = Percentile(rt.Latencies, 0.95); rt.Result.P99LatencyMs = Percentile(rt.Latencies, 0.99); rt.Result.RecoveryTimeMs = rt.LastFaultStartedAt < 0 || rt.LastRecoveryAt < 0 ? 0 : Math.Max(0, rt.LastRecoveryAt - rt.LastFaultStartedAt); rt.Result.MaximumReplicaLag = rt.Replicas.Count == 0 ? 0 : (int)Math.Min(Int32.MaxValue, rt.Replicas.Values.Max(x => Math.Max(0, x.LogIndex - x.AppliedIndex))); rt.Result.DataLossWindowMs = EstimateDataLossWindow(rt);
            rt.Result.FinalProcesses.AddRange(rt.Processes.Values.Select(x => new DistributedProcess { Id = x.Source.Id, Name = x.Source.Name, Service = x.Source.Service, NodeId = x.Source.NodeId, GroupId = x.Source.GroupId, Role = x.Role, Term = x.Term, Epoch = x.Source.Epoch, LastLogIndex = x.LogIndex, CommitIndex = x.CommitIndex, AppliedIndex = x.AppliedIndex, Priority = x.Source.Priority, Voting = x.Source.Voting, AutoRestart = x.Source.AutoRestart, RestartDelayMs = x.Source.RestartDelayMs, Health = Available(rt, x) ? "HEALTHY" : "UNAVAILABLE", EvidenceNodeIds = new List<string>(x.Source.EvidenceNodeIds ?? new List<string>()) }).OrderBy(x => x.Id));
            rt.Result.FinalReplicas.AddRange(rt.Replicas.Values.Select(x => new DistributedReplica { Id = x.Source.Id, DatasetId = x.Source.DatasetId, ShardId = x.Source.ShardId, NodeId = x.Source.NodeId, ProcessId = x.Source.ProcessId, Role = x.Source.Role, Term = x.Source.Term, LogIndex = x.LogIndex, CommitIndex = x.CommitIndex, AppliedIndex = x.AppliedIndex, Version = x.Version, StateHash = x.StateHash, VectorClock = x.VectorClock, DurableThroughMs = x.Source.DurableThroughMs, Status = x.Online ? "ONLINE" : "OFFLINE" }).OrderBy(x => x.Id));
            AuditInvariants(rt); rt.Result.Consistency.AddRange(AnalyzeConsistency(rt)); rt.Result.Audit.Add("Trace hash chain root=" + rt.PriorEventHash + "."); rt.Result.Audit.Add("Message accounting sent/delivered/dropped/duplicated=" + rt.Result.MessagesSent + "/" + rt.Result.MessagesDelivered + "/" + rt.Result.MessagesDropped + "/" + rt.Result.MessagesDuplicated + "."); rt.Result.TraceRootHash = rt.PriorEventHash;
        }

        private static void AuditInvariants(Runtime rt)
        {
            foreach (IGrouping<long, DistributedTraceEvent> term in rt.Result.Trace.Where(x => Eq(x.Kind, "LEADER_ELECTED")).GroupBy(x => x.Term)) { List<string> leaders = term.Select(x => x.ActorId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); if (leaders.Count > 1) Finding(rt.Result, "ERROR", "ONE_LEADER_PER_TERM", String.Join(",", leaders.ToArray()), term.Min(x => x.AtMs), "Multiple distinct leaders were elected in the same modeled term.", String.Join("|", term.Select(x => x.EventHash).ToArray())); }
            foreach (DistributedProcess p in rt.Result.FinalProcesses) { if (p.AppliedIndex > p.CommitIndex) Finding(rt.Result, "ERROR", "APPLIED_NOT_AHEAD_OF_COMMIT", p.Id, rt.Result.SimulatedThroughMs, p.AppliedIndex + " > " + p.CommitIndex, p.ToString()); if (p.CommitIndex > p.LastLogIndex) Finding(rt.Result, "ERROR", "COMMIT_NOT_AHEAD_OF_LOG", p.Id, rt.Result.SimulatedThroughMs, p.CommitIndex + " > " + p.LastLogIndex, p.ToString()); }
            foreach (DistributedReplica r in rt.Result.FinalReplicas) { if (r.AppliedIndex > r.CommitIndex || r.CommitIndex > r.LogIndex) Finding(rt.Result, "ERROR", "REPLICA_INDEX_MONOTONICITY", r.Id, rt.Result.SimulatedThroughMs, "Expected applied ≤ commit ≤ log.", r.ToString()); }
            foreach (IGrouping<string, DistributedTraceEvent> op in rt.Result.Trace.Where(x => Eq(x.Kind, "WRITE_COMMITTED")).GroupBy(x => x.SubjectId ?? "", StringComparer.OrdinalIgnoreCase)) if (op.Select(x => x.LogIndex).Distinct().Count() > 1) Finding(rt.Result, "ERROR", "SINGLE_COMMIT_VERSION_PER_OPERATION", op.Key, op.Min(x => x.AtMs), "One operation was acknowledged at multiple log indexes.", String.Join("|", op.Select(x => x.EventHash).ToArray()));
            foreach (DistributedDataset d in rt.Model.Datasets ?? new List<DistributedDataset>()) if (d != null && (d.ReadQuorum + d.WriteQuorum <= d.ReplicationFactor || d.WriteQuorum * 2 <= d.ReplicationFactor)) Finding(rt.Result, "WARN", "QUORUM_INTERSECTION", d.Id, 0, "Declared R/W quorums permit disjoint operations.", d.ToString());
            if (rt.Result.Invariants.Count == 0) Finding(rt.Result, "INFO", "BOUNDED_INVARIANT_AUDIT", rt.Model.Id, rt.Result.SimulatedThroughMs, "No invariant violation was found in the explored bounded trace; this is not a universal proof.", rt.Result.TraceRootHash);
        }

        private static List<DistributedConsistencyReport> AnalyzeConsistency(Runtime rt)
        {
            List<DistributedConsistencyReport> reports = new List<DistributedConsistencyReport>();
            foreach (DistributedDataset dataset in rt.Model.Datasets ?? new List<DistributedDataset>())
            {
                if (dataset == null) continue; DistributedConsistencyReport report = new DistributedConsistencyReport { DatasetId = dataset.Id }; Dictionary<string, long> lastSeen = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase); Dictionary<string, long> lastWrite = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                foreach (OperationState state in rt.Operations.Values.Where(x => x.Source != null && Eq(x.Source.DatasetId, dataset.Id)).OrderBy(x => x.Started).ThenBy(x => x.Source.Id))
                {
                    bool write = Eq(state.Source.Kind, "WRITE") || Eq(state.Source.Kind, "PUT") || Eq(state.Source.Kind, "DELETE"); string session = (state.Source.ClientId ?? "CLIENT") + "|" + (state.Source.Key ?? "KEY"); if (write) { report.Writes++; if (Eq(state.Outcome, "ACKNOWLEDGED")) { report.AcknowledgedWrites++; lastWrite[session] = Math.Max(lastWrite.ContainsKey(session) ? lastWrite[session] : 0, state.ObservedVersion); } continue; }
                    report.Reads++; long monotonic = lastSeen.ContainsKey(session) ? lastSeen[session] : 0, own = lastWrite.ContainsKey(session) ? lastWrite[session] : 0, required = Math.Max(monotonic, own); string anomaly = null; if (state.ObservedVersion < LatestCommitted(rt, dataset.Id, state.Completed)) { report.StaleReads++; anomaly = "STALE_READ"; } if (state.ObservedVersion < monotonic) { report.MonotonicReadViolations++; anomaly = JoinAnomaly(anomaly, "NON_MONOTONIC_READ"); } if (state.ObservedVersion < own) { report.ReadYourWritesViolations++; anomaly = JoinAnomaly(anomaly, "READ_YOUR_WRITES"); } lastSeen[session] = Math.Max(monotonic, state.ObservedVersion); report.Observations.Add(new DistributedConsistencyObservation { OperationId = state.Source.Id, ClientId = state.Source.ClientId, DatasetId = dataset.Id, Key = state.Source.Key, Kind = state.Source.Kind, ObservedVersion = state.ObservedVersion, RequiredVersion = required, StartedMs = state.Started, CompletedMs = state.Completed, Verdict = anomaly == null ? "CONSISTENT_IN_TRACE" : "ANOMALY", Anomaly = anomaly ?? "NONE" });
                }
                HashSet<long> finalCommitted = new HashSet<long>(rt.Result.FinalReplicas.Where(x => Eq(x.DatasetId, dataset.Id)).Select(x => x.CommitIndex)); foreach (OperationState state in rt.Operations.Values.Where(x => x.Source != null && Eq(x.Source.DatasetId, dataset.Id) && Eq(x.Outcome, "ACKNOWLEDGED"))) if (!rt.Result.FinalReplicas.Any(x => Eq(x.DatasetId, dataset.Id) && x.CommitIndex >= state.ObservedVersion)) report.LostAcknowledgedWrites++;
                report.ConcurrentVersions = rt.Result.FinalReplicas.Where(x => Eq(x.DatasetId, dataset.Id)).Select(x => x.StateHash ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count(); int anomalies = report.StaleReads + report.MonotonicReadViolations + report.ReadYourWritesViolations + report.LostAcknowledgedWrites; report.Verdict = anomalies > 0 ? "ANOMALIES_OBSERVED" : report.ConcurrentVersions > 1 && Eq(dataset.ConsistencyModel, "LINEARIZABLE") ? "DIVERGENT_REPLICAS" : "NO_ANOMALY_IN_BOUNDED_TRACE"; report.Audit.Add("Declared model=" + dataset.ConsistencyModel + ", N/R/W=" + dataset.ReplicationFactor + "/" + dataset.ReadQuorum + "/" + dataset.WriteQuorum + "."); report.Audit.Add("Absence of an observed anomaly is bounded evidence, not proof for all executions."); report.CertificateHash = HashText(report.DatasetId + "|" + report.Reads + "|" + report.Writes + "|" + report.AcknowledgedWrites + "|" + report.StaleReads + "|" + report.MonotonicReadViolations + "|" + report.ReadYourWritesViolations + "|" + report.LostAcknowledgedWrites + "|" + report.ConcurrentVersions + "|" + report.Verdict + "|" + String.Join("|", report.Observations.Select(x => x.ToString()).ToArray())); reports.Add(report);
            }
            return reports;
        }

        private static long LatestCommitted(Runtime rt, string datasetId, double atMs) { return rt.Result.Trace.Where(x => x != null && Eq(x.Kind, "WRITE_COMMITTED") && x.AtMs <= atMs + 0.000001).Select(x => x.LogIndex).DefaultIfEmpty(0).Max(); }
        private static string JoinAnomaly(string left, string right) { return String.IsNullOrWhiteSpace(left) ? right : left + "+" + right; }
        private static double EstimateDataLossWindow(Runtime rt) { List<OperationState> ack = rt.Operations.Values.Where(x => Eq(x.Outcome, "ACKNOWLEDGED")).OrderBy(x => x.Completed).ToList(); if (ack.Count == 0) return 0; long durable = rt.Replicas.Values.Select(x => x.CommitIndex).DefaultIfEmpty(0).Max(); List<OperationState> lost = ack.Where(x => x.ObservedVersion > durable).ToList(); return lost.Count == 0 ? 0 : Math.Max(0, lost.Max(x => x.Completed) - lost.Min(x => x.Completed)); }

        private static string RouteOperation(Runtime rt, DistributedWorkloadOperation op)
        {
            List<RuntimeProcess> candidates = rt.Processes.Values.Where(x => Available(rt, x) && (String.IsNullOrWhiteSpace(op.ShardId) || rt.Replicas.Values.Any(r => Eq(r.Source.ProcessId, x.Source.Id) && Eq(r.Source.ShardId, op.ShardId)))).ToList(); bool write = Eq(op.Kind, "WRITE") || Eq(op.Kind, "PUT") || Eq(op.Kind, "DELETE"); if (write) { RuntimeProcess leader = candidates.FirstOrDefault(x => Eq(x.Role, "LEADER")); return leader == null ? null : leader.Source.Id; } RuntimeProcess local = candidates.OrderBy(x => ProcessDelay(rt.Model, rt.Scenario, op.ClientId, x.Source.Id, 128, op.Id)).ThenByDescending(x => x.AppliedIndex).FirstOrDefault(); return local == null ? null : local.Source.Id;
        }

        private static DistributedLink FindLink(DistributedSystemModel model, string fromNode, string toNode) { return (model.Links ?? new List<DistributedLink>()).FirstOrDefault(x => x != null && Active(x.Status) && ((Eq(x.FromNodeId, fromNode) && Eq(x.ToNodeId, toNode)) || (x.Bidirectional && Eq(x.FromNodeId, toNode) && Eq(x.ToNodeId, fromNode)))); }
        private static bool LinkAvailable(Runtime rt, DistributedLink link, string fromNode, string toNode) { if (link == null || rt.DownLinks.Contains(link.Id ?? "")) return false; if (!link.Bidirectional && !Eq(link.FromNodeId, fromNode)) return false; if (rt.ActivePartitions.Contains(PairKey(fromNode, toNode, true)) || rt.ActivePartitions.Contains(PairKey(fromNode, toNode, false))) return false; return true; }
        private static bool Available(Runtime rt, RuntimeProcess p) { return p != null && p.Up && !rt.DownProcesses.Contains(p.Source.Id ?? "") && !rt.DownNodes.Contains(p.Source.NodeId ?? ""); }
        private static RuntimeProcess Leader(Runtime rt, string group) { string id; RuntimeProcess p; return rt.LatestLeaderByGroup.TryGetValue(group ?? "default", out id) && rt.Processes.TryGetValue(id ?? "", out p) && Available(rt, p) && Eq(p.Role, "LEADER") ? p : rt.Processes.Values.FirstOrDefault(x => Available(rt, x) && Eq(x.Source.GroupId, group) && Eq(x.Role, "LEADER")); }
        private static List<RuntimeProcess> Group(Runtime rt, string group) { return rt.Processes.Values.Where(x => Eq(x.Source.GroupId, group)).ToList(); }
        private static int VotingCount(Runtime rt, string group) { return Group(rt, group).Count(x => x.Source.Voting); }
        private static int Majority(int n) { return Math.Max(1, n / 2 + 1); }
        private static double ElectionDeadline(DistributedSystemModel model, DistributedProcess p, double now) { double min = Math.Max(10, Parameter(model, "election_min_ms", 2200)), max = Math.Max(min + 1, Parameter(model, "election_max_ms", 4200)); return now + min + (max - min) * StableUnit(model.Seed, "election|" + (p == null ? "PROCESS" : p.Id) + "|" + now.ToString("R", CultureInfo.InvariantCulture)); }

        private static HashSet<string> UnavailableNodes(DistributedSystemModel model, DistributedScenario scenario)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase); HashSet<string> enabled = EnabledFaults(model, scenario); foreach (DistributedFault f in model.Faults ?? new List<DistributedFault>()) if (f != null && Active(f.Status) && enabled.Contains(f.Id) && (Eq(f.Kind, "NODE_CRASH") || Eq(f.Kind, "NODE_PAUSE") || Eq(f.Kind, "DISK_FAILURE"))) set.Add(f.TargetId ?? ""); return set;
        }
        private static HashSet<string> UnavailableProcesses(DistributedSystemModel model, DistributedScenario scenario) { HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase); HashSet<string> nodes = UnavailableNodes(model, scenario); foreach (DistributedProcess p in model.Processes ?? new List<DistributedProcess>()) if (p != null && nodes.Contains(p.NodeId ?? "")) set.Add(p.Id); HashSet<string> enabled = EnabledFaults(model, scenario); foreach (DistributedFault f in model.Faults ?? new List<DistributedFault>()) if (f != null && Active(f.Status) && enabled.Contains(f.Id) && (Eq(f.Kind, "PROCESS_CRASH") || Eq(f.Kind, "PROCESS_PAUSE"))) set.Add(f.TargetId ?? ""); return set; }
        private static HashSet<string> EnabledFaults(DistributedSystemModel model, DistributedScenario scenario) { HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase); if (scenario == null || String.IsNullOrWhiteSpace(scenario.EnabledFaultIds)) return result; foreach (string token in scenario.EnabledFaultIds.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) result.Add(token.Trim()); return result; }

        private static bool ProcessAvailable(DistributedSystemModel model, DistributedScenario scenario, string processId) { if (model == null) return false; DistributedProcess p = (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, processId)); return p != null && Active(p.Health) && !UnavailableProcesses(model, scenario).Contains(p.Id); }
        private static bool CanProcessesCommunicate(DistributedSystemModel model, DistributedScenario scenario, string fromProcess, string toProcess) { if (model == null) return false; DistributedProcess a = (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, fromProcess)), b = (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, toProcess)); if (a == null || b == null) return true; if (Eq(a.NodeId, b.NodeId)) return true; DistributedLink link = FindLink(model, a.NodeId, b.NodeId); if (link == null || !Active(link.Status)) return false; HashSet<string> enabled = EnabledFaults(model, scenario); foreach (DistributedFault f in model.Faults ?? new List<DistributedFault>()) if (f != null && Active(f.Status) && enabled.Contains(f.Id) && ((Eq(f.Kind, "LINK_DOWN") && Eq(f.TargetId, link.Id)) || ((Eq(f.Kind, "PARTITION") || Eq(f.Kind, "ASYMMETRIC_PARTITION")) && PartitionMatches(f, a.NodeId, b.NodeId)))) return false; return true; }
        private static bool PartitionMatches(DistributedFault f, string from, string to) { return Eq(f.TargetId, from) && Eq(f.SecondaryTargetId, to) || (Eq(f.Kind, "PARTITION") && Eq(f.TargetId, to) && Eq(f.SecondaryTargetId, from)); }
        private static double ProcessDelay(DistributedSystemModel model, DistributedScenario scenario, string fromProcess, string toProcess, int bytes, string salt) { if (model == null) return Double.PositiveInfinity; DistributedProcess a = (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, fromProcess)), b = (model.Processes ?? new List<DistributedProcess>()).FirstOrDefault(x => x != null && Eq(x.Id, toProcess)); if (a == null || b == null || Eq(a.NodeId, b.NodeId)) return 0.1; DistributedLink link = FindLink(model, a.NodeId, b.NodeId); if (link == null) return Double.PositiveInfinity; double jitter = (StableUnit(model.Seed, "delay|" + salt + "|" + fromProcess + "|" + toProcess) * 2 - 1) * link.JitterMs; return Math.Max(0.001, (link.BaseLatencyMs + jitter + bytes * 8.0 / Math.Max(1, link.BandwidthMbps * 1000000.0) * 1000.0) * (scenario == null ? 1 : Math.Max(0, scenario.LatencyMultiplier))); }

        private static List<string> DeadlockCycles(IEnumerable<string> edges)
        {
            Dictionary<string, List<string>> graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); foreach (string edge in edges ?? Enumerable.Empty<string>()) { string[] p = edge.Split(new[] { '→', '>', '|' }, StringSplitOptions.RemoveEmptyEntries); if (p.Length < 2) continue; if (!graph.ContainsKey(p[0])) graph[p[0]] = new List<string>(); graph[p[0]].Add(p[1]); if (!graph.ContainsKey(p[1])) graph[p[1]] = new List<string>(); } List<List<string>> scc = StrongComponents(graph.Keys.ToList(), graph); return scc.Where(x => x.Count > 1 || (x.Count == 1 && graph[x[0]].Contains(x[0], StringComparer.OrdinalIgnoreCase))).Select(x => String.Join("→", x.Concat(new[] { x[0] }).ToArray())).ToList();
        }

        private static int Components(List<string> nodes, Dictionary<string, HashSet<string>> graph) { int count = 0; HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string start in nodes) if (seen.Add(start)) { count++; Queue<string> q = new Queue<string>(); q.Enqueue(start); while (q.Count > 0) { string u = q.Dequeue(); foreach (string v in graph[u]) if (seen.Add(v)) q.Enqueue(v); } } return count; }
        private static List<List<string>> StrongComponents(List<string> nodes, Dictionary<string, List<string>> graph) { List<List<string>> result = new List<List<string>>(); Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> on = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int next = 0; foreach (string v in nodes) if (!index.ContainsKey(v)) StrongDfs(v, graph, index, low, stack, on, ref next, result); return result; }
        private static void StrongDfs(string v, Dictionary<string, List<string>> graph, Dictionary<string, int> index, Dictionary<string, int> low, Stack<string> stack, HashSet<string> on, ref int next, List<List<string>> result) { index[v] = low[v] = next++; stack.Push(v); on.Add(v); List<string> adjacent; if (!graph.TryGetValue(v, out adjacent)) adjacent = new List<string>(); foreach (string w in adjacent) { if (!index.ContainsKey(w)) { StrongDfs(w, graph, index, low, stack, on, ref next, result); low[v] = Math.Min(low[v], low[w]); } else if (on.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> component = new List<string>(); string w; do { w = stack.Pop(); on.Remove(w); component.Add(w); } while (!Eq(w, v)); result.Add(component); } }
        private static void ArticulationDfs(string u, string parentId, Dictionary<string, HashSet<string>> graph, List<DistributedLink> edges, Dictionary<string, int> discovery, Dictionary<string, int> low, Dictionary<string, string> parent, ref int time, List<string> articulations, List<string> bridges) { discovery[u] = low[u] = ++time; int children = 0; foreach (string v in graph[u]) { if (!discovery.ContainsKey(v)) { children++; parent[v] = u; ArticulationDfs(v, u, graph, edges, discovery, low, parent, ref time, articulations, bridges); low[u] = Math.Min(low[u], low[v]); if (parentId == null && children > 1) articulations.Add(u); if (parentId != null && low[v] >= discovery[u]) articulations.Add(u); if (low[v] > discovery[u]) { DistributedLink link = edges.FirstOrDefault(x => x != null && ((Eq(x.FromNodeId, u) && Eq(x.ToNodeId, v)) || (Eq(x.FromNodeId, v) && Eq(x.ToNodeId, u)))); if (link != null) bridges.Add(link.Id); } } else if (!Eq(v, parentId)) low[u] = Math.Min(low[u], discovery[v]); } }

        private static DistributedTopologyResult SealTopology(DistributedTopologyResult r) { r.CertificateHash = HashText(r.ActiveNodes + "|" + r.ActiveLinks + "|" + r.WeakComponents + "|" + r.StrongComponents + "|" + r.Density.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join(",", r.ArticulationNodeIds.ToArray()) + "|" + String.Join(",", r.BridgeLinkIds.ToArray()) + "|" + String.Join(",", r.SingleRegionDatasetIds.ToArray()) + "|" + String.Join(",", r.SingleDomainDatasetIds.ToArray()) + "|" + String.Join(",", r.QuorumLossDomainIds.ToArray()) + "|" + String.Join("|", r.Audit.ToArray())); return r; }
        private static DistributedSimulationResult SealSimulation(DistributedSimulationResult r) { r.CertificateHash = HashText((r.ModelFingerprint ?? "") + "|" + (r.ScenarioId ?? "") + "|" + r.RequestedDurationMs.ToString("R", CultureInfo.InvariantCulture) + "|" + r.SimulatedThroughMs.ToString("R", CultureInfo.InvariantCulture) + "|" + r.EventsProcessed + "|" + r.MessagesSent + "|" + r.MessagesDelivered + "|" + r.MessagesDropped + "|" + r.Elections + "|" + r.LeaderChanges + "|" + r.OperationsCompleted + "|" + r.OperationsTimedOut + "|" + r.Availability.ToString("R", CultureInfo.InvariantCulture) + "|" + r.P95LatencyMs.ToString("R", CultureInfo.InvariantCulture) + "|" + r.TraceRootHash + "|" + String.Join("|", r.Invariants.Select(x => x.ToString()).ToArray()) + "|" + String.Join("|", r.Consistency.Select(x => x.CertificateHash).ToArray())); return r; }
        private static DistributedGossipResult SealGossip(DistributedGossipResult r) { r.CertificateHash = HashText((r.ScenarioId ?? "") + "|" + r.Members + "|" + r.RequestedRounds + "|" + r.ConvergedRound + "|" + r.TotalMessages + "|" + r.FinalKnowledgeRatio.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join("|", r.Rounds.Select(x => x.StateHash).ToArray()) + "|" + String.Join(",", r.IsolatedMemberIds.ToArray())); return r; }
        private static DistributedChaosCampaignResult SealCampaign(DistributedChaosCampaignResult r) { r.CertificateHash = HashText(r.RequestedWorlds + "|" + r.CompletedWorlds + "|" + r.WeightedAvailability.ToString("R", CultureInfo.InvariantCulture) + "|" + r.WorstRecoveryTimeMs.ToString("R", CultureInfo.InvariantCulture) + "|" + r.WorstScenarioId + "|" + String.Join("|", r.Worlds.Select(x => x.ToString() + ":" + x.SimulationHash).ToArray()) + "|" + String.Join("|", r.Recommendations.ToArray())); return r; }
        private static void Finding(DistributedSimulationResult result, string severity, string invariant, string subject, double at, string detail, string witness) { result.Invariants.Add(new DistributedInvariantFinding { Severity = severity, Invariant = invariant, SubjectId = subject, AtMs = at, Detail = detail, WitnessHash = HashText(witness ?? "") }); }
        private static double FaultValue(Dictionary<string, double> values, string key) { double value, star; value = values != null && values.TryGetValue(key ?? "", out value) ? value : 0; star = values != null && values.TryGetValue("*", out star) ? star : 0; return Math.Max(value, star); }
        private static string PairKey(string a, string b, bool symmetric) { a = a ?? ""; b = b ?? ""; if (symmetric && String.Compare(a, b, StringComparison.OrdinalIgnoreCase) > 0) { string t = a; a = b; b = t; } return a + "→" + b; }
        private static double StableUnit(int seed, string salt) { byte[] bytes; using (SHA256 sha = SHA256.Create()) bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + "|" + (salt ?? ""))); ulong v = BitConverter.ToUInt64(bytes, 0); return v / (double)UInt64.MaxValue; }
        private static Dictionary<string, long> ParseVector(string raw) { Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase); foreach (string token in (raw ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) { int i = token.IndexOf('='); if (i <= 0) continue; long value; if (Int64.TryParse(token.Substring(i + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) result[token.Substring(0, i).Trim()] = Math.Max(0, value); } return result; }
        private static string FormatVector(Dictionary<string, long> vector) { return String.Join(",", (vector ?? new Dictionary<string, long>()).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key + "=" + x.Value.ToString(CultureInfo.InvariantCulture)).ToArray()); }
        private static double Parameter(DistributedSystemModel model, string key, double fallback) { double value; return model != null && model.Parameters != null && model.Parameters.TryGetValue(key, out value) && Finite(value) ? value : fallback; }
        private static HashSet<string> IdSet(IEnumerable<string> ids) { return new HashSet<string>((ids ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase); }
        private static void CheckIds(List<DistributedValidationIssue> issues, string kind, IEnumerable<string> ids) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string id in ids ?? Enumerable.Empty<string>()) { if (String.IsNullOrWhiteSpace(id)) Issue(issues, "ERROR", "MISSING_ID", kind, "Entity has no stable identifier."); else if (!seen.Add(id)) Issue(issues, "ERROR", "DUPLICATE_ID", id, "Duplicate " + kind + " identifier."); } }
        private static void CheckCap<T>(List<DistributedValidationIssue> issues, string code, List<T> values, int cap) { if (values != null && values.Count > cap) Issue(issues, "FATAL", code, "SYSTEM", values.Count + " > " + cap); }
        private static void DetectParentCycles(Dictionary<string, string> parents, List<DistributedValidationIssue> issues) { foreach (string start in parents.Keys) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string cursor = start; while (!String.IsNullOrWhiteSpace(cursor) && parents.ContainsKey(cursor)) { if (!seen.Add(cursor)) { Issue(issues, "ERROR", "FAULT_DOMAIN_CYCLE", start, "Containment cycle through " + cursor + "."); break; } cursor = parents[cursor]; } } }
        private static void Issue(List<DistributedValidationIssue> issues, string severity, string code, string subject, string detail) { issues.Add(new DistributedValidationIssue { Severity = severity, Code = code, SubjectId = subject, Detail = detail }); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "DOWN") && !Eq(status, "FAILED") && !Eq(status, "RETIRED") && !Eq(status, "OFFLINE") && !Eq(status, "DESTROYED"); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static bool Probability(double value) { return Finite(value) && value >= 0 && value <= 1; }
        private static double Clamp01(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static double Percentile(List<double> sorted, double p) { if (sorted == null || sorted.Count == 0) return 0; int index = (int)Math.Ceiling(Clamp01(p) * sorted.Count) - 1; return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))]; }
        private static string HashText(string value) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class SpatialCoordinateReference
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Unit { get; set; }
        public int Epsg { get; set; }
        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public double Scale { get; set; }
        public string AxisOrder { get; set; }
        public string Status { get; set; }
        public SpatialCoordinateReference() { Kind = "GEOGRAPHIC"; Unit = "degree"; Scale = 1; AxisOrder = "X,Y,Z"; Status = "ACTIVE"; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "CRS") + "   ·   " + (Kind ?? "GEOGRAPHIC") + (Epsg > 0 ? " EPSG:" + Epsg.ToString(CultureInfo.InvariantCulture) : "") + "   ·   " + (Unit ?? "unit"); }
    }

    internal sealed class SpatialCoordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Measure { get; set; }
        public SpatialCoordinate() { }
        public SpatialCoordinate(double x, double y, double z) { X = x; Y = y; Z = z; }
        public override string ToString() { return X.ToString("0.######", CultureInfo.InvariantCulture) + ", " + Y.ToString("0.######", CultureInfo.InvariantCulture) + (Math.Abs(Z) > 0.0000001 ? ", " + Z.ToString("0.###", CultureInfo.InvariantCulture) : ""); }
    }

    internal sealed class SpatialLayer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int ZIndex { get; set; }
        public bool Visible { get; set; }
        public double Opacity { get; set; }
        public string Style { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialLayer() { Kind = "REFERENCE"; Visible = true; Opacity = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Visible ? "VISIBLE" : "HIDDEN") + "   ·   " + (Name ?? Id ?? "LAYER") + "   ·   " + (Kind ?? "REFERENCE") + "   ·   z" + ZIndex.ToString(CultureInfo.InvariantCulture) + "   ·   α " + Opacity.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialPlace
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string ParentId { get; set; }
        public string LayerId { get; set; }
        public string CrsId { get; set; }
        public SpatialCoordinate Position { get; set; }
        public double RadiusMeters { get; set; }
        public double Capacity { get; set; }
        public double Population { get; set; }
        public double ElevationMeters { get; set; }
        public string AccessClass { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialPlace() { Kind = "PLACE"; Position = new SpatialCoordinate(); AccessClass = "PUBLIC"; Status = "ACTIVE"; Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "PLACE") + "   ·   " + (Kind ?? "PLACE") + "   ·   " + Position + "   ·   cap/pop " + Capacity.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Population.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialRing
    {
        public string Id { get; set; }
        public bool Hole { get; set; }
        public List<SpatialCoordinate> Vertices { get; set; }
        public SpatialRing() { Vertices = new List<SpatialCoordinate>(); }
        public override string ToString() { return (Hole ? "HOLE" : "SHELL") + "   ·   " + Vertices.Count.ToString(CultureInfo.InvariantCulture) + " vertices"; }
    }

    internal sealed class SpatialTerritory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string ParentId { get; set; }
        public string LayerId { get; set; }
        public string CrsId { get; set; }
        public string Jurisdiction { get; set; }
        public string Status { get; set; }
        public List<SpatialRing> Rings { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialTerritory() { Kind = "REGION"; Status = "ACTIVE"; Rings = new List<SpatialRing>(); Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "TERRITORY") + "   ·   " + (Kind ?? "REGION") + "   ·   " + Rings.Sum(x => x == null ? 0 : x.Vertices.Count).ToString(CultureInfo.InvariantCulture) + " vertices   ·   " + (Jurisdiction ?? "NO JURISDICTION"); }
    }

    internal sealed class SpatialConnection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FromPlaceId { get; set; }
        public string ToPlaceId { get; set; }
        public string Mode { get; set; }
        public bool Bidirectional { get; set; }
        public double LengthMeters { get; set; }
        public double FreeFlowSeconds { get; set; }
        public double CapacityPerHour { get; set; }
        public double MonetaryCost { get; set; }
        public double EnergyCost { get; set; }
        public double Reliability { get; set; }
        public double Risk { get; set; }
        public double AccessibilityPenalty { get; set; }
        public string AccessClass { get; set; }
        public string InfrastructureId { get; set; }
        public string Status { get; set; }
        public List<SpatialCoordinate> Geometry { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialConnection() { Mode = "WALK"; Bidirectional = true; Reliability = 1; AccessClass = "PUBLIC"; Status = "ACTIVE"; Geometry = new List<SpatialCoordinate>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (FromPlaceId ?? "FROM") + (Bidirectional ? " ⇄ " : " → ") + (ToPlaceId ?? "TO") + "   ·   " + (Mode ?? "MODE") + "   ·   " + LengthMeters.ToString("0", CultureInfo.InvariantCulture) + "m / " + FreeFlowSeconds.ToString("0", CultureInfo.InvariantCulture) + "s   ·   cap " + CapacityPerHour.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialInfrastructure
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string PlaceId { get; set; }
        public string TerritoryId { get; set; }
        public double Capacity { get; set; }
        public double Utilization { get; set; }
        public double Condition { get; set; }
        public double Redundancy { get; set; }
        public double Criticality { get; set; }
        public string Owner { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> Commodities { get; set; }
        public List<string> DependencyIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialInfrastructure() { Kind = "FACILITY"; Condition = 1; Redundancy = 1; Status = "ACTIVE"; Commodities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); DependencyIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "INFRASTRUCTURE") + "   ·   " + (Kind ?? "FACILITY") + "   ·   cap/use " + Capacity.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Utilization.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   condition " + Condition.ToString("P0", CultureInfo.InvariantCulture) + "   ·   criticality " + Criticality.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string CurrentPlaceId { get; set; }
        public string DestinationPlaceId { get; set; }
        public string PreferredMode { get; set; }
        public double SpeedMetersPerSecond { get; set; }
        public double Size { get; set; }
        public double Priority { get; set; }
        public string AccessClass { get; set; }
        public string Status { get; set; }
        public List<string> ItineraryPlaceIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialEntity() { Kind = "AGENT"; PreferredMode = "WALK"; SpeedMetersPerSecond = 1.4; Size = 1; AccessClass = "PUBLIC"; Status = "READY"; ItineraryPlaceIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "READY") + "   ·   " + (Name ?? Id ?? "ENTITY") + "   ·   " + (Kind ?? "AGENT") + "   ·   " + (CurrentPlaceId ?? "∅") + " → " + (DestinationPlaceId ?? "∅") + "   ·   " + (PreferredMode ?? "WALK"); }
    }

    internal sealed class SpatialEvent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string PlaceId { get; set; }
        public string TerritoryId { get; set; }
        public double StartSecond { get; set; }
        public double EndSecond { get; set; }
        public double Magnitude { get; set; }
        public double RadiusMeters { get; set; }
        public string Status { get; set; }
        public List<string> AffectedModeIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialEvent() { Kind = "EVENT"; Status = "PLANNED"; AffectedModeIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "PLANNED") + "   ·   " + (Name ?? Id ?? "EVENT") + "   ·   " + (Kind ?? "EVENT") + "   ·   t[" + StartSecond.ToString("0", CultureInfo.InvariantCulture) + "," + EndSecond.ToString("0", CultureInfo.InvariantCulture) + "]   ·   magnitude " + Magnitude.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Weight { get; set; }
        public double DemandMultiplier { get; set; }
        public double SpeedMultiplier { get; set; }
        public double CapacityMultiplier { get; set; }
        public double ReliabilityMultiplier { get; set; }
        public double HazardMagnitude { get; set; }
        public string ClosedConnectionIds { get; set; }
        public string Status { get; set; }
        public List<string> Assumptions { get; set; }
        public SpatialScenario() { Weight = 1; DemandMultiplier = 1; SpeedMultiplier = 1; CapacityMultiplier = 1; ReliabilityMultiplier = 1; Status = "ACTIVE"; Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SCENARIO") + "   ·   demand/speed/capacity/reliability × " + DemandMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + SpeedMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + CapacityMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + ReliabilityMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   hazard " + HazardMagnitude.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialWorldModel
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
        public string DefaultCrsId { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<SpatialCoordinateReference> CoordinateSystems { get; set; }
        public List<SpatialLayer> Layers { get; set; }
        public List<SpatialPlace> Places { get; set; }
        public List<SpatialTerritory> Territories { get; set; }
        public List<SpatialConnection> Connections { get; set; }
        public List<SpatialInfrastructure> Infrastructure { get; set; }
        public List<SpatialEntity> Entities { get; set; }
        public List<SpatialEvent> Events { get; set; }
        public List<SpatialScenario> Scenarios { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public SpatialWorldModel() { Status = "ACTIVE"; Revision = 1; Seed = 1337; Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); CoordinateSystems = new List<SpatialCoordinateReference>(); Layers = new List<SpatialLayer>(); Places = new List<SpatialPlace>(); Territories = new List<SpatialTerritory>(); Connections = new List<SpatialConnection>(); Infrastructure = new List<SpatialInfrastructure>(); Entities = new List<SpatialEntity>(); Events = new List<SpatialEvent>(); Scenarios = new List<SpatialScenario>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SPATIAL WORLD") + "   ·   " + Places.Count.ToString(CultureInfo.InvariantCulture) + " places / " + Territories.Count.ToString(CultureInfo.InvariantCulture) + " territories / " + Connections.Count.ToString(CultureInfo.InvariantCulture) + " connections / " + Entities.Count.ToString(CultureInfo.InvariantCulture) + " movers   ·   r" + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialValidationIssue
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string SubjectId { get; set; }
        public string Detail { get; set; }
        public override string ToString() { return (Severity ?? "INFO") + "   ·   " + (Code ?? "ISSUE") + "   ·   " + (SubjectId ?? "WORLD") + "   ·   " + (Detail ?? ""); }
    }

    internal sealed class SpatialRouteLeg
    {
        public int Sequence { get; set; }
        public string ConnectionId { get; set; }
        public string FromPlaceId { get; set; }
        public string ToPlaceId { get; set; }
        public string Mode { get; set; }
        public double DistanceMeters { get; set; }
        public double TravelSeconds { get; set; }
        public double GeneralizedCost { get; set; }
        public double Capacity { get; set; }
        public override string ToString() { return Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   " + (FromPlaceId ?? "FROM") + " → " + (ToPlaceId ?? "TO") + "   ·   " + (Mode ?? "MODE") + "   ·   " + DistanceMeters.ToString("0", CultureInfo.InvariantCulture) + "m / " + TravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   g=" + GeneralizedCost.ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialRouteResult
    {
        public string OriginPlaceId { get; set; }
        public string DestinationPlaceId { get; set; }
        public string ScenarioId { get; set; }
        public string Status { get; set; }
        public double TotalDistanceMeters { get; set; }
        public double TotalTravelSeconds { get; set; }
        public double TotalGeneralizedCost { get; set; }
        public double BottleneckCapacity { get; set; }
        public string RouteHash { get; set; }
        public List<SpatialRouteLeg> Legs { get; set; }
        public List<string> Diagnostics { get; set; }
        public SpatialRouteResult() { Status = "UNREACHABLE"; BottleneckCapacity = Double.PositiveInfinity; Legs = new List<SpatialRouteLeg>(); Diagnostics = new List<string>(); }
        public override string ToString() { return (Status ?? "UNREACHABLE") + "   ·   " + (OriginPlaceId ?? "FROM") + " → " + (DestinationPlaceId ?? "TO") + "   ·   " + TotalDistanceMeters.ToString("0", CultureInfo.InvariantCulture) + "m / " + TotalTravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   " + Legs.Count.ToString(CultureInfo.InvariantCulture) + " legs   ·   " + Short(RouteHash); }
        private static string Short(string value) { return String.IsNullOrWhiteSpace(value) ? "∅" : value.Substring(0, Math.Min(16, value.Length)); }
    }

    internal sealed class SpatialReachabilityRecord
    {
        public string PlaceId { get; set; }
        public double TravelSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public int Transfers { get; set; }
        public override string ToString() { return (PlaceId ?? "PLACE") + "   ·   " + TravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   " + DistanceMeters.ToString("0", CultureInfo.InvariantCulture) + "m   ·   " + Transfers.ToString(CultureInfo.InvariantCulture) + " transfers"; }
    }

    internal sealed class SpatialTopologyResult
    {
        public int ActivePlaces { get; set; }
        public int ActiveConnections { get; set; }
        public int WeakComponents { get; set; }
        public int StrongComponents { get; set; }
        public int ParentCycles { get; set; }
        public int TerritoryOverlaps { get; set; }
        public double NetworkDensity { get; set; }
        public List<List<string>> WeakComponentMembers { get; set; }
        public List<List<string>> StrongComponentMembers { get; set; }
        public List<string> ArticulationPlaceIds { get; set; }
        public List<string> BridgeConnectionIds { get; set; }
        public List<SpatialValidationIssue> Issues { get; set; }
        public string CertificateHash { get; set; }
        public SpatialTopologyResult() { WeakComponentMembers = new List<List<string>>(); StrongComponentMembers = new List<List<string>>(); ArticulationPlaceIds = new List<string>(); BridgeConnectionIds = new List<string>(); Issues = new List<SpatialValidationIssue>(); }
        public override string ToString() { return ActivePlaces.ToString(CultureInfo.InvariantCulture) + " places / " + ActiveConnections.ToString(CultureInfo.InvariantCulture) + " connections   ·   weak/strong " + WeakComponents.ToString(CultureInfo.InvariantCulture) + "/" + StrongComponents.ToString(CultureInfo.InvariantCulture) + "   ·   articulation/bridges " + ArticulationPlaceIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + BridgeConnectionIds.Count.ToString(CultureInfo.InvariantCulture) + "   ·   " + Issues.Count.ToString(CultureInfo.InvariantCulture) + " issues"; }
    }

    internal sealed class SpatialMovementRecord
    {
        public int Tick { get; set; }
        public string EntityId { get; set; }
        public string FromPlaceId { get; set; }
        public string ToPlaceId { get; set; }
        public string ConnectionId { get; set; }
        public double DepartSecond { get; set; }
        public double ArriveSecond { get; set; }
        public double CongestionMultiplier { get; set; }
        public string Status { get; set; }
        public override string ToString() { return "t" + Tick.ToString(CultureInfo.InvariantCulture) + "   ·   " + (EntityId ?? "ENTITY") + "   ·   " + (FromPlaceId ?? "FROM") + " → " + (ToPlaceId ?? "TO") + "   ·   " + DepartSecond.ToString("0.##", CultureInfo.InvariantCulture) + "–" + ArriveSecond.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   congestion ×" + CongestionMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Status ?? "MOVED"); }
    }

    internal sealed class SpatialMovementResult
    {
        public string ScenarioId { get; set; }
        public int RequestedEntities { get; set; }
        public int ArrivedEntities { get; set; }
        public int StrandedEntities { get; set; }
        public double MakespanSeconds { get; set; }
        public double MeanTravelSeconds { get; set; }
        public double MaximumUtilization { get; set; }
        public List<SpatialMovementRecord> Movements { get; set; }
        public List<string> StrandedEntityIds { get; set; }
        public Dictionary<string, double> ConnectionLoads { get; set; }
        public string CertificateHash { get; set; }
        public SpatialMovementResult() { Movements = new List<SpatialMovementRecord>(); StrandedEntityIds = new List<string>(); ConnectionLoads = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        public override string ToString() { return ArrivedEntities.ToString(CultureInfo.InvariantCulture) + "/" + RequestedEntities.ToString(CultureInfo.InvariantCulture) + " arrived   ·   " + StrandedEntities.ToString(CultureInfo.InvariantCulture) + " stranded   ·   makespan/mean " + MakespanSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "/" + MeanTravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   peak utilization " + MaximumUtilization.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialCriticalityRecord
    {
        public string SubjectId { get; set; }
        public string Kind { get; set; }
        public double BaselineReachablePairs { get; set; }
        public double ImpairedReachablePairs { get; set; }
        public double AccessibilityLoss { get; set; }
        public double PopulationExposed { get; set; }
        public double ReplacementDistanceMeters { get; set; }
        public override string ToString() { return (Kind ?? "ASSET") + "   ·   " + (SubjectId ?? "∅") + "   ·   accessibility loss " + AccessibilityLoss.ToString("P2", CultureInfo.InvariantCulture) + "   ·   exposed " + PopulationExposed.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   detour " + ReplacementDistanceMeters.ToString("0", CultureInfo.InvariantCulture) + "m"; }
    }

    internal sealed class SpatialStressWorldResult
    {
        public string ScenarioId { get; set; }
        public string ScenarioName { get; set; }
        public double Weight { get; set; }
        public int ReachablePairs { get; set; }
        public int TotalPairs { get; set; }
        public double MeanTravelSeconds { get; set; }
        public double P95TravelSeconds { get; set; }
        public double PopulationIsolated { get; set; }
        public int StrandedEntities { get; set; }
        public int InfrastructureCascadeDepth { get; set; }
        public double InfrastructureCapacityLost { get; set; }
        public List<string> FailedInfrastructureIds { get; set; }
        public List<SpatialCriticalityRecord> CriticalAssets { get; set; }
        public string CertificateHash { get; set; }
        public SpatialStressWorldResult() { FailedInfrastructureIds = new List<string>(); CriticalAssets = new List<SpatialCriticalityRecord>(); }
        public override string ToString() { return (ScenarioName ?? ScenarioId ?? "STRESS WORLD") + "   ·   reachable " + ReachablePairs.ToString(CultureInfo.InvariantCulture) + "/" + TotalPairs.ToString(CultureInfo.InvariantCulture) + "   ·   mean/p95 " + MeanTravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "/" + P95TravelSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s   ·   isolated pop " + PopulationIsolated.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   infrastructure failed/depth " + FailedInfrastructureIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + InfrastructureCascadeDepth.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialInfrastructureCascadeResult
    {
        public string ScenarioId { get; set; }
        public int Depth { get; set; }
        public double CapacityLost { get; set; }
        public double PopulationExposed { get; set; }
        public List<string> FailedInfrastructureIds { get; set; }
        public Dictionary<string, int> FailureDepthById { get; set; }
        public List<string> Audit { get; set; }
        public string CertificateHash { get; set; }
        public SpatialInfrastructureCascadeResult() { FailedInfrastructureIds = new List<string>(); FailureDepthById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Audit = new List<string>(); }
        public override string ToString() { return (ScenarioId ?? "BASE") + "   ·   " + FailedInfrastructureIds.Count.ToString(CultureInfo.InvariantCulture) + " failed assets   ·   depth " + Depth.ToString(CultureInfo.InvariantCulture) + "   ·   capacity lost " + CapacityLost.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   exposed pop " + PopulationExposed.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialIndexCell
    {
        public string Key { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public List<string> PlaceIds { get; set; }
        public List<string> TerritoryIds { get; set; }
        public List<string> InfrastructureIds { get; set; }
        public SpatialIndexCell() { PlaceIds = new List<string>(); TerritoryIds = new List<string>(); InfrastructureIds = new List<string>(); }
        public override string ToString() { return (Key ?? "CELL") + "   ·   places/territories/infrastructure " + PlaceIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + TerritoryIds.Count.ToString(CultureInfo.InvariantCulture) + "/" + InfrastructureIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class SpatialIndexResult
    {
        public double CellSize { get; set; }
        public List<SpatialIndexCell> Cells { get; set; }
        public string CertificateHash { get; set; }
        public SpatialIndexResult() { Cells = new List<SpatialIndexCell>(); }
        public override string ToString() { return Cells.Count.ToString(CultureInfo.InvariantCulture) + " occupied cells   ·   cell size " + CellSize.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal static class SpatialKernel
    {
        internal const int MaxCoordinateSystems = 64;
        internal const int MaxLayers = 512;
        internal const int MaxPlaces = 16384;
        internal const int MaxTerritories = 4096;
        internal const int MaxVertices = 262144;
        internal const int MaxConnections = 65536;
        internal const int MaxInfrastructure = 16384;
        internal const int MaxEntities = 32768;
        internal const int MaxEvents = 32768;
        internal const int MaxScenarios = 1024;
        internal const int MaxRetainedMovements = 262144;

        private sealed class Arc
        {
            public SpatialConnection Connection;
            public string From;
            public string To;
            public bool Reverse;
            public double Seconds;
            public double Cost;
        }

        private sealed class DijkstraState
        {
            public Dictionary<string, double> Cost = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> Seconds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, double> Distance = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Arc> Prior = new Dictionary<string, Arc>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Closed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static string Fingerprint(SpatialWorldModel model)
        {
            if (model == null) return HashText("NULL-SPATIAL-WORLD");
            StringBuilder s = new StringBuilder();
            s.Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.Name).Append('|').Append(model.Revision).Append('|').Append(model.Seed).Append('|').Append(model.DefaultCrsId).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint);
            foreach (KeyValuePair<string, double> p in (model.Parameters ?? new Dictionary<string, double>()).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) s.Append("|P:").Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture));
            foreach (SpatialCoordinateReference x in (model.CoordinateSystems ?? new List<SpatialCoordinateReference>()).OrderBy(x => x.Id)) s.Append("|C:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.Epsg).Append(':').Append(x.OriginLatitude.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.OriginLongitude.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Scale.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SpatialPlace x in (model.Places ?? new List<SpatialPlace>()).OrderBy(x => x.Id)) s.Append("|N:").Append(x.Id).Append(':').Append(x.ParentId).Append(':').Append(x.Kind).Append(':').Append(x.CrsId).Append(':').Append(x.Position == null ? "NULL" : x.Position.X.ToString("R", CultureInfo.InvariantCulture) + "," + x.Position.Y.ToString("R", CultureInfo.InvariantCulture) + "," + x.Position.Z.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Population.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SpatialTerritory x in (model.Territories ?? new List<SpatialTerritory>()).OrderBy(x => x.Id)) { s.Append("|T:").Append(x.Id).Append(':').Append(x.ParentId).Append(':').Append(x.Kind).Append(':').Append(x.CrsId).Append(':').Append(x.Status); foreach (SpatialRing r in x.Rings ?? new List<SpatialRing>()) { s.Append("/R:").Append(r.Hole); foreach (SpatialCoordinate p in r.Vertices ?? new List<SpatialCoordinate>()) s.Append(':').Append(p.X.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(p.Y.ToString("R", CultureInfo.InvariantCulture)); } }
            foreach (SpatialConnection x in (model.Connections ?? new List<SpatialConnection>()).OrderBy(x => x.Id)) s.Append("|E:").Append(x.Id).Append(':').Append(x.FromPlaceId).Append(':').Append(x.ToPlaceId).Append(':').Append(x.Mode).Append(':').Append(x.Bidirectional).Append(':').Append(x.LengthMeters.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.FreeFlowSeconds.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.CapacityPerHour.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SpatialInfrastructure x in (model.Infrastructure ?? new List<SpatialInfrastructure>()).OrderBy(x => x.Id)) s.Append("|I:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.PlaceId).Append(':').Append(x.TerritoryId).Append(':').Append(x.Capacity.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Condition.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status).Append(':').Append(String.Join(",", (x.DependencyIds ?? new List<string>()).OrderBy(v => v).ToArray()));
            foreach (SpatialEntity x in (model.Entities ?? new List<SpatialEntity>()).OrderBy(x => x.Id)) s.Append("|A:").Append(x.Id).Append(':').Append(x.Kind).Append(':').Append(x.CurrentPlaceId).Append(':').Append(x.DestinationPlaceId).Append(':').Append(x.PreferredMode).Append(':').Append(x.Size.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.Status);
            foreach (SpatialScenario x in (model.Scenarios ?? new List<SpatialScenario>()).OrderBy(x => x.Id)) s.Append("|S:").Append(x.Id).Append(':').Append(x.Weight.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.DemandMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.SpeedMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.CapacityMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ReliabilityMultiplier.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.HazardMagnitude.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(x.ClosedConnectionIds).Append(':').Append(x.Status);
            return HashText(s.ToString());
        }

        public static List<SpatialValidationIssue> Validate(SpatialWorldModel model)
        {
            List<SpatialValidationIssue> issues = new List<SpatialValidationIssue>();
            if (model == null) { Issue(issues, "FATAL", "NULL_WORLD", "WORLD", "No spatial world exists."); return issues; }
            CheckCap(issues, "CRS_CAP", model.CoordinateSystems, MaxCoordinateSystems); CheckCap(issues, "LAYER_CAP", model.Layers, MaxLayers); CheckCap(issues, "PLACE_CAP", model.Places, MaxPlaces); CheckCap(issues, "TERRITORY_CAP", model.Territories, MaxTerritories); CheckCap(issues, "CONNECTION_CAP", model.Connections, MaxConnections); CheckCap(issues, "INFRASTRUCTURE_CAP", model.Infrastructure, MaxInfrastructure); CheckCap(issues, "ENTITY_CAP", model.Entities, MaxEntities); CheckCap(issues, "EVENT_CAP", model.Events, MaxEvents); CheckCap(issues, "SCENARIO_CAP", model.Scenarios, MaxScenarios);
            CheckIds(issues, "CRS", (model.CoordinateSystems ?? new List<SpatialCoordinateReference>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "LAYER", (model.Layers ?? new List<SpatialLayer>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "PLACE", (model.Places ?? new List<SpatialPlace>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "TERRITORY", (model.Territories ?? new List<SpatialTerritory>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "CONNECTION", (model.Connections ?? new List<SpatialConnection>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "INFRASTRUCTURE", (model.Infrastructure ?? new List<SpatialInfrastructure>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "ENTITY", (model.Entities ?? new List<SpatialEntity>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "EVENT", (model.Events ?? new List<SpatialEvent>()).Select(x => x == null ? null : x.Id));
            CheckIds(issues, "SCENARIO", (model.Scenarios ?? new List<SpatialScenario>()).Select(x => x == null ? null : x.Id));
            HashSet<string> placeIds = new HashSet<string>((model.Places ?? new List<SpatialPlace>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            HashSet<string> territoryIds = new HashSet<string>((model.Territories ?? new List<SpatialTerritory>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            HashSet<string> crsIds = new HashSet<string>((model.CoordinateSystems ?? new List<SpatialCoordinateReference>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            HashSet<string> layerIds = new HashSet<string>((model.Layers ?? new List<SpatialLayer>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            HashSet<string> connectionIds = new HashSet<string>((model.Connections ?? new List<SpatialConnection>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            HashSet<string> infrastructureIds = new HashSet<string>((model.Infrastructure ?? new List<SpatialInfrastructure>()).Where(x => x != null).Select(x => x.Id ?? ""), StringComparer.OrdinalIgnoreCase);
            if (!String.IsNullOrWhiteSpace(model.DefaultCrsId) && !crsIds.Contains(model.DefaultCrsId)) Issue(issues, "ERROR", "DEFAULT_CRS_MISSING", model.DefaultCrsId, "The default coordinate reference is not declared.");
            foreach (SpatialPlace x in model.Places ?? new List<SpatialPlace>()) if (x != null) { if (x.Position == null || !Finite(x.Position.X) || !Finite(x.Position.Y) || !Finite(x.Position.Z)) Issue(issues, "ERROR", "INVALID_COORDINATE", x.Id, "Position must contain finite coordinates."); if (!String.IsNullOrWhiteSpace(x.ParentId) && !placeIds.Contains(x.ParentId)) Issue(issues, "ERROR", "MISSING_PLACE_PARENT", x.Id, x.ParentId); if (!String.IsNullOrWhiteSpace(x.CrsId) && !crsIds.Contains(x.CrsId)) Issue(issues, "ERROR", "MISSING_PLACE_CRS", x.Id, x.CrsId); if (!String.IsNullOrWhiteSpace(x.LayerId) && !layerIds.Contains(x.LayerId)) Issue(issues, "ERROR", "MISSING_PLACE_LAYER", x.Id, x.LayerId); if (x.Capacity < 0 || x.Population < 0) Issue(issues, "ERROR", "NEGATIVE_PLACE_QUANTITY", x.Id, "Capacity and population must be nonnegative."); }
            int vertices = 0;
            foreach (SpatialTerritory x in model.Territories ?? new List<SpatialTerritory>()) if (x != null) { if (!String.IsNullOrWhiteSpace(x.ParentId) && !territoryIds.Contains(x.ParentId)) Issue(issues, "ERROR", "MISSING_TERRITORY_PARENT", x.Id, x.ParentId); if (!String.IsNullOrWhiteSpace(x.CrsId) && !crsIds.Contains(x.CrsId)) Issue(issues, "ERROR", "MISSING_TERRITORY_CRS", x.Id, x.CrsId); if (!String.IsNullOrWhiteSpace(x.LayerId) && !layerIds.Contains(x.LayerId)) Issue(issues, "ERROR", "MISSING_TERRITORY_LAYER", x.Id, x.LayerId); foreach (SpatialRing ring in x.Rings ?? new List<SpatialRing>()) { vertices += ring == null || ring.Vertices == null ? 0 : ring.Vertices.Count; if (ring == null || ring.Vertices == null || ring.Vertices.Count < 3) Issue(issues, "ERROR", "DEGENERATE_RING", x.Id, "Every polygon ring requires at least three vertices."); else if (Math.Abs(SignedArea(ring.Vertices)) < 1e-12) Issue(issues, "WARN", "ZERO_AREA_RING", x.Id, ring.Id ?? "ring"); } }
            if (vertices > MaxVertices) Issue(issues, "FATAL", "VERTEX_CAP", "WORLD", vertices.ToString(CultureInfo.InvariantCulture) + " > " + MaxVertices.ToString(CultureInfo.InvariantCulture));
            foreach (SpatialConnection x in model.Connections ?? new List<SpatialConnection>()) if (x != null) { if (!placeIds.Contains(x.FromPlaceId ?? "")) Issue(issues, "ERROR", "MISSING_CONNECTION_ORIGIN", x.Id, x.FromPlaceId); if (!placeIds.Contains(x.ToPlaceId ?? "")) Issue(issues, "ERROR", "MISSING_CONNECTION_DESTINATION", x.Id, x.ToPlaceId); if (Eq(x.FromPlaceId, x.ToPlaceId)) Issue(issues, "WARN", "SELF_LOOP", x.Id, x.FromPlaceId); if (x.LengthMeters < 0 || x.FreeFlowSeconds < 0 || x.CapacityPerHour < 0) Issue(issues, "ERROR", "NEGATIVE_CONNECTION_QUANTITY", x.Id, "Length, time and capacity must be nonnegative."); if (!String.IsNullOrWhiteSpace(x.InfrastructureId) && !infrastructureIds.Contains(x.InfrastructureId)) Issue(issues, "ERROR", "MISSING_CONNECTION_INFRASTRUCTURE", x.Id, x.InfrastructureId); }
            foreach (SpatialInfrastructure x in model.Infrastructure ?? new List<SpatialInfrastructure>()) if (x != null) { if (!String.IsNullOrWhiteSpace(x.PlaceId) && !placeIds.Contains(x.PlaceId)) Issue(issues, "ERROR", "MISSING_INFRASTRUCTURE_PLACE", x.Id, x.PlaceId); if (!String.IsNullOrWhiteSpace(x.TerritoryId) && !territoryIds.Contains(x.TerritoryId)) Issue(issues, "ERROR", "MISSING_INFRASTRUCTURE_TERRITORY", x.Id, x.TerritoryId); foreach (string d in x.DependencyIds ?? new List<string>()) if (!infrastructureIds.Contains(d)) Issue(issues, "WARN", "MISSING_INFRASTRUCTURE_DEPENDENCY", x.Id, d); }
            List<string> infrastructureNodes = infrastructureIds.Where(x => !String.IsNullOrWhiteSpace(x)).ToList(); Dictionary<string, List<string>> dependencyGraph = infrastructureNodes.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (SpatialInfrastructure x in model.Infrastructure ?? new List<SpatialInfrastructure>()) if (x != null && dependencyGraph.ContainsKey(x.Id ?? "")) dependencyGraph[x.Id].AddRange((x.DependencyIds ?? new List<string>()).Where(dependencyGraph.ContainsKey)); foreach (List<string> component in StrongComponents(infrastructureNodes, dependencyGraph)) if (component.Count > 1 || (component.Count == 1 && dependencyGraph[component[0]].Contains(component[0], StringComparer.OrdinalIgnoreCase))) Issue(issues, "WARN", "INFRASTRUCTURE_DEPENDENCY_CYCLE", String.Join(",", component.ToArray()), "Mutually dependent assets require independent recovery assumptions.");
            foreach (SpatialEntity x in model.Entities ?? new List<SpatialEntity>()) if (x != null) { if (!placeIds.Contains(x.CurrentPlaceId ?? "")) Issue(issues, "ERROR", "MISSING_ENTITY_ORIGIN", x.Id, x.CurrentPlaceId); if (!String.IsNullOrWhiteSpace(x.DestinationPlaceId) && !placeIds.Contains(x.DestinationPlaceId)) Issue(issues, "ERROR", "MISSING_ENTITY_DESTINATION", x.Id, x.DestinationPlaceId); if (x.Size < 0 || x.SpeedMetersPerSecond < 0) Issue(issues, "ERROR", "NEGATIVE_ENTITY_QUANTITY", x.Id, "Size and speed must be nonnegative."); }
            foreach (SpatialEvent x in model.Events ?? new List<SpatialEvent>()) if (x != null) { if (!String.IsNullOrWhiteSpace(x.PlaceId) && !placeIds.Contains(x.PlaceId)) Issue(issues, "ERROR", "MISSING_EVENT_PLACE", x.Id, x.PlaceId); if (!String.IsNullOrWhiteSpace(x.TerritoryId) && !territoryIds.Contains(x.TerritoryId)) Issue(issues, "ERROR", "MISSING_EVENT_TERRITORY", x.Id, x.TerritoryId); if (x.EndSecond < x.StartSecond) Issue(issues, "ERROR", "EVENT_TIME_INVERSION", x.Id, "End time precedes start time."); }
            foreach (SpatialScenario x in model.Scenarios ?? new List<SpatialScenario>()) if (x != null) foreach (string connectionId in Tokens(x.ClosedConnectionIds)) if (!connectionIds.Contains(connectionId)) Issue(issues, "WARN", "SCENARIO_UNKNOWN_CLOSURE", x.Id, connectionId);
            DetectParentCycles(model.Places == null ? new Dictionary<string, string>() : model.Places.Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First().ParentId, StringComparer.OrdinalIgnoreCase), "PLACE_PARENT_CYCLE", issues);
            DetectParentCycles(model.Territories == null ? new Dictionary<string, string>() : model.Territories.Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First().ParentId, StringComparer.OrdinalIgnoreCase), "TERRITORY_PARENT_CYCLE", issues);
            return issues;
        }

        public static SpatialRouteResult Route(SpatialWorldModel model, string originId, string destinationId, SpatialScenario scenario, string modeFilter, string accessClass)
        {
            SpatialRouteResult result = new SpatialRouteResult { OriginPlaceId = originId, DestinationPlaceId = destinationId, ScenarioId = scenario == null ? "BASE" : scenario.Id };
            if (model == null || String.IsNullOrWhiteSpace(originId) || String.IsNullOrWhiteSpace(destinationId)) { result.Diagnostics.Add("World, origin and destination are required."); return SealRoute(result); }
            scenario = ScenarioWithInfrastructureCascade(model, scenario);
            HashSet<string> places = new HashSet<string>((model.Places ?? new List<SpatialPlace>()).Where(Active).Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            if (!places.Contains(originId) || !places.Contains(destinationId)) { result.Diagnostics.Add("Origin or destination is inactive or missing."); return SealRoute(result); }
            Dictionary<string, List<Arc>> graph = Graph(model, scenario, modeFilter, accessClass, null);
            DijkstraState state = Dijkstra(graph, originId, destinationId);
            double final;
            if (!state.Cost.TryGetValue(destinationId, out final)) { result.Diagnostics.Add("No admissible path exists under the current mode, access, closure and capacity filters."); return SealRoute(result); }
            List<Arc> arcs = new List<Arc>(); string cursor = destinationId; int guard = 0; while (!Eq(cursor, originId) && guard++ <= MaxPlaces) { Arc arc; if (!state.Prior.TryGetValue(cursor, out arc)) break; arcs.Add(arc); cursor = arc.From; } arcs.Reverse();
            int sequence = 0; foreach (Arc arc in arcs) { SpatialRouteLeg leg = new SpatialRouteLeg { Sequence = ++sequence, ConnectionId = arc.Connection.Id, FromPlaceId = arc.From, ToPlaceId = arc.To, Mode = arc.Connection.Mode, DistanceMeters = Math.Max(0, arc.Connection.LengthMeters), TravelSeconds = arc.Seconds, GeneralizedCost = arc.Cost, Capacity = EffectiveCapacity(arc.Connection, scenario) }; result.Legs.Add(leg); result.TotalDistanceMeters += leg.DistanceMeters; result.TotalTravelSeconds += leg.TravelSeconds; result.TotalGeneralizedCost += leg.GeneralizedCost; result.BottleneckCapacity = Math.Min(result.BottleneckCapacity, leg.Capacity); }
            result.Status = Eq(cursor, originId) ? "ROUTED" : "UNREACHABLE"; if (Double.IsPositiveInfinity(result.BottleneckCapacity)) result.BottleneckCapacity = 0; return SealRoute(result);
        }

        public static List<SpatialRouteResult> Alternatives(SpatialWorldModel model, string originId, string destinationId, SpatialScenario scenario, string modeFilter, string accessClass, int count)
        {
            scenario = ScenarioWithInfrastructureCascade(model, scenario); count = Math.Max(1, Math.Min(32, count)); List<SpatialRouteResult> routes = new List<SpatialRouteResult>(); HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++) { Dictionary<string, List<Arc>> graph = Graph(model, scenario, modeFilter, accessClass, excluded); DijkstraState state = Dijkstra(graph, originId, destinationId); if (!state.Cost.ContainsKey(destinationId)) break; SpatialRouteResult route = BuildRoute(originId, destinationId, scenario, state); if (route.Legs.Count == 0) break; routes.Add(route); SpatialRouteLeg widest = route.Legs.OrderByDescending(x => x.GeneralizedCost).ThenByDescending(x => x.DistanceMeters).First(); excluded.Add(widest.ConnectionId); }
            return routes;
        }

        public static List<SpatialReachabilityRecord> Isochrone(SpatialWorldModel model, string originId, SpatialScenario scenario, string modeFilter, string accessClass, double maximumSeconds)
        {
            scenario = ScenarioWithInfrastructureCascade(model, scenario); maximumSeconds = Math.Max(0, maximumSeconds); Dictionary<string, List<Arc>> graph = Graph(model, scenario, modeFilter, accessClass, null); DijkstraState state = Dijkstra(graph, originId, null); List<SpatialReachabilityRecord> rows = new List<SpatialReachabilityRecord>();
            foreach (KeyValuePair<string, double> x in state.Seconds.Where(x => x.Value <= maximumSeconds).OrderBy(x => x.Value).ThenBy(x => x.Key)) { int transfers = 0; string cursor = x.Key; string priorMode = null; int guard = 0; while (!Eq(cursor, originId) && guard++ < MaxPlaces) { Arc a; if (!state.Prior.TryGetValue(cursor, out a)) break; if (priorMode != null && !Eq(priorMode, a.Connection.Mode)) transfers++; priorMode = a.Connection.Mode; cursor = a.From; } double distance; state.Distance.TryGetValue(x.Key, out distance); rows.Add(new SpatialReachabilityRecord { PlaceId = x.Key, TravelSeconds = x.Value, DistanceMeters = distance, Transfers = transfers }); }
            return rows;
        }

        public static SpatialTopologyResult AnalyzeTopology(SpatialWorldModel model)
        {
            SpatialTopologyResult result = new SpatialTopologyResult(); result.Issues = Validate(model); if (model == null) return SealTopology(result);
            List<string> nodes = (model.Places ?? new List<SpatialPlace>()).Where(Active).Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); List<SpatialConnection> edges = (model.Connections ?? new List<SpatialConnection>()).Where(Active).Where(x => nodes.Contains(x.FromPlaceId, StringComparer.OrdinalIgnoreCase) && nodes.Contains(x.ToPlaceId, StringComparer.OrdinalIgnoreCase)).ToList(); result.ActivePlaces = nodes.Count; result.ActiveConnections = edges.Count;
            Dictionary<string, HashSet<string>> undirected = nodes.ToDictionary(x => x, x => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> directed = nodes.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (SpatialConnection e in edges) { undirected[e.FromPlaceId].Add(e.ToPlaceId); undirected[e.ToPlaceId].Add(e.FromPlaceId); directed[e.FromPlaceId].Add(e.ToPlaceId); if (e.Bidirectional) directed[e.ToPlaceId].Add(e.FromPlaceId); }
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string node in nodes) if (!visited.Contains(node)) { List<string> component = new List<string>(); Queue<string> q = new Queue<string>(); q.Enqueue(node); visited.Add(node); while (q.Count > 0) { string u = q.Dequeue(); component.Add(u); foreach (string v in undirected[u]) if (visited.Add(v)) q.Enqueue(v); } result.WeakComponentMembers.Add(component.OrderBy(x => x).ToList()); }
            result.WeakComponents = result.WeakComponentMembers.Count; result.StrongComponentMembers = StrongComponents(nodes, directed); result.StrongComponents = result.StrongComponentMembers.Count;
            Dictionary<string, int> discovery = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Dictionary<string, int> low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); int time = 0; foreach (string node in nodes) if (!discovery.ContainsKey(node)) ArticulationDfs(node, null, undirected, edges, discovery, low, parent, ref time, result.ArticulationPlaceIds, result.BridgeConnectionIds);
            result.ArticulationPlaceIds = result.ArticulationPlaceIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(); result.BridgeConnectionIds = result.BridgeConnectionIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(); result.ParentCycles = result.Issues.Count(x => x.Code != null && x.Code.EndsWith("PARENT_CYCLE", StringComparison.OrdinalIgnoreCase)); result.TerritoryOverlaps = CountTerritoryOverlaps(model.Territories); result.NetworkDensity = nodes.Count <= 1 ? 0 : Math.Min(1, edges.Sum(x => x.Bidirectional ? 2.0 : 1.0) / (nodes.Count * (nodes.Count - 1.0)));
            return SealTopology(result);
        }

        public static SpatialMovementResult SimulateMovement(SpatialWorldModel model, SpatialScenario scenario, int ticks, double tickSeconds)
        {
            SpatialMovementResult result = new SpatialMovementResult { ScenarioId = scenario == null ? "BASE" : scenario.Id }; if (model == null) return SealMovement(result); scenario = ScenarioWithInfrastructureCascade(model, scenario); ticks = Math.Max(1, Math.Min(4096, ticks)); tickSeconds = Math.Max(0.1, Math.Min(86400, tickSeconds));
            List<SpatialEntity> entities = (model.Entities ?? new List<SpatialEntity>()).Where(Active).Take(MaxEntities).OrderByDescending(x => x.Priority).ThenBy(x => x.Id).ToList(); result.RequestedEntities = entities.Count; Dictionary<string, double> loads = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); double totalTravel = 0;
            int tick = 0; foreach (SpatialEntity entity in entities) { if (tick >= ticks) tick = ticks - 1; SpatialRouteResult route = Route(model, entity.CurrentPlaceId, entity.DestinationPlaceId, scenario, entity.PreferredMode, entity.AccessClass); if (!Eq(route.Status, "ROUTED")) { result.StrandedEntityIds.Add(entity.Id); continue; } double clock = tick * tickSeconds; bool stranded = false; foreach (SpatialRouteLeg leg in route.Legs) { double prior; loads.TryGetValue(leg.ConnectionId, out prior); double demand = prior + Math.Max(0.0001, entity.Size) * (scenario == null ? 1 : Math.Max(0, scenario.DemandMultiplier)); loads[leg.ConnectionId] = demand; double capacity = Math.Max(0.0001, leg.Capacity); double ratio = demand / capacity; double congestion = 1 + 0.15 * Math.Pow(Math.Max(0, ratio), 4); double eventFriction = MovementEventMultiplier(model, leg, clock); if (!Finite(congestion) || congestion > 10000 || !Finite(eventFriction)) { stranded = true; break; } double effectiveMultiplier = congestion * eventFriction; double travel = leg.TravelSeconds * effectiveMultiplier; SpatialMovementRecord movement = new SpatialMovementRecord { Tick = tick, EntityId = entity.Id, FromPlaceId = leg.FromPlaceId, ToPlaceId = leg.ToPlaceId, ConnectionId = leg.ConnectionId, DepartSecond = clock, ArriveSecond = clock + travel, CongestionMultiplier = effectiveMultiplier, Status = eventFriction > 1.000001 ? "MOVED_WITH_EVENT_FRICTION" : "MOVED" }; if (result.Movements.Count < MaxRetainedMovements) result.Movements.Add(movement); clock += travel; result.MaximumUtilization = Math.Max(result.MaximumUtilization, ratio); }
                if (stranded) result.StrandedEntityIds.Add(entity.Id); else { result.ArrivedEntities++; double elapsed = clock - tick * tickSeconds; totalTravel += elapsed; result.MakespanSeconds = Math.Max(result.MakespanSeconds, clock); } tick++;
            }
            result.StrandedEntities = result.StrandedEntityIds.Count; result.MeanTravelSeconds = result.ArrivedEntities == 0 ? 0 : totalTravel / result.ArrivedEntities; result.ConnectionLoads = loads; return SealMovement(result);
        }

        public static List<SpatialStressWorldResult> StressWorlds(SpatialWorldModel model)
        {
            List<SpatialStressWorldResult> results = new List<SpatialStressWorldResult>(); if (model == null) return results; List<SpatialPlace> places = (model.Places ?? new List<SpatialPlace>()).Where(Active).Take(512).ToList(); List<SpatialScenario> scenarios = (model.Scenarios ?? new List<SpatialScenario>()).Where(Active).Take(MaxScenarios).ToList(); if (scenarios.Count == 0) scenarios.Add(new SpatialScenario { Id = "BASE", Name = "Base world" });
            foreach (SpatialScenario scenario in scenarios) { SpatialInfrastructureCascadeResult cascade = InfrastructureCascade(model, scenario); SpatialScenario effectiveScenario = ScenarioWithInfrastructureCascade(model, scenario, cascade); List<double> times = new List<double>(); int reachable = 0; int total = 0; HashSet<string> reached = new HashSet<string>(StringComparer.OrdinalIgnoreCase); Dictionary<string, List<Arc>> scenarioGraph = Graph(model, effectiveScenario, null, "PUBLIC", null); foreach (SpatialPlace origin in places) { DijkstraState state = Dijkstra(scenarioGraph, origin.Id, null); foreach (SpatialPlace destination in places) if (!Eq(origin.Id, destination.Id)) { total++; double seconds; if (state.Seconds.TryGetValue(destination.Id, out seconds)) { reachable++; reached.Add(destination.Id); times.Add(seconds); } } } times.Sort(); SpatialMovementResult movement = SimulateMovement(model, effectiveScenario, 4096, 60); SpatialStressWorldResult world = new SpatialStressWorldResult { ScenarioId = scenario.Id, ScenarioName = scenario.Name, Weight = Math.Max(0, scenario.Weight), ReachablePairs = reachable, TotalPairs = total, MeanTravelSeconds = times.Count == 0 ? 0 : times.Average(), P95TravelSeconds = Percentile(times, 0.95), PopulationIsolated = places.Where(x => !reached.Contains(x.Id) && places.Count > 1).Sum(x => Math.Max(0, x.Population)), StrandedEntities = movement.StrandedEntities, InfrastructureCascadeDepth = cascade.Depth, InfrastructureCapacityLost = cascade.CapacityLost, FailedInfrastructureIds = cascade.FailedInfrastructureIds };
                world.CriticalAssets = Criticality(model, effectiveScenario, 48); world.CertificateHash = HashText(Fingerprint(model) + "|STRESS|" + scenario.Id + "|" + reachable + "|" + total + "|" + world.MeanTravelSeconds.ToString("R", CultureInfo.InvariantCulture) + "|" + world.P95TravelSeconds.ToString("R", CultureInfo.InvariantCulture) + "|" + cascade.CertificateHash + "|" + movement.CertificateHash); results.Add(world); }
            return results;
        }

        public static SpatialInfrastructureCascadeResult InfrastructureCascade(SpatialWorldModel model, SpatialScenario scenario)
        {
            SpatialInfrastructureCascadeResult result = new SpatialInfrastructureCascadeResult { ScenarioId = scenario == null ? "BASE" : scenario.Id }; if (model == null) return SealCascade(result); double hazard = scenario == null ? 0 : Clamp01(scenario.HazardMagnitude); List<SpatialInfrastructure> assets = (model.Infrastructure ?? new List<SpatialInfrastructure>()).Where(x => x != null && Active(x.Status)).Take(MaxInfrastructure).OrderBy(x => x.Id).ToList(); Dictionary<string, SpatialInfrastructure> byId = new Dictionary<string, SpatialInfrastructure>(StringComparer.OrdinalIgnoreCase); foreach (SpatialInfrastructure asset in assets) if (!String.IsNullOrWhiteSpace(asset.Id) && !byId.ContainsKey(asset.Id)) byId[asset.Id] = asset; HashSet<string> failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SpatialInfrastructure asset in assets) { if (String.IsNullOrWhiteSpace(asset.Id)) continue; double utilization = asset.Capacity <= 0 ? 1 : Clamp01(asset.Utilization / Math.Max(0.000001, asset.Capacity)); double stress = hazard * Math.Max(0.05, asset.Criticality) * (1 + 0.35 * utilization); double resistance = Math.Max(0.01, Clamp01(asset.Condition)) * Math.Max(0.25, asset.Redundancy) * 0.95; if (stress > resistance) { failed.Add(asset.Id); result.FailureDepthById[asset.Id] = 0; result.Audit.Add("DIRECT_HAZARD_FAILURE · " + asset.Id + " · stress " + stress.ToString("0.####", CultureInfo.InvariantCulture) + " > resistance " + resistance.ToString("0.####", CultureInfo.InvariantCulture)); } }
            int depth = 0; bool changed = true; while (changed && depth++ < Math.Min(MaxInfrastructure, 512)) { changed = false; foreach (SpatialInfrastructure asset in assets) { if (String.IsNullOrWhiteSpace(asset.Id) || failed.Contains(asset.Id)) continue; List<string> deps = (asset.DependencyIds ?? new List<string>()).Where(byId.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); if (deps.Count == 0) continue; int lost = deps.Count(failed.Contains); double lostFraction = lost / (double)deps.Count; double tolerance = Clamp01(asset.Redundancy / (1 + Math.Max(0, asset.Redundancy))); if (lost > 0 && lostFraction >= tolerance) { failed.Add(asset.Id); result.FailureDepthById[asset.Id] = depth; result.Audit.Add("DEPENDENCY_CASCADE_FAILURE · " + asset.Id + " · lost " + lost.ToString(CultureInfo.InvariantCulture) + "/" + deps.Count.ToString(CultureInfo.InvariantCulture) + " dependencies"); changed = true; } } }
            result.Depth = result.FailureDepthById.Count == 0 ? 0 : result.FailureDepthById.Values.Max(); result.FailedInfrastructureIds = failed.OrderBy(x => x).ToList(); result.CapacityLost = assets.Where(x => failed.Contains(x.Id)).Sum(x => Math.Max(0, x.Capacity)); HashSet<string> exposedPlaces = new HashSet<string>(assets.Where(x => failed.Contains(x.Id) && !String.IsNullOrWhiteSpace(x.PlaceId)).Select(x => x.PlaceId), StringComparer.OrdinalIgnoreCase); result.PopulationExposed = (model.Places ?? new List<SpatialPlace>()).Where(x => x != null && exposedPlaces.Contains(x.Id ?? "")).Sum(x => Math.Max(0, x.Population)); return SealCascade(result);
        }

        public static List<SpatialCriticalityRecord> Criticality(SpatialWorldModel model, SpatialScenario scenario, int cap)
        {
            scenario = ScenarioWithInfrastructureCascade(model, scenario); cap = Math.Max(1, Math.Min(512, cap)); List<SpatialCriticalityRecord> rows = new List<SpatialCriticalityRecord>(); if (model == null) return rows; List<SpatialPlace> places = (model.Places ?? new List<SpatialPlace>()).Where(Active).OrderByDescending(x => x.Population).Take(96).ToList(); double baseline = ReachablePairs(model, scenario, places, null); if (baseline <= 0) return rows;
            SpatialTopologyResult topology = AnalyzeTopology(model); HashSet<string> structural = new HashSet<string>(topology.BridgeConnectionIds, StringComparer.OrdinalIgnoreCase); List<SpatialConnection> candidates = (model.Connections ?? new List<SpatialConnection>()).Where(Active).OrderByDescending(x => structural.Contains(x.Id ?? "")).ThenByDescending(x => Math.Max(0, x.Risk) + Math.Max(0, 1 - x.Reliability)).ThenByDescending(x => x.CapacityPerHour).ThenBy(x => x.Id).Take(Math.Min(192, Math.Max(16, cap * 2))).ToList();
            foreach (SpatialConnection edge in candidates) { HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { edge.Id }; double impaired = ReachablePairs(model, scenario, places, excluded); if (impaired >= baseline) continue; double exposed = places.Where(p => Eq(p.Id, edge.FromPlaceId) || Eq(p.Id, edge.ToPlaceId)).Sum(p => Math.Max(0, p.Population)); SpatialRouteResult replacement = RouteWithExcluded(model, edge.FromPlaceId, edge.ToPlaceId, scenario, excluded); rows.Add(new SpatialCriticalityRecord { SubjectId = edge.Id, Kind = "CONNECTION", BaselineReachablePairs = baseline, ImpairedReachablePairs = impaired, AccessibilityLoss = Clamp01((baseline - impaired) / baseline), PopulationExposed = exposed, ReplacementDistanceMeters = Eq(replacement.Status, "ROUTED") ? replacement.TotalDistanceMeters : Double.PositiveInfinity }); }
            return rows.OrderByDescending(x => x.AccessibilityLoss).ThenByDescending(x => x.PopulationExposed).ThenBy(x => x.SubjectId).Take(cap).ToList();
        }

        public static SpatialIndexResult BuildIndex(SpatialWorldModel model, double cellSize)
        {
            SpatialIndexResult result = new SpatialIndexResult { CellSize = Math.Max(0.000001, cellSize) }; if (model == null) return SealIndex(result); Dictionary<string, SpatialIndexCell> cells = new Dictionary<string, SpatialIndexCell>(StringComparer.OrdinalIgnoreCase);
            foreach (SpatialPlace p in model.Places ?? new List<SpatialPlace>()) if (p != null && p.Position != null && Finite(p.Position.X) && Finite(p.Position.Y)) Cell(cells, p.Position.X, p.Position.Y, result.CellSize).PlaceIds.Add(p.Id);
            foreach (SpatialTerritory t in model.Territories ?? new List<SpatialTerritory>()) if (t != null) foreach (SpatialRing ring in t.Rings ?? new List<SpatialRing>()) if (ring != null && ring.Vertices != null && ring.Vertices.Count > 0) { SpatialCoordinate c = Centroid(ring.Vertices); Cell(cells, c.X, c.Y, result.CellSize).TerritoryIds.Add(t.Id); }
            Dictionary<string, SpatialPlace> places = new Dictionary<string, SpatialPlace>(StringComparer.OrdinalIgnoreCase); foreach (SpatialPlace place in model.Places ?? new List<SpatialPlace>()) if (place != null && !String.IsNullOrWhiteSpace(place.Id) && !places.ContainsKey(place.Id)) places[place.Id] = place; foreach (SpatialInfrastructure i in model.Infrastructure ?? new List<SpatialInfrastructure>()) { SpatialPlace p; if (i != null && places.TryGetValue(i.PlaceId ?? "", out p) && p.Position != null) Cell(cells, p.Position.X, p.Position.Y, result.CellSize).InfrastructureIds.Add(i.Id); }
            result.Cells = cells.Values.OrderBy(x => x.Key).ToList(); return SealIndex(result);
        }

        public static SpatialCoordinate Transform(SpatialCoordinate point, SpatialCoordinateReference from, SpatialCoordinateReference to)
        {
            if (point == null) return null; if (from == null || to == null || Eq(from.Id, to.Id)) return new SpatialCoordinate(point.X, point.Y, point.Z) { Measure = point.Measure };
            double latitude; double longitude; if (Eq(from.Kind, "GEOGRAPHIC")) { longitude = point.X; latitude = point.Y; } else { double metersPerDegreeLatitude = 111132.92; double metersPerDegreeLongitude = 111412.84 * Math.Cos(from.OriginLatitude * Math.PI / 180.0); longitude = from.OriginLongitude + point.X / Math.Max(0.000001, metersPerDegreeLongitude * Math.Max(0.000001, from.Scale)); latitude = from.OriginLatitude + point.Y / Math.Max(0.000001, metersPerDegreeLatitude * Math.Max(0.000001, from.Scale)); }
            if (Eq(to.Kind, "GEOGRAPHIC")) return new SpatialCoordinate(longitude, latitude, point.Z) { Measure = point.Measure }; double targetMetersPerDegreeLatitude = 111132.92; double targetMetersPerDegreeLongitude = 111412.84 * Math.Cos(to.OriginLatitude * Math.PI / 180.0); return new SpatialCoordinate((longitude - to.OriginLongitude) * targetMetersPerDegreeLongitude * Math.Max(0.000001, to.Scale), (latitude - to.OriginLatitude) * targetMetersPerDegreeLatitude * Math.Max(0.000001, to.Scale), point.Z) { Measure = point.Measure };
        }

        public static double DistanceMeters(SpatialCoordinate a, SpatialCoordinate b, SpatialCoordinateReference crs)
        {
            if (a == null || b == null) return Double.PositiveInfinity; if (crs != null && Eq(crs.Kind, "GEOGRAPHIC")) { double lat1 = a.Y * Math.PI / 180.0, lat2 = b.Y * Math.PI / 180.0, dLat = (b.Y - a.Y) * Math.PI / 180.0, dLon = (b.X - a.X) * Math.PI / 180.0; double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2); double surface = 6371008.8 * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(Math.Max(0, 1 - h))); return Math.Sqrt(surface * surface + (b.Z - a.Z) * (b.Z - a.Z)); } double scale = crs == null ? 1 : Math.Max(0.000001, crs.Scale); double dx = (b.X - a.X) * scale, dy = (b.Y - a.Y) * scale, dz = b.Z - a.Z; return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static bool Contains(SpatialTerritory territory, SpatialCoordinate point)
        {
            if (territory == null || point == null) return false; bool insideShell = false; foreach (SpatialRing ring in territory.Rings ?? new List<SpatialRing>()) if (ring != null && ring.Vertices != null && ring.Vertices.Count >= 3) { bool inside = PointInRing(point, ring.Vertices); if (ring.Hole && inside) return false; if (!ring.Hole && inside) insideShell = true; } return insideShell;
        }

        public static double PolygonArea(SpatialTerritory territory)
        {
            if (territory == null) return 0; double area = 0; foreach (SpatialRing ring in territory.Rings ?? new List<SpatialRing>()) if (ring != null) area += (ring.Hole ? -1 : 1) * Math.Abs(SignedArea(ring.Vertices)); return Math.Abs(area);
        }

        private static Dictionary<string, List<Arc>> Graph(SpatialWorldModel model, SpatialScenario scenario, string modeFilter, string accessClass, HashSet<string> excluded)
        {
            Dictionary<string, List<Arc>> graph = new Dictionary<string, List<Arc>>(StringComparer.OrdinalIgnoreCase); if (model == null) return graph; HashSet<string> closed = Tokens(scenario == null ? null : scenario.ClosedConnectionIds); if (excluded != null) closed.UnionWith(excluded); double speed = scenario == null ? 1 : Math.Max(0.000001, scenario.SpeedMultiplier); double reliability = scenario == null ? 1 : Math.Max(0.000001, scenario.ReliabilityMultiplier);
            foreach (SpatialPlace p in model.Places ?? new List<SpatialPlace>()) if (Active(p) && !String.IsNullOrWhiteSpace(p.Id)) graph[p.Id] = new List<Arc>();
            foreach (SpatialConnection e in model.Connections ?? new List<SpatialConnection>()) { if (!Active(e) || closed.Contains(e.Id ?? "") || !graph.ContainsKey(e.FromPlaceId ?? "") || !graph.ContainsKey(e.ToPlaceId ?? "") || (!String.IsNullOrWhiteSpace(modeFilter) && !Eq(modeFilter, "ANY") && !Eq(e.Mode, modeFilter)) || !AccessAllowed(accessClass, e.AccessClass) || EffectiveCapacity(e, scenario) <= 0) continue; double seconds = Math.Max(0.001, e.FreeFlowSeconds / speed); double hazard = scenario == null ? 0 : Clamp01(scenario.HazardMagnitude); double risk = Math.Max(0, e.Risk) * (1 + hazard) + Math.Max(0, 1 - Clamp01(e.Reliability * reliability)) + hazard * 0.05; double cost = seconds + Math.Max(0, e.MonetaryCost) * Parameter(model, "money_to_seconds", 60) + Math.Max(0, e.EnergyCost) * Parameter(model, "energy_to_seconds", 1) + risk * Parameter(model, "risk_to_seconds", 900) + Math.Max(0, e.AccessibilityPenalty); Arc forward = new Arc { Connection = e, From = e.FromPlaceId, To = e.ToPlaceId, Reverse = false, Seconds = seconds, Cost = cost }; graph[forward.From].Add(forward); if (e.Bidirectional) graph[e.ToPlaceId].Add(new Arc { Connection = e, From = e.ToPlaceId, To = e.FromPlaceId, Reverse = true, Seconds = seconds, Cost = cost }); }
            return graph;
        }

        private static DijkstraState Dijkstra(Dictionary<string, List<Arc>> graph, string originId, string stopId)
        {
            DijkstraState state = new DijkstraState(); if (graph == null || !graph.ContainsKey(originId ?? "")) return state; state.Cost[originId] = 0; state.Seconds[originId] = 0; state.Distance[originId] = 0;
            while (true) { string u = null; double best = Double.PositiveInfinity; foreach (KeyValuePair<string, double> x in state.Cost) if (!state.Closed.Contains(x.Key) && x.Value < best) { best = x.Value; u = x.Key; } if (u == null) break; state.Closed.Add(u); if (!String.IsNullOrWhiteSpace(stopId) && Eq(u, stopId)) break; List<Arc> arcs; if (!graph.TryGetValue(u, out arcs)) continue; foreach (Arc arc in arcs) { double candidate = best + arc.Cost; double known; if (!state.Cost.TryGetValue(arc.To, out known) || candidate < known - 1e-9) { state.Cost[arc.To] = candidate; state.Seconds[arc.To] = state.Seconds[u] + arc.Seconds; state.Distance[arc.To] = state.Distance[u] + Math.Max(0, arc.Connection.LengthMeters); state.Prior[arc.To] = arc; } } }
            return state;
        }

        private static SpatialRouteResult BuildRoute(string originId, string destinationId, SpatialScenario scenario, DijkstraState state)
        {
            SpatialRouteResult result = new SpatialRouteResult { OriginPlaceId = originId, DestinationPlaceId = destinationId, ScenarioId = scenario == null ? "BASE" : scenario.Id }; if (state == null || !state.Cost.ContainsKey(destinationId)) return SealRoute(result); List<Arc> arcs = new List<Arc>(); string cursor = destinationId; int guard = 0; while (!Eq(cursor, originId) && guard++ < MaxPlaces) { Arc a; if (!state.Prior.TryGetValue(cursor, out a)) break; arcs.Add(a); cursor = a.From; } arcs.Reverse(); int n = 0; foreach (Arc a in arcs) { double capacity = EffectiveCapacity(a.Connection, scenario); result.Legs.Add(new SpatialRouteLeg { Sequence = ++n, ConnectionId = a.Connection.Id, FromPlaceId = a.From, ToPlaceId = a.To, Mode = a.Connection.Mode, DistanceMeters = a.Connection.LengthMeters, TravelSeconds = a.Seconds, GeneralizedCost = a.Cost, Capacity = capacity }); result.TotalDistanceMeters += Math.Max(0, a.Connection.LengthMeters); result.TotalTravelSeconds += a.Seconds; result.TotalGeneralizedCost += a.Cost; result.BottleneckCapacity = Math.Min(result.BottleneckCapacity, capacity); } result.Status = Eq(cursor, originId) ? "ROUTED" : "UNREACHABLE"; if (Double.IsPositiveInfinity(result.BottleneckCapacity)) result.BottleneckCapacity = 0; return SealRoute(result);
        }

        private static SpatialRouteResult RouteWithExcluded(SpatialWorldModel model, string originId, string destinationId, SpatialScenario scenario, HashSet<string> excluded) { return BuildRoute(originId, destinationId, scenario, Dijkstra(Graph(model, scenario, null, "PUBLIC", excluded), originId, destinationId)); }
        private static double ReachablePairs(SpatialWorldModel model, SpatialScenario scenario, List<SpatialPlace> places, HashSet<string> excluded) { double count = 0; Dictionary<string, List<Arc>> graph = Graph(model, scenario, null, "PUBLIC", excluded); foreach (SpatialPlace p in places) { DijkstraState state = Dijkstra(graph, p.Id, null); count += places.Count(x => !Eq(x.Id, p.Id) && state.Cost.ContainsKey(x.Id)); } return count; }
        private static SpatialRouteResult SealRoute(SpatialRouteResult result) { result.RouteHash = HashText((result.OriginPlaceId ?? "") + "|" + (result.DestinationPlaceId ?? "") + "|" + (result.ScenarioId ?? "") + "|" + (result.Status ?? "") + "|" + String.Join("|", result.Legs.Select(x => x.ConnectionId + ":" + x.FromPlaceId + ">" + x.ToPlaceId + ":" + x.GeneralizedCost.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return result; }
        private static SpatialTopologyResult SealTopology(SpatialTopologyResult result) { result.CertificateHash = HashText(result.ActivePlaces + "|" + result.ActiveConnections + "|" + result.WeakComponents + "|" + result.StrongComponents + "|" + result.ParentCycles + "|" + result.TerritoryOverlaps + "|" + String.Join(",", result.ArticulationPlaceIds.ToArray()) + "|" + String.Join(",", result.BridgeConnectionIds.ToArray()) + "|" + String.Join("|", result.Issues.Select(x => x.ToString()).ToArray())); return result; }
        private static SpatialMovementResult SealMovement(SpatialMovementResult result) { result.CertificateHash = HashText((result.ScenarioId ?? "") + "|" + result.RequestedEntities + "|" + result.ArrivedEntities + "|" + result.StrandedEntities + "|" + result.MakespanSeconds.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join("|", result.Movements.Select(x => x.EntityId + ":" + x.ConnectionId + ":" + x.ArriveSecond.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return result; }
        private static SpatialInfrastructureCascadeResult SealCascade(SpatialInfrastructureCascadeResult result) { result.CertificateHash = HashText((result.ScenarioId ?? "") + "|" + result.Depth + "|" + result.CapacityLost.ToString("R", CultureInfo.InvariantCulture) + "|" + result.PopulationExposed.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join(",", result.FailedInfrastructureIds.ToArray()) + "|" + String.Join("|", result.Audit.ToArray())); return result; }
        private static SpatialIndexResult SealIndex(SpatialIndexResult result) { result.CertificateHash = HashText(result.CellSize.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join("|", result.Cells.Select(x => x.Key + ":" + String.Join(",", x.PlaceIds.ToArray()) + ":" + String.Join(",", x.TerritoryIds.ToArray())).ToArray())); return result; }

        private static List<List<string>> StrongComponents(List<string> nodes, Dictionary<string, List<string>> graph)
        {
            List<List<string>> result = new List<List<string>>(); Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Dictionary<string, int> low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> onStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int next = 0; foreach (string v in nodes) if (!index.ContainsKey(v)) StrongDfs(v, graph, index, low, stack, onStack, ref next, result); return result;
        }
        private static void StrongDfs(string v, Dictionary<string, List<string>> graph, Dictionary<string, int> index, Dictionary<string, int> low, Stack<string> stack, HashSet<string> onStack, ref int next, List<List<string>> result)
        {
            index[v] = low[v] = next++; stack.Push(v); onStack.Add(v); foreach (string w in graph[v]) { if (!index.ContainsKey(w)) { StrongDfs(w, graph, index, low, stack, onStack, ref next, result); low[v] = Math.Min(low[v], low[w]); } else if (onStack.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> component = new List<string>(); string w; do { w = stack.Pop(); onStack.Remove(w); component.Add(w); } while (!Eq(w, v)); result.Add(component.OrderBy(x => x).ToList()); }
        }
        private static void ArticulationDfs(string u, string parentId, Dictionary<string, HashSet<string>> graph, List<SpatialConnection> edges, Dictionary<string, int> discovery, Dictionary<string, int> low, Dictionary<string, string> parent, ref int time, List<string> articulations, List<string> bridges)
        {
            discovery[u] = low[u] = ++time; int children = 0; foreach (string v in graph[u]) { if (!discovery.ContainsKey(v)) { children++; parent[v] = u; ArticulationDfs(v, u, graph, edges, discovery, low, parent, ref time, articulations, bridges); low[u] = Math.Min(low[u], low[v]); if (parentId == null && children > 1) articulations.Add(u); if (parentId != null && low[v] >= discovery[u]) articulations.Add(u); if (low[v] > discovery[u]) { SpatialConnection edge = edges.FirstOrDefault(x => (Eq(x.FromPlaceId, u) && Eq(x.ToPlaceId, v)) || (Eq(x.FromPlaceId, v) && Eq(x.ToPlaceId, u))); if (edge != null) bridges.Add(edge.Id); } } else if (!Eq(v, parentId)) low[u] = Math.Min(low[u], discovery[v]); }
        }
        private static int CountTerritoryOverlaps(List<SpatialTerritory> territories) { int count = 0; List<SpatialTerritory> active = (territories ?? new List<SpatialTerritory>()).Where(Active).Take(1024).ToList(); for (int i = 0; i < active.Count; i++) for (int j = i + 1; j < active.Count; j++) { SpatialCoordinate a = FirstVertex(active[i]); SpatialCoordinate b = FirstVertex(active[j]); if ((a != null && Contains(active[j], a)) || (b != null && Contains(active[i], b))) count++; } return count; }
        private static SpatialCoordinate FirstVertex(SpatialTerritory territory) { return (territory.Rings ?? new List<SpatialRing>()).Where(x => x != null && x.Vertices != null && x.Vertices.Count > 0).Select(x => x.Vertices[0]).FirstOrDefault(); }
        private static void DetectParentCycles(Dictionary<string, string> parents, string code, List<SpatialValidationIssue> issues) { foreach (string start in parents.Keys) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); string cursor = start; while (!String.IsNullOrWhiteSpace(cursor) && parents.ContainsKey(cursor)) { if (!seen.Add(cursor)) { Issue(issues, "ERROR", code, start, "Parent containment contains a cycle through " + cursor + "."); break; } cursor = parents[cursor]; } } }
        private static void CheckIds(List<SpatialValidationIssue> issues, string kind, IEnumerable<string> ids) { HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string id in ids ?? Enumerable.Empty<string>()) { if (String.IsNullOrWhiteSpace(id)) Issue(issues, "ERROR", "MISSING_ID", kind, "An entity has no stable identifier."); else if (!seen.Add(id)) Issue(issues, "ERROR", "DUPLICATE_ID", id, "Duplicate " + kind + " identifier."); } }
        private static void CheckCap<T>(List<SpatialValidationIssue> issues, string code, List<T> values, int cap) { if (values != null && values.Count > cap) Issue(issues, "FATAL", code, "WORLD", values.Count.ToString(CultureInfo.InvariantCulture) + " > " + cap.ToString(CultureInfo.InvariantCulture)); }
        private static void Issue(List<SpatialValidationIssue> issues, string severity, string code, string subject, string detail) { issues.Add(new SpatialValidationIssue { Severity = severity, Code = code, SubjectId = subject, Detail = detail }); }
        private static bool PointInRing(SpatialCoordinate point, List<SpatialCoordinate> ring) { bool inside = false; for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++) { SpatialCoordinate a = ring[i], b = ring[j]; if (((a.Y > point.Y) != (b.Y > point.Y)) && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside; } return inside; }
        private static double SignedArea(List<SpatialCoordinate> ring) { if (ring == null || ring.Count < 3) return 0; double sum = 0; for (int i = 0; i < ring.Count; i++) { SpatialCoordinate a = ring[i], b = ring[(i + 1) % ring.Count]; sum += a.X * b.Y - b.X * a.Y; } return sum / 2; }
        private static SpatialCoordinate Centroid(List<SpatialCoordinate> vertices) { if (vertices == null || vertices.Count == 0) return new SpatialCoordinate(); double area = SignedArea(vertices); if (Math.Abs(area) < 1e-12) return new SpatialCoordinate(vertices.Average(q => q.X), vertices.Average(q => q.Y), vertices.Average(q => q.Z)); double x = 0, y = 0; for (int i = 0; i < vertices.Count; i++) { SpatialCoordinate a = vertices[i], b = vertices[(i + 1) % vertices.Count]; double cross = a.X * b.Y - b.X * a.Y; x += (a.X + b.X) * cross; y += (a.Y + b.Y) * cross; } return new SpatialCoordinate(x / (6 * area), y / (6 * area), vertices.Average(v => v.Z)); }
        private static SpatialIndexCell Cell(Dictionary<string, SpatialIndexCell> cells, double x, double y, double size) { long ix = (long)Math.Floor(x / size), iy = (long)Math.Floor(y / size); string key = ix.ToString(CultureInfo.InvariantCulture) + ":" + iy.ToString(CultureInfo.InvariantCulture); SpatialIndexCell cell; if (!cells.TryGetValue(key, out cell)) { cell = new SpatialIndexCell { Key = key, MinX = ix * size, MinY = iy * size, MaxX = (ix + 1) * size, MaxY = (iy + 1) * size }; cells[key] = cell; } return cell; }
        private static HashSet<string> Tokens(string raw) { return new HashSet<string>((raw ?? "").Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase); }
        private static SpatialScenario ScenarioWithInfrastructureCascade(SpatialWorldModel model, SpatialScenario scenario) { return ScenarioWithInfrastructureCascade(model, scenario, InfrastructureCascade(model, scenario)); }
        private static SpatialScenario ScenarioWithInfrastructureCascade(SpatialWorldModel model, SpatialScenario scenario, SpatialInfrastructureCascadeResult cascade) { if (scenario == null || cascade == null || cascade.FailedInfrastructureIds.Count == 0) return scenario; HashSet<string> failed = new HashSet<string>(cascade.FailedInfrastructureIds, StringComparer.OrdinalIgnoreCase); HashSet<string> closed = Tokens(scenario.ClosedConnectionIds); foreach (SpatialConnection edge in model.Connections ?? new List<SpatialConnection>()) if (edge != null && !String.IsNullOrWhiteSpace(edge.InfrastructureId) && failed.Contains(edge.InfrastructureId)) closed.Add(edge.Id); return new SpatialScenario { Id = scenario.Id, Name = scenario.Name, Weight = scenario.Weight, DemandMultiplier = scenario.DemandMultiplier, SpeedMultiplier = scenario.SpeedMultiplier, CapacityMultiplier = scenario.CapacityMultiplier, ReliabilityMultiplier = scenario.ReliabilityMultiplier, HazardMagnitude = scenario.HazardMagnitude, ClosedConnectionIds = String.Join(",", closed.OrderBy(x => x).ToArray()), Status = scenario.Status, Assumptions = new List<string>(scenario.Assumptions ?? new List<string>()) }; }
        private static double MovementEventMultiplier(SpatialWorldModel model, SpatialRouteLeg leg, double second) { if (model == null || leg == null) return 1; Dictionary<string, SpatialPlace> places = new Dictionary<string, SpatialPlace>(StringComparer.OrdinalIgnoreCase); foreach (SpatialPlace place in model.Places ?? new List<SpatialPlace>()) if (place != null && !String.IsNullOrWhiteSpace(place.Id) && !places.ContainsKey(place.Id)) places[place.Id] = place; double multiplier = 1; foreach (SpatialEvent evt in model.Events ?? new List<SpatialEvent>()) { if (evt == null || !Active(evt.Status) || second < evt.StartSecond || second > evt.EndSecond || ((evt.AffectedModeIds ?? new List<string>()).Count > 0 && !(evt.AffectedModeIds ?? new List<string>()).Contains(leg.Mode, StringComparer.OrdinalIgnoreCase))) continue; bool applies = Eq(evt.PlaceId, leg.FromPlaceId) || Eq(evt.PlaceId, leg.ToPlaceId); if (!applies && !String.IsNullOrWhiteSpace(evt.TerritoryId)) { SpatialTerritory territory = (model.Territories ?? new List<SpatialTerritory>()).FirstOrDefault(x => x != null && Eq(x.Id, evt.TerritoryId)); SpatialPlace from; SpatialPlace to; applies = territory != null && ((places.TryGetValue(leg.FromPlaceId ?? "", out from) && Contains(territory, from.Position)) || (places.TryGetValue(leg.ToPlaceId ?? "", out to) && Contains(territory, to.Position))); } if (!applies) continue; double magnitude = Math.Max(0, Math.Min(10, evt.Magnitude)); if ((Eq(evt.Kind, "CLOSURE") || Eq(evt.Kind, "BLOCKADE")) && magnitude >= 0.999) return Double.PositiveInfinity; multiplier *= 1 + magnitude; } return multiplier; }
        private static double EffectiveCapacity(SpatialConnection edge, SpatialScenario scenario) { double hazard = scenario == null ? 0 : Clamp01(scenario.HazardMagnitude); double vulnerability = Clamp01(Math.Max(0, edge.Risk) + Math.Max(0, 1 - edge.Reliability)); double hazardFactor = Math.Max(0, 1 - 0.5 * hazard * vulnerability); return Math.Max(0, edge.CapacityPerHour * (scenario == null ? 1 : Math.Max(0, scenario.CapacityMultiplier)) * hazardFactor); }
        private static bool AccessAllowed(string entityAccess, string edgeAccess) { if (String.IsNullOrWhiteSpace(edgeAccess) || Eq(edgeAccess, "PUBLIC")) return true; return Eq(entityAccess, edgeAccess) || Eq(entityAccess, "SOVEREIGN"); }
        private static double Parameter(SpatialWorldModel model, string key, double fallback) { double value; return model != null && model.Parameters != null && model.Parameters.TryGetValue(key, out value) && Finite(value) ? value : fallback; }
        private static bool Active(SpatialPlace x) { return x != null && Active(x.Status); }
        private static bool Active(SpatialTerritory x) { return x != null && Active(x.Status); }
        private static bool Active(SpatialConnection x) { return x != null && Active(x.Status); }
        private static bool Active(SpatialEntity x) { return x != null && Active(x.Status); }
        private static bool Active(SpatialScenario x) { return x != null && Active(x.Status); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "CLOSED") && !Eq(status, "DESTROYED") && !Eq(status, "RETIRED"); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static double Clamp01(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static double Percentile(List<double> sorted, double p) { if (sorted == null || sorted.Count == 0) return 0; int index = (int)Math.Ceiling(Clamp01(p) * sorted.Count) - 1; return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))]; }
        private static string HashText(string value) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

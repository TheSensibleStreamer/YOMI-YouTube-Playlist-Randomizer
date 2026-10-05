using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class OrLocation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Zone { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double NetSupply { get; set; }
        public double DemandWeight { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrLocation() { Kind = "NODE"; Zone = "DEFAULT"; Status = "ACTIVE"; DemandWeight = 1; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "LOCATION") + "   ·   " + (Kind ?? "NODE") + "   ·   (" + X.ToString("0.###", CultureInfo.InvariantCulture) + ", " + Y.ToString("0.###", CultureInfo.InvariantCulture) + ")   ·   supply " + NetSupply.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrArc
    {
        public string Id { get; set; }
        public string FromLocationId { get; set; }
        public string ToLocationId { get; set; }
        public double Distance { get; set; }
        public double TravelTime { get; set; }
        public double UnitCost { get; set; }
        public double Capacity { get; set; }
        public bool Bidirectional { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrArc() { Distance = 1; TravelTime = 1; UnitCost = 1; Capacity = Double.MaxValue; Bidirectional = true; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (FromLocationId ?? "?") + (Bidirectional ? " ⇄ " : " → ") + (ToLocationId ?? "?") + "   ·   d/t/c " + Distance.ToString("0.###", CultureInfo.InvariantCulture) + "/" + TravelTime.ToString("0.###", CultureInfo.InvariantCulture) + "/" + UnitCost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cap " + Capacity.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrShipment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OriginLocationId { get; set; }
        public string DestinationLocationId { get; set; }
        public double Quantity { get; set; }
        public double ServiceTime { get; set; }
        public double ReadyTime { get; set; }
        public double DueTime { get; set; }
        public double Priority { get; set; }
        public double LatePenalty { get; set; }
        public string RequiredSkill { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrShipment() { Quantity = 1; DueTime = Double.MaxValue; Priority = 1; LatePenalty = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SHIPMENT") + "   ·   " + (OriginLocationId ?? "?") + " → " + (DestinationLocationId ?? "?") + "   ·   q " + Quantity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   window " + ReadyTime.ToString("0.###", CultureInfo.InvariantCulture) + ".." + DueTime.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrVehicle
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DepotLocationId { get; set; }
        public double Capacity { get; set; }
        public double FixedCost { get; set; }
        public double CostPerDistance { get; set; }
        public double Speed { get; set; }
        public double AvailableFrom { get; set; }
        public double AvailableUntil { get; set; }
        public double MaximumRouteDistance { get; set; }
        public int MaximumStops { get; set; }
        public string Status { get; set; }
        public List<string> Skills { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrVehicle() { Capacity = 100; CostPerDistance = 1; Speed = 1; AvailableUntil = Double.MaxValue; MaximumRouteDistance = Double.MaxValue; MaximumStops = 256; Status = "ACTIVE"; Skills = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "VEHICLE") + "   ·   depot " + (DepotLocationId ?? "?") + "   ·   cap " + Capacity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   max stops " + MaximumStops.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrTask
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Duration { get; set; }
        public double ReleaseTime { get; set; }
        public double DueTime { get; set; }
        public double Priority { get; set; }
        public double LatePenalty { get; set; }
        public string ResourceKind { get; set; }
        public int ResourceUnits { get; set; }
        public string LocationId { get; set; }
        public string Status { get; set; }
        public List<string> PredecessorIds { get; set; }
        public List<string> RequiredSkills { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrTask() { Duration = 1; DueTime = Double.MaxValue; Priority = 1; LatePenalty = 1; ResourceKind = "GENERAL"; ResourceUnits = 1; Status = "ACTIVE"; PredecessorIds = new List<string>(); RequiredSkills = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "TASK") + "   ·   duration " + Duration.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (ResourceKind ?? "GENERAL") + "×" + ResourceUnits.ToString(CultureInfo.InvariantCulture) + "   ·   predecessors " + PredecessorIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrResourcePool
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int Capacity { get; set; }
        public double AvailableFrom { get; set; }
        public double AvailableUntil { get; set; }
        public double UnitTimeCost { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrResourcePool() { Kind = "GENERAL"; Capacity = 1; AvailableUntil = Double.MaxValue; UnitTimeCost = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "RESOURCE") + "   ·   " + (Kind ?? "GENERAL") + "   ·   capacity " + Capacity.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrWorker
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string HomeLocationId { get; set; }
        public double CostPerAssignment { get; set; }
        public int MaximumAssignments { get; set; }
        public string Status { get; set; }
        public List<string> Skills { get; set; }
        public List<string> EligibleShiftIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrWorker() { CostPerAssignment = 1; MaximumAssignments = 1; Status = "ACTIVE"; Skills = new List<string>(); EligibleShiftIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "WORKER") + "   ·   skills " + String.Join(",", Skills.ToArray()) + "   ·   cap " + MaximumAssignments.ToString(CultureInfo.InvariantCulture) + "   ·   cost " + CostPerAssignment.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrShiftRequirement
    {
        public string Id { get; set; }
        public string ShiftId { get; set; }
        public string Skill { get; set; }
        public int RequiredWorkers { get; set; }
        public double UnderstaffPenalty { get; set; }
        public string Status { get; set; }
        public OrShiftRequirement() { Skill = "GENERAL"; RequiredWorkers = 1; UnderstaffPenalty = 1000; Status = "ACTIVE"; }
        public override string ToString() { return (ShiftId ?? "SHIFT") + "   ·   " + (Skill ?? "GENERAL") + "   ·   required " + RequiredWorkers.ToString(CultureInfo.InvariantCulture) + "   ·   penalty " + UnderstaffPenalty.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrFacilityCandidate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LocationId { get; set; }
        public double FixedCost { get; set; }
        public double Capacity { get; set; }
        public double HandlingCost { get; set; }
        public bool Mandatory { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrFacilityCandidate() { Capacity = Double.MaxValue; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Mandatory ? "MANDATORY" : (Status ?? "ACTIVE")) + "   ·   " + (Name ?? Id ?? "FACILITY") + "   ·   " + (LocationId ?? "?") + "   ·   fixed " + FixedCost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cap " + Capacity.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrInventoryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string RequiredZone { get; set; }
        public double AnnualDemand { get; set; }
        public double DailyDemandMean { get; set; }
        public double DailyDemandStdDev { get; set; }
        public double OrderCost { get; set; }
        public double AnnualHoldingCost { get; set; }
        public double LeadTimeDays { get; set; }
        public double ServiceLevel { get; set; }
        public double ShortageCost { get; set; }
        public double UnitCost { get; set; }
        public double PickFrequency { get; set; }
        public double Cube { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrInventoryItem() { Family = "GENERAL"; ServiceLevel = 0.95; LeadTimeDays = 1; OrderCost = 1; AnnualHoldingCost = 1; PickFrequency = 1; Cube = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "ITEM") + "   ·   annual demand " + AnnualDemand.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   lead " + LeadTimeDays.ToString("0.###", CultureInfo.InvariantCulture) + "d   ·   service " + ServiceLevel.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrWarehouseSlot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Zone { get; set; }
        public double TravelDistance { get; set; }
        public double CubeCapacity { get; set; }
        public double ErgonomicPenalty { get; set; }
        public string Status { get; set; }
        public OrWarehouseSlot() { Zone = "DEFAULT"; CubeCapacity = 1; Status = "ACTIVE"; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SLOT") + "   ·   zone " + (Zone ?? "DEFAULT") + "   ·   distance " + TravelDistance.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cube " + CubeCapacity.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrPortfolioItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Cost { get; set; }
        public double Value { get; set; }
        public double Risk { get; set; }
        public bool Mandatory { get; set; }
        public string ExclusiveGroup { get; set; }
        public string Status { get; set; }
        public List<string> RequiredItemIds { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OrPortfolioItem() { Status = "ACTIVE"; RequiredItemIds = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Mandatory ? "MANDATORY" : (Status ?? "ACTIVE")) + "   ·   " + (Name ?? Id ?? "OPTION") + "   ·   cost/value/risk " + Cost.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Value.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Risk.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double ProbabilityWeight { get; set; }
        public double DemandMultiplier { get; set; }
        public double TravelCostMultiplier { get; set; }
        public double CapacityMultiplier { get; set; }
        public double DurationMultiplier { get; set; }
        public string Status { get; set; }
        public List<string> Assumptions { get; set; }
        public OrScenario() { ProbabilityWeight = 1; DemandMultiplier = 1; TravelCostMultiplier = 1; CapacityMultiplier = 1; DurationMultiplier = 1; Status = "ACTIVE"; Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SCENARIO") + "   ·   demand/cost/capacity/duration × " + DemandMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + "/" + TravelCostMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + "/" + CapacityMultiplier.ToString("0.###", CultureInfo.InvariantCulture) + "/" + DurationMultiplier.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OperationsResearchModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Revision { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public string ObjectiveSense { get; set; }
        public double Budget { get; set; }
        public int FacilityLimit { get; set; }
        public int Seed { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<OrLocation> Locations { get; set; }
        public List<OrArc> Arcs { get; set; }
        public List<OrShipment> Shipments { get; set; }
        public List<OrVehicle> Vehicles { get; set; }
        public List<OrTask> Tasks { get; set; }
        public List<OrResourcePool> Resources { get; set; }
        public List<OrWorker> Workers { get; set; }
        public List<OrShiftRequirement> ShiftRequirements { get; set; }
        public List<OrFacilityCandidate> Facilities { get; set; }
        public List<OrInventoryItem> Inventory { get; set; }
        public List<OrWarehouseSlot> WarehouseSlots { get; set; }
        public List<OrPortfolioItem> Portfolio { get; set; }
        public List<OrScenario> Scenarios { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public OperationsResearchModel()
        {
            Status = "ACTIVE"; Revision = 1; ObjectiveSense = "MINIMIZE_COST"; Budget = 1000; FacilityLimit = 2; Seed = 1337; Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Locations = new List<OrLocation>(); Arcs = new List<OrArc>(); Shipments = new List<OrShipment>(); Vehicles = new List<OrVehicle>(); Tasks = new List<OrTask>(); Resources = new List<OrResourcePool>(); Workers = new List<OrWorker>(); ShiftRequirements = new List<OrShiftRequirement>(); Facilities = new List<OrFacilityCandidate>(); Inventory = new List<OrInventoryItem>(); WarehouseSlots = new List<OrWarehouseSlot>(); Portfolio = new List<OrPortfolioItem>(); Scenarios = new List<OrScenario>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>();
        }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "OPERATIONS MODEL") + "   ·   " + Locations.Count.ToString(CultureInfo.InvariantCulture) + " nodes / " + Shipments.Count.ToString(CultureInfo.InvariantCulture) + " shipments / " + Tasks.Count.ToString(CultureInfo.InvariantCulture) + " tasks   ·   rev " + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrPathResult
    {
        public string OriginId { get; set; }
        public string DestinationId { get; set; }
        public List<string> LocationIds { get; set; }
        public double Distance { get; set; }
        public double TravelTime { get; set; }
        public double Cost { get; set; }
        public bool Reachable { get; set; }
        public List<string> Audit { get; set; }
        public OrPathResult() { LocationIds = new List<string>(); Audit = new List<string>(); Distance = Double.PositiveInfinity; TravelTime = Double.PositiveInfinity; Cost = Double.PositiveInfinity; }
        public override string ToString() { return "PATH   ·   " + (OriginId ?? "?") + " → " + (DestinationId ?? "?") + "   ·   " + (Reachable ? String.Join(" › ", LocationIds.ToArray()) : "UNREACHABLE") + "   ·   distance/time/cost " + F(Distance) + "/" + F(TravelTime) + "/" + F(Cost); }
        private static string F(double x) { return Double.IsInfinity(x) ? "∞" : x.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrRouteStop
    {
        public string ShipmentId { get; set; }
        public string LocationId { get; set; }
        public double ArrivalTime { get; set; }
        public double DepartureTime { get; set; }
        public double LoadAfter { get; set; }
        public double Lateness { get; set; }
        public override string ToString() { return (ShipmentId ?? "STOP") + "   ·   " + (LocationId ?? "?") + "   ·   arrive/depart " + ArrivalTime.ToString("0.###", CultureInfo.InvariantCulture) + "/" + DepartureTime.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   load " + LoadAfter.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   late " + Lateness.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrVehicleRoute
    {
        public string VehicleId { get; set; }
        public List<OrRouteStop> Stops { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public double Load { get; set; }
        public double Cost { get; set; }
        public double LatePenalty { get; set; }
        public bool Feasible { get; set; }
        public OrVehicleRoute() { Stops = new List<OrRouteStop>(); Feasible = true; }
        public override string ToString() { return "ROUTE   ·   " + (VehicleId ?? "VEHICLE") + "   ·   stops " + Stops.Count.ToString(CultureInfo.InvariantCulture) + "   ·   distance/duration/load " + Distance.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Duration.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Load.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cost " + (Cost + LatePenalty).ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Feasible ? "FEASIBLE" : "VIOLATED"); }
    }

    internal sealed class OrRoutingResult
    {
        public List<OrVehicleRoute> Routes { get; set; }
        public List<string> UnservedShipmentIds { get; set; }
        public double TotalDistance { get; set; }
        public double TotalCost { get; set; }
        public double TotalLatePenalty { get; set; }
        public double CapacityUtilization { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrRoutingResult() { Routes = new List<OrVehicleRoute>(); UnservedShipmentIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "VEHICLE ROUTING   ·   " + Routes.Count.ToString(CultureInfo.InvariantCulture) + " routes   ·   distance " + TotalDistance.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cost+late " + (TotalCost + TotalLatePenalty).ToString("0.###", CultureInfo.InvariantCulture) + "   ·   utilization " + CapacityUtilization.ToString("P1", CultureInfo.InvariantCulture) + "   ·   unserved " + UnservedShipmentIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrScheduledTask
    {
        public string TaskId { get; set; }
        public string ResourcePoolId { get; set; }
        public string LaneIds { get; set; }
        public double Start { get; set; }
        public double Finish { get; set; }
        public double Lateness { get; set; }
        public bool Critical { get; set; }
        public override string ToString() { return (Critical ? "CRITICAL" : "SCHEDULED") + "   ·   " + (TaskId ?? "TASK") + "   ·   " + (ResourcePoolId ?? "RESOURCE") + "[" + (LaneIds ?? "") + "]   ·   " + Start.ToString("0.###", CultureInfo.InvariantCulture) + ".." + Finish.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   late " + Lateness.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrScheduleResult
    {
        public List<OrScheduledTask> Tasks { get; set; }
        public List<string> UnscheduledTaskIds { get; set; }
        public double Makespan { get; set; }
        public double WeightedTardiness { get; set; }
        public double ResourceUtilization { get; set; }
        public List<string> CriticalTaskIds { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrScheduleResult() { Tasks = new List<OrScheduledTask>(); UnscheduledTaskIds = new List<string>(); CriticalTaskIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "RESOURCE SCHEDULE   ·   " + Tasks.Count.ToString(CultureInfo.InvariantCulture) + " tasks   ·   makespan " + Makespan.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   weighted tardiness " + WeightedTardiness.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   utilization " + ResourceUtilization.ToString("P1", CultureInfo.InvariantCulture) + "   ·   unscheduled " + UnscheduledTaskIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrAssignmentRecord
    {
        public string WorkerId { get; set; }
        public string TaskId { get; set; }
        public double Cost { get; set; }
        public bool SkillCompatible { get; set; }
        public override string ToString() { return (WorkerId ?? "WORKER") + " → " + (TaskId ?? "TASK") + "   ·   cost " + Cost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (SkillCompatible ? "SKILL MATCH" : "INCOMPATIBLE"); }
    }

    internal sealed class OrAssignmentResult
    {
        public List<OrAssignmentRecord> Assignments { get; set; }
        public List<string> UnassignedWorkerIds { get; set; }
        public List<string> UnfilledTaskIds { get; set; }
        public double TotalCost { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrAssignmentResult() { Assignments = new List<OrAssignmentRecord>(); UnassignedWorkerIds = new List<string>(); UnfilledTaskIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "HUNGARIAN ASSIGNMENT   ·   " + Assignments.Count.ToString(CultureInfo.InvariantCulture) + " matches   ·   cost " + TotalCost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   unfilled " + UnfilledTaskIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrFlowRecord
    {
        public string ArcId { get; set; }
        public string FromLocationId { get; set; }
        public string ToLocationId { get; set; }
        public double Flow { get; set; }
        public double Capacity { get; set; }
        public double UnitCost { get; set; }
        public override string ToString() { return (ArcId ?? "ARC") + "   ·   " + (FromLocationId ?? "?") + " → " + (ToLocationId ?? "?") + "   ·   " + Flow.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Capacity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   unit " + UnitCost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrFlowResult
    {
        public List<OrFlowRecord> Flows { get; set; }
        public double RequiredFlow { get; set; }
        public double DeliveredFlow { get; set; }
        public double TotalCost { get; set; }
        public bool Feasible { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrFlowResult() { Flows = new List<OrFlowRecord>(); Audit = new List<string>(); }
        public override string ToString() { return "MIN-COST FLOW   ·   " + DeliveredFlow.ToString("0.###", CultureInfo.InvariantCulture) + "/" + RequiredFlow.ToString("0.###", CultureInfo.InvariantCulture) + " delivered   ·   cost " + TotalCost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Feasible ? "FEASIBLE" : "DEFICIT"); }
    }

    internal sealed class OrFacilityAssignment
    {
        public string DemandLocationId { get; set; }
        public string FacilityId { get; set; }
        public double Quantity { get; set; }
        public double Distance { get; set; }
        public double Cost { get; set; }
        public override string ToString() { return (DemandLocationId ?? "DEMAND") + " → " + (FacilityId ?? "FACILITY") + "   ·   q " + Quantity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   distance/cost " + Distance.ToString("0.###", CultureInfo.InvariantCulture) + "/" + Cost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrFacilityResult
    {
        public List<string> OpenFacilityIds { get; set; }
        public List<OrFacilityAssignment> Assignments { get; set; }
        public double FixedCost { get; set; }
        public double TransportCost { get; set; }
        public double UnservedDemand { get; set; }
        public double MeanServiceDistance { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrFacilityResult() { OpenFacilityIds = new List<string>(); Assignments = new List<OrFacilityAssignment>(); Audit = new List<string>(); }
        public override string ToString() { return "FACILITY LOCATION   ·   open " + OpenFacilityIds.Count.ToString(CultureInfo.InvariantCulture) + "   ·   fixed+transport " + (FixedCost + TransportCost).ToString("0.###", CultureInfo.InvariantCulture) + "   ·   mean distance " + MeanServiceDistance.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   unserved " + UnservedDemand.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrSlotAssignment
    {
        public string ItemId { get; set; }
        public string SlotId { get; set; }
        public double AnnualTravelBurden { get; set; }
        public bool Compatible { get; set; }
        public override string ToString() { return (ItemId ?? "ITEM") + " → " + (SlotId ?? "SLOT") + "   ·   burden " + AnnualTravelBurden.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Compatible ? "COMPATIBLE" : "VIOLATION"); }
    }

    internal sealed class OrSlottingResult
    {
        public List<OrSlotAssignment> Assignments { get; set; }
        public List<string> UnslottedItemIds { get; set; }
        public double TotalTravelBurden { get; set; }
        public int CompatibilityViolations { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrSlottingResult() { Assignments = new List<OrSlotAssignment>(); UnslottedItemIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "WAREHOUSE SLOTTING   ·   " + Assignments.Count.ToString(CultureInfo.InvariantCulture) + " placements   ·   travel burden " + TotalTravelBurden.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   violations " + CompatibilityViolations.ToString(CultureInfo.InvariantCulture) + "   ·   unslotted " + UnslottedItemIds.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrStaffAssignment
    {
        public string WorkerId { get; set; }
        public string ShiftId { get; set; }
        public string Skill { get; set; }
        public double Cost { get; set; }
        public override string ToString() { return (WorkerId ?? "WORKER") + " → " + (ShiftId ?? "SHIFT") + "   ·   " + (Skill ?? "GENERAL") + "   ·   cost " + Cost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrStaffingResult
    {
        public List<OrStaffAssignment> Assignments { get; set; }
        public Dictionary<string, int> RemainingDeficits { get; set; }
        public double LaborCost { get; set; }
        public double UnderstaffPenalty { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrStaffingResult() { Assignments = new List<OrStaffAssignment>(); RemainingDeficits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Audit = new List<string>(); }
        public override string ToString() { return "WORKFORCE COVERAGE   ·   " + Assignments.Count.ToString(CultureInfo.InvariantCulture) + " assignments   ·   labor+deficit " + (LaborCost + UnderstaffPenalty).ToString("0.###", CultureInfo.InvariantCulture) + "   ·   uncovered " + RemainingDeficits.Values.Sum().ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrInventoryPolicy
    {
        public string ItemId { get; set; }
        public double EconomicOrderQuantity { get; set; }
        public double SafetyStock { get; set; }
        public double ReorderPoint { get; set; }
        public double NewsvendorQuantity { get; set; }
        public double ExpectedAnnualRelevantCost { get; set; }
        public override string ToString() { return (ItemId ?? "ITEM") + "   ·   EOQ " + EconomicOrderQuantity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   safety " + SafetyStock.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   reorder " + ReorderPoint.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   newsvendor " + NewsvendorQuantity.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   annual relevant cost " + ExpectedAnnualRelevantCost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrInventoryResult
    {
        public List<OrInventoryPolicy> Policies { get; set; }
        public double AggregateAnnualRelevantCost { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrInventoryResult() { Policies = new List<OrInventoryPolicy>(); Audit = new List<string>(); }
        public override string ToString() { return "INVENTORY POLICY   ·   " + Policies.Count.ToString(CultureInfo.InvariantCulture) + " items   ·   annual relevant cost " + AggregateAnnualRelevantCost.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrPortfolioResult
    {
        public List<string> SelectedItemIds { get; set; }
        public double TotalCost { get; set; }
        public double TotalValue { get; set; }
        public double TotalRisk { get; set; }
        public double Objective { get; set; }
        public double BudgetSlack { get; set; }
        public bool Feasible { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrPortfolioResult() { SelectedItemIds = new List<string>(); Audit = new List<string>(); }
        public override string ToString() { return "PORTFOLIO   ·   " + SelectedItemIds.Count.ToString(CultureInfo.InvariantCulture) + " selected   ·   cost/value/risk " + TotalCost.ToString("0.###", CultureInfo.InvariantCulture) + "/" + TotalValue.ToString("0.###", CultureInfo.InvariantCulture) + "/" + TotalRisk.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   slack " + BudgetSlack.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   " + (Feasible ? "FEASIBLE" : "VIOLATED"); }
    }

    internal sealed class OrScenarioScore
    {
        public string ScenarioId { get; set; }
        public double ProbabilityWeight { get; set; }
        public double RoutingCost { get; set; }
        public int UnservedShipments { get; set; }
        public double Makespan { get; set; }
        public int UnscheduledTasks { get; set; }
        public double FlowDeficit { get; set; }
        public double CompositeLoss { get; set; }
        public override string ToString() { return (ScenarioId ?? "SCENARIO") + "   ·   loss " + CompositeLoss.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   route " + RoutingCost.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   unserved/unscheduled " + UnservedShipments.ToString(CultureInfo.InvariantCulture) + "/" + UnscheduledTasks.ToString(CultureInfo.InvariantCulture) + "   ·   flow deficit " + FlowDeficit.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrRobustnessResult
    {
        public List<OrScenarioScore> Scores { get; set; }
        public double WeightedMeanLoss { get; set; }
        public double WorstLoss { get; set; }
        public string WorstScenarioId { get; set; }
        public double LossSpread { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrRobustnessResult() { Scores = new List<OrScenarioScore>(); Audit = new List<string>(); }
        public override string ToString() { return "SCENARIO STRESS   ·   worlds " + Scores.Count.ToString(CultureInfo.InvariantCulture) + "   ·   weighted/worst loss " + WeightedMeanLoss.ToString("0.###", CultureInfo.InvariantCulture) + "/" + WorstLoss.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   worst " + (WorstScenarioId ?? "∅") + "   ·   spread " + LossSpread.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrTradePoint
    {
        public int OpenFacilities { get; set; }
        public double TotalCost { get; set; }
        public double MeanServiceDistance { get; set; }
        public double UnservedDemand { get; set; }
        public bool ParetoEfficient { get; set; }
        public override string ToString() { return (ParetoEfficient ? "PARETO" : "DOMINATED") + "   ·   facilities " + OpenFacilities.ToString(CultureInfo.InvariantCulture) + "   ·   cost/distance/unserved " + TotalCost.ToString("0.###", CultureInfo.InvariantCulture) + "/" + MeanServiceDistance.ToString("0.###", CultureInfo.InvariantCulture) + "/" + UnservedDemand.ToString("0.###", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrTradeSpaceResult
    {
        public List<OrTradePoint> Points { get; set; }
        public string KneeDescription { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrTradeSpaceResult() { Points = new List<OrTradePoint>(); Audit = new List<string>(); }
        public override string ToString() { return "FACILITY TRADE SPACE   ·   " + Points.Count.ToString(CultureInfo.InvariantCulture) + " designs   ·   Pareto " + Points.Count(x => x.ParetoEfficient).ToString(CultureInfo.InvariantCulture) + "   ·   knee " + (KneeDescription ?? "UNRESOLVED"); }
    }

    internal sealed class OrSensitivityEffect
    {
        public string ParameterKey { get; set; }
        public double BaselineValue { get; set; }
        public double MinusLoss { get; set; }
        public double PlusLoss { get; set; }
        public double ElementaryEffect { get; set; }
        public double Elasticity { get; set; }
        public override string ToString() { return (ParameterKey ?? "parameter") + "   ·   base " + BaselineValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   loss −/+ " + MinusLoss.ToString("0.###", CultureInfo.InvariantCulture) + "/" + PlusLoss.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   effect " + ElementaryEffect.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   elasticity " + Elasticity.ToString("0.######", CultureInfo.InvariantCulture); }
    }

    internal sealed class OrSensitivityResult
    {
        public List<OrSensitivityEffect> Effects { get; set; }
        public string DominantParameter { get; set; }
        public double ConditionProxy { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrSensitivityResult() { Effects = new List<OrSensitivityEffect>(); Audit = new List<string>(); }
        public override string ToString() { return "OPTIMIZATION SENSITIVITY   ·   dominant " + (DominantParameter ?? "∅") + "   ·   condition proxy " + ConditionProxy.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   dimensions " + Effects.Count.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class OrSolveBundle
    {
        public string ModelFingerprint { get; set; }
        public OrRoutingResult Routing { get; set; }
        public OrScheduleResult Schedule { get; set; }
        public OrAssignmentResult Assignment { get; set; }
        public OrFlowResult Flow { get; set; }
        public OrFacilityResult Facilities { get; set; }
        public OrSlottingResult Slotting { get; set; }
        public OrStaffingResult Staffing { get; set; }
        public OrInventoryResult Inventory { get; set; }
        public OrPortfolioResult Portfolio { get; set; }
        public double CompositeLoss { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public OrSolveBundle() { Audit = new List<string>(); }
        public override string ToString() { return "OPERATIONS SOLVE BUNDLE   ·   composite loss " + CompositeLoss.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   cert " + Short(CertificateHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(20, x.Length)); }
    }

    internal static class OperationsResearchKernel
    {
        private const double Epsilon = 1e-9;
        private const double Forbidden = 1e12;
        private const int MaximumNodes = 4096;
        private const int MaximumArcs = 262144;
        private const int MaximumItems = 8192;

        private sealed class MetricEdge { public int To; public double Distance; public double Time; public double Cost; }
        private sealed class ResidualEdge { public int To; public int Reverse; public double Capacity; public double Cost; public double OriginalCapacity; public string ArcId; public bool Original; }
        private sealed class FacilityEvaluation { public double Fixed; public double Transport; public double Unserved; public double MeanDistance; public List<OrFacilityAssignment> Assignments = new List<OrFacilityAssignment>(); }

        public static OrPathResult ShortestPath(OperationsResearchModel model, string originId, string destinationId, string metric)
        {
            OrPathResult result = new OrPathResult { OriginId = originId, DestinationId = destinationId }; result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result;
            List<OrLocation> nodes = ActiveList(model.Locations); Dictionary<string, int> index = Index(nodes.Select(x => x.Id)); int source, target; if (!index.TryGetValue(originId ?? "", out source) || !index.TryGetValue(destinationId ?? "", out target)) { result.Audit.Add("BLOCKER · PATH_ENDPOINT_UNKNOWN"); return result; }
            List<MetricEdge>[] graph = MetricGraph(model, nodes, index); int n = nodes.Count; double[] d = Enumerable.Repeat(Double.PositiveInfinity, n).ToArray(); int[] prior = Enumerable.Repeat(-1, n).ToArray(); bool[] used = new bool[n]; d[source] = 0;
            for (int iteration = 0; iteration < n; iteration++) { int u = -1; for (int i = 0; i < n; i++) if (!used[i] && (u < 0 || d[i] < d[u])) u = i; if (u < 0 || Double.IsInfinity(d[u])) break; used[u] = true; if (u == target) break; foreach (MetricEdge e in graph[u]) { double weight = Eq(metric, "TIME") ? e.Time : (Eq(metric, "COST") ? e.Cost : e.Distance); if (weight < 0) { result.Audit.Add("BLOCKER · NEGATIVE_PATH_WEIGHT"); return result; } if (d[u] + weight + Epsilon < d[e.To]) { d[e.To] = d[u] + weight; prior[e.To] = u; } } }
            if (Double.IsInfinity(d[target])) { result.Audit.Add("WARN · DESTINATION_UNREACHABLE"); return result; } List<int> reversed = new List<int>(); for (int at = target; at >= 0; at = prior[at]) { reversed.Add(at); if (at == source) break; } reversed.Reverse(); result.LocationIds = reversed.Select(x => nodes[x].Id).ToList(); result.Reachable = result.LocationIds.Count > 0 && Eq(result.LocationIds[0], originId); result.Distance = PathMetric(graph, reversed, "DISTANCE"); result.TravelTime = PathMetric(graph, reversed, "TIME"); result.Cost = PathMetric(graph, reversed, "COST"); return result;
        }

        public static OrRoutingResult SolveVehicleRouting(OperationsResearchModel model)
        {
            OrRoutingResult result = new OrRoutingResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrVehicle> vehicles = ActiveList(model.Vehicles); List<OrShipment> shipments = ActiveList(model.Shipments).OrderByDescending(x => x.Priority).ThenBy(x => x.DueTime).ThenBy(x => x.Id).ToList(); if (vehicles.Count == 0) { result.UnservedShipmentIds.AddRange(shipments.Select(x => x.Id)); result.Audit.Add("WARN · NO_ACTIVE_VEHICLES"); return result; }
            Dictionary<string, OrLocation> locations = ActiveList(model.Locations).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); double demandMultiplier = P(model, "demand_multiplier", 1), capacityMultiplier = P(model, "capacity_multiplier", 1), costMultiplier = P(model, "travel_cost_multiplier", 1); Dictionary<string, Dictionary<string, double>> distances = AllPairs(model, "DISTANCE"), times = AllPairs(model, "TIME");
            foreach (OrVehicle vehicle in vehicles) result.Routes.Add(new OrVehicleRoute { VehicleId = vehicle.Id });
            foreach (OrShipment shipment in shipments)
            {
                double quantity = Math.Max(0, shipment.Quantity * demandMultiplier); int bestRoute = -1, bestPosition = -1; double bestIncrement = Double.PositiveInfinity;
                for (int r = 0; r < result.Routes.Count; r++)
                {
                    OrVehicle vehicle = vehicles[r]; OrVehicleRoute route = result.Routes[r]; if (!Eq(vehicle.DepotLocationId, shipment.OriginLocationId) || route.Stops.Count >= vehicle.MaximumStops || route.Load + quantity > vehicle.Capacity * capacityMultiplier + Epsilon || (!String.IsNullOrWhiteSpace(shipment.RequiredSkill) && !vehicle.Skills.Contains(shipment.RequiredSkill, StringComparer.OrdinalIgnoreCase))) continue;
                    for (int position = 0; position <= route.Stops.Count; position++) { string before = position == 0 ? vehicle.DepotLocationId : route.Stops[position - 1].LocationId; string after = position == route.Stops.Count ? vehicle.DepotLocationId : route.Stops[position].LocationId; double increment = D(distances, before, shipment.DestinationLocationId) + D(distances, shipment.DestinationLocationId, after) - D(distances, before, after); if (!Finite(increment)) continue; if (increment + Epsilon < bestIncrement) { bestIncrement = increment; bestRoute = r; bestPosition = position; } }
                }
                if (bestRoute < 0) { result.UnservedShipmentIds.Add(shipment.Id); continue; } result.Routes[bestRoute].Stops.Insert(bestPosition, new OrRouteStop { ShipmentId = shipment.Id, LocationId = shipment.DestinationLocationId }); result.Routes[bestRoute].Load += quantity;
            }
            Dictionary<string, OrShipment> shipmentById = shipments.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); for (int r = 0; r < result.Routes.Count; r++) { TwoOpt(result.Routes[r], vehicles[r], distances); EvaluateRoute(result.Routes[r], vehicles[r], shipmentById, distances, times, demandMultiplier, capacityMultiplier, costMultiplier); }
            result.Routes = result.Routes.Where(x => x.Stops.Count > 0).ToList(); result.TotalDistance = result.Routes.Sum(x => x.Distance); result.TotalCost = result.Routes.Sum(x => x.Cost); result.TotalLatePenalty = result.Routes.Sum(x => x.LatePenalty); double used = result.Routes.Sum(x => x.Load), available = result.Routes.Sum(x => Math.Max(Epsilon, vehicles.First(v => Eq(v.Id, x.VehicleId)).Capacity * capacityMultiplier)); result.CapacityUtilization = available <= Epsilon ? 0 : used / available; result.CertificateHash = HashText(Fingerprint(model) + "|ROUTING|" + String.Join(";", result.Routes.Select(x => x.VehicleId + ":" + String.Join(",", x.Stops.Select(s => s.ShipmentId).ToArray()) + ":" + x.Distance.ToString("R", CultureInfo.InvariantCulture)).ToArray()) + "|UNSERVED=" + String.Join(",", result.UnservedShipmentIds.ToArray())); result.Audit.Add("BOUNDARY · insertion plus 2-opt is deterministic bounded heuristic optimization and does not certify global VRP optimality"); return result;
        }

        public static OrScheduleResult SolveSchedule(OperationsResearchModel model)
        {
            OrScheduleResult result = new OrScheduleResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrTask> tasks = ActiveList(model.Tasks); Dictionary<string, OrTask> byId = tasks.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase); Dictionary<string, int> indegree = tasks.ToDictionary(x => x.Id, x => 0, StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> successors = tasks.ToDictionary(x => x.Id, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (OrTask task in tasks) foreach (string p in task.PredecessorIds.Where(byId.ContainsKey)) { indegree[task.Id]++; successors[p].Add(task.Id); }
            List<OrResourcePool> pools = ActiveList(model.Resources); Dictionary<string, List<double>> lanes = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase); foreach (OrResourcePool pool in pools) lanes[pool.Id] = Enumerable.Repeat(pool.AvailableFrom, Math.Max(1, pool.Capacity)).ToList(); Dictionary<string, double> finishes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); double durationMultiplier = P(model, "duration_multiplier", 1), capacityMultiplier = P(model, "capacity_multiplier", 1); HashSet<string> remaining = new HashSet<string>(tasks.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            while (remaining.Count > 0)
            {
                OrTask task = remaining.Where(x => indegree[x] == 0).Select(x => byId[x]).OrderByDescending(x => CriticalRank(x, byId, successors, new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase))).ThenBy(x => x.DueTime).ThenByDescending(x => x.Priority).ThenBy(x => x.Id).FirstOrDefault(); if (task == null) break;
                OrResourcePool pool = pools.Where(x => Eq(x.Kind, task.ResourceKind)).OrderBy(x => x.UnitTimeCost).ThenByDescending(x => x.Capacity).ThenBy(x => x.Id).FirstOrDefault(); if (pool == null) { result.UnscheduledTaskIds.Add(task.Id); remaining.Remove(task.Id); foreach (string s in successors[task.Id]) indegree[s]--; continue; }
                List<double> availability = lanes[pool.Id]; int effective = Math.Max(1, Math.Min(availability.Count, (int)Math.Floor(availability.Count * Math.Max(Epsilon, capacityMultiplier)))); int units = Math.Max(1, task.ResourceUnits); if (units > effective) { result.UnscheduledTaskIds.Add(task.Id); remaining.Remove(task.Id); foreach (string s in successors[task.Id]) indegree[s]--; continue; } List<int> selected = Enumerable.Range(0, effective).OrderBy(i => availability[i]).Take(units).ToList(); double predecessorFinish = task.PredecessorIds.Where(finishes.ContainsKey).Select(x => finishes[x]).DefaultIfEmpty(0).Max(), start = Math.Max(task.ReleaseTime, Math.Max(predecessorFinish, selected.Max(i => availability[i]))), finish = start + Math.Max(Epsilon, task.Duration * durationMultiplier); if (finish > pool.AvailableUntil + Epsilon) { result.UnscheduledTaskIds.Add(task.Id); } else { foreach (int lane in selected) availability[lane] = finish; finishes[task.Id] = finish; OrScheduledTask scheduled = new OrScheduledTask { TaskId = task.Id, ResourcePoolId = pool.Id, LaneIds = String.Join(",", selected.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()), Start = start, Finish = finish, Lateness = Math.Max(0, finish - task.DueTime) }; result.Tasks.Add(scheduled); result.WeightedTardiness += scheduled.Lateness * task.LatePenalty * task.Priority; }
                remaining.Remove(task.Id); foreach (string s in successors[task.Id]) indegree[s]--;
            }
            result.UnscheduledTaskIds.AddRange(remaining.OrderBy(x => x)); result.Makespan = result.Tasks.Select(x => x.Finish).DefaultIfEmpty(0).Max(); double busy = result.Tasks.Sum(x => (x.Finish - x.Start) * byId[x.TaskId].ResourceUnits), availableCapacityTime = pools.Sum(x => Math.Max(0, Math.Min(result.Makespan, x.AvailableUntil) - x.AvailableFrom) * Math.Max(1, x.Capacity)); result.ResourceUtilization = availableCapacityTime <= Epsilon ? 0 : busy / availableCapacityTime;
            if (result.Tasks.Count > 0) { double end = result.Makespan; OrScheduledTask at = result.Tasks.OrderByDescending(x => x.Finish).First(); HashSet<string> critical = new HashSet<string>(StringComparer.OrdinalIgnoreCase); while (at != null && critical.Add(at.TaskId)) { OrTask source = byId[at.TaskId]; at = result.Tasks.Where(x => source.PredecessorIds.Contains(x.TaskId, StringComparer.OrdinalIgnoreCase) && Math.Abs(x.Finish - at.Start) <= 1e-6).OrderByDescending(x => x.Finish).FirstOrDefault(); } foreach (OrScheduledTask x in result.Tasks) { x.Critical = critical.Contains(x.TaskId); if (x.Critical) result.CriticalTaskIds.Add(x.TaskId); } }
            result.CertificateHash = HashText(Fingerprint(model) + "|SCHEDULE|" + String.Join(";", result.Tasks.Select(x => x.TaskId + ":" + x.Start.ToString("R", CultureInfo.InvariantCulture) + ":" + x.Finish.ToString("R", CultureInfo.InvariantCulture) + ":" + x.ResourcePoolId).ToArray()) + "|UNSCHEDULED=" + String.Join(",", result.UnscheduledTaskIds.ToArray())); result.Audit.Add("BOUNDARY · serial schedule generation is precedence-feasible where declared resources permit but does not certify global job-shop optimality"); return result;
        }

        public static OrAssignmentResult SolveAssignment(OperationsResearchModel model)
        {
            OrAssignmentResult result = new OrAssignmentResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrWorker> workers = ActiveList(model.Workers); List<OrTask> tasks = ActiveList(model.Tasks); if (workers.Count == 0 || tasks.Count == 0) { result.UnassignedWorkerIds.AddRange(workers.Select(x => x.Id)); result.UnfilledTaskIds.AddRange(tasks.Select(x => x.Id)); return result; } Dictionary<string, Dictionary<string, double>> distances = AllPairs(model, "DISTANCE"); int n = Math.Max(workers.Count, tasks.Count); double[,] cost = new double[n, n]; bool[,] compatible = new bool[workers.Count, tasks.Count];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i >= workers.Count || j >= tasks.Count) cost[i, j] = 0; else { OrWorker w = workers[i]; OrTask t = tasks[j]; compatible[i, j] = t.RequiredSkills.Count == 0 || t.RequiredSkills.All(s => w.Skills.Contains(s, StringComparer.OrdinalIgnoreCase)); double travel = String.IsNullOrWhiteSpace(w.HomeLocationId) || String.IsNullOrWhiteSpace(t.LocationId) ? 0 : D(distances, w.HomeLocationId, t.LocationId); cost[i, j] = w.CostPerAssignment + (Finite(travel) ? travel : Forbidden / 10) + (compatible[i, j] ? 0 : Forbidden); } }
            int[] assignment = Hungarian(cost, n); HashSet<int> filled = new HashSet<int>(); for (int i = 0; i < workers.Count; i++) { int j = assignment[i]; if (j >= 0 && j < tasks.Count && cost[i, j] < Forbidden) { result.Assignments.Add(new OrAssignmentRecord { WorkerId = workers[i].Id, TaskId = tasks[j].Id, Cost = cost[i, j], SkillCompatible = compatible[i, j] }); result.TotalCost += cost[i, j]; filled.Add(j); } else result.UnassignedWorkerIds.Add(workers[i].Id); } for (int j = 0; j < tasks.Count; j++) if (!filled.Contains(j)) result.UnfilledTaskIds.Add(tasks[j].Id); result.CertificateHash = HashText(Fingerprint(model) + "|ASSIGNMENT|" + String.Join(";", result.Assignments.Select(x => x.WorkerId + ":" + x.TaskId + ":" + x.Cost.ToString("R", CultureInfo.InvariantCulture)).ToArray())); result.Audit.Add("BOUNDARY · assignment is exact for the constructed one-worker/one-task cost matrix; multi-period workload coupling belongs to the staffing model"); return result;
        }

        public static OrFlowResult SolveMinCostFlow(OperationsResearchModel model)
        {
            OrFlowResult result = new OrFlowResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrLocation> nodes = ActiveList(model.Locations); Dictionary<string, int> index = Index(nodes.Select(x => x.Id)); int n = nodes.Count, source = n, sink = n + 1; List<ResidualEdge>[] graph = Enumerable.Range(0, n + 2).Select(x => new List<ResidualEdge>()).ToArray(); double demandMultiplier = P(model, "demand_multiplier", 1), capacityMultiplier = P(model, "capacity_multiplier", 1), costMultiplier = P(model, "travel_cost_multiplier", 1);
            foreach (OrArc arc in ActiveList(model.Arcs)) { int from, to; if (!index.TryGetValue(arc.FromLocationId ?? "", out from) || !index.TryGetValue(arc.ToLocationId ?? "", out to)) continue; AddResidual(graph, from, to, Math.Max(0, arc.Capacity * capacityMultiplier), arc.UnitCost * costMultiplier, arc.Id); if (arc.Bidirectional) AddResidual(graph, to, from, Math.Max(0, arc.Capacity * capacityMultiplier), arc.UnitCost * costMultiplier, arc.Id + "#reverse"); }
            double supply = 0, demand = 0; for (int i = 0; i < n; i++) { double balance = nodes[i].NetSupply * demandMultiplier; if (balance > Epsilon) { AddResidual(graph, source, i, balance, 0, "SUPPLY:" + nodes[i].Id); supply += balance; } else if (balance < -Epsilon) { AddResidual(graph, i, sink, -balance, 0, "DEMAND:" + nodes[i].Id); demand -= balance; } } result.RequiredFlow = demand; int limit = Math.Max(1, (n + ActiveList(model.Arcs).Count) * 8);
            for (int iteration = 0; iteration < limit && result.DeliveredFlow + Epsilon < demand; iteration++) { double[] dist = Enumerable.Repeat(Double.PositiveInfinity, n + 2).ToArray(); int[] priorNode = Enumerable.Repeat(-1, n + 2).ToArray(), priorEdge = Enumerable.Repeat(-1, n + 2).ToArray(); dist[source] = 0; for (int pass = 0; pass < n + 1; pass++) { bool changed = false; for (int u = 0; u < graph.Length; u++) if (!Double.IsInfinity(dist[u])) for (int e = 0; e < graph[u].Count; e++) { ResidualEdge edge = graph[u][e]; if (edge.Capacity > Epsilon && dist[u] + edge.Cost + Epsilon < dist[edge.To]) { dist[edge.To] = dist[u] + edge.Cost; priorNode[edge.To] = u; priorEdge[edge.To] = e; changed = true; } } if (!changed) break; } if (priorNode[sink] < 0) break; double augment = demand - result.DeliveredFlow; for (int v = sink; v != source; v = priorNode[v]) augment = Math.Min(augment, graph[priorNode[v]][priorEdge[v]].Capacity); for (int v = sink; v != source; v = priorNode[v]) { ResidualEdge edge = graph[priorNode[v]][priorEdge[v]]; edge.Capacity -= augment; graph[v][edge.Reverse].Capacity += augment; result.TotalCost += augment * edge.Cost; } result.DeliveredFlow += augment; }
            foreach (OrArc arc in ActiveList(model.Arcs)) { int from; if (!index.TryGetValue(arc.FromLocationId ?? "", out from)) continue; ResidualEdge edge = graph[from].FirstOrDefault(x => x.Original && Eq(x.ArcId, arc.Id)); if (edge != null && edge.OriginalCapacity - edge.Capacity > Epsilon) result.Flows.Add(new OrFlowRecord { ArcId = arc.Id, FromLocationId = arc.FromLocationId, ToLocationId = arc.ToLocationId, Flow = edge.OriginalCapacity - edge.Capacity, Capacity = edge.OriginalCapacity, UnitCost = edge.Cost }); }
            result.Feasible = Math.Abs(supply - demand) <= 1e-6 && result.DeliveredFlow + 1e-6 >= demand; if (Math.Abs(supply - demand) > 1e-6) result.Audit.Add("WARN · SUPPLY_DEMAND_IMBALANCE · supply " + supply.ToString("R", CultureInfo.InvariantCulture) + " demand " + demand.ToString("R", CultureInfo.InvariantCulture)); result.CertificateHash = HashText(Fingerprint(model) + "|FLOW|" + result.DeliveredFlow.ToString("R", CultureInfo.InvariantCulture) + "|" + result.TotalCost.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join(";", result.Flows.Select(x => x.ArcId + ":" + x.Flow.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return result;
        }

        public static OrFacilityResult SolveFacilityLocation(OperationsResearchModel model, int openLimit)
        {
            OrFacilityResult result = new OrFacilityResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrFacilityCandidate> candidates = ActiveList(model.Facilities), mandatory = candidates.Where(x => x.Mandatory).ToList(); openLimit = Math.Max(mandatory.Count, Math.Min(candidates.Count, openLimit)); if (candidates.Count == 0 || openLimit == 0) { result.Audit.Add("WARN · NO_FACILITY_CANDIDATES"); return result; } List<OrFacilityCandidate> open = new List<OrFacilityCandidate>(mandatory); while (open.Count < openLimit) { OrFacilityCandidate best = candidates.Where(x => !open.Contains(x)).OrderBy(x => EvaluateFacilities(model, open.Concat(new[] { x }).ToList()).Fixed + EvaluateFacilities(model, open.Concat(new[] { x }).ToList()).Transport + EvaluateFacilities(model, open.Concat(new[] { x }).ToList()).Unserved * P(model, "unserved_penalty", 10000)).ThenBy(x => x.Id).FirstOrDefault(); if (best == null) break; open.Add(best); }
            bool improved = true; for (int iteration = 0; iteration < 64 && improved; iteration++) { improved = false; FacilityEvaluation current = EvaluateFacilities(model, open); double currentLoss = current.Fixed + current.Transport + current.Unserved * P(model, "unserved_penalty", 10000); foreach (OrFacilityCandidate remove in open.Where(x => !x.Mandatory).ToList()) foreach (OrFacilityCandidate add in candidates.Where(x => !open.Contains(x)).ToList()) { List<OrFacilityCandidate> trial = open.Where(x => x != remove).Concat(new[] { add }).ToList(); FacilityEvaluation e = EvaluateFacilities(model, trial); double loss = e.Fixed + e.Transport + e.Unserved * P(model, "unserved_penalty", 10000); if (loss + Epsilon < currentLoss) { open = trial; improved = true; currentLoss = loss; break; } } }
            FacilityEvaluation final = EvaluateFacilities(model, open); result.OpenFacilityIds = open.Select(x => x.Id).OrderBy(x => x).ToList(); result.Assignments = final.Assignments; result.FixedCost = final.Fixed; result.TransportCost = final.Transport; result.UnservedDemand = final.Unserved; result.MeanServiceDistance = final.MeanDistance; result.CertificateHash = HashText(Fingerprint(model) + "|FACILITY|" + openLimit + "|" + String.Join(",", result.OpenFacilityIds.ToArray()) + "|" + (result.FixedCost + result.TransportCost).ToString("R", CultureInfo.InvariantCulture)); result.Audit.Add("BOUNDARY · greedy-add plus deterministic one-swap local search is capacity-aware but does not certify global facility-location optimality"); return result;
        }

        public static OrSlottingResult SolveWarehouseSlotting(OperationsResearchModel model)
        {
            OrSlottingResult result = new OrSlottingResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrInventoryItem> items = ActiveList(model.Inventory).OrderByDescending(x => x.PickFrequency / Math.Max(Epsilon, x.Cube)).ThenBy(x => x.Id).ToList(); List<OrWarehouseSlot> slots = ActiveList(model.WarehouseSlots).OrderBy(x => x.TravelDistance).ThenBy(x => x.Id).ToList(); int n = Math.Max(items.Count, slots.Count); if (n == 0) return result; double[,] cost = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { if (i >= items.Count || j >= slots.Count) cost[i, j] = 0; else { bool compatible = slots[j].CubeCapacity + Epsilon >= items[i].Cube && (String.IsNullOrWhiteSpace(items[i].RequiredZone) || Eq(items[i].RequiredZone, slots[j].Zone)); cost[i, j] = items[i].PickFrequency * slots[j].TravelDistance + slots[j].ErgonomicPenalty + (compatible ? 0 : Forbidden); } } int[] assignment = Hungarian(cost, n); HashSet<int> used = new HashSet<int>(); for (int i = 0; i < items.Count; i++) { int j = assignment[i]; if (j < 0 || j >= slots.Count) { result.UnslottedItemIds.Add(items[i].Id); continue; } bool compatible = cost[i, j] < Forbidden; OrSlotAssignment record = new OrSlotAssignment { ItemId = items[i].Id, SlotId = slots[j].Id, AnnualTravelBurden = compatible ? cost[i, j] : items[i].PickFrequency * slots[j].TravelDistance + slots[j].ErgonomicPenalty, Compatible = compatible }; result.Assignments.Add(record); result.TotalTravelBurden += record.AnnualTravelBurden; if (!compatible) result.CompatibilityViolations++; used.Add(j); } result.CertificateHash = HashText(Fingerprint(model) + "|SLOTTING|" + String.Join(";", result.Assignments.Select(x => x.ItemId + ":" + x.SlotId).ToArray())); result.Audit.Add("BOUNDARY · Hungarian slotting is exact for the declared one-item/one-slot cost matrix; replenishment congestion and multi-slot SKUs are outside this model"); return result;
        }

        public static OrStaffingResult SolveStaffing(OperationsResearchModel model)
        {
            OrStaffingResult result = new OrStaffingResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrShiftRequirement> requirements = ActiveList(model.ShiftRequirements).GroupBy(x => StaffKey(x.ShiftId, x.Skill), StringComparer.OrdinalIgnoreCase).Select(g => new OrShiftRequirement { Id = String.Join("+", g.Select(x => x.Id).ToArray()), ShiftId = g.First().ShiftId, Skill = g.First().Skill, RequiredWorkers = g.Sum(x => Math.Max(0, x.RequiredWorkers)), UnderstaffPenalty = g.Max(x => x.UnderstaffPenalty), Status = "ACTIVE" }).ToList(); List<OrWorker> workers = ActiveList(model.Workers); Dictionary<string, OrShiftRequirement> requirementByKey = requirements.ToDictionary(x => StaffKey(x.ShiftId, x.Skill), StringComparer.OrdinalIgnoreCase); foreach (OrShiftRequirement q in requirements) result.RemainingDeficits[StaffKey(q.ShiftId, q.Skill)] = Math.Max(0, q.RequiredWorkers); Dictionary<string, int> loads = workers.ToDictionary(x => x.Id, x => 0, StringComparer.OrdinalIgnoreCase); HashSet<string> usedPair = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (result.RemainingDeficits.Values.Any(x => x > 0)) { OrWorker bestWorker = null; OrShiftRequirement bestRequirement = null; double bestScore = 0; foreach (OrWorker worker in workers.Where(x => loads[x.Id] < Math.Max(1, x.MaximumAssignments))) foreach (OrShiftRequirement q in requirements) { string key = StaffKey(q.ShiftId, q.Skill), pair = worker.Id + "|" + q.ShiftId; if (result.RemainingDeficits[key] <= 0 || usedPair.Contains(pair) || (worker.EligibleShiftIds.Count > 0 && !worker.EligibleShiftIds.Contains(q.ShiftId, StringComparer.OrdinalIgnoreCase)) || !worker.Skills.Contains(q.Skill, StringComparer.OrdinalIgnoreCase)) continue; double score = q.UnderstaffPenalty / Math.Max(Epsilon, worker.CostPerAssignment); if (score > bestScore + Epsilon || (Math.Abs(score - bestScore) <= Epsilon && String.CompareOrdinal(worker.Id, bestWorker == null ? "" : bestWorker.Id) < 0)) { bestScore = score; bestWorker = worker; bestRequirement = q; } } if (bestWorker == null) break; string bestKey = StaffKey(bestRequirement.ShiftId, bestRequirement.Skill); result.Assignments.Add(new OrStaffAssignment { WorkerId = bestWorker.Id, ShiftId = bestRequirement.ShiftId, Skill = bestRequirement.Skill, Cost = bestWorker.CostPerAssignment }); result.LaborCost += bestWorker.CostPerAssignment; result.RemainingDeficits[bestKey]--; loads[bestWorker.Id]++; usedPair.Add(bestWorker.Id + "|" + bestRequirement.ShiftId); }
            foreach (KeyValuePair<string, int> deficit in result.RemainingDeficits) if (deficit.Value > 0) result.UnderstaffPenalty += deficit.Value * requirementByKey[deficit.Key].UnderstaffPenalty; result.CertificateHash = HashText(Fingerprint(model) + "|STAFFING|" + String.Join(";", result.Assignments.Select(x => x.WorkerId + ":" + x.ShiftId + ":" + x.Skill).ToArray()) + "|DEFICIT=" + String.Join(";", result.RemainingDeficits.Select(x => x.Key + ":" + x.Value).ToArray())); result.Audit.Add("BOUNDARY · deterministic coverage-benefit scheduling respects declared skills, eligibility and assignment caps; rest law and preferences require explicit constraints"); return result;
        }

        public static OrInventoryResult SolveInventory(OperationsResearchModel model)
        {
            OrInventoryResult result = new OrInventoryResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; double demandMultiplier = P(model, "demand_multiplier", 1); foreach (OrInventoryItem item in ActiveList(model.Inventory)) { double annual = Math.Max(0, item.AnnualDemand * demandMultiplier), mean = Math.Max(0, item.DailyDemandMean * demandMultiplier), sigma = Math.Max(0, item.DailyDemandStdDev * demandMultiplier), lead = Math.Max(0, item.LeadTimeDays), eoq = item.AnnualHoldingCost <= Epsilon ? annual : Math.Sqrt(Math.Max(0, 2 * annual * Math.Max(0, item.OrderCost) / item.AnnualHoldingCost)), z = InverseNormal(Clamp(item.ServiceLevel, 1e-6, 1 - 1e-6)), safety = z * sigma * Math.Sqrt(lead), reorder = mean * lead + safety, critical = item.ShortageCost <= Epsilon && item.AnnualHoldingCost <= Epsilon ? 0.5 : Math.Max(0, item.ShortageCost) / Math.Max(Epsilon, Math.Max(0, item.ShortageCost) + Math.Max(0, item.AnnualHoldingCost)), newsvendor = mean * lead + InverseNormal(Clamp(critical, 1e-6, 1 - 1e-6)) * sigma * Math.Sqrt(lead), annualCost = (eoq <= Epsilon ? 0 : annual / eoq * Math.Max(0, item.OrderCost)) + eoq * 0.5 * Math.Max(0, item.AnnualHoldingCost) + Math.Max(0, safety) * Math.Max(0, item.AnnualHoldingCost); OrInventoryPolicy policy = new OrInventoryPolicy { ItemId = item.Id, EconomicOrderQuantity = eoq, SafetyStock = safety, ReorderPoint = reorder, NewsvendorQuantity = Math.Max(0, newsvendor), ExpectedAnnualRelevantCost = annualCost }; result.Policies.Add(policy); result.AggregateAnnualRelevantCost += annualCost; } result.CertificateHash = HashText(Fingerprint(model) + "|INVENTORY|" + String.Join(";", result.Policies.Select(x => x.ItemId + ":" + x.EconomicOrderQuantity.ToString("R", CultureInfo.InvariantCulture) + ":" + x.ReorderPoint.ToString("R", CultureInfo.InvariantCulture)).ToArray())); result.Audit.Add("BOUNDARY · EOQ, normal-demand safety stock and newsvendor quantities are analytical policies under declared stationary assumptions, not purchase orders"); return result;
        }

        public static OrPortfolioResult SolvePortfolio(OperationsResearchModel model)
        {
            OrPortfolioResult result = new OrPortfolioResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrPortfolioItem> items = ActiveList(model.Portfolio); double riskWeight = P(model, "risk_weight", 1), budget = Math.Max(0, model.Budget); List<OrPortfolioItem> mandatory = items.Where(x => x.Mandatory).ToList(); result.SelectedItemIds.AddRange(mandatory.Select(x => x.Id)); result.TotalCost = mandatory.Sum(x => x.Cost); result.TotalValue = mandatory.Sum(x => x.Value); result.TotalRisk = mandatory.Sum(x => x.Risk); double remainingBudget = budget - result.TotalCost; if (remainingBudget < -Epsilon) { result.Audit.Add("BLOCKER · MANDATORY_PORTFOLIO_EXCEEDS_BUDGET"); result.BudgetSlack = remainingBudget; return result; }
            List<OrPortfolioItem> optional = items.Where(x => !x.Mandatory && x.Cost >= 0).Take(512).ToList(); int cap = 20000, scale = remainingBudget <= Epsilon ? 1 : Math.Max(1, (int)Math.Ceiling(remainingBudget / cap)); int capacity = Math.Max(0, Math.Min(cap, (int)Math.Floor(remainingBudget / scale))); double[] dp = Enumerable.Repeat(Double.NegativeInfinity, capacity + 1).ToArray(); List<string>[] picks = Enumerable.Range(0, capacity + 1).Select(x => new List<string>()).ToArray(); dp[0] = 0;
            foreach (OrPortfolioItem item in optional) { int weight = Math.Max(0, (int)Math.Ceiling(item.Cost / scale)); double objective = item.Value - riskWeight * item.Risk; for (int b = capacity; b >= weight; b--) if (!Double.IsNegativeInfinity(dp[b - weight]) && dp[b - weight] + objective > dp[b] + Epsilon) { dp[b] = dp[b - weight] + objective; picks[b] = new List<string>(picks[b - weight]); picks[b].Add(item.Id); } }
            int best = Enumerable.Range(0, capacity + 1).OrderByDescending(x => dp[x]).ThenBy(x => x).First(); HashSet<string> selected = new HashSet<string>(result.SelectedItemIds.Concat(picks[best]), StringComparer.OrdinalIgnoreCase); foreach (IGrouping<string, OrPortfolioItem> group in items.Where(x => selected.Contains(x.Id) && !String.IsNullOrWhiteSpace(x.ExclusiveGroup)).GroupBy(x => x.ExclusiveGroup, StringComparer.OrdinalIgnoreCase)) foreach (OrPortfolioItem remove in group.OrderByDescending(x => x.Value - riskWeight * x.Risk).Skip(1)) selected.Remove(remove.Id);
            bool changed = true; for (int pass = 0; pass < items.Count && changed; pass++) { changed = false; foreach (OrPortfolioItem item in items.Where(x => selected.Contains(x.Id))) foreach (string required in item.RequiredItemIds) if (!selected.Contains(required) && items.Any(x => Eq(x.Id, required))) { selected.Add(required); changed = true; } }
            List<OrPortfolioItem> final = items.Where(x => selected.Contains(x.Id)).ToList(); result.SelectedItemIds = final.Select(x => x.Id).OrderBy(x => x).ToList(); result.TotalCost = final.Sum(x => x.Cost); result.TotalValue = final.Sum(x => x.Value); result.TotalRisk = final.Sum(x => x.Risk); result.Objective = result.TotalValue - riskWeight * result.TotalRisk; result.BudgetSlack = budget - result.TotalCost; result.Feasible = result.BudgetSlack >= -Epsilon && final.Where(x => !String.IsNullOrWhiteSpace(x.ExclusiveGroup)).GroupBy(x => x.ExclusiveGroup, StringComparer.OrdinalIgnoreCase).All(x => x.Count() <= 1) && final.All(x => x.RequiredItemIds.All(selected.Contains)); result.CertificateHash = HashText(Fingerprint(model) + "|PORTFOLIO|" + budget.ToString("R", CultureInfo.InvariantCulture) + "|" + String.Join(",", result.SelectedItemIds.ToArray())); result.Audit.Add("BOUNDARY · scaled dynamic programming is exact on its integerized budget grid; dependency repair and exclusivity can make the final portfolio conservative rather than globally optimal"); return result;
        }

        public static OrRobustnessResult StressScenarios(OperationsResearchModel model)
        {
            OrRobustnessResult result = new OrRobustnessResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<OrScenario> scenarios = ActiveList(model.Scenarios); if (scenarios.Count == 0) scenarios.Add(new OrScenario { Id = "BASE", Name = "Declared baseline" }); Dictionary<string, double> original = new Dictionary<string, double>(model.Parameters, StringComparer.OrdinalIgnoreCase);
            try { foreach (OrScenario scenario in scenarios) { model.Parameters["demand_multiplier"] = scenario.DemandMultiplier; model.Parameters["travel_cost_multiplier"] = scenario.TravelCostMultiplier; model.Parameters["capacity_multiplier"] = scenario.CapacityMultiplier; model.Parameters["duration_multiplier"] = scenario.DurationMultiplier; OrRoutingResult route = SolveVehicleRouting(model); OrScheduleResult schedule = SolveSchedule(model); OrFlowResult flow = SolveMinCostFlow(model); double loss = route.TotalCost + route.TotalLatePenalty + route.UnservedShipmentIds.Count * P(model, "unserved_penalty", 10000) + schedule.WeightedTardiness + schedule.UnscheduledTaskIds.Count * P(model, "unscheduled_penalty", 10000) + Math.Max(0, flow.RequiredFlow - flow.DeliveredFlow) * P(model, "flow_deficit_penalty", 10000); result.Scores.Add(new OrScenarioScore { ScenarioId = scenario.Id, ProbabilityWeight = Math.Max(0, scenario.ProbabilityWeight), RoutingCost = route.TotalCost + route.TotalLatePenalty, UnservedShipments = route.UnservedShipmentIds.Count, Makespan = schedule.Makespan, UnscheduledTasks = schedule.UnscheduledTaskIds.Count, FlowDeficit = Math.Max(0, flow.RequiredFlow - flow.DeliveredFlow), CompositeLoss = loss }); } } finally { model.Parameters = original; }
            double weights = result.Scores.Sum(x => x.ProbabilityWeight); result.WeightedMeanLoss = weights <= Epsilon ? Mean(result.Scores.Select(x => x.CompositeLoss)) : result.Scores.Sum(x => x.CompositeLoss * x.ProbabilityWeight) / weights; OrScenarioScore worst = result.Scores.OrderByDescending(x => x.CompositeLoss).FirstOrDefault(); if (worst != null) { result.WorstLoss = worst.CompositeLoss; result.WorstScenarioId = worst.ScenarioId; } result.LossSpread = result.Scores.Count == 0 ? 0 : result.Scores.Max(x => x.CompositeLoss) - result.Scores.Min(x => x.CompositeLoss); result.CertificateHash = HashText(Fingerprint(model) + "|STRESS|" + String.Join(";", result.Scores.Select(x => x.ScenarioId + ":" + x.CompositeLoss.ToString("R", CultureInfo.InvariantCulture)).ToArray())); result.Audit.Add("BOUNDARY · each stress world is reoptimized under declared multipliers; this is a recourse envelope, not fixed-plan out-of-sample performance or a calibrated forecast"); return result;
        }

        public static OrTradeSpaceResult ExploreFacilityTradeSpace(OperationsResearchModel model)
        {
            OrTradeSpaceResult result = new OrTradeSpaceResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; int count = ActiveList(model.Facilities).Count; for (int k = 1; k <= Math.Min(count, 32); k++) { OrFacilityResult solution = SolveFacilityLocation(model, k); result.Points.Add(new OrTradePoint { OpenFacilities = k, TotalCost = solution.FixedCost + solution.TransportCost, MeanServiceDistance = solution.MeanServiceDistance, UnservedDemand = solution.UnservedDemand }); }
            foreach (OrTradePoint p in result.Points) p.ParetoEfficient = !result.Points.Any(q => q != p && q.TotalCost <= p.TotalCost + Epsilon && q.MeanServiceDistance <= p.MeanServiceDistance + Epsilon && q.UnservedDemand <= p.UnservedDemand + Epsilon && (q.TotalCost < p.TotalCost - Epsilon || q.MeanServiceDistance < p.MeanServiceDistance - Epsilon || q.UnservedDemand < p.UnservedDemand - Epsilon)); List<OrTradePoint> efficient = result.Points.Where(x => x.ParetoEfficient).OrderBy(x => x.OpenFacilities).ToList(); if (efficient.Count > 0) { double minCost = efficient.Min(x => x.TotalCost), maxCost = efficient.Max(x => x.TotalCost), minDistance = efficient.Min(x => x.MeanServiceDistance), maxDistance = efficient.Max(x => x.MeanServiceDistance); OrTradePoint knee = efficient.OrderBy(x => Sq(Normalize(x.TotalCost, minCost, maxCost)) + Sq(Normalize(x.MeanServiceDistance, minDistance, maxDistance)) + (x.UnservedDemand > Epsilon ? 1 : 0)).First(); result.KneeDescription = knee.OpenFacilities + " facilities · cost " + knee.TotalCost.ToString("0.###", CultureInfo.InvariantCulture) + " · mean distance " + knee.MeanServiceDistance.ToString("0.###", CultureInfo.InvariantCulture); } result.CertificateHash = HashText(Fingerprint(model) + "|TRADE|" + String.Join(";", result.Points.Select(x => x.OpenFacilities + ":" + x.TotalCost.ToString("R", CultureInfo.InvariantCulture) + ":" + x.MeanServiceDistance.ToString("R", CultureInfo.InvariantCulture)).ToArray())); return result;
        }

        public static OrSensitivityResult AnalyzeSensitivity(OperationsResearchModel model, double relativeStep)
        {
            OrSensitivityResult result = new OrSensitivityResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; relativeStep = Clamp(relativeStep, 0.001, 0.5); string[] keys = { "demand_multiplier", "travel_cost_multiplier", "capacity_multiplier", "duration_multiplier", "risk_weight" }; Dictionary<string, double> original = new Dictionary<string, double>(model.Parameters, StringComparer.OrdinalIgnoreCase); double baseline = CompositeLoss(SolveAll(model)); try { foreach (string key in keys) { double value = P(model, key, 1), delta = Math.Max(1e-6, Math.Abs(value) * relativeStep); model.Parameters[key] = value - delta; double minus = CompositeLoss(SolveAll(model)); model.Parameters[key] = value + delta; double plus = CompositeLoss(SolveAll(model)); model.Parameters[key] = value; double effect = (plus - minus) / (2 * delta), elasticity = Math.Abs(baseline) <= Epsilon ? 0 : effect * value / baseline; result.Effects.Add(new OrSensitivityEffect { ParameterKey = key, BaselineValue = value, MinusLoss = minus, PlusLoss = plus, ElementaryEffect = effect, Elasticity = elasticity }); } } finally { model.Parameters = original; } OrSensitivityEffect dominant = result.Effects.OrderByDescending(x => Math.Abs(x.Elasticity)).FirstOrDefault(); result.DominantParameter = dominant == null ? null : dominant.ParameterKey; double min = result.Effects.Select(x => Math.Abs(x.ElementaryEffect)).Where(x => x > Epsilon).DefaultIfEmpty(0).Min(), max = result.Effects.Select(x => Math.Abs(x.ElementaryEffect)).DefaultIfEmpty(0).Max(); result.ConditionProxy = min <= Epsilon ? (max <= Epsilon ? 0 : Double.MaxValue) : max / min; result.CertificateHash = HashText(Fingerprint(model) + "|SENSITIVITY|" + String.Join(";", result.Effects.Select(x => x.ParameterKey + ":" + x.ElementaryEffect.ToString("R", CultureInfo.InvariantCulture)).ToArray())); result.Audit.Add("BOUNDARY · local one-at-a-time sensitivity does not expose higher-order interactions or prove stability outside the perturbation neighborhood"); return result;
        }

        public static OrSolveBundle SolveAll(OperationsResearchModel model)
        {
            OrSolveBundle result = new OrSolveBundle(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; result.ModelFingerprint = Fingerprint(model); result.Routing = SolveVehicleRouting(model); result.Schedule = SolveSchedule(model); result.Assignment = SolveAssignment(model); result.Flow = SolveMinCostFlow(model); result.Facilities = SolveFacilityLocation(model, model.FacilityLimit); result.Slotting = SolveWarehouseSlotting(model); result.Staffing = SolveStaffing(model); result.Inventory = SolveInventory(model); result.Portfolio = SolvePortfolio(model); result.CompositeLoss = CompositeLoss(result); result.CertificateHash = HashText(result.ModelFingerprint + "|BUNDLE|" + result.CompositeLoss.ToString("R", CultureInfo.InvariantCulture) + "|" + (result.Routing == null ? "" : result.Routing.CertificateHash) + "|" + (result.Schedule == null ? "" : result.Schedule.CertificateHash) + "|" + (result.Flow == null ? "" : result.Flow.CertificateHash) + "|" + (result.Facilities == null ? "" : result.Facilities.CertificateHash) + "|" + (result.Portfolio == null ? "" : result.Portfolio.CertificateHash)); result.Audit.Add("BOUNDARY · a composite loss is an operator-declared comparison coordinate, not an autonomous decision or authorization to dispatch, staff, purchase, schedule or allocate"); return result;
        }

        public static List<string> Audit(OperationsResearchModel model)
        {
            List<string> findings = new List<string>();
            if (model == null) { findings.Add("BLOCKER · NULL_OPERATIONS_MODEL"); return findings; }
            List<OrLocation> locationRows = model.Locations ?? new List<OrLocation>(); List<OrArc> arcs = model.Arcs ?? new List<OrArc>(); List<OrShipment> shipments = model.Shipments ?? new List<OrShipment>(); List<OrVehicle> vehicles = model.Vehicles ?? new List<OrVehicle>(); List<OrTask> tasks = model.Tasks ?? new List<OrTask>(); List<OrResourcePool> resources = model.Resources ?? new List<OrResourcePool>(); List<OrWorker> workers = model.Workers ?? new List<OrWorker>(); List<OrShiftRequirement> shifts = model.ShiftRequirements ?? new List<OrShiftRequirement>(); List<OrFacilityCandidate> facilities = model.Facilities ?? new List<OrFacilityCandidate>(); List<OrInventoryItem> inventory = model.Inventory ?? new List<OrInventoryItem>(); List<OrWarehouseSlot> slots = model.WarehouseSlots ?? new List<OrWarehouseSlot>(); List<OrPortfolioItem> portfolio = model.Portfolio ?? new List<OrPortfolioItem>(); List<OrScenario> scenarios = model.Scenarios ?? new List<OrScenario>(); Dictionary<string, double> parameters = model.Parameters ?? new Dictionary<string, double>();
            if (model.Locations == null || model.Arcs == null || model.Shipments == null || model.Vehicles == null || model.Tasks == null || model.Resources == null || model.Workers == null || model.ShiftRequirements == null || model.Facilities == null || model.Inventory == null || model.WarehouseSlots == null || model.Portfolio == null || model.Scenarios == null || model.Parameters == null) findings.Add("BLOCKER · NULL_MODEL_COLLECTION");
            if (String.IsNullOrWhiteSpace(model.Id)) findings.Add("WARN · MISSING_MODEL_ID"); if (String.IsNullOrWhiteSpace(model.Name)) findings.Add("BLOCKER · MISSING_MODEL_NAME"); if (!Finite(model.Budget) || model.Budget < 0 || model.FacilityLimit < 0) findings.Add("BLOCKER · MODEL_NUMERIC_DOMAIN"); if (locationRows.Count > MaximumNodes) findings.Add("BLOCKER · LOCATION_CAPACITY"); if (arcs.Count > MaximumArcs) findings.Add("BLOCKER · ARC_CAPACITY"); if (new[] { shipments.Count, vehicles.Count, tasks.Count, resources.Count, workers.Count, shifts.Count, facilities.Count, inventory.Count, slots.Count, portfolio.Count, scenarios.Count }.Any(x => x > MaximumItems)) findings.Add("BLOCKER · ITEM_CAPACITY");
            Missing(findings, locationRows.Where(x => x != null).Select(x => x.Id), "LOCATION"); Missing(findings, arcs.Where(x => x != null).Select(x => x.Id), "ARC"); Missing(findings, shipments.Where(x => x != null).Select(x => x.Id), "SHIPMENT"); Missing(findings, vehicles.Where(x => x != null).Select(x => x.Id), "VEHICLE"); Missing(findings, tasks.Where(x => x != null).Select(x => x.Id), "TASK"); Missing(findings, resources.Where(x => x != null).Select(x => x.Id), "RESOURCE"); Missing(findings, workers.Where(x => x != null).Select(x => x.Id), "WORKER"); Missing(findings, shifts.Where(x => x != null).Select(x => x.Id), "SHIFT_REQUIREMENT"); Missing(findings, facilities.Where(x => x != null).Select(x => x.Id), "FACILITY"); Missing(findings, inventory.Where(x => x != null).Select(x => x.Id), "INVENTORY"); Missing(findings, slots.Where(x => x != null).Select(x => x.Id), "SLOT"); Missing(findings, portfolio.Where(x => x != null).Select(x => x.Id), "PORTFOLIO"); Missing(findings, scenarios.Where(x => x != null).Select(x => x.Id), "SCENARIO");
            Duplicate(findings, locationRows.Where(x => x != null).Select(x => x.Id), "LOCATION"); Duplicate(findings, arcs.Where(x => x != null).Select(x => x.Id), "ARC"); Duplicate(findings, shipments.Where(x => x != null).Select(x => x.Id), "SHIPMENT"); Duplicate(findings, vehicles.Where(x => x != null).Select(x => x.Id), "VEHICLE"); Duplicate(findings, tasks.Where(x => x != null).Select(x => x.Id), "TASK"); Duplicate(findings, resources.Where(x => x != null).Select(x => x.Id), "RESOURCE"); Duplicate(findings, workers.Where(x => x != null).Select(x => x.Id), "WORKER"); Duplicate(findings, shifts.Where(x => x != null).Select(x => x.Id), "SHIFT_REQUIREMENT"); Duplicate(findings, facilities.Where(x => x != null).Select(x => x.Id), "FACILITY"); Duplicate(findings, inventory.Where(x => x != null).Select(x => x.Id), "INVENTORY"); Duplicate(findings, slots.Where(x => x != null).Select(x => x.Id), "SLOT"); Duplicate(findings, portfolio.Where(x => x != null).Select(x => x.Id), "PORTFOLIO"); Duplicate(findings, scenarios.Where(x => x != null).Select(x => x.Id), "SCENARIO");
            HashSet<string> locations = new HashSet<string>(locationRows.Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (OrLocation x in locationRows.Where(x => x != null && Active(x.Status))) if (!Finite(x.X) || !Finite(x.Y) || !Finite(x.NetSupply) || !Finite(x.DemandWeight) || x.DemandWeight < 0) findings.Add("BLOCKER · LOCATION_NUMERIC_DOMAIN · " + (x.Id ?? "∅"));
            foreach (OrArc x in arcs.Where(x => x != null && Active(x.Status))) { if (!locations.Contains(x.FromLocationId ?? "") || !locations.Contains(x.ToLocationId ?? "")) findings.Add("BLOCKER · ARC_ENDPOINT_UNKNOWN · " + (x.Id ?? "∅")); if (!Finite(x.Distance) || !Finite(x.TravelTime) || !Finite(x.UnitCost) || !Finite(x.Capacity) || x.Distance < 0 || x.TravelTime < 0 || x.UnitCost < 0 || x.Capacity < 0) findings.Add("BLOCKER · ARC_NUMERIC_DOMAIN · " + (x.Id ?? "∅")); }
            HashSet<string> depots = new HashSet<string>(vehicles.Where(x => x != null && Active(x.Status)).Select(x => x.DepotLocationId), StringComparer.OrdinalIgnoreCase); foreach (OrShipment x in shipments.Where(x => x != null && Active(x.Status))) { if (!locations.Contains(x.OriginLocationId ?? "") || !locations.Contains(x.DestinationLocationId ?? "")) findings.Add("BLOCKER · SHIPMENT_ENDPOINT_UNKNOWN · " + (x.Id ?? "∅")); if (!Finite(x.Quantity) || !Finite(x.ServiceTime) || !Finite(x.ReadyTime) || !Finite(x.DueTime) || !Finite(x.Priority) || !Finite(x.LatePenalty) || x.Quantity < 0 || x.ServiceTime < 0 || x.DueTime < x.ReadyTime || x.Priority < 0 || x.LatePenalty < 0) findings.Add("BLOCKER · SHIPMENT_DOMAIN · " + (x.Id ?? "∅")); if (depots.Count > 0 && !depots.Contains(x.OriginLocationId ?? "")) findings.Add("WARN · SHIPMENT_ORIGIN_HAS_NO_COMPATIBLE_DEPOT · " + (x.Id ?? "∅")); }
            foreach (OrVehicle x in vehicles.Where(x => x != null && Active(x.Status))) if (!locations.Contains(x.DepotLocationId ?? "") || !Finite(x.Capacity) || !Finite(x.FixedCost) || !Finite(x.CostPerDistance) || !Finite(x.Speed) || !Finite(x.AvailableFrom) || !Finite(x.AvailableUntil) || !Finite(x.MaximumRouteDistance) || x.Capacity <= 0 || x.FixedCost < 0 || x.CostPerDistance < 0 || x.Speed <= 0 || x.AvailableUntil < x.AvailableFrom || x.MaximumRouteDistance < 0 || x.MaximumStops < 1) findings.Add("BLOCKER · VEHICLE_DOMAIN · " + (x.Id ?? "∅"));
            List<OrTask> activeTasks = tasks.Where(x => x != null && Active(x.Status)).ToList(); HashSet<string> taskIds = new HashSet<string>(activeTasks.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (OrTask x in activeTasks) { if (!Finite(x.Duration) || !Finite(x.ReleaseTime) || !Finite(x.DueTime) || !Finite(x.Priority) || !Finite(x.LatePenalty) || x.Duration <= 0 || x.ReleaseTime < 0 || x.DueTime < x.ReleaseTime || x.Priority < 0 || x.LatePenalty < 0 || x.ResourceUnits < 1 || (x.PredecessorIds ?? new List<string>()).Any(p => !taskIds.Contains(p) || Eq(p, x.Id)) || (!String.IsNullOrWhiteSpace(x.LocationId) && !locations.Contains(x.LocationId))) findings.Add("BLOCKER · TASK_DOMAIN_OR_PREDECESSOR · " + (x.Id ?? "∅")); } if (TaskCycle(activeTasks)) findings.Add("BLOCKER · TASK_PRECEDENCE_CYCLE");
            HashSet<string> resourceKinds = new HashSet<string>(resources.Where(x => x != null && Active(x.Status)).Select(x => x.Kind), StringComparer.OrdinalIgnoreCase); foreach (OrResourcePool x in resources.Where(x => x != null && Active(x.Status))) if (x.Capacity < 1 || !Finite(x.AvailableFrom) || !Finite(x.AvailableUntil) || !Finite(x.UnitTimeCost) || x.AvailableUntil < x.AvailableFrom || x.UnitTimeCost < 0) findings.Add("BLOCKER · RESOURCE_DOMAIN · " + (x.Id ?? "∅")); foreach (OrTask x in activeTasks) if (!resourceKinds.Contains(x.ResourceKind ?? "GENERAL")) findings.Add("WARN · TASK_RESOURCE_KIND_UNAVAILABLE · " + (x.Id ?? "∅") + " · " + (x.ResourceKind ?? "GENERAL"));
            foreach (OrWorker x in workers.Where(x => x != null && Active(x.Status))) if (!Finite(x.CostPerAssignment) || x.CostPerAssignment < 0 || x.MaximumAssignments < 1 || (!String.IsNullOrWhiteSpace(x.HomeLocationId) && !locations.Contains(x.HomeLocationId))) findings.Add("BLOCKER · WORKER_DOMAIN · " + (x.Id ?? "∅")); foreach (OrShiftRequirement x in shifts.Where(x => x != null && Active(x.Status))) if (String.IsNullOrWhiteSpace(x.ShiftId) || String.IsNullOrWhiteSpace(x.Skill) || x.RequiredWorkers < 0 || !Finite(x.UnderstaffPenalty) || x.UnderstaffPenalty < 0) findings.Add("BLOCKER · SHIFT_REQUIREMENT_DOMAIN · " + (x.Id ?? "∅"));
            foreach (OrFacilityCandidate x in facilities.Where(x => x != null && Active(x.Status))) if (!locations.Contains(x.LocationId ?? "") || !Finite(x.FixedCost) || !Finite(x.Capacity) || !Finite(x.HandlingCost) || x.FixedCost < 0 || x.Capacity < 0 || x.HandlingCost < 0) findings.Add("BLOCKER · FACILITY_DOMAIN · " + (x.Id ?? "∅")); foreach (OrWarehouseSlot x in slots.Where(x => x != null && Active(x.Status))) if (!Finite(x.TravelDistance) || !Finite(x.CubeCapacity) || !Finite(x.ErgonomicPenalty) || x.TravelDistance < 0 || x.CubeCapacity < 0 || x.ErgonomicPenalty < 0) findings.Add("BLOCKER · WAREHOUSE_SLOT_DOMAIN · " + (x.Id ?? "∅"));
            foreach (OrInventoryItem x in inventory.Where(x => x != null && Active(x.Status))) if (!Finite(x.AnnualDemand) || !Finite(x.DailyDemandMean) || !Finite(x.DailyDemandStdDev) || !Finite(x.OrderCost) || !Finite(x.AnnualHoldingCost) || !Finite(x.LeadTimeDays) || !Finite(x.ServiceLevel) || !Finite(x.ShortageCost) || !Finite(x.UnitCost) || !Finite(x.PickFrequency) || !Finite(x.Cube) || x.AnnualDemand < 0 || x.DailyDemandMean < 0 || x.DailyDemandStdDev < 0 || x.OrderCost < 0 || x.AnnualHoldingCost < 0 || x.LeadTimeDays < 0 || x.ServiceLevel <= 0 || x.ServiceLevel >= 1 || x.ShortageCost < 0 || x.UnitCost < 0 || x.PickFrequency < 0 || x.Cube < 0) findings.Add("BLOCKER · INVENTORY_DOMAIN · " + (x.Id ?? "∅"));
            List<OrPortfolioItem> activePortfolio = portfolio.Where(x => x != null && Active(x.Status)).ToList(); HashSet<string> optionIds = new HashSet<string>(activePortfolio.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); foreach (OrPortfolioItem x in activePortfolio) if (!Finite(x.Cost) || !Finite(x.Value) || !Finite(x.Risk) || x.Cost < 0 || x.Risk < 0 || (x.RequiredItemIds ?? new List<string>()).Any(id => !optionIds.Contains(id) || Eq(id, x.Id))) findings.Add("BLOCKER · PORTFOLIO_DOMAIN_OR_DEPENDENCY · " + (x.Id ?? "∅"));
            foreach (OrScenario x in scenarios.Where(x => x != null && Active(x.Status))) if (!Finite(x.DemandMultiplier) || !Finite(x.TravelCostMultiplier) || !Finite(x.CapacityMultiplier) || !Finite(x.DurationMultiplier) || !Finite(x.ProbabilityWeight) || x.DemandMultiplier < 0 || x.TravelCostMultiplier < 0 || x.CapacityMultiplier <= 0 || x.DurationMultiplier <= 0 || x.ProbabilityWeight < 0) findings.Add("BLOCKER · SCENARIO_DOMAIN · " + (x.Id ?? "∅")); foreach (KeyValuePair<string, double> x in parameters) if (String.IsNullOrWhiteSpace(x.Key) || !Finite(x.Value)) findings.Add("BLOCKER · PARAMETER_DOMAIN · " + (x.Key ?? "∅"));
            foreach (string key in new[] { "demand_multiplier", "travel_cost_multiplier", "risk_weight", "unserved_penalty", "unscheduled_penalty", "flow_deficit_penalty" }) if (P(model, key, 1) < 0) findings.Add("BLOCKER · NONNEGATIVE_PARAMETER_REQUIRED · " + key); foreach (string key in new[] { "capacity_multiplier", "duration_multiplier" }) if (P(model, key, 1) <= 0) findings.Add("BLOCKER · POSITIVE_PARAMETER_REQUIRED · " + key);
            if ((model.Assumptions ?? new List<string>()).Count == 0) findings.Add("WARN · NO_EXPLICIT_ASSUMPTIONS"); if ((model.EvidenceNodeIds ?? new List<string>()).Count == 0) findings.Add("INFO · NO_WORLD_EVIDENCE_ANCHORS"); findings.Add("BOUNDARY · optimization results are conditional mathematical artifacts under declared data, constraints, objectives and approximations; they do not execute logistics, staffing, purchasing, dispatch, scheduling, policy or control"); return findings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string Fingerprint(OperationsResearchModel model)
        {
            if (model == null) return HashText("NULL_OPERATIONS_MODEL"); StringBuilder s = new StringBuilder(); s.Append(model.Id).Append('|').Append(model.ParentModelId).Append('|').Append(model.Name).Append('|').Append(model.Description).Append('|').Append(model.Status).Append('|').Append(model.Revision).Append('|').Append(model.SourceKind).Append('|').Append(model.SourceId).Append('|').Append(model.SourceFingerprint).Append('|').Append(model.ObjectiveSense).Append('|').Append(R(model.Budget)).Append('|').Append(model.FacilityLimit).Append('|').Append(model.Seed);
            AppendMap(s, model.Parameters); foreach (OrLocation x in model.Locations.OrderBy(x => x.Id)) { s.Append("|L:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Kind).Append(':').Append(x.Zone).Append(':').Append(R(x.X)).Append(':').Append(R(x.Y)).Append(':').Append(R(x.NetSupply)).Append(':').Append(R(x.DemandWeight)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (OrArc x in model.Arcs.OrderBy(x => x.Id)) { s.Append("|A:").Append(x.Id).Append(':').Append(x.FromLocationId).Append(':').Append(x.ToLocationId).Append(':').Append(R(x.Distance)).Append(':').Append(R(x.TravelTime)).Append(':').Append(R(x.UnitCost)).Append(':').Append(R(x.Capacity)).Append(':').Append(x.Bidirectional).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (OrShipment x in model.Shipments.OrderBy(x => x.Id)) { s.Append("|S:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.OriginLocationId).Append(':').Append(x.DestinationLocationId).Append(':').Append(R(x.Quantity)).Append(':').Append(R(x.ServiceTime)).Append(':').Append(R(x.ReadyTime)).Append(':').Append(R(x.DueTime)).Append(':').Append(R(x.Priority)).Append(':').Append(R(x.LatePenalty)).Append(':').Append(x.RequiredSkill).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (OrVehicle x in model.Vehicles.OrderBy(x => x.Id)) { s.Append("|V:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.DepotLocationId).Append(':').Append(R(x.Capacity)).Append(':').Append(R(x.FixedCost)).Append(':').Append(R(x.CostPerDistance)).Append(':').Append(R(x.Speed)).Append(':').Append(R(x.AvailableFrom)).Append(':').Append(R(x.AvailableUntil)).Append(':').Append(R(x.MaximumRouteDistance)).Append(':').Append(x.MaximumStops).Append(':').Append(x.Status); AppendList(s, x.Skills); AppendList(s, x.EvidenceNodeIds); }
            foreach (OrTask x in model.Tasks.OrderBy(x => x.Id)) { s.Append("|T:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(R(x.Duration)).Append(':').Append(R(x.ReleaseTime)).Append(':').Append(R(x.DueTime)).Append(':').Append(R(x.Priority)).Append(':').Append(R(x.LatePenalty)).Append(':').Append(x.ResourceKind).Append(':').Append(x.ResourceUnits).Append(':').Append(x.LocationId).Append(':').Append(x.Status); AppendList(s, x.PredecessorIds); AppendList(s, x.RequiredSkills); AppendList(s, x.EvidenceNodeIds); } foreach (OrResourcePool x in model.Resources.OrderBy(x => x.Id)) s.Append("|R:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Kind).Append(':').Append(x.Capacity).Append(':').Append(R(x.AvailableFrom)).Append(':').Append(R(x.AvailableUntil)).Append(':').Append(R(x.UnitTimeCost)).Append(':').Append(x.Status); foreach (OrWorker x in model.Workers.OrderBy(x => x.Id)) { s.Append("|W:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.HomeLocationId).Append(':').Append(R(x.CostPerAssignment)).Append(':').Append(x.MaximumAssignments).Append(':').Append(x.Status); AppendList(s, x.Skills); AppendList(s, x.EligibleShiftIds); }
            foreach (OrShiftRequirement x in model.ShiftRequirements.OrderBy(x => x.Id)) s.Append("|Q:").Append(x.Id).Append(':').Append(x.ShiftId).Append(':').Append(x.Skill).Append(':').Append(x.RequiredWorkers).Append(':').Append(R(x.UnderstaffPenalty)).Append(':').Append(x.Status); foreach (OrFacilityCandidate x in model.Facilities.OrderBy(x => x.Id)) s.Append("|F:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.LocationId).Append(':').Append(R(x.FixedCost)).Append(':').Append(R(x.Capacity)).Append(':').Append(R(x.HandlingCost)).Append(':').Append(x.Mandatory).Append(':').Append(x.Status); foreach (OrInventoryItem x in model.Inventory.OrderBy(x => x.Id)) s.Append("|I:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Family).Append(':').Append(x.RequiredZone).Append(':').Append(R(x.AnnualDemand)).Append(':').Append(R(x.DailyDemandMean)).Append(':').Append(R(x.DailyDemandStdDev)).Append(':').Append(R(x.OrderCost)).Append(':').Append(R(x.AnnualHoldingCost)).Append(':').Append(R(x.LeadTimeDays)).Append(':').Append(R(x.ServiceLevel)).Append(':').Append(R(x.ShortageCost)).Append(':').Append(R(x.PickFrequency)).Append(':').Append(R(x.Cube)).Append(':').Append(x.Status);
            foreach (OrWarehouseSlot x in model.WarehouseSlots.OrderBy(x => x.Id)) s.Append("|H:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Zone).Append(':').Append(R(x.TravelDistance)).Append(':').Append(R(x.CubeCapacity)).Append(':').Append(R(x.ErgonomicPenalty)).Append(':').Append(x.Status); foreach (OrPortfolioItem x in model.Portfolio.OrderBy(x => x.Id)) { s.Append("|P:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(R(x.Cost)).Append(':').Append(R(x.Value)).Append(':').Append(R(x.Risk)).Append(':').Append(x.Mandatory).Append(':').Append(x.ExclusiveGroup).Append(':').Append(x.Status); AppendList(s, x.RequiredItemIds); } foreach (OrScenario x in model.Scenarios.OrderBy(x => x.Id)) s.Append("|X:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(R(x.ProbabilityWeight)).Append(':').Append(R(x.DemandMultiplier)).Append(':').Append(R(x.TravelCostMultiplier)).Append(':').Append(R(x.CapacityMultiplier)).Append(':').Append(R(x.DurationMultiplier)).Append(':').Append(x.Status); AppendList(s, model.Assumptions); AppendList(s, model.EvidenceNodeIds); return HashText(s.ToString());
        }

        private static double CompositeLoss(OrSolveBundle x) { if (x == null) return Double.PositiveInfinity; return (x.Routing == null ? 0 : x.Routing.TotalCost + x.Routing.TotalLatePenalty + x.Routing.UnservedShipmentIds.Count * 10000) + (x.Schedule == null ? 0 : x.Schedule.WeightedTardiness + x.Schedule.UnscheduledTaskIds.Count * 10000) + (x.Flow == null ? 0 : x.Flow.TotalCost + Math.Max(0, x.Flow.RequiredFlow - x.Flow.DeliveredFlow) * 10000) + (x.Facilities == null ? 0 : x.Facilities.FixedCost + x.Facilities.TransportCost + x.Facilities.UnservedDemand * 10000) + (x.Slotting == null ? 0 : x.Slotting.TotalTravelBurden + x.Slotting.CompatibilityViolations * 10000) + (x.Staffing == null ? 0 : x.Staffing.LaborCost + x.Staffing.UnderstaffPenalty) + (x.Inventory == null ? 0 : x.Inventory.AggregateAnnualRelevantCost) - (x.Portfolio == null ? 0 : x.Portfolio.Objective); }
        private static FacilityEvaluation EvaluateFacilities(OperationsResearchModel model, List<OrFacilityCandidate> open) { FacilityEvaluation result = new FacilityEvaluation(); result.Fixed = open.Sum(x => x.FixedCost); Dictionary<string, double> remaining = open.ToDictionary(x => x.Id, x => x.Capacity * P(model, "capacity_multiplier", 1), StringComparer.OrdinalIgnoreCase); Dictionary<string, Dictionary<string, double>> distances = AllPairs(model, "DISTANCE"); List<OrShipment> demand = ActiveList(model.Shipments); double totalDistanceQuantity = 0, served = 0, multiplier = P(model, "demand_multiplier", 1), costMultiplier = P(model, "travel_cost_multiplier", 1); foreach (OrShipment shipment in demand.OrderByDescending(x => x.Priority).ThenBy(x => x.Id)) { double left = shipment.Quantity * multiplier; foreach (OrFacilityCandidate facility in open.OrderBy(x => D(distances, x.LocationId, shipment.DestinationLocationId)).ThenBy(x => x.Id)) { if (left <= Epsilon) break; double distance = D(distances, facility.LocationId, shipment.DestinationLocationId); if (!Finite(distance) || remaining[facility.Id] <= Epsilon) continue; double quantity = Math.Min(left, remaining[facility.Id]), cost = quantity * (distance * costMultiplier + facility.HandlingCost); result.Assignments.Add(new OrFacilityAssignment { DemandLocationId = shipment.DestinationLocationId, FacilityId = facility.Id, Quantity = quantity, Distance = distance, Cost = cost }); result.Transport += cost; remaining[facility.Id] -= quantity; left -= quantity; served += quantity; totalDistanceQuantity += quantity * distance; } result.Unserved += Math.Max(0, left); } result.MeanDistance = served <= Epsilon ? 0 : totalDistanceQuantity / served; return result; }
        private static void EvaluateRoute(OrVehicleRoute route, OrVehicle vehicle, Dictionary<string, OrShipment> shipments, Dictionary<string, Dictionary<string, double>> distances, Dictionary<string, Dictionary<string, double>> times, double demandMultiplier, double capacityMultiplier, double costMultiplier) { double time = vehicle.AvailableFrom, distance = 0, load = 0; string at = vehicle.DepotLocationId; route.LatePenalty = 0; foreach (OrRouteStop stop in route.Stops) { OrShipment shipment = shipments[stop.ShipmentId]; double leg = D(distances, at, stop.LocationId), duration = D(times, at, stop.LocationId); if (!Finite(leg) || !Finite(duration)) { route.Feasible = false; continue; } distance += leg; time += duration / Math.Max(Epsilon, vehicle.Speed); time = Math.Max(time, shipment.ReadyTime); stop.ArrivalTime = time; stop.Lateness = Math.Max(0, time - shipment.DueTime); route.LatePenalty += stop.Lateness * shipment.LatePenalty * shipment.Priority; time += shipment.ServiceTime; stop.DepartureTime = time; load += shipment.Quantity * demandMultiplier; stop.LoadAfter = load; at = stop.LocationId; } double back = D(distances, at, vehicle.DepotLocationId), backTime = D(times, at, vehicle.DepotLocationId); if (Finite(back)) distance += back; else route.Feasible = false; if (Finite(backTime)) time += backTime / Math.Max(Epsilon, vehicle.Speed); else route.Feasible = false; route.Distance = distance; route.Duration = time - vehicle.AvailableFrom; route.Load = load; route.Cost = vehicle.FixedCost + distance * vehicle.CostPerDistance * costMultiplier; if (distance > vehicle.MaximumRouteDistance + Epsilon || time > vehicle.AvailableUntil + Epsilon || load > vehicle.Capacity * capacityMultiplier + Epsilon) route.Feasible = false; }
        private static void TwoOpt(OrVehicleRoute route, OrVehicle vehicle, Dictionary<string, Dictionary<string, double>> distance) { if (route.Stops.Count < 4) return; bool improved = true; for (int pass = 0; pass < 64 && improved; pass++) { improved = false; for (int i = 0; i < route.Stops.Count - 2; i++) for (int k = i + 1; k < route.Stops.Count - 1; k++) { string a = i == 0 ? vehicle.DepotLocationId : route.Stops[i - 1].LocationId, b = route.Stops[i].LocationId, c = route.Stops[k].LocationId, d = k + 1 >= route.Stops.Count ? vehicle.DepotLocationId : route.Stops[k + 1].LocationId; double before = D(distance, a, b) + D(distance, c, d), after = D(distance, a, c) + D(distance, b, d); if (after + Epsilon < before) { route.Stops.Reverse(i, k - i + 1); improved = true; } } } }
        private static Dictionary<string, Dictionary<string, double>> AllPairs(OperationsResearchModel model, string metric) { List<OrLocation> nodes = ActiveList(model.Locations); Dictionary<string, int> index = Index(nodes.Select(x => x.Id)); List<MetricEdge>[] graph = MetricGraph(model, nodes, index); Dictionary<string, Dictionary<string, double>> result = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase); for (int source = 0; source < nodes.Count; source++) { double[] d = Enumerable.Repeat(Double.PositiveInfinity, nodes.Count).ToArray(); bool[] used = new bool[nodes.Count]; d[source] = 0; for (int iteration = 0; iteration < nodes.Count; iteration++) { int u = -1; for (int i = 0; i < nodes.Count; i++) if (!used[i] && (u < 0 || d[i] < d[u])) u = i; if (u < 0 || Double.IsInfinity(d[u])) break; used[u] = true; foreach (MetricEdge e in graph[u]) { double w = Eq(metric, "TIME") ? e.Time : (Eq(metric, "COST") ? e.Cost : e.Distance); if (w >= 0 && d[u] + w + Epsilon < d[e.To]) d[e.To] = d[u] + w; } } result[nodes[source].Id] = nodes.Select((x, i) => new { x.Id, Value = d[i] }).ToDictionary(x => x.Id, x => x.Value, StringComparer.OrdinalIgnoreCase); } return result; }
        private static List<MetricEdge>[] MetricGraph(OperationsResearchModel model, List<OrLocation> nodes, Dictionary<string, int> index) { List<MetricEdge>[] graph = Enumerable.Range(0, nodes.Count).Select(x => new List<MetricEdge>()).ToArray(); List<OrArc> arcs = ActiveList(model.Arcs); if (arcs.Count == 0) { for (int i = 0; i < nodes.Count; i++) for (int j = i + 1; j < nodes.Count; j++) { double d = Math.Sqrt(Sq(nodes[i].X - nodes[j].X) + Sq(nodes[i].Y - nodes[j].Y)); graph[i].Add(new MetricEdge { To = j, Distance = d, Time = d, Cost = d }); graph[j].Add(new MetricEdge { To = i, Distance = d, Time = d, Cost = d }); } return graph; } foreach (OrArc arc in arcs) { int from, to; if (!index.TryGetValue(arc.FromLocationId ?? "", out from) || !index.TryGetValue(arc.ToLocationId ?? "", out to)) continue; graph[from].Add(new MetricEdge { To = to, Distance = arc.Distance, Time = arc.TravelTime, Cost = arc.UnitCost }); if (arc.Bidirectional) graph[to].Add(new MetricEdge { To = from, Distance = arc.Distance, Time = arc.TravelTime, Cost = arc.UnitCost }); } return graph; }
        private static double PathMetric(List<MetricEdge>[] graph, List<int> path, string metric) { double total = 0; for (int i = 1; i < path.Count; i++) { MetricEdge edge = graph[path[i - 1]].Where(x => x.To == path[i]).OrderBy(x => Eq(metric, "TIME") ? x.Time : (Eq(metric, "COST") ? x.Cost : x.Distance)).FirstOrDefault(); if (edge == null) return Double.PositiveInfinity; total += Eq(metric, "TIME") ? edge.Time : (Eq(metric, "COST") ? edge.Cost : edge.Distance); } return total; }
        private static int[] Hungarian(double[,] a, int n) { double[] u = new double[n + 1], v = new double[n + 1]; int[] p = new int[n + 1], way = new int[n + 1]; for (int i = 1; i <= n; i++) { p[0] = i; int j0 = 0; double[] minv = Enumerable.Repeat(Double.PositiveInfinity, n + 1).ToArray(); bool[] used = new bool[n + 1]; do { used[j0] = true; int i0 = p[j0], j1 = 0; double delta = Double.PositiveInfinity; for (int j = 1; j <= n; j++) if (!used[j]) { double cur = a[i0 - 1, j - 1] - u[i0] - v[j]; if (cur < minv[j]) { minv[j] = cur; way[j] = j0; } if (minv[j] < delta) { delta = minv[j]; j1 = j; } } for (int j = 0; j <= n; j++) if (used[j]) { u[p[j]] += delta; v[j] -= delta; } else minv[j] -= delta; j0 = j1; } while (p[j0] != 0); do { int j1 = way[j0]; p[j0] = p[j1]; j0 = j1; } while (j0 != 0); } int[] answer = Enumerable.Repeat(-1, n).ToArray(); for (int j = 1; j <= n; j++) if (p[j] > 0) answer[p[j] - 1] = j - 1; return answer; }
        private static void AddResidual(List<ResidualEdge>[] graph, int from, int to, double capacity, double cost, string id) { ResidualEdge forward = new ResidualEdge { To = to, Reverse = graph[to].Count, Capacity = capacity, OriginalCapacity = capacity, Cost = cost, ArcId = id, Original = true }, reverse = new ResidualEdge { To = from, Reverse = graph[from].Count, Capacity = 0, OriginalCapacity = 0, Cost = -cost, ArcId = id, Original = false }; graph[from].Add(forward); graph[to].Add(reverse); }
        private static double CriticalRank(OrTask task, Dictionary<string, OrTask> byId, Dictionary<string, List<string>> successors, Dictionary<string, double> memo) { return CriticalRankInner(task, byId, successors, memo, new HashSet<string>(StringComparer.OrdinalIgnoreCase)); }
        private static double CriticalRankInner(OrTask task, Dictionary<string, OrTask> byId, Dictionary<string, List<string>> successors, Dictionary<string, double> memo, HashSet<string> visiting) { double cached; if (memo.TryGetValue(task.Id, out cached)) return cached; if (!visiting.Add(task.Id)) return 0; double tail = successors[task.Id].Where(byId.ContainsKey).Select(x => CriticalRankInner(byId[x], byId, successors, memo, visiting)).DefaultIfEmpty(0).Max(); visiting.Remove(task.Id); double value = task.Duration + tail; memo[task.Id] = value; return value; }
        private static double InverseNormal(double p) { p = Clamp(p, 1e-12, 1 - 1e-12); double[] a = { -39.6968302866538, 220.946098424521, -275.928510446969, 138.357751867269, -30.6647980661472, 2.50662827745924 }, b = { -54.4760987982241, 161.585836858041, -155.698979859887, 66.8013118877197, -13.2806815528857 }, c = { -0.00778489400243029, -0.322396458041136, -2.40075827716184, -2.54973253934373, 4.37466414146497, 2.93816398269878 }, d = { 0.00778469570904146, 0.32246712907004, 2.445134137143, 3.75440866190742 }; double q, r; if (p < 0.02425) { q = Math.Sqrt(-2 * Math.Log(p)); return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1); } if (p > 1 - 0.02425) { q = Math.Sqrt(-2 * Math.Log(1 - p)); return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1); } q = p - 0.5; r = q * q; return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q / (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1); }
        private static Dictionary<string, int> Index(IEnumerable<string> ids) { Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); int i = 0; foreach (string id in ids) if (!String.IsNullOrWhiteSpace(id) && !result.ContainsKey(id)) result[id] = i++; return result; }
        private static double D(Dictionary<string, Dictionary<string, double>> matrix, string from, string to) { Dictionary<string, double> row; double value; return matrix != null && matrix.TryGetValue(from ?? "", out row) && row.TryGetValue(to ?? "", out value) ? value : Double.PositiveInfinity; }
        private static double P(OperationsResearchModel model, string key, double fallback) { double value; return model != null && model.Parameters != null && model.Parameters.TryGetValue(key, out value) && Finite(value) ? value : fallback; }
        private static List<T> ActiveList<T>(IEnumerable<T> source) where T : class { return (source ?? Enumerable.Empty<T>()).Where(x => x != null && Active(Status(x))).ToList(); }
        private static string Status(object value) { if (value == null) return "ARCHIVED"; System.Reflection.PropertyInfo property = value.GetType().GetProperty("Status"); return property == null ? "ACTIVE" : (property.GetValue(value, null) as string ?? "ACTIVE"); }
        private static bool Active(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETIRED") && !Eq(status, "REJECTED"); }
        private static string StaffKey(string shift, string skill) { return (shift ?? "SHIFT") + "|" + (skill ?? "GENERAL"); }
        private static bool Blocked(IEnumerable<string> audit) { return (audit ?? Enumerable.Empty<string>()).Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase)); }
        private static void Missing(List<string> findings, IEnumerable<string> ids, string kind) { if ((ids ?? Enumerable.Empty<string>()).Any(String.IsNullOrWhiteSpace)) findings.Add("BLOCKER · MISSING_" + kind + "_ID"); }
        private static void Duplicate(List<string> findings, IEnumerable<string> ids, string kind) { foreach (IGrouping<string, string> group in (ids ?? Enumerable.Empty<string>()).Where(x => !String.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.OrdinalIgnoreCase)) if (group.Count() > 1) findings.Add("BLOCKER · DUPLICATE_" + kind + "_ID · " + group.Key); }
        private static bool TaskCycle(List<OrTask> tasks) { List<OrTask> valid = (tasks ?? new List<OrTask>()).Where(x => x != null && !String.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList(); Dictionary<string, int> indegree = valid.ToDictionary(x => x.Id, x => 0, StringComparer.OrdinalIgnoreCase); Dictionary<string, List<string>> next = indegree.Keys.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase); foreach (OrTask task in valid) foreach (string predecessor in task.PredecessorIds ?? new List<string>()) if (indegree.ContainsKey(predecessor) && !Eq(predecessor, task.Id)) { indegree[task.Id]++; next[predecessor].Add(task.Id); } Queue<string> ready = new Queue<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key)); int seen = 0; while (ready.Count > 0) { string id = ready.Dequeue(); seen++; foreach (string child in next[id]) if (--indegree[child] == 0) ready.Enqueue(child); } return seen != indegree.Count; }
        private static void AppendMap(StringBuilder s, IDictionary<string, double> values) { foreach (KeyValuePair<string, double> x in (values ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append("|M:").Append(x.Key).Append(':').Append(R(x.Value)); }
        private static void AppendList(StringBuilder s, IEnumerable<string> values) { foreach (string x in (values ?? Enumerable.Empty<string>()).OrderBy(x => x)) s.Append(":E:").Append(x); }
        private static double Normalize(double value, double minimum, double maximum) { return maximum - minimum <= Epsilon ? 0 : Clamp((value - minimum) / (maximum - minimum), 0, 1); }
        private static double Mean(IEnumerable<double> values) { List<double> list = (values ?? Enumerable.Empty<double>()).Where(Finite).ToList(); return list.Count == 0 ? 0 : list.Average(); }
        private static double Sq(double x) { return x * x; }
        private static double Clamp(double value, double minimum, double maximum) { return Math.Max(minimum, Math.Min(maximum, Finite(value) ? value : minimum)); }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static string R(double x) { return x.ToString("R", CultureInfo.InvariantCulture); }
        private static string HashText(string text) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

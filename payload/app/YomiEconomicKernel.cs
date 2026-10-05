using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yomi.ProductShell
{
    internal sealed class EcoRegion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public double ExchangeRateToNumeraire { get; set; }
        public double ImportTariffRate { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoRegion() { Currency = "UNIT"; ExchangeRateToNumeraire = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "REGION") + "   ·   " + (Currency ?? "UNIT") + " @ " + ExchangeRateToNumeraire.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   tariff " + ImportTariffRate.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoGood
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Kind { get; set; }
        public double ReferencePrice { get; set; }
        public double ConsumptionWeight { get; set; }
        public double DepreciationRate { get; set; }
        public bool Essential { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoGood() { Unit = "unit"; Kind = "FINAL"; ReferencePrice = 1; ConsumptionWeight = 1; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "GOOD") + "   ·   " + (Kind ?? "FINAL") + "   ·   reference " + ReferencePrice.ToString("0.####", CultureInfo.InvariantCulture) + " / " + (Unit ?? "unit") + (Essential ? "   ·   ESSENTIAL" : ""); }
    }

    internal sealed class EcoHousehold
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegionId { get; set; }
        public double PopulationWeight { get; set; }
        public double Cash { get; set; }
        public double Deposits { get; set; }
        public double OtherWealth { get; set; }
        public double Debt { get; set; }
        public double AvailableHours { get; set; }
        public double ReservationWage { get; set; }
        public double Productivity { get; set; }
        public double PropensityToConsume { get; set; }
        public double EssentialBudgetShare { get; set; }
        public string Skill { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoHousehold() { PopulationWeight = 1; AvailableHours = 40; Productivity = 1; PropensityToConsume = 0.75; EssentialBudgetShare = 0.65; Skill = "GENERAL"; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "HOUSEHOLD") + "   ·   pop×" + PopulationWeight.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   cash " + Cash.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   debt " + Debt.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   " + (Skill ?? "GENERAL"); }
    }

    internal sealed class EcoFirm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegionId { get; set; }
        public string BankId { get; set; }
        public double Cash { get; set; }
        public double Equity { get; set; }
        public double Debt { get; set; }
        public double CapitalStock { get; set; }
        public double CapacityBatches { get; set; }
        public double Productivity { get; set; }
        public double OfferedWage { get; set; }
        public double Markup { get; set; }
        public double TargetInventoryPeriods { get; set; }
        public string RequiredSkill { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> Inventory { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoFirm() { CapacityBatches = 10; Productivity = 1; OfferedWage = 15; Markup = 0.2; TargetInventoryPeriods = 2; RequiredSkill = "GENERAL"; Status = "ACTIVE"; Inventory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "FIRM") + "   ·   cash/equity/debt " + Cash.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Equity.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Debt.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   wage " + OfferedWage.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   μ " + Markup.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoProductionRecipe
    {
        public string Id { get; set; }
        public string FirmId { get; set; }
        public string OutputGoodId { get; set; }
        public double OutputPerBatch { get; set; }
        public double LaborHoursPerBatch { get; set; }
        public double CapitalUsePerBatch { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> InputsPerBatch { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoProductionRecipe() { OutputPerBatch = 1; LaborHoursPerBatch = 1; Status = "ACTIVE"; InputsPerBatch = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (FirmId ?? "FIRM") + "   ·   " + String.Join(" + ", InputsPerBatch.Select(x => x.Value.ToString("0.###", CultureInfo.InvariantCulture) + " " + x.Key).ToArray()) + " + " + LaborHoursPerBatch.ToString("0.###", CultureInfo.InvariantCulture) + "h → " + OutputPerBatch.ToString("0.###", CultureInfo.InvariantCulture) + " " + (OutputGoodId ?? "OUTPUT"); }
    }

    internal sealed class EcoBank
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegionId { get; set; }
        public double Reserves { get; set; }
        public double Deposits { get; set; }
        public double Equity { get; set; }
        public double BaseInterestRate { get; set; }
        public double MinimumCapitalRatio { get; set; }
        public double MinimumReserveRatio { get; set; }
        public double MaximumBorrowerLeverage { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoBank() { BaseInterestRate = 0.04; MinimumCapitalRatio = 0.1; MinimumReserveRatio = 0.05; MaximumBorrowerLeverage = 4; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "BANK") + "   ·   reserves/deposits/equity " + Reserves.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Deposits.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Equity.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   base " + BaseInterestRate.ToString("P2", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoGovernment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegionId { get; set; }
        public double Cash { get; set; }
        public double Debt { get; set; }
        public double IncomeTaxRate { get; set; }
        public double SalesTaxRate { get; set; }
        public double CorporateTaxRate { get; set; }
        public double PayrollTaxRate { get; set; }
        public double TransferPerHousehold { get; set; }
        public double ProcurementBudget { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoGovernment() { IncomeTaxRate = 0.15; SalesTaxRate = 0.05; CorporateTaxRate = 0.18; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "GOVERNMENT") + "   ·   cash/debt " + Cash.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Debt.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   income/sales/corporate " + IncomeTaxRate.ToString("P1", CultureInfo.InvariantCulture) + "/" + SalesTaxRate.ToString("P1", CultureInfo.InvariantCulture) + "/" + CorporateTaxRate.ToString("P1", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoContract
    {
        public string Id { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public string GoodId { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public int StartPeriod { get; set; }
        public int DuePeriod { get; set; }
        public double LatePenaltyRate { get; set; }
        public double DeliveredQuantity { get; set; }
        public double PaidAmount { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoContract() { Quantity = 1; UnitPrice = 1; DuePeriod = 1; LatePenaltyRate = 0.01; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (BuyerId ?? "BUYER") + " ⇄ " + (SellerId ?? "SELLER") + "   ·   " + Quantity.ToString("0.###", CultureInfo.InvariantCulture) + " " + (GoodId ?? "GOOD") + " @ " + UnitPrice.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   due t" + DuePeriod.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoLoan
    {
        public string Id { get; set; }
        public string BankId { get; set; }
        public string BorrowerId { get; set; }
        public double OriginalPrincipal { get; set; }
        public double OutstandingPrincipal { get; set; }
        public double AnnualInterestRate { get; set; }
        public int OriginationPeriod { get; set; }
        public int RemainingPeriods { get; set; }
        public int MissedPayments { get; set; }
        public double CollateralValue { get; set; }
        public string Status { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EcoLoan() { RemainingPeriods = 12; Status = "ACTIVE"; EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (BorrowerId ?? "BORROWER") + " ← " + (BankId ?? "BANK") + "   ·   outstanding " + OutstandingPrincipal.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   " + AnnualInterestRate.ToString("P2", CultureInfo.InvariantCulture) + "   ·   " + RemainingPeriods.ToString(CultureInfo.InvariantCulture) + " periods"; }
    }

    internal sealed class EcoScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Weight { get; set; }
        public double HouseholdDemandMultiplier { get; set; }
        public double ProductivityMultiplier { get; set; }
        public double InputCostMultiplier { get; set; }
        public double WageMultiplier { get; set; }
        public double CreditAvailabilityMultiplier { get; set; }
        public double TradeFrictionMultiplier { get; set; }
        public double ExogenousInflationMultiplier { get; set; }
        public string Status { get; set; }
        public List<string> Assumptions { get; set; }
        public EcoScenario() { Weight = 1; HouseholdDemandMultiplier = 1; ProductivityMultiplier = 1; InputCostMultiplier = 1; WageMultiplier = 1; CreditAvailabilityMultiplier = 1; TradeFrictionMultiplier = 1; ExogenousInflationMultiplier = 1; Status = "ACTIVE"; Assumptions = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "SCENARIO") + "   ·   demand/productivity/cost/wage/credit/trade/inflation × " + HouseholdDemandMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + ProductivityMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + InputCostMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + WageMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + CreditAvailabilityMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + TradeFrictionMultiplier.ToString("0.##", CultureInfo.InvariantCulture) + "/" + ExogenousInflationMultiplier.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EconomicModel
    {
        public string Id { get; set; }
        public string ParentModelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Revision { get; set; }
        public int Horizon { get; set; }
        public int Seed { get; set; }
        public string CreatedUtc { get; set; }
        public string UpdatedUtc { get; set; }
        public string SourceKind { get; set; }
        public string SourceId { get; set; }
        public string SourceFingerprint { get; set; }
        public Dictionary<string, double> Parameters { get; set; }
        public List<EcoRegion> Regions { get; set; }
        public List<EcoGood> Goods { get; set; }
        public List<EcoHousehold> Households { get; set; }
        public List<EcoFirm> Firms { get; set; }
        public List<EcoProductionRecipe> Recipes { get; set; }
        public List<EcoBank> Banks { get; set; }
        public List<EcoGovernment> Governments { get; set; }
        public List<EcoContract> Contracts { get; set; }
        public List<EcoLoan> Loans { get; set; }
        public List<EcoScenario> Scenarios { get; set; }
        public List<string> Assumptions { get; set; }
        public List<string> EvidenceNodeIds { get; set; }
        public EconomicModel() { Status = "ACTIVE"; Revision = 1; Horizon = 24; Seed = 1337; Parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); Regions = new List<EcoRegion>(); Goods = new List<EcoGood>(); Households = new List<EcoHousehold>(); Firms = new List<EcoFirm>(); Recipes = new List<EcoProductionRecipe>(); Banks = new List<EcoBank>(); Governments = new List<EcoGovernment>(); Contracts = new List<EcoContract>(); Loans = new List<EcoLoan>(); Scenarios = new List<EcoScenario>(); Assumptions = new List<string>(); EvidenceNodeIds = new List<string>(); }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Name ?? Id ?? "ECONOMY") + "   ·   " + Households.Count.ToString(CultureInfo.InvariantCulture) + " households / " + Firms.Count.ToString(CultureInfo.InvariantCulture) + " firms / " + Goods.Count.ToString(CultureInfo.InvariantCulture) + " goods   ·   T=" + Horizon.ToString(CultureInfo.InvariantCulture) + "   ·   r" + Revision.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoJournalLine
    {
        public string EntityId { get; set; }
        public string Account { get; set; }
        public double Debit { get; set; }
        public double Credit { get; set; }
        public override string ToString() { return (EntityId ?? "ENTITY") + ":" + (Account ?? "ACCOUNT") + "   ·   Dr " + Debit.ToString("0.####", CultureInfo.InvariantCulture) + "   Cr " + Credit.ToString("0.####", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoJournalEntry
    {
        public long Sequence { get; set; }
        public int Period { get; set; }
        public string Event { get; set; }
        public string Memo { get; set; }
        public double Debits { get; set; }
        public double Credits { get; set; }
        public string EntryHash { get; set; }
        public List<EcoJournalLine> Lines { get; set; }
        public EcoJournalEntry() { Lines = new List<EcoJournalLine>(); }
        public override string ToString() { return Sequence.ToString(CultureInfo.InvariantCulture).PadLeft(7) + "   ·   t" + Period.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   " + (Event ?? "ENTRY") + "   ·   " + (Memo ?? "") + "   ·   " + Debits.ToString("0.##", CultureInfo.InvariantCulture) + " = " + Credits.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoTradeRecord
    {
        public int Period { get; set; }
        public string Kind { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public string GoodId { get; set; }
        public string OriginRegionId { get; set; }
        public string DestinationRegionId { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Tax { get; set; }
        public double Tariff { get; set; }
        public double Total { get; set; }
        public override string ToString() { return "t" + Period.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Kind ?? "TRADE") + "   ·   " + (BuyerId ?? "BUYER") + " ← " + (SellerId ?? "SELLER") + "   ·   " + Quantity.ToString("0.###", CultureInfo.InvariantCulture) + " " + (GoodId ?? "GOOD") + " @ " + UnitPrice.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   total " + Total.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoEmploymentRecord
    {
        public int Period { get; set; }
        public string HouseholdId { get; set; }
        public string FirmId { get; set; }
        public double Hours { get; set; }
        public double WageRate { get; set; }
        public double GrossWage { get; set; }
        public double Tax { get; set; }
        public override string ToString() { return "t" + Period.ToString(CultureInfo.InvariantCulture) + "   ·   " + (HouseholdId ?? "HOUSEHOLD") + " → " + (FirmId ?? "FIRM") + "   ·   " + Hours.ToString("0.##", CultureInfo.InvariantCulture) + "h @ " + WageRate.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   gross " + GrossWage.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoProductionRecord
    {
        public int Period { get; set; }
        public string FirmId { get; set; }
        public string GoodId { get; set; }
        public double Batches { get; set; }
        public double Quantity { get; set; }
        public double LaborHours { get; set; }
        public double UnitCostProxy { get; set; }
        public override string ToString() { return "t" + Period.ToString(CultureInfo.InvariantCulture) + "   ·   " + (FirmId ?? "FIRM") + "   ·   " + Quantity.ToString("0.###", CultureInfo.InvariantCulture) + " " + (GoodId ?? "GOOD") + "   ·   " + LaborHours.ToString("0.##", CultureInfo.InvariantCulture) + " labor hours"; }
    }

    internal sealed class EcoDefaultRecord
    {
        public int Period { get; set; }
        public string BorrowerId { get; set; }
        public string BankId { get; set; }
        public string LoanId { get; set; }
        public double Outstanding { get; set; }
        public double Recovery { get; set; }
        public double Loss { get; set; }
        public string Kind { get; set; }
        public override string ToString() { return "t" + Period.ToString(CultureInfo.InvariantCulture) + "   ·   " + (Kind ?? "DEFAULT") + "   ·   " + (BorrowerId ?? "BORROWER") + "   ·   loss " + Loss.ToString("0.##", CultureInfo.InvariantCulture) + " / outstanding " + Outstanding.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoPeriodIndicators
    {
        public int Period { get; set; }
        public double NominalGdp { get; set; }
        public double ProductionValue { get; set; }
        public double HouseholdConsumption { get; set; }
        public double IntermediateTrade { get; set; }
        public double WageIncome { get; set; }
        public double CorporateProfit { get; set; }
        public double Cpi { get; set; }
        public double InflationRate { get; set; }
        public double UnemploymentRate { get; set; }
        public double ShortageRate { get; set; }
        public double WealthGini { get; set; }
        public double IncomeGini { get; set; }
        public double MoneySupply { get; set; }
        public double MoneyVelocity { get; set; }
        public double GovernmentRevenue { get; set; }
        public double GovernmentSpending { get; set; }
        public double GovernmentBalance { get; set; }
        public double PrivateDebt { get; set; }
        public double NonperformingLoanRatio { get; set; }
        public double BankCapitalRatio { get; set; }
        public double MarketHhi { get; set; }
        public double CrossRegionTrade { get; set; }
        public int ActiveFirms { get; set; }
        public int Defaults { get; set; }
        public override string ToString() { return "t" + Period.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "   ·   GDP " + NominalGdp.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   CPI " + Cpi.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   π " + InflationRate.ToString("P2", CultureInfo.InvariantCulture) + "   ·   U " + UnemploymentRate.ToString("P1", CultureInfo.InvariantCulture) + "   ·   shortage " + ShortageRate.ToString("P1", CultureInfo.InvariantCulture) + "   ·   Gini " + WealthGini.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   defaults " + Defaults.ToString(CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoEntitySnapshot
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public double Cash { get; set; }
        public double Debt { get; set; }
        public double Equity { get; set; }
        public double Income { get; set; }
        public double Sales { get; set; }
        public override string ToString() { return (Status ?? "ACTIVE") + "   ·   " + (Kind ?? "ENTITY") + "   ·   " + (Id ?? "∅") + "   ·   cash/debt/equity " + Cash.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Debt.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Equity.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   income/sales " + Income.ToString("0.##", CultureInfo.InvariantCulture) + "/" + Sales.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoBoundedTradeLedger : List<EcoTradeRecord>
    {
        public new void Add(EcoTradeRecord item) { if (item != null && Count < 131072) base.Add(item); }
    }

    internal sealed class EcoSimulationResult
    {
        public string ModelFingerprint { get; set; }
        public string ScenarioId { get; set; }
        public int CompletedPeriods { get; set; }
        public string CertificateHash { get; set; }
        public List<EcoPeriodIndicators> Periods { get; set; }
        public EcoBoundedTradeLedger Trades { get; set; }
        public List<EcoEmploymentRecord> Employment { get; set; }
        public List<EcoProductionRecord> Production { get; set; }
        public List<EcoDefaultRecord> Defaults { get; set; }
        public List<EcoJournalEntry> Journal { get; set; }
        public List<EcoEntitySnapshot> FinalEntities { get; set; }
        public List<string> Audit { get; set; }
        public EcoSimulationResult() { Periods = new List<EcoPeriodIndicators>(); Trades = new EcoBoundedTradeLedger(); Employment = new List<EcoEmploymentRecord>(); Production = new List<EcoProductionRecord>(); Defaults = new List<EcoDefaultRecord>(); Journal = new List<EcoJournalEntry>(); FinalEntities = new List<EcoEntitySnapshot>(); Audit = new List<string>(); }
        public override string ToString() { EcoPeriodIndicators last = Periods.LastOrDefault(); return "ECONOMIC RUN   ·   " + CompletedPeriods.ToString(CultureInfo.InvariantCulture) + " periods   ·   " + (last == null ? "NO INDICATORS" : last.ToString()) + "   ·   cert " + Short(CertificateHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(20, x.Length)); }
    }

    internal sealed class EcoSupplyChainNode
    {
        public string GoodId { get; set; }
        public int UpstreamDependencies { get; set; }
        public int DownstreamUses { get; set; }
        public double TechnicalMultiplier { get; set; }
        public double BottleneckScore { get; set; }
        public bool InCycle { get; set; }
        public override string ToString() { return (GoodId ?? "GOOD") + "   ·   upstream/downstream " + UpstreamDependencies.ToString(CultureInfo.InvariantCulture) + "/" + DownstreamUses.ToString(CultureInfo.InvariantCulture) + "   ·   Leontief " + TechnicalMultiplier.ToString("0.####", CultureInfo.InvariantCulture) + "   ·   bottleneck " + BottleneckScore.ToString("0.####", CultureInfo.InvariantCulture) + (InCycle ? "   ·   CYCLE" : ""); }
    }

    internal sealed class EcoSupplyChainResult
    {
        public List<EcoSupplyChainNode> Nodes { get; set; }
        public List<List<string>> StrongComponents { get; set; }
        public double[,] TechnicalCoefficientMatrix { get; set; }
        public double[,] LeontiefInverse { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public EcoSupplyChainResult() { Nodes = new List<EcoSupplyChainNode>(); StrongComponents = new List<List<string>>(); Audit = new List<string>(); }
        public override string ToString() { return "SUPPLY CHAIN   ·   " + Nodes.Count.ToString(CultureInfo.InvariantCulture) + " goods   ·   " + StrongComponents.Count(x => x.Count > 1).ToString(CultureInfo.InvariantCulture) + " cyclic components   ·   cert " + Short(CertificateHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(20, x.Length)); }
    }

    internal sealed class EcoMarketStructureRecord
    {
        public string GoodId { get; set; }
        public double TotalSales { get; set; }
        public double Hhi { get; set; }
        public double TopFirmShare { get; set; }
        public int ActiveSellers { get; set; }
        public string Classification { get; set; }
        public override string ToString() { return (GoodId ?? "GOOD") + "   ·   sellers " + ActiveSellers.ToString(CultureInfo.InvariantCulture) + "   ·   HHI " + Hhi.ToString("0.0000", CultureInfo.InvariantCulture) + "   ·   top " + TopFirmShare.ToString("P1", CultureInfo.InvariantCulture) + "   ·   " + (Classification ?? "UNCLASSIFIED"); }
    }

    internal sealed class EcoPolicyPoint
    {
        public double ParameterValue { get; set; }
        public double Gdp { get; set; }
        public double Inflation { get; set; }
        public double Unemployment { get; set; }
        public double Shortage { get; set; }
        public double Gini { get; set; }
        public double GovernmentBalance { get; set; }
        public double DefaultLoss { get; set; }
        public bool ParetoEfficient { get; set; }
        public override string ToString() { return (ParetoEfficient ? "PARETO" : "DOMINATED") + "   ·   θ=" + ParameterValue.ToString("0.######", CultureInfo.InvariantCulture) + "   ·   GDP " + Gdp.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   π/U/shortage/Gini " + Inflation.ToString("P1", CultureInfo.InvariantCulture) + "/" + Unemployment.ToString("P1", CultureInfo.InvariantCulture) + "/" + Shortage.ToString("P1", CultureInfo.InvariantCulture) + "/" + Gini.ToString("0.###", CultureInfo.InvariantCulture) + "   ·   fiscal " + GovernmentBalance.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoPolicySweepResult
    {
        public string ParameterKey { get; set; }
        public List<EcoPolicyPoint> Points { get; set; }
        public string KneeDescription { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public EcoPolicySweepResult() { Points = new List<EcoPolicyPoint>(); Audit = new List<string>(); }
        public override string ToString() { return "POLICY SWEEP   ·   " + (ParameterKey ?? "PARAMETER") + "   ·   " + Points.Count.ToString(CultureInfo.InvariantCulture) + " worlds   ·   " + (KneeDescription ?? "NO KNEE") + "   ·   cert " + Short(CertificateHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(20, x.Length)); }
    }

    internal sealed class EcoStressScore
    {
        public string ScenarioId { get; set; }
        public double Weight { get; set; }
        public double TerminalGdp { get; set; }
        public double PeakInflation { get; set; }
        public double PeakUnemployment { get; set; }
        public double PeakShortage { get; set; }
        public double DefaultLoss { get; set; }
        public double WelfareLoss { get; set; }
        public string CertificateHash { get; set; }
        public override string ToString() { return (ScenarioId ?? "SCENARIO") + "   ·   loss " + WelfareLoss.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   GDP " + TerminalGdp.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   peak π/U/shortage " + PeakInflation.ToString("P1", CultureInfo.InvariantCulture) + "/" + PeakUnemployment.ToString("P1", CultureInfo.InvariantCulture) + "/" + PeakShortage.ToString("P1", CultureInfo.InvariantCulture) + "   ·   defaults " + DefaultLoss.ToString("0.##", CultureInfo.InvariantCulture); }
    }

    internal sealed class EcoStressResult
    {
        public List<EcoStressScore> Scores { get; set; }
        public string WorstScenarioId { get; set; }
        public double WorstLoss { get; set; }
        public double WeightedMeanLoss { get; set; }
        public string CertificateHash { get; set; }
        public List<string> Audit { get; set; }
        public EcoStressResult() { Scores = new List<EcoStressScore>(); Audit = new List<string>(); }
        public override string ToString() { return "ECONOMIC STRESS   ·   " + Scores.Count.ToString(CultureInfo.InvariantCulture) + " worlds   ·   worst " + (WorstScenarioId ?? "∅") + " @ " + WorstLoss.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   mean " + WeightedMeanLoss.ToString("0.##", CultureInfo.InvariantCulture) + "   ·   cert " + Short(CertificateHash); }
        private static string Short(string x) { return String.IsNullOrWhiteSpace(x) ? "∅" : x.Substring(0, Math.Min(20, x.Length)); }
    }

    internal static class EconomicKernel
    {
        private const double Epsilon = 1e-9;
        private const int MaximumEntities = 8192;
        private const int MaximumTrades = 131072;
        private const int MaximumJournal = 131072;

        private sealed class HouseholdState { public EcoHousehold Source; public double Cash; public double Debt; public double Income; public double Consumption; public double Hours; public string EmployerId; }
        private sealed class FirmState { public EcoFirm Source; public double Cash; public double Debt; public double Equity; public double Sales; public double Revenue; public double WageCost; public double InputCost; public double Tax; public double LaborHours; public double OfferedWage; public double Markup; public string Status; public Dictionary<string, double> Inventory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); }
        private sealed class BankState { public EcoBank Source; public double Reserves; public double Deposits; public double Equity; public double InterestIncome; public double Losses; public double BaseInterestRate; public double MinimumCapitalRatio; }
        private sealed class GovernmentState { public EcoGovernment Source; public double Cash; public double Debt; public double Revenue; public double Spending; public double IncomeTaxRate; public double SalesTaxRate; public double CorporateTaxRate; public double PayrollTaxRate; public double TransferPerHousehold; }
        private sealed class LoanState { public EcoLoan Source; public double Outstanding; public int Remaining; public int Missed; public string Status; }
        private sealed class ContractState { public EcoContract Source; public double Delivered; public double Paid; public string Status; }
        private sealed class SaleOffer { public FirmState Firm; public string GoodId; public double Price; public double Available; }

        private sealed class Book
        {
            public long Sequence;
            public List<EcoJournalEntry> Entries = new List<EcoJournalEntry>();
            public void Post(int period, string evt, string memo, params EcoJournalLine[] lines)
            {
                List<EcoJournalLine> retained = (lines ?? new EcoJournalLine[0]).Where(x => x != null && (Math.Abs(x.Debit) > Epsilon || Math.Abs(x.Credit) > Epsilon)).ToList(); double debit = retained.Sum(x => x.Debit), credit = retained.Sum(x => x.Credit); if (Math.Abs(debit - credit) > 1e-6) throw new InvalidOperationException("UNBALANCED JOURNAL " + evt + " " + debit.ToString("R", CultureInfo.InvariantCulture) + " != " + credit.ToString("R", CultureInfo.InvariantCulture)); EcoJournalEntry entry = new EcoJournalEntry { Sequence = ++Sequence, Period = period, Event = evt, Memo = memo, Debits = debit, Credits = credit, Lines = retained }; entry.EntryHash = HashText(entry.Sequence + "|" + period + "|" + evt + "|" + memo + "|" + String.Join(";", retained.Select(x => x.EntityId + ":" + x.Account + ":" + R(x.Debit) + ":" + R(x.Credit)).ToArray())); if (Entries.Count < MaximumJournal) Entries.Add(entry);
            }
        }

        public static EcoSimulationResult Simulate(EconomicModel model, EcoScenario scenario, IDictionary<string, double> policyOverrides)
        {
            EcoSimulationResult result = new EcoSimulationResult { ScenarioId = scenario == null ? "BASE" : scenario.Id }; result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; result.ModelFingerprint = Fingerprint(model); EcoScenario world = scenario ?? new EcoScenario { Id = "BASE", Name = "Declared baseline" }; Dictionary<string, double> policy = new Dictionary<string, double>(policyOverrides ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase);
            List<HouseholdState> households = Active(model.Households).Select(x => new HouseholdState { Source = x, Cash = x.Cash + x.Deposits, Debt = x.Debt }).ToList();
            List<FirmState> firms = Active(model.Firms).Select(x => new FirmState { Source = CopyFirm(x), Cash = x.Cash, Debt = x.Debt, Equity = x.Equity, OfferedWage = x.OfferedWage, Markup = x.Markup, Status = x.Status, Inventory = new Dictionary<string, double>(x.Inventory ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase) }).ToList();
            List<BankState> banks = Active(model.Banks).Select(x => new BankState { Source = CopyBank(x), Reserves = x.Reserves, Deposits = x.Deposits, Equity = x.Equity, BaseInterestRate = x.BaseInterestRate, MinimumCapitalRatio = x.MinimumCapitalRatio }).ToList();
            List<GovernmentState> governments = Active(model.Governments).Select(x => new GovernmentState { Source = CopyGovernment(x), Cash = x.Cash, Debt = x.Debt, IncomeTaxRate = x.IncomeTaxRate, SalesTaxRate = x.SalesTaxRate, CorporateTaxRate = x.CorporateTaxRate, PayrollTaxRate = x.PayrollTaxRate, TransferPerHousehold = x.TransferPerHousehold }).ToList();
            List<LoanState> loans = Active(model.Loans).Select(x => new LoanState { Source = x, Outstanding = x.OutstandingPrincipal <= 0 ? x.OriginalPrincipal : x.OutstandingPrincipal, Remaining = Math.Max(1, x.RemainingPeriods), Missed = x.MissedPayments, Status = x.Status }).ToList();
            List<ContractState> contracts = Active(model.Contracts).Select(x => new ContractState { Source = x, Delivered = x.DeliveredQuantity, Paid = x.PaidAmount, Status = x.Status }).ToList();
            Dictionary<string, EcoGood> goods = Active(model.Goods).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, EcoRegion> regions = Active(model.Regions).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, double> prices = goods.Values.ToDictionary(x => x.Id, x => x.ReferencePrice, StringComparer.OrdinalIgnoreCase);
            Book book = new Book(); OpenBooks(book, households, firms, banks, governments, loans);
            double previousCpi = 1; int horizon = Math.Max(1, Math.Min(512, model.Horizon));
            for (int period = 1; period <= horizon; period++)
            {
                foreach (HouseholdState h in households) { h.Income = 0; h.Consumption = 0; h.Hours = 0; h.EmployerId = null; } foreach (FirmState f in firms) { f.Sales = 0; f.Revenue = 0; f.WageCost = 0; f.InputCost = 0; f.Tax = 0; f.LaborHours = 0; } foreach (GovernmentState g in governments) { g.Revenue = 0; g.Spending = 0; } Dictionary<string, double> demand = goods.Keys.ToDictionary(x => x, x => 0.0, StringComparer.OrdinalIgnoreCase), supply = goods.Keys.ToDictionary(x => x, x => 0.0, StringComparer.OrdinalIgnoreCase); int defaultsBefore = result.Defaults.Count;
                ApplyPolicy(policy, banks, governments, firms); IssueWorkingCapital(period, model, world, firms, banks, loans, book); MatchLaborAndPayroll(period, model, world, households, firms, governments, book, result); Produce(period, model, world, firms, prices, supply, book, result); FulfillContracts(period, model, world, firms, governments, contracts, prices, regions, book, result); TradeIntermediate(period, model, world, firms, governments, goods, prices, regions, demand, supply, book, result); TradeHouseholdConsumption(period, model, world, households, firms, governments, goods, prices, regions, demand, supply, book, result); ServiceLoans(period, model, firms, banks, loans, book, result); PayTransfers(period, model, households, governments, book); SettleCorporateTax(period, model, firms, governments, book); ApplyDefaults(period, model, firms, banks, loans, book, result); DepreciateInventories(model, firms, goods); UpdatePrices(model, world, policy, goods, prices, demand, supply);
                EcoPeriodIndicators indicators = Indicators(period, model, households, firms, banks, governments, loans, goods, prices, demand, supply, result, previousCpi, defaultsBefore); result.Periods.Add(indicators); previousCpi = indicators.Cpi; result.CompletedPeriods = period;
            }
            result.Journal = book.Entries; foreach (HouseholdState h in households) result.FinalEntities.Add(new EcoEntitySnapshot { Id = h.Source.Id, Kind = "HOUSEHOLD", Status = h.Source.Status, Cash = h.Cash, Debt = h.Debt, Equity = h.Cash + h.Source.OtherWealth - h.Debt, Income = h.Income }); foreach (FirmState f in firms) result.FinalEntities.Add(new EcoEntitySnapshot { Id = f.Source.Id, Kind = "FIRM", Status = f.Status, Cash = f.Cash, Debt = f.Debt, Equity = f.Equity, Income = f.Revenue - f.WageCost - f.InputCost - f.Tax, Sales = f.Sales }); foreach (BankState b in banks) result.FinalEntities.Add(new EcoEntitySnapshot { Id = b.Source.Id, Kind = "BANK", Status = b.Source.Status, Cash = b.Reserves, Debt = b.Deposits, Equity = b.Equity, Income = b.InterestIncome - b.Losses }); foreach (GovernmentState g in governments) result.FinalEntities.Add(new EcoEntitySnapshot { Id = g.Source.Id, Kind = "GOVERNMENT", Status = g.Source.Status, Cash = g.Cash, Debt = g.Debt, Equity = g.Cash - g.Debt, Income = g.Revenue - g.Spending });
            result.CertificateHash = HashText(result.ModelFingerprint + "|ECONOMY|" + (world.Id ?? "BASE") + "|" + String.Join(";", result.Periods.Select(x => x.Period + ":" + R(x.NominalGdp) + ":" + R(x.Cpi) + ":" + R(x.UnemploymentRate) + ":" + R(x.ShortageRate) + ":" + R(x.WealthGini)).ToArray()) + "|J=" + String.Join(",", result.Journal.Select(x => x.EntryHash).ToArray())); result.Audit.Add("BOUNDARY · the run is a deterministic stock-flow planning world under declared behavioral rules; it is not an empirical forecast, market instruction, financial recommendation, labor authorization or autonomous economic policy"); return result;
        }

        public static EcoSupplyChainResult AnalyzeSupplyChain(EconomicModel model)
        {
            EcoSupplyChainResult result = new EcoSupplyChainResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<EcoGood> goods = Active(model.Goods); Dictionary<string, int> index = goods.Select((x, i) => new { x.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index, StringComparer.OrdinalIgnoreCase); int n = goods.Count; double[,] a = new double[n, n]; Dictionary<string, HashSet<string>> graph = goods.ToDictionary(x => x.Id, x => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase), reverse = goods.ToDictionary(x => x.Id, x => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            foreach (EcoProductionRecipe recipe in Active(model.Recipes)) { int output; if (!index.TryGetValue(recipe.OutputGoodId ?? "", out output)) continue; foreach (KeyValuePair<string, double> input in recipe.InputsPerBatch ?? new Dictionary<string, double>()) { int source; if (!index.TryGetValue(input.Key, out source)) continue; a[source, output] += input.Value / Math.Max(Epsilon, recipe.OutputPerBatch); graph[input.Key].Add(recipe.OutputGoodId); reverse[recipe.OutputGoodId].Add(input.Key); } }
            result.TechnicalCoefficientMatrix = a; double[,] identityMinus = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) identityMinus[i, j] = (i == j ? 1 : 0) - a[i, j]; result.LeontiefInverse = Invert(identityMinus, result.Audit); result.StrongComponents = StrongComponents(graph); HashSet<string> cyclic = new HashSet<string>(result.StrongComponents.Where(x => x.Count > 1).SelectMany(x => x), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < n; i++) { double multiplier = result.LeontiefInverse == null ? Double.PositiveInfinity : Enumerable.Range(0, n).Sum(row => Math.Abs(result.LeontiefInverse[row, i])); double bottleneck = graph[goods[i].Id].Count * (Finite(multiplier) ? multiplier : n * 10); result.Nodes.Add(new EcoSupplyChainNode { GoodId = goods[i].Id, UpstreamDependencies = reverse[goods[i].Id].Count, DownstreamUses = graph[goods[i].Id].Count, TechnicalMultiplier = multiplier, BottleneckScore = bottleneck, InCycle = cyclic.Contains(goods[i].Id) }); }
            result.CertificateHash = HashText(Fingerprint(model) + "|SUPPLY_CHAIN|" + String.Join(";", result.Nodes.OrderBy(x => x.GoodId).Select(x => x.GoodId + ":" + R(x.TechnicalMultiplier) + ":" + R(x.BottleneckScore) + ":" + x.InCycle).ToArray())); result.Audit.Add("BOUNDARY · Leontief multipliers assume fixed technical coefficients and unconstrained linear propagation; they do not encode substitution, prices, inventories, capacity, innovation or calibrated shock probabilities"); return result;
        }

        public static List<EcoMarketStructureRecord> AnalyzeMarketStructure(EconomicModel model, EcoSimulationResult run)
        {
            if (model == null || run == null) return new List<EcoMarketStructureRecord>(); List<EcoMarketStructureRecord> rows = new List<EcoMarketStructureRecord>(); foreach (EcoGood good in Active(model.Goods)) { List<IGrouping<string, EcoTradeRecord>> sellers = run.Trades.Where(x => Eq(x.GoodId, good.Id)).GroupBy(x => x.SellerId, StringComparer.OrdinalIgnoreCase).ToList(); double total = sellers.Sum(x => x.Sum(t => t.Total)); List<double> shares = sellers.Select(x => total <= Epsilon ? 0 : x.Sum(t => t.Total) / total).ToList(); double hhi = shares.Sum(x => x * x), top = shares.DefaultIfEmpty(0).Max(); rows.Add(new EcoMarketStructureRecord { GoodId = good.Id, TotalSales = total, Hhi = hhi, TopFirmShare = top, ActiveSellers = sellers.Count, Classification = sellers.Count == 0 ? "NO MARKET" : hhi >= 0.25 ? "HIGH CONCENTRATION" : hhi >= 0.15 ? "MODERATE CONCENTRATION" : "LOW CONCENTRATION" }); } return rows.OrderByDescending(x => x.Hhi).ThenBy(x => x.GoodId).ToList();
        }

        public static EcoStressResult Stress(EconomicModel model)
        {
            EcoStressResult result = new EcoStressResult(); result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; List<EcoScenario> worlds = Active(model.Scenarios); if (worlds.Count == 0) worlds.Add(new EcoScenario { Id = "BASE", Name = "Declared baseline" }); foreach (EcoScenario scenario in worlds) { EcoSimulationResult run = Simulate(model, scenario, null); EcoPeriodIndicators last = run.Periods.LastOrDefault(); if (last == null) continue; double peakInflation = run.Periods.Max(x => Math.Abs(x.InflationRate)), peakUnemployment = run.Periods.Max(x => x.UnemploymentRate), peakShortage = run.Periods.Max(x => x.ShortageRate), defaultLoss = run.Defaults.Sum(x => x.Loss), loss = peakUnemployment * 10000 + peakShortage * 10000 + peakInflation * 5000 + defaultLoss + Math.Max(0, -last.GovernmentBalance) + Math.Max(0, run.Periods.First().NominalGdp - last.NominalGdp); result.Scores.Add(new EcoStressScore { ScenarioId = scenario.Id, Weight = Math.Max(0, scenario.Weight), TerminalGdp = last.NominalGdp, PeakInflation = peakInflation, PeakUnemployment = peakUnemployment, PeakShortage = peakShortage, DefaultLoss = defaultLoss, WelfareLoss = loss, CertificateHash = run.CertificateHash }); }
            double weights = result.Scores.Sum(x => x.Weight); result.WeightedMeanLoss = weights <= Epsilon ? result.Scores.Select(x => x.WelfareLoss).DefaultIfEmpty(0).Average() : result.Scores.Sum(x => x.WelfareLoss * x.Weight) / weights; EcoStressScore worst = result.Scores.OrderByDescending(x => x.WelfareLoss).FirstOrDefault(); if (worst != null) { result.WorstScenarioId = worst.ScenarioId; result.WorstLoss = worst.WelfareLoss; } result.CertificateHash = HashText(Fingerprint(model) + "|STRESS|" + String.Join(";", result.Scores.Select(x => x.ScenarioId + ":" + R(x.WelfareLoss) + ":" + x.CertificateHash).ToArray())); result.Audit.Add("BOUNDARY · scenario weights are declared coordinates, not calibrated probabilities; each world is an internally generated conditional trajectory rather than a forecast distribution"); return result;
        }

        public static EcoPolicySweepResult SweepPolicy(EconomicModel model, string parameterKey, double minimum, double maximum, int steps)
        {
            EcoPolicySweepResult result = new EcoPolicySweepResult { ParameterKey = parameterKey }; result.Audit.AddRange(Audit(model)); if (model == null || Blocked(result.Audit)) return result; steps = Math.Max(2, Math.Min(32, steps)); if (maximum < minimum) { double t = minimum; minimum = maximum; maximum = t; } for (int i = 0; i < steps; i++) { double value = minimum + (maximum - minimum) * i / Math.Max(1, steps - 1); Dictionary<string, double> policy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); policy[parameterKey ?? ""] = value; EcoSimulationResult run = Simulate(model, null, policy); EcoPeriodIndicators last = run.Periods.LastOrDefault(); if (last == null) continue; result.Points.Add(new EcoPolicyPoint { ParameterValue = value, Gdp = last.NominalGdp, Inflation = Math.Abs(last.InflationRate), Unemployment = last.UnemploymentRate, Shortage = last.ShortageRate, Gini = last.WealthGini, GovernmentBalance = last.GovernmentBalance, DefaultLoss = run.Defaults.Sum(x => x.Loss) }); }
            foreach (EcoPolicyPoint p in result.Points) p.ParetoEfficient = !result.Points.Any(q => q != p && q.Gdp >= p.Gdp - Epsilon && q.Unemployment <= p.Unemployment + Epsilon && q.Shortage <= p.Shortage + Epsilon && q.Gini <= p.Gini + Epsilon && q.DefaultLoss <= p.DefaultLoss + Epsilon && (q.Gdp > p.Gdp + Epsilon || q.Unemployment < p.Unemployment - Epsilon || q.Shortage < p.Shortage - Epsilon || q.Gini < p.Gini - Epsilon || q.DefaultLoss < p.DefaultLoss - Epsilon)); List<EcoPolicyPoint> efficient = result.Points.Where(x => x.ParetoEfficient).ToList(); if (efficient.Count > 0) { double minGdp = efficient.Min(x => x.Gdp), maxGdp = efficient.Max(x => x.Gdp), minU = efficient.Min(x => x.Unemployment), maxU = efficient.Max(x => x.Unemployment), minG = efficient.Min(x => x.Gini), maxG = efficient.Max(x => x.Gini); EcoPolicyPoint knee = efficient.OrderBy(x => Sq(1 - Normalize(x.Gdp, minGdp, maxGdp)) + Sq(Normalize(x.Unemployment, minU, maxU)) + Sq(Normalize(x.Gini, minG, maxG))).First(); result.KneeDescription = (parameterKey ?? "parameter") + "=" + knee.ParameterValue.ToString("0.######", CultureInfo.InvariantCulture) + " · GDP " + knee.Gdp.ToString("0.##", CultureInfo.InvariantCulture) + " · U " + knee.Unemployment.ToString("P1", CultureInfo.InvariantCulture) + " · Gini " + knee.Gini.ToString("0.###", CultureInfo.InvariantCulture); } result.CertificateHash = HashText(Fingerprint(model) + "|POLICY_SWEEP|" + parameterKey + "|" + String.Join(";", result.Points.Select(x => R(x.ParameterValue) + ":" + R(x.Gdp) + ":" + R(x.Unemployment) + ":" + R(x.Gini) + ":" + x.ParetoEfficient).ToArray())); result.Audit.Add("BOUNDARY · the sweep compares internal rule worlds and does not identify causal policy effects, political feasibility, legal validity, distributional consent or real-world implementation authority"); return result;
        }

        public static List<string> Audit(EconomicModel model)
        {
            List<string> f = new List<string>(); if (model == null) { f.Add("BLOCKER · NULL_ECONOMIC_MODEL"); return f; } List<EcoRegion> regions = model.Regions ?? new List<EcoRegion>(); List<EcoGood> goods = model.Goods ?? new List<EcoGood>(); List<EcoHousehold> households = model.Households ?? new List<EcoHousehold>(); List<EcoFirm> firms = model.Firms ?? new List<EcoFirm>(); List<EcoProductionRecipe> recipes = model.Recipes ?? new List<EcoProductionRecipe>(); List<EcoBank> banks = model.Banks ?? new List<EcoBank>(); List<EcoGovernment> governments = model.Governments ?? new List<EcoGovernment>(); List<EcoContract> contracts = model.Contracts ?? new List<EcoContract>(); List<EcoLoan> loans = model.Loans ?? new List<EcoLoan>(); List<EcoScenario> scenarios = model.Scenarios ?? new List<EcoScenario>(); if (String.IsNullOrWhiteSpace(model.Name)) f.Add("BLOCKER · MISSING_MODEL_NAME"); if (model.Horizon < 1 || model.Horizon > 512) f.Add("BLOCKER · HORIZON_DOMAIN"); if (new[] { regions.Count, goods.Count, households.Count, firms.Count, recipes.Count, banks.Count, governments.Count, contracts.Count, loans.Count, scenarios.Count }.Any(x => x > MaximumEntities)) f.Add("BLOCKER · MODEL_CAPACITY");
            MissingAndDuplicate(f, regions.Where(x => x != null).Select(x => x.Id), "REGION"); MissingAndDuplicate(f, goods.Where(x => x != null).Select(x => x.Id), "GOOD"); MissingAndDuplicate(f, households.Where(x => x != null).Select(x => x.Id), "HOUSEHOLD"); MissingAndDuplicate(f, firms.Where(x => x != null).Select(x => x.Id), "FIRM"); MissingAndDuplicate(f, recipes.Where(x => x != null).Select(x => x.Id), "RECIPE"); MissingAndDuplicate(f, banks.Where(x => x != null).Select(x => x.Id), "BANK"); MissingAndDuplicate(f, governments.Where(x => x != null).Select(x => x.Id), "GOVERNMENT"); MissingAndDuplicate(f, contracts.Where(x => x != null).Select(x => x.Id), "CONTRACT"); MissingAndDuplicate(f, loans.Where(x => x != null).Select(x => x.Id), "LOAN"); MissingAndDuplicate(f, scenarios.Where(x => x != null).Select(x => x.Id), "SCENARIO");
            HashSet<string> regionIds = new HashSet<string>(regions.Where(x => x != null).Select(x => x.Id), StringComparer.OrdinalIgnoreCase), goodIds = new HashSet<string>(goods.Where(x => x != null).Select(x => x.Id), StringComparer.OrdinalIgnoreCase), firmIds = new HashSet<string>(firms.Where(x => x != null).Select(x => x.Id), StringComparer.OrdinalIgnoreCase), bankIds = new HashSet<string>(banks.Where(x => x != null).Select(x => x.Id), StringComparer.OrdinalIgnoreCase), govIds = new HashSet<string>(governments.Where(x => x != null).Select(x => x.Id), StringComparer.OrdinalIgnoreCase), borrowerIds = new HashSet<string>(firmIds.Concat(households.Where(x => x != null).Select(x => x.Id)), StringComparer.OrdinalIgnoreCase);
            foreach (EcoRegion x in Active(regions)) if (!Finite(x.ExchangeRateToNumeraire) || !Finite(x.ImportTariffRate) || x.ExchangeRateToNumeraire <= 0 || x.ImportTariffRate < 0 || x.ImportTariffRate > 5) f.Add("BLOCKER · REGION_DOMAIN · " + x.Id); foreach (EcoGood x in Active(goods)) if (!Finite(x.ReferencePrice) || !Finite(x.ConsumptionWeight) || !Finite(x.DepreciationRate) || x.ReferencePrice <= 0 || x.ConsumptionWeight < 0 || x.DepreciationRate < 0 || x.DepreciationRate > 1) f.Add("BLOCKER · GOOD_DOMAIN · " + x.Id);
            foreach (EcoHousehold x in Active(households)) if (!regionIds.Contains(x.RegionId ?? "") || !Finite(x.PopulationWeight) || !Finite(x.Cash) || !Finite(x.Deposits) || !Finite(x.Debt) || !Finite(x.AvailableHours) || !Finite(x.ReservationWage) || !Finite(x.Productivity) || !Finite(x.PropensityToConsume) || x.PopulationWeight <= 0 || x.Cash < 0 || x.Deposits < 0 || x.Debt < 0 || x.AvailableHours < 0 || x.ReservationWage < 0 || x.Productivity <= 0 || x.PropensityToConsume < 0 || x.PropensityToConsume > 1) f.Add("BLOCKER · HOUSEHOLD_DOMAIN · " + x.Id);
            foreach (EcoFirm x in Active(firms)) { if (!regionIds.Contains(x.RegionId ?? "") || (!String.IsNullOrWhiteSpace(x.BankId) && !bankIds.Contains(x.BankId)) || !Finite(x.Cash) || !Finite(x.Equity) || !Finite(x.Debt) || !Finite(x.CapitalStock) || !Finite(x.CapacityBatches) || !Finite(x.Productivity) || !Finite(x.OfferedWage) || !Finite(x.Markup) || x.Cash < 0 || x.Debt < 0 || x.CapitalStock < 0 || x.CapacityBatches < 0 || x.Productivity <= 0 || x.OfferedWage < 0 || x.Markup < -0.99) f.Add("BLOCKER · FIRM_DOMAIN · " + x.Id); foreach (KeyValuePair<string, double> inventory in x.Inventory ?? new Dictionary<string, double>()) if (!goodIds.Contains(inventory.Key) || !Finite(inventory.Value) || inventory.Value < 0) f.Add("BLOCKER · FIRM_INVENTORY_DOMAIN · " + x.Id + ":" + inventory.Key); }
            foreach (EcoProductionRecipe x in Active(recipes)) { if (!firmIds.Contains(x.FirmId ?? "") || !goodIds.Contains(x.OutputGoodId ?? "") || !Finite(x.OutputPerBatch) || !Finite(x.LaborHoursPerBatch) || x.OutputPerBatch <= 0 || x.LaborHoursPerBatch < 0) f.Add("BLOCKER · RECIPE_DOMAIN · " + x.Id); foreach (KeyValuePair<string, double> input in x.InputsPerBatch ?? new Dictionary<string, double>()) if (!goodIds.Contains(input.Key) || !Finite(input.Value) || input.Value < 0) f.Add("BLOCKER · RECIPE_INPUT_DOMAIN · " + x.Id + ":" + input.Key); }
            foreach (EcoBank x in Active(banks)) if (!regionIds.Contains(x.RegionId ?? "") || !Finite(x.Reserves) || !Finite(x.Deposits) || !Finite(x.Equity) || !Finite(x.BaseInterestRate) || !Finite(x.MinimumCapitalRatio) || !Finite(x.MinimumReserveRatio) || x.Reserves < 0 || x.Deposits < 0 || x.MinimumCapitalRatio <= 0 || x.MinimumCapitalRatio > 1 || x.MinimumReserveRatio < 0 || x.MinimumReserveRatio > 1 || x.BaseInterestRate < -0.5) f.Add("BLOCKER · BANK_DOMAIN · " + x.Id); foreach (EcoGovernment x in Active(governments)) if (!regionIds.Contains(x.RegionId ?? "") || new[] { x.IncomeTaxRate, x.SalesTaxRate, x.CorporateTaxRate, x.PayrollTaxRate }.Any(v => !Finite(v) || v < 0 || v > 1) || !Finite(x.Cash) || !Finite(x.Debt) || !Finite(x.TransferPerHousehold) || x.Debt < 0 || x.TransferPerHousehold < 0) f.Add("BLOCKER · GOVERNMENT_DOMAIN · " + x.Id);
            foreach (EcoContract x in Active(contracts)) if (!firmIds.Contains(x.BuyerId ?? "") || !firmIds.Contains(x.SellerId ?? "") || !goodIds.Contains(x.GoodId ?? "") || !Finite(x.Quantity) || !Finite(x.UnitPrice) || x.Quantity < 0 || x.UnitPrice < 0 || x.DuePeriod < x.StartPeriod) f.Add("BLOCKER · CONTRACT_DOMAIN · " + x.Id); foreach (EcoLoan x in Active(loans)) if (!bankIds.Contains(x.BankId ?? "") || !borrowerIds.Contains(x.BorrowerId ?? "") || !Finite(x.OriginalPrincipal) || !Finite(x.OutstandingPrincipal) || !Finite(x.AnnualInterestRate) || x.OriginalPrincipal < 0 || x.OutstandingPrincipal < 0 || x.RemainingPeriods < 0) f.Add("BLOCKER · LOAN_DOMAIN · " + x.Id); foreach (EcoScenario x in Active(scenarios)) if (new[] { x.Weight, x.HouseholdDemandMultiplier, x.ProductivityMultiplier, x.InputCostMultiplier, x.WageMultiplier, x.CreditAvailabilityMultiplier, x.TradeFrictionMultiplier, x.ExogenousInflationMultiplier }.Any(v => !Finite(v) || v < 0) || x.ProductivityMultiplier <= 0 || x.CreditAvailabilityMultiplier <= 0 || x.ExogenousInflationMultiplier <= 0) f.Add("BLOCKER · SCENARIO_DOMAIN · " + x.Id); foreach (KeyValuePair<string, double> x in model.Parameters ?? new Dictionary<string, double>()) if (String.IsNullOrWhiteSpace(x.Key) || !Finite(x.Value)) f.Add("BLOCKER · PARAMETER_DOMAIN · " + (x.Key ?? "∅")); if ((model.Assumptions ?? new List<string>()).Count == 0) f.Add("WARN · NO_EXPLICIT_ASSUMPTIONS"); if ((model.EvidenceNodeIds ?? new List<string>()).Count == 0) f.Add("INFO · NO_WORLD_EVIDENCE_ANCHORS"); f.Add("BOUNDARY · economic entities are synthetic planning records; no result authorizes pricing, lending, taxation, employment, trade, purchasing, investment, bankruptcy, benefit eligibility or policy action"); return f.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string Fingerprint(EconomicModel m)
        {
            if (m == null) return HashText("NULL_ECONOMIC_MODEL"); StringBuilder s = new StringBuilder(); s.Append(m.Id).Append('|').Append(m.ParentModelId).Append('|').Append(m.Name).Append('|').Append(m.Description).Append('|').Append(m.Status).Append('|').Append(m.Revision).Append('|').Append(m.Horizon).Append('|').Append(m.Seed).Append('|').Append(m.SourceKind).Append('|').Append(m.SourceId).Append('|').Append(m.SourceFingerprint); AppendMap(s, m.Parameters); foreach (EcoRegion x in (m.Regions ?? new List<EcoRegion>()).OrderBy(x => x.Id)) { s.Append("|R:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Currency).Append(':').Append(R(x.ExchangeRateToNumeraire)).Append(':').Append(R(x.ImportTariffRate)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoGood x in (m.Goods ?? new List<EcoGood>()).OrderBy(x => x.Id)) { s.Append("|G:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.Unit).Append(':').Append(x.Kind).Append(':').Append(R(x.ReferencePrice)).Append(':').Append(R(x.ConsumptionWeight)).Append(':').Append(R(x.DepreciationRate)).Append(':').Append(x.Essential).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoHousehold x in (m.Households ?? new List<EcoHousehold>()).OrderBy(x => x.Id)) { s.Append("|H:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.RegionId).Append(':').Append(R(x.PopulationWeight)).Append(':').Append(R(x.Cash)).Append(':').Append(R(x.Deposits)).Append(':').Append(R(x.OtherWealth)).Append(':').Append(R(x.Debt)).Append(':').Append(R(x.AvailableHours)).Append(':').Append(R(x.ReservationWage)).Append(':').Append(R(x.Productivity)).Append(':').Append(R(x.PropensityToConsume)).Append(':').Append(R(x.EssentialBudgetShare)).Append(':').Append(x.Skill).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoFirm x in (m.Firms ?? new List<EcoFirm>()).OrderBy(x => x.Id)) { s.Append("|F:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.RegionId).Append(':').Append(x.BankId).Append(':').Append(R(x.Cash)).Append(':').Append(R(x.Equity)).Append(':').Append(R(x.Debt)).Append(':').Append(R(x.CapitalStock)).Append(':').Append(R(x.CapacityBatches)).Append(':').Append(R(x.Productivity)).Append(':').Append(R(x.OfferedWage)).Append(':').Append(R(x.Markup)).Append(':').Append(R(x.TargetInventoryPeriods)).Append(':').Append(x.RequiredSkill).Append(':').Append(x.Status); AppendMap(s, x.Inventory); AppendList(s, x.EvidenceNodeIds); }
            foreach (EcoProductionRecipe x in (m.Recipes ?? new List<EcoProductionRecipe>()).OrderBy(x => x.Id)) { s.Append("|P:").Append(x.Id).Append(':').Append(x.FirmId).Append(':').Append(x.OutputGoodId).Append(':').Append(R(x.OutputPerBatch)).Append(':').Append(R(x.LaborHoursPerBatch)).Append(':').Append(R(x.CapitalUsePerBatch)).Append(':').Append(x.Status); AppendMap(s, x.InputsPerBatch); AppendList(s, x.EvidenceNodeIds); } foreach (EcoBank x in (m.Banks ?? new List<EcoBank>()).OrderBy(x => x.Id)) { s.Append("|B:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.RegionId).Append(':').Append(R(x.Reserves)).Append(':').Append(R(x.Deposits)).Append(':').Append(R(x.Equity)).Append(':').Append(R(x.BaseInterestRate)).Append(':').Append(R(x.MinimumCapitalRatio)).Append(':').Append(R(x.MinimumReserveRatio)).Append(':').Append(R(x.MaximumBorrowerLeverage)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoGovernment x in (m.Governments ?? new List<EcoGovernment>()).OrderBy(x => x.Id)) { s.Append("|V:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(x.RegionId).Append(':').Append(R(x.Cash)).Append(':').Append(R(x.Debt)).Append(':').Append(R(x.IncomeTaxRate)).Append(':').Append(R(x.SalesTaxRate)).Append(':').Append(R(x.CorporateTaxRate)).Append(':').Append(R(x.PayrollTaxRate)).Append(':').Append(R(x.TransferPerHousehold)).Append(':').Append(R(x.ProcurementBudget)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); }
            foreach (EcoContract x in (m.Contracts ?? new List<EcoContract>()).OrderBy(x => x.Id)) { s.Append("|C:").Append(x.Id).Append(':').Append(x.BuyerId).Append(':').Append(x.SellerId).Append(':').Append(x.GoodId).Append(':').Append(R(x.Quantity)).Append(':').Append(R(x.UnitPrice)).Append(':').Append(x.StartPeriod).Append(':').Append(x.DuePeriod).Append(':').Append(R(x.LatePenaltyRate)).Append(':').Append(R(x.DeliveredQuantity)).Append(':').Append(R(x.PaidAmount)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoLoan x in (m.Loans ?? new List<EcoLoan>()).OrderBy(x => x.Id)) { s.Append("|L:").Append(x.Id).Append(':').Append(x.BankId).Append(':').Append(x.BorrowerId).Append(':').Append(R(x.OriginalPrincipal)).Append(':').Append(R(x.OutstandingPrincipal)).Append(':').Append(R(x.AnnualInterestRate)).Append(':').Append(x.OriginationPeriod).Append(':').Append(x.RemainingPeriods).Append(':').Append(x.MissedPayments).Append(':').Append(R(x.CollateralValue)).Append(':').Append(x.Status); AppendList(s, x.EvidenceNodeIds); } foreach (EcoScenario x in (m.Scenarios ?? new List<EcoScenario>()).OrderBy(x => x.Id)) { s.Append("|S:").Append(x.Id).Append(':').Append(x.Name).Append(':').Append(R(x.Weight)).Append(':').Append(R(x.HouseholdDemandMultiplier)).Append(':').Append(R(x.ProductivityMultiplier)).Append(':').Append(R(x.InputCostMultiplier)).Append(':').Append(R(x.WageMultiplier)).Append(':').Append(R(x.CreditAvailabilityMultiplier)).Append(':').Append(R(x.TradeFrictionMultiplier)).Append(':').Append(R(x.ExogenousInflationMultiplier)).Append(':').Append(x.Status); AppendList(s, x.Assumptions); } AppendList(s, m.Assumptions); AppendList(s, m.EvidenceNodeIds); return HashText(s.ToString());
        }

        private static EcoFirm CopyFirm(EcoFirm x) { return new EcoFirm { Id = x.Id, Name = x.Name, RegionId = x.RegionId, BankId = x.BankId, Cash = x.Cash, Equity = x.Equity, Debt = x.Debt, CapitalStock = x.CapitalStock, CapacityBatches = x.CapacityBatches, Productivity = x.Productivity, OfferedWage = x.OfferedWage, Markup = x.Markup, TargetInventoryPeriods = x.TargetInventoryPeriods, RequiredSkill = x.RequiredSkill, Status = x.Status, Inventory = new Dictionary<string, double>(x.Inventory ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase), EvidenceNodeIds = new List<string>(x.EvidenceNodeIds ?? new List<string>()) }; }
        private static EcoBank CopyBank(EcoBank x) { return new EcoBank { Id = x.Id, Name = x.Name, RegionId = x.RegionId, Reserves = x.Reserves, Deposits = x.Deposits, Equity = x.Equity, BaseInterestRate = x.BaseInterestRate, MinimumCapitalRatio = x.MinimumCapitalRatio, MinimumReserveRatio = x.MinimumReserveRatio, MaximumBorrowerLeverage = x.MaximumBorrowerLeverage, Status = x.Status, EvidenceNodeIds = new List<string>(x.EvidenceNodeIds ?? new List<string>()) }; }
        private static EcoGovernment CopyGovernment(EcoGovernment x) { return new EcoGovernment { Id = x.Id, Name = x.Name, RegionId = x.RegionId, Cash = x.Cash, Debt = x.Debt, IncomeTaxRate = x.IncomeTaxRate, SalesTaxRate = x.SalesTaxRate, CorporateTaxRate = x.CorporateTaxRate, PayrollTaxRate = x.PayrollTaxRate, TransferPerHousehold = x.TransferPerHousehold, ProcurementBudget = x.ProcurementBudget, Status = x.Status, EvidenceNodeIds = new List<string>(x.EvidenceNodeIds ?? new List<string>()) }; }

        private static void OpenBooks(Book book, List<HouseholdState> households, List<FirmState> firms, List<BankState> banks, List<GovernmentState> governments, List<LoanState> loans) { foreach (HouseholdState h in households) if (h.Cash > Epsilon) book.Post(0, "OPENING_BALANCE", h.Source.Id, Dr(h.Source.Id, "CASH", h.Cash), Cr(h.Source.Id, "OPENING_EQUITY", h.Cash)); foreach (FirmState f in firms) if (f.Cash > Epsilon) book.Post(0, "OPENING_BALANCE", f.Source.Id, Dr(f.Source.Id, "CASH", f.Cash), Cr(f.Source.Id, "OPENING_EQUITY", f.Cash)); foreach (BankState b in banks) if (b.Reserves > Epsilon) book.Post(0, "OPENING_BALANCE", b.Source.Id, Dr(b.Source.Id, "RESERVES", b.Reserves), Cr(b.Source.Id, "OPENING_EQUITY", b.Reserves)); foreach (GovernmentState g in governments) if (g.Cash > Epsilon) book.Post(0, "OPENING_BALANCE", g.Source.Id, Dr(g.Source.Id, "CASH", g.Cash), Cr(g.Source.Id, "OPENING_EQUITY", g.Cash)); foreach (LoanState l in loans.Where(x => x.Outstanding > Epsilon)) book.Post(0, "OPENING_LOAN", l.Source.Id, Dr(l.Source.BorrowerId, "CASH_OR_PRIOR_USE", l.Outstanding), Cr(l.Source.BorrowerId, "LOAN_PAYABLE", l.Outstanding), Dr(l.Source.BankId, "LOAN_RECEIVABLE", l.Outstanding), Cr(l.Source.BankId, "DEPOSIT_LIABILITY", l.Outstanding)); }
        private static void ApplyPolicy(Dictionary<string, double> p, List<BankState> banks, List<GovernmentState> governments, List<FirmState> firms) { double v; foreach (GovernmentState g in governments) { if (p.TryGetValue("income_tax_rate", out v)) g.Source.IncomeTaxRate = g.IncomeTaxRate = Clamp(v, 0, 1); if (p.TryGetValue("sales_tax_rate", out v)) g.Source.SalesTaxRate = g.SalesTaxRate = Clamp(v, 0, 1); if (p.TryGetValue("corporate_tax_rate", out v)) g.Source.CorporateTaxRate = g.CorporateTaxRate = Clamp(v, 0, 1); if (p.TryGetValue("payroll_tax_rate", out v)) g.Source.PayrollTaxRate = g.PayrollTaxRate = Clamp(v, 0, 1); if (p.TryGetValue("transfer_per_household", out v)) g.Source.TransferPerHousehold = g.TransferPerHousehold = Math.Max(0, v); } foreach (BankState b in banks) { if (p.TryGetValue("base_interest_rate", out v)) b.Source.BaseInterestRate = b.BaseInterestRate = Math.Max(-0.5, v); if (p.TryGetValue("capital_ratio", out v)) b.Source.MinimumCapitalRatio = b.MinimumCapitalRatio = Clamp(v, 0.001, 1); } if (p.TryGetValue("minimum_wage", out v)) foreach (FirmState f in firms) f.Source.OfferedWage = f.OfferedWage = Math.Max(f.OfferedWage, Math.Max(0, v)); }

        private static void IssueWorkingCapital(int period, EconomicModel model, EcoScenario world, List<FirmState> firms, List<BankState> banks, List<LoanState> loans, Book book) { foreach (FirmState f in firms.Where(x => ActiveStatus(x.Status))) { double desired = Math.Max(0, f.Source.CapacityBatches * f.Source.OfferedWage * P(model, "working_capital_periods", 1) - f.Cash); if (desired <= Epsilon || String.IsNullOrWhiteSpace(f.Source.BankId)) continue; BankState bank = banks.FirstOrDefault(x => Eq(x.Source.Id, f.Source.BankId)); if (bank == null || bank.Equity <= 0) continue; double existing = loans.Where(x => Eq(x.Source.BorrowerId, f.Source.Id) && ActiveStatus(x.Status)).Sum(x => x.Outstanding), leverageRoom = Math.Max(0, Math.Max(0, f.Equity) * bank.Source.MaximumBorrowerLeverage - existing), outstanding = loans.Where(x => Eq(x.Source.BankId, bank.Source.Id) && ActiveStatus(x.Status)).Sum(x => x.Outstanding), capitalRoom = Math.Max(0, bank.Equity / Math.Max(Epsilon, bank.Source.MinimumCapitalRatio) - outstanding), amount = Math.Min(desired, Math.Min(leverageRoom, capitalRoom) * world.CreditAvailabilityMultiplier); if (amount <= Epsilon) continue; EcoLoan source = new EcoLoan { Id = "auto-loan-" + period.ToString(CultureInfo.InvariantCulture) + "-" + f.Source.Id, BankId = bank.Source.Id, BorrowerId = f.Source.Id, OriginalPrincipal = amount, OutstandingPrincipal = amount, AnnualInterestRate = bank.Source.BaseInterestRate + P(model, "credit_spread", 0.03), OriginationPeriod = period, RemainingPeriods = Math.Max(1, (int)P(model, "loan_term_periods", 12)), CollateralValue = Math.Max(0, f.Source.CapitalStock), Status = "ACTIVE" }; loans.Add(new LoanState { Source = source, Outstanding = amount, Remaining = source.RemainingPeriods, Status = "ACTIVE" }); f.Cash += amount; f.Debt += amount; bank.Deposits += amount; book.Post(period, "LOAN_ORIGINATION", f.Source.Id + " ← " + bank.Source.Id, Dr(f.Source.Id, "CASH", amount), Cr(f.Source.Id, "LOAN_PAYABLE", amount), Dr(bank.Source.Id, "LOAN_RECEIVABLE", amount), Cr(bank.Source.Id, "DEPOSIT_LIABILITY", amount)); } }

        private static void MatchLaborAndPayroll(int period, EconomicModel model, EcoScenario world, List<HouseholdState> households, List<FirmState> firms, List<GovernmentState> governments, Book book, EcoSimulationResult result) { Dictionary<string, double> need = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase); foreach (FirmState f in firms.Where(x => ActiveStatus(x.Status))) { double labor = Active(model.Recipes).Where(x => Eq(x.FirmId, f.Source.Id)).Sum(x => x.LaborHoursPerBatch * f.Source.CapacityBatches); need[f.Source.Id] = Math.Max(0, labor); } foreach (HouseholdState h in households.OrderBy(x => x.Source.ReservationWage).ThenByDescending(x => x.Source.Productivity).ThenBy(x => x.Source.Id)) { FirmState employer = firms.Where(x => ActiveStatus(x.Status) && need.ContainsKey(x.Source.Id) && need[x.Source.Id] > Epsilon && x.Source.OfferedWage * world.WageMultiplier + Epsilon >= h.Source.ReservationWage && (Eq(x.Source.RequiredSkill, "GENERAL") || Eq(x.Source.RequiredSkill, h.Source.Skill))).OrderByDescending(x => x.Source.OfferedWage).ThenBy(x => x.Source.Id).FirstOrDefault(); if (employer == null) continue; double hours = Math.Min(h.Source.AvailableHours * h.Source.PopulationWeight, need[employer.Source.Id]), rate = employer.Source.OfferedWage * world.WageMultiplier, gross = hours * rate * h.Source.Productivity; if (gross > employer.Cash) { gross = employer.Cash; hours = rate * h.Source.Productivity <= Epsilon ? 0 : gross / (rate * h.Source.Productivity); } if (gross <= Epsilon) continue; GovernmentState gov = governments.FirstOrDefault(x => Eq(x.Source.RegionId, h.Source.RegionId)); double taxRate = gov == null ? 0 : Clamp(gov.Source.IncomeTaxRate + gov.Source.PayrollTaxRate, 0, 1), tax = gross * taxRate, net = gross - tax; employer.Cash -= gross; employer.WageCost += gross; employer.LaborHours += hours; h.Cash += net; h.Income += gross; h.Hours += hours; h.EmployerId = employer.Source.Id; need[employer.Source.Id] -= hours; if (gov != null) { gov.Cash += tax; gov.Revenue += tax; } book.Post(period, "PAYROLL", h.Source.Id + " @ " + employer.Source.Id, Dr(employer.Source.Id, "WAGE_EXPENSE", gross), Cr(employer.Source.Id, "CASH", gross), Dr(h.Source.Id, "CASH", net), Dr(h.Source.Id, "TAX_EXPENSE", tax), Cr(h.Source.Id, "WAGE_INCOME", gross), Dr(gov == null ? "TAX_VOID" : gov.Source.Id, "CASH", tax), Cr(gov == null ? "TAX_VOID" : gov.Source.Id, "TAX_REVENUE", tax)); result.Employment.Add(new EcoEmploymentRecord { Period = period, HouseholdId = h.Source.Id, FirmId = employer.Source.Id, Hours = hours, WageRate = rate, GrossWage = gross, Tax = tax }); } }

        private static void Produce(int period, EconomicModel model, EcoScenario world, List<FirmState> firms, Dictionary<string, double> prices, Dictionary<string, double> supply, Book book, EcoSimulationResult result) { foreach (FirmState f in firms.Where(x => ActiveStatus(x.Status))) { double laborRemaining = f.LaborHours; foreach (EcoProductionRecipe recipe in Active(model.Recipes).Where(x => Eq(x.FirmId, f.Source.Id)).OrderBy(x => x.Id)) { double batches = f.Source.CapacityBatches * f.Source.Productivity * world.ProductivityMultiplier; if (recipe.LaborHoursPerBatch > Epsilon) batches = Math.Min(batches, laborRemaining / recipe.LaborHoursPerBatch); foreach (KeyValuePair<string, double> input in recipe.InputsPerBatch) if (input.Value > Epsilon) batches = Math.Min(batches, Get(f.Inventory, input.Key) / input.Value); batches = Math.Max(0, batches); if (batches <= Epsilon) continue; double inputValue = 0; foreach (KeyValuePair<string, double> input in recipe.InputsPerBatch) { double used = batches * input.Value; f.Inventory[input.Key] = Math.Max(0, Get(f.Inventory, input.Key) - used); inputValue += used * Get(prices, input.Key) * world.InputCostMultiplier; } double quantity = batches * recipe.OutputPerBatch; f.Inventory[recipe.OutputGoodId] = Get(f.Inventory, recipe.OutputGoodId) + quantity; laborRemaining -= batches * recipe.LaborHoursPerBatch; supply[recipe.OutputGoodId] = Get(supply, recipe.OutputGoodId) + quantity; result.Production.Add(new EcoProductionRecord { Period = period, FirmId = f.Source.Id, GoodId = recipe.OutputGoodId, Batches = batches, Quantity = quantity, LaborHours = batches * recipe.LaborHoursPerBatch, UnitCostProxy = quantity <= Epsilon ? 0 : (inputValue + f.WageCost) / quantity }); if (inputValue > Epsilon) book.Post(period, "PRODUCTION_CONSUMPTION", f.Source.Id + " · " + recipe.Id, Dr(f.Source.Id, "WORK_IN_PROCESS", inputValue), Cr(f.Source.Id, "INPUT_INVENTORY", inputValue)); } } }

        private static void FulfillContracts(int period, EconomicModel model, EcoScenario world, List<FirmState> firms, List<GovernmentState> governments, List<ContractState> contracts, Dictionary<string, double> prices, Dictionary<string, EcoRegion> regions, Book book, EcoSimulationResult result) { foreach (ContractState c in contracts.Where(x => ActiveStatus(x.Status) && period >= x.Source.StartPeriod).OrderBy(x => x.Source.DuePeriod).ThenBy(x => x.Source.Id)) { FirmState buyer = firms.FirstOrDefault(x => Eq(x.Source.Id, c.Source.BuyerId) && ActiveStatus(x.Status)), seller = firms.FirstOrDefault(x => Eq(x.Source.Id, c.Source.SellerId) && ActiveStatus(x.Status)); if (buyer == null || seller == null) { c.Status = "IMPOSSIBLE"; continue; } double remaining = Math.Max(0, c.Source.Quantity - c.Delivered), available = Get(seller.Inventory, c.Source.GoodId), price = c.Source.UnitPrice <= 0 ? Get(prices, c.Source.GoodId) : c.Source.UnitPrice, late = period > c.Source.DuePeriod ? 1 + (period - c.Source.DuePeriod) * c.Source.LatePenaltyRate : 1, quantity = Math.Min(remaining, Math.Min(available, buyer.Cash / Math.Max(Epsilon, price * late))); if (quantity <= Epsilon) { if (period > c.Source.DuePeriod) c.Status = "LATE"; continue; } double amount = quantity * price * late; buyer.Cash -= amount; seller.Cash += amount; buyer.Inventory[c.Source.GoodId] = Get(buyer.Inventory, c.Source.GoodId) + quantity; seller.Inventory[c.Source.GoodId] = Math.Max(0, available - quantity); buyer.InputCost += amount; seller.Revenue += amount; seller.Sales += amount; c.Delivered += quantity; c.Paid += amount; c.Status = c.Delivered + Epsilon >= c.Source.Quantity ? "FULFILLED" : period > c.Source.DuePeriod ? "LATE" : "PARTIAL"; book.Post(period, "CONTRACT_SETTLEMENT", c.Source.Id, Dr(buyer.Source.Id, "INVENTORY", amount), Cr(buyer.Source.Id, "CASH", amount), Dr(seller.Source.Id, "CASH", amount), Cr(seller.Source.Id, "REVENUE", amount)); result.Trades.Add(new EcoTradeRecord { Period = period, Kind = "CONTRACT", BuyerId = buyer.Source.Id, SellerId = seller.Source.Id, GoodId = c.Source.GoodId, OriginRegionId = seller.Source.RegionId, DestinationRegionId = buyer.Source.RegionId, Quantity = quantity, UnitPrice = price * late, Total = amount }); } }

        private static void TradeIntermediate(int period, EconomicModel model, EcoScenario world, List<FirmState> firms, List<GovernmentState> governments, Dictionary<string, EcoGood> goods, Dictionary<string, double> prices, Dictionary<string, EcoRegion> regions, Dictionary<string, double> demand, Dictionary<string, double> supply, Book book, EcoSimulationResult result) { foreach (FirmState buyer in firms.Where(x => ActiveStatus(x.Status)).OrderBy(x => x.Source.Id)) foreach (EcoProductionRecipe recipe in Active(model.Recipes).Where(x => Eq(x.FirmId, buyer.Source.Id))) foreach (KeyValuePair<string, double> input in recipe.InputsPerBatch) { double target = input.Value * buyer.Source.CapacityBatches * Math.Max(1, buyer.Source.TargetInventoryPeriods), wanted = Math.Max(0, target - Get(buyer.Inventory, input.Key)); demand[input.Key] = Get(demand, input.Key) + wanted; if (wanted <= Epsilon) continue; List<SaleOffer> offers = Offers(model, world, firms, prices, input.Key, buyer.Source.RegionId, regions); foreach (SaleOffer offer in offers) { if (wanted <= Epsilon || buyer.Cash <= Epsilon) break; double tariffRate = CrossRegionTariff(offer.Firm.Source.RegionId, buyer.Source.RegionId, regions, world), unit = offer.Price * (1 + tariffRate), quantity = Math.Min(wanted, Math.Min(offer.Available, buyer.Cash / Math.Max(Epsilon, unit))), baseAmount = quantity * offer.Price, tariff = baseAmount * tariffRate, total = baseAmount + tariff; if (quantity <= Epsilon) continue; buyer.Cash -= total; offer.Firm.Cash += baseAmount; buyer.Inventory[input.Key] = Get(buyer.Inventory, input.Key) + quantity; offer.Firm.Inventory[input.Key] = Math.Max(0, Get(offer.Firm.Inventory, input.Key) - quantity); buyer.InputCost += total; offer.Firm.Revenue += baseAmount; offer.Firm.Sales += baseAmount; GovernmentState gov = governments.FirstOrDefault(x => Eq(x.Source.RegionId, buyer.Source.RegionId)); if (gov != null) { gov.Cash += tariff; gov.Revenue += tariff; } book.Post(period, "INTERMEDIATE_TRADE", buyer.Source.Id + " ← " + offer.Firm.Source.Id, Dr(buyer.Source.Id, "INVENTORY", total), Cr(buyer.Source.Id, "CASH", total), Dr(offer.Firm.Source.Id, "CASH", baseAmount), Cr(offer.Firm.Source.Id, "REVENUE", baseAmount), Dr(gov == null ? "TARIFF_VOID" : gov.Source.Id, "CASH", tariff), Cr(gov == null ? "TARIFF_VOID" : gov.Source.Id, "TARIFF_REVENUE", tariff)); result.Trades.Add(new EcoTradeRecord { Period = period, Kind = "INTERMEDIATE", BuyerId = buyer.Source.Id, SellerId = offer.Firm.Source.Id, GoodId = input.Key, OriginRegionId = offer.Firm.Source.RegionId, DestinationRegionId = buyer.Source.RegionId, Quantity = quantity, UnitPrice = offer.Price, Tariff = tariff, Total = total }); wanted -= quantity; offer.Available -= quantity; } } }

        private static void TradeHouseholdConsumption(int period, EconomicModel model, EcoScenario world, List<HouseholdState> households, List<FirmState> firms, List<GovernmentState> governments, Dictionary<string, EcoGood> goods, Dictionary<string, double> prices, Dictionary<string, EcoRegion> regions, Dictionary<string, double> demand, Dictionary<string, double> supply, Book book, EcoSimulationResult result) { double essentialWeight = goods.Values.Where(x => x.Essential).Sum(x => x.ConsumptionWeight), discretionaryWeight = goods.Values.Where(x => !x.Essential).Sum(x => x.ConsumptionWeight); foreach (HouseholdState h in households.OrderBy(x => x.Source.Id)) { double totalBudget = Math.Min(h.Cash, h.Cash * h.Source.PropensityToConsume * world.HouseholdDemandMultiplier); foreach (EcoGood good in goods.Values.OrderByDescending(x => x.Essential).ThenBy(x => x.Id)) { double bucket = good.Essential ? h.Source.EssentialBudgetShare : 1 - h.Source.EssentialBudgetShare, denominator = good.Essential ? essentialWeight : discretionaryWeight; double budget = totalBudget * bucket * (denominator <= Epsilon ? 0 : good.ConsumptionWeight / denominator), reference = Math.Max(Epsilon, Get(prices, good.Id)), wanted = budget / reference; demand[good.Id] = Get(demand, good.Id) + wanted; List<SaleOffer> offers = Offers(model, world, firms, prices, good.Id, h.Source.RegionId, regions); foreach (SaleOffer offer in offers) { if (wanted <= Epsilon || h.Cash <= Epsilon) break; GovernmentState gov = governments.FirstOrDefault(x => Eq(x.Source.RegionId, h.Source.RegionId)); double salesRate = gov == null ? 0 : gov.Source.SalesTaxRate, tariffRate = CrossRegionTariff(offer.Firm.Source.RegionId, h.Source.RegionId, regions, world), unit = offer.Price * (1 + salesRate + tariffRate), quantity = Math.Min(wanted, Math.Min(offer.Available, h.Cash / Math.Max(Epsilon, unit))), baseAmount = quantity * offer.Price, tax = baseAmount * salesRate, tariff = baseAmount * tariffRate, total = baseAmount + tax + tariff; if (quantity <= Epsilon) continue; h.Cash -= total; h.Consumption += total; offer.Firm.Cash += baseAmount; offer.Firm.Inventory[good.Id] = Math.Max(0, Get(offer.Firm.Inventory, good.Id) - quantity); offer.Firm.Revenue += baseAmount; offer.Firm.Sales += baseAmount; if (gov != null) { gov.Cash += tax + tariff; gov.Revenue += tax + tariff; } book.Post(period, "HOUSEHOLD_CONSUMPTION", h.Source.Id + " ← " + offer.Firm.Source.Id, Dr(h.Source.Id, "CONSUMPTION_EXPENSE", total), Cr(h.Source.Id, "CASH", total), Dr(offer.Firm.Source.Id, "CASH", baseAmount), Cr(offer.Firm.Source.Id, "REVENUE", baseAmount), Dr(gov == null ? "TAX_VOID" : gov.Source.Id, "CASH", tax + tariff), Cr(gov == null ? "TAX_VOID" : gov.Source.Id, "TAX_REVENUE", tax + tariff)); result.Trades.Add(new EcoTradeRecord { Period = period, Kind = "FINAL_CONSUMPTION", BuyerId = h.Source.Id, SellerId = offer.Firm.Source.Id, GoodId = good.Id, OriginRegionId = offer.Firm.Source.RegionId, DestinationRegionId = h.Source.RegionId, Quantity = quantity, UnitPrice = offer.Price, Tax = tax, Tariff = tariff, Total = total }); wanted -= quantity; offer.Available -= quantity; } } } }

        private static void ServiceLoans(int period, EconomicModel model, List<FirmState> firms, List<BankState> banks, List<LoanState> loans, Book book, EcoSimulationResult result) { foreach (LoanState loan in loans.Where(x => ActiveStatus(x.Status) && x.Outstanding > Epsilon).OrderBy(x => x.Source.Id)) { FirmState borrower = firms.FirstOrDefault(x => Eq(x.Source.Id, loan.Source.BorrowerId)); BankState bank = banks.FirstOrDefault(x => Eq(x.Source.Id, loan.Source.BankId)); if (borrower == null || bank == null || !ActiveStatus(borrower.Status)) continue; double principal = loan.Outstanding / Math.Max(1, loan.Remaining), interest = loan.Outstanding * loan.Source.AnnualInterestRate / Math.Max(1, P(model, "periods_per_year", 12)), due = principal + interest, paid = Math.Min(borrower.Cash, Math.Max(0, due)); if (paid + Epsilon < due) { loan.Missed++; if (paid > Epsilon) { double paidInterest = Math.Min(interest, paid), paidPrincipal = Math.Max(0, paid - paidInterest); borrower.Cash -= paid; borrower.Debt -= paidPrincipal; loan.Outstanding -= paidPrincipal; bank.Reserves += paid; bank.InterestIncome += paidInterest; book.Post(period, "PARTIAL_DEBT_SERVICE", loan.Source.Id, Dr(borrower.Source.Id, "LOAN_PAYABLE", paidPrincipal), Dr(borrower.Source.Id, "INTEREST_EXPENSE", paidInterest), Cr(borrower.Source.Id, "CASH", paid), Dr(bank.Source.Id, "CASH", paid), Cr(bank.Source.Id, "LOAN_RECEIVABLE", paidPrincipal), Cr(bank.Source.Id, "INTEREST_INCOME", paidInterest)); } } else { borrower.Cash -= due; borrower.Debt -= principal; loan.Outstanding -= principal; loan.Remaining--; loan.Missed = 0; bank.Reserves += due; bank.InterestIncome += interest; book.Post(period, "DEBT_SERVICE", loan.Source.Id, Dr(borrower.Source.Id, "LOAN_PAYABLE", principal), Dr(borrower.Source.Id, "INTEREST_EXPENSE", interest), Cr(borrower.Source.Id, "CASH", due), Dr(bank.Source.Id, "CASH", due), Cr(bank.Source.Id, "LOAN_RECEIVABLE", principal), Cr(bank.Source.Id, "INTEREST_INCOME", interest)); if (loan.Outstanding <= Epsilon || loan.Remaining <= 0) loan.Status = "PAID"; } } }
        private static void PayTransfers(int period, EconomicModel model, List<HouseholdState> households, List<GovernmentState> governments, Book book) { foreach (GovernmentState gov in governments) foreach (HouseholdState h in households.Where(x => Eq(x.Source.RegionId, gov.Source.RegionId))) { double amount = gov.Source.TransferPerHousehold * h.Source.PopulationWeight; if (amount <= Epsilon) continue; gov.Cash -= amount; if (gov.Cash < 0) { gov.Debt += -gov.Cash; gov.Cash = 0; } gov.Spending += amount; h.Cash += amount; h.Income += amount; book.Post(period, "TRANSFER", gov.Source.Id + " → " + h.Source.Id, Dr(gov.Source.Id, "TRANSFER_EXPENSE", amount), Cr(gov.Source.Id, "CASH_OR_DEBT", amount), Dr(h.Source.Id, "CASH", amount), Cr(h.Source.Id, "TRANSFER_INCOME", amount)); } }
        private static void SettleCorporateTax(int period, EconomicModel model, List<FirmState> firms, List<GovernmentState> governments, Book book) { foreach (FirmState f in firms.Where(x => ActiveStatus(x.Status))) { double profit = f.Revenue - f.WageCost - f.InputCost, rate = governments.Where(x => Eq(x.Source.RegionId, f.Source.RegionId)).Select(x => x.Source.CorporateTaxRate).DefaultIfEmpty(0).First(), tax = Math.Min(f.Cash, Math.Max(0, profit) * rate); if (tax <= Epsilon) { f.Equity += profit; continue; } GovernmentState gov = governments.FirstOrDefault(x => Eq(x.Source.RegionId, f.Source.RegionId)); f.Cash -= tax; f.Tax += tax; f.Equity += profit - tax; if (gov != null) { gov.Cash += tax; gov.Revenue += tax; } book.Post(period, "CORPORATE_TAX", f.Source.Id, Dr(f.Source.Id, "TAX_EXPENSE", tax), Cr(f.Source.Id, "CASH", tax), Dr(gov == null ? "TAX_VOID" : gov.Source.Id, "CASH", tax), Cr(gov == null ? "TAX_VOID" : gov.Source.Id, "TAX_REVENUE", tax)); } }
        private static void ApplyDefaults(int period, EconomicModel model, List<FirmState> firms, List<BankState> banks, List<LoanState> loans, Book book, EcoSimulationResult result) { foreach (LoanState loan in loans.Where(x => ActiveStatus(x.Status) && x.Missed >= Math.Max(1, (int)P(model, "default_after_missed_payments", 2))).ToList()) { FirmState firm = firms.FirstOrDefault(x => Eq(x.Source.Id, loan.Source.BorrowerId)); BankState bank = banks.FirstOrDefault(x => Eq(x.Source.Id, loan.Source.BankId)); double recovery = Math.Min(loan.Outstanding, Math.Max(0, loan.Source.CollateralValue) * P(model, "collateral_recovery_rate", 0.5)), loss = Math.Max(0, loan.Outstanding - recovery); loan.Status = "DEFAULTED"; if (firm != null) { firm.Status = "BANKRUPT"; firm.Debt = Math.Max(0, firm.Debt - loan.Outstanding); firm.Equity -= loss; } if (bank != null) { bank.Equity -= loss; bank.Losses += loss; bank.Reserves += recovery; } book.Post(period, "CREDIT_DEFAULT", loan.Source.Id, Dr(firm == null ? loan.Source.BorrowerId : firm.Source.Id, "LOAN_PAYABLE", loan.Outstanding), Cr(firm == null ? loan.Source.BorrowerId : firm.Source.Id, "DEFAULT_GAIN_OR_EQUITY_LOSS", loan.Outstanding), Dr(bank == null ? loan.Source.BankId : bank.Source.Id, "CASH_RECOVERY", recovery), Dr(bank == null ? loan.Source.BankId : bank.Source.Id, "CREDIT_LOSS", loss), Cr(bank == null ? loan.Source.BankId : bank.Source.Id, "LOAN_RECEIVABLE", loan.Outstanding)); result.Defaults.Add(new EcoDefaultRecord { Period = period, BorrowerId = loan.Source.BorrowerId, BankId = loan.Source.BankId, LoanId = loan.Source.Id, Outstanding = loan.Outstanding, Recovery = recovery, Loss = loss, Kind = "LOAN_DEFAULT" }); loan.Outstanding = 0; } }
        private static void DepreciateInventories(EconomicModel model, List<FirmState> firms, Dictionary<string, EcoGood> goods) { foreach (FirmState f in firms) foreach (string id in f.Inventory.Keys.ToList()) { EcoGood good; if (goods.TryGetValue(id, out good)) f.Inventory[id] = Math.Max(0, f.Inventory[id] * (1 - Clamp(good.DepreciationRate, 0, 1))); } }
        private static void UpdatePrices(EconomicModel model, EcoScenario world, Dictionary<string, double> policy, Dictionary<string, EcoGood> goods, Dictionary<string, double> prices, Dictionary<string, double> demand, Dictionary<string, double> supply) { double speed; if (!policy.TryGetValue("price_adjustment_speed", out speed)) speed = P(model, "price_adjustment_speed", 0.12); foreach (EcoGood good in goods.Values) { double imbalance = (Get(demand, good.Id) - Get(supply, good.Id)) / Math.Max(1, Get(supply, good.Id)), factor = Math.Exp(Clamp(speed * imbalance, -0.693147, 0.693147)) * world.ExogenousInflationMultiplier; prices[good.Id] = Math.Max(Epsilon, Get(prices, good.Id) * factor); } }

        private static EcoPeriodIndicators Indicators(int period, EconomicModel model, List<HouseholdState> households, List<FirmState> firms, List<BankState> banks, List<GovernmentState> governments, List<LoanState> loans, Dictionary<string, EcoGood> goods, Dictionary<string, double> prices, Dictionary<string, double> demand, Dictionary<string, double> supply, EcoSimulationResult result, double previousCpi, int defaultsBefore) { List<EcoTradeRecord> trades = result.Trades.Where(x => x.Period == period).ToList(); double consumption = trades.Where(x => Eq(x.Kind, "FINAL_CONSUMPTION")).Sum(x => x.Total), intermediate = trades.Where(x => !Eq(x.Kind, "FINAL_CONSUMPTION")).Sum(x => x.Total), production = result.Production.Where(x => x.Period == period).Sum(x => x.Quantity * Get(prices, x.GoodId)), weight = goods.Values.Sum(x => x.ConsumptionWeight), cpi = weight <= Epsilon ? 1 : goods.Values.Sum(x => x.ConsumptionWeight * Get(prices, x.Id) / Math.Max(Epsilon, x.ReferencePrice)) / weight, population = households.Sum(x => x.Source.PopulationWeight), employed = households.Where(x => x.Hours > Epsilon).Sum(x => x.Source.PopulationWeight), totalDemand = demand.Values.Sum(), unmet = goods.Keys.Sum(x => Math.Max(0, Get(demand, x) - Get(supply, x))), money = households.Sum(x => Math.Max(0, x.Cash)) + firms.Sum(x => Math.Max(0, x.Cash)), debt = loans.Where(x => ActiveStatus(x.Status)).Sum(x => x.Outstanding), npl = loans.Sum(x => x.Outstanding) <= Epsilon ? 0 : loans.Where(x => x.Missed > 0 || Eq(x.Status, "DEFAULTED")).Sum(x => x.Outstanding) / loans.Sum(x => x.Outstanding), loansTotal = loans.Where(x => ActiveStatus(x.Status)).Sum(x => x.Outstanding), bankCapital = loansTotal <= Epsilon ? 1 : banks.Sum(x => x.Equity) / loansTotal, gdp = consumption, cross = trades.Where(x => !Eq(x.OriginRegionId, x.DestinationRegionId)).Sum(x => x.Total), hhi = Hhi(trades); return new EcoPeriodIndicators { Period = period, NominalGdp = gdp, ProductionValue = production, HouseholdConsumption = consumption, IntermediateTrade = intermediate, WageIncome = result.Employment.Where(x => x.Period == period).Sum(x => x.GrossWage), CorporateProfit = firms.Sum(x => x.Revenue - x.WageCost - x.InputCost - x.Tax), Cpi = cpi, InflationRate = previousCpi <= Epsilon ? 0 : cpi / previousCpi - 1, UnemploymentRate = population <= Epsilon ? 0 : 1 - employed / population, ShortageRate = totalDemand <= Epsilon ? 0 : unmet / totalDemand, WealthGini = Gini(households.Select(x => Math.Max(0, x.Cash + x.Source.OtherWealth - x.Debt)), households.Select(x => x.Source.PopulationWeight)), IncomeGini = Gini(households.Select(x => Math.Max(0, x.Income)), households.Select(x => x.Source.PopulationWeight)), MoneySupply = money, MoneyVelocity = money <= Epsilon ? 0 : gdp / money, GovernmentRevenue = governments.Sum(x => x.Revenue), GovernmentSpending = governments.Sum(x => x.Spending), GovernmentBalance = governments.Sum(x => x.Revenue - x.Spending), PrivateDebt = debt, NonperformingLoanRatio = npl, BankCapitalRatio = bankCapital, MarketHhi = hhi, CrossRegionTrade = cross, ActiveFirms = firms.Count(x => ActiveStatus(x.Status)), Defaults = result.Defaults.Count - defaultsBefore }; }
        private static List<SaleOffer> Offers(EconomicModel model, EcoScenario world, List<FirmState> firms, Dictionary<string, double> prices, string goodId, string destinationRegionId, Dictionary<string, EcoRegion> regions) { return firms.Where(x => ActiveStatus(x.Status) && Get(x.Inventory, goodId) > Epsilon).Select(x => new SaleOffer { Firm = x, GoodId = goodId, Available = Get(x.Inventory, goodId), Price = Math.Max(Epsilon, Get(prices, goodId) * (1 + x.Source.Markup) * world.InputCostMultiplier) }).OrderBy(x => x.Price * (1 + CrossRegionTariff(x.Firm.Source.RegionId, destinationRegionId, regions, world))).ThenBy(x => x.Firm.Source.Id).ToList(); }
        private static double CrossRegionTariff(string origin, string destination, Dictionary<string, EcoRegion> regions, EcoScenario world) { if (Eq(origin, destination)) return 0; EcoRegion region; return regions.TryGetValue(destination ?? "", out region) ? Math.Max(0, region.ImportTariffRate * world.TradeFrictionMultiplier) : 0; }
        private static double Hhi(List<EcoTradeRecord> trades) { double total = (trades ?? new List<EcoTradeRecord>()).Sum(x => x.Total); return total <= Epsilon ? 0 : trades.GroupBy(x => x.SellerId, StringComparer.OrdinalIgnoreCase).Select(x => x.Sum(t => t.Total) / total).Sum(x => x * x); }
        private static double Gini(IEnumerable<double> rawValues, IEnumerable<double> rawWeights) { List<double> values = (rawValues ?? Enumerable.Empty<double>()).ToList(), weights = (rawWeights ?? Enumerable.Empty<double>()).ToList(); int n = Math.Min(values.Count, weights.Count); List<Tuple<double, double>> rows = Enumerable.Range(0, n).Select(i => Tuple.Create(Math.Max(0, values[i]), Math.Max(0, weights[i]))).Where(x => x.Item2 > Epsilon).OrderBy(x => x.Item1).ToList(); double totalWeight = rows.Sum(x => x.Item2), totalValue = rows.Sum(x => x.Item1 * x.Item2); if (totalWeight <= Epsilon || totalValue <= Epsilon) return 0; double cumulativeWeight = 0, cumulativeValue = 0, area = 0, priorX = 0, priorY = 0; foreach (Tuple<double, double> row in rows) { cumulativeWeight += row.Item2; cumulativeValue += row.Item1 * row.Item2; double x = cumulativeWeight / totalWeight, y = cumulativeValue / totalValue; area += (x - priorX) * (y + priorY) / 2; priorX = x; priorY = y; } return Clamp(1 - 2 * area, 0, 1); }

        private static double[,] Invert(double[,] matrix, List<string> audit) { if (matrix == null) return null; int n = matrix.GetLength(0); double[,] a = new double[n, n * 2]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { a[i, j] = matrix[i, j]; a[i, n + j] = i == j ? 1 : 0; } for (int col = 0; col < n; col++) { int pivot = Enumerable.Range(col, n - col).OrderByDescending(row => Math.Abs(a[row, col])).First(); if (Math.Abs(a[pivot, col]) <= 1e-10) { audit.Add("WARN · LEONTIEF_MATRIX_SINGULAR"); return null; } if (pivot != col) for (int j = 0; j < n * 2; j++) { double t = a[col, j]; a[col, j] = a[pivot, j]; a[pivot, j] = t; } double divisor = a[col, col]; for (int j = 0; j < n * 2; j++) a[col, j] /= divisor; for (int row = 0; row < n; row++) if (row != col) { double factor = a[row, col]; for (int j = 0; j < n * 2; j++) a[row, j] -= factor * a[col, j]; } } double[,] inverse = new double[n, n]; for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) inverse[i, j] = a[i, n + j]; return inverse; }
        private static List<List<string>> StrongComponents(Dictionary<string, HashSet<string>> graph) { List<List<string>> result = new List<List<string>>(); Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), low = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); Stack<string> stack = new Stack<string>(); HashSet<string> on = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int next = 0; Action<string> visit = null; visit = delegate(string v) { index[v] = next; low[v] = next++; stack.Push(v); on.Add(v); foreach (string w in graph[v]) { if (!index.ContainsKey(w)) { visit(w); low[v] = Math.Min(low[v], low[w]); } else if (on.Contains(w)) low[v] = Math.Min(low[v], index[w]); } if (low[v] == index[v]) { List<string> component = new List<string>(); string w; do { w = stack.Pop(); on.Remove(w); component.Add(w); } while (!Eq(w, v)); result.Add(component.OrderBy(x => x).ToList()); } }; foreach (string v in graph.Keys.OrderBy(x => x)) if (!index.ContainsKey(v)) visit(v); return result.OrderByDescending(x => x.Count).ThenBy(x => x.FirstOrDefault()).ToList(); }
        private static void MissingAndDuplicate(List<string> findings, IEnumerable<string> ids, string kind) { List<string> list = (ids ?? Enumerable.Empty<string>()).ToList(); if (list.Any(String.IsNullOrWhiteSpace)) findings.Add("BLOCKER · MISSING_" + kind + "_ID"); foreach (IGrouping<string, string> g in list.Where(x => !String.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)) findings.Add("BLOCKER · DUPLICATE_" + kind + "_ID · " + g.Key); }
        private static EcoJournalLine Dr(string entity, string account, double amount) { return new EcoJournalLine { EntityId = entity, Account = account, Debit = Math.Max(0, amount) }; }
        private static EcoJournalLine Cr(string entity, string account, double amount) { return new EcoJournalLine { EntityId = entity, Account = account, Credit = Math.Max(0, amount) }; }
        private static List<T> Active<T>(IEnumerable<T> source) where T : class { return (source ?? Enumerable.Empty<T>()).Where(x => x != null && ActiveStatus(Status(x))).ToList(); }
        private static string Status(object value) { System.Reflection.PropertyInfo p = value == null ? null : value.GetType().GetProperty("Status"); return p == null ? "ACTIVE" : (p.GetValue(value, null) as string ?? "ACTIVE"); }
        private static bool ActiveStatus(string status) { return !Eq(status, "ARCHIVED") && !Eq(status, "DISABLED") && !Eq(status, "RETIRED") && !Eq(status, "REJECTED") && !Eq(status, "BANKRUPT") && !Eq(status, "DEFAULTED") && !Eq(status, "PAID") && !Eq(status, "FULFILLED"); }
        private static bool Blocked(IEnumerable<string> findings) { return (findings ?? Enumerable.Empty<string>()).Any(x => x.StartsWith("BLOCKER", StringComparison.OrdinalIgnoreCase)); }
        private static double P(EconomicModel model, string key, double fallback) { double value; return model != null && model.Parameters != null && model.Parameters.TryGetValue(key, out value) && Finite(value) ? value : fallback; }
        private static double Get(IDictionary<string, double> map, string key) { double value; return map != null && map.TryGetValue(key ?? "", out value) ? value : 0; }
        private static double Normalize(double value, double minimum, double maximum) { return maximum - minimum <= Epsilon ? 0 : Clamp((value - minimum) / (maximum - minimum), 0, 1); }
        private static double Sq(double x) { return x * x; }
        private static double Clamp(double value, double minimum, double maximum) { return Math.Max(minimum, Math.Min(maximum, Finite(value) ? value : minimum)); }
        private static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        private static bool Eq(string a, string b) { return String.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase); }
        private static string R(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static void AppendMap(StringBuilder s, IDictionary<string, double> map) { foreach (KeyValuePair<string, double> x in (map ?? new Dictionary<string, double>()).OrderBy(x => x.Key)) s.Append(":M:").Append(x.Key).Append(':').Append(R(x.Value)); }
        private static void AppendList(StringBuilder s, IEnumerable<string> list) { foreach (string x in (list ?? Enumerable.Empty<string>()).OrderBy(x => x)) s.Append(":E:").Append(x); }
        private static string HashText(string value) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
    }
}

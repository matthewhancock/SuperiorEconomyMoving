namespace api.Quote;

internal sealed record CategoryBreakdown(string Category, int Pieces, double CubicFeet);

internal sealed record QuoteResult {
    public required int TotalPieces { get; init; }
    public required double TotalCubicFeet { get; init; }
    public required int EstimatedWeightLbs { get; init; }

    public required CategoryBreakdown[] CategoryBreakdowns { get; init; }

    public required int CrewSize { get; init; }
    public required double HourlyRate { get; init; }

    public required double LaborManHours { get; init; }
    public required double DriveTimeHours { get; init; }

    /// <summary>Billable total time = max(2, manHours/crew + driveTime), rounded to quarter hour.</summary>
    public required double TotalBillableHours { get; init; }

    public required double FuelSurcharge { get; init; }

    /// <summary>Estimated total: billable hours × hourly rate + fuel surcharge.</summary>
    public required double TotalPrice { get; init; }

    public required ValuationTiers Valuation { get; init; }

    /// <summary>True if any bulky item was in the inventory (forces 3-person minimum).</summary>
    public required bool HasBulkyItems { get; init; }
}

internal sealed record ValuationTiers(
    ValuationTier ReleasedValue,
    ValuationTier ActualCashValue,
    ValuationTier FullValueZeroDeductible,
    ValuationTier FullValue250Deductible,
    ValuationTier FullValue500Deductible
);

internal sealed record ValuationTier(
    string Name,
    string Description,
    double CoverageDollars,
    double Cost
);

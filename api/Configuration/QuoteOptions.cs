using System.Collections.Generic;

namespace api.Configuration;

internal sealed class QuoteOptions {
    public bool InstaQuote { get; init; } = false;
    public string[] Recipients { get; init; } = [];

    public int DriveSpeedMph { get; init; } = 45;
    public int MinimumBillableMiles { get; init; } = 10;
    public bool TwoHourMinimum { get; init; } = true;

    public double FuelGallonsPerMile { get; init; } = 0.2;
    public double FuelPricePerGallon { get; init; } = 2.50;
    public double FuelMinCharge { get; init; } = 15.00;

    public int WeightLbsPerCubicFoot { get; init; } = 7;

    public Dictionary<string, double> HourlyRates { get; init; } = new();

    public ValuationOptions Valuation { get; init; } = new();
}

internal sealed class ValuationOptions {
    public double ActualCashValueMultiplier { get; init; } = 2;
    public double ActualCashValueRate { get; init; } = 0.0075;
    public double FullValueMultiplier { get; init; } = 4;
    public double FullValueZeroDeductibleRate { get; init; } = 0.0095;
    public double FullValue250DeductibleRate { get; init; } = 0.0045;
    public double FullValue500DeductibleRate { get; init; } = 0.0025;
}

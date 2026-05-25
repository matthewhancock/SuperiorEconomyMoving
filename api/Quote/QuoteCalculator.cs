using System;
using System.Collections.Generic;
using System.Linq;
using api.Configuration;

namespace api.Quote;

/// <summary>
/// Port of the legacy <c>instaquote.js</c> calculator. Math is preserved exactly so that
/// historical quotes remain reproducible; the only differences are surfacing the input
/// parameters via <see cref="QuoteOptions"/> (so rates can be updated without redeploy)
/// and returning a strongly-typed <see cref="QuoteResult"/>.
/// </summary>
internal sealed class QuoteCalculator(QuoteOptions options) {
    private readonly QuoteOptions _options = options;
    private readonly ValuationCalculator _valuation = new(options.Valuation);

    public QuoteResult Calculate(QuoteRequest request) {
        // 1. Resolve inventory into per-category breakdowns
        var byCategory = new Dictionary<string, (int Pieces, double CuFt)>(StringComparer.OrdinalIgnoreCase);
        var totalPieces = 0;
        var totalCubicFeet = 0.0;
        var hasBulky = false;

        foreach (var item in request.Items) {
            if (item.Quantity <= 0) continue;
            if (!ItemCatalog.ByKey.TryGetValue(item.Key, out var catalogItem)) continue;

            var pieces = item.Quantity;
            var cuft = catalogItem.CubicFeet * pieces;
            totalPieces += pieces;
            totalCubicFeet += cuft;
            if (catalogItem.IsBulky) hasBulky = true;

            var existing = byCategory.GetValueOrDefault(catalogItem.Category);
            byCategory[catalogItem.Category] = (existing.Pieces + pieces, existing.CuFt + cuft);
        }

        // Boxes are separate from pieces in the man-hours formula but contribute to cubic feet / weight
        var totalSmallMedLargeBoxes = request.SmallBoxes + request.MediumBoxes + request.LargeBoxes;
        var boxCubicFeet =
            request.SmallBoxes    * ItemCatalog.SmallBoxCubicFeet +
            request.MediumBoxes   * ItemCatalog.MediumBoxCubicFeet +
            request.LargeBoxes    * ItemCatalog.LargeBoxCubicFeet +
            request.WardrobeBoxes * ItemCatalog.WardrobeBoxCubicFeet;
        totalCubicFeet += boxCubicFeet;

        var categoryBreakdowns = ItemCatalog.Categories
            .Select(c => {
                var stats = byCategory.GetValueOrDefault(c.Key);
                return new CategoryBreakdown(c.DisplayName, stats.Pieces, stats.CuFt);
            })
            .Where(b => b.Pieces > 0)
            .ToArray();

        var weight = (int)Math.Round(totalCubicFeet * _options.WeightLbsPerCubicFoot);

        // 2. Drive time — round-trip at DriveSpeedMph, doubled per California regulation, rounded to ¼ hour
        var miles = Math.Max(request.Miles, _options.MinimumBillableMiles);
        var driveTime = Math.Round((miles / (double)_options.DriveSpeedMph) * 2 * 4) / 4;

        // 3. Fuel — round-trip miles × gallons/mile × $/gallon, with minimum
        var fuelCharge = Math.Max(
            _options.FuelMinCharge,
            miles * 2 * _options.FuelGallonsPerMile * _options.FuelPricePerGallon
        );

        // 4. Man-hours: (pieces / 3) + (boxes × 0.075 / 3) + (wardrobe × 0.3 / 3)
        var manHours =
            (totalPieces / 3.0) +
            (totalSmallMedLargeBoxes * 0.075 / 3.0) +
            (request.WardrobeBoxes   * 0.300 / 3.0);

        // 5. Crew sizing: enough to keep the move under 9 hours; minimum 2; minimum 3 if any bulky item
        var crewSize = Math.Max(
            hasBulky ? 3 : 2,
            (int)Math.Ceiling(manHours / 9)
        );

        // 6. Look up hourly rate; if exact key isn't configured, fall back to the largest configured rate
        var hourlyRate = LookupHourlyRate(crewSize);

        // 7. Total billable time = man-hours / crew + drive time, rounded to nearest 15 min, with 2-hour minimum
        var rawHours = manHours / crewSize + driveTime;
        var rawMinutes = Math.Round(rawHours * 60 / 15) * 15;
        var billableHours = rawMinutes / 60;
        if (_options.TwoHourMinimum && billableHours < 2) billableHours = 2;

        // 8. Total price
        var totalPrice = Math.Round(billableHours * hourlyRate + fuelCharge, 2);

        // 9. Valuation tiers (insurance options shown on the receipt)
        var valuation = _valuation.Calculate(weight);

        return new QuoteResult {
            TotalPieces = totalPieces,
            TotalCubicFeet = Math.Round(totalCubicFeet, 1),
            EstimatedWeightLbs = weight,
            CategoryBreakdowns = categoryBreakdowns,
            CrewSize = crewSize,
            HourlyRate = hourlyRate,
            LaborManHours = Math.Round(manHours, 2),
            DriveTimeHours = driveTime,
            TotalBillableHours = billableHours,
            FuelSurcharge = Math.Round(fuelCharge, 2),
            TotalPrice = totalPrice,
            Valuation = valuation,
            HasBulkyItems = hasBulky
        };
    }

    private double LookupHourlyRate(int crewSize) {
        // Exact match first
        if (_options.HourlyRates.TryGetValue(crewSize.ToString(), out var rate)) return rate;

        // Fall back to the largest rate configured (handles crews larger than the rate table)
        var maxConfigured = _options.HourlyRates
            .Where(kv => int.TryParse(kv.Key, out _))
            .OrderByDescending(kv => int.Parse(kv.Key))
            .FirstOrDefault();
        return maxConfigured.Value > 0 ? maxConfigured.Value : 0;
    }
}

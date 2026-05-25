using System;
using api.Configuration;

namespace api.Quote;

/// <summary>
/// Insurance / valuation pricing — ported from the legacy <c>viewquote.php</c>.
/// Five tiers are offered to the customer on the receipt; rates and multipliers
/// are configurable via <see cref="ValuationOptions"/>.
/// </summary>
internal sealed class ValuationCalculator(ValuationOptions options) {
    private readonly ValuationOptions _options = options;

    public ValuationTiers Calculate(int weightLbs) {
        // Released Value — California-default minimum, included at no charge ($0.60/lb max coverage per article).
        var released = new ValuationTier(
            Name: "Released Value",
            Description: "Included at no charge. Carrier is liable for up to $0.60 per pound per article (California default).",
            CoverageDollars: 0,
            Cost: 0
        );

        // Actual Cash Value — coverage = weight × multiplier; cost = coverage × rate
        var acvCoverage = weightLbs * _options.ActualCashValueMultiplier;
        var acv = new ValuationTier(
            Name: "Actual Cash Value",
            Description: "Covers depreciated value of damaged items.",
            CoverageDollars: Math.Round(acvCoverage),
            Cost: Math.Round(acvCoverage * _options.ActualCashValueRate, 2)
        );

        // Full Value Protection — coverage = weight × multiplier (typically higher); three deductible tiers
        var fvCoverage = weightLbs * _options.FullValueMultiplier;
        var fv0 = new ValuationTier(
            Name: "Full Value — $0 deductible",
            Description: "Covers full replacement value. No deductible.",
            CoverageDollars: Math.Round(fvCoverage),
            Cost: Math.Round(fvCoverage * _options.FullValueZeroDeductibleRate, 2)
        );
        var fv250 = new ValuationTier(
            Name: "Full Value — $250 deductible",
            Description: "Covers full replacement value with a $250 deductible per claim.",
            CoverageDollars: Math.Round(fvCoverage),
            Cost: Math.Round(fvCoverage * _options.FullValue250DeductibleRate, 2)
        );
        var fv500 = new ValuationTier(
            Name: "Full Value — $500 deductible",
            Description: "Covers full replacement value with a $500 deductible per claim.",
            CoverageDollars: Math.Round(fvCoverage),
            Cost: Math.Round(fvCoverage * _options.FullValue500DeductibleRate, 2)
        );

        return new ValuationTiers(released, acv, fv0, fv250, fv500);
    }
}

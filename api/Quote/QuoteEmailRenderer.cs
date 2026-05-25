using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace api.Quote;

internal static class QuoteEmailRenderer {
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string RenderSubject(QuoteRequest request) {
        var who = !string.IsNullOrWhiteSpace(request.LastName)
            ? $"{request.FirstName} {request.LastName}".Trim()
            : request.FirstName.Trim();
        return $"InstaQuote — {who}";
    }

    /// <summary>Plain-text body sent to staff at <c>Features.Quote.Recipients</c>.</summary>
    public static string RenderBody(QuoteRequest request, QuoteResult result) {
        var sb = new StringBuilder();

        sb.AppendLine("=== Superior Moving Co. — InstaQuote ===");
        sb.AppendLine();

        sb.AppendLine($"Customer:    {request.FirstName} {request.LastName}".TrimEnd());
        sb.AppendLine($"Phone:       {request.Phone}");
        sb.AppendLine($"Email:       {request.Email}");
        if (request.MoveDate is { } d) sb.AppendLine($"Move date:   {d:yyyy-MM-dd (ddd)}");
        sb.AppendLine();

        sb.AppendLine("--- Move ---");
        sb.AppendLine($"From:        {FormatAddress(request.FromStreet, request.FromCity, request.FromState, request.FromZip)}");
        if (HasAny(request.FromHomeType, request.FromBedrooms, request.FromFloor))
            sb.AppendLine($"             {Join(request.FromHomeType, request.FromBedrooms, request.FromFloor)}");
        sb.AppendLine($"To:          {FormatAddress(request.ToStreet, request.ToCity, request.ToState, request.ToZip)}");
        if (HasAny(request.ToHomeType, request.ToBedrooms, request.ToFloor))
            sb.AppendLine($"             {Join(request.ToHomeType, request.ToBedrooms, request.ToFloor)}");
        sb.AppendLine($"Distance:    {request.Miles} mi one-way");
        sb.AppendLine();

        sb.AppendLine("--- Inventory ---");
        foreach (var cat in result.CategoryBreakdowns) {
            sb.AppendLine($"  {cat.Category,-22}  {cat.Pieces,3} pcs   {cat.CubicFeet,6:N1} cu ft");
        }
        var totalBoxes = request.SmallBoxes + request.MediumBoxes + request.LargeBoxes;
        if (totalBoxes > 0 || request.WardrobeBoxes > 0) {
            sb.Append("  Boxes                  ");
            sb.Append($"  {totalBoxes,3} total");
            if (request.SmallBoxes > 0)    sb.Append($" ({request.SmallBoxes} sm");
            if (request.MediumBoxes > 0)   sb.Append($"{(request.SmallBoxes > 0 ? "/" : " (")}{request.MediumBoxes} md");
            if (request.LargeBoxes > 0)    sb.Append($"{(totalBoxes > request.LargeBoxes ? "/" : " (")}{request.LargeBoxes} lg)");
            else if (totalBoxes > 0)       sb.Append(")");
            if (request.WardrobeBoxes > 0) sb.Append($"   + {request.WardrobeBoxes} wardrobe");
            sb.AppendLine();
        }
        sb.AppendLine($"  {"Total",-22}  {result.TotalPieces,3} pcs   {result.TotalCubicFeet,6:N1} cu ft   ~{result.EstimatedWeightLbs:N0} lbs");
        sb.AppendLine();

        sb.AppendLine("--- Quote ---");
        sb.AppendLine($"Crew:        {result.CrewSize} movers @ {result.HourlyRate.ToString("C", Us)}/hr");
        sb.AppendLine($"Labor:       {result.LaborManHours:N2} man-hrs  →  {(result.LaborManHours / result.CrewSize):N2} hrs at crew size");
        sb.AppendLine($"Drive time:  {FormatHoursMinutes(result.DriveTimeHours)}  (CA double drive time applied)");
        sb.AppendLine($"Billable:    {FormatHoursMinutes(result.TotalBillableHours)}");
        sb.AppendLine($"Fuel:        {result.FuelSurcharge.ToString("C", Us)}");
        sb.AppendLine($"TOTAL:       {result.TotalPrice.ToString("C", Us)}");
        if (result.HasBulkyItems) sb.AppendLine("Bulky:       yes (3-person minimum applied)");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.Notes)) {
            sb.AppendLine("--- Notes from customer ---");
            sb.AppendLine(request.Notes);
            sb.AppendLine();
        }

        sb.AppendLine("--- Valuation options shown to customer ---");
        sb.AppendLine($"  Released Value (60¢/lb)                FREE");
        sb.AppendLine($"  ACV @ {result.Valuation.ActualCashValue.CoverageDollars.ToString("C0", Us)} coverage           {result.Valuation.ActualCashValue.Cost.ToString("C", Us)}");
        sb.AppendLine($"  Full Value $0 ded @ {result.Valuation.FullValueZeroDeductible.CoverageDollars.ToString("C0", Us)}    {result.Valuation.FullValueZeroDeductible.Cost.ToString("C", Us)}");
        sb.AppendLine($"  Full Value $250 ded @ {result.Valuation.FullValue250Deductible.CoverageDollars.ToString("C0", Us)}    {result.Valuation.FullValue250Deductible.Cost.ToString("C", Us)}");
        sb.AppendLine($"  Full Value $500 ded @ {result.Valuation.FullValue500Deductible.CoverageDollars.ToString("C0", Us)}    {result.Valuation.FullValue500Deductible.Cost.ToString("C", Us)}");

        return sb.ToString();
    }

    private static string FormatAddress(string street, string city, string state, string zip) {
        var parts = new[] { street, city, state, zip }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }

    private static bool HasAny(params string[] parts) => parts.Any(p => !string.IsNullOrWhiteSpace(p));

    private static string Join(params string[] parts) =>
        string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string FormatHoursMinutes(double hours) {
        var h = (int)Math.Floor(hours);
        var m = (int)Math.Round((hours - h) * 60);
        return m == 0 ? $"{h} hr" : $"{h} hr {m} min";
    }
}

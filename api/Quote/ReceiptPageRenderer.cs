using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace api.Quote;

/// <summary>
/// Renders the customer-facing quote receipt as a full HTML page using the existing site
/// header/footer/styles. Self-contained — does not depend on the InstaQuote app shell so the
/// page is printable and can be opened standalone.
/// </summary>
internal static class ReceiptPageRenderer {
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string Render(QuoteRequest request, QuoteResult result, bool emailSent) {
        var name = $"{request.FirstName} {request.LastName}".Trim();
        var moveDate = request.MoveDate is { } d ? d.ToString("MMMM d, yyyy") : "TBD";

        var sb = new StringBuilder();
        sb.Append("""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="robots" content="noindex, nofollow" />
  <title>Your InstaQuote | Superior Moving</title>
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
  <link href="https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;1,9..40,400&family=Instrument+Serif:ital@0;1&display=swap" rel="stylesheet" />
  <link rel="stylesheet" href="/css/styles.css" />
  <link rel="icon" href="/img/logo-transparent.png" />
</head>
<body>
  <header class="site-header">
    <div class="container header__row">
      <a class="logo" href="/"><img class="logo__img" src="/img/logo-transparent.png" alt="Superior Moving Co. logo" width="56" height="56" /><span class="logo__text"><span class="logo__name">Superior Moving</span><span class="logo__tag">Your InstaQuote</span></span></a>
      <nav class="nav"><div class="nav__cta"><a class="btn btn--ghost" href="javascript:window.print()">Print</a><a class="btn btn--primary" href="tel:+18188846125">Call (818) 884-6125</a></div></nav>
    </div>
  </header>

  <main class="article-page container">
""");

        sb.Append("    <h1>Your InstaQuote</h1>\n");
        sb.Append($"    <p style=\"margin-top:-.5rem;color:var(--text-muted)\">Generated {DateTime.Now.ToString("MMMM d, yyyy 'at' h:mm tt", Us)} for <strong>{Html(name)}</strong>.</p>\n\n");

        if (emailSent) {
            sb.Append("    <div class=\"callout\" style=\"margin-bottom:2rem\"><p>A copy of this quote has been sent to our team. We'll be in touch within one business day to confirm your dates and answer any questions.</p></div>\n\n");
        }

        // Move details
        sb.Append("    <h2>Move details</h2>\n    <table class=\"data-table\"><tbody>\n");
        sb.Append($"      <tr><th style=\"width:30%\">Move date</th><td>{Html(moveDate)}</td></tr>\n");
        sb.Append($"      <tr><th>From</th><td>{Html(FormatAddress(request.FromStreet, request.FromCity, request.FromState, request.FromZip))}{FormatDetailRow(request.FromHomeType, request.FromBedrooms, request.FromFloor)}</td></tr>\n");
        sb.Append($"      <tr><th>To</th><td>{Html(FormatAddress(request.ToStreet, request.ToCity, request.ToState, request.ToZip))}{FormatDetailRow(request.ToHomeType, request.ToBedrooms, request.ToFloor)}</td></tr>\n");
        sb.Append($"      <tr><th>One-way distance</th><td>{request.Miles:N0} mi</td></tr>\n");
        sb.Append($"      <tr><th>Contact</th><td>{Html(request.Phone)}{(string.IsNullOrEmpty(request.Email) ? "" : $" · {Html(request.Email)}")}</td></tr>\n");
        sb.Append("    </tbody></table>\n\n");

        // Inventory
        sb.Append("    <h2>Inventory summary</h2>\n    <table class=\"data-table\"><thead><tr><th>Category</th><th style=\"text-align:right\">Pieces</th><th style=\"text-align:right\">Cubic feet</th></tr></thead><tbody>\n");
        foreach (var cat in result.CategoryBreakdowns) {
            sb.Append($"      <tr><td>{Html(cat.Category)}</td><td style=\"text-align:right\">{cat.Pieces}</td><td style=\"text-align:right\">{cat.CubicFeet:N1}</td></tr>\n");
        }
        var totalBoxes = request.SmallBoxes + request.MediumBoxes + request.LargeBoxes + request.WardrobeBoxes;
        if (totalBoxes > 0) {
            var boxDetails = new List<string>();
            if (request.SmallBoxes > 0) boxDetails.Add($"{request.SmallBoxes} small");
            if (request.MediumBoxes > 0) boxDetails.Add($"{request.MediumBoxes} medium");
            if (request.LargeBoxes > 0) boxDetails.Add($"{request.LargeBoxes} large");
            if (request.WardrobeBoxes > 0) boxDetails.Add($"{request.WardrobeBoxes} wardrobe");
            var boxCuFt = request.SmallBoxes * ItemCatalog.SmallBoxCubicFeet +
                          request.MediumBoxes * ItemCatalog.MediumBoxCubicFeet +
                          request.LargeBoxes * ItemCatalog.LargeBoxCubicFeet +
                          request.WardrobeBoxes * ItemCatalog.WardrobeBoxCubicFeet;
            sb.Append($"      <tr><td>Boxes <span style=\"color:var(--text-muted)\">({Html(string.Join(", ", boxDetails))})</span></td><td style=\"text-align:right\">{totalBoxes}</td><td style=\"text-align:right\">{boxCuFt:N1}</td></tr>\n");
        }
        sb.Append($"      <tr><th>Total</th><th style=\"text-align:right\">{result.TotalPieces + totalBoxes}</th><th style=\"text-align:right\">{result.TotalCubicFeet:N1}</th></tr>\n");
        sb.Append($"      <tr><td colspan=\"3\" style=\"font-size:.85rem;color:var(--text-muted)\">Estimated weight: ~{result.EstimatedWeightLbs:N0} lbs</td></tr>\n");
        sb.Append("    </tbody></table>\n\n");

        // Pricing
        sb.Append("    <h2>Your price</h2>\n    <table class=\"data-table\"><tbody>\n");
        sb.Append($"      <tr><th style=\"width:60%\">Crew</th><td>{result.CrewSize} movers</td></tr>\n");
        sb.Append($"      <tr><th>Hourly rate</th><td>{result.HourlyRate.ToString("C", Us)} / hour</td></tr>\n");
        sb.Append($"      <tr><th>Drive time <span style=\"font-weight:400;color:var(--text-muted)\">(California double drive time per P.U.C. tariff)</span></th><td>{FormatHoursMinutes(result.DriveTimeHours)}</td></tr>\n");
        sb.Append($"      <tr><th>Total billable time</th><td>{FormatHoursMinutes(result.TotalBillableHours)}</td></tr>\n");
        sb.Append($"      <tr><th>Fuel surcharge</th><td>{result.FuelSurcharge.ToString("C", Us)}</td></tr>\n");
        sb.Append($"      <tr style=\"font-size:1.15rem\"><th>Estimated total</th><td style=\"font-weight:700;color:var(--accent)\">{result.TotalPrice.ToString("C", Us)}</td></tr>\n");
        sb.Append("    </tbody></table>\n\n");
        sb.Append("    <p style=\"font-size:.875rem;color:var(--text-muted);margin-top:.5rem\">This estimate is based on the inventory you provided. The price you actually pay is the lesser of this estimate or the actual time required (with the agreed-upon hourly rate); if your inventory is significantly larger than what's listed, we'll re-quote on the spot before loading.</p>\n\n");

        // Valuation
        sb.Append("    <h2>Valuation options</h2>\n");
        sb.Append("    <p>California requires us to offer the choices below. You can update your selection any time before the move; the default is Released Value.</p>\n");
        sb.Append("    <table class=\"data-table\"><thead><tr><th>Option</th><th style=\"text-align:right\">Coverage</th><th style=\"text-align:right\">Cost</th></tr></thead><tbody>\n");
        sb.Append($"      <tr><td><strong>{Html(result.Valuation.ReleasedValue.Name)}</strong><br/><span style=\"font-size:.85rem;color:var(--text-muted)\">{Html(result.Valuation.ReleasedValue.Description)}</span></td><td style=\"text-align:right\">$0.60/lb per article</td><td style=\"text-align:right\">FREE</td></tr>\n");
        sb.Append(RenderValuationRow(result.Valuation.ActualCashValue));
        sb.Append(RenderValuationRow(result.Valuation.FullValueZeroDeductible));
        sb.Append(RenderValuationRow(result.Valuation.FullValue250Deductible));
        sb.Append(RenderValuationRow(result.Valuation.FullValue500Deductible));
        sb.Append("    </tbody></table>\n\n");

        // Notes
        if (!string.IsNullOrWhiteSpace(request.Notes)) {
            sb.Append("    <h2>Notes you shared</h2>\n");
            sb.Append($"    <p style=\"background:var(--bg-alt);padding:1rem 1.25rem;border-radius:var(--radius-sm);white-space:pre-wrap\">{Html(request.Notes)}</p>\n\n");
        }

        sb.Append("    <div class=\"page-hero__actions\" style=\"margin-top:2.5rem\">\n");
        sb.Append("      <a class=\"btn btn--primary btn--lg\" href=\"javascript:window.print()\">Print this quote</a>\n");
        sb.Append("      <a class=\"btn btn--outline btn--lg\" href=\"tel:+18188846125\">Call (818) 884-6125</a>\n");
        sb.Append("      <a class=\"btn btn--ghost btn--lg\" href=\"/\">Back to home</a>\n");
        sb.Append("    </div>\n");
        sb.Append("  </main>\n");

        sb.Append("""
  <footer class="site-footer">
    <div class="container site-footer__bottom"><p>© Superior Moving Co. · <a href="tel:+18188846125" style="color:#cbd5e1">(818) 884-6125</a></p></div>
  </footer>
</body>
</html>
""");

        return sb.ToString();
    }

    private static string RenderValuationRow(ValuationTier tier) {
        var sb = new StringBuilder();
        sb.Append($"      <tr><td><strong>{Html(tier.Name)}</strong><br/><span style=\"font-size:.85rem;color:var(--text-muted)\">{Html(tier.Description)}</span></td>");
        sb.Append($"<td style=\"text-align:right\">{tier.CoverageDollars.ToString("C0", Us)}</td>");
        sb.Append($"<td style=\"text-align:right\">{tier.Cost.ToString("C", Us)}</td></tr>\n");
        return sb.ToString();
    }

    private static string FormatAddress(string street, string city, string state, string zip) {
        var parts = new[] { street, city, state, zip }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }

    private static string FormatDetailRow(string homeType, string bedrooms, string floor) {
        var parts = new[] { homeType, bedrooms, floor }.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return parts.Length == 0 ? "" : $"<br/><span style=\"font-size:.85rem;color:var(--text-muted)\">{Html(string.Join(" · ", parts))}</span>";
    }

    private static string FormatHoursMinutes(double hours) {
        var h = (int)Math.Floor(hours);
        var m = (int)Math.Round((hours - h) * 60);
        return m == 0 ? $"{h} hr" : $"{h} hr {m} min";
    }

    private static string Html(string? s) => WebUtility.HtmlEncode(s ?? "");
}

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace api.Quote;

/// <summary>
/// Parses an <see cref="IFormCollection"/> into a strongly-typed <see cref="QuoteRequest"/>.
/// The InstaQuote form submits one input per catalog item plus separate fields for boxes,
/// addresses, contact info, and notes.
/// </summary>
internal static class QuoteFormBinder {
    public static QuoteRequest Bind(IFormCollection form) {
        var items = new List<QuoteItemQuantity>(ItemCatalog.Items.Length);
        foreach (var item in ItemCatalog.Items) {
            var qty = ParseInt(form, item.Key);
            if (qty > 0) items.Add(new QuoteItemQuantity(item.Key, qty));
        }

        return new QuoteRequest {
            FirstName = form["first_name"].ToString().Trim(),
            LastName = form["last_name"].ToString().Trim(),
            Phone = form["phone"].ToString().Trim(),
            Email = form["email"].ToString().Trim(),
            MoveDate = ParseDate(form["move_date"]),

            FromStreet = form["from_street"].ToString().Trim(),
            FromCity = form["from_city"].ToString().Trim(),
            FromState = ValueOrDefault(form["from_state"], "CA"),
            FromZip = form["from_zip"].ToString().Trim(),
            FromHomeType = form["from_home_type"].ToString().Trim(),
            FromBedrooms = form["from_bedrooms"].ToString().Trim(),
            FromFloor = form["from_floor"].ToString().Trim(),

            ToStreet = form["to_street"].ToString().Trim(),
            ToCity = form["to_city"].ToString().Trim(),
            ToState = ValueOrDefault(form["to_state"], "CA"),
            ToZip = form["to_zip"].ToString().Trim(),
            ToHomeType = form["to_home_type"].ToString().Trim(),
            ToBedrooms = form["to_bedrooms"].ToString().Trim(),
            ToFloor = form["to_floor"].ToString().Trim(),

            Miles = ParseInt(form, "miles"),

            Items = [.. items],
            SmallBoxes = ParseInt(form, "box_small"),
            MediumBoxes = ParseInt(form, "box_medium"),
            LargeBoxes = ParseInt(form, "box_large"),
            WardrobeBoxes = ParseInt(form, "box_wardrobe"),

            Notes = form["notes"].ToString().Trim()
        };
    }

    private static int ParseInt(IFormCollection form, string key) {
        var raw = form[key].ToString();
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : 0;
    }

    private static DateOnly? ParseDate(string? raw) =>
        DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static string ValueOrDefault(string? raw, string fallback) =>
        string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
}

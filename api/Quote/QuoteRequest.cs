using System;

namespace api.Quote;

internal sealed record QuoteItemQuantity(string Key, int Quantity);

internal sealed record QuoteRequest {
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; }
    public required string Email { get; init; }

    public DateOnly? MoveDate { get; init; }

    public string FromStreet { get; init; } = "";
    public string FromCity { get; init; } = "";
    public string FromState { get; init; } = "CA";
    public string FromZip { get; init; } = "";
    public string FromHomeType { get; init; } = "";
    public string FromBedrooms { get; init; } = "";
    public string FromFloor { get; init; } = "";

    public string ToStreet { get; init; } = "";
    public string ToCity { get; init; } = "";
    public string ToState { get; init; } = "CA";
    public string ToZip { get; init; } = "";
    public string ToHomeType { get; init; } = "";
    public string ToBedrooms { get; init; } = "";
    public string ToFloor { get; init; } = "";

    public int Miles { get; init; }

    public QuoteItemQuantity[] Items { get; init; } = [];

    public int SmallBoxes { get; init; }
    public int MediumBoxes { get; init; }
    public int LargeBoxes { get; init; }
    public int WardrobeBoxes { get; init; }

    public string Notes { get; init; } = "";
}

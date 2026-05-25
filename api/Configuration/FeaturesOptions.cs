namespace api.Configuration;

internal sealed class FeaturesOptions {
    public const string SectionName = "Features";

    public QuoteOptions Quote { get; init; } = new();
    public RedirectOptions[] Redirects { get; init; } = [];
}

namespace api.Configuration;

internal sealed class RedirectOptions {
    public string[] Sources { get; init; } = [];
    public string Target { get; init; } = "";
}

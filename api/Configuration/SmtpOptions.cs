namespace api.Configuration;

internal sealed class SmtpOptions {
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseStartTls { get; init; } = true;
    public string From { get; init; } = "";
    public string FromName { get; init; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

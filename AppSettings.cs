namespace NetstatSshParsing;

public record AppSettings
{
    public required string Host { get; set; }

    public required string Login { get; set; }

    public required string Password { get; set; }
}

namespace TravelPax.Workforce.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "TravelPax Workforce";
    public int MaxAttempts { get; set; } = 5;
    public int PollIntervalSeconds { get; set; } = 20;
    public int BatchSize { get; set; } = 20;
}

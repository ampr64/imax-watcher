namespace ImaxWatcher.Models;

public sealed record WatchAlert(
    string Cinema,
    string Movie,
    DateTimeOffset DetectedAt,
    IReadOnlyList<Showing> Availabilities);

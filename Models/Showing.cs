namespace ImaxWatcher.Models;

public sealed record Showing(
    DateOnly Date,
    string Format,
    IReadOnlyList<TimeOnly> Times,
    string BookingUrl);

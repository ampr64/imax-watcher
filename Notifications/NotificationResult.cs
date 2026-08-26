namespace ImaxWatcher.Notifications;

public sealed record NotificationResult(
    string NotifierName,
    bool Success,
    Exception? Error = null);

public sealed record NotificationBatchResult(
    IReadOnlyList<NotificationResult> Results)
{
    public int EnabledCount => Results.Count;

    public int SuccessCount => Results.Count(x => x.Success);

    public int FailureCount => Results.Count(x => !x.Success);

    public bool AnySucceeded => Results.Any(x => x.Success);

    public bool AllSucceeded =>
        Results.Count > 0 &&
        Results.All(x => x.Success);

    public IReadOnlyList<NotificationResult> Successful =>
        [.. Results.Where(x => x.Success)];

    public IReadOnlyList<NotificationResult> Failed =>
        [.. Results.Where(x => !x.Success)];
}
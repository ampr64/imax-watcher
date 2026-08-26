using Microsoft.Extensions.Logging;
using ImaxWatcher.Models;
using ImaxWatcher.Notifications;
using ImaxWatcher.Options;

namespace ImaxWatcher.Services;

public sealed class WatcherRunner(
    IShowcaseReader watcher,
    NotificationService notifications,
    LocalAlertService localAlert,
    WatcherOptions options,
    ILogger<WatcherRunner> logger)
{
    private readonly IShowcaseReader _watcher = watcher;
    private readonly NotificationService _notifications = notifications;
    private readonly LocalAlertService _localAlert = localAlert;
    private readonly WatcherOptions _options = options;
    private readonly ILogger<WatcherRunner> _logger = logger;

    /// <summary>Showings already delivered by some remote channel. Never re-sent.</summary>
    private readonly HashSet<ShowingKey> _notified = [];

    /// <summary>Showings that already fired a local alert. Avoids repeat beeps and tabs.</summary>
    private readonly HashSet<ShowingKey> _locallyAlerted = [];

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        PrintHeader();

        if (args.Contains("--test-notifications", StringComparer.OrdinalIgnoreCase))
        {
            await TestNotificationsAsync(cancellationToken);
            return;
        }

        if (args.Contains("--once", StringComparer.OrdinalIgnoreCase))
        {
            await RunOnceAsync(cancellationToken);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var matches = await _watcher.CheckAsync(cancellationToken);

                var pending = matches
                    .Where(match => !_notified.Contains(ShowingKey.Of(match)))
                    .ToArray();

                if (pending.Length > 0)
                {
                    var delivered = await HandleMatchAsync(pending, cancellationToken);

                    if (delivered)
                    {
                        foreach (var match in pending)
                            _notified.Add(ShowingKey.Of(match));

                        if (_options.StopAfterFirstMatch)
                            return;
                    }
                    else
                    {
                        // Not marked as notified, so the next cycle retries them.
                        _logger.LogWarning(
                            "No salió ninguna notificación remota. Reintento en el próximo ciclo.");
                    }
                }
                else if (matches.Count > 0)
                {
                    _logger.LogInformation(
                        "Sin novedades: las {Count} funciones detectadas ya fueron avisadas.",
                        matches.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "Sin funciones de {Movie} posteriores al {Cutoff:dd/MM/yyyy} con horarios disponibles.",
                        _options.Movie,
                        _options.CutoffDate);
                }

                var delay = Random.Shared.Next(_options.PollMinSeconds, _options.PollMaxSeconds + 1);

                if (!await DelayAsync(TimeSpan.FromSeconds(delay), cancellationToken))
                    return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el chequeo de Showcase. Reintento en {Seconds}s.", _options.ErrorRetrySeconds);

                // Note: this await sits inside the catch, so a cancellation here is not
                // caught by the catch(OperationCanceledException) clause above.
                if (!await DelayAsync(TimeSpan.FromSeconds(_options.ErrorRetrySeconds), cancellationToken))
                    return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var matches = await _watcher.CheckAsync(cancellationToken);

        if (matches.Count == 0)
        {
            _logger.LogInformation(
                "Sin funciones de {Movie} posteriores al {Cutoff:dd/MM/yyyy} con horarios disponibles.",
                _options.Movie,
                _options.CutoffDate);
            return;
        }

        PrintMatches(matches);
    }

    /// <returns>
    /// <c>true</c> if the alert was delivered: some remote channel answered OK, or no
    /// remote channel is enabled and the local alert is the only delivery.
    /// </returns>
    private async Task<bool> HandleMatchAsync(
        IReadOnlyList<Showing> matches,
        CancellationToken cancellationToken)
    {
        PrintMatches(matches);

        var alert = new WatchAlert(
            _options.Cinema,
            _options.Movie,
            DateTimeOffset.Now,
            matches);

        var notificationTask =
            _notifications.SendAllAsync(alert, cancellationToken);

        // The local alert blocks for ~3s, so it runs while the sends are in flight.
        // Once per showing only: if a send fails and we retry, we don't repeat the beeps
        // or open another browser tab.
        var newForLocalAlert = matches
            .Where(match => _locallyAlerted.Add(ShowingKey.Of(match)))
            .ToArray();

        if (newForLocalAlert.Length > 0)
            _localAlert.Fire(newForLocalAlert[0].BookingUrl);

        var result = await notificationTask;

        return result.EnabledCount == 0 || result.AnySucceeded;
    }

    private async Task TestNotificationsAsync(CancellationToken cancellationToken)
    {
        var fakeDate = _options.CutoffDate.AddDays(1);
        var fake = new Showing(
            fakeDate,
            "FORMATO DE PRUEBA",
            [new TimeOnly(19, 0), new TimeOnly(22, 35)],
            _options.BookingUrl);

        var alert = new WatchAlert(
            _options.Cinema,
            _options.Movie,
            DateTimeOffset.Now,
            [fake]);

        _logger.LogInformation("Enviando notificaciones de prueba...");
        await _notifications.SendAllAsync(alert, cancellationToken);
    }

    private void PrintHeader()
    {
        Console.WriteLine("============================================================");
        Console.WriteLine($"  {_options.Movie} - {_options.Cinema}");
        Console.WriteLine("============================================================");
        Console.WriteLine($"Corte: {_options.CutoffDate:dd/MM/yyyy} (avisa sólo por fechas posteriores)");
        Console.WriteLine($"Intervalo: {_options.PollMinSeconds}-{_options.PollMaxSeconds}s");
        Console.WriteLine($"Browser: {(_options.Headless ? "headless" : "visible")}");
        Console.WriteLine("Ctrl+C para detener.");
        Console.WriteLine();
    }

    private static void PrintMatches(IReadOnlyList<Showing> matches)
    {
        var originalColor = Console.ForegroundColor;

        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("FUNCIONES ENCONTRADAS:");

            foreach (var match in matches)
            {
                Console.WriteLine(
                    $"  {match.Date:dd/MM/yyyy} | {match.Format} | " +
                    string.Join(", ", match.Times.Select(t => t.ToString("HH:mm"))));
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <returns><c>false</c> if the wait was interrupted by cancellation.</returns>
    private static async Task<bool> DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(duration, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Identity of a showing for the purpose of deduplicating alerts.</summary>
    private readonly record struct ShowingKey(DateOnly Date, string Format)
    {
        public static ShowingKey Of(Showing availability) =>
            new(availability.Date, availability.Format);
    }
}

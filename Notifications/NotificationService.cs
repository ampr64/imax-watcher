using Microsoft.Extensions.Logging;
using ImaxWatcher.Models;

namespace ImaxWatcher.Notifications;

public sealed class NotificationService(
    IEnumerable<INotifier> notifiers,
    ILogger<NotificationService> logger)
{
    public async Task<NotificationBatchResult> SendAllAsync(
        WatchAlert alert,
        CancellationToken cancellationToken)
    {
        var enabledNotifiers = notifiers
            .Where(notifier => notifier.IsEnabled)
            .ToArray();

        if (enabledNotifiers.Length == 0)
        {
            logger.LogWarning(
                "No hay notificadores remotos habilitados.");

            return new([]);
        }

        var tasks = enabledNotifiers
            .Select(
                notifier => SendAsync(
                    notifier,
                    alert,
                    cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successful = results.Count(result => result.Success);
        var failed = results.Length - successful;

        if (failed == 0)
        {
            logger.LogInformation(
                "Todas las notificaciones se enviaron correctamente. Total: {Count}.",
                successful);
        }
        else if (successful > 0)
        {
            logger.LogWarning(
                "Notificaciones completadas parcialmente. OK: {Successful}. Fallidas: {Failed}.",
                successful,
                failed);
        }
        else
        {
            logger.LogError(
                "Fallaron todas las notificaciones remotas. Total: {Count}.",
                failed);
        }

        return new NotificationBatchResult(results);
    }

    private async Task<NotificationResult> SendAsync(
        INotifier notifier,
        WatchAlert alert,
        CancellationToken cancellationToken)
    {
        try
        {
            await notifier.SendAsync(
                alert,
                cancellationToken);

            logger.LogInformation(
                "{Notifier}: enviado OK.",
                notifier.Name);

            return new NotificationResult(
                notifier.Name,
                Success: true);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falló {Notifier}.",
                notifier.Name);

            return new NotificationResult(
                notifier.Name,
                Success: false,
                Error: ex);
        }
    }
}
using Microsoft.Extensions.Logging;
using ImaxWatcher.Options;
using ImaxWatcher.Services.Browser;
using ImaxWatcher.Services.Sound;

namespace ImaxWatcher.Services;

public sealed class LocalAlertService(
    WatcherOptions options,
    IBrowserLauncher browserLauncher,
    IAlertSoundPlayer soundPlayer,
    ILogger<LocalAlertService> logger)
{
    public void Fire(string bookingUrl)
    {
        PrintBanner();
        TryOpenBrowser(bookingUrl);
        TryPlayAlertSound();
    }

    private void TryOpenBrowser(string bookingUrl)
    {
        if (!options.OpenBrowserOnMatch)
        {
            return;
        }

        try
        {
            browserLauncher.Open(bookingUrl);

            logger.LogInformation(
                "Se abrió la página de compra en el navegador.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "No se pudo abrir automáticamente la página de compra: {Url}",
                bookingUrl);
        }
    }

    private void TryPlayAlertSound()
    {
        try
        {
            soundPlayer.Play();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo reproducir la alarma sonora.");
        }
    }

    private void PrintBanner()
    {
        var originalColor =
            Console.ForegroundColor;

        var headline = $"🚨 {options.Movie} - {options.Cinema} DISPONIBLE 🚨";
        var rule = new string('#', Math.Max(headline.Length + 8, 60));

        try
        {
            Console.ForegroundColor =
                ConsoleColor.Green;

            Console.WriteLine();
            Console.WriteLine(rule);
            Console.WriteLine($"### {headline.PadRight(rule.Length - 8)} ###");
            Console.WriteLine(rule);
        }
        finally
        {
            Console.ForegroundColor =
                originalColor;
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ImaxWatcher.Notifications;
using ImaxWatcher.Options;
using ImaxWatcher.Services;
using ImaxWatcher.Services.Browser;
using ImaxWatcher.Services.Sound;

namespace ImaxWatcher.Extensions;

internal static class DependencyInjectionExtensions
{
    internal static IServiceCollection AddAppServices(
        this IServiceCollection services)
    {
        services
            .AddSingleton<IShowcaseReader, ShowcaseReader>()
            .AddSingleton<INotifier, GmailNotifier>()
            .AddSingleton<NotificationService>()
            .AddSingleton<WatcherRunner>();

        return services;
    }

    internal static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<WatcherOptions>(configuration, WatcherOptions.SectionName)
            .AddValidatedOptions<EmailOptions>(configuration, EmailOptions.SectionName)
            .AddValidatedOptions<TelegramOptions>(configuration, TelegramOptions.SectionName)
            .AddValidatedOptions<WhatsAppOptions>(configuration, WhatsAppOptions.SectionName);

        return services;
    }

    internal static IServiceCollection AddLocalAlerts(
        this IServiceCollection services)
    {
        services.AddSingleton<LocalAlertService>();

        AddBrowserLauncher(services);
        AddAlertSoundPlayer(services);

        return services;
    }

    internal static IServiceCollection AddTelegram(
        this IServiceCollection services)
    {
        services.AddHttpClient(TelegramNotifier.HttpClientName, (sp, client) =>
        {
            var telegramOptions = sp.GetRequiredService<TelegramOptions>();

            // The trailing slash is required: without it a relative URI replaces the
            // last path segment, so "sendMessage" would resolve without the token.
            client.BaseAddress = new Uri(
                $"{telegramOptions.BotApiUrl}{telegramOptions.BotToken}/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        return services.AddSingleton<INotifier, TelegramNotifier>();
    }

    internal static IServiceCollection AddWhatsApp(
        this IServiceCollection services)
    {
        services.AddHttpClient(TwilioWhatsAppNotifier.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        return services.AddSingleton<INotifier, TwilioWhatsAppNotifier>();
    }

    private static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOptions>>().Value);

        return services;
    }

    private static void AddBrowserLauncher(
        IServiceCollection services)
    {
        services.AddSingleton<IBrowserLauncher>(_ =>
        {
            return _ switch
            {
                _ when OperatingSystem.IsWindows() => new WindowsBrowserLauncher(),
                _ when OperatingSystem.IsMacOS() => new MacOsBrowserLauncher(),
                _ when OperatingSystem.IsLinux() => new LinuxBrowserLauncher(),
                _ => new UnsupportedBrowserLauncher(),
            };
        });
    }

    private static void AddAlertSoundPlayer(
        IServiceCollection services)
    {
        services.AddSingleton<IAlertSoundPlayer>(sp =>
        {
            return OperatingSystem.IsWindows()
                ? new WindowsAlertSoundPlayer()
                : new NoOpAlertSoundPlayer();
        });
    }
}
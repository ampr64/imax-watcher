using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImaxWatcher.Extensions;
using ImaxWatcher.Notifications;
using ImaxWatcher.Services;

// Without this, appsettings.json is resolved against the current working directory
// rather than next to the executable, so running the watcher from any other folder
// fails with missing configuration.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory
});
var services = builder.Services;
var configuration = builder.Configuration;

configuration.AddUserSecrets<Program>(optional: true);
configuration.AddEnvironmentVariables(prefix: "IMAX_");

if (args.Contains("--headed", StringComparer.OrdinalIgnoreCase))
    configuration["Watcher:Headless"] = "false";

builder.Logging.ClearProviders();
// The Telegram token travels in the URL path, and HttpClientFactory logs the full URI
// at Information. Silencing this category keeps the token out of the console.
builder.Logging.AddFilter(
    $"System.Net.Http.HttpClient.{TelegramNotifier.HttpClientName}",
    LogLevel.Warning);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

services
    .AddAppOptions(configuration)
    .AddAppServices()
    .AddLocalAlerts()
    .AddTelegram()
    .AddWhatsApp();

var host = builder.Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var runner = host.Services.GetRequiredService<WatcherRunner>();
    await runner.RunAsync(args, cts.Token);
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    // Cancelling mid-await surfaces here; cancelling during a poll delay instead
    // returns normally. Both paths are reported from the finally below.
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine("Configuración inválida:");

    foreach (var failure in ex.Failures)
        Console.Error.WriteLine($"  - {failure}");

    Environment.ExitCode = 1;
}
finally
{
    if (cts.IsCancellationRequested)
        Console.WriteLine("Detenido.");

    if (host is IAsyncDisposable asyncDisposable)
        await asyncDisposable.DisposeAsync();
    else
        host.Dispose();
}

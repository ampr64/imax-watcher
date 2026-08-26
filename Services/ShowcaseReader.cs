using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ImaxWatcher.Models;
using ImaxWatcher.Options;
using ImaxWatcher.Services.Parsing;

namespace ImaxWatcher.Services;

public sealed class ShowcaseReader(WatcherOptions options, ILogger<ShowcaseReader> logger) : IShowcaseReader, IAsyncDisposable
{
    private readonly WatcherOptions _options = options;
    private readonly ILogger<ShowcaseReader> _logger = logger;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public async Task<IReadOnlyList<Showing>> CheckAsync(CancellationToken cancellationToken)
    {
        var page = await GetPageAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await page.GotoAsync(_options.BookingUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = _options.NavigationTimeoutSeconds * 1000
        });

        await SettleAsync(page, cancellationToken);

        var cinema = ShowcaseParsing.FindOption(
            await GetOptionsAsync(page, _options.Selectors.Cinema),
            _options.Cinema);

        if (cinema is null)
            throw new InvalidOperationException($"No encontré el cine '{_options.Cinema}'.");

        await SelectAsync(page, _options.Selectors.Cinema, cinema, cancellationToken);

        var movieOptions = await GetOptionsAsync(page, _options.Selectors.Movie);
        var movie = ShowcaseParsing.FindOption(movieOptions, _options.Movie);

        if (movie is null)
        {
            throw new InvalidOperationException(
                $"No encontré '{_options.Movie}'. Películas visibles: {string.Join(" | ", movieOptions.Select(x => x.Text))}");
        }

        await SelectAsync(page, _options.Selectors.Movie, movie, cancellationToken);

        var formats = (await GetOptionsAsync(page, _options.Selectors.Format))
            .Where(ShowcaseParsing.IsRealOption)
            .ToArray();

        if (formats.Length == 0)
            throw new InvalidOperationException(
                $"'{_options.Movie}' no tiene formatos disponibles en '{_options.Cinema}'.");

        var results = new List<Showing>();

        foreach (var format in formats)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SelectAsync(page, _options.Selectors.Format, format, cancellationToken);

            var dayOptions = (await GetOptionsAsync(page, _options.Selectors.Day))
                .Where(ShowcaseParsing.IsRealOption)
                .Select(x => new { Option = x, Date = ShowcaseParsing.TryParseDate(x.Text) })
                .ToArray();

            // If Showcase changes its date format, everything is filtered out silently and
            // the watcher reports "no showings" forever. Make that visible.
            var unparsed = dayOptions
                .Where(x => x.Date is null)
                .Select(x => x.Option.Text)
                .ToArray();

            if (unparsed.Length > 0)
            {
                _logger.LogWarning(
                    "No pude interpretar {Count} de {Total} opciones de día para {Format}: {Texts}. " +
                    "Puede que Showcase haya cambiado el formato de fecha.",
                    unparsed.Length,
                    dayOptions.Length,
                    format.Text,
                    string.Join(" | ", unparsed));
            }

            var days = dayOptions
                .Where(x => x.Date is not null && x.Date.Value > _options.CutoffDate)
                .OrderBy(x => x.Date)
                .ToArray();

            _logger.LogDebug(
                "{Format}: {Total} días leídos, {Parsed} interpretados, {Past} posteriores al corte.",
                format.Text,
                dayOptions.Length,
                dayOptions.Length - unparsed.Length,
                days.Length);

            foreach (var day in days)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SelectAsync(page, _options.Selectors.Day, day.Option, cancellationToken);

                var times = await FindShowtimesAsync(page);

                if (times.Count == 0)
                {
                    _logger.LogWarning(
                        "Apareció {Date} para {Format}, pero no pude extraer horarios comprables; no disparo alerta todavía.",
                        day.Date!.Value,
                        format.Text);
                    continue;
                }

                results.Add(new Showing(
                    day.Date!.Value,
                    format.Text,
                    times,
                    _options.BookingUrl));
            }
        }

        return [.. results
            .GroupBy(x => new { x.Date, x.Format })
            .Select(g => g.First())
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Format)];
    }

    private async Task<IPage> GetPageAsync(CancellationToken cancellationToken)
    {
        // Reusing the page between checks is deliberate (latency), but if Chromium died we
        // have to rebuild: otherwise every check fails against a dead handle and the watcher
        // retries forever without ever recovering.
        if (_page is { IsClosed: false } alive && _browser is { IsConnected: true })
            return alive;

        if (_page is not null)
            _logger.LogWarning("El navegador dejó de responder. Relanzando Chromium.");

        await DisposeBrowserAsync();

        cancellationToken.ThrowIfCancellationRequested();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36"
        });

        _page = await _context.NewPageAsync();
        return _page;
    }

    private async Task SelectAsync(
        IPage page,
        string selector,
        DomOption option,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(option.Value))
        {
            await page.SelectOptionAsync(selector, new SelectOptionValue { Value = option.Value });
        }
        else
        {
            await page.SelectOptionAsync(selector, new SelectOptionValue { Label = option.Text });
        }

        await SettleAsync(page, cancellationToken);
    }

    private async Task SettleAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await page.WaitForTimeoutAsync(_options.SettleDelayMilliseconds);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task<IReadOnlyList<DomOption>> GetOptionsAsync(IPage page, string selector)
    {
        var locator = page.Locator($"{selector} option");

        var options = await locator.EvaluateAllAsync<DomOption[]>(
            """
            options => options.map(o => ({
                value: o.value || "",
                text: (o.textContent || "").trim(),
                disabled: !!o.disabled
            }))
            """);

        return options ?? [];
    }

    /// <summary>
    /// Reads showtimes from the dedicated select that Showcase fills in after a day is
    /// chosen. Deliberately scoped to that control: sweeping the whole page for HH:mm
    /// turns any "10:00 a 22:00" in a footer or banner into a phantom showing.
    /// </summary>
    private async Task<IReadOnlyList<TimeOnly>> FindShowtimesAsync(IPage page)
    {
        var options = (await GetOptionsAsync(page, _options.Selectors.Showtime))
            .Where(ShowcaseParsing.IsRealOption)
            .ToArray();

        var found = ShowcaseParsing.ParseTimes(options.Select(x => x.Text));

        if (options.Length > 0 && found.Count == 0)
        {
            _logger.LogWarning(
                "El select de horarios trajo {Count} opciones pero ninguna con formato HH:mm: {Texts}.",
                options.Length,
                string.Join(" | ", options.Select(x => x.Text)));
        }

        return [.. found];
    }

    public async ValueTask DisposeAsync() => await DisposeBrowserAsync();

    /// <summary>
    /// Closes whatever is still alive and nulls the fields. Idempotent and safe against
    /// dead handles: called both on disposal and when recovering from a crash.
    /// </summary>
    private async ValueTask DisposeBrowserAsync()
    {
        try
        {
            if (_context is not null)
                await _context.CloseAsync();

            if (_browser is not null)
                await _browser.CloseAsync();

            _playwright?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error cerrando el navegador; se descarta igual.");
        }
        finally
        {
            _page = null;
            _context = null;
            _browser = null;
            _playwright = null;
        }
    }

}

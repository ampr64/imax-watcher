using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ImaxWatcher.Extensions;
using ImaxWatcher.Models;
using ImaxWatcher.Options;

namespace ImaxWatcher.Notifications;

public sealed class TelegramNotifier : INotifier
{
    public const string HttpClientName = "Telegram";

    private readonly TelegramOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramNotifier(
        IOptions<TelegramOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "Telegram";
    public bool IsEnabled => _options.Enabled;

    public async Task SendAsync(
        WatchAlert alert,
        CancellationToken cancellationToken)
    {
        var first = alert.Availabilities
            .OrderBy(x => x.Date)
            .First();

        var payload = new
        {
            chat_id = _options.ChatId,
            text = alert.AsTelegramText(),
            disable_web_page_preview = true,
            reply_markup = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new
                        {
                            text = "🎟️ COMPRAR AHORA",
                            url = first.BookingUrl
                        }
                    }
                }
            }
        };

        var client =
            _httpClientFactory.CreateClient(HttpClientName);

        // The token lives in the named client's BaseAddress; only the method here.
        using var response =
            await client.PostAsJsonAsync(
                "sendMessage",
                payload,
                cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram devolvió " +
                $"{(int)response.StatusCode} " +
                $"{response.ReasonPhrase}: " +
                responseBody);
        }
    }
}
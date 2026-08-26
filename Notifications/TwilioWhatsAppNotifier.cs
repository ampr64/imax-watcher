using System.Text;
using System.Text.Json;
using ImaxWatcher.Extensions;
using ImaxWatcher.Models;
using ImaxWatcher.Options;

namespace ImaxWatcher.Notifications;

public sealed class TwilioWhatsAppNotifier(
    WhatsAppOptions options,
    IHttpClientFactory httpClientFactory) : INotifier
{
    public const string HttpClientName = "WhatsApp";

    private const string WhatsAppPrefix = "whatsapp:";

    private readonly WhatsAppOptions _options = options;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string Name => "WhatsApp (Twilio)";
    public bool IsEnabled => _options.Enabled;

    public async Task SendAsync(WatchAlert alert, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ApiKeySid}:{_options.ApiKeySecret}"));
        request.Headers.Authorization = new("Basic", credentials);

        var first = alert.Availabilities.OrderBy(x => x.Date).First();
        var fields = new Dictionary<string, string>
        {
            ["From"] = EnsureWhatsAppPrefix(_options.From),
            ["To"] = EnsureWhatsAppPrefix(_options.To)
        };

        if (!string.IsNullOrWhiteSpace(_options.ContentSid))
        {
            fields["ContentSid"] = _options.ContentSid;
            fields["ContentVariables"] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["1"] = first.Date.ToString("dd/MM/yyyy"),
                ["2"] = string.Join(", ", first.Times.Select(t => t.ToString("HH:mm"))),
                ["3"] = first.BookingUrl
            });
        }
        else
        {
            fields["Body"] = alert.AsWhatsAppText();
        }

        request.Content = new FormUrlEncodedContent(fields);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Twilio devolvió {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}");
        }
    }

    private static string EnsureWhatsAppPrefix(string number) =>
        number.StartsWith(WhatsAppPrefix, StringComparison.OrdinalIgnoreCase)
            ? number
            : $"{WhatsAppPrefix}{number}";
}

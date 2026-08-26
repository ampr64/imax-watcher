using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ImaxWatcher.Extensions;
using ImaxWatcher.Models;
using ImaxWatcher.Options;

namespace ImaxWatcher.Notifications;

public sealed class EmailNotifier(EmailOptions options) : INotifier
{
    private readonly EmailOptions _options = options;

    public string Name => "Email";
    public bool IsEnabled => _options.Enabled;

    public async Task SendAsync(WatchAlert alert, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));

        foreach (var address in _options.Recipients)
            message.To.Add(MailboxAddress.Parse(address));

        var firstDate = alert.Availabilities.Min(x => x.Date);
        message.Subject = $"🚨 {alert.Movie} disponible en {alert.Cinema} - {firstDate:dd/MM}";
        message.Body = new TextPart("plain")
        {
            Text = alert.AsPlainText(),
        };

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.From, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

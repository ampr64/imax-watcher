using System.Text;
using ImaxWatcher.Models;

namespace ImaxWatcher.Extensions;

public static class WatchAlertExtensions
{
    public static string AsPlainText(this WatchAlert alert)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🚨 {alert.Movie} - {alert.Cinema}");
        sb.AppendLine();
        sb.AppendLine("Se detectaron funciones posteriores a la fecha de corte con horarios disponibles:");
        sb.AppendLine();

        foreach (var item in alert.Availabilities.OrderBy(x => x.Date).ThenBy(x => x.Format))
        {
            sb.AppendLine($"Fecha: {item.Date:dd/MM/yyyy}");
            sb.AppendLine($"Formato: {item.Format}");
            sb.AppendLine($"Horarios: {string.Join(", ", item.Times.Select(t => t.ToString("HH:mm")))}");
            sb.AppendLine();
        }

        sb.AppendLine($"Comprar: {alert.Availabilities[0].BookingUrl}");
        sb.AppendLine($"Detectado: {alert.DetectedAt:dd/MM/yyyy HH:mm:ss zzz}");
        return sb.ToString();
    }

    public static string AsWhatsAppText(this WatchAlert alert)
    {
        var first = alert.Availabilities.OrderBy(x => x.Date).First();
        return $"🚨 {alert.Movie} - {alert.Cinema}\n" +
               $"📅 {first.Date:dd/MM/yyyy}\n" +
               $"🕒 {string.Join(", ", first.Times.Select(t => t.ToString("HH:mm")))}\n" +
               $"🎟️ {first.BookingUrl}";
    }

    public static string AsTelegramText(this WatchAlert alert)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"🚨 {alert.Movie} — {alert.Cinema}");
        sb.AppendLine();
        sb.AppendLine("¡Se habilitaron nuevas funciones!");
        sb.AppendLine();

        foreach (var item in alert.Availabilities
                     .OrderBy(x => x.Date)
                     .ThenBy(x => x.Format))
        {
            sb.AppendLine($"📅 {item.Date:dd/MM/yyyy}");
            sb.AppendLine($"🎞️ {item.Format}");
            sb.AppendLine(
                $"🕒 {string.Join(", ",
                    item.Times.Select(t => t.ToString("HH:mm")))}");

            sb.AppendLine();
        }

        sb.AppendLine(
            $"Detectado: {alert.DetectedAt:dd/MM/yyyy HH:mm:ss}");

        return sb.ToString().TrimEnd();
    }
}

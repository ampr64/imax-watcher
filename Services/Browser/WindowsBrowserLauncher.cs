using System.Diagnostics;

namespace ImaxWatcher.Services.Browser;

public sealed class WindowsBrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
    }
}
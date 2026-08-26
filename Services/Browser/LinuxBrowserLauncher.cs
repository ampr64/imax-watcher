using System.Diagnostics;

namespace ImaxWatcher.Services.Browser;

public sealed class LinuxBrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "xdg-open",
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(url);

        Process.Start(startInfo);
    }
}
using System.Diagnostics;

namespace ImaxWatcher.Services.Browser;

public sealed class MacOsBrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "open",
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(url);

        Process.Start(startInfo);
    }
}
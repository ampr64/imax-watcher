namespace ImaxWatcher.Services.Browser;

public sealed class UnsupportedBrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        throw new PlatformNotSupportedException(
            "La apertura automática del navegador no está soportada " +
            "en este sistema operativo.");
    }
}
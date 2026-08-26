namespace ImaxWatcher.Services.Sound;

public sealed class WindowsAlertSoundPlayer : IAlertSoundPlayer
{
    public void Play()
    {
        for (var i = 0; i < 8; i++)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            Console.Beep(
                frequency: 1500,
                duration: 300);
#pragma warning restore CA1416 // Validate platform compatibility

            Thread.Sleep(100);
        }
    }
}
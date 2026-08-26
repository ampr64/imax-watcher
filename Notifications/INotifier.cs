using ImaxWatcher.Models;

namespace ImaxWatcher.Notifications;

public interface INotifier
{
    string Name { get; }

    bool IsEnabled { get; }
    
    Task SendAsync(WatchAlert alert, CancellationToken cancellationToken);
}

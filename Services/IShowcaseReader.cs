using ImaxWatcher.Models;

namespace ImaxWatcher.Services;

public interface IShowcaseReader
{
    Task<IReadOnlyList<Showing>> CheckAsync(CancellationToken cancellationToken);
}

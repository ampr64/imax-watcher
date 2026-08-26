namespace ImaxWatcher.Options;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using ImaxWatcher.Options.Validation;

public sealed record WatcherOptions
{
    public const string SectionName = "Watcher";

    [Required, Url]
    public required string BookingUrl { get; init; }
    
    [Required]
    public required string Cinema { get; init; }
    
    [Required]
    public required string Movie { get; init; }
    
    public DateOnly CutoffDate { get; init; }
    
    [Range(10, int.MaxValue)]
    public int PollMinSeconds { get; init; }
    
    [Range(10, int.MaxValue)]
    [GreaterThanOrEqualTo(nameof(PollMinSeconds), ErrorMessage = "Watcher:PollMaxSeconds debe ser >= PollMinSeconds.")]
    public int PollMaxSeconds { get; init; }
    
    [Range(1, int.MaxValue)]
    public int ErrorRetrySeconds { get; init; }
    
    [Range(100, int.MaxValue)]
    public int SettleDelayMilliseconds { get; init; }
    
    [Range(1, int.MaxValue)]
    public int NavigationTimeoutSeconds { get; init; }
    
    public bool Headless { get; init; }
    
    public bool OpenBrowserOnMatch { get; init; }
    
    public bool StopAfterFirstMatch { get; init; }
    
    [Required]
    [ValidateObjectMembers]
    public required SelectorOptions Selectors { get; init; }
}

public sealed record SelectorOptions
{
    [Required]
    public required string Cinema { get; init; }
    
    [Required]
    public required string Movie { get; init; }
    
    [Required]
    public required string Format { get; init; }
    
    [Required]
    public required string Day { get; init; }

    /// <summary>Showtimes select that Showcase fills in once a day is chosen.</summary>
    [Required]
    public required string Showtime { get; init; }
}

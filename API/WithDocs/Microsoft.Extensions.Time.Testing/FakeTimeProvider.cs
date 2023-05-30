// Assembly 'Microsoft.Extensions.TimeProvider.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Time.Testing;

/// <summary>
/// A synthetic time provider used to enable deterministic behavior in tests.
/// </summary>
[Experimental("EXTEXP0004", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class FakeTimeProvider : TimeProvider
{
    /// <summary>
    /// Gets the starting date and time for this provider.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets or sets the amount of time by which time advances whenever the clock is read.
    /// </summary>
    /// <remarks>
    /// This defaults to <see cref="F:System.TimeSpan.Zero" />.
    /// </remarks>
    public TimeSpan AutoAdvanceAmount { get; set; }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone { get; }

    /// <summary>
    /// Gets the amount by which the value from <see cref="M:Microsoft.Extensions.Time.Testing.FakeTimeProvider.GetTimestamp" /> increments per second.
    /// </summary>
    /// <remarks>
    /// This is fixed to the value of <see cref="F:System.TimeSpan.TicksPerSecond" />.
    /// </remarks>
    public override long TimestampFrequency { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Time.Testing.FakeTimeProvider" /> class.
    /// </summary>
    /// <remarks>
    /// This creates a provider whose time is initially set to midnight January 1st 2000.
    /// The provider is set to not automatically advance time each time it is read.
    /// </remarks>
    public FakeTimeProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Time.Testing.FakeTimeProvider" /> class.
    /// </summary>
    /// <param name="startDateTime">The initial time and date reported by the provider.</param>
    /// <remarks>
    /// The provider is set to not automatically advance time each time it is read.
    /// </remarks>
    public FakeTimeProvider(DateTimeOffset startDateTime);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow();

    /// <summary>
    /// Sets the date and time in the UTC time zone.
    /// </summary>
    /// <param name="value">The date and time in the UTC time zone.</param>
    public void SetUtcNow(DateTimeOffset value);

    /// <summary>
    /// Advances time by a specific amount.
    /// </summary>
    /// <param name="delta">The amount of time to advance the clock by.</param>
    /// <remarks>
    /// Advancing time affects the timers created from this provider, and all other operations that are directly or
    /// indirectly using this provider as a time source. Whereas when using <see cref="P:System.TimeProvider.System" />, time
    /// marches forward automatically in hardware, for the fake time provider the application is responsible for
    /// doing this explicitly by calling this method.
    /// </remarks>
    public void Advance(TimeSpan delta);

    /// <inheritdoc />
    public override long GetTimestamp();

    /// <summary>
    /// Sets the local time zone.
    /// </summary>
    /// <param name="localTimeZone">The local time zone.</param>
    public void SetLocalTimeZone(TimeZoneInfo localTimeZone);

    /// <summary>
    /// Returns a string representation this provider's idea of current time.
    /// </summary>
    /// <returns>A string representing the provider's current time.</returns>
    public override string ToString();

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);
}

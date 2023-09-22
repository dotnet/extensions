// Assembly 'Microsoft.Extensions.TimeProvider.Testing'

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Time.Testing;

public class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset Start { get; }
    public TimeSpan AutoAdvanceAmount { get; set; }
    public override TimeZoneInfo LocalTimeZone { get; }
    public override long TimestampFrequency { get; }
    public FakeTimeProvider();
    public FakeTimeProvider(DateTimeOffset startDateTime);
    public override DateTimeOffset GetUtcNow();
    public void SetUtcNow(DateTimeOffset value);
    public void Advance(TimeSpan delta);
    public override long GetTimestamp();
    public void SetLocalTimeZone(TimeZoneInfo localTimeZone);
    public override string ToString();
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);
}

# Microsoft.Extensions.TimeProvider.Testing

Provides a `FakeTimeProvider` for testing components that depend on `System.TimeProvider`.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.TimeProvider.Testing
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

`FakeTimeProvider` can be used to manually adjust time to test time dependent components in a deterministic way.

`FakeTimeProvider` derives from [TimeProvider](https://learn.microsoft.com/dotnet/api/system.timeprovider) and adds the following APIs:

```csharp
public FakeTimeProvider(DateTimeOffset startDateTime)
public DateTimeOffset Start { get; }
public TimeSpan AutoAdvanceAmount { get; set; }
public void SetUtcNow(DateTimeOffset value)
public void Advance(TimeSpan delta)
public void SetLocalTimeZone(TimeZoneInfo localTimeZone)
```

These can be used as follows:

```csharp
var timeProvider = new FakeTimeProvider();
var myComponent = new MyComponent(timeProvider);
timeProvider.Advance(TimeSpan.FromSeconds(5));
myComponent.CheckState();
```

## SynchronizationContext in xUnit Tests

### xUnit v2

Some testing libraries such as xUnit v2 provide custom `SynchronizationContext` for running tests. xUnit v2, for instance, provides `AsyncTestSyncContext` that allows to properly manage asynchronous operations withing the test execution. However, it brings an issue when we test asynchronous code that uses `ConfigureAwait(false)` in combination with class like `FakeTimeProvider`. In such cases, the xUnit context may lose track of the continuation, causing the test to become unresponsive, whether the test itself is asynchronous or not.

To prevent this issue, remove the xUnit context for tests dependent on `FakeTimeProvider` by setting the synchronization context to `null`:
```
SynchronizationContext.SetSynchronizationContext(null)
```

The `Advance` method is used to simulate the passage of time. Below is an example how to create a test for a code that uses `ConfigureAwait(false)` that ensures that the continuation of the awaited task (i.e., the code that comes after the await statement) works correctly.

For a more realistic example, consider the following test using Polly:

```cs
using Polly;
using Polly.Retry;

public class SomeService(TimeProvider timeProvider)
{
    // Don't do this in real life, not thread safe
    public int Tries { get; private set; }

    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
        .AddRetry(
            new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Linear,
            })
        .Build();

    public async Task<int> PollyRetry(double taskDelay, double cancellationSeconds)
    {
        Tries = 0;
        return await _retryPipeline.ExecuteAsync(
            async _ =>
            {
                Tries++;
                // Simulate a task that takes some time to complete
                // With xUnit Context this would fail.
                await timeProvider.Delay(TimeSpan.FromSeconds(taskDelay)).ConfigureAwait(false);
                if (Tries < 2)
                {
                    throw new InvalidOperationException();
                }
                return Tries;
            },
            CancellationToken.None);
    }
}

using Microsoft.Extensions.Time.Testing;

public class SomeServiceTests
{
    [Fact]
    public void PollyRetry_ShouldHave2Tries()
    {
        // Arrange
        // Remove xUnit Context for this test
        SynchronizationContext.SetSynchronizationContext(null);
        var timeProvider = new FakeTimeProvider();
        var someService = new SomeService(timeProvider);

        // Act
        var result = someService.PollyRetry(taskDelay: 1, cancellationSeconds: 6);

        // Advancing the time more than one second should resolves the first execution delay.
        timeProvider.Advance(TimeSpan.FromMilliseconds(1001));

        // Advancing the time more than the retry delay time of 1s,
        // and less then the task execution delay should start the second try
        timeProvider.Advance(TimeSpan.FromMilliseconds(1050));
        
        // Assert
        result.IsCompleted.Should().BeFalse();
        someService.Tries.Should().Be(2);
    }
}
```

### xUnit v3 

`AsyncTestSyncContext` has been removed more [here](https://xunit.net/docs/getting-started/v3/migration) so described issue is no longer a problem.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

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

## Use ConfigureAwait(true) with FakeTimeProvider.Advance

The Advance method is used to simulate the passage of time. This can be useful in tests where you need to control the timing of asynchronous operations.
When awaiting a task in a test that uses `FakeTimeProvider`, it's important to use `ConfigureAwait(true)`.

Here's an example:

```cs
await provider.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(true);
```

This ensures that the continuation of the awaited task (i.e., the code that comes after the await statement) runs in the original context.

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
        CancellationTokenSource cts = new(TimeSpan.FromSeconds(cancellationSeconds), timeProvider);
        Tries = 0;

        // get a context from the pool and return it when done
        var context = ResilienceContextPool.Shared.Get(
            // ensure execution continues on captured context 
            continueOnCapturedContext: true, 
            cancellationToken: cts.Token);

        var result = await _retryPipeline.ExecuteAsync(
            async _ =>
            {
                Tries++;

                // Simulate a task that takes some time to complete
                await Task.Delay(TimeSpan.FromSeconds(taskDelay), timeProvider).ConfigureAwait(true);

                if (Tries <= 2)
                {
                    throw new InvalidOperationException();
                }

                return Tries;
            },
            context);

        ResilienceContextPool.Shared.Return(context);

        return result;
    }
}

using Microsoft.Extensions.Time.Testing;

public class SomeServiceTests
{
    [Fact]
    public void PollyRetry_ShouldHave2Tries()
    {
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

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

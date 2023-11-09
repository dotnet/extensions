# Microsoft.Extensions.Diagnostics.Testing

This library provides utilities for easy testing logging and metering functionality.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Diagnostics.Testing
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Diagnostics.Testing" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Logging fakes

The `FakeLogger` is the implementation of the `Microsoft.Extensions.Logging.ILogger` that collects log messages
in a memory list of log records. It is designed to be used for validation that the functionality under
test writes the expected log messages:

```csharp
[Fact]
public void TestMethod()
{
  var fakeLogger = new FakeLogger<TheClassUnderTest>();

  // Run the functionality that is supposed to write log messages
  var classUnderTest = new TheClassUnderTest(fakeLogger);
  classUnderTest.DoSomething();

  var loggedRecords = fakeLogger.Collector.GetSnapshot();

  // Assert that the expected messages were logged
  Assert.Equal(2, loggedRecords.Count);
  Assert.Equal("Something is executing", loggedRecords[0].Message);
  Assert.Equal("Code completed successfully", loggedRecords[1].Message);
}
```

The `FakeLoggerServiceCollectionExtensions` and `FakeLoggerBuilderExtensions` types provide extension methods
for integrating the fake logging into the dependency injection container.
These extension methods register the `FakeLogCollector` - the in-memory holder of log messages
and the `FakeLoggerProvider` - the implementation of `Microsoft.Extensions.Logging.ILoggerProvider` that returns `FakeLogger` instances.

The following example shows the usage of fake logging in combination with the dependency injection container in a unit test:

```csharp
[Fact]
public void TestMethod()
{
  var serviceProvider = new ServiceCollection()
    .AddFakeLogging();
    .AddSingleton<TheClassUnderTest>()
    .BuildServiceProvider();

  var classUnderTest = serviceProvider.GetRequiredService<TheClassUnderTest>();

  // Run the functionality that is supposed to write log messages
  classUnderTest.DoSomething();

  var logCollector = serviceProvider.GetFakeLogCollector();
  var loggedRecords = fakeLogger.Collector.GetSnapshot();

  // Assert that the expected messages were logged
  Assert.Equal(2, loggedRecords.Count);
  Assert.Equal("Something is executing", loggedRecords[0].Message);
  Assert.Equal("Code completed successfully", loggedRecords[1].Message);
}
```

### Metrics collection

The `MetricCollector` is a utility class that can be used to test that metrics are published correctly by a metering instrument:

```csharp
[Fact]
public void TestMethod()
{
  using var meter = new Meter("MyMeter");
  using var counterInstrument = meter.CreateCounter<long>("request.counter");
  using var collector = new MetricCollector(meter, "request.counter");

  // Record some metric
  counterInstrument.Add(3);

  // Assert that the metric was published
  Assert.NotNull(collector.LastMeasurement);
  Assert.Equal(3, collector.LastMeasurement.Value);
}
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

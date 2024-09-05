# Microsoft.Extensions.Http.Resilience

Resilience mechanisms for `HttpClient` built on the [Polly framework](https://www.pollydocs.org/).

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.Http.Resilience
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

When configuring an HttpClient through the [HTTP client factory](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory) the following extensions can add a set of pre-configured hedging or resilience behaviors. These pipelines combine multiple resilience strategies with pre-configured defaults.
- The total request timeout pipeline applies an overall timeout to the execution, ensuring that the request including hedging attempts, does not exceed the configured limit.
- The retry pipeline retries the request in case the dependency is slow or returns a transient error.
- The rate limiter pipeline limits the maximum number of requests being send to the dependency.
- The circuit breaker blocks the execution if too many direct failures or timeouts are detected.
- The attempt timeout pipeline limits each request attempt duration and throws if its exceeded.

### Resilience

The standard resilience pipeline makes use of the above strategies to ensure HTTP requests can be sent reliably.

```csharp
var clientBuilder = services.AddHttpClient("MyClient");

clientBuilder.AddStandardResilienceHandler().Configure(o =>
{
    o.CircuitBreaker.MinimumThroughput = 10;
});
```

### Hedging

The standard hedging pipeline uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against. By default, the selection from pool is based on the URL Authority (scheme + host + port). It is recommended that you configure the way the strategies are selected by calling the `SelectPipelineByAuthority()` extensions. The last three strategies are applied to each individual endpoint.

```csharp
var clientBuilder = services.AddHttpClient("MyClient");

clientBuilder.AddStandardHedgingHandler().Configure(o =>
{
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
});
```

### Custom Resilience

For more granular control a custom pipeline can be constructed.

```csharp
var clientBuilder = services.AddHttpClient("MyClient");

clientBuilder.AddResilienceHandler("myHandler", b =>
{
    b.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>()
    {
        FallbackAction = _ => Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
    })
    .AddConcurrencyLimiter(100)
    .AddRetry(new HttpRetryStrategyOptions())
    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions())
    .AddTimeout(new HttpTimeoutStrategyOptions());
});
```

## Known issues

### Compatibility with the `Grpc.Net.ClientFactory` package

If you're using `Grpc.Net.ClientFactory` version `2.63.0` or earlier, then enabling the standard resilience or hedging
handlers for a gRPC client could cause a runtime exception. Specifically, consider the following code sample:

```csharp
services
    .AddGrpcClient<Greeter.GreeterClient>()
    .AddStandardResilienceHandler();
```

The preceding code results in the following exception:

```
System.InvalidOperationException: The ConfigureHttpClient method is not supported when creating gRPC clients. Unable to create client with name 'GreeterClient'.
```

To resolve this issue, we recommend upgrading to `Grpc.Net.ClientFactory` version `2.64.0` or later.

We've implemented a build time check that verifies if you're using `Grpc.Net.ClientFactory` version
`2.63.0` or earlier, and if you are the check produces a compilation warning. You can suppress the
warning by setting the following property in your project file:

```xml
<PropertyGroup>
  <SuppressCheckGrpcNetClientFactoryVersion>true</SuppressCheckGrpcNetClientFactoryVersion>
</PropertyGroup>
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

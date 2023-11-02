# Microsoft.Extensions.Http.Resilience

Resilience mechanisms for `HttpClient` built on the [Polly framework](https://www.pollydocs.org/).

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Http.Resilience
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

When configuring an HttpClient through the client factory the following extensions can add a set of pre-configured hedging or resilience behaviors. These pipelines combine multiple strategies with pre-configured defaults.
- The total request timeout pipeline applies an overall timeout to the execution, ensuring that the request including hedging attempts, does not exceed the configured limit.
- The retry pipeline retries the request in case the dependency is slow or returns a transient error.
- The bulkhead pipeline limits the maximum number of concurrent requests being send to the dependency.
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

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

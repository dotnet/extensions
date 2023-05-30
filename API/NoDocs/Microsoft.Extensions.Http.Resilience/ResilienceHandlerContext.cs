// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public sealed class ResilienceHandlerContext
{
    public IServiceProvider ServiceProvider { get; }
    public string BuilderName { get; }
    public string StrategyKey { get; }
    public void EnableReloads<TOptions>(string? name = null);
}

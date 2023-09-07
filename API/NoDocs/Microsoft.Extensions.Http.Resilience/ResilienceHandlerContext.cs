// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public sealed class ResilienceHandlerContext
{
    public IServiceProvider ServiceProvider { get; }
    public string BuilderName { get; }
    public string InstanceName { get; }
    public void EnableReloads<TOptions>(string? name = null);
}

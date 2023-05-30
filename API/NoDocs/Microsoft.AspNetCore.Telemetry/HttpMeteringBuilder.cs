// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

public class HttpMeteringBuilder
{
    public IServiceCollection Services { get; }
    public HttpMeteringBuilder(IServiceCollection services);
}

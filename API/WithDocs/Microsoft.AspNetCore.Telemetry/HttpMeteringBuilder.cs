// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for creating a builder.
/// </summary>
public class HttpMeteringBuilder
{
    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.AspNetCore.Telemetry.HttpMeteringBuilder" /> class.
    /// </summary>
    /// <param name="services">Application services.</param>
    public HttpMeteringBuilder(IServiceCollection services);
}

// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Telemetry.Metrics;

/// <summary>
/// A meter class where the meter name is derived from the specified <typeparamref name="TMeterName" /> type name.
/// </summary>
/// <typeparam name="TMeterName">The type whose name is used as the meter name.</typeparam>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class Meter<TMeterName> : Meter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Metrics.Meter`1" /> class.
    /// </summary>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public Meter();
}

// Assembly 'Microsoft.Extensions.Hosting.Testing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Options to configure <see cref="T:Microsoft.Extensions.Hosting.Testing.FakeHost" />.
/// </summary>
public class FakeHostOptions
{
    /// <summary>
    /// Gets or sets the time limit for the host to start.
    /// </summary>
    /// <value>The default value is 5 seconds.</value>
    /// <remarks>This limit is used if there's no cancellation token.</remarks>
    public TimeSpan StartUpTimeout { get; set; }

    /// <summary>
    /// Gets or sets the time limit for the host to shut down.
    /// </summary>
    /// <value>The default value is 10 seconds.</value>
    /// <remarks>This limit is used if there's no cancellation token.</remarks>
    public TimeSpan ShutDownTimeout { get; set; }

    /// <summary>
    /// Gets or sets the time limit for the host to be up.
    /// </summary>
    /// <value>The default is 30 seconds.</value>
    /// <remarks>
    /// -1 millisecond means infinite time to live.
    /// TimeToLive is not enforced when debugging.
    /// </remarks>
    public TimeSpan TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fake logging is configured automatically.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool FakeLogging { get; set; }

    /// <inheritdoc cref="T:Microsoft.Extensions.DependencyInjection.ServiceProviderOptions" />
    public bool ValidateScopes { get; set; }

    /// <inheritdoc cref="T:Microsoft.Extensions.DependencyInjection.ServiceProviderOptions" />
    public bool ValidateOnBuild { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fake redaction is configured automatically.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool FakeRedaction { get; set; }

    public FakeHostOptions();
}

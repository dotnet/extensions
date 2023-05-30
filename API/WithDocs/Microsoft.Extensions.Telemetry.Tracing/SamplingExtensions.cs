// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Extension methods for setting up trace sampling.
/// </summary>
public static class SamplingExtensions
{
    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add sampling to.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder);

    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add sampling to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Tracing.SamplingOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder, Action<SamplingOptions> configure);

    /// <summary>
    /// Adds sampling for traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add sampling to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Telemetry.Tracing.SamplingOptions" />.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder, IConfigurationSection section);
}

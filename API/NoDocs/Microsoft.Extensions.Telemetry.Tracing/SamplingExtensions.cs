// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Tracing;

public static class SamplingExtensions
{
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder);
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder, Action<SamplingOptions> configure);
    public static TracerProviderBuilder AddSampling(this TracerProviderBuilder builder, IConfigurationSection section);
}

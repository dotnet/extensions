// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Latency;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.MessageContext" /> class to add support for setting/retrieving <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" />.
/// </summary>
public static class MessageLatencyContextFeatureExtensions
{
    /// <summary>
    /// Sets the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> in <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <remarks>
    /// The <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> allows user to set fine-grained latency and associated properties for different processing steps.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="latencyContext">The latency context to store fine-grained latency for different processing steps.</param>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static void SetLatencyContext(this MessageContext context, ILatencyContext latencyContext);

    /// <summary>
    /// Try to get the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> from the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <remarks>
    /// Application should set the <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" /> in the <see cref="T:System.Cloud.Messaging.MessageContext" /> via the <see cref="M:System.Cloud.Messaging.MessageLatencyContextFeatureExtensions.SetLatencyContext(System.Cloud.Messaging.MessageContext,Microsoft.Extensions.Telemetry.Latency.ILatencyContext)" />.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="latencyContext">The optional latency context registered with the <paramref name="context" />.</param>
    /// <returns><see cref="T:System.Boolean" /> and if <see langword="true" />, a corresponding <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public static bool TryGetLatencyContext(this MessageContext context, [NotNullWhen(true)] out ILatencyContext? latencyContext);
}

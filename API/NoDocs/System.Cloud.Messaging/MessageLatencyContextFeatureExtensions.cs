// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Latency;

namespace System.Cloud.Messaging;

public static class MessageLatencyContextFeatureExtensions
{
    public static void SetLatencyContext(this MessageContext context, ILatencyContext latencyContext);
    public static bool TryGetLatencyContext(this MessageContext context, [NotNullWhen(true)] out ILatencyContext? latencyContext);
}

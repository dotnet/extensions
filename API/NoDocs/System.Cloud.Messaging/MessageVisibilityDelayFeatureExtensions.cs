// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;

namespace System.Cloud.Messaging;

public static class MessageVisibilityDelayFeatureExtensions
{
    public static void SetVisibilityDelay(this MessageContext context, TimeSpan visibilityDelay);
    public static bool TryGetVisibilityDelay(this MessageContext context, [NotNullWhen(true)] out IMessageVisibilityDelayFeature? visibilityDelay);
}

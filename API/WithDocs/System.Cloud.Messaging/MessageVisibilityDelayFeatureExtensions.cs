// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.MessageContext" /> to add support for <see cref="T:System.Cloud.Messaging.IMessageVisibilityDelayFeature" />.
/// </summary>
public static class MessageVisibilityDelayFeatureExtensions
{
    /// <summary>
    /// Sets <see cref="T:System.Cloud.Messaging.IMessageVisibilityDelayFeature" /> with the provided <paramref name="visibilityDelay" /> in the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="visibilityDelay">The time span representing when the message should be next visible for processing via a different <see cref="T:System.Cloud.Messaging.MessageConsumer" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public static void SetVisibilityDelay(this MessageContext context, TimeSpan visibilityDelay);

    /// <summary>
    /// Tries to get <see cref="T:System.Cloud.Messaging.IMessageVisibilityDelayFeature" /> in the provided <paramref name="visibilityDelay" /> from the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="visibilityDelay">The optional feature to delay the message visibility.</param>
    /// <returns><see cref="T:System.Boolean" /> and if <see langword="true" />, a corresponding <see cref="T:System.Cloud.Messaging.IMessageVisibilityDelayFeature" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">No source <see cref="T:Microsoft.AspNetCore.Http.Features.IFeatureCollection" /> is added to <paramref name="context" />.</exception>
    public static bool TryGetVisibilityDelay(this MessageContext context, [NotNullWhen(true)] out IMessageVisibilityDelayFeature? visibilityDelay);
}

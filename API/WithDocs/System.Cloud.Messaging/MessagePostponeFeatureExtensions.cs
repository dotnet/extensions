// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.MessageContext" /> class to add support for <see cref="T:System.Cloud.Messaging.IMessagePostponeFeature" />.
/// </summary>
public static class MessagePostponeFeatureExtensions
{
    /// <summary>
    /// Postpones the message processing asynchronously.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should ensure to set the <see cref="T:System.Cloud.Messaging.IMessagePostponeFeature" /> via <see cref="M:System.Cloud.Messaging.MessagePostponeFeatureExtensions.SetMessagePostponeFeature(System.Cloud.Messaging.MessageContext,System.Cloud.Messaging.IMessagePostponeFeature)" />
    /// typically in their <see cref="T:System.Cloud.Messaging.IMessageSource" /> implementations.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="delay">The time by which the message processing is to be postponed.</param>
    /// <param name="cancellationToken">The cancellation token for the postpone operation.</param>
    /// <returns>To be added.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">There is no <see cref="T:System.Cloud.Messaging.IMessagePostponeFeature" /> added to <paramref name="context" />.</exception>
    public static ValueTask PostponeAsync(this MessageContext context, TimeSpan delay, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the <see cref="T:System.Cloud.Messaging.IMessagePostponeFeature" /> in the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="messagePostponeFeature">The feature to postpone message processing.</param>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static void SetMessagePostponeFeature(this MessageContext context, IMessagePostponeFeature messagePostponeFeature);
}

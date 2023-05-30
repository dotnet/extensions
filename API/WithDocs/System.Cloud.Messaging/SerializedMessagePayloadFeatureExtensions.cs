// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="T:System.Cloud.Messaging.MessageContext" /> class to add support for <see cref="T:System.Cloud.Messaging.ISerializedMessagePayloadFeature`1" />.
/// </summary>
public static class SerializedMessagePayloadFeatureExtensions
{
    /// <summary>
    /// Gets the message payload as a serialized <typeparamref name="T" /> type.
    /// </summary>
    /// <remarks>
    /// Ensure the serialized message payload is set in the pipeline via <see cref="M:System.Cloud.Messaging.SerializedMessagePayloadFeatureExtensions.SetSerializedPayload``1(System.Cloud.Messaging.MessageContext,``0)" /> before calling this method.
    /// </remarks>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context">The message context.</param>
    /// <returns>The serialized message payload.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">No <see cref="T:System.Cloud.Messaging.ISerializedMessagePayloadFeature`1" /> is set in the <paramref name="context" />.</exception>
    public static T GetSerializedPayload<T>(this MessageContext context) where T : notnull;

    /// <summary>
    /// Sets the message payload in the <see cref="T:System.Cloud.Messaging.MessageContext" /> as a serialized <typeparamref name="T" /> type.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="payload">The serialized message payload.</param>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static void SetSerializedPayload<T>(this MessageContext context, T payload) where T : notnull;

    /// <summary>
    /// Try to get the serialized message payload of <typeparamref name="T" /> type from the <see cref="T:System.Cloud.Messaging.MessageContext" />.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="payload">The optional serialized message payload.</param>
    /// <returns><see cref="T:System.Boolean" /> and if <see langword="true" />, a corresponding <typeparamref name="T" /> representing the serialized message payload.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public static bool TryGetSerializedPayload<T>(this MessageContext context, [NotNullWhen(true)] out T? payload) where T : notnull;
}

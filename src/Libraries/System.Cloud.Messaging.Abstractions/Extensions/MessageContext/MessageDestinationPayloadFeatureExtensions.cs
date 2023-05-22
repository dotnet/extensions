// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for writing <see cref="IMessagePayloadFeature"/> to a <see cref="IMessageDestination"/> messages.
/// </summary>
public static class MessageDestinationPayloadFeatureExtensions
{
    /// <summary>
    /// Gets the message payload for <see cref="IMessageDestination"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns>Message payload as <see cref="ReadOnlyMemory{T}"/>.</returns>
    public static ReadOnlyMemory<byte> GetDestinationPayload(this MessageContext context)
    {
        _ = context.TryGetMessageDestinationFeatures(out IFeatureCollection? destinationFeatures);
        _ = Throw.IfNull(destinationFeatures);

        IMessagePayloadFeature? payloadFeature = destinationFeatures.Get<IMessagePayloadFeature>();
        _ = Throw.IfNull(payloadFeature);

        return payloadFeature!.Payload;
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> for <see cref="IMessageDestination"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">Message payload in <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.</param>
    public static void SetDestinationPayload(this MessageContext context, ReadOnlyMemory<byte> payload)
    {
        _ = Throw.IfNull(payload);

        _ = context.TryGetMessageDestinationFeatures(out IFeatureCollection? destinationFeatures);
        destinationFeatures ??= new FeatureCollection();

        destinationFeatures.Set<IMessagePayloadFeature>(new MessagePayloadFeature(payload));
        context.SetMessageDestinationFeatures(destinationFeatures);
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> for <see cref="IMessageDestination"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">Message payload in <see cref="byte"/> array.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetDestinationPayload(this MessageContext context, byte[] payload)
    {
        _ = Throw.IfNull(payload);

        _ = context.TryGetMessageDestinationFeatures(out IFeatureCollection? destinationFeatures);
        destinationFeatures ??= new FeatureCollection();

        destinationFeatures.Set<IMessagePayloadFeature>(new MessagePayloadFeature(payload));
        context.SetMessageDestinationFeatures(destinationFeatures);
    }

    /// <summary>
    /// Try to get the message payload for <see cref="IMessageDestination"/> as <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">The <see langword="out"/> to store the <see cref="ReadOnlyMemory{T}"/> representing the message payload.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="ReadOnlyMemory{T}"/>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetDestinationPayload(this MessageContext context, out ReadOnlyMemory<byte>? payload)
    {
        try
        {
            payload = context.GetDestinationPayload();
            return payload.HasValue;
        }
        catch (Exception)
        {
            payload = null;
            return false;
        }
    }

    /// <summary>
    /// Gets the message payload for <see cref="IMessageDestination"/> as <see cref="string"/> in <see cref="Encoding.UTF8"/>.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation,
    /// including starting and stopping quotes on a string.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="utf8StringPayload">The <see langword="out"/> to store the resultant payload as <see cref="Encoding.UTF8"/> <see cref="string"/>.</param>
    /// <returns>Message payload as <see cref="string"/>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Try Get Pattern.")]
    public static bool TryGetDestinationPayloadAsUTF8String(this MessageContext context, out string utf8StringPayload)
    {
        ReadOnlyMemory<byte> payload = context.GetDestinationPayload();
        try
        {
            utf8StringPayload = UTF8ConverterUtils.ConvertToUTF8StringUnsafe(payload);
            return utf8StringPayload != null;
        }
        catch (Exception)
        {
            utf8StringPayload = string.Empty;
            return false;
        }
    }
}

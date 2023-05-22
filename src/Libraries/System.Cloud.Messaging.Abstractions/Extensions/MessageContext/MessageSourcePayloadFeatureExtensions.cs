// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> obtained from <see cref="IMessageSource"/> for <see cref="IMessagePayloadFeature"/>.
/// </summary>
public static class MessageSourcePayloadFeatureExtensions
{
    /// <summary>
    /// Gets the message payload obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns>Message payload as <see cref="ReadOnlyMemory{T}"/>.</returns>
    public static ReadOnlyMemory<byte> GetSourcePayload(this MessageContext context)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        _ = Throw.IfNull(sourceFeatures);

        IMessagePayloadFeature? payloadFeature = sourceFeatures.Get<IMessagePayloadFeature>();
        _ = Throw.IfNull(payloadFeature);

        return payloadFeature!.Payload;
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">Message payload in <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.</param>
    public static void SetSourcePayload(this MessageContext context, ReadOnlyMemory<byte> payload)
    {
        _ = Throw.IfNull(payload);

        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set<IMessagePayloadFeature>(new MessagePayloadFeature(payload));
        context.SetMessageSourceFeatures(sourceFeatures);
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">Message payload in <see cref="byte"/> array.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetSourcePayload(this MessageContext context, byte[] payload)
    {
        _ = Throw.IfNull(payload);

        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set<IMessagePayloadFeature>(new MessagePayloadFeature(payload));
        context.SetMessageSourceFeatures(sourceFeatures);
    }

    /// <summary>
    /// Try to get the message payload obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">The <see langword="out"/> to store the <see cref="ReadOnlyMemory{T}"/> representing the message payload.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="ReadOnlyMemory{T}"/>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetSourcePayload(this MessageContext context, out ReadOnlyMemory<byte>? payload)
    {
        try
        {
            payload = context.GetSourcePayload();
            return payload.HasValue;
        }
        catch (Exception)
        {
            payload = null;
            return false;
        }
    }

    /// <summary>
    /// Gets the message payload obtained from <see cref="IMessageSource"/> as <see cref="string"/>.
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
    public static bool TryGetSourcePayloadAsUTF8String(this MessageContext context, out string utf8StringPayload)
    {
        ReadOnlyMemory<byte> payload = context.GetSourcePayload();
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

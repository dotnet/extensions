// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Represents the context for storing different <see cref="Features"/> required for the processing of message(s).
/// </summary>
/// <remarks>Inspired from <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext">ASP.NET Core HttpContext</see>.</remarks>
public abstract class MessageContext
{
    /// <summary>
    /// Gets the feature collection to register implementation for different types which will be helpful to process the message.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the message payload obtained from the <see cref="IMessageSource"/>.
    /// </summary>
    public ReadOnlyMemory<byte> SourcePayload { get; private set; }

    /// <summary>
    /// Gets the feature collection to register implementation for different types for the message obtained from the <see cref="IMessageSource"/>.
    /// </summary>
    public IFeatureCollection? SourceFeatures { get; private set; }

    /// <summary>
    /// Gets the message payload to be sent to <see cref="IMessageDestination"/>.
    /// </summary>
    public ReadOnlyMemory<byte>? DestinationPayload { get; private set; }

    /// <summary>
    /// Gets the feature collection to register implementation for different types for sending message to <see cref="IMessageDestination"/>.
    /// </summary>
    public IFeatureCollection? DestinationFeatures { get; private set; }

    /// <summary>
    /// Gets or sets the cancellation token for the cancelling the message processing.
    /// </summary>
    public CancellationToken MessageCancelledToken { get; set; }

    /// <summary>
    /// Marks the message processing to be completed asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public abstract ValueTask MarkCompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContext"/> class.
    /// </summary>
    /// <param name="features">The feature collection.</param>
    /// <param name="sourcePayload">The source payload.</param>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    protected MessageContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload)
    {
        Features = Throw.IfNull(features);
        SourcePayload = sourcePayload;
        MessageCancelledToken = CancellationToken.None;
    }

    /// <summary>
    /// Sets the feature of the <see cref="IMessageSource"/> in the <see cref="Features"/>.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="Features"/>.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="Features"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    public void AddFeature<T>(T feature)
    {
        _ = Throw.IfNull(feature);
        Features.Set(feature);
    }

    /// <summary>
    /// Sets the feature of the <see cref="IMessageSource"/> in the <see cref="SourceFeatures"/>.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="SourceFeatures"/>.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="SourceFeatures"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    public void AddSourceFeature<T>(T feature)
    {
        _ = Throw.IfNull(feature);
        SourceFeatures ??= new FeatureCollection();
        SourceFeatures.Set(feature);
    }

    /// <summary>
    /// Sets the feature for the <see cref="IMessageDestination"/> in the <see cref="DestinationFeatures"/>.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="DestinationFeatures"/>.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="DestinationFeatures"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is <see langword="null"/>.</exception>
    public void AddDestinationFeature<T>(T feature)
    {
        _ = Throw.IfNull(feature);
        DestinationFeatures ??= new FeatureCollection();
        DestinationFeatures.Set(feature);
    }

    /// <summary>
    /// Gets the <see cref="SourcePayload"/> as <see cref="Encoding.UTF8"/> encoded <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation, including starting and stopping quotes on a string.
    /// </remarks>
    /// <returns>The <see cref="SourcePayload"/> in <see cref="Encoding.UTF8"/> encoding.</returns>
    [SuppressMessage("Minor Code Smell", "S4049:Properties should be preferred", Justification = "A string payload may not be provided by all async processing implementations.")]
    public string GetUTF8SourcePayloadAsString()
    {
        return UTF8ConverterUtils.ConvertToUTF8StringUnsafe(SourcePayload);
    }

    /// <summary>
    /// Sets the payload in the <see cref="MessageContext"/> for the <see cref="IMessageDestination"/> message.
    /// </summary>
    /// <param name="payload">The payload for destination message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public void SetDestinationPayload(ReadOnlyMemory<byte> payload)
    {
        _ = Throw.IfNull(payload);
        DestinationPayload = payload;
    }

    /// <summary>
    /// Try to get the <see cref="DestinationPayload"/> message registered with <see cref="MessageContext"/> as a <see cref="string"/> in the <see cref="Encoding.UTF8"/> encoding.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation, including starting and stopping quotes on a string.
    /// </remarks>
    /// <param name="payload">The optional payload for the destination message in <see cref="Encoding.UTF8"/> encoding.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding payload for the destination message as a <see cref="Encoding.UTF8"/> encoded <see cref="string"/>.</returns>
    public bool TryGetUTF8DestinationPayloadAsString([NotNullWhen(true)] out string? payload)
    {
        if (DestinationPayload.HasValue)
        {
            payload = UTF8ConverterUtils.ConvertToUTF8StringUnsafe(DestinationPayload.Value);
            return payload != null;
        }
        else
        {
            payload = string.Empty;
            return false;
        }
    }
}

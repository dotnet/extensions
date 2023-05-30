// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging;

/// <summary>
/// Represents the context for storing different <see cref="P:System.Cloud.Messaging.MessageContext.Features" /> required for the processing of message(s).
/// </summary>
/// <remarks>Inspired from <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext">ASP.NET Core HttpContext</see>.</remarks>
public abstract class MessageContext
{
    /// <summary>
    /// Gets the feature collection to register implementation for different types which will be helpful to process the message.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the message payload obtained from the <see cref="T:System.Cloud.Messaging.IMessageSource" />.
    /// </summary>
    public ReadOnlyMemory<byte> SourcePayload { get; }

    /// <summary>
    /// Gets the feature collection to register implementation for different types for the message obtained from the <see cref="T:System.Cloud.Messaging.IMessageSource" />.
    /// </summary>
    public IFeatureCollection? SourceFeatures { get; }

    /// <summary>
    /// Gets the message payload to be sent to <see cref="T:System.Cloud.Messaging.IMessageDestination" />.
    /// </summary>
    public ReadOnlyMemory<byte>? DestinationPayload { get; }

    /// <summary>
    /// Gets the feature collection to register implementation for different types for sending message to <see cref="T:System.Cloud.Messaging.IMessageDestination" />.
    /// </summary>
    public IFeatureCollection? DestinationFeatures { get; }

    /// <summary>
    /// Gets or sets the cancellation token for the cancelling the message processing.
    /// </summary>
    public CancellationToken MessageCancelledToken { get; set; }

    /// <summary>
    /// Marks the message processing to be completed asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    public abstract ValueTask MarkCompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.Messaging.MessageContext" /> class.
    /// </summary>
    /// <param name="features">The feature collection.</param>
    /// <param name="sourcePayload">The source payload.</param>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    protected MessageContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload);

    /// <summary>
    /// Sets the feature of the <see cref="T:System.Cloud.Messaging.IMessageSource" /> in the <see cref="P:System.Cloud.Messaging.MessageContext.Features" />.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.Features" />.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.Features" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="feature" /> is <see langword="null" />.</exception>
    public void AddFeature<T>(T feature);

    /// <summary>
    /// Sets the feature of the <see cref="T:System.Cloud.Messaging.IMessageSource" /> in the <see cref="P:System.Cloud.Messaging.MessageContext.SourceFeatures" />.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.SourceFeatures" />.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.SourceFeatures" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="feature" /> is <see langword="null" />.</exception>
    public void AddSourceFeature<T>(T feature);

    /// <summary>
    /// Sets the feature for the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> in the <see cref="P:System.Cloud.Messaging.MessageContext.DestinationFeatures" />.
    /// </summary>
    /// <typeparam name="T">The type of the feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.DestinationFeatures" />.</typeparam>
    /// <param name="feature">The feature to be added to the <see cref="P:System.Cloud.Messaging.MessageContext.DestinationFeatures" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="feature" /> is <see langword="null" />.</exception>
    public void AddDestinationFeature<T>(T feature);

    /// <summary>
    /// Gets the <see cref="P:System.Cloud.Messaging.MessageContext.SourcePayload" /> as <see cref="P:System.Text.Encoding.UTF8" /> encoded <see cref="T:System.String" />.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation, including starting and stopping quotes on a string.
    /// </remarks>
    /// <returns>The <see cref="P:System.Cloud.Messaging.MessageContext.SourcePayload" /> in <see cref="P:System.Text.Encoding.UTF8" /> encoding.</returns>
    public string GetUTF8SourcePayloadAsString();

    /// <summary>
    /// Sets the payload in the <see cref="T:System.Cloud.Messaging.MessageContext" /> for the <see cref="T:System.Cloud.Messaging.IMessageDestination" /> message.
    /// </summary>
    /// <param name="payload">The payload for destination message.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="payload" /> is <see langword="null" />.</exception>
    public void SetDestinationPayload(ReadOnlyMemory<byte> payload);

    /// <summary>
    /// Try to get the <see cref="P:System.Cloud.Messaging.MessageContext.DestinationPayload" /> message registered with <see cref="T:System.Cloud.Messaging.MessageContext" /> as a <see cref="T:System.String" /> in the <see cref="P:System.Text.Encoding.UTF8" /> encoding.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation, including starting and stopping quotes on a string.
    /// </remarks>
    /// <param name="payload">The optional payload for the destination message in <see cref="P:System.Text.Encoding.UTF8" /> encoding.</param>
    /// <returns><see cref="T:System.Boolean" /> and if <see langword="true" />, a corresponding payload for the destination message as a <see cref="P:System.Text.Encoding.UTF8" /> encoded <see cref="T:System.String" />.</returns>
    public bool TryGetUTF8DestinationPayloadAsString([NotNullWhen(true)] out string? payload);
}

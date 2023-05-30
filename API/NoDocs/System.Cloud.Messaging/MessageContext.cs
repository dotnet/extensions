// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging;

public abstract class MessageContext
{
    public IFeatureCollection Features { get; }
    public ReadOnlyMemory<byte> SourcePayload { get; }
    public IFeatureCollection? SourceFeatures { get; }
    public ReadOnlyMemory<byte>? DestinationPayload { get; }
    public IFeatureCollection? DestinationFeatures { get; }
    public CancellationToken MessageCancelledToken { get; set; }
    public abstract ValueTask MarkCompleteAsync(CancellationToken cancellationToken);
    protected MessageContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload);
    public void AddFeature<T>(T feature);
    public void AddSourceFeature<T>(T feature);
    public void AddDestinationFeature<T>(T feature);
    public string GetUTF8SourcePayloadAsString();
    public void SetDestinationPayload(ReadOnlyMemory<byte> payload);
    public bool TryGetUTF8DestinationPayloadAsString([NotNullWhen(true)] out string? payload);
}

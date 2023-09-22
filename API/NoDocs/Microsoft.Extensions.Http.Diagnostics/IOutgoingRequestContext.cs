// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Http.Diagnostics;

public interface IOutgoingRequestContext
{
    RequestMetadata? RequestMetadata { get; }
    void SetRequestMetadata(RequestMetadata metadata);
}

// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Http.Diagnostics;

public interface IOutgoingRequestContext
{
    RequestMetadata? RequestMetadata { get; }
    void SetRequestMetadata(RequestMetadata metadata);
}

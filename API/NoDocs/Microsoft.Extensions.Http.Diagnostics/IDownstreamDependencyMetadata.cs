// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Diagnostics;

public interface IDownstreamDependencyMetadata
{
    string DependencyName { get; }
    ISet<string> UniqueHostNameSuffixes { get; }
    ISet<RequestMetadata> RequestMetadata { get; }
}

// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Logging;

public interface ITagCollector
{
    void Add(string tagName, object? tagValue);
    void Add(string tagName, object? tagValue, DataClassification classification);
}

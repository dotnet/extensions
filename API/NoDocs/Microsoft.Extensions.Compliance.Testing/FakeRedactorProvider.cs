// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Compliance.Testing;

public class FakeRedactorProvider : IRedactorProvider
{
    public FakeRedactionCollector Collector { get; }
    public FakeRedactorProvider(FakeRedactorOptions? options = null, FakeRedactionCollector? eventCollector = null);
    public Redactor GetRedactor(DataClassification classification);
}

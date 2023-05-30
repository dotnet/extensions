// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System.Collections.Generic;

namespace Microsoft.Extensions.Compliance.Testing;

public class FakeRedactionCollector
{
    public RedactorRequested LastRedactorRequested { get; }
    public IReadOnlyList<RedactorRequested> AllRedactorRequests { get; }
    public RedactedData LastRedactedData { get; }
    public IReadOnlyList<RedactedData> AllRedactedData { get; }
    public FakeRedactionCollector();
}

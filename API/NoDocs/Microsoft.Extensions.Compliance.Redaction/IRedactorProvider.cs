// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction;

public interface IRedactorProvider
{
    Redactor GetRedactor(DataClassification classification);
}

// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction;

public sealed class NullRedactorProvider : IRedactorProvider
{
    public static NullRedactorProvider Instance { get; }
    public Redactor GetRedactor(DataClassification classification);
    public NullRedactorProvider();
}

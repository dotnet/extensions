// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Compliance.Redaction;

public interface IRedactionBuilder
{
    IServiceCollection Services { get; }
    IRedactionBuilder SetRedactor<T>(params DataClassification[] classifications) where T : Redactor;
    IRedactionBuilder SetFallbackRedactor<T>() where T : Redactor;
}

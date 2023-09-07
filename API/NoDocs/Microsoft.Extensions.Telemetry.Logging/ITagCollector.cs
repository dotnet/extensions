// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public interface ITagCollector
{
    void Add(string tagName, object? tagValue);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    void Add(string tagName, object? tagValue, DataClassification classification);
}

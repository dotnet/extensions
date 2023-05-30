// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public interface IEnrichmentPropertyBag
{
    void Add(string key, object value);
    void Add(string key, string value);
    void Add(ReadOnlySpan<KeyValuePair<string, object>> properties);
    void Add(ReadOnlySpan<KeyValuePair<string, string>> properties);
}

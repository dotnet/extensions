// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect tags in the modern logging path.
    /// </summary>
    internal sealed class EnrichmentTagCollector(List<KeyValuePair<string, object?>> extraTags) : IEnrichmentTagCollector
    {
        public void Add(string key, object value) => extraTags.Add(new KeyValuePair<string, object?>(key, value));
        public void AddRange(IEnumerable<KeyValuePair<string, object?>> items) => extraTags.AddRange(items);
    }
}

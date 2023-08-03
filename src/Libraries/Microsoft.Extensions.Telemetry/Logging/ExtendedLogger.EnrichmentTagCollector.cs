// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect tags in the modern logging path.
    /// </summary>
    internal sealed class EnrichmentTagCollector(List<KeyValuePair<string, object?>> extraTags) : IEnrichmentTagCollector
    {
        public void Add(string key, object value) => extraTags.Add(new KeyValuePair<string, object?>(key, value));
        public void Add(string key, string value) => extraTags.Add(new KeyValuePair<string, object?>(key, value));

        public void Add(ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            foreach (var p in tags)
            {
                // we're going from KVP<string, object> to KVP<string, object?> which is strictly correct, so ignore the complaint
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                extraTags.Add(p);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
        }

        public void Add(ReadOnlySpan<KeyValuePair<string, string>> tags)
        {
            foreach (var p in tags)
            {
                extraTags.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object?>> items) => extraTags.AddRange(items);
    }
}

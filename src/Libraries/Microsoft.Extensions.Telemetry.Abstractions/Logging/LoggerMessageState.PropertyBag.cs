// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState
{
    private sealed class PropertyBag : IEnrichmentPropertyBag
    {
        private readonly List<KeyValuePair<string, object?>> _properties;

        public PropertyBag(List<KeyValuePair<string, object?>> properties)
        {
            _properties = properties;
        }

        void IEnrichmentPropertyBag.Add(string key, object value)
        {
            _properties.Add(new KeyValuePair<string, object?>(key, value));
        }

        /// <inheritdoc/>
        void IEnrichmentPropertyBag.Add(string key, string value)
        {
            _properties.Add(new KeyValuePair<string, object?>(key, value));
        }

        /// <inheritdoc/>
        void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
        {
            foreach (var p in properties)
            {
                // we're going from KVP<string, object> to KVP<string, object?> which is strictly correct, so ignore the complaint
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                _properties.Add(p);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
        }

        /// <inheritdoc/>
        void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
        {
            foreach (var p in properties)
            {
                _properties.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
        }
    }
}

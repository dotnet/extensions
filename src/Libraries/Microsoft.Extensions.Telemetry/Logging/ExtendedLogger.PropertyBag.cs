// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect properties in the legacy logging path.
    /// </summary>
    internal sealed class PropertyBag : IReadOnlyList<KeyValuePair<string, object?>>, IEnrichmentPropertyBag
    {
        public object? Formatter;
        public object? State;
        public IReadOnlyList<KeyValuePair<string, object?>> DynamicProperties = null!;
        public KeyValuePair<string, object?>[] StaticProperties = null!;

        private readonly List<KeyValuePair<string, object?>> _properties = new();

        public void Clear()
        {
            DynamicProperties = Array.Empty<KeyValuePair<string, object?>>();
            _properties.Clear();
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < DynamicProperties.Count)
                {
                    return DynamicProperties[index];
                }
                else if (index < DynamicProperties.Count + _properties.Count)
                {
                    return _properties[index - DynamicProperties.Count];
                }
                else
                {
                    return StaticProperties[index - DynamicProperties.Count - _properties.Count];
                }
            }
        }

        public int Count => DynamicProperties.Count + _properties.Count + StaticProperties.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            if (DynamicProperties != null)
            {
                foreach (var p in DynamicProperties)
                {
                    yield return p;
                }
            }

            foreach (var p in _properties)
            {
                yield return p;
            }

            foreach (var p in StaticProperties)
            {
                yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(string key, object value) => _properties.Add(new KeyValuePair<string, object?>(key, value));
        public void Add(string key, string value) => _properties.Add(new KeyValuePair<string, object?>(key, value));

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

        void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
        {
            foreach (var p in properties)
            {
                _properties.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object?>> properties) => _properties.AddRange(properties);
        public KeyValuePair<string, object?>[] ToArray() => _properties.ToArray();
    }
}

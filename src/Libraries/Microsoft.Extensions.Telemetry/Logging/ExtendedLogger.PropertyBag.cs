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
        public readonly KeyValuePair<string, object?>[] StaticProperties;
        public object? Formatter;
        public object? State;
        public IReadOnlyList<KeyValuePair<string, object?>> IncomingProperties = null!;

        private const int PropCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraProperties = new(PropCapacity);

        public PropertyBag(KeyValuePair<string, object?>[] staticProperties)
        {
            StaticProperties = staticProperties;
        }

        public void Clear()
        {
            IncomingProperties = Array.Empty<KeyValuePair<string, object?>>();
            _extraProperties.Clear();
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < IncomingProperties.Count)
                {
                    return IncomingProperties[index];
                }
                else if (index < IncomingProperties.Count + _extraProperties.Count)
                {
                    return _extraProperties[index - IncomingProperties.Count];
                }
                else
                {
                    return StaticProperties[index - IncomingProperties.Count - _extraProperties.Count];
                }
            }
        }

        public int Count => IncomingProperties.Count + _extraProperties.Count + StaticProperties.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            if (IncomingProperties != null)
            {
                int count = IncomingProperties.Count;
                for (int i = 0; i < count; i++)
                {
                    yield return IncomingProperties[i];
                }
            }

            foreach (var p in _extraProperties)
            {
                yield return p;
            }

            foreach (var p in StaticProperties)
            {
                yield return p;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(string key, object value) => _extraProperties.Add(new KeyValuePair<string, object?>(key, value));
        public void Add(string key, string value) => _extraProperties.Add(new KeyValuePair<string, object?>(key, value));

        void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
        {
            foreach (var p in properties)
            {
                // we're going from KVP<string, object> to KVP<string, object?> which is strictly correct, so ignore the complaint
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                _extraProperties.Add(p);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
        }

        void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
        {
            foreach (var p in properties)
            {
                _extraProperties.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object?>> properties) => _extraProperties.AddRange(properties);
        public KeyValuePair<string, object?>[] ToArray() => _extraProperties.ToArray();
    }
}

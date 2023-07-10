// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect properties in the modern logging path.
    /// </summary>
    internal sealed class PropertyJoiner : IReadOnlyList<KeyValuePair<string, object?>>, IEnrichmentPropertyBag
    {
        public KeyValuePair<string, object?>[]? StaticProperties;
        public Func<LoggerMessageState, Exception?, string>? Formatter;
        public LoggerMessageState? State;

        private const int PropCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraProperties = new(PropCapacity);
        private LoggerMessageState? _incomingProperties;

        public void Clear()
        {
            _extraProperties.Clear();
            _incomingProperties = null;
            State = null;
            Formatter = null;
        }

        [MemberNotNull(nameof(_incomingProperties))]
        public void SetIncomingProperties(LoggerMessageState value)
        {
            _incomingProperties = value;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingProperties!.NumProperties)
                {
                    return _incomingProperties[index];
                }
                else if (index < _incomingProperties.NumProperties + _extraProperties.Count)
                {
                    return _extraProperties[index - _incomingProperties.NumProperties];
                }
                else
                {
                    return StaticProperties![index - _incomingProperties.NumProperties - _extraProperties.Count];
                }
            }
        }

        public int Count => _incomingProperties!.NumProperties + _extraProperties.Count + StaticProperties!.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(string key, object value) => _extraProperties.Add(new KeyValuePair<string, object?>(key, value));
        public void Add(string key, string value) => _extraProperties.Add(new KeyValuePair<string, object?>(key, value));

        public void Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
        {
            foreach (var p in properties)
            {
                // we're going from KVP<string, object> to KVP<string, object?> which is strictly correct, so ignore the complaint
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                _extraProperties.Add(p);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
        }

        public void Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
        {
            foreach (var p in properties)
            {
                _extraProperties.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
        }
    }
}

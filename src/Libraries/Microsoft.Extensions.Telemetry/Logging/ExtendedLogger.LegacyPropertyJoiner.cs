// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect properties in the legacy logging path.
    /// </summary>
    internal sealed class LegacyPropertyJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public KeyValuePair<string, object?>[]? StaticProperties;
        public object? Formatter;
        public object? State;

        private const int PropCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraProperties = new(PropCapacity);
        private IReadOnlyList<KeyValuePair<string, object?>>? _incomingProperties;
        private int _incomingPropertyCount;

        public LegacyPropertyJoiner()
        {
            PropertyBag = new(_extraProperties);
        }

        public EnrichmentPropertyBag PropertyBag { get; }

        public void Clear()
        {
            _extraProperties.Clear();
            _incomingProperties = null;
            _incomingPropertyCount = 0;
            State = null;
            Formatter = null;
        }

        [MemberNotNull(nameof(_incomingProperties))]
        public void SetIncomingProperties(IReadOnlyList<KeyValuePair<string, object?>> value)
        {
            _incomingProperties = value;
            _incomingPropertyCount = _incomingProperties.Count;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingPropertyCount)
                {
                    return _incomingProperties![index];
                }
                else if (index < _incomingPropertyCount + _extraProperties.Count)
                {
                    return _extraProperties[index - _incomingPropertyCount];
                }
                else
                {
                    return StaticProperties![index - _incomingPropertyCount - _extraProperties.Count];
                }
            }
        }

        public int Count => _incomingPropertyCount + _extraProperties.Count + StaticProperties!.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

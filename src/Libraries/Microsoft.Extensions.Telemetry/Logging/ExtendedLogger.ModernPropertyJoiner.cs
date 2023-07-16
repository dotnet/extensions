// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect properties in the modern logging path.
    /// </summary>
    internal sealed class ModernPropertyJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public KeyValuePair<string, object?>[]? StaticProperties;
        public Func<LoggerMessageState, Exception?, string>? Formatter;
        public LoggerMessageState? State;

        private const int PropCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraProperties = new(PropCapacity);
        private KeyValuePair<string, object?>[]? _incomingProperties;
        private KeyValuePair<string, object?>[]? _redactedProperties;
        private int _incomingPropertiesCount;
        private int _redactedPropertiesCount;

        public ModernPropertyJoiner()
        {
            PropertyBag = new(_extraProperties);
        }

        public EnrichmentPropertyBag PropertyBag { get; }

        public void Clear()
        {
            _extraProperties.Clear();
            _incomingProperties = null;
            _redactedProperties = null;
            State = null;
            Formatter = null;
        }

        [MemberNotNull(nameof(_incomingProperties))]
        public void SetIncomingProperties(LoggerMessageState value)
        {
            _incomingProperties = value.PropertyArray;
            _incomingPropertiesCount = value.NumProperties;

            _redactedProperties = value.RedactedPropertyArray;
            _redactedPropertiesCount = value.NumRedactedProperties;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingPropertiesCount)
                {
                    return _incomingProperties![index];
                }
                else if (index < _incomingPropertiesCount + _redactedPropertiesCount)
                {
                    return _redactedProperties![index - _incomingPropertiesCount];
                }
                else if (index < _incomingPropertiesCount + _redactedPropertiesCount + _extraProperties.Count)
                {
                    return _extraProperties[index - _incomingPropertiesCount - _redactedPropertiesCount];
                }
                else
                {
                    return StaticProperties![index - _incomingPropertiesCount - _redactedPropertiesCount - _extraProperties.Count];
                }
            }
        }

        public int Count => _incomingPropertiesCount + _redactedPropertiesCount + _extraProperties.Count + StaticProperties!.Length;

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

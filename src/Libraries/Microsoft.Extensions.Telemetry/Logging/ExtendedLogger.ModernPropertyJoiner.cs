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
        private int _incomingPropertiesCount;

        public ModernPropertyJoiner()
        {
            PropertyBag = new(_extraProperties);
        }

        public EnrichmentPropertyBag PropertyBag { get; }

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
            _incomingProperties = value.PropertyArray;
            _incomingPropertiesCount = value.NumProperties;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingPropertiesCount)
                {
                    return _incomingProperties![index];
                }
                else if (index < _incomingPropertiesCount + _extraProperties.Count)
                {
                    return _extraProperties[index - _incomingPropertiesCount];
                }
                else
                {
                    return StaticProperties![index - _incomingPropertiesCount - _extraProperties.Count];
                }
            }
        }

        public int Count => _incomingPropertiesCount + _extraProperties.Count + StaticProperties!.Length;

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Logging;

internal sealed partial class ExtendedLogger
{
    /// <summary>
    /// Used to collect tags in the legacy logging path.
    /// </summary>
    internal sealed class LegacyTagJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public KeyValuePair<string, object?>[]? StaticTags;
        public object? Formatter;
        public object? State;

        private const int TagCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraTags = new(TagCapacity);
        private IReadOnlyList<KeyValuePair<string, object?>>? _incomingTags;
        private int _incomingTagCount;

        public LegacyTagJoiner()
        {
            EnrichmentTagCollector = new(_extraTags);
        }

        public EnrichmentTagCollector EnrichmentTagCollector { get; }

        public void Clear()
        {
            _extraTags.Clear();
            _incomingTags = null;
            _incomingTagCount = 0;
            State = null;
            Formatter = null;
        }

        [MemberNotNull(nameof(_incomingTags))]
        public void SetIncomingTags(IReadOnlyList<KeyValuePair<string, object?>> value)
        {
            _incomingTags = value;
            _incomingTagCount = _incomingTags.Count;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingTagCount)
                {
                    return _incomingTags![index];
                }
                else if (index < _incomingTagCount + _extraTags.Count)
                {
                    return _extraTags[index - _incomingTagCount];
                }
                else
                {
                    return StaticTags![index - _incomingTagCount - _extraTags.Count];
                }
            }
        }

        public int Count => _incomingTagCount + _extraTags.Count + StaticTags!.Length;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string? ToString() => State?.ToString();
    }
}

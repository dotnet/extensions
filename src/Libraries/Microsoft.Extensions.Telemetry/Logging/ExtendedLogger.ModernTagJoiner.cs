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
    /// Used to collect tags in the modern logging path.
    /// </summary>
    internal sealed class ModernTagJoiner : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public KeyValuePair<string, object?>[]? StaticTags;
        public Func<LoggerMessageState, Exception?, string>? Formatter;
        public LoggerMessageState? State;

        private const int TagCapacity = 4;
        private readonly List<KeyValuePair<string, object?>> _extraTags = new(TagCapacity);
        private KeyValuePair<string, object?>[]? _incomingTags;
        private KeyValuePair<string, object?>[]? _redactedTags;
        private int _incomingTagsCount;
        private int _redactedTagsCount;

        public ModernTagJoiner()
        {
            EnrichmentTagCollector = new(_extraTags);
        }

        public EnrichmentTagCollector EnrichmentTagCollector { get; }

        public void Clear()
        {
            _extraTags.Clear();
            _incomingTags = null;
            _redactedTags = null;
            State = null;
            Formatter = null;
        }

        [MemberNotNull(nameof(_incomingTags))]
        public void SetIncomingTags(LoggerMessageState value)
        {
            _incomingTags = value.TagArray;
            _incomingTagsCount = value.TagsCount;

            _redactedTags = value.RedactedTagArray;
            _redactedTagsCount = value.ClassifiedTagsCount;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < _incomingTagsCount)
                {
                    return _incomingTags![index];
                }
                else if (index < _incomingTagsCount + _redactedTagsCount)
                {
                    return _redactedTags![index - _incomingTagsCount];
                }
                else if (index < _incomingTagsCount + _redactedTagsCount + _extraTags.Count)
                {
                    return _extraTags[index - _incomingTagsCount - _redactedTagsCount];
                }
                else
                {
                    return StaticTags![index - _incomingTagsCount - _redactedTagsCount - _extraTags.Count];
                }
            }
        }

        public int Count => _incomingTagsCount + _redactedTagsCount + _extraTags.Count + StaticTags!.Length;

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

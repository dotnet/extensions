// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// Represents zero (null), one (string) or more (string[]) tags, avoiding the additional array overhead when necessary.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1066:Implement IEquatable when overriding Object.Equals", Justification = "Equals throws by intent")]
[System.Diagnostics.DebuggerDisplay("{ToString()}")]
internal readonly struct TagSet
{
    public static readonly TagSet Empty = default!;

    // this array is used in CopyTo to efficiently copy out of collections
    [ThreadStatic]
    private static string[]? _perThreadSingleLengthArray;

    private readonly object? _tagOrTags;

    internal TagSet(string tag)
    {
        Validate(tag);
        _tagOrTags = tag;
    }

    internal TagSet(string[] tags)
    {
        Debug.Assert(tags is { Length: > 1 }, "should be non-trivial array");
        foreach (var tag in tags)
        {
            Validate(tag);
        }

        Array.Sort(tags, StringComparer.InvariantCulture);
        _tagOrTags = tags;
    }

    public string GetSinglePrechecked() => (string)_tagOrTags!; // we expect this to fail if used on incorrect types
    public Span<string> GetSpanPrechecked() => (string[])_tagOrTags!; // we expect this to fail if used on incorrect types

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Intentional; should not be used")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods", Justification = "Intentional; should not be used")]
    public override bool Equals(object? obj) => throw new NotSupportedException();

    // [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Intentional; should not be used")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods", Justification = "Intentional; should not be used")]
    public override int GetHashCode() => throw new NotSupportedException();

    public override string ToString() => _tagOrTags switch
    {
        string tag => tag,
        string[] tags => string.Join(", ", tags),
        _ => "(no tags)",
    };

    public bool IsEmpty => _tagOrTags is null;

    public int Count => _tagOrTags switch
    {
        null => 0,
        string => 1,
        string[] arr => arr.Length,
        _ => 0, // should never happen, but treat as empty
    };

    internal bool IsArray => _tagOrTags is string[];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "This is the most appropriate exception here.")]
    public string this[int index] => _tagOrTags switch
    {
        string tag when index == 0 => tag,
        string[] tags => tags[index],
        _ => throw new IndexOutOfRangeException(nameof(index)),
    };

    public void CopyTo(Span<string> target)
    {
        switch (_tagOrTags)
        {
            case string tag:
                target[0] = tag;
                break;
            case string[] tags:
                tags.CopyTo(target);
                break;
        }
    }

    internal static TagSet Create(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return Empty;
        }

        // note that in multi-tag scenarios we always create a defensive copy
        if (tags is ICollection<string> collection)
        {
            switch (collection.Count)
            {
                case 0:
                    return Empty;
                case 1 when collection is IList<string> list:
                    return new TagSet(list[0]);
                case 1:
                    // avoid the GetEnumerator() alloc
                    var arr = _perThreadSingleLengthArray ??= new string[1];
                    collection.CopyTo(arr, 0);
                    return new TagSet(arr[0]);
                default:
                    arr = new string[collection.Count];
                    collection.CopyTo(arr, 0);
                    return new TagSet(arr);
            }
        }

        // perhaps overkill, but: avoid as much as possible when unrolling
        using var iterator = tags.GetEnumerator();
        if (!iterator.MoveNext())
        {
            return Empty;
        }

        var firstTag = iterator.Current;
        if (!iterator.MoveNext())
        {
            return new TagSet(firstTag);
        }

        string[] oversized = ArrayPool<string>.Shared.Rent(8);
        oversized[0] = firstTag;
        int count = 1;
        do
        {
            if (count == oversized.Length)
            {
                // grow
                var bigger = ArrayPool<string>.Shared.Rent(count * 2);
                oversized.CopyTo(bigger, 0);
                ArrayPool<string>.Shared.Return(oversized);
                oversized = bigger;
            }

            oversized[count++] = iterator.Current;
        }
        while (iterator.MoveNext());

        if (count == oversized.Length)
        {
            return new TagSet(oversized);
        }
        else
        {
            var final = oversized.AsSpan(0, count).ToArray();
            ArrayPool<string>.Shared.Return(oversized);
            return new TagSet(final);
        }
    }

    internal string[] ToArray() // for testing only
    {
        var arr = new string[Count];
        CopyTo(arr);
        return arr;
    }

    internal const string WildcardTag = "*";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "Not needed")]
    internal bool TryFind(ReadOnlySpan<char> span, [NotNullWhen(true)] out string? tag)
    {
        switch (_tagOrTags)
        {
            case string single when span.SequenceEqual(single.AsSpan()):
                tag = single;
                return true;
            case string[] tags:
                foreach (string test in tags)
                {
                    if (span.SequenceEqual(test.AsSpan()))
                    {
                        tag = test;
                        return true;
                    }
                }

                break;
        }

        tag = null;
        return false;
    }

    internal int MaxLength()
    {
        switch (_tagOrTags)
        {
            case string single:
                return single.Length;
            case string[] tags:
                int max = 0;
                foreach (string test in tags)
                {
                    max = Math.Max(max, test.Length);
                }

                return max;
            default:
                return 0;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3928:Parameter names used into ArgumentException constructors should match an existing one ",
        Justification = "Using parameter name from public callable API")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Using parameter name from public callable API")]
    private static void Validate(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            ThrowEmpty();
        }

        if (tag == WildcardTag)
        {
            ThrowReserved();
        }

        static void ThrowEmpty() => throw new ArgumentException("Tags cannot be empty.", "tags");
        static void ThrowReserved() => throw new ArgumentException($"The tag '{WildcardTag}' is reserved and cannot be used in this context.", "tags");
    }
}

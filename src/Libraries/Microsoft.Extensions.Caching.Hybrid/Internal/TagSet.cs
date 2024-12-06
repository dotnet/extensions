// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// Represents zero (null), one (string) or more (string[]) tags, avoiding the additional array overhead when necessary.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1066:Implement IEquatable when overriding Object.Equals", Justification = "Equals throws by intent")]
internal readonly struct TagSet
{
    public static readonly TagSet Empty = default!;

    private readonly object? _tagOrTags;

    private TagSet(string tag)
    {
        Validate(tag);
        _tagOrTags = tag;
    }

    private TagSet(string[] tags)
    {
        Debug.Assert(tags is { Length: > 1 }, "should be non-trivial array");
        foreach (var tag in tags)
        {
            Validate(tag);
        }

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
                    var arr = ArrayPool<string>.Shared.Rent(1);
                    collection.CopyTo(arr, 0);
                    string tag = arr[0];
                    ArrayPool<string>.Shared.Return(arr);
                    return new TagSet(tag);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Microsoft.Shared.Collections.Test;

public class EmptyCollectionExtensionsTests
{
    internal static void Verify<T>()
        where T : notnull
    {
        EmptyCollectionExtensions.EmptyIfNull((IEnumerable<T>?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyCollection<T>?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyList<T>?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((ICollection<T>?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((IList<T>?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((T[]?)null).Should().BeEmpty();
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyDictionary<T, T>?)null).Should().BeEmpty();

        var input = new List<T>();
        EmptyCollectionExtensions.EmptyIfNull((IEnumerable<T>)input).Should().BeEmpty().And.NotBeSameAs(input);
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyCollection<T>)input).Should().BeEmpty().And.NotBeSameAs(input);
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyList<T>)input).Should().BeEmpty().And.NotBeSameAs(input);
        EmptyCollectionExtensions.EmptyIfNull((ICollection<T>)input).Should().BeEmpty().And.NotBeSameAs(input);
        EmptyCollectionExtensions.EmptyIfNull((IList<T>)input).Should().BeEmpty().And.NotBeSameAs(input);

        var empty = new T[0];
        EmptyCollectionExtensions.EmptyIfNull(empty).Should().BeEmpty().And.NotBeSameAs(empty);

        var nonempty = new T[1];
        EmptyCollectionExtensions.EmptyIfNull((IEnumerable<T>)nonempty).Should().BeSameAs(nonempty);
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyCollection<T>)nonempty).Should().BeSameAs(nonempty);
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyList<T>)nonempty).Should().BeSameAs(nonempty);
        EmptyCollectionExtensions.EmptyIfNull((ICollection<T>)nonempty).Should().BeSameAs(nonempty);
        EmptyCollectionExtensions.EmptyIfNull((IList<T>)nonempty).Should().BeSameAs(nonempty);
        EmptyCollectionExtensions.EmptyIfNull(nonempty).Should().BeSameAs(nonempty);

        var enumerable = new Enumerable<T>();
        EmptyCollectionExtensions.EmptyIfNull(enumerable).Should().BeSameAs(enumerable);

        var coll = new Collection<T>();
        EmptyCollectionExtensions.EmptyIfNull((IEnumerable<T>)coll).Should().NotBeSameAs(coll);

        var dictionary = new Dictionary<T, T>();
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyDictionary<T, T>?)dictionary).Should().NotBeSameAs(dictionary);

        dictionary.Add(default!, default!);
        EmptyCollectionExtensions.EmptyIfNull((IReadOnlyDictionary<T, T>?)dictionary).Should().BeSameAs(dictionary);
    }

    [Fact]
    public void Tests()
    {
        Verify<int>();
    }

    [Fact]
    public void EmptyReadOnlyListTests()
    {
        var nothing = EmptyReadOnlyList<int>.Instance;
        Assert.Empty(nothing);

        var count = 0;
        foreach (var _ in nothing)
        {
            count++;
        }

        Assert.Equal(0, count);
        Assert.Throws<ArgumentOutOfRangeException>(() => nothing[0]);
    }

    private sealed class Enumerable<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class Collection<T> : ICollection<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear()
        {
            // nothing to clear
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // nothing to copy
        }

        public void Add(T item) => throw new NotSupportedException();
        public bool Contains(T item) => false;
        public bool Remove(T item) => false;
        public int Count => 0;
        public bool IsReadOnly => true;
    }
}

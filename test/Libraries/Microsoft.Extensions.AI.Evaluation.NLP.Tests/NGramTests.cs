// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

public class NGramTests
{
    [Fact]
    public void Constructor_ValuesAndLength()
    {
        var ngram = new NGram<int>(1, 2, 3);
        Assert.Equal(new[] { 1, 2, 3 }, ngram.Values);
        Assert.Equal(3, ngram.Length);
    }

    [Fact]
    public void Constructor_ThrowsOnEmpty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NGram<int>(Array.Empty<int>()));
    }

    [Fact]
    public void Equals_And_HashCode_WorkCorrectly()
    {
        var a = new NGram<int>(1, 2, 3);
        var b = new NGram<int>(1, 2, 3);
        var c = new NGram<int>(3, 2, 1);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(c));
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    [Fact]
    public void Enumerator_And_IEnumerable()
    {
        var ngram = new NGram<char>('a', 'b', 'c');
        var list = ngram.ToList();
        Assert.Equal(new[] { 'a', 'b', 'c' }, list);
    }

    [Fact]
    public void ToDebugString_FormatsCorrectly()
    {
        var ngram = new NGram<string>("x", "y");
        Assert.Equal("[x,y]", ngram.ToDebugString());
    }

    [Fact]
    public void NGramBuilder_Create_Works()
    {
        NGram<int> ngram = [1, 2];
        Assert.Equal(new NGram<int>(1, 2), ngram);
    }

    [Fact]
    public void CreateNGrams()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new int[0].CreateNGrams(-1).ToList());

        ReadOnlySpan<int> data = [1, 2, 3];

        var nGram = data.CreateNGrams(1);
        Assert.Equal([[1], [2], [3]], nGram);

        nGram = data.CreateNGrams(2);
        Assert.Equal([[1, 2], [2, 3]], nGram);

        nGram = data.CreateNGrams(3);
        Assert.Equal([[1, 2, 3]], nGram);

        nGram = data.CreateNGrams(4);
        Assert.Equal([], nGram);
    }

    [Fact]
    public void CreateAllNGrams()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new int[0].CreateAllNGrams(-1).ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => new int[0].CreateAllNGrams(0).ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => new int[0].CreateAllNGrams(1, 0).ToList());

        ReadOnlySpan<int> arr = [1, 2, 3];

        var nGram = arr.CreateAllNGrams(1).ToList();
        Assert.Equal([[1], [1, 2], [1, 2, 3], [2], [2, 3], [3]], nGram);

        nGram = arr.CreateAllNGrams(2).ToList();
        Assert.Equal([[1, 2], [1, 2, 3], [2, 3]], nGram);

        nGram = arr.CreateAllNGrams(3).ToList();
        Assert.Equal([[1, 2, 3]], nGram);

        nGram = arr.CreateAllNGrams(3, 5).ToList();
        Assert.Equal([[1, 2, 3]], nGram);

        nGram = arr.CreateAllNGrams(1, 2).ToList();
        Assert.Equal([[1], [1, 2], [2], [2, 3], [3]], nGram);

        nGram = arr.CreateAllNGrams(1, 1).ToList();
        Assert.Equal([[1], [2], [3]], nGram);

        nGram = arr.CreateAllNGrams(4).ToList();
        Assert.Equal([], nGram);
    }
}

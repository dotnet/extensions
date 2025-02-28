// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection

namespace Microsoft.Extensions.AI;

public class GeneratedEmbeddingsTests
{
    [Fact]
    public void Ctor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("embeddings", () => new GeneratedEmbeddings<Embedding<float>>(null!));
        Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new GeneratedEmbeddings<Embedding<float>>(-1));
    }

    [Fact]
    public void Ctor_ValidArgs_NoExceptions()
    {
        GeneratedEmbeddings<Embedding<float>>[] instances =
        [
            [],
            new(0),
            new(42),
            new([])
        ];

        foreach (var instance in instances)
        {
            Assert.Empty(instance);

            Assert.False(((ICollection<Embedding<float>>)instance).IsReadOnly);
            Assert.Equal(0, instance.Count);

            Assert.False(instance.Contains(new Embedding<float>(new float[] { 1, 2, 3 })));
            Assert.False(instance.Contains(null!));

            Assert.Equal(-1, instance.IndexOf(new Embedding<float>(new float[] { 1, 2, 3 })));
            Assert.Equal(-1, instance.IndexOf(null!));

            instance.CopyTo(Array.Empty<Embedding<float>>(), 0);

            Assert.Throws<ArgumentOutOfRangeException>("index", () => instance[0]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => instance[-1]);
        }
    }

    [Fact]
    public void Ctor_RoundtripsEnumerable()
    {
        List<Embedding<float>> embeddings =
        [
            new(new float[] { 1, 2, 3 }),
            new(new float[] { 4, 5, 6 }),
        ];

        var generatedEmbeddings = new GeneratedEmbeddings<Embedding<float>>(embeddings);

        Assert.Equal(embeddings, generatedEmbeddings);
        Assert.Equal(2, generatedEmbeddings.Count);

        Assert.Same(embeddings[0], generatedEmbeddings[0]);
        Assert.Same(embeddings[1], generatedEmbeddings[1]);

        Assert.Equal(0, generatedEmbeddings.IndexOf(embeddings[0]));
        Assert.Equal(1, generatedEmbeddings.IndexOf(embeddings[1]));

        Assert.True(generatedEmbeddings.Contains(embeddings[0]));
        Assert.True(generatedEmbeddings.Contains(embeddings[1]));

        Assert.False(generatedEmbeddings.Contains(null!));
        Assert.Equal(-1, generatedEmbeddings.IndexOf(null!));

        Assert.Throws<ArgumentOutOfRangeException>("index", () => generatedEmbeddings[-1]);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => generatedEmbeddings[2]);

        Assert.True(embeddings.SequenceEqual(generatedEmbeddings));

        var e = new Embedding<float>(new float[] { 7, 8, 9 });
        generatedEmbeddings.Add(e);
        Assert.Equal(3, generatedEmbeddings.Count);
        Assert.Same(e, generatedEmbeddings[2]);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        Assert.Null(embeddings.Usage);

        UsageDetails usage = new();
        embeddings.Usage = usage;
        Assert.Same(usage, embeddings.Usage);
        embeddings.Usage = null;
        Assert.Null(embeddings.Usage);

        Assert.Null(embeddings.AdditionalProperties);
        AdditionalPropertiesDictionary props = [];
        embeddings.AdditionalProperties = props;
        Assert.Same(props, embeddings.AdditionalProperties);
        embeddings.AdditionalProperties = null;
        Assert.Null(embeddings.AdditionalProperties);
    }

    [Fact]
    public void Add()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];
        var e = new Embedding<float>(new float[] { 1, 2, 3 });

        embeddings.Add(e);
        Assert.Equal(1, embeddings.Count);
        Assert.Same(e, embeddings[0]);
    }

    [Fact]
    public void AddRange()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });

        Assert.Equal(2, embeddings.Count);
        Assert.Same(e1, embeddings[0]);
        Assert.Same(e2, embeddings[1]);
    }

    [Fact]
    public void Clear()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        embeddings.Clear();
        Assert.Equal(0, embeddings.Count);
        Assert.Empty(embeddings);
    }

    [Fact]
    public void Remove()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        Assert.True(embeddings.Remove(e1));
        Assert.Equal(1, embeddings.Count);
        Assert.Same(e2, embeddings[0]);

        Assert.False(embeddings.Remove(e1));
        Assert.Equal(1, embeddings.Count);
        Assert.Same(e2, embeddings[0]);

        Assert.True(embeddings.Remove(e2));
        Assert.Equal(0, embeddings.Count);
    }

    [Fact]
    public void RemoveAt()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        embeddings.RemoveAt(0);
        Assert.Equal(1, embeddings.Count);
        Assert.Same(e2, embeddings[0]);

        embeddings.RemoveAt(0);
        Assert.Equal(0, embeddings.Count);
    }

    [Fact]
    public void Insert()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        var e3 = new Embedding<float>(new float[] { 7, 8, 9 });
        embeddings.Insert(1, e3);
        Assert.Equal(3, embeddings.Count);
        Assert.Same(e3, embeddings[1]);
        Assert.Same(e2, embeddings[2]);
    }

    [Fact]
    public void Indexer()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        var e3 = new Embedding<float>(new float[] { 7, 8, 9 });
        embeddings[1] = e3;
        Assert.Equal(2, embeddings.Count);
        Assert.Same(e1, embeddings[0]);
        Assert.Same(e3, embeddings[1]);
    }

    [Fact]
    public void Indexer_InvalidIndex_Throws()
    {
        GeneratedEmbeddings<Embedding<float>> embeddings = [];

        var e1 = new Embedding<float>(new float[] { 1, 2, 3 });
        var e2 = new Embedding<float>(new float[] { 4, 5, 6 });

        embeddings.AddRange(new[] { e1, e2 });
        Assert.Equal(2, embeddings.Count);

        Assert.Throws<ArgumentOutOfRangeException>("index", () => embeddings[-1]);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => embeddings[2]);
    }

    [Fact]
    public async Task Generator_SupportsCovariantInput()
    {
        var expectedGeneratedEmbeddings = new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1, 2, 3 })]);

        using IEmbeddingGenerator<object, Embedding<float>> acceptsObject = new TestEmbeddingGenerator<object, Embedding<float>>
        {
            GenerateAsyncCallback = (values, options, cancellationToken) => Task.FromResult(expectedGeneratedEmbeddings),
        };

        IEmbeddingGenerator<string, Embedding<float>> acceptsString = acceptsObject;

        var actual = await acceptsString.GenerateAsync(["hello"]);

        Assert.Same(expectedGeneratedEmbeddings, actual);
    }
}

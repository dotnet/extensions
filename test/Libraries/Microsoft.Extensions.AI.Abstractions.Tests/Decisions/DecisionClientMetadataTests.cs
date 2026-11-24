// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionClientMetadataTests
{
    [Fact]
    public void Constructor_DefaultsToUnknownValues()
    {
        DecisionClientMetadata metadata = new();

        Assert.Null(metadata.ProviderName);
        Assert.Null(metadata.ProviderUri);
        Assert.Null(metadata.DefaultModelId);
        Assert.Null(metadata.SupportedKinds);
    }

    [Fact]
    public void Constructor_SetsValues()
    {
        Uri uri = new("https://example.com/");
        DecisionClientMetadata metadata = new("provider", uri, "model", [DecisionKind.Score]);

        Assert.Equal("provider", metadata.ProviderName);
        Assert.Same(uri, metadata.ProviderUri);
        Assert.Equal("model", metadata.DefaultModelId);
        Assert.Equal([DecisionKind.Score], metadata.SupportedKinds!);
    }

    [Fact]
    public void SupportedKinds_DistinguishesUnknownFromEmpty()
    {
        Assert.Null(new DecisionClientMetadata(supportedKinds: null).SupportedKinds);

        IReadOnlyList<DecisionKind>? empty = new DecisionClientMetadata(supportedKinds: []).SupportedKinds;
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public void SupportedKinds_DeduplicatesPreservingFirstOccurrenceOrder()
    {
        DecisionClientMetadata metadata = new(supportedKinds: [DecisionKind.Choice, DecisionKind.Binary, DecisionKind.Choice, DecisionKind.Binary]);

        Assert.Equal([DecisionKind.Choice, DecisionKind.Binary], metadata.SupportedKinds!);
    }

    [Fact]
    public void SupportedKinds_RejectsUndefinedKinds()
    {
        Assert.Throws<ArgumentException>("supportedKinds", () => new DecisionClientMetadata(supportedKinds: [DecisionKind.Binary, (DecisionKind)99]));
    }

    [Fact]
    public void SupportedKinds_IsOwnedSnapshot()
    {
        List<DecisionKind> kinds = [DecisionKind.Binary];
        DecisionClientMetadata metadata = new(supportedKinds: kinds);

        kinds.Add(DecisionKind.Score);
        kinds[0] = DecisionKind.Choice;

        Assert.Equal([DecisionKind.Binary], metadata.SupportedKinds!);
        Assert.NotSame(kinds, metadata.SupportedKinds);
        Assert.Throws<NotSupportedException>(() => ((IList<DecisionKind>)metadata.SupportedKinds!).Add(DecisionKind.Score));
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class TagJoinerTests
{
    // Bounds the enumeration of a joiner so that a test fails, rather than runs out of memory, if the enumeration never ends.
    private const int MaxTags = 100;

    [Fact]
    public static void LegacyTagJoiner_AddingItsOwnTags_AddsTagsPresentWhenEnumerationStarted()
    {
        var joiner = new ExtendedLogger.LegacyTagJoiner { StaticTags = [new("S1", "SV1")] };
        joiner.EnrichmentTagCollector.Add("E1", "EV1");
        joiner.SetIncomingTags(new List<KeyValuePair<string, object?>> { new("I1", "IV1"), new("I2", "IV2") });

        joiner.EnrichmentTagCollector.AddRange(joiner.Append(new("A1", "AV1")).Take(MaxTags));

        KeyValuePair<string, object?>[] originalTags = [new("S1", "SV1"), new("E1", "EV1"), new("I1", "IV1"), new("I2", "IV2")];
        AssertTags([.. originalTags, .. originalTags, new("A1", "AV1")], joiner);
    }

    [Fact]
    public static void ModernTagJoiner_AddingItsOwnTags_AddsTagsPresentWhenEnumerationStarted()
    {
        var state = new LoggerMessageState();
        state.AddTag("I1", "IV1");
        state.AddTag("I2", "IV2");
        int index = state.ReserveClassifiedTagSpace(1);
        state.RedactedTagArray[index] = new("R1", "RV1");

        var joiner = new ExtendedLogger.ModernTagJoiner { StaticTags = [new("S1", "SV1")] };
        joiner.SetIncomingTags(state);
        joiner.EnrichmentTagCollector.Add("E1", "EV1");

        joiner.EnrichmentTagCollector.AddRange(joiner.Append(new("A1", "AV1")).Take(MaxTags));

        KeyValuePair<string, object?>[] originalTags = [new("R1", "RV1"), new("E1", "EV1"), new("S1", "SV1"), new("I1", "IV1"), new("I2", "IV2")];
        AssertTags([.. originalTags, .. originalTags, new("A1", "AV1")], joiner);
    }

    [Fact]
    public static void ModernTagJoiner_ReturnsRedactedTagsAsStrings()
    {
        var redactor = FakeRedactor.Create(new FakeRedactorOptions { RedactionFormat = "R<{0}>" });
        var jr = JustInTimeRedactor.Get("RV1", redactor, string.Empty);

        var state = new LoggerMessageState();
        int index = state.ReserveClassifiedTagSpace(2);
        state.RedactedTagArray[index] = new("R1", jr);
        state.RedactedTagArray[index + 1] = new("R2", null);

        var joiner = new ExtendedLogger.ModernTagJoiner { StaticTags = [] };
        joiner.SetIncomingTags(state);

        KeyValuePair<string, object?>[] expected = [new("R1", "R<RV1>"), new("R2", null)];
        Assert.Equal(expected, new[] { joiner[0], joiner[1] });
        Assert.Equal(expected, joiner);
        Assert.Single(redactor.EventCollector.AllRedactedData);

        jr.Return();
    }

    private static void AssertTags(KeyValuePair<string, object?>[] expected, IReadOnlyList<KeyValuePair<string, object?>> joiner)
    {
        Assert.Equal(expected.Length, joiner.Count);
        Assert.Equal(expected.OrderBy(tag => tag.Key, StringComparer.Ordinal), joiner.OrderBy(tag => tag.Key, StringComparer.Ordinal));
    }
}

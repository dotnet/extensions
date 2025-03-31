// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

public class RelevanceTruthAndCompletenessEvaluatorRatingTests
{
    [Fact]
    public void JsonIsValid()
    {
        string json = """
                      {"relevance": 1, "truth": 5, "completeness": 4}
                      """;

        var rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(5, rating.Truth);
        Assert.Equal(4, rating.Completeness);
        Assert.Null(rating.RelevanceReasoning);
        Assert.Null(rating.TruthReasoning);
        Assert.Null(rating.CompletenessReasoning);
        Assert.Empty(rating.RelevanceReasons);
        Assert.Empty(rating.TruthReasons);
        Assert.Empty(rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntax()
    {
        string json = """

                      ```
                      {"relevance": 1, "truth": 5, "completeness": 4}
                      ```

                      """;

        var rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(5, rating.Truth);
        Assert.Equal(4, rating.Completeness);
        Assert.Null(rating.RelevanceReasoning);
        Assert.Null(rating.TruthReasoning);
        Assert.Null(rating.CompletenessReasoning);
        Assert.Empty(rating.RelevanceReasons);
        Assert.Empty(rating.TruthReasons);
        Assert.Empty(rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntaxWithJsonPrefix()
    {
        string json = """

                      ```json
                      {"relevance": 1, "truth": 5, "completeness": 4}
                      ```

                      """;

        var rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(5, rating.Truth);
        Assert.Equal(4, rating.Completeness);
        Assert.Null(rating.RelevanceReasoning);
        Assert.Null(rating.TruthReasoning);
        Assert.Null(rating.CompletenessReasoning);
        Assert.Empty(rating.RelevanceReasons);
        Assert.Empty(rating.TruthReasons);
        Assert.Empty(rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonCanBeRoundTripped()
    {
        var rating = new RelevanceTruthAndCompletenessEvaluator.Rating(
            relevance: 1,
            relevanceReasoning: "The response is not relevant to the request.",
            relevanceReasons: ["Reason 1", "Reason 2"],
            truth: 5,
            truthReasoning: "The response is mostly true.",
            truthReasons: ["Reason 1", "Reason 2"],
            completeness: 4,
            completenessReasoning: "The response is mostly complete.",
            completenessReasons: ["Reason 1", "Reason 2"]);

        string json = JsonSerializer.Serialize(rating, RelevanceTruthAndCompletenessEvaluator.SerializerContext.Default.Rating);
        var deserialized = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.Equal(rating.Relevance, deserialized.Relevance);
        Assert.Equal(rating.RelevanceReasoning, deserialized.RelevanceReasoning);
        Assert.True(rating.RelevanceReasons.SequenceEqual(deserialized.RelevanceReasons));
        Assert.Equal(rating.Truth, deserialized.Truth);
        Assert.Equal(rating.TruthReasoning, deserialized.TruthReasoning);
        Assert.True(rating.TruthReasons.SequenceEqual(deserialized.TruthReasons));
        Assert.Equal(rating.Completeness, deserialized.Completeness);
        Assert.Equal(rating.CompletenessReasoning, deserialized.CompletenessReasoning);
        Assert.True(rating.CompletenessReasons.SequenceEqual(deserialized.CompletenessReasons));
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonContainsInconclusiveMetrics()
    {
        string json = """{"relevance": -1, "truth": 4, "completeness": 7}""";
        var rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);

        json = """{"relevance": 0, "truth": -1, "completeness": 3}""";
        rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);

        json = """{"relevance": 0, "truth": 4, "completeness": -5}""";
        rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);

        json = """{"relevance": 10, "truth": 4, "completeness": 3}""";
        rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);

        json = """{"relevance": 0, "truth": 5, "completeness": 3}""";
        rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);

        json = """{"relevance": 1, "truth": 4, "completeness": 6}""";
        rating = RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json);
        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonContainsErrors()
    {
        string json = """{"relevance": 0, "truth": 2 ;"completeness": 3}""";
        Assert.Throws<JsonException>(() => RelevanceTruthAndCompletenessEvaluator.Rating.FromJson(json));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Quality.JsonSerialization;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

public class RelevanceTruthAndCompletenessRatingTests
{
    [Fact]
    public void JsonIsValid()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(1, rating.Truth);
        Assert.Equal(1, rating.Completeness);
        Assert.Equal("The reason for the relevance score", rating.RelevanceReasoning);
        Assert.Equal("The reason for the truth score", rating.TruthReasoning);
        Assert.Equal("The reason for the completeness score", rating.CompletenessReasoning);
        Assert.Single(rating.RelevanceReasons);
        Assert.Equal("relevance_reason_distant_topic", rating.RelevanceReasons[0]);
        Assert.Equal(3, rating.TruthReasons.Length);
        Assert.Contains("truth_reason_incorrect_information", rating.TruthReasons);
        Assert.Contains("truth_reason_outdated_information", rating.TruthReasons);
        Assert.Contains("truth_reason_misleading_incorrectforintent", rating.TruthReasons);
        Assert.Equal(2, rating.CompletenessReasons.Length);
        Assert.Contains("completeness_reason_no_solution", rating.CompletenessReasons);
        Assert.Contains("completeness_reason_genericsolution_missingcode", rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntax()
    {
        string json =
            """

            ```
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": [],
                "truth": 4,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": [],
                "completeness": 5,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": []
            }
            ```

            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(4, rating.Truth);
        Assert.Equal(5, rating.Completeness);
        Assert.Equal("The reason for the relevance score", rating.RelevanceReasoning);
        Assert.Equal("The reason for the truth score", rating.TruthReasoning);
        Assert.Equal("The reason for the completeness score", rating.CompletenessReasoning);
        Assert.Empty(rating.RelevanceReasons);
        Assert.Empty(rating.TruthReasons);
        Assert.Empty(rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntaxWithJsonPrefix()
    {
        string json =
            """

            ```json
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 3,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_misleading_incorrectforintent"],
                "completeness": 2,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution"],
            }
            ```

            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(3, rating.Truth);
        Assert.Equal(2, rating.Completeness);
        Assert.Equal("The reason for the relevance score", rating.RelevanceReasoning);
        Assert.Equal("The reason for the truth score", rating.TruthReasoning);
        Assert.Equal("The reason for the completeness score", rating.CompletenessReasoning);
        Assert.Single(rating.RelevanceReasons);
        Assert.Equal("relevance_reason_distant_topic", rating.RelevanceReasons[0]);
        Assert.Single(rating.TruthReasons);
        Assert.Contains("truth_reason_misleading_incorrectforintent", rating.TruthReasons);
        Assert.Single(rating.CompletenessReasons);
        Assert.Contains("completeness_reason_no_solution", rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonCanBeRoundTripped()
    {
        RelevanceTruthAndCompletenessRating rating =
            new RelevanceTruthAndCompletenessRating(
                relevance: 1,
                relevanceReasoning: "The response is not relevant to the request.",
                relevanceReasons: ["Reason 1", "Reason 2"],
                truth: 5,
                truthReasoning: "The response is mostly true.",
                truthReasons: ["Reason 1", "Reason 2"],
                completeness: 4,
                completenessReasoning: "The response is mostly complete.",
                completenessReasons: ["Reason 1", "Reason 2"]);

        string json = JsonSerializer.Serialize(rating, SerializerContext.Default.RelevanceTruthAndCompletenessRating);
        RelevanceTruthAndCompletenessRating deserialized = RelevanceTruthAndCompletenessRating.FromJson(json);

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
    public void InconclusiveJsonCanBeRoundTripped()
    {
        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.Inconclusive;

        string json = JsonSerializer.Serialize(rating, SerializerContext.Default.RelevanceTruthAndCompletenessRating);
        RelevanceTruthAndCompletenessRating deserialized = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(rating.Relevance, deserialized.Relevance);
        Assert.Equal(rating.RelevanceReasoning, deserialized.RelevanceReasoning);
        Assert.True(rating.RelevanceReasons.SequenceEqual(deserialized.RelevanceReasons));
        Assert.Equal(rating.Truth, deserialized.Truth);
        Assert.Equal(rating.TruthReasoning, deserialized.TruthReasoning);
        Assert.True(rating.TruthReasons.SequenceEqual(deserialized.TruthReasons));
        Assert.Equal(rating.Completeness, deserialized.Completeness);
        Assert.Equal(rating.CompletenessReasoning, deserialized.CompletenessReasoning);
        Assert.True(rating.CompletenessReasons.SequenceEqual(deserialized.CompletenessReasons));
        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithNegativeScoreIsInconclusive()
    {
        string json =
           """
            {
                "relevance": -1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithZeroScoreIsInconclusive()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 0,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithExcessivelyHighScoreIsInconclusive()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 100,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithAdditionalHallucinatedPropertyIsProcessedCorrectly()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "hallucinatedProperty": "Some hallucinated text",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(1, rating.Truth);
        Assert.Equal(1, rating.Completeness);
        Assert.Equal("The reason for the relevance score", rating.RelevanceReasoning);
        Assert.Equal("The reason for the truth score", rating.TruthReasoning);
        Assert.Equal("The reason for the completeness score", rating.CompletenessReasoning);
        Assert.Single(rating.RelevanceReasons);
        Assert.Equal("relevance_reason_distant_topic", rating.RelevanceReasons[0]);
        Assert.Equal(3, rating.TruthReasons.Length);
        Assert.Contains("truth_reason_incorrect_information", rating.TruthReasons);
        Assert.Contains("truth_reason_outdated_information", rating.TruthReasons);
        Assert.Contains("truth_reason_misleading_incorrectforintent", rating.TruthReasons);
        Assert.Equal(2, rating.CompletenessReasons.Length);
        Assert.Contains("completeness_reason_no_solution", rating.CompletenessReasons);
        Assert.Contains("completeness_reason_genericsolution_missingcode", rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithDuplicatePropertyUsesLastValue()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": "The reason for the relevance score",
                "relevanceReasoning": "Duplicate reason for the relevance score",
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        RelevanceTruthAndCompletenessRating rating = RelevanceTruthAndCompletenessRating.FromJson(json);

        Assert.Equal(1, rating.Relevance);
        Assert.Equal(1, rating.Truth);
        Assert.Equal(1, rating.Completeness);
        Assert.Equal("Duplicate reason for the relevance score", rating.RelevanceReasoning);
        Assert.Equal("The reason for the truth score", rating.TruthReasoning);
        Assert.Equal("The reason for the completeness score", rating.CompletenessReasoning);
        Assert.Single(rating.RelevanceReasons);
        Assert.Equal("relevance_reason_distant_topic", rating.RelevanceReasons[0]);
        Assert.Equal(3, rating.TruthReasons.Length);
        Assert.Contains("truth_reason_incorrect_information", rating.TruthReasons);
        Assert.Contains("truth_reason_outdated_information", rating.TruthReasons);
        Assert.Contains("truth_reason_misleading_incorrectforintent", rating.TruthReasons);
        Assert.Equal(2, rating.CompletenessReasons.Length);
        Assert.Contains("completeness_reason_no_solution", rating.CompletenessReasons);
        Assert.Contains("completeness_reason_genericsolution_missingcode", rating.CompletenessReasons);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithSemicolonsInsteadOfCommasThrowsException()
    {
        string json =
            """
            {
                "relevance": 1;
                "relevanceReasoning": "The reason for the relevance score";
                "relevanceReasons": ["relevance_reason_distant_topic"];
                "truth": 1;
                "truthReasoning": "The reason for the truth score";
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"];
                "completeness": 1;
                "completenessReasoning": "The reason for the completeness score";
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"];
            }
            """;

        Assert.Throws<JsonException>(() => RelevanceTruthAndCompletenessRating.FromJson(json));
    }

    [Fact]
    public void JsonWithMissingPropertiesThrowsException()
    {
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
            }
            """;

        Assert.Throws<JsonException>(() => RelevanceTruthAndCompletenessRating.FromJson(json));
    }

    [Fact]
    public void JsonWithIncorrectPropertyValueTypeThrowsException()
    {
        // Incorrect property value (integer instead of string for relevanceReasoning).
        string json =
            """
            {
                "relevance": 1,
                "relevanceReasoning": 6,
                "relevanceReasons": ["relevance_reason_distant_topic"],
                "truth": 1,
                "truthReasoning": "The reason for the truth score",
                "truthReasons": ["truth_reason_incorrect_information", "truth_reason_outdated_information", "truth_reason_misleading_incorrectforintent"],
                "completeness": 1,
                "completenessReasoning": "The reason for the completeness score",
                "completenessReasons": ["completeness_reason_no_solution", "completeness_reason_genericsolution_missingcode"],
            }
            """;

        Assert.Throws<JsonException>(() => RelevanceTruthAndCompletenessRating.FromJson(json));
    }
}

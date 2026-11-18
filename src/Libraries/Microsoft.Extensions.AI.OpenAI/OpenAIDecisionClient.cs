// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI.Decisions;

#pragma warning disable OPENAI001 // The OpenAI Decisions API is experimental
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S2302 // Messages refer to decision answers and questions, not parameter names

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IDecisionClient"/> for an OpenAI <see cref="DecisionsClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
internal sealed class OpenAIDecisionClient : IDecisionClient
{
    /// <summary>The key into <see cref="AIContent.AdditionalProperties"/> used to specify an image detail level.</summary>
    private const string ImageDetailKey = "detail";

    /// <summary>The key into <see cref="DecisionAnswer.AdditionalProperties"/> used to report native confidence.</summary>
    private const string ConfidenceKey = "confidence";

    /// <summary>Metadata about the client.</summary>
    private readonly DecisionClientMetadata _metadata;

    /// <summary>The underlying <see cref="DecisionsClient" />.</summary>
    private readonly DecisionsClient _decisionsClient;

    /// <summary>The model to use when none is specified in options.</summary>
    private readonly string? _defaultModelId;

    /// <summary>Initializes a new instance of the <see cref="OpenAIDecisionClient"/> class for the specified <see cref="DecisionsClient"/>.</summary>
    /// <param name="decisionsClient">The underlying client.</param>
    /// <param name="defaultModelId">The model to use when none is specified in options.</param>
    public OpenAIDecisionClient(DecisionsClient decisionsClient, string? defaultModelId)
    {
        _decisionsClient = Throw.IfNull(decisionsClient);
        _defaultModelId = defaultModelId;

        _metadata = new("openai", decisionsClient.Endpoint, defaultModelId, [DecisionKind.Binary, DecisionKind.Choice, DecisionKind.Score]);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(DecisionClientMetadata) ? _metadata :
            serviceType == typeof(DecisionsClient) ? _decisionsClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<DecisionResponse> GetResponseAsync(
        DecisionRequest request, DecisionOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(request);

        DecisionCreationOptions creationOptions = ToOpenAICreationOptions(request, options);

        ClientResult<DecisionResource> result;
        try
        {
            result = await _decisionsClient.CreateDecisionAsync(creationOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException ex)
        {
            throw new DecisionClientException(ex.Message, isTransient: IsTransient(ex.Status), ex);
        }

        DecisionResponse response = FromOpenAIDecision(request, result.Value);
        response.Validate(request);
        return response;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IDecisionClient interface.
    }

    /// <summary>Gets whether an HTTP status code represents a transient failure.</summary>
    private static bool IsTransient(int status) => status is 0 or 408 or 429 or >= 500;

    /// <summary>Converts a <see cref="DecisionRequest"/> and <see cref="DecisionOptions"/> to an OpenAI <see cref="DecisionCreationOptions"/>.</summary>
    private DecisionCreationOptions ToOpenAICreationOptions(DecisionRequest request, DecisionOptions? options)
    {
        DecisionCreationOptions result = options?.RawRepresentationFactory?.Invoke(this) as DecisionCreationOptions ?? new();

        result.Model ??= options?.ModelId ?? _defaultModelId ??
            throw new InvalidOperationException("No model was specified. Provide a default model when creating the client or set DecisionOptions.ModelId.");

        DecisionsUserMessageItem message = new();
        if (request.State is JsonElement state)
        {
            message.Content.Add(new DecisionsInputJsonContentPart([BinaryData.FromString(state.GetRawText())]));
        }

        foreach (AIContent content in request.Contents)
        {
            message.Content.Add(ToInputContentPart(content));
        }

        result.InputItems.Add(message);

        foreach (DecisionQuestion question in request.Questions)
        {
            result.Questions.Add(ToOpenAIQuestion(question));
        }

        return result;
    }

    /// <summary>Converts an <see cref="AIContent"/> request input to an OpenAI input content part.</summary>
    private static DecisionsInputContentPart ToInputContentPart(AIContent content) => content switch
    {
        { RawRepresentation: DecisionsInputContentPart rawPart } => rawPart,
        TextContent textContent => new DecisionsInputTextContentPart(textContent.Text),
        DataContent dataContent when dataContent.HasTopLevelMediaType("image") =>
            new DecisionsInputImageContentPart(dataContent.Uri) { ImageDetailLevel = GetImageDetail(content) },
        UriContent uriContent when uriContent.HasTopLevelMediaType("image") =>
            new DecisionsInputImageContentPart(uriContent.Uri.ToString()) { ImageDetailLevel = GetImageDetail(content) },
        _ => throw new NotSupportedException($"Content of type '{content.GetType().Name}' is not supported as a decision input by this client."),
    };

    /// <summary>Converts an <see cref="AIContent"/> instruction to an OpenAI instructions part.</summary>
    private static DecisionsInstructionsPart ToInstructionsPart(AIContent content) => content switch
    {
        { RawRepresentation: DecisionsInstructionsPart rawPart } => rawPart,
        TextContent textContent => new DecisionsInputTextInstructionsPart(textContent.Text),
        DataContent dataContent when dataContent.HasTopLevelMediaType("image") =>
            new DecisionsInputImageInstructionsPart(dataContent.Uri) { ImageDetailLevel = GetImageDetail(content) },
        UriContent uriContent when uriContent.HasTopLevelMediaType("image") =>
            new DecisionsInputImageInstructionsPart(uriContent.Uri.ToString()) { ImageDetailLevel = GetImageDetail(content) },
        _ => throw new NotSupportedException($"Content of type '{content.GetType().Name}' is not supported in decision instructions by this client."),
    };

    /// <summary>Gets the image detail level specified in an <see cref="AIContent"/>'s additional properties, if any.</summary>
    private static DecisionsImageDetailLevel? GetImageDetail(AIContent content)
    {
        if (content.AdditionalProperties is null || !content.AdditionalProperties.TryGetValue(ImageDetailKey, out object? value))
        {
            return null;
        }

        return value switch
        {
            string detail => new DecisionsImageDetailLevel(detail),
            DecisionsImageDetailLevel detail => detail,
            _ => (DecisionsImageDetailLevel?)null,
        };
    }

    /// <summary>Converts a <see cref="DecisionQuestion"/> to an OpenAI <see cref="DecisionsQuestion"/>.</summary>
    private static DecisionsQuestion ToOpenAIQuestion(DecisionQuestion question)
    {
        switch (question)
        {
            case BinaryDecisionQuestion binary:
                DecisionsPredicateQuestion predicate = new() { Name = binary.Id };
                AddInstructions(predicate.Instructions, binary.Instructions);
                AddOutcomeDescription(predicate.Instructions, "If true: ", binary.TrueDescription);
                AddOutcomeDescription(predicate.Instructions, "If false: ", binary.FalseDescription);
                return predicate;

            case ChoiceDecisionQuestion choice:
                DecisionsChoiceQuestion choiceQuestion = new() { Name = choice.Id };
                AddInstructions(choiceQuestion.Instructions, choice.Instructions);
                foreach (DecisionCandidate candidate in choice.Candidates)
                {
                    choiceQuestion.Choices.Add(new DecisionsChoice(new DecisionsChoiceValue(candidate.Id))
                    {
                        Description = ToDescriptionText(candidate.Description),
                    });
                }

                return choiceQuestion;

            case ScoreDecisionQuestion score:
                DecisionsScoreQuestion scoreQuestion = new() { Name = score.Id };
                AddInstructions(scoreQuestion.Instructions, score.Instructions);
                foreach (DecisionScoreLevel level in score.Levels)
                {
                    scoreQuestion.Levels.Add(new DecisionsScoreLevel(level.Id)
                    {
                        Description = ToDescriptionText(level.Description),
                    });
                }

                return scoreQuestion;

            default:
                throw new NotSupportedException($"Decision questions of type '{question.GetType().Name}' are not supported by this client.");
        }
    }

    private static void AddInstructions(IList<DecisionsInstructionsPart> target, IReadOnlyList<AIContent> instructions)
    {
        foreach (AIContent content in instructions)
        {
            target.Add(ToInstructionsPart(content));
        }
    }

    /// <summary>Appends a binary outcome description to the question instructions, since OpenAI predicates have no outcome descriptions.</summary>
    private static void AddOutcomeDescription(IList<DecisionsInstructionsPart> target, string label, IReadOnlyList<AIContent>? description)
    {
        if (description is not { Count: > 0 })
        {
            return;
        }

        target.Add(new DecisionsInputTextInstructionsPart(label));
        AddInstructions(target, description);
    }

    /// <summary>Converts a candidate or level description to text, since OpenAI supports only text descriptions.</summary>
    private static string? ToDescriptionText(IReadOnlyList<AIContent> description)
    {
        if (description.Count == 0)
        {
            return null;
        }

        if (description.Count == 1 && description[0] is TextContent single)
        {
            return single.Text;
        }

        StringBuilder builder = new();
        foreach (AIContent content in description)
        {
            if (content is not TextContent text)
            {
                throw new NotSupportedException($"Content of type '{content.GetType().Name}' is not supported in candidate or level descriptions by this client.");
            }

            _ = builder.Append(text.Text);
        }

        return builder.ToString();
    }

    /// <summary>Converts an OpenAI <see cref="DecisionResource"/> to a <see cref="DecisionResponse"/>.</summary>
    private static DecisionResponse FromOpenAIDecision(DecisionRequest request, DecisionResource resource)
    {
        Dictionary<string, DecisionsAnswer> nativeAnswers = new(StringComparer.Ordinal);
        foreach (DecisionsAnswer native in resource.Answers)
        {
            string? name = GetAnswerName(native);
            if (name is null || !TryAdd(nativeAnswers, name, native))
            {
                throw new DecisionProtocolException("The OpenAI decision response contains an answer without a unique question name.");
            }
        }

        List<DecisionAnswer> answers = new(request.Questions.Count);
        foreach (DecisionQuestion question in request.Questions)
        {
            if (!nativeAnswers.TryGetValue(question.Id, out DecisionsAnswer? native))
            {
                throw new DecisionProtocolException($"The OpenAI decision response does not answer question '{question.Id}'.");
            }

            DecisionAnswer answer;
            try
            {
                answer = (question, native) switch
                {
                    (_, DecisionsRefusalAnswer) => new UnansweredDecisionAnswer(question.Id, question.Kind) { Reason = DecisionUnansweredReason.Refusal },
                    (BinaryDecisionQuestion, DecisionsPredicateAnswer predicate) => new BinaryDecisionAnswer(question.Id, ToDouble(predicate.Probability)),
                    (ChoiceDecisionQuestion choice, DecisionsChoiceAnswer choiceAnswer) => FromChoiceAnswer(choice, choiceAnswer),
                    (ScoreDecisionQuestion score, DecisionsScoreAnswer scoreAnswer) => FromScoreAnswer(score, scoreAnswer),
                    _ => throw new DecisionProtocolException($"The OpenAI decision answer for question '{question.Id}' does not match the question kind."),
                };
            }
            catch (ArgumentException ex)
            {
                throw new DecisionProtocolException($"The OpenAI decision answer for question '{question.Id}' is invalid: {ex.Message}", ex);
            }

            answer.RawRepresentation = native;
            answers.Add(answer);
        }

        if (nativeAnswers.Count != answers.Count)
        {
            throw new DecisionProtocolException("The OpenAI decision response contains answers for questions that were not asked.");
        }

        return new DecisionResponse(answers)
        {
            ModelId = resource.Model,
            Usage = resource.Usage is { } usage ? ToUsageDetails(usage) : null,
            RawRepresentation = resource,
        };
    }

    private static string? GetAnswerName(DecisionsAnswer answer) => answer switch
    {
        DecisionsPredicateAnswer predicate => predicate.Name,
        DecisionsChoiceAnswer choice => choice.Name,
        DecisionsScoreAnswer score => score.Name,
        DecisionsRefusalAnswer refusal => refusal.Name,
        _ => throw new DecisionProtocolException($"The OpenAI decision response contains an unsupported answer kind '{answer.Kind}'."),
    };

    private static ChoiceDecisionAnswer FromChoiceAnswer(ChoiceDecisionQuestion question, DecisionsChoiceAnswer answer)
    {
        Dictionary<string, double> reported = new(StringComparer.Ordinal);
        foreach (DecisionsChoiceProbability probability in answer.Probabilities)
        {
            if (!TryAdd(reported, ToCandidateId(probability.Value), ToDouble(probability.Probability)))
            {
                throw new DecisionProtocolException($"The OpenAI decision answer for '{question.Id}' reports a candidate more than once.");
            }
        }

        DecisionProbability[] probabilities = ToDistribution(question.Id, question.Candidates.Count, i => question.Candidates[i].Id, reported);
        return new ChoiceDecisionAnswer(question.Id, ToCandidateId(answer.Choice), probabilities)
        {
            AdditionalProperties = new() { [ConfidenceKey] = ToDouble(answer.Confidence) },
        };
    }

    private static ScoreDecisionAnswer FromScoreAnswer(ScoreDecisionQuestion question, DecisionsScoreAnswer answer)
    {
        // OpenAI reports each level's zero-based position as its value, and the score as the probability-weighted position.
        Dictionary<string, double> reported = new(StringComparer.Ordinal);
        foreach (DecisionsScoreProbability probability in answer.Probabilities)
        {
            if (probability.Label is null || probability.Value != IndexOfLevel(question, probability.Label) || !TryAdd(reported, probability.Label, ToDouble(probability.Probability)))
            {
                throw new DecisionProtocolException($"The OpenAI decision answer for '{question.Id}' reports levels that do not match the requested levels.");
            }
        }

        if (double.IsNaN(answer.Score) || double.IsInfinity(answer.Score))
        {
            throw new DecisionProtocolException("The OpenAI decision response contains a non-finite value.");
        }

        // Clamp to the level scale to absorb rounding in the service-computed expectation.
        double score = Math.Min(Math.Max(answer.Score, 0d), question.Levels.Count - 1);

        DecisionProbability[] probabilities = ToDistribution(question.Id, question.Levels.Count, i => question.Levels[i].Id, reported);
        return new ScoreDecisionAnswer(question.Id, score, probabilities)
        {
            AdditionalProperties = new() { [ConfidenceKey] = ToDouble(answer.Confidence) },
        };
    }

    private static int IndexOfLevel(ScoreDecisionQuestion question, string? label)
    {
        for (int i = 0; i < question.Levels.Count; i++)
        {
            if (string.Equals(question.Levels[i].Id, label, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Builds a distribution in requested order, treating options omitted by the provider as having zero probability.</summary>
    private static DecisionProbability[] ToDistribution(string questionId, int count, Func<int, string> getId, Dictionary<string, double> reported)
    {
        DecisionProbability[] probabilities = new DecisionProbability[count];
        int matched = 0;
        for (int i = 0; i < count; i++)
        {
            string id = getId(i);
            if (reported.TryGetValue(id, out double value))
            {
                matched++;
            }

            probabilities[i] = new DecisionProbability(id, value);
        }

        if (matched != reported.Count)
        {
            throw new DecisionProtocolException($"The OpenAI decision answer for question '{questionId}' reports options that were not requested.");
        }

        return probabilities;
    }

    private static bool TryAdd<TValue>(Dictionary<string, TValue> dictionary, string key, TValue value)
    {
        if (dictionary.ContainsKey(key))
        {
            return false;
        }

        dictionary[key] = value;
        return true;
    }

    private static string ToCandidateId(DecisionsChoiceValue? value) =>
        value?.StringValue ?? throw new DecisionProtocolException("The OpenAI decision response contains a choice that is not a candidate ID.");

    /// <summary>Converts a single-precision value reported by the service without introducing binary widening artifacts.</summary>
    // Use the shortest round-trippable float text so 0.1f maps to 0.1 rather than 0.10000000149011612.
    private static double ToDouble(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ?
            throw new DecisionProtocolException("The OpenAI decision response contains a non-finite value.") :
            double.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static UsageDetails ToUsageDetails(DecisionsUsage usage)
    {
        UsageDetails details = new()
        {
            InputTokenCount = usage.InputTokenCount,
            OutputTokenCount = usage.OutputTokenCount,
            TotalTokenCount = usage.TotalTokenCount,
        };

        if (usage.InputTokenDetails is { } inputDetails)
        {
            details.CachedInputTokenCount = inputDetails.CachedTokenCount;
        }

        if (usage.OutputTokenDetails is { } outputDetails)
        {
            details.ReasoningTokenCount = outputDetails.ReasoningTokenCount;
        }

        return details;
    }
}

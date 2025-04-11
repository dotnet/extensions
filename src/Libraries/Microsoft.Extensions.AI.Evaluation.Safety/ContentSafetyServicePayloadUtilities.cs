// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Xml.Linq;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class ContentSafetyServicePayloadUtilities
{
    internal static (string payload, IReadOnlyList<EvaluationDiagnostic>? diagnostics) GetPayload(
        ContentSafetyServicePayloadFormat payloadFormat,
        IEnumerable<ChatMessage> conversation,
        string annotationTask,
        string evaluatorName,
        IEnumerable<string?>? perTurnContext = null,
        IEnumerable<string>? metricNames = null,
        CancellationToken cancellationToken = default) =>
            payloadFormat switch
            {
                ContentSafetyServicePayloadFormat.HumanSystem =>
                    GetUserTextListPayloadWithEmbeddedXml(
                        conversation,
                        annotationTask,
                        evaluatorName,
                        perTurnContext,
                        metricNames,
                        cancellationToken: cancellationToken),

                ContentSafetyServicePayloadFormat.QuestionAnswer =>
                    GetUserTextListPayloadWithEmbeddedJson(
                        conversation,
                        annotationTask,
                        evaluatorName,
                        perTurnContext,
                        metricNames,
                        cancellationToken: cancellationToken),

                ContentSafetyServicePayloadFormat.QueryResponse =>
                    GetUserTextListPayloadWithEmbeddedJson(
                        conversation,
                        annotationTask,
                        evaluatorName,
                        perTurnContext,
                        metricNames,
                        questionPropertyName: "query",
                        answerPropertyName: "response",
                        cancellationToken: cancellationToken),

                ContentSafetyServicePayloadFormat.ContextCompletion =>
                    GetUserTextListPayloadWithEmbeddedJson(
                        conversation,
                        annotationTask,
                        evaluatorName,
                        perTurnContext,
                        metricNames,
                        questionPropertyName: "context",
                        answerPropertyName: "completion",
                        cancellationToken: cancellationToken),

                ContentSafetyServicePayloadFormat.Conversation =>
                    GetConversationPayload(
                        conversation,
                        annotationTask,
                        evaluatorName,
                        perTurnContext,
                        metricNames,
                        cancellationToken: cancellationToken),

                _ => throw new NotSupportedException($"The payload kind '{payloadFormat}' is not supported."),
            };

#pragma warning disable S107 // Methods should not have too many parameters
    private static (string payload, IReadOnlyList<EvaluationDiagnostic>? diagnostics)
        GetUserTextListPayloadWithEmbeddedXml(
            IEnumerable<ChatMessage> conversation,
            string annotationTask,
            string evaluatorName,
            IEnumerable<string?>? perTurnContext = null,
            IEnumerable<string>? metricNames = null,
            string questionElementName = "Human",
            string answerElementName = "System",
            string contextElementName = "Context",
            ContentSafetyServicePayloadStrategy strategy = ContentSafetyServicePayloadStrategy.AnnotateConversation,
            CancellationToken cancellationToken = default)
#pragma warning restore S107
    {
        List<Dictionary<string, ChatMessage>> turns;
        List<string?>? normalizedPerTurnContext;
        List<EvaluationDiagnostic>? diagnostics;

        (turns, normalizedPerTurnContext, diagnostics, _) =
            PreProcessConversation(
                conversation,
                evaluatorName,
                perTurnContext,
                returnLastTurnOnly: strategy is ContentSafetyServicePayloadStrategy.AnnotateLastTurn,
                cancellationToken: cancellationToken);

        IEnumerable<List<XElement>> userTextListItems =
            turns.Select(
                (turn, index) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    List<XElement> item = [];

                    if (turn.TryGetValue("question", out ChatMessage? question))
                    {
                        item.Add(new XElement(questionElementName, question.Text));
                    }

                    if (turn.TryGetValue("answer", out ChatMessage? answer))
                    {
                        item.Add(new XElement(answerElementName, answer.Text));
                    }

                    if (normalizedPerTurnContext is not null && normalizedPerTurnContext.Any())
                    {
                        item.Add(new XElement(contextElementName, normalizedPerTurnContext[index]));
                    }

                    return item;
                });

        IEnumerable<string> userTextListStrings =
            userTextListItems.Select(item => string.Join(string.Empty, item.Select(e => e.ToString())));

        if (strategy is ContentSafetyServicePayloadStrategy.AnnotateConversation)
        {
            // Combine all turns into a single string. In this case, the service will produce a single annotation
            // result for the entire conversation.
            userTextListStrings = [string.Join(Environment.NewLine, userTextListStrings)];
        }
        else
        {
            // If ContentSafetyServicePayloadStrategy.AnnotateLastTurn is used, we have already discarded all turns
            // except the last one above. In this case, the service will produce a single annotation result for
            // the last conversation turn only.
            //
            // On the other hand, if ContentSafetyServicePayloadStrategy.AnnotateEachTurn is used, all turns should be
            // retained individually in userTextListStrings above. In this case, the service will produce a separate
            // annotation result for each conversation turn.
        }

        var payload =
                new JsonObject
                {
                    ["UserTextList"] = new JsonArray([.. userTextListStrings]),
                    ["AnnotationTask"] = annotationTask,
                };

        if (metricNames is not null && metricNames.Any())
        {
            payload["MetricList"] = new JsonArray([.. metricNames]);
        }

        return (payload.ToJsonString(), diagnostics);
    }

#pragma warning disable S107 // Methods should not have too many parameters
    private static (string payload, IReadOnlyList<EvaluationDiagnostic>? diagnostics)
        GetUserTextListPayloadWithEmbeddedJson(
            IEnumerable<ChatMessage> conversation,
            string annotationTask,
            string evaluatorName,
            IEnumerable<string?>? perTurnContext = null,
            IEnumerable<string>? metricNames = null,
            string questionPropertyName = "question",
            string answerPropertyName = "answer",
            string contextPropertyName = "context",
            ContentSafetyServicePayloadStrategy strategy = ContentSafetyServicePayloadStrategy.AnnotateLastTurn,
            CancellationToken cancellationToken = default)
#pragma warning restore S107
    {
        if (strategy is ContentSafetyServicePayloadStrategy.AnnotateConversation)
        {
            throw new NotSupportedException(
                $"{nameof(GetUserTextListPayloadWithEmbeddedJson)} does not support the {strategy} {nameof(ContentSafetyServicePayloadStrategy)}.");
        }

        List<Dictionary<string, ChatMessage>> turns;
        List<string?>? normalizedPerTurnContext;
        List<EvaluationDiagnostic>? diagnostics;

        (turns, normalizedPerTurnContext, diagnostics, _) =
            PreProcessConversation(
                conversation,
                evaluatorName,
                perTurnContext,
                returnLastTurnOnly: strategy is ContentSafetyServicePayloadStrategy.AnnotateLastTurn,
                cancellationToken: cancellationToken);

        IEnumerable<JsonObject> userTextListItems =
            turns.Select(
                (turn, index) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = new JsonObject();

                    if (turn.TryGetValue("question", out ChatMessage? question))
                    {
                        item[questionPropertyName] = question.Text;
                    }

                    if (turn.TryGetValue("answer", out ChatMessage? answer))
                    {
                        item[answerPropertyName] = answer.Text;
                    }

                    if (normalizedPerTurnContext is not null && normalizedPerTurnContext.Any())
                    {
                        item[contextPropertyName] = normalizedPerTurnContext[index];
                    }

                    return item;
                });

        IEnumerable<string> userTextListStrings = userTextListItems.Select(item => item.ToJsonString());

        // If ContentSafetyServicePayloadStrategy.AnnotateLastTurn is used, we have already discarded all turns except
        // the last one above. In this case, the service will produce a single annotation result for the last
        // conversation turn only.
        //
        // On the other hand, if ContentSafetyServicePayloadStrategy.AnnotateEachTurn is used, all turns should be
        // retained individually in userTextListStrings above. In this case, the service will produce a separate
        // annotation result for each conversation turn.

        var payload =
            new JsonObject
            {
                ["UserTextList"] = new JsonArray([.. userTextListStrings]),
                ["AnnotationTask"] = annotationTask,
            };

        if (metricNames is not null && metricNames.Any())
        {
            payload["MetricList"] = new JsonArray([.. metricNames]);
        }

        return (payload.ToJsonString(), diagnostics);
    }

    private static (string payload, IReadOnlyList<EvaluationDiagnostic>? diagnostics) GetConversationPayload(
        IEnumerable<ChatMessage> conversation,
        string annotationTask,
        string evaluatorName,
        IEnumerable<string?>? perTurnContext = null,
        IEnumerable<string>? metricNames = null,
        ContentSafetyServicePayloadStrategy strategy = ContentSafetyServicePayloadStrategy.AnnotateConversation,
        CancellationToken cancellationToken = default)
    {
        if (strategy is ContentSafetyServicePayloadStrategy.AnnotateEachTurn)
        {
            throw new NotSupportedException(
                $"{nameof(GetConversationPayload)} does not support the {strategy} {nameof(ContentSafetyServicePayloadStrategy)}.");
        }

        List<Dictionary<string, ChatMessage>> turns;
        List<string?>? normalizedPerTurnContext;
        List<EvaluationDiagnostic>? diagnostics;
        string contentType;

        (turns, normalizedPerTurnContext, diagnostics, contentType) =
            PreProcessConversation(
                conversation,
                evaluatorName,
                perTurnContext,
                returnLastTurnOnly: strategy is ContentSafetyServicePayloadStrategy.AnnotateLastTurn,
                areImagesSupported: true,
                cancellationToken);

        IEnumerable<JsonObject> GetMessages(Dictionary<string, ChatMessage> turn, int turnIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (turn.TryGetValue("question", out ChatMessage? question))
            {
                IEnumerable<JsonObject> contents = GetContents(question);

                yield return new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray([.. contents])
                };
            }

            if (turn.TryGetValue("answer", out ChatMessage? answer))
            {
                IEnumerable<JsonObject> contents = GetContents(answer);

                if (normalizedPerTurnContext is not null &&
                    normalizedPerTurnContext.Any() &&
                    normalizedPerTurnContext[turnIndex] is string context)
                {
                    yield return new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = new JsonArray([.. contents]),
                        ["context"] = context
                    };
                }
                else
                {
                    yield return new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = new JsonArray([.. contents]),
                    };
                }
            }

            IEnumerable<JsonObject> GetContents(ChatMessage message)
            {
                foreach (AIContent content in message.Contents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (content is TextContent textContent)
                    {
                        yield return new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = textContent.Text
                        };
                    }
                    else if (content is UriContent uriContent && uriContent.HasTopLevelMediaType("image"))
                    {
                        yield return new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] =
                                new JsonObject
                                {
                                    ["url"] = uriContent.Uri.AbsoluteUri
                                }
                        };
                    }
                    else if (content is DataContent dataContent && dataContent.HasTopLevelMediaType("image"))
                    {
                        string url;
                        if (dataContent.IsUriBase64Encoded())
                        {
                            url = dataContent.Uri;
                        }
                        else
                        {
                            BinaryData imageBytes = BinaryData.FromBytes(dataContent.Data);
                            string base64ImageData = Convert.ToBase64String(imageBytes.ToArray());
                            url = $"data:{dataContent.MediaType};base64,{base64ImageData}";
                        }

                        yield return new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] =
                                new JsonObject
                                {
                                    ["url"] = url
                                }
                        };
                    }
                }
            }
        }

        var payload =
            new JsonObject
            {
                ["ContentType"] = contentType,
                ["Contents"] =
                    new JsonArray(
                        new JsonObject
                        {
                            ["messages"] = new JsonArray([.. turns.SelectMany(GetMessages)]),
                        }),
                ["AnnotationTask"] = annotationTask,
            };

        if (metricNames is not null && metricNames.Any())
        {
            payload["MetricList"] = new JsonArray([.. metricNames]);
        }

        // If ContentSafetyServicePayloadStrategy.AnnotateLastTurn is used, we have already discarded all turns except
        // the last one above. In this case, the service will produce a single annotation result for the last
        // conversation turn only.
        //
        // On the other hand, if ContentSafetyServicePayloadStrategy.AnnotateConversation is used, the service will
        // produce a single annotation result for the entire conversation.
        return (payload.ToJsonString(), diagnostics);
    }

    private static
        (List<Dictionary<string, ChatMessage>> turns,
        List<string?>? normalizedPerTurnContext,
        List<EvaluationDiagnostic>? diagnostics,
        string contentType) PreProcessConversation(
            IEnumerable<ChatMessage> conversation,
            string evaluatorName,
            IEnumerable<string?>? perTurnContext = null,
            bool returnLastTurnOnly = false,
            bool areImagesSupported = false,
            CancellationToken cancellationToken = default)
    {
        List<Dictionary<string, ChatMessage>> turns = [];
        Dictionary<string, ChatMessage> currentTurn = [];
        List<string?>? normalizedPerTurnContext =
            perTurnContext is null || !perTurnContext.Any() ? null : [.. perTurnContext];

        int currentTurnIndex = 0;
        int ignoredMessageCount = 0;
        int incompleteTurnCount = 0;

        void StartNewTurn()
        {
            if (!currentTurn.ContainsKey("question") || !currentTurn.ContainsKey("answer"))
            {
                ++incompleteTurnCount;
            }

            turns.Add(currentTurn);
            currentTurn = [];
            ++currentTurnIndex;
        }

        foreach (ChatMessage message in conversation)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (message.Role == ChatRole.User)
            {
                if (currentTurn.ContainsKey("question"))
                {
                    StartNewTurn();
                }

                currentTurn["question"] = message;
            }
            else if (message.Role == ChatRole.Assistant)
            {
                currentTurn["answer"] = message;

                StartNewTurn();
            }
            else
            {
                // System prompts are currently not supported.
                ignoredMessageCount++;
            }
        }

        if (returnLastTurnOnly)
        {
            turns.RemoveRange(index: 0, count: turns.Count - 1);
        }

        int imagesCount = 0;
        int unsupportedContentCount = 0;

        void ValidateContents(ChatMessage message)
        {
            foreach (AIContent content in message.Contents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (areImagesSupported)
                {
                    if (content.IsImageWithSupportedFormat())
                    {
                        ++imagesCount;
                    }
                    else if (!content.IsTextOrUsage())
                    {
                        ++unsupportedContentCount;
                    }
                }
                else if (!content.IsTextOrUsage())
                {
                    ++unsupportedContentCount;
                }
            }
        }

        foreach (var turn in turns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var message in turn.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ValidateContents(message);
            }
        }

        List<EvaluationDiagnostic>? diagnostics = null;

        if (ignoredMessageCount > 0)
        {
            diagnostics = [
                EvaluationDiagnostic.Warning(
                    $"The supplied conversation contained {ignoredMessageCount} messages with unsupported roles. " +
                    $"{evaluatorName} only considers messages with role '{ChatRole.User}' and '{ChatRole.Assistant}'. " +
                    $"The unsupported messages (which may include messages with role '{ChatRole.System}' and '{ChatRole.Tool}') were ignored.")];
        }

        if (incompleteTurnCount > 0)
        {
            diagnostics ??= [];
            diagnostics.Add(
                EvaluationDiagnostic.Warning(
                    $"The supplied conversation contained {incompleteTurnCount} incomplete turns. " +
                    $"These turns were either missing a message with role '{ChatRole.User}' or '{ChatRole.Assistant}'. " +
                    $"This may indicate that the supplied conversation was not well-formed and may result in inaccurate evaluation results."));
        }

        if (unsupportedContentCount > 0)
        {
            diagnostics ??= [];
            if (areImagesSupported)
            {
                diagnostics.Add(
                    EvaluationDiagnostic.Warning(
                        $"The supplied conversation contained {unsupportedContentCount} instances of unsupported content within messages. " +
                        $"The current evaluation being performed by {evaluatorName} only supports content of type '{nameof(TextContent)}', '{nameof(UriContent)}' and '{nameof(DataContent)}'. " +
                        $"For '{nameof(UriContent)}' and '{nameof(DataContent)}', only content with media type 'image/png', 'image/jpeg' and 'image/gif' are supported. " +
                        $"The unsupported contents were ignored for this evaluation."));
            }
            else
            {
                diagnostics.Add(
                    EvaluationDiagnostic.Warning(
                        $"The supplied conversation contained {unsupportedContentCount} instances of unsupported content within messages. " +
                        $"The current evaluation being performed by {evaluatorName} only supports content of type '{nameof(TextContent)}'. " +
                        $"The unsupported contents were ignored for this evaluation."));
            }
        }

        if (normalizedPerTurnContext is not null && normalizedPerTurnContext.Any())
        {
            if (normalizedPerTurnContext.Count > turns.Count)
            {
                var ignoredContextCount = normalizedPerTurnContext.Count - turns.Count;

                diagnostics ??= [];
                diagnostics.Add(
                    EvaluationDiagnostic.Warning(
                        $"The supplied conversation contained {turns.Count} turns. " +
                        $"However, context for {normalizedPerTurnContext.Count} turns were supplied as part of the context collection. " +
                        $"The initial {ignoredContextCount} items from the context collection were ignored. " +
                        $"Only the last {turns.Count} items from the context collection were used."));

                normalizedPerTurnContext.RemoveRange(0, ignoredContextCount);
            }
            else if (normalizedPerTurnContext.Count < turns.Count)
            {
                int missingContextCount = turns.Count - normalizedPerTurnContext.Count;

                diagnostics ??= [];
                diagnostics.Add(
                    EvaluationDiagnostic.Warning(
                        $"The supplied conversation contained {turns.Count} turns. " +
                        $"However, context for only {normalizedPerTurnContext.Count} turns were supplied as part of the context collection. " +
                        $"The initial {missingContextCount} turns in the conversations were evaluated without any context. " +
                        $"The supplied items in the context collection were applied to the last {normalizedPerTurnContext.Count} turns."));

                normalizedPerTurnContext.InsertRange(0, Enumerable.Repeat<string?>(null, missingContextCount));
            }
        }

        string contentType = areImagesSupported && imagesCount > 0 ? "image" : "text";

        return (turns, normalizedPerTurnContext, diagnostics, contentType);
    }
}

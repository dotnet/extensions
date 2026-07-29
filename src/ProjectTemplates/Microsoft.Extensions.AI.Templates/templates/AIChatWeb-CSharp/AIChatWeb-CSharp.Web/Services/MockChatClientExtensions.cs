using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.AI;

namespace AIChatWeb_CSharp.Web.Services;

#pragma warning disable MEAI001 // Mock provider uses experimental testing APIs

internal static class MockChatClientExtensions
{
    private const string LoadDocumentsCallId = "mock-load-documents";
    private const string SearchCallId = "mock-search";
    private const string MockConversationId = "mock-conversation";

    /// <summary>Adds a canned response for matching requests.</summary>
    /// <param name="client">The mock chat client to configure.</param>
    /// <param name="requestPredicate">Predicate used to select whether the response applies to a request.</param>
    /// <param name="response">The assistant response text.</param>
    /// <param name="minDelay">
    /// The optional minimum simulated response delay in milliseconds. When supplied alone, it is the fixed delay.
    /// </param>
    /// <param name="maxDelay">
    /// The optional maximum simulated response delay in milliseconds. When supplied alone, a delay from zero up to this
    /// value is selected.
    /// </param>
    /// <param name="suggestions">
    /// Optional aichatweb follow-up suggestions returned through its structured-output request. Supply an empty
    /// collection when the response is a conversation dead end.
    /// </param>
    /// <param name="includeCitation">
    /// <see langword="true"/> to invoke the document-search tools and append a citation from the search result.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove the response seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The configured <paramref name="client"/>.</returns>
    internal static MockChatClient AddResponse(
        this MockChatClient client,
        Func<MockChatClientRequest, bool> requestPredicate,
        string response,
        int? minDelay = null,
        int? maxDelay = null,
        IEnumerable<string>? suggestions = null,
        bool includeCitation = false,
        bool singleUse = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestPredicate);
        ArgumentNullException.ThrowIfNull(response);
        ValidateDelayRange(minDelay, maxDelay);

        if (suggestions is null)
        {
            return AddResponse(client, requestPredicate, response, minDelay, maxDelay, includeCitation, singleUse);
        }

        string structuredResponse = JsonSerializer.Serialize(new { data = suggestions.ToArray() });

        MockChatClient configuredClient = AddResponse(
            client,
            request => request.Options?.ResponseFormat is not ChatResponseFormatJson && requestPredicate(request),
            response,
            minDelay,
            maxDelay,
            includeCitation,
            singleUse);

        return AddResponse(
            configuredClient,
            request => request.Options?.ResponseFormat is ChatResponseFormatJson && requestPredicate(request),
            structuredResponse,
            minDelay,
            maxDelay,
            includeCitation,
            singleUse);
    }

    internal static string Citation(string filename, string quote)
    {
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(quote);

        return new XElement(
            "citation",
            new XAttribute("filename", filename),
            quote).ToString(SaveOptions.DisableFormatting);
    }

    private static MockChatClient AddResponse(
        MockChatClient client,
        Func<MockChatClientRequest, bool> requestPredicate,
        string response,
        int? minDelay,
        int? maxDelay,
        bool includeCitation,
        bool singleUse) =>
        client.AddResponse(
            requestPredicate,
            async (request, cancellationToken) =>
            {
                await Task.Delay(GetDelay(minDelay, maxDelay), cancellationToken);
                return CreateResponse(request, response, includeCitation);
            },
            singleUse);

    private static ChatResponse CreateResponse(MockChatClientRequest request, string response, bool includeCitation)
    {
        if (request.Options?.ResponseFormat is ChatResponseFormatJson)
        {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
        }

        if (TryGetFunctionResult(request, SearchCallId, out FunctionResultContent? searchResult) && searchResult is not null)
        {
            return CreateConversationResponse($"{response}{GetCitation(searchResult, request.LastUserText)}");
        }

        if (TryGetFunctionResult(request, LoadDocumentsCallId, out _))
        {
            return CreateSearchFunctionCallResponse(request);
        }

        if (includeCitation)
        {
            return request.Options?.ConversationId is null
                ? CreateFunctionCallResponse(LoadDocumentsCallId, "LoadDocuments")
                : CreateSearchFunctionCallResponse(request);
        }

        return CreateConversationResponse(response);
    }

    private static ChatResponse CreateConversationResponse(string response) =>
        new(new ChatMessage(ChatRole.Assistant, response))
        {
            ConversationId = MockConversationId,
        };

    private static ChatResponse CreateFunctionCallResponse(
        string callId,
        string name,
        IDictionary<string, object?>? arguments = null) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]));

    private static ChatResponse CreateSearchFunctionCallResponse(MockChatClientRequest request) =>
        CreateFunctionCallResponse(
            SearchCallId,
            "Search",
            new Dictionary<string, object?>
            {
                ["searchPhrase"] = request.LastUserText ?? string.Empty,
            });

    private static string GetCitation(FunctionResultContent searchResult, string? searchPhrase)
    {
        IEnumerable<string> results = searchResult.Result switch
        {
            IEnumerable<string> result => result,
            JsonElement { ValueKind: JsonValueKind.String } result when result.GetString() is { } resultText => [resultText],
            JsonElement { ValueKind: JsonValueKind.Array } result => result
                .EnumerateArray()
                .Where(static element => element.ValueKind is JsonValueKind.String)
                .Select(static element => element.GetString()!),
            _ => [],
        };

        foreach (string result in results)
        {
            try
            {
                XElement element = XElement.Parse(result);
                if (element.Name.LocalName != "result" ||
                    element.Attribute("filename")?.Value is not { Length: > 0 } filename)
                {
                    continue;
                }

                string quote = GetQuote(element.Value, searchPhrase);
                if (quote.Length > 0)
                {
                    return Citation(filename, quote);
                }
            }
            catch (XmlException)
            {
                // Ignore malformed search results.
            }
        }

        return string.Empty;
    }

    private static string GetQuote(string text, string? searchPhrase)
    {
        string[] words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        int start = 0;
        if (!string.IsNullOrWhiteSpace(searchPhrase))
        {
            int fewestOccurrences = int.MaxValue;
            foreach (string queryWord in searchPhrase.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                string term = NormalizeWord(queryWord);
                if (term.Length < 4)
                {
                    continue;
                }

                int occurrences = 0;
                int firstMatch = -1;
                for (int i = 0; i < words.Length; i++)
                {
                    if (IsTermMatch(NormalizeWord(words[i]), term))
                    {
                        occurrences++;
                        firstMatch = firstMatch < 0 ? i : firstMatch;
                    }
                }

                if (occurrences > 0 && occurrences < fewestOccurrences)
                {
                    fewestOccurrences = occurrences;
                    start = firstMatch;
                }
            }
        }

        return string.Join(" ", words, start, Math.Min(5, words.Length - start));
    }

    private static bool IsTermMatch(string word, string term) =>
        word.Length > 0 && (word.Contains(term, StringComparison.Ordinal) || term.Contains(word, StringComparison.Ordinal));

    private static string NormalizeWord(string value) =>
        string.Concat(value.Where(static character => char.IsLetterOrDigit(character)).Select(static character => char.ToLowerInvariant(character)));

    private static bool TryGetFunctionResult(
        MockChatClientRequest request,
        string callId,
        out FunctionResultContent? functionResult)
    {
        for (int i = request.Messages.Count - 1; i >= 0; i--)
        {
            ChatMessage message = request.Messages[i];
            if (message.Role == ChatRole.User)
            {
                break;
            }

            foreach (AIContent content in message.Contents)
            {
                if (content is FunctionResultContent result && result.CallId == callId)
                {
                    functionResult = result;
                    return true;
                }
            }
        }

        functionResult = null;
        return false;
    }

    private static int GetDelay(int? minDelay, int? maxDelay) =>
        (minDelay, maxDelay) switch
        {
            (null, null) => 0,
            ({ } min, null) => min,
            (null, { } max) => Random.Shared.Next(0, max),
            ({ } min, { } max) => Random.Shared.Next(min, max),
        };

    private static void ValidateDelayRange(int? minDelay, int? maxDelay)
    {
        if (minDelay is { } minimumDelayMilliseconds)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minimumDelayMilliseconds, 0, nameof(minDelay));
        }

        if (maxDelay is { } maximumDelayMilliseconds)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maximumDelayMilliseconds, 0, nameof(maxDelay));

            if (minDelay is { } minimumDelayMillisecondsForMaximum)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(maximumDelayMilliseconds, minimumDelayMillisecondsForMaximum, nameof(maxDelay));
            }
        }
    }
}
#pragma warning restore MEAI001

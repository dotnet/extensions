# Experimental decision abstractions

`IDecisionClient` is a provider-neutral abstraction for models that answer a batch of closed-form questions about an input with a probability for every allowed outcome, rather than with generated text.

The abstraction intentionally has a narrow scope. It transports questions to a provider and returns provider-observed answers. It does not threshold, calibrate, renormalize, retry, or turn probabilities into business decisions; those remain application concerns.

## Questions

A request contains one or more questions with caller-owned IDs that are unique within the request. Three kinds are supported:

| Kind | Question | Answer |
| --- | --- | --- |
| Binary | `BinaryDecisionQuestion` | `BinaryDecisionAnswer.TrueProbability` |
| Choice | `ChoiceDecisionQuestion` with `DecisionCandidate` items | `ChoiceDecisionAnswer.SelectedCandidateId` and a distribution over the candidates |
| Score | `ScoreDecisionQuestion` with ordered `DecisionScoreLevel` items | `ScoreDecisionAnswer.Score` (optional) and a distribution over the levels |

Instructions and candidate or level descriptions are lists of `AIContent`. Every constructor also accepts a `string` for the common text-only case.

```csharp
#pragma warning disable MEAI001

using Microsoft.Extensions.AI;

DecisionQuestion[] questions =
[
    new BinaryDecisionQuestion("refund", "Is the customer asking for a refund?"),
    new ChoiceDecisionQuestion("category", "Classify the customer's main concern.",
    [
        new("billing", "Payment, invoice, or charge concerns."),
        new("technical", "Product defects, errors, or outages."),
        new("account", "Sign-in, profile, or account access."),
    ]),
    new ScoreDecisionQuestion("urgency", "How urgent is this ticket?",
    [
        new("low", "Can wait for normal processing."),
        new("medium", "Should be handled today."),
        new("high", "Needs immediate attention."),
    ]),
];
```

## Inputs

A request evaluates every question against the same input, which consists of optional JSON state, optional ordered `AIContent` items, or both:

```csharp
using System.Text.Json;

DecisionRequest request = new(
    state: JsonSerializer.SerializeToElement(ticket, TicketJsonContext.Default.SupportTicket),
    questions: questions,
    contents: [new DataContent(screenshotBytes, "image/png")]);

DecisionResponse response = await client.GetResponseAsync(request);
```

The `GetResponseAsync<TState>` extension serializes application state with a supplied `JsonTypeInfo<TState>`, sends the request, and validates that the response corresponds to it:

```csharp
DecisionResponse response = await client.GetResponseAsync(
    ticket,
    TicketJsonContext.Default.SupportTicket,
    questions,
    contents: [new UriContent(screenshotUri, "image/png")]);
```

Providers must map every content item faithfully or throw `NotSupportedException`. They must not silently drop content. A provider can recognize a provider-specific content item through `AIContent.RawRepresentation`.

## Answers

A response contains exactly one answer per question, in request order. Each answer is either the kind-specific answer type or an `UnansweredDecisionAnswer`, which a provider returns when it declined or could not answer a question (for example, a refusal). Answers to other questions in the same request are preserved.

```csharp
response.Validate(request);

switch (response.GetAnswer("category"))
{
    case ChoiceDecisionAnswer choice:
        Console.WriteLine($"{choice.SelectedCandidateId}: {string.Join(", ", choice.Probabilities.Select(p => $"{p.Id}={p.Value:F3}"))}");
        break;

    case UnansweredDecisionAnswer unanswered:
        Console.WriteLine($"Unanswered ({unanswered.Reason}): {unanswered.Message}");
        break;
}
```

Probabilities are reported as observed by the provider. Each value is in [0, 1], but the abstraction does not require distributions to sum to 1 because providers can report rounded values. `ScoreDecisionAnswer.ExpectedScore` computes the expectation over the observed distribution after normalizing by its total.

`DecisionResponse.Validate` checks that a response has one answer per question in request order, that each answer's ID and kind match, and that each choice or score distribution contains exactly the requested candidate or level IDs in order. It throws `DecisionProtocolException` otherwise.

`RawRepresentation` and `AdditionalProperties` on responses and answers carry provider-specific data such as confidence labels.

## Implementing a provider

An `IDecisionClient` implementation:

- Maps instructions, criteria, and request contents to the provider's input format, and throws `NotSupportedException` for anything it can't represent.
- Returns exactly one answer per question in request order, using `UnansweredDecisionAnswer` for questions that the provider declined to answer.
- Reports distributions using the requested candidate and level IDs in request order.
- Throws `DecisionClientException` for provider failures, setting `IsTransient` for failures that a caller can retry.
- Uses `DecisionOptions.RawRepresentationFactory` to let callers supply provider-specific request options.

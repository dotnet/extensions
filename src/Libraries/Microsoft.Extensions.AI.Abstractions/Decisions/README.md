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

Instructions and candidate or level descriptions are lists of `AIContent`. Every constructor also accepts a `string` for the common text-only case. Choice and score questions take their candidates or levels before their optional instructions. These lists are copied into read-only snapshots: they're typed as `IList<AIContent>` but can't be modified in place, and the `AIContent` items themselves are shared.

```csharp
#pragma warning disable MEAI001

using Microsoft.Extensions.AI;

DecisionQuestion[] questions =
[
    new BinaryDecisionQuestion("refund", "Is the customer asking for a refund?"),
    new ChoiceDecisionQuestion("category",
    [
        new("billing", "Payment, invoice, or charge concerns."),
        new("technical", "Product defects, errors, or outages."),
        new("account", "Sign-in, profile, or account access."),
    ],
    "Classify the customer's main concern."),
    new ScoreDecisionQuestion("urgency",
    [
        new("low", "Can wait for normal processing."),
        new("medium", "Should be handled today."),
        new("high", "Needs immediate attention."),
    ],
    "How urgent is this ticket?"),
];
```

## Inputs

A request has two optional inputs, and at least one must be provided: `State`, a JSON value, and `Contents`, a list of `AIContent` items. Both are request-level inputs shared by every question in the request. Because both are settable, `DecisionRequest` can't enforce that rule itself; each `IDecisionClient` implementation validates it when the request is executed.

The simplest way to send a request is to pass application state as a .NET object. The `GetDecisionAsync<TState>` extension serializes it to `State` with `AIJsonUtilities.DefaultOptions`:

```csharp
record SupportTicket(string Subject, string Body);

SupportTicket ticket = new("Charged twice", "I was billed twice for my subscription this month.");

DecisionResponse response = await client.GetDecisionAsync(
    ticket,
    questions,
    contents: [new DataContent(screenshotBytes, "image/png")]);
```

For Native AOT, use the overload that accepts `JsonSerializerOptions` and pass options from a source-generated context:

```csharp
DecisionResponse response = await client.GetDecisionAsync(
    ticket,
    TicketJsonContext.Default.Options,
    questions);
```

`AIContent` is passed through `contents`, not as state. For full control over the request, serialize the state yourself and pass the resulting `JsonElement` through a `DecisionRequest`:

```csharp
using System.Text.Json;

DecisionRequest request = new(questions)
{
    State = JsonSerializer.SerializeToElement(ticket, TicketJsonContext.Default.SupportTicket),
    Contents = [new UriContent(screenshotUri, "image/png")],
};

DecisionResponse response = await client.GetDecisionAsync(request);
```

`DecisionRequest` requires a nonempty list of questions with unique IDs. Assigning `State` clones the `JsonElement`. A `null` state means no state, which is distinct in memory from a JSON `null` value, although both deserialize from JSON as `null`. Assigning `Contents` copies the list into a read-only snapshot and rejects `null` items; assign a new list to change it.

Providers map the inputs they support and throw `NotSupportedException` for any state or content item they can't represent. They must not silently drop input. A provider can recognize a provider-specific content item through `AIContent.RawRepresentation`.

### Reference guidance

Guidance that applies to every question, such as a policy, rubric, or definitions, is an input like any other. Pass it as a `TextContent` in `contents`, or include it in `State`:

```csharp
DecisionResponse response = await client.GetDecisionAsync(
    ticket,
    questions,
    contents: [new TextContent(refundPolicy)]);
```

Guidance that applies to a single question belongs in that question's `Instructions`. To use a provider-specific channel, such as a system or developer message, supply native request options through `DecisionOptions.RawRepresentationFactory`.

## Answers

A response contains exactly one answer per question, in request order. Each answer is either the kind-specific answer type or an `UnansweredDecisionAnswer`, which a provider returns when it declined or could not answer a question (for example, a refusal). Answers to other questions in the same request are preserved.

```csharp
switch (response.GetAnswer("category"))
{
    case ChoiceDecisionAnswer choice:
        Console.WriteLine($"{choice.SelectedCandidateId}: {string.Join(", ", choice.Probabilities.Select(p => $"{p.Id}={p.Value:F3}"))}");
        break;

    case UnansweredDecisionAnswer unanswered:
        Console.WriteLine($"Unanswered ({unanswered.Reason})");
        break;
}
```

Probabilities are reported as observed by the provider. Each value is in [0, 1], but the abstraction does not require distributions to be complete, ordered, or to sum to 1, because providers can round values or report only some options. Answers only validate what is intrinsic to them; checking an answer against its question's candidates or levels is the job of the consumer that holds the question. `ScoreDecisionAnswer.GetExpectedScore(question)` computes the expectation over the reported levels by level ID, after normalizing by their total.

`IDecisionClient` implementations are responsible for returning one answer per question in request order, and throw `InvalidOperationException` when a provider response can't be mapped to the request.

`RawRepresentation` and `AdditionalProperties` on responses and answers carry provider-specific data such as confidence labels.

## Implementing a provider

An `IDecisionClient` implementation:

- Validates that the request provides `State`, nonempty `Contents`, or both.
- Maps the request inputs (state and contents), instructions, and criteria to the provider's input format, and throws `NotSupportedException` for anything it can't represent.
- Returns exactly one answer per question in request order, using `UnansweredDecisionAnswer` for questions that the provider declined or failed to answer.
- Reports distributions using the requested candidate and level IDs, as the provider reported them. It doesn't need to fill in, reorder, or renormalize them.
- Throws `DecisionClientException` for provider failures, setting `IsTransient` for failures that a caller can retry.
- Uses `DecisionOptions.RawRepresentationFactory` to let callers supply provider-specific request options.

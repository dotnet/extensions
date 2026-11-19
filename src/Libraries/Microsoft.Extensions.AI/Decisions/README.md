# Experimental typed decision APIs

`Microsoft.Extensions.AI` builds on the `IDecisionClient` abstraction from `Microsoft.Extensions.AI.Abstractions` with typed APIs that declare decision questions from a .NET result type and bind provider answers back to it. See the [decision abstractions overview](../../Microsoft.Extensions.AI.Abstractions/Decisions/README.md) for questions, inputs, answers, and provider guidance.

## Typed definitions

A `DecisionDefinition<TResult>` declares one question per bound result property. Question IDs come from the JSON property names of `TResult`, and instructions default to the property's `[Description]` or name:

```csharp
#pragma warning disable MEAI001

using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

DecisionDefinition<TicketDecision> definition = DecisionDefinition<TicketDecision>.Create(
    builder =>
    {
        builder.Choice(result => result.Category);
        builder.BinaryProbability(result => result.RefundProbability, "Is the customer asking for a refund?");
        builder.ExpectedScore(result => result.Urgency,
        [
            new("low", "Can wait for normal processing."),
            new("medium", "Should be handled today."),
            new("high", "Needs immediate attention."),
        ]);
    },
    TicketJsonContext.Default.Options);

public sealed record TicketDecision(
    [property: Description("Classify the customer's main concern.")] TicketCategory Category,
    double RefundProbability,
    double Urgency);

[JsonConverter(typeof(JsonStringEnumConverter<TicketCategory>))]
public enum TicketCategory
{
    [Description("Payment, invoice, or charge concerns.")] Billing,
    [Description("Product defects, errors, or outages.")] Technical,
    [Description("Sign-in, profile, or account access.")] Account,
}
```

| Builder method | Question | Bound value |
| --- | --- | --- |
| `Choice` | Choice over the enum's members, using their serialized names as candidate IDs and their `[Description]` or name as descriptions | The selected enum value |
| `BinaryProbability` | Binary | `BinaryDecisionAnswer.TrueProbability` |
| `Score` | Score over the supplied levels | `ScoreDecisionAnswer.Score`, which the provider must report |
| `ExpectedScore` | Score over the supplied levels | `ScoreDecisionAnswer.GetExpectedScore(question)` |

The definition uses the supplied `JsonSerializerOptions` to resolve the result contract, so it works with source-generated contexts and Native AOT.

## Getting a typed response

The typed `GetDecisionAsync` overloads send the definition's questions and bind the response to `TResult`. Binding throws `InvalidOperationException` when the answer for a declared question is missing or does not match its kind, candidates, or levels. The request-level inputs, shared by every question, are the state and optional `AIContent` items such as an image:

```csharp
DecisionResponse<TicketDecision> response = await client.GetDecisionAsync(ticket, definition);

DecisionResponse<TicketDecision> withImage = await client.GetDecisionAsync(
    ticketText,
    definition,
    contents: [new DataContent(screenshotBytes, "image/png")]);

TicketDecision decision = response.Result;
IReadOnlyDictionary<TicketCategory, double> categories = response.GetDistribution(result => result.Category);
DecisionResponse untyped = response.Response;
```

State passed as a .NET object is serialized with the definition's `SerializerOptions`, so for Native AOT the state type must be in the same source-generated context as the result (for example, `TicketJsonContext.Default.Options`). For full control, serialize the state yourself with `JsonSerializer.SerializeToElement(state, typeInfo)` and pass the `JsonElement`. `AIContent` and sequences of `AIContent` can't be used as state; pass them through the `contents` parameter. To evaluate content without state, create a `DecisionRequest` with `Contents` and call the untyped `IDecisionClient.GetDecisionAsync`.

`DecisionResponse<TResult>.Response` exposes the underlying `DecisionResponse`, including `ModelId`, `Usage`, and the raw answers. `GetDistribution` maps a choice question's distribution to enum values.

## Unanswered questions

When a provider returns an `UnansweredDecisionAnswer` for a question bound by the definition, the typed APIs throw `DecisionUnansweredException`. Its `QuestionIds` property lists the unanswered questions, and its `Response` property exposes the untyped response so that callers can still read the questions that were answered:

```csharp
try
{
    DecisionResponse<TicketDecision> response = await client.GetDecisionAsync(ticket, definition);
}
catch (DecisionUnansweredException ex)
{
    foreach (string questionId in ex.QuestionIds)
    {
        var unanswered = (UnansweredDecisionAnswer)ex.Response.GetAnswer(questionId)!;
        Console.WriteLine($"{questionId}: {unanswered.Reason}");
    }
}
```

## Binding untyped responses

To build a result from an untyped response, use `Bind` with a `DecisionAnswerSet`. A `DecisionEnumDefinition<TEnum>` maps candidate IDs to enum values. Only the answers that the mapping reads must be answered:

```csharp
DecisionEnumDefinition<TicketCategory> categories = new(
[
    new(TicketCategory.Billing, "billing", "Payment, invoice, or charge concerns."),
    new(TicketCategory.Technical, "technical", "Product defects, errors, or outages."),
    new(TicketCategory.Account, "account", "Sign-in, profile, or account access."),
]);

DecisionResponse response = await client.GetDecisionAsync(
    ticket,
    TicketJsonContext.Default.Options,
    [categories.CreateQuestion("category", "Classify the customer's main concern."), refundQuestion]);

TicketTriage triage = response.Bind(answers => new TicketTriage(
    answers.GetChoice("category", categories),
    answers.GetBinary("refund").TrueProbability));
```

## Exposing a definition as a tool

`AsAIFunction<TState>` exposes a definition as an `AIFunction` that accepts `TState` and returns `TResult`, so a chat model can request a decision through function calling:

```csharp
[JsonSerializable(typeof(SupportTicket))]
[JsonSerializable(typeof(TicketDecision))]
internal sealed partial class TicketJsonContext : JsonSerializerContext;

DecisionDefinition<TicketDecision> definition = DecisionDefinition<TicketDecision>.Create(
    builder => { /* declare questions */ },
    TicketJsonContext.Default.Options);

AIFunction evaluate = definition.AsAIFunction<SupportTicket>(decisionClient);
```

The function serializes the state with the definition's `SerializerOptions`, the same as `GetDecisionAsync`, so those options must contain contracts for both the state and result types.

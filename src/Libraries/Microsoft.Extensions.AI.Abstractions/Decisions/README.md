# Experimental decision abstractions

The `Microsoft.Extensions.AI.Abstractions` decision contracts provide a small provider-neutral proposed Layer 1 capability for heterogeneous binary, choice, and ordinal-score questions. Requests preserve caller-owned question and candidate IDs and order; responses preserve complete probability observations, score precision, provenance, usage, and provider extensions without applying thresholds, calibration, or probability repair.

## A business example

Suppose a retailer annotates customer-support tickets. The application wants a normal .NET category, a probability that the ticket is a refund request, and the complete provider evidence for reporting, human review, or downstream training. It is not automatically authorizing a refund or routing a chat.

The domain types remain ordinary application-owned types:

```csharp
#pragma warning disable MEAI001

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

public sealed record SupportTicket(string Message);

public enum TicketCategory
{
    Billing,
    Technical,
    Account,
}

public sealed record TicketAnalysis(
    TicketCategory Category,
    double RefundRequestProbability);
```

The application defines the questions once, then reuses each question's exact caller-owned ID when it maps the response. The client and cancellation token are supplied and owned by the host:

```csharp
public static class SupportTicketAnalyzer
{
    public static async Task<(TicketAnalysis Result, DecisionResponse Evidence)> AnalyzeAsync(
        IDecisionClient client,
        SupportTicket ticket,
        CancellationToken cancellationToken)
    {
        DecisionEnumDefinition<TicketCategory> categories = new(
        [
            new(TicketCategory.Billing, "billing", "The ticket concerns invoices, charges, refunds, or payment."),
            new(TicketCategory.Technical, "technical", "The ticket concerns product defects, errors, outages, or troubleshooting."),
            new(TicketCategory.Account, "account", "The ticket concerns sign-in, profile, or account access."),
        ]);

        ChoiceDecisionQuestion categoryQuestion = categories.CreateQuestion(
            "category",
            "Classify the support ticket as billing, technical, or account access.");
        BinaryDecisionQuestion refundQuestion = new(
            "refund-request",
            "Does the ticket request or describe a refund?");

        DecisionResultBinding<TicketAnalysis> binding = new(
            TicketJsonContext.Default.TicketAnalysis,
            answers => new TicketAnalysis(
                answers.GetChoice(categoryQuestion.Id, categories),
                answers.GetBinary(refundQuestion.Id).TrueProbability));

        DecisionResponse response = await client.GetResponseAsync(
            ticket,
            TicketJsonContext.Default.SupportTicket,
            [categoryQuestion, refundQuestion],
            cancellationToken: cancellationToken);

        return (response.Bind(binding), response);
    }
}
```

The JSON metadata setup is separate from the business call:

```csharp
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(SupportTicket))]
[JsonSerializable(typeof(TicketAnalysis))]
public partial class TicketJsonContext : JsonSerializerContext;
```

`TicketCategory` is a normal caller-owned enum. `DecisionEnumDefinition<TicketCategory>` is only an explicit ID-to-enum lookup plus candidate descriptions; it does not generate an enum, act as an ORM, or infer a domain model. The simple IDs (`"billing"`, `"technical"`, and `"account"`) are exact wire-correlation identities chosen by the caller. They can be assigned once and reused through `categoryQuestion.Id`; no pipe or version syntax is required. The same rule applies to the `"refund-request"` question ID.

`JsonTypeInfo<T>` describes the serialized shape of state or a result. In this example it is supplied by source generation; a configured reflection resolver is also a platform option. It is not handwritten JSON schema and it does not replace the decision instructions or exact provider-facing IDs. The current `DecisionResultBinding<T>` API requires result metadata even though the application mapper shown here does not use metadata to perform its mapping; simplifying that requirement is an API review candidate, not an implicit behavior of this example.

The returned `Evidence` is the complete `DecisionResponse`, including the category distribution and binary observation. The mapped `TicketAnalysis` is therefore not a claim that a hidden threshold or calibration step established a business fact. Applications can retain or inspect the complete evidence for reporting, review, or later training.

Typed application results use an explicit `JsonTypeInfo<T>` and mapper supplied by the application. Feature projection is likewise explicit and optional: callers define a versioned schema of named `double` coordinates over the complete observations when a downstream feature export is useful. A `DecisionFeatureSchema` is a downstream column/coordinate contract, distinct from JSON serialization metadata and not a prerequisite for typed classification.

## .NET platform reuse / ergonomics under review

Layer 1 intentionally reuses existing .NET and MEAI primitives rather than adding a builder, `IQueryable`, ORM, or new source-generator dependency:

- [`JsonSchemaExporter`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/extract-schema) was introduced in .NET 9. The decision contracts use `JsonTypeInfo` and serializer options for explicit metadata; they do not add another schema exporter, and `AIJsonUtilities` provides the existing MEAI compatibility helpers where applicable.
- [System.Text.Json source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation) is the AOT-friendly way to supply `JsonTypeInfo`. `JsonTypeInfo` itself is not synonymous with generated code; ordinary JIT reflection through a configured resolver remains a platform option.
- [`JsonStringEnumMemberNameAttribute`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/customize-properties#custom-enum-member-names) and the generic [`JsonStringEnumConverter`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonstringenumconverter-1) are useful for JSON enum representation. They do not replace decision ID validation: enum conversion is case-insensitive and permits integer values by default, while decision IDs are exact ordinal identities.
- [`AIFunctionFactory`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.aifunctionfactory) remains the existing MEAI primitive for inspecting an explicitly declared delegate, producing a tool schema, and marshalling inputs and outputs. Tool interoperability is a later layer, not a prerequisite for this decision foundation.
- [Structured output](https://learn.microsoft.com/dotnet/ai/quickstarts/structured-output) already supports enum/record-shaped chat results. If an application only needs a label or JSON result, `IChatClient.GetResponseAsync<T>` may be the simpler choice; that path does not by itself establish a provider-reported probability distribution.
- [EF Core model conventions and explicit overrides](https://learn.microsoft.com/ef/core/modeling/) are useful precedent for bounded opt-in conventions, not a reason to add an EF dependency or `DbContext`. [EF Core query providers](https://learn.microsoft.com/ef/core/querying/) likewise do not justify an `IQueryable` decision API for explicit paid asynchronous model inference; ordinary LINQ projection over returned probabilities remains appropriate.

Automated arbitrary semantic inference is out. Bounded opt-in conventions for known enum naming or member mappings remain possible future ergonomics if they are explicit, source-generation/AOT-compatible, and do not hide provider-facing identities or application-owned result mapping.

This layer intentionally does not define tools, routing composition, MEDI processors, provider adapters, ML.NET or Arrow integrations, or automatic POCO/union inference. Those concerns require a later layer and are not implied by these abstractions.

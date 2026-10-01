# Experimental decision abstractions

The `Microsoft.Extensions.AI.Abstractions` decision contracts provide a small provider-neutral proposed Layer 1 capability for heterogeneous binary, choice, and ordinal-score questions. Requests preserve caller-owned question and candidate IDs and order; responses preserve complete probability observations, score precision, provenance, usage, and provider extensions without applying thresholds, calibration, probability repair, retry policy, or a joint-distribution promise.

## A business example

Suppose a retailer annotates customer-support tickets. The application wants a normal .NET category, a probability that the ticket is a refund request, and the complete provider evidence for reporting, human review, or downstream training. It is not automatically authorizing a refund, choosing a route, or treating a probability as a calibrated business decision.

The domain types remain ordinary application-owned types. The standard `DescriptionAttribute` on the positional record parameters supplies business instructions for the selected result properties, while the enum members supply separate candidate criteria:

```csharp
#pragma warning disable MEAI001

using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

public sealed record SupportTicket(string Message);

public enum TicketCategory
{
    [Description("Payment, invoice, or charge concerns.")]
    Billing,

    [Description("Product defects, errors, outages, or troubleshooting.")]
    Technical,

    [Description("Sign-in, profile, or account-access concerns.")]
    Account,
}

public sealed record TicketAnalysis(
    [Description("Classify the customer's main concern.")] TicketCategory Category,
    [Description("Is the customer requesting a refund or payment reversal?")] double RefundRequestProbability);
```

The business call defines a reusable, explicit declaration once. `Choice` declares the closed enum domain and uses its configured JSON enum names as exact candidate IDs; `BinaryProbability` maps the binary answer's `TrueProbability` to a `double`. No handwritten question IDs, enum dictionary, result mapper, JSON schema, or feature schema is required for this ordinary flow. The client and cancellation token are supplied and owned by the host:

```csharp
public static class SupportTicketAnalyzer
{
    private static readonly DecisionDefinition<TicketAnalysis> Definition =
        DecisionDefinition<TicketAnalysis>.Create(definition =>
        {
            definition.Choice(result => result.Category);
            definition.BinaryProbability(result => result.RefundRequestProbability);
        });

    public static async Task<DecisionResponse<TicketAnalysis>> AnalyzeAsync(
        IDecisionClient client,
        SupportTicket ticket,
        CancellationToken cancellationToken)
    {
        return await client.GetResponseAsync(
            ticket,
            Definition,
            cancellationToken: cancellationToken);
    }
}
```

The typed response exposes both the application result and the complete original evidence:

```csharp
DecisionResponse<TicketAnalysis> analyzed = await SupportTicketAnalyzer.AnalyzeAsync(client, ticket, cancellationToken);
TicketAnalysis result = analyzed.Result;
IReadOnlyDictionary<TicketCategory, double> categoryDistribution =
    analyzed.GetDistribution(value => value.Category);
DecisionResponse evidence = analyzed.Evidence;
```

The same definition can include an explicitly ordered score rubric, and heterogeneous declarations remain independent:

```csharp
DecisionDefinition<TicketAnalysis> definition = DecisionDefinition<TicketAnalysis>.Create(builder =>
{
    builder.Choice(result => result.Category, "Classify the customer's main concern.");
    builder.BinaryProbability(result => result.RefundRequestProbability);
    // A score property would be declared with an explicit DecisionScoreLevel list:
    // builder.Score(result => result.Satisfaction, [new("low", "Low"), new("high", "High")]);
});
```

At an explicit JSON boundary, callers can use source-generated metadata for both state and result contracts. The explicit generic arguments are optional for inference, but make the boundary visible:

```csharp
DecisionDefinition<TicketAnalysis> definition =
    DecisionDefinition<TicketAnalysis>.Create(TicketJsonContext.Default.TicketAnalysis, builder =>
    {
        builder.Choice(result => result.Category);
        builder.BinaryProbability(result => result.RefundRequestProbability);
    });

DecisionResponse<TicketAnalysis> analyzed = await client.GetResponseAsync<SupportTicket, TicketAnalysis>(
    ticket,
    TicketJsonContext.Default.SupportTicket,
    definition,
    cancellationToken: cancellationToken);
```

Applications that only need the neutral response can keep the no-binding path:

```csharp
DecisionResponse evidence = await client.GetResponseAsync(
    ticket,
    TicketJsonContext.Default.SupportTicket,
    definition.Questions,
    cancellationToken: cancellationToken);
```

The JSON metadata setup is separate from the business call:

```csharp
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(SupportTicket))]
[JsonSerializable(typeof(TicketCategory))]
[JsonSerializable(typeof(TicketAnalysis))]
public partial class TicketJsonContext : JsonSerializerContext;
```

`TicketCategory` is a normal caller-owned enum. `DecisionDefinition<TicketAnalysis>` is an explicit declaration of which result members participate; it is not an enum generator, ORM, arbitrary POCO/union inference engine, or LINQ query provider. The candidate IDs come from the configured JSON enum contract and are exact ordinal wire-correlation identities. They are stable simple strings reused through the question definition; no pipe/version syntax is required. If a domain needs dynamic IDs instead of a closed enum, use the lower-level `ChoiceDecisionQuestion` and explicit mapping APIs so stale or unknown IDs remain rejected.

Attributes are an ergonomic fallback for instructions and enum candidate descriptions. A fluent instruction supplied to `Choice`, `BinaryProbability`, or `Score` wins over an attribute. The metadata reader checks an attributed property first, then an associated positional-record constructor parameter, then a type-level fallback. JSON enum serialization controls the candidate IDs, but JSON conversion's permissive/case-insensitive behavior does not replace exact decision correlation validation.

`JsonTypeInfo<T>` describes the serialized shape of state or a result. It may come from source generation or a configured reflection resolver; it is not handwritten JSON schema and it does not replace decision instructions, exact IDs, complete distributions, or stale-domain checks. The older `DecisionResultBinding<T>` API still requires result metadata even though a delegate mapper may not use it; simplifying that requirement is an API review candidate, not an implicit behavior of this convenience layer.

The returned `Evidence` is the complete `DecisionResponse`, including every category probability and the binary observation. The mapped `TicketAnalysis` is therefore not a claim that a hidden threshold or calibration step established a business fact. Applications can retain or inspect the complete evidence for reporting, review, or later training. Feature projection is an optional follow-on: callers may define a versioned `DecisionFeatureSchema` of named native-`double` coordinates over complete observations when a downstream feature export is useful. That schema is a downstream column/coordinate contract, distinct from JSON metadata and not a prerequisite for typed classification.

## .NET platform reuse / ergonomics under review

Layer 1 intentionally reuses existing .NET and MEAI primitives rather than adding a builder, `IQueryable`, ORM, or new source-generator dependency:

- [`JsonSchemaExporter`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/extract-schema) was introduced in .NET 9. The decision contracts use `JsonTypeInfo` and serializer options for explicit metadata; they do not add another schema exporter, and `AIJsonUtilities` provides the existing MEAI compatibility helpers where applicable.
- [System.Text.Json source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation) is the AOT-friendly way to supply `JsonTypeInfo`. `JsonTypeInfo` itself is not synonymous with generated code; ordinary JIT reflection through a configured resolver remains a platform option.
- [`JsonStringEnumMemberNameAttribute`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/customize-properties#custom-enum-member-names) and the generic [`JsonStringEnumConverter`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonstringenumconverter-1) are useful for JSON enum representation. They do not replace decision ID validation: enum conversion is case-insensitive and permits integer values by default, while decision IDs are exact ordinal identities.
- [`AIFunctionFactory`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.aifunctionfactory) remains the existing MEAI primitive for inspecting an explicitly declared delegate, producing a tool schema, and marshalling inputs and outputs. Tool interoperability is a later layer, not a prerequisite for this decision foundation.
- [Structured output](https://learn.microsoft.com/dotnet/ai/quickstarts/structured-output) already supports enum/record-shaped chat results. If an application only needs a label or JSON result, `IChatClient.GetResponseAsync<T>` may be the simpler choice; that path does not by itself establish a provider-reported probability distribution.
- [EF Core model conventions and explicit overrides](https://learn.microsoft.com/ef/core/modeling/) are useful precedent for bounded opt-in conventions, not a reason to add an EF dependency or `DbContext`. [EF Core query providers](https://learn.microsoft.com/ef/core/querying/) likewise do not justify an `IQueryable` decision API for explicit paid asynchronous model inference; ordinary LINQ projection over returned probabilities remains appropriate.

Automated arbitrary semantic inference is out. Bounded opt-in conventions for known enum naming or member mappings remain possible future ergonomics if they are explicit, source-generation/AOT-compatible, and do not hide provider-facing identities or application-owned result mapping.

This layer intentionally does not define tools, routing composition, MEDI processors, provider adapters, ML.NET or Arrow integrations, or automatic arbitrary POCO/union semantic inference. AIFunction/tool interoperability is a Layer 2 concern and is not implied by these abstractions.

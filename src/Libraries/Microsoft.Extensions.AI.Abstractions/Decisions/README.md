# Experimental decision abstractions

The `Microsoft.Extensions.AI.Abstractions` decision contracts provide a small provider-neutral Layer 1 capability for heterogeneous binary, choice, and ordinal-score questions. Requests preserve caller-owned question and candidate IDs and order. Responses retain complete probability observations, score precision, provenance, usage, and provider evidence without applying thresholds, calibration, probability repair, retry policy, or a joint-distribution promise.

## A business example

Suppose a retailer annotates customer-support tickets. The application wants a normal .NET category, a probability that the ticket is a refund request, and complete provider evidence for reporting or human review. It is not automatically authorizing a refund, choosing a route, or treating a probability as a calibrated business decision.

The domain types remain ordinary application-owned types:

```csharp
#pragma warning disable MEAI001

using System.ComponentModel;

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

The host explicitly supplies an `IDecisionClient`. It is provider-neutral; MEAI does not create a default implementation and an arbitrary `IChatClient` is not automatically a decision client. In the external, unpublished provider proof, acquisition is separate from platform composition:

```csharp
// External proof/provider packages only; not shipped by Microsoft.Extensions.AI.
using DecisionInference.Julia;
using DecisionInference.MEAI;

JuliaDecisionGenerator generator =
    JuliaDecisionGenerator.LoadFromDirectory(assetDirectory);
IDecisionClient client = generator.AsDecisionClient();
```

The Julia/MEAI bridge above is historical implementation evidence, not a released dependency or a claim that this repository repins or executes a provider. Jev/OllamaSharp and the Ollaya, Laya, Julia, and Qwen adapters remain provider-owned. A deterministic test double is used for the platform proofs.

The reusable typed declaration is ordinary application code:

```csharp
DecisionDefinition<TicketAnalysis> definition =
    DecisionDefinition<TicketAnalysis>.Create(builder =>
    {
        builder.Choice(result => result.Category);
        builder.BinaryProbability(result => result.RefundRequestProbability);
    });

SupportTicket ticket = new(
    "I was charged twice. Please refund the duplicate payment.");

DecisionResponse<TicketAnalysis> response =
    await client.GetResponseAsync(ticket, definition);

TicketAnalysis analysis = response.Result;
DecisionResponse evidence = response.Evidence;
```

`response.Evidence` contains the complete neutral response, including every category probability and the binary observation. It is separate from any chat usage or business action. The mapped result is not a hidden threshold or calibration claim.

For an explicit JSON boundary, supply source-generated metadata to both state and result contracts:

```csharp
DecisionDefinition<TicketAnalysis> metadataDefinition =
    DecisionDefinition<TicketAnalysis>.Create(
        TicketJsonContext.Default.TicketAnalysis,
        builder =>
        {
            builder.Choice(result => result.Category);
            builder.BinaryProbability(result => result.RefundRequestProbability);
        });

DecisionResponse<TicketAnalysis> metadataResponse =
    await client.GetResponseAsync(
        ticket,
        TicketJsonContext.Default.SupportTicket,
        metadataDefinition);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SupportTicket))]
[JsonSerializable(typeof(TicketCategory))]
[JsonSerializable(typeof(TicketAnalysis))]
public partial class TicketJsonContext : JsonSerializerContext;
```

`JsonTypeInfo<T>` describes a serialized state or result shape. It does not replace decision instructions, exact IDs, complete distributions, or stale-domain checks. JSON enum naming controls candidate IDs, while decision correlation remains exact and rejects unknown or changed IDs.

## Layer 2 interoperability: two independent paths

Layer 2 deliberately adds no decision-specific function wrapper and no public routing type. The two useful compositions are independent consumer-owned uses of existing MEAI primitives.

### 1. Annotated application method to `AIFunctionFactory`

An ordinary annotated method can close over a host-injected client and definition. The client, definition, endpoint, model, and options are not model-supplied tool arguments:

```csharp
public sealed class SupportTicketTools(
    IDecisionClient client,
    DecisionDefinition<TicketAnalysis> definition,
    Action<DecisionResponse>? recordEvidence = null)
{
    [DisplayName("analyze_support_ticket")]
    [Description("Annotate one support ticket.")]
    public async Task<TicketAnalysis> AnalyzeAsync(
        [Description("The ticket to classify.")] SupportTicket ticket,
        CancellationToken cancellationToken)
    {
        DecisionResponse<TicketAnalysis> response = await client.GetResponseAsync(
            ticket,
            definition,
            cancellationToken: cancellationToken);

        recordEvidence?.Invoke(response.Evidence);
        return response.Result;
    }
}

SupportTicketTools host = new(client, definition, recordEvidence);
AIFunction analyzeTicket = AIFunctionFactory.Create(
    host.AnalyzeAsync,
    new AIFunctionFactoryOptions
    {
        Name = "analyze_support_ticket",
        Description = "Annotate a support ticket for review.",
    });

IChatClient chatClient = configuredChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

ChatResponse chatResponse = await chatClient.GetResponseAsync(
    messages,
    new ChatOptions { Tools = [analyzeTicket] },
    cancellationToken);
```

The result-only method returns the business result by default. `recordEvidence` is an explicit host-owned per-invocation callback; it is not a shared last-response slot or automatic analytics. If selected metadata is useful, define an application DTO and return only the mapped result plus `response.Evidence.Provenance` and `response.Evidence.Usage`. Do not return raw state, provider extensions, credentials, reasoning text, or unrestricted dictionaries. The decision call is one logical operation, cancellation and provider/mapper/host failures propagate, and decision usage remains separate from chat usage.

`AIFunctionFactory` options can override method attributes. Its schema contains only the application input (`ticket` here); `client`, `definition`, and `CancellationToken` are infrastructure, not model-facing parameters. The middleware example above exercises normal function invocation; direct `AIFunction.InvokeAsync` is sufficient for a platform-only proof and should not be described as a chat-loop proof.

For reflection-disabled or trimmed deployments, provide source-generated metadata at both boundaries: use `DecisionDefinition<TicketAnalysis>.Create(TicketJsonContext.Default.TicketAnalysis, ...)`, call the overload accepting `TicketJsonContext.Default.SupportTicket`, and set `AIFunctionFactoryOptions.SerializerOptions = TicketJsonContext.Default.Options`. Function metadata alone does not configure decision state binding. This is a source-generation-compatible deployment variant, not a Native AOT certification.

### 2. Decision-driven chat routing

Routing is a separate provider/client-selection problem. Construct one provider-neutral decision client and multiple configured, caller-owned `IChatClient` instances. Project meaningful bounded request state, use a typed `DecisionDefinition`, and compose with the existing `RoutingChatClient.Create`:

```csharp
public enum RouteKind
{
    Fast,
    Reasoning,
}

public sealed record RoutingDecision(RouteKind Route);
public sealed record RoutingState(string RequestText, bool HasTools);

DecisionDefinition<RoutingDecision> routeDefinition =
    DecisionDefinition<RoutingDecision>.Create(
        RoutingJsonContext.Default.RoutingDecision,
        builder => builder.Choice(result => result.Route));

IReadOnlyDictionary<RouteKind, IChatClient> clients =
    new Dictionary<RouteKind, IChatClient>
    {
        [RouteKind.Fast] = fastConfiguredClient,
        [RouteKind.Reasoning] = reasoningConfiguredClient,
    };

RoutingChatClient router = RoutingChatClient.Create(
    async (context, cancellationToken) =>
    {
        RoutingState state = new(
            string.Join("\n", context.Messages.Select(message => message.Text)),
            context.ChatOptions?.Tools is not null);

        DecisionResponse<RoutingDecision> decision =
            await decisionClient.GetResponseAsync(
                state,
                RoutingJsonContext.Default.RoutingState,
                routeDefinition,
                cancellationToken: cancellationToken);

        if (!clients.TryGetValue(decision.Result.Route, out IChatClient? selected))
        {
            throw new InvalidOperationException("The decision selected an unconfigured route.");
        }

        return selected;
    });
```

Each configured client owns its route-specific model identity and options. The router forwards the original messages and a cloned request-options object to exactly one selected client, including streaming calls. The caller owns the configured clients and remains responsible for disposing them; the router does not dispose borrowed clients. Invalid or wrong-domain decision responses, cancellation, and provider failures stop before chat forwarding. There is no retry, failover, conversation affinity, mid-stream switching, outcome learning, or automatic calibration. Tool loops run inside the selected client after routing; the first example starts a fresh decision per router call. Decision probabilities are task observations, not calibrated task-success guarantees.

### Semantic ingestion proof

The MEDI proof remains consumer-owned and uses the existing document-reader -> chunker -> semantic decision chunk processor -> collecting/serializing writer flow. It preserves chunk content, document identity/context, unrelated metadata, bounded streaming, cancellation, and stable feature schema identity/version/provenance. It does not add `IngestionDocument.Metadata`, alter `ClassificationEnricher` or batching, fabricate chat responses, or add an upstream ML.NET/Arrow dependency.

## Alternative Designs

- A decision-specific `AIFunction` wrapper was removed: ordinary annotated methods plus `AIFunctionFactory.Create` already provide the safe schema, host closure, cancellation, and standard middleware composition.
- A reusable decision router was not added: typed `DecisionDefinition` plus `RoutingChatClient.Create` is enough for application-specific eligibility and client maps.
- A reusable MEDI processor was not added: the real pipeline proof does not establish a separately justified public ingestion API.

## Risks and limitations

These contracts are experimental (`MEAI001`). Hosts own client lifetime, safe output DTOs, evidence retention, route policy, and business actions. Provider adapters and external proof packages are not shipped or repinned here. No live provider/model execution, package-release availability, Native AOT certification, calibration guarantee, or universal provider accuracy is claimed.

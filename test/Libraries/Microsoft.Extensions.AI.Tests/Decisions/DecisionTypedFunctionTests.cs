// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Test models and their source-generated contexts are co-located.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class DecisionTypedFunctionTests
{
    [Fact]
    public async Task AIFunctionFactory_UsesAnnotatedMethodAndTypedDecisionDefinition()
    {
        using SupportTicketDecisionClient client = new();
        SupportTicketFunction host = new(client, CreateDefinition());

        AIFunction function = AIFunctionFactory.Create(
            host.AnalyzeAsync,
            new AIFunctionFactoryOptions { SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options });

        Assert.Equal("analyze_support_ticket", function.Name);
        Assert.Equal("Annotate one support ticket.", function.Description);
        JsonElement schema = function.JsonSchema;
        JsonElement properties = schema.GetProperty("properties");
        Assert.Equal(["ticket"], properties.EnumerateObject().Select(static property => property.Name));
        Assert.Equal("The ticket to classify.", properties.GetProperty("ticket").GetProperty("description").GetString());
        Assert.Contains("ticket", schema.GetProperty("required").EnumerateArray().Select(static value => value.GetString()));

        JsonElement output = Assert.IsType<JsonElement>(await function.InvokeAsync(CreateArguments("The product is unavailable and I need help.")));

        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("refundRequestProbability").GetDouble());
        Assert.Equal(1, client.CallCount);
        Assert.Equal("The product is unavailable and I need help.", client.LastRequest!.State!.Value.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AsAIFunction_ExposesStateAndReturnsOnlyTheResult()
    {
        SupportTicketDecisionClient client = new();
        AIFunction function = CreateDefinition().AsAIFunction<SupportTicket>(client);

        Assert.Equal("evaluate_decision", function.Name);
        Assert.Equal("Evaluate the declared decision for one application state.", function.Description);
        JsonElement properties = function.JsonSchema.GetProperty("properties");
        Assert.Equal(["state"], properties.EnumerateObject().Select(static property => property.Name));
        Assert.Equal("The application state for the declared decision.", properties.GetProperty("state").GetProperty("description").GetString());
        Assert.Contains("state", function.JsonSchema.GetProperty("required").EnumerateArray().Select(static value => value.GetString()));

        JsonElement output = Assert.IsType<JsonElement>(await function.InvokeAsync(CreateDecisionArguments()));

        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("refundRequestProbability").GetDouble());
        Assert.DoesNotContain("usage", output.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("provider-evidence", output.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(1, client.CallCount);
        Assert.Equal("Please review this account issue.", client.LastRequest!.State!.Value.GetProperty("message").GetString());
        Assert.Empty(client.LastRequest.Contents);
        Assert.Equal(0, client.DisposeCount);

        client.Dispose();
    }

    [Fact]
    public async Task AsAIFunction_PreservesFactoryOptionsAndClonesDecisionOptionsPerInvocation()
    {
        using SupportTicketDecisionClient client = new() { MutateOptions = true };
        DecisionOptions decisionOptions = new() { ModelId = "configured-model" };
        AIFunction function = CreateDefinition().AsAIFunction<SupportTicket>(
            client,
            new AIFunctionFactoryOptions
            {
                Name = "custom_decision",
                Description = "Custom decision description.",
                ExcludeResultSchema = true,
                MarshalResult = static (value, _, _) =>
                {
                    TicketAnalysis result = Assert.IsType<TicketAnalysis>(value);
                    Assert.Equal(TicketCategory.Technical, result.Category);
                    Assert.Equal(0.25, result.RefundRequestProbability);
                    return new ValueTask<object?>("custom-marshaled");
                },
            },
            decisionOptions);
        decisionOptions.ModelId = "caller-mutated";

        Assert.Equal("custom_decision", function.Name);
        Assert.Equal("Custom decision description.", function.Description);
        Assert.Null(function.ReturnJsonSchema);

        Assert.Equal("custom-marshaled", await function.InvokeAsync(CreateDecisionArguments()));
        Assert.Equal("custom-marshaled", await function.InvokeAsync(CreateDecisionArguments("A second request.")));

        Assert.Equal(["configured-model", "configured-model"], client.SeenModelIds);
        Assert.Equal(2, client.SeenOptions.Count);
        Assert.NotSame(client.SeenOptions[0], client.SeenOptions[1]);
        Assert.Equal("caller-mutated", decisionOptions.ModelId);
    }

    [Fact]
    public async Task AsAIFunction_CombinesSeparateStateAndResultContexts()
    {
#if NET
        Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
#endif
        using SupportTicketDecisionClient client = new();
        DecisionDefinition<TicketAnalysis> definition = CreateResultOnlyDefinition();

        AIFunction function = definition.AsAIFunction(client, stateTypeInfo: DecisionStateOnlyJsonContext.Default.SupportTicket);

        JsonElement output = Assert.IsType<JsonElement>(await function.InvokeAsync(CreateRawDecisionArguments()));

        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("refundRequestProbability").GetDouble());
        Assert.Equal("Source-generated state.", client.LastRequest!.State!.Value.GetProperty("message").GetString());
        Assert.Equal("state", Assert.Single(function.JsonSchema.GetProperty("properties").EnumerateObject()).Name);
    }

    [Fact]
    public async Task Create_SupportsCombinedSourceGeneratedResolvers()
    {
#if NET
        Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
#endif
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(DecisionStateOnlyJsonContext.Default, DecisionResultOnlyJsonContext.Default),
            Converters = { new JsonStringEnumConverter<TicketCategory>() },
        };
        DecisionDefinition<TicketAnalysis> definition = DecisionDefinition<TicketAnalysis>.Create(
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability);
            },
            options);
        using SupportTicketDecisionClient client = new();

        DecisionResponse<TicketAnalysis> response = await client.GetDecisionAsync(
            JsonSerializer.SerializeToElement(new SupportTicket("Combined state."), DecisionStateOnlyJsonContext.Default.SupportTicket),
            definition);
        AIFunction function = definition.AsAIFunction<SupportTicket>(client);
        JsonElement output = Assert.IsType<JsonElement>(await function.InvokeAsync(CreateRawDecisionArguments()));

        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(2, client.CallCount);
    }

    [Fact]
    public void AsAIFunction_RequiresStateMetadataWhenDefinitionOptionsDoNotContainIt()
    {
        using SupportTicketDecisionClient client = new();

        Assert.Throws<NotSupportedException>(() => CreateResultOnlyDefinition().AsAIFunction<SupportTicket>(client));
    }

    [Fact]
    public async Task AsAIFunction_PropagatesProviderAndCancellationFailures()
    {
        DecisionClientException providerFailure = new("provider failed", isTransient: true);
        using SupportTicketDecisionClient failedClient = new() { Failure = providerFailure };
        AIFunction failedFunction = CreateDefinition().AsAIFunction<SupportTicket>(failedClient);

        DecisionClientException actualProviderFailure = await Assert.ThrowsAsync<DecisionClientException>(
            () => failedFunction.InvokeAsync(CreateDecisionArguments()).AsTask());
        Assert.Same(providerFailure, actualProviderFailure);

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using SupportTicketDecisionClient cancelledClient = new();
        AIFunction cancelledFunction = CreateDefinition().AsAIFunction<SupportTicket>(cancelledClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledFunction.InvokeAsync(CreateDecisionArguments(), cancellation.Token).AsTask());
        Assert.Equal(1, cancelledClient.CallCount);
    }

    [Theory]
    [InlineData(ResponseMismatch.Question)]
    [InlineData(ResponseMismatch.Candidate)]
    public async Task AsAIFunction_RejectsMismatchedResponsesBeforeReturningAResult(ResponseMismatch mismatch)
    {
        using SupportTicketDecisionClient client = new() { Mismatch = mismatch };
        AIFunction function = CreateDefinition().AsAIFunction<SupportTicket>(client);

        await Assert.ThrowsAsync<DecisionProtocolException>(() => function.InvokeAsync(CreateDecisionArguments()).AsTask());

        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task AsAIFunction_PropagatesUnansweredBoundQuestion()
    {
        using SupportTicketDecisionClient client = new() { Unanswered = true };
        AIFunction function = CreateDefinition().AsAIFunction<SupportTicket>(client);

        DecisionUnansweredException exception = await Assert.ThrowsAsync<DecisionUnansweredException>(
            () => function.InvokeAsync(CreateDecisionArguments()).AsTask());

        Assert.Equal(["refundRequestProbability"], exception.QuestionIds);
        Assert.IsType<UnansweredDecisionAnswer>(exception.Response.Answers[1]);
    }

    [Fact]
    public void AsAIFunction_RejectsNullClient()
    {
        Assert.Throws<ArgumentNullException>("client", () => CreateDefinition().AsAIFunction<SupportTicket>(null!));
    }

    [Fact]
    public async Task AsAIFunction_CanBeRegisteredPerScopeWithOneClientResolution()
    {
        using SupportTicketDecisionClient client = new();
        ServiceCollection services = new();
        int clientFactoryCalls = 0;
        services.AddScoped<IDecisionClient>(_ =>
        {
            clientFactoryCalls++;
            return client;
        });
        services.AddScoped<AIFunction>(serviceProvider =>
            CreateDefinition().AsAIFunction<SupportTicket>(
                serviceProvider.GetRequiredService<IDecisionClient>(),
                new AIFunctionFactoryOptions { Name = "scoped_decision" }));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        AIFunction first = scope.ServiceProvider.GetRequiredService<AIFunction>();
        AIFunction second = scope.ServiceProvider.GetRequiredService<AIFunction>();

        Assert.Same(first, second);
        Assert.Equal(1, clientFactoryCalls);

        _ = await first.InvokeAsync(CreateDecisionArguments());

        Assert.Equal(1, client.CallCount);
        Assert.Equal(0, client.DisposeCount);
    }

    [Fact]
    public async Task AIFunctionFactory_CanReturnResultAndRecordResponsePerInvocation()
    {
        using SupportTicketDecisionClient client = new();
        List<DecisionResponse> recorded = [];
        SupportTicketFunction host = new(client, CreateDefinition(), recorded.Add);

        AIFunction function = AIFunctionFactory.Create(
            host.AnalyzeWithMetadataAsync,
            new AIFunctionFactoryOptions { SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options });

        JsonElement output = Assert.IsType<JsonElement>(await function.InvokeAsync(CreateArguments("The product is unavailable and I need help.")));

        Assert.Equal("Technical", output.GetProperty("result").GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("result").GetProperty("refundRequestProbability").GetDouble());
        Assert.Equal("test-model", output.GetProperty("modelId").GetString());
        Assert.Equal(7, output.GetProperty("usage").GetProperty("inputTokenCount").GetInt32());
        Assert.Equal(2, output.GetProperty("usage").GetProperty("outputTokenCount").GetInt32());
        Assert.DoesNotContain("provider-evidence", output.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("retained", output.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("The product is unavailable", output.GetRawText(), StringComparison.Ordinal);
        DecisionResponse response = Assert.Single(recorded);
        Assert.Equal("provider-evidence", response.RawRepresentation);
        Assert.Equal("retained", response.AdditionalProperties!["evidence"]);
    }

    [Fact]
    public void AIFunctionFactory_OptionsOverrideAnnotatedMetadata()
    {
        using SupportTicketDecisionClient client = new();
        SupportTicketFunction host = new(client, CreateDefinition());

        AIFunction function = AIFunctionFactory.Create(
            host.AnalyzeAsync,
            new AIFunctionFactoryOptions
            {
                Name = "override_name",
                Description = "Override description.",
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

        Assert.Equal("override_name", function.Name);
        Assert.Equal("Override description.", function.Description);
    }

    [Fact]
    public async Task AIFunctionFactory_PropagatesCancellationProviderAndHostFailures()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using SupportTicketDecisionClient cancelledClient = new();
        AIFunction cancelledFunction = CreateFunction(new SupportTicketFunction(cancelledClient, CreateDefinition()).AnalyzeAsync);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledFunction.InvokeAsync(CreateArguments(), cancellation.Token).AsTask());
        Assert.Equal(1, cancelledClient.CallCount);

        DecisionClientException providerFailure = new("provider failed", isTransient: true);
        using SupportTicketDecisionClient failedClient = new() { Failure = providerFailure };
        AIFunction failedFunction = CreateFunction(new SupportTicketFunction(failedClient, CreateDefinition()).AnalyzeAsync);

        DecisionClientException actualProviderFailure = await Assert.ThrowsAsync<DecisionClientException>(
            () => failedFunction.InvokeAsync(CreateArguments()).AsTask());
        Assert.Same(providerFailure, actualProviderFailure);

        InvalidOperationException hostFailure = new("host recording failed");
        using SupportTicketDecisionClient hostClient = new();
        SupportTicketFunction failingHost = new(hostClient, CreateDefinition(), _ => throw hostFailure);
        AIFunction hostFunction = CreateFunction(failingHost.AnalyzeWithMetadataAsync);

        InvalidOperationException actualHostFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostFunction.InvokeAsync(CreateArguments()).AsTask());
        Assert.Same(hostFailure, actualHostFailure);
    }

    [Theory]
    [InlineData(ResponseMismatch.Question)]
    [InlineData(ResponseMismatch.Candidate)]
    public async Task AIFunctionFactory_RejectsResponseCorrelationBeforeResultMapping(ResponseMismatch mismatch)
    {
        int recordCalls = 0;
        using SupportTicketDecisionClient client = new() { Mismatch = mismatch };
        SupportTicketFunction host = new(client, CreateDefinition(), _ => recordCalls++);
        AIFunction function = CreateFunction(host.AnalyzeWithMetadataAsync);

        await Assert.ThrowsAsync<DecisionProtocolException>(() => function.InvokeAsync(CreateArguments()).AsTask());

        Assert.Equal(1, client.CallCount);
        Assert.Equal(0, recordCalls);
    }

    private static AIFunction CreateFunction(Delegate method) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = "analyze_support_ticket",
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

    private static AIFunctionArguments CreateArguments(string message = "Please review this account issue.") =>
        new()
        {
            ["ticket"] = JsonSerializer.SerializeToElement(new SupportTicket(message), DecisionTypedFunctionJsonContext.Default.SupportTicket),
        };

    private static AIFunctionArguments CreateDecisionArguments(string message = "Please review this account issue.") =>
        new()
        {
            ["state"] = JsonSerializer.SerializeToElement(new SupportTicket(message), DecisionTypedFunctionJsonContext.Default.SupportTicket),
        };

    private static AIFunctionArguments CreateRawDecisionArguments()
    {
        using JsonDocument document = JsonDocument.Parse("{\"message\":\"Source-generated state.\"}");
        return new() { ["state"] = document.RootElement.Clone() };
    }

    private static DecisionDefinition<TicketAnalysis> CreateDefinition() =>
        CreateDefinition(DecisionTypedFunctionJsonContext.Default.Options);

    private static DecisionDefinition<TicketAnalysis> CreateDefinition(JsonSerializerOptions options) =>
        DecisionDefinition<TicketAnalysis>.Create(
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability);
            },
            options);

    private static DecisionDefinition<TicketAnalysis> CreateResultOnlyDefinition() =>
        CreateDefinition(DecisionResultOnlyJsonContext.Default.Options);

    private sealed class SupportTicketFunction(
        IDecisionClient client,
        DecisionDefinition<TicketAnalysis> definition,
        Action<DecisionResponse>? record = null)
    {
        [DisplayName("analyze_support_ticket")]
        [Description("Annotate one support ticket.")]
        public async Task<TicketAnalysis> AnalyzeAsync(
            [Description("The ticket to classify.")] SupportTicket ticket,
            CancellationToken cancellationToken)
        {
            DecisionResponse<TicketAnalysis> response = await client.GetDecisionAsync(
                JsonSerializer.SerializeToElement(ticket, DecisionTypedFunctionJsonContext.Default.SupportTicket),
                definition,
                cancellationToken: cancellationToken);
            record?.Invoke(response.Response);
            return response.Result;
        }

        [DisplayName("analyze_support_ticket_with_metadata")]
        [Description("Annotate one support ticket and return selected decision metadata.")]
        public async Task<MetadataOnlyToolResult> AnalyzeWithMetadataAsync(
            [Description("The ticket to classify.")] SupportTicket ticket,
            CancellationToken cancellationToken)
        {
            DecisionResponse<TicketAnalysis> response = await client.GetDecisionAsync(
                JsonSerializer.SerializeToElement(ticket, DecisionTypedFunctionJsonContext.Default.SupportTicket),
                definition,
                cancellationToken: cancellationToken);
            record?.Invoke(response.Response);
            return new(response.Result, response.Response.ModelId, response.Response.Usage);
        }
    }

    private sealed class SupportTicketDecisionClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public DecisionClientException? Failure { get; init; }

        public ResponseMismatch Mismatch { get; init; }

        public bool Unanswered { get; init; }

        public DecisionRequest? LastRequest { get; private set; }

        public bool MutateOptions { get; init; }

        public List<DecisionOptions> SeenOptions { get; } = [];

        public List<string?> SeenModelIds { get; } = [];

        public int DisposeCount { get; private set; }

        public Task<DecisionResponse> GetDecisionAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            if (options is not null)
            {
                SeenOptions.Add(options);
                SeenModelIds.Add(options.ModelId);
                if (MutateOptions)
                {
                    options.ModelId = "provider-mutated";
                }
            }

            if (Failure is not null)
            {
                return Task.FromException<DecisionResponse>(Failure);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            ChoiceDecisionQuestion choice = Assert.IsType<ChoiceDecisionQuestion>(request.Questions[0]);
            BinaryDecisionQuestion binary = Assert.IsType<BinaryDecisionQuestion>(request.Questions[1]);
            string choiceId = Mismatch == ResponseMismatch.Question ? "different-question" : choice.Id;
            ChoiceDecisionAnswer choiceAnswer = Mismatch == ResponseMismatch.Candidate
                ? new(choiceId, "unrelated", [new("unrelated", 1)])
                : new(choiceId, choice.Candidates[1].Id, [new(choice.Candidates[0].Id, 0.2), new(choice.Candidates[1].Id, 0.5), new(choice.Candidates[2].Id, 0.3)]);
            DecisionAnswer binaryAnswer = Unanswered
                ? new UnansweredDecisionAnswer(binary.Id, DecisionKind.Binary) { Reason = DecisionUnansweredReason.Refusal }
                : new BinaryDecisionAnswer(binary.Id, 0.25);

            return Task.FromResult(new DecisionResponse([choiceAnswer, binaryAnswer])
            {
                ModelId = "test-model",
                Usage = new UsageDetails { InputTokenCount = 7, OutputTokenCount = 2 },
                RawRepresentation = "provider-evidence",
                AdditionalProperties = new() { ["evidence"] = "retained" },
            });
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            DisposeCount = 1;
        }
    }

    public enum ResponseMismatch
    {
        None,
        Question,
        Candidate,
    }

    internal sealed record SupportTicket(string Message);

    internal enum TicketCategory
    {
        [Description("Payment, invoice, or charge concerns.")]
        Billing,

        [Description("Product defects, errors, outages, or troubleshooting.")]
        Technical,

        [Description("Sign-in, profile, or account-access concerns.")]
        Account,
    }

    internal sealed record TicketAnalysis(
        [Description("Classify the customer's main concern.")] TicketCategory Category,
        [Description("Is the customer requesting a refund or payment reversal?")] double RefundRequestProbability);

    internal sealed record MetadataOnlyToolResult(
        TicketAnalysis Result,
        string? ModelId,
        UsageDetails? Usage);
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DecisionTypedFunctionTests.SupportTicket))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketCategory))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketAnalysis))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.MetadataOnlyToolResult))]
[JsonSerializable(typeof(UsageDetails))]
internal sealed partial class DecisionTypedFunctionJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketCategory))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketAnalysis))]
internal sealed partial class DecisionResultOnlyJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DecisionTypedFunctionTests.SupportTicket))]
internal sealed partial class DecisionStateOnlyJsonContext : JsonSerializerContext;

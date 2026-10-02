// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Test models and their source-generated context are co-located.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class DecisionTypedFunctionTests
{
    [Fact]
    public async Task AIFunctionFactory_UsesAnnotatedMethodAndTypedDecisionDefinition()
    {
        using SupportTicketDecisionClient client = new();
        DecisionDefinition<TicketAnalysis> definition = CreateDefinition();
        SupportTicketFunction host = new(client, definition);

        AIFunction function = AIFunctionFactory.Create(
            host.AnalyzeAsync,
            new AIFunctionFactoryOptions
            {
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

        Assert.Equal("analyze_support_ticket", function.Name);
        Assert.Equal("Annotate one support ticket.", function.Description);
        JsonElement schema = function.JsonSchema;
        JsonElement properties = schema.GetProperty("properties");
        Assert.Equal(["ticket"], properties.EnumerateObject().Select(static property => property.Name));
        Assert.Equal(
            "The ticket to classify.",
            properties.GetProperty("ticket").GetProperty("description").GetString());
        Assert.Contains("ticket", schema.GetProperty("required").EnumerateArray().Select(static value => value.GetString()));
        Assert.DoesNotContain("client", properties.EnumerateObject().Select(static property => property.Name));
        Assert.DoesNotContain("definition", properties.EnumerateObject().Select(static property => property.Name));
        Assert.DoesNotContain("cancellationToken", properties.EnumerateObject().Select(static property => property.Name));

        JsonElement output = Assert.IsType<JsonElement>(
            await function.InvokeAsync(new AIFunctionArguments
            {
                ["ticket"] = JsonSerializer.SerializeToElement(
                    new SupportTicket("The product is unavailable and I need help."),
                    DecisionTypedFunctionJsonContext.Default.SupportTicket),
            }));

        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("refundRequestProbability").GetDouble());
        Assert.Equal(1, client.CallCount);
        Assert.Equal(
            "The product is unavailable and I need help.",
            client.LastRequest!.State.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AIFunctionFactory_CanReturnBusinessResultAndRecordEvidencePerInvocation()
    {
        using SupportTicketDecisionClient client = new();
        DecisionDefinition<TicketAnalysis> definition = CreateDefinition();
        List<DecisionResponse> evidence = [];
        SupportTicketFunction host = new(client, definition, evidence.Add);

        AIFunction function = AIFunctionFactory.Create(
            host.AnalyzeWithMetadataAsync,
            new AIFunctionFactoryOptions
            {
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

        JsonElement output = Assert.IsType<JsonElement>(
            await function.InvokeAsync(new AIFunctionArguments
            {
                ["ticket"] = JsonSerializer.SerializeToElement(
                    new SupportTicket("The product is unavailable and I need help."),
                    DecisionTypedFunctionJsonContext.Default.SupportTicket),
            }));

        Assert.Equal("Technical", output.GetProperty("result").GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("result").GetProperty("refundRequestProbability").GetDouble());
        Assert.Equal("test-provider", output.GetProperty("provenance").GetProperty("providerName").GetString());
        Assert.Equal("test-model", output.GetProperty("provenance").GetProperty("modelId").GetString());
        Assert.Equal(7, output.GetProperty("usage").GetProperty("inputTokenCount").GetInt32());
        Assert.Equal(2, output.GetProperty("usage").GetProperty("outputTokenCount").GetInt32());
        Assert.DoesNotContain("provider-evidence", output.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("retained", output.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("The product is unavailable", output.GetRawText(), StringComparison.Ordinal);
        Assert.Single(evidence);
        Assert.Equal("provider-evidence", evidence[0].RawRepresentation);
        Assert.Equal("retained", evidence[0].AdditionalProperties!["evidence"]);
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
        SupportTicketFunction cancelledHost = new(cancelledClient, CreateDefinition());
        AIFunction cancelledFunction = CreateFunction(cancelledHost.AnalyzeAsync);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledFunction.InvokeAsync(
                CreateArguments(),
                cancellation.Token).AsTask());
        Assert.Equal(1, cancelledClient.CallCount);

        DecisionClientException providerFailure = new("provider failed", isTransient: true);
        using SupportTicketDecisionClient failedClient = new() { Failure = providerFailure };
        AIFunction failedFunction = CreateFunction(new SupportTicketFunction(failedClient, CreateDefinition()).AnalyzeAsync);

        DecisionClientException actualProviderFailure = await Assert.ThrowsAsync<DecisionClientException>(
            () => failedFunction.InvokeAsync(CreateArguments()).AsTask());
        Assert.Same(providerFailure, actualProviderFailure);

        InvalidOperationException hostFailure = new("host evidence failed");
        using SupportTicketDecisionClient hostClient = new();
        SupportTicketFunction failingHost = new(hostClient, CreateDefinition(), _ => throw hostFailure);
        AIFunction hostFunction = CreateFunction(failingHost.AnalyzeWithMetadataAsync);

        InvalidOperationException actualHostFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostFunction.InvokeAsync(CreateArguments()).AsTask());
        Assert.Same(hostFailure, actualHostFailure);
    }

    [Theory]
    [InlineData(ResponseMismatch.State)]
    [InlineData(ResponseMismatch.Question)]
    [InlineData(ResponseMismatch.Candidate)]
    public async Task AIFunctionFactory_RejectsResponseCorrelationBeforeResultMapping(ResponseMismatch mismatch)
    {
        int mapperCalls = 0;
        using SupportTicketDecisionClient client = new() { Mismatch = mismatch };
        DecisionDefinition<TicketAnalysis> definition = CreateDefinition();
        SupportTicketFunction host = new(client, definition, _ => mapperCalls++);
        AIFunction function = CreateFunction(host.AnalyzeWithMetadataAsync);

        await Assert.ThrowsAsync<DecisionProtocolException>(
            () => function.InvokeAsync(CreateArguments()).AsTask());

        Assert.Equal(1, client.CallCount);
        Assert.Equal(0, mapperCalls);
    }

    [Fact]
    public async Task AIFunctionFactory_RejectsInvalidSelectedCandidateDuringTypedBinding()
    {
        using SupportTicketDecisionClient client = new() { InvalidSelectedCandidate = true };
        AIFunction function = CreateFunction(new SupportTicketFunction(client, CreateDefinition()).AnalyzeAsync);

        await Assert.ThrowsAsync<DecisionProtocolException>(
            () => function.InvokeAsync(CreateArguments()).AsTask());

        Assert.Equal(1, client.CallCount);
    }

    private static AIFunction CreateFunction(Delegate method) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = "analyze_support_ticket",
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

    private static AIFunctionArguments CreateArguments() =>
        new()
        {
            ["ticket"] = JsonSerializer.SerializeToElement(
                new SupportTicket("Please review this account issue."),
                DecisionTypedFunctionJsonContext.Default.SupportTicket),
        };

    private static DecisionDefinition<TicketAnalysis> CreateDefinition() =>
        DecisionDefinition<TicketAnalysis>.Create(
            DecisionTypedFunctionJsonContext.Default.TicketAnalysis,
            definition =>
            {
                definition.Choice(result => result.Category);
                definition.BinaryProbability(result => result.RefundRequestProbability);
            });

    private sealed class SupportTicketFunction
    {
        private readonly IDecisionClient _client;
        private readonly DecisionDefinition<TicketAnalysis> _definition;
        private readonly Action<DecisionResponse>? _recordEvidence;

        public SupportTicketFunction(
            IDecisionClient client,
            DecisionDefinition<TicketAnalysis> definition,
            Action<DecisionResponse>? recordEvidence = null)
        {
            _client = client;
            _definition = definition;
            _recordEvidence = recordEvidence;
        }

        [DisplayName("analyze_support_ticket")]
        [Description("Annotate one support ticket.")]
        public async Task<TicketAnalysis> AnalyzeAsync(
            [Description("The ticket to classify.")] SupportTicket ticket,
            CancellationToken cancellationToken)
        {
            DecisionResponse<TicketAnalysis> response = await _client.GetResponseAsync(
                ticket,
                DecisionTypedFunctionJsonContext.Default.SupportTicket,
                _definition,
                cancellationToken: cancellationToken);
            _recordEvidence?.Invoke(response.Evidence);
            return response.Result;
        }

        [DisplayName("analyze_support_ticket_with_metadata")]
        [Description("Annotate one support ticket and return selected decision metadata.")]
        public async Task<MetadataOnlyToolResult> AnalyzeWithMetadataAsync(
            [Description("The ticket to classify.")] SupportTicket ticket,
            CancellationToken cancellationToken)
        {
            DecisionResponse<TicketAnalysis> response = await _client.GetResponseAsync(
                ticket,
                DecisionTypedFunctionJsonContext.Default.SupportTicket,
                _definition,
                cancellationToken: cancellationToken);
            _recordEvidence?.Invoke(response.Evidence);
            return new(
                response.Result,
                response.Evidence.Provenance,
                response.Evidence.Usage);
        }
    }

    private sealed class SupportTicketDecisionClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public DecisionClientException? Failure { get; init; }

        public ResponseMismatch Mismatch { get; init; }

        public bool InvalidSelectedCandidate { get; init; }

        public DecisionRequest? LastRequest { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;

            if (Failure is not null)
            {
                return Task.FromException<DecisionResponse>(Failure);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            DecisionQuestion[] questions = request.Questions.ToArray();
            JsonElement state = request.State;
            if (Mismatch == ResponseMismatch.State)
            {
                state = JsonSerializer.SerializeToElement(
                    new SupportTicket("different state"),
                    DecisionTypedFunctionJsonContext.Default.SupportTicket);
            }

            if (Mismatch == ResponseMismatch.Question)
            {
                ChoiceDecisionQuestion choice = Assert.IsType<ChoiceDecisionQuestion>(questions[0]);
                questions[0] = new ChoiceDecisionQuestion("different-question", choice.Instructions, choice.Candidates);
            }
            else if (Mismatch == ResponseMismatch.Candidate)
            {
                ChoiceDecisionQuestion choice = Assert.IsType<ChoiceDecisionQuestion>(questions[0]);
                questions[0] = new ChoiceDecisionQuestion(
                    choice.Id,
                    choice.Instructions,
                    [new DecisionCandidate("unrelated", "Unrelated candidate.")]);
            }

            DecisionRequest responseRequest = new(state, questions);
            ChoiceDecisionQuestion responseChoice = Assert.IsType<ChoiceDecisionQuestion>(responseRequest.Questions[0]);
            BinaryDecisionQuestion responseBinary = Assert.IsType<BinaryDecisionQuestion>(responseRequest.Questions[1]);
            string selectedCandidateId = InvalidSelectedCandidate
                ? "unknown"
                : responseChoice.Candidates[Math.Min(1, responseChoice.Candidates.Count - 1)].Id;
            DecisionProbability[] probabilities = responseChoice.Candidates
                .Select((candidate, index) => new DecisionProbability(candidate.Id, GetProbability(
                    responseChoice.Candidates.Count,
                    index)))
                .ToArray();

            return Task.FromResult(
                new DecisionResponse(
                    responseRequest,
                    [
                        new ChoiceDecisionAnswer(responseChoice.Id, selectedCandidateId, probabilities),
                        new BinaryDecisionAnswer(responseBinary.Id, 0.25),
                    ],
                    new DecisionProvenance(providerName: "test-provider", modelId: "test-model"),
                    new UsageDetails { InputTokenCount = 7, OutputTokenCount = 2 },
                    rawRepresentation: "provider-evidence",
                    additionalProperties: new Dictionary<string, object?> { ["evidence"] = "retained" }));
        }

        private static double GetProbability(int candidateCount, int index) =>
            candidateCount == 1
                ? 1
                : index switch
                {
                    0 => 0.2,
                    1 => 0.5,
                    _ => 0.3,
                };

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    public enum ResponseMismatch
    {
        None,
        State,
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
        DecisionProvenance? Provenance,
        UsageDetails? Usage);
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DecisionTypedFunctionTests.SupportTicket))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketCategory))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketAnalysis))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.MetadataOnlyToolResult))]
[JsonSerializable(typeof(DecisionProvenance))]
[JsonSerializable(typeof(UsageDetails))]
internal sealed partial class DecisionTypedFunctionJsonContext : JsonSerializerContext;

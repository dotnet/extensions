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
    public async Task AIFunctionFactory_RoundTripsSourceGeneratedTypedBusinessResultAndEvidence()
    {
        using SupportTicketDecisionClient client = new();
        DecisionDefinition<TicketAnalysis> definition = CreateDefinition();
        DecisionResponse<TicketAnalysis>? observed = null;

        AIFunction function = AIFunctionFactory.Create(
            async (SupportTicket ticket, CancellationToken cancellationToken) =>
            {
                observed = await client.GetResponseAsync(
                    ticket,
                    definition,
                    cancellationToken: cancellationToken);
                return observed.Result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "analyze_support_ticket",
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

        JsonElement input = JsonSerializer.SerializeToElement(
            new SupportTicket("The product is unavailable and I need help."),
            DecisionTypedFunctionJsonContext.Default.SupportTicket);
        JsonElement output = Assert.IsType<JsonElement>(
            await function.InvokeAsync(new AIFunctionArguments { ["ticket"] = input }));

        Assert.Equal("analyze_support_ticket", function.Name);
        Assert.Equal("Technical", output.GetProperty("category").GetString());
        Assert.Equal(0.25, output.GetProperty("refundRequestProbability").GetDouble());

        Assert.NotNull(observed);
        Assert.Equal(new TicketAnalysis(TicketCategory.Technical, 0.25), observed.Result);
        Assert.Equal(1, client.CallCount);
        Assert.Equal(
            "The product is unavailable and I need help.",
            client.LastRequest!.State.GetProperty("message").GetString());

        ChoiceDecisionQuestion category = Assert.IsType<ChoiceDecisionQuestion>(
            client.LastRequest.Questions[0]);
        Assert.Equal(["Billing", "Technical", "Account"], category.Candidates.Select(static candidate => candidate.Id));
        Assert.Equal(
            [
                "Payment, invoice, or charge concerns.",
                "Product defects, errors, outages, or troubleshooting.",
                "Sign-in, profile, or account-access concerns.",
            ],
            category.Candidates.Select(static candidate => candidate.Description));

        Assert.Equal(
            new Dictionary<TicketCategory, double>
            {
                [TicketCategory.Billing] = 0.2,
                [TicketCategory.Technical] = 0.5,
                [TicketCategory.Account] = 0.3,
            },
            observed.GetDistribution(result => result.Category));
        Assert.Equal(
            0.25,
            ((BinaryDecisionAnswer)observed.Evidence.GetAnswer("refundRequestProbability")).TrueProbability);
        Assert.Equal("test-provider", observed.Evidence.Provenance!.ProviderName);
        Assert.Equal("provider-evidence", observed.Evidence.RawRepresentation);
        Assert.Equal("retained", observed.Evidence.AdditionalProperties!["evidence"]);
    }

    [Fact]
    public async Task AIFunctionFactory_RejectsWrongDomainResponseWithoutToolResult()
    {
        using SupportTicketDecisionClient client = new() { ReturnWrongDomain = true };
        DecisionResponse<TicketAnalysis>? observed = null;

        AIFunction function = AIFunctionFactory.Create(
            async (SupportTicket ticket, CancellationToken cancellationToken) =>
            {
                observed = await client.GetResponseAsync(
                    ticket,
                    CreateDefinition(),
                    cancellationToken: cancellationToken);
                return observed.Result;
            },
            new AIFunctionFactoryOptions
            {
                Name = "analyze_support_ticket",
                SerializerOptions = DecisionTypedFunctionJsonContext.Default.Options,
            });

        JsonElement input = JsonSerializer.SerializeToElement(
            new SupportTicket("Please review this account issue."),
            DecisionTypedFunctionJsonContext.Default.SupportTicket);

        DecisionProtocolException exception = await Assert.ThrowsAsync<DecisionProtocolException>(
            () => function.InvokeAsync(new AIFunctionArguments { ["ticket"] = input }).AsTask());

        Assert.NotNull(exception);
        Assert.Equal(1, client.CallCount);
        Assert.Null(observed);
    }

    private static DecisionDefinition<TicketAnalysis> CreateDefinition() =>
        DecisionDefinition<TicketAnalysis>.Create(
            DecisionTypedFunctionJsonContext.Default.TicketAnalysis,
            definition =>
            {
                definition.Choice(result => result.Category);
                definition.BinaryProbability(result => result.RefundRequestProbability);
            });

    private sealed class SupportTicketDecisionClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public bool ReturnWrongDomain { get; init; }

        public DecisionRequest? LastRequest { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;

            DecisionQuestion[] questions = request.Questions.ToArray();
            if (ReturnWrongDomain)
            {
                ChoiceDecisionQuestion choice = Assert.IsType<ChoiceDecisionQuestion>(questions[0]);
                questions[0] = new ChoiceDecisionQuestion(
                    choice.Id,
                    choice.Instructions,
                    [new DecisionCandidate("Other", "An unrelated domain.")]);
            }

            DecisionRequest responseRequest = new(request.State, questions);
            ChoiceDecisionQuestion responseChoice = Assert.IsType<ChoiceDecisionQuestion>(responseRequest.Questions[0]);
            BinaryDecisionQuestion responseBinary = Assert.IsType<BinaryDecisionQuestion>(responseRequest.Questions[1]);
            string selectedCandidateId = responseChoice.Candidates.Count > 1
                ? responseChoice.Candidates[1].Id
                : responseChoice.Candidates[0].Id;
            DecisionProbability[] probabilities = responseChoice.Candidates.Select((candidate, index) =>
            {
                double probability;
                if (responseChoice.Candidates.Count == 1)
                {
                    probability = 1;
                }
                else
                {
                    probability = index switch
                    {
                        0 => 0.2,
                        1 => 0.5,
                        _ => 0.3,
                    };
                }

                return new DecisionProbability(candidate.Id, probability);
            }).ToArray();

            return Task.FromResult(
                new DecisionResponse(
                    responseRequest,
                    [
                        new ChoiceDecisionAnswer(
                            responseChoice.Id,
                            selectedCandidateId,
                            probabilities),
                        new BinaryDecisionAnswer(responseBinary.Id, 0.25),
                    ],
                    new DecisionProvenance(providerName: "test-provider", modelId: "test-model"),
                    rawRepresentation: "provider-evidence",
                    additionalProperties: new Dictionary<string, object?> { ["evidence"] = "retained" }));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
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
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DecisionTypedFunctionTests.SupportTicket))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketCategory))]
[JsonSerializable(typeof(DecisionTypedFunctionTests.TicketAnalysis))]
internal sealed partial class DecisionTypedFunctionJsonContext : JsonSerializerContext;

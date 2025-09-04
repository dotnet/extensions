// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Evaluation;

using System;
using Evaluation::Microsoft.Extensions.AI.Evaluation.Utilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

public class ModelInfoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("openai")]
    public void GetModelProvider_NoProviderUriAndModelSpecified_ReturnsProviderNameOnly(string? providerName)
    {
        var metadata = new ChatClientMetadata(providerName, providerUri: null);

        string? result = ModelInfo.GetModelProvider(model: null, metadata);

        Assert.Equal(providerName, result);
    }

    [Theory]
    [InlineData(null, "https://localhost:11434", " (local)")]
    [InlineData(null, "https://test.services.ai.azure.com/", " (azure.ai.foundry)")]
    [InlineData(null, "https://myapp.openai.azure.com/v1/chat", " (azure.openai)")]
    [InlineData(null, "https://myapp.ml.azure.com/", " (azure.ml)")]
    [InlineData(null, "https://models.inference.ai.azure.com/v1", " (github.models)")]
    [InlineData(null, "https://models.github.ai", " (github.models)")]
    [InlineData("", "https://custom.azure.com", " (azure)")]
    [InlineData(" ", "https://models.github.com/openai", "  (github)")]
    [InlineData("\t", "https://services.microsoft.com/models", "\t (microsoft)")]
    [InlineData(null, "https://localhost.com:11434/models", null)]
    [InlineData(null, "https://github.com/models", null)]
    [InlineData("", "https://azure.com/models", "")]
    [InlineData("\t", "https://microsoft.com/models", "\t")]
    [InlineData(null, "https://example.com/models", null)]
    public void GetModelProvider_NoProviderNameAndModelSpecified_ReturnsHostMonikerOnly(
        string? providerName,
        string providerUri,
        string? expected)
    {
        Uri? uri = providerUri != null ? new Uri(providerUri) : null;
        var metadata = new ChatClientMetadata(providerName, providerUri: uri);

        string? result = ModelInfo.GetModelProvider(model: null, metadata);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("\t", null)]
    [InlineData("unknown", null)]
    [InlineData("azure.ai.foundry.evaluation", "azure.ai.foundry (azure.ai.foundry)")]
    [InlineData(" azure.ai.foundry.evaluation", null)]
    [InlineData("azure.ai.foundry.evaluation\t", null)]
    [InlineData("azure.ai.foundry . evaluation", null)]
    [InlineData("(azure.ai.foundry.evaluation)", null)]
    [InlineData("azure.AI.FOUNDRY.evaluation", null)]
    [InlineData("ai.foundry.evaluation", null)]
    public void GetModelProvider_NoMetadataSpecified_ReturnsExpectedFormat(
        string? model,
        string? expected)
    {
        string? result = ModelInfo.GetModelProvider(model, metadata: null);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("azure.ai.foundry.evaluation", null, null, "azure.ai.foundry (azure.ai.foundry)")]
    [InlineData(" azure.ai.foundry.evaluation", null, null, null)]
    [InlineData("azure.ai.foundry.evaluation\t", null, null, null)]
    [InlineData("(azure.ai.foundry.evaluation)", null, null, null)]
    [InlineData("azure.ai.foundry.evaluation", null, "https://myapp.openai.azure.com/", "azure.ai.foundry (azure.ai.foundry)")]
    [InlineData("azure.ai.foundry.evaluation", "openai", null, "azure.ai.foundry (azure.ai.foundry)")]
    [InlineData("azure.ai.foundry.evaluation", "azure", "https://services.ai.azure.com/", "azure.ai.foundry (azure.ai.foundry)")]
    [InlineData("azure.AI.FOUNDRY.evaluation", "custom", null, "custom")]
    [InlineData("ai.foundry.evaluation", "custom", "https://myapp.openai.azure.com/", "custom (azure.openai)")]
    [InlineData(null, "custom", "https://services.ai.azure.com/", "custom (azure.ai.foundry)")]
    [InlineData("", null, "https://myapp.openai.azure.com/", " (azure.openai)")]
    [InlineData(" ", null, "https://myapp.openai.azure.com/v1", " (azure.openai)")]
    [InlineData("\t", null, "https://myapp.OpenAI.Azure.com/v1/chat", " (azure.openai)")]
    [InlineData("unknown", null, "https://myapp.OpenAI.Azure.com/v1/chat", " (azure.openai)")]
    public void GetModelProvider_ModelSpecified_ReturnsExpectedFormat(
        string? model,
        string? providerName,
        string? providerUri,
        string? expected)
    {
        Uri? uri = providerUri != null ? new Uri(providerUri) : null;
        var metadata = new ChatClientMetadata(providerName, providerUri: uri, defaultModelId: "ignored");

        string? result = ModelInfo.GetModelProvider(model, metadata);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("llama", "https://localhost:11434", "llama (local)")]
    [InlineData("llama", "https://LocalHost", "llama (local)")]
    [InlineData("llama", "https://localhost:1234/models/llama", "llama (local)")]
    [InlineData("openai", "https://services.ai.azure.com/", "openai (azure.ai.foundry)")]
    [InlineData("azure", "https://test.services.ai.azure.com/endpoint", "azure (azure.ai.foundry)")]
    [InlineData("openai", "https://myapp.openai.azure.com/", "openai (azure.openai)")]
    [InlineData("azure", "https://test.openai.azure.com/v1/chat", "azure (azure.openai)")]
    [InlineData("ml", "https://myapp.ml.azure.com/", "ml (azure.ml)")]
    [InlineData("azure", "https://myapp.inference.ml.azure.com/v1", "azure (azure.ml)")]
    [InlineData("github", "https://models.github.ai/", "github (github.models)")]
    [InlineData("openai", "https://models.github.ai/v1", "openai (github.models)")]
    [InlineData("github", "https://models.inference.ai.azure.com/", "github (github.models)")]
    [InlineData("openai", "https://models.inference.ai.azure.com/v1", "openai (github.models)")]
    [InlineData("custom", "https://test.azure.com/", "custom (azure)")]
    [InlineData("provider", "https://api.github.com/", "provider (github)")]
    [InlineData("service", "https://api.microsoft.com/", "service (microsoft)")]
    [InlineData("openai", "https://api.openai.com/", "openai")]
    [InlineData("anthropic.claude", "https://api.anthropic.com/", "anthropic.claude")]
    [InlineData("custom", "https://example.com/", "custom")]
    [InlineData("custom", "https://localhost.com:11434/", "custom")]
    [InlineData("custom", "https://host:11434", "custom")]
    [InlineData("custom", "https://127.0.0.0:11434", "custom")]
    [InlineData("provider", "https://unknown-host.com/", "provider")]
    [InlineData("OPENAI provider", "https://SERVICES.AI.AZURE.COM/", "OPENAI provider (azure.ai.foundry)")]
    [InlineData("Azure-model-provider", "https://Test.OpenAI.Azure.Com/", "Azure-model-provider (azure.openai)")]
    public void GetModelProvider_ReturnsProviderWithHostMoniker(
        string providerName,
        string providerUri,
        string expected)
    {
        var metadata = new ChatClientMetadata(providerName, new Uri(providerUri));

        string? result = ModelInfo.GetModelProvider(model: "some-model", metadata);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://myapp.openai.azure.services.ai.azure.com/", "azure.ai.foundry")]
    [InlineData("https://myapp.services.ai.azure.openai.azure.com/", "azure.ai.foundry")]
    [InlineData("https://myapp.microsoft.services.ai.azure.com/", "azure.ai.foundry")]
    [InlineData("https://inference.openai.azure.ml.azure.com/", "azure.openai")]
    [InlineData("https://inference.ml.azure.openai.azure.com/", "azure.openai")]
    [InlineData("https://myapp.azure.models.github.ai/", "github.models")]
    [InlineData("https://test.azure.microsoft.com/", "azure")]
    [InlineData("https://test.microsoft.github.com/", "github")]
    public void GetModelProvider_MultipleHostPatternMatches_ReturnsExpectedHostMoniker(
        string providerUri,
        string expectedHostMoniker)
    {
        var metadata = new ChatClientMetadata(providerName: "some-provider", new Uri(providerUri));

        string? result = ModelInfo.GetModelProvider(model: "some-model", metadata);

        Assert.Equal($"some-provider ({expectedHostMoniker})", result);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("", false, false)]
    [InlineData(" ", false, false)]
    [InlineData("\t", false, false)]
    [InlineData("llama (local)", false, true)]
    [InlineData("openai (azure.ai.foundry)", true, false)]
    [InlineData("azure (azure.openai)", true, false)]
    [InlineData("azure (azure.ml)", true, false)]
    [InlineData("github (github.models)", true, false)]
    [InlineData("(azure.ai.foundry)", true, false)]
    [InlineData("provider (azure)", true, false)]
    [InlineData("service (github)", true, false)]
    [InlineData("custom (microsoft)", true, false)]
    [InlineData(" (azure.ai.foundry)", true, false)]
    [InlineData("   (azure.openai)", true, false)]
    [InlineData("\t(github.models)", true, false)]
    [InlineData("\t (local)", false, true)]
    [InlineData("(azure) ", false, false)]
    [InlineData("(github)  ", false, false)]
    [InlineData("(microsoft)\t", false, false)]
    [InlineData(" (local)\t", false, false)]
    [InlineData("( azure.ml)", false, false)]
    [InlineData("(local\t)", false, false)]
    [InlineData("(azure .ml)", false, false)]
    [InlineData("(azure. ml)", false, false)]
    [InlineData("(LOCAL)", false, false)]
    [InlineData("ml [azure.ml]", false, false)]
    [InlineData("{azure.ml}", false, false)]
    [InlineData("openai (AZURE.OPENAI)", false, false)]
    [InlineData("prefix provider (azure.openai)", true, false)]
    [InlineData("local", false, false)]
    [InlineData("openai", false, false)]
    [InlineData("azure.ai.foundry", false, false)]
    [InlineData("azure.openai", false, false)]
    [InlineData("azure.ml", false, false)]
    [InlineData("github.models", false, false)]
    [InlineData("azure", false, false)]
    [InlineData("github", false, false)]
    [InlineData("microsoft", false, false)]
    [InlineData("(custom-host)", false, false)]
    [InlineData("provider (unknown)", false, false)]
    [InlineData("provider (", false, false)]
    [InlineData("provider )", false, false)]
    [InlineData("provider (azure.ai.foundry) extra", false, false)]
    [InlineData("(microsoft)\tcustom (other)", false, false)]
    [InlineData("provider (azure.ai.foundry", false, false)]
    [InlineData("provider azure.ai.foundry)", false, false)]
    public void ModelHostMonikerClassificationWorks(
        string? modelProvider,
        bool expectedIsModelHostWellKnown,
        bool expectedIsModelHostedLocally)
    {
        bool isModelHostWellKnown = ModelInfo.IsModelHostWellKnown(modelProvider);
        Assert.Equal(expectedIsModelHostWellKnown, isModelHostWellKnown);

        bool isModelHostedLocally = ModelInfo.IsModelHostedLocally(modelProvider);
        Assert.Equal(expectedIsModelHostedLocally, isModelHostedLocally);
    }
}

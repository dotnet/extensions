// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class ModelInfo
{
    internal static class KnownModels
    {
        internal const string AzureAIFoundryEvaluation = "azure.ai.foundry.evaluation";
    }

    internal static class KnownModelProviders
    {
        internal const string AzureAIFoundry = "azure.ai.foundry";
    }

    internal static class KnownModelHostMonikers
    {
        internal const string LocalMachine = "local";
        internal const string AzureAIFoundry = "azure.ai.foundry";
        internal const string AzureOpenAI = "azure.openai";
        internal const string AzureML = "azure.ml";
        internal const string GitHubModels = "github.models";
        internal const string Azure = "azure";
        internal const string GitHub = "github";
        internal const string Microsoft = "microsoft";
    }

    private const string LocalMachineHost = "localhost";

    private static Regex LocalMachineHostMonikerRegex { get; } =
        new Regex($"\\({Regex.Escape(KnownModelHostMonikers.LocalMachine)}\\)$");

    // NOTE: Order more specific patterns first.
    private static (string hostPattern, string hostMoniker)[] KnownHostMonikers { get; } =
        [
            ("services.ai.azure.", KnownModelHostMonikers.AzureAIFoundry),
            ("openai.azure.", KnownModelHostMonikers.AzureOpenAI),
            ("ml.azure.", KnownModelHostMonikers.AzureML),
            ("models.github.ai", KnownModelHostMonikers.GitHubModels),
            ("models.inference.ai.azure.", KnownModelHostMonikers.GitHubModels),
            (".azure.", KnownModelHostMonikers.Azure),
            (".github.", KnownModelHostMonikers.GitHub),
            (".microsoft.", KnownModelHostMonikers.Microsoft)
        ];

    private static Regex KnownHostMonikersRegex { get; } =
        new Regex(
            $"\\((" +
                $"{Regex.Escape(KnownModelHostMonikers.AzureAIFoundry)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.AzureOpenAI)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.AzureML)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.GitHubModels)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.Azure)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.GitHub)}|" +
                $"{Regex.Escape(KnownModelHostMonikers.Microsoft)}" +
            $")\\)$");

    /// <summary>
    /// Returns a string with format <i>{provider} ({host})</i> where <i>{provider}</i> is the name of the model
    /// provider (available via <see cref="ChatClientMetadata.ProviderName"/> - for example, <i>openai</i>) and
    /// <i>{host}</i> is a moniker that identifies the hosting service (for example, <i>azure.openai</i> or
    /// <i>github.models</i>). If the hosting service is not recognized, only the name of the model provider is
    /// returned.
    /// </summary>
    /// <param name="model">
    /// The <see cref="ChatResponse.ModelId"/> that identifies the model that produced a particular response.
    /// </param>
    /// <param name="metadata">
    /// The <see cref="ChatClientMetadata"/> for the <see cref="IChatClient"/> that was used to communicate with the
    /// model.
    /// </param>
    internal static string? GetModelProvider(string? model, ChatClientMetadata? metadata)
    {
#pragma warning disable S2219 // Runtime type checking should be simplified
        if (model is KnownModels.AzureAIFoundryEvaluation)
#pragma warning restore S2219
        {
            // We know that the model provider and the host are both Azure AI Foundry in this case.
            return $"{KnownModelProviders.AzureAIFoundry} ({KnownModelHostMonikers.AzureAIFoundry})";
        }

        if (metadata is null)
        {
            return null;
        }

        string? provider = metadata.ProviderName;
        string? host = metadata.ProviderUri?.Host;

        if (!string.IsNullOrWhiteSpace(host))
        {
            if (string.Equals(host, LocalMachineHost, StringComparison.OrdinalIgnoreCase))
            {
                return $"{provider} ({KnownModelHostMonikers.LocalMachine})";
            }

            foreach (var (hostPattern, hostMoniker) in KnownHostMonikers)
            {
#if NET
                if (host.Contains(hostPattern, StringComparison.OrdinalIgnoreCase))
#else
                if (host!.IndexOf(hostPattern, StringComparison.OrdinalIgnoreCase) >= 0)
#endif
                {
                    return $"{provider} ({hostMoniker})";
                }
            }
        }

        return provider;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified <paramref name="modelProvider"/> indicates that the model is
    /// hosted by a well-known (Microsoft-owned) service; <see langword="false"/> otherwise.
    /// </summary>
    internal static bool IsModelHostWellKnown(string? modelProvider)
        => !string.IsNullOrWhiteSpace(modelProvider) && KnownHostMonikersRegex.IsMatch(modelProvider);

    /// <summary>
    /// Returns <see langword="true"/> if the specified <paramref name="modelProvider"/> indicates that the model is
    /// hosted locally (using ollama, for example); <see langword="false"/> otherwise.
    /// </summary>
    internal static bool IsModelHostedLocally(string? modelProvider)
        => !string.IsNullOrWhiteSpace(modelProvider) && LocalMachineHostMonikerRegex.IsMatch(modelProvider);
}

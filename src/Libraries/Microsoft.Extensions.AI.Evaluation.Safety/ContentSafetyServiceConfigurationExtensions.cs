// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// Extension methods for <see cref="ContentSafetyServiceConfiguration"/>.
/// </summary>
public static class ContentSafetyServiceConfigurationExtensions
{
    /// <summary>
    /// Returns a <see cref="ChatConfiguration"/> that can be used to communicate with the Azure AI Content Safety
    /// service for performing content safety evaluations.
    /// </summary>
    /// <param name="contentSafetyServiceConfiguration">
    /// An object that specifies configuration parameters such as the Azure AI project that should be used, and the
    /// credentials that should be used, when communicating with the Azure AI Content Safety service to perform
    /// content safety evaluations.
    /// </param>
    /// <param name="originalChatConfiguration">
    /// The original <see cref="ChatConfiguration"/>, if any. If specified, the returned
    /// <see cref="ChatConfiguration"/> will be based on <paramref name="originalChatConfiguration"/>, with the
    /// <see cref="ChatConfiguration.ChatClient"/> in <paramref name="originalChatConfiguration"/> being replaced with
    /// a new <see cref="IChatClient"/> that can be used both to communicate with the AI model that
    /// <paramref name="originalChatConfiguration"/> is configured to communicate with, as well as to communicate with
    /// the Azure AI Content Safety service.
    /// </param>
    /// <returns>
    /// A <see cref="ChatConfiguration"/> that can be used to communicate with the Azure AI Content Safety service for
    /// performing content safety evaluations.
    /// </returns>
    public static ChatConfiguration ToChatConfiguration(
        this ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
        ChatConfiguration? originalChatConfiguration = null)
    {
        _ = Throw.IfNull(contentSafetyServiceConfiguration);

#pragma warning disable CA2000 // Dispose objects before they go out of scope.
        // We can't dispose newChatClient here because it is returned to the caller.

        var newChatClient =
            new ContentSafetyChatClient(
                contentSafetyServiceConfiguration,
                originalChatClient: originalChatConfiguration?.ChatClient);
#pragma warning restore CA2000

        return new ChatConfiguration(newChatClient);
    }

    /// <summary>
    /// Returns an <see cref="IChatClient"/> that can be used to communicate with the Azure AI Content Safety service
    /// for performing content safety evaluations.
    /// </summary>
    /// <param name="contentSafetyServiceConfiguration">
    /// An object that specifies configuration parameters such as the Azure AI project that should be used, and the
    /// credentials that should be used, when communicating with the Azure AI Content Safety service to perform
    /// content safety evaluations.
    /// </param>
    /// <param name="originalChatClient">
    /// The original <see cref="IChatClient"/>, if any. If specified, the returned
    /// <see cref="IChatClient"/> will be a wrapper around <paramref name="originalChatClient"/> that can be used both
    /// to communicate with the AI model that <paramref name="originalChatClient"/> is configured to communicate with,
    /// as well as to communicate with the Azure AI Content Safety service.
    /// </param>
    /// <returns>
    /// A <see cref="ChatConfiguration"/> that can be used to communicate with the Azure AI Content Safety service for
    /// performing content safety evaluations.
    /// </returns>
    public static IChatClient ToIChatClient(
        this ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
        IChatClient? originalChatClient = null)
    {
        _ = Throw.IfNull(contentSafetyServiceConfiguration);

        return new ContentSafetyChatClient(contentSafetyServiceConfiguration, originalChatClient);
    }
}

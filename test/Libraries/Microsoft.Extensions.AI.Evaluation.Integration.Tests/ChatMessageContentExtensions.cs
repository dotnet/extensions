// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.AI;

internal static class ChatMessageContentExtensions
{
    [Experimental("AIEVAL001")]
    internal static ChatMessage ToChatMessage(this ChatMessageContent content)
    {
        ChatMessage message = new()
        {
            AdditionalProperties = content.Metadata is not null ? new(content.Metadata) : null,
            AuthorName = content.AuthorName,
            RawRepresentation = content.InnerContent,
            Role = content.Role.Label is string label ? new ChatRole(label) : ChatRole.User,
        };

        foreach (var item in content.Items)
        {
            AIContent? aiContent = null;
            switch (item)
            {
                case SemanticKernel.TextContent tc:
                    aiContent = new TextContent(tc.Text);
                    break;

                case ImageContent ic:
#pragma warning disable S3358 // Ternary operators should not be nested
                    aiContent =
                        ic.DataUri is not null ? new Microsoft.Extensions.AI.DataContent(ic.DataUri, ic.MimeType) :
                        ic.Uri is not null ? new Microsoft.Extensions.AI.UriContent(ic.Uri, ic.MimeType ?? "image/*") :
                        null;
#pragma warning restore S3358
                    break;

                case AudioContent ac:
#pragma warning disable S3358 // Ternary operators should not be nested
                    aiContent =
                        ac.DataUri is not null ? new Microsoft.Extensions.AI.DataContent(ac.DataUri, ac.MimeType) :
                        ac.Uri is not null ? new Microsoft.Extensions.AI.UriContent(ac.Uri, ac.MimeType ?? "audio/*") :
                        null;
#pragma warning restore S3358
                    break;

                case BinaryContent bc:
#pragma warning disable S3358 // Ternary operators should not be nested
                    aiContent =
                        bc.DataUri is not null ? new Microsoft.Extensions.AI.DataContent(bc.DataUri, bc.MimeType) :
                        bc.Uri is not null ? new Microsoft.Extensions.AI.UriContent(bc.Uri, bc.MimeType ?? "application/octet-stream") :
                        null;
#pragma warning restore S3358
                    break;

                case SemanticKernel.FunctionCallContent fcc:
                    aiContent = new FunctionCallContent(fcc.Id ?? string.Empty, fcc.FunctionName, fcc.Arguments);
                    break;

                case SemanticKernel.FunctionResultContent frc:
                    aiContent = new FunctionResultContent(frc.CallId ?? string.Empty, frc.Result);
                    break;
            }

            if (aiContent is not null)
            {
                aiContent.RawRepresentation = item.InnerContent;
                aiContent.AdditionalProperties = item.Metadata is not null ? new(item.Metadata) : null;

                message.Contents.Add(aiContent);
            }
        }

        return message;
    }
}

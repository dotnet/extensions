// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class ChatMessageExtensions
{
    internal static bool ContainsImage(this ChatMessage message)
        => message.Contents.Any(c => c.IsImage());

    internal static bool ContainsImage(this IEnumerable<ChatMessage> conversation)
        => conversation.Any(ContainsImage);
}

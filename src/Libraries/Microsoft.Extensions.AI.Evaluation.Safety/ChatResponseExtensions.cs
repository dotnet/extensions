// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class ChatResponseExtensions
{
    internal static bool ContainsImageWithSupportedFormat(this ChatResponse response)
        => response.Messages.ContainsImageWithSupportedFormat();
}

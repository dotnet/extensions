// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OllamaSharp;

namespace Microsoft.Extensions.AI;

public class OllamaSharpChatClientIntegrationTests : OllamaChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOllamaUri() is Uri endpoint ?
            new OllamaApiClient(endpoint, "llama3.2") :
            null;
}

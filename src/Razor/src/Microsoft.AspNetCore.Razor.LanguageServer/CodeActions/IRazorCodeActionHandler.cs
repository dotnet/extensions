// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    [Parallel, Method("textDocument/codeAction", Direction.ClientToServer)]
    internal interface IRazorCodeActionHandler :
        IJsonRpcRequestHandler<RazorCodeActionParams, CommandOrCodeActionContainer>,
        IRegistration<CodeActionRegistrationOptions>,
        ICapability<CodeActionCapability>
    {
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Interfaces
{
    [Serial, Method(LanguageServerConstants.RazorSemanticTokensRangeEndpoint)]
    internal interface ISemanticTokensRangeHandler :
        IJsonRpcRequestHandler<SemanticTokensRangeParams, SemanticTokens>
    {
    }
}

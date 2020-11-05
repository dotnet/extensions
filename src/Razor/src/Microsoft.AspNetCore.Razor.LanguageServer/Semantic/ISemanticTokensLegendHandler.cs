// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    [Method(LanguageServerConstants.RazorSemanticTokensLegendEndpoint)]
    internal interface ISemanticTokensLegendHandler :
        IJsonRpcRequestHandler<SemanticTokensLegendParams, SemanticTokensLegend>
    {
    }
}

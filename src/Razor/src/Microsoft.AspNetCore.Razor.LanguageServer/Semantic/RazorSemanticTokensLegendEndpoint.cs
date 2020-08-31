// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class RazorSemanticTokensLegendEndpoint : ISemanticTokensLegendHandler
    {
        public Task<SemanticTokensLegend> Handle(SemanticTokensLegendParams request, CancellationToken cancellationToken)
        {
            return Task.FromResult(RazorSemanticTokensLegend.Instance);
        }
    }
}

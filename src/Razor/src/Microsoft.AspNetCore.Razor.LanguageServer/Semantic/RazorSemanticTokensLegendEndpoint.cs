// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Interfaces;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class RazorSemanticTokensLegendEndpoint : ISemanticTokensLegendHandler
    {
        public Task<SemanticTokensLegend> Handle(SemanticTokensLegendParams request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SemanticTokensLegend.Instance);
        }
    }
}

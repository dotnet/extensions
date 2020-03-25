// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    public class RazorSemanticTokenLegendEndpoint : ISemanticTokenLegendHandler
    {
        private SemanticTokenLegendCapability _capability;

        public Task<SemanticTokenLegendResponse> Handle(SemanticTokenLegendParams request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SemanticTokenLegend.GetResponse());
        }

        public void SetCapability(SemanticTokenLegendCapability capability)
        {
            _capability = capability;
        }
    }
}

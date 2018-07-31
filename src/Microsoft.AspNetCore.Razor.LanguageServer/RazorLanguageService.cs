// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageService : IRazorLanguageQueryHandler
    {
        public Task<RazorLanguageQueryResponse> Handle(RazorLanguageQueryParams request, CancellationToken cancellationToken)
        {
            var response = new RazorLanguageQueryResponse()
            {
                Position = request.HostDocumentPosition,
                TextDocumentUri = request.ProjectedCSharpDocumentUri,
            };

            return Task.FromResult(response);
        }
    }
}
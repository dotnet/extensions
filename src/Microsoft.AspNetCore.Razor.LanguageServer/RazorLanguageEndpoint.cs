// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageEndpoint : IRazorLanguageQueryHandler
    {
        private readonly ProjectSnapshotManagerShimAccessor _projectSnapshotManagerAccessor;
        private readonly ForegroundDispatcherShim _foregroundDispatcher;

        public RazorLanguageEndpoint(
            ProjectSnapshotManagerShimAccessor projectSnapshotManagerAccessor,
            ForegroundDispatcherShim foregroundDispatcher)
        {
            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _foregroundDispatcher = foregroundDispatcher;
        }

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
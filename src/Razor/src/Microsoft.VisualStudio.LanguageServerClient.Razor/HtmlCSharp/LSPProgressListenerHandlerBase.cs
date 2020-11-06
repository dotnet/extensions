// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal abstract class LSPProgressListenerHandlerBase<TParams, TResult>
    {
        // Roslyn sends Progress Notifications every 0.5s *only* if results have been found.
        // Consequently, at ~ time > 0.5s ~ after the last notification, we don't know whether Roslyn is
        // done searching for results, or just hasn't found any additional results yet.
        // To work around this, we wait for up to 3.5s since the last notification before timing out.
        //
        // Internal for testing
        internal virtual TimeSpan WaitForProgressNotificationTimeout { get; set; } = TimeSpan.FromSeconds(3.5);

        public async Task<TResult> HandleRequestAsync(TParams requestParams, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            // Temporary till IProgress serialization is fixed
            var token = Guid.NewGuid().ToString(); // request.PartialResultToken.Id
            return await HandleRequestAsync(requestParams, clientCapabilities, token, cancellationToken).ConfigureAwait(false);
        }

        // Internal for testing
        internal abstract Task<TResult> HandleRequestAsync(TParams request, ClientCapabilities clientCapabilities, string token, CancellationToken cancellationToken);
    }
}

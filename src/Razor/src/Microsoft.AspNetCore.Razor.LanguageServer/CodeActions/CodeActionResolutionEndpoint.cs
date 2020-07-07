// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class CodeActionResolutionEndpoint : IRazorCodeActionResolutionHandler
    {
        private readonly IReadOnlyDictionary<string, RazorCodeActionResolver> _resolvers;
        private readonly ILogger _logger;

        public CodeActionResolutionEndpoint(
            IEnumerable<RazorCodeActionResolver> resolvers,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CodeActionResolutionEndpoint>();

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            var resolverMap = new Dictionary<string, RazorCodeActionResolver>();
            foreach (var resolver in resolvers)
            {
                if (resolverMap.ContainsKey(resolver.Action))
                {
                    Debug.Fail($"Duplicate resolver action for {resolver.Action}.");
                }
                resolverMap[resolver.Action] = resolver;
            }
            _resolvers = resolverMap;
        }

        public async Task<RazorCodeActionResolutionResponse> Handle(RazorCodeActionResolutionParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _logger.LogDebug($"Resolving action {request.Action} with data {request.Data}.");

            if (!_resolvers.TryGetValue(request.Action, out var resolver))
            {
                Debug.Fail($"No resolver registered for {request.Action}.");
                return new RazorCodeActionResolutionResponse();
            }

            var edit = await resolver.ResolveAsync(request.Data, cancellationToken).ConfigureAwait(false);
            return new RazorCodeActionResolutionResponse() { Edit = edit };
        }
    }
}

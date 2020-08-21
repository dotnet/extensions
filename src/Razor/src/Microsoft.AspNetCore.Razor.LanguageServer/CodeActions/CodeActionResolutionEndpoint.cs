// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class CodeActionResolutionEndpoint : ICodeActionResolveHandler
    {
        private static readonly string CodeActionsResolveProviderCapability = "codeActionsResolveProvider";

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

            if (resolvers is null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            _logger = loggerFactory.CreateLogger<CodeActionResolutionEndpoint>();

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

        // Register VS LSP code action resolution server capability
        public RegistrationExtensionResult GetRegistration() => new RegistrationExtensionResult(CodeActionsResolveProviderCapability, true);

        public async Task<RazorCodeAction> Handle(RazorCodeAction request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!(request.Data is JObject paramsObj))
            {
                Debug.Fail($"Invalid CodeAction Received {request.Title}.");
                return request;
            }

            var resolutionParams = paramsObj.ToObject<RazorCodeActionResolutionParams>();
            request.Edit = await GetWorkspaceEditAsync(resolutionParams, cancellationToken).ConfigureAwait(false);
            return request;
        }

        // Internal for testing
        internal async Task<WorkspaceEdit> GetWorkspaceEditAsync(RazorCodeActionResolutionParams resolutionParams, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Resolving workspace edit for action `{resolutionParams.Action}`.");

            if (!_resolvers.TryGetValue(resolutionParams.Action, out var resolver))
            {
                Debug.Fail($"No resolver registered for {resolutionParams.Action}.");
                return default;
            }

            return await resolver.ResolveAsync(resolutionParams.Data as JObject, cancellationToken).ConfigureAwait(false);
        }
    }
}

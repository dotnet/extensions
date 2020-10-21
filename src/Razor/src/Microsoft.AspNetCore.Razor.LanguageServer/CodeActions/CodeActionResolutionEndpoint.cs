// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class CodeActionResolutionEndpoint : IRazorCodeActionResolveHandler
    {
        private static readonly string CodeActionsResolveProviderCapability = "codeActionsResolveProvider";

        private readonly IReadOnlyDictionary<string, RazorCodeActionResolver> _razorCodeActionResolvers;
        private readonly IReadOnlyDictionary<string, CSharpCodeActionResolver> _csharpCodeActionResolvers;
        private readonly ILogger _logger;

        public CodeActionResolutionEndpoint(
            IEnumerable<RazorCodeActionResolver> razorCodeActionResolvers,
            IEnumerable<CSharpCodeActionResolver> csharpCodeActionResolvers,
            ILoggerFactory loggerFactory)
        {
            if (razorCodeActionResolvers is null)
            {
                throw new ArgumentNullException(nameof(razorCodeActionResolvers));
            }

            if (csharpCodeActionResolvers is null)
            {
                throw new ArgumentNullException(nameof(csharpCodeActionResolvers));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CodeActionResolutionEndpoint>();

            _razorCodeActionResolvers = CreateResolverMap(razorCodeActionResolvers);;
            _csharpCodeActionResolvers = CreateResolverMap(csharpCodeActionResolvers);
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
                Debug.Fail($"Invalid CodeAction Received '{request.Title}'.");
                return request;
            }

            var resolutionParams = paramsObj.ToObject<RazorCodeActionResolutionParams>();

            _logger.LogInformation($"Resolving workspace edit for action {GetCodeActionId(resolutionParams)}.");

            switch (resolutionParams.Language)
            {
                case LanguageServerConstants.CodeActions.Languages.Razor:
                    return await ResolveRazorCodeActionAsync(
                        request,
                        resolutionParams,
                        cancellationToken).ConfigureAwait(false);
                case LanguageServerConstants.CodeActions.Languages.CSharp:
                    return await ResolveCSharpCodeActionAsync(
                        request,
                        resolutionParams,
                        cancellationToken);
                default:
                    Debug.Fail($"Invalid CodeAction.Data.Language. Received {GetCodeActionId(resolutionParams)}.");
                    return request;
            }
        }

        // Internal for testing
        internal async Task<RazorCodeAction> ResolveRazorCodeActionAsync(
            RazorCodeAction codeAction,
            RazorCodeActionResolutionParams resolutionParams,
            CancellationToken cancellationToken)
        {
            if (!_razorCodeActionResolvers.TryGetValue(resolutionParams.Action, out var resolver))
            {
                Debug.Fail($"No resolver registered for {GetCodeActionId(resolutionParams)}.");
                return codeAction;
            }

            codeAction.Edit = await resolver.ResolveAsync(resolutionParams.Data as JObject, cancellationToken).ConfigureAwait(false);
            return codeAction;
        }

        // Internal for testing
        internal async Task<RazorCodeAction> ResolveCSharpCodeActionAsync(
            RazorCodeAction codeAction,
            RazorCodeActionResolutionParams resolutionParams,
            CancellationToken cancellationToken)
        {
            if (!(resolutionParams.Data is JObject csharpParamsObj))
            {
                Debug.Fail($"Invalid CSharp CodeAction Received.");
                return codeAction;
            }

            var csharpParams = csharpParamsObj.ToObject<CSharpCodeActionParams>();
            codeAction.Data = csharpParams.Data;

            if (!_csharpCodeActionResolvers.TryGetValue(resolutionParams.Action, out var resolver))
            {
                Debug.Fail($"No resolver registered for {GetCodeActionId(resolutionParams)}.");
                return codeAction;
            }

            var resolvedCodeAction = await resolver.ResolveAsync(csharpParams, codeAction, cancellationToken);
            return resolvedCodeAction;
        }

        private static Dictionary<string, T> CreateResolverMap<T>(IEnumerable<T> codeActionResolvers)
            where T : BaseCodeActionResolver
        {
            var resolverMap = new Dictionary<string, T>();
            foreach (var resolver in codeActionResolvers)
            {
                if (resolverMap.ContainsKey(resolver.Action))
                {
                    Debug.Fail($"Duplicate resolver action for {resolver.Action} of type {typeof(T).ToString()}.");
                }
                resolverMap[resolver.Action] = resolver;
            }

            return resolverMap;
        }

        private static string GetCodeActionId(RazorCodeActionResolutionParams resolutionParams) =>
            $"`{resolutionParams.Language}.{resolutionParams.Action}`";
    }
}

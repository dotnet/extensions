// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal abstract class CSharpCodeActionResolver : BaseCodeActionResolver
    {
        protected readonly IClientLanguageServer _languageServer;

        public CSharpCodeActionResolver(IClientLanguageServer languageServer)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            _languageServer = languageServer;
        }

        public abstract Task<CodeAction> ResolveAsync(
            CSharpCodeActionParams csharpParams,
            CodeAction codeAction,
            CancellationToken cancellationToken);

        protected async Task<CodeAction> ResolveCodeActionWithServerAsync(CodeAction codeAction, CancellationToken cancellationToken)
        {
            var response = _languageServer.SendRequest(LanguageServerConstants.RazorResolveCodeActionsEndpoint, codeAction);
            var resolvedCodeAction = await response.Returning<CodeAction>(cancellationToken);

            return resolvedCodeAction;
        }
    }
}

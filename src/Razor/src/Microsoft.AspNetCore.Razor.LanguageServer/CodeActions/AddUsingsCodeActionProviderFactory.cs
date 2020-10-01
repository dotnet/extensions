// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal static class AddUsingsCodeActionProviderFactory
    {
        // Internal for testing
        internal static string GetNamespaceFromFQN(string fullyQualifiedName)
        {
            if (!DefaultRazorTagHelperBinderPhase.ComponentDirectiveVisitor.TrySplitNamespaceAndType(
                    fullyQualifiedName,
                    out var namespaceSpan,
                    out _))
            {
                return string.Empty;
            }

            var namespaceName = fullyQualifiedName.Substring(namespaceSpan.Start, namespaceSpan.Length);
            return namespaceName;
        }

        internal static RazorCodeAction CreateAddUsingCodeAction(string fullyQualifiedName, DocumentUri uri)
        {
            var @namespace = GetNamespaceFromFQN(fullyQualifiedName);
            if (string.IsNullOrEmpty(@namespace))
            {
                return null;
            }

            var actionParams = new AddUsingsCodeActionParams
            {
                Uri = uri,
                Namespace = @namespace
            };

            var resolutionParams = new RazorCodeActionResolutionParams
            {
                Action = LanguageServerConstants.CodeActions.AddUsing,
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = actionParams,
            };

            return new RazorCodeAction()
            {
                Title = $"@using {@namespace}",
                Data = resolutionParams
            };
        }
    }
}

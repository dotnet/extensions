// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal static class AddUsingsCodeActionProviderFactory
    {
        internal static readonly Regex AddUsingVSCodeAction = new Regex("^using (.+);$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

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

        internal static CodeAction CreateAddUsingCodeAction(string fullyQualifiedName, DocumentUri uri)
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

            return new CodeAction()
            {
                Title = $"@using {@namespace}",
                Data = JToken.FromObject(resolutionParams)
            };
        }

        /// <summary>
        /// Extracts the namespace from a C# add using statement provided by Visual Studio
        /// </summary>
        /// <param name="csharpAddUsing">Add using statement of the form `using System.X;`</param>
        /// <param name="namespace">Extract namespace `System.X`</param>
        /// <returns></returns>
        internal static bool TryExtractNamespace(string csharpAddUsing, out string @namespace)
        {
            // We must remove any leading/trailing new lines from the add using edit
            csharpAddUsing = csharpAddUsing.Trim();
            var regexMatchedTextEdit = AddUsingVSCodeAction.Match(csharpAddUsing);
            if (!regexMatchedTextEdit.Success ||

                // Two Regex matching groups are expected
                // 1. `using namespace;`
                // 2. `namespace`
                regexMatchedTextEdit.Groups.Count != 2)
            {
                // Text edit in an unexpected format
                @namespace = string.Empty;
                return false;
            }

            @namespace = regexMatchedTextEdit.Groups[1].Value;
            return true;
        }
    }
}

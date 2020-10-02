// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    [Export(typeof(FallbackCapabilitiesFilterResolver))]
    internal class DefaultFallbackCapabilitiesFilterResolver : FallbackCapabilitiesFilterResolver
    {
        public override Func<JToken, bool> Resolve(string lspRequestMethodName)
        {
            if (lspRequestMethodName is null)
            {
                throw new ArgumentNullException(nameof(lspRequestMethodName));
            }

            switch (lspRequestMethodName)
            {
                // Standard LSP capabilities
                case Methods.TextDocumentImplementationName:
                    return CheckImplementationCapabilities;
                case Methods.TextDocumentTypeDefinitionName:
                    return CheckTypeDefinitionCapabilities;
                case Methods.TextDocumentReferencesName:
                    return CheckFindAllReferencesCapabilities;
                case Methods.TextDocumentRenameName:
                    return CheckRenameCapabilities;
                case Methods.TextDocumentSignatureHelpName:
                    return CheckSignatureHelpCapabilities;
                case Methods.TextDocumentWillSaveName:
                    return CheckWillSaveCapabilities;
                case Methods.TextDocumentWillSaveWaitUntilName:
                    return CheckWillSaveWaitUntilCapabilities;
                case Methods.TextDocumentRangeFormattingName:
                    return CheckRangeFormattingCapabilities;
                case Methods.WorkspaceSymbolName:
                    return CheckWorkspaceSymbolCapabilities;
                case Methods.TextDocumentOnTypeFormattingName:
                    return CheckOnTypeFormattingCapabilities;
                case Methods.TextDocumentFormattingName:
                    return CheckFormattingCapabilities;
                case Methods.TextDocumentHoverName:
                    return CheckHoverCapabilities;
                case Methods.TextDocumentCodeActionName:
                    return CheckCodeActionCapabilities;
                case Methods.TextDocumentCodeLensName:
                    return CheckCodeLensCapabilities;
                case Methods.TextDocumentCompletionName:
                    return CheckCompletionCapabilities;
                case Methods.TextDocumentCompletionResolveName:
                    return CheckCompletionResolveCapabilities;
                case Methods.TextDocumentDefinitionName:
                    return CheckDefinitionCapabilities;
                case Methods.TextDocumentDocumentHighlightName:
                    return CheckHighlightCapabilities;

                // VS LSP Expansion capabilities
                case MSLSPMethods.DocumentReferencesName:
                    return CheckMSReferencesCapabilities;
                case MSLSPMethods.ProjectContextsName:
                    return CheckProjectContextsCapabilities;
                case MSLSPMethods.TextDocumentCodeActionResolveName:
                    return CheckCodeActionResolveCapabilities;
                case MSLSPMethods.OnAutoInsertName:
                    return CheckOnAutoInsertCapabilities;
                case MSLSPMethods.DocumentPullDiagnosticName:
                case MSLSPMethods.WorkspacePullDiagnosticName:
                    return CheckPullDiagnosticCapabilities;

                default:
                    return FallbackCheckCapabilties;
            }
        }

        private static bool CheckImplementationCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.ImplementationProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private bool CheckTypeDefinitionCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.TypeDefinitionProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckFindAllReferencesCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.ReferencesProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckRenameCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.RenameProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckSignatureHelpCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.SignatureHelpProvider != null;
        }

        private static bool CheckWillSaveCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.TextDocumentSync?.WillSave == true;
        }

        private static bool CheckWillSaveWaitUntilCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.TextDocumentSync?.WillSaveWaitUntil == true;
        }

        private static bool CheckRangeFormattingCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.DocumentRangeFormattingProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckWorkspaceSymbolCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.WorkspaceSymbolProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckOnTypeFormattingCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.DocumentOnTypeFormattingProvider != null;
        }

        private static bool CheckFormattingCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.DocumentFormattingProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckHoverCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.HoverProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckCodeActionCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.CodeActionProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckCodeLensCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.CodeLensProvider != null;
        }

        private static bool CheckCompletionCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.CompletionProvider != null;
        }

        private static bool CheckCompletionResolveCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.CompletionProvider?.ResolveProvider == true;
        }

        private static bool CheckDefinitionCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.DefinitionProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckHighlightCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<ServerCapabilities>();

            return serverCapabilities?.DocumentHighlightProvider.Match(
                boolValue => boolValue,
                options => options != null) ?? false;
        }

        private static bool CheckMSReferencesCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<VSServerCapabilities>();

            return serverCapabilities?.MSReferencesProvider == true;
        }

        private static bool CheckProjectContextsCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<VSServerCapabilities>();

            return serverCapabilities?.ProjectContextProvider == true;
        }

        private static bool CheckCodeActionResolveCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<VSServerCapabilities>();

            return serverCapabilities?.CodeActionsResolveProvider == true;
        }

        private static bool CheckOnAutoInsertCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<VSServerCapabilities>();

            return serverCapabilities?.OnAutoInsertProvider != null;
        }

        private static bool CheckPullDiagnosticCapabilities(JToken token)
        {
            var serverCapabilities = token.ToObject<VSServerCapabilities>();

            return serverCapabilities?.SupportsDiagnosticRequests == true;
        }

        private bool FallbackCheckCapabilties(JToken token)
        {
            // Fallback is to assume present

            return true;
        }
    }
}

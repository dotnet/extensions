// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ExtendableServerCapabilities : ServerCapabilities
    {
        public ExtendableServerCapabilities(ServerCapabilities inner, IEnumerable<IRegistrationExtension> registrationExtensions)
        {
            FoldingRangeProvider = inner.FoldingRangeProvider;
            ColorProvider = inner.ColorProvider;
            ImplementationProvider = inner.ImplementationProvider;
            TypeDefinitionProvider = inner.TypeDefinitionProvider;
            Experimental = inner.Experimental;
            ExecuteCommandProvider = inner.ExecuteCommandProvider;
            DocumentLinkProvider = inner.DocumentLinkProvider;
            RenameProvider = inner.RenameProvider;
            DocumentOnTypeFormattingProvider = inner.DocumentOnTypeFormattingProvider;
            DocumentRangeFormattingProvider = inner.DocumentRangeFormattingProvider;
            DeclarationProvider = inner.DeclarationProvider;
            DocumentFormattingProvider = inner.DocumentFormattingProvider;
            CodeActionProvider = inner.CodeActionProvider;
            WorkspaceSymbolProvider = inner.WorkspaceSymbolProvider;
            DocumentSymbolProvider = inner.DocumentSymbolProvider;
            DocumentHighlightProvider = inner.DocumentHighlightProvider;
            ReferencesProvider = inner.ReferencesProvider;
            DefinitionProvider = inner.DefinitionProvider;
            SignatureHelpProvider = inner.SignatureHelpProvider;
            CompletionProvider = inner.CompletionProvider;
            HoverProvider = inner.HoverProvider;
            TextDocumentSync = inner.TextDocumentSync;
            CodeLensProvider = inner.CodeLensProvider;
            Workspace = inner.Workspace;

            CapabilityExtensions = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var registrationExtension in registrationExtensions)
            {
                var optionsResult = registrationExtension.GetRegistration();
                CapabilityExtensions[optionsResult.ServerCapability] = optionsResult.Options;
            }
        }

        [JsonExtensionData]
        public Dictionary<string, object> CapabilityExtensions { get; set; }
    }
}

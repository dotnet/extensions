// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.InitializeName)]
    internal class InitializeHandler : IRequestHandler<InitializeParams, InitializeResult>
    {
        private static readonly InitializeResult InitializeResult = new InitializeResult
        {
            Capabilities = new VSServerCapabilities
            {
                CompletionProvider = new CompletionOptions()
                {
                    AllCommitCharacters = new[] { " ", "{", "}", "[", "]", "(", ")", ".", ",", ":", ";", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "<", ">", "?", "@", "#", "'", "\"", "\\" },
                    ResolveProvider = true,
                    TriggerCharacters = CompletionHandler.AllTriggerCharacters.ToArray()
                },
                OnAutoInsertProvider = new DocumentOnAutoInsertOptions()
                {
                    TriggerCharacters = new[] { ">", "=", "-" }
                },
                DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions()
                {
                    // These trigger characters cannot overlap with OnAutoInsert trigger characters or they will be ignored.
                    FirstTriggerCharacter = "}",
                    MoreTriggerCharacter = new[] { ";" }
                },
                HoverProvider = true,
                DefinitionProvider = true,
                DocumentHighlightProvider = true,
                RenameProvider = true,
                ReferencesProvider = true,
                SemanticTokensOptions = new SemanticTokensOptions()
                {
                    RangeProvider = true,
                    DocumentProvider = new SemanticTokensDocumentProviderOptions()
                    {
                        Edits = true,
                    },
                },
                SignatureHelpProvider = new SignatureHelpOptions()
                {
                    TriggerCharacters = new[] { "(", "," }
                },
                ImplementationProvider = true,
            }
        };

        public Task<InitializeResult> HandleRequestAsync(InitializeParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => Task.FromResult(InitializeResult);
    }
}

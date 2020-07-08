// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class CodeActionEndpoint : ICodeActionHandler
    {
        private readonly IEnumerable<RazorCodeActionProvider> _providers;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly ILogger _logger;

        private CodeActionCapability _capability;

        public CodeActionEndpoint(
            IEnumerable<RazorCodeActionProvider> providers,
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _logger = loggerFactory.CreateLogger<CodeActionEndpoint>();
        }

        public CodeActionRegistrationOptions GetRegistrationOptions()
        {
            return new CodeActionRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector
            };
        }

        public async Task<CommandOrCodeActionContainer> Handle(CodeActionParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var documentSnapshot = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);

            if (documentSnapshot is null)
            {
                return null;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var sourceText = await documentSnapshot.GetTextAsync().ConfigureAwait(false);
            var linePosition = new LinePosition((int)request.Range.Start.Line, (int)request.Range.Start.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(hostDocumentIndex, (int)request.Range.Start.Line, (int)request.Range.Start.Character);

            var context = new RazorCodeActionContext(request, documentSnapshot, codeDocument, location);
            var tasks = new List<Task<CommandOrCodeActionContainer>>();

            foreach (var provider in _providers)
            {
                var result = provider.ProvideAsync(context, cancellationToken);
                if (result != null)
                {
                    tasks.Add(result);
                }
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var container = new List<CommandOrCodeAction>();
            foreach (var result in results)
            {
                if (result != null)
                {
                    foreach (var commandOrCodeAction in result)
                    {
                        container.Add(commandOrCodeAction);
                    }
                }
            }

            return new CommandOrCodeActionContainer(container);
        }

        public void SetCapability(CodeActionCapability capability)
        {
            _capability = capability;
        }
    }
}

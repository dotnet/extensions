// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class CodeActionEndpoint : IRazorCodeActionHandler
    {
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly IEnumerable<RazorCodeActionProvider> _razorCodeActionProviders;
        private readonly IEnumerable<CSharpCodeActionProvider> _csharpCodeActionProviders;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
        private readonly ClientNotifierServiceBase _languageServer;

        private CodeActionCapability _capability;

        internal bool _supportsCodeActionResolve = false;

        private readonly IReadOnlyCollection<string> _allAvailableCodeActionNames;

        public CodeActionEndpoint(
            RazorDocumentMappingService documentMappingService,
            IEnumerable<RazorCodeActionProvider> razorCodeActionProviders,
            IEnumerable<CSharpCodeActionProvider> csharpCodeActionProviders,
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            ClientNotifierServiceBase languageServer,
            LanguageServerFeatureOptions languageServerFeatureOptions)
        {
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
            _razorCodeActionProviders = razorCodeActionProviders ?? throw new ArgumentNullException(nameof(razorCodeActionProviders));
            _csharpCodeActionProviders = csharpCodeActionProviders ?? throw new ArgumentNullException(nameof(csharpCodeActionProviders));
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _languageServer = languageServer ?? throw new ArgumentNullException(nameof(languageServer));
            _languageServerFeatureOptions = languageServerFeatureOptions ?? throw new ArgumentNullException(nameof(languageServerFeatureOptions));

            _allAvailableCodeActionNames = GetAllAvailableCodeActionNames();
        }

        public CodeActionRegistrationOptions GetRegistrationOptions()
        {
            return new CodeActionRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector,
                CodeActionKinds = new[] {
                    CodeActionKind.RefactorExtract,
                    CodeActionKind.QuickFix,
                    CodeActionKind.Refactor
                }
            };
        }

        public void SetCapability(CodeActionCapability capability)
        {
            _capability = capability;

            var extendableClientCapabilities = _languageServer.ClientSettings?.Capabilities as ExtendableClientCapabilities;
            _supportsCodeActionResolve = extendableClientCapabilities?.SupportsCodeActionResolve ?? false;
        }

        public async Task<CommandOrCodeActionContainer> Handle(RazorCodeActionParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var razorCodeActionContext = await GenerateRazorCodeActionContextAsync(request, cancellationToken).ConfigureAwait(false);
            if (razorCodeActionContext is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var razorCodeActions = await GetRazorCodeActionsAsync(razorCodeActionContext, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var csharpCodeActions = await GetCSharpCodeActionsAsync(razorCodeActionContext, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var codeActions = Enumerable.Concat(
                razorCodeActions ?? Array.Empty<RazorCodeAction>(),
                csharpCodeActions ?? Array.Empty<RazorCodeAction>());

            if (!codeActions.Any())
            {
                return null;
            }

            // We must cast the RazorCodeAction into a platform compliant code action
            // For VS (SupportsCodeActionResolve = true) this means just encapsulating the RazorCodeAction in the `CommandOrCodeAction` struct
            // For VS Code (SupportsCodeActionResolve = false) we must convert it into a CodeAction or Command before encapsulating in the `CommandOrCodeAction` struct.
            var commandsOrCodeActions = codeActions.Select(c =>
                _supportsCodeActionResolve ? new CommandOrCodeAction(c) : c.AsVSCodeCommandOrCodeAction());

            return new CommandOrCodeActionContainer(commandsOrCodeActions);
        }

        // internal for testing
        internal async Task<RazorCodeActionContext> GenerateRazorCodeActionContextAsync(RazorCodeActionParams request, CancellationToken cancellationToken)
        {
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

            // VS Provides `CodeActionParams.Context.SelectionRange` in addition to
            // `CodeActionParams.Range`. The `SelectionRange` is relative to where the
            // code action was invoked (ex. line 14, char 3) whereas the `Range` is
            // always at the start of the line (ex. line 14, char 0). We want to utilize
            // the relative positioning to ensure we provide code actions for the appropriate
            // context.
            //
            // Note: VS Code doesn't provide a `SelectionRange`.
            if (request.Context.SelectionRange != null)
            {
                request.Range = request.Context.SelectionRange;
            }

            // We hide `CodeActionParams.CodeActionContext` in order to capture
            // `RazorCodeActionParams.ExtendedCodeActionContext`, we must
            // restore this context to access diagnostics.
            (request as CodeActionParams).Context = request.Context;

            var linePosition = new LinePosition(
                request.Range.Start.Line,
                request.Range.Start.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(
                hostDocumentIndex,
                request.Range.Start.Line,
                request.Range.Start.Character);

            var context = new RazorCodeActionContext(
                request,
                documentSnapshot,
                codeDocument,
                location,
                sourceText,
                _languageServerFeatureOptions.SupportsFileManipulation,
                _supportsCodeActionResolve);

            return context;
        }

        private async Task<IEnumerable<RazorCodeAction>> GetCSharpCodeActionsAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            var csharpCodeActions = await GetCSharpCodeActionsFromLanguageServerAsync(context, cancellationToken);
            if (csharpCodeActions is null || !csharpCodeActions.Any())
            {
                return null;
            }

            var csharpNamedCodeActions = ExtractCSharpCodeActionNamesFromData(csharpCodeActions);
            var filteredCSharpCodeActions = await FilterCSharpCodeActionsAsync(context, csharpNamedCodeActions, cancellationToken);
            return filteredCSharpCodeActions;
        }

        private IEnumerable<RazorCodeAction> ExtractCSharpCodeActionNamesFromData(IEnumerable<RazorCodeAction> codeActions)
        {
            return codeActions.Where(codeAction =>
            {
                // Note: we may see a perf benefit from using a JsonConverter
                var tags = codeAction.Data["CustomTags"]?.ToObject<string[]>(); ;
                if (tags is null || tags.Length == 0)
                {
                    return false;
                }

                foreach (var tag in tags)
                {
                    if (_allAvailableCodeActionNames.Contains(tag))
                    {
                        codeAction.Name = tag;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(codeAction.Name))
                {
                    return false;
                }

                return true;
            }).ToArray();
        }

        private async Task<IEnumerable<RazorCodeAction>> FilterCSharpCodeActionsAsync(
            RazorCodeActionContext context,
            IEnumerable<RazorCodeAction> codeActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = new List<Task<IReadOnlyList<RazorCodeAction>>>();

            foreach (var provider in _csharpCodeActionProviders)
            {
                var result = provider.ProvideAsync(context, codeActions, cancellationToken);
                if (result != null)
                {
                    tasks.Add(result);
                }
            }

            return await ConsolidateCodeActionsFromProvidersAsync(tasks, cancellationToken);
        }

        // Internal for testing
        internal async Task<IEnumerable<RazorCodeAction>> GetCSharpCodeActionsFromLanguageServerAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            Range projectedRange = null;
            if (context.Request.Range != null &&
                !_documentMappingService.TryMapToProjectedDocumentRange(
                    context.CodeDocument,
                    context.Request.Range,
                    out projectedRange))
            {
                return Array.Empty<RazorCodeAction>();
            }

            context.Request.Range = projectedRange;

            cancellationToken.ThrowIfCancellationRequested();

            var response = await _languageServer.SendRequestAsync(LanguageServerConstants.RazorProvideCodeActionsEndpoint, context.Request);
            return await response.Returning<RazorCodeAction[]>(cancellationToken);
        }

        private async Task<IEnumerable<RazorCodeAction>> GetRazorCodeActionsAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = new List<Task<IReadOnlyList<RazorCodeAction>>>();

            foreach (var provider in _razorCodeActionProviders)
            {
                var result = provider.ProvideAsync(context, cancellationToken);
                if (result != null)
                {
                    tasks.Add(result);
                }
            }

            return await ConsolidateCodeActionsFromProvidersAsync(tasks, cancellationToken);
        }

        private static async Task<IEnumerable<RazorCodeAction>> ConsolidateCodeActionsFromProvidersAsync(
            List<Task<IReadOnlyList<RazorCodeAction>>> tasks,
            CancellationToken cancellationToken)
        {
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var codeActions = new List<RazorCodeAction>();

            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];

                if (!(result is null))
                {
                    codeActions.AddRange(result);
                }
            }

            return codeActions;
        }

        private static HashSet<string> GetAllAvailableCodeActionNames()
        {
            var availableCodeActionNames = new HashSet<string>();

            var refactoringProviderNames = typeof(RazorPredefinedCodeRefactoringProviderNames)
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string))
                .Select(property => property.GetValue(null) as string);
            var codeFixProviderNames = typeof(RazorPredefinedCodeFixProviderNames)
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string))
                .Select(property => property.GetValue(null) as string);

            availableCodeActionNames.UnionWith(refactoringProviderNames);
            availableCodeActionNames.UnionWith(codeFixProviderNames);
            availableCodeActionNames.Add(LanguageServerConstants.CodeActions.CodeActionFromVSCode);

            return availableCodeActionNames;
        }
    }
}

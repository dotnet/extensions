// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

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
                    TriggerCharacters = new[] { ">", "=", "-", "'", "/", "\n" }
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
                SignatureHelpProvider = new SignatureHelpOptions()
                {
                    TriggerCharacters = new[] { "(", ",", "<" },
                    RetriggerCharacters = new[] { ">", ")" }
                },
                ImplementationProvider = true,
                SupportsDiagnosticRequests = true,
                OnTypeRenameProvider = new DocumentOnTypeRenameOptions()
            }
        };
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly ILanguageClientBroker _languageClientBroker;
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly List<(ILanguageClient Client, VSServerCapabilities Capabilities)> _serverCapabilities;
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public InitializeHandler(
            JoinableTaskContext joinableTaskContext,
            ILanguageClientBroker languageClientBroker,
            ILanguageServiceBroker2 languageServiceBroker,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (languageClientBroker is null)
            {
                throw new ArgumentNullException(nameof(languageClientBroker));
            }

            if (languageServiceBroker is null)
            {
                throw new ArgumentNullException(nameof(languageServiceBroker));
            }

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
            _languageClientBroker = languageClientBroker;
            _languageServiceBroker = languageServiceBroker;

            _logger = loggerProvider.CreateLogger(nameof(InitializeHandler));

            _serverCapabilities = new List<(ILanguageClient, VSServerCapabilities)>();

            _serializer = new JsonSerializer();
            _serializer.AddVSExtensionConverters();
        }

        public Task<InitializeResult> HandleRequestAsync(InitializeParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            VerifyMergedLanguageServerCapabilities();

            _logger.LogInformation("Providing initialization configuration.");

            return Task.FromResult(InitializeResult);
        }

        [Conditional("DEBUG")]
        private void VerifyMergedLanguageServerCapabilities()
        {
            _ = Task.Run(async () =>
            {
                var containedLanguageServerClients = await EnsureContainedLanguageServersInitializedAsync().ConfigureAwait(false);

                var mergedCapabilities = GetMergedServerCapabilities(containedLanguageServerClients);

                await VerifyMergedCompletionOptionsAsync(mergedCapabilities);

                await VerifyMergedHoverAsync(mergedCapabilities);

                await VerifyMergedOnAutoInsertAsync(mergedCapabilities);

                await VerifyMergedSignatureHelpOptionsAsync(mergedCapabilities);

                await VerifyMergedDefinitionProviderAsync(mergedCapabilities);

                await VerifyMergedReferencesProviderAsync(mergedCapabilities);

                await VerifyMergedRenameProviderAsync(mergedCapabilities);

                await VerifyMergedOnTypeFormattingProviderAsync(mergedCapabilities);
            }).ConfigureAwait(false);
        }

        private VSServerCapabilities GetMergedServerCapabilities(List<ILanguageClient> relevantLanguageClients)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var languageClientInstance in _languageServiceBroker.ActiveLanguageClients)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (relevantLanguageClients.Contains(languageClientInstance.Client))
                {
                    var resultToken = languageClientInstance.InitializeResult;
                    var initializeResult = resultToken.ToObject<InitializeResult>(_serializer);

                    _serverCapabilities.Add((languageClientInstance.Client, (initializeResult.Capabilities as VSServerCapabilities)!));
                }
            }

            var serverCapabilities = new VSServerCapabilities
            {
                CompletionProvider = GetMergedCompletionOptions(),
                TextDocumentSync = GetMergedTextDocumentSyncOptions(),
                HoverProvider = GetMergedHoverProvider(),
                OnAutoInsertProvider = GetMergedDocumentOnAutoInsertOptions(),
                SignatureHelpProvider = GetMergedSignatureHelpOptions(),
                DefinitionProvider = GetMergedDefinitionProvider(),
                ReferencesProvider = GetMergedReferencesProvider(),
                RenameProvider = GetMergedRenameProvider(),
                DocumentOnTypeFormattingProvider = GetMergedOnTypeFormattingProvider(),
            };

            return serverCapabilities;
        }

        private DocumentOnTypeFormattingOptions GetMergedOnTypeFormattingProvider()
        {
            var documentOnTypeFormattingProviderOptions = _serverCapabilities.Where(s => s.Capabilities.DocumentOnTypeFormattingProvider != null).Select(s => s.Capabilities.DocumentOnTypeFormattingProvider!);
            var triggerChars = new HashSet<string>();

            foreach (var options in documentOnTypeFormattingProviderOptions)
            {
                if (options.FirstTriggerCharacter != null)
                {
                    triggerChars.Add(options.FirstTriggerCharacter);
                }

                if (options.MoreTriggerCharacter != null)
                {
                    triggerChars.UnionWith(options.MoreTriggerCharacter);
                }
            }

            return new DocumentOnTypeFormattingOptions()
            {
                MoreTriggerCharacter = triggerChars.ToArray(),
            };
        }

        private bool GetMergedHoverProvider()
        {
            return _serverCapabilities.Any(s => s.Capabilities.HoverProvider?.Value is bool isHoverSupported && isHoverSupported);
        }

        private DocumentOnAutoInsertOptions GetMergedDocumentOnAutoInsertOptions()
        {
            var allDocumentOnAutoInsertOptions = _serverCapabilities.Where(s => s.Capabilities.OnAutoInsertProvider != null).Select(s => s.Capabilities.OnAutoInsertProvider!);
            var triggerChars = new HashSet<string>();

            foreach (var documentOnAutoInsertOptions in allDocumentOnAutoInsertOptions)
            {
                if (documentOnAutoInsertOptions.TriggerCharacters != null)
                {
                    triggerChars.UnionWith(documentOnAutoInsertOptions.TriggerCharacters);
                }
            }

            return new DocumentOnAutoInsertOptions()
            {
                TriggerCharacters = triggerChars.ToArray(),
            };
        }

        private TextDocumentSyncOptions GetMergedTextDocumentSyncOptions()
        {
            var allTextDocumentSyncOptions = _serverCapabilities.Where(s => s.Capabilities.TextDocumentSync != null).Select(s => s.Capabilities.TextDocumentSync!);

            var openClose = false;

            foreach (var curTextDocumentSyncOptions in allTextDocumentSyncOptions)
            {
                openClose |= curTextDocumentSyncOptions.OpenClose;
            }

            var textDocumentSyncOptions = new TextDocumentSyncOptions()
            {
                OpenClose = openClose,
                Change = TextDocumentSyncKind.Incremental,
            };

            return textDocumentSyncOptions;
        }

        private CompletionOptions GetMergedCompletionOptions()
        {
            var allCompletionOptions = _serverCapabilities.Where(s => s.Capabilities.CompletionProvider != null).Select(s => s.Capabilities.CompletionProvider!);

            var commitChars = new HashSet<string>();
            var triggerChars = new HashSet<string>();
            var resolveProvider = false;

            foreach (var curCompletionOptions in allCompletionOptions)
            {
                if (curCompletionOptions.AllCommitCharacters != null)
                {
                    commitChars.UnionWith(curCompletionOptions.AllCommitCharacters);
                }

                if (curCompletionOptions.TriggerCharacters != null)
                {
                    triggerChars.UnionWith(curCompletionOptions.TriggerCharacters);
                }

                resolveProvider |= curCompletionOptions.ResolveProvider;
            }

            var completionOptions = new CompletionOptions()
            {
                AllCommitCharacters = commitChars.ToArray(),
                ResolveProvider = resolveProvider,
                TriggerCharacters = triggerChars.ToArray(),
            };

            return completionOptions;
        }

        private SignatureHelpOptions GetMergedSignatureHelpOptions()
        {
            var allSignatureHelpOptions = _serverCapabilities.Where(s => s.Capabilities.SignatureHelpProvider != null).Select(s => s.Capabilities.SignatureHelpProvider!);

            var triggerCharacters = new HashSet<string>();
            var retriggerChars = new HashSet<string>();
            var workDoneProgress = false;

            foreach (var curSignatureHelpOptions in allSignatureHelpOptions)
            {
                if (curSignatureHelpOptions.TriggerCharacters != null)
                {
                    triggerCharacters.UnionWith(curSignatureHelpOptions.TriggerCharacters);
                }

                if (curSignatureHelpOptions.RetriggerCharacters != null)
                {
                    retriggerChars.UnionWith(curSignatureHelpOptions.RetriggerCharacters);
                }

                workDoneProgress |= curSignatureHelpOptions.WorkDoneProgress;
            }

            var signatureHelpOptions = new SignatureHelpOptions()
            {
                TriggerCharacters = triggerCharacters.ToArray(),
                RetriggerCharacters = retriggerChars.ToArray(),
                WorkDoneProgress = workDoneProgress,
            };

            return signatureHelpOptions;
        }

        private bool GetMergedDefinitionProvider()
        {
            return _serverCapabilities.Any(s => s.Capabilities.DefinitionProvider?.Value is bool isDefinitionSupported && isDefinitionSupported);
        }

        private bool GetMergedReferencesProvider()
        {
            return _serverCapabilities.Any(s => s.Capabilities.ReferencesProvider?.Value is bool isFindAllReferencesSupported && isFindAllReferencesSupported);
        }

        private bool GetMergedRenameProvider()
        {
            return _serverCapabilities.Any(s => s.Capabilities.RenameProvider?.Value is bool isRenameSupported && isRenameSupported);
        }

        private async Task VerifyMergedOnAutoInsertAsync(VSServerCapabilities mergedCapabilities)
        {
            var triggerCharEnumeration = mergedCapabilities.OnAutoInsertProvider?.TriggerCharacters ?? Enumerable.Empty<string>();
            var onAutoInsertMergedTriggerChars = new HashSet<string>(triggerCharEnumeration);
            if (!onAutoInsertMergedTriggerChars.SetEquals(triggerCharEnumeration))
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("on auto insert contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedHoverAsync(VSServerCapabilities mergedCapabilities)
        {
            if (mergedCapabilities.HoverProvider != InitializeResult.Capabilities.HoverProvider)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("hover contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedCompletionOptionsAsync(VSServerCapabilities mergedCapabilities)
        {
            var mergedAllCommitCharEnumeration = mergedCapabilities.CompletionProvider?.AllCommitCharacters ?? Enumerable.Empty<string>();
            var mergedTriggerCharEnumeration = mergedCapabilities.CompletionProvider?.TriggerCharacters ?? Enumerable.Empty<string>();
            var mergedCommitChars = new HashSet<string>(mergedAllCommitCharEnumeration);
            var purposefullyRemovedTriggerCharacters = new[]
            {
                "_" // https://github.com/dotnet/aspnetcore-tooling/pull/2827
            };
            mergedTriggerCharEnumeration = mergedTriggerCharEnumeration.Except(purposefullyRemovedTriggerCharacters);
            var mergedTriggerChars = new HashSet<string>(mergedTriggerCharEnumeration);

            if (!mergedCommitChars.SetEquals(InitializeResult.Capabilities.CompletionProvider?.AllCommitCharacters!) ||
                !mergedTriggerChars.SetEquals(InitializeResult.Capabilities.CompletionProvider?.TriggerCharacters!))
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("completion merged contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedSignatureHelpOptionsAsync(VSServerCapabilities mergedCapabilities)
        {
            var mergedTriggerCharEnumeration = mergedCapabilities.SignatureHelpProvider?.TriggerCharacters ?? Enumerable.Empty<string>();
            var mergedTriggerChars = new HashSet<string>(mergedTriggerCharEnumeration);
            var mergedRetriggerCharEnumeration = mergedCapabilities.SignatureHelpProvider?.RetriggerCharacters ?? Enumerable.Empty<string>();
            var mergedRetriggerChars = new HashSet<string>(mergedRetriggerCharEnumeration);
            var mergedWorkDoneProgress = mergedCapabilities.SignatureHelpProvider?.WorkDoneProgress;

            if (!mergedTriggerChars.SetEquals(InitializeResult.Capabilities.SignatureHelpProvider?.TriggerCharacters!) ||
                !mergedRetriggerChars.SetEquals(InitializeResult.Capabilities.SignatureHelpProvider?.RetriggerCharacters!) ||
                mergedWorkDoneProgress != InitializeResult.Capabilities.SignatureHelpProvider?.WorkDoneProgress)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("signature help merged contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedDefinitionProviderAsync(VSServerCapabilities mergedCapabilities)
        {
            if (mergedCapabilities.DefinitionProvider != InitializeResult.Capabilities.DefinitionProvider)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("definition provider contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedReferencesProviderAsync(VSServerCapabilities mergedCapabilities)
        {
            if (mergedCapabilities.ReferencesProvider != InitializeResult.Capabilities.ReferencesProvider)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("references provider contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedRenameProviderAsync(VSServerCapabilities mergedCapabilities)
        {
            if (mergedCapabilities.RenameProvider != InitializeResult.Capabilities.RenameProvider)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("rename provider contained langauge server capabilities mismatch");
            }
        }

        private async Task VerifyMergedOnTypeFormattingProviderAsync(VSServerCapabilities mergedCapabilities)
        {
            var mergedTriggerCharacters = mergedCapabilities.DocumentOnTypeFormattingProvider.MoreTriggerCharacter;
            var purposefullyRemovedTriggerCharacters = new[]
            {
                "\n" // https://github.com/dotnet/aspnetcore/issues/28002
            };
            var filteredMergedTriggerCharacters = mergedTriggerCharacters.Except(purposefullyRemovedTriggerCharacters);
            var mergedTriggerChars = new HashSet<string>(filteredMergedTriggerCharacters);

            var razorOnTypeFormattingOptions = InitializeResult.Capabilities.DocumentOnTypeFormattingProvider;
            var razorTriggerCharacters = new HashSet<string>();
            razorTriggerCharacters.Add(razorOnTypeFormattingOptions.FirstTriggerCharacter);
            razorTriggerCharacters.UnionWith(razorOnTypeFormattingOptions.MoreTriggerCharacter);

            if (!mergedTriggerChars.SetEquals(razorTriggerCharacters))
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Debug.Fail("OnTypeFormatting trigger characters capabilities mismatch");
            }
        }

        // Ensures all contained language servers that we rely on are started.
        private async Task<List<ILanguageClient>> EnsureContainedLanguageServersInitializedAsync()
        {
            var relevantLanguageClients = new List<ILanguageClient>();
            var clientLoadTasks = new List<Task>();

#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var languageClientAndMetadata in _languageServiceBroker.LanguageClients)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (!(languageClientAndMetadata.Metadata is ILanguageClientMetadata metadata))
                {
                    continue;
                }

                if (metadata is IIsUserExperienceDisabledMetadata userExperienceDisabledMetadata &&
                    userExperienceDisabledMetadata.IsUserExperienceDisabled)
                {
                    continue;
                }

                if (IsCSharpApplicable(metadata) ||
                    metadata.ContentTypes.Contains(RazorLSPConstants.HtmlLSPContentTypeName))
                {
                    relevantLanguageClients.Add(languageClientAndMetadata.Value);

                    var loadAsyncTask = _languageClientBroker.LoadAsync(metadata, languageClientAndMetadata.Value);
                    clientLoadTasks.Add(loadAsyncTask);
                }
            }

            await Task.WhenAll(clientLoadTasks).ConfigureAwait(false);

            return relevantLanguageClients;

            static bool IsCSharpApplicable(ILanguageClientMetadata metadata)
            {
                return metadata.ContentTypes.Contains(RazorLSPConstants.CSharpContentTypeName) &&
                    metadata.ClientName == CSharpVirtualDocumentFactory.CSharpClientName;
            }
        }
    }
}

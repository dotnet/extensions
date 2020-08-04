// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [ExportCollaborationService(typeof(SessionActiveDetector), Scope = SessionScope.Guest)]
    internal class RazorGuestInitializationService : ICollaborationServiceFactory
    {
        private const string ViewImportsFileName = "_ViewImports.cshtml";
        private readonly DefaultLiveShareSessionAccessor _sessionAccessor;

        // Internal for testing
        internal Task _viewImportsCopyTask;

        [ImportingConstructor]
        public RazorGuestInitializationService([Import(typeof(LiveShareSessionAccessor))] DefaultLiveShareSessionAccessor sessionAccessor)
        {
            if (sessionAccessor == null)
            {
                throw new ArgumentNullException(nameof(sessionAccessor));
            }

            _sessionAccessor = sessionAccessor;
        }

        public Task<ICollaborationService> CreateServiceAsync(CollaborationSession sessionContext, CancellationToken cancellationToken)
        {
            if (sessionContext == null)
            {
                throw new ArgumentNullException(nameof(sessionContext));
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#pragma warning restore CA2000 // Dispose objects before losing scope
            _viewImportsCopyTask = EnsureViewImportsCopiedAsync(sessionContext, cts.Token);

            _sessionAccessor.SetSession(sessionContext);
            var sessionDetector = new SessionActiveDetector(() =>
            {
                cts.Cancel();
                _sessionAccessor.SetSession(session: null);
            });
            return Task.FromResult<ICollaborationService>(sessionDetector);
        }

        // Today we ensure that all _ViewImports in the shared project exist on the guest because we don't currently track import documents
        // in a manner that would allow us to retrieve/monitor that data across the wire. Once the Razor sub-system is moved to use
        // DocumentSnapshots we'll be able to rely on that API to more properly manage files that impact parsing of Razor documents.
        private async Task EnsureViewImportsCopiedAsync(CollaborationSession sessionContext, CancellationToken cancellationToken)
        {
            var listDirectoryOptions = new ListDirectoryOptions()
            {
                Recursive = true,
                IncludePatterns = new[] { "*.cshtml" }
            };

            var copyTasks = new List<Task>();

            try
            {
                var roots = await sessionContext.ListRootsAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                foreach (var root in roots)
                {
                    var fileUris = await sessionContext.ListDirectoryAsync(root, listDirectoryOptions, cancellationToken);
                    StartViewImportsCopy(fileUris, copyTasks, sessionContext, cancellationToken);
                }

                await Task.WhenAll(copyTasks);
            }
            catch (OperationCanceledException)
            {
                // Swallow task cancellations
            }
        }

        private static void StartViewImportsCopy(Uri[] fileUris, List<Task> copyTasks, CollaborationSession sessionContext, CancellationToken cancellationToken)
        {
            foreach (var fileUri in fileUris)
            {
                if (fileUri.GetAbsoluteOrUNCPath().EndsWith(ViewImportsFileName, StringComparison.Ordinal))
                {
                    var copyTask = sessionContext.DownloadFileAsync(fileUri, cancellationToken);
                    copyTasks.Add(copyTask);
                }
            }
        }
    }

    public class SessionActiveDetector : ICollaborationService, IDisposable
    {
        private readonly Action _onDispose;

        public SessionActiveDetector(Action onDispose)
        {
            if (onDispose == null)
            {
                throw new ArgumentNullException(nameof(onDispose));
            }

            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}

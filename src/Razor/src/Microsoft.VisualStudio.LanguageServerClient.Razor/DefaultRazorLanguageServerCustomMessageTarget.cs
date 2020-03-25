// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(RazorLanguageServerCustomMessageTarget))]
    public class DefaultRazorLanguageServerCustomMessageTarget : RazorLanguageServerCustomMessageTarget, IDisposable
    {
        private readonly TrackingLSPDocumentManager _documentManager;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly SingleThreadedFIFOSemaphoreSlim _updateCSharpSemaphoreSlim;
        private readonly SingleThreadedFIFOSemaphoreSlim _updateHtmlSemaphoreSlim;

        [ImportingConstructor]
        public DefaultRazorLanguageServerCustomMessageTarget(
            LSPDocumentManager documentManager,
            JoinableTaskContext joinableTaskContext)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _documentManager = documentManager as TrackingLSPDocumentManager;

            if (_documentManager is null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentException("The LSP document manager should be of type " + typeof(TrackingLSPDocumentManager).FullName, nameof(_documentManager));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
            _updateCSharpSemaphoreSlim = new SingleThreadedFIFOSemaphoreSlim();
            _updateHtmlSemaphoreSlim = new SingleThreadedFIFOSemaphoreSlim();
        }

        // Testing constructor
        internal DefaultRazorLanguageServerCustomMessageTarget(TrackingLSPDocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        public override async Task UpdateCSharpBufferAsync(JToken token, CancellationToken cancellationToken)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            await _updateCSharpSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                UpdateCSharpBuffer(token);
            }
            finally
            {
                _updateCSharpSemaphoreSlim.Release();
            }
        }

        // Internal for testing
        internal void UpdateCSharpBuffer(JToken token)
        {
            var request = token.ToObject<UpdateBufferRequest>();
            if (request == null || request.HostDocumentFilePath == null)
            {
                return;
            }

            var hostDocumentUri = ConvertFilePathToUri(request.HostDocumentFilePath);
            _documentManager.UpdateVirtualDocument<CSharpVirtualDocument>(
                hostDocumentUri,
                request.Changes,
                request.HostDocumentVersion);
        }

        public override async Task UpdateHtmlBufferAsync(JToken token, CancellationToken cancellationToken)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            await _updateHtmlSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                UpdateHtmlBuffer(token);
            }
            finally
            {
                _updateHtmlSemaphoreSlim.Release();
            }
        }

        // Internal for testing
        internal void UpdateHtmlBuffer(JToken token)
        {
            var request = token.ToObject<UpdateBufferRequest>();
            if (request == null || request.HostDocumentFilePath == null)
            {
                return;
            }

            var hostDocumentUri = ConvertFilePathToUri(request.HostDocumentFilePath);
            _documentManager.UpdateVirtualDocument<HtmlVirtualDocument>(
                hostDocumentUri,
                request.Changes,
                request.HostDocumentVersion);
        }

        private static Uri ConvertFilePathToUri(string filePath)
        {
            if (filePath.StartsWith("/", StringComparison.Ordinal) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filePath = filePath.Substring(1);
            }

            var uri = new Uri(filePath, UriKind.Absolute);
            return uri;
        }

        public void Dispose()
        {
            _updateCSharpSemaphoreSlim?.Dispose();
            _updateHtmlSemaphoreSlim?.Dispose();
        }

        private class SingleThreadedFIFOSemaphoreSlim : IDisposable
        {
            private readonly SemaphoreSlim _inner;
            private readonly ConcurrentQueue<TaskCompletionSource<bool>> _queue;

            public SingleThreadedFIFOSemaphoreSlim()
            {
                _inner = new SemaphoreSlim(1, 1);
                _queue = new ConcurrentQueue<TaskCompletionSource<bool>>();
            }

            public Task WaitAsync()
            {
                var tcs = new TaskCompletionSource<bool>();
                _queue.Enqueue(tcs);

                _inner.WaitAsync().ContinueWith(_ =>
                {
                    // When the thread becomes available, unblock the next task on a FIFO basis.
                    if (_queue.TryDequeue(out var oldest))
                    {
                        oldest.SetResult(true);
                    }
                }, TaskScheduler.Default);

                return tcs.Task;
            }

            public void Release()
            {
                _inner.Release();
            }

            public void Dispose()
            {
                _inner.Dispose();
            }
        }
    }
}

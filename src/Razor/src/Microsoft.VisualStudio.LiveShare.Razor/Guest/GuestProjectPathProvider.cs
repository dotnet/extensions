// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [System.Composition.Shared]
    [Export(typeof(LiveShareProjectPathProvider))]
    internal class GuestProjectPathProvider : LiveShareProjectPathProvider
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly ProxyAccessor _proxyAccessor;
        private readonly LiveShareSessionAccessor _liveShareSessionAccessor;

        [ImportingConstructor]
        public GuestProjectPathProvider(
            JoinableTaskContext joinableTaskContext,
            ITextDocumentFactoryService textDocumentFactory,
            ProxyAccessor proxyAccessor,
            LiveShareSessionAccessor liveShareSessionAccessor)
        {
            if (joinableTaskContext == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (proxyAccessor == null)
            {
                throw new ArgumentNullException(nameof(proxyAccessor));
            }

            if (liveShareSessionAccessor == null)
            {
                throw new ArgumentNullException(nameof(liveShareSessionAccessor));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
            _textDocumentFactory = textDocumentFactory;
            _proxyAccessor = proxyAccessor;
            _liveShareSessionAccessor = liveShareSessionAccessor;
        }

        public override bool TryGetProjectPath(ITextBuffer textBuffer, out string filePath)
        {
            if (!_liveShareSessionAccessor.IsGuestSessionActive)
            {
                filePath = null;
                return false;
            }

            if (!_textDocumentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                filePath = null;
                return false;
            }

            var hostProjectPath = GetHostProjectPath(textDocument);
            if (hostProjectPath == null)
            {
                filePath = null;
                return false;
            }

            // Host always responds with a host-based path, convert back to a guest one.
            filePath = ResolveGuestPath(hostProjectPath);
            return true;
        }

        // Internal virtual for testing
        internal virtual Uri GetHostProjectPath(ITextDocument textDocument)
        {
            // The path we're given is from the guest so following other patterns we always ask the host information in its own form (aka convert on guest instead of on host).
            var ownerPath = _liveShareSessionAccessor.Session?.ConvertLocalPathToSharedUri(textDocument.FilePath);

            var hostProjectPath = _joinableTaskFactory.Run(() =>
            {
                var projectHierarchyProxy = _proxyAccessor.GetProjectHierarchyProxy();

                // We need to block the foreground thread to get a proper project path. However, this is only done once on opening thedocument.
                return projectHierarchyProxy.GetProjectPathAsync(ownerPath, CancellationToken.None);
            });

            return hostProjectPath;
        }

        // We do not want this inlined because the work done in this method requires the VisualStudio.LiveShare assembly.
        // We do not want to load that assembly outside of a LiveShare session.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string ResolveGuestPath(Uri hostProjectPath)
        {
            return _liveShareSessionAccessor.Session?.ConvertSharedUriToLocalPath(hostProjectPath);
        }
    }
}

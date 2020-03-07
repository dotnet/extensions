// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(RazorLanguageServerCustomMessageTarget))]
    public class DefaultRazorLanguageServerCustomMessageTarget : RazorLanguageServerCustomMessageTarget
    {
        private readonly TrackingLSPDocumentManager _documentManager;
        private readonly JoinableTaskFactory _joinableTaskFactory;

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
                throw new ArgumentNullException(nameof(_documentManager));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
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

            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            UpdateCSharpBuffer(token);
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

            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            UpdateHtmlBuffer(token);
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

        private Uri ConvertFilePathToUri(string filePath)
        {
            if (filePath.StartsWith("/") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filePath = filePath.Substring(1);
            }

            var uri = new Uri(filePath, UriKind.Absolute);
            return uri;
        }
    }
}

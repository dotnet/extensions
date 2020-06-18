// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorFileChangeDetectorManager : IDisposable
    {
        private readonly IEnumerable<IFileChangeDetector> _fileChangeDetectors;
        private readonly object _disposeLock = new object();
        private bool _disposed;

        public RazorFileChangeDetectorManager(IEnumerable<IFileChangeDetector> fileChangeDetectors)
        {
            if (fileChangeDetectors is null)
            {
                throw new ArgumentNullException(nameof(fileChangeDetectors));
            }

            _fileChangeDetectors = fileChangeDetectors;
        }

        public async Task InitializedAsync(ILanguageServer languageServer)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            // Initialized request, this occurs once the server and client have agreed on what sort of features they both support. It only happens once.

            var workspaceDirectory = ResolveWorkspaceDirectory(languageServer.ClientSettings);

            foreach (var fileChangeDetector in _fileChangeDetectors)
            {
                // We create a dummy cancellation token for now. Have an issue to pass through the cancellation token in the O# lib: https://github.com/OmniSharp/csharp-language-server-protocol/issues/200
                var cancellationToken = CancellationToken.None;
                await fileChangeDetector.StartAsync(workspaceDirectory, cancellationToken);
            }

            lock (_disposeLock)
            {
                if (_disposed)
                {
                    // Got disposed while starting our file change detectors. We need to re-stop our change detectors.
                    Stop();
                }
            }
        }

        private void Stop()
        {
            foreach (var fileChangeDetector in _fileChangeDetectors)
            {
                fileChangeDetector.Stop();
            }
        }

        // Internal for testing
        internal static string ResolveWorkspaceDirectory(InitializeParams clientSettings)
        {
            if (clientSettings.RootUri == null)
            {
                // RootUri was added in LSP3, fallback to RootPath
                return clientSettings.RootPath;
            }

            return clientSettings.RootUri.LocalPath;
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                Stop();
            }
        }
    }
}

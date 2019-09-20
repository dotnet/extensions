// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultHostDocumentFactory : HostDocumentFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly GeneratedCodeContainerStore _generatedCodeContainerStore;

        public DefaultHostDocumentFactory(
            ForegroundDispatcher foregroundDispatcher,
            GeneratedCodeContainerStore generatedCodeContainerStore)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (generatedCodeContainerStore == null)
            {
                throw new ArgumentNullException(nameof(generatedCodeContainerStore));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _generatedCodeContainerStore = generatedCodeContainerStore;
        }

        public override HostDocument Create(string filePath, string targetFilePath)
            => Create(filePath, targetFilePath, fileKind: null);

        public override HostDocument Create(string filePath, string targetFilePath, string fileKind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (targetFilePath == null)
            {
                throw new ArgumentNullException(nameof(targetFilePath));
            }

            var hostDocument = new HostDocument(filePath, targetFilePath, fileKind);
            hostDocument.GeneratedCodeContainer.GeneratedCodeChanged += (sender, args) =>
            {
                var sharedContainer = _generatedCodeContainerStore.Get(filePath);
                var container = (GeneratedCodeContainer)sender;
                var latestDocument = (DefaultDocumentSnapshot)container.LatestDocument;
                Task.Factory.StartNew(async () =>
                {
                    var codeDocument = await latestDocument.GetGeneratedOutputAsync();

                    sharedContainer.SetOutput(
                        latestDocument,
                        codeDocument.GetCSharpDocument(),
                        container.InputVersion,
                        container.OutputVersion);
                }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.BackgroundScheduler);

            };

            return hostDocument;
        }
    }
}

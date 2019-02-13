// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;

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

        public override HostDocument Create(string documentFilePath, ProjectSnapshot projectSnapshot)
        {
            var engine = projectSnapshot.GetProjectEngine();
            var fileSystem = engine.FileSystem;
            var documentItem = fileSystem.GetItem(documentFilePath);
            var targetPath = documentItem.FilePath;

            // Representing all of our host documents with a re-normalized target path to workaround GetRelatedDocument limitations.
            var normalizedTargetPath = targetPath.Replace('/', '\\').TrimStart('\\');
            var hostDocument = new HostDocument(documentItem.PhysicalPath, normalizedTargetPath);
            hostDocument.GeneratedCodeContainer.GeneratedCodeChanged += (sender, args) =>
            {
                var sharedContainer = _generatedCodeContainerStore.Get(documentItem.PhysicalPath);
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

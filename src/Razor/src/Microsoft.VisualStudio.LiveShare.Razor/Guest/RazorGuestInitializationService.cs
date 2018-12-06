//// Copyright (c) .NET Foundation. All rights reserved.
//// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Cascade.Contracts;
//using Microsoft.VisualStudio.Cascade.Contracts;

//namespace Microsoft.VisualStudio.LiveShare.Razor.Host
//{
//    [ExportCollaborationService(typeof(ICollaborationService), Scope = SessionScope.Guest)]
//    internal class RazorGuestInitializationService : ICollaborationServiceFactory
//    {
//        private const string ViewImportsFileName = "_ViewImports.cshtml";
//        private readonly IVsRemoteWorkspaceManager _remoteWorkspaceManager;

//        [ImportingConstructor]
//        public RazorGuestInitializationService(IVsRemoteWorkspaceManager remoteWorkspaceManager)
//        {
//            if (remoteWorkspaceManager == null)
//            {
//                throw new ArgumentNullException(nameof(remoteWorkspaceManager));
//            }

//            _remoteWorkspaceManager = remoteWorkspaceManager;
//        }

//        public async Task<ICollaborationService> CreateServiceAsync(CollaborationSession sessionContext, CancellationToken cancellationToken)
//        {
//            if (sessionContext == null)
//            {
//                throw new ArgumentNullException(nameof(sessionContext));
//            }

//            await EnsureViewImportsCopiedAsync(cancellationToken);

//            // Our services don't actually have any state that needs to be disposed. 
//            return null;
//        }

//        // Today we ensure that all _ViewImports in the shared project exist on the guest because we don't currently track import documents
//        // in a manner that would allow us to retrieve/monitor that data across the wire. Once the Razor sub-system is moved to use
//        // DocumentSnapshots we'll be able to rely on that API to more properly manage files that impact parsing of Razor documents.
//        private async Task EnsureViewImportsCopiedAsync(CancellationToken cancellationToken)
//        {
//            var fileListOptions = new FileListOptions()
//            {
//                RecurseMode = FileRecurseMode.AllDescendants,
//                ExcludePatterns = new[]
//                            {
//                    "*.cs",
//                    "*.js",
//                    "*.css",
//                    "*.html",
//                    "*.json",
//                    "*.csproj",
//                    "*.sln",
//                    "bin",
//                    "obj",
//                }
//            };
//            var files = await _remoteWorkspaceManager.FileService.ListAsync(new[] { "/" }, fileListOptions, cancellationToken);

//            var copyTasks = new List<Task>();
//            StartViewImportsCopy(files, copyTasks, cancellationToken);

//            await Task.WhenAll(copyTasks);
//        }

//        private void StartViewImportsCopy(FileInfo[] files, List<Task> copyTasks, CancellationToken cancellationToken)
//        {
//            foreach (var file in files)
//            {
//                if (file.IsDirectory)
//                {
//                    StartViewImportsCopy(file.Children, copyTasks, cancellationToken);
//                }
//                else if (file.Path.EndsWith(ViewImportsFileName, StringComparison.OrdinalIgnoreCase))
//                {
//                    // A _ViewImports.cshtml file, need to ensure it's copied to the guest.
//                    var filePath = _remoteWorkspaceManager.ConvertToJoinerPath(file.Path, cancellationToken);
//                    var copyTask = _remoteWorkspaceManager.WorkspaceFileService.EnsureFileExistsAsync(filePath, cancellationToken);
//                    copyTasks.Add(copyTask);
//                }
//            }
//        }
//    }
//}

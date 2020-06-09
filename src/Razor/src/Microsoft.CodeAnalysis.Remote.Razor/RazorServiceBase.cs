// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal abstract class RazorServiceBase : ServiceHubServiceBase
    {
        public RazorServiceBase(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream, GetRazorConverters())
        {
            RazorServices = new RazorServices();

            // Due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // We need to manually start the RPC connection. Otherwise we'd be opting ourselves into 
            // race condition prone call paths.
            StartService();
        }

        protected RazorServices RazorServices { get; }

        protected virtual Task<ProjectSnapshot> GetProjectSnapshotAsync(ProjectSnapshotHandle projectHandle, CancellationToken cancellationToken)
        {
            if (projectHandle == null)
            {
                throw new ArgumentNullException(nameof(projectHandle));
            }

            return Task.FromResult<ProjectSnapshot>(new SerializedProjectSnapshot(projectHandle.FilePath, projectHandle.Configuration, projectHandle.RootNamespace));
        }

        private static IEnumerable<JsonConverter> GetRazorConverters()
        {
            var collection = new JsonConverterCollection();
            collection.RegisterRazorConverters();
            return collection;
        }

        private class SerializedProjectSnapshot : ProjectSnapshot
        {
            public SerializedProjectSnapshot(string filePath, RazorConfiguration configuration, string rootNamespace)
            {
                FilePath = filePath;
                Configuration = configuration;
                RootNamespace = rootNamespace;

                Version = VersionStamp.Default;
            }

            public override RazorConfiguration Configuration { get; }

            public override IEnumerable<string> DocumentFilePaths => Array.Empty<string>();

            public override string FilePath { get; }

            public override string RootNamespace { get; }

            public override VersionStamp Version { get; }

            public override DocumentSnapshot GetDocument(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                return null;
            }

            public override bool IsImportDocument(DocumentSnapshot document)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
            {
                throw new NotImplementedException();
            }

            public override RazorProjectEngine GetProjectEngine()
            {
                throw new NotImplementedException();
            }
        }
    }
}

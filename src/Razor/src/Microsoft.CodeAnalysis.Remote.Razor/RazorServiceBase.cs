// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal abstract class RazorServiceBase : IDisposable
    {
        protected readonly ServiceBrokerClient ServiceBrokerClient;

        public RazorServiceBase(IServiceBroker serviceBroker)
        {
            RazorServices = new RazorServices();

#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            ServiceBrokerClient = new ServiceBrokerClient(serviceBroker);
#pragma warning restore
        }

        protected RazorServices RazorServices { get; }

        public void Dispose()
        {
            ServiceBrokerClient.Dispose();
        }

        protected virtual Task<ProjectSnapshot> GetProjectSnapshotAsync(ProjectSnapshotHandle projectHandle, CancellationToken cancellationToken)
        {
            if (projectHandle == null)
            {
                throw new ArgumentNullException(nameof(projectHandle));
            }

            var snapshot = new SerializedProjectSnapshot(projectHandle.FilePath, projectHandle.Configuration, projectHandle.RootNamespace);
            return Task.FromResult<ProjectSnapshot>(snapshot);
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

            public override DocumentSnapshot? GetDocument(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                return null;
            }

            public override bool IsImportDocument(DocumentSnapshot document) => throw new NotImplementedException();

            public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document) => throw new NotImplementedException();

            public override RazorProjectEngine GetProjectEngine() => throw new NotImplementedException();
        }
    }
}

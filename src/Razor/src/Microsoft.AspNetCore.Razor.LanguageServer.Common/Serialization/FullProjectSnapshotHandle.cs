// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization
{
    // FullProjectSnapshotHandle exists in order to allow ProjectSnapshots to be serialized and then deserialized.
    // It has named "Full" because there's a similar concept in core Razor of a ProjectSnapshotHandle. In Razor's
    // case that handle doesn't contain ProjectWorkspaceState information
    internal sealed class FullProjectSnapshotHandle
    {
        public FullProjectSnapshotHandle(
            string filePath,
            RazorConfiguration configuration,
            string rootNamespace,
            ProjectWorkspaceState projectWorkspaceState,
            IReadOnlyList<DocumentSnapshotHandle> documents)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            FilePath = filePath;
            Configuration = configuration;
            RootNamespace = rootNamespace;
            ProjectWorkspaceState = projectWorkspaceState;
            Documents = documents;
        }

        public string FilePath { get; }

        public RazorConfiguration Configuration { get; }

        public string RootNamespace { get; }

        public ProjectWorkspaceState ProjectWorkspaceState { get; }

        public IReadOnlyList<DocumentSnapshotHandle> Documents { get; }
    }
}

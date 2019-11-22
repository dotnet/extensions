// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class OmniSharpProjectSnapshot
    {
        internal readonly ProjectSnapshot InternalProjectSnapshot;

        internal OmniSharpProjectSnapshot(ProjectSnapshot projectSnapshot)
        {
            InternalProjectSnapshot = projectSnapshot;
        }

        public string FilePath => InternalProjectSnapshot.FilePath;

        public IEnumerable<string> DocumentFilePaths => InternalProjectSnapshot.DocumentFilePaths;

        public RazorConfiguration Configuration => InternalProjectSnapshot.Configuration;

        public ProjectWorkspaceState ProjectWorkspaceState => InternalProjectSnapshot.ProjectWorkspaceState;

        public OmniSharpDocumentSnapshot GetDocument(string filePath)
        {
            var documentSnapshot = InternalProjectSnapshot.GetDocument(filePath);
            if (documentSnapshot == null)
            {
                return null;
            }

            var internalDocumentSnapshot = new OmniSharpDocumentSnapshot(documentSnapshot);
            return internalDocumentSnapshot;
        }

        internal static OmniSharpProjectSnapshot Convert(ProjectSnapshot projectSnapshot)
        {
            if (projectSnapshot == null)
            {
                return null;
            }

            return new OmniSharpProjectSnapshot(projectSnapshot);
        }

        public static OmniSharpProjectSnapshot CreateForTest(object projectSnapshot)
        {
            if (projectSnapshot is ProjectSnapshot stronglyTypedSnapshot)
            {
                return new OmniSharpProjectSnapshot(stronglyTypedSnapshot);
            }

            throw new ArgumentException("Snapshot is not of type " + typeof(ProjectSnapshot).FullName, nameof(projectSnapshot));
        }
    }
}

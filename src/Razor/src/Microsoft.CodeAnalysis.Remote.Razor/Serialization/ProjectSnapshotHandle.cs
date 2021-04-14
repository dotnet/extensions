// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal sealed class ProjectSnapshotHandle
    {
        public ProjectSnapshotHandle(
            string filePath, 
            RazorConfiguration configuration,
            string rootNamespace)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
            Configuration = configuration;
            RootNamespace = rootNamespace;
        }

        public RazorConfiguration Configuration { get; }

        public string FilePath { get; }

        public string RootNamespace { get; }
    }
}

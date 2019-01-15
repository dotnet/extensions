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
            RazorConfiguration configuration)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
            Configuration = configuration;
        }

        public RazorConfiguration Configuration { get; }

        public string FilePath { get; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Execution;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal class RazorFileChangeEventArgs : EventArgs
    {
        public RazorFileChangeEventArgs(
            string filePath,
            ProjectInstance projectInstance,
            RazorFileChangeKind kind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            FilePath = filePath;
            UnevaluatedProjectInstance = projectInstance;
            Kind = kind;
        }

        public string FilePath { get; }

        public ProjectInstance UnevaluatedProjectInstance { get; }

        public RazorFileChangeKind Kind { get; }
    }
}

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
            string relativeFilePath,
            ProjectInstance projectInstance,
            RazorFileChangeKind kind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (relativeFilePath == null)
            {
                throw new ArgumentNullException(nameof(relativeFilePath));
            }

            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            FilePath = filePath;
            RelativeFilePath = relativeFilePath;
            UnevaluatedProjectInstance = projectInstance;
            Kind = kind;
        }

        public string FilePath { get; }

        public string RelativeFilePath { get; }

        public ProjectInstance UnevaluatedProjectInstance { get; }

        public RazorFileChangeKind Kind { get; }
    }
}

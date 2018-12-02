// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectChangeEventArgs : EventArgs
    {
        [Obsolete("Adding this as a workaround to unblock live share")]
        public ProjectChangeEventArgs(string projectFilePath, ProjectChangeKind kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            Kind = kind;
        }

        [Obsolete("Adding this as a workaround to unblock live share")]
        public ProjectChangeEventArgs(string projectFilePath, string documentFilePath, ProjectChangeKind kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            DocumentFilePath = documentFilePath;
            Kind = kind;
        }

        public ProjectChangeEventArgs(ProjectSnapshot older, ProjectSnapshot newer, ProjectChangeKind kind)
        {
            if (older == null && newer == null)
            {
                throw new ArgumentException("Both projects cannot be null.");
            }

            Older = older;
            Newer = newer;
            Kind = kind;

            ProjectFilePath = older?.FilePath ?? newer.FilePath;
        }

        public ProjectChangeEventArgs(ProjectSnapshot older, ProjectSnapshot newer, string documentFilePath, ProjectChangeKind kind)
        {
            if (older == null && newer == null)
            {
                throw new ArgumentException("Both projects cannot be null.");
            }

            Older = older;
            Newer = newer;
            DocumentFilePath = documentFilePath;
            Kind = kind;

            ProjectFilePath = older?.FilePath ?? newer.FilePath;
        }

        public ProjectSnapshot Older { get; }

        public ProjectSnapshot Newer { get; }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public ProjectChangeKind Kind { get; }
    }
}

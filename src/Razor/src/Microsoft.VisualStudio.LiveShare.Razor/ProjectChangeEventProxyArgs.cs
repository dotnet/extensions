// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public sealed class ProjectChangeEventProxyArgs : EventArgs
    {
        public ProjectChangeEventProxyArgs(ProjectSnapshotHandleProxy older, ProjectSnapshotHandleProxy newer, ProjectProxyChangeKind kind)
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

        public ProjectSnapshotHandleProxy Older { get; }

        public ProjectSnapshotHandleProxy Newer { get; }

        public Uri ProjectFilePath { get; }

        public ProjectProxyChangeKind Kind { get; }
    }
}

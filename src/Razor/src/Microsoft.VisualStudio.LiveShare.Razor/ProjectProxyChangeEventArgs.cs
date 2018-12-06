// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public sealed class ProjectProxyChangeEventArgs
    {
        public ProjectProxyChangeEventArgs(
            Uri projectFilePath, 
            ProjectProxyChangeKind kind)
        {
            ProjectFilePath = projectFilePath;
            Kind = kind;
        }

        public Uri ProjectFilePath { get; }

        public ProjectProxyChangeKind Kind { get; }
    }
}

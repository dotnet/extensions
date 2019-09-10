// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public enum OmniSharpProjectChangeKind
    {
        ProjectAdded = ProjectChangeKind.ProjectAdded,
        ProjectRemoved = ProjectChangeKind.ProjectRemoved,
        ProjectChanged = ProjectChangeKind.ProjectChanged,
        DocumentAdded = ProjectChangeKind.DocumentAdded,
        DocumentRemoved = ProjectChangeKind.DocumentRemoved,
    }
}

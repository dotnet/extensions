// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal abstract class ProjectSnapshotFactory : ILanguageService
    {
        public abstract ProjectSnapshot Create(ProjectSnapshotHandleProxy projectHandle);
    }
}

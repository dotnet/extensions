// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal abstract class ProjectSnapshotHandleStore : ILanguageService
    {
        public abstract IReadOnlyList<ProjectSnapshotHandleProxy> GetProjectHandles();

        public abstract event EventHandler<ProjectProxyChangeEventArgs> Changed;
    }
}

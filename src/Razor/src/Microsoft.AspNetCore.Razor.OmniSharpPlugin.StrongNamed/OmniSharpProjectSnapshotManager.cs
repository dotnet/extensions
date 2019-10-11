// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class OmniSharpProjectSnapshotManager
    {
        public abstract event EventHandler<OmniSharpProjectChangeEventArgs> Changed;

        public abstract IReadOnlyList<OmniSharpProjectSnapshot> Projects { get; }

        public abstract OmniSharpProjectSnapshot GetLoadedProject(string filePath);
    }
}

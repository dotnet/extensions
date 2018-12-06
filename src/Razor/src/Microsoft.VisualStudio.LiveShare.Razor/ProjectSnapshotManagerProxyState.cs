// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public sealed class ProjectSnapshotManagerProxyState
    {
        public ProjectSnapshotManagerProxyState(IReadOnlyList<ProjectSnapshotHandleProxy> projectHandles)
        {
            if (projectHandles == null)
            {
                throw new ArgumentNullException(nameof(projectHandles));
            }

            ProjectHandles = projectHandles;
        }

        public IReadOnlyList<ProjectSnapshotHandleProxy> ProjectHandles { get; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public sealed class ProjectManagerProxyChangeEventArgs : EventArgs
    {
        public ProjectManagerProxyChangeEventArgs(
            ProjectProxyChangeEventArgs change,
            ProjectSnapshotManagerProxyState state)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Change = change;
            State = state;
        }

        public ProjectProxyChangeEventArgs Change { get; }

        public ProjectSnapshotManagerProxyState State { get; }
    }
}

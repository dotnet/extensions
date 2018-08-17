// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    // Only for testing

    internal class TestProjectSnapshotManager : DefaultProjectSnapshotManager
    {
        public TestProjectSnapshotManager(DefaultForegroundDispatcherShim foregroundDispatcher, Workspace workspace)
            : base(foregroundDispatcher.InnerForegroundDispatcher, new DefaultErrorReporter(), Enumerable.Empty<ProjectSnapshotChangeTrigger>(), workspace)
        {
        }

        public bool AllowNotifyListeners { get; set; }

        protected override void NotifyListeners(ProjectChangeEventArgs e)
        {
            if (AllowNotifyListeners)
            {
                base.NotifyListeners(e);
            }
        }
    }
}

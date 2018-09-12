// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    internal class TestProjectSnapshotManager : DefaultProjectSnapshotManager
    {
        private TestProjectSnapshotManager(ForegroundDispatcher dispatcher, Workspace workspace)
            : base(dispatcher, new DefaultErrorReporter(), Enumerable.Empty<ProjectSnapshotChangeTrigger>(), workspace)
        {

        }

        public static TestProjectSnapshotManager Create(ForegroundDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            var defaultAccessor = new DefaultProjectSnapshotManagerAccessor(dispatcher, Enumerable.Empty<ProjectSnapshotChangeTrigger>());
            var workspace = defaultAccessor.Instance.Workspace;
            var testProjectManager = new TestProjectSnapshotManager(dispatcher, workspace);

            return testProjectManager;
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

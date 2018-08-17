// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class TestProjectSnapshotManager
    {
        public static ProjectSnapshotManagerShim Create(ForegroundDispatcherShim dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            // Need to rely on reflection to create a test project snapshot manager that can be controlled
            // to not spit out excess notifications.
            var assembly = Assembly.Load("Microsoft.AspnetCore.Razor.LanguageServer.StrongNamed");
            var defaultAccessorType = assembly.GetType("Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed.DefaultProjectSnapshotManagerShimAccessor");
            var accessor = (ProjectSnapshotManagerShimAccessor)Activator.CreateInstance(defaultAccessorType, dispatcher, Enumerable.Empty<ProjectSnapshotChangeTriggerShim>());
            var workspace = accessor.Instance.Workspace;
            var testProjectManagerType = assembly.GetType("Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed.TestProjectSnapshotManager");
            var testProjectManager = Activator.CreateInstance(testProjectManagerType, dispatcher, workspace);
            var defaultSnapshotManagerType = assembly.GetType("Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed.DefaultProjectSnapshotManagerShim");
            var projectManager = (ProjectSnapshotManagerShim)Activator.CreateInstance(defaultSnapshotManagerType, testProjectManager);

            return projectManager;
        }

        private TestProjectSnapshotManager()
        {
        }
    }
}

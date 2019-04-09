// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class OmniSharpTestBase : TestBase
    {
        private readonly MethodInfo _createTestProjectSnapshotMethod;
        private readonly MethodInfo _createWithDocumentsTestProjectSnapshotMethod;
        private readonly MethodInfo _createProjectSnapshotManagerMethod;
        private readonly PropertyInfo _allowNotifyListenersProperty;
        private readonly PropertyInfo _dispatcherProperty;
        private readonly ConstructorInfo _omniSharpProjectSnapshotMangerConstructor;
        private readonly ConstructorInfo _omniSharpSnapshotConstructor;

        public OmniSharpTestBase()
        {
            var commonTestAssembly = Assembly.Load("Microsoft.AspNetCore.Razor.Test.Common");
            var testProjectSnapshotType = commonTestAssembly.GetType("Microsoft.AspNetCore.Razor.Test.Common.TestProjectSnapshot");
            var testProjectSnapshotManagerType = commonTestAssembly.GetType("Microsoft.AspNetCore.Razor.Test.Common.TestProjectSnapshotManager");
            var strongNamedAssembly = Assembly.Load("Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed");
            var defaultSnapshotManagerType = strongNamedAssembly.GetType("Microsoft.AspNetCore.Razor.OmniSharpPlugin.DefaultOmniSharpProjectSnapshotManager");

            _createTestProjectSnapshotMethod = testProjectSnapshotType.GetMethod("Create", new[] { typeof(string) });
            _createWithDocumentsTestProjectSnapshotMethod = testProjectSnapshotType.GetMethod("Create", new[] { typeof(string), typeof(string[]) });
            _createProjectSnapshotManagerMethod = testProjectSnapshotManagerType.GetMethod("Create");
            _allowNotifyListenersProperty = testProjectSnapshotManagerType.GetProperty("AllowNotifyListeners");
            _dispatcherProperty = typeof(OmniSharpForegroundDispatcher).GetProperty("InternalDispatcher", BindingFlags.NonPublic | BindingFlags.Instance);
            _omniSharpProjectSnapshotMangerConstructor = defaultSnapshotManagerType.GetConstructors().Single();
            _omniSharpSnapshotConstructor = typeof(OmniSharpProjectSnapshot).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            Dispatcher = new DefaultOmniSharpForegroundDispatcher();
        }

        protected OmniSharpForegroundDispatcher Dispatcher { get; }

        protected OmniSharpProjectSnapshot CreateProjectSnapshot(string projectFilePath)
        {
            var projectSnapshot = _createTestProjectSnapshotMethod.Invoke(null, new[] { projectFilePath });
            var omniSharpProjectSnapshot = (OmniSharpProjectSnapshot)_omniSharpSnapshotConstructor.Invoke(new[] { projectSnapshot });

            return omniSharpProjectSnapshot;
        }

        protected OmniSharpProjectSnapshot CreateProjectSnapshot(string projectFilePath, string[] documentFilePaths)
        {
            var projectSnapshot = _createWithDocumentsTestProjectSnapshotMethod.Invoke(null, new object[] { projectFilePath, documentFilePaths });
            var omniSharpProjectSnapshot = (OmniSharpProjectSnapshot)_omniSharpSnapshotConstructor.Invoke(new[] { projectSnapshot });

            return omniSharpProjectSnapshot;
        }

        protected OmniSharpProjectSnapshotManagerBase CreateProjectSnapshotManager(bool allowNotifyListeners = false)
        {
            var dispatcher = _dispatcherProperty.GetValue(Dispatcher);
            var testSnapshotManager = _createProjectSnapshotManagerMethod.Invoke(null, new object[] { dispatcher });
            _allowNotifyListenersProperty.SetValue(testSnapshotManager, allowNotifyListeners);
            var snapshotManager = (OmniSharpProjectSnapshotManagerBase)_omniSharpProjectSnapshotMangerConstructor.Invoke(new[] { testSnapshotManager });

            return snapshotManager;
        }

        protected Task RunOnForegroundAsync(Action action)
        {
            return Task.Factory.StartNew(
                () => action(),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }

        protected Task<TReturn> RunOnForegroundAsync<TReturn>(Func<TReturn> action)
        {
            return Task.Factory.StartNew(
                () => action(),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }

        protected Task RunOnForegroundAsync(Func<Task> action)
        {
            return Task.Factory.StartNew(
                async () => await action(),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }
    }
}

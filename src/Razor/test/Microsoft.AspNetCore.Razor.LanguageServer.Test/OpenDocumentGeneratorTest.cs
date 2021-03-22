// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class OpenDocumentGeneratorTest : LanguageServerTestBase
    {
        public OpenDocumentGeneratorTest()
        {
            Documents = new HostDocument[]
            {
                new HostDocument("c:/Test1/Index.cshtml", "Index.cshtml"),
                new HostDocument("c:/Test1/Components/Counter.cshtml", "Components/Counter.cshtml"),
            };

            HostProject1 = new HostProject("c:/Test1/Test1.csproj", RazorConfiguration.Default, "TestRootNamespace");
            HostProject2 = new HostProject("c:/Test2/Test2.csproj", RazorConfiguration.Default, "TestRootNamespace");
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject1 { get; }

        private HostProject HostProject2 { get; }

        [Fact]
        public void Enqueue_IgnoresClosedDocuments()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new TestOpenDocumentGenerator(Dispatcher);

            queue.Initialize(projectManager);

            // Act & Assert
            queue.Enqueue(project.GetDocument(Documents[0].FilePath));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have anything pending");
        }

        [Fact]
        public void Enqueue_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[0].FilePath, SourceText.From(string.Empty));
            projectManager.DocumentAdded(HostProject1, Documents[1], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[1].FilePath, SourceText.From(string.Empty));

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new TestOpenDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            queue.Initialize(projectManager);

            // Act & Assert
            queue.Enqueue(project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();
            queue.BlockBackgroundWorkCompleting.Set();

            Assert.True(queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)), "Timed out waiting for background work to complete");

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [Fact]
        public void Enqueue_ProcessesNotifications_AndRestarts()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[0].FilePath, SourceText.From(string.Empty));
            projectManager.DocumentAdded(HostProject1, Documents[1], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[1].FilePath, SourceText.From(string.Empty));

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new TestOpenDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundCapturedWorkload = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            queue.Initialize(projectManager);

            // Act & Assert
            queue.Enqueue(project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to start.
            queue.BlockBackgroundWorkStart.Set();

            Assert.True(queue.NotifyBackgroundWorkStarting.Wait(TimeSpan.FromSeconds(1)), "Timed out waiting for background work to start");
            Assert.True(queue.IsScheduledOrRunning, "Worker should be processing now");

            Assert.True(queue.NotifyBackgroundCapturedWorkload.Wait(TimeSpan.FromSeconds(1)), "Timed out waiting for background work to be captured");
            Assert.False(queue.HasPendingNotifications, "Worker should have taken all notifications");

            queue.Enqueue(project.GetDocument(Documents[1].FilePath));
            Assert.True(queue.HasPendingNotifications); // Now we should see the worker restart when it finishes.

            // Allow work to complete, which should restart the timer.
            queue.BlockBackgroundWorkCompleting.Set();

            Assert.True(queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)), "Timed out waiting for background work to complete");
            queue.NotifyBackgroundWorkCompleted.Reset();

            // It should start running again right away.
            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            queue.BlockBackgroundWorkCompleting.Set();
            Assert.True(queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)), "Timed out waiting for background work to complete again");

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        // The below are more like integration tests where notifications start from the project snapshot manager and we ensure things flow as expected.

        [Fact]
        public void ProjectChanged_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[0].FilePath, SourceText.From(string.Empty));
            projectManager.AllowNotifyListeners = true;

            var queue = new TestOpenDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };
            queue.Initialize(projectManager);

            var newWorkspaceState = new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp7_3);


            // Act & Assert
            projectManager.ProjectWorkspaceStateChanged(HostProject1.FilePath, newWorkspaceState);

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();
            queue.BlockBackgroundWorkCompleting.Set();

            Assert.True(queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [Fact]
        public void DocumentAdded_IgnoresClosedDocument()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.AllowNotifyListeners = true;

            var queue = new TestOpenDocumentGenerator(Dispatcher);
            queue.Initialize(projectManager);

            // Act & Assert
            projectManager.DocumentAdded(HostProject1, Documents[0], null);

            Assert.False(queue.IsScheduledOrRunning, "Queue should not be scheduled when a document is added for the first time (closed by default)");
        }

        [Fact]
        public void DocumentChanged_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[0].FilePath, SourceText.From(string.Empty));
            projectManager.AllowNotifyListeners = true;

            var queue = new TestOpenDocumentGenerator(Dispatcher)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };
            queue.Initialize(projectManager);

            // Act & Assert
            projectManager.DocumentChanged(HostProject1.FilePath, Documents[0].FilePath, SourceText.From("Changed"));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();
            queue.BlockBackgroundWorkCompleting.Set();

            Assert.True(queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [Fact]
        public void DocumentRemoved_IgnoresClosedDocument()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.ProjectAdded(HostProject1);
            projectManager.ProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentOpened(HostProject1.FilePath, Documents[0].FilePath, SourceText.From(string.Empty));
            projectManager.AllowNotifyListeners = true;

            var queue = new TestOpenDocumentGenerator(Dispatcher);
            queue.Initialize(projectManager);

            // Act & Assert
            projectManager.DocumentRemoved(HostProject1, Documents[0]);

            Assert.False(queue.IsScheduledOrRunning, "Queue should not be scheduled when a document is added for the first time (closed by default)");
        }

        private class TestOpenDocumentGenerator : OpenDocumentGenerator
        {
            public TestOpenDocumentGenerator(ForegroundDispatcher foregroundDispatcher) : base(foregroundDispatcher)
            {
            }
        }
    }
}

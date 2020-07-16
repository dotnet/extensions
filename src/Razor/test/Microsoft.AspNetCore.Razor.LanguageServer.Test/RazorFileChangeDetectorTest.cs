// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorFileChangeDetectorTest : LanguageServerTestBase
    {
        [Fact]
        public async Task StartAsync_NotifiesListenersOfExistingRazorFiles()
        {
            // Arrange
            var args1 = new List<(string FilePath, RazorFileChangeKind Kind)>();
            var listener1 = new Mock<IRazorFileChangeListener>(MockBehavior.Strict);
            listener1.Setup(l => l.RazorFileChanged(It.IsAny<string>(), It.IsAny<RazorFileChangeKind>()))
                .Callback<string, RazorFileChangeKind>((filePath, kind) => args1.Add((filePath, kind)));
            var args2 = new List<(string FilePath, RazorFileChangeKind Kind)>();
            var listener2 = new Mock<IRazorFileChangeListener>(MockBehavior.Strict);
            listener2.Setup(l => l.RazorFileChanged(It.IsAny<string>(), It.IsAny<RazorFileChangeKind>()))
                .Callback<string, RazorFileChangeKind>((filePath, kind) => args2.Add((filePath, kind)));
            var existingRazorFiles = new[] { "c:/path/to/index.razor", "c:/other/path/_Host.cshtml" };
            var cts = new CancellationTokenSource();
            var detector = new TestRazorFileChangeDetector(
                cts,
                Dispatcher,
                new[] { listener1.Object, listener2.Object },
                existingRazorFiles);

            // Act
            await detector.StartAsync("/some/workspacedirectory", cts.Token);

            // Assert
            Assert.Collection(args1,
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingRazorFiles[0], args.FilePath);
                },
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingRazorFiles[1], args.FilePath);
                });
            Assert.Collection(args2,
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingRazorFiles[0], args.FilePath);
                },
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingRazorFiles[1], args.FilePath);
                });
        }

        [Fact]
        public async Task FileSystemWatcher_RazorFileEvent_Background_NotifiesChange()
        {
            // Arrange
            var filePath = "C:/path/to/file.razor";
            var changeKind = RazorFileChangeKind.Added;
            var listener = new Mock<IRazorFileChangeListener>(MockBehavior.Strict);
            listener.Setup(l => l.RazorFileChanged(filePath, changeKind)).Verifiable();
            var fileChangeDetector = new RazorFileChangeDetector(Dispatcher, FilePathNormalizer, new[] { listener.Object })
            {
                EnqueueDelay = 50,
                BlockNotificationWorkStart = new ManualResetEventSlim(initialState: false),
            };

            // Act
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, changeKind);

            // Assert

            // We acquire the notification prior to unblocking notification work because once we allow that work to proceed the notification will be removed.
            var notification = Assert.Single(fileChangeDetector._pendingNotifications);

            fileChangeDetector.BlockNotificationWorkStart.Set();

            await notification.Value.NotifyTask;

            listener.VerifyAll();
        }

        [Fact]
        public void FileSystemWatcher_RazorFileEvent_Background_AddRemoveDoesNotNotify()
        {
            // Arrange
            var filePath = "C:/path/to/file.razor";
            var listenerCalled = false;
            var listener = new Mock<IRazorFileChangeListener>(MockBehavior.Strict);
            listener.Setup(l => l.RazorFileChanged(filePath, It.IsAny<RazorFileChangeKind>())).Callback(() => listenerCalled = true);
            var fileChangeDetector = new RazorFileChangeDetector(Dispatcher, FilePathNormalizer, new[] { listener.Object })
            {
                EnqueueDelay = 50,
                NotifyNotificationNoop = new ManualResetEventSlim(initialState: false),
                BlockNotificationWorkStart = new ManualResetEventSlim(initialState: false)
            };

            // Act
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, RazorFileChangeKind.Added);
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, RazorFileChangeKind.Removed);

            // Assert
            fileChangeDetector.BlockNotificationWorkStart.Set();
            Assert.True(fileChangeDetector.NotifyNotificationNoop.Wait(TimeSpan.FromSeconds(10)));
            Assert.False(listenerCalled);
        }

        [Fact]
        public async Task FileSystemWatcher_RazorFileEvent_Background_NotificationNoopToAdd_NotifiesAddedOnce()
        {
            // Arrange
            var filePath = "C:/path/to/file.razor";
            var listener = new Mock<IRazorFileChangeListener>(MockBehavior.Strict);
            var callCount = 0;
            listener.Setup(l => l.RazorFileChanged(filePath, RazorFileChangeKind.Added)).Callback(() => callCount++);
            var fileChangeDetector = new RazorFileChangeDetector(Dispatcher, FilePathNormalizer, new[] { listener.Object })
            {
                EnqueueDelay = 50,
                BlockNotificationWorkStart = new ManualResetEventSlim(initialState: false),
            };

            // Act
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, RazorFileChangeKind.Added);
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, RazorFileChangeKind.Removed);
            fileChangeDetector.FileSystemWatcher_RazorFileEvent_Background(filePath, RazorFileChangeKind.Added);

            // Assert

            // We acquire the notification prior to unblocking notification work because once we allow that work to proceed the notification will be removed.
            var notification = Assert.Single(fileChangeDetector._pendingNotifications);

            fileChangeDetector.BlockNotificationWorkStart.Set();

            await notification.Value.NotifyTask;

            Assert.Equal(1, callCount);
        }

        private class TestRazorFileChangeDetector : RazorFileChangeDetector
        {
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly IReadOnlyList<string> _existingProjectFiles;

            public TestRazorFileChangeDetector(
                CancellationTokenSource cancellationTokenSource,
                ForegroundDispatcher foregroundDispatcher,
                IEnumerable<IRazorFileChangeListener> listeners,
                IReadOnlyList<string> existingprojectFiles) : base(foregroundDispatcher, new FilePathNormalizer(), listeners)
            {
                _cancellationTokenSource = cancellationTokenSource;
                _existingProjectFiles = existingprojectFiles;
            }

            protected override void OnInitializationFinished()
            {
                // Once initialization has finished we want to ensure that no file watchers are created so cancel!
                _cancellationTokenSource.Cancel();
            }

            protected override IReadOnlyList<string> GetExistingRazorFiles(string workspaceDirectory)
            {
                return _existingProjectFiles;
            }
        }
    }
}

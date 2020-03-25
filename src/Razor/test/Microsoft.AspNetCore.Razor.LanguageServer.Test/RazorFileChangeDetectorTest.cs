// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var existingRazorFiles = new[] { "/c:/path/to/index.razor", "/c:/other/path/_Host.cshtml" };
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

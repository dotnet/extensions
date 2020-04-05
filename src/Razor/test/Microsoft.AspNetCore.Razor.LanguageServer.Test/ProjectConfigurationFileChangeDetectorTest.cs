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
    public class ProjectConfigurationFileChangeDetectorTest : LanguageServerTestBase
    {
        [Fact]
        public async Task StartAsync_NotifiesListenersOfExistingConfigurationFiles()
        {
            // Arrange
            var eventArgs1 = new List<ProjectConfigurationFileChangeEventArgs>();
            var listener1 = new Mock<IProjectConfigurationFileChangeListener>(MockBehavior.Strict);
            listener1.Setup(l => l.ProjectConfigurationFileChanged(It.IsAny<ProjectConfigurationFileChangeEventArgs>()))
                .Callback<ProjectConfigurationFileChangeEventArgs>(args => eventArgs1.Add(args));
            var eventArgs2 = new List<ProjectConfigurationFileChangeEventArgs>();
            var listener2 = new Mock<IProjectConfigurationFileChangeListener>(MockBehavior.Strict);
            listener2.Setup(l => l.ProjectConfigurationFileChanged(It.IsAny<ProjectConfigurationFileChangeEventArgs>()))
                .Callback<ProjectConfigurationFileChangeEventArgs>(args => eventArgs2.Add(args));
            var existingConfigurationFiles = new[] { "c:/path/to/project.razor.json", "c:/other/path/project.razor.json" };
            var cts = new CancellationTokenSource();
            var detector = new TestProjectConfigurationFileChangeDetector(
                cts,
                Dispatcher,
                new[] { listener1.Object, listener2.Object },
                existingConfigurationFiles);

            // Act
            await detector.StartAsync("/some/workspacedirectory", cts.Token);

            // Assert
            Assert.Collection(eventArgs1,
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingConfigurationFiles[0], args.ConfigurationFilePath);
                },
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingConfigurationFiles[1], args.ConfigurationFilePath);
                });
            Assert.Collection(eventArgs2,
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingConfigurationFiles[0], args.ConfigurationFilePath);
                },
                args =>
                {
                    Assert.Equal(RazorFileChangeKind.Added, args.Kind);
                    Assert.Equal(existingConfigurationFiles[1], args.ConfigurationFilePath);
                });
        }

        private class TestProjectConfigurationFileChangeDetector : ProjectConfigurationFileChangeDetector
        {
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly IReadOnlyList<string> _existingConfigurationFiles;

            public TestProjectConfigurationFileChangeDetector(
                CancellationTokenSource cancellationTokenSource,
                ForegroundDispatcher foregroundDispatcher,
                IEnumerable<IProjectConfigurationFileChangeListener> listeners,
                IReadOnlyList<string> existingConfigurationFiles) : base(foregroundDispatcher, new FilePathNormalizer(), listeners)
            {
                _cancellationTokenSource = cancellationTokenSource;
                _existingConfigurationFiles = existingConfigurationFiles;
            }

            protected override void OnInitializationFinished()
            {
                // Once initialization has finished we want to ensure that no file watchers are created so cancel!
                _cancellationTokenSource.Cancel();
            }

            protected override IReadOnlyList<string> GetExistingConfigurationFiles(string workspaceDirectory)
            {
                return _existingConfigurationFiles;
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class GuestProjectSnapshotFactoryTest
    {
        [Fact]
        public async Task Create_ConvertsFromHandleToSnapshotAsync()
        {
            // Arrange
            var configuration = RazorConfiguration.Create(RazorLanguageVersion.Version_1_1, "TestConfiguration", Enumerable.Empty<RazorExtension>());
            var expectedTagHelpers = new[] { TagHelperDescriptorBuilder.Create("test1", "TestAssembly1").Build() };
            var projectHandle = new ProjectSnapshotHandleProxy(new Uri("vsls:/path/project.csproj"), expectedTagHelpers, configuration);
            var collabSession = new TestCollaborationSession(isHost: false);
            var liveShareClientProvider = new LiveShareClientProvider();
            await liveShareClientProvider.CreateServiceAsync(collabSession, CancellationToken.None);
            var workspace = TestWorkspace.Create();
            var factory = new GuestProjectSnapshotFactory(workspace, liveShareClientProvider);

            // Act
            var project = factory.Create(projectHandle);

            // Assert
            Assert.Empty(project.DocumentFilePaths);
            Assert.Equal(configuration, project.Configuration);
            Assert.Equal("/guest/path/project.csproj", project.FilePath);
            Assert.True(project.TryGetTagHelpers(out var tagHelpers));
            Assert.Equal(expectedTagHelpers, tagHelpers);
        }
    }
}

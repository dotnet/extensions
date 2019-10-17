// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorProjectEndpointTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_UpdateProject_NoProjectSnapshotHandle_Noops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var endpoint = new RazorProjectEndpoint(Dispatcher, projectService.Object, LoggerFactory);
            var request = new RazorUpdateProjectParams()
            {
                ProjectSnapshotHandle = null,
            };

            // Act & Assert
            await endpoint.Handle(request, CancellationToken.None);
        }
    }
}

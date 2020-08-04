// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class RazorGuestInitializationServiceTest
    {
        public RazorGuestInitializationServiceTest()
        {
            LiveShareSessionAccessor = new DefaultLiveShareSessionAccessor();
        }

        private DefaultLiveShareSessionAccessor LiveShareSessionAccessor { get; }

        [Fact]
        public async Task CreateServiceAsync_StartsViewImportsCopy()
        {
            // Arrange
            var service = new RazorGuestInitializationService(LiveShareSessionAccessor);
            var session = new Mock<CollaborationSession>();
            session.Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Array.Empty<Uri>()))
                .Verifiable();

            // Act
            await service.CreateServiceAsync(session.Object, default);

            // Assert
            Assert.NotNull(service._viewImportsCopyTask);
            await service._viewImportsCopyTask;

            session.VerifyAll();
        }

        [Fact]
        public async Task CreateServiceAsync_SessionDispose_CancelsListRootsToken()
        {
            // Arrange
            var service = new RazorGuestInitializationService(LiveShareSessionAccessor);
            var session = new Mock<CollaborationSession>();
            var disposedService = false;
            IDisposable sessionService = null;
            session.Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>((cancellationToken) =>
                {
                    return Task.Run(() =>
                    {
                        cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        Assert.True(disposedService);
                        return Array.Empty<Uri>();
                    });
                })
                .Verifiable();
            sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, default);

            // Act
            sessionService.Dispose();
            disposedService = true;

            // Assert
            Assert.NotNull(service._viewImportsCopyTask);
            await service._viewImportsCopyTask;

            session.VerifyAll();
        }

        [Fact]
        public async Task CreateServiceAsync_InitializationDispose_CancelsListRootsToken()
        {
            // Arrange
            var service = new RazorGuestInitializationService(LiveShareSessionAccessor);
            var session = new Mock<CollaborationSession>();
            using var cts = new CancellationTokenSource();
            IDisposable sessionService = null;
            session.Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>((cancellationToken) =>
                {
                    return Task.Run(() =>
                    {
                        cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        Assert.True(cts.IsCancellationRequested);
                        return Array.Empty<Uri>();
                    });
                })
                .Verifiable();
            sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, cts.Token);

            // Act
            cts.Cancel();

            // Assert
            Assert.NotNull(service._viewImportsCopyTask);
            await service._viewImportsCopyTask;

            session.VerifyAll();
        }

        [Fact]
        public async Task CreateServiceAsync_EnsureViewImportsCopiedAsync_CancellationExceptionsGetSwallowed()
        {
            // Arrange
            var service = new RazorGuestInitializationService(LiveShareSessionAccessor);
            var session = new Mock<CollaborationSession>();
            using var cts = new CancellationTokenSource();
            IDisposable sessionService = null;
            session.Setup(s => s.ListRootsAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>((cancellationToken) =>
                {
                    return Task.Run(() =>
                    {
                        cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        cancellationToken.ThrowIfCancellationRequested();

                        return Array.Empty<Uri>();
                    });
                })
                .Verifiable();
            sessionService = (IDisposable)await service.CreateServiceAsync(session.Object, cts.Token);

            // Act
            cts.Cancel();

            // Assert
            Assert.NotNull(service._viewImportsCopyTask);
            await service._viewImportsCopyTask;

            session.VerifyAll();
        }
    }
}

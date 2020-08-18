// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Nerdbank.Streams;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class AutoFlushingNerdbankStreamTest
    {
        [Fact]
        public async void Simultaneous_ReadWrite_ClientServer_SingleThreads()
        {
            // Arrange
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            using var autoFlushingStream = new AutoFlushingNerdbankStream(serverStream);
            const int INIT_BYTES = 10000;
            var fileContents = new byte[INIT_BYTES];
            var randomGenerator = new Random();
            randomGenerator.NextBytes(fileContents);
            await clientStream.WriteAsync(fileContents, 0, INIT_BYTES);
            await clientStream.FlushAsync();
            await serverStream.WriteAsync(fileContents, 0, INIT_BYTES);
            await serverStream.FlushAsync();

            // Act
            var serverReads = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    try
                    {
                        var tmpBuffer = new byte[10];
                        var result = autoFlushingStream.Read(tmpBuffer, 0, 10);
                        Assert.Equal(10, result);
                    }
                    catch
                    {
                        // Force throw assertion failure when exception is raised
                        // Test should fail on exception, however this doesn't actually
                        // happen here. Likely due to running on a seperate thread.
                        Assert.True(false);
                        break;
                    }
                }
            });

            var serverWrites = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    try
                    {
                        var tmpBuffer = new byte[10];
                        randomGenerator.NextBytes(tmpBuffer);
                        autoFlushingStream.Write(tmpBuffer, 0, 10);
                    }
                    catch
                    {
                        // Force throw assertion failure when exception is raised
                        // Test should fail on exception, however this doesn't actually
                        // happen here. Likely due to running on a seperate thread.
                        Assert.True(false);
                        break;
                    }
                }
            });

            var clientReads = Task.Run(async () =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    var tmpBuffer = new byte[10];
                    var result = await clientStream.ReadAsync(tmpBuffer, 0, 10);
                    Assert.Equal(10, result);
                }
            });

            var clientWrites = Task.Run(async () =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    var tmpBuffer = new byte[10];
                    randomGenerator.NextBytes(tmpBuffer);
                    await clientStream.WriteAsync(tmpBuffer, 0, 10);
                    await clientStream.FlushAsync();
                }
            });

            var clientReadsException = await Record.ExceptionAsync(async () => await clientReads);
            var clientWritesException = await Record.ExceptionAsync(async () => await clientWrites);

            // Assert
            Assert.Null(clientReadsException);
            Assert.Null(clientWritesException);
            Task.WaitAll(new[] { serverReads, serverWrites });
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    public class AzureBlobSinkTests
    {
        DateTimeOffset _timestampOne = new DateTimeOffset(2016, 05, 04, 03, 02, 01, TimeSpan.Zero);

        [Fact]
        public async Task WritesMessagesInBatches()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            blob.Setup(b => b.OpenWriteAsync(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestBlobSink(name => blob.Object);
            var logger = (BatchingLogger)sink.CreateLogger("Cat");

            await sink.IntervalControl.Pause;

            for (int i = 0; i < 5; i++)
            {
                logger.Log(_timestampOne, LogLevel.Information, 0, "Text " + i, null, (state, ex) => state);
            }

            sink.IntervalControl.Resume();
            await sink.IntervalControl.Pause;

            Assert.Equal(1, buffers.Count);
            Assert.Equal(
                "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 0" + Environment.NewLine +
                "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 1" + Environment.NewLine +
                "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 2" + Environment.NewLine +
                "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 3" + Environment.NewLine +
                "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 4" + Environment.NewLine,
                Encoding.UTF8.GetString(buffers[0]));
        }

        [Fact]
        public async Task GroupsByHour()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            var names = new List<string>();

            blob.Setup(b => b.OpenWriteAsync(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestBlobSink(name =>
            {
                names.Add(name);
                return blob.Object;
            });
            var logger = (BatchingLogger)sink.CreateLogger("Cat");

            await sink.IntervalControl.Pause;

            var startDate = _timestampOne;
            for (int i = 0; i < 3; i++)
            {
                logger.Log(startDate, LogLevel.Information, 0, "Text " + i, null, (state, ex) => state);

                startDate = startDate.AddHours(1);
            }

            sink.IntervalControl.Resume();
            await sink.IntervalControl.Pause;

            Assert.Equal(3, buffers.Count);

            Assert.Equal("appname/2016/05/04/03/42_filename", names[0]);
            Assert.Equal("appname/2016/05/04/04/42_filename", names[1]);
            Assert.Equal("appname/2016/05/04/05/42_filename", names[2]);
        }

        [Fact]
        public async Task CreatesBlobIfNotExists()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            bool created = false;

            blob.Setup(b => b.OpenWriteAsync(It.IsAny<CancellationToken>())).Returns(() =>
            {
                if (!created)
                {
                    throw new StorageException(new RequestResult() { HttpStatusCode = 404 }, string.Empty, null);
                }
                return Task.FromResult((Stream)new TestMemoryStream(buffers));
            });

            blob.Setup(b => b.CreateAsync(It.IsAny<CancellationToken>())).Returns(() =>
            {
                created = true;
                return Task.FromResult(0);
            });

            var sink = new TestBlobSink(name => blob.Object);
            var logger = (BatchingLogger)sink.CreateLogger("Cat");

            await sink.IntervalControl.Pause;

            logger.Log(_timestampOne, LogLevel.Information, 0, "Text", null, (state, ex) => state);


            sink.IntervalControl.Resume();
            await sink.IntervalControl.Pause;

            Assert.Equal(1, buffers.Count);
            Assert.True(created);
        }

        private class TestMemoryStream : MemoryStream
        {
            public List<byte[]> Buffers { get; }

            public TestMemoryStream(List<byte[]> buffers)
            {
                Buffers = buffers;
            }

            protected override void Dispose(bool disposing)
            {
                Buffers.Add(ToArray());
                base.Dispose(disposing);
            }
        }
    }
}

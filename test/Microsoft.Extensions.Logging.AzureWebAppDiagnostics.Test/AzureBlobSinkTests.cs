// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureWebApps.Test
{
    public class AzureBlobSinkTests
    {
        [Fact]
        public async Task WritesMessagesInBatches()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            blob.Setup(b => b.OpenWriteAsync()).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestAzureBlobSink(name => blob.Object);

            var events = new List<LogEvent>();

            for (int i = 0; i < 5; i++)
            {
                events.Add(CreateEvent(DateTime.Now, "Text "+i));
            }
            await sink.DoEmitBatchInternalAsync(events.ToArray());

            Assert.Equal(1, buffers.Count);
            Assert.Equal(Encoding.UTF8.GetString(buffers[0]), @"Information Text 0
Information Text 1
Information Text 2
Information Text 3
Information Text 4
");
        }

        [Fact]
        public async Task GroupsByHour()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            var names = new List<string>();

            blob.Setup(b => b.OpenWriteAsync()).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestAzureBlobSink(name =>
            {
                names.Add(name);
                return blob.Object;
            });

            var events = new List<LogEvent>();
            var startDate = new DateTime(2016, 8, 29, 22, 0, 0);
            for (int i = 0; i < 3; i++)
            {
                var addHours = startDate.AddHours(i);
                events.Add(CreateEvent(addHours, "Text"));
            }

            await sink.DoEmitBatchInternalAsync(events.ToArray());

            Assert.Equal(3, buffers.Count);

            Assert.Equal("appname/2016/08/29/22/filename", names[0]);
            Assert.Equal("appname/2016/08/29/23/filename", names[1]);
            Assert.Equal("appname/2016/08/30/00/filename", names[2]);
        }

        [Fact]
        public async Task CreatesBlobIfNotExists()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            bool created = false;

            blob.Setup(b => b.OpenWriteAsync()).Returns(() =>
            {
                if (!created)
                {
                    throw new StorageException(new RequestResult() { HttpStatusCode = 404 }, string.Empty, null);
                }
                return Task.FromResult((Stream) new TestMemoryStream(buffers));
            });

            blob.Setup(b => b.CreateAsync()).Returns(() =>
            {
                created = true;
                return Task.FromResult(0);
            });

            var sink = new TestAzureBlobSink(name => blob.Object);
            await sink.DoEmitBatchInternalAsync(new[] {CreateEvent(DateTime.Now, "Text")});

            Assert.Equal(1, buffers.Count);
            Assert.Equal(true, created);
        }

        private static LogEvent CreateEvent(DateTime addHours, string text)
        {
            MessageTemplateParser p = new MessageTemplateParser();
            var tempd = p.Parse(text);
            return new LogEvent(
                new DateTimeOffset(addHours),
                LogEventLevel.Information,
                null,
                tempd,
                Enumerable.Empty<LogEventProperty>());
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

    internal class TestAzureBlobSink : AzureBlobSink
    {
        public TestAzureBlobSink(Func<string, ICloudAppendBlob> blobReferenceFactory): base (blobReferenceFactory,
                "appname",
                "filename",
                new MessageTemplateTextFormatter("{Level} {Message}{NewLine}", CultureInfo.InvariantCulture),
                10,
                TimeSpan.FromSeconds(0.1))
        {
        }

        public Task DoEmitBatchInternalAsync(IEnumerable<LogEvent> events)
        {
            return EmitBatchAsync(events);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureWebApps.Test
{
    public class AzureBlobSinkTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        [Fact]
        public void WritesMessagesInBatches()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            blob.Setup(b => b.OpenWriteAsync()).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestAzureBlobSink(name => blob.Object, 5);
            var logger = CreateLogger(sink);

            for (int i = 0; i < 5; i++)
            {
                logger.Information("Text " + i);
            }

            Assert.True(sink.CountdownEvent.Wait(DefaultTimeout));

#if NET451
            Assert.Equal(1, buffers.Count);
            Assert.Equal(Encoding.UTF8.GetString(buffers[0]), @"Information Text 0
Information Text 1
Information Text 2
Information Text 3
Information Text 4
");
#else
            // PeriodicBatchingSink always writes first message as seperate batch on coreclr
            // https://github.com/serilog/serilog-sinks-periodicbatching/issues/7
            Assert.Equal(2, buffers.Count);
            Assert.Equal(Encoding.UTF8.GetString(buffers[0]), @"Information Text 0" + Environment.NewLine);
            Assert.Equal(Encoding.UTF8.GetString(buffers[1]), @"Information Text 1
Information Text 2
Information Text 3
Information Text 4
");
#endif
        }

        [Fact]
        public void GroupsByHour()
        {
            var blob = new Mock<ICloudAppendBlob>();
            var buffers = new List<byte[]>();
            var names = new List<string>();

            blob.Setup(b => b.OpenWriteAsync()).Returns(() => Task.FromResult((Stream)new TestMemoryStream(buffers)));

            var sink = new TestAzureBlobSink(name =>
            {
                names.Add(name);
                return blob.Object;
            }, 3);
            var logger = CreateLogger(sink);

            var startDate = new DateTime(2016, 8, 29, 22, 0, 0);
            for (int i = 0; i < 3; i++)
            {
                var addHours = startDate.AddHours(i);
                logger.Write(new LogEvent(
                    new DateTimeOffset(addHours),
                    LogEventLevel.Information,
                    null,
                    new MessageTemplate("Text", Enumerable.Empty<MessageTemplateToken>()),
                    Enumerable.Empty<LogEventProperty>()));
            }

            Assert.True(sink.CountdownEvent.Wait(DefaultTimeout));

            Assert.Equal(3, buffers.Count);

            Assert.Equal("appname/2016/08/29/22/filename", names[0]);
            Assert.Equal("appname/2016/08/29/23/filename", names[1]);
            Assert.Equal("appname/2016/08/30/00/filename", names[2]);
        }

        [Fact]
        public void CreatesBlobIfNotExists()
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

            var sink = new TestAzureBlobSink((name) => blob.Object, 1);
            var logger = CreateLogger(sink);
            logger.Information("Text");

            Assert.True(sink.CountdownEvent.Wait(DefaultTimeout));

            Assert.Equal(1, buffers.Count);
            Assert.Equal(true, created);
        }

        private static Logger CreateLogger(AzureBlobSink sink)
        {
            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.Sink(sink);
            var logger = loggerConfiguration.CreateLogger();
            return logger;
        }

        private class TestAzureBlobSink: AzureBlobSink
        {
            public CountdownEvent CountdownEvent { get; }

            public TestAzureBlobSink(Func<string, ICloudAppendBlob> blob, int count):base(
                blob,
                "appname",
                "filename",
                new MessageTemplateTextFormatter("{Level} {Message}{NewLine}", CultureInfo.InvariantCulture),
                10,
                TimeSpan.FromSeconds(0.1))
            {
                CountdownEvent = new CountdownEvent(count);
            }

            protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
            {
                await base.EmitBatchAsync(events);
                CountdownEvent.Signal(events.Count());
            }
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

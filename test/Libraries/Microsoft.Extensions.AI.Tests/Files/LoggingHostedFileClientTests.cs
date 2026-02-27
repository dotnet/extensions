// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingHostedFileClientTests
{
    [Fact]
    public void LoggingHostedFileClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingHostedFileClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingHostedFileClient(new TestHostedFileClient(), null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestHostedFileClient();

        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingHostedFileClient)));
        Assert.Same(innerClient, innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IHostedFileClient)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerClient.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingHostedFileClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerClient.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingHostedFileClient)));
        Assert.NotNull(innerClient.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingHostedFileClient)));
        Assert.Null(innerClient.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingHostedFileClient)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task UploadAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-123") { Name = "test.txt" }),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await client.UploadAsync(stream, "text/plain", "test.txt", new HostedFileUploadOptions { Purpose = "assistants" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("UploadAsync invoked.", entry.Message);
                    Assert.Contains("text/plain", entry.Message);
                    Assert.Contains("test.txt", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("UploadAsync completed:", entry.Message);
                    Assert.Contains("file-123", entry.Message);
                });
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("UploadAsync invoked.", entry.Message);
                    Assert.DoesNotContain("text/plain", entry.Message);
                    Assert.DoesNotContain("test.txt", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("UploadAsync completed.", entry.Message);
                    Assert.DoesNotContain("file-123", entry.Message);
                });
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task DownloadAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using var innerClient = new TestHostedFileClient
        {
            DownloadAsyncCallback = (fileId, options, ct) =>
                Task.FromResult<HostedFileDownloadStream>(new TestDownloadStream(new byte[] { 1 })),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        using var stream = await client.DownloadAsync("file-123");

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("DownloadAsync invoked.", entry.Message);
                    Assert.Contains("file-123", entry.Message);
                },
                entry => Assert.Contains("DownloadAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("DownloadAsync invoked.", entry.Message);
                    Assert.DoesNotContain("file-123", entry.Message);
                },
                entry => Assert.Contains("DownloadAsync completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetFileInfoAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using var innerClient = new TestHostedFileClient
        {
            GetFileInfoAsyncCallback = (fileId, options, ct) =>
                Task.FromResult<HostedFile?>(new HostedFile("file-456") { Name = "report.pdf" }),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await client.GetFileInfoAsync("file-456");

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("GetFileInfoAsync invoked.", entry.Message);
                    Assert.Contains("file-456", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("GetFileInfoAsync completed:", entry.Message);
                    Assert.Contains("file-456", entry.Message);
                    Assert.Contains("report.pdf", entry.Message);
                });
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("GetFileInfoAsync invoked.", entry.Message);
                    Assert.DoesNotContain("file-456", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("GetFileInfoAsync completed.", entry.Message);
                    Assert.DoesNotContain("report.pdf", entry.Message);
                });
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task ListFilesAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using var innerClient = new TestHostedFileClient
        {
            ListFilesAsyncCallback = (options, ct) => GetFilesAsync(),
        };

        static async IAsyncEnumerable<HostedFile> GetFilesAsync()
        {
            await Task.Yield();
            yield return new HostedFile("file-1") { Name = "a.txt" };
            yield return new HostedFile("file-2") { Name = "b.txt" };
        }

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await foreach (var file in client.ListFilesAsync())
        {
            // consume
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("ListFilesAsync invoked.", entry.Message),
                entry =>
                {
                    Assert.Contains("ListFilesAsync received item:", entry.Message);
                    Assert.Contains("file-1", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("ListFilesAsync received item:", entry.Message);
                    Assert.Contains("file-2", entry.Message);
                },
                entry => Assert.Contains("ListFilesAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("ListFilesAsync invoked.", entry.Message),
                entry => Assert.Contains("ListFilesAsync completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task DeleteAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using var innerClient = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, ct) => Task.FromResult(true),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await client.DeleteAsync("file-789");

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("DeleteAsync invoked.", entry.Message);
                    Assert.Contains("file-789", entry.Message);
                },
                entry =>
                {
                    Assert.Contains("DeleteAsync completed:", entry.Message);
                    Assert.Contains("true", entry.Message);
                });
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry =>
                {
                    Assert.Contains("DeleteAsync invoked.", entry.Message);
                    Assert.DoesNotContain("file-789", entry.Message);
                },
                entry => Assert.Contains("DeleteAsync completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Fact]
    public async Task UploadAsync_OnException_LogsError()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                throw new InvalidOperationException("upload failed"),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadAsync(stream));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("UploadAsync invoked.", entry.Message),
            entry =>
            {
                Assert.Contains("UploadAsync failed.", entry.Message);
                Assert.Equal(LogLevel.Error, entry.Level);
            });
    }

    [Fact]
    public async Task UploadAsync_OnCancellation_LogsCanceled()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                throw new OperationCanceledException(),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.UploadAsync(stream));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("UploadAsync invoked.", entry.Message),
            entry => Assert.Contains("UploadAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task DownloadAsync_OnException_LogsError()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            DownloadAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("download failed"),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DownloadAsync("file-1"));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("DownloadAsync invoked.", entry.Message),
            entry =>
            {
                Assert.Contains("DownloadAsync failed.", entry.Message);
                Assert.Equal(LogLevel.Error, entry.Level);
            });
    }

    [Fact]
    public async Task GetFileInfoAsync_OnException_LogsError()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            GetFileInfoAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("get failed"),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetFileInfoAsync("file-1"));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("GetFileInfoAsync invoked.", entry.Message),
            entry =>
            {
                Assert.Contains("GetFileInfoAsync failed.", entry.Message);
                Assert.Equal(LogLevel.Error, entry.Level);
            });
    }

    [Fact]
    public async Task DeleteAsync_OnException_LogsError()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("delete failed"),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteAsync("file-1"));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("DeleteAsync invoked.", entry.Message),
            entry =>
            {
                Assert.Contains("DeleteAsync failed.", entry.Message);
                Assert.Equal(LogLevel.Error, entry.Level);
            });
    }

    [Fact]
    public async Task DeleteAsync_OnCancellation_LogsCanceled()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, ct) =>
                throw new OperationCanceledException(),
        };

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.DeleteAsync("file-1"));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("DeleteAsync invoked.", entry.Message),
            entry => Assert.Contains("DeleteAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task ListFilesAsync_OnIterationException_LogsError()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerClient = new TestHostedFileClient
        {
            ListFilesAsyncCallback = (options, ct) => ThrowOnSecondItem(),
        };

        static async IAsyncEnumerable<HostedFile> ThrowOnSecondItem()
        {
            await Task.Yield();
            yield return new HostedFile("file-1");
            throw new InvalidOperationException("iteration failed");
        }

        using IHostedFileClient client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var file in client.ListFilesAsync())
            {
                _ = file;
            }
        });

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("ListFilesAsync invoked.", entry.Message),
            entry =>
            {
                Assert.Contains("ListFilesAsync failed.", entry.Message);
                Assert.Equal(LogLevel.Error, entry.Level);
            });
    }

    private sealed class TestDownloadStream : HostedFileDownloadStream
    {
        private readonly MemoryStream _inner;

        public TestDownloadStream(byte[] data)
        {
            _inner = new MemoryStream(data);
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

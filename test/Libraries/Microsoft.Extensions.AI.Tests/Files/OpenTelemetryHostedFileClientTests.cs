// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryHostedFileClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new OpenTelemetryHostedFileClient(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UploadAsync_TracesExpectedData(bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-abc") { Name = "test.txt", SizeInBytes = 1024 }),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = enableSensitiveData)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await client.UploadAsync(stream, "text/plain", "test.txt", new HostedFileUploadOptions { Purpose = "assistants", Scope = "container-1" });

        var activity = Assert.Single(activities);
        Assert.Equal("files.upload", activity.DisplayName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("files.upload", activity.GetTagItem("files.operation.name"));
        Assert.Equal("testprovider", activity.GetTagItem("files.provider.name"));
        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(8080, (int)activity.GetTagItem("server.port")!);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        // Always-present operational metadata
        Assert.Equal("text/plain", activity.GetTagItem("files.media_type"));
        Assert.Equal("assistants", activity.GetTagItem("files.purpose"));
        Assert.Equal("container-1", activity.GetTagItem("files.scope"));
        Assert.Equal("file-abc", activity.GetTagItem("files.id"));
        Assert.Equal(1024L, activity.GetTagItem("file.size"));

        // file.name is sensitive (could contain PII)
        if (enableSensitiveData)
        {
            Assert.Equal("test.txt", activity.GetTagItem("file.name"));
        }
        else
        {
            Assert.Null(activity.GetTagItem("file.name"));
        }
    }

    [Fact]
    public async Task DownloadAsync_TracesExpectedData()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            DownloadAsyncCallback = (fileId, options, ct) =>
                Task.FromResult<HostedFileDownloadStream>(new TestDownloadStream(new byte[] { 1 })),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        using var stream = await client.DownloadAsync("file-xyz", new HostedFileDownloadOptions { Scope = "container-2" });

        var activity = Assert.Single(activities);
        Assert.Equal("files.download", activity.DisplayName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("files.download", activity.GetTagItem("files.operation.name"));
        Assert.Equal("testprovider", activity.GetTagItem("files.provider.name"));
        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(8080, (int)activity.GetTagItem("server.port")!);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        Assert.Equal("file-xyz", activity.GetTagItem("files.id"));
        Assert.Equal("container-2", activity.GetTagItem("files.scope"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetFileInfoAsync_TracesExpectedData(bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            GetFileInfoAsyncCallback = (fileId, options, ct) =>
                Task.FromResult<HostedFile?>(new HostedFile("file-info") { Name = "report.pdf", SizeInBytes = 2048 }),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = enableSensitiveData)
            .Build();

        await client.GetFileInfoAsync("file-info", new HostedFileGetOptions { Scope = "container-3" });

        var activity = Assert.Single(activities);
        Assert.Equal("files.get_info", activity.DisplayName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("files.get_info", activity.GetTagItem("files.operation.name"));
        Assert.Equal("testprovider", activity.GetTagItem("files.provider.name"));
        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(8080, (int)activity.GetTagItem("server.port")!);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        Assert.Equal("file-info", activity.GetTagItem("files.id"));
        Assert.Equal("container-3", activity.GetTagItem("files.scope"));
        Assert.Equal(2048L, activity.GetTagItem("file.size"));

        if (enableSensitiveData)
        {
            Assert.Equal("report.pdf", activity.GetTagItem("file.name"));
        }
        else
        {
            Assert.Null(activity.GetTagItem("file.name"));
        }
    }

    [Fact]
    public async Task ListFilesAsync_TracesExpectedData()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            ListFilesAsyncCallback = (options, ct) => GetFilesAsync(),
            GetServiceCallback = CreateMetadataCallback(),
        };

        static async IAsyncEnumerable<HostedFile> GetFilesAsync()
        {
            await Task.Yield();
            yield return new HostedFile("file-1") { Name = "a.txt" };
            yield return new HostedFile("file-2") { Name = "b.txt" };
            yield return new HostedFile("file-3") { Name = "c.txt" };
        }

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await foreach (var file in client.ListFilesAsync(new HostedFileListOptions { Purpose = "assistants", Scope = "container-4" }))
        {
            _ = file;
        }

        var activity = Assert.Single(activities);
        Assert.Equal("files.list", activity.DisplayName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("files.list", activity.GetTagItem("files.operation.name"));
        Assert.Equal("testprovider", activity.GetTagItem("files.provider.name"));
        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(8080, (int)activity.GetTagItem("server.port")!);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        Assert.Equal(3, activity.GetTagItem("files.list.count"));

        Assert.Equal("container-4", activity.GetTagItem("files.scope"));
        Assert.Equal("assistants", activity.GetTagItem("files.purpose"));
    }

    [Fact]
    public async Task DeleteAsync_TracesExpectedData()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, ct) => Task.FromResult(true),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await client.DeleteAsync("file-del", new HostedFileDeleteOptions { Scope = "container-5" });

        var activity = Assert.Single(activities);
        Assert.Equal("files.delete", activity.DisplayName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("files.delete", activity.GetTagItem("files.operation.name"));
        Assert.Equal("testprovider", activity.GetTagItem("files.provider.name"));
        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(8080, (int)activity.GetTagItem("server.port")!);
        Assert.True(activity.Duration.TotalMilliseconds > 0);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);

        Assert.Equal("file-del", activity.GetTagItem("files.id"));
        Assert.Equal("container-5", activity.GetTagItem("files.scope"));
    }

    [Fact]
    public async Task UploadAsync_OnError_SetsErrorStatus()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                throw new InvalidOperationException("upload failed"),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadAsync(stream));

        var activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("upload failed", activity.StatusDescription);
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task ListFilesAsync_OnIterationError_SetsErrorStatus()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            ListFilesAsyncCallback = (options, ct) => ThrowOnSecondItem(),
            GetServiceCallback = CreateMetadataCallback(),
        };

        static async IAsyncEnumerable<HostedFile> ThrowOnSecondItem()
        {
            await Task.Yield();
            yield return new HostedFile("file-1");
            throw new InvalidOperationException("iteration failed");
        }

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var file in client.ListFilesAsync())
            {
                _ = file;
            }
        });

        var activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
        Assert.Equal(1, activity.GetTagItem("files.list.count"));
    }

    [Fact]
    public async Task GetService_ReturnsActivitySource()
    {
        var sourceName = Guid.NewGuid().ToString();

        using var innerClient = new TestHostedFileClient();
        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        var activitySource = client.GetService<ActivitySource>();
        Assert.NotNull(activitySource);
        Assert.Equal(sourceName, activitySource.Name);
    }

    [Fact]
    public async Task NoListeners_NoActivityCreated()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();

        // Deliberately not subscribing to the source name — no listeners
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource("some-other-source")
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-1")),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await client.UploadAsync(stream);

        Assert.Empty(activities);
    }

    [Fact]
    public async Task DownloadAsync_OnError_SetsErrorStatus()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            DownloadAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("download failed"),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DownloadAsync("file-1"));

        var activity = Assert.Single(activities);
        Assert.Equal("files.download", activity.DisplayName);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("download failed", activity.StatusDescription);
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task DeleteAsync_OnError_SetsErrorStatus()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            DeleteAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("delete failed"),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteAsync("file-1"));

        var activity = Assert.Single(activities);
        Assert.Equal("files.delete", activity.DisplayName);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("delete failed", activity.StatusDescription);
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task GetFileInfoAsync_OnError_SetsErrorStatus()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            GetFileInfoAsyncCallback = (fileId, options, ct) =>
                throw new InvalidOperationException("get info failed"),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetFileInfoAsync("file-1"));

        var activity = Assert.Single(activities);
        Assert.Equal("files.get_info", activity.DisplayName);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("get info failed", activity.StatusDescription);
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task NoMetadata_ServerTagsAbsent()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-1")),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await client.UploadAsync(stream);

        var activity = Assert.Single(activities);
        Assert.Equal("files.upload", activity.DisplayName);
        Assert.Null(activity.GetTagItem("files.provider.name"));
        Assert.Null(activity.GetTagItem("server.address"));
        Assert.Null(activity.GetTagItem("server.port"));
    }

    [Fact]
    public void UseOpenTelemetry_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>("builder",
            () => OpenTelemetryHostedFileClientBuilderExtensions.UseOpenTelemetry(null!));
    }

    [Fact]
    public async Task AdditionalProperties_TaggedWhenSensitiveDataEnabled()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-1")),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await client.UploadAsync(stream, options: new HostedFileUploadOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["custom.tag1"] = "value1",
                ["custom.tag2"] = 42,
            }
        });

        var activity = Assert.Single(activities);
        Assert.Equal("value1", activity.GetTagItem("custom.tag1"));
        Assert.Equal(42, activity.GetTagItem("custom.tag2"));
    }

    [Fact]
    public async Task AdditionalProperties_NotTaggedWhenSensitiveDataDisabled()
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        using var innerClient = new TestHostedFileClient
        {
            UploadAsyncCallback = (stream, mediaType, fileName, options, ct) =>
                Task.FromResult(new HostedFile("file-1")),
            GetServiceCallback = CreateMetadataCallback(),
        };

        using var client = innerClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = false)
            .Build();

        using var stream = new MemoryStream(new byte[] { 1 });
        await client.UploadAsync(stream, options: new HostedFileUploadOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["custom.tag1"] = "value1",
            }
        });

        var activity = Assert.Single(activities);
        Assert.Null(activity.GetTagItem("custom.tag1"));
    }

    private static Func<Type, object?, object?> CreateMetadataCallback() =>
        (serviceType, serviceKey) =>
            serviceType == typeof(HostedFileClientMetadata) ? new HostedFileClientMetadata("testprovider", new Uri("http://localhost:8080/files")) :
            null;

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;
using StreamJsonRpc;
using Trace = Microsoft.AspNetCore.Razor.LanguageServer.Trace;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [ClientName(ClientName)]
    [Export(typeof(ILanguageClient))]
    [ContentType(RazorLSPContentTypeDefinition.Name)]
    internal class RazorLanguageServerClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        // ClientName enables us to turn on-off the ILanguageClient functionality for specific TextBuffers of content type RazorLSPContentTypeDefinition.Name.
        // This typically is used in cloud scenarios where we want to utilize an ILanguageClient on the server but not the client; therefore we disable this
        // ILanguageClient infrastructure on the guest to ensure that two language servers don't provide results.
        public const string ClientName = "RazorLSPClientName";
        private readonly RazorLanguageServerCustomMessageTarget _customMessageTarget;

        [ImportingConstructor]
        public RazorLanguageServerClient(RazorLanguageServerCustomMessageTarget customTarget)
        {
            if (customTarget is null)
            {
                throw new ArgumentNullException(nameof(customTarget));
            }

            _customMessageTarget = customTarget;
        }

        public string Name => "Razor Language Server Client";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer => null;

        public object CustomMessageTarget => _customMessageTarget;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync
        {
            add { }
            remove { }
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            // Need an auto-flushing stream for the server because O# doesn't currently flush after writing responses. Without this
            // performing the Initialize handshake with the LanguageServer hangs.
            var autoFlushingStream = new AutoFlushingStream(serverStream);
            var server = await RazorLanguageServer.CreateAsync(autoFlushingStream, autoFlushingStream, Trace.Verbose);

            // Fire and forget for Initialized. Need to allow the LSP infrastructure to run in order to actually Initialize.
            _ = server.InitializedAsync(token);
            var connection = new Connection(clientStream, clientStream);
            return connection;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc) => Task.CompletedTask;

        private class AutoFlushingStream : Stream
        {
            private readonly Stream _inner;

            public AutoFlushingStream(Stream inner)
            {
                _inner = inner;
            }

            public override bool CanRead => _inner.CanRead;

            public override bool CanSeek => _inner.CanSeek;

            public override bool CanWrite => _inner.CanWrite;

            public override long Length => _inner.Length;

            public override long Position { get => _inner.Position; set => _inner.Position = value; }

            public override void Flush() => _inner.Flush();

            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

            public override void SetLength(long value) => _inner.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
                _inner.Flush();
            }
        }
    }
}

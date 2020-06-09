// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    // Intended for use with a party from a Nerdbank FullDuplexStream:
    // https://github.com/AArnott/Nerdbank.Streams/blob/master/doc/FullDuplexStream.md
    internal class AutoFlushingNerdbankStream : Stream
    {
        private readonly Stream _inner;

        public AutoFlushingNerdbankStream(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        // We ensure the Read/Write/Flush calls happen in a synchronous manner to avoid
        // concurrent read/write exceptions resulting in crashes of the language server.
        public override void Flush() => FlushAsync(CancellationToken.None).GetAwaiter().GetResult();

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await _inner.ReadAsync(buffer, offset, count).ConfigureAwait(false);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer, offset, count).ConfigureAwait(false);
            await FlushAsync().ConfigureAwait(false);
        }
    }
}

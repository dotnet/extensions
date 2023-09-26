// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

internal sealed class RequestBodyMultiSegmentPipeFeature : IRequestBodyPipeFeature
{
    public PipeReader Reader => new ErrorPipeReader();

    private sealed class ErrorPipeReader : PipeReader
    {
        private readonly ReadOnlySequence<byte> _buffer;
        private bool _isFirstReturn = true;

        public ErrorPipeReader()
        {
            var memory = new byte[] { 84, 101, 115, 116, 32, 83, 101, 103, 109, 101, 110, 116 }; // "Test Segment"

            var secondSegment = new SequenceSegment<byte>(memory, null, memory.Length);
            var firstSegment = new SequenceSegment<byte>(memory, secondSegment, 0);

            _buffer = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, memory.Length);
        }

        public override void CancelPendingRead() => throw new NotSupportedException();
        public override void Complete(Exception? exception = null) => throw new NotSupportedException();
        public override bool TryRead(out ReadResult result) => throw new NotSupportedException();
        public override void AdvanceTo(SequencePosition consumed) => throw new NotSupportedException();
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            // do nothing
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            var result = new ReadResult(_buffer, isCanceled: false, isCompleted: !_isFirstReturn);

            _isFirstReturn = false;
            return new ValueTask<ReadResult>(result);
        }

        private class SequenceSegment<T> : ReadOnlySequenceSegment<T>
        {
            public SequenceSegment(ReadOnlyMemory<T> memory, ReadOnlySequenceSegment<T>? next, long runningIndex)
            {
                Memory = memory;
                Next = next;
                RunningIndex = runningIndex;
            }
        }
    }
}

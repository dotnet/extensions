// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;

internal sealed class RequestBodyInfinitePipeFeature : IRequestBodyPipeFeature
{
    private readonly Action? _requestBodyStartReadingCallback;

    public RequestBodyInfinitePipeFeature(Action? requestBodyStartReadingCallback)
    {
        _requestBodyStartReadingCallback = requestBodyStartReadingCallback;
    }

    public PipeReader Reader => new InfinitePipeReader(_requestBodyStartReadingCallback);

    private class InfinitePipeReader : PipeReader
    {
        private Action? _requestBodyStartReadingCallback;

        public InfinitePipeReader(Action? requestBodyStartReadingCallback)
        {
            _requestBodyStartReadingCallback = requestBodyStartReadingCallback;
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            _requestBodyStartReadingCallback?.Invoke();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return default;
        }

        public override void AdvanceTo(SequencePosition consumed) => throw new InvalidOperationException();
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => throw new InvalidOperationException();
        public override void CancelPendingRead() => throw new InvalidOperationException();
        public override void Complete(Exception? exception = null) => throw new InvalidOperationException();
        public override bool TryRead(out ReadResult result) => throw new InvalidOperationException();
    }
}

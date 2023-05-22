// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;

internal sealed class RequestBodyErrorPipeFeature : IRequestBodyPipeFeature
{
    internal const string ErrorMessage = "TestPipeReader synthetic error";

    public PipeReader Reader => new ErrorPipeReader();

    private sealed class ErrorPipeReader : PipeReader
    {
        public override void AdvanceTo(SequencePosition consumed) => throw new InvalidOperationException(ErrorMessage);
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => throw new InvalidOperationException(ErrorMessage);
        public override void CancelPendingRead() => throw new InvalidOperationException(ErrorMessage);
        public override void Complete(Exception? exception = null) => throw new InvalidOperationException(ErrorMessage);
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(ErrorMessage);
        public override bool TryRead(out ReadResult result) => throw new InvalidOperationException(ErrorMessage);
    }
}

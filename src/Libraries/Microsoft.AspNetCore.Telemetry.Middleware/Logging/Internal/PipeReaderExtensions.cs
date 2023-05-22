// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal static class PipeReaderExtensions
{
    [SuppressMessage("Major Code Smell", "S125:Sections commented out", Justification = "Diagram")]
    public static async Task<ReadOnlySequence<byte>> ReadAsync(this PipeReader pipeReader, int numBytes,
        CancellationToken token)
    {
        long pointer = 0L;
        while (true)
        {
            ReadResult result;
            try
            {
                result = await pipeReader.ReadAsync(token).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                throw new OperationCanceledException(ex.Message, ex, ex.CancellationToken);
            }

            /*
             * Move pointer to (N*buffer) lower than numBytes.
             * +---------+ +---------+ +---------+
             * ||||||||||| ||||||||||| |         |
             * +---------+ +---------+ +-+-------+
             *                       ^   ^       ^
             *        pointer ------ +   |       |
             *      num bytes -----------+       |
             *    advanced to -------------------+
             *
             */

            if (!result.IsCompleted && result.Buffer.Length < numBytes)
            {
                var bufferStart = result.Buffer.Start;
                var bufferEnd = result.Buffer.End;

                pipeReader.AdvanceTo(bufferStart, bufferEnd);
                pointer += bufferEnd.GetInteger() - bufferStart.GetInteger();

                continue;
            }

            /*
             * Move pointer by bytes remaining after (N*buffer).
             * +---------+ +---------+ +---------+
             * ||||||||||| ||||||||||| |||       |
             * +---------+ +---------+ +-+-------+
             *                           ^       ^
             *        pointer -----------+       |
             *      num bytes -----------+       |
             *    advanced to -------------------+
             *
             */
            if (!result.IsCompleted && pointer < numBytes)
            {
                pointer = numBytes;
            }

            return result.Buffer.Slice(0L, pointer);
        }
    }
}

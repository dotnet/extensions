// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

public class TestVideoGenerationOperation : VideoGenerationOperation
{
    private string? _operationId;
    private string? _status;
    private int? _percentComplete;

    public TestVideoGenerationOperation(
        string? operationId = "test-op-id",
        string? status = "completed",
        int? percentComplete = 100)
    {
        _operationId = operationId;
        _status = status;
        _percentComplete = percentComplete;
    }

    public override string? OperationId => _operationId;

    public override string? Status => _status;

    public override int? PercentComplete => _percentComplete;

    public override bool IsCompleted =>
        string.Equals(_status, "completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(_status, "failed", StringComparison.OrdinalIgnoreCase);

    public override string? FailureReason => null;

    public IList<AIContent>? Contents { get; set; }

    public override Task UpdateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override Task WaitForCompletionAsync(
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override Task<IList<AIContent>> GetContentsAsync(
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IList<AIContent>>(Contents ?? new List<AIContent>());
}

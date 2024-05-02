// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

internal sealed class OperationCanceledExceptionMock : OperationCanceledException
{
    public OperationCanceledExceptionMock(Exception innerException)
        : base(null, innerException)
    {
    }

    public override string? Source { get => "System.Private.CoreLib"; set => base.Source = value; }
}

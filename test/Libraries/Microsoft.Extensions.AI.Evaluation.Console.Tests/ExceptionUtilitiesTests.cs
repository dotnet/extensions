// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Console.Tests;

public class ExceptionUtilitiesTests
{
    [Fact]
    public void IsCancellationReturnsFalseForNonCancellationException()
    {
        var exception = new InvalidOperationException();

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationReturnsTrueForOperationCanceledException()
    {
        var exception = new OperationCanceledException();

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsTrueForTaskCanceledException()
    {
        var exception = new TaskCanceledException();

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsTrueForAggregateExceptionWithOnlyOperationCanceledExceptions()
    {
        var exception = new AggregateException(new OperationCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsTrueForAggregateExceptionWithOnlyTaskCanceledExceptions()
    {
        var exception = new AggregateException(new TaskCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsTrueForAggregateExceptionWithMultipleCancellationExceptions()
    {
        var exception =
            new AggregateException(
                new TaskCanceledException(),
                new OperationCanceledException(),
                new OperationCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsFalseForEmptyAggregateException()
    {
        var exception = new AggregateException();

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationReturnsFalseForAggregateExceptionWithNonCancellationExceptions()
    {
        var exception = new AggregateException(new InvalidOperationException(), new ArgumentException());

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationReturnsFalseForAggregateExceptionWithCancellationAndNonCancellationExceptions()
    {
        var exception = new AggregateException(new OperationCanceledException(), new InvalidOperationException());

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationReturnsTrueForNestedAggregateExceptionsContainingOnlyCancellationExceptions()
    {
        var exception1 =
            new AggregateException(
                new TaskCanceledException(),
                new OperationCanceledException(),
                new OperationCanceledException());

        var exception2 = new AggregateException(new TaskCanceledException(), exception1);

        exception2.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationReturnsFalseForNestedAggregateExceptionsContainingNonCancellationExceptions()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new InvalidOperationException());
        var exception2 = new AggregateException(new TaskCanceledException(), exception1);

        exception2.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationHandlesLoopsInNestedAggregateExceptions1()
    {
        var exception1 = new AggregateException();
        var exception2 = new AggregateException(exception1);

        exception2.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationHandlesLoopsInNestedAggregateExceptions2()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new OperationCanceledException());
        var exception2 = new AggregateException(new OperationCanceledException());
        var exception3 = new AggregateException(exception1, exception2, new TaskCanceledException());

        exception3.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void IsCancellationHandlesLoopsInNestedAggregateExceptions3()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new OperationCanceledException());
        var exception2 = new AggregateException(new InvalidOperationException(), new OperationCanceledException());
        var exception3 = new AggregateException(exception1, exception2, new TaskCanceledException());

        exception3.IsCancellation().Should().BeFalse();
    }
}

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
    public void ReturnsTrueForOperationCanceledException()
    {
        var exception = new OperationCanceledException();

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void ReturnsTrueForAggregateExceptionWithOnlyOperationCanceledExceptions()
    {
        var exception = new AggregateException(new OperationCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void ReturnsFalseForNonCancellationException()
    {
        var exception = new InvalidOperationException();

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void ReturnsTrueForAggregateExceptionWithOnlyTaskCanceledExceptions()
    {
        var exception = new AggregateException(new TaskCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void ReturnsTrueForAggregateExceptionWithMultipleCancellationExceptions()
    {
        var exception =
            new AggregateException(
                new TaskCanceledException(),
                new OperationCanceledException(),
                new OperationCanceledException());

        exception.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void ReturnsFalseForEmptyAggregateException()
    {
        var exception = new AggregateException();

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalseForAggregateExceptionWithNonCancellationExceptions()
    {
        var exception = new AggregateException(new InvalidOperationException(), new ArgumentException());

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalseForAggregateExceptionWithCancellationAndNonCancellationExceptions()
    {
        var exception = new AggregateException(new OperationCanceledException(), new InvalidOperationException());

        exception.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void ReturnsTrueForNestedAggregateExceptionWithMultipleCancellationExceptions()
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
    public void ReturnsFalseForNestedAggregateExceptionWithNonCancellationExceptions()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new InvalidOperationException());
        var exception2 = new AggregateException(new TaskCanceledException(), exception1);

        exception2.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void HandlesLoopsInNestedAggregateExceptions1()
    {
        var exception1 = new AggregateException();
        var exception2 = new AggregateException(exception1);

        exception2.IsCancellation().Should().BeFalse();
    }

    [Fact]
    public void HandlesLoopsInNestedAggregateExceptions2()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new OperationCanceledException());
        var exception2 = new AggregateException(new OperationCanceledException());
        var exception3 = new AggregateException(exception1, exception2, new TaskCanceledException());

        exception3.IsCancellation().Should().BeTrue();
    }

    [Fact]
    public void HandlesLoopsInNestedAggregateExceptions3()
    {
        var exception1 = new AggregateException(new TaskCanceledException(), new OperationCanceledException());
        var exception2 = new AggregateException(new InvalidOperationException(), new OperationCanceledException());
        var exception3 = new AggregateException(exception1, exception2, new TaskCanceledException());

        exception3.IsCancellation().Should().BeFalse();
    }
}

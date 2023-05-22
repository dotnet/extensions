// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class PatchOperationTest
{
    [Fact]
    public void TestProperties()
    {
        string path = "/path";
        string stringValue = "sval";
        long longValue = 5;
        double doubleValue = 6;

        Validate(PatchOperation.Add(path, stringValue), path, PatchOperationType.Add, stringValue);
        Validate(PatchOperation.Remove(path), path, PatchOperationType.Remove, string.Empty);
        Validate(PatchOperation.Replace(path, stringValue), path, PatchOperationType.Replace, stringValue);
        Validate(PatchOperation.Set(path, stringValue), path, PatchOperationType.Set, stringValue);
        Validate(PatchOperation.Increment(path, longValue), path, PatchOperationType.Increment, longValue);
        Validate(PatchOperation.Increment(path, doubleValue), path, PatchOperationType.Increment, doubleValue);
    }

    private static void Validate(PatchOperation operation, string path, PatchOperationType type, object value)
    {
        operation.Path.Should().Be(path);
        operation.OperationType.Should().Be(type);
        operation.Value.Should().Be(value);
    }
}

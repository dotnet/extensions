// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

internal readonly struct TestObject
{
    private readonly string _value;

    public TestObject(string value)
    {
        _value = value;
    }

    public override string ToString() => _value;
}

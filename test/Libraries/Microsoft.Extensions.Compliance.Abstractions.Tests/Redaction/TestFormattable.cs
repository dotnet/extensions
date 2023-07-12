// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

internal sealed class TestFormattable : IFormattable
{
    private readonly string _value;

    public TestFormattable(string value)
    {
        _value = value;
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => _value;
}

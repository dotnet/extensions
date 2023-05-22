// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

internal sealed class FakeFormattable : IFormattable
{
    private readonly string _value;

    public FakeFormattable(string value)
    {
        _value = value;
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => _value;
}

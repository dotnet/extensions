// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Performs delayed redaction in order to avoid allocating intermediate strings and redact from span to span.
/// </summary>
internal sealed class JustInTimeRedactor : IResettable
#if NET6_0_OR_GREATER
    , ISpanFormattable
#else
    , IFormattable
#endif
{
    public static JustInTimeRedactor Get() => _pool.Get();
    public void Return() => _pool.Return(this);

    public JustInTimeRedactor? Next { get; set; }
    public Redactor? Redactor { get; set; }
    public object? Value { get; set; }
    public override string ToString() => Redactor!.Redact(Value, null, CultureInfo.InvariantCulture);
    public string ToString(string? format, IFormatProvider? formatProvider) => Redactor!.Redact(Value, format, formatProvider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => Redactor!.TryRedact(Value, destination, out charsWritten, format, provider);

    public bool TryReset()
    {
        Value = null;
        return true;
    }

    private static readonly ObjectPool<JustInTimeRedactor> _pool = PoolFactory.CreatePool<JustInTimeRedactor>();
}

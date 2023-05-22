// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Redactor designed for use in tests.
/// </summary>
public class FakeRedactor : Redactor
{
    private readonly CompositeFormat _format;
    private int _redactedSoFar;

    /// <summary>
    /// Gets the collector of redaction events.
    /// </summary>
    public FakeRedactionCollector EventCollector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeRedactor"/> class.
    /// </summary>
    /// <param name="options">The options to control behavior of redactor.</param>
    /// <param name="collector">Collects info about redacted values.</param>
    public FakeRedactor(IOptions<FakeRedactorOptions>? options = null, FakeRedactionCollector? collector = null)
    {
        var opt = options ?? Microsoft.Extensions.Options.Options.Create(new FakeRedactorOptions());
        EventCollector = collector ?? new FakeRedactionCollector();

        _format = GetRedactionFormat(opt.Value.RedactionFormat);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeRedactor"/> class.
    /// </summary>
    /// <param name="options">The options to control behavior of redactor.</param>
    /// <param name="collector">Collects info about redacted values.</param>
    /// <returns>New instance of <see cref="FakeRedactor"/>.</returns>
    public static FakeRedactor Create(FakeRedactorOptions? options = null, FakeRedactionCollector? collector = null) => new(Options.Options.Create(options ?? new FakeRedactorOptions()), collector);

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        Throw.IfBufferTooSmall(destination.Length, GetRedactedLength(source), nameof(destination));

        int charsWritten;

        var sourceString = source.ToString();

        if (_format.NumArgumentsNeeded == 0)
        {
            _ = _format.TryFormat(destination, out charsWritten, CultureInfo.InvariantCulture, Array.Empty<object?>());
        }
        else
        {
            _ = _format.TryFormat(destination, out charsWritten, CultureInfo.InvariantCulture, sourceString);
        }

        var order = Interlocked.Increment(ref _redactedSoFar);

        EventCollector.Append(new RedactedData(sourceString, destination.Slice(0, charsWritten).ToString(), order));

        return charsWritten;
    }

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        if (_format.NumArgumentsNeeded == 0)
        {
            return _format.Format(CultureInfo.InvariantCulture, Array.Empty<object?>()).Length;
        }

        return _format.Format(CultureInfo.InvariantCulture, input.ToString()).Length;
    }

    private static CompositeFormat GetRedactionFormat(string redactionFormat)
    {
        if (!CompositeFormat.TryParse(redactionFormat, out var parsed, out var error))
        {
            Throw.ArgumentException(nameof(FakeRedactorOptions.RedactionFormat), error);
        }

        return parsed;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
#if NET8_0_OR_GREATER
using System.Text;
#endif
using System.Threading;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Redactor designed for use in tests.
/// </summary>
public class FakeRedactor : Redactor
{
#if NET8_0_OR_GREATER
    private readonly CompositeFormat _format;
#else
    private readonly string _format;
#endif

    private int _redactedSoFar;

    /// <summary>
    /// Gets the collector of redaction events.
    /// </summary>
    public FakeRedactionCollector EventCollector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeRedactor"/> class.
    /// </summary>
    /// <param name="options">The options to control the redactor's behavior.</param>
    /// <param name="collector">Collects info about redacted values.</param>
    public FakeRedactor(IOptions<FakeRedactorOptions>? options = null, FakeRedactionCollector? collector = null)
    {
        var opt = options ?? Microsoft.Extensions.Options.Options.Create(new FakeRedactorOptions());

        IValidateOptions<FakeRedactorOptions> validator = new FakeRedactorOptionsAutoValidator();
        var r = validator.Validate(nameof(options), opt.Value);
        if (r.Succeeded)
        {
            validator = new FakeRedactorOptionsCustomValidator();
            r = validator.Validate(nameof(options), opt.Value);
        }

        if (r.Failed)
        {
            Throw.ArgumentException(nameof(options), r.ToString());
        }

        EventCollector = collector ?? new FakeRedactionCollector();

#if NET8_0_OR_GREATER
        _format = CompositeFormat.Parse(opt.Value.RedactionFormat);
#else
        _format = opt.Value.RedactionFormat;
#endif
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
        var sourceString = source.ToString();
        var str = string.Format(CultureInfo.InvariantCulture, _format, sourceString);

        Throw.IfBufferTooSmall(destination.Length, str.Length, nameof(destination));

        str.AsSpan().CopyTo(destination);

        var order = Interlocked.Increment(ref _redactedSoFar);
        EventCollector.Append(new RedactedData(sourceString, destination.Slice(0, str.Length).ToString(), order));

        return str.Length;
    }

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        return string.Format(CultureInfo.InvariantCulture, _format, input.ToString()).Length;
    }
}

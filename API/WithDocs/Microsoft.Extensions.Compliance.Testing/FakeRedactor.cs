// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Redactor designed for use in tests.
/// </summary>
public class FakeRedactor : Redactor
{
    /// <summary>
    /// Gets the collector of redaction events.
    /// </summary>
    public FakeRedactionCollector EventCollector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactor" /> class.
    /// </summary>
    /// <param name="options">The options to control behavior of redactor.</param>
    /// <param name="collector">Collects info about redacted values.</param>
    public FakeRedactor(IOptions<FakeRedactorOptions>? options = null, FakeRedactionCollector? collector = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactor" /> class.
    /// </summary>
    /// <param name="options">The options to control behavior of redactor.</param>
    /// <param name="collector">Collects info about redacted values.</param>
    /// <returns>New instance of <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactor" />.</returns>
    public static FakeRedactor Create(FakeRedactorOptions? options = null, FakeRedactionCollector? collector = null);

    /// <inheritdoc />
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);

    /// <inheritdoc />
    public override int GetRedactedLength(ReadOnlySpan<char> input);
}

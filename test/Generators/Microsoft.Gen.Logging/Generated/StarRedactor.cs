// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Redaction;

namespace TestClasses
{
    internal class StarRedactor : Redactor
    {
        public override int GetRedactedLength(ReadOnlySpan<char> source)
        {
            return source!.ToString()!.Length;
        }

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            var len = source!.ToString()!.Length;
            var redacted = new string('*', len);
            redacted.AsSpan().CopyTo(destination);
            return len;
        }
    }
}

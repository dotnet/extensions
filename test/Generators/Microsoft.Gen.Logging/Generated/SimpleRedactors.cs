// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Redaction;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace TestClasses
{
    internal class SimpleRedactor : Redactor
    {
        private readonly char _replacement;

        public SimpleRedactor(char replacement)
        {
            _replacement = replacement;
        }

        public override int GetRedactedLength(ReadOnlySpan<char> source)
        {
            return source!.ToString()!.Length;
        }

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            var len = source!.ToString()!.Length;
            var redacted = new string(_replacement, len);
            redacted.AsSpan().CopyTo(destination);
            return len;
        }
    }

    internal class StarRedactor : SimpleRedactor
    {
        public StarRedactor()
            : base('*')
        {
        }
    }

    internal class PlusRedactor : SimpleRedactor
    {
        public PlusRedactor()
            : base('+')
        {
        }
    }

    internal class MinusRedactor : SimpleRedactor
    {
        public MinusRedactor()
            : base('-')
        {
        }
    }

    internal class HashRedactor : SimpleRedactor
    {
        public HashRedactor()
            : base('#')
        {
        }
    }
}

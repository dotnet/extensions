// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace TestClasses
{
    internal class SimpleRedactorProvider : IRedactorProvider
    {
        private readonly char _replacement;

        public SimpleRedactorProvider()
            : this('*')
        {
        }

        public SimpleRedactorProvider(char replacement)
        {
            _replacement = replacement;
        }

        public Redactor GetRedactor(DataClassification dataClass) => new SimpleRedactor(_replacement);
    }
}

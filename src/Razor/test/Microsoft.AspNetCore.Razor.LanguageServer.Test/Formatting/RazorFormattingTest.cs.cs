// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class RazorFormattingTest : FormattingTestBase
    {
        [Fact]
        public async Task CodeBlock_SpansMultipleLines()
        {
            await RunFormattingTestAsync(
input: @"
@code
        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code
{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_IndentedBlock_MaintainsIndent()
        {
            await RunFormattingTestAsync(
input: @"
<boo>
    @code
            {
        private int currentCount = 0;

        private void IncrementCount()
        {
            currentCount++;
        }
    }
</boo>
",
expected: @"
<boo>
    @code
    {
        private int currentCount = 0;

        private void IncrementCount()
        {
            currentCount++;
        }
    }
</boo>
");
        }

        [Fact]
        public async Task CodeBlock_TooMuchWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_NonSpaceWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code	{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_NoWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task FunctionsBlock_BraceOnNewLine()
        {
            await RunFormattingTestAsync(
input: @"
@functions
        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@functions
{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task FunctionsBlock_TooManySpaces()
        {
            await RunFormattingTestAsync(
input: @"
@functions        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@functions {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }
    }
}

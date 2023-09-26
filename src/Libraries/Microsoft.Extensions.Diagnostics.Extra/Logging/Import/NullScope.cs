// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This source file was lovingly 'borrowed' from dotnet/runtime/src/libraries/Microsoft.Extensions.Logging
#pragma warning disable SA1629 // Documentation text should end with a period
#pragma warning disable SA1505 // Opening braces should not be followed by blank line
#pragma warning disable S1186 // Methods should not be empty

using System;

namespace Microsoft.Extensions.Logging
{

    /// <summary>
    /// An empty scope without any logic
    /// </summary>
    internal sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}

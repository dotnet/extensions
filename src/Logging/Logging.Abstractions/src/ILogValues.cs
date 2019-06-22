// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides an interface for a state of <see cref="ILogger.Log{TState}"/> that wants to make their values visitable without boxing them to objects.
    /// </summary>
    public interface ILogValues
    {
        /// <summary>
        /// The original format of the log.
        /// </summary>
        string OriginalFormat { get; }

        /// <summary>
        /// Visits values of the log entry.
        /// </summary>
        /// <typeparam name="TVisitor">The type of the visitor.</typeparam>
        /// <param name="visitor">The visitor to visit log values.</param>
        void Accept<TVisitor>(ref TVisitor visitor)
            where TVisitor : ILogValueVisitor;
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// A visitor visiting values of an implementor of <see cref="ILogValues"/>.
    /// </summary>
    public interface ILogValueVisitor
    {
        /// <summary>
        /// Visits a single value.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="value">The value.</param>
        void Visit<TValue>(TValue value);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         An arbitrary piece of metadata that can be stored on an object that implements <see cref="IReadOnlyAnnotatable" />.
    ///     </para>
    ///     <para>
    ///         This interface is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    /// </summary>
    public interface IAnnotation
    {
        /// <summary>
        ///     Gets the key of this annotation.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the value assigned to this annotation.
        /// </summary>
        object? Value { get; }
    }
}
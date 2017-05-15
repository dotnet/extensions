// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents something that initializes the TOptions type.
    /// Note: These are run after all <see cref="IConfigureOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public interface IInitializeOptions<in TOptions> where TOptions : class
    {
        /// <summary>
        /// Invoked to initialize a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being initialized.</param>
        /// <param name="options">The options instance to initialize.</param>
        void Initialize(string name, TOptions options);
    }
}
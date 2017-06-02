// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IPostConfigureOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class PostConfigureOptions<TOptions> : IPostConfigureOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Creates a new instance of <see cref="PostConfigureOptions{TOptions}"/>.
        /// </summary>
        /// <param name="name">The name of the options.</param>
        /// <param name="action">The action to register.</param>
        public PostConfigureOptions(string name, Action<TOptions> action)
        {
            Name = name;
            Action = action;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The initialization action.
        /// </summary>
        public Action<TOptions> Action { get; }

        /// <summary>
        /// Invokes the registered initialization Action if the name matches.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        public virtual void PostConfigure(string name, TOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Null name is used to initialize all named options.
            if (Name == null || name == Name)
            {
                Action?.Invoke(options);
            }
        }
    }
}
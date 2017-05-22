// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options.Infrastructure
{
    /// <summary>
    /// Used by ConfigureDefaults to configure defaults.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class ConfigureDefaultOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor for custom Configure implementations.
        /// </summary>
        protected ConfigureDefaultOptions() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the options.</param>
        /// <param name="action">The action to register.</param>
        public ConfigureDefaultOptions(string name, Action<TOptions> action)
        {
            Name = name;
            Action = action;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="action">The action to register.</param>
        public ConfigureDefaultOptions(Action<TOptions> action) : this(Options.DefaultName, action)
        { }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The configuration action.
        /// </summary>
        public Action<TOptions> Action { get; }

        /// <summary>
        /// Invokes the registered configure Action if the name matches.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        public virtual void Configure(string name, TOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Null name is used to configure all named options.
            if (Name == null || name == Name)
            {
                Action?.Invoke(options);
            }
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of IOptionsSnapshot.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class OptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private TOptions _options;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="monitor">The monitor to fetch the options value from.</param>
        public OptionsSnapshot(IOptionsMonitor<TOptions> monitor)
        {
            _options = monitor.CurrentValue;
        }

        /// <summary>
        /// The configured options instance.
        /// </summary>
        public virtual TOptions Value
        {
            get
            {
                return _options;
            }
        }
    }
}
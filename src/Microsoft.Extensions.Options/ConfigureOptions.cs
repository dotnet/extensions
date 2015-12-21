// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    public class ConfigureOptions<TOptions> : IConfigureOptions<TOptions> where TOptions : class
    {
        public ConfigureOptions(Action<TOptions> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Action = action;
        }

        public Action<TOptions> Action { get; private set; }

        public virtual void Configure(TOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Action.Invoke(options);
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.OptionsModel
{
    public class ConfigureOptions<TOptions> : IConfigureOptions<TOptions>
    {
        public ConfigureOptions([NotNull]Action<TOptions> action)
        {
            Action = action;
        }

        public Action<TOptions> Action { get; private set; }

        public virtual void Configure([NotNull]TOptions options)
        {
            Action.Invoke(options);
        }
    }
}
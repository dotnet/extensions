// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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

        public string Name { get; set; } = "";
        public virtual int Order { get; set; } = OptionsConstants.DefaultOrder;

        public virtual void Configure([NotNull]TOptions options, string name = "")
        {
            // Always invoke the action if no Name was specified, otherwise only if it was the requested name
            if (string.IsNullOrEmpty(Name) || string.Equals(name, Name, StringComparison.OrdinalIgnoreCase))
            {
                Action.Invoke(options);
            }
        }
    }
}
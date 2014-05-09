// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public class OptionsSetup<TOptions> : IOptionsSetup<TOptions>
    {
        public Action<TOptions> SetupAction { get; private set; }

        public OptionsSetup(Action<TOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException("setupAction");
            }
            SetupAction = setupAction;
        }

        public void Setup(TOptions options)
        {
            SetupAction(options);
        }

        public int Order { get; set; }
    }
}
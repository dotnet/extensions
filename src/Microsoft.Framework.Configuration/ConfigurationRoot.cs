// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public class ConfigurationRoot : ConfigurationBase, IConfigurationRoot
    {
        public ConfigurationRoot(IList<IConfigurationSource> sources)
            : base(sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }
        }

        public override string Path
        {
            get
            {
                return string.Empty;
            }
        }

        public void Reload()
        {
            foreach (var src in Sources)
            {
                src.Load();
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Configuration
{
    public class IncludedConfigurationProvider : ConfigurationProvider
    {
        private int _pathStart;

        public IncludedConfigurationProvider(IConfiguration source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var section = source as IConfigurationSection;
            if (section != null)
            {
                _pathStart = section.Path.Length + 1;
            }
            foreach (var child in source.GetChildren())
            {
                AddSection(child);
            }
        }

        private void AddSection(IConfigurationSection section)
        {
            Data.Add(section.Path.Substring(_pathStart), section.Value);
            foreach (var child in section.GetChildren())
            {
                AddSection(child);
            }
        }
    }
}

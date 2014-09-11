// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.OptionsModel
{
    public class OptionsServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describe = new ServiceDescriber(configuration);

            yield return describe.Describe(
                typeof(IOptionsAccessor<>),
                typeof(OptionsAccessor<>),
                null,
                LifecycleKind.Singleton);
        }

        public static void ReadProperties(object obj, IConfiguration config)
        {
            // No convert on portable
#if NET45 || ASPNET50|| ASPNETCORE50
            if (obj == null || config == null)
            {
                return;
            }
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                // Only try to set properties with public setters
                if (prop.GetSetMethod() == null)
                {
                    continue;
                }
                var configValue = config.Get(prop.Name);
                if (configValue == null)
                {
                    // Try to bind recursively
                    ReadProperties(prop.GetValue(obj), config.GetSubKey(prop.Name));
                    continue;
                }
                prop.SetValue(obj, Convert.ChangeType(configValue, prop.PropertyType));
            }
#endif
        }
    }
}

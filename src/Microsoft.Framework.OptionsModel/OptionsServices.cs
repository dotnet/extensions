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
        public static void ReadProperties(object obj, IConfiguration config)
        {
            // No convert on portable or core
#if NET45 || DNX451
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

                var propertyType = prop.PropertyType;

                // Handle Nullable<T>
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                try
                {
                    prop.SetValue(obj, Convert.ChangeType(configValue, propertyType));
                }
                catch
                {
                    // Ignore errors
                }
            }
#endif
        }
    }
}

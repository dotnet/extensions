// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.OptionsModel
{
    public class OptionsServices
    {
        private static IEnumerable<PropertyInfo> GetAllProperties(object obj)
        {
            var allProperties = new List<PropertyInfo>();
            var type = obj.GetType().GetTypeInfo();
            do
            {
                allProperties.AddRange(type.DeclaredProperties);
                type = type.BaseType.GetTypeInfo();
            } while (type != typeof(object).GetTypeInfo());
            return allProperties;
        }

        public static void ReadProperties(object obj, IConfiguration config)
        {
            if (obj == null || config == null)
            {
                return;
            }
            var props = GetAllProperties(obj);
            foreach (var prop in props)
            {
                // Only try to set properties with public setters
                if (prop.SetMethod == null || !prop.SetMethod.IsPublic)
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
                if (propertyType.GetTypeInfo().IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                try
                {
                    //Handle enums
                    if (propertyType.GetTypeInfo().IsEnum)
                    {
                        prop.SetValue(obj, Enum.Parse(propertyType, configValue));
                    }
                    else
                    {
                        prop.SetValue(obj, Convert.ChangeType(configValue, propertyType));
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
    }
}

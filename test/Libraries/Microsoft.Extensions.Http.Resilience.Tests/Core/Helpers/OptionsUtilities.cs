// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Http.Resilience.Test;

internal static class OptionsUtilities
{
    public static void ValidateOptions(object options)
    {
        var context = new ValidationContext(options);
        Validator.ValidateObject(options, context, true);
    }

    public static bool EqualOptions<T>(T options1, T options2)
    {
        if (options1 is null && options2 is null)
        {
            return true;
        }

        if (options1 is null || options2 is null)
        {
            return false;
        }

        var propertiesValuesByName1 = options1.GetPropertiesValuesByName();
        var propertiesValuesByName2 = options2.GetPropertiesValuesByName();

        foreach (var propertyDefinition1 in propertiesValuesByName1)
        {
            var propertyName = propertyDefinition1.Key;
            var propertyValue1 = propertyDefinition1.Value;

            if (!propertiesValuesByName2.TryGetValue(propertyName, out var propertyValue2) ||
               !Equals(propertyValue1, propertyValue2))
            {
                return false;
            }
        }

        return true;
    }

    private static IDictionary<string, object> GetPropertiesValuesByName<T>(this T options)
    {
        return options!
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .GroupBy(property => property.Name)
            .ToDictionary(
                propertyGroup => propertyGroup.Key,
                propertyGroup => propertyGroup.Last().GetValue(options)!);
    }
}

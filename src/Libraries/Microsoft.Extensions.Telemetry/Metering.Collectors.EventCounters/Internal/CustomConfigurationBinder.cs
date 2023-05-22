// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET7_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Metering.Internal;

// Contains modifications to bind ISet<T> and IReadOnlySet<T> within options classes properly
// Originally taken from: https://source.dot.net/#Microsoft.Extensions.Configuration.Binder/ConfigurationBinder.cs
// This class can be removed once https://github.com/dotnet/runtime/issues/66141 is resolved
// Tracked under https://domoreexp.visualstudio.com/R9/_workitems/edit/2285691/
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Original impl")]
[ExcludeFromCodeCoverage]
internal static class CustomConfigurationBinder
{
    private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const string TrimmingWarningMessage = "In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.";
    private const string InstanceGetTypeTrimmingWarningMessage = "Cannot statically analyze the type of instance so its members may be trimmed";
    private const string PropertyTrimmingWarningMessage = "Cannot statically analyze property.PropertyType so its members may be trimmed.";

    /// <summary>
    /// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
    /// </summary>
    /// <param name="configuration">The configuration instance to bind.</param>
    /// <param name="instance">The object to bind.</param>
    [RequiresUnreferencedCode(InstanceGetTypeTrimmingWarningMessage)]
    internal static void Bind(IConfiguration configuration, object? instance)
    {
        if (instance != null)
        {
            _ = BindInstance(instance.GetType(), instance, configuration);
        }
    }

    [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
    private static void BindNonScalar(this IConfiguration configuration, object? instance)
    {
        if (instance != null)
        {
            List<PropertyInfo> modelProperties = GetAllProperties(instance.GetType());

            foreach (PropertyInfo property in modelProperties)
            {
                BindProperty(property, instance, configuration);
            }
        }
    }

    [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
    private static void BindProperty(PropertyInfo property, object instance, IConfiguration config)
    {
        // We don't support set only, non public, or indexer properties
        if (property.GetMethod == null ||
            (!property.GetMethod.IsPublic) ||
            property.GetMethod.GetParameters().Length > 0)
        {
            return;
        }

        object? propertyValue = property.GetValue(instance);
        bool hasSetter = property.SetMethod != null && property.SetMethod.IsPublic;

        if (propertyValue == null && !hasSetter)
        {
            // Property doesn't have a value and we cannot set it so there is no
            // point in going further down the graph
            return;
        }

        propertyValue = GetPropertyValue(property, instance, config);

        if (propertyValue != null && hasSetter)
        {
            property.SetValue(instance, propertyValue);
        }
    }

    [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection in type so its members may be trimmed.")]
    private static object? BindToCollection(Type type, IConfiguration config)
    {
        Type genericType = typeof(List<>).MakeGenericType(type.GenericTypeArguments[0]);
        object? instance = Activator.CreateInstance(genericType);
        BindCollection(instance, genericType, config);
        return instance;
    }

    [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection in type so its members may be trimmed.")]
    private static object? BindToSet(Type type, IConfiguration config)
    {
        Type genericType = typeof(HashSet<>).MakeGenericType(type.GenericTypeArguments[0]);
        object? instance = Activator.CreateInstance(genericType);
        BindCollection(instance, genericType, config);
        return instance;
    }

    // Try to create an array/dictionary instance to back various collection interfaces
    [RequiresUnreferencedCode("In case type is a Dictionary, cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
    private static object? AttemptBindToCollectionInterfaces(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type,
        IConfiguration config)
    {
        if (!type.IsInterface)
        {
            return null;
        }

        Type? collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyList<>), type);
        if (collectionInterface != null)
        {
            // IEnumerable<T> is guaranteed to have exactly one parameter
            return BindToCollection(type, config);
        }

        collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyDictionary<,>), type);
        if (collectionInterface != null)
        {
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            object? instance = Activator.CreateInstance(dictionaryType);
            BindDictionary(instance, dictionaryType, config);
            return instance;
        }

        collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
        if (collectionInterface != null)
        {
            object? instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]));
            BindDictionary(instance, collectionInterface, config);
            return instance;
        }

#if NET5_0_OR_GREATER
        collectionInterface = FindOpenGenericInterface(typeof(IReadOnlySet<>), type);
        if (collectionInterface != null)
        {
            // IReadOnlySet<T> is guaranteed to have exactly one parameter
            return BindToSet(type, config);
        }
#endif

        collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyCollection<>), type);
        if (collectionInterface != null)
        {
            // IReadOnlyCollection<T> is guaranteed to have exactly one parameter
            return BindToCollection(type, config);
        }

        collectionInterface = FindOpenGenericInterface(typeof(ISet<>), type);
        if (collectionInterface != null)
        {
            // ISet<T> is guaranteed to have exactly one parameter
            return BindToSet(type, config);
        }

        collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
        if (collectionInterface != null)
        {
            // ICollection<T> is guaranteed to have exactly one parameter
            return BindToCollection(type, config);
        }

        collectionInterface = FindOpenGenericInterface(typeof(IEnumerable<>), type);
        if (collectionInterface != null)
        {
            // IEnumerable<T> is guaranteed to have exactly one parameter
            return BindToCollection(type, config);
        }

        return null;
    }

    [RequiresUnreferencedCode(TrimmingWarningMessage)]
    private static object? BindInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type,
        object? instance, IConfiguration config)
    {
        // if binding IConfigurationSection, break early
        if (type == typeof(IConfigurationSection))
        {
            return config;
        }

        var section = config as IConfigurationSection;
        string? configValue = section?.Value;
        if (configValue != null && TryConvertValue(type, configValue, section?.Path, out object? convertedValue, out Exception? error))
        {
            if (error != null)
            {
                throw error;
            }

            // Leaf nodes are always reinitialized
            return convertedValue;
        }

        if (config != null && config.GetChildren().Any())
        {
            // If we don't have an instance, try to create one
            if (instance == null)
            {
                // We are already done if binding to a new collection instance worked
                instance = AttemptBindToCollectionInterfaces(type, config);
                if (instance != null)
                {
                    return instance;
                }

                instance = CreateInstance(type);
            }

            // See if its a Dictionary
            Type? collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
            if (collectionInterface != null)
            {
                BindDictionary(instance, collectionInterface, config);
            }
            else if (type.IsArray)
            {
                instance = BindArray((Array)instance!, config);
            }
            else
            {
                // See if its an ICollection
                collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
                if (collectionInterface != null)
                {
                    BindCollection(instance, collectionInterface, config);
                }
                else
                {
                    BindNonScalar(config, instance);
                }
            }
        }

        return instance;
    }

    private static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
    {
        if (type.IsInterface || type.IsAbstract)
        {
            Throw.InvalidOperationException($"Cannot create instance of type '{type}' because it is either abstract or an interface.");
        }

        if (type.IsArray)
        {
            if (type.GetArrayRank() > 1)
            {
                Throw.InvalidOperationException(
                    $"Cannot create instance of type '{type}' because multidimensional arrays are not supported.");
            }

            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (!type.IsValueType)
        {
            bool hasDefaultConstructor = type.GetConstructors(DeclaredOnlyLookup)
                .Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0);

            if (!hasDefaultConstructor)
            {
                Throw.InvalidOperationException(
                    $"Cannot create instance of type '{type}' because it is missing a public parameterless constructor.");
            }
        }

        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of type '{type}'.", ex);
        }
    }

    [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
    private static void BindDictionary(
        object? dictionary,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type dictionaryType,
        IConfiguration config)
    {
        // IDictionary<K,V> is guaranteed to have exactly two parameters
        Type keyType = dictionaryType.GenericTypeArguments[0];
        Type valueType = dictionaryType.GenericTypeArguments[1];
        bool keyTypeIsEnum = keyType.IsEnum;

        if (keyType != typeof(string) && !keyTypeIsEnum)
        {
            // We only support string and enum keys
            return;
        }

        MethodInfo tryGetValue = dictionaryType.GetMethod("TryGetValue")!;
        PropertyInfo setter = dictionaryType.GetProperty("Item", DeclaredOnlyLookup)!;
        foreach (IConfigurationSection child in config.GetChildren())
        {
            try
            {
                object key = keyTypeIsEnum ? Enum.Parse(keyType, child.Key) : child.Key;
                var args = new object?[] { key, null };
                _ = tryGetValue.Invoke(dictionary, args);
                object? item = BindInstance(
                    type: valueType,
                    instance: args[1],
                    config: child);

                if (item != null)
                {
                    setter.SetValue(dictionary, item, new[] { key });
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection so its members may be trimmed.")]
    private static void BindCollection(
        object? collection,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type collectionType,
        IConfiguration config)
    {
        // ICollection<T> is guaranteed to have exactly one parameter
        Type itemType = collectionType.GenericTypeArguments[0];
        MethodInfo? addMethod = collectionType.GetMethod("Add", DeclaredOnlyLookup);

        foreach (IConfigurationSection section in config.GetChildren())
        {
            try
            {
                object? item = BindInstance(
                    type: itemType,
                    instance: null,
                    config: section);

                if (item != null)
                {
                    _ = addMethod?.Invoke(collection, new[] { item });
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    [RequiresUnreferencedCode("Cannot statically analyze what the element type is of the Array so its members may be trimmed.")]
    private static Array BindArray(Array source, IConfiguration config)
    {
        IConfigurationSection[] children = config.GetChildren().ToArray();
        int arrayLength = source.Length;
        Type elementType = source.GetType().GetElementType()!;
        var newArray = Array.CreateInstance(elementType, arrayLength + children.Length);

        // binding to array has to preserve already initialized arrays with values
        if (arrayLength > 0)
        {
            Array.Copy(source, newArray, arrayLength);
        }

        for (int i = 0; i < children.Length; i++)
        {
            try
            {
                object? item = BindInstance(
                    type: elementType,
                    instance: null,
                    config: children[i]);

                if (item != null)
                {
                    newArray.SetValue(item, arrayLength + i);
                }
            }
            catch
            {
                // ignored
            }
        }

        return newArray;
    }

    [RequiresUnreferencedCode(TrimmingWarningMessage)]
    private static bool TryConvertValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type,
        string value, string? path, out object? result, out Exception? error)
    {
        error = null;
        result = null;
        if (type == typeof(object))
        {
            result = value;
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return TryConvertValue(Nullable.GetUnderlyingType(type)!, value, path, out result, out error);
        }

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                result = converter.ConvertFromInvariantString(value);
            }
            catch (Exception ex)
            {
                error = new InvalidOperationException(
                    $"Failed to convert configuration value at '{path}' to type '{type}'.", ex);
            }

            return true;
        }

        if (type == typeof(byte[]))
        {
            try
            {
                result = Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                error = new InvalidOperationException($"Failed to convert configuration value at '{path}' to type '{type}'.", ex);
            }

            return true;
        }

        return false;
    }

    private static Type? FindOpenGenericInterface(
        Type expected,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type actual)
    {
        if (actual.IsGenericType &&
            actual.GetGenericTypeDefinition() == expected)
        {
            return actual;
        }

        Type[] interfaces = actual.GetInterfaces();
        foreach (Type interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == expected)
            {
                return interfaceType;
            }
        }

        return null;
    }

    private static List<PropertyInfo> GetAllProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type)
    {
        var allProperties = new List<PropertyInfo>();

        Type? baseType = type;
        do
        {
            allProperties.AddRange(baseType!.GetProperties(DeclaredOnlyLookup));
            baseType = baseType.BaseType;
        }
        while (baseType != typeof(object));

        return allProperties;
    }

    [RequiresUnreferencedCode(PropertyTrimmingWarningMessage)]
    private static object? GetPropertyValue(PropertyInfo property, object instance, IConfiguration config)
    {
        return BindInstance(
            property.PropertyType,
            property.GetValue(instance),
            config.GetSection(property.Name));
    }
}

#endif

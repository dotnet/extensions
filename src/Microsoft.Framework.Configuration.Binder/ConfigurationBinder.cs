// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Configuration.Binder;

namespace Microsoft.Framework.Configuration
{
    public static class ConfigurationBinder
    {
        public static TModel Bind<TModel>(IConfiguration configuration) where TModel : new()
        {
            var model = new TModel();
            Bind(model, configuration);
            return model;
        }

        public static void Bind(object model, IConfiguration configuration)
        {
            if (model == null)
            {
                return;
            }

            BindObjectProperties(model, configuration);
        }

        private static void BindObjectProperties(object obj, IConfiguration configuration)
        {
            foreach (var property in GetAllProperties(obj.GetType().GetTypeInfo()))
            {
                BindProperty(property, obj, configuration);
            }
        }

        private static void BindProperty(PropertyInfo property, object propertyOwner, IConfiguration configuration)
        {
            configuration = configuration.GetConfigurationSection(property.Name);

            if (property.GetMethod == null || !property.GetMethod.IsPublic)
            {
                // We don't support set only properties
                return;
            }

            var propertyValue = property.GetValue(propertyOwner);
            var hasPublicSetter = property.SetMethod != null && property.SetMethod.IsPublic;

            if (propertyValue == null && !hasPublicSetter)
            {
                // Property doesn't have a value and we cannot set it so there is no
                // point in going further down the graph
                return;
            }

            propertyValue = BindType(
                property.PropertyType,
                propertyValue,
                configuration);

            if (propertyValue != null && hasPublicSetter)
            {
                property.SetValue(propertyOwner, propertyValue);
            }
        }

        private static object BindType(Type type, object typeInstance, IConfiguration configuration)
        {
            var configValue = configuration.Get(null);
            var typeInfo = type.GetTypeInfo();

            if (configValue != null)
            {
                // Leaf nodes are always reinitialized
                return CreateValueFromConfiguration(type, configValue, configuration);
            }
            else
            {
                var subkeys = configuration.GetConfigurationSections();
                if (subkeys.Count() != 0)
                {
                    if (typeInstance == null)
                    {
                        if (typeInfo.IsInterface || typeInfo.IsAbstract)
                        {
                            throw new InvalidOperationException(Resources.FormatError_CannotActivateAbstractOrInterface(type));
                        }

                        bool hasParameterlessConstructor = typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0);
                        if (!hasParameterlessConstructor)
                        {
                            throw new InvalidOperationException(Resources.FormatError_MissingParameterlessConstructor(type));
                        }

                        try
                        {
                            typeInstance = Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(Resources.FormatError_FailedToActivate(type), ex);
                        }
                    }

                    var collectionInterface = GetGenericOpenInterfaceImplementation(typeof(IDictionary<,>), type);
                    if (collectionInterface != null)
                    {
                        // Dictionary
                        BindDictionary(typeInstance, collectionInterface, configuration);
                    }
                    else
                    {
                        collectionInterface = GetGenericOpenInterfaceImplementation(typeof(ICollection<>), type);
                        if (collectionInterface != null)
                        {
                            // ICollection
                            BindCollection(typeInstance, collectionInterface, configuration);
                        }
                        else
                        {
                            // Something else
                            BindObjectProperties(typeInstance, configuration);
                        }
                    }
                }
                return typeInstance;
            }
        }

        private static void BindDictionary(object dictionary, Type iDictionaryType, IConfiguration configuration)
        {
            var iDictionaryTypeInfo = iDictionaryType.GetTypeInfo();

            // It is guaranteed to have a two and only two parameters
            // because this is an IDictionary<K,V>
            var keyType = iDictionaryTypeInfo.GenericTypeArguments[0];
            var valueType = iDictionaryTypeInfo.GenericTypeArguments[1];

            if (keyType != typeof(string))
            {
                // We only support string keys
                return;
            }

            var addMethod = iDictionaryTypeInfo.GetDeclaredMethod("Add");
            var subkeys = configuration.GetConfigurationSections().ToList();

            foreach (var keyProperty in subkeys)
            {
                var keyConfiguration = keyProperty.Value;

                var item = BindType(
                    type: valueType,
                    typeInstance: null,
                    configuration: keyConfiguration);
                if (item != null)
                {
                    addMethod.Invoke(dictionary, new[] { keyProperty.Key, item });
                }
            }
        }

        private static void BindCollection(object collection, Type iCollectionType, IConfiguration configuration)
        {
            var iCollectionTypeInfo = iCollectionType.GetTypeInfo();

            // It is guaranteed to have a one and only one parameter
            // because this is an ICollection<T>
            var itemType = iCollectionTypeInfo.GenericTypeArguments[0];

            var addMethod = iCollectionTypeInfo.GetDeclaredMethod("Add");
            var subkeys = configuration.GetConfigurationSections().ToList();

            foreach (var keyProperty in subkeys)
            {
                var keyConfiguration = keyProperty.Value;

                try
                {
                    var item = BindType(
                        type: itemType,
                        typeInstance: null,
                        configuration: keyConfiguration);
                    if (item != null)
                    {
                        addMethod.Invoke(collection, new[] { item });
                    }
                }
                catch
                {
                }
            }
        }

        private static object CreateValueFromConfiguration(Type type, string value, IConfiguration configuration)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return CreateValueFromConfiguration(Nullable.GetUnderlyingType(type), value, configuration);
            }

            var configurationValue = configuration.Get(key: null);

            try
            {
                return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(configurationValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(Resources.FormatError_FailedBinding(configurationValue, type), ex);
            }
        }

        private static Type GetGenericOpenInterfaceImplementation(Type expectedOpenGeneric, Type actual)
        {
            var interfaces = actual.GetTypeInfo().ImplementedInterfaces;
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.GetTypeInfo().IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == expectedOpenGeneric)
                {
                    return interfaceType;
                }
            }

            return null;
        }

        private static IEnumerable<PropertyInfo> GetAllProperties(TypeInfo type)
        {
            var allProperties = new List<PropertyInfo>();

            do
            {
                allProperties.AddRange(type.DeclaredProperties);
                type = type.BaseType.GetTypeInfo();
            }
            while (type != typeof(object).GetTypeInfo());

            return allProperties;
        }
    }
}

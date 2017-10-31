// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Internal
{
    internal class TypeNameHelper
    {
        private static readonly Dictionary<Type, string> _builtInTypeNames = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
        };

        public static string GetTypeDisplayName(object item, bool fullName = true)
        {
            return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
        }

        public static string GetTypeDisplayName(Type type, bool fullName = true)
        {
            var builder = new StringBuilder();
            ProcessTypeName(builder, type, fullName);
            return builder.ToString();
        }

        private static void ProcessTypeName(StringBuilder builder, Type type, bool fullName)
        {
            if (type.IsGenericType)
            {
                ProcessGenericType(builder, type, new ArraySegment<Type>(type.GenericTypeArguments), fullName);
                return;
            }

            if (_builtInTypeNames.TryGetValue(type, out var builtInName))
            {
                builder.Append(builtInName);
            }
            else
            {
                builder.Append(fullName ? type.FullName : type.Name);
            }
        }

        private static void ProcessGenericType(StringBuilder builder, Type type, ArraySegment<Type> genericArguments, bool fullName)
        {
            var ownGenericArguments = genericArguments;
            if (type.IsNested)
            {
                var offset = type.DeclaringType.GetGenericArguments().Length;
                ownGenericArguments = new ArraySegment<Type>(genericArguments.Array, offset, genericArguments.Count - offset);
            }

            if (fullName)
            {
                if (type.DeclaringType != null)
                {
                    var declaringTypeGenericArguments = new ArraySegment<Type>(
                        genericArguments.Array,
                        0,
                        genericArguments.Count - ownGenericArguments.Count);
                    ProcessGenericType(builder, type.DeclaringType, declaringTypeGenericArguments, fullName);
                    builder.Append('+');
                }
                else
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }

            AppendNameOfGenericType(builder, type);

            if (ownGenericArguments.Count > 0)
            {
                builder.Append('<');
                for (var i = 0; i < ownGenericArguments.Count; i++)
                {
                    ProcessTypeName(builder, genericArguments.Array[ownGenericArguments.Offset + i], fullName);
                    if (i != ownGenericArguments.Count - 1)
                    {
                        builder.Append(", ");
                    }
                }

                builder.Append('>');
            }
        }

        private static void AppendNameOfGenericType(StringBuilder builder, Type type)
        {
            var genericPartIndex = type.Name.IndexOf('`');
            if (genericPartIndex > 0)
            {
                builder.Append(type.Name, startIndex: 0, count: genericPartIndex);
            }
            else
            {
                builder.Append(type.Name);
            }
        }
    }
}

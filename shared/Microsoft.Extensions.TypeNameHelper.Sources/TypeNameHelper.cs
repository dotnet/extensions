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
            { typeof(ushort), "ushort" }
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
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName);
            }
            else if (_builtInTypeNames.TryGetValue(type, out var buildInName))
            {
                builder.Append(buildInName);
            }
            else
            {
                builder.Append(fullName ? type.FullName : type.Name);
            }
        }

        private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length, bool fullName)
        {
            var offset = 0;
            if (type.IsNested)
            {
                offset = type.DeclaringType.GetGenericArguments().Length;
            }

            if (fullName)
            {
                if (type.IsNested)
                {
                    ProcessGenericType(builder, type.DeclaringType, genericArguments, offset, fullName);
                    builder.Append('+');
                }
                else
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }

            var genericPartIndex = type.Name.IndexOf('`');
            if (genericPartIndex <= 0)
            {
                builder.Append(type.Name);
                return;
            }

            builder.Append(type.Name, 0, genericPartIndex);

            builder.Append('<');
            for (var i = offset; i < length; i++)
            {
                ProcessTypeName(builder, genericArguments[i], fullName);
                if (i + 1 != length)
                {
                    builder.Append(", ");
                }
            }
            builder.Append('>');
        }
    }
}

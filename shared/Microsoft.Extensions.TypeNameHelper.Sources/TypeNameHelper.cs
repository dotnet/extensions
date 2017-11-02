// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    internal class TypeNameHelper
    {
        private static readonly Dictionary<Type, string> _builtInTypeNames = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
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

        private static NameFormatting NameFormatFromBool(bool isFullName) => isFullName ? NameFormatting.Full : NameFormatting.Short;
        private static bool IsFullName(NameFormatting formatting) => formatting.HasFlag(NameFormatting.Full);

        public static string GetTypeDisplayName(object item, bool fullName = true)
        {
            return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
        }

        public static string GetTypeDisplayName(Type type, bool fullName = true)
        {
            var builder = new StringBuilder();
            ProcessType(builder, type, NameFormatFromBool(fullName));
            return builder.ToString();
        }

        public static string GetMethodDisplayName(MethodBase method, bool fullTypeName = true)
        {
            var sb = new StringBuilder();
            ProcessMethodName(method, sb, fullTypeName);
            return sb.ToString();
        }

        private static void ProcessType(StringBuilder builder, Type type, NameFormatting nameFormatting)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType(builder, type, genericArguments, genericArguments.Length, nameFormatting);
            }
            else if (type.IsArray)
            {
                ProcessArrayType(builder, type, nameFormatting);
            }
            else if (_builtInTypeNames.TryGetValue(type, out var builtInName))
            {
                builder.Append(builtInName);
            }
            else if (!type.IsGenericParameter)
            {
                builder.Append(IsFullName(nameFormatting) ? type.FullName : type.Name);
            }
            else if (nameFormatting.HasFlag(NameFormatting.GenericParameterName))
            {
                builder.Append(type.Name);
            }
        }

        private static void ProcessArrayType(StringBuilder builder, Type type, NameFormatting nameFormatting)
        {
            var innerType = type;
            while (innerType.IsArray)
            {
                innerType = innerType.GetElementType();
            }

            ProcessType(builder, innerType, nameFormatting);

            while (type.IsArray)
            {
                builder.Append('[');
                builder.Append(',', type.GetArrayRank() - 1);
                builder.Append(']');
                type = type.GetElementType();
            }
        }

        private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length, NameFormatting nameFormatting)
        {
            var offset = 0;
            if (type.IsNested)
            {
                offset = type.DeclaringType.GetGenericArguments().Length;
            }

            if (IsFullName(nameFormatting))
            {
                if (type.IsNested)
                {
                    ProcessGenericType(builder, type.DeclaringType, genericArguments, offset, nameFormatting);
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
                ProcessType(builder, genericArguments[i], nameFormatting);
                if (i + 1 == length)
                {
                    continue;
                }

                builder.Append(',');
                if (!genericArguments[i + 1].IsGenericParameter)
                {
                    builder.Append(' ');
                }
            }
            builder.Append('>');
        }

        private static void ProcessMethodName(MethodBase method, StringBuilder sb, bool fullTypeName)
        {            
            var nameFormatting = NameFormatFromBool(fullTypeName) | NameFormatting.GenericParameterName;
            var methodInfo = method as MethodInfo;
            if (methodInfo != null)
            {
                ProcessType(sb, methodInfo.ReturnType, nameFormatting);
                sb.Append(' ');
            }

            sb.Append(method.Name);

            if (method.IsGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();
                sb.Append("<");

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    ProcessType(sb, genericArguments[i], nameFormatting);
                    if (i + 1 < genericArguments.Length)
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(">");
            }

            var parameters = method.GetParameters();

            sb.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var type = parameter.ParameterType;

                if (parameter.IsOut)
                {
                    sb.Append("out ");
                }
                else if (type.IsByRef)
                {
                    sb.Append("ref ");
                }

                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                ProcessType(sb, type, nameFormatting);
                sb.Append(" ");
                sb.Append(parameter.Name);


                if (i + 1 < parameters.Length)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");
        }

        [Flags]
        private enum NameFormatting
        {
            Full = 1,
            Short = 2,
            GenericParameterName = 4
        }
    }
}

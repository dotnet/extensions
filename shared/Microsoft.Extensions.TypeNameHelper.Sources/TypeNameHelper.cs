// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

        public static string GetTypeDisplayName(object item, bool fullName = true)
        {
            return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
        }

        public static string GetTypeDisplayName(Type type, bool fullName = true)
        {
            var sb = new StringBuilder();
            ProcessTypeName(type, sb, fullName ? NameFormatting.Full : NameFormatting.Short);
            return sb.ToString();
        }

        public static string GetMethodDisplayName(MethodBase method, bool fullTypeName = true)
        {
            var sb = new StringBuilder();
            ProcessMethodName(method, sb, fullTypeName);
            return sb.ToString();
        }

        private static void ProcessMethodName(MethodBase method, StringBuilder sb, bool fullTypeName)
        {
            var nameFormatting = fullTypeName ? NameFormatting.FullIfAvailible : NameFormatting.Short;
            var methodInfo = method as MethodInfo;

            if (methodInfo != null)
            {
                ProcessTypeName(methodInfo.ReturnType, sb, nameFormatting);
                sb.Append(' ');
            }

            sb.Append(method.Name);

            if (method.IsGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();
                sb.Append("<");

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    ProcessTypeName(genericArguments[i], sb, nameFormatting);
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

                ProcessTypeName(type, sb, nameFormatting);
                sb.Append(" ");
                sb.Append(parameter.Name);

                if (i + 1 < parameters.Length)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");
        }

        private static void AppendGenericArguments(Type[] args, int startIndex, int numberOfArgsToAppend, StringBuilder sb, NameFormatting nameFormatting)
        {
            var totalArgs = args.Length;
            if (totalArgs >= startIndex + numberOfArgsToAppend)
            {
                sb.Append("<");
                for (int i = startIndex; i < startIndex + numberOfArgsToAppend; i++)
                {
                    ProcessTypeName(args[i], sb, nameFormatting);
                    if (i + 1 < startIndex + numberOfArgsToAppend)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(">");
            }
        }

        private static string GetName(Type type, NameFormatting nameFormatting)
        {
            switch (nameFormatting)
            {
                case NameFormatting.Short:
                    return type.Name;
                case NameFormatting.FullIfAvailible:
                    return type.FullName ?? type.Name;
                case NameFormatting.Full:
                default:
                    return type.FullName;
            }
        }

        private static void ProcessTypeName(Type t, StringBuilder sb, NameFormatting nameFormatting)
        {
            if (t.GetTypeInfo().IsGenericType)
            {
                ProcessNestedGenericTypes(t, sb, nameFormatting);
                return;
            }
            if (_builtInTypeNames.ContainsKey(t))
            {
                sb.Append(_builtInTypeNames[t]);
            }
            else
            {
                sb.Append(GetName(t, nameFormatting));
            }
        }

        private static void ProcessNestedGenericTypes(Type t, StringBuilder sb, NameFormatting nameFormatting)
        {
            var isFullName = nameFormatting == NameFormatting.Full || nameFormatting == NameFormatting.FullIfAvailible;
            var genericFullName = t.GetGenericTypeDefinition().FullName;
            var genericSimpleName = t.GetGenericTypeDefinition().Name;
            var parts = genericFullName.Split('+');
            var genericArguments = t.GetTypeInfo().GenericTypeArguments;
            var index = 0;
            var totalParts = parts.Length;
            if (totalParts == 1)
            {
                var part = parts[0];
                var num = part.IndexOf('`');
                if (num == -1) return;

                var name = part.Substring(0, num);
                var numberOfGenericTypeArgs = int.Parse(part.Substring(num + 1));
                sb.Append(isFullName ? name : genericSimpleName.Substring(0, genericSimpleName.IndexOf('`')));
                AppendGenericArguments(genericArguments, index, numberOfGenericTypeArgs, sb, nameFormatting);
                return;
            }
            for (var i = 0; i < totalParts; i++)
            {
                var part = parts[i];
                var num = part.IndexOf('`');
                if (num != -1)
                {
                    var name = part.Substring(0, num);
                    var numberOfGenericTypeArgs = int.Parse(part.Substring(num + 1));
                    if (isFullName || i == totalParts - 1)
                    {
                        sb.Append(name);
                        AppendGenericArguments(genericArguments, index, numberOfGenericTypeArgs, sb, nameFormatting);
                    }
                    if (isFullName && i != totalParts - 1)
                    {
                        sb.Append("+");
                    }
                    index += numberOfGenericTypeArgs;
                }
                else
                {
                    if (isFullName || i == totalParts - 1)
                    {
                        sb.Append(part);
                    }
                    if (isFullName && i != totalParts - 1)
                    {
                        sb.Append("+");
                    }
                }
            }
        }

        private enum NameFormatting
        {
            Full,
            Short,
            FullIfAvailible
        }
    }
}

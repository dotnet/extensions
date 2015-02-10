// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Framework.Logging.Internal
{
    public static class TypeNameHelper
    {
        public static string GetTypeDisplayFullName(Type type)
        {
            var sb = new StringBuilder(64);
            ProcessTypeName(type, sb);
            return sb.ToString();
        }

        private static void AppendGenericArguments(Type[] args, StringBuilder sb)
        {
            if (args.Length > 0)
            {
                sb.Append("<");
                for (int i = 0; i < args.Length; i++)
                {
                    ProcessTypeName(args[i], sb);
                    if (i + 1 < args.Length)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(">");
            }
        }

        private static Type GetMostGenericTypeDefinition(Type t)
        {
            while (t.GetTypeInfo().IsGenericType)
            {
                var genericTypeDefinition = t.GetGenericTypeDefinition();
                if (genericTypeDefinition == null || t == genericTypeDefinition)
                {
                    return t;
                }
                t = genericTypeDefinition;
            }
            return t;
        }

        private static void ProcessTypeName(Type t, StringBuilder sb)
        {
            if (t.IsGenericParameter)
            {
                sb.Append(t.GetTypeInfo().Name);
                return;
            }
            if (t.GetTypeInfo().IsGenericType)
            {
                var mostGenericTypeDefinition = GetMostGenericTypeDefinition(t);
                sb.Append(GetSimpleGenericTypeName(mostGenericTypeDefinition));
                AppendGenericArguments(t.GetTypeInfo().GenericTypeArguments, sb);
                return;
            }
            sb.Append(GetSimpleGenericTypeName(t));
        }

        private static string GetSimpleGenericTypeName(Type t)
        {
            var text = t.FullName;
            if (text == null)
            {
                text = t.Name;
            }
            var num = text.IndexOf('`');
            return num != -1 ? text.Substring(0, num) : text;
        }
    }
}
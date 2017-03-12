// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class StackTraceHelper
    {
        public static IList<StackFrameInfo> GetFrames(Exception exception)
        {
            var frames = new List<StackFrameInfo>();

            if (exception == null)
            {
                return frames;
            }

            var needFileInfo = true;
            var stackTrace = new System.Diagnostics.StackTrace(exception, needFileInfo);
            var stackFrames = stackTrace.GetFrames();

            if (stackFrames == null)
            {
                return frames;
            }

            foreach (var frame in stackFrames)
            {
                var method = frame.GetMethod();

                var stackFrame = new StackFrameInfo
                {
                    StackFrame = frame,
                    FilePath = frame.GetFileName(),
                    LineNumber = frame.GetFileLineNumber(),
                    MethodDisplayInfo = GetMethodDisplayString(frame.GetMethod()),
                };

                frames.Add(stackFrame);

            }

            return frames;
        }

        internal static MethodDisplayInfo GetMethodDisplayString(MethodBase method)
        {
            // Special case: no method available
            if (method == null)
            {
                return null;
            }

            var methodDisplayInfo = new MethodDisplayInfo();

            // Type name
            var type = method.DeclaringType;
            if (type != null)
            {
                methodDisplayInfo.DeclaringTypeName = TypeNameHelper.GetTypeDisplayName(type);
            }

            // Method name
            methodDisplayInfo.Name = method.Name;
            if (method.IsGenericMethod)
            {
                var genericArguments = string.Join(", ", method.GetGenericArguments()
                    .Select(arg => TypeNameHelper.GetTypeDisplayName(arg, fullName: false)));
                methodDisplayInfo.GenericArguments += "<" + genericArguments + ">";
            }

            // Method parameters
            methodDisplayInfo.Parameters = method.GetParameters().Select(parameter =>
            {
                var parameterType = parameter.ParameterType;

                var prefix = string.Empty;
                if (parameter.IsOut)
                {
                    prefix = "out";
                }
                else if (parameterType != null && parameterType.IsByRef)
                {
                    prefix = "ref";
                }

                var parameterTypeString = "?";
                if (parameterType != null)
                {
                    if (parameterType.IsByRef)
                    {
                        parameterType = parameterType.GetElementType();
                    }

                    parameterTypeString = TypeNameHelper.GetTypeDisplayName(parameterType, fullName: false);
                }

                return new ParameterDisplayInfo
                {
                    Prefix = prefix,
                    Name = parameter.Name,
                    Type = parameterTypeString,
                };
            });

            return methodDisplayInfo;
        }

    }
}

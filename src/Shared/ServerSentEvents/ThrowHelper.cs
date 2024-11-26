// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable LA0001 // Use Microsoft.Shared.Diagnostics.Throws for improved performance

namespace System.Net.ServerSentEvents
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        public static void ThrowInvalidOperationException_EnumerateOnlyOnce()
        {
            throw new InvalidOperationException("The enumerable may be enumerated only once.");
        }

        public static void ThrowArgumentException_CannotContainLineBreaks(string parameterName)
        {
            throw new ArgumentException("The argument cannot contain line breaks.", parameterName);
        }

        public static void ThrowArgumentException_CannotBeNegative(string parameterName)
        {
            throw new ArgumentException("The argument cannot be a negative value.", parameterName);
        }
    }
}

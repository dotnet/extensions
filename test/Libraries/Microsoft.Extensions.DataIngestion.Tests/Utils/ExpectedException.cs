// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DataIngestion;

internal sealed class ExpectedException : Exception
{
    internal const string ExceptionMessage = "An expected exception occurred.";

    internal ExpectedException()
        : base(ExceptionMessage)
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Tests;

public class TestException : Exception
{
    public TestException()
    {
    }

    public TestException(string message)
        : base(message)
    {
    }

    public TestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TestException(uint hresult, string message, Exception innerException)
        : base(message, innerException)
    {
        HResult = (int)hresult;
    }

    public TestException(uint hresult)
    {
        HResult = (int)hresult;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

#pragma warning disable CA1031 // Do not catch general exception types

internal static class TestExceptionThrower
{
    public static void ThrowExceptionWithoutInnerException()
    {
        new A().InvokingMethodOnClassA();
    }

    public static void ThrowExceptionWithInnerException()
    {
        new C().InvokingMethodOnClassC();
    }

    public static void ThrowExceptionWithMultipleLevelInnerException()
    {
        try
        {
            try
            {
                new C().InvokingMethodOnClassC();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("2nd level exception", innerException: ex);
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("3rd level exception", innerException: ex);
        }
    }

    public static void ThrowExceptionWithMultipleLevelLargeStack()
    {
        Exception innerException;
        try
        {
            try
            {
                new C().InvokingMethodOnClassC();
            }
            catch (Exception ex)
            {
                innerException = ex;
                new F().InvokingMethodOnClassF(1);
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("top level exception", innerException: ex);
        }
    }

    public static void ThrowExceptionWithBigExceptionStack()
    {
        new F().InvokingMethodOnClassF(1);
    }

    internal sealed class A
    {
        private readonly B _obj = new();

        public void InvokingMethodOnClassA()
        {
            _obj.InvokingMethodOnClassB();
        }
    }

    internal sealed class B
    {
        private readonly E _obj = new();

        public void InvokingMethodOnClassB()
        {
            _obj.InvokingMethodOnClassE();
        }
    }

    internal sealed class C
    {
        private readonly D _obj = new();

        public void InvokingMethodOnClassC()
        {
            try
            {
                _obj.InvokingMethodOnClassD();
            }
            catch (Exception ex)
            {
                throw new AggregateException("Exception caught in Class C", innerException: ex);
            }
        }
    }

    internal sealed class D
    {
        private readonly E _obj = new();

        public void InvokingMethodOnClassD()
        {
            _obj.InvokingMethodOnClassE();
        }
    }

    internal sealed class E
    {
        public void InvokingMethodOnClassE()
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class F
    {
        private static readonly G _g = new();
        private readonly int _maxCallCount = 1000;

        public void InvokingMethodOnClassF(int callCount)
        {
            if (callCount > _maxCallCount)
            {
                throw new ArgumentException("call count exceeded max call count", nameof(callCount));
            }

            _g.InvokingMethodOnClassG(callCount + 1);
        }
    }

    internal sealed class G
    {
        private static readonly F _f = new();

#pragma warning disable CA1822 // Mark members as static
        public void InvokingMethodOnClassG(int callCount)
#pragma warning restore CA1822 // Mark members as static
        {
            _f.InvokingMethodOnClassF(callCount + 1);
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}

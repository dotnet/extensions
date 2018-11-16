// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class CallSiteFormatter: CallSiteVisitor<CallSiteFormatter.CallSiteFormatterContext, object>
    {
        internal static CallSiteFormatter Instance = new CallSiteFormatter();

        private CallSiteFormatter()
        {
        }

        public string Format(ServiceCallSite callSite)
        {
            var stringBuilder = new StringBuilder();

            VisitCallSite(callSite, new CallSiteFormatterContext(stringBuilder, 0));

            return stringBuilder.ToString();
        }

        protected override object VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("new {0}", constructorCallSite.ImplementationType);

            var newContext = argument.IncrementOffset();
            foreach (var parameter in constructorCallSite.ParameterCallSites)
            {
                VisitCallSite(parameter, newContext);
            }

            return null;
        }

        protected override object VisitConstant(ConstantCallSite constantCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("{0}", constantCallSite.DefaultValue ?? "<null>");

            return null;
        }

        protected override object VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("<ServiceProvider>");

            return null;
        }

        protected override object VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("<ScopeFactory>");

            return null;
        }

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("IEnumerable<{0}> (size {1})", enumerableCallSite.ItemType, enumerableCallSite.ServiceCallSites.Length);

            var newContext = argument.IncrementOffset();
            foreach (var item in enumerableCallSite.ServiceCallSites)
            {
                VisitCallSite(item, newContext);
            }

            return null;
        }

        protected override object VisitFactory(FactoryCallSite factoryCallSite, CallSiteFormatterContext argument)
        {
            argument.ContinueLine("Factory ({0})", factoryCallSite.Factory.Method);

            return null;
        }

        protected override object VisitDisposeCache(ServiceCallSite callSite, CallSiteFormatterContext argument)
        {
            argument.StartLine("DisposeCache ");

            return base.VisitDisposeCache(callSite, argument);
        }

        protected override object VisitNoCache(ServiceCallSite callSite, CallSiteFormatterContext argument)
        {
            argument.StartLine(string.Empty);

            return base.VisitNoCache(callSite, argument);
        }

        protected override object VisitRootCache(ServiceCallSite callSite, CallSiteFormatterContext argument)
        {
            argument.StartLine("RootCache ");

            return base.VisitRootCache(callSite, argument);
        }

        protected override object VisitScopeCache(ServiceCallSite callSite, CallSiteFormatterContext argument)
        {
            argument.StartLine("ScopeCache ");

            return base.VisitScopeCache(callSite, argument);
        }

        internal readonly struct CallSiteFormatterContext
        {
            public CallSiteFormatterContext(StringBuilder builder, int offset)
            {
                Builder = builder;
                Offset = offset;
            }

            public int Offset { get; }

            public StringBuilder Builder { get; }

            public CallSiteFormatterContext IncrementOffset()
            {
                return new CallSiteFormatterContext(Builder, Offset + 4);
            }

            public void ContinueLine(string format, params object[] args)
            {
                Builder.AppendFormat(format, args);
                Builder.AppendLine();
            }

            public void StartLine(string s)
            {
                for (int i = 0; i < Offset; i++)
                {
                    Builder.Append(' ');
                }

                Builder.Append(s);
            }
        }
    }
}

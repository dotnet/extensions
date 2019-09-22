// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    // This class walks the service call site tree and tries to calculate approximate
    // code size to avoid array resizings during IL generation
    // It also detects if lock is required for scoped services resolution
    internal sealed class ILEmitCallSiteAnalyzer : CallSiteVisitor<object, ILEmitCallSiteAnalysisResult>
    {
        private const int ConstructorILSize = 6;

        private const int ScopedILSize = 24;
        private const int BaseScopedILSize = 96;

        private const int SingletonILSize = 24;

        private const int ConstantILSize = 4;

        private const int ServiceProviderSize = 1;

        private const int FactoryILSize = 16;

        internal static ILEmitCallSiteAnalyzer Instance { get; } = new ILEmitCallSiteAnalyzer();

        protected override ILEmitCallSiteAnalysisResult VisitDisposeCache(ServiceCallSite transientCallSite, object argument) => VisitCallSiteMain(transientCallSite, argument);

        protected override ILEmitCallSiteAnalysisResult VisitConstructor(ConstructorCallSite constructorCallSite, object argument)
        {
            var result = new ILEmitCallSiteAnalysisResult(ConstructorILSize);
            foreach (var callSite in constructorCallSite.ParameterCallSites)
            {
                result = result.Add(VisitCallSite(callSite, argument));
            }
            return result;
        }

        protected override ILEmitCallSiteAnalysisResult VisitRootCache(ServiceCallSite singletonCallSite, object argument) => new ILEmitCallSiteAnalysisResult(SingletonILSize);

        protected override ILEmitCallSiteAnalysisResult VisitScopeCache(ServiceCallSite scopedCallSite, object argument) => new ILEmitCallSiteAnalysisResult(ScopedILSize, hasScope: true);

        protected override ILEmitCallSiteAnalysisResult VisitConstant(ConstantCallSite constantCallSite, object argument) => new ILEmitCallSiteAnalysisResult(ConstantILSize);

        protected override ILEmitCallSiteAnalysisResult VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, object argument) => new ILEmitCallSiteAnalysisResult(ServiceProviderSize);

        protected override ILEmitCallSiteAnalysisResult VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, object argument) => new ILEmitCallSiteAnalysisResult(ConstantILSize);

        protected override ILEmitCallSiteAnalysisResult VisitIEnumerable(IEnumerableCallSite enumerableCallSite, object argument)
        {
            var result = new ILEmitCallSiteAnalysisResult(ConstructorILSize);
            foreach (var callSite in enumerableCallSite.ServiceCallSites)
            {
                result = result.Add(VisitCallSite(callSite, argument));
            }
            return result;
        }

        protected override ILEmitCallSiteAnalysisResult VisitFactory(FactoryCallSite factoryCallSite, object argument) => new ILEmitCallSiteAnalysisResult(FactoryILSize);

        public ILEmitCallSiteAnalysisResult CollectGenerationInfo(ServiceCallSite callSite)
        {
            ILEmitCallSiteAnalysisResult result = default;

            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                result = result.Add(new ILEmitCallSiteAnalysisResult(BaseScopedILSize, hasScope: true));
            }

            return result.Add(VisitCallSiteMain(callSite, null));
        }
    }
}

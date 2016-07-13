using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class CallSiteVisitor<TArgument, TResult>
    {
        protected virtual TResult VisitCallSite(IServiceCallSite callSite, TArgument argument)
        {
            var factoryService = callSite as FactoryService;
            if (factoryService != null)
            {
                return VisitFactoryService(factoryService, argument);
            }
            var closedIEnumerableCallSite = callSite as ClosedIEnumerableCallSite;
            if (closedIEnumerableCallSite != null)
            {
                return VisitClosedIEnumerable(closedIEnumerableCallSite, argument);
            }
            var constructorCallSite = callSite as ConstructorCallSite;
            if (constructorCallSite != null)
            {
                return VisitConstructor(constructorCallSite, argument);
            }
            var transientCallSite = callSite as TransientCallSite;
            if (transientCallSite != null)
            {
                return VisitTransient(transientCallSite, argument);
            }
            var singletonCallSite = callSite as SingletonCallSite;
            if (singletonCallSite != null)
            {
                return VisitSingleton(singletonCallSite, argument);
            }
            var scopedCallSite = callSite as ScopedCallSite;
            if (scopedCallSite != null)
            {
                return VisitScoped(scopedCallSite, argument);
            }
            var constantCallSite = callSite as ConstantCallSite;
            if (constantCallSite != null)
            {
                return VisitConstant(constantCallSite, argument);
            }
            var createInstanceCallSite = callSite as CreateInstanceCallSite;
            if (createInstanceCallSite != null)
            {
                return VisitCreateInstance(createInstanceCallSite, argument);
            }
            var instanceCallSite = callSite as InstanceService;
            if (instanceCallSite != null)
            {
                return VisitInstanceService(instanceCallSite, argument);
            }
            var serviceProviderService = callSite as ServiceProviderService;
            if (serviceProviderService != null)
            {
                return VisitServiceProviderService(serviceProviderService, argument);
            }
            var emptyIEnumerableCallSite = callSite as EmptyIEnumerableCallSite;
            if (emptyIEnumerableCallSite != null)
            {
                return VisitEmptyIEnumerable(emptyIEnumerableCallSite, argument);
            }
            var serviceScopeService = callSite as ServiceScopeService;
            if (serviceScopeService != null)
            {
                return VisitServiceScopeService(serviceScopeService, argument);
            }
            throw new NotSupportedException($"Call site type {callSite.GetType()} is not supported");
        }

        protected abstract TResult VisitTransient(TransientCallSite transientCallSite, TArgument argument);

        protected abstract TResult VisitConstructor(ConstructorCallSite constructorCallSite, TArgument argument);

        protected abstract TResult VisitSingleton(SingletonCallSite singletonCallSite, TArgument argument);

        protected abstract TResult VisitScoped(ScopedCallSite scopedCallSite, TArgument argument);

        protected abstract TResult VisitConstant(ConstantCallSite constantCallSite, TArgument argument);

        protected abstract TResult VisitCreateInstance(CreateInstanceCallSite createInstanceCallSite, TArgument argument);

        protected abstract TResult VisitInstanceService(InstanceService instanceCallSite, TArgument argument);

        protected abstract TResult VisitServiceProviderService(ServiceProviderService serviceProviderService, TArgument argument);

        protected abstract TResult VisitEmptyIEnumerable(EmptyIEnumerableCallSite emptyIEnumerableCallSite, TArgument argument);

        protected abstract TResult VisitServiceScopeService(ServiceScopeService serviceScopeService, TArgument argument);

        protected abstract TResult VisitClosedIEnumerable(ClosedIEnumerableCallSite closedIEnumerableCallSite, TArgument argument);

        protected abstract TResult VisitFactoryService(FactoryService factoryService, TArgument argument);
    }
}
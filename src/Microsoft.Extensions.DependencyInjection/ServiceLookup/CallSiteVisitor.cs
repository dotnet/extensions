using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class CallSiteVisitor<TArgument, TResult>
    {
        protected virtual TResult VisitCallSite(IServiceCallSite callSite, TArgument argument)
        {
            switch (callSite)
            {
                case FactoryCallSite factoryCallSite:
                    return VisitFactory(factoryCallSite, argument);
                case IEnumerableCallSite enumerableCallSite:
                    return VisitIEnumerable(enumerableCallSite, argument);
                case ConstructorCallSite constructorCallSite:
                    return VisitConstructor(constructorCallSite, argument);
                case TransientCallSite transientCallSite:
                    return VisitTransient(transientCallSite, argument);
                case SingletonCallSite singletonCallSite:
                    return VisitSingleton(singletonCallSite, argument);
                case ScopedCallSite scopedCallSite:
                    return VisitScoped(scopedCallSite, argument);
                case ConstantCallSite constantCallSite:
                    return VisitConstant(constantCallSite, argument);
                case CreateInstanceCallSite createInstanceCallSite:
                    return VisitCreateInstance(createInstanceCallSite, argument);
                case ServiceProviderCallSite serviceProviderCallSite:
                    return VisitServiceProvider(serviceProviderCallSite, argument);
                case ServiceScopeFactoryCallSite scopeFactoryCallSite:
                    return VisitServiceScopeFactory(scopeFactoryCallSite, argument);
                default:
                    throw new NotSupportedException($"Call site type {callSite.GetType()} is not supported");
            }
        }

        protected abstract TResult VisitTransient(TransientCallSite transientCallSite, TArgument argument);

        protected abstract TResult VisitConstructor(ConstructorCallSite constructorCallSite, TArgument argument);

        protected abstract TResult VisitSingleton(SingletonCallSite singletonCallSite, TArgument argument);

        protected abstract TResult VisitScoped(ScopedCallSite scopedCallSite, TArgument argument);

        protected abstract TResult VisitConstant(ConstantCallSite constantCallSite, TArgument argument);

        protected abstract TResult VisitCreateInstance(CreateInstanceCallSite createInstanceCallSite, TArgument argument);

        protected abstract TResult VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, TArgument argument);

        protected abstract TResult VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, TArgument argument);

        protected abstract TResult VisitIEnumerable(IEnumerableCallSite enumerableCallSite, TArgument argument);

        protected abstract TResult VisitFactory(FactoryCallSite factoryCallSite, TArgument argument);
    }
}
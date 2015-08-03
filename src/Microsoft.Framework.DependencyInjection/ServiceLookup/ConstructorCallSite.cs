// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    internal class ConstructorCallSite : IServiceCallSite
    {
        private readonly ConstructorInfo _constructorInfo;
        private readonly IServiceCallSite[] _parameterCallSites;

        public ConstructorCallSite(ConstructorInfo constructorInfo, IServiceCallSite[] parameterCallSites)
        {
            _constructorInfo = constructorInfo;
            _parameterCallSites = parameterCallSites;
        }

        public ConstructorInfo Constructor => _constructorInfo;

        public object Invoke(ServiceProvider provider)
        {
            object[] parameterValues = new object[_parameterCallSites.Length];
            for (var index = 0; index != parameterValues.Length; ++index)
            {
                parameterValues[index] = _parameterCallSites[index].Invoke(provider);
            }

            try
            {
                return _constructorInfo.Invoke(parameterValues);
            }
            catch (Exception ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // The above line will always throw, but the compiler requires we throw explicitly.
                throw;
            }
        }

        public Expression Build(Expression provider)
        {
            var parameters = _constructorInfo.GetParameters();
            return Expression.New(
                _constructorInfo,
                _parameterCallSites.Select((callSite, index) =>
                    Expression.Convert(
                        callSite.Build(provider),
                        parameters[index].ParameterType)));
        }
    }
}

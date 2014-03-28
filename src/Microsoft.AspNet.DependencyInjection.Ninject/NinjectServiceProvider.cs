using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Parameters;
using Ninject.Syntax;

namespace Microsoft.AspNet.DependencyInjection.Ninject
{
    internal class NinjectServiceProvider : IServiceProvider
    {
        private static readonly MethodInfo _getAll;

        private readonly IResolutionRoot _resolver;
        private readonly IParameter[] _inheritedParameters;
        private readonly object[] _getAllParameters;

        static NinjectServiceProvider()
        {
            _getAll = typeof(ResolutionExtensions).GetMethod(
                "GetAll", new Type[] { typeof(IResolutionRoot), typeof(IParameter[]) });
        }

        public NinjectServiceProvider(IResolutionRoot resolver, IParameter[] inheritedParameters)
        {
            _resolver = resolver;
            _inheritedParameters = inheritedParameters;
            _getAllParameters = new object[] { resolver, inheritedParameters };
        }


        public object GetService(Type type)
        {
            return GetSingleService(type) ?? GetLast(GetAll(type)) ?? GetMultiService(type);
        }

        private object GetSingleService(Type type)
        {
            return _resolver.TryGet(type, _inheritedParameters);
        }

        private IEnumerable GetMultiService(Type collectionType)
        {
            var collectionTypeInfo = collectionType.GetTypeInfo();
            if (collectionTypeInfo.IsGenericType &&
                collectionTypeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var serviceType = collectionType.GetTypeInfo().GenericTypeArguments.Single();
                return GetAll(serviceType);
            }

            return null;
        }

        private IEnumerable GetAll(Type type)
        {
            var getAll = _getAll.MakeGenericMethod(type);
            return (IEnumerable)getAll.Invoke(null, _getAllParameters);
        }

        private static object GetLast(IEnumerable services)
        {
            object result = null;
            foreach (var service in services)
            {
                result = service;
            }
            return result;
        }
    }
}

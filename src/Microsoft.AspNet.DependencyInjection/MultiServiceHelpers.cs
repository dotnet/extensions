using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class MultiServiceHelpers
    {
        public static IEnumerable GetMultiService(Type collectionType, Func<Type, IEnumerable> getAllServices)
        {
            if (IsGenericIEnumerable(collectionType))
            {
                Type serviceType = FirstGenericArgument(collectionType);
                return Cast(getAllServices(serviceType), serviceType);
            }

            return null;
        }

        private static IEnumerable Cast(IEnumerable collection, Type castItemsTo)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            IList castedCollection = CreateEmptyList(castItemsTo);

            foreach (object item in collection)
            {
                castedCollection.Add(item);
            }

            return castedCollection;
        }

        private static bool IsGenericIEnumerable(Type type)
        {
            return type.GetTypeInfo().IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static Type FirstGenericArgument(Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments.Single();
        }

        private static IList CreateEmptyList(Type innerType)
        {
            Type listType = typeof(List<>).MakeGenericType(innerType);
            return (IList)Activator.CreateInstance(listType);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection
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

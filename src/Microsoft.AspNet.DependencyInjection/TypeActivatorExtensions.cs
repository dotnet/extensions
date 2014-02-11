using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class TypeActivatorExtensions
    {
        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static T CreateInstance<T>(this ITypeActivator activator)
        {
            if (activator == null)
            {
                throw new ArgumentNullException("activator");
            }

            return (T)activator.CreateInstance(typeof(T));
        }
    }
}
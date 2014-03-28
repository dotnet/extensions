using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            return (T)services.GetService(typeof(T));
        }

        /// <summary>
        /// Retrieve a service of type T from the IServiceProvider.
        /// Return T's default value if no service of type T is available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        public static T GetServiceOrDefault<T>(this IServiceProvider services)
        {
            try
            {
                return services.GetService<T>();
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "IServiceProvider may throw unknown exceptions")]
        public static bool HasService(this IServiceProvider provider, Type serviceType)
        {
            try
            {
                var obj = provider.GetService(serviceType);

                // Return false for empty enumerables
                if (obj is IEnumerable)
                {
                    return ((IEnumerable)obj).GetEnumerator().MoveNext();
                }

                return obj != null;
            }
            catch
            {
                return false;
            }
        }
    }
}

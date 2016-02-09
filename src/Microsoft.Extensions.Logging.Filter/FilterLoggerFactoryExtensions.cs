using Microsoft.Extensions.Logging.Filter;
using Microsoft.Extensions.Logging.Filter.Internal;

namespace Microsoft.Extensions.Logging
{
    public static class FilterLoggerFactoryExtensions
    {
        public static ILoggerFactory WithFilter(
            this ILoggerFactory loggerFactory,
            IFilterLoggerSettings settings)
        {
            return new FilterLoggerFactory(
                loggerFactory,
                settings);
        }

        public static ILoggerFactory WithFilter(
            this ILoggerFactory loggerFactory,
            FilterLoggerSettings settings)
        {
            return new FilterLoggerFactory(
                loggerFactory,
                settings);
        }
    }
}

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Helper class.
    /// </summary>
    public static class Options
    {
        /// <summary>
        /// Creates a wrapper around an instance of TOptions to return itself as an IOptions.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IOptions<TOptions> Create<TOptions>(TOptions options) where TOptions : class, new()
        {
            return new OptionsWrapper<TOptions>(options);
        }
    }
}

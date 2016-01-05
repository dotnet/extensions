namespace Microsoft.Extensions.Options
{
    public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class, new()
    {
        public OptionsWrapper(TOptions options)
        {
            Value = options;
        }

        public TOptions Value { get; }
    }
}

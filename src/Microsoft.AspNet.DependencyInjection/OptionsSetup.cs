using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public class OptionsSetup<TOptions> : IOptionsSetup<TOptions>
    {
        public Action<TOptions> SetupAction { get; private set; }

        public OptionsSetup(Action<TOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException("setupAction");
            }
            SetupAction = setupAction;
        }

        public void Setup(TOptions options)
        {
            SetupAction(options);
        }

        public int Order { get; set; }
    }
}
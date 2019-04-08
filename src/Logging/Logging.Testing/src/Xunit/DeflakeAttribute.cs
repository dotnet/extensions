using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.Extensions.Logging.Testing
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class DeflakeAttribute : Attribute
    {
        public DeflakeAttribute(int runCount = 10)
        {
            RunCount = runCount;
        }

        public int RunCount { get; }
    }
}

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassImplementingIComparable : IComparable<ClassImplementingIComparable>
    {
        public int CompareTo(ClassImplementingIComparable other) => 0;
    }
}

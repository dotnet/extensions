using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Testing
{
    public static class HelixQueues
    {
        public const string All = "all";
        public const string None = "none";

        // Queue names end in ';' because it makes it easier to concat these into a list using a constant expression:
        //  HelixQueues.Fedora28 + HelixQueues.Centos7

        public const string Fedora28 = "Fedora.28." + HelixSuffix + ";";
        public const string Fedora27 = "Fedora.27." + HelixSuffix + ";";
        public const string Redhat7 = "Redhat.7." + HelixSuffix + ";";
        public const string Debian9 = "Debian.9." + HelixSuffix + ";";
        public const string Debian8 = "Debian.8." + HelixSuffix + ";";
        public const string Centos7 = "Centos.7." + HelixSuffix + ";";
        public const string Ubuntu1604 = "Ubuntu.1604." + HelixSuffix + ";";
        public const string Ubuntu1810 = "Ubuntu.1810." + HelixSuffix + ";";
        public const string macOS1012 = "OSX.1012." + HelixSuffix + ";";
        public const string Windows10 = "Windows.10.Amd64.ClientRS4.VS2017.Open;"; // Doesn't have the default suffix!

        private const string HelixSuffix = "Amd64.Open";
    }
}

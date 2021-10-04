using System;
using System.Collections.Generic;
using System.Text;

namespace Sprint.Parser
{
    public struct ProjectInfo
    {
        public ProjectInfo(IList<NuGetInfo> packages, string sdk, string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(sdk))
            {
                throw new ArgumentException($"'{nameof(sdk)}' cannot be null or whitespace.", nameof(sdk));
            }

            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                throw new ArgumentException($"'{nameof(targetFramework)}' cannot be null or whitespace.", nameof(targetFramework));
            }

            Packages = packages ?? throw new ArgumentNullException(nameof(packages));
            SDK = sdk;
            TargetFramework = targetFramework;
        }

        public IList<NuGetInfo> Packages { get; }

        public string SDK { get; }

        public string TargetFramework { get; }
    }
}

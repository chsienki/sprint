using System;

namespace Sprint.Parser
{
    public struct NuGetInfo
    {
        public NuGetInfo(string name, string version, string? feed)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Feed = feed;
        }

        public string Name { get; }

        public string Version { get; }

        public string? Feed { get; }
    }
}

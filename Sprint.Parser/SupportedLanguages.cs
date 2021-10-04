using System;
using System.Collections.Generic;
using System.Text;

namespace Sprint.Parser
{
    public static class SupportedLanguages
    {
        public const string CSharpExtension = ".cs";

        public const string VisualBasicExtension = ".vb";

        public const string FSharpExtension = ".fs";

        public static readonly string[] SupportedExtensions = new[] { CSharpExtension, VisualBasicExtension, FSharpExtension };

        public static bool IsSupported(string extension) => Array.IndexOf(SupportedExtensions, extension) >= 0;

        public static string GetLineStarter(string extension) => extension switch
        {
            CSharpExtension => "//",
            FSharpExtension => "//",
            VisualBasicExtension => "'",
            _ => throw new NotImplementedException(),
        };
    }
}

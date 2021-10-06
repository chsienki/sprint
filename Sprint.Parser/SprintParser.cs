using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Sprint.Parser
{
    public class SprintParser
    {
        private readonly string lineStarter;

        private readonly StringBuilder? program;

        private IList<NuGetInfo> nuGetPackages = new List<NuGetInfo>();

        private string sdk = "Microsoft.NET.Sdk";

        private string targetFramework = "net6.0";

        private bool hasHashbang = false;

        public SprintParser(string lineStarter, StringBuilder? graduatedProgram = null)
        {
            this.lineStarter = lineStarter;
            this.program = graduatedProgram;
        }

        public static ProjectInfo Parse(string text, string language) => ParseStream(new StringReader(text), language);

        public static ProjectInfo ParseFile(string fileName)
        {
            using var reader = new StreamReader(File.OpenRead(fileName));
            return ParseStream(reader, Path.GetExtension(fileName));
        }

        public static ProjectInfo ParseStream(TextReader reader, string language)
        {
            var parser = new SprintParser(SupportedLanguages.GetLineStarter(language));
            while (reader.Peek() != -1)
            {
                parser.ParseLine(reader.ReadLine());
            }
            return new ProjectInfo(parser.nuGetPackages, parser.sdk, parser.targetFramework, parser.hasHashbang);
        }

        private void ParseLine(string line)
        {
            // handle the #! specially here, and remove it
            if (line.StartsWith("#!"))
            {
                hasHashbang = true;
                return;
            }

            var trimmed = line.Trim();
            if (!TryParseSprintDirective(trimmed))
            {
                // extract out the graduated program if needed
                program?.AppendLine(line);
            }
        }

        private bool TryParseSprintDirective(string fullDirective)
        {
            if (!fullDirective.StartsWith(lineStarter))
            {
                return false;
            }
            fullDirective = fullDirective.Remove(0, lineStarter.Length);

            var index = fullDirective.IndexOf(":");
            if (index == -1)
            {
                return false;
            }

            var directive = fullDirective.Substring(0, index).Trim().ToLower();
            var arguments = fullDirective.Substring(index + 1).Trim();

            switch (directive)
            {
                case "nuget":
                    return ParseNugetDirective(arguments);

                case "sdk":
                    return ParseSingleValueDirective(arguments, (s) => this.sdk = s);

                case "targetframework":
                    return ParseSingleValueDirective(arguments, (s) => this.targetFramework = s);

                default:
                    return false;
            }
        }

        private bool ParseSingleValueDirective(string arguments, Action<string> assign)
        {
            if (string.IsNullOrWhiteSpace(arguments) || arguments.Contains(' '))
                return false;

            assign(arguments);
            return true;
        }

        private bool ParseNugetDirective(string nugetString)
        {
            // expects a format of PackageName [Version [Feed]]
            var parts = nugetString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length > 2)
            {
                //TODO: log error
                return false;
            }

            var packageName = parts[0];
            var version = parts.Length > 1 ? parts[1] : "*";
            var feed = parts.Length > 2 ? parts[2] : null;

            this.nuGetPackages.Add(new NuGetInfo(packageName, version, feed));
            return true;
        }
    }
}

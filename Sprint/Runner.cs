using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Text;
using Sprint.Parser;
using System.Data.HashFunction;
using System.Data.HashFunction.FNV;
using System.Diagnostics;

namespace Sprint
{
    internal class Runner
    {

        public enum Action { Run, Prepare, Graduate, Watch };

        public record Options(FileInfo File, Action Action, bool Verbose, bool Binlog, IConsole Output);

        private static IFNV1a fnva = FNV1aFactory.Instance.Create();
        private readonly FileInfo file;
        private readonly bool verbose;
        private readonly bool log;
        private readonly IConsole output;
        private readonly string outputDir;
        private readonly string outputFile;
        private readonly string projectFilePath;

        internal static int Execute(Options options) => Execute(options.Action, options.File, options.Verbose, options.Binlog, options.Output);

        internal static int Execute(Action action, FileInfo file, bool verbose, bool log, IConsole output)
        {
            if (!file.Exists)
            {
                output.Error.WriteLine($"File {file.Name} does not exist.");
                return -1;
            }

            if (!SupportedLanguages.IsSupported(file.Extension))
            {
                output.Error.WriteLine($"File extension {file.Extension} is not supported.");
                output.Error.WriteLine($"Supported extenions are: {string.Join(", ", SupportedLanguages.SupportedExtensions)}");
                return -2;
            }

            var runner = new Runner(file, verbose, log, output);
            var prepResult = runner.Prep(restore: true, build: action != Action.Prepare);
            if (action == Action.Prepare || prepResult != 0)
                return prepResult;

            if (action == Action.Watch)
                return runner.Watch();
            else
                return runner.Run();
        }

        private Runner(FileInfo file, bool verbose, bool log, IConsole output)
        {
            this.file = file;
            this.verbose = verbose;
            this.log = log;
            this.output = output;

            // directory is a hash of path + filename.
            var tempDirectory = Path.GetTempPath();
            var dirHash = fnva.ComputeHash(file.Directory.FullName).AsHexString();
            this.outputDir = Path.Combine(tempDirectory, dirHash + "_" + file.Name);
            this.projectFilePath = Path.Combine(this.outputDir, $"temp{file.Extension}proj");
            this.outputFile = Path.Combine(this.outputDir, file.Name);
        }

        private int Prep(bool restore, bool build)
        {
            // TODO: check if the first line of the program is #!/
            var projectInfo = SprintParser.ParseFile(file.FullName);

            // compute the file hash, so we can either write it or compare it (//TODO: we should do this as we parse the file!)
            var fileHash = fnva.ComputeHash(File.ReadAllBytes(file.FullName), 1024).AsBase64String();
            var hashPath = Path.Combine(outputDir, "previous.hash");

            if (!Directory.Exists(outputDir))
            {
                LogVerbose("Creating directory " + outputDir);
                Directory.CreateDirectory(outputDir);
            }
            else
            {
                LogVerbose("Directory " + outputDir + " already exists");
                if (File.Exists(hashPath))
                {
                    var previous = File.ReadAllText(hashPath);
                    if (previous == fileHash)
                    {
                        // no work to do for prep
                        LogVerbose("File hashes match. No prep required.");
                        return 0;
                    }
                }
            }

            LogVerbose("Writing hash file");
            File.WriteAllText(hashPath, fileHash);

            // copy the program over, and write out the project file
            File.Copy(file.FullName, outputFile, true);

            WriteProjectFile(projectInfo);

            if (restore)
            {
                // restore the project
                LogVerbose("Restoring project ...");
                var restoreCode = RunExe("dotnet", $"msbuild -t:restore -v:q -nologo {GetLogArgs(true)} {projectFilePath}");
                if (restoreCode != 0)
                {
                    LogVerbose("Restore Failed.");
                    return restoreCode;
                }
            }

            if (build)
            {
                LogVerbose("Building project...");
                var buildCode = RunExe("dotnet", $"build -nologo -v:q -consoleLoggerParameters:NoSummary -nologo {GetLogArgs()} {projectFilePath}");
                if (buildCode != 0)
                {
                    LogVerbose("Build Failed.");
                    return buildCode;
                }
            }

            return 0;
        } 

        private int Run()
        {
            LogVerbose("Running program");
            return RunExe("dotnet", $"run --no-build --project {projectFilePath}", redirect: false);
        }

        private int Watch()
        {
            LogVerbose("Watching program");

            // when watching we set up a file listener for the original. we re-parse and copy accross any changes as they occur
            using var watcher = new FileSystemWatcher(file.Directory.FullName);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += Watcher_Changed;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            // TODO: we should stream the output from watch so we can replace the paths too
            return RunExe("dotnet", $"watch --project {projectFilePath}", redirect: false);

            void Watcher_Changed(object sender, FileSystemEventArgs e)
            {
                if (e.Name == this.file.Name)
                {
                    this.Prep(restore: false, build: false);
                }
            }
        }

        private int RunExe(string processname, string args, bool redirect = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo(processname, args) { WorkingDirectory = outputDir, RedirectStandardOutput = redirect };
            LogVerbose($"{psi.FileName} {psi.Arguments}");
            var process = Process.Start(psi);
            process.WaitForExit();

            if (redirect && (verbose || process.ExitCode != 0))
            {
                output.Out.Write(process.StandardOutput.ReadToEnd().Replace(outputDir, file.Directory.FullName));
            }

            return process.ExitCode;
        }

        private string GetLogArgs(bool restore = false)
        {
            if (!this.log)
            {
                return String.Empty;
            }
            string logLocation = Path.ChangeExtension(file.FullName, $"{(restore ? "restore." : "")}binlog");
            LogVerbose("Writing Log file to: " + logLocation);
            return "-bl:\"" + logLocation + "\"";
        }

        private int Graduate()
        {
            return 0;
        }

        private void WriteProjectFile(ProjectInfo projectInfo)
        {
            using var stringWriter = new StreamWriter(this.projectFilePath, false);
            stringWriter.WriteLine($@"<Project Sdk=""{projectInfo.SDK}"">");

            stringWriter.WriteLine("    <PropertyGroup>");
            stringWriter.WriteLine("        <OutputType>exe</OutputType>");
            stringWriter.WriteLine("        <LangVersion>latest</LangVersion>");
            stringWriter.WriteLine($"        <TargetFramework>{projectInfo.TargetFramework}</TargetFramework>");
            stringWriter.WriteLine("    </PropertyGroup>");


            stringWriter.WriteLine("    <ItemGroup>");
            stringWriter.WriteLine(@$"        <Compile Include=""{outputFile}"" />");
            foreach (var package in projectInfo.Packages)
            {
                stringWriter.WriteLine(@$"        <PackageReference Include=""{package.Name}"" Version=""{package.Version}"" />");
            }
            stringWriter.WriteLine("    </ItemGroup>");

            stringWriter.WriteLine("</Project>");
        }

        private void LogVerbose(string s)
        {
            if (verbose)
            {
                output.Out.WriteLine(s);
            }
        }
    }
}

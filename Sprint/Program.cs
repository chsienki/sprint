using Sprint;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

return new RunnerCommand(Runner.Action.Run, "")
{
    new RunnerCommand(Runner.Action.Prepare, "Prepares a file for execution without actually running it."),
    new RunnerCommand(Runner.Action.Watch, "Executes the file under dotnet-watch for real time updates."),
    //new RunnerCommand(Runner.Action.Graduate, "Converts a dotnet code file into a full dotnet project.")
    //{
    //    new Option(new[] { "-f", "--force" }, "Force in place graduation"),
    //    new Option<DirectoryInfo?>(new[]{ "-o", "--output"}, "Specify an output location for the graduated project")
    //},
}
.Invoke(args);

class RunnerCommand : Command
{
    Argument<FileInfo> fileArg = new("file", "The file to execute. Supported extensions are .cs, .fs and .vb");
    Option<bool> verbose = new (new[] { "--verbose", "-v" }, "Display verbose output.");
    Option<bool> binlog = new (new[] { "--binlog", "-b" }, "Produce a binary log of the build next to the input file. Useful for debugging.");

    public RunnerCommand(Runner.Action verb, string description) 
        : base(verb.ToString().ToLower(), description)
    {
        // run is the default action
        if(verb == Runner.Action.Run)
        {
            this.Name = "Sprint";
        }

        this.Handler = CommandHandler.Create<Runner.Options>(o => Runner.Execute(o with { Action = verb }));
        this.Add(fileArg);
        this.Add(verbose);
        this.Add(binlog);
    }
}

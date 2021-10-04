# Sprint

>_Get from A to B fast. Like a run, but different._

dotnet sprint allows you to run a execute a single dotnet source file from the command line, without the need for a separate project file.

## Basic Usage

Sprint allows you to embed information that is usually written in a project file directly into a source file via [comment directives](#supported-directives) . When you execute the file via `dotnet sprint` these directives are parsed and used to compile an executable that is then run.

For example, given jsonDemo.cs:

```cs
// nuget: Newtonsoft.Json
using Newtonsoft.Json;

Console.WriteLine(JsonConvert.SerializeObject(new { hello = "world" }));
```

`dotnet sprint jsonDemo.cs` will compile and execute the code, producing the following output: `{"hello" : "world" }`

Under the covers, sprint will parse the `nuget:` comment directive, and pull the required dependency from nuget as required. It will then compile and run the program, without you needing to think about a separate project file.

Sprint supports `.cs`, `.vb`, and `.fs` files (using each languages comment syntax), and the use of comments means source files can be easily written in an IDE such as VS code, as they remain valid source files. 

Sprint can also be instructed to `watch` a given file, which allows you to edit and apply changes to your program while it is still running. This is especially useful for aspnetcore sites: `dotnet sprint watch mySingleAspSite.cs`

## Installation and usage

Sprint is distributed as a dotnet global tool. Simply run the following command to install:

```text
dotnet tool install --global sprint --version 0.0.1-alpha
```

```txt
Usage:
  Sprint [options] <file> [command]

Arguments:
  <file>  The file to execute. Supported extensions are .cs, .fs and .vb

Options:
  -v, --verbose   Display verbose output.
  -b, --binlog    Produce a binary log of the build next to the input file. Useful for debugging.
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  prepare <file>  Prepares a file for execution without actually running it.
  watch <file>    Executes the file under dotnet-watch for real time updates.
```

## Rationale

For most applications, a project file is a good thing: it keeps concerns separated, and allows for a sensible organization of many source files and options into a single unit. 

However, with the introduction of new language features (such as C# top level statements and global usings) it has become extremely simple to write only a few lines of source code to have a fully functional program.

In a language like C, simple programs can be easily compiled and executed via `clang myfile.c && ./a.out`. dotnet in contrast, requires many references to be passed to the compiler even for a simple 'hello world' to compile correctly, meaning command line compilation is infeasible. Instead we use `msbuild` and a project file (`.[cs,vb,fs]proj`) to collect all the required arguments for the compiler.

While at some point in compiling C code you are likely to switch to a build system like `make`, in dotnet, even for simple programs you are forced to create a project file that is mostly empty[^1]. Dotnet sprint aims to address this gap by making it simple to compile and execute a single source file, without the need to create a parallel project file.

Of course in todays software development world its difficult to make a useful program without depending on third party libraries, and sprint aims to allow you to 'grow up' your project via comment directives. These allow you to control aspects of your program such as which NuGet packages it depends on, and which dotnet version you want to target.

[^1]: Of course it only _appears_ empty thanks to the work of minimal project files. In reality the build system is pulling in a lot of complex logic to compile your application.

## Supported Directives

Comment directives in sprint allow your source code to communicate things that are usually controlled via the project file. They consist of a single line comment in your source file in the form of `directive: args`

### Nuget Directive

Syntax: `//nuget: PackageName [Version [Feed]]`

Allows you to specify a nuget package to restore and include as a dependency.

**PackageName:** _Required_ The name of the package to restore.
**Version:**  _Optional_ The version of the package to restore. Supports nuget package format (e.g. `[3.0,4.0)` or `3.*`). Defaults to `*` (latest stable).
**Feed:** _Optional. Only valid with Version_ The url of the NuGet feed that includes this package.

### SDK Directive

Syntax: `//sdk: SDKName`

Allows you to specify the SDK to build this application against.

**SDKName**: _Required_ The MSBuild SDK to build this project against (eg. `Microsoft.NET.Sdk` or `Microsoft.NET.Sdk.Web`)

When not specified, defaults to `Microsoft.NET.Sdk`. Multiple sdk directives can be specified, but only the last one in the file is respected.

### Target Framework Directive

Syntax: `//targetFramework: TFM`

Allows you to specify the target framework to build this project against. 

**TFM**: _Required_ The target framework to build this project against (eg. `net5.0` or `netcoreapp3.1`)

When not specified, defaults to `net6.0`. Multiple target framework directives can be specified, but only the last one in the file is respected.

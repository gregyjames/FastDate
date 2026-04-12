using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Pack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Release : Configuration.Release;
    
    [Parameter("Version number for the package")]
    readonly string Version = "1.0.0";
    
    [Solution] readonly Solution Solution;
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath RustLibDirectory => RootDirectory / "rust-lib";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath LibsDirectory => RootDirectory / "libs";
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
            LibsDirectory.CreateOrCleanDirectory();
            
            // Clean Rust target directory
            var rustTarget = RustLibDirectory / "target";
            if (Directory.Exists(rustTarget))
            {
                rustTarget.DeleteDirectory();
            }
        });

    Target BuildRust => _ => _
        .Executes(() =>
        {
            Log.Information("Building Rust in {config} mode", Configuration);
            
            var cargoArgs = Configuration == Configuration.Release 
                ? "build --release --quiet" 
                : "build";
            
            var result = ProcessTasks.StartProcess(
                "cargo",
                cargoArgs,
                RustLibDirectory,
                logOutput: true,
                logInvocation: true,
                logger: (type, s) =>
                {
                    Log.Information(s);
                }
            ).AssertWaitForExit();
            
            Assert.True(result.ExitCode == 0, "Cargo build failed");
            
            var targetDir = Configuration == Configuration.Release
                ? RustLibDirectory / "target" / "release"
                : RustLibDirectory / "target" / "debug";
            
            LibsDirectory.CreateDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Information("Copying dll for Windows...");
                (targetDir / "rust_lib.dll").Copy(LibsDirectory / "rust_lib.dll", ExistsPolicy.FileOverwrite);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Log.Information("Copying dll for Linux...");
                (targetDir / "librust_lib.so").Copy(LibsDirectory / "librust_lib.so", ExistsPolicy.FileOverwrite);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Log.Information("Copying dll for OSX...");
                (targetDir / "librust_lib.dylib").Copy(LibsDirectory / "librust_lib.dylib", ExistsPolicy.FileOverwrite);
            }
            
            Log.Information("Rust library built and copied successfully");
        });
            
    Target Restore => _ => _
        .DependsOn(BuildRust)
        .Executes(() =>
        {
            DotNetRestore(s =>
                s.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetVersion(Version));
        });
    
    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetOutputDirectory(ArtifactsDirectory)
                .SetVersion(Version)
                .SetProperty("PackageVersion", Version));
            
            Log.Information($"Created nuget package with version: {Version}");
        });

}

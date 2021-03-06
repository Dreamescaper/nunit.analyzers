#tool nuget:?package=NUnit.ConsoleRunner&version=3.8.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "0.1.0";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var SRC_DIR = PROJECT_DIR + "src/";
var PACKAGE_DIR = PROJECT_DIR + "package/" + configuration;

var PLAYGROUND_OUTPUT_DIR = SRC_DIR + "nunit.analyzers.playground/bin/";
var ANALYZERS_TESTS_OUTPUT_DIR = SRC_DIR + "nunit.analyzers.tests/bin/";
var ANALYZERS_OUTPUT_DIR = SRC_DIR + "nunit.analyzers/bin/";

// Solution
var SOLUTION_FILE = PROJECT_DIR + "src/nunit.analyzers.sln";

// Test Assembly
var TEST_FILE = ANALYZERS_TESTS_OUTPUT_DIR + configuration + "/net461/nunit.analyzers.tests.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
};

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    if (BuildSystem.IsRunningOnAppVeyor)
    {
        var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
        var branch = AppVeyor.Environment.Repository.Branch;
        var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

        if (branch == "master" && !isPullRequest)
        {
            packageVersion = version + "-dev-" + buildNumber + dbgSuffix;
        }
        else
        {
            var suffix = "-ci-" + buildNumber + dbgSuffix;

            if (isPullRequest)
                suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
            else if (AppVeyor.Environment.Repository.Branch.StartsWith("release", StringComparison.OrdinalIgnoreCase))
                suffix += "-pre-" + buildNumber;
            else
                suffix += "-" + branch;

            // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
            if (suffix.Length > 21)
                suffix = suffix.Substring(0, 21);

            packageVersion = version + suffix;
        }

        AppVeyor.UpdateBuildVersion(packageVersion);
    }

    // Executed BEFORE the first task.
    Information("Building {0} version {1} of NUnit.Analyzers", configuration, packageVersion);
});

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(PLAYGROUND_OUTPUT_DIR);
        CleanDirectory(ANALYZERS_TESTS_OUTPUT_DIR);
        CleanDirectory(ANALYZERS_OUTPUT_DIR);
    });


//////////////////////////////////////////////////////////////////////
// RESTORE NUGET PACKAGES
//////////////////////////////////////////////////////////////////////

Task("RestorePackages")
    .Does(() =>
    {
        NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings
        {
            Source = PACKAGE_SOURCE,
        });
    });

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
    {
        MSBuild(SOLUTION_FILE, new MSBuildSettings()
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
            .SetPlatformTarget(PlatformTarget.MSIL)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
        );
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        NUnit3(TEST_FILE);
    })
    .Finally(() =>
    {
        if (AppVeyor.IsRunningOnAppVeyor)
        {
            AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
        }
    });


//////////////////////////////////////////////////////////////////////
// Pack
//////////////////////////////////////////////////////////////////////

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        NuGetPack("./src/nunit.analyzers/nunit.analyzers.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            OutputDirectory = PACKAGE_DIR,
            Properties = new Dictionary<string, string>()
            {
                {"Configuration", configuration}
            }
        });
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Appveyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

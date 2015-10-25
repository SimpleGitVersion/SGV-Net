# SGV-Net
Contains SimpleGitVersion.Core, SimpleGitVersionTask (MSBuild task) and other tools that support CSemver on .Net platform.

master: [![Build status](https://ci.appveyor.com/api/projects/status/6gjisya5id62i720/branch/master?svg=true)](https://ci.appveyor.com/project/olivier-spinelli/sgv-net/branch/master)

develop: [![Build status](https://ci.appveyor.com/api/projects/status/6gjisya5id62i720/branch/develop?svg=true)](https://ci.appveyor.com/project/olivier-spinelli/sgv-net/branch/develop)

# MSBuild: SimpleGitVersionTask
## Installation
- Installs the NuGet package in your project.
- Removes any AssemblyVersion, AssemblyFileVersion and AssemblyInformationalVersion attributes
from the project: the task automatically generate them during build.

You can uninstall it at anytime.

## What it does
Once installed, your assemply is necessarily tagged with version information obtained from the Git repository.
- AssemblyVersion is set to Major.Minor.0.0
- AssemblyFileVersion is set to the CSemVer version number.
- AssemblyInformationalVersion contains the expected NuGet package version followed by the commit SHA1 and the Machine\User that generated the assembly.

If anything prevents a correct version to be assigned (typical case is when some files are not committed) this becomes:
- AssemblyVersion is set to 0.0.0.0
- AssemblyFileVersion is set 0.0.0.0.
- AssemblyInformationalVersion contains the error message followed by the commit SHA1 and the Machine\User that generated the assembly.

During build, version information is displayed:

#DNX: SimpleGitVersion.DNXComands:
## Installation
To install the command: dnu commands install --source "C:\Dev\LocalFeed" SimpleGitVersion.DNXCommands 

This command should be installed here: %USERPROFILE%\.dnx\bin\packages\SimpleGitVersion.DNXCommands

To uninstall it: dnu commands uninstall sgv

## Supported commands



# SGV-Net
Contains SimpleGitVersion.Core, SimpleGitVersionTask (MSBuild task) and other tools that support CSemver on .Net platform.

master: [![Build status](https://ci.appveyor.com/api/projects/status/6gjisya5id62i720/branch/master?svg=true)](https://ci.appveyor.com/project/olivier-spinelli/sgv-net/branch/master)

develop: [![Build status](https://ci.appveyor.com/api/projects/status/6gjisya5id62i720/branch/develop?svg=true)](https://ci.appveyor.com/project/olivier-spinelli/sgv-net/branch/develop)

CI Builds packages can be found here: https://www.myget.org/F/invenietis-explore/api/v2

# MSBuild: SimpleGitVersionTask
## Installation
- Installs the NuGet package in your project (https://www.nuget.org/packages/SimpleGitVersionTask/).
- Removes any AssemblyVersion, AssemblyFileVersion and AssemblyInformationalVersion attributes
from the project: the task automatically generate them during build.

You can uninstall it at anytime.

## What it does
Once installed, your assemply is necessarily tagged with version information obtained from the Git repository.
- AssemblyVersion is set to Major.Minor.0.0
- AssemblyFileVersion is set to the CSemVer version number.
- AssemblyInformationalVersion contains the expected NuGet package version followed by the commit SHA1 and the Machine\User that generated the assembly.

If anything prevents a correct version to be assigned (typical case is when some local files are not committed) this becomes:
- AssemblyVersion is set to 0.0.0.0
- AssemblyFileVersion is set 0.0.0.0.
- AssemblyInformationalVersion contains the error message followed by the commit SHA1 and the Machine\User that generated the assembly.

During a build, logs are displayed (MSBuild outputs):

1>  Working folder has non committed changes.
1>  SimpleGitVersionTask (0.7.2): AssemblyVersion = '0.0', AssemblyFileVersion = '0.0.0.0', AssemblyInformationalVersion = 'Working folder has non committed changes. Sha:baa4033bae4d4faec1950fb2091f64749e7bb56c User:DESKTOP-5BQ4M95\olivi'.

1>  CI release: '0.7.3--ci-explore.12'.
1>  Valid release tags are: v0.5.0-rc.0.1, v0.7.3-alpha, v0.7.3-beta, v0.7.3-chi, v0.7.3-delta, v0.7.3-epsilon, v0.7.3-gamma, v0.7.3-iota, v0.7.3-kappa, v0.7.3-lambda, v0.7.3-mu, v0.7.3-omicron, v0.7.3-prerelease, v0.7.3-rc, v0.7.3, v0.8.0-alpha, v0.8.0-beta, v0.8.0-chi, v0.8.0-delta, v0.8.0-epsilon, v0.8.0-gamma, v0.8.0-iota, v0.8.0-kappa, v0.8.0-lambda, v0.8.0-mu, v0.8.0-omicron, v0.8.0-prerelease, v0.8.0-rc, v0.8.0, v1.0.0-alpha, v1.0.0-beta, v1.0.0-chi, v1.0.0-delta, v1.0.0-epsilon, v1.0.0-gamma, v1.0.0-iota, v1.0.0-kappa, v1.0.0-lambda, v1.0.0-mu, v1.0.0-omicron, v1.0.0-prerelease, v1.0.0-rc, v1.0.0
1>  SimpleGitVersionTask (0.7.2): AssemblyVersion = '0.7', AssemblyFileVersion = '0.2.7789.65507', AssemblyInformationalVersion = '0.7.3-Cexplore-0012 Sha:6b130dba1d67e4e5afc1000649827d4f06f5f840 User:DESKTOP-5BQ4M95\olivi'.

1>  Release: '0.7.3-alpha'.
1>  SimpleGitVersionTask (0.7.2): AssemblyVersion = '0.7', AssemblyFileVersion = '0.2.7789.65508', AssemblyInformationalVersion = '0.7.3-a Sha:d7a16d9ee050864704380fdd9b6165e846cb1fc6 User:DESKTOP-5BQ4M95\olivi'.


#DNX: SimpleGitVersion.DNXComands:
## Installation
To install the command: `dnu commands install --source "C:\Dev\LocalFeed" SimpleGitVersion.DNXCommands` 

This installs the command here: %USERPROFILE%\.dnx\bin\packages\SimpleGitVersion.DNXCommands

To uninstall it: `dnu commands uninstall sgv`

## Supported commands



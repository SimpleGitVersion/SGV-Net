# SGV-Net
Contains SimpleGitVersion.Core, SimpleGitVersionTask (MSBuild task), SimpleGitVersion.DNXCommands (to use with new
class library package), and SimpleGitVersion.Cake that support CSemver on .Net platform.

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
- AssemblyFileVersion is set to the file version number (based on the CSemVer version number).
- AssemblyInformationalVersion contains the expected NuGet package version followed by the commit SHA1 and the Machine\User that generated the assembly.

If anything prevents a correct version to be assigned (typical case is when some local files are not committed) this becomes:
  - AssemblyVersion is set to 0.0.0.0
  - AssemblyFileVersion is set 0.0.0.0.
  - AssemblyInformationalVersion contains the error message followed by the commit SHA1 and the Machine\User that generated the assembly.

During a build, logs are displayed (MSBuild outputs), for example:

 1. when on a branch with CI-Build:
```
1>  CI release: '0.3.1--ci-explore.2'.
1>  Valid release tags are: v0.3.1-alpha, v0.3.1-beta, v0.3.1-delta, v0.3.1-epsilon, v0.3.1-gamma, v0.3.1-kappa, v0.3.1-prerelease, v0.3.1-rc, v0.3.1, v0.4.0-alpha, v0.4.0-beta, v0.4.0-delta, v0.4.0-epsilon, v0.4.0-gamma, v0.4.0-kappa, v0.4.0-prerelease, v0.4.0-rc, v0.4.0, v1.0.0-alpha, v1.0.0-beta, v1.0.0-delta, v1.0.0-epsilon, v1.0.0-gamma, v1.0.0-kappa, v1.0.0-prerelease, v1.0.0-rc, v1.0.0
1>  SimpleGitVersionTask (0.8.0-beta): AssemblyVersion = '0.3', AssemblyFileVersion = '0.1.7709.35683', AssemblyInformationalVersion = '0.3.1-Cexplore-0002 Sha:891237e2d0a3dc94207798e26a4c9e8f3c9e4d88 User:DESKTOP-5BQ4M95\olivi'.
```
2. when a release tag is set:
```
1>  Release: '0.3.1'.
1>  SimpleGitVersionTask (0.8.0-beta): AssemblyVersion = '0.3', AssemblyFileVersion = '0.1.7711.64612', AssemblyInformationalVersion = '0.3.1 Sha:891237e2d0a3dc94207798e26a4c9e8f3c9e4d88 User:DESKTOP-5BQ4M95\olivi'.
```

#DNX: SimpleGitVersion.DNXComands:
## Installation

To install the command:

- The latest release (from NuGet): 
`dnu commands install SimpleGitVersion.DNXCommands` 

- The latest CI-build: 
`dnu commands install -s="https://www.myget.org/F/invenietis-explore/api/v2" SimpleGitVersion.DNXCommands` 

To uninstall it: 
`dnu commands uninstall sgv`

This installs the command here: %USERPROFILE%\.dnx\bin\packages\SimpleGitVersion.DNXCommands

Once installed, add this to any project.json file:
```
  "scripts": {
    "prebuild": "sgv prebuild"
  },
```
The `prebuild` command is described below.

## Supported commands

- `prebuild`: creates or updates the Properties/SGVVersionInfo.cs file with the version attributes:
  - **AssemblyVersion** is set to Major.Minor.0.0
  - **AssemblyFileVersion** is set to the file version number (based on the CSemVer version number).
  - **AssemblyInformationalVersion** contains the expected NuGet package version followed by the commit SHA1 and the Machine\User that generated the assembly.

- `update`: Updates version properties in project.json files based on Git repository.

- `restore`: Restores project.json files that differ only by version properties for this solution. This can safely be called at anytime: if the local project.json differs from the comitted version this will do nothing.

The `update`/`restore` commands should be called before/after `dnu pack`.

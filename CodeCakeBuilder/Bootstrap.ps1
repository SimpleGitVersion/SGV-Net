# Try download NuGet.exe if do not exist.
$toolsDir = Join-Path $PSScriptRoot "Tools"
$nugetExe = Join-Path $toolsDir "nuget.exe"
if (!(Test-Path $nugetExe)) {
    New-Item -ItemType Directory $toolsDir
    Invoke-WebRequest -Uri http://nuget.org/nuget.exe -OutFile $nugetExe
    # Make sure NuGet exists where we expect it.
    if (!(Test-Path $nugetExe)) {
        Throw "Could not find NuGet.exe"
    }
}

# Find MSBuild 4.0.
$dotNetVersion = "4.0"
$regKey = "HKLM:\software\Microsoft\MSBuild\ToolsVersions\$dotNetVersion"
$regProperty = "MSBuildToolsPath"
$msbuildExe = join-path -path (Get-ItemProperty $regKey).$regProperty -childpath "msbuild.exe"
if (!(Test-Path $msbuildExe)) {
    Throw "Could not find msbuild.exe"
}

# Ensures that CodeCakeBuilder project exists.
$builderProj = Join-Path $PSScriptRoot "CodeCakeBuilder.csproj"
if (!(Test-Path $builderProj)) {
    Throw "Could not find CodeCakeBuilder.csproj"
}

$solutionDir = Join-Path $PSScriptRoot  ".."
Push-Location $solutionDir
&$nugetExe restore
if( !($?) ) {
    Pop-Location
    Throw "nuget.exe failed."
} 
Pop-Location
&$msbuildExe $builderProj /p:Configuration=Release


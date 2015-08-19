# Set-ExecutionPolicy RemoteSigned
$nuGet = $env:LOCALAPPDATA + "\NuGet\nuget.exe"

$releaseDir = [System.IO.Path]::GetDirectoryName( $MyInvocation.MyCommand.Path );
$solutionDir = [System.IO.Path]::GetDirectoryName( $releaseDir )

if( !(Test-Path $releaseDir\Release) ) {
    New-Item $releaseDir\Release -ItemType Directory
}

write "Packaging .nuspec in "$releaseDir"..."
Get-ChildItem $releaseDir\*.nuspec | foreach ($_) `
{
    Invoke-Command { `
                        param([String]$Script, [string]$NuSpec, [string]$BasePath, [string]$OutDir ) `
                        &$Script pack $NuSpec -BasePath $BasePath -Output $OutDir `
                   } `
                   -ArgumentList $nuGet, $_.fullname, $solutionDir, $releaseDir\Release
}   
pause
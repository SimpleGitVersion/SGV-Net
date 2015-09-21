using Cake.Common;
using Cake.Common.Solution;
using Cake.Common.IO;
using Cake.Common.Tools.NUnit;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Core;
using SimpleGitVersion;
using Cake.Common.Diagnostics;
using Code.Cake;
using Cake.Common.Text;
using Cake.Common.Tools.NuGet.Pack;
using System;
using System.Linq;
using Cake.Core.Diagnostics;
using Cake.Common.Tools.NuGet.Push;

namespace CodeCake
{
    /// <summary>
    /// Sample build "script".
    /// It can be decorated with AddPath attributes that inject paths into the PATH environment variable. 
    /// </summary>
    [AddPath( "CodeCakeBuilder/tools" )]
    [AddPath( "packages/**/tools*" )]
    public class Build : CodeCakeHost
    {
        public Build()
        {
            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );
            string configuration = null;
            SimpleRepositoryInfo gitInfo = null;

            Task( "Check-Repository" )
                .Does( () =>
                {
                    gitInfo = Cake.GetSimpleRepositoryInfo();
                    if( !gitInfo.IsValid ) throw new Exception( "Repository is not ready to be published." );
                    configuration = gitInfo.IsValidRelease && gitInfo.PreReleaseName.Length == 0 ? "Release" : "Debug";
                    Cake.Information( "Publishing {0} in {1}.", gitInfo.SemVer, configuration );
                } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                {
                    Cake.CleanDirectories( "**/bin/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
                    Cake.CleanDirectories( "**/obj/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
                    Cake.CleanDirectories( releasesDir );
                } );

            Task( "Restore-NuGet-Packages" )
                .Does( () =>
                {
                    Cake.NuGetRestore( "SGV-Net.sln" );
                } );

            Task( "Build" )
                .IsDependentOn( "Clean" )
                .IsDependentOn( "Restore-NuGet-Packages" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                {
                    using( var tempSln = Cake.CreateTemporarySolutionFile( "SGV-Net.sln" ) )
                    {
                        tempSln.ExcludeProjectsFromBuild( "CodeCakeBuilder" );
                        Cake.MSBuild( tempSln.FullPath, settings => 
                        {
                            settings.Configuration = configuration;
                            settings.Verbosity = Verbosity.Minimal;
                            // Always generates Xml documentation. Relies on this definition in the csproj files:
                            //
                            // <PropertyGroup Condition=" $(GenerateDocumentation) != '' ">
                            //   <DocumentationFile>bin\$(Configuration)\$(AssemblyName).xml</DocumentationFile>
                            // </PropertyGroup>
                            //
                            settings.Properties.Add( "GenerateDocumentation", new[] { "true" } );
                        } );
                    }
                } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .Does( () => 
                {
                    Cake.NUnit( "Tests/*.Tests/bin/"+configuration+"/*.Tests.dll", new NUnitSettings() { Framework = "v4.5" } );
                } );

            Task( "Create-NuGet-Packages" )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                {
                    Cake.CreateDirectory( releasesDir );
                    var settings = new NuGetPackSettings()
                    {
                        Version = gitInfo.NuGetVersion,
                        BasePath = Cake.Environment.WorkingDirectory,
                        OutputDirectory = releasesDir
                    };
                    Cake.CopyFiles( "CodeCakeBuilder/NuSpec/*.nuspec", releasesDir );
                    foreach( var nuspec in Cake.GetFiles( releasesDir.Path + "/*.nuspec" ) )
                    {
                        Cake.TransformTextFile( nuspec, "{{", "}}" )
                                .WithToken( "configuration", configuration )
                                .WithToken( "CSemVer", gitInfo.SemVer )
                                .Save( nuspec );
                        Cake.NuGetPack( nuspec, settings );
                    }
                    Cake.DeleteFiles( releasesDir.Path + "/*.nuspec" );
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValidRelease )
                .Does( () =>
                {
                    if( Cake.IsInteractiveMode() )
                    {
                        var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                        if( localFeed != null )
                        {
                            Cake.Information( "LocalFeed directory found: {0}", localFeed );
                            if( Cake.ReadInteractiveOption( "Do you want to publish to LocalFeed?", 'y', 'n' ) == 'y' )
                            {
                                Cake.CopyFiles( releasesDir.Path + "/*.nupkg", localFeed );
                            }
                        }
                    }
                    // Resolves nuget.com API key.
                    var apiKey = Cake.InteractiveEnvironmentVariable( "NUGET_API_KEY" );
                    if( string.IsNullOrEmpty( apiKey ) )
                    {
                        Cake.Information( "Could not resolve NuGet API key. Push to NuGet is skipped." );
                    }
                    else
                    {
                        var settings = new NuGetPushSettings
                        {
                            Source = "https://www.nuget.org/api/v2/package",
                            ApiKey = apiKey
                        };

                        foreach( var nupkg in Cake.GetFiles( releasesDir.Path + "/*.nupkg" ) )
                        {
                            Cake.NuGetPush( nupkg, settings );
                        }
                    }
                } );

            Task( "Default" ).IsDependentOn( "Push-NuGet-Packages" );
        }
    }
}

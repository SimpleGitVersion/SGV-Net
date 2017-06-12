using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public static class ToolSettingsSettingsVersionExtension
    {
        public const string VersionWhenInvalid = "0.0.0-AbsolutelyInvalid";

        /// <summary>
        /// Adds standard version information on <see cref="DotNetCoreSettings"/> objects.
        /// </summary>
        /// <typeparam name="T">Specialized DotNetCoreSettings type.</typeparam>
        /// <param name="this">This settings.</param>
        /// <param name="info">The repository information.</param>
        /// <param name="conf">Optional configuration to apply after version arguments have been injected.</param>
        /// <returns>This settings.</returns>
        public static T AddVersionArguments<T>( this T @this, SimpleRepositoryInfo info, Action<T> conf = null ) where T : DotNetCoreSettings
        {
            AddVersionToolArguments( @this, info );
            conf?.Invoke( @this );
            return @this;
        }

        /// <summary>
        /// Adds standard version information on <see cref="MSBuildSettings"/>.
        /// </summary>
        /// <param name="this">This settings.</param>
        /// <param name="info">The repository information.</param>
        /// <param name="conf">Optional configuration to apply after version arguments have been injected.</param>
        /// <returns>This settings.</returns>
        public static MSBuildSettings AddVersionArguments( this MSBuildSettings @this, SimpleRepositoryInfo info, Action<MSBuildSettings> conf = null )
        {
            AddVersionToolArguments( @this, info );
            conf?.Invoke( @this );
            return @this;
        }
        static void AddVersionToolArguments( Cake.Core.Tooling.ToolSettings t, SimpleRepositoryInfo info )
        {
            string version = VersionWhenInvalid, assemblyVersion = "0.0", fileVersion = "0.0.0.0", informationalVersion = "";
            if( info.IsValid )
            {
                version = info.NuGetVersion;
                assemblyVersion = info.MajorMinor;
                fileVersion = info.FileVersion;
                informationalVersion = $"{info.SemVer} ({info.NuGetVersion}) - SHA1: {info.CommitSha} - CommitDate: {info.CommitDateUtc.ToString( "u" )}";
            }
            var prev = t.ArgumentCustomization;
            t.ArgumentCustomization = args => (prev?.Invoke( args ) ?? args)
                            .Append( $@"/p:CakeBuild=""true""" )
                            .Append( $@"/p:Version=""{version}""" )
                            .Append( $@"/p:AssemblyVersion=""{assemblyVersion}.0""" )
                            .Append( $@"/p:FileVersion=""{fileVersion}""" )
                            .Append( $@"/p:InformationalVersion=""{informationalVersion}""" );
        }

    }


}

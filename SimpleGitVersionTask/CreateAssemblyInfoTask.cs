using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection;
using System.Linq;

namespace SimpleGitVersion
{

    /// <summary>
    /// This task must be used before compile: it creates a 'SimpleGitVersionTaskAssemblyInfo.g.cs' in <see cref="IntermediateOutputPath"/> 
    /// that defines AssemblyVersion, AssemblyFileVersion and AssemblyInformationalVersion attributes.
    /// </summary>
    public class CreateAssemblyInfoTask : Task
    {
        [Required]
        public string GitSolutionDirectory { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public string MajorMinor { get; set; }

        [Required]
        public string MajorMinorPatch { get; set; }

        [Required]
        public string AssemblyFileVersionInfo { get; set; }

        [Required]
        public string AssemblyInformationalVersionInfo { get; set; }

        [Required]
        public string SemVer { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        public CreateAssemblyInfoTask()
        {
        }

        public override bool Execute()
        {
            try
            {
                string text = SimpleRepositoryInfo.FormatAssemblyVersionAttributesFile( MajorMinor, AssemblyFileVersionInfo, SemVer, "SimpleGitVersionTask", AssemblyInformationalVersionInfo );
                this.LogInfo( string.Format( "SimpleGitVersionTask ({3}): AssemblyVersion = '{0}', AssemblyFileVersion = '{1}', AssemblyInformationalVersion = '{2}'.", MajorMinor, AssemblyFileVersionInfo, AssemblyInformationalVersionInfo, SimpleRepositoryInfo.SGVSemVer ) );
                if( !Directory.Exists( IntermediateOutputPath ) )
                {
                    this.LogInfo( string.Format( "Creating IntermediateOutputPath='{0}' directory.", IntermediateOutputPath ) );
                    Directory.CreateDirectory( IntermediateOutputPath );
                }

                AssemblyInfoTempFilePath = Path.Combine( IntermediateOutputPath, "SimpleGitVersionTaskAssemblyInfo.g.cs" );
                File.WriteAllText( AssemblyInfoTempFilePath, text );
                return true;
            }
            catch( Exception exception )
            {
                this.LogError( "Error occurred: " + exception );
                return false;
            }
        }

    }
}

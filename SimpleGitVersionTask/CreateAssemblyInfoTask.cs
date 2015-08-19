using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SimpleGitVersion
{

    /// <summary>
    /// This task must be used before compile: it creates a 'SimpleGitVersionTaskAssemblyInfo.g.cs' in <see cref="IntermediateOutputPath"/> 
    /// that defines AssemblyVersion, AssemblyFileVersion and AssemblyInformationalVersion attributes.
    /// </summary>
    public class CreateAssemblyInfoTask : Task 
    {
        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public string AssemblyVersionInfo { get; set; }

        [Required]
        public string AssemblyFileVersionInfo { get; set; }

        [Required]
        public string AssemblyInformationalVersionInfo { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        public CreateAssemblyInfoTask()
        {
        }

        const string _format = @"
using System;
using System.Reflection;

[assembly: AssemblyVersion(@""{0}"")]
[assembly: AssemblyFileVersion(@""{1}"")]
[assembly: AssemblyInformationalVersion(@""{2}"")]
 ";

        public override bool Execute()
        {
            try
            {
                this.LogInfo( String.Format( "AssemblyVersion = '{0}', AssemblyFileVersion = '{1}', AssemblyInformationalVersion = '{2}'.", AssemblyVersionInfo, AssemblyFileVersionInfo, AssemblyInformationalVersionInfo ) );
                if( !Directory.Exists( IntermediateOutputPath ) )
                {
                    this.LogInfo( String.Format( "Creating IntermediateOutputPath='{0}' directory.", IntermediateOutputPath ) );
                    Directory.CreateDirectory( IntermediateOutputPath );
                }
                AssemblyInfoTempFilePath = Path.Combine( IntermediateOutputPath, "SimpleGitVersionTaskAssemblyInfo.g.cs" );
                var text = String.Format( _format, AssemblyVersionInfo, AssemblyFileVersionInfo, AssemblyInformationalVersionInfo );
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Describes settings for <see cref="DotNetSupport.DotNetRestore"/> method.
    /// </summary>
    public enum DotNetRestoreSettingsVerbosity
    {
        Debug, Verbose, Information, Minimal, Warning, Error
    }

    public class DotNetRestoreSettings
    {
        readonly HashSet<string> _projectPaths;
        readonly HashSet<string> _fallbackSources;

        /// <summary>
        /// Initializes a new empty settings.
        /// </summary>
        public DotNetRestoreSettings()
        {
            _projectPaths = new HashSet<string>();
            _fallbackSources = new HashSet<string>();
            Verbosity = DotNetRestoreSettingsVerbosity.Minimal;
        }

        /// <summary>
        /// The project paths to restore. When empty, defaults to current directory.
        /// Command line option: [root] List of projects and project folders to restore. 
        ///                             Each value can be: a path to a project.json or global.json file, or a 
        ///                             folder to recursively search for project.json files.
        /// </summary>
        public ISet<string> ProjectPaths { get { return _projectPaths; } }

        /// <summary>
        /// The NuGet package source to use during the restore.
        /// Command line option: -s|--source &lt;source&gt; Specifies a NuGet package source to use during the restore.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// A list of packages sources to use as a fallback.
        /// Command line option: -f|--fallbacksource &lt;FEED&gt; A list of packages sources to use as a fallback.
        /// </summary>
        public ISet<string> FallbackSources { get { return _fallbackSources; } }

        /// <summary>
        /// The directory to install packages in.
        /// Command line option: --packages &lt;packagesDirectory&gt; Directory to install packages in.
        /// </summary>
        public string Packages { get; set; }

        /// <summary>
        /// Disables restoring multiple projects in parallel.
        /// Command line option: --disable-parallel Disables restoring multiple projects in parallel.
        /// </summary>
        public bool DisableParallel { get; set; }

        /// <summary>
        /// Do not cache packages and http requests.
        /// Command line option: --no-cache Do not cache packages and http requests.
        /// </summary>
        public bool NoCache { get; set; }

        /// <summary>
        /// The NuGet configuration file to use.
        /// Command line option: --configfile &lt;file&gt; The NuGet configuration file to use.
        /// </summary>
        public string ConfigFile { get; set; }

        /// <summary>
        /// The verbosity of logging to use.
        /// Command line option: -v|--verbosity &lt;verbosity&gt; The verbosity of logging to use.
        /// Default to <see cref="DotNetRestoreSettingsVerbosity.Minimal"/>.
        /// </summary>
        public DotNetRestoreSettingsVerbosity Verbosity { get; set; }

        /// <summary>
        /// Generates the arguments.
        /// </summary>
        /// <returns>The arguments to the dotnet command.</returns>
        public override string ToString()
        {
            return ToString( new StringBuilder() ).ToString();
        }

        /// <summary>
        /// Generates the arguments.
        /// </summary>
        /// <param name="b">The string builder.</param>
        /// <returns>The string builder.</returns>
        public StringBuilder ToString( StringBuilder b )
        {
            foreach( var p in ProjectPaths )
            {
                b.Append( " \"" ).Append( p ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( Source ) )
            {
                b.Append( " -s " ).Append( Source );
            }
            foreach( var fs in FallbackSources )
            {
                b.Append( " -f \"" ).Append( fs ).Append( '"' );
            }
            if( DisableParallel )
            {
                 b.Append( " --disable-parallel" );
            }
            if( NoCache )
            {
                 b.Append( " --no-cache" );
            }
            if( !string.IsNullOrEmpty( ConfigFile ) )
            {
                b.Append( " --configfile " ).Append( ConfigFile );
            }
            b.Append( " -v " ).Append( Verbosity );
            return b;
        }
    }
}

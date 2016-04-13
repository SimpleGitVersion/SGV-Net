using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Describes settings for <see cref="DotNetSupport.DotNetPack"/> method.
    /// </summary>

    public class DotNetPackSettings
    {
        /// <summary>
        /// Initializes a new empty settings.
        /// By default, <see cref="NoBuild"/> is false. 
        /// This should be set to true and dotnet build should be used before to actually build the project.
        /// </summary>
        public DotNetPackSettings()
        {
        }

        /// <summary>
        /// The project to pack, defaults to the current directory. 
        /// Can be a path to a project.json or a project directory.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// Set it to true to not build the project first. 
        /// Command line option: --no-build Do not build project before packing
        /// </summary>
        public bool NoBuild { get; set; }

        /// <summary>
        /// Gets or set the output directory. When null, the /bin sub directory is used.
        /// Command line option: -o|--output &lt;OUTPUT_DIR&gt; Directory in which to place outputs
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// The configurations to build. When empty, defaults to "Debug".
        /// Command line option: -c|--configuration &lt;CONFIGURATION&gt; Configuration under which to build
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets or set the output directory. When null, the /obj sub directory is used.
        /// Command line option: -b|--build-base-path &lt;OUTPUT_DIR&gt; Directory in which to place temporary outputs
        /// </summary>
        public string BuildBasePath { get; set; }

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
            if( !string.IsNullOrEmpty( Project ) )
            {
                b.Append( " \"" ).Append( Project.TrimEnd( Path.DirectorySeparatorChar ).TrimEnd( Path.AltDirectorySeparatorChar ) ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( Output ) )
            {
                b.Append( ' ' ).Append( "-o \"" ).Append( Output ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( Configuration ) )
            {
                b.Append( ' ' ).Append( "-c " ).Append( Configuration );
            }
            if( NoBuild )
            {
                b.Append( "--no-build" );
            }
            return b;
        }
    }
}

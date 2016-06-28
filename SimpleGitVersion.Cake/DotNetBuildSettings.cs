using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Describes settings for <see cref="DotNetSupport.DotNetBuild"/> method.
    /// </summary>
    /// 

  //

  //
  //
  //-r|--runtime<RUNTIME_IDENTIFIER> Produce runtime-specific assets for the specified runtime
  //--version-suffix<VERSION_SUFFIX> Defines what `*` should be replaced with in version field in project.json
  //-n|--native Compiles source to native machine code.
  //-a|--arch<ARCH> The architecture for which to compile.x64 only currently supported.
  //--ilcarg<ARG> Command line option to be passed directly to ILCompiler.
  //--ilcpath<PATH> Path to the folder containing custom built ILCompiler.
  //--ilcsdkpath<PATH> Path to the folder containing ILCompiler application dependencies.
  //--appdepsdkpath<PATH> Path to the folder containing ILCompiler application dependencies.
  //--cpp Flag to do native compilation with C++ code generator.
  //--cppcompilerflags<flags> Additional flags to be passed to the native compiler.
  //--build-profile Set this flag to print the incremental safety checks that prevent incremental compilation
  //--no-incremental Set this flag to turn off incremental build
  //--no-dependencies Set this flag to ignore project to project references and only build the root project

    public class DotNetBuildSettings
    {
        /// <summary>
        /// Initializes a new empty settings.
        /// </summary>
        public DotNetBuildSettings()
        {
        }

        /// <summary>
        /// The project to compile, defaults to the current directory. 
        /// Can be a path to a project.json or a project directory.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// The configurations to build. When empty, defaults to "Debug".
        /// Command line option: -c|--configuration &lt;CONFIGURATION&gt; Configuration under which to build
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// The target frameworks to build. When empty defaults to all the "frameworks" defined in the project.json.
        /// Command line option: -f|--framework &lt;FRAMEWORK&gt; Compile a specific framework
        /// </summary>
        public string Framework { get; set; }

        /// <summary>
        /// Gets or set the output directory. When null, the /bin sub directory is used.
        /// Command line option: -o|--output &lt;OUTPUT_DIR&gt; Directory in which to place outputs
        /// </summary>
        public string Output { get; set; }

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
            if( !string.IsNullOrEmpty( Configuration ) )
            {
                b.Append( ' ' ).Append( "-c " ).Append( Configuration );
            }
            if( !string.IsNullOrEmpty( Framework ) )
            {
                b.Append( ' ' ).Append( "-f " ).Append( Framework );
            }
            if( !string.IsNullOrEmpty( BuildBasePath ) )
            {
                b.Append( ' ' ).Append( "-b \"" ).Append( BuildBasePath ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( Output ) )
            {
                b.Append( ' ' ).Append( "-o \"" ).Append( Output ).Append( '"' );
            }
            return b;
        }
    }
}

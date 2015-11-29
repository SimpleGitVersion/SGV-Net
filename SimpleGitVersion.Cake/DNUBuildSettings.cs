using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    public class DNUBuildSettings
    {
        readonly HashSet<string> _projectPaths;
        readonly HashSet<string> _configurations;
        readonly HashSet<string> _targetFrameworks;

        public DNUBuildSettings()
        {
            _projectPaths = new HashSet<string>();
            _configurations = new HashSet<string>();
            _targetFrameworks = new HashSet<string>();
        }

        /// <summary>
        /// Gets or sets whether the dnu pack must be called (instead of only dnu build).
        /// </summary>
        public bool GeneratePackage { get; set; }

        /// <summary>
        /// Gets or sets whether the display will not provide much information (such as references used).
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// The project paths to pack. When empty, defaults to current directory.
        /// </summary>
        public ISet<string> ProjectPaths { get { return _projectPaths; } }

        /// <summary>
        /// The configurations to build. When empty, defaults to "Debug".
        /// </summary>
        public ISet<string> Configurations { get { return _configurations; } }

        /// <summary>
        /// The target frameworks to build. When empty defaults to all the "frameworks" defined in the project.json.
        /// </summary>
        public ISet<string> TargetFrameworks { get { return _targetFrameworks; } }

        /// <summary>
        /// Gets or set the output directory. When null, the /bin sub directory is used.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Generates the arguments.
        /// </summary>
        /// <returns>The arguments to the dnu command.</returns>
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
            b.Append( GeneratePackage ? "pack" : "build" );
            if( Quiet ) b.Append( ' ' ).Append( "--quiet" );
            if( !string.IsNullOrEmpty( OutputDirectory ) )
            {
                b.Append( ' ' ).Append( "--out \"" ).Append( OutputDirectory ).Append( '"' );
            }
            foreach( var p in ProjectPaths )
            {
                b.Append( " \"" ).Append( p.TrimEnd( Path.DirectorySeparatorChar ).TrimEnd( Path.AltDirectorySeparatorChar ) ).Append( '"' );
            }
            foreach( var t in TargetFrameworks )
            {
                b.Append( ' ' ).Append( "--framework " ).Append( t );
            }
            foreach( var c in Configurations )
            {
                b.Append( ' ' ).Append( "--configuration " ).Append( c );
            }
            return b;
        }
    }
}

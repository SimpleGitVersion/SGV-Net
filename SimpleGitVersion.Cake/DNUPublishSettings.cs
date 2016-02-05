using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Describes settings for <see cref="DNXSupport.DNUPublish"/> method.
    /// </summary>
    public class DNUPublishSettings
    {
        readonly HashSet<string> _projectPaths;
        readonly HashSet<string> _configurations;
        readonly HashSet<string> _targetFrameworks;

        /// <summary>
        /// Initializes a new empty settings.
        /// </summary>
        public DNUPublishSettings()
        {
            _projectPaths = new HashSet<string>();
            _configurations = new HashSet<string>();
            _targetFrameworks = new HashSet<string>();
        }

        /// <summary>
        /// Gets or sets whether the display will not provide much information (such as references used).
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// The project paths to publish. When empty, defaults to current directory.
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
        /// Gets or sets whether the command create or not sources files into the publish directory.
        /// </summary>
        public bool NoSource { get; set; }

        /// <summary>
        /// Overrides the command name to use in the web.config for the httpPlatformHandler. The default is web.
        /// </summary>
        public string IIsCommand { get; set; }

        /// <summary>
        /// Name or full path of the runtime folder to include, or "active" for current runtime on PATH.
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// Build and include native image. User must provide targeted CoreCLR runtime versions along with this option.
        /// </summary>
        public bool Native { get; set; }

        /// <summary>
        /// Include debug symbols in output bundle.
        /// </summary>
        public bool IncludeSymbols { get; set; }

        /// <summary>
        /// Name of public folder in the project directory
        /// </summary>
        public string WwwRoot { get; set; }

        /// <summary>
        /// Name of public folder in the output, can be used only when the WwwRoot option or webroot in project.json is specified.
        /// </summary>
        public string WwwRootOut { get; set; }

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
            if( Quiet ) b.Append( ' ' ).Append( "--quiet" );
            if( NoSource ) b.Append( ' ' ).Append( "--no-source" );
            if( Native ) b.Append( ' ' ).Append( "--native" );
            if( IncludeSymbols ) b.Append( ' ' ).Append( "--include-symbols" );
            if( !string.IsNullOrEmpty( IIsCommand ) )
            {
                b.Append( ' ' ).Append( "--iis-command \"" ).Append( IIsCommand ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( Runtime ) )
            {
                b.Append( ' ' ).Append( "--runtime \"" ).Append( Runtime ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( WwwRoot ) )
            {
                b.Append( ' ' ).Append( "--wwwroot \"" ).Append( WwwRoot ).Append( '"' );
            }
            if( !string.IsNullOrEmpty( WwwRootOut ) )
            {
                b.Append( ' ' ).Append( "--wwwroot-out \"" ).Append( WwwRootOut ).Append( '"' );
            }
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    public class DNURestoreSettings
    {
        readonly HashSet<string> _projectPaths;

        public DNURestoreSettings()
        {
            _projectPaths = new HashSet<string>();
        }

        /// <summary>
        /// Gets or sets whether the display will not provide much information (such as HttpRequest/cache information).
        /// Defaults to false.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// The project paths to restore (can be project.json file path). When empty, defaults to current directory.
        /// </summary>
        public ISet<string> ProjectPaths { get { return _projectPaths; } }

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
            b.Append( "restore" );
            if( Quiet ) b.Append( ' ' ).Append( "--quiet" );
            foreach( var p in ProjectPaths )
            {
                b.Append( " \"" ).Append( p.TrimEnd( Path.DirectorySeparatorChar ).TrimEnd( Path.AltDirectorySeparatorChar ) ).Append( '"' );
            }
            return b;
        }
    }
}

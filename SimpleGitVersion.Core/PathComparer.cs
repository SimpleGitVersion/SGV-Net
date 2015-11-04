using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Simple path comparer.
    /// Just use the <see cref="Default"/> instance.
    /// </summary>
    public sealed class PathComparer : IEqualityComparer<string>
    {
        /// <summary>
        /// Gets the default instance to use.
        /// </summary>
        public readonly static IEqualityComparer<string> Default = new PathComparer();

        bool IEqualityComparer<string>.Equals( string x, string y )
        {
            return StringComparer.OrdinalIgnoreCase.Equals( Normalize( x ), Normalize( y ) );
        }

        int IEqualityComparer<string>.GetHashCode( string path )
        {
            return Normalize( path ).GetHashCode();
        }

        static readonly char[] _dirChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        static string Normalize( string path )
        {
            return path.Trim( _dirChars ).Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
        }
    }

}

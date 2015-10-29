using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Summarizes the kind of release tag.
    /// </summary>
    [Flags]
    public enum ReleaseTagKind
    {
        /// <summary>
        /// Not a release tag.
        /// </summary>
        None = 0,
        /// <summary>
        /// The tag looks like a release tag but is syntaxically incorrect.
        /// </summary>
        Malformed = 1,
        /// <summary>
        /// This release tag is 'Major.Minor.Patch' only.
        /// </summary>
        OfficialRelease = 2,
        /// <summary>
        /// This release tag is 'Major.Minor.Patch-prerelease[.Number[.Fix]]'.
        /// </summary>
        PreRelease = 4,
        /// <summary>
        /// This release tag is +Invalid.
        /// </summary>
        MarkedInvalid = 8
    }

    /// <summary>
    /// Implements <see cref="ReleaseTagKind"/> enum extension methods.
    /// </summary>
    public static class ReleaseTagKindExtensions
    {
        /// <summary>
        /// Returns true if this tag is marked with <see cref="ReleaseTagKind.MarkedInvalid"/>.
        /// </summary>
        /// <param name="this"></param>
        /// <returns>True if MarkedInvalid.</returns>
        public static bool IsMarkedInvalid( this ReleaseTagKind @this )
        {
            return (@this & ReleaseTagKind.MarkedInvalid) != 0;
        }

        /// <summary>
        /// Obtains the marker as a string. <see cref="string.Empty"/> if this is nor marked.
        /// </summary>
        /// <param name="this">This <see cref="ReleaseTagKind"/>.</param>
        /// <param name="prefixPlus">Optionally removes the '+' build meta separator.</param>
        /// <returns>A string with the marker if any.</returns>
        public static string ToStringMarker( this ReleaseTagKind @this, bool prefixPlus = true )
        {
            if( (@this & ReleaseTagKind.MarkedInvalid) != 0 ) 
            {
                return prefixPlus ? "+invalid" : "invalid";
            }
            return string.Empty;
        }


    }
}

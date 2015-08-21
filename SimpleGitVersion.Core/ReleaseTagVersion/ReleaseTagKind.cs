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
        Release = 2,
        /// <summary>
        /// This release tag is 'Major.Minor.Patch-prerelease[.Number[.Fix]]'.
        /// </summary>
        PreRelease = 4,
        /// <summary>
        /// This release tag is +Valid.
        /// </summary>
        MarkedValid = 8,
        /// <summary>
        /// This release tag is +Published.
        /// </summary>
        MarkedPublished = 16,
        /// <summary>
        /// This release tag is +Invalid.
        /// </summary>
        MarkedInvalid = 32
    }

    /// <summary>
    /// Implements <see cref="ReleaseTagKind"/> enum extension methods.
    /// </summary>
    public static class ReleaseTagKindExtensions
    {
        /// <summary>
        /// Returns true if this tag is marked with <see cref="ReleaseTagKind.MarkedValid"/> or <see cref="ReleaseTagKind.MarkedPublished"/> or <see cref="ReleaseTagKind.MarkedInvalid"/>.
        /// </summary>
        /// <param name="this">This <see cref="ReleaseTagKind"/>.</param>
        /// <returns>True if MarkedValid or MarkedPublished or MarkedInvalid.</returns>
        public static bool IsMarked( this ReleaseTagKind @this )
        {
            return (@this & (ReleaseTagKind.MarkedValid | ReleaseTagKind.MarkedPublished | ReleaseTagKind.MarkedInvalid)) != 0;
        }

        /// <summary>
        /// Returns true if this tag is marked with <see cref="ReleaseTagKind.MarkedValid"/>.
        /// </summary>
        /// <param name="this"></param>
        /// <returns>True if MarkedValid.</returns>
        public static bool IsMarkedValid( this ReleaseTagKind @this )
        {
            return (@this & ReleaseTagKind.MarkedValid) != 0;
        }

        /// <summary>
        /// Returns true if this tag is marked with <see cref="ReleaseTagKind.MarkedPublished"/>.
        /// </summary>
        /// <param name="this"></param>
        /// <returns>True if MarkedPublished.</returns>
        public static bool IsMarkedPublished( this ReleaseTagKind @this )
        {
            return (@this & ReleaseTagKind.MarkedPublished) != 0;
        }

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
        /// Returns a <see cref="ReleaseTagKind"/> without markers.
        /// </summary>
        /// <param name="this">This <see cref="ReleaseTagKind"/>.</param>
        /// <returns>Unmarked ReleaseTagKind.</returns>
        public static ReleaseTagKind ClearMarker( this ReleaseTagKind @this )
        {
            return @this & ~(ReleaseTagKind.MarkedValid | ReleaseTagKind.MarkedPublished | ReleaseTagKind.MarkedInvalid);
        }

        /// <summary>
        /// Obtains the marker as a string. <see cref="string.Empty"/> if this is nor marked.
        /// </summary>
        /// <param name="this">This <see cref="ReleaseTagKind"/>.</param>
        /// <param name="prefixPlus">Optionally removes the '+' build meta separator.</param>
        /// <returns>A string with the marker if any.</returns>
        public static string ToStringMarker( this ReleaseTagKind @this, bool prefixPlus = true )
        {
            switch( @this & (ReleaseTagKind.MarkedValid | ReleaseTagKind.MarkedPublished | ReleaseTagKind.MarkedInvalid) ) 
            {
                case ReleaseTagKind.MarkedValid: return prefixPlus ? "+valid" : "valid";
                case ReleaseTagKind.MarkedPublished: return prefixPlus ? "+published" : "published";
                case ReleaseTagKind.MarkedInvalid: return prefixPlus ? "+invalid" : "invalid";
            }
            return string.Empty;
        }


    }
}

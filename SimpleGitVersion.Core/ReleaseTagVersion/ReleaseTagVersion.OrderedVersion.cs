using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public sealed partial class ReleaseTagVersion
    {
        [StructLayout( LayoutKind.Explicit)]
        struct SOrderedVersion
        {
            [FieldOffset( 0 )]
            public long Number;
            [FieldOffset( 6 )]
            public UInt16 Major;
            [FieldOffset( 4 )]
            public UInt16 Minor;
            [FieldOffset( 2 )]
            public UInt16 Build;
            [FieldOffset( 0 )]
            public UInt16 Revision;
         }

        readonly SOrderedVersion _orderedVersion;

        /// <summary>
        /// The maximum number of major versions.
        /// </summary>
        public const int MaxMajor = 99999;
        /// <summary>
        /// The maximum number of minor versions for a major one.
        /// </summary>
        public const int MaxMinor = 49999;
        /// <summary>
        /// The maximum number of patches for a minor one.
        /// </summary>
        public const int MaxPatch = 9999;
        /// <summary>
        /// The maximum number of prereleaseis also the index of the "rc" entry in <see cref="StandardPreReleaseNames"/>.
        /// </summary>
        public const int MaxPreReleaseNameIdx = 7;
        /// <summary>
        /// The maximum number of pre-releases.
        /// </summary>
        public const int MaxPreReleaseNumber = 99;
        /// <summary>
        /// The maximum number of fixes to a pre-release.
        /// </summary>
        public const int MaxPreReleaseFix = 99;
        static readonly string[] _standardNames = new[]{ "alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc" };

        const long MulNum = MaxPreReleaseFix + 1;
        const long MulName = MulNum * (MaxPreReleaseNumber + 1);
        const long MulPatch = MulName * (MaxPreReleaseNameIdx + 1) + 1;
        const long MulMinor = MulPatch * (MaxPatch + 1);
        const long MulMajor = MulMinor * (MaxMinor + 1);

        const long DivPatch = MulPatch + 1;
        const long DivMinor = DivPatch * (MaxPatch);
        const long DivMajor = DivMinor * (MaxMinor + 1);

        /// <summary>
        /// Gets the standard <see cref="PreReleaseName"/>.
        /// </summary>
        public static IReadOnlyList<string> StandardPreReleaseNames => _standardNames;

        /// <summary>
        /// Gets the very first possible version (0.0.0-alpha).
        /// </summary>
        public static readonly ReleaseTagVersion VeryFirstVersion = new ReleaseTagVersion( 1L, true );

        /// <summary>
        /// Gets the very first possible release versions (0.0.0, 0.1.0 or 1.0.0 or any prereleases of them).
        /// </summary>
        public static readonly IReadOnlyList<ReleaseTagVersion> FirstPossibleVersions = BuildFirstPossibleVersions();

        static IReadOnlyList<ReleaseTagVersion> BuildFirstPossibleVersions()
        {
            var versions = new ReleaseTagVersion[3 * 9];
            long v = 1L;
            int i = 0;
            while( i < 3 * 9 )
            {
                versions[i++] = new ReleaseTagVersion( v, true );
                if( (i % 18) == 0 ) v += MulMajor - MulMinor - MulPatch + 1;
                else if( (i % 9) == 0 ) v += MulMinor - MulPatch + 1;
                else v += MulName;
            }
            return versions;
        }

        /// <summary>
        /// Gets the very last possible version.
        /// </summary>
        public static readonly ReleaseTagVersion VeryLastVersion = new ReleaseTagVersion( string.Format( "{0}.{1}.{2}", MaxMajor, MaxMinor, MaxPatch ), MaxMajor, MaxMinor, MaxPatch, string.Empty, -1, 0, 0, ReleaseTagKind.OfficialRelease );

        /// <summary>
        /// Initializes a new tag from an ordered version that must be between 0 (invalid tag) and <see cref="VeryLastVersion"/>.<see cref="OrderedVersion"/>.
        /// </summary>
        /// <param name="v">The ordered version.</param>
        public ReleaseTagVersion( long v )
            : this( ValidateCtorArgument( v ), true )
        {
        }

        static long ValidateCtorArgument( long v )
        {
            if( v < 0 || v > VeryLastVersion.OrderedVersion ) throw new ArgumentException( "Must be between 0 and VeryLastVersion.OrderedVersion." ); 
            return v;
        }

        ReleaseTagVersion( long v, bool privateCall )
        {
            Debug.Assert( v >= 0 && (VeryLastVersion == null || v <= VeryLastVersion._orderedVersion.Number) );
            if( v == 0 )
            {
                Kind = ReleaseTagKind.None;
                ParseErrorMessage = _noTagParseErrorMessage;
                PreReleaseNameIdx = -1;
            }
            else
            {
                _orderedVersion.Number = v;

                long preReleasePart = v % MulPatch;
                if( preReleasePart != 0 )
                {
                    preReleasePart = preReleasePart - 1L;
                    PreReleaseNameIdx = (int)(preReleasePart / MulName);
                    PreReleaseNameFromTag = _standardNames[PreReleaseNameIdx];
                    preReleasePart -= (long)PreReleaseNameIdx * MulName;
                    PreReleaseNumber = (int)(preReleasePart / MulNum);
                    preReleasePart -= (long)PreReleaseNumber * MulNum;
                    PreReleasePatch = (int)preReleasePart;
                    Kind = ReleaseTagKind.PreRelease;
                }
                else
                {
                    v -= MulPatch;
                    PreReleaseNameIdx = -1;
                    PreReleaseNameFromTag = string.Empty;
                    Kind = ReleaseTagKind.OfficialRelease;
                }
                Major = (int)(v / MulMajor);
                v -= Major * MulMajor;
                Minor = (int)(v / MulMinor);
                v -= Minor * MulMinor;
                Patch = (int)(v / MulPatch);
            }
        }

        static long ComputeOrderedVersion( int major, int minor, int patch, int preReleaseNameIdx = -1, int preReleaseNumber = 0, int preReleaseFix = 0 )
        {
            long v = MulMajor * major;
            v += MulMinor * minor;
            v += MulPatch * (patch + 1);
            if( preReleaseNameIdx >= 0 )
            {
                v -= MulPatch - 1;
                v += MulName * preReleaseNameIdx;
                v += MulNum * preReleaseNumber;
                v += preReleaseFix;
            }
            Debug.Assert( new ReleaseTagVersion( v, true )._orderedVersion.Number == v );
            Debug.Assert( preReleaseNameIdx >= 0 == ((v % MulPatch) != 0) );
            Debug.Assert( major == (int)((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMajor) );
            Debug.Assert( minor == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMinor) - major * (MaxMinor + 1L)) );
            Debug.Assert( patch == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulPatch) - (major * (MaxMinor + 1L) + minor) * (MaxPatch + 1L)) );
            Debug.Assert( preReleaseNameIdx == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) / MulName) : -1) );
            Debug.Assert( preReleaseNumber == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) / MulNum - preReleaseNameIdx * MulNum) : 0) );
            Debug.Assert( preReleaseFix == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) % MulNum) : 0) );
            return v;
        }

        int ComputeDefinitionStrength()
        {
            Debug.Assert( IsValid && !IsMalformed );
            int d = 3;
            if( IsPreRelease && !IsPreReleaseNameStandard ) d -= 1;
            if( IsMarkedInvalid ) d += 2;
            return d;
        }

        /// <summary>
        /// Gets the ordered version number.
        /// </summary>
        public long OrderedVersion => _orderedVersion.Number;
        
        /// <summary>
        /// Gets the Major (first, most significant) part of the <see cref="OrderedVersion"/>: between 0 and 32767.
        /// </summary>
        public int OrderedVersionMajor => _orderedVersion.Major;

        /// <summary>
        /// Gets the Minor (second) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionMinor => _orderedVersion.Minor;

        /// <summary>
        /// Gets the Build (third) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionBuild => _orderedVersion.Build;

        /// <summary>
        /// Gets the Revision (last, less significant) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionRevision => _orderedVersion.Revision;

        /// <summary>
        /// Tags are equal it their <see cref="OrderedVersion"/> are equals.
        /// No other members are used for equality and comparison.
        /// </summary>
        /// <param name="other">Other release tag.</param>
        /// <returns>True if they have the same OrderedVersion.</returns>
        public bool Equals( ReleaseTagVersion other )
        {
            if( other == null ) return false;
            return _orderedVersion.Number == other._orderedVersion.Number;
        }

        /// <summary>
        /// Relies only on <see cref="OrderedVersion"/>.
        /// </summary>
        /// <param name="other">Other release tag (can be null).</param>
        /// <returns>A signed number indicating the relative values of this instance and <paramref name="other"/>.</returns>
        public int CompareTo( ReleaseTagVersion other )
        {
            if( other == null ) return 1;
            return _orderedVersion.Number.CompareTo( other._orderedVersion.Number );
        }

        /// <summary>
        /// Implements == operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if they are equal.</returns>
        static public bool operator ==( ReleaseTagVersion x, ReleaseTagVersion y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( !ReferenceEquals( x, null ) && !ReferenceEquals( y, null ) )
            {
                return x._orderedVersion.Number == y._orderedVersion.Number;
            }
            return false;
        }

        /// <summary>
        /// Implements != operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if they are not equal.</returns>
        static public bool operator !=( ReleaseTagVersion x, ReleaseTagVersion y ) => !(x == y);

        /// <summary>
        /// Implements &gt; operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if x is greater than y.</returns>
        static public bool operator >( ReleaseTagVersion x, ReleaseTagVersion y )
        {
            if( ReferenceEquals( x, y ) ) return false;
            if( !ReferenceEquals( x, null ) && !ReferenceEquals( y, null ) )
            {
                return x._orderedVersion.Number > y._orderedVersion.Number;
            }
            return x != null;
        }

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator <( ReleaseTagVersion x, ReleaseTagVersion y )
        {
            if( ReferenceEquals( x, y ) ) return false;
            if( !ReferenceEquals( x, null ) && !ReferenceEquals( y, null ) )
            {
                return x._orderedVersion.Number < y._orderedVersion.Number;
            }
            return y != null;
        }

        /// <summary>
        /// Implements &lt;= operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if x is lower than or equal to y.</returns>
        static public bool operator <=( ReleaseTagVersion x, ReleaseTagVersion y ) => !(x > y);

        /// <summary>
        /// Implements &gt;= operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if x is greater than or equal to y.</returns>
        static public bool operator >=( ReleaseTagVersion x, ReleaseTagVersion y ) => !(x < y);

        /// <summary>
        /// Tags are equal it their <see cref="OrderedVersion"/> are equals.
        /// No other members are used for equality and comparison.
        /// </summary>
        /// <param name="obj">Other release tag.</param>
        /// <returns>True if obj is a tag that has the same OrderedVersion as this.</returns>
        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            ReleaseTagVersion other = obj as ReleaseTagVersion;
            if( other == null ) throw new ArgumentException();
            return Equals( other );
        }

        /// <summary>
        /// Tags are equal it their <see cref="OrderedVersion"/> are equals.
        /// No other members are used for equality and comparison.
        /// </summary>
        /// <returns>True if they have the same OrderedVersion.</returns>
        public override int GetHashCode() => _orderedVersion.Number.GetHashCode();

    }
}

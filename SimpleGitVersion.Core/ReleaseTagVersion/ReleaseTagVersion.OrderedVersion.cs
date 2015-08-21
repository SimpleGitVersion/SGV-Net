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
            public UInt64 Number;
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
        public const int MaxMinor = 99999;
        /// <summary>
        /// The maximum number of patches for a minor one.
        /// </summary>
        public const int MaxPatch = 9999;
        /// <summary>
        /// The index of the "prerelease" entry in <see cref="StandardPreReleaseNames"/>.
        /// </summary>
        public const int MaxPreReleaseNameIdx = 12;
        /// <summary>
        /// The maximum number of pre-releases.
        /// </summary>
        public const int MaxPreReleaseNumber = 99;
        /// <summary>
        /// The maximum number of fixes to a pre-release.
        /// </summary>
        public const int MaxPreReleaseFix = 99;
        static readonly string[] _standardNames = new[]{ "alpha", "beta", "delta", "epsilon", "gamma", "iota", "kappa", "lambda", "mu", "omicron", "pi", "prerelease", "rc" };

        const UInt64 MulNum = MaxPreReleaseFix + 1;
        const UInt64 MulName = MulNum * (MaxPreReleaseNumber + 1);
        const UInt64 MulPatch = MulName * (MaxPreReleaseNameIdx + 1) + 1;
        const UInt64 MulMinor = MulPatch * (MaxPatch + 1);
        const UInt64 MulMajor = MulMinor * (MaxMinor + 1);

        const UInt64 DivPatch = MulPatch + 1;
        const UInt64 DivMinor = DivPatch * (MaxPatch);
        const UInt64 DivMajor = DivMinor * (MaxMinor + 1);

        /// <summary>
        /// Gets the standard <see cref="PreReleaseName"/>.
        /// </summary>
        public static IReadOnlyList<string> StandardPreReleaseNames { get { return _standardNames; } }

        /// <summary>
        /// Gets the very first possible version (0.0.0-alpha).
        /// </summary>
        public static readonly ReleaseTagVersion VeryFirstVersion = new ReleaseTagVersion( 1L );

        /// <summary>
        /// Gets the very first possible release versions (0.0.0, 0.1.0 or 1.0.0 or any prereleases of them).
        /// </summary>
        public static readonly IReadOnlyList<ReleaseTagVersion> FirstPossibleVersions = BuildFirstPossibleVersions();

        static IReadOnlyList<ReleaseTagVersion> BuildFirstPossibleVersions()
        {
            var versions = new ReleaseTagVersion[3 * 14];
            UInt64 v = 1L;
            int i = 0;
            while( i < 3 * 14 )
            {
                versions[i++] = new ReleaseTagVersion( v );
                if( (i % 28) == 0 ) v += MulMajor - MulMinor - MulPatch + 1;
                else if( (i % 14) == 0 ) v += MulMinor - MulPatch + 1;
                else v += MulName;
            }
            return versions;
        }

        /// <summary>
        /// Gets the very last possible version.
        /// </summary>
        public static readonly ReleaseTagVersion VeryLastVersion = new ReleaseTagVersion( string.Format( "{0}.{1}.{2}", MaxMajor, MaxMinor, MaxPatch ), MaxMajor, MaxMinor, MaxPatch, string.Empty, -1, 0, 0, ReleaseTagKind.Release );

        /// <summary>
        /// Initializes a new tag from an ordered version that must be between 0 (invalid tag) and <see cref="VeryLastVersion"/>.<see cref="OrderedVersion"/>.
        /// </summary>
        /// <param name="v">The ordered version.</param>
        public ReleaseTagVersion( Decimal v )
            : this( ValidateCtorArgument( v ) )
        {
        }

        static UInt64 ValidateCtorArgument( Decimal v )
        {
            if( v < 0 || v > VeryLastVersion.OrderedVersion ) throw new ArgumentException( "Must be between 0 and VeryLastVersion.OrderedVersion." ); 
            return (UInt64)v;
        }

        ReleaseTagVersion( UInt64 v )
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

                UInt64 preReleasePart = v % MulPatch;
                if( preReleasePart != 0 )
                {
                    preReleasePart = preReleasePart - 1L;
                    PreReleaseNameIdx = (int)(preReleasePart / MulName);
                    PreReleaseNameFromTag = _standardNames[PreReleaseNameIdx];
                    preReleasePart -= (UInt64)PreReleaseNameIdx * MulName;
                    PreReleaseNumber = (int)(preReleasePart / MulNum);
                    preReleasePart -= (UInt64)PreReleaseNumber * MulNum;
                    PreReleaseFix = (int)preReleasePart;
                    Kind = ReleaseTagKind.PreRelease;
                }
                else
                {
                    v -= MulPatch;
                    PreReleaseNameIdx = -1;
                    PreReleaseNameFromTag = string.Empty;
                    Kind = ReleaseTagKind.Release;
                }
                Major = (int)(v / MulMajor);
                v -= (UInt64)Major * MulMajor;
                Minor = (int)(v / MulMinor);
                v -= (UInt64)Minor * MulMinor;
                Patch = (int)(v / MulPatch);
            }
        }

        static UInt64 ComputeOrderedVersion( int major, int minor, int patch, int preReleaseNameIdx = -1, int preReleaseNumber = 0, int preReleaseFix = 0 )
        {
            UInt64 v = MulMajor * (UInt64)major;
            v += MulMinor * (UInt64)minor;
            v += MulPatch * (UInt64)(patch + 1);
            if( preReleaseNameIdx >= 0 )
            {
                v -= MulPatch - 1;
                v += MulName * (UInt64)preReleaseNameIdx;
                v += MulNum * (UInt64)preReleaseNumber;
                v += (UInt64)preReleaseFix;
            }
            Debug.Assert( new ReleaseTagVersion( v )._orderedVersion.Number == v );
            Debug.Assert( preReleaseNameIdx >= 0 == ((v % MulPatch) != 0) );
            Debug.Assert( major == (int)((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMajor) );
            Debug.Assert( minor == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulMinor) - (UInt64)major * (MaxMinor + 1L)) );
            Debug.Assert( patch == (int)(((preReleaseNameIdx >= 0 ? v : v - MulPatch) / MulPatch) - ((UInt64)major * (MaxMinor + 1L) + (UInt64)minor) * (MaxPatch + 1L)) );
            Debug.Assert( preReleaseNameIdx == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) / MulName) : -1) );
            Debug.Assert( preReleaseNumber == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) / MulNum - (UInt64)preReleaseNameIdx * MulNum) : 0) );
            Debug.Assert( preReleaseFix == (preReleaseNameIdx >= 0 ? (int)(((v - 1L) % MulPatch) % MulNum) : 0) );
            return v;
        }

        int ComputeDefinitionStrength()
        {
            Debug.Assert( IsValid && !IsMalformed );
            int d = 3;
            if( IsPreRelease && !IsPreReleaseNameStandard ) d -= 1;
            if( IsMarked ) d += 2;
            if( IsMarkedPublished ) d += 2;
            if( IsMarkedInvalid ) d += 4;
            return d;
        }

        /// <summary>
        /// Gets the projected ordered version.
        /// </summary>
        public Decimal OrderedVersion { get { return _orderedVersion.Number; } }
        
        /// <summary>
        /// Gets the Major (first, most significant) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionMajor { get { return _orderedVersion.Major; } }
        /// <summary>
        /// Gets the Minor (second) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionMinor { get { return _orderedVersion.Minor; } }
        /// <summary>
        /// Gets the Build (third) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionBuild { get { return _orderedVersion.Build; } }
        /// <summary>
        /// Gets the Revision (last, less significant) part of the <see cref="OrderedVersion"/>: between 0 and 65535.
        /// </summary>
        public int OrderedVersionRevision { get { return _orderedVersion.Revision; } }

        /// <summary>
        /// Tags are equal it their <see cref="OrderedVersion"/> are equals.
        /// No other memebers are used for equality and comparison.
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
        /// <param name="other">Other release tag.</param>
        /// <returns>A signed number indicating the relative values of this instance and <paramref name="other"/>.</returns>
        public int CompareTo( ReleaseTagVersion other )
        {
            if( other == null ) return 1;
            return _orderedVersion.Number.CompareTo( other._orderedVersion.Number );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            ReleaseTagVersion other = obj as ReleaseTagVersion;
            if( other == null ) throw new ArgumentException();
            return Equals( other );
        }

        /// <summary>
        /// Tags are equal it their <see cref="OrderedVersion"/> are equals.
        /// No other memebers are used for equality and comparison.
        /// </summary>
        /// <returns>True if they have the same OrderedVersion.</returns>
        public override int GetHashCode()
        {
            return _orderedVersion.Number.GetHashCode();
        }
    }
}

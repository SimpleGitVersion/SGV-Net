using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CSemVer
{
    /// <summary>
    /// Semantic version implementation.
    /// Strictly conforms to http://semver.org/ v2.0.0.
    /// </summary>
    public sealed class SVersion : IEquatable<SVersion>, IComparable<SVersion>
    {
        static Regex _regEx =
            new Regex( @"^(?<1>0|[1-9][0-9]*)\.(?<2>0|[1-9][0-9]*)\.(?<3>0|[1-9][0-9]*)(\-(?<4>[0-9A-Za-z\-\.]+))?(\+(?<5>[0-9A-Za-z\-\.]+))?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );
        static Regex _regexDottedPart =
            new Regex( @"^(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+)(\.(?<1>0|[1-9][0-9]*|[0-9A-Za-z\-]+))*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture );

        /// <summary>
        /// The invalid version is "0.0.0-0". It is syntaxically valid and 
        /// its precedence is greater than null and lower than any other syntaxically valid <see cref="SVersion"/>.
        /// </summary>
        static public readonly SVersion Invalid = new SVersion( 0, 0, 0, "0" );

        /// <summary>
        /// Initializes a new instance of the <see cref="SVersion" /> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prerelease">The prerelease version ("alpha", "rc.1.2", etc.).</param>
        /// <param name="buildMetaData">The build meta data.</param>
        /// <param name="checkBuildMetaDataSyntax">False to opt-out of strict <see cref="BuildMetaData"/> compliance.</param>
        public SVersion( int major, int minor, int patch, string prerelease = null, string buildMetaData = null, bool checkBuildMetaDataSyntax = true )
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            if( Major < 0 || Minor < 0 || Patch < 0 )
            {
                ParseErrorMessage += "Major, minor and patch must positive or 0. ";
            }
            Text = $"{Major}.{Minor}.{Patch}";
            if( !string.IsNullOrEmpty( prerelease ) )
            {
                var error = ValidateDottedIdentifiers( prerelease, "pre-release" );
                // Test is required: null string + null string is String.Empty, not null!
                if( error != null ) ParseErrorMessage += error;
                Prerelease = prerelease;
                Text += '-' + prerelease;
            }
            else Prerelease = string.Empty;
            if( !string.IsNullOrEmpty( buildMetaData ) )
            {
                if( checkBuildMetaDataSyntax )
                {
                    var error = ValidateDottedIdentifiers( buildMetaData, "build metadata" );
                    if( error != null ) ParseErrorMessage += error;
                }
                BuildMetaData = buildMetaData;
                Text += '+' + buildMetaData;
            }
            else BuildMetaData = string.Empty;
        }

        string ValidateDottedIdentifiers( string s, string partName )
        {
            Match m = _regexDottedPart.Match( s );
            if( !m.Success ) return "Invalid " + partName;
            else
            {
                CaptureCollection captures = m.Groups[1].Captures;
                Debug.Assert( captures.Count > 0 );
                foreach( Capture id in captures )
                {
                    Debug.Assert( id.Value.Length > 0 );
                    string p = id.Value;
                    if( p.Length > 1 && p[0] == '0' )
                    {
                        int i = 1;
                        while( i < p.Length )
                        {
                            if( !Char.IsDigit( p, i++ ) ) break;
                        }
                        if( i == p.Length )
                        {
                            return $"Numeric identifiers in {partName} must not start with a 0.";
                        }
                    }
                }
            }
            return null;
        }

        SVersion( string text, int major, int minor, int patch, string prerelease, string buildMetaData )
            : this( major, minor, patch, prerelease, buildMetaData )
        {
            Text = text;
        }

        SVersion( string parseError, string originalText )
        {
            ParseErrorMessage = parseError;
            Text = originalText;
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Gets the pre-release version.
        /// </summary>
        public string Prerelease { get; }

        /// <summary>
        /// Gets the build version.
        /// </summary>
        public string BuildMetaData { get; }

        /// <summary>
        /// An error message that describes the error if <see cref="IsValidSyntax"/> is false. Null otherwise.
        /// </summary>
        public string ParseErrorMessage { get; }

        /// <summary>
        /// Gets whether this <see cref="SVersion"/> has no <see cref="ParseErrorMessage"/>.
        /// </summary>
        public bool IsValidSyntax => ParseErrorMessage == null;

        /// <summary>
        /// The text is available even if <see cref="IsValidSyntax"/> is false.
        /// It is null if and only if the original parsed string was null.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Parses the specified string to a semantic version and returns a <see cref="SVersion"/> that 
        /// may not be <see cref="IsValidSyntax"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The SVersion object that may be invalid.</returns>
        public static SVersion TryParse( string s )
        {
            if( string.IsNullOrEmpty( s ) ) return new SVersion( "Null or empty version string.", s );
            Match m = _regEx.Match( s );
            if( !m.Success ) return new SVersion( "Pattern not matched.", s );
            string sMajor = m.Groups[1].Value;
            string sMinor = m.Groups[2].Value;
            string sPatch = m.Groups[3].Value;
            int major, minor, patch;
            if( !int.TryParse( sMajor, out major ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sMinor, out minor ) ) return new SVersion( "Invalid Major.", s );
            if( !int.TryParse( sPatch, out patch ) ) return new SVersion( "Invalid Patch.", s );
            return new SVersion( s, major, minor, patch, m.Groups[4].Value, m.Groups[5].Value );
        }

        /// <summary>
        /// Parses the specified string to a semantic version and throws an <see cref="ArgumentException"/> 
        /// it the resulting <see cref="SVersion"/> is not <see cref="IsValidSyntax"/>.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The SVersion object.</returns>
        public static SVersion Parse( string s )
        {
            SVersion v = TryParse( s );
            if( !v.IsValidSyntax ) throw new ArgumentException( nameof( s ) );
            return v;
        }

        /// <summary>
        /// Returns the <see cref="Text"/> of this instance or "[null Text]" if Text is null.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString() => Text ?? "[null Text]";

        /// <summary>
        /// Compares this with another <see cref="SVersion"/>.
        /// </summary>
        /// <param name="other">The other version to compare with this instance.</param>
        /// <returns>
        /// </returns>
        public int CompareTo( SVersion other )
        {
            if( ReferenceEquals( other, null ) ) return 1;
            if( IsValidSyntax )
            {
                if( !other.IsValidSyntax ) return 1;
            }
            else if( other.IsValidSyntax ) return -1;

            var r = Major - other.Major;
            if( r != 0 ) return r;

            r = Minor - other.Minor;
            if( r != 0 ) return r;

            r = Patch - other.Patch;
            if( r != 0 ) return r;

            return ComparePreRelease( Prerelease, other.Prerelease );
        }

        static int ComparePreRelease( string x, string y )
        {
            if( x.Length == 0 ) return y.Length == 0 ? 0 : 1;
            if( y.Length == 0 ) return -1;

            var xParts = x.Split( '.' );
            var yParts = y.Split( '.' );

            int commonParts = xParts.Length;
            int ultimateResult = -1;
            if( yParts.Length < xParts.Length )
            {
                commonParts = yParts.Length;
                ultimateResult = 1;
            }
            for( int i = 0; i < commonParts; i++ )
            {
                var xP = xParts[i];
                var yP = yParts[i];
                int xN, yN, r;
                if( int.TryParse( xP, out xN ) )
                {
                    if( int.TryParse( yP, out yN ) )
                    {
                        r = xN - yN;
                        if( r != 0 ) return r;
                    }
                    else return -1;
                }
                else
                {
                    if( int.TryParse( yP, out yN ) ) return 1;
                    r = String.CompareOrdinal( xP, yP );
                    if( r != 0 ) return r;
                }
            }
            return ultimateResult;
        }

        /// <summary>
        /// Equality ignore ths <see cref="BuildMetaData"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals( object obj )
        {
            if( ReferenceEquals( obj, null ) ) return false;
            if( ReferenceEquals( this, obj ) ) return true;
            return Equals( obj as SVersion );
        }

        /// <summary>
        /// Returns a hash code that ignores the <see cref="BuildMetaData"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Major * 31 + Minor) * 31 + Patch) * 31 + Prerelease.GetHashCode();
            }
        }

        public bool Equals( SVersion other )
        {
            if( other == null ) return false;
            if( ReferenceEquals( this, other ) ) return true;
            if( IsValidSyntax )
            {
                if( !other.IsValidSyntax ) return false;
            }
            else if( other.IsValidSyntax ) return false;
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Prerelease == other.Prerelease;
        }

        /// <summary>
        /// Implements == operator.
        /// </summary>
        /// <param name="x">First tag.</param>
        /// <param name="y">Second tag.</param>
        /// <returns>True if they are equal.</returns>
        static public bool operator ==( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( !ReferenceEquals( x, null ) )
            {
                return x.Equals( y );
            }
            return false;
        }

        /// <summary>
        /// Implements &gt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is greater than y.</returns>
        static public bool operator >( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return false;
            if( !ReferenceEquals( x, null ) )
            {
                if( ReferenceEquals( y, null ) ) return true;
                return x.CompareTo( y ) > 0;
            }
            return false;
        }

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator >=( SVersion x, SVersion y )
        {
            if( ReferenceEquals( x, y ) ) return true;
            if( !ReferenceEquals( x, null ) )
            {
                if( ReferenceEquals( y, null ) ) return true;
                return x.CompareTo( y ) >= 0;
            }
            return false;
        }

        /// <summary>
        /// Implements != operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if they are not equal.</returns>
        static public bool operator !=( SVersion x, SVersion y ) => !(x == y);

        /// <summary>
        /// Implements &lt;= operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than or equal to y.</returns>
        static public bool operator <=( SVersion x, SVersion y ) => !(x > y);

        /// <summary>
        /// Implements &lt; operator.
        /// </summary>
        /// <param name="x">First version.</param>
        /// <param name="y">Second version.</param>
        /// <returns>True if x is lower than y.</returns>
        static public bool operator <( SVersion x, SVersion y ) => !(x >= y);
    }
}


using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Project.json wrapper. 
    /// </summary>
    class ProjectFileContent
    {
        readonly string _originalText;
        readonly string _text;
        readonly Func<string, bool> _projectNameFinder;

        string _parseError;
        VersionOccurrence _thisVersion;
        IReadOnlyList<VersionOccurrence> _allVersions;
        bool _sameVersions;

        public ProjectFileContent( string text, Func<string, bool> projectNameFinder = null, bool normalizeLineEndings = true )
        {
            Debug.Assert( text != null );
            _text = _originalText = text;
            _projectNameFinder = projectNameFinder ?? (_ => false);
            if( normalizeLineEndings )
            {
                if( Environment.NewLine == "\r\n" )
                {
                    _text = ToCRLF( text );
                }
                else if( Environment.NewLine == "\n" )
                {
                    _text = ToLF( text );
                }
                else throw new NotSupportedException( "Unsupported Environment.NewLine." );
            }
        }

        static readonly Regex _rLFOnly = new Regex( @"(?<!\r)\n", RegexOptions.CultureInvariant );

        static string ToCRLF( string text )
        {
            return _rLFOnly.Replace( text, "\r\n" );
        }

        static string ToLF( string text )
        {
            return text.Replace( "\r\n", "\n" );
        }

        /// <summary>
        /// Gets the original text without any CRLF normalization applied.
        /// </summary>
        /// <value>The original text.</value>
        public string OriginalText
        {
            get { return _originalText; }
        }

        /// <summary>
        /// Gets the normalized text: line ends with <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <value>The normalized text.</value>
        public string Text => _text; 

        /// <summary>
        /// Gets the error message whenever the project.json has not been parsed correctly.
        /// </summary>
        public string ErrorMessage => _parseError;

        /// <summary>
        /// Gets the version: null if this is not a valid json, can be the empty string 
        /// if the version property does not exist or is empty ("version":""). 
        /// </summary>
        /// <value>The original version.</value>
        public string Version
        {
            get
            {
                if( _allVersions == null ) ExtractVersions( _text );
                return _thisVersion != null ? _thisVersion.Version : null;
            }
        }

        public string GetReplacedText( string newVersion )
        {
            if( Version == null || (_sameVersions && _thisVersion.Version == newVersion) ) return _text;
            StringBuilder b = new StringBuilder();
            int last = 0;
            foreach( var occ in _allVersions )
            {
                b.Append( _text, last, occ.Start - last );
                if( occ.IsNakedVersionNumber ) b.Append( '"' ).Append( newVersion ).Append( '"' );
                else
                {
                    b.Append( @"""version"": """ ).Append( newVersion ).Append( '"' );
                    if( occ.ExpectComma ) b.Append( ',' );
                }
                last = occ.End;
            }
            b.Append( _text, last, _text.Length - last );
            return b.ToString();
        }

        /// <summary>
        /// Checks whether the two files are equal regardless of the "version: "" property.
        /// Line endings must be normalized (or be the same) for this to work correctly.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if files are the same or differ only by their version, <c>false</c> otherwise.</returns>
        public bool EqualsWithoutVersion( ProjectFileContent other )
        {
            // This ensures that ExtractVersion has been called.
            if( (Version != null) != (other.Version != null) ) return false;
            if( _thisVersion == null ) return _text == other._text;
            if( _allVersions.Count != other._allVersions.Count ) return false;
            int last = 0, oLast = 0;
            for( int i = 0; i < _allVersions.Count; ++i )
            {
                VersionOccurrence o = _allVersions[i];
                VersionOccurrence oo = other._allVersions[i];
                int lenBefore = o.Start - last;
                if( lenBefore != (oo.Start - oLast)
                    || string.Compare( _text, last, other._text, oLast, lenBefore, StringComparison.Ordinal ) != 0 )
                {
                    return false;
                }
                last = o.End;
                oLast = oo.End;
            }
            int lenAfter = _text.Length - last;
            if( lenAfter != (other._text.Length - oLast)
                || string.Compare( _text, last, other._text, oLast, lenAfter, StringComparison.Ordinal ) != 0 )
            {
                return false;
            }
            return true;
        }

        void ExtractVersions( string text )
        {
            StringMatcher m = new StringMatcher( text );
            var allVersions = new List<VersionOccurrence>();
            JsonVersionFinder finder = new JsonVersionFinder( m, _projectNameFinder, allVersions );
            _thisVersion = finder.ThisVersion;
            _allVersions = allVersions;
            _sameVersions = finder.SameVersions;
            _parseError = m.ErrorMessage;
        }
    }
}

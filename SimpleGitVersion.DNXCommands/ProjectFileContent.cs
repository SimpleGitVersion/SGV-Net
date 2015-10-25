using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands
{
    /// <summary>
    /// Project.json wrapper. 
    /// </summary>
    class ProjectFileContent
    {
        string _text;
        Tuple<string, int, int, bool> _version;

        public ProjectFileContent( string text )
        {
            Debug.Assert( text != null );
            _text = text;
        }

        public string OriginalText
        {
            get { return _text; }
        }

        /// <summary>
        /// Gets the original version: null if this is not a valid json, can be the empty string 
        /// if the version property does not exist or is empty ("version":""). 
        /// </summary>
        /// <value>The original version.</value>
        public string OriginalVersion
        {
            get
            {
                if( _version == null ) _version = ExtractVersion( _text );
                return _version != null ? _version.Item1 : null;
            }
        }

        public string GetReplacedText( string newVersion )
        {
            if( OriginalVersion == null || _version.Item1 == newVersion ) return _text;
            return _text.Substring( 0, _version.Item2 )
                    + @"""version"": """ + newVersion + @""""
                    + (_version.Item4 ? "," : "")
                    + _text.Substring( _version.Item3 );
        }

        public bool EqualsWithoutVersion( ProjectFileContent other )
        {
            // This ensures that ExtractVersion has been called.
            if( (OriginalVersion != null) != (other.OriginalVersion != null) ) return false;
            if( _version.Item2 != other._version.Item2
                || (_text.Length - _version.Item3) != (other._text.Length - other._version.Item3)
                || string.Compare( _text, 0, other._text, 0, _version.Item2, StringComparison.Ordinal ) != 0
                || string.Compare( _text, _version.Item3, other._text, other._version.Item3, _text.Length - _version.Item3, StringComparison.Ordinal ) != 0 )
            {
                return false;
            }
            return true;
        }

        static readonly Regex _rVersion = new Regex( @"""version""\s*:\s*""(?<1>.*?)""(?<2>\s*,)?", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

        static Tuple<string, int, int, bool> ExtractVersion( string text )
        {
            // Temporary awful and buggy code (hoping there is no unbalanced { or } inside strings...
            // I want to replace the version in place here, not to load/rewrite the whole file
            // that will loose ordering, layout, etc.
            // TODO: This MUST be done with a lexer or the Matcher pattern...
            int beg = text.IndexOf( '{' );
            int end = text.LastIndexOf( '}' );
            if( beg < 0 || end < beg ) return null;

            foreach( Match m in _rVersion.Matches( text ) )
            {
                if( m.Index < beg ) continue;
                if( m.Index > end ) break;
                int depth = 0;
                int idx = m.Index;
                while( idx > beg )
                {
                    char c = text[idx--];
                    if( c == '{' ) ++depth;
                    else if( c == '}' ) --depth;
                }
                if( depth == 0 ) return Tuple.Create( m.Groups[1].Value, m.Index, m.Index + m.Length, m.Groups[2].Length > 0 );
            }
            bool expectComma = false;
            int iNext = beg + 1;
            while( iNext < end )
            {
                if( text[iNext] == ',' ) break;
                if( !char.IsWhiteSpace( text, iNext ) )
                {
                    expectComma = true;
                    break;
                }
                ++iNext;
            }
            return Tuple.Create( string.Empty, beg + 1, beg + 1, expectComma );
        }
    }

}

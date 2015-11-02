using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands
{
    class VersionOccurrence
    {
        public string Version;
        public int Start;
        public int Length;
        public bool ExpectComma;
        public int End { get { return Start + Length; } }
        public bool NakedVersionNumber { get { return Length == Version.Length + 2; } }
    }

    class JSONVersionFinder : JSONVisitor
    {
        int _propLevel;
        string _parentPropertyName;
        int _objectStart;
        bool _hasTopLevelProperties;
        bool _diffVersions;
        VersionOccurrence _thisVersion;
        readonly List<VersionOccurrence> _versions;
        readonly Func<string, bool> _projectNameFinder;

        public JSONVersionFinder( StringMatcher m, Func<string,bool> projectNameFinder, List<VersionOccurrence> allVersions )
            : base( m )
        {
            _objectStart = -1;
            _versions = allVersions;
            _projectNameFinder = projectNameFinder;
            if( Visit() )
            {
                if( _thisVersion == null && _objectStart >= 0 )
                {
                    _thisVersion = new VersionOccurrence()
                    {
                        Version = String.Empty,
                        Start = _objectStart,
                        Length = 0,
                        ExpectComma = _hasTopLevelProperties
                    };
                    if( _versions != null ) _versions.Insert( 0, _thisVersion );
                }
            }
            if( _thisVersion == null && _versions != null ) _versions.Clear();
        }

        /// <summary>
        /// Gets this version: null JSON was not valid.
        /// </summary>
        public VersionOccurrence ThisVersion
        {
            get { return _thisVersion; }
        }

        public bool SameVersions
        {
            get { return !_diffVersions; }
        }

        public override bool VisitObjectContent()
        {
            if( _objectStart == -1 ) _objectStart = Matcher.StartIndex;
            return base.VisitObjectContent();
        }

        public override bool VisitObjectProperty( int startPropertyIndex, string propName )
        {
            if( _propLevel == 0 )
            {
                _hasTopLevelProperties = true;
                if( propName == "version" )
                {
                    if( _thisVersion != null ) return Matcher.SetError( "Duplicate version." );
                    return GetVersionValue( startPropertyIndex, out _thisVersion );
                }
            }
            if( _versions != null && _parentPropertyName == "dependencies" && _projectNameFinder( propName ) )
            {
                Matcher.MatchWhiteSpaces( 0 );
                if( Matcher.Head == '{' )
                {
                    JSONVersionFinder f = new JSONVersionFinder( Matcher, null, null );
                    if( f.ThisVersion == null ) return Matcher.SetError( "Property version expected." );
                    CollectVersion( f.ThisVersion );
                }
                else
                {
                    int start = Matcher.StartIndex;
                    string version;
                    if( !Matcher.TryMatchJSONQuotedString( out version ) ) return Matcher.SetError( "Version string expected." );
                    VersionOccurrence v = new VersionOccurrence()
                    {
                        Start = start,
                        Version = version,
                        Length = Matcher.StartIndex - start
                    };
                    Debug.Assert( v.Length == version.Length + 2 );
                    CollectVersion( v );
                }
                return true;
            }
            string prevParentName = _parentPropertyName;
            try
            {
                ++_propLevel;
                _parentPropertyName = propName;
                return base.VisitObjectProperty( startPropertyIndex, propName );
            }
            finally
            {
                --_propLevel;
                _parentPropertyName = prevParentName;
            }
        }

        void CollectVersion( VersionOccurrence v )
        {
            Debug.Assert( v != null );
            if( _versions.Count > 0 )
            {
                _diffVersions |= _versions[0].Version != v.Version;
            }
            _versions.Add( v );
        }

        bool GetVersionValue( int startPropertyIndex, out VersionOccurrence v )
        {
            v = null;
            Matcher.MatchWhiteSpaces( 0 );
            string version;
            if( !Matcher.TryMatchJSONQuotedString( out version ) ) return Matcher.SetError( "Version string expected." );
            int end = Matcher.StartIndex;
            Matcher.MatchWhiteSpaces( 0 );
            bool comma = Matcher.Head == ',';
            if( comma ) end = Matcher.StartIndex + 1;
            v = new VersionOccurrence()
            {
                Version = version,
                Start = startPropertyIndex,
                Length = end - startPropertyIndex,
                ExpectComma = comma
            };
            if( _versions != null ) CollectVersion( v );
            return true;
        }

    }
}

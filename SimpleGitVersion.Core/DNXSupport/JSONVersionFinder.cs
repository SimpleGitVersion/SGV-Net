using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    class JSONVersionFinder : JSONVisitor
    {
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
                    _thisVersion = new VersionOccurrence( String.Empty, _objectStart, 0, _hasTopLevelProperties );
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

        public override bool VisitObjectProperty( int startPropertyIndex, string propertyName, int propertyIndex )
        {
            if( Path.Count == 0 )
            {
                _hasTopLevelProperties = true;
                if( propertyName == "version" )
                {
                    if( _thisVersion != null ) return Matcher.SetError( "Duplicate version." );
                    return GetVersionValue( startPropertyIndex, out _thisVersion );
                }
            }
            else if( _versions != null && Path[Path.Count-1].PropertyName == "dependencies" && _projectNameFinder( propertyName ) )
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
                    VersionOccurrence v = new VersionOccurrence( version, start, Matcher.StartIndex - start, false );
                    Debug.Assert( v.IsNakedVersionNumber );
                    CollectVersion( v );
                }
                return true;
            }
            return base.VisitObjectProperty( startPropertyIndex, propertyName, propertyIndex );
        }

        void CollectVersion( VersionOccurrence v )
        {
            Debug.Assert( _versions != null && v != null );
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
            v = new VersionOccurrence( version, startPropertyIndex, end - startPropertyIndex, comma );
            if( _versions != null ) CollectVersion( v );
            return true;
        }

    }
}

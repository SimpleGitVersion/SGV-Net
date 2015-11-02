using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    class JSONVisitor
    {
        readonly StringMatcher _m;
        public JSONVisitor( StringMatcher m )
        {
            _m = m;
        }

        public StringMatcher Matcher { get { return _m; } }

        public virtual bool VisitObjectContent()
        {
            while( !_m.IsEnd )
            {
                _m.MatchWhiteSpaces( 0 );
                if( _m.TryMatchChar( '}' ) ) return true;
                int startPropertyIndex = _m.StartIndex;
                string propName;
                if( !_m.TryMatchJSONQuotedString( out propName ) ) return false;
                _m.MatchWhiteSpaces( 0 );
                if( !_m.MatchChar( ':' ) || !VisitObjectProperty( startPropertyIndex, propName ) ) return false;
                _m.MatchWhiteSpaces( 0 );
                _m.TryMatchChar( ',' );
            }
            return false;
        }

        public virtual bool VisitObjectProperty( int startPropertyIndex, string propName )
        {
            return Visit();
        }

        public virtual bool Visit()
        {
            _m.MatchWhiteSpaces( 0 );
            if( _m.TryMatchChar( '{' ) ) return VisitObjectContent();
            if( _m.TryMatchChar( '[' ) ) return VisitArrayContent();
            return VisitTerminal();
        }

        public virtual bool VisitTerminal()
        {
            _m.MatchWhiteSpaces( 0 );
            return _m.TryMatchJSONQuotedString( true ) 
                    || _m.TryMatchDoubleValue() 
                    || _m.TryMatchString( "true" )
                    || _m.TryMatchString( "false" ) 
                    ? true 
                    : _m.SetError(); 
        }

        public virtual bool VisitArrayContent()
        {
            while( !_m.IsEnd )
            {
                _m.MatchWhiteSpaces( 0 );
                if( _m.TryMatchChar( ']' ) ) return true;
                if( !Visit() ) return false;
                _m.TryMatchChar( ',' );
            }
            return false;
        }
    }
}

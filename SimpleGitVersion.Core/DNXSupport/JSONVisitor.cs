using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    internal class JSONVisitor
    {
        readonly StringMatcher _m;
        readonly List<Parent> _path;

        /// <summary>
        /// Describes a parent object: it is the name of a property and its index or the index in a array.
        /// </summary>
        public struct Parent
        {
            /// <summary>
            /// The name of the property or null if this is an array entry.
            /// </summary>
            public readonly string PropertyName;

            /// <summary>
            /// The index in the array or the property index (the count of properties 
            /// that appear before this one in the object definition).
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// Gets whether this is an array cell (ie. <see cref="PropertyName"/> is null). 
            /// </summary>
            public bool IsArrayCell { get { return PropertyName == null; } }

            /// <summary>
            /// Initializes a new parent object.
            /// </summary>
            /// <param name="propertyName">Name of the property. Null for an array entry.</param>
            /// <param name="arrayIndex">Index of the property or index in an array.</param>
            public Parent( string propertyName, int index )
            {
                PropertyName = propertyName;
                Index = index;
            }

            /// <summary>
            /// Overridden to return either <see cref="PropertyName"/> or [<see cref="Index"/>].
            /// </summary>
            /// <returns>Representation of the accessor.</returns>
            public override string ToString()
            {
                return IsArrayCell 
                        ? '[' + Index.ToString( CultureInfo.InvariantCulture ) + ']'
                        : PropertyName;
            }
        }

        public JSONVisitor( StringMatcher m )
        {
            _m = m;
            _path = new List<Parent>();
        }

        public StringMatcher Matcher { get { return _m; } }

        /// <summary>
        /// Gets the current path of the visited item.
        /// </summary>
        protected IReadOnlyList<Parent> Path { get { return _path; } }

        public virtual bool VisitObjectContent()
        {
            int propertyIndex = 0;
            while( !_m.IsEnd )
            {
                _m.MatchWhiteSpaces( 0 );
                if( _m.TryMatchChar( '}' ) ) return true;
                int startPropertyIndex = _m.StartIndex;
                string propName;
                if( !_m.TryMatchJSONQuotedString( out propName ) ) return false;
                _m.MatchWhiteSpaces( 0 );
                if( !_m.MatchChar( ':' ) || !VisitObjectProperty( startPropertyIndex, propName, propertyIndex ) ) return false;
                _m.MatchWhiteSpaces( 0 );
                _m.TryMatchChar( ',' );
                ++propertyIndex;
            }
            return false;
        }

        public virtual bool VisitObjectProperty( int startPropertyIndex, string propertyName, int propertyIndex )
        {
            try
            {
                _path.Add( new Parent( propertyName, propertyIndex ) );
                return Visit();
            }
            finally
            {
                _path.RemoveAt( _path.Count - 1 );
            }
        }

        public virtual bool VisitArrayContent()
        {
            int cellIndex = 0;
            while( !_m.IsEnd )
            {
                _m.MatchWhiteSpaces( 0 );
                if( _m.TryMatchChar( ']' ) ) return true;
                if( !VisitArrayCell( cellIndex ) ) return false;
                _m.TryMatchChar( ',' );
                ++cellIndex;
            }
            return false;
        }

        public virtual bool VisitArrayCell( int cellIndex )
        {
            try
            {
                _path.Add( new Parent( null, cellIndex ) );
                return Visit();
            }
            finally
            {
                _path.RemoveAt( _path.Count - 1 );
            }
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
    }
}

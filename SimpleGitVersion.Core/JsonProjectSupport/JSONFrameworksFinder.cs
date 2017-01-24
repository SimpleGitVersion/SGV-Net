using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    class JsonFrameworksFinder : JsonVisitor
    {
        readonly List<string> _frameworks;

        JsonFrameworksFinder( StringMatcher m, List<string> frameworks )
            : base( m )
        {
            _frameworks = frameworks;
            Visit();
        }

        public static List<string> GetFrameworks( string project )
        {
            var result = new List<string>();
            new JsonFrameworksFinder( new StringMatcher( project ), result );
            return result;
        }

        public override bool VisitObjectProperty( int startPropertyIndex, string propertyName, int propertyIndex )
        {
            if( Path.Count == 1 && Path[0].PropertyName == "frameworks" ) _frameworks.Add( propertyName );
            return base.VisitObjectProperty( startPropertyIndex, propertyName, propertyIndex );
        }
    }
}

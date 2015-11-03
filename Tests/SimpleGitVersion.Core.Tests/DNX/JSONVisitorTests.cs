using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNX.Tests
{
    [TestFixture]
    public class JSONVisitorTests
    {
        class JSONProperties : JSONVisitor
        {
            public List<string> Properties;

            public JSONProperties( StringMatcher m)
                : base( m )
            {
                Properties = new List<string>();
            }

            public override bool VisitObjectProperty( int startPropertyIndex, string propName )
            {
                Properties.Add( propName );
                return base.VisitObjectProperty( startPropertyIndex, propName );
            }
        }

        [Test]
        public void json_visit_all_properties()
        {
            string s = @"
{ 
    ""p1"": ""n"", 
    ""p2"": 
    { 
        ""p3"": 
        [ 
            {
                ""p4"": 
                { 
                    ""p5"" : 0.989, 
                    ""p6"": [],
                    ""p7"": {}
                }
            }
        ] 
    } 
}";
            JSONProperties p = new JSONProperties( new StringMatcher( s ) );
            p.Visit();
            CollectionAssert.AreEqual( new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7" }, p.Properties );
        }

    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands.Tests
{
    [TestFixture]
    public class ProjectFileTests
    {
        [TestCase( "", null )]
        [TestCase( "abc", null )]
        [TestCase( "ab{c", null )]
        [TestCase( "ab}c", null )]
        [TestCase( "a{b}c", "" )]
        [TestCase( "012{}c", "" )]
        [TestCase( @"012345{{""version"":""v""b}c}", "" )]
        [TestCase( @"01{""version""     :    ""v""  }c", "v" )]
        [TestCase( @"a{{""version"":""v""b}c""version"":""v""}", "v" )]
        public void version_extraction_tests( string text, string version )
        {
            ProjectFileContent f = new ProjectFileContent( text );
            Assert.That( f.OriginalVersion, Is.EqualTo( version ) );
        }

        [TestCase( "", null )]
        [TestCase( "abc", null )]
        [TestCase( "ab{c", null )]
        [TestCase( "ab}c", null )]
        [TestCase( "a{b}c", @"a{""version"": ""XXX"",b}c" )]
        [TestCase( "012{}c", @"012{""version"": ""XXX""}c" )]
        [TestCase( @"012345{{""version"":""v""b}c}", @"012345{""version"": ""XXX"",{""version"":""v""b}c}" )]
        [TestCase( @"01{""version""     :    ""v""  }c", @"01{""version"": ""XXX""  }c" )]
        [TestCase( @"a{{""version"":""v""b}c""version"":""v""}", @"a{{""version"":""v""b}c""version"": ""XXX""}" )]
        public void replacing_or_injecting_versions( string text, string replaced )
        {
            ProjectFileContent f = new ProjectFileContent( text );
            string r = f.GetReplacedText( "XXX" );
            if( replaced == null ) Assert.That( r, Is.SameAs( text ), "No replacement nor injection since json is not valid." );
            else Assert.That( r, Is.EqualTo( replaced ) );
        }

        [TestCase( "a{b}c", @"a{""version"": ""XXX"",b}c" )]
        [TestCase( "012{}", @"012{""version"": ""XXX""}" )]
        [TestCase( @"{{""version"":""v""b}c}", @"{""version"": ""XXX"",{""version"":""v""b}c}" )]
        [TestCase( @"{""version""     :    ""YYYY""  }c", @"{""version"": ""XXX""  }c" )]
        [TestCase( @"a{{""version"":""v""b}c""version"":  ""ZZZZZZZZ""  ,A}", @"a{{""version"":""v""b}c""version"": ""XXX"",A}" )]
        public void project_equality_without_version( string text1, string text2 )
        {
            ProjectFileContent f1 = new ProjectFileContent( text1 );
            ProjectFileContent f2 = new ProjectFileContent( text2 );
            Assert.That( f1.EqualsWithoutVersion( f2 ) );
        }
    }
}

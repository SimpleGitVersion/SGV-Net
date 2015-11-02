using CK.Core;
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
        [TestCase( "a{b}c", null )]
        [TestCase( "012{}c", null )]
        [TestCase( @"{ ""X"": {""version"":""v""} }", "" )]
        [TestCase( @"{""version""     :    ""v""  }", "v" )]
        [TestCase( @"{""X"":{""version"":""not""},""version"":""v2""}", "v2" )]
        public void version_extraction_tests( string text, string version )
        {
            ProjectFileContent f = new ProjectFileContent( text );
            Assert.That( f.Version, Is.EqualTo( version ) );
        }

        [TestCase( @"{""version""     :    ""v""      }", @"{""version"": ""XXX""      }" )]
        [TestCase( @"{""X"" : {""version"":""not""}, ""version"":""v""}", @"{""X"" : {""version"":""not""}, ""version"": ""XXX""}" )]
        [TestCase( @"{""X"" : {""version"":""not""}, ""version"":""v"", ""Other"":[]}", @"{""X"" : {""version"":""not""}, ""version"": ""XXX"", ""Other"":[]}" )]
        [TestCase( @"{""P1"" : ""v"",""Other"":{}}", @"{""version"": ""XXX"",""P1"" : ""v"",""Other"":{}}" )]
        [TestCase( "{}", @"{""version"": ""XXX""}" )]
        public void replacing_or_injecting_versions( string text, string replaced )
        {
            ProjectFileContent f = new ProjectFileContent( text );
            string r = f.GetReplacedText( "XXX" );
            Assert.That( r, Is.EqualTo( replaced ) );
        }

        [TestCase( "{}", @"{""version"": ""XXX""}" )]
        [TestCase( @"{""version""      :    ""v""      , ""X"":[], ""Y"": {} }", @"{""version"": ""XXX"", ""X"":[], ""Y"": {} }" )]
        public void project_equality_without_version( string text1, string text2 )
        {
            ProjectFileContent f1 = new ProjectFileContent( text1 );
            ProjectFileContent f2 = new ProjectFileContent( text2 );
            Assert.That( f1.EqualsWithoutVersion( f2 ) );
        }

        [TestCase( "\n{\n\"b\":\n\n\"X\"\n}\n", "\r\n{\"version\": \"XXX\",\r\n\"b\":\r\n\r\n\"X\"\r\n}\r\n" )]
        [TestCase( "{\n\"version\":\n\"KKKKKKKKKK\",\n}", "{\r\n\"version\":\r\n\"XXXX\",\r\n}" )]
        [TestCase( "{\n\"version\":\"KKKKKKKKKK\"}\n", "{\r\n\"version\":\"XXXX\"}\r\n" )]
        public void project_equality_CRLF_normalization( string text1, string text2 )
        {
            ProjectFileContent f1 = new ProjectFileContent( text1 );
            ProjectFileContent f2 = new ProjectFileContent( text2 );
            Assert.That( f1.EqualsWithoutVersion( f2 ) );
        }

        [TestCase( @"{
  ""version"": ""VVVVVVVV"",
  ""description"": ""CK.Core Class Library"",
  ""authors"": [ ""Olivier Spinelli"" ],
  ""owners"": [ ""Invenietis"" ],
  ""tags"": [ """" ],
  ""projectUrl"": """",
  ""compilationOptions"": {   
    ""keyFile"": ""../SharedKey.snk""
  },
  ""licenseUrl"": """",
    ""scripts"": {
    ""prepack"": ""sgv prepack"",
    ""prebuild"": ""sgv prebuild"",
    ""postpack"": ""sgv postpack""
  },

  ""frameworks"": {
    ""dnx451"": { },
    ""dnxcore50"": {
      ""dependencies"": {
        ""Microsoft.CSharp"": ""4.0.1-beta-23409"",
        ""System.Collections"": ""4.0.11-beta-23409"",
        ""System.Linq"": ""4.0.1-beta-23409"",
        ""System.Runtime"": ""4.0.21-beta-23409"",
        ""System.Threading"": ""4.0.11-beta-23409"",
        ""System.Console"": ""4.0.0-beta-23409""
      }
    }
  },
  ""dependencies"": {
    ""CK.Core"": ""VVVVVVVV""
  }
}" )]
        [TestCase( @"
{
    ""version"": ""VVVVVVVV"",
    ""In.The.Same.Solution"": ""MUST not be changed"",
    ""dependencies"": {
        ""System.Collections"": ""4.0.0.0"",
        ""In.The.Same.Solution"": ""VVVVVVVV"",
        ""System.ComponentModel"": ""4.0.0.0""
    },
    ""frameworks"": 
    {
        ""In.The.Same.Solution"": ""MUST not be changed"",
        ""dnxcore50"": 
        {
            ""dependencies"": 
            {
                ""inside dependecies"": 
                {
                    ""In.The.Same.Solution"": ""MUST not be changed"",
                    ""Another.In.The.Same.Solution"": { ""version"": ""MUST not be changed"" }
                },
                ""System.Collections"": ""4.0.0.0"",
                ""In.The.Same.Solution"": ""VVVVVVVV"",
                ""System.ComponentModel"": ""4.0.0.0"",
                ""Another.In.The.Same.Solution"": { ""version"": ""VVVVVVVV"", ""type"": ""Default"" },
                ""System.Reflection"": ""4.0.10.0"",
                ""System.Threading.Tasks"": ""4.0.0.0""
            },
            ""In.The.Same.Solution"": ""MUST not be changed""
        }
    }
" )]
        public void replace_version_dependencies( string text )
        {
            ProjectFileContent f = new ProjectFileContent( 
                text, 
                name => name == "In.The.Same.Solution" 
                        || name == "Another.In.The.Same.Solution"
                        || name == "CK.Core",
                // AppVeyor build machines have Environment.NewLine == "\n"!!  
                normalizeLineEndings: false );
            string r = f.GetReplacedText( "!TheVersion!" );
            Assert.That( r, Is.EqualTo( text.Replace( "VVVVVVVV", "!TheVersion!" ) ) );
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using SimpleGitVersion;

namespace SimpleGitVersionTask.Tests
{
    [TestFixture]
    public class RepositoryInfoXmlFileTests
    {
        [Test]
        public void reading_repository_info_xml_file()
        {
            string s =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RepositoryInfo xmlns=""http://csemver.org/schemas/2015"">
	<StartingVersionForCSemVer>v4.2.0</StartingVersionForCSemVer>
    <IgnoreModifiedFiles>
        <Add>SharedKey.snk</Add>
    </IgnoreModifiedFiles>
</RepositoryInfo>";
            XmlSchema schema = XmlSchema.Read( File.OpenRead( TestHelper.RepositoryXSDPath ), ( o, e ) => { throw new Exception( "Invalid xsd." ); } );
            XmlSchemaSet set = new XmlSchemaSet();
            set.Add( schema );
            XDocument d = XDocument.Parse( s );
            d.Validate( set, ( o, e ) => { throw new Exception( "Invalid xsd." ); } );
            RepositoryInfoOptions opt = RepositoryInfoOptions.Read( d.Root );

            Assert.That( opt.Branches, Is.Empty );
            Assert.That( opt.StartingVersionForCSemVer, Is.EqualTo( "v4.2.0" ) );
            Assert.That( opt.StartingCommitSha, Is.Null );
            CollectionAssert.AreEquivalent( opt.IgnoreModifiedFiles, new[] { "SharedKey.snk" } );
        }

        //[Test]
        //public void Test()
        //{
        //    string SolutionDirectory = @"E:\Dev\CK-Database";
        //    string optionFile = Path.Combine( SolutionDirectory, "RepositoryInfo.xml" );
        //    RepositoryInfoOptions options = File.Exists( optionFile ) ? RepositoryInfoOptions.Read( optionFile ) : new RepositoryInfoOptions();
        //    RepositoryInfo info = RepositoryInfo.LoadFromPath( SolutionDirectory, options );
        //    Assert.That( info.ReleaseTagErrorText, Is.Null );
        //}
    }
}

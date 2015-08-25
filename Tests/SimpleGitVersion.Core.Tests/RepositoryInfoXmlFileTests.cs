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

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class RepositoryInfoXmlFileTests
    {
        [Test]
        public void reading_repository_info_xml_file_StartingVersionForCSemVer_and_IgnoreModifiedFiles()
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

        [Test]
        public void reading_repository_info_xml_file_Branches()
        {
            string s =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RepositoryInfo xmlns=""http://csemver.org/schemas/2015"">
    <Branches>
        <Branch  xmlns=""http://csemver.org/schemas/2015"" Name=""develop"" CIVersionMode=""LastReleaseBased"" />
        <Branch  xmlns=""http://csemver.org/schemas/2015"" Name=""exploratory"" CIVersionMode=""ZeroTimed"" VersionName=""Preview"" />
    </Branches>
</RepositoryInfo>";
            XDocument d = XDocument.Parse( s );

            XmlSchema schema = XmlSchema.Read( File.OpenRead( TestHelper.RepositoryXSDPath ), ( o, e ) => { throw new Exception( "Invalid xsd." ); } );
            XmlSchemaSet set = new XmlSchemaSet();
            set.Add( schema );
            d.Validate( set, ( o, e ) => { throw new Exception( "Invalid xsd." ); } );

            RepositoryInfoOptions opt = RepositoryInfoOptions.Read( d.Root );

            Assert.That( opt.StartingVersionForCSemVer, Is.Null );
            Assert.That( opt.IgnoreModifiedFiles, Is.Empty );
            Assert.That( opt.Branches.Count, Is.EqualTo( 2 ) );

            Assert.That( opt.Branches[0].Name, Is.EqualTo( "develop" ) );
            Assert.That( opt.Branches[0].CIVersionMode, Is.EqualTo( CIBranchVersionMode.LastReleaseBased ) );
            Assert.That( opt.Branches[0].VersionName, Is.Null );

            Assert.That( opt.Branches[1].Name, Is.EqualTo( "exploratory" ) );
            Assert.That( opt.Branches[1].CIVersionMode, Is.EqualTo( CIBranchVersionMode.ZeroTimed ) );
            Assert.That( opt.Branches[1].VersionName, Is.EqualTo( "Preview" ) );
        }

    }
}

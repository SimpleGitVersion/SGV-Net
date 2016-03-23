using NUnit.Framework;
using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;

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
            XDocument d = XDocument.Parse( s );
            ValidateAgainstSchema( d );

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
        <Branch Name=""develop"" CIVersionMode=""LastReleaseBased"" />
        <Branch Name=""exploratory"" CIVersionMode=""ZeroTimed"" VersionName=""Preview"" />
    </Branches>
</RepositoryInfo>";
            XDocument d = XDocument.Parse( s );
            ValidateAgainstSchema( d );

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

        [Test]
        public void full_repository_info_to_xml_is_valid_according_to_schema()
        {
            string s =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RepositoryInfo xmlns=""http://csemver.org/schemas/2015"">
    <Debug IgnoreDirtyWorkingFolder=""true"" />
    <Branches>
        <Branch Name=""develop"" CIVersionMode=""LastReleaseBased"" />
        <Branch Name=""exploratory"" CIVersionMode=""ZeroTimed"" VersionName=""Preview"" />
        <Branch Name=""other"" CIVersionMode=""None"" />
    </Branches>
	<StartingVersionForCSemVer>v4.2.0</StartingVersionForCSemVer>
    <PossibleVersionsMode>AllSuccessors</PossibleVersionsMode>
    <IgnoreModifiedFiles>
        <Add>SharedKey.snk</Add>
    </IgnoreModifiedFiles>
	<RemoteName>not-the-origin</RemoteName>
</RepositoryInfo>";
            XDocument d = XDocument.Parse( s );
            ValidateAgainstSchema( d );

            RepositoryInfoOptions opt = RepositoryInfoOptions.Read( d.Root );

            XDocument d2 = new XDocument( opt.ToXml() );
            ValidateAgainstSchema( d2 );
            RepositoryInfoOptions opt2 = RepositoryInfoOptions.Read( d2.Root );

            Assert.That( opt.IgnoreDirtyWorkingFolder, Is.EqualTo( opt2.IgnoreDirtyWorkingFolder ) );
            Assert.That( opt.RemoteName, Is.EqualTo( opt2.RemoteName ) );
            Assert.That( opt.StartingVersionForCSemVer, Is.EqualTo( opt2.StartingVersionForCSemVer ) );
            Assert.That( opt.Branches.Count, Is.EqualTo( opt2.Branches.Count ) );
            Assert.That( opt.IgnoreModifiedFiles.Count, Is.EqualTo( opt2.IgnoreModifiedFiles.Count ) );
            Assert.That( opt.PossibleVersionsMode, Is.EqualTo( PossibleVersionsMode.AllSuccessors ) );
        }

        private static void ValidateAgainstSchema( XDocument d )
        {
            XmlSchema schema = XmlSchema.Read( File.OpenRead( TestHelper.RepositoryXSDPath ), ( o, e ) => { throw new Exception( "Invalid xsd." ); } );
            XmlSchemaSet set = new XmlSchemaSet();
            set.Add( schema );
            d.Validate( set, ( o, e ) => 
            {
                throw new Exception( "Invalid document:" + e.Message );
            } );
        }

    }
    }

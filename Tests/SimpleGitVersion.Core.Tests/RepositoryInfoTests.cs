using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class RepositoryInfoTests
    {
        [Test]
        public void on_a_non_tagged_repository_all_commits_can_be_a_first_possible_version()
        {
            var repoTest = TestHelper.TestGitRepository;
            foreach( SimpleCommit c in repoTest.Commits )
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( c.Sha );
                Assert.That( i.RepositoryError, Is.Null );
                Assert.That( i.ReleaseTagErrorLines, Is.Null );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.PreviousRelease, Is.Null );
                Assert.That( i.PreviousMaxRelease, Is.Null );
                CollectionAssert.AreEqual( ReleaseTagVersion.FirstPossibleVersions, i.PossibleVersions );
            }
        }

        [Test]
        public void repository_with_the_very_first_version_only()
        {
            var repoTest = TestHelper.TestGitRepository;
            var tagged = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second b/b1" ) );

            var bb1Tag = ReleaseTagVersion.VeryFirstVersion;
            var overrides = new TagsOverride().MutableAdd( tagged.Sha, bb1Tag.ToString() );

            Action<SimpleCommit> checkOK = sc =>
            {
                var i = repoTest.GetRepositoryInfo( sc.Sha, overrides );
                Assert.That( i.ValidReleaseTag, Is.Null );
                CollectionAssert.AreEqual( bb1Tag.GetDirectSuccessors(), i.PossibleVersions );
                // Now tag the commit and checks that each tag is valid.
                foreach( var next in bb1Tag.GetDirectSuccessors() )
                {
                    var iWithTag = repoTest.GetRepositoryInfo( sc.Sha, overrides.Add( sc.Sha, next.ToString() ) );
                    Assert.That( iWithTag.ValidReleaseTag, Is.EqualTo( next ) );
                }
            };

            Action<SimpleCommit> checkKO = sc =>
            {
                var i = repoTest.GetRepositoryInfo( sc.Sha, overrides );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.PossibleVersions, Is.Empty );
                // Now tag the commit and checks that each tag is invalid.
                foreach( var next in bb1Tag.GetDirectSuccessors() )
                {
                    var iWithTag = repoTest.GetRepositoryInfo( sc.Sha, overrides.Add( sc.Sha, next.ToString() ) );
                    Assert.That( iWithTag.ValidReleaseTag, Is.Null );
                    Assert.That( iWithTag.ReleaseTagErrorLines, Is.Not.Null );
                }
            };

            // The version on the commit point.
            {
                var i = repoTest.GetRepositoryInfo( tagged.Sha, overrides );
                Assert.That( i.ValidReleaseTag, Is.EqualTo( bb1Tag ) );
                CollectionAssert.AreEqual( ReleaseTagVersion.FirstPossibleVersions, i.PossibleVersionsStrict );
            };

            // Checking possible versions before: none.
            var before1 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branch 'a' into b" ) );
            checkKO( before1 );
            var before2 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second a/a2" ) );
            checkKO( before2 );
            var before3 = repoTest.Commits.First( sc => sc.Message.StartsWith( "On master again" ) );
            checkKO( before3 );

            // Checking possible versions after: all successors are allowed.
            var after1 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second b/b2" ) );
            checkOK( after1 );
            var after2 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branch 'b' into c" ) );
            checkOK( after2 );
            var after3 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branches 'c', 'd' and 'e'" ) );
            checkOK( after3 );

        }

        [Test]
        public void ignoring_legacy_versions_with_StartingVersionForCSemVer_option()
        {
            var repoTest = TestHelper.TestGitRepository;
            var cOK = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second b/b1" ) );
            var cKO1 = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second a/a1" ) );
            var cKO2 = repoTest.Commits.First( sc => sc.Message.StartsWith( "First b/b1" ) );
            var cKO3 = repoTest.Commits.First( sc => sc.Message.StartsWith( "First a/a2" ) );

            var overrides = new TagsOverride()
                                    .MutableAdd( cOK.Sha, "4.0.3-beta" )
                                    .MutableAdd( cKO1.Sha, "0.0.0-alpha" )
                                    .MutableAdd( cKO2.Sha, "1.1.0" )
                                    .MutableAdd( cKO3.Sha, "2.0.2" );

            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( cOK.Sha, overrides );
                Assert.That( i.ReleaseTagErrorText, Is.Not.Null );
            }
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cOK.Sha,
                    OverriddenTags = overrides.Overrides,
                    StartingVersionForCSemVer = "4.0.3-beta"
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.ValidReleaseTag.ToString(), Is.EqualTo( "v4.0.3-beta" ) );
                Assert.That( i.PreviousRelease, Is.Null );
                CollectionAssert.AreEqual( i.PossibleVersions.Select( t => t.ToString() ), new[] { "v4.0.3-beta" } );
            }
            {
                var cAbove = repoTest.Commits.First( sc => sc.Message.StartsWith( "Second b/b2" ) );
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cAbove.Sha,
                    OverriddenTags = overrides.Overrides,
                    StartingVersionForCSemVer = "4.0.3-beta"
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.PreviousRelease.ThisTag.ToString(), Is.EqualTo( "v4.0.3-beta" ) );
                Assert.That( i.ValidReleaseTag, Is.Null );
                CollectionAssert.Contains( i.PossibleVersions.Select( t => t.ToString() ), "v4.0.3-beta.0.1", "v4.0.3-beta.1", "v4.0.3-delta", "v4.0.3", "v4.1.0-rc", "v4.1.0", "v5.0.0" );
            }

            // Commit before the StartingVersionForCSemVer has no PossibleVersions.
            {
                var cBelow = repoTest.Commits.First( sc => sc.Message.StartsWith( "On master again" ) );
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cBelow.Sha,
                    OverriddenTags = overrides.Overrides,
                    StartingVersionForCSemVer = "4.0.3-beta"
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.PreviousRelease, Is.Null );
                Assert.That( i.ValidReleaseTag, Is.Null );
                CollectionAssert.IsEmpty( i.PossibleVersions );
            }
            {
                var cBelow = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branch 'a' into b" ) );
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cBelow.Sha,
                    OverriddenTags = overrides.Overrides,
                    StartingVersionForCSemVer = "4.0.3-beta"
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.PreviousRelease, Is.Null );
                Assert.That( i.ValidReleaseTag, Is.Null );
                CollectionAssert.IsEmpty( i.PossibleVersions );
            }
        }

        [Test]
        public void propagation_through_multiple_hops()
        {
            var repoTest = TestHelper.TestGitRepository;
            var cAlpha = repoTest.Commits.First( sc => sc.Message.StartsWith( "Real Dev in Alpha." ) );
            // cReleased is "Merge branch 'gamma' into parallel-world" but there are two of them...
            // This is the head of parallel-world branch.
            var cReleased = repoTest.Commits.First( sc => sc.Sha == "fc9802013c23398978744de1618fb01638f7347e" );
            var v1beta = ReleaseTagVersion.TryParse( "1.0.0-beta" );
            var overrides = new TagsOverride().MutableAdd( cAlpha.Sha, "1.0.0-beta" );

            // cReleased
            //   |
            //   |
            // cAlpha - v1.0.0-beta

            // This is "normal": cReleased has 1.0.0-beta in its parent.
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cReleased.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.PreviousRelease.ThisTag, Is.EqualTo( v1beta ) );
                Assert.That( i.ValidReleaseTag, Is.Null );
                CollectionAssert.AreEqual( v1beta.GetDirectSuccessors(), i.PossibleVersions );
                CollectionAssert.AreEqual( v1beta.GetDirectSuccessors(), i.PossibleVersionsStrict );
            }

            var cAlphaContinue = repoTest.Commits.First( sc => sc.Message.StartsWith( "Dev again in Alpha." ) );
            // We set 2.0.0 on cReleased. Its content is the same as cAlpha (mege commits with no changes). 
            //
            // cAlphaContinue
            //   |
            //   |    cReleased - v2.0.0
            //   |  /
            //   |/
            // cAlpha - v1.0.0-beta

            overrides.MutableAdd( cReleased.Sha, "2.0.0" );
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cReleased.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.ValidReleaseTag.ToString(), Is.EqualTo( "v2.0.0" ) );
            }
            // Subsequent developments of alpha branch now starts after 2.0.0, for instance 2.1.0-beta.
            overrides.MutableAdd( cAlphaContinue.Sha, "2.1.0-beta" );
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cAlphaContinue.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                var tagged = ReleaseTagVersion.TryParse( "2.1.0-beta" );
                Assert.That( i.ReleaseTagErrorText, Is.Null );
                Assert.That( i.ValidReleaseTag, Is.EqualTo( tagged ) );
                CollectionAssert.AreEqual( 
                    new[] { ReleaseTagVersion.TryParse( "1.0.0-beta.0.1" ) }
                            .Concat( ReleaseTagVersion.TryParse( "2.0.0" ).GetDirectSuccessors() ), 
                    i.PossibleVersionsStrict );
                // In no strict mode, alpha branch can continue with any successors of
                // the 1.0.0-beta except the v2.0.0 of course.
                CollectionAssert.AreEqual( 
                            ReleaseTagVersion.TryParse( "1.0.0-beta" ).GetDirectSuccessors()
                            .Where( v => v != ReleaseTagVersion.TryParse( "2.0.0" ) )
                            .Concat( ReleaseTagVersion.TryParse( "2.0.0" ).GetDirectSuccessors() ), 
                    i.PossibleVersions );
            }
        }

        [Test]
        public void StrictMode_content_based_decisions_saves_the_cherry_picks()
        {
            var repoTest = TestHelper.TestGitRepository;

            var cRoot = repoTest.Commits.Single( sc => sc.Message.StartsWith( "First in parallel world." ) );
            var cChange = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Change in parallel-world.txt content (1)." ) );
            var cReset = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Reset change in parallel-world.txt content (2)." ) );

            var cPickReset = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Cherry Pick - Reset change in parallel-world.txt content (2)." ) );
            var cPickChange = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Cherry Pick - Change in parallel-world.txt content (1)." ) );
            var cPostReset = repoTest.Commits.Single( sc => sc.Sha == "3035a581af1302293739e5caf7dfbc009a71454f" ); // "Merge branch 'gamma' into parallel-world" (there are two of them);
            var cDevInGamma = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Dev in Gamma." ) );
            var cMergeAll = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Merge branch 'parallel-world' into alpha" ) );

            var v1 = ReleaseTagVersion.TryParse( "1.0.0" );
            var v2 = ReleaseTagVersion.TryParse( "2.0.0" );
            var overrides = new TagsOverride()
                .MutableAdd( cRoot.Sha, v1.ToString() )
                .MutableAdd( cChange.Sha, v2.ToString() );

            //     cMergeAll            => succ(v2.0.0) 
            //      /     \
            //    /         \
            //   |           |
            // cDevInGamma   |          => fixes(v1.0.0)+succ(v2.0.0).
            //   |           |
            // cPickReset    |          => fixes(v1.0.0)+succ(v2.0.0) because the cherry pick makes cPickChange content v2.0.0 
            //   |           |
            // cPickChange   |          => fixes(v1.0.0). (Its content is actually v2.0.0)   
            //   |           |
            //   |       cPostReset     => succ(v2.0.0)
            //   |           |     
            //   |       cReset         => succ(v2.0.0)
            //   |           |
            //   |   cChange - v2.0.0
            //   |      /
            //   |    /
            //   |  /
            //   |/
            // cRoot - v1.0.0

            Action<SimpleCommit> v1Successors = commit =>
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = commit.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                CollectionAssert.AreEqual( v1.GetDirectSuccessors().Where( t => t.ToString() != "v2.0.0" ), i.PossibleVersionsStrict );
            };

            Action<SimpleCommit> v1FixSuccessors = commit =>
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = commit.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                CollectionAssert.AreEqual( v1.GetDirectSuccessors( true ).Where( t => t.ToString() != "v2.0.0" ), i.PossibleVersionsStrict );
            };

            Action<SimpleCommit> v1FixAndV2Successors = commit =>
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = commit.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                CollectionAssert.AreEqual( v1.GetDirectSuccessors( true ).Where( t => t.ToString() != "v2.0.0" ).Concat( v2.GetDirectSuccessors() ), i.PossibleVersionsStrict );
            };

            Action<SimpleCommit> v2Successors = commit =>
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = commit.Sha,
                    PossibleVersionsMode = PossibleVersionsMode.Restricted,
                    OverriddenTags = overrides.Overrides
                } );
                CollectionAssert.AreEqual( v2.GetDirectSuccessors(), i.PossibleVersionsStrict );
            };

            v2Successors( cReset );
            v1FixSuccessors( cPickChange );
            v1FixAndV2Successors( cPickReset );
            v2Successors( cPostReset );
            v1FixAndV2Successors( cDevInGamma );
            v2Successors( cMergeAll );
        }

        [Test]
        public void CIBuildVersion_with_merged_tags()
        {
            var repoTest = TestHelper.TestGitRepository;

            var cRoot = repoTest.Commits.Single( sc => sc.Message.StartsWith( "First in parallel world." ) );
            var cDevInAlpha = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Dev in Alpha." ) );
            var cDevInBeta = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Dev in Beta." ) );
            var cDevInGamma = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Dev in Gamma." ) );

            var overrides = new TagsOverride()
                        .MutableAdd( cRoot.Sha, "v1.0.0" )
                        .MutableAdd( cDevInAlpha.Sha, "v2.0.0" );

            // cDevInBeta
            //   |
            //   |  cDevInGamma
            //   | / 
            //   |/   cDevInAlpha - v2.0.0
            //   |   /
            //   |  /
            //   | /
            // cRoot - v1.0.0

            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cDevInAlpha.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                Assert.That( i.ValidReleaseTag, Is.EqualTo( ReleaseTagVersion.TryParse( "v2.0.0" ) ) );
            }

            overrides.MutableAdd( cDevInBeta.Sha, "v1.0.1-beta" );
            // cDevInBeta - v1.0.1-beta
            //   |
            //   |  cDevInGamma
            //   | / 
            //   |/   cDevInAlpha - v2.0.0
            //   |  /
            //   | /
            // cRoot - v1.0.0
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cDevInBeta.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                Assert.That( i.ValidReleaseTag, Is.EqualTo( ReleaseTagVersion.TryParse( "v1.0.1-beta" ) ) );
            }

            overrides.MutableAdd( cDevInGamma.Sha, "v1.0.1-alpha" );
            // cDevInBeta - v1.0.1-beta
            //   |
            //   |  cDevInGamma - v1.0.1-alpha
            //   | / 
            //   |/   cDevInAlpha - v2.0.0
            //   |  /
            //   | /
            // cRoot - v1.0.0
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingCommitSha = cDevInGamma.Sha,
                    OverriddenTags = overrides.Overrides
                } );
                Assert.That( i.ValidReleaseTag, Is.EqualTo( ReleaseTagVersion.TryParse( "v1.0.1-alpha" ) ) );
            }
            // On "gamma" branch, the head is 7 commits ahead of the v2.0.0 tag: this is the longest path. 
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "gamma",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "gamma", CIVersionMode = CIBranchVersionMode.LastReleaseBased }
                    }
                } );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "2.0.1--ci-gamma.7" ) );
            }
            // Testing "gamma" branch in ZeroTimed mode. 
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "gamma",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "gamma", CIVersionMode = CIBranchVersionMode.ZeroTimed }
                    }
                } );
                Assert.That( i.CIRelease.BuildVersionNuGet, Is.EqualTo( "0.0.0-Cgamma-00185gh" ) );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "0.0.0--ci-gamma.2015-07-13T07-46-29-00+v2.0.0" ) );
            }
            // On "alpha" branch, the head is 6 commits ahead of the v2.0.0 tag (always the take the longest path). 
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "alpha",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "alpha", VersionName="ALPHAAAA", CIVersionMode = CIBranchVersionMode.LastReleaseBased }
                    }
                } );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "2.0.1--ci-ALPHAAAA.6" ) );
            }
            // Testing "alpha" branch in ZeroTimed mode.  
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "alpha",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "alpha", VersionName="ALPH", CIVersionMode = CIBranchVersionMode.ZeroTimed }
                    }
                } );
                Assert.That( i.CIRelease.BuildVersionNuGet, Is.EqualTo( "0.0.0-CALPH-00187mq" ) );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "0.0.0--ci-ALPH.2015-07-13T10-00-58-00+v2.0.0" ) );
            }
            // On "beta" branch, the head is 6 commits ahead of the v2.0.0 tag. 
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "beta",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "beta", VersionName="BBBBBB", CIVersionMode = CIBranchVersionMode.LastReleaseBased }
                    }
                } );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "2.0.1--ci-BBBBBB.6" ) );
            }
            // Testing ZeroTimed mode on "beta" branch. 
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = "beta",
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = "beta", VersionName="beta", CIVersionMode = CIBranchVersionMode.ZeroTimed }
                    }
                } );
                Assert.That( i.CIRelease.BuildVersionNuGet, Is.EqualTo( "0.0.0-Cbeta-00185fx" ) );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( "0.0.0--ci-beta.2015-07-13T07-45-43-00+v2.0.0" ) );
            }

        }

        [TestCase( "v1.0.0", "alpha", "1.0.1--ci-alpha.1", null, "1.0.1-Calpha-0001" )]
        [TestCase( "v1.0.0", "beta", "1.0.1--ci-beta.1", null, "1.0.1-Cbeta-0001" )]
        [TestCase( "v1.0.0", "gamma", "1.0.1--ci-gamma.2", null, "1.0.1-Cgamma-0002" )]
        [TestCase( "v1.0.0", "parallel-world", "1.0.1--ci-parallel.3", "parallel", "1.0.1-Cparallel-0003" )]
        [TestCase( "v0.1.0-beta", "alpha", "0.1.0-beta.0.0.ci-alpha.1", null, "0.1.0-b00-00-alpha-0001" )]
        [TestCase( "v0.0.0-rc", "beta", "0.0.0-rc.0.0.ci-beta.1", null, "0.0.0-r00-00-beta-0001" )]
        public void CIBuildVersion_from_RealDevInAlpha_commits_ahead_tests( string vRealDevInAlpha, string branchName, string ciBuildVersion, string branchVersionName, string ciBuildVersionNuGet )
        {
            var repoTest = TestHelper.TestGitRepository;
            var cRealDevInAlpha = repoTest.Commits.Single( sc => sc.Message.StartsWith( "Real Dev in Alpha." ) );
            var overrides = new TagsOverride().MutableAdd( cRealDevInAlpha.Sha, vRealDevInAlpha );
            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = branchName,
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = branchName, CIVersionMode = CIBranchVersionMode.LastReleaseBased, VersionName = branchVersionName }
                    }
                } );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( ciBuildVersion ) );
                Assert.That( i.CIRelease.BuildVersionNuGet, Is.EqualTo( ciBuildVersionNuGet ) );
            }
        }

        [TestCase( "v0.0.0-alpha.1.1", "alpha", "0.0.0-alpha.1.1.ci-alpha.6", null, "0.0.0-a01-01-alpha-0006" )]
        [TestCase( "v0.0.0-alpha.2", "alpha", "0.0.0-alpha.2.0.ci-alpha.6", null, "0.0.0-a02-00-alpha-0006" )]
        [TestCase( "v0.0.0-beta", "alpha", "0.0.0-beta.0.0.ci-alpha.6", null, "0.0.0-b00-00-alpha-0006" )]

        [TestCase( "v0.0.0-alpha.1.1", "beta", "0.0.0-alpha.1.1.ci-beta.6", null, "0.0.0-a01-01-beta-0006" )]
        [TestCase( "v0.0.0-alpha.2", "beta", "0.0.0-alpha.2.0.ci-beta.6", null, "0.0.0-a02-00-beta-0006" )]
        [TestCase( "v0.0.0-beta", "beta", "0.0.0-beta.0.0.ci-beta.6", null, "0.0.0-b00-00-beta-0006" )]

        [TestCase( "v0.0.0-alpha.1.1", "parallel-world", "0.0.0-alpha.1.1.ci-parallel.8", "parallel", "0.0.0-a01-01-parallel-0008" )]
        [TestCase( "v0.0.0-alpha.2", "parallel-world", "0.0.0-alpha.2.0.ci-parallel.8", "parallel", "0.0.0-a02-00-parallel-0008" )]
        [TestCase( "v0.0.0-beta", "parallel-world", "0.0.0-beta.0.0.ci-parallel.8", "parallel", "0.0.0-b00-00-parallel-0008" )]

        [TestCase( "v0.0.0-nimp", "f-beta-nothing", "0.0.0-alpha.1.0.ci-XXX.4", "XXX", "0.0.0-a01-00-XXX-0004" )]
        [TestCase( "v0.0.0-dont-care", "f-beta-nothing", "0.0.0-alpha.1.0.ci-YYYY.4", "YYYY", "0.0.0-a01-00-YYYY-0004" )]
        [TestCase( "v0.0.0-onDevInAlpha", "f-beta-nothing", "0.0.0-alpha.1.0.ci-B.4", "B", "0.0.0-a01-00-B-0004" )]
        public void CIBuildVersion_from_DevInAlpha_commits_ahead_tests( string vDevInAlpha, string branchName, string ciBuildVersion, string branchNameVersion, string ciBuildVersionNuGet )
        {
            var repoTest = TestHelper.TestGitRepository;
            var cRoot = repoTest.Commits.First( sc => sc.Message.StartsWith( "First in parallel world." ) );
            var cPickChange = repoTest.Commits.First( sc => sc.Message.StartsWith( "Cherry Pick - Change in parallel-world.txt content (1)." ) );
            var cDevInAlpha = repoTest.Commits.First( sc => sc.Message.StartsWith( "Dev in Alpha." ) );
            var overrides = new TagsOverride()
                .MutableAdd( cRoot.Sha, "v0.0.0-alpha" )
                .MutableAdd( cPickChange.Sha, "v0.0.0-alpha.1" )
                .MutableAdd( cDevInAlpha.Sha, vDevInAlpha );

            {
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    StartingBranchName = branchName,
                    OverriddenTags = overrides.Overrides,
                    Branches = new RepositoryInfoOptionsBranch[]
                    {
                        new RepositoryInfoOptionsBranch() { Name = branchName, CIVersionMode = CIBranchVersionMode.LastReleaseBased, VersionName = branchNameVersion }
                    }
                } );
                Assert.That( i.ValidReleaseTag, Is.Null );
                Assert.That( i.CIRelease.BuildVersion, Is.EqualTo( ciBuildVersion ) );
                Assert.That( i.CIRelease.BuildVersionNuGet, Is.EqualTo( ciBuildVersionNuGet ) );
            }
        }


        [Test]
        public void options_can_contain_IgnoreModified_files()
        {
            var repoTest = TestHelper.TestGitRepository;
            string fileToChange = Directory.EnumerateFiles( repoTest.Path ).FirstOrDefault();
            Assume.That( fileToChange, Is.Not.Null );

            Console.WriteLine( "Modifiying '{0}'.", fileToChange );

            byte[] original = File.ReadAllBytes( fileToChange );

            try
            {
                var options = new RepositoryInfoOptions();

                RepositoryInfo info;

                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False );

                File.WriteAllText( fileToChange, "!MODIFIED!" );
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.True );

                options.IgnoreModifiedFiles.Add( fileToChange.Substring( repoTest.Path.Length + 1 ) );
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False );
            }
            finally
            {
                File.WriteAllBytes( fileToChange, original );
            }
        }


        [Test]
        public void options_IgnoreModified_files_filter()
        {
            var repoTest = TestHelper.TestGitRepository;
            repoTest.CheckOut( "origin/parallel-world" );

            string devPath = Path.Combine( repoTest.Path, "Dev in Alpha.txt" );
            string devTxt = File.ReadAllText( devPath );
            Assume.That( devTxt, Is.EqualTo( "Dev in Aplpha." ) );

            string realDevPath = Path.Combine( repoTest.Path, "Real Dev in Alpha.txt" ); ;
            string realDevTxt = File.ReadAllText( realDevPath );
            Assume.That( realDevTxt, Is.EqualTo( "Real Dev in Alpha." ) );

            try
            {
                RepositoryInfo info;
                var options = new RepositoryInfoOptions();
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False, "Working folder is clean." );

                File.WriteAllText( devPath, "!MODIFIED!" + devTxt );
                File.WriteAllText( realDevPath, "!MODIFIED!" + realDevTxt );

                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, "Working folder is dirty." );

                options.IgnoreModifiedFiles.Add( "Dev in Alpha.txt" );
                options.IgnoreModifiedFiles.Add( "Real Dev in Alpha.txt" );
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False, "Working folder is dirty but IgnoreModifiedFiles explicitly ignores the 2 files." );

                int nbCall = 0;
                options.IgnoreModifiedFiles.Clear();
                options.IgnoreModifiedFilePredicate = m =>
                {
                    // Always returns true: the file is NOT modified.
                    ++nbCall;
                    return true;
                };
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False, "Working folder is dirty but IgnoreModifiedFilePredicate explicitly ignores all files." );
                Assert.That( nbCall, Is.EqualTo( 2 ) );

                nbCall = 0;
                options.IgnoreModifiedFilePredicate = m =>
                {
                    // Returns false: the file is actually modified.
                    // without IgnoreModifiedFileFullProcess, this stops the lookups.
                    ++nbCall;
                    return false;
                };
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, "Working folder is dirty (IgnoreModifiedFilePredicate returned false)." );
                Assert.That( nbCall, Is.EqualTo( 1 ), "As soon as the predicate returns false, the lookup stops." );

                nbCall = 0;
                options.IgnoreModifiedFileFullProcess = true;
                options.IgnoreModifiedFilePredicate = m =>
                {
                    // Returns false: the file is actually modified.
                    // with IgnoreModifiedFileFullProcess = true, the process continues.
                    ++nbCall;
                    return false;
                };
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, "Working folder is dirty (IgnoreModifiedFilePredicate returned false)." );
                Assert.That( nbCall, Is.EqualTo( 2 ), "Thanks to IgnoreModifiedFileFullProcess, all modified files are processed." );

                nbCall = 0;
                options.IgnoreModifiedFiles.Add( "Dev in Alpha.txt" );
                options.IgnoreModifiedFilePredicate = m =>
                {
                    ++nbCall;
                    Assert.That( m.Path, Is.Not.EqualTo( "Dev in Alpha.txt" ), "This has been filtered by IgnoreModifiedFiles set." );
                    Assert.That( m.CommittedText, Is.EqualTo( "Real Dev in Alpha." ) );
                    return m.Path == "Real Dev in Alpha.txt";
                };
                info = repoTest.GetRepositoryInfo( options );
                Assert.That( info.IsDirty, Is.False, "Working folder is dirty but IgnoreModifiedFiles ignores one file and ModifiedFileFilter ignores the other one." );
                Assert.That( nbCall, Is.EqualTo( 1 ) );
            }
            finally
            {
                File.WriteAllText( devPath, devTxt );
                File.WriteAllText( realDevPath, realDevTxt );
            }
        }

        [Test]
        public void fumble_commit_scenario()
        {
            var repoTest = TestHelper.TestGitRepository;
            var cD = repoTest.Commits.First( sc => sc.Message.StartsWith( "D-Commit." ) );
            var cC = repoTest.Commits.First( sc => sc.Message.StartsWith( "C-Commit." ) );
            var cF = repoTest.Commits.First( sc => sc.Sha == "27a629754c6b9034f7ca580442b589a0241773c5" );
            var cB = repoTest.Commits.First( sc => sc.Message.StartsWith( "B-Commit." ) );
            var cA = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branch 'fumble-develop' into fumble-master" ) );
            var cFix = repoTest.Commits.First( sc => sc.Sha == "e6766d127f9a2df42567151222c6569601614626" );
            var cX = repoTest.Commits.First( sc => sc.Message.StartsWith( "X-Commit." ) );
            var overrides = new TagsOverride()
                .MutableAdd( cD.Sha, "v4.3.2" )
                .MutableAdd( cC.Sha, "v4.4.0-alpha" )
                .MutableAdd( cB.Sha, "v5.0.0-rc" )
                .MutableAdd( cA.Sha, "v5.0.0" );
            var v5 = ReleaseTagVersion.TryParse( "v5.0.0" );
            var v5rc = ReleaseTagVersion.TryParse( "v5.0.0-rc" );
            var v5rc01 = ReleaseTagVersion.TryParse( "v5.0.0-rc.0.1" );
            var v5rc1 = ReleaseTagVersion.TryParse( "v5.0.0-rc.1" );
            {
                // On the fix of the fumble commit, only v5.0.0-rc.0.1 is possible in Restricted mode.
                // Restricted mode disallow rc.1 (next version below the first already released next one).  
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    OverriddenTags = overrides.Overrides,
                    StartingCommitSha = cFix.Sha
                } );
                Assert.That( i.PreviousRelease.ThisTag, Is.EqualTo( v5rc ) );
                Assert.That( i.PreviousMaxRelease.ThisTag, Is.SameAs( i.PreviousRelease.ThisTag ) );
                CollectionAssert.AreEqual( new[] { v5rc01 }, i.PossibleVersionsStrict );
                // In default mode, the fix of the fumble commit, v5.0.0-rc.0.1 and rc.1 are possible.  
                CollectionAssert.AreEqual( new[] { v5rc01, v5rc1 }, i.PossibleVersions );
            }
            {
                // Above the fix of the fumble commit, v5.0.0-rc.0.1 and any successor of the 5.0.0 is possible.
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    OverriddenTags = overrides.Overrides,
                    StartingCommitSha = cX.Sha
                } );
                Assert.That( i.PreviousRelease.ThisTag, Is.EqualTo( v5rc ) );
                Assert.That( i.PreviousMaxRelease.ThisTag, Is.EqualTo( v5 ) );

                var possible = new List<ReleaseTagVersion>() { v5rc01, v5rc1 };
                possible.AddRange( v5.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possible, i.PossibleVersions );

                var possibleStrict = new List<ReleaseTagVersion>() { v5rc01 };
                possibleStrict.AddRange( v5.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possibleStrict, i.PossibleVersionsStrict );
            }
        }

        [Test]
        public void fumble_commit_plus_an_extra_content_with_a_big_release_number()
        {
            var repoTest = TestHelper.TestGitRepository;
            var cD = repoTest.Commits.First( sc => sc.Message.StartsWith( "D-Commit." ) );
            var cC = repoTest.Commits.First( sc => sc.Message.StartsWith( "C-Commit." ) );
            var cF = repoTest.Commits.First( sc => sc.Sha == "27a629754c6b9034f7ca580442b589a0241773c5" );
            var cB = repoTest.Commits.First( sc => sc.Message.StartsWith( "B-Commit." ) );
            var cA = repoTest.Commits.First( sc => sc.Message.StartsWith( "Merge branch 'fumble-develop' into fumble-master" ) );
            var cFix = repoTest.Commits.First( sc => sc.Sha == "e6766d127f9a2df42567151222c6569601614626" );
            var cX = repoTest.Commits.First( sc => sc.Message.StartsWith( "X-Commit." ) );
            var cExtra = repoTest.Commits.First( sc => sc.Message.StartsWith( "C-Commit (cherry pick)." ) );
            var overrides = new TagsOverride()
                .MutableAdd( cD.Sha, "v4.3.2" )
                .MutableAdd( cC.Sha, "v4.4.0-alpha" )
                .MutableAdd( cB.Sha, "v5.0.0-rc" )
                .MutableAdd( cA.Sha, "v5.0.0" )
                .MutableAdd( cExtra.Sha, "v10.0.0" );
            var v5 = ReleaseTagVersion.TryParse( "v5.0.0" );
            var v5rc = ReleaseTagVersion.TryParse( "v5.0.0-rc" );
            var v5rc01 = ReleaseTagVersion.TryParse( "v5.0.0-rc.0.1" );
            var v5rc1 = ReleaseTagVersion.TryParse( "v5.0.0-rc.1" );
            var v10 = ReleaseTagVersion.TryParse( "v10.0.0" );
            {
                // The injected v10 overrides everything except the possibilty to release the v5.0.0-rc.0.1.
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    OverriddenTags = overrides.Overrides,
                    StartingCommitSha = cFix.Sha
                } );
                Assert.That( i.PreviousRelease.ThisTag, Is.EqualTo( v5rc ) );
                Assert.That( i.PreviousMaxRelease.ThisTag, Is.EqualTo( v10 ) );

                var possibleStrict = new List<ReleaseTagVersion>() { v5rc01 };
                possibleStrict.AddRange( v10.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possibleStrict, i.PossibleVersionsStrict );

                var possible = new List<ReleaseTagVersion>() { v5rc01, v5rc1 };
                possible.AddRange( v10.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possible, i.PossibleVersions );
            }
            {
                // On B-Commit:
                // Restricted Mode: it is the same, v4.4.0-alpha.0.1 and successors of v10.
                // AllSuccessors Mode: all successors of v4.4.0-alpha (except the v5.0.0) are allowed and successors of v10.
                var v44a = ReleaseTagVersion.TryParse( "v4.4.0-alpha" );
                var v44a01 = ReleaseTagVersion.TryParse( "v4.4.0-alpha.0.1" );
                var v44a1 = ReleaseTagVersion.TryParse( "v4.4.0-alpha.1" );
                var v500 = ReleaseTagVersion.TryParse( "v5.0.0" );
                RepositoryInfo i = repoTest.GetRepositoryInfo( new RepositoryInfoOptions
                {
                    OverriddenTags = overrides.Overrides,
                    StartingCommitSha = cB.Sha
                } );
                Assert.That( i.PreviousRelease.ThisTag, Is.EqualTo( v44a ) );
                Assert.That( i.PreviousMaxRelease.ThisTag, Is.EqualTo( v10 ) );

                var possibleStrict = new List<ReleaseTagVersion>() { v44a01 };
                possibleStrict.AddRange( v10.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possibleStrict, i.PossibleVersionsStrict );

                var possible = new List<ReleaseTagVersion>();
                possible.AddRange( v44a.GetDirectSuccessors().Where( v => v != v500 ) );
                possible.AddRange( v10.GetDirectSuccessors() );
                CollectionAssert.AreEqual( possible, i.PossibleVersions );
            }
        }
    }
}

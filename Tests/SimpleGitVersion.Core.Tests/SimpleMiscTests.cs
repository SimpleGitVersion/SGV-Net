using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using Semver;
using System.IO;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class SimpleMiscTests
    {
        [Test]
        public void sha1_of_trees_rocks()
        {
            var first = TestHelper.TestGitRepository.Commits.First( sc => sc.Message.StartsWith( "First in parallel world." ) );
            var modified = TestHelper.TestGitRepository.Commits.First( sc => sc.Message.StartsWith( "Change in parallel-world.txt content (1)." ) );
            var reset = TestHelper.TestGitRepository.Commits.First( sc => sc.Message.StartsWith( "Reset change in parallel-world.txt content (2)." ) );
            using( var r = new Repository( TestHelper.TestGitRepositoryFolder ) )
            {
                var cFirst = r.Lookup<Commit>( first.Sha );
                var cModified = r.Lookup<Commit>( modified.Sha );
                var cReset = r.Lookup<Commit>( reset.Sha );
                Assert.That( cFirst.Tree.Sha, Is.Not.EqualTo( cModified.Tree.Sha ) );
                Assert.That( cReset.Tree.Sha, Is.Not.EqualTo( cModified.Tree.Sha ) );
                Assert.That( cFirst.Tree.Sha, Is.EqualTo( cReset.Tree.Sha ) );
            }
        }

        [Test]
        public void testing_connection_to_this_repository()
        {
            using( var thisRepo = GitHelper.LoadFromPath( TestHelper.SolutionFolder ) )
            {
                Console.WriteLine( "This repo has {0} commits", thisRepo.Commits.Count() );
            }
        }

        [Test]
        public void testing_SimpleGitRepositoryInfo_on_this_repository()
        {
            var info = SimpleRepositoryInfo.LoadFromPath( new ConsoleLogger(), TestHelper.SolutionFolder );
            Console.WriteLine( "This repo's SemVer: {0}", info.SemVer );
        }

        [Test]
        public void testing_SimpleGitRepositoryInfo_on_other_repository()
        {
            var info = SimpleRepositoryInfo.LoadFromPath( new ConsoleLogger(), @"C:\Dev\CK-Database\CK-SqlServer-Parser" );
            Console.WriteLine( "This repo's SemVer: {0}", info.SemVer );
        }
    }
}

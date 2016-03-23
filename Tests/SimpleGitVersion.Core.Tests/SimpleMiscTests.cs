using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class SimpleMiscTests
    {

        [Test]
        public void checking_that_SimpleGitVersion_Core_props_file_reference_the_current_native_dll_name()
        {
            var binPath = Path.Combine( TestHelper.SolutionFolder, "SimpleGitVersion.Core", "bin" );
            var allGit2Dll = Directory.EnumerateFiles( binPath, "git2-*.dll", SearchOption.AllDirectories );
            Assert.That( allGit2Dll.Count(), Is.GreaterThan( 0 ), "There must be at least one git2-XXXXX.dll file inside SimpleGitVersion.Core/bin folder!" );
            string firstFound = Path.GetFileName( allGit2Dll.First() );
            Assert.That( allGit2Dll.All( p => Path.GetFileName( p ) == firstFound ), "All git2-XXXXX.dll inside SimpleGitVersion.Core/bin folder must be the same!" );

            var propsPath = Path.Combine( TestHelper.SolutionFolder, "SimpleGitVersion.Core", "NuGetAssets", "SimpleGitVersion.Core.props" );
            int countDllName = new Regex( Regex.Escape( firstFound ) ).Matches( File.ReadAllText( propsPath ) ).Count;
            Assert.That( countDllName, Is.EqualTo( 4 ), "There must be exactly 4 references to {0} in SimpleGitVersion.Core/NuGetAssets/SimpleGitVersion.Core.props file.", firstFound );
        }


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

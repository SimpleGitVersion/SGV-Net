using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{
    static class TestHelper
    {
        static string _solutionFolder;
        static string _testGitRepositoryFolder;
        static RepositoryTester _testGitRepository;

        public static string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
        }

        public static string TestGitRepositoryFolder
        {
            get
            {
                if( _testGitRepositoryFolder == null )
                {
                    _testGitRepositoryFolder = Path.GetFullPath( Path.Combine( SolutionFolder, @"Tests\TestGitRepository" ) );
                    string gitPath = _testGitRepositoryFolder + @"\.git";
                    if( !Directory.Exists( gitPath ) )
                    {
                        Directory.CreateDirectory( _testGitRepositoryFolder );
                        gitPath = Repository.Clone( "https://github.com/SimpleGitVersion/TestGitRepository.git", _testGitRepositoryFolder );
                    }
                    using( var r = new Repository( gitPath ) )
                    {
                        r.Fetch( "origin", new FetchOptions() { TagFetchMode = TagFetchMode.All } );
                    }
                }
                return _testGitRepositoryFolder;
            }
        }

        static public RepositoryTester TestGitRepository
        {
            get { return _testGitRepository ?? (_testGitRepository = new RepositoryTester( TestGitRepositoryFolder ));}
        }

        public static string RepositoryXSDPath
        {
            get { return Path.Combine( TestHelper.SolutionFolder, "SimpleGitVersionTask", "NuGetAssets", "RepositoryInfo.xsd" ); }
        }


        static void InitalizePaths()
        {
            string p = new Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase ).LocalPath;
            do
            {
                p = Path.GetDirectoryName( p );
            }
            while( !Directory.Exists( Path.Combine( p, ".git" ) ) );
            _solutionFolder = p;
        }

    }
}

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
            get { return Path.GetFullPath( Path.Combine( SolutionFolder, @"..\TestGitRepository" ) ); }
        }

        static public RepositoryTester TestGitRepository
        {
            get { return _testGitRepository ?? (_testGitRepository = new RepositoryTester( TestGitRepositoryFolder ));}
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

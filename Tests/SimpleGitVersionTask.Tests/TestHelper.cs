using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersionTask.Tests
{
    static class TestHelper
    {
        static string _solutionFolder;

        public static string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
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

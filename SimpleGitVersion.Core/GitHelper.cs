using System;
using System.IO;
using LibGit2Sharp;

namespace SimpleGitVersion
{
    class GitHelper
    {
        public static Repository LoadFromPath( string path )
        {
            path = Path.GetFullPath( path );
            var gitDir = Repository.Discover( path );
            return gitDir != null ? new Repository( gitDir ) : null;
        }
    }
}
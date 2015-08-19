using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{
    class RepositoryTester
    {
        public readonly string Path;
        public readonly List<SimpleCommit> Commits;

        public RepositoryTester( string path )
        {
            Path = path;
            using( var r = new Repository( path ) )
            {
                Commits = r.Commits.QueryBy( new CommitFilter { Since = r.Refs }).Select( c => new SimpleCommit() { Sha = c.Sha, Message = c.Message } ).ToList();
            }
        }

        public RepositoryInfo GetRepositoryInfo( RepositoryInfoOptions options = null )
        {
            return RepositoryInfo.LoadFromPath( Path, options );
        }
        
        public RepositoryInfo GetRepositoryInfo( string commitSha, TagsOverride tags = null )
        {
            return RepositoryInfo.LoadFromPath( Path, new RepositoryInfoOptions { StartingCommitSha = commitSha, OverridenTags = tags != null ? tags.Overrides : null } );
        }
    }
}


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
        readonly List<SimpleCommit> _commits;

        public RepositoryTester( string path )
        {
            Path = path;
            using( var r = new Repository( path ) )
            {
                _commits = r.Commits.QueryBy( new CommitFilter { IncludeReachableFrom = r.Refs }).Select( c => new SimpleCommit() { Sha = c.Sha, Message = c.Message } ).ToList();
            }
        }

        public IReadOnlyList<SimpleCommit> Commits { get { return _commits; } }

        public void CheckOut( string branchName )
        {
            using( var r = new Repository( Path ) )
            {
                Branch b = r.Branches[branchName];
                Commands.Checkout( r, b, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
            }
        }

        public RepositoryInfo GetRepositoryInfo( RepositoryInfoOptions options = null )
        {
            return RepositoryInfo.LoadFromPath( Path, options );
        }
        
        public RepositoryInfo GetRepositoryInfo( string commitSha, TagsOverride tags = null )
        {
            return RepositoryInfo.LoadFromPath( Path, new RepositoryInfoOptions { StartingCommitSha = commitSha, OverriddenTags = tags != null ? tags.Overrides : null } );
        }
    }
}


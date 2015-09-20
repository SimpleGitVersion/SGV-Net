using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{

    partial class TagCollector
    {
        public CommitVersionInfo GetVersionInfo( Commit c )
        {
            string commitSha = c.Sha;
            CommitVersionInfo info;
            if( _versionsCache.TryGetValue( commitSha, out info ) )
            {
                return info;
            }
            IFullTagCommit thisCommit = GetCommit( commitSha );
            IFullTagCommit contentCommit = thisCommit ?? GetCommit( c.Tree.Sha );
            CommitVersionInfo prevCommitParent;
            CommitVersionInfo baseCommitParent;
            ReadParentInfo( c, out prevCommitParent, out baseCommitParent );
            info = new CommitVersionInfo( this, commitSha, thisCommit, contentCommit, prevCommitParent, baseCommitParent );
            _versionsCache.Add( commitSha, info );
            return info;
        }

        void ReadParentInfo( Commit c, out CommitVersionInfo prevCommitParent, out CommitVersionInfo bestBaseParent )
        {
            Debug.Assert( !_versionsCache.ContainsKey( c.Sha ) );
            prevCommitParent = bestBaseParent = null;
            foreach( var p in c.Parents )
            {
                Debug.Assert( bestBaseParent == null || bestBaseParent.BestTag != null );

                var pV = GetVersionInfo( p );
                if( prevCommitParent == null ) prevCommitParent = pV;
                else
                {
                    var prevCommitTag = prevCommitParent.ThisTag ?? prevCommitParent.PreviousTag;
                    var prevTag = pV.ThisTag ?? pV.PreviousTag;
                    if( prevCommitTag == null )
                    {
                        if( prevTag != null || prevCommitParent.PreviousDepth < pV.PreviousDepth )
                        {
                            prevCommitParent = pV;
                        }
                    }
                    else 
                    {
                        int cmp = prevCommitTag.CompareTo( prevTag );
                        if( cmp < 0 || (cmp == 0 && prevCommitParent.PreviousDepth < pV.PreviousDepth) )
                        {
                            prevCommitParent = pV;
                        }
                    }
                }

                var bestTag = pV.BestTag;
                if( bestTag != null )
                {
                    if( bestBaseParent == null || bestBaseParent.BestTag < bestTag )
                    {
                        bestBaseParent = pV;
                    }
                }
            }
        }
    }
}

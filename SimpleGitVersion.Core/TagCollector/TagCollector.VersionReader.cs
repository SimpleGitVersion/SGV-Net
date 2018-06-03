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
            CommitVersionInfo prevCommitParent, prevMaxCommitParent;
            ReadParentInfo( c, out prevCommitParent, out prevMaxCommitParent );
            info = new CommitVersionInfo( this, commitSha, thisCommit, contentCommit, prevCommitParent, prevMaxCommitParent );
            _versionsCache.Add( commitSha, info );
            return info;
        }

        void ReadParentInfo( Commit c, out CommitVersionInfo prevCommitParent, out CommitVersionInfo prevMaxCommitParent )
        {
            Debug.Assert( !_versionsCache.ContainsKey( c.Sha ) );
            prevCommitParent = prevMaxCommitParent = null;
            foreach( var p in c.Parents )
            {
                var pV = GetVersionInfo( p );

                var prevTag = pV.ThisTag ?? pV.PreviousTag;
                if( prevTag != null )
                {
                    if( prevCommitParent == null
                        || prevTag > (prevCommitParent.ThisTag ?? prevCommitParent.PreviousTag) )
                    {
                        prevCommitParent = pV;
                    }
                }

                if( prevMaxCommitParent == null ) prevMaxCommitParent = pV;
                else
                {
                    var prevMaxTag = prevMaxCommitParent.MaxTag;
                    var maxTag = pV.MaxTag;
                    if( prevMaxTag == null )
                    {
                        if( maxTag != null || prevMaxCommitParent.PreviousMaxCommitDepth < pV.PreviousMaxCommitDepth )
                        {
                            prevMaxCommitParent = pV;
                        }
                    }
                    else 
                    {
                        int cmp = prevMaxTag.CompareTo( maxTag );
                        if( cmp < 0 ) prevMaxCommitParent = pV;
                        else if( cmp == 0 )
                        {
                            // Same MaxTag: we must choose the one with the greatest Depth for the parent.
                            int curDepth = prevMaxCommitParent.PreviousMaxCommitDepth;
                            if( prevMaxCommitParent.ThisContentCommit?.ThisTag > prevMaxCommitParent.PreviousMaxTag )
                            {
                                curDepth = 0;
                            }
                            int pVDepth = pV.PreviousMaxCommitDepth;
                            if( pV.ThisContentCommit?.ThisTag > pV.PreviousMaxTag )
                            {
                                pVDepth = 0;
                            }
                            if( curDepth < pVDepth ) prevMaxCommitParent = pV;
                        }
                    }
                }
            }
        }
    }
}

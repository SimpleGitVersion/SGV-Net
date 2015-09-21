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
            CommitVersionInfo prevCommitParent, baseCommitParent, ciBaseCommitParent;
            ReadParentInfo( c, out prevCommitParent, out baseCommitParent, out ciBaseCommitParent );
            info = new CommitVersionInfo( this, commitSha, thisCommit, contentCommit, prevCommitParent, baseCommitParent, ciBaseCommitParent );
            _versionsCache.Add( commitSha, info );
            return info;
        }

        void ReadParentInfo( Commit c, out CommitVersionInfo prevCommitParent, out CommitVersionInfo bestBaseParent, out CommitVersionInfo ciBaseCommitParent )
        {
            Debug.Assert( !_versionsCache.ContainsKey( c.Sha ) );
            prevCommitParent = bestBaseParent = ciBaseCommitParent = null;
            foreach( var p in c.Parents )
            {
                Debug.Assert( bestBaseParent == null || bestBaseParent.BestTag != null );

                var pV = GetVersionInfo( p );

                if( ciBaseCommitParent == null ) ciBaseCommitParent = pV;
                else
                {
                    var ciBaseTag = ciBaseCommitParent.CIBaseTag;
                    var maxTag = pV.CIBaseTag;
                    if( ciBaseTag == null )
                    {
                        if( maxTag != null || ciBaseCommitParent.CIBaseDepth < pV.CIBaseDepth )
                        {
                            ciBaseCommitParent = pV;
                        }
                    }
                    else 
                    {
                        int cmp = ciBaseTag.CompareTo( maxTag );
                        if( cmp < 0 || (cmp == 0 && ciBaseCommitParent.CIBaseDepth < pV.CIBaseDepth) )
                        {
                            ciBaseCommitParent = pV;
                        }
                    }
                }

                var prevTag = pV.ThisTag ?? pV.PreviousTag;
                if( prevTag != null )
                {
                    if( prevCommitParent == null || (prevCommitParent.ThisTag ?? prevCommitParent.PreviousTag) < prevTag )
                    {
                        prevCommitParent = pV;
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

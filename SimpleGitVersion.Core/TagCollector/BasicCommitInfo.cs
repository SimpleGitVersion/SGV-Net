using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public class BasicCommitInfo
    {
        /// <summary>
        /// The commit tag of the commit point.
        /// Can be null.
        /// </summary>
        public readonly ITagCommit UnfilteredThisCommit;

        /// <summary>
        /// The best commit associated to the commit point (itself or the best version associated to
        /// the content's commit).
        /// Never null if <see cref="UnfilteredThisCommit"/> is not null.
        /// </summary>
        public readonly ITagCommit BestCommit;

        /// <summary>
        /// Gets the best commit tag below. Null if <see cref="BestCommit"/> is better
        /// than any parent commits.
        /// </summary>
        public readonly ITagCommit BestCommitBelow;

        /// <summary>
        /// The maximal number of commit points required to reach the <see cref="BestCommitBelow"/>.
        /// Zero if <see cref="BestCommit"/> is better than any parent commits.
        /// </summary>
        public readonly int BelowDepth;

        /// <summary>
        /// Gets the best among <see cref="BestCommit"/> and <see cref="BestCommitBelow"/>.
        /// This is never null.
        /// </summary>
        public ITagCommit MaxCommit => BestCommit?.ThisTag > BestCommitBelow?.ThisTag ? BestCommit : BestCommitBelow;

        /// <summary>
        /// Gets whether <see cref="BestCommit"/> is not the original <see cref="UnfilteredThisCommit"/> point:
        /// the commit content is associated to a better version by another commit. 
        /// </summary>
        public bool IsBestCommitRedirected => BestCommit != null && UnfilteredThisCommit != BestCommit;

        internal BasicCommitInfo( ITagCommit thisCommit, ITagCommit best, BasicCommitInfo parent )
        {
            UnfilteredThisCommit = thisCommit;
            if( best != null )
            {
                BestCommit = best;
                if( parent != null )
                {
                    var maxBelow = parent.MaxCommit;
                    if( best.ThisTag > maxBelow.ThisTag )
                    {
                        BelowDepth = 0;
                    }
                    else
                    {
                        BelowDepth = parent.BelowDepth + 1;
                        BestCommitBelow = maxBelow;
                    }
                }
            }
            else
            {
                Debug.Assert( parent != null, "Not both can be null." );
                BelowDepth = parent.BelowDepth + 1;
                BestCommitBelow = parent.MaxCommit;
            }
            Debug.Assert( MaxCommit != null );
        }

        internal bool IsBetterThan( BasicCommitInfo other )
        {
            int cmp = MaxCommit.ThisTag.CompareTo( other.MaxCommit.ThisTag );
            return cmp == 0 ? BelowDepth > other.BelowDepth : cmp > 0;
        }
    }
}

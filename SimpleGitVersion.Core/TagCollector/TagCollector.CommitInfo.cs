using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSemVer;
using LibGit2Sharp;

namespace SimpleGitVersion
{

    partial class TagCollector
    {
        FilteredView _default;
        Dictionary<CSVersion, FilteredView> _filtered;

        class FilteredView
        {
            readonly TagCollector _collector;
            readonly CSVersion _excluded;
            readonly Dictionary<string, BasicCommitInfo> _cache;

            public FilteredView( TagCollector c, CSVersion excluded )
            {
                _collector = c;
                _excluded = excluded;
                _cache = new Dictionary<string, BasicCommitInfo>();
            }

            public BasicCommitInfo GetInfo( Commit c )
            {
                string sha = c.Sha;
                if( _cache.TryGetValue( sha, out var d ) ) return d;
                TagCommit commit = _collector.GetCommit( sha );
                ITagCommit best;
                if( commit != null )
                {
                    best = commit.GetBestCommitExcept( _excluded );
                }
                else
                {
                    TagCommit content = _collector.GetCommit( c.Tree.Sha );
                    best = content?.GetBestCommitExcept( _excluded );
                }
                BasicCommitInfo p = ReadParents( c );
                if( best != null || p != null ) d = new BasicCommitInfo( commit, best, p );
                _cache.Add( sha, d );
                return d;
            }

            BasicCommitInfo ReadParents( Commit c )
            {
                BasicCommitInfo current = null;
                foreach( var p in c.Parents )
                {
                    var d = GetInfo( p );
                    if( current == null || (d != null && d.IsBetterThan( current )) ) current = d;
                }
                return current;
            }
        }

        FilteredView GetCommitView( CSVersion excluded )
        {
            Debug.Assert( excluded == null || _repoVersions.Versions.Any( t => t.ThisTag == excluded ) );
            if( excluded == null )
            {
                if( _default == null ) _default = new FilteredView( this, null );
                return _default;
            }
            if( _filtered == null ) _filtered = new Dictionary<CSVersion, FilteredView>();
            else if( _filtered.TryGetValue( excluded, out var view ) ) return view;
            var v = new FilteredView( this, excluded );
            _filtered.Add( excluded, v );
            return v;
        }

        public CommitInfo GetCommitInfo( Commit c )
        {
            var basic = GetCommitView( null ).GetInfo( c );

            IReadOnlyList<CSVersion> nextPossibleVersions;
            IReadOnlyList<CSVersion> possibleVersions;
            // Special case: there is no existing versions but there is a startingVersionForCSemVer,
            // every commit may be the first one. 
            if( _startingVersionForCSemVer != null && _repoVersions.Versions.Count == 0 )
            {
                possibleVersions = nextPossibleVersions = new[] { _startingVersionForCSemVer };
            }
            else
            {
                nextPossibleVersions = GetPossibleVersions( basic?.MaxCommit.ThisTag, null );
                bool thisHasCommit = basic?.UnfilteredThisCommit != null;
                // Special case: there is no existing versions (other than this one that is skipped if it exists) but
                // there is a startingVersionForCSemVer, every commit may be the first one. 
                if( _startingVersionForCSemVer != null
                    && _repoVersions.Versions.Count == 1
                    && thisHasCommit )
                {
                    possibleVersions = new[] { _startingVersionForCSemVer };
                }
                else
                {
                    if( thisHasCommit )
                    {
                        var excluded = basic.UnfilteredThisCommit.ThisTag;
                        var noVersion = GetCommitView( excluded ).GetInfo( c );
                        possibleVersions = GetPossibleVersions( noVersion?.MaxCommit.ThisTag, excluded );
                    }
                    else possibleVersions = nextPossibleVersions;
                }
            }
            return new CommitInfo( c.Sha, basic, possibleVersions, nextPossibleVersions );
        }

        List<CSVersion> GetPossibleVersions( CSVersion baseVersion, CSVersion excluded )
        {
            // The base version can be null here: a null version tag correctly generates 
            // the very first possible versions (and the comparison operators handle null).
            IEnumerable<IFullTagCommit> allVersions = _repoVersions.Versions;
            if( excluded != null ) allVersions = allVersions.Where( c => c.ThisTag != excluded ); 
            var nextReleased = allVersions.FirstOrDefault( c => c.ThisTag > baseVersion );
            var successors = CSVersion.GetDirectSuccessors( false, baseVersion );
            return successors.Where( v => v > _startingVersionForCSemVer
                                              && (nextReleased == null || v < nextReleased.ThisTag) )
                             .ToList();
        }


    }
}

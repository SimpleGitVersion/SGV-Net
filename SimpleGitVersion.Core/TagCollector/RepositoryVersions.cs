using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    class RepositoryVersions
    {
        readonly IReadOnlyList<TagCommit> _versions;

        internal RepositoryVersions( IEnumerable<TagCommit> collected, StringBuilder errors, ReleaseTagVersion startingVersionForCSemVer, bool checkCompactExistingVersions )
        {
            Debug.Assert( collected.All( c => c.ThisTag != null ) );
            _versions = collected.OrderBy( t => t.ThisTag ).ToList();
            if( _versions.Count > 0 )
            {
                var first = _versions[0].ThisTag;
                if( checkCompactExistingVersions && startingVersionForCSemVer == null && !first.IsDirectPredecessor( null ) )
                {
                    errors.AppendFormat( "First existing version is '{0}' (on '{1}'). One or more previous versions are missing.", first, _versions[0].CommitSha )
                            .AppendLine();
                }
                for( int i = 0; i < _versions.Count - 1; ++i )
                {
                    var prev = _versions[i].ThisTag;
                    var next = _versions[i + 1].ThisTag;
                    if( next.Equals( prev ) )
                    {
                        errors.AppendFormat( "Version '{0}' is defined on '{1}' and '{2}'.", prev, _versions[i].CommitSha, _versions[i + 1].CommitSha )
                                .AppendLine();
                    }
                    else if( checkCompactExistingVersions && !next.IsDirectPredecessor( prev ) )
                    {
                        errors.AppendFormat( "Missing one or more version(s) between '{0}' and '{1}'.", prev, next )
                                .AppendLine();
                    }
                }
            }
        }

        internal IReadOnlyList<TagCommit> TagCommits => _versions;

        public IReadOnlyList<IFullTagCommit> Versions => _versions; 
    }
}

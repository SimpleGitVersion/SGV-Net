using CSemVer;
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

        internal RepositoryVersions(
            IEnumerable<TagCommit> collected,
            StringBuilder errors,
            CSVersion startingVersionForCSemVer,
            bool checkCompactExistingVersions )
        {
            Debug.Assert( collected.All( c => c.ThisTag != null ) );
            _versions = collected.OrderBy( t => t.ThisTag ).ToList();
            if( _versions.Count > 0 )
            {
                var first = _versions[0].ThisTag;
                if( checkCompactExistingVersions && startingVersionForCSemVer == null && !first.IsDirectPredecessor( null ) )
                {
                    errors.AppendFormat( $"First existing version is '{first}' (on '{_versions[0].CommitSha}'). One or more previous versions are missing." )
                            .AppendLine();
                }
                for( int i = 0; i < _versions.Count - 1; ++i )
                {
                    var prev = _versions[i].ThisTag;
                    var next = _versions[i + 1].ThisTag;
                    if( next.Equals( prev ) )
                    {
                        errors.AppendFormat( $"Version '{prev}' is defined on '{_versions[i].CommitSha}' and '{_versions[i + 1].CommitSha}'." )
                                .AppendLine();
                    }
                    else if( checkCompactExistingVersions && !next.IsDirectPredecessor( prev ) )
                    {
                        errors.AppendFormat( $"Missing one or more version(s) between '{prev}' and '{next}'." )
                                .AppendLine();
                    }
                }
            }
        }

        internal IReadOnlyList<TagCommit> TagCommits => _versions;

        public IReadOnlyList<IFullTagCommit> Versions => _versions; 
    }
}

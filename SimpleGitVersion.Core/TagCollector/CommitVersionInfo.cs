using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public class CommitVersionInfo
    {
        readonly TagCollector _tagCollector;
        readonly string _commitSha;
        readonly IFullTagCommit _thisCommit;
        readonly IFullTagCommit _contentCommit;
        readonly CommitVersionInfo _prevCommit;
        readonly CommitVersionInfo _prevMaxCommit;
        readonly IFullTagCommit _maxCommit;
        readonly int _maxCommitDepth;
        IReadOnlyList<ReleaseTagVersion> _possibleVersions;

        internal CommitVersionInfo( 
            TagCollector tagCollector, 
            string commitSha, 
            IFullTagCommit thisCommit, 
            IFullTagCommit contentCommit, 
            CommitVersionInfo prevCommitParent, 
            CommitVersionInfo prevMaxCommitParent )
        {
            Debug.Assert( thisCommit == null || thisCommit.ThisTag != null );
            Debug.Assert( thisCommit == null || contentCommit == thisCommit, "this commit exists => content commit is this commit" );
            _tagCollector = tagCollector;
            _commitSha = commitSha;
            _thisCommit = thisCommit;
            _contentCommit = contentCommit;

            if( prevCommitParent != null )
            {
                _prevCommit = prevCommitParent._thisCommit != null ? prevCommitParent : prevCommitParent._prevCommit;
            }

            if( prevMaxCommitParent != null )
            {
                Debug.Assert( prevMaxCommitParent.PreviousMaxTag == null || prevMaxCommitParent._prevMaxCommit != null );
                if( prevMaxCommitParent._prevMaxCommit == null || prevMaxCommitParent.BestContentTag > prevMaxCommitParent.PreviousMaxTag )
                {
                    Debug.Assert( prevMaxCommitParent.MaxTag == prevMaxCommitParent.BestContentTag );
                    _prevMaxCommit = prevMaxCommitParent;
                    _maxCommitDepth = 1;
                }
                else
                {
                    Debug.Assert( prevMaxCommitParent.MaxTag == prevMaxCommitParent.PreviousMaxTag );
                    _prevMaxCommit = prevMaxCommitParent._prevMaxCommit;
                    _maxCommitDepth = prevMaxCommitParent._maxCommitDepth + 1;
                }
                Debug.Assert( _prevMaxCommit != null );
            }
            _maxCommit = BestContentTag >= PreviousMaxTag 
                            ? (_contentCommit != null ? _contentCommit.BestCommit : null) 
                            : (_prevMaxCommit._contentCommit != null ? _prevMaxCommit._contentCommit.BestCommit : null);
        }

        public string CommitSha { get { return _commitSha; } }

        public ReleaseTagVersion ThisTag { get { return _thisCommit != null ? _thisCommit.ThisTag : null; } }

        public ITagCommit ThisCommit { get { return _thisCommit; } }

        public ReleaseTagVersion BestContentTag { get { return _contentCommit != null ? _contentCommit.BestCommit.ThisTag : null; } }

        public ReleaseTagVersion PreviousTag { get { return _prevCommit != null ? _prevCommit.ThisTag : null; } }

        public ReleaseTagVersion MaxTag { get { return _maxCommit != null ? _maxCommit.ThisTag : null; } }

        public ReleaseTagVersion PreviousMaxTag { get { return _prevMaxCommit != null ? _prevMaxCommit.MaxTag : null; } }

        public int PreviousMaxTagDepth { get { return _maxCommitDepth; } }


        public CommitVersionInfo PreviousCommit { get { return _prevCommit; } }

        public CommitVersionInfo PreviousMaxCommit { get { return _prevMaxCommit; } }


        /// <summary>
        /// Gets the possible versions on this commit regardless of the actual <see cref="ThisTag"/> already set on it.
        /// These possible versions are not necessarily valid.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> PossibleVersions
        {
            get
            {
                if( _possibleVersions == null )
                {
                    var allVersions = _tagCollector.ExistingVersions.Versions;

                    // Special case: there is no existing versions (other than this that is skippped it it exists) but
                    // there is a startingVersionForCSemVer, every commit may be the first one. 
                    if( _tagCollector.StartingVersionForCSemVer != null && (allVersions.Count == 0 || (allVersions.Count == 1 && ThisTag != null)) )
                    {
                        _possibleVersions = new[] { _tagCollector.StartingVersionForCSemVer };
                    }
                    else
                    {
                        var versions = allVersions.Where( c => c != _thisCommit );

                        List<ReleaseTagVersion> result = new List<ReleaseTagVersion>();
                        foreach( var b in GetBaseTags() )
                        {
                            var nextReleased = versions.FirstOrDefault( c => c.ThisTag > b );
                            var successors = ReleaseTagVersion.GetDirectSuccessors( nextReleased != null, b );
                            foreach( var v in successors.Where( v => v > _tagCollector.StartingVersionForCSemVer && (nextReleased == null || v < nextReleased.ThisTag) ) )
                            {
                                if( !result.Contains( v ) ) result.Add( v );
                            }
                        }
                        _possibleVersions = result;
                    }
                }
                return _possibleVersions;
            }
        }

        IReadOnlyList<ReleaseTagVersion> GetBaseTags()
        {
            var tP = PreviousTag;
            var tM = PreviousMaxTag;
            if( tP != null && tP != tM )
            {
                if( tM != null ) return new[] { tP, tM };
                return new[] { tP };
            }
            return new[] { tM };
        }

        /// <summary>
        /// Gets the valid versions on this commit: this is a subset of the <see cref="PossibleVersions"/>.
        /// Valid versions guaranty that subsequent versions, if any, is built on this commit.
        /// This is not currently implemented: for the moment, ValidVersions are simply a snapshot of the possible ones.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> ValidVersions
        {
            get { return PossibleVersions; }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();

            b.Append( _commitSha );

            if( _contentCommit != null )
            {
                if( _thisCommit == null ) b.Append( " No Tag" );
                else b.Append( ' ' ).Append( _thisCommit.ThisTag.ToString() );
                b.Append( " content=[" )
                    .Append( String.Join( ", ", _contentCommit.GetContentTagCommits( true ).Where( c => c != _thisCommit ).Select( c => c.ThisTag.ToString() ) ) )
                    .Append( ']' );
            }

            if( PreviousTag == null ) b.Append( " No PreviousTag" );
            else b.Append( " Previous=" ).Append( PreviousTag );

            if( PreviousMaxTag != null ) b.Append( " No PreviousMaxtag" );
            else b.Append( " PreviousMaxTag=" ).Append( PreviousMaxTag );
            b.Append( " Depth=" ).Append( _maxCommitDepth );

            return b.ToString();
        }
    }
}
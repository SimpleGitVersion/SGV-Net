using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Final object describing a commit. Release information can easily be generated from this.
    /// </summary>
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
        IReadOnlyList<CSVersion> _possibleVersions;
        IReadOnlyList<CSVersion> _nextPossibleVersions;

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
                if( prevMaxCommitParent._prevMaxCommit == null
                    || prevMaxCommitParent._contentCommit?.BestCommit.ThisTag > prevMaxCommitParent.PreviousMaxTag )
                {
                    Debug.Assert( prevMaxCommitParent.MaxTag == prevMaxCommitParent._contentCommit?.BestCommit.ThisTag );
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
            _maxCommit = _contentCommit?.BestCommit.ThisTag >= PreviousMaxTag 
                            ? (_contentCommit?.BestCommit) 
                            : (_prevMaxCommit._contentCommit?.BestCommit);
        }

        /// <summary>
        /// Gets this commit sha.
        /// </summary>
        public string CommitSha => _commitSha;

        /// <summary>
        /// Gets this <see cref="ITagCommit"/>. Null if no tag is associated to this commit.
        /// </summary>
        public ITagCommit ThisCommit => _thisCommit;

        /// <summary>
        /// Gets the greatest <see cref="ITagCommit"/> associated to the content of this commit.
        /// Null if no other tagged commits exist with this content or if it is <see cref="ThisCommit"/>.
        /// </summary>
        public ITagCommit ThisContentCommit => _thisCommit != _contentCommit
                                                    ? _contentCommit?.BestCommit
                                                    : null; 

        /// <summary>
        /// Gets the greatest version associated to the content of this commit.
        /// Null if no other tagged commits exist with this content.
        /// </summary>
        CSVersion ThisContentTag => ThisContentCommit?.ThisTag;

        /// <summary>
        /// Gets this release tag. Null if no tag is associated to this commit.
        /// </summary>
        public CSVersion ThisTag => _thisCommit?.ThisTag; 

        /// <summary>
        /// Gets the maximum release tag: it can be this tag, this content tag or a previous tag.
        /// </summary>
        public CSVersion MaxTag => _maxCommit?.ThisTag;

        /// <summary>
        /// Gets the maximmum <see cref="ITagCommit"/>. It can be this commit or any previous commit.
        /// </summary>
        public ITagCommit MaxCommit => _maxCommit; 

        /// <summary>
        /// Gets the best previous release tag set among the parent commits.
        /// </summary>
        public CSVersion PreviousTag => _prevCommit?.ThisTag; 

        /// <summary>
        /// Gets the best previous <see cref="ITagCommit"/> set among the parent commits.
        /// </summary>
        public ITagCommit PreviousCommit => _prevCommit?.ThisCommit;

        /// <summary>
        /// Gets the maximum release tag among parents (either explicit tags or tags on content).
        /// </summary>
        public CSVersion PreviousMaxTag => _prevMaxCommit?.MaxTag;

        /// <summary>
        /// Gets the maximum <see cref="ITagCommit"/> among parents (either explicit tags or tags on content).
        /// </summary>
        public ITagCommit PreviousMaxCommit => _prevMaxCommit?._maxCommit;

        /// <summary>
        /// Gets the number of commits between this commit (longest path) and the <see cref="PreviousMaxCommit"/>, including this one:
        /// this is the build index to use for post-releases.
        /// </summary>
        public int PreviousMaxCommitDepth => _maxCommitDepth;

        /// <summary>
        /// Gets the possible versions on this commit regardless of the actual <see cref="ThisTag"/> already set on it.
        /// </summary>
        public IReadOnlyList<CSVersion> PossibleVersions
        {
            get
            {
                if( _possibleVersions == null ) ComputePossibleVersions();
                return _possibleVersions;
            }
        }

        /// <summary>
        /// Gets the possible next versions based on on this commit.
        /// </summary>
        public IReadOnlyList<CSVersion> NextPossibleVersions
        {
            get
            {
                if( _nextPossibleVersions == null ) ComputeNextPossibleVersions();
                return _nextPossibleVersions;
            }
        }

        void ComputePossibleVersions()
        {
            var allVersions = _tagCollector.ExistingVersions.Versions;

            // Special case: there is no existing versions (other than this that is skipped if it exists) but
            // there is a startingVersionForCSemVer, every commit may be the first one. 
            if( _tagCollector.StartingVersionForCSemVer != null
                && (allVersions.Count == 0 || (allVersions.Count == 1 && ThisTag != null)) )
            {
                _possibleVersions = new[] { _tagCollector.StartingVersionForCSemVer };
            }
            else
            {
                var versions = allVersions.Where( c => c != _thisCommit );

                List<CSVersion> possible = new List<CSVersion>();
                if( PreviousTag != null ) CollectPossibleVersions( PreviousTag, versions, possible );
                if( PreviousMaxTag != null && PreviousMaxTag != PreviousTag ) CollectPossibleVersions( PreviousMaxTag, versions, possible );
                if( PreviousMaxTag == null && PreviousTag == null ) CollectPossibleVersions( null, versions, possible );
                _possibleVersions = possible;
            }
        }


        void ComputeNextPossibleVersions()
        {
            var allVersions = _tagCollector.ExistingVersions.Versions;
            // Special case: there is no existing versions but there is a startingVersionForCSemVer,
            // every commit may be the first one. 
            if( _tagCollector.StartingVersionForCSemVer != null && allVersions.Count == 0 )
            {
                _nextPossibleVersions = new[] { _tagCollector.StartingVersionForCSemVer };
            }
            else
            {
                List<CSVersion> possible = new List<CSVersion>();
                var t = ThisTag ?? PreviousTag;
                if( t != null ) CollectPossibleVersions( t, allVersions, possible );
                if( MaxTag != null && MaxTag != t ) CollectPossibleVersions( MaxTag, allVersions, possible );
                if( t == null && MaxTag == null ) CollectPossibleVersions( null, allVersions, possible );
                _nextPossibleVersions = possible;
            }
        }

        void CollectPossibleVersions(CSVersion baseVersion, IEnumerable<IFullTagCommit> allVersions, List<CSVersion> possible )
        {
            // The base version can be null here: a null version tag correctly generates 
            // the very first possible versions (and the comparison operators handle null).
            var nextReleased = allVersions.FirstOrDefault( c => c.ThisTag > baseVersion );
            var successors = CSVersion.GetDirectSuccessors( false, baseVersion );
            foreach( var v in successors.Where( v => v > _tagCollector.StartingVersionForCSemVer
                                                     && (nextReleased == null || v < nextReleased.ThisTag) ) )
            {
                if( !possible.Contains( v ) ) possible.Add( v );
            }
        }

        /// <summary>
        /// Returns either { PreviousTag, PreviousMaxTag }, { PreviousTag }, { PreviousMaxTag } or { null }.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<CSVersion> GetBaseVersions()
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
        /// Overridden to describe the content, previous and max previous tags if they exist.
        /// </summary>
        /// <returns>Detailed string.</returns>
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

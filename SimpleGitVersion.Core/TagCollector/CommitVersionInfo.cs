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
        IReadOnlyList<ReleaseTagVersion> _possibleVersions;
        IReadOnlyList<ReleaseTagVersion> _possibleVersionsStrict;

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

        /// <summary>
        /// Gets this commit sha.
        /// </summary>
        public string CommitSha { get { return _commitSha; } }

        /// <summary>
        /// Gets this release tag. Null if no tag is associated to this commit.
        /// </summary>
        public ReleaseTagVersion ThisTag { get { return _thisCommit != null ? _thisCommit.ThisTag : null; } }

        /// <summary>
        /// Gets this <see cref="ITagCommit"/>. Null if no tag is associated to this commit.
        /// </summary>
         public ITagCommit ThisCommit { get { return _thisCommit; } }

        /// <summary>
        /// Gets the maximum release tag: it can be this tag, this content tag or a previous tag.
        /// </summary>
        public ReleaseTagVersion MaxTag { get { return _maxCommit != null ? _maxCommit.ThisTag : null; } }

        /// <summary>
        /// Gets the maximmum <see cref="ITagCommit"/>. It can be this commit or any previous commit.
        /// </summary>
        public ITagCommit MaxCommit { get { return _maxCommit; } }

        /// <summary>
        /// Gets the best previous release tag set among the parent commits.
        /// </summary>
        public ReleaseTagVersion PreviousTag { get { return _prevCommit != null ? _prevCommit.ThisTag : null; } }

        /// <summary>
        /// Gets the best previous <see cref="ITagCommit"/> set among the parent commits.
        /// </summary>
        public ITagCommit PreviousCommit { get { return _prevCommit != null ? _prevCommit.ThisCommit : null; } }

        /// <summary>
        /// Gets the maximum release tag among parents (either explicit tags or tags on content).
        /// </summary>
        public ReleaseTagVersion PreviousMaxTag { get { return _prevMaxCommit != null ? _prevMaxCommit.MaxTag : null; } }

        /// <summary>
        /// Gets the maximum <see cref="ITagCommit"/> among parents (either explicit tags or tags on content).
        /// </summary>
        public ITagCommit PreviousMaxCommit { get { return _prevMaxCommit != null ? _prevMaxCommit._maxCommit : null; } }

        /// <summary>
        /// Gets the number of commits between this commit (longest path) and the <see cref="PreviousMaxCommit"/>, including this one:
        /// this is the build index to use for post-releases.
        /// </summary>
        public int PreviousMaxCommitDepth { get { return _maxCommitDepth; } }

        /// <summary>
        /// Gets the possible versions on this commit regardless of the actual <see cref="ThisTag"/> already set on it.
        /// These possible versions are not necessarily valid.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> PossibleVersions
        {
            get
            {
                if( _possibleVersions == null ) ComputePossibleVersions();
                return _possibleVersions;
            }
        }

        /// <summary>
        /// Gets the possible versions on this commit in a strict sense: this is a subset 
        /// of the <see cref="PossibleVersions"/>.
        /// A possible versions that is not a <see cref="ReleaseTagVersion.IsPatch"/> do not appear here 
        /// if a greater version exists in the repository.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> PossibleVersionsStrict
        {
            get
            {
                if( _possibleVersionsStrict == null ) ComputePossibleVersions();
                return _possibleVersionsStrict;
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
                _possibleVersionsStrict = _possibleVersions = new[] { _tagCollector.StartingVersionForCSemVer };
            }
            else
            {
                var versions = allVersions.Where( c => c != _thisCommit );

                List<ReleaseTagVersion> resultLarge = new List<ReleaseTagVersion>();
                List<ReleaseTagVersion> resultStrict = new List<ReleaseTagVersion>();
                foreach( var b in GetBaseTags() )
                {
                    // The base tag b can be null here: a null version tag correctly generates 
                    // the very first possible versions (and the comparison operators handle null).
                    var nextReleased = versions.FirstOrDefault( c => c.ThisTag > b );
                    var successors = ReleaseTagVersion.GetDirectSuccessors( false, b );
                    foreach( var v in successors.Where( v => v > _tagCollector.StartingVersionForCSemVer && (nextReleased == null || v < nextReleased.ThisTag) ) )
                    {
                        if( !resultLarge.Contains( v ) )
                        {
                            resultLarge.Add( v );
                            if( nextReleased == null || v.IsPatch )
                            {
                                resultStrict.Add( v );
                            }
                        }
                    }
                }
                _possibleVersions = resultLarge;
                _possibleVersionsStrict = resultStrict;
            }
        }

        ReleaseTagVersion BestContentTag { get { return _contentCommit != null ? _contentCommit.BestCommit.ThisTag : null; } }

        /// <summary>
        /// Returns either { PreviousTag, PreviousMaxTag }, { PreviousTag }, { PreviousMaxTag } or { null }.
        /// </summary>
        /// <returns></returns>
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
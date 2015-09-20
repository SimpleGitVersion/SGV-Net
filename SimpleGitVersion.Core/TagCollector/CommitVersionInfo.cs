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
        readonly CommitVersionInfo _baseCommit;
        readonly int _previousDepth;
        IReadOnlyList<ReleaseTagVersion> _validVersions;

        internal CommitVersionInfo( TagCollector tagCollector, string commitSha, IFullTagCommit thisCommit, IFullTagCommit contentCommit, CommitVersionInfo prevCommitParent, CommitVersionInfo baseCommit )
        {
            Debug.Assert( thisCommit == null || thisCommit.ThisTag != null );
            Debug.Assert( thisCommit == null || contentCommit == thisCommit, "this commit exists => content commit is this commit" );
            _tagCollector = tagCollector;
            _commitSha = commitSha;
            _thisCommit = thisCommit;
            _contentCommit = contentCommit;
            _baseCommit = baseCommit;
            if( prevCommitParent != null )
            {
                if( prevCommitParent._thisCommit != null )
                {
                    _prevCommit = prevCommitParent;
                }
                else
                {
                    _prevCommit = prevCommitParent._prevCommit;
                }
                if( _thisCommit == null ) _previousDepth = prevCommitParent._previousDepth + 1;
            }
        }


        public string CommitSha { get { return _commitSha; } }

        public ReleaseTagVersion ThisTag { get { return _thisCommit != null ? _thisCommit.ThisTag : null; } }

        public ITagCommit ThisCommit { get { return _thisCommit; } }

        internal ReleaseTagVersion GetBaseTag( ReleaseTagVersion exclude )
        {
            var t = ThisTag;
            if( t != null && t != exclude ) return t;
            if( _contentCommit != null )
            {
                t = _contentCommit.BestCommit.ThisTag;
                Debug.Assert( t != null );
                if( t != exclude ) return t;
            }
            t = PreviousTag;
            if( t != exclude ) return t;
            return null;
        }

        internal IReadOnlyList<ReleaseTagVersion> GetBaseTags( ReleaseTagVersion exclude )
        {
            var prev = PreviousTag;
            if( _contentCommit != null )
            {
                var contents = _contentCommit.GetContentTagCommits( true )
                                                .Select( c => c.ThisTag )
                                                .Where( v => v != exclude && v >= prev )
                                                .ToList();
                if( contents.Count > 0 ) return contents;
            }
            return new ReleaseTagVersion[] { prev };
        }

        public ReleaseTagVersion BestTag { get { return _contentCommit != null ? _contentCommit.BestCommit.ThisTag : PreviousTag; } }

        public IEnumerable<ITagCommit> ContentCommits { get { return _contentCommit != null ? _contentCommit.GetContentTagCommits( true ) : Enumerable.Empty<ITagCommit>(); } }

        public ITagCommit ContentBestCommit { get { return _contentCommit != null ? _contentCommit.BestCommit : null; } }

        public ReleaseTagVersion PreviousTag { get { return _prevCommit != null ? _prevCommit.ThisTag : null; } }

        public int PreviousDepth { get { return _previousDepth; } }

        public CommitVersionInfo PreviousCommit { get { return _prevCommit; } }

        public CommitVersionInfo BaseCommit { get { return _baseCommit; } }

        /// <summary>
        /// Gets the possible versions on this commit regardless of the actual <see cref="ThisTag"/> already set on it.
        /// These possible versions are not necessarily valid.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> PossibleVersions
        {
            get
            {
                var allVersions = _tagCollector.ExistingVersions.Versions;

                // Special case: there is no existing versions (other than this that is skippped it it exists) but
                // there is a startingVersionForCSemVer, every commit may be the first one. 
                if( _tagCollector.StartingVersionForCSemVer != null && (allVersions.Count == 0 || (allVersions.Count == 1 && ThisTag != null)) )
                {
                    return new[] { _tagCollector.StartingVersionForCSemVer };
                }
                var versions = allVersions.Where( c => c != _thisCommit );
                IReadOnlyList<ReleaseTagVersion> baseTags = _baseCommit != null ? _baseCommit.GetBaseTags( exclude: ThisTag ) : new ReleaseTagVersion[] { null };

                List<ReleaseTagVersion> result = new List<ReleaseTagVersion>();
                foreach( var b in baseTags )
                {
                    var nextReleased = versions.FirstOrDefault( c => c.ThisTag > b );
                    var successors = ReleaseTagVersion.GetDirectSuccessors( nextReleased != null, b );
                    foreach( var v in successors.Where( v => v > _tagCollector.StartingVersionForCSemVer && (nextReleased == null || v < nextReleased.ThisTag) ))
                    {
                        if( !result.Contains( v ) ) result.Add( v );
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the valid versions on this commit: this is a subset of the <see cref="PossibleVersions"/>.
        /// Valid versions guaranty that subsequent versions, if any, is built on this commit.
        /// This is not currently implemented: for the moment, ValidVersions are simply a snapshot of the possible ones.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> ValidVersions
        {
            get { return _validVersions ?? (_validVersions = PossibleVersions.ToArray()); }
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

            if( _prevCommit == null || _prevCommit.ThisTag == null ) b.Append( " No Previous" );
            else b.Append( " Previous=" ).Append( _prevCommit.ThisTag );
            b.Append( " Depth=" ).Append( _previousDepth );

            if( _baseCommit == null ) b.Append( " No Base" );
            else b.Append( " Base=" ).Append( _baseCommit.BestTag ).Append( '(' ).Append( _baseCommit.CommitSha ).Append( ')' );

            return b.ToString();
        }
    }
}
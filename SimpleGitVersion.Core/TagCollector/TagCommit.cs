using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using CSemVer;

namespace SimpleGitVersion
{
    class TagCommit : IFullTagCommit
    {
        readonly string _commitSha;
        readonly string _contentSha;
        TagCommit _bestTagCommit;
        TagCommit _altBestTagCommit;
        CSVersion _thisTag;
        List<CSVersion> _extraCollectedTags;
        TagCommit _nextSameTree;
        TagCommit _headSameTree;

        public TagCommit( Commit c, CSVersion first )
        {
            Debug.Assert( c != null && first != null && first.IsValid );
            _commitSha = c.Sha;
            _contentSha = c.Tree.Sha;
            _thisTag = first;
        }

        /// <summary>
        /// Gets this commit Sha.
        /// </summary>
        public string CommitSha => _commitSha; 

        /// <summary>
        /// Gets this commit content Sha.
        /// </summary>
        public string ContentSha => _contentSha; 

        /// <summary>
        /// Gets the valid tag, if any, directly associated to this <see cref="CommitSha"/>.
        /// It is necessarily not null once <see cref="TagCollector"/> exposes it: tags that are invalid are 
        /// removed.
        /// </summary>
        public CSVersion ThisTag => _thisTag; 

        /// <summary>
        /// Gets the best commit. This <see cref="IFullTagCommit"/> if no better version exists on the content.
        /// </summary>
        public IFullTagCommit BestCommit => _headSameTree != null ? _headSameTree._bestTagCommit : this;

        /// <summary>
        /// Gets the best commit tag for this commit, skipping the given version.
        /// Null if and only if <see cref="ITagCommit.ThisTag"/> is equal to <paramref name="v"/>
        /// and there is no better <see cref="GetContentTagCommits"/>.
        /// </summary>
        /// <param name="v">The version to ignore. Can be null (BestCommit is returned).</param>
        /// <returns>The best commit tag or null.</returns>
        public ITagCommit GetBestCommitExcept( CSVersion v )
        {
            var best = BestCommit;
            if( best.ThisTag != v ) return best;
            return _headSameTree != null ? _headSameTree._altBestTagCommit : null;
        }

        /// <summary>
        /// Gets all <see cref="IFullTagCommit"/> with the same content.
        /// </summary>
        /// <param name="withThis">True to include this commit into the list.</param>
        /// <returns>A list of the commits with the same content.</returns>
        public IEnumerable<IFullTagCommit> GetContentTagCommits( bool withThis = false )
        {
            if( withThis ) yield return this;
            var n = _headSameTree;
            while( n != null )
            {
                if( n != this ) yield return n;
                n = n._nextSameTree;
            }
        }

        /// <summary>
        /// Gets whether the content of this commit is the same as other exitsting tags.
        /// </summary>
        public bool HasContentTagCommits => _headSameTree != null;

        #region Step 1: Collect

        public void AddCollectedTag( CSVersion t )
        {
            Debug.Assert( t != null );
            if( t.Equals( _thisTag ) )
            {
                if( t.DefinitionStrength > _thisTag.DefinitionStrength ) _thisTag = t;
            }
            else
            {
                if( _extraCollectedTags == null ) _extraCollectedTags = new List<CSVersion>();
                _extraCollectedTags.Add( t );
            }
        }

        /// <summary>
        /// Computes the final release tag: +invalid hides any other version tags.
        /// If multiple versions exist on this commit, an error is raised.
        /// </summary>
        /// <param name="errors">Errors collector.</param>
        /// <returns>False it this tag is invalid.</returns>
        public bool CloseCollect( StringBuilder errors )
        {
            var t = DoCloseCollect( errors );
            if( t != null && t.IsValid )
            {
                _thisTag = t;
                return true;
            }
            _thisTag = null;
            return false;
        }

        CSVersion DoCloseCollect( StringBuilder errors )
        {
            if( _extraCollectedTags == null ) return _thisTag.IsMarkedInvalid ? null : _thisTag;
            _extraCollectedTags.Add( _thisTag );
            var best = _extraCollectedTags.GroupBy( v => v )
                            .Select( g => g.MaxBy( v => v.DefinitionStrength ) )
                            .Where( v => !v.IsMarkedInvalid )
                            .ToList();
            if( best.Count == 0 ) return null;
            if( best.Count > 1 )
            {
                errors.AppendFormat( $"Commit '{CommitSha}' has {best.Count} different released version tags. Delete some of them or create +invalid tag(s) if they are already pushed to a remote repository." )
                      .AppendLine();
                return null;
            }
            return best[0];
        }

        #endregion

        #region Step 2: Same content handling.

        public void AddSameTree( TagCommit otherCommit )
        {
            Debug.Assert( this != otherCommit );
            Debug.Assert( (_nextSameTree == null && _headSameTree == null) || (_nextSameTree != null && _headSameTree != null && _nextSameTree._headSameTree == _headSameTree) );
            if( _headSameTree == null )
            {
                if( otherCommit._headSameTree == null )
                {
                    _headSameTree = this;
                    otherCommit._headSameTree = this;
                    _nextSameTree = otherCommit;
                    if( _thisTag.CompareTo( otherCommit._thisTag ) < 0 ) SetBestTagCommit( otherCommit );
                    else SetBestTagCommit( this );
                }
                else otherCommit._headSameTree.AddSameTreeFromHead( this );
            }
            else _headSameTree.AddSameTreeFromHead( otherCommit );
        }

        void SetBestTagCommit( TagCommit c )
        {
            Debug.Assert( c != null );
            _altBestTagCommit = _bestTagCommit ?? this;
            _bestTagCommit = c;
        }

        void AddSameTreeFromHead( TagCommit other )
        {
            Debug.Assert( _headSameTree == this && _bestTagCommit != null );
            if( other._headSameTree == null )
            {
                other._headSameTree = this;
                other._nextSameTree = _nextSameTree;
                _nextSameTree = other;
                Debug.Assert( other._bestTagCommit == null );
                if( _bestTagCommit._thisTag.CompareTo( other._thisTag ) < 0 ) SetBestTagCommit( other );
            }
            else
            {
                var firstOther = other._headSameTree;
                Debug.Assert( firstOther._bestTagCommit != null );
                if( _bestTagCommit._thisTag.CompareTo( firstOther._bestTagCommit._thisTag ) < 0 )
                {
                    SetBestTagCommit( firstOther );
                }
                var n = firstOther;
                for( ; ; )
                {
                    n._headSameTree = this;
                    if( n._nextSameTree == null ) break;
                    n = n._nextSameTree;
                }
                n._nextSameTree = _nextSameTree;
                _nextSameTree = firstOther;
            }
        }

        #endregion

        public override string ToString()
        {
            var s = _thisTag.ToString();
            if( _headSameTree != null
                && _headSameTree._bestTagCommit != null
                && _headSameTree._bestTagCommit != this )
            {
                s += " -> " + _headSameTree._bestTagCommit.ThisTag.ToString();
            }
            return s;
        }
    }

}

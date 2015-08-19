using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{
    class TagCommit : IComparable<ReleaseTagVersion>, IEquatable<ReleaseTagVersion>, IComparable<TagCommit>, IEquatable<TagCommit>
    {
        public readonly string CommitSha;
        public readonly string ContentSha;
        /// <summary>
        /// This is _thisTag or a better propagated tag from child with same content.
        /// </summary>
        ReleaseTagVersion _bestTag;
        ReleaseTagVersion _thisTag;
        List<ReleaseTagVersion> _extraCollectedTags;
        TagCommit _nextSameTree;
        TagCommit _headSameTree;

        public readonly Commit Commit;

        //// Live data, bound to a repository.
        //// To implement caching once...
        //class Transient
        //{
        //    public Repository Repo;
        //    public Commit Commit;
        //    public List<ReleaseTagVersion> ExtraTags;
        //}
        //Transient _runtime;

        public TagCommit( Commit c, ReleaseTagVersion first )
        {
            Debug.Assert( c != null );
            Commit = c;
            CommitSha = c.Sha;
            ContentSha = c.Tree.Sha;
            _thisTag = first;
        }

        /// <summary>
        /// Gets the valid tag, if any, directly associated to this <see cref="CommitSha"/>.
        /// </summary>
        public ReleaseTagVersion ThisTag
        {
            get { return _thisTag; }
        }

        /// <summary>
        /// Gets <see cref="ThisTag"/> or the propagated tag if any.
        /// </summary>
        public ReleaseTagVersion BestTag
        {
            get { return _bestTag; }
        }

        public bool IsBestTagPropagated
        {
            get { return _thisTag != _bestTag; }
        }

        public IEnumerable<TagCommit> GetSameContent( bool withThis )
        {
            if( withThis ) yield return this;
            var n = _headSameTree;
            while( n != null )
            {
                if( n != this ) yield return n;
                n = n._nextSameTree;
            }
        }

        public CommitBaseVersions BaseVersions { get; set; }

        #region Step 1: Collect

        public void AddCollectedTag( ReleaseTagVersion t )
        {
            Debug.Assert( t != null );
            if( t.Equals( _thisTag ) )
            {
                if( t.DefinitionStrength > _thisTag.DefinitionStrength ) _thisTag = t;
            }
            else
            {
                if( _extraCollectedTags == null ) _extraCollectedTags = new List<ReleaseTagVersion>();
                _extraCollectedTags.Add( t );
            }
        }

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
                }
                else otherCommit._headSameTree.AddSameTreeFromHead( this );
            }
            else _headSameTree.AddSameTreeFromHead( otherCommit );
        }

        void AddSameTreeFromHead( TagCommit other )
        {
            Debug.Assert( _headSameTree == this );
            if( other._headSameTree == null )
            {
                other._headSameTree = this;
                other._nextSameTree = _nextSameTree;
                _nextSameTree = other;
            }
            else
            {
                var firstOther = other._headSameTree;
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

        /// <summary>
        /// Computes the final release tag: +invalid hides any other version tags.
        /// If multiple versions exist on this commit, an error is raised.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>The collected <see cref="ThisTag"/>.</returns>
        public void CloseCollect( StringBuilder errors )
        {
            var t = DoCloseCollect( errors );
            if( t != null && t.IsValid ) _bestTag = _thisTag = t;
        }

        ReleaseTagVersion DoCloseCollect( StringBuilder errors )
        {
            if( _extraCollectedTags == null ) return _thisTag.IsMarkedInvalid ? null : _thisTag;
            if( !_thisTag.IsMarkedInvalid ) _extraCollectedTags.Add( _thisTag );
            var best = _extraCollectedTags.GroupBy( v => v )
                            .Select( g => g.MaxBy( v => v.DefinitionStrength ) )
                            .Where( v => !v.IsMarkedInvalid )
                            .ToList();
            if( best.Count == 0 ) return null;
            if( best.Count > 1 )
            {
                errors.AppendFormat( "Commit '{0}' has {1} different released version tags. Delete some of them or create a +invalid tag(s) if they are already pushed to a remote repository.", Commit.Sha, best.Count ).AppendLine();
                return null;
            }
            return best[0];
        }

        #endregion

        public bool AddPropagatedVersionFromChild( StringBuilder errors, TagCommit child, bool applicable )
        {
            Debug.Assert( child != null );
            ReleaseTagVersion tagChild = child.BestTag;
            Debug.Assert( tagChild != null && tagChild.IsValid );
            if( _bestTag != null )
            {
                int cmp = tagChild.CompareTo( _bestTag );
                if( cmp == 0 )
                {
                    // Same version from child. This is valid only if applicable is true.
                    if( !applicable )
                    {
                        errors.AppendLine( String.Format( "Invalid repository: version '{0}' on {1} is also on {2} but the content is not the same! Fix the repository by deleting one of the tag (or create a '+invalid' one if it is already pushed).",
                                                            _bestTag.ToString(), CommitSha, child.CommitSha ) );
                    }
                    return false;
                }
                //
                // A lower version comes from a child: 
                // - If this version is defined by a tag on this commit, this is an error.
                // - If this version comes from another propagation, this is not an error:
                //   This corresponds, for instance, to a commit on an intermediate branch that has been released as a 2.3.0-rc on a 'develop' branch and released as a 2.3.0 on a 'master' branch.
                //   
                //   In both case, we do not propagate anything. 
                if( cmp < 0 )
                {
                    if( _bestTag == _thisTag )
                    {
                        errors.AppendLine( String.Format( "Invalid repository: version '{0}' on '{1}' is greater than version '{2}' on '{3}'. Fix the repository by deleting one of the tag (or create a '+invalid' one if it is already pushed).",
                                                        _bestTag.ToString(), CommitSha, tagChild.ToString(), child.CommitSha ) );
                    }
                    return false;
                }
                // A greater version comes from a child.
                // We don't care here about the source of our version. It can be explicitely set or already propagated: we propagate if we can.
            }
            if( applicable )
            {
                _bestTag = tagChild;
                return true;
            }
            return false;
        }

        public override bool Equals( object obj )
        {
            Debug.Assert( obj is TagCommit, "Internal use only." );
            return Equals( (TagCommit)obj );
        }

        public bool Equals( TagCommit other )
        {
            if( other == null ) return false;
            if( _bestTag == null ) return other._bestTag == null;
            return _bestTag.Equals( other._bestTag );
        }

        public bool Equals( ReleaseTagVersion other )
        {
            if( _bestTag == null ) return other == null;
            return _bestTag.Equals( other );
        }

        public override int GetHashCode()
        {
            return _bestTag != null ? _bestTag.GetHashCode() : 0;
        }

        public int CompareTo( TagCommit other )
        {
            if( other == null ) return 1;
            if( _bestTag == null ) return other._bestTag == null ? 0 : -1;
            return _bestTag.CompareTo( other._bestTag );
        }

        public int CompareTo( ReleaseTagVersion t )
        {
            if( _bestTag == null ) return t == null ? 0 : -1;
            return _bestTag.CompareTo( t );
        }


    }

}

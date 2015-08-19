using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Captures version information for a commit.
    /// </summary>
    public class CommitVersions
    {
        readonly ReleaseTagVersion _tag;
        readonly string _commitSha;
        readonly CommitBaseVersions _baseVersions;

        internal CommitVersions( ReleaseTagVersion tag, string commitSha, CommitBaseVersions baseVersions )
        {
            Debug.Assert( commitSha != null && baseVersions != null );
            _tag = tag;
            _commitSha = commitSha;
            _baseVersions = baseVersions;
        }

        /// <summary>
        /// Gets the tag (if any) that is applied to the commit.
        /// </summary>
        public ReleaseTagVersion Tag { get { return _tag; } }

        /// <summary>
        /// Gets the commit sha.
        /// </summary>
        public string CommitSha { get { return _commitSha; } }

        /// <summary>
        /// Gets the version from parents and content.
        /// </summary>
        public CommitBaseVersions BaseVersions { get { return _baseVersions; } }

        /// <summary>
        /// Gets the tag (if any) that is applied to this commit or the best tag from the parents.
        /// </summary>
        public ReleaseTagVersion TagOrParentTag { get { return _tag ?? _baseVersions.ParentTag; } }

        /// <summary>
        /// Gets the commit's sha of <see cref="TagOrParentTag"/>.
        /// </summary>
        public string TagOrParentTagSha { get { return _tag != null ? _commitSha : _baseVersions.ParentTagSha; } }

        /// <summary>
        /// Gets the content tags of this commit or the content tags from parents.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> ContentOrParentContentTags 
        { 
            get { return _baseVersions.ContentTags.Count > 0 ? _baseVersions.ContentTags : _baseVersions.ParentContentTags; } 
        }

        /// <summary>
        /// Gets the number of commits betwwen this and the base released parent.
        /// </summary>
        public int DepthFromParent
        {
            get { return _tag != null ? 0 : _baseVersions.DepthFromParent; }
        }

    }

}

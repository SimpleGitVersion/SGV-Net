using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{

    /// <summary>
    /// Captures version information from a commit's parents and its content.
    /// </summary>
    public class CommitBaseVersions
    {
        /// <summary>
        /// Gets the greatest version from parents.
        /// </summary>
        public ReleaseTagVersion ParentTag { get; internal set; }
        
        /// <summary>
        /// Gets the commit Sha of the <see cref="ParentTag"/>.
        /// </summary>
        public string ParentTagSha { get; internal set; }
        
        /// <summary>
        /// Gets the versions that apply to the content of the commit. Never null.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> ContentTags { get; internal set; }

        /// <summary>
        /// Gets the <see cref="CommitVersions.ContentOrParentContentTags"/> of the parents.
        /// When there is no parent this is empty.
        /// </summary>
        public IReadOnlyList<ReleaseTagVersion> ParentContentTags { get; internal set; }

        /// <summary>
        /// Gets the number of commits to the <see cref="ParentTag"/>.
        /// </summary>
        public int DepthFromParent { get; internal set; }

    }

}

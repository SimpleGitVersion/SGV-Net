using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Defines the way the current commit on a branch is considered.
    /// </summary>
    public enum CIBranchVersionMode
    {
        /// <summary>
        /// The branch does not generate any version information.
        /// </summary>
        None,

        /// <summary>
        /// The version will be a 0.0.0--ci-BranchName-SortableUtcDateTime+PreviousReleaseVersion.
        /// </summary>
        ZeroTimed,

        /// <summary>
        /// The version will be based on the PreviousRelease.
        /// </summary>
        LastReleaseBased
    }
}

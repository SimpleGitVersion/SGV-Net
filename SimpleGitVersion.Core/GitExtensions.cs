using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{

    /// <summary>
    /// Git objects related extensions methods.
    /// </summary>
    public static class GitExtensions
    {
        /// <summary>
        /// Follows the targets of a Git <see cref="Tag"/>.
        /// </summary>
        /// <param name="tag">Starting tag.</param>
        /// <returns>The tagged object.</returns>
        public static GitObject ResolveTarget( this Tag tag )
        {
            GitObject target = tag.Target;
            while( target is TagAnnotation )
            {
                target = ((TagAnnotation)(target)).Target;
            }
            return target;
        }
    }
}

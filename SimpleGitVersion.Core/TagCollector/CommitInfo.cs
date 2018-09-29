using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public class CommitInfo
    {
        /// <summary>
        /// Gets this commit sha.
        /// </summary>
        public readonly string CommitSha;

        /// <summary>
        /// The basic commit info.
        /// Can be null if no version information can be found in the repository on or
        /// below this commit point.
        /// </summary>
        public readonly BasicCommitInfo BasicInfo;

        /// <summary>
        /// Gets the possible next versions based on on this commit.
        /// </summary>
        public readonly IReadOnlyList<CSVersion> NextPossibleVersions;

        /// <summary>
        /// Gets the possible versions on this commit regardless of the tag already set on it.
        /// </summary>
        public readonly IReadOnlyList<CSVersion> PossibleVersions;


        internal CommitInfo(
            string sha,
            BasicCommitInfo basic,
            IReadOnlyList<CSVersion> possibleVersions,
            IReadOnlyList<CSVersion> nextPossibleVersions)
        {
            CommitSha = sha;
            BasicInfo = basic;
            NextPossibleVersions = nextPossibleVersions;
            PossibleVersions = possibleVersions;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Describes how a tag on a commit point must be parsed. 
    /// </summary>
    enum ReleaseTagParsingMode
    {
        /// <summary>
        /// The tag is analysed without any attempt to detect whether it is malformed: it must be valid otherwise it is silently ignored.
        /// </summary>
        IgnoreMalformedTag,
        /// <summary>
        /// If the tag is malformed (<see cref="ReleaseTagVersion.IsValid"/> is false and <see cref="ReleaseTagVersion.IsMalformed"/> is true), an error is raised.
        /// </summary>
        RaiseErrorOnMalformedTag,
        /// <summary>
        /// Same as <see cref="RaiseErrorOnMalformedTag"/> with the addition that <see cref="ReleaseTagVersion.IsPreReleaseNameStandard"/> must be true.
        /// </summary>
        RaiseErrorOnMalformedTagAndNonStandardPreReleaseName
    }
}

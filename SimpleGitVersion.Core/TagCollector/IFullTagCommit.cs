using CSemVer;
using System;
using System.Collections.Generic;

namespace SimpleGitVersion
{
    /// <summary>
    /// Extends a <see cref="ITagCommit"/> with information related to the commit content.
    /// </summary>
    public interface IFullTagCommit : ITagCommit
    {
        /// <summary>
        /// Gets this commit content Sha.
        /// </summary>
        string ContentSha { get; }

        /// <summary>
        /// Gets the best commit.
        /// It is this <see cref="IFullTagCommit"/> if no better version exists on the content.
        /// </summary>
        IFullTagCommit BestCommit { get; }

        /// <summary>
        /// Gets the best commit tag for this commit, skipping the given version.
        /// Null if and only if <see cref="ITagCommit.ThisTag"/> is equal to <paramref name="v"/>
        /// and there is no better <see cref="GetContentTagCommits"/>.
        /// </summary>
        /// <param name="v">The version to ignore.</param>
        /// <returns>The best commit tag or null.</returns>
        ITagCommit GetBestCommitExcept( CSVersion v );

        /// <summary>
        /// Gets all <see cref="IFullTagCommit"/> with the same content.
        /// </summary>
        /// <param name="withThis">True to include this commit into the list.</param>
        /// <returns>A list of the commits with the same content.</returns>
        IEnumerable<IFullTagCommit> GetContentTagCommits( bool withThis );

        /// <summary>
        /// Gets whether the content of this commit is the same as other exitsting tags.
        /// </summary>
        bool HasContentTagCommits { get; }
    }
}

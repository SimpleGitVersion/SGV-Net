using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Describes a file that is modified compared to the committed version.
    /// </summary>
    public interface IWorkingFolderModifiedFile
    {
        /// <summary>
        /// Gets the full path of repository root (the folder that contains the .git folder).
        /// This ends with <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <value>The repository full path.</value>
        string RepositoryFullPath { get; }

        /// <summary>
        /// Gets the local path of the modified file, relative to the <see cref="RepositoryFullPath"/>.
        /// </summary>
        /// <value>The modified file path.</value>
        string Path { get; }

        /// <summary>
        /// Gets the full path of the modified file.
        /// </summary>
        /// <value>The modified file full path.</value>
        string FullPath { get; }

        /// <summary>
        /// Gets the size of the committed content. 
        /// </summary>
        /// <value>The size of the committed content.</value>
        long CommittedContentSize { get; }

        /// <summary>
        /// Gets the content of the committed file. 
        /// The stream should be disposed.
        /// </summary>
        /// <returns>An opened Stream.</returns>
        Stream GetCommittedContent();

        /// <summary>
        /// Gets the content of the committed file.
        /// </summary>
        /// <value>The committed file content.</value>
        string CommittedText { get; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleGitVersion
{
    /// <summary>
    /// Describes options for initializing <see cref="RepositoryInfo"/>.
    /// </summary>
    public class RepositoryInfoOptions
    {
        string _remoteName;

        /// <summary>
        /// Initializes a new <see cref="RepositoryInfoOptions"/>.
        /// </summary>
        public RepositoryInfoOptions()
        {
            IgnoreModifiedFiles = new HashSet<string>( PathComparer.Default );
        }

        /// <summary>
        /// Gets or sets the commit that will be analyzed.
        /// When null (the default) or empty, the <see cref="StartingBranchName"/> is used.
        /// This property must be used programmatically: it does not appear in the Xml file.
        /// </summary>
        public string StartingCommitSha { get; set; }

        /// <summary>
        /// Gets or sets the branch whose name will be analyzed. Applies only when <see cref="StartingCommitSha"/> is null or empty.
        /// When null (the default) or empty, the current head is used.
        /// This property must be used programmatically: it does not appear in the Xml file.
        /// </summary>
        public string StartingBranchName { get; set; }

        /// <summary>
        /// Gets or sets an enumerable of commits' sha with tags. Defaults to null.
        /// All commit sha MUST exist in the repository otherwise an error will be added to the error collector.
        /// If the key is "head" (instead of a SHA1) the tags are applied on the current head of the repository.
        /// These tags are applied as if they exist in the repository.
        /// This property must be used programmatically: it does not appear in the Xml file.
        /// </summary>
        /// <remarks>
        /// A dictionary of string to list of sting can be directly assigned to this property.
        /// </remarks>
        public IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> OverriddenTags { get; set; }

        /// <summary>
        /// Gets or sets a version from which CSemVer rules are enforced.
        /// When set, any version before this one are silently ignored.
        /// This is useful to accommodate an existing repository that did not use Simple Git Versioning by easily forgetting the past.
        /// </summary>
        public string StartingVersionForCSemVer { get; set; }

        /// <summary>
        /// Gets or sets the only major version that must be released.
        /// This is typically for LTS branches.
        /// Obviously defaults to null.
        /// </summary>
        public int? SingleMajor { get; set; }

        /// <summary>
        /// Gets or sets the whether patch only versions must be released.
        /// This is typically for LTS (fix only) branches. 
        /// </summary>
        public bool OnlyPatch { get; set; }

        /// <summary>
        /// Gets or sets branches informations.
        /// </summary>
        public IList<RepositoryInfoOptionsBranch> Branches { get; set; }

        /// <summary>
        /// Gets a set of paths for which local modifications are ignored.
        /// It is empty by default.
        /// </summary>
        public ISet<string> IgnoreModifiedFiles { get; private set; }

        /// <summary>
        /// Gets or sets a filter for modified file: when null, all <see cref="IWorkingFolderModifiedFile"/>
        /// are considered modified (as if this predicate always evaluates to false).
        /// This hook is called only if the file does not appear in <see cref="IgnoreModifiedFiles"/>.
        /// </summary>
        /// <value>The file filter.</value>
        public Func<IWorkingFolderModifiedFile, bool> IgnoreModifiedFilePredicate { get; set; }

        /// <summary>
        /// Gets or sets whether all modified files must be processed: when false (the default), as soon as a modified file 
        /// is not in the <see cref="IgnoreModifiedFiles"/> and <see cref="IgnoreModifiedFilePredicate"/> returned 
        /// false, the process stops.
        /// </summary>
        public bool IgnoreModifiedFileFullProcess { get; set; }

        /// <summary>
        /// Gets or sets the name of the remote repository that will be considered when
        /// working with branches. Defaults to "origin" (can never be null or empty).
        /// </summary>
        public string RemoteName
        {
            get { return string.IsNullOrWhiteSpace( _remoteName ) ? "origin" : _remoteName; }
            set { _remoteName = value; }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="RepositoryInfo.IsDirty"/> is ignored.
        /// This should be used only for debugging purposes.
        /// </summary>
        public bool IgnoreDirtyWorkingFolder { get; set; }

        /// <summary>
        /// Reads <see cref="RepositoryInfoOptions"/> from a xml file.
        /// </summary>
        /// <param name="existingFilePath">Path to a xml file.</param>
        /// <returns>Returns a configured <see cref="RepositoryInfoOptions"/>.</returns>
        public static RepositoryInfoOptions Read( string existingFilePath )
        {
            return Read( XDocument.Load( existingFilePath ).Root );
        }

        /// <summary>
        /// Gets this options as an Xml element.
        /// </summary>
        /// <returns>The XElement.</returns>
        public XElement ToXml()
        {
            return new XElement( SGVSchema.RepositoryInfo,
                                    IgnoreDirtyWorkingFolder 
                                        ? new XElement( SGVSchema.Debug, new XAttribute( SGVSchema.IgnoreDirtyWorkingFolder, "true" ) ) 
                                        : null,
                                    StartingVersionForCSemVer != null ? new XElement( SGVSchema.StartingVersionForCSemVer, StartingVersionForCSemVer ) : null,
                                    SingleMajor.HasValue
                                        ? new XElement( SGVSchema.SingleMajor, SingleMajor.Value.ToString() )
                                        : null,
                                    OnlyPatch
                                        ? new XElement( SGVSchema.OnlyPatch, "true" )
                                        : null,
                                    IgnoreModifiedFiles.Count > 0
                                        ? new XElement( SGVSchema.IgnoreModifiedFiles,
                                                            IgnoreModifiedFiles.Where( f=> !string.IsNullOrWhiteSpace(f) ).Select( f => new XElement( SGVSchema.Add, f ) ) )
                                        : null,
                                    Branches != null
                                        ? new XElement( SGVSchema.Branches,
                                                            Branches.Where( b => b != null ).Select( b => b.ToXml() ) )
                                        : null,
                                    RemoteName != "origin" ? new XElement( SGVSchema.RemoteName, RemoteName ) : null );
        }

        /// <summary>
        /// Reads <see cref="RepositoryInfoOptions"/> from a xml element.
        /// </summary>
        /// <param name="e">Xml element.</param>
        /// <returns>Returns a configured <see cref="RepositoryInfoOptions"/>.</returns>
        public static RepositoryInfoOptions Read( XElement e )
        {
            var info = new RepositoryInfoOptions();

            info.IgnoreDirtyWorkingFolder = (bool?)e.Element( SGVSchema.Debug )
                                                    ?.Attribute( SGVSchema.IgnoreDirtyWorkingFolder ) ?? false;

            info.StartingVersionForCSemVer = (string)e.Element( SGVSchema.StartingVersionForCSemVer );


            info.SingleMajor = (int?)e.Element( SGVSchema.SingleMajor );

            info.OnlyPatch = (bool?)e.Element( SGVSchema.OnlyPatch ) ?? false;

            info.Branches = e.Elements( SGVSchema.Branches )
                                    .Elements( SGVSchema.Branch )
                                    .Select( b => new RepositoryInfoOptionsBranch( b ) ).ToList();

            info.IgnoreModifiedFiles.UnionWith( e.Elements( SGVSchema.IgnoreModifiedFiles ).Elements( SGVSchema.Add ).Select( i => i.Value ) );

            info.RemoteName = (string)e.Element( SGVSchema.RemoteName );

            return info;
        }
    }
}

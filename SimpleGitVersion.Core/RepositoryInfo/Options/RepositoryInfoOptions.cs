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

        class PathComparer : IEqualityComparer<string>
        {
            public bool Equals( string x, string y )
            {
                return StringComparer.OrdinalIgnoreCase.Equals( Normalize( x ), Normalize( y ) );
            }

            public int GetHashCode( string path )
            {
                return Normalize( path ).GetHashCode();
            }

            static string Normalize( string path )
            {
                return Path.GetFullPath( path ).TrimEnd( Path.DirectorySeparatorChar );
            }
        }

        /// <summary>
        /// Initializes a new <see cref="RepositoryInfoOptions"/>.
        /// </summary>
        public RepositoryInfoOptions()
        {
            IgnoreModifiedFiles = new HashSet<string>( new PathComparer() );
        }

        /// <summary>
        /// Gets or sets the commit that will be analysed.
        /// When null (the default) or empty, the <see cref="StartingBranchName"/> is used.
        /// This property must be used programmatically: it does not appear in the Xml file.
        /// </summary>
        public string StartingCommitSha { get; set; }

        /// <summary>
        /// Gets or sets the branch whose name will be analysed. Applies only when <see cref="StartingCommitSha"/> is null or empty.
        /// When null (the default) or empty, the current head is used.
        /// This property must be used programmatically: it does not appear in the Xml file.
        /// </summary>
        public string StartingBranchName { get; set; }

        /// <summary>
        /// Gets or sets an enumerable of commits' sha with tags. Defaults to null.
        /// All commit sha MUST exist in the repository otherwise an error will be added to the error collector.
        /// If the key is "head" (instead of a SHA1) the tags are applied on the current head of the repository.
        /// These tags are applied as if they exist in the repository.
        /// </summary>
        /// <remarks>
        /// A dictionnary of string to list of sting can be directly assigned to this property.
        /// </remarks>
        public IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> OverridenTags { get; set; }

        /// <summary>
        /// Gets or sets a version from which CSemVer rules are enforced.
        /// When set, any version before this one are silently ignored.
        /// This is useful to accomodate an existing repository that did not use Simple Git Versionning by easily forgetting the past.
        /// </summary>
        public string StartingVersionForCSemVer { get; set; }

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
        /// Reads <see cref="RepositoryInfoOptions"/> from a xml file.
        /// </summary>
        /// <param name="existingFilePath">Path to a xml file.</param>
        /// <returns>Returns a configured <see cref="RepositoryInfoOptions"/>.</returns>
        public static RepositoryInfoOptions Read( string existingFilePath )
        {
            return Read( XDocument.Load( existingFilePath ).Root );
        }

        /// <summary>
        /// Reads <see cref="RepositoryInfoOptions"/> from a xml element.
        /// </summary>
        /// <param name="e">Xml element.</param>
        /// <returns>Returns a configured <see cref="RepositoryInfoOptions"/>.</returns>
        public static RepositoryInfoOptions Read( XElement e )
        {
            var info = new RepositoryInfoOptions();
            var eS = e.Element( SGVSchema.StartingVersionForCSemVer );
            if( eS != null ) info.StartingVersionForCSemVer = eS.Value;
            info.Branches = e.Elements( SGVSchema.Branches )
                                    .Elements( SGVSchema.Branch )
                                    .Select( b => new RepositoryInfoOptionsBranch( b ) ).ToList();
            info.IgnoreModifiedFiles.UnionWith( e.Elements( SGVSchema.IgnoreModifiedFiles ).Elements( SGVSchema.Add ).Select( i => i.Value ) );
            return info;
        }
    }
}

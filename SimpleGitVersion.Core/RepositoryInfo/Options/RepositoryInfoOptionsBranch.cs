using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleGitVersion
{
    /// <summary>
    /// Describes options related to a Git branch.
    /// </summary>
    public class RepositoryInfoOptionsBranch
    {
        /// <summary>
        /// Initializes a new default <see cref="RepositoryInfoOptionsBranch"/> object.
        /// </summary>
        public RepositoryInfoOptionsBranch()
        {
        }

        /// <summary>
        /// Initializes a new branch information from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public RepositoryInfoOptionsBranch( XElement e )
        {
            Name = (string)(e.Attribute( SGVSchema.Name ) ?? e.Attribute( SGVSchema.NameNoNS )); 
            VersionName = (string)(e.Attribute( SGVSchema.VersionName ) ?? e.Attribute( SGVSchema.VersionNameNoNS )); 
            var a = e.Attribute( SGVSchema.CIVersionMode ) ?? e.Attribute( SGVSchema.CIVersionModeNoNS );
            CIBranchVersionMode mode;
            if( a != null && Enum.TryParse<CIBranchVersionMode>( a.Value, true, out mode ) ) 
            {
                CIVersionMode = mode;
            }
        }

        /// <summary>
        /// Gets or sets the name of the branch.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets an optional name that will be used instead of <see cref="Name"/> in the version.
        /// </summary>
        public string VersionName { get; set; }

        /// <summary>
        /// Gets or sets the wanted behavior for this branch.
        /// </summary>
        public CIBranchVersionMode CIVersionMode { get; set; }
    }
}

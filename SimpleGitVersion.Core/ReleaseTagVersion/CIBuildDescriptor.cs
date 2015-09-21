using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{

    /// <summary>
    /// Encapsulates CSemVer-CI suffix formatting.
    /// </summary>
    public class CIBuildDescriptor
    {
        /// <summary>
        /// Defines the maximal build index.
        /// This is required to be able to pad it with a constant number of '0'.
        /// </summary>
        public const int MaxNuGetV2BuildIndex = 9999;

        int _buildIndex;

        /// <summary>
        /// Gets or sets the build index. Must be greater or equal to 0.
        /// To be valid for NuGetV2, it must not exceed <see cref="MaxNuGetV2BuildIndex"/>.
        /// </summary>
        public int BuildIndex 
        {
            get { return _buildIndex; } 
            set
            {
                if( _buildIndex < 0 ) throw new ArgumentException();
                _buildIndex = value;
            }
        }

        /// <summary>
        /// Gets or set the branch name to use.
        /// When null or empty, this descriptor is not applicable.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets whether this descriptor can be applied.
        /// </summary>
        public bool IsValid
        {
            get { return _buildIndex >= 0 && !string.IsNullOrWhiteSpace( BranchName ); }
        }

        /// <summary>
        /// Gets whether this descriptor can be applied for NuGetV2 special name case.
        /// </summary>
        public bool IsValidForNuGetV2
        {
            get { return IsValid && _buildIndex <= MaxNuGetV2BuildIndex && BranchName.Length <= 8; }
        }

        /// <summary>
        /// Overridden to return "ci-<see cref="BranchName"/>.<see cref="BuildIndex"/>" when <see cref="IsValid"/> is true,
        /// the empty string otherwise.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return IsValid ? string.Format( "ci-{0}.{1}", BranchName, BuildIndex ) : string.Empty;
        }

        /// <summary>
        /// When <see cref="IsValidForNuGetV2"/> is true, returns "<see cref="BranchName"/>-<see cref="BuildIndex"/>" where 
        /// the index is padded with 0, the empty string otherwise.
        /// </summary>
        /// <returns></returns>
        public string ToStringForNuGetV2()
        {
            Debug.Assert( MaxNuGetV2BuildIndex.ToString().Length == 4 );
            return IsValid ? string.Format( "{0}-{1:0000}", BranchName, BuildIndex ) : string.Empty;
        }

    }
}

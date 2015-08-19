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
        public const int MaxBuildIndex = 999999;

        int _buildIndex;

        /// <summary>
        /// Gets or sets the build index. Must be between 0 and <see cref="MaxBuildIndex"/> otherwise an <see cref="ArgumentException"/> is thrown.
        /// When 0, this descriptor is not applicable.
        /// </summary>
        public int BuildIndex 
        {
            get { return _buildIndex; } 
            set
            {
                if( _buildIndex < 0 || _buildIndex > MaxBuildIndex ) throw new ArgumentException();
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
        public bool IsApplicable
        {
            get { return BuildIndex > 0 && !String.IsNullOrWhiteSpace( BranchName ); }
        }

        /// <summary>
        /// Overridden to return "ci-<see cref="BranchName"/>.<see cref="BuildIndex"/>" when <see cref="IsApplicable"/> is true,
        /// the empty string otherwise.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return IsApplicable ? String.Format( "ci-{0}.{1}", BranchName, BuildIndex ) : String.Empty;
        }

        /// <summary>
        /// When <see cref="IsApplicable"/> is true, returns "ci-<see cref="BranchName"/><param name="nameNumberSeparator">(separator)</param><see cref="BuildIndex"/>" where 
        /// the index is padded with 0, the empty string otherwise.
        /// </summary>
        /// <returns></returns>
        public string ToStringPadded( char nameNumberSeparator )
        {
            Debug.Assert( MaxBuildIndex.ToString().Length == 6 );
            return IsApplicable ? String.Format( "ci-{0}{1}{2:000000}", BranchName, nameNumberSeparator, BuildIndex ) : String.Empty;
        }

    }
}

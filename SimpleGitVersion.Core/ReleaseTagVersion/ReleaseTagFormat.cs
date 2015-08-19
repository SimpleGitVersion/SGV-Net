using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Format description for <see cref="ReleaseTagVersion.ToString(ReleaseTagFormat,CIBuildDescriptor,bool)"/>.
    /// </summary>
    public enum ReleaseTagFormat
    {
        /// <summary>
        /// Normalized format is 'v' + <see cref="SemVerWithMarker"/>.
        /// This is the default.
        /// </summary>
        Normalized,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) and there is no build meata data.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVer,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) plus build meata data (+valid, +published or +invalid).
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVerWithMarker,

        /// <summary>
        /// The ordered version in dotted notation (1542.6548.777.8787) where each parts are between 0 and 65535.
        /// </summary>
        DottedOrderedVersion,

        /// <summary>
        /// NuGet version 2. If the <see cref="ReleaseTagVersion.IsMarkedInvalid"/> the "+invalid" build meta data is added.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        NugetPackageV2,

        /// <summary>
        /// NuGet format. Currently <see cref="NugetPackageV2"/>.
        /// </summary>
        NuGetPackage = NugetPackageV2,

        /// <summary>
        /// Default is <see cref="Normalized"/>.
        /// </summary>
        Default = Normalized
    }
}

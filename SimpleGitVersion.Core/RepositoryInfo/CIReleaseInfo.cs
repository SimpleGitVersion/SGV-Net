using CSemVer;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Encapsulates CI release information.
    /// Instances of this class are created internally if and only a CI build can 
    /// actually be done.
    /// </summary>
    public class CIReleaseInfo
    {
        CIReleaseInfo( CSVersion ciBaseTag, int ciBaseDepth, SVersion ciBuildVersion, SVersion ciBuildVersionNuGet )
        {
            BaseTag = ciBaseTag;
            Depth = ciBaseDepth;
            BuildVersion = ciBuildVersion;
            BuildVersionNuGet = ciBuildVersionNuGet;
        }

        /// <summary>
        /// The base <see cref="CSVersion"/> from which <see cref="BuildVersion"/> is built.
        /// It is either the the previous release or the <see cref="CSVersion.VeryFirstVersion"/>.
        /// </summary>
        public readonly CSVersion BaseTag;

        /// <summary>
        /// The greatest number of commits between the current commit and the deepest occurence 
        /// of <see cref="BaseTag"/>.
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// Never null: this is the CSemVer-CI version in <see cref="CSVersionFormat.Normalized"/> format.
        /// </summary>
        public readonly SVersion BuildVersion;

        /// <summary>
        /// Never null: this is the CSemVer-CI version in <see cref="CSVersionFormat.NuGetPackage"/> format.
        /// </summary>
        public readonly SVersion BuildVersionNuGet;

        internal static CIReleaseInfo Create(
            Commit commit,
            CIBranchVersionMode ciVersionMode,
            string ciBuildName,
            StringBuilder errors,
            BasicCommitInfo info )
        {
            var actualBaseTag = info?.MaxCommit.ThisTag;
            CSVersion ciBaseTag = actualBaseTag ?? CSVersion.VeryFirstVersion;
            SVersion ciBuildVersionNuGet = null, ciBuildVersion = null;

            // If there is no base release found, we fall back to ZeroTimedBased mode.
            if( ciVersionMode == CIBranchVersionMode.ZeroTimed || actualBaseTag == null )
            {
                DateTime timeRelease = commit.Committer.When.ToUniversalTime().UtcDateTime;
                string vS = CIBuildDescriptor.CreateSemVerZeroTimed( ciBuildName, timeRelease );
                string vN = CIBuildDescriptor.CreateShortFormZeroTimed( ciBuildName, timeRelease );
                if( actualBaseTag != null )
                {
                    string buildMetaData = "+v" + actualBaseTag;
                    vS += buildMetaData;
                    vN += buildMetaData;
                }
                ciBuildVersion = SVersion.Parse( vS );
                ciBuildVersionNuGet = SVersion.Parse( vN, false );
                return new CIReleaseInfo( ciBaseTag, 0, ciBuildVersion, ciBuildVersionNuGet );

            }
            Debug.Assert( ciVersionMode == CIBranchVersionMode.LastReleaseBased && actualBaseTag != null );
            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = ciBuildName, BuildIndex = info.BelowDepth };
            if( !ci.IsValidForShortForm )
            {
                errors.AppendLine( "Due to ShortForm (NuGet V2 compliance) limitation, the branch name must not be longer than 8 characters. " );
                errors.Append( "Adds a VersionName attribute to the branch element in RepositoryInfo.xml with a shorter name: " )
                      .AppendLine()
                      .Append( $@"<Branch Name=""{ci.BranchName}"" VersionName=""{ci.BranchName.Substring( 0, 8 )}"" ... />." )
                      .AppendLine();
            }
            else
            {
                ciBuildVersion = SVersion.Parse( actualBaseTag.ToString( CSVersionFormat.Normalized, ci ) );
                ciBuildVersionNuGet = SVersion.Parse( actualBaseTag.ToString( CSVersionFormat.NuGetPackage, ci ), false );
            }
            Debug.Assert( ciBuildVersion == null || errors.Length == 0 );
            return ciBuildVersion != null
                    ? new CIReleaseInfo( ciBaseTag, info.BelowDepth, ciBuildVersion, ciBuildVersionNuGet )
                    : null;
        }
    }

}


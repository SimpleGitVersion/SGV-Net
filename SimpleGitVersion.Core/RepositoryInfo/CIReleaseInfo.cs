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
    /// </summary>
    public class CIReleaseInfo
    {
        internal CIReleaseInfo( ReleaseTagVersion ciBaseTag, int ciBaseDepth, string ciBuildVersion, string ciBuildVersionNuGet )
        {
            BaseTag = ciBaseTag;
            Depth = ciBaseDepth;
            BuildVersion = ciBuildVersion;
            BuildVersionNuGet = ciBuildVersionNuGet;
        }

        /// <summary>
        /// The base <see cref="ReleaseTagVersion"/> from which <see cref="BuildVersion"/> is built.
        /// It is either the the previous release or the <see cref="ReleaseTagVersion.VeryFirstVersion"/>.
        /// Null if and only if CIBuildVersion is null.
        /// </summary>
        public readonly ReleaseTagVersion BaseTag;

        /// <summary>
        /// The greatest number of commits between the current commit and the deepest occurence 
        /// of <see cref="BaseTag"/>.
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// Not null only if we are on a branch that is enabled in <see cref="RepositoryInfoOptions.Branches"/> (either because it is the current branch 
        /// or <see cref="RepositoryInfoOptions.StartingBranchName"/> specifies it), the <see cref="RepositoryInfoOptions.StartingCommitSha"/> is null or 
        /// empty and there is no <see cref="RepositoryInfo.ValidReleaseTag"/> on the commit.
        /// The format is based on <see cref="ReleaseTagFormat.SemVer"/>
        /// </summary>
        public readonly string BuildVersion;

        /// <summary>
        /// Same as <see cref="BuildVersion"/> instead that the format is based on <see cref="ReleaseTagFormat.NuGetPackage"/>
        /// </summary>
        public readonly string BuildVersionNuGet;

        internal static CIReleaseInfo Create( Commit commit, CIBranchVersionMode ciVersionMode, string ciVersionName, StringBuilder errors, CommitVersionInfo info )
        {
            var actualBaseTag = info.PreviousMaxTag;
            ReleaseTagVersion ciBaseTag = actualBaseTag ?? ReleaseTagVersion.VeryFirstVersion;
            string ciBuildVersionNuGet = null, ciBuildVersion = null;

            // If there is no base release found, we fall back to ZeroTimedBased mode.
            if( ciVersionMode == CIBranchVersionMode.ZeroTimed || actualBaseTag == null )
            {
                var name = string.Format( "0.0.0--ci-{0}-{1:u}", ciVersionName, commit.Committer.When );
                string suffix = actualBaseTag != null ? '+' + actualBaseTag.ToString() : null;
                ciBuildVersion = ciBuildVersionNuGet = ciVersionName + suffix;
            }
            else
            {
                Debug.Assert( ciVersionMode == CIBranchVersionMode.LastReleaseBased && actualBaseTag != null );
                CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = ciVersionName, BuildIndex = info.PreviousMaxCommitDepth };
                if( !ci.IsValidForNuGetV2 )
                {
                    errors.AppendLine( "Due to NuGet V2 limitation, the branch name must not be longer than 8 characters. " );
                    errors.Append( "Adds a VersionName attribute to the branch element in RepositoryInfo.xml with a shorter name: " )
                            .AppendFormat( @"<Branch Name=""{0}"" VersionName=""{1}"" ... />.", ci.BranchName, ci.BranchName.Substring( 0, 8 ) )
                            .AppendLine();
                }
                else
                {
                    ciBuildVersion = actualBaseTag.ToString( ReleaseTagFormat.SemVer, ci );
                    ciBuildVersionNuGet = actualBaseTag.ToString( ReleaseTagFormat.NuGetPackage, ci );
                }
            }
            Debug.Assert( ciBuildVersion == null || errors.Length == 0 );
            return ciBuildVersion != null ? new CIReleaseInfo( ciBaseTag, info.PreviousMaxCommitDepth, ciBuildVersion, ciBuildVersionNuGet ) : null;
        }

    }

}

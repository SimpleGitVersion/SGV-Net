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

        internal static CIReleaseInfo Create( Commit commit, CIBranchVersionMode ciVersionMode, string ciBuildName, StringBuilder errors, CommitVersionInfo info )
        {
            var actualBaseTag = info.PreviousMaxTag;
            ReleaseTagVersion ciBaseTag = actualBaseTag ?? ReleaseTagVersion.VeryFirstVersion;
            string ciBuildVersionNuGet = null, ciBuildVersion = null;

            // If there is no base release found, we fall back to ZeroTimedBased mode.
            if( ciVersionMode == CIBranchVersionMode.ZeroTimed || actualBaseTag == null )
            {
                DateTime timeRelease = commit.Committer.When.ToUniversalTime().UtcDateTime;
                ciBuildVersion = CreateSemVerZeroTimed( ciBuildName, timeRelease, actualBaseTag?.ToString() );
                ciBuildVersionNuGet = CreateNuGetZeroTimed( ciBuildName, timeRelease );
            }
            else
            {
                Debug.Assert( ciVersionMode == CIBranchVersionMode.LastReleaseBased && actualBaseTag != null );
                CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = ciBuildName, BuildIndex = info.PreviousMaxCommitDepth };
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

        /// <summary>
        /// Creates the ZeroTimed NuGetV2 version string.
        /// </summary>
        /// <param name="ciBuildName">The BuildName string (typically "develop").</param>
        /// <param name="timeRelease">The utc date time of the release.</param>
        /// <returns>A NuGetV2 O.O.O-C version string.</returns>
        public static string CreateNuGetZeroTimed( string ciBuildName, DateTime timeRelease )
        {
            if( string.IsNullOrWhiteSpace( ciBuildName ) ) throw new ArgumentException( nameof( ciBuildName ) );
            DateTime baseTime = new DateTime( 2015, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            if( timeRelease < baseTime ) throw new ArgumentException( $"Must be at least {baseTime}.", nameof( timeRelease ) );
            string ciBuildVersionNuGet;
            TimeSpan delta200 = timeRelease - new DateTime( 2015, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            Debug.Assert( Math.Log( 1000 * 366 * 24 * 60 * (long)60, 62 ) < 7, "Using Base62: 1000 years in seconds on 7 chars!" );
            long second = (long)delta200.TotalSeconds;
            string b62 = ToBase62( second );
            string ver = new string( '0', 7 - b62.Length ) + b62;
            ciBuildVersionNuGet = string.Format( "0.0.0-C{0}-{1}", ciBuildName, ver );
            return ciBuildVersionNuGet;
        }

        /// <summary>
        /// Creates the ZeroTimed SemVer version string. The <paramref name="actualBaseTag"/>, if not null, is appended 
        /// as a suffix (Build metadata).
        /// </summary>
        /// <param name="ciBuildName">The BuildName string (typically "develop").</param>
        /// <param name="timeRelease">The utc date time of the release.</param>
        /// <param name="actualBaseTag">An optional base release that will be added as build metadata.</param>
        /// <returns>A SemVer O.O.O--ci version string.</returns>
        public static string CreateSemVerZeroTimed( string ciBuildName, DateTime timeRelease, string actualBaseTag = null )
        {
            if( string.IsNullOrWhiteSpace( ciBuildName ) ) throw new ArgumentException( nameof( ciBuildName ) );
            var name = string.Format( "0.0.0--ci-{0}.{1:yyyy-MM-ddTHH-mm-ss-ff}", ciBuildName, timeRelease );
            return name + (actualBaseTag != null ? '+' + actualBaseTag : null);
        }

        static string ToBase62( long number )
        {
            // Naïve implementation that does the job.
            var alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var n = number;
            long basis = 62;
            var ret = "";
            while( n > 0 )
            {
                long temp = n % basis;
                ret = alphabet[(int)temp] + ret;
                n = (n / basis);

            }
            return ret;
        }
    }

}


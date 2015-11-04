using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SimpleGitVersion
{
    /// <summary>
    /// This is the primary and most complex task. It extracts Git repository informations from the 
    /// current head and computes relevant version information.
    /// The Repository.xml file is used to configure the process.
    /// </summary>
    public class GetGitRepositoryInfoTask : Task 
    {
        /// <summary>
        /// Gets the solution directory: the one that contains the .git folder.
        /// </summary>
        [Output]
        public string GitSolutionDirectory { get; set; }

        /// <summary>
        /// Gets whether a release can be produced from the current commit point.
        /// It is either a release or a CI build (see <see cref="IsValidRelease"/> and <see cref="IsValidCIBuild"/>).
        /// </summary>
        [Output]
        public bool IsValid { get { return OrderedVersion != 0m; } }

        /// <summary>
        /// Gets whether this is a valid, normal, release (not a CI build).
        /// </summary>
        [Output]
        public bool IsValidRelease { get; set; }

        /// <summary>
        /// Gets whether this is a valid CI build.
        /// </summary>
        [Output]
        public bool IsValidCIBuild { get; set; }

        /// <summary>
        /// Gets the semantic version.
        /// When <see cref="IsValid"/> is false, it contains the error message (the first error line) so that
        /// any attempt to use this to actually package something will fail.
        /// </summary>
        [Output]
        public string SemVer { get; set; }

        /// <summary>
        /// Gets the NuGet version to use.
        /// When <see cref="IsValid"/> is false, it contains the error message (the first error line) so that
        /// any attempt to use this to actually package something will fail and this is used as the InformationalVersionInfo (Windows Product version) content
        /// so that it immediately appears that the assembly is not a good one to use.
        /// </summary>
        [Output]
        public string NuGetVersion { get; set; }

        /// <summary>
        /// Gets the original tag on the current commit point.
        /// When <see cref="IsValid"/> is false or if there is no tag (ie. we are on a CI build), it is null.
        /// </summary>
        [Output]
        public string OriginalTagText { get; set; }

        [Output]
        public int Major { get; set; }

        [Output]
        public int Minor { get; set; }

        [Output]
        public int Patch { get; set; }

        [Output]
        public string PreReleaseName { get; set; }

        [Output]
        public int PreReleaseNumber { get; set; }

        [Output]
        public int PreReleaseFix { get; set; }

        [Output]
        public string MajorMinor { get; set; }

        [Output]
        public string MajorMinorPatch { get; set; }

        [Output]
        public string FileVersion { get; set; }

        [Output]
        public long OrderedVersion { get; set; }

        [Output]
        public string CurrentUserName { get; set; }

        [Output]
        public string CommitSha { get; set; }

        [Output]
        public DateTime CommitDateUtc { get; set; }

        class Logger : ILogger
        {
            readonly ITask _task;

            public Logger( ITask t )
            {
                _task = t;
            }
            public void Error( string msg )
            {
                _task.LogError( msg );
            }

            public void Warn( string msg )
            {
                _task.LogWarning( msg );
            }

            public void Info( string msg )
            {
                _task.LogInfo( msg );
            }

            public void Trace( string msg )
            {
                _task.LogTrace( msg );
            }
        }

        /// <summary>
        /// Reads version information.
        /// On errors, they are logged but the return of this task is always true to avoid blocking the build process.
        /// </summary>
        /// <returns>Always true.</returns>
        public override bool Execute()
        {
            try
            {
                var i = SimpleRepositoryInfo.LoadFromPath( new Logger( this ), BuildEngine.ProjectFileOfTaskNode );
                GitSolutionDirectory = i.Info.GitSolutionDirectory;
                IsValidRelease = i.IsValidRelease;
                IsValidCIBuild = i.IsValidCIBuild;
                SemVer = i.SemVer;
                NuGetVersion = i.NuGetVersion;
                OriginalTagText = i.OriginalTagText;
                Major = i.Major;
                Minor = i.Minor;
                Patch = i.Patch;
                PreReleaseName = i.PreReleaseName;
                PreReleaseNumber = i.PreReleaseNumber;
                PreReleaseFix = i.PreReleaseFix;
                MajorMinor = i.MajorMinor;
                MajorMinorPatch = i.MajorMinorPatch;
                FileVersion = i.FileVersion;
                OrderedVersion = i.OrderedVersion;
                CurrentUserName = i.CurrentUserName;
                CommitSha = i.CommitSha;
                CommitDateUtc = i.CommitDateUtc;

                if( i.Info.HasError )
                {
                    this.LogError( i.Info.RepositoryError ?? i.Info.ReleaseTagErrorText );
                }

                return true;
            }
            catch( Exception exception )
            {
                this.LogError( "Error occurred: " + exception );
                return true;
            }
        }
    }
}
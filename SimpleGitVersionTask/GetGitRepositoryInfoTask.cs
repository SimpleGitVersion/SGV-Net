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
        [Required]
        public string SolutionDirectory { get; set; }

        /// <summary>
        /// Gets whether a release can be produced from the current commit point.
        /// It is either a release or a CI build (see <see cref="IsValidRelease"/> and <see cref="IsValidCIBuild"/>).
        /// </summary>
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

        [Output]
        public string SemVer { get; set; }

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
        public string DottedOrderedVersion { get; set; }

        [Output]
        public Decimal OrderedVersion { get; set; }

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

        public override bool Execute()
        {
            try
            {
                var i = SimpleRepositoryInfo.LoadFromPath( new Logger( this ), SolutionDirectory );
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
                DottedOrderedVersion = i.DottedOrderedVersion;
                OrderedVersion = i.OrderedVersion;
                CurrentUserName = i.CurrentUserName;
                CommitSha = i.CommitSha;
                CommitDateUtc = i.CommitDateUtc;

                return !i.Info.HasError;
            }
            catch( Exception exception )
            {
                this.LogError( "Error occurred: " + exception );
                return false;
            }
        }
    }
}
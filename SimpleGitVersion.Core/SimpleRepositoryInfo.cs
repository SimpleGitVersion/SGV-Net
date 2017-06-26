using CSemVer;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SimpleGitVersion
{
    /// <summary>
    /// Immutable object that exposes simplified information by wrapping a more complex <see cref="RepositoryInfo"/>.
    /// The <see cref="LoadFromPath"/> also handles the read of the RepositoryInfo.xml that may exist 
    /// at the root of the solution directory (the Repository.xml file creates a <see cref="RepositoryInfoOptions"/> that 
    /// configures the analysis).
    /// </summary>
    public sealed partial class SimpleRepositoryInfo
    {
        /// <summary>
        /// Gets the <see cref="RepositoryInfo"/> onto which this simplified representation is built.
        /// Never null.
        /// </summary>
        public RepositoryInfo Info { get; private set; }

        /// <summary>
        /// Gets whether a release can be produced from the current commit point.
        /// It is either a release or a CI build (see <see cref="IsValidRelease"/> and <see cref="IsValidCIBuild"/>).
        /// </summary>
        public bool IsValid
        {
            get
            {
                Debug.Assert( OrderedVersion >= 0m );
                Debug.Assert( OrderedVersion == 0m || (IsValidRelease || IsValidCIBuild), "IsValid (== OrderedVersion != 0) => IsValidRelease || IsValidCIBuild" );
                return OrderedVersion != 0m;
            }
        }

        /// <summary>
        /// Gets whether this is a valid, normal, release (not a CI build).
        /// </summary>
        public bool IsValidRelease { get; private set; }

        /// <summary>
        /// Gets whether this is a valid CI build.
        /// </summary>
        public bool IsValidCIBuild { get; private set; }

        /// <summary>
        /// Gets the major version.
        /// When <see cref="IsValid"/> is false, it is 0.
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// Gets the minor version.
        /// When <see cref="IsValid"/> is false, it is 0.
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// Gets the patch version.
        /// When <see cref="IsValid"/> is false, it is 0.
        /// </summary>
        public int Patch { get; private set; }

        /// <summary>
        /// Gets the standard pre release name among <see cref="CSVersion.StandardPreReleaseNames"/>.
        /// <see cref="string.Empty"/> when this is not a pre release version or <see cref="IsValid"/> is false.
        /// </summary>
        public string PreReleaseName { get; private set; }

        /// <summary>
        /// Gets the pre-release number (between 0 and 99).
        /// Meaningful only if <see cref="PreReleaseName"/> is not empty. Always 0 otherwise. 
        /// </summary>
        public int PreReleaseNumber { get; private set; }

        /// <summary>
        /// Gets the the pre-release fix number between 1 and 99. 
        /// When <see cref="IsValid"/> is false or if it is not a pre-release fix, it is 0. 
        /// </summary>
        public int PreReleaseFix { get; private set; }

        /// <summary>
        /// Gets the "<see cref="Major"/>.<see cref="Minor"/>" as a string: this is the component version (the AssemblyVersion).
        /// </summary>
        public string MajorMinor { get; private set; }

        /// <summary>
        /// Gets the "<see cref="Major"/>.<see cref="Minor"/>.<see cref="Patch"/>" as a string.
        /// </summary>
        public string MajorMinorPatch { get; private set; }

        /// <summary>
        /// Gets the 'Major.Minor.Build.Revision' windows file version to use.
        /// When <see cref="IsValid"/> is false, it is '0.0.0.0' (<see cref="InformationalVersion.ZeroFileVersion"/>).
        /// When it is a release the last part (Revision) is even and it is odd for CI builds. 
        /// </summary>
        public string FileVersion { get; private set; }

        /// <summary>
        /// Gets the ordered version.
        /// When <see cref="IsValid"/> it is greater than 0.
        /// </summary>
        public long OrderedVersion { get; private set; }

        /// <summary>
        /// Gets the current user name.
        /// </summary>
        public string CurrentUserName { get { return Info.CurrentUserName; } }

        /// <summary>
        /// Gets the Sha of the current commit.
        /// </summary>
        public string CommitSha { get; private set; }

        /// <summary>
        /// Gets the UTC date and time of the current commit.
        /// </summary>
        public DateTime CommitDateUtc { get; private set; }

        /// <summary>
        /// Gets the version in <see cref="CSVersionFormat.SemVer"/> format.
        /// When <see cref="IsValid"/> is false, it contains the error message (the first error line) so that
        /// any attempt to use this to actually package something will fail.
        /// </summary>
        public string SafeSemVersion { get; private set; }

        /// <summary>
        /// Gets the NuGet version to use.
        /// When <see cref="IsValid"/> is false, it contains the error message (the first error line) so that
        /// any attempt to use this to actually package something will fail.
        /// </summary>
        public string SafeNuGetVersion { get; private set; }

        /// <summary>
        /// Gets the original tag on the current commit point.
        /// When <see cref="IsValid"/> is false or if there is no tag (ie. we are on a CI build), it is null.
        /// </summary>
        public string OriginalTagText { get; private set; }

        /// <summary>
        /// Creates a new <see cref="SimpleRepositoryInfo"/> based on a path (that can be below the folder with the '.git' sub folder). 
        /// </summary>
        /// <param name="path">The path to lookup.</param>
        /// <param name="logger">Logger that will be used.</param>
        /// <param name="optionsChecker">
        /// Optional action that accepts the logger, a boolean that is true if a RepositoryInfo.xml has been 
        /// found, and the <see cref="RepositoryInfoOptions"/> that will be used.
        /// </param>
        /// <returns>An immutable SimpleRepositoryInfo instance.</returns>
        static public SimpleRepositoryInfo LoadFromPath( ILogger logger, string path, Action<ILogger,bool,RepositoryInfoOptions> optionsChecker = null )
        {
            if( logger == null ) throw new ArgumentNullException( nameof( logger ) ); 
            RepositoryInfo info = RepositoryInfo.LoadFromPath( path, gitPath =>
            {
                string optionFile = Path.Combine( gitPath, "RepositoryInfo.xml" );
                bool fileExists = File.Exists( optionFile );
                var options = fileExists ? RepositoryInfoOptions.Read( optionFile ) : new RepositoryInfoOptions();
                if( optionsChecker != null ) optionsChecker( logger, fileExists, options );
                return options;
            } );
            return new SimpleRepositoryInfo( logger, info );
        }

        /// <summary>
        /// Initializes a new <see cref="SimpleRepositoryInfo"/> based on a (more complex) <see cref="Info"/>.
        /// </summary>
        /// <param name="logger">Logger that will be used.</param>
        /// <param name="info">The simplified repository information.</param>
        public SimpleRepositoryInfo( ILogger logger, RepositoryInfo info )
        {
            if( logger == null ) throw new ArgumentNullException( nameof(logger) );
            if( info == null ) throw new ArgumentNullException( nameof( info ) );

            Info = info;
            if( !HandleRepositoryInfoError( logger, info ) )
            {
                CommitSha = info.CommitSha;
                CommitDateUtc = info.CommitDateUtc;
                var t = info.ValidReleaseTag;
                // Always warn on non standard pre release name.
                if( t != null && t.IsPreRelease && !t.IsPreReleaseNameStandard )
                {
                    logger.Warn( $"Non standard pre release name '{t.PreReleaseNameFromTag}' is mapped to '{t.PreReleaseName}'." );
                }
                if( info.IsDirty && !info.Options.IgnoreDirtyWorkingFolder )
                {
                    SetInvalidValuesAndLog( logger, "Working folder has non committed changes.", false );
                    logger.Info( info.IsDirtyExplanations );
                }
                else
                {
                    Debug.Assert( info.PossibleVersions != null );
                    if( info.IsDirty )
                    {
                        logger.Warn( "Working folder is Dirty! Checking this has been disabled since RepositoryInfoOptions.IgnoreDirtyWorkingFolder is true." );
                        logger.Warn( info.IsDirtyExplanations );
                    }
                    if( info.PreviousRelease != null )
                    {
                        logger.Trace( $"Previous release found '{info.PreviousRelease.ThisTag}' on commit '{info.PreviousRelease.CommitSha}'." );
                    }
                    if( info.PreviousMaxRelease != null && info.PreviousMaxRelease != info.PreviousRelease )
                    {
                        logger.Trace( $"Previous max release found '{info.PreviousMaxRelease.ThisTag}' on commit '{info.PreviousMaxRelease.CommitSha}'." );
                    }
                    if( info.PreviousRelease == null && info.PreviousMaxRelease == null )
                    {
                        logger.Trace( "No previous release found'." );
                    }

                    // Will be replaced by SetInvalidValuesAndLog if needed.
                    SafeNuGetVersion = info.FinalNuGetVersion.Text;
                    SafeSemVersion = info.FinalSemVersion.Text;

                    if( info.CIRelease != null )
                    {
                        IsValidCIBuild = true;
                        SetNumericalVersionValues( info.CIRelease.BaseTag, true );
                        logger.Info( $"CI release: '{SafeSemVersion}'." );
                        LogValidVersions( logger, info );
                    }
                    else
                    {
                        if( t == null )
                        {
                            SetInvalidValuesAndLog( logger, "No valid release tag.", false );
                            LogValidVersions( logger, info );
                        }
                        else
                        {
                            IsValidRelease = true;
                            OriginalTagText = t.OriginalParsedText;
                            SetNumericalVersionValues( t, false );
                            logger.Info( $"Release: '{SafeSemVersion}'." );
                        }
                    }
                }
            }
            MajorMinor = $"{Major}.{Minor}";
            MajorMinorPatch = $"{MajorMinor}.{Patch}";
        }

        void LogValidVersions( ILogger logger, RepositoryInfo info )
        {
            if( info.PossibleVersions.Count == 0 )
            {
                logger.Info( "No possible versions." );
            }
            else
            {
                if( info.Options.PossibleVersionsMode == PossibleVersionsMode.Restricted )
                {
                    logger.Info( $"Possible version(s) (Restricted): {string.Join( ", ", info.PossibleVersionsStrict )}" );
                }
                else
                {
                    logger.Info( $"Possible version(s) (AllSuccessors): {string.Join( ", ", info.PossibleVersions )}" );
                }
            }
        }

        void SetNumericalVersionValues( CSVersion t, bool isCIBuild )
        {
            Major = t.Major;
            Minor = t.Minor;
            Patch = t.Patch;
            PreReleaseName = t.PreReleaseName;
            PreReleaseNumber = t.PreReleaseNumber;
            PreReleaseFix = t.PreReleasePatch;
            FileVersion = t.ToStringFileVersion( isCIBuild );
            OrderedVersion = t.OrderedVersion;
        }

        bool HandleRepositoryInfoError( ILogger logger, RepositoryInfo info )
        {
            if( !info.HasError ) return false;
            string allText = info.RepositoryError ?? info.ReleaseTagErrorText;
            string oneLine = info.ErrorHeaderText;
            logger.Error( allText );
            SetInvalidValues( oneLine );
            return true;
        }

        void SetInvalidValuesAndLog( ILogger logger, string reason, bool isWarning )
        {
            if( isWarning ) logger.Warn( reason );
            else logger.Info( reason );
            if( !InvalidValuesAlreadySet ) SetInvalidValues( reason );
        }

        bool InvalidValuesAlreadySet { get { return FileVersion != null; } }

        void SetInvalidValues( string reason )
        {
            Major = 0;
            Minor = 0;
            Patch = 0;
            PreReleaseName = string.Empty;
            PreReleaseNumber = 0;
            PreReleaseFix = 0;
            FileVersion = InformationalVersion.ZeroFileVersion;
            OrderedVersion = 0;
            SafeNuGetVersion = reason;
            SafeSemVersion = reason;
        }
    }
}
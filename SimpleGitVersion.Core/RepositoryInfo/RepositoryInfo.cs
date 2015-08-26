using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using System.IO;

namespace SimpleGitVersion
{

    /// <summary>
    /// Immutable object that can be obtained by calling <see cref="RepositoryInfo.LoadFromPath(string, RepositoryInfoOptions)"/> 
    /// that describes the commit and all the CSemVer information.
    /// </summary>
    public partial class RepositoryInfo
    {
        /// <summary>
        /// Gets the solution directory: the one that contains the .git folder.
        /// Null only if <see cref="RepositoryError"/> is 'No Git repository.'.
        /// </summary>
        public readonly string GitSolutionDirectory;

        /// <summary>
        /// Gets the repository level error if any: it is one line of text or null ('No Git repository.' or 'Unitialized Git repository.').
        /// </summary>
        public readonly string RepositoryError;
        /// <summary>
        /// Gets the errors related to the release tags as a list of lines.
        /// Null if no errors.
        /// </summary>
        public readonly IReadOnlyList<string> ReleaseTagErrorLines;
        /// <summary>
        /// Gets the errors related to the release tags as a text.
        /// Null if no errors.
        /// </summary>
        public readonly string ReleaseTagErrorText;

        /// <summary>
        /// Gets a one line error text if <see cref="HasError"/> is true. Null otherwise.
        /// </summary>
        public string ErrorHeaderText { get { return RepositoryError ?? (ReleaseTagErrorLines != null ? ReleaseTagErrorLines[0] : null); } }

        /// <summary>
        /// Gets a one line error text if <see cref="HasError"/> is true. Null otherwise.
        /// </summary>
        public bool HasError { get { return RepositoryError != null || ReleaseTagErrorText != null; } }

        /// <summary>
        /// Gets whether there are non committed files in the working directory.
        /// </summary>
        public readonly bool IsDirty;
        /// <summary>
        /// Gets the release tag. If there is error, this is null.
        /// It is also null if there is actually no release tag on the current commit.
        /// </summary>
        public readonly ReleaseTagVersion ValidReleaseTag;

        /// <summary>
        /// Gets whether the error is the fact that the release tag on the current commit point
        /// is not one of the <see cref="PossibleVersions"/>. An error that describes this appears 
        /// in <see cref="ReleaseTagErrorLines"/> and <see cref="ReleaseTagErrorText"/>
        /// </summary>
        public readonly bool ReleaseTagIsNotPossibleError;

        /// <summary>
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// Can also be null if there is simply no previous release: the <see cref="PossibleVersions"/> are then <see cref="ReleaseTagVersion.FirstPossibleVersions"/>.
        /// </summary>
        public readonly ReleaseTagVersion PreviousRelease;

        /// <summary>
        /// If there is a <see cref="PreviousRelease"/>, this is the Sha of the corresponding commit.
        /// </summary>
        public readonly string PreviousReleaseCommitSha;

        /// <summary>
        /// Gets the existing versions in the repository in ascending order.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> ExistingVersions;

        /// <summary>
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// When empty, this means that there can not be a valid release tag on the current commit point based on the <see cref="PreviousRelease"/>.
        /// However, <see cref="PossibleVersionsFromContent"/> may be non empty.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> PossibleVersions;

        /// <summary>
        /// Possible versions computed from the commit content.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> PossibleVersionsFromContent;

        /// <summary>
        /// All possible versions is given by the union of <see cref="PossibleVersions"/> and <see cref="PossibleVersionsFromContent"/>.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> AllPossibleVersions;

        /// <summary>
        /// Not null only if we are on a branch that is enabled in <see cref="RepositoryInfoOptions.Branches"/> (either because it is the current branch 
        /// or <see cref="RepositoryInfoOptions.StartingBranchName"/> specifies it), the <see cref="RepositoryInfoOptions.StartingCommitSha"/> is null or 
        /// empty and there is no <see cref="ValidReleaseTag"/> on the commit.
        /// The format is based on <see cref="ReleaseTagFormat.SemVer"/>
        /// </summary>
        public readonly string CIBuildVersion;

        /// <summary>
        /// Same as <see cref="CIBuildVersion"/> instead that the format is based on <see cref="ReleaseTagFormat.NuGetPackage"/>
        /// </summary>
        public readonly string CIBuildVersionNuGet;

        /// <summary>
        /// The base <see cref="ReleaseTagVersion"/> from which <see cref="CIBuildVersion"/> is built.
        /// It is either <see cref="PreviousRelease"/> or <see cref="ReleaseTagVersion.VeryFirstVersion"/>.
        /// Null if and only if CIBuildVersion is null.
        /// </summary>
        public readonly ReleaseTagVersion CIBaseTag;

        /// <summary>
        /// Gets the NuGet version that must be used.
        /// Null if for any reason, no version can be generated.
        /// </summary>
        public string FinalNuGetVersion
        {
            get { return CIBuildVersionNuGet ?? (ValidReleaseTag != null ? ValidReleaseTag.ToString( ReleaseTagFormat.NuGetPackage ) : null); }
        }

        /// <summary>
        /// Gets the <see cref="RepositoryInfoOptions"/> that has been used.
        /// </summary>
        public readonly RepositoryInfoOptions Options;

        /// <summary>
        /// The UTC date and time of the commit.
        /// </summary>
        public readonly DateTime CommitDateUtc;

        /// <summary>
        /// The Sha of the commit.
        /// </summary>
        public readonly string CommitSha;

        /// <summary>
        /// The current user name.
        /// </summary>
        public readonly string CurrentUserName;

        RepositoryInfo()
        {
            CurrentUserName = string.IsNullOrWhiteSpace( Environment.UserDomainName )
                           ? Environment.UserName
                           : string.Format( @"{0}\{1}", Environment.UserDomainName, Environment.UserName );
        }

        RepositoryInfo( Repository r, RepositoryInfoOptions options, string gitSolutionDir )
            : this()
        {
            if( options == null ) options = new RepositoryInfoOptions();
            Options = options;
            if( r == null ) RepositoryError = "No Git repository.";
            else
            {
                Debug.Assert( gitSolutionDir != null );
                GitSolutionDirectory = gitSolutionDir;
                Commit commit;
                CIBranchVersionMode ciVersionMode;
                string ciVersionName;
                RepositoryError = TryFindCommit( options, r, out commit, out ciVersionMode, out ciVersionName );
                Debug.Assert( (ciVersionMode != CIBranchVersionMode.None) == (ciVersionName != null) );
                if( commit != null )
                {
                    CommitSha = commit.Sha;
                    CommitDateUtc = commit.Author.When.ToUniversalTime().DateTime;
                    RepositoryStatus repositoryStatus = r.RetrieveStatus();
                    IsDirty = ComputeIsDirty( repositoryStatus, options );
                    if( !IsDirty || options.IgnoreDirtyWorkingFolder )
                    {
                        StringBuilder errors = new StringBuilder();
                        TagCollector collector = new TagCollector(  errors, 
                                                                    r, 
                                                                    options.StartingVersionForCSemVer, 
                                                                    c => c.Sha == CommitSha ? ReleaseTagParsingMode.RaiseErrorOnMalformedTag : ReleaseTagParsingMode.IgnoreMalformedTag, 
                                                                    options.OverridenTags );
                        if( errors.Length == 0 )
                        {
                            CommitVersions cv = collector.GetVersions( commit );
                            PreviousRelease = cv.BaseVersions.ParentTag;
                            PreviousReleaseCommitSha = cv.BaseVersions.ParentTagSha;
                            ExistingVersions = collector.ExistingVersions.ToArray();
                            if( errors.Length == 0 )
                            {
                                #region Computes PossibleVersions and ValidReleaseTag
                                // Special case: we are on the StartingVersionForCSemVer.
                                if( collector.StartingVersionForCSemVer != null && collector.StartingVersionForCSemVer.Equals( cv.Tag ) )
                                {
                                    Debug.Assert( PreviousRelease == null );
                                    AllPossibleVersions = PossibleVersions = new ReleaseTagVersion[] { ValidReleaseTag = cv.Tag };
                                    PossibleVersionsFromContent = ReleaseTagVersion.EmptyArray;
                                }
                                else
                                {
                                    // Gets the successors of the previous release.
                                    IEnumerable<ReleaseTagVersion> successors = PreviousRelease != null ? PreviousRelease.GetDirectSuccessors( false ) : ReleaseTagVersion.FirstPossibleVersions;
                                    // Possible versions are the successors computed above from which we must remove all versions greater than 
                                    // the smallest existing version greater than the PreviousRelease (or this tag if we are on a tag).
                                    // If there is no ExistingVersions, we have nothing to remove.
                                    // And if there is no PreviousRelease, it is simply the smallest existing version.
                                    if( ExistingVersions.Count > 0 )
                                    {
                                        int idx = 0;
                                        if( PreviousRelease != null )
                                        {
                                            idx = ExistingVersions.BinarySearch( cv.TagOrParentTag );
                                            Debug.Assert( idx >= 0 );
                                            ++idx;
                                        }
                                        else if( cv.Tag != null && ExistingVersions[idx].Equals( cv.Tag ) ) idx = 1;
                                        if( idx < ExistingVersions.Count ) successors = successors.Where( s => s.CompareTo( ExistingVersions[idx] ) < 0 );
                                    }
                                    // Always removes proposals that are lower than the StartingVersionForCSemVer if it exists.
                                    if( collector.StartingVersionForCSemVer != null )
                                    {
                                        successors = successors.Where( s => s.CompareTo( collector.StartingVersionForCSemVer ) >= 0 );
                                    }
                                    PossibleVersions = successors.ToArray();
                                    // Now... "Save the Cherry Pick" operation!
                                    PossibleVersionsFromContent = ComputePossibleVersionsFromContent( cv, collector.StartingVersionForCSemVer );
                                    AllPossibleVersions = PossibleVersions.Concat( PossibleVersionsFromContent ).OrderBy( t => t ).ToArray();
                                    if( cv.Tag == null )
                                    {
                                        // There is no release tag on the current commit.
                                        if( ciVersionName != null )
                                        {
                                            CIBaseTag = PreviousRelease ?? ReleaseTagVersion.VeryFirstVersion;
                                            // If there is no previous release, we fall back to ZeroTimed mode.
                                            if( ciVersionMode == CIBranchVersionMode.ZeroTimed || PreviousRelease == null )
                                            {
                                                var name = string.Format( "0.0.0--ci-{0}-{1:u}", ciVersionName, commit.Committer.When );
                                                string suffix = PreviousRelease != null ? '+' + PreviousRelease.ToString() : null;
                                                CIBuildVersionNuGet = CIBuildVersion = ciVersionName + suffix;
                                            }
                                            else
                                            {
                                                Debug.Assert( ciVersionMode == CIBranchVersionMode.LastReleaseBased && PreviousRelease != null );
                                                CIBuildDescriptor ci = new CIBuildDescriptor{ BranchName = ciVersionName, BuildIndex = cv.DepthFromParent };
                                                CIBuildVersion = PreviousRelease.ToString( ReleaseTagFormat.SemVer, ci );
                                                CIBuildVersionNuGet = PreviousRelease.ToString( ReleaseTagFormat.NuGetPackage, ci );
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // There is a release tag on the current commit.
                                        if( AllPossibleVersions.Contains( cv.Tag ) )
                                        {
                                            ValidReleaseTag = cv.Tag;
                                        }
                                        else
                                        {
                                            ReleaseTagIsNotPossibleError = true;
                                            errors.Append( "Release tag '" )
                                                    .Append( cv.Tag.OriginalTagText )
                                                    .Append( "' is not valid here. Possible tags are: " )
                                                    .Append( string.Join( ", ", PossibleVersions ) );
                                            if( PossibleVersionsFromContent.Count > 0 )
                                            {
                                                errors.Append( " or (from content): '" )
                                                    .Append( string.Join( ", ", PossibleVersionsFromContent ) );
                                            }
                                            errors.AppendLine();
                                        }
                                    }
                                }
                                #endregion Computes PossibleVersions and ValidReleaseTag
                            }
                        }
                        if( errors.Length > 0 ) SetError( errors, out ReleaseTagErrorLines, out ReleaseTagErrorText );
                    } 
                }
            }
        }

        bool ComputeIsDirty( RepositoryStatus repositoryStatus, RepositoryInfoOptions options )
        {
            if( repositoryStatus.Added.Any()
                || repositoryStatus.Missing.Any()
                || repositoryStatus.Removed.Any()
                || repositoryStatus.Staged.Any() ) return true;
            foreach( var m in repositoryStatus.Modified )
            {
                if( !options.IgnoreModifiedFiles.Contains( m.FilePath  ) ) return true;
            }
            return false;
        }

        IReadOnlyList<ReleaseTagVersion> ComputePossibleVersionsFromContent( CommitVersions cv, ReleaseTagVersion startingVersionForCSemVer )
        {
            HashSet<ReleaseTagVersion> result = new HashSet<ReleaseTagVersion>();
            foreach( var t in cv.BaseVersions.ContentTags.Concat( cv.BaseVersions.ParentContentTags ) )
            {
                if( t.Equals( cv.TagOrParentTag ) ) continue;
                var succ = t.GetDirectSuccessors( false );
                int idx = ExistingVersions.BinarySearch( t );
                Debug.Assert( idx >= 0 );
                ++idx;
                if( idx < ExistingVersions.Count ) succ = succ.Where( s => s.CompareTo( ExistingVersions[idx] ) < 0 );
                result.UnionWith( succ );
            }
            result.ExceptWith( PossibleVersions );
            // Always removes proposals that are lower than the StartingVersionForCSemVer if it exists.
            if( startingVersionForCSemVer != null )
            {
                result.RemoveWhere( s => s.CompareTo( startingVersionForCSemVer ) < 0 );
            }
            return result.OrderBy( t => t ).ToArray();
        }

        string TryFindCommit( RepositoryInfoOptions options, Repository r, out Commit commit, out CIBranchVersionMode ciVersionMode, out string branchNameForCIVersion )
        {
            ciVersionMode = CIBranchVersionMode.None;
            commit = null;
            branchNameForCIVersion = null;
            string commitSha = options.StartingCommitSha;

            // Find current commit (the head) if none is provided.
            if( string.IsNullOrWhiteSpace( commitSha ) )
            {
                string branchName;
                if( string.IsNullOrWhiteSpace( options.StartingBranchName ) )
                {
                    commit = r.Head.Tip;
                    if( commit == null ) return "Unitialized Git repository.";
                    branchName = r.Head.Name;
                }
                else
                {
                    Branch br = r.Branches[options.StartingBranchName] ?? r.Branches[ options.RemoteName + '/' + options.StartingBranchName];
                    if( br == null ) return string.Format( "Unknown StartingBranchName: '{0}' (also tested on remote '{1}/{0}').", options.StartingBranchName, options.RemoteName );
                    commit = br.Tip;
                    branchName = options.StartingBranchName;
                }
                RepositoryInfoOptionsBranch bOpt;
                if( options.Branches != null
                    && (bOpt = options.Branches.FirstOrDefault( b => b.Name == branchName )) != null
                    && bOpt.CIVersionMode != CIBranchVersionMode.None )
                {
                    ciVersionMode = bOpt.CIVersionMode;
                    branchNameForCIVersion = string.IsNullOrWhiteSpace( bOpt.VersionName ) ? bOpt.Name : bOpt.VersionName;
                }
            }
            else
            {
                commit = r.Lookup<Commit>( commitSha );
                if( commit == null ) return string.Format( "Commit '{0}' not found.", commitSha );
            }
            return null;
        }

        private static void SetError( StringBuilder errors, out IReadOnlyList<string> lines, out string text )
        {
            Debug.Assert( errors.Length > 0 );
            text = errors.ToString();
            lines = text.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
        }

        /// <summary>
        /// Creates a new <see cref="RepositoryInfo"/> based on a path (that can be below the folder with the '.git' sub folder). 
        /// </summary>
        /// <param name="path">The path to lookup.</param>
        /// <param name="options">Optional <see cref="RepositoryInfoOptions"/>.</param>
        /// <returns>An immutable RepositoryInfo instance. Never null.</returns>
        public static RepositoryInfo LoadFromPath( string path, RepositoryInfoOptions options = null )
        {
            if( path == null ) throw new ArgumentNullException( nameof( path ) );
            using( var repo = GitHelper.LoadFromPath( path ) )
            {
                return new RepositoryInfo( repo, options, repo != null ? repo.Info.WorkingDirectory : null );
            }
        }

        /// <summary>
        /// Creates a new <see cref="RepositoryInfo"/> based on a path (that can be below the folder with the '.git' sub folder)
        /// and a function that can create a <see cref="RepositoryInfoOptions"/> from the actual Git repository path. 
        /// </summary>
        /// <param name="path">The path to lookup.</param>
        /// <param name="optionsBuilder">Function that can create a <see cref="RepositoryInfoOptions"/> from the Git working directory (the Solution folder).</param>
        /// <returns>An immutable RepositoryInfo instance. Never null.</returns>
        public static RepositoryInfo LoadFromPath( string path, Func<string,RepositoryInfoOptions> optionsBuilder )
        {
            if( path == null ) throw new ArgumentNullException( nameof( path ) );
            if( optionsBuilder == null ) throw new ArgumentNullException( nameof( optionsBuilder ) );

            using( var repo = GitHelper.LoadFromPath( path ) )
            {
                if( repo == null ) return new RepositoryInfo( null, null, null );
                return new RepositoryInfo( repo, optionsBuilder( repo.Info.WorkingDirectory ), repo.Info.WorkingDirectory );
            }
        }

    }
}

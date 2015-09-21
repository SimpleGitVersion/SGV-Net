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
        /// Can also be null if there is simply no previous release: the <see cref="PossibleVersions"/> are then based on <see cref="ReleaseTagVersion.FirstPossibleVersions"/>.
        /// </summary>
        public readonly ITagCommit PreviousRelease;

        /// <summary>
        /// Gets the existing versions in the repository in ascending order.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly IReadOnlyList<ITagCommit> ExistingVersions;

        /// <summary>
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// When empty, this means that there can not be a valid release tag on the current commit point.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> PossibleVersions;

        /// <summary>
        /// Among possible versions this gives the versions that are valid regarding the whole repository.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly IReadOnlyList<ReleaseTagVersion> ValidVersions;

        /// <summary>
        /// Gets CI informations if a CI release must be done.
        /// Not null only if we are on a branch that is enabled in <see cref="RepositoryInfoOptions.Branches"/> (either 
        /// because it is the current branch or <see cref="RepositoryInfoOptions.StartingBranchName"/> specifies it), the <see cref="RepositoryInfoOptions.StartingCommitSha"/> 
        /// is null or empty and there is no <see cref="ValidReleaseTag"/> on the commit.
        public readonly CIReleaseInfo CIRelease;

        /// <summary>
        /// Gets the NuGet version that must be used.
        /// Null if for any reason, no version can be generated.
        /// </summary>
        public string FinalNuGetVersion
        {
            get { return CIRelease != null ? CIRelease.BuildVersionNuGet : (ValidReleaseTag != null ? ValidReleaseTag.ToString( ReleaseTagFormat.NuGetPackage ) : null); }
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
                        TagCollector collector = new TagCollector( errors,
                                                                    r,
                                                                    options.StartingVersionForCSemVer,
                                                                    c => c.Sha == CommitSha ? ReleaseTagParsingMode.RaiseErrorOnMalformedTag : ReleaseTagParsingMode.IgnoreMalformedTag,
                                                                    options.OverridenTags );
                        if( errors.Length == 0 )
                        {
                            CommitVersionInfo info = collector.GetVersionInfo( commit );
                            ExistingVersions = collector.ExistingVersions.TagCommits;
                            PossibleVersions = info.PossibleVersions.ToArray();
                            ValidVersions = info.ValidVersions;
                            if( info.PreviousTag != null ) PreviousRelease = info.PreviousCommit.ThisCommit;
                            if( info.ThisCommit != null )
                            {
                                if( ValidVersions.Contains( info.ThisCommit.ThisTag ) )
                                {
                                    ValidReleaseTag = info.ThisCommit.ThisTag;
                                }
                                else
                                {
                                    ReleaseTagIsNotPossibleError = true;
                                    errors.Append( "Release tag '" )
                                           .Append( info.ThisCommit.ThisTag.OriginalTagText )
                                           .Append( "' is not valid here. Valid tags are: " )
                                           .Append( string.Join( ", ", ValidVersions ) )
                                           .AppendLine();
                                }
                            }
                            else
                            {
                                if( ciVersionName != null )
                                {
                                    CIRelease = CIReleaseInfo.Create( commit, ciVersionMode, ciVersionName, errors, info );
                                }
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

        string TryFindCommit( RepositoryInfoOptions options, Repository r, out Commit commit, out CIBranchVersionMode ciVersionMode, out string branchNameForCIVersion )
        {
            ciVersionMode = CIBranchVersionMode.None;
            commit = null;
            branchNameForCIVersion = null;
            string commitSha = options.StartingCommitSha;

            // Find current commit (the head) if none is provided.
            if( string.IsNullOrWhiteSpace( commitSha ) )
            {
                IEnumerable<string> branchNames;
                if( string.IsNullOrWhiteSpace( options.StartingBranchName ) )
                {
                    // locCommit is here because one cannot use an out parameter inside a lambda.
                    var locCommit = commit = r.Head.Tip;
                    if( locCommit == null ) return "Unitialized Git repository.";
                    // Save the branches!
                    // By doing this, when we are in 'Detached Head' state (the head of the repository is on a commit and not on a branch: git checkout <sha>),
                    // we can detect that it is the head of a branch and hence apply possible options (mainly CI) for it.
                    // We take into account only the branches from options.RemoteName remote here.
                    string branchName = r.Head.Name;
                    if( branchName == "(no branch)" )
                    {
                        string remotePrefix = options.RemoteName + '/';
                        branchNames = r.Branches
                                        .Where( b => b.Tip == locCommit && (!b.IsRemote || b.Name.StartsWith( remotePrefix )) )
                                        .Select( b => b.IsRemote ? b.Name.Substring( remotePrefix.Length ) : b.Name );
                    }
                    else branchNames = new[] { branchName };
                }
                else
                {
                    Branch br = r.Branches[options.StartingBranchName] ?? r.Branches[ options.RemoteName + '/' + options.StartingBranchName];
                    if( br == null ) return string.Format( "Unknown StartingBranchName: '{0}' (also tested on remote '{1}/{0}').", options.StartingBranchName, options.RemoteName );
                    commit = br.Tip;
                    branchNames = new[] { options.StartingBranchName };
                }
                RepositoryInfoOptionsBranch bOpt;
                if( options.Branches != null
                    && (bOpt = options.Branches.FirstOrDefault( b => branchNames.Contains( b.Name ) )) != null
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

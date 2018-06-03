using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using System.IO;
using CSemVer;

namespace SimpleGitVersion
{

    /// <summary>
    /// Immutable object that describes the commit and all the CSemVer information.
    /// It can be obtained by calling static helper <see cref="LoadFromPath(string, RepositoryInfoOptions)"/>
    /// (a <see cref="Repository"/> is created and disposed) or by using its constructor.
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>
        /// Gets the solution directory: the one that contains the .git folder.
        /// Null only if <see cref="RepositoryError"/> is 'No Git repository.'.
        /// It ends with the <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <remarks>
        /// This captures the <see cref="RepositoryInformation.WorkingDirectory"/>.
        /// </remarks>
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
        public string ErrorHeaderText => RepositoryError ?? (ReleaseTagErrorLines != null ? ReleaseTagErrorLines[0] : null); 

        /// <summary>
        /// Gets whether an error occurred.
        /// </summary>
        public bool HasError => RepositoryError != null || ReleaseTagErrorText != null;

        /// <summary>
        /// Gets whether there are non committed files in the working directory.
        /// </summary>
        public bool IsDirty => IsDirtyExplanations != null;

        /// <summary>
        /// Gets detailed explanations about <see cref="IsDirty"/>.
        /// </summary>
        public readonly string IsDirtyExplanations;

        /// <summary>
        /// Gets the release tag. If there is error, this is null.
        /// It is also null if there is actually no release tag on the current commit.
        /// </summary>
        public readonly CSVersion ValidReleaseTag;

        /// <summary>
        /// Gets whether the error is the fact that the release tag on the current commit point
        /// is not one of the <see cref="PossibleVersions"/>. An error that describes this appears 
        /// in <see cref="ReleaseTagErrorLines"/> and <see cref="ReleaseTagErrorText"/>
        /// </summary>
        public readonly bool ReleaseTagIsNotPossibleError;

        /// <summary>
        /// Gets the <see cref="CommitVersionInfo"/> of the current commit point.
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// </summary>
        public readonly CommitVersionInfo CommitVersionInfo;

        /// <summary>
        /// Gets whether the current commit point's content is already tagged with a version.
        /// </summary>
        public readonly bool CommitContentHasTag;

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
        /// This is the set of filtered versions (<see cref="RepositoryInfoOptions.SingleMajor"/>
        /// and <see cref="RepositoryInfoOptions.OnlyPatch"/> are ignored) from <see cref="CommitVersionInfo.PossibleVersions"/>.
        /// </summary>
        public readonly IReadOnlyList<CSVersion> PossibleVersions;

        /// <summary>
        /// Null if there is a <see cref="RepositoryError"/> or a <see cref="ReleaseTagErrorText"/> that 
        /// prevented its computation.
        /// These are the versions that may be available to any commit above the current one.
        /// This is the set of filtered versions (<see cref="RepositoryInfoOptions.SingleMajor"/>
        /// and <see cref="RepositoryInfoOptions.OnlyPatch"/> are ignored) from <see cref="CommitVersionInfo.NextPossibleVersions"/>.
        /// </summary>
        public readonly IReadOnlyList<CSVersion> NextPossibleVersions;

        /// <summary>
        /// Gets CI informations if a CI release can be done.
        /// Not null only if we are on a branch that is enabled in <see cref="RepositoryInfoOptions.Branches"/> (either 
        /// because it is the current branch or <see cref="RepositoryInfoOptions.StartingBranchName"/> specifies it),
        /// the <see cref="RepositoryInfoOptions.StartingCommitSha"/> is null or empty, there is no <see cref="ValidReleaseTag"/>
        /// on the commit and <see cref="CommitContentHasTag"/> is false.
        /// </summary>
        public readonly CIReleaseInfo CIRelease;

        /// <summary>
        /// Gets the NuGet version (short form) that must be used to build this commit point.
        /// Never null: defaults to <see cref="SVersion.ZeroVersion"/>.
        /// </summary>
        public readonly SVersion FinalNuGetVersion;

        /// <summary>
        /// Gets the semantic version (long form) that must be used to build this commit point.
        /// Never null: defaults to <see cref="SVersion.ZeroVersion"/>.
        /// </summary>
        public readonly SVersion FinalSemVersion;

        /// <summary>
        /// Gets the standardized information version string that must be used to build this commit point.
        /// Never null: defaults to <see cref="InformationalVersion.ZeroInformationalVersion"/> string.
        /// </summary>
        public readonly string FinalInformationalVersion;

        /// <summary>
        /// Gets the NuGet version (short form) that is the content version if <see cref="CommitContentHasTag"/>
        /// id true or the <see cref="FinalNuGetVersion"/>.
        /// Never null: defaults to <see cref="SVersion.ZeroVersion"/>.
        /// </summary>
        public readonly SVersion ContentOrFinalNuGetVersion;

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
        /// Initializes a new <see cref="RepositoryInfo"/> on a <see cref="Repository"/>.
        /// </summary>
        /// <param name="r">The rpository (can be invalid and even null).</param>
        /// <param name="options">Optional options.</param>
        public RepositoryInfo( Repository r, RepositoryInfoOptions options = null )
        {
            Options = options ?? new RepositoryInfoOptions();
            if( r == null ) RepositoryError = "No Git repository.";
            else
            {
                Commit commit;
                CIBranchVersionMode ciVersionMode;
                string ciBuildName;
                RepositoryError = TryFindCommit( options, r, out commit, out ciVersionMode, out ciBuildName );
                Debug.Assert( (ciVersionMode != CIBranchVersionMode.None) == (ciBuildName != null) );
                if( commit != null )
                {
                    CommitSha = commit.Sha;
                    CommitDateUtc = commit.Author.When.UtcDateTime;
                    IsDirtyExplanations = ComputeIsDirty( r, commit, options );
                    if( !IsDirty || options.IgnoreDirtyWorkingFolder )
                    {
                        StringBuilder errors = new StringBuilder();
                        TagCollector collector = new TagCollector( errors,
                                                                   r,
                                                                   options.StartingVersionForCSemVer,
                                                                   options.OverriddenTags );
                        if( errors.Length == 0 )
                        {
                            CommitVersionInfo = collector.GetVersionInfo( commit );
                            ExistingVersions = collector.ExistingVersions.TagCommits;

                            var rawPossibleVersions = CommitVersionInfo.PossibleVersions;
                            IEnumerable<CSVersion> possibleSet = rawPossibleVersions;
                            if( options.OnlyPatch ) possibleSet = possibleSet.Where( v => v.IsPatch );
                            if( options.SingleMajor.HasValue ) possibleSet = possibleSet.Where( v => v.Major == options.SingleMajor.Value );
                            PossibleVersions = possibleSet != rawPossibleVersions ? possibleSet.ToList() : rawPossibleVersions;

                            var rawNextPossibleVersions = CommitVersionInfo.NextPossibleVersions;
                            IEnumerable<CSVersion> nextPossibleSet = rawNextPossibleVersions;
                            if( options.OnlyPatch ) nextPossibleSet = nextPossibleSet.Where( v => v.IsPatch );
                            if( options.SingleMajor.HasValue ) nextPossibleSet = nextPossibleSet.Where( v => v.Major == options.SingleMajor.Value );
                            NextPossibleVersions = nextPossibleSet != rawNextPossibleVersions ? nextPossibleSet.ToList() : rawNextPossibleVersions;

                            CommitContentHasTag = CommitVersionInfo.ThisContentCommit != null;

                            if( CommitVersionInfo.ThisCommit != null )
                            {
                                if( PossibleVersions.Contains( CommitVersionInfo.ThisTag ) && !CommitContentHasTag )
                                {
                                    ValidReleaseTag = CommitVersionInfo.ThisTag;
                                }
                                else
                                {
                                    ReleaseTagIsNotPossibleError = true;
                                    errors.Append( "Release tag '" )
                                           .Append( CommitVersionInfo.ThisTag.ParsedText )
                                           .Append( "' is not valid here. " );
                                    if( CommitContentHasTag )
                                    {
                                        errors.Append( "This commit's content is the same as the " )
                                               .Append( CommitVersionInfo.ThisContentCommit.ThisTag )
                                               .Append( " (sha1: " )
                                               .Append( CommitVersionInfo.ThisContentCommit.CommitSha )
                                               .Append( ")." )
                                               .AppendLine();
                                    }
                                    else
                                    {
                                        errors.Append( "Valid tags are: " )
                                               .Append( string.Join( ", ", possibleSet ) )
                                               .AppendLine();
                                        if( PossibleVersions != rawPossibleVersions
                                            && rawPossibleVersions.Contains( CommitVersionInfo.ThisTag ) )
                                        {
                                            errors.Append( "Note: this version is invalid because of <SingleMajor> or <OnlyPatch> setting in RepositoryInfo.xml." );
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // There is no release tag on the commit point.
                                if( ciBuildName != null && !CommitContentHasTag )
                                {
                                    CIRelease = CIReleaseInfo.Create( commit, ciVersionMode, ciBuildName, errors, CommitVersionInfo );
                                }
                            }
                        }
                        if( errors.Length > 0 ) SetError( errors, out ReleaseTagErrorLines, out ReleaseTagErrorText );
                    }

                    // Conclusion:
                    if( CIRelease != null )
                    {
                        ContentOrFinalNuGetVersion = FinalNuGetVersion = CIRelease.BuildVersionNuGet;
                        FinalSemVersion = CIRelease.BuildVersion;
                    }
                    else if( ValidReleaseTag != null )
                    {
                        ContentOrFinalNuGetVersion = FinalNuGetVersion = SVersion.Parse( ValidReleaseTag.ToString( CSVersionFormat.NuGetPackage ), false );
                        FinalSemVersion = SVersion.Parse( ValidReleaseTag.ToString( CSVersionFormat.Normalized ) );
                    }
                    else
                    {
                        if( CommitContentHasTag )
                        {
                            ContentOrFinalNuGetVersion = SVersion.Parse( CommitVersionInfo.ThisContentCommit.ThisTag.ToString( CSVersionFormat.NuGetPackage ) );
                        }
                        else
                        {
                            ContentOrFinalNuGetVersion = SVersion.ZeroVersion;
                        }
                    }
                }
            }
            // Handles FinalInformationalVersion and SVersion.ZeroVersion for versions if needed.
            if( FinalSemVersion == null )
            {
                FinalSemVersion = SVersion.ZeroVersion;
                FinalNuGetVersion = SVersion.ZeroVersion;
                FinalInformationalVersion = InformationalVersion.ZeroInformationalVersion;
            }
            else
            {
                FinalInformationalVersion = InformationalVersion.BuildInformationalVersion( FinalSemVersion.NormalizedText, FinalNuGetVersion.NormalizedText, CommitSha, CommitDateUtc );
            }
        }

        class ModifiedFile : IWorkingFolderModifiedFile
        {
            readonly Repository _r;
            readonly Commit _commit;
            readonly StatusEntry _entry;
            Blob _committedBlob;
            string _committedText;

            public ModifiedFile( Repository r, Commit commit, StatusEntry e, string entryFilePath )
            {
                Debug.Assert( entryFilePath == e.FilePath );
                _r = r;
                _commit = commit;
                _entry = e;
                Path = entryFilePath;
            }

            Blob GetBlob()
            {
                if( _committedBlob == null )
                {
                    TreeEntry e = _commit[Path];
                    Debug.Assert( e.TargetType == TreeEntryTargetType.Blob );
                    _committedBlob = (Blob)e.Target;
                }
                return _committedBlob;
            }

            public long CommittedContentSize => GetBlob().Size; 

            public Stream GetCommittedContent() => GetBlob().GetContentStream();

            public string CommittedText
            {
                get
                {
                    if( _committedText == null )
                    {
                        using( var s = GetCommittedContent() )
                        using( var r = new StreamReader( s ) )
                        {
                            _committedText = r.ReadToEnd();
                        }
                    }
                    return _committedText;
                }
            }

            public string Path { get; }

            public string FullPath => _r.Info.WorkingDirectory + Path; 

            public string RepositoryFullPath => _r.Info.WorkingDirectory;

        }

        string ComputeIsDirty( Repository r, Commit commit, RepositoryInfoOptions options )
        {
            RepositoryStatus repositoryStatus = r.RetrieveStatus();
            int addedCount = repositoryStatus.Added.Count();
            int missingCount = repositoryStatus.Missing.Count();
            int removedCount = repositoryStatus.Removed.Count();
            int stagedCount = repositoryStatus.Staged.Count();
            StringBuilder b = null;
            if( addedCount > 0 || missingCount > 0 || removedCount > 0 || stagedCount > 0 )
            {
                b = new StringBuilder( "Found: " );
                if( addedCount > 0 ) b.AppendFormat( "{0} file(s) added", addedCount );
                if( missingCount > 0 ) b.AppendFormat( "{0}{1} file(s) missing", b.Length > 10 ? ", " : null, missingCount );
                if( removedCount > 0 ) b.AppendFormat( "{0}{1} file(s) removed", b.Length > 10 ? ", " : null, removedCount );
                if( stagedCount > 0 ) b.AppendFormat( "{0}{1} file(s) staged", b.Length > 10 ? ", " : null, removedCount );
            }
            else
            {
                int fileCount = 0;
                foreach( StatusEntry m in repositoryStatus.Modified )
                {
                    string path = m.FilePath;
                    if( !options.IgnoreModifiedFiles.Contains( path )
                        && (options.IgnoreModifiedFilePredicate == null
                            || !options.IgnoreModifiedFilePredicate( new ModifiedFile( r, commit, m, path ) )) )
                    {
                        ++fileCount;
                        if( !options.IgnoreModifiedFileFullProcess )
                        {
                            Debug.Assert( b == null );
                            b = new StringBuilder( "At least one Modified file found: " );
                            b.Append( path );
                            break;
                        }
                        if( b == null )
                        {
                            b = new StringBuilder( "Modified file(s) found: " );
                            b.Append( path );
                        }
                        else if( fileCount <= 10 ) b.Append( ", " ).Append( path );
                    }
                }
                if( fileCount > 10 ) b.AppendFormat( ", and {0} other file(s)", fileCount - 10 );
            }
            if( b == null ) return null;
            b.Append( '.' );
            return b.ToString();
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
                    string branchName = r.Head.FriendlyName;
                    if( branchName == "(no branch)" )
                    {
                        string remotePrefix = options.RemoteName + '/';
                        branchNames = r.Branches
                                        .Where( b => b.Tip == locCommit && (!b.IsRemote || b.FriendlyName.StartsWith( remotePrefix )) )
                                        .Select( b => b.IsRemote ? b.FriendlyName.Substring( remotePrefix.Length ) : b.FriendlyName );
                    }
                    else branchNames = new[] { branchName };
                }
                else
                {
                    string remotePrefix = options.RemoteName + '/';
                    string localBranchName = options.StartingBranchName.StartsWith( remotePrefix )
                                                ? options.StartingBranchName.Substring( remotePrefix.Length )
                                                : options.StartingBranchName;
                    Branch br = r.Branches[options.StartingBranchName];
                    if( br == null && ReferenceEquals( localBranchName, options.StartingBranchName ) )
                    {
                        string remoteName = remotePrefix + options.StartingBranchName;
                        br = r.Branches[remoteName];
                        if( br == null ) return $"Unknown StartingBranchName: '{options.StartingBranchName}' (also tested on remote '{remoteName}').";
                    }
                    if( br == null ) return $"Unknown (remote) StartingBranchName: '{options.StartingBranchName}'.";
                    commit = br.Tip;
                    branchNames = new[] { localBranchName };
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
                if( commit == null ) return $"Commit '{commitSha}' not found.";
            }
            return null;
        }

        static void SetError( StringBuilder errors, out IReadOnlyList<string> lines, out string text )
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
            path = Repository.Discover( path );
            using( var repo = path != null ? new Repository( path ) : null )
            {
                return new RepositoryInfo( repo, options );
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

            path = Repository.Discover( path );
            using( var repo = path != null ? new Repository( path ) : null )
            {
                if( repo == null ) return new RepositoryInfo( null, null );
                return new RepositoryInfo( repo, optionsBuilder( repo.Info.WorkingDirectory ) );
            }
        }

    }
}

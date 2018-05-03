using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using CSemVer;

namespace SimpleGitVersion
{

    /// <summary>
    /// Discovers existing tags in the repository, resolves them by applying <see cref="CSVersion.DefinitionStrength"/>, detects some of the possible inconsistencies
    /// and provide a <see cref="GetVersionInfo"/> to retrieve commit information.
    /// </summary>
    partial class TagCollector
    {
        readonly CSVersion _startingVersionForCSemVer;
        readonly Dictionary<string, TagCommit> _collector;
        readonly Dictionary<string, CommitVersionInfo> _versionsCache;
        readonly RepositoryVersions _repoVersions;

        /// <summary>
        /// Gets the minimal version to consider. When null, the whole repository must be valid in terms of release tags.
        /// </summary>
        public CSVersion StartingVersionForCSemVer => _startingVersionForCSemVer; 

        /// <summary>
        /// Gets a read only and ordered list of the existing versions in the repository. 
        /// If there is no <see cref="StartingVersionForCSemVer"/>, the first version is checked (it must be one of the <see cref="CSVersion.FirstPossibleVersions"/>), otherwise
        /// this existing versions does not contain any version smaller than StartingVersionForCSemVer.
        /// This existing versions must always be compact (ie. no "holes" must exist between them) otherwise an error is added to the collector.
        /// </summary>
        public RepositoryVersions ExistingVersions => _repoVersions; 

        /// <summary>
        /// Initializes a new <see cref="TagCollector"/>.
        /// Errors may be appended to the collector that can be syntaxic errors or multiple different versions applied to the same commit point.
        /// </summary>
        /// <param name="errors">A collector of errors. One line per error.</param>
        /// <param name="repo">The Git repository.</param>
        /// <param name="startingVersionForCSemVer">Vesion tags lower than this version will be ignored.</param>
        /// <param name="overriddenTags">Optional commits with associated tags that are applied as if they exist in the repository.</param>
        /// <param name="checkValidExistingVersions">
        /// When true, existing versions are checked: one of the valid first version must exist and exisitng versions
        /// must be compact.
        /// </param>
        public TagCollector(
            StringBuilder errors,
            Repository repo,
            string startingVersionForCSemVer = null,
            IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> overriddenTags = null,
            bool checkValidExistingVersions = false )
        {
            Debug.Assert( errors != null && repo != null );

            _collector = new Dictionary<string, TagCommit>();
            _versionsCache = new Dictionary<string, CommitVersionInfo>();

            if( startingVersionForCSemVer != null )
            {
                _startingVersionForCSemVer = CSVersion.TryParse( startingVersionForCSemVer, true );
                if( !_startingVersionForCSemVer.IsValid )
                {
                    errors.Append( "Invalid StartingVersionForCSemVer. " ).Append( _startingVersionForCSemVer.ErrorMessage ).AppendLine();
                    return;
                }
            }
            // Register all tags.
            RegisterAllTags( errors, repo, overriddenTags );

            // Resolves multiple tags on the same commit.
            CloseCollect( errors );

            // Sorts TagCommit, optionally checking the existing versions. 
            _repoVersions = new RepositoryVersions( _collector.Values, errors, _startingVersionForCSemVer, checkValidExistingVersions );

            // Register content.
            if( errors.Length == 0 )
            {
                foreach( var tc in _repoVersions.TagCommits )
                {
                    RegisterContent( tc );
                }
            }
        }

        void RegisterAllTags( StringBuilder errors, Repository repo, IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> overriddenTags )
        {
            bool startingVersionForCSemVerFound = _startingVersionForCSemVer == null;
            foreach( var tag in repo.Tags )
            {
                Commit tagCommit = tag.ResolveTarget() as Commit;
                if( tagCommit == null ) continue;
                RegisterOneTag( errors, tagCommit, tag.FriendlyName, ref startingVersionForCSemVerFound );
            }
            // Applies overrides (if any) as if they exist in the repository.
            if( overriddenTags != null )
            {
                foreach( var k in overriddenTags )
                {
                    Commit o = null;
                    if( string.IsNullOrEmpty( k.Key ) )
                    {
                        errors.AppendFormat( "Invalid overriden commit: the key is null or empty." ).AppendLine();
                    }
                    else if( k.Key.Equals( "head", StringComparison.OrdinalIgnoreCase ) )
                    {
                        o = repo.Head.Tip;
                        Debug.Assert( o != null, "Unitialized Git repository. Already handled." );
                    }
                    else
                    {
                        o = repo.Lookup<Commit>( k.Key );
                        if( o == null )
                        {
                            errors.AppendFormat( "Overriden commit '{0}' does not exist.", k.Key ).AppendLine();
                        }
                    }
                    if( o != null )
                    {
                        foreach( string tagName in k.Value )
                        {
                            RegisterOneTag( errors, o, tagName, ref startingVersionForCSemVerFound );
                        }
                    }
                }
            }
            if( !startingVersionForCSemVerFound )
            {
                Debug.Assert( _startingVersionForCSemVer != null && _startingVersionForCSemVer.IsValid );
                errors.AppendFormat( "Unable to find StartingVersionForCSemVer = '{0}'. A commit must be tagged with it.", _startingVersionForCSemVer ).AppendLine();
            }
        }

        void RegisterOneTag( StringBuilder errors, Commit c, string tagName, ref bool startingVersionForCSemVerFound )
        {
            CSVersion v = CSVersion.TryParse( tagName );
           if( v.IsValid )
            {
                if( _startingVersionForCSemVer != null )
                {
                    int cmp = _startingVersionForCSemVer.CompareTo( v );
                    if( cmp == 0 ) startingVersionForCSemVerFound = true;
                    else if( cmp > 0 )
                    {
                        // This version is smaller than the StartingVersionForCSemVer:
                        // we ignore it.
                        return;
                    }
                }
                TagCommit tagCommit;
                if( _collector.TryGetValue( c.Sha, out tagCommit ) )
                {
                    tagCommit.AddCollectedTag( v );
                }
                else _collector.Add( c.Sha, tagCommit = new TagCommit( c, v ) );
            }
        }

        void CloseCollect( StringBuilder errors )
        {
            List<TagCommit> invalidTags = null;
            foreach( var c in _collector.Values )
            {
                if( !c.CloseCollect( errors ) )
                {
                    if( invalidTags == null ) invalidTags = new List<TagCommit>();
                    invalidTags.Add( c );
                }
            }
            if( invalidTags != null )
            {
                foreach( var c in invalidTags )
                {
                    _collector.Remove( c.CommitSha );
                }
            }
        }

        void RegisterContent( TagCommit tagCommit )
        {
            TagCommit contentExists;
            if( _collector.TryGetValue( tagCommit.ContentSha, out contentExists ) )
            {
                if( tagCommit != contentExists ) contentExists.AddSameTree( tagCommit );
            }
            else _collector.Add( tagCommit.ContentSha, tagCommit );
        }

        TagCommit GetCommit( string sha )
        {
            TagCommit t;
            _collector.TryGetValue( sha, out t );
            return t;
        }
    }   
}

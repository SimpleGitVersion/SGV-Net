using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{

    public partial class RepositoryInfo
    {
        /// <summary>
        /// Discovers existing tags in the repository, resolves them by applying <see cref="ReleaseTagVersion.DefinitionStrength"/>, detects some of the possible inconsistencies
        /// and provide a <see cref="GetVersions"/> to retrieve commit information.
        /// </summary>
        class TagCollector
        {
            readonly ReleaseTagVersion _startingVersionForCSemVer;
            readonly Dictionary<string, TagCommit> _collector;
            readonly IReadOnlyList<ReleaseTagVersion> _existingVersions;

            /// <summary>
            /// Gets the minimal version to consider. When null, the whole repository must be valid in terms of release tags.
            /// </summary>
            public ReleaseTagVersion StartingVersionForCSemVer
            {
                get { return _startingVersionForCSemVer; }
            }

            /// <summary>
            /// Gets a read only and ordered list of the existing versions in the repository. 
            /// If there is no <see cref="StartingVersionForCSemVer"/>, the first version is checked (it must be one of the <see cref="ReleaseTagVersion.FirstPossibleVersions"/>), otherwise
            /// this existing versions does not contain any version smaller than StartingVersionForCSemVer.
            /// This existing versions must always be compact (ie. no "holes" must exist between them) otherwise an error is added to the collector.
            /// </summary>
            public IReadOnlyList<ReleaseTagVersion> ExistingVersions
            {
                get { return _existingVersions; }
            } 

            /// <summary>
            /// Initializes a new <see cref="TagCollector"/>.
            /// Errors may be appended to the collector that can be syntaxic errors or multiple different versions applied to the same commit point.
            /// </summary>
            /// <param name="errors">A collector of errors. One line per error.</param>
            /// <param name="repo">The Git repository.</param>
            /// <param name="startingVersionForCSemVer">Vesion tags lower than this version will be ignored.</param>
            /// <param name="analyseInvalidTagSyntax">
            /// Optional function that drives the behavior regarding malformed tags of commits.
            /// When null, <see cref="ReleaseTagParsingMode.IgnoreMalformedTag">IgnoreMalformedTag</see> is used for all tags.
            /// </param>
            /// <param name="overridenTags">Optional commits with associated tags that are applied as if they exist in the repository.</param>
            public TagCollector( 
                StringBuilder errors, 
                Repository repo, 
                string startingVersionForCSemVer = null,
                Func<Commit, ReleaseTagParsingMode> analyseInvalidTagSyntax = null, 
                IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> overridenTags = null )
            {
                Debug.Assert( errors != null && repo != null );

                _collector = new Dictionary<string, TagCommit>();
                if( startingVersionForCSemVer != null )
                {
                    _startingVersionForCSemVer = ReleaseTagVersion.TryParse( startingVersionForCSemVer, true );
                    if( !_startingVersionForCSemVer.IsValid )
                    {
                        errors.Append( "Invalid StartingVersionForCSemVer. " ).Append( _startingVersionForCSemVer.ParseErrorMessage ).AppendLine();
                        return;
                    }
                }
                bool startingVersionForCSemVerFound = _startingVersionForCSemVer == null;
                foreach( var tag in repo.Tags )
                {
                    Commit tagCommit = tag.ResolveTarget() as Commit;
                    if( tagCommit == null ) continue;
                    RegisterTag( errors, tagCommit, tag.Name, analyseInvalidTagSyntax, ref startingVersionForCSemVerFound );
                }
                // Applies overrides (if any) as if they exist in the repository.
                if( overridenTags != null )
                {
                    foreach( var k in overridenTags )
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
                        if( o != null)
                        {
                            foreach( string tagName in k.Value )
                            {
                                RegisterTag( errors, o, tagName, analyseInvalidTagSyntax, ref startingVersionForCSemVerFound );
                            }
                        }
                    }
                }
                if( !startingVersionForCSemVerFound )
                {
                    Debug.Assert( _startingVersionForCSemVer != null && _startingVersionForCSemVer.IsValid );
                    errors.AppendFormat( "Unable to find StartingVersionForCSemVer = '{0}'. A commit must be tagged with it.", _startingVersionForCSemVer ).AppendLine();
                }
                // End of first step: resolves multiple tags.
                foreach( var c in _collector.Values )
                {
                    c.CloseCollect( errors );
                }
                _existingVersions = ComputeExistingVersions( errors );
                // If there is no error, propagate child version whenever the content are the same.
                if( errors.Length == 0 )
                {
                    var allCommits = _collector.Values.Distinct().Where( c => c.ThisTag != null ).ToList();
                    foreach( var tc in allCommits )
                    {
                        Propagate( errors, tc );
                    }
                }
            }

            void Propagate( StringBuilder errors, TagCommit tc )
            {
                Debug.Assert( tc.BestTag != null );
                Commit commit = tc.Commit;
                foreach( Commit p in commit.Parents )
                {
                    TagCommit rParent;
                    if( !_collector.TryGetValue( p.Sha, out rParent ) )
                    {
                        rParent = new TagCommit( p, null );
                        _collector.Add( p.Sha, rParent );
                        RegisterContent( rParent );
                    }
                    bool applicable = commit.Tree.Sha == rParent.Commit.Tree.Sha;
                    if( rParent.AddPropagatedVersionFromChild( errors, tc, applicable ) )
                    {
                        Debug.Assert( rParent.BestTag != null );
                        Propagate( errors, rParent );
                    }
                }
            }

            IReadOnlyList<ReleaseTagVersion> ComputeExistingVersions( StringBuilder errors )
            {
                var existingVersions = _collector.Values.Where( t => t.ThisTag != null ).Distinct().OrderBy( t => t.ThisTag ).ToList();
                if( existingVersions.Count > 0 )
                {
                    var first = existingVersions[0].ThisTag;
                    if( _startingVersionForCSemVer == null && !first.IsDirectPredecessor( null ) )
                    {
                        errors.AppendFormat( "First existing version is '{0}' (on '{1}'). One or more previous versions are missing.", first, existingVersions[0].CommitSha )
                                .AppendLine();
                    }
                    for( int i = 0; i < existingVersions.Count - 1; ++i )
                    {
                        var prev = existingVersions[i].ThisTag;
                        var next = existingVersions[i + 1].ThisTag;
                        if( next.Equals( prev ) )
                        {
                            errors.AppendFormat( "Version '{0}' is defined on '{1}' and '{2}'.", prev, existingVersions[i].CommitSha, existingVersions[i + 1].CommitSha )
                                    .AppendLine();
                        }
                        else if( !next.IsDirectPredecessor( prev ) )
                        {
                            errors.AppendFormat( "Missing one or more version(s) between '{0}' and '{1}'.", prev, next )
                                    .AppendLine();
                        }
                    }
                }
                return existingVersions.Select( tc => tc.ThisTag ).ToArray();
            }

            class VersionReader
            {
                readonly Dictionary<string,TagCommit> _collector;
                readonly Commit _topCommit;
                readonly Dictionary<Commit,CommitVersions> _cache;
                ReleaseTagVersion _ignorePropagated;

                public VersionReader( Dictionary<string,TagCommit> collector, Commit c )
                {
                    _collector = collector;
                    _topCommit = c;
                    _cache = new Dictionary<Commit, CommitVersions>();
                }
                
                public CommitVersions GetVersions()
                {
                    return GetVersions( _topCommit );
                }

                CommitVersions GetVersions( Commit c )
                {
                    Debug.Assert( _ignorePropagated == null || _ignorePropagated.IsValid );
                    Debug.Assert( (_topCommit != c) || _ignorePropagated == null, "top commit => ignorePropagated == null" );

                    CommitVersions exist;
                    if( !_cache.TryGetValue( c, out exist ) )
                    {
                        TagCommit tc;
                        if( _collector.TryGetValue( c.Sha, out tc ) )
                        {
                            if( c == _topCommit ) _ignorePropagated = tc.ThisTag;
                            var bc = EnsureBaseVersions( c );
                            ReleaseTagVersion tag = null;
                            if( tc.Commit.Sha == c.Sha )
                            {
                                if( _ignorePropagated != null && !_ignorePropagated.Equals( tc.BestTag ) )
                                {
                                    tag = tc.BestTag;
                                }
                                else
                                {
                                    tag = tc.ThisTag;
                                }
                            }
                            exist = new CommitVersions( tag, c.Sha, bc );
                        }
                        else exist = new CommitVersions( null, c.Sha, EnsureBaseVersions( c ) );
                        _cache.Add( c, exist );
                    }
                    return exist;
                }

                CommitBaseVersions EnsureBaseVersions( Commit c )
                {
                    Debug.Assert( _ignorePropagated == null || _ignorePropagated.IsValid );
                    CommitBaseVersions b = new CommitBaseVersions();
                    TagCommit tcContent;
                    if( _collector.TryGetValue( c.Tree.Sha, out tcContent ) )
                    {
                        b.ContentTags = tcContent.GetSameContent( true ).Select( tContent => tContent.BestTag ).Where( tag => tag != null ).ToArray();
                    }
                    else b.ContentTags = ReleaseTagVersion.EmptyArray;
                    List<CommitVersions> parents = c.Parents.Select( p => GetVersions( p ) ).ToList();
                    var taggedParents = parents.Where( p => p.TagOrParentTag != null );
                    if( taggedParents.Any() )
                    {
                        CommitVersions best = taggedParents.MaxBy( cv => cv.TagOrParentTag );
                        b.ParentTag = best.TagOrParentTag;
                        b.ParentTagSha = best.TagOrParentTagSha;
                        b.DepthFromParent = best.DepthFromParent + 1;
                    }
                    b.ParentContentTags = parents.SelectMany( p => p.ContentOrParentContentTags ).Distinct().ToArray();
                    return b;
                }
            }

            public CommitVersions GetVersions( Commit c )
            {
                return new VersionReader( _collector, c ).GetVersions();
            }

            void RegisterTag( StringBuilder errors, Commit c, string tagName, Func<Commit, ReleaseTagParsingMode> analyseInvalidTagSyntax, ref bool startingVersionForCSemVerFound )
            {
                ReleaseTagParsingMode mode = analyseInvalidTagSyntax == null ? ReleaseTagParsingMode.IgnoreMalformedTag : analyseInvalidTagSyntax( c );
                ReleaseTagVersion v = ReleaseTagVersion.TryParse( tagName, mode == ReleaseTagParsingMode.RaiseErrorOnMalformedTag );
                if( v.IsMalformed )
                {
                    // Parsing in strict mode can result in malformed tag. We can not assume that here:
                    // Debug.Assert( mode == ReleaseTagParsingMode.RaiseErrorOnMalformedTag );
                    if( mode == ReleaseTagParsingMode.RaiseErrorOnMalformedTag )
                    {
                        errors.AppendFormat( "Malformed {0} on commit '{1}'.", v.ParseErrorMessage, c.Sha ).AppendLine();
                    }
                    return;
                }
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
                    if( mode == ReleaseTagParsingMode.RaiseErrorOnMalformedTagAndNonStandardPreReleaseName && v.IsPreRelease && !v.IsPreReleaseNameStandard )
                    {
                        errors.AppendFormat( "Invalid PreRelease name in '{0}' on commit '{1}'.", v.OriginalTagText, c.Sha ).AppendLine();
                        return;
                    }
                    TagCommit tagCommit;
                    if( _collector.TryGetValue( c.Sha, out tagCommit ) )
                    {
                        tagCommit.AddCollectedTag( v );
                    }
                    else _collector.Add( c.Sha, tagCommit = new TagCommit( c, v ) );
                    // Register commit content.
                    RegisterContent( tagCommit );
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
        }
    }
}

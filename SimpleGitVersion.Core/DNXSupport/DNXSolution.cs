using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    public class DNXSolution
    {
        readonly string _solutionDir;
        readonly DNXProjectFile[] _projects;
        readonly ILogger _logger;
        SimpleRepositoryInfo _info;

        /// <summary>
        /// Initializes a new <see cref="DNXSolution"/>. 
        /// </summary>
        /// <param name="path">Path of the directory that contains the project.json file.</param>
        /// <param name="logger">Logger to use. Must not be null.</param>
        /// <param name="projectFilter">Optional project filter.</param>
        public DNXSolution( string path, ILogger logger, Func<DNXProjectFile, bool> projectFilter = null )
        {
            if( path == null ) throw new ArgumentNullException( nameof( path ) );
            if( logger == null ) throw new ArgumentNullException( nameof( logger ) );

            _logger = logger;
            _solutionDir = FindDirectoryFrom( path, ".git" );
            if( _solutionDir == null ) _logger.Error( ".git directory not found." );
            else
            {
                _solutionDir = _solutionDir.Remove( _solutionDir.Length - 4 );
                _projects = Directory.EnumerateFiles( _solutionDir, "project.json", SearchOption.AllDirectories )
                                    .Where( p => p.IndexOf( @"\bin\", _solutionDir.Length ) < 0 )
                                    .Select( p => new DNXProjectFile( this, p ) )
                                    .Where( p => projectFilter == null || projectFilter( p ) )
                                    .ToArray();
                var dup = _projects.GroupBy( p => p.ProjectName, StringComparer.OrdinalIgnoreCase )
                                    .Where( g => g.Count() > 1 );
                if( dup.Any() )
                {
                    _logger.Error( String.Format( "Duplicate names found for projects: {0}.", String.Join( ", ", dup.SelectMany( g => g.Select( p => p.RelativeProjectFilePath ) ) ) ) );
                    _projects = null;
                }
            }
        }

        /// <summary>
        /// Gets the solution directory (where the .git folder is). Null if not found: this CommandContext is not valid.
        /// This ends with a <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <value>The solution directory.</value>
        public string SolutionDir { get { return _solutionDir; } }

        /// <summary>
        /// Gets whether this solution is valid: <see cref="SolutionDir"/> exists (.git folder has been found)
        /// and there is no duplicate project name.
        /// </summary>
        public bool IsValid { get { return _projects != null; } }

        /// <summary>
        /// Gets the logger to use.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get { return _logger; } }

        /// <summary>
        /// Gets all the DNX projects found in this solution.
        /// </summary>
        public IReadOnlyList<DNXProjectFile> Projects { get { return _projects; } }


        /// <summary>
        /// Gets the <see cref="DNXProjectFile"/> from the project path (or from the path of the project.json file).
        /// This serach is case insensitive.
        /// </summary>
        /// <param name="projectPath">Path of the project directory or project.json file.</param>
        /// <returns>The project or null if not found.</returns>
        public DNXProjectFile FindFromPath( string projectPath )
        {
            if( projectPath == null ) throw new ArgumentNullException( projectPath );
            string name = Path.GetFileName( projectPath );
            if( string.IsNullOrEmpty( name ) || name == "project.json" )
            {
                name = Path.GetFileName( Path.GetDirectoryName( projectPath ) );
            }
            return _projects.FirstOrDefault( p => StringComparer.OrdinalIgnoreCase.Equals( p.ProjectName, name ) );
        }
        
        /// <summary>
        /// Simple predicates that looks up for a project by name.
        /// Used to filter projects references. This lookup is case sensitive.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsProject( string name )
        {
            return _projects.FirstOrDefault( p => p.ProjectName == name ) != null;
        }

        /// <summary>
        /// Gets the repository information, ignoring any project.json "version" property.
        /// </summary>
        /// <value>The repository information.</value>
        public SimpleRepositoryInfo RepositoryInfo
        {
            get { return _info ?? (_info = GetRepositoryInfo( _logger, null )); }
        }

        /// <summary>
        /// Restores the project.json files that differ only by version from committed content.
        /// </summary>
        /// <returns>The number of files that have been restored.</returns>
        public int RestoreProjectFilesThatDifferOnlyByVersion()
        {
            int count = 0;
            GetRepositoryInfo( EmptyLogger.Empty, (m,content) =>
            {
                _logger.Trace( string.Format( "Restoring file '{0}'.", m.Path ) );
                // Use content.Text that has normalized line endings instead of m.CommittedText
                // that comes directly from the git.
                File.WriteAllText( m.FullPath, content.Text );
                ++count;
            } );
            _logger.Info( string.Format( "Restored {0} project.json file(s).", count ) );
            return count;
        }

        /// <summary>
        /// Updates the project.json files with the given version (or the computed version from <see cref="RepositoryInfo"/>).
        /// </summary>
        /// <param name="version">The version to set.</param>
        public void UpdateProjectFiles( string version = null )
        {
            if( _projects != null && _projects.Length > 0 )
            {
                if( version == null )
                {
                    SimpleRepositoryInfo info = RepositoryInfo;
                    version = info.IsValid ? info.SemVer : "0.0.0-Absolutely-Invalid";
                }
                _logger.Info( string.Format( "Updating or injecting \"version\": \"{0}\" in {1} project.json file(s).", version, _projects.Length ) );
                foreach( var f in _projects )
                {
                    f.UpdateProjectJSONFile( version );
                }
            }
            else _logger.Warn( "No project.json files found." );
        }

        private SimpleRepositoryInfo GetRepositoryInfo( ILogger logger, Action<IWorkingFolderModifiedFile,ProjectFileContent> hook )
        {
            return SimpleRepositoryInfo.LoadFromPath( logger, _solutionDir, ( log, hasRepoXml, options ) =>
            {
                options.IgnoreModifiedFileFullProcess = true;
                options.IgnoreModifiedFilePredicate = m =>
                {
                    if( m.Path.EndsWith( "project.json", StringComparison.Ordinal )
                        && _projects.FirstOrDefault( p => PathComparer.Default.Equals( p.RelativeProjectFilePath, m.Path ) ) != null )
                    {
                        var local = new ProjectFileContent( File.ReadAllText( m.FullPath ), ContainsProject );
                        var committed = new ProjectFileContent( m.CommittedText, ContainsProject );
                        if( local.EqualsWithoutVersion( committed ) )
                        {
                            if( hook != null ) hook( m, committed );
                            return true;
                        }
                    }
                    return false;
                };
            }
          );
        }

        /// <summary>
        /// Finds a named directory above or next to the specified <paramref name="start"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="start">Starting directory.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>Null if not found, otherwise the path of the directory.</returns>
        static string FindDirectoryFrom( string start, string directoryName )
        {
            if( start == null ) throw new ArgumentNullException( nameof( start ) );
            if( directoryName == null ) throw new ArgumentNullException( nameof( directoryName ) );
            string p = start;
            string pF;
            while( !Directory.Exists( pF = Path.Combine( p, directoryName ) ) )
            {
                p = Path.GetDirectoryName( p );
                if( String.IsNullOrEmpty( p ) ) return null;
            }
            return pF;
        }
    }
}

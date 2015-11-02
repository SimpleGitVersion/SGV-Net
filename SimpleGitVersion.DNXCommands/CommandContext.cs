using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands
{
    class CommandContext
    {
        readonly string _projectDir;
        readonly string _solutionDir;
        readonly LoggerAdapter _logger;
        readonly string[] _projectFiles;
        readonly string[] _relativeProjectFiles;
        readonly string[] _projectNames;
        SimpleRepositoryInfo _info;

        public CommandContext( string projectPath, bool verbose )
        {
            _logger = new LoggerAdapter( verbose );
            _projectDir = projectPath;
            _solutionDir = FindDirectoryFrom( projectPath, ".git" );
            if( _solutionDir == null ) _logger.Error( ".git directory not found." );
            else
            {
                _solutionDir = _solutionDir.Remove( _solutionDir.Length - 4 );
                _projectFiles = Directory.EnumerateFiles( _solutionDir, "project.json", SearchOption.AllDirectories )
                                    .Where( p => p.IndexOf( @"\bin\", _solutionDir.Length ) < 0 )
                                    .ToArray();
                _relativeProjectFiles = _projectFiles.Select( p => p.Substring( _solutionDir.Length ) ).ToArray();
                _projectNames = _projectFiles.Select( p => Path.GetFileName( Path.GetDirectoryName( p ) ) ).ToArray();
                _logger.Trace( String.Format( "{0} project(s): {1}", _projectFiles.Length, String.Join( ", ", _relativeProjectFiles ) ) );
            }
        }

        /// <summary>
        /// Gets the solution directory. Null if not found: this CommandContext is not valid.
        /// This ends with a <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <value>The solution directory.</value>
        public string SolutionDir { get { return _solutionDir; } }

        /// <summary>
        /// Gets the logger to use.
        /// </summary>
        /// <value>The logger.</value>
        public LoggerAdapter Logger { get { return _logger; } }

        /// <summary>
        /// Gets the full path of all project.json files in the <see cref="SolutionDir"/>.
        /// </summary>
        /// <value>All project.json full paths.</value>
        public IReadOnlyList<string> ProjectFiles { get { return _projectFiles; } }

        /// <summary>
        /// Gets the relative path of all project.json files in the <see cref="SolutionDir"/>.
        /// </summary>
        /// <value>All project.json relative paths.</value>
        public IReadOnlyList<string> RelativeProjectFiles { get { return _relativeProjectFiles; } }

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
            GetRepositoryInfo( LoggerAdapter.Empty, (m,content) =>
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
            if( _projectFiles.Length > 0 )
            {
                if( version == null )
                {
                    SimpleRepositoryInfo info = RepositoryInfo;
                    version = info.IsValid ? info.SemVer : "0.0.0-Absolutely-Invalid";
                }
                _logger.Info( string.Format( "Updating or injecting \"version\": \"{0}\" in {1} project.json file(s).", version, _projectFiles.Length ) );
                foreach( var f in _projectFiles )
                {
                    string text = File.ReadAllText( f );
                    _logger.Trace( "================ Original ================" );
                    _logger.Trace( text );
                    _logger.Trace( "=============== /Original ================" );
                    ProjectFileContent content = new ProjectFileContent( text, _projectNames.Contains );
                    if( content.Version == null ) _logger.Warn( "Unable to update version in: " + f );
                    else if( content.Version == version )
                    {
                        _logger.Trace( "(File is up to date.)" );
                    }
                    else
                    {
                        string modified = content.GetReplacedText( version );
                        _logger.Trace( "================ Modified ================" );
                        _logger.Trace( modified );
                        File.WriteAllText( f, modified );
                        _logger.Trace( "=============== /Modified ================" );
                    }
                }
            }
            else _logger.Warn( "No project.json files found." );
        }

        private SimpleRepositoryInfo GetRepositoryInfo( ILogger logger, Action<IWorkingFolderModifiedFile,ProjectFileContent> hook )
        {
            return SimpleRepositoryInfo.LoadFromPath( logger, _solutionDir, ( log, hasRepoXml, options ) =>
            {
                options.IgnoreModifiedFilePredicate = m =>
                {
                    if( m.Path.EndsWith( "project.json", StringComparison.Ordinal )
                        && _relativeProjectFiles.Contains( m.Path, PathComparer.Default ) )
                    {
                        var local = new ProjectFileContent( File.ReadAllText( m.FullPath ), _projectNames.Contains );
                        var committed = new ProjectFileContent( m.CommittedText, _projectNames.Contains );
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

        public IEnumerable<string> ExistingSGVVersionInfoFiles
        {
            get { return Directory.EnumerateFiles( _solutionDir, "SGVVersionInfo.cs" ); }
        }

        public string ProjectSGVVersionInfoFile
        {
            get
            {
                string f = Path.GetFullPath( Path.Combine( _projectDir, @"Properties\SGVVersionInfo.cs" ) );
                if( File.Exists( f ) ) return f;
                f = Directory.EnumerateFiles( _projectDir, "SGVVersionInfo.cs" ).FirstOrDefault();
                if( f != null ) _logger.Trace( "Found: " + f.Substring( _solutionDir.Length ) );
                return f;
            }
        }

        /// <summary>
        /// Gets the theoretical path to project SGV version information file: either project/Properties/SGVVersionInfo.cs
        /// or project/SGVVersionInfo.cs if Properties directory does not exist.
        /// </summary>
        /// <value>The theoretical path to SGVVersionInfo.cs.</value>
        public string TheoreticalProjectSGVVersionInfoFile
        {
            get
            {
                string propertiesDir = Path.Combine( _projectDir, @"Properties" );
                if( Directory.Exists( propertiesDir ))
                {
                    return Path.Combine( propertiesDir, @"SGVVersionInfo.cs" );
                }
                return Path.Combine( _projectDir, @"SGVVersionInfo.cs" );
            }
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

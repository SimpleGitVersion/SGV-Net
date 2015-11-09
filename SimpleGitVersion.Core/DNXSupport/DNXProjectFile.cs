using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Simple project.json description.
    /// </summary>
    public class DNXProjectFile
    {
        readonly DNXSolution _ctx;
        readonly string _projectFile;
        readonly string _relativeProjectFile;
        readonly string _projectName;
        readonly string _projectDir;
        string _projectFileCache;
        string _projectLockFileCache;

        internal DNXProjectFile( DNXSolution ctx, string projectFile )
        {
            _ctx = ctx;
            Debug.Assert( projectFile != null && projectFile.EndsWith( "project.json" ) && File.Exists( projectFile ) );
            _projectFile = projectFile;
            _relativeProjectFile = projectFile.Substring( ctx.SolutionDir.Length );
            _projectName = Path.GetFileName( Path.GetDirectoryName( projectFile ) );
            Debug.Assert( "project.json".Length == 12 );
            _projectDir = _projectFile.Substring( 0, _projectFile.Length - 12 );
        }

        /// <summary>
        /// Gets the project directory.
        /// This ends with a <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <value>The project directory.</value>
        public string ProjectDir { get { return _projectDir; } }

        /// <summary>
        /// Gets the project name.
        /// </summary>
        /// <value>The project name.</value>
        public string ProjectName { get { return _projectName; } }

        /// <summary>
        /// Gets the project.json file path.
        /// </summary>
        /// <value>The project.json file path.</value>
        public string ProjectFilePath { get { return _projectFile; } }

        /// <summary>
        /// Gets the project.json file path.
        /// </summary>
        /// <value>The project.json file path.</value>
        public string RelativeProjectFilePath { get { return _relativeProjectFile; } }

        /// <summary>
        /// Gets the path to the 'SGVVersionInfo.cs' file in <see cref="ProjectDir"/>.
        /// Null if it does not exist.
        /// </summary>
        public string SGVVersionInfoFile
        {
            get
            {
                string f = Path.GetFullPath( Path.Combine( _projectDir, @"Properties\SGVVersionInfo.cs" ) );
                if( File.Exists( f ) ) return f;
                return Directory.EnumerateFiles( _projectDir, "SGVVersionInfo.cs" ).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the theoretical path to project SGV version information file: either <see cref="ProjectDir"/>/Properties/SGVVersionInfo.cs
        /// or ProjectDir/SGVVersionInfo.cs if Properties directory does not exist.
        /// </summary>
        /// <value>The theoretical path to SGVVersionInfo.cs.</value>
        public string TheoreticalSGVVersionInfoFile
        {
            get
            {
                string propertiesDir = _projectDir + "Properties";
                if( Directory.Exists( propertiesDir ) )
                {
                    return Path.Combine( propertiesDir, @"SGVVersionInfo.cs" );
                }
                return Path.Combine( _projectDir, @"SGVVersionInfo.cs" );
            }
        }

        /// <summary>
        /// Creates or updates the SGVVersionInfo.cs file in this project.
        /// </summary>
        public void CreateOrUpdateSGVVersionInfoFile()
        {
            string f = SGVVersionInfoFile;
            if( f == null )
            {
                _ctx.Logger.Warn( "File SGVVersionInfo.cs not found. Creating it." );
                f = TheoreticalSGVVersionInfoFile;
            }
            string text = _ctx.RepositoryInfo.BuildAssemblyVersionAttributesFile( "'sgv prebuild'" );
            File.WriteAllText( f, text );
        }


        /// <summary>
        /// Updates the project.json file with the specified version.
        /// </summary>
        /// <param name="version">The version to inject.</param>
        /// <returns>True if the file has actually been modified. False if they are the same or the project file can not be updated.</returns>
        public bool UpdateProjectJSONFile( string version )
        {
            string text = File.ReadAllText( _projectFile );
            ProjectFileContent content = new ProjectFileContent( text, _ctx.ContainsProject );
            if( content.Version == null ) _ctx.Logger.Warn( "Unable to update version in: " + _projectFile );
            else if( content.Version == version )
            {
                _ctx.Logger.Trace( "(File is up to date.)" );
            }
            else
            {
                string modified = content.GetReplacedText( version );
                if( _projectFileCache == null )
                {
                    _projectFileCache = text;
                    string fLock = _projectDir + "project.lock.json";
                    if( File.Exists( fLock ) ) _projectLockFileCache = File.ReadAllText( fLock );
                }
                File.WriteAllText( _projectFile, modified );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restores the project.json and project.lock.json files (and project.lock.json) if <see cref="UpdateProjectJSONFile(string)"/>
        /// has been called.
        /// </summary>
        /// <returns>True if the files have been actually restored. False otherwise.</returns>
        public bool RestoreProjectJSONFile()
        {
            if( _projectFileCache != null )
            {
                File.WriteAllText( _projectFile, _projectFileCache );
                if( _projectLockFileCache != null ) File.WriteAllText( _projectDir + "project.lock.json", _projectLockFileCache );
                _projectFileCache = _projectLockFileCache = null;
                return true;
            }
            return false;
        }
    }
}

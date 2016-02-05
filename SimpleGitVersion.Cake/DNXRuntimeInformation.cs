using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Captures current DNX runtime information.
    /// This is used by <see cref="DNXSupport.DNXRun"/> to switch to another runtime if needed.
    /// </summary>
    public class DNXRuntimeInformation
    {
        readonly string _runtimePath;
        readonly string _fullRuntime;
        readonly string _version;
        readonly string _architecture;
        readonly string _runtime;
        readonly string _operatingSystem;

        /// <summary>
        /// Initializes a new DNX information based on the current dnx path
        /// from the ful path to dnx.exe application (can be null if not found: <see cref="IsValid"/> will 
        /// be false).
        /// </summary>
        /// <param name="dnxExeFullPath">Full path of the dnx.exe application.</param>
        public DNXRuntimeInformation( string dnxExeFullPath )
        {
            if( !string.IsNullOrEmpty( dnxExeFullPath ) )
            {
                _runtimePath = Path.GetDirectoryName( dnxExeFullPath );
                _fullRuntime = Path.GetFileName( Path.GetDirectoryName( _runtimePath ) );
                int idxVersion = _fullRuntime.IndexOf( '.' );
                _version = _fullRuntime.Substring( idxVersion + 1 );
                string[] split = _fullRuntime.Substring( 0, idxVersion ).Split( '-' );
                _runtime = split[1];
                _operatingSystem = split[2];
                _architecture = split[3];
            }
        }

        /// <summary>
        /// Gets whether this runtime information is valid.
        /// </summary>
        public bool IsValid { get { return _runtimePath != null; } }

        /// <summary>
        /// Gets the runtime path: the [runtime]/bin folder.
        /// </summary>
        public string RuntimePath { get { return _runtimePath; } }

        /// <summary>
        /// Gets the full runtime name: like "dnx-clr-win-x86.1.0.0-rc".
        /// </summary>
        public string FullRuntime { get { return _fullRuntime; } }

        /// <summary>
        /// Gets the runtime version string (ie. "1.0.0-rc")
        /// </summary>
        public string Version { get { return _version; } }

        /// <summary>
        /// Gets the architecture (ie. "x86").
        /// </summary>
        public string Architecture { get { return _architecture; } }

        /// <summary>
        /// Gets the runtime (ie. "clr", "coreclr", etc.).
        /// </summary>
        public string Runtime { get { return _runtime; } }

        /// <summary>
        /// Gets the OS (ie. "win").
        /// </summary>
        public string OperatingSystem { get { return _operatingSystem; } }

    }
}

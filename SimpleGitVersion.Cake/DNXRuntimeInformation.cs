using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    public class DNXRuntimeInformation
    {
        readonly string _runtimePath;
        readonly string _fullRuntime;
        readonly string _version;
        readonly string _architecture;
        readonly string _runtime;
        readonly string _operatingSystem;

        /// <summary>
        /// Initializes a new DNX information based on the current dnx path.
        /// </summary>
        /// <param name="dnxPath">Path to the dnx application.</param>
        public DNXRuntimeInformation( string dnxPath )
        {
            if( !string.IsNullOrEmpty( dnxPath ) )
            {
                _runtimePath = Path.GetDirectoryName( dnxPath );
                _fullRuntime = Path.GetFileName( Path.GetDirectoryName( _runtimePath ) );
                int idxVersion = _fullRuntime.IndexOf( '.' );
                _version = _fullRuntime.Substring( idxVersion + 1 );
                string[] split = _fullRuntime.Substring( 0, idxVersion ).Split( '-' );
                _runtime = split[1];
                _operatingSystem = split[2];
                _architecture = split[3];
            }
        }

        public bool IsValid { get { return _runtimePath != null; } }

        public string RuntimePath { get { return _runtimePath; } }

        public string FullRuntime { get { return _fullRuntime; } }

        public string Version { get { return _version; } }

        public string Architecture { get { return _architecture; } }

        public string Runtime { get { return _runtime; } }

        public string OperatingSystem { get { return _operatingSystem; } }

    }
}

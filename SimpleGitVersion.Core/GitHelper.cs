using System;
using System.IO;
using LibGit2Sharp;
using System.Runtime.InteropServices;

namespace SimpleGitVersion
{
    class GitHelper
    {
        public static Repository LoadFromPath( string path )
        {
            using( new DllPath() )
            {
                path = Path.GetFullPath( path );
                var gitDir = Repository.Discover( path );
                return gitDir != null ? new Repository( gitDir ) : null;
            }
        }

        class DllPath : IDisposable
        {
            [DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
            static extern bool SetDllDirectory( string lpPathName );

            public DllPath()
            {
                // SimpleGitVersion.Core.dll is in a lib/net45/
                // The native dlls are in NativeBinaries/(amd64|x86)/
                string sgvPackagepath = Path.GetDirectoryName( new Uri( typeof( SimpleRepositoryInfo ).Assembly.CodeBase ).LocalPath );
                sgvPackagepath = Path.GetDirectoryName( Path.GetDirectoryName( sgvPackagepath ) );
                string currentArchSubPath = "NativeBinaries/" + (IntPtr.Size == 8 ? "amd64" : "x86");
                string binPath = Path.Combine( sgvPackagepath, currentArchSubPath );
                SetDllDirectory( binPath );
            }

            public void Dispose()
            {
                SetDllDirectory( null );
            }
        }
    }
}
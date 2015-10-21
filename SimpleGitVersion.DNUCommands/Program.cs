using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleGitVersion.DNUCommands
{
    public class Program
    {
        public Program( IRuntimeEnvironment runtimeEnv )
        {
        }
        class Logger : ILogger
        {
            readonly bool _verbose;

            public Logger( bool verbose )
            {
                _verbose = verbose;
            }

            public void Error( string msg )
            {
                Console.Write( "Error: " );
                Console.WriteLine( msg );
            }

            public void Warn( string msg )
            {
                Console.Write( "Warning: " );
                Console.WriteLine( msg );
            }

            public void Info( string msg )
            {
                Console.Write( "Info: " );
                Console.WriteLine( msg );
            }

            public void Trace( string msg )
            {
                if( _verbose )
                {
                    Console.Write( "Trace: " );
                    Console.WriteLine( msg );
                }
            }
        }

        public int Main( string[] args )
        {
            try
            {
                var app = new CommandLineApplication();
                app.Name = "sgv";
                app.Description = "SimpleGitVersion commands.";
                app.ShortVersionGetter = GetVersion;
                app.LongVersionGetter = GetInformationalVersion;
                app.HelpOption( "-?|-h|--help" );
                var optVerbose = app.Option( "-v|--verbose", "Verbose output", CommandOptionType.NoValue );

                app.Command( "update", c =>
                {
                    c.Description = "Updates project.json version information from Git.";
                    var optionProject = c.Option( "-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue );
                    c.HelpOption( "-?|-h|--help" );

                    c.OnExecute( () =>
                    {
                        var projectPath = optionProject.Value() ?? Directory.GetCurrentDirectory();
                        var logger = new Logger( optVerbose.HasValue() );
                        string solutionDir = FindSiblingDirectoryAbove( projectPath, ".git" );
                        if( solutionDir == null ) logger.Error( ".git directory not found." );
                        else
                        {
                            solutionDir = solutionDir.Remove( solutionDir.Length - 4 );
                            var projectFiles = Directory.GetFiles( solutionDir, "project.json", SearchOption.AllDirectories );
                            SimpleRepositoryInfo info = SimpleRepositoryInfo.LoadFromPath( logger, solutionDir, ( log, hasRepoXml, options ) =>
                            {
                                options.IgnoreModifiedFiles.UnionWith( projectFiles.Select( p => p.Substring( solutionDir.Length ) ) );
                            } );
                            string version = info.IsValid ? info.SemVer : "0.0.0-Absolutely-Invalid";
                            foreach( var f in projectFiles )
                            {
                                UpdateProjectFile( f, version );
                            }
                        }
                        return 0;
                    } );
                } );

                // Show help information if no subcommand/option was specified.
                app.OnExecute( () =>
                {
                    app.ShowHelp();
                    return 2;
                } );

                return app.Execute( args );
            }
            catch( Exception exception )
            {
                Console.WriteLine( "Error: {0}",  exception.Message );
                return 1;
            }
        }

        static readonly Regex _rVersion = new Regex( @"""version""\s*:\s*"".*?""", RegexOptions.Compiled|RegexOptions.CultureInvariant );
        private static void UpdateProjectFile( string f, string version )
        {
            string text = File.ReadAllText( f );
            File.WriteAllText( f, _rVersion.Replace( text, @"""version"": """ + version + @"""" ) );
        }

        private static string GetInformationalVersion()
        {
            return "0.0.0.0";
        }

        private static string GetVersion()
        {
            return "0.0.0.0";
        }

        /// <summary>
        /// Finds a named directory above or next to the specified <paramref name="start"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="start">Starting directory.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>Null if not found, otherwise the path of the directory.</returns>
        static string FindSiblingDirectoryAbove( string start, string directoryName )
        {
            if( start == null ) throw new ArgumentNullException( "start" );
            if( directoryName == null ) throw new ArgumentNullException( "directortyName" );
            string p = Path.GetDirectoryName( start );
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

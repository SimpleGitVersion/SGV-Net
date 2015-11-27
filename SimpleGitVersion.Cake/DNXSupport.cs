using Cake.Core;
using Cake.Core.Diagnostics;
using System;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Code.Cake
{
    public static class DNXSupport
    {
        /// <summary>
        /// Runs cmd.exe with a command line and throws an exception if the command exits with a result that is not 0.
        /// This is currently private but may be exposed once.
        /// </summary>
        /// <param name="context">The cake context.</param>
        /// <param name="commandLine">The command line to execute.</param>
        static void RunSuccessfullCmd( this ICakeContext context, string commandLine )
        {
            int r = RunCmd( context, commandLine );
            if( r != 0 ) throw new Exception( "An error occured in command: " + commandLine );
        }

        /// <summary>
        /// Runs cmd.exe with a command line and returns the process result value.
        /// This is currently private but may be exposed once.
        /// </summary>
        /// <param name="context">The cake context.</param>
        /// <param name="commandLine">The command line to execute.</param>
        /// <param name="output">Optional standard output collector.</param>
        static int RunCmd( this ICakeContext context, string commandLine, StringBuilder output = null )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.FileName = @"cmd.exe";
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError = true;
            cmdStartInfo.RedirectStandardInput = true;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;

            Process cmdProcess = new Process();
            cmdProcess.StartInfo = cmdStartInfo;
            cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) context.Log.Error( e.Data ); };
            cmdProcess.OutputDataReceived += ( o, e ) => 
            {
                if( e.Data != null )
                {
                    if( e.Data.Length > 0 ) context.Log.Information( e.Data );
                    if( output != null ) output.AppendLine( e.Data );
                }
            };
            cmdProcess.EnableRaisingEvents = true;
            cmdProcess.Start();
            cmdProcess.BeginOutputReadLine();
            cmdProcess.BeginErrorReadLine();
            cmdProcess.StandardInput.WriteLine( commandLine );
            cmdProcess.StandardInput.WriteLine( "exit" );
            cmdProcess.WaitForExit();
            return cmdProcess.ExitCode;
        }

        /// <summary>
        /// Runs dnu restore.
        /// </summary>
        /// <param name="context">This cake context.</param>
        /// <param name="config">The configuration to use.</param>
        public static void DNURestore( this ICakeContext context, Action<DNURestoreSettings> config )
        {
            var c = new DNURestoreSettings();
            config( c );
            var b = new StringBuilder();
            b.Append( "dnu " );
            c.ToString( b );
            RunSuccessfullCmd( context, b.ToString() );
        }

        public static void DNUBuild( this ICakeContext context, Action<DNUBuildSettings> config )
        {
            var c = new DNUBuildSettings();
            config( c );
            var b = new StringBuilder();
            b.Append( "dnu " );
            c.ToString( b );
            RunSuccessfullCmd( context, b.ToString() );
        }

        public static void DNXRun( this ICakeContext context, Action<DNXRunSettings> config )
        {
            var c = new DNXRunSettings();
            config( c );
            var b = new StringBuilder();
            var current = GetDNXRuntimeInformation( context );
            if( c.EstimatedRuntime != null )
            {
                if( c.EstimatedRuntime != current.Runtime )
                {
                    b.Append( "dnvm use " ).Append( current.Version ).Append( " -r " ).Append( c.EstimatedRuntime ).Append( " && " );
                }
            }
            b.Append( "dnx " );
            c.ToString( b );
            RunSuccessfullCmd( context, b.ToString() );
        }

        static DNXRuntimeInformation LoadDNXRuntimeInformation( ICakeContext context )
        {
            StringBuilder output = new StringBuilder();
            if( RunCmd( context, "where dnx", output ) != 0 ) return new DNXRuntimeInformation( null );
            return new DNXRuntimeInformation( Path.GetDirectoryName( output.ToString().Trim() ) );
        }

        static DNXRuntimeInformation _dnxRTI;

        static DNXRuntimeInformation GetDNXRuntimeInformation( ICakeContext context )
        {
            return _dnxRTI ?? (_dnxRTI = LoadDNXRuntimeInformation( context ));
        }

    }
}

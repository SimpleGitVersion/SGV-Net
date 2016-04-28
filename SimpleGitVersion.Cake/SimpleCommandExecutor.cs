using Cake.Core;
using Cake.Core.Diagnostics;
using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Code.Cake
{
    /// <summary>
    /// Internal helpers.
    /// </summary>
    internal static class SimpleCommandExecutor
    {
        /// <summary>
        /// Runs cmd.exe with a command line and throws an exception if the command exits with a result that is not 0.
        /// This is currently private but may be exposed once.
        /// </summary>
        /// <param name="context">The cake context.</param>
        /// <param name="commandLine">The command line to execute.</param>
        static public void RunSuccessfullCmd( this ICakeContext context, string commandLine )
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
        /// <param name="output">Optional standard output lines collector.</param>
        static public int RunCmd( this ICakeContext context, string commandLine, Action<string> output = null )
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.FileName = @"cmd.exe";
            cmdStartInfo.Arguments = "/C " + commandLine;
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError = true;
            cmdStartInfo.RedirectStandardInput = true;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;

            using( Process cmdProcess = new Process() )
            {
                cmdProcess.StartInfo = cmdStartInfo;
                cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) context.Log.Error( e.Data ); };
                cmdProcess.OutputDataReceived += ( o, e ) =>
                {
                    if( e.Data != null )
                    {
                        context.Log.Information( e.Data );
                        if( output != null ) output( e.Data );
                    }
                };
                cmdProcess.Start();
                cmdProcess.BeginErrorReadLine();
                cmdProcess.BeginOutputReadLine();
                cmdProcess.WaitForExit();
                return cmdProcess.ExitCode;
            }
        }

    }
}

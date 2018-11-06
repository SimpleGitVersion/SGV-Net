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
    /// Small helper that uses cmd.exe to execute commands.
    /// </summary>
    public static class RunCmdAliases
    {
        /// <summary>
        /// Runs cmd.exe with a command line and throws an exception if the command exits with a result that is not 0.
        /// The command line contains both the executable and its parameters.
        /// </summary>
        /// <param name="context">The cake context.</param>
        /// <param name="commandLine">The command line to execute.</param>
        static public void RunCmdSuccessful( this ICakeContext context, string commandLine )
        {
            int r = RunCmd( context, commandLine );
            if( r != 0 ) throw new Exception( "An error occured in command: " + commandLine );
        }

        /// <summary>
        /// Runs cmd.exe with a command line and returns the process result value.
        /// The command line contains both the executable and its parameters.
        /// </summary>
        /// <param name="context">The cake context.</param>
        /// <param name="commandLine">The command line to execute.</param>
        /// <param name="output">Optional standard output lines collector.</param>
        /// <returns>The command exit code. Typically 0 on succes...</returns>
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
                StringBuilder stdError = new StringBuilder();
                cmdProcess.StartInfo = cmdStartInfo;
                cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) stdError.Append( "<STDERR>" ).AppendLine( e.Data ); };
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
                if( stdError.Length > 0 ) context.Log.Error( stdError.ToString() );
                return cmdProcess.ExitCode;
            }
        }

    }
}

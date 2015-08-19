using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SimpleGitVersion
{
    public static class TaskExtensions
    {
        public static void LogError( this ITask @this, string message, string file = null )
        {
            @this.BuildEngine.LogErrorEvent( new BuildErrorEventArgs( String.Empty, String.Empty, file, 0, 0, 0, 0, message, String.Empty, "SimpleGitVersionTask" ) );
        }

        public static void LogWarning( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogWarningEvent( new BuildWarningEventArgs( string.Empty, String.Empty, null, 0, 0, 0, 0, String.Format( message, parameters ), String.Empty, "SimpleGitVersionTask" ) );
        }

        public static void LogInfo( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogMessageEvent( new BuildMessageEventArgs( String.Format( message, parameters ), String.Empty, "SimpleGitVersionTask", MessageImportance.High ) );
        }

        public static void LogTrace( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogMessageEvent( new BuildMessageEventArgs( String.Format( message, parameters ), String.Empty, "SimpleGitVersionTask", MessageImportance.Normal ) );
        }

    }

}
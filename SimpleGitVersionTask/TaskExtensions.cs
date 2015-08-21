using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SimpleGitVersion
{
    public static class TaskExtensions
    {
        public static void LogError( this ITask @this, string message, string file = null )
        {
            @this.BuildEngine.LogErrorEvent( new BuildErrorEventArgs( string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "SimpleGitVersionTask" ) );
        }

        public static void LogWarning( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogWarningEvent( new BuildWarningEventArgs( string.Empty, string.Empty, null, 0, 0, 0, 0, string.Format( message, parameters ), string.Empty, "SimpleGitVersionTask" ) );
        }

        public static void LogInfo( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogMessageEvent( new BuildMessageEventArgs( string.Format( message, parameters ), string.Empty, "SimpleGitVersionTask", MessageImportance.High ) );
        }

        public static void LogTrace( this ITask @this, string message, params object[] parameters )
        {
            @this.BuildEngine.LogMessageEvent( new BuildMessageEventArgs( string.Format( message, parameters ), string.Empty, "SimpleGitVersionTask", MessageImportance.Normal ) );
        }

    }

}
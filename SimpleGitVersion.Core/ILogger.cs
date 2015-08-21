using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    /// <summary>
    /// Simple logger abstraction.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="msg">Error message.</param>
        void Error( string msg );
        
        /// <summary>
        /// logs a warning.
        /// </summary>
        /// <param name="msg">Warning message.</param>
        void Warn( string msg );

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="msg">Information message.</param>
        void Info( string msg );

        /// <summary>
        /// Logs a trace.
        /// </summary>
        /// <param name="msg">Trace message.</param>
        void Trace( string msg );
    }

    static class ILoggerExtensions
    {
        static public void Error( this ILogger @this, string msg, params object[] parameters )
        {
            @this.Error( string.Format( msg, parameters ) );
        }
        static public void Warn( this ILogger @this, string msg, params object[] parameters )
        {
            @this.Warn( string.Format( msg, parameters ) );
        }
        static public void Info( this ILogger @this, string msg, params object[] parameters )
        {
            @this.Info( string.Format( msg, parameters ) );
        }
        static public void Trace( this ILogger @this, string msg, params object[] parameters )
        {
            @this.Trace( string.Format( msg, parameters ) );
        }
    }
}

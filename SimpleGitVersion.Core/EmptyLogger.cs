using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{

    /// <summary>
    /// Empty object pattern.
    /// </summary>
    public class EmptyLogger : ILogger
    {
        /// <summary>
        /// The empty logger to use.
        /// </summary>
        public static readonly ILogger Empty = new EmptyLogger();

        void ILogger.Error( string msg )
        {
        }

        void ILogger.Warn( string msg )
        {
        }

        void ILogger.Info( string msg )
        {
        }

        void ILogger.Trace( string msg )
        {
        }
    }
}

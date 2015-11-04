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
        public static readonly ILogger Empty = new EmptyLogger();

        public void Error( string msg )
        {
        }

        public void Warn( string msg )
        {
        }

        public void Info( string msg )
        {
        }

        public void Trace( string msg )
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.Core.Tests
{
    public class ConsoleLogger : ILogger
    {
        public void Error( string msg )
        {
            Console.WriteLine( "Error: " + msg );
        }
        public void Warn( string msg )
        {
            Console.WriteLine( "Warn: " + msg );
        }

        public void Info( string msg )
        {
            Console.WriteLine( "Info: " + msg );
        }

        public void Trace( string msg )
        {
            Console.WriteLine( "Trace: " + msg );
        }

    }
}

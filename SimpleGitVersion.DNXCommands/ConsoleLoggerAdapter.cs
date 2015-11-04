using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands
{
    class ConsoleLoggerAdapter : ILogger
    {
        bool _verbose;

        public ConsoleLoggerAdapter( bool verbose = false )
        {
            _verbose = verbose;
        }

        public bool Verbose { get { return _verbose; } set { _verbose = value; } }

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

}

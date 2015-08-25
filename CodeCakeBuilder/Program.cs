using Code.Cake;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    class Program
    {
        static int Main( string[] args )
        {
            var app = new CodeCakeApplication();
            bool interactive = !args.Contains( "-nointeractive", StringComparer.OrdinalIgnoreCase );
            int result = app.Run( args );
            Console.WriteLine();
            if( interactive )
            {
                Console.WriteLine( "Hit any key to exit. (Use -nointeractive parameter to exit immediately)" );
                Console.ReadKey();
            }
            return result;
        }
    }
}

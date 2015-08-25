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
        static void Main( string[] args )
        {
            var app = new CodeCakeApplication();
            var a = new List<string>( args );
            int idx = a.FindIndex( s => s.Equals( "-nowait", StringComparison.OrdinalIgnoreCase ) );
            if( idx >= 0 ) a.RemoveAt( idx );
            app.Run( a.ToArray() );
            Console.WriteLine();
            if( idx < 0 )
            {
                Console.WriteLine( "Hit any key to exit. (Use -nowait parameter to end immediately)" );
                Console.ReadKey();
            }
        }
    }
}

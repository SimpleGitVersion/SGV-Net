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
            app.Run( args );
            // From: http://stackoverflow.com/questions/1188658/how-can-a-c-sharp-windows-console-application-tell-if-it-is-run-interactively
            if( Console.OpenStandardInput( 1 ) != Stream.Null )
            {
                Console.WriteLine();
                Console.WriteLine( "Interactive mode detected: hit any key to exit." );
                Console.ReadKey();
            }
        }
    }
}

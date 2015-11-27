using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Code.Cake
{
    public class DNXRunSettings
    {
        public DNXRunSettings()
        {
        }

        /// <summary>
        /// Gets or sets the configuration (DEBUG, RELEASE, etc.).
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets or sets the framework version to use when running (i.e. dnx451, dnx452, dnx46, ...).
        /// Even if this option is supported by DNX only on the clr, any framework name can be set here.
        /// </summary>
        public string Framework { get; set; }

        /// <summary>
        /// Gets the runtime from the <see cref="Framework"/>.
        /// Can be null (if Framework is null), "clr" or "coreclr".
        /// </summary>
        public string EstimatedRuntime
        {
            get
            {
                if( Framework == null ) return null;
                return Regex.IsMatch( Framework, @"^(dnx|net)\d+$" ) ? "clr" : "coreclr";
            }
        }

        /// <summary>
        /// Waits for the debugger to attach before beginning execution.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Gets or sets the path to the project.json file or the application folder. 
        /// Defaults to the current folder if not provided.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// Gets or sets the Application base directory path.
        /// </summary>
        public string AppBase { get; set; }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Generates the arguments.
        /// </summary>
        /// <returns>The arguments to the dnx command.</returns>
        public override string ToString()
        {
            return ToString( new StringBuilder() ).ToString();
        }

        /// <summary>
        /// Generates the arguments.
        /// </summary>
        /// <param name="b">The string builder.</param>
        /// <returns>The string builder.</returns>
        public StringBuilder ToString( StringBuilder b )
        {
            if( Framework != null && EstimatedRuntime == "clr" )
            {
                b.Append( " --framework " ).Append( Framework );
            }
            if( Configuration != null )
            {
                b.Append( " --configuration " ).Append( Configuration.ToUpperInvariant() );
            }
            if( Debug )
            {
                b.Append( " --debug " );
            }
            if( AppBase != null )
            {
                b.Append( " --appbase " ).Append( '"' ).Append( AppBase ).Append( '"' );
            }
            if( Project != null )
            {
                b.Append( " -p " ).Append( '"' ).Append( Project ).Append( '"' );
            }
            if( Arguments != null )
            {
                b.Append( ' ' ).Append( Arguments );
            }
            return b;
        }
    }
}

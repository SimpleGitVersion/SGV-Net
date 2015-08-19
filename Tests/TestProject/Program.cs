using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{

    /// <summary>
    /// This project is here only to ease tests and tuning of the MSBuild process.
    /// The Build.cmd command can be used to trigger the calls to SimpleGitVersionTask.dll (GetGitRepositoryInfoTask and CreateAssemblyInfoTask)
    /// that are used by this TestProject.csproj.
    /// When running SimpleGitVersion.Core, this project is built: this acts as a kind of "experimental instance" to debug SimpleGitVersion.Core.
    /// </summary>
    class Program
    {
        static void Main( string[] args )
        {
            var aInfo = Assembly.GetCallingAssembly().GetName().Version;
            var fInfo = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>();
            var iInfo = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            Console.WriteLine( "Attributes:" );
            Console.WriteLine( "AssemblyVersion: {0}", aInfo );
            Console.WriteLine( "AssemblyFileVersion: {0}", fInfo != null ? fInfo.Version : "(null)" );
            Console.WriteLine( "AssemblyInformationalVersion: {0}", iInfo != null ? iInfo.InformationalVersion : "(null)" );
            Console.ReadKey();
        }
    }
}

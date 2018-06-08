using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.NUnit;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CodeCake
{
    public partial class Build
    {
        void StandardUnitTests( string configuration, IEnumerable<SolutionProject> testProjects )
        {
            var testDlls = testProjects.Select( p =>
                         new
                         {
                             CSProjPath = p.Path,
                             ProjectPath = p.Path.GetDirectory(),
                             NetCoreAppDll21 = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/netcoreapp2.1/" + p.Name + ".dll" ),
                             NetCoreAppDll20 = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/netcoreapp2.0/" + p.Name + ".dll" ),
                             Net461Dll = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/net461/" + p.Name + ".dll" ),
                             Net461Exe = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/net461/" + p.Name + ".exe" ),
                         } );

            foreach( var test in testDlls )
            {
                var net461 = Cake.FileExists( test.Net461Dll )
                                ? test.Net461Dll
                                : Cake.FileExists( test.Net461Exe )
                                    ? test.Net461Exe
                                    : null;
                if( net461 != null )
                {
                    Cake.Information( "Testing via NUnit (net461): {0}", net461 );
                    Cake.NUnit( new[] { net461 }, new NUnitSettings()
                    {
                        Framework = "v4.5",
                        ResultsFile = test.ProjectPath.CombineWithFilePath( "TestResult.Net461.xml" )
                    } );
                }
                if( Cake.FileExists( test.NetCoreAppDll20 ) ) TestNetCore( test.CSProjPath.FullPath, test.NetCoreAppDll20, "netcoreapp2.0" );
                if( Cake.FileExists( test.NetCoreAppDll21 ) ) TestNetCore( test.CSProjPath.FullPath, test.NetCoreAppDll21, "netcoreapp2.1" );
            }

            void TestNetCore( string projectPath, Cake.Core.IO.FilePath dllFilePath, string framework )
            {
                var e = XDocument.Load( projectPath ).Root;
                if( e.Descendants( "PackageReference" ).Any( r => r.Attribute( "Include" )?.Value == "Microsoft.NET.Test.Sdk" ) )
                {
                    Cake.Information( $"Testing via VSTest ({framework}): {dllFilePath}" );
                    Cake.DotNetCoreTest( projectPath, new DotNetCoreTestSettings()
                    {
                        Configuration = configuration,
                        Framework = framework,
                        NoBuild = true
                    } );
                }
                else
                {
                    Cake.Information( $"Testing via NUnitLite ({framework}): {dllFilePath}" );
                    Cake.DotNetCoreExecute( dllFilePath );
                }
            }
        }

    }
}

using System;
using System.Reflection;
using System.Runtime.InteropServices;

// Shared Assembly info: it must be added as a link to
// each project.
// It contains information that must be the same for all
// assemblies produced by this solution.


[assembly: AssemblyCompany( "Invenietis" )]
[assembly: AssemblyProduct( "SimpleGitVersion" )]
[assembly: AssemblyCopyright( "Copyright © Invenietis 2015" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]
[assembly: CLSCompliant( true )]
[assembly: ComVisible( false )]

// 0.1.0 ==> Ordered Version = 1300140001, File = 0.0.19838.36833
[assembly: AssemblyVersion( "0.0" )]
[assembly: AssemblyFileVersion( "0.0.19838.36833" )]
[assembly: AssemblyInformationalVersion( "0.1.0" )]


#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif

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
// 0.1.1 ==> Ordered Version = 1300270002, File = 0.0.19840.35762
// 0.1.2 ==> Ordered Version = 1300400003, File = 0.0.19842.34691
[assembly: AssemblyVersion( "0.1" )]
[assembly: AssemblyFileVersion( "0.0.19842.34691" )]
[assembly: AssemblyInformationalVersion( "0.1.2" )]


#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif

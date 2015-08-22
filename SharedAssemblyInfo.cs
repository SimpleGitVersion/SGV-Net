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

#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration( "Release" )]
#endif

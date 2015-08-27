using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using Semver;
using System.IO;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class CIBuildNameTests
    {
        [TestCase( "1.0.0" )]
        [TestCase( "1.0.0-alpha" )]
        [TestCase( "1.0.0-alpha.0.1" )]
        [TestCase( "1.0.0-alpha.1" )]
        [TestCase( "1.0.0-alpha.3" )]
        [TestCase( "1.0.0-alpha.3.4" )]
        [TestCase( "1.0.0-epsilon.4.5" )]
        [TestCase( "1.0.0-rc.99.99" )]
        [TestCase( "1.0.1" )]
        [TestCase( "1.0.9999" )]
        public void CIBuildVersion_LastReleaseBased_are_correctely_ordered( string tag )
        {
            ReleaseTagFormat formatV2 = ReleaseTagFormat.NugetPackageV2;

            var t = ReleaseTagVersion.TryParse( tag );
            var v = SemVersion.Parse( t.ToString( ReleaseTagFormat.SemVer ), true );
            var tNext = new ReleaseTagVersion( t.OrderedVersion + 1 );
            var vNext = SemVersion.Parse( tNext.ToString( ReleaseTagFormat.SemVer ), true );
            var tPrev = new ReleaseTagVersion( t.OrderedVersion - 1 );
            var vPrev = SemVersion.Parse( tPrev.ToString( ReleaseTagFormat.SemVer ), true );
            Assert.That( vPrev < v, "{0} < {1}", vPrev, v );
            Assert.That( v < vNext, "{0} < {1}", v, vNext );
            
            var sNuGet = t.ToString( formatV2 );
            var sNuGetPrev = tPrev.ToString( formatV2 );
            var sNuGetNext = tNext.ToString( formatV2 );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetPrev, sNuGet ) < 0, "{0} < {1}", sNuGetPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGet, sNuGetNext ) < 0, "{0} < {1}", sNuGet, sNuGetNext );


            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = "dev", BuildIndex = 1 };

            string sCI =  t.ToString( ReleaseTagFormat.SemVer, ci );
            SemVersion vCi = SemVersion.Parse( sCI, true );
            Assert.That( v < vCi, "{0} < {1}", v, vCi );
            Assert.That( vCi < vNext, "{0} < {1}", vCi, vNext );

            var sNuGetCI = t.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGet, sNuGetCI ) < 0, "{0} < {1}", sNuGet, sNuGetCI );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCI, sNuGetNext ) < 0, "{0} < {1}", sNuGetCI, sNuGetNext );
            
            string sCiNext = tNext.ToString( ReleaseTagFormat.SemVer, ci );
            SemVersion vCiNext = SemVersion.Parse( sCiNext, true );
            Assert.That( vCiNext > vCi, "{0} > {1}", vCiNext, vCi );
            Assert.That( vCiNext > vNext, "{0} > {1}", vCiNext, vNext );

            var sNuGetCINext = tNext.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCINext, sNuGetCI ) > 0, "{0} > {1}", sNuGetCINext, sNuGetCI );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCINext, sNuGetNext ) > 0, "{0} > {1}", sNuGetCINext, sNuGetNext );

            string sCiPrev = tPrev.ToString( ReleaseTagFormat.SemVer, ci );
            SemVersion vCiPrev = SemVersion.Parse( sCiPrev, true );
            Assert.That( vCiPrev > vPrev, "{0} > {1}", vCiPrev, vPrev );
            Assert.That( vCiPrev < v, "{0} < {1}", vCiPrev, v );
            Assert.That( vCiPrev < vCiNext, "{0} < {1}", vCiPrev, vCiNext );

            var sNuGetCIPrev = tPrev.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCIPrev, sNuGetPrev ) > 0, "{0} > {1}", sNuGetCIPrev, sNuGetPrev );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCIPrev,  sNuGet ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.Default.Compare( sNuGetCIPrev, sNuGetCINext ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGetCINext );
        }

    }
}

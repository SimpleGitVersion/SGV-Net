using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using Semver;
using System.IO;
using CSemVer;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class CIBuildNameTests
    {

        [Explicit]
        [TestCase( "v0.4.1-rc.2.1", "0.4.1-rc.2.2, 0.4.1-rc.3, 0.4.2" )]
        [TestCase( "v3.2.1-rc.1", "3.2.1-rc.1.1, 3.2.1-rc.2, 3.2.2" )]
        [TestCase( "v3.2.1-beta", "3.2.1-beta.1, 3.2.1-beta.0.1, 3.2.1-chi, 3.2.1-beta.5, 3.2.2" )]
        [TestCase( "v1.2.3", "1.2.4-alpha, 1.2.4-alpha.0.1, 1.2.4" )]
        public void display_versions_and_CI_version( string version, string after )
        {
            var buildInfo = new CIBuildDescriptor() { BranchName = "develop", BuildIndex = 15 };
            CSVersion v = CSVersion.TryParse( version );
            string vCI = v.ToString( CSVersionFormat.SemVer, buildInfo );
            CSVersion vNext = new CSVersion( v.OrderedVersion + 1 );

            Console.WriteLine( "Version = {0}, CI = {1}, Next = {2}", v, vCI, vNext );

            var vSemVer = SemVersion.Parse( v.ToString( CSVersionFormat.SemVer ) );
            var vCISemVer = SemVersion.Parse( vCI );
            var vNextSemVer = SemVersion.Parse( vNext.ToString( CSVersionFormat.SemVer ) );
            Assert.That( vSemVer < vCISemVer, "{0} < {1}", vSemVer, vCISemVer );
            Assert.That( vCISemVer < vNextSemVer, "{0} < {1}", vCISemVer, vNextSemVer );

            foreach( var vAfter in after.Split( ',' ).Select( s => SemVersion.Parse( s.Trim() )) ) 
            {
                Assert.That( vAfter.CompareTo( vCISemVer ) > 0, "{0} > {1}", vAfter, vCISemVer );
            }
        }


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
            CSVersionFormat formatV2 = CSVersionFormat.NugetPackageV2;

            var t = CSVersion.TryParse( tag );
            var v = SemVersion.Parse( t.ToString( CSVersionFormat.SemVer ), true );
            var tNext = new CSVersion( t.OrderedVersion + 1 );
            var vNext = SemVersion.Parse( tNext.ToString( CSVersionFormat.SemVer ), true );
            var tPrev = new CSVersion( t.OrderedVersion - 1 );
            var vPrev = SemVersion.Parse( tPrev.ToString( CSVersionFormat.SemVer ), true );
            Assert.That( vPrev < v, "{0} < {1}", vPrev, v );
            Assert.That( v < vNext, "{0} < {1}", v, vNext );
            
            var sNuGet = t.ToString( formatV2 );
            var sNuGetPrev = tPrev.ToString( formatV2 );
            var sNuGetNext = tNext.ToString( formatV2 );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetPrev, sNuGet ) < 0, "{0} < {1}", sNuGetPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetNext ) < 0, "{0} < {1}", sNuGet, sNuGetNext );


            CIBuildDescriptor ci = new CIBuildDescriptor { BranchName = "dev", BuildIndex = 1 };

            string sCI =  t.ToString( CSVersionFormat.SemVer, ci );
            SemVersion vCi = SemVersion.Parse( sCI, true );
            Assert.That( v < vCi, "{0} < {1}", v, vCi );
            Assert.That( vCi < vNext, "{0} < {1}", vCi, vNext );

            var sNuGetCI = t.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGet, sNuGetCI ) < 0, "{0} < {1}", sNuGet, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCI, sNuGetNext ) < 0, "{0} < {1}", sNuGetCI, sNuGetNext );
            
            string sCiNext = tNext.ToString( CSVersionFormat.SemVer, ci );
            SemVersion vCiNext = SemVersion.Parse( sCiNext, true );
            Assert.That( vCiNext > vCi, "{0} > {1}", vCiNext, vCi );
            Assert.That( vCiNext > vNext, "{0} > {1}", vCiNext, vNext );

            var sNuGetCINext = tNext.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetCI ) > 0, "{0} > {1}", sNuGetCINext, sNuGetCI );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCINext, sNuGetNext ) > 0, "{0} > {1}", sNuGetCINext, sNuGetNext );

            string sCiPrev = tPrev.ToString( CSVersionFormat.SemVer, ci );
            SemVersion vCiPrev = SemVersion.Parse( sCiPrev, true );
            Assert.That( vCiPrev > vPrev, "{0} > {1}", vCiPrev, vPrev );
            Assert.That( vCiPrev < v, "{0} < {1}", vCiPrev, v );
            Assert.That( vCiPrev < vCiNext, "{0} < {1}", vCiPrev, vCiNext );

            var sNuGetCIPrev = tPrev.ToString( formatV2, ci );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetPrev ) > 0, "{0} > {1}", sNuGetCIPrev, sNuGetPrev );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev,  sNuGet ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGet );
            Assert.That( NuGetV2StringComparer.DefaultComparer.Compare( sNuGetCIPrev, sNuGetCINext ) < 0, "{0} < {1}", sNuGetCIPrev, sNuGetCINext );
        }

    }
}

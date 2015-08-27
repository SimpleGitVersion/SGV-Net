using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Semver;

namespace SimpleGitVersion.Core.Tests
{
    [TestFixture]
    public class NuGetV2NameTests
    {

        [TestCase( "0.0.0-alpha" )]
        [TestCase( "0.0.0-alpha.1" )]
        [TestCase( "0.0.0-alpha.2" )]
        [TestCase( "0.0.0-alpha.0.1" )]
        [TestCase( "1.0.0-rc" )]
        [TestCase( "99999.99999.9999" )]
        public void display_name_and_successors_samples( string v )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( v );
            var succ = t.GetDirectSuccessors( false );

            Console.WriteLine( " -> - found {0} successors for '{1}' (NuGetV2 = {2}, Ordered Version = {3}, File = {4}.{5}.{6}.{7}):",
                                succ.Count(),
                                t,
                                t.ToString( ReleaseTagFormat.NugetPackageV2 ),
                                t.OrderedVersion,
                                t.OrderedVersionMajor,
                                t.OrderedVersionMinor,
                                t.OrderedVersionBuild,
                                t.OrderedVersionRevision
                                );
            Console.WriteLine( "      " + string.Join( ", ", succ.Select( s => s.ToString() ) ) );
        }

        [TestCase( "0.0.0-alpha" )]
        [TestCase( "0.0.0-alpha.1" )]
        [TestCase( "0.0.0-alpha.2" )]
        [TestCase( "0.0.0-alpha.0.1" )]
        [TestCase( "1.0.0-prerelease" )]
        [TestCase( "99999.99999.9999" )]
        public void display_name_for_CI_build_and_check_20_characters_limit( string v )
        {
            var buildInfo = new CIBuildDescriptor() { BranchName = "dev", BuildIndex = 21 };
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( v );
            var nugetV2 = t.ToString( ReleaseTagFormat.NugetPackageV2, buildInfo );
            Console.WriteLine( "SemVer = {0}, NuGetV2 = {1}, Ordered Version = {2}, File = {3}.{4}.{5}.{6}):",
                                t.ToString( ReleaseTagFormat.SemVer, buildInfo ),
                                nugetV2,
                                t.OrderedVersion,
                                t.OrderedVersionMajor,
                                t.OrderedVersionMinor,
                                t.OrderedVersionBuild,
                                t.OrderedVersionRevision
                                );
            Assert.That( SemVersion.Parse( nugetV2 ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );
        }

        [TestCase( "0.0.0-alpha", "0.0.0-alpha" )]
        [TestCase( "3.0.1-beta.12", "3.0.1-beta-12" )]
        [TestCase( "3.0.1-chi.25", "3.0.1-chi-25" )]
        [TestCase( "3.0.1-delta.1", "3.0.1-delta-01" )]
        [TestCase( "3.0.1-epsilon.18", "3.0.1-epsilon-18" )]
        [TestCase( "3.0.1-gamma.19", "3.0.1-gamma-19" )]
        [TestCase( "3.0.1-iota.20", "3.0.1-iota-20" )]
        [TestCase( "3.0.1-kappa.21", "3.0.1-kappa-21" )]
        [TestCase( "3.0.1-lambda.22", "3.0.1-lambda-22" )]
        [TestCase( "3.0.1-mu.23", "3.0.1-mu-23" )]
        [TestCase( "3.0.1-omicron.24", "3.0.1-omicron-24" )]
        [TestCase( "3.0.1-prerelease.24", "3.0.1-pre-24" )]
        [TestCase( "99999.99999.9999-rc.99", "99999.99999.9999-rc-99" )]
        public void pre_release_with_standard_names_nugetV2_mappings( string tag, string nuget )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsPreRelease );
            Assert.That( t.IsPreReleaseNameStandard );
            Assert.That( t.IsPreReleaseFix, Is.False );
            Assert.That( t.ToString( ReleaseTagFormat.SemVer ), Is.EqualTo( tag ) );
            Assert.That( t.ToString( ReleaseTagFormat.NuGetPackage ), Is.EqualTo( nuget ) );
            Assert.That( SemVersion.Parse( nuget ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );
        }

        [TestCase( "0.0.0-epsilon.5.1", "0.0.0-epsilon-05-01" )]
        [TestCase( "0.0.0-prerelease.5.1", "0.0.0-pre-05-01" )]
        public void ensures_that_longest_nugetV2_special_name_is_less_than_20_characters_long( string tag, string nuget )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsPreRelease );
            Assert.That( t.IsPreReleaseNameStandard );
            Assert.That( t.ToString( ReleaseTagFormat.SemVer ), Is.EqualTo( tag ) );
            Assert.That( t.ToString( ReleaseTagFormat.NuGetPackage ), Is.EqualTo( nuget ) );
            Assert.That( SemVersion.Parse( nuget ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );
        }

        [TestCase( "0.0.0-alpha.0.1", "0.0.0-alpha-00-01" )]
        [TestCase( "3.0.1-beta.12.8", "3.0.1-beta-12-08" )]
        [TestCase( "3.0.1-chi.0.1", "3.0.1-chi-00-01" )]
        [TestCase( "3.0.1-delta.1.99", "3.0.1-delta-01-99" )]
        [TestCase( "3.0.1-epsilon.18.2", "3.0.1-epsilon-18-02" )]
        [TestCase( "3.0.1-gamma.19.4", "3.0.1-gamma-19-04" )]
        [TestCase( "3.0.1-iota.1.1", "3.0.1-iota-01-01" )]
        [TestCase( "3.0.1-kappa.1.5", "3.0.1-kappa-01-05" )]
        [TestCase( "3.0.1-lambda.10.10", "3.0.1-lambda-10-10" )]
        [TestCase( "3.0.1-mu.23.23", "3.0.1-mu-23-23" )]
        [TestCase( "3.0.1-omicron.0.1", "3.0.1-omicron-00-01" )]
        [TestCase( "3.0.1-prerelease.0.1", "3.0.1-pre-00-01" )]
        [TestCase( "99999.99999.9999-rc.99.99", "99999.99999.9999-rc-99-99" )]
        public void pre_release_with_standard_names_and_fix_number_nugetV2_mappings( string tag, string nuget )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsPreRelease );
            Assert.That( t.IsPreReleaseNameStandard );
            Assert.That( t.IsPreReleaseFix );
            Assert.That( t.PreReleaseFix, Is.GreaterThan( 0 ) );
            Assert.That( t.ToString( ReleaseTagFormat.SemVer ), Is.EqualTo( tag ) );
            Assert.That( t.ToString( ReleaseTagFormat.NugetPackageV2 ), Is.EqualTo( nuget ) );
            Assert.That( SemVersion.Parse( nuget ).Prerelease.Length, Is.LessThanOrEqualTo( 20 ) );
        }

    }
}


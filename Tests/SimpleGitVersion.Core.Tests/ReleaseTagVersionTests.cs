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
    public class ReleaseTagVersionTests
    {

        [Explicit]
        [TestCase( "v0.0.0-alpha.1" )]
        [TestCase( "v0.0.0-alpha.2" )]
        [TestCase( "v0.0.0-alpha.0.1" )]
        [TestCase( "v1.0.0" )]
        [TestCase( "v1.0.1" )]
        [TestCase( "v1.1.0" )]
        [TestCase( "v1.1.0" )]
        [TestCase( "v2.0.0-rc" )]
        [TestCase( "v1.2.3" )]
        [TestCase( "v1.2.3-alpha" )]
        [TestCase( "v1.2.3-delta.5" )]
        [TestCase( "v1.2.3-prerelease.2.3" )]
        [TestCase( "v1.2.3-rc" )]
        public void display_successors_samples( string v )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( v );
            var succ = t.GetDirectSuccessors( false );

            Console.WriteLine( " -> - found {0} successors for '{1}' (Ordered Version = {2}, File = {3}):",
                                succ.Count(),
                                t,
                                t.OrderedVersion,
                                t.ToStringFileVersion( false ) );
            Console.WriteLine( "      " + string.Join( ", ", succ.Select( s => s.ToString() ) ) );

            var closest = t.GetDirectSuccessors( true ).Select( s => s.ToString() ).ToList();
            Console.WriteLine( "    - {0} next fixes:", closest.Count, t );
            Console.WriteLine( "      " + string.Join( ", ", closest ) );
        }

        [TestCase( "v0.0.0-alpha.0.999" )]
        public void fix_parsing_syntax_error_helper_max_prerelease_fix( string tag )
        {
            var error = string.Format( "Fix Number must be between 1 and {0}.", ReleaseTagVersion.MaxPreReleaseFix );

            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag, true );
            Assert.That( t.ParseErrorMessage, Contains.Substring( error ) );
        }

        [TestCase( "0.0.0" )]
        [TestCase( "3.0.1" )]
        [TestCase( "3.0.1" )]
        [TestCase( "99999.49999.9999" )]
        public void parsing_valid_release( string tag )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsPreRelease, Is.False );
            Assert.That( t.IsPreReleasePatch, Is.False );
            Assert.That( t.ToString( ReleaseTagFormat.SemVer ), Is.EqualTo( tag ) );
            Assert.That( t.ToString( ReleaseTagFormat.NuGetPackage ), Is.EqualTo( tag ) );
        }

        [TestCase( "", -1 )]
        [TestCase( "alpha", 0 )]
        [TestCase( "beta", 1 )]
        [TestCase( "delta", 2 )]
        [TestCase( "epsilon", 3 )]
        [TestCase( "gamma", 4 )]
        [TestCase( "kappa", 5 )]
        [TestCase( "A", 6 )]
        [TestCase( "Alpha", 6 )]
        [TestCase( "c", 6 )]
        [TestCase( "mmmmm", 6 )]
        [TestCase( "Rc", 6 )]
        [TestCase( "rc", 7 )]
        public void handling_pre_release_name_index( string n, int idx )
        {
            Assert.That( ReleaseTagVersion.GetPreReleaseNameIdx( n ), Is.EqualTo( idx ), n );
        }

        [TestCase( "3.0.1-ready.0.1" )]
        [TestCase( "99999.49999.9999-nonstandard.99.99" )]
        public void to_string_pre_release_with_nonstandard_names_works_for_SemVer_but_throws_for_NuGetV2( string tag )
        {
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( tag );
            Assert.That( t.IsValid );
            Assert.That( t.IsPreRelease );
            Assert.That( !t.IsPreReleaseNameStandard );
            Assert.That( t.IsPreReleasePatch );
            Assert.That( t.PreReleasePatch, Is.GreaterThan( 0 ) );
            Assert.That( t.ToString( ReleaseTagFormat.SemVer, null, true ), Is.EqualTo( tag ) );
            Assert.Throws<ArgumentException>( () => t.ToString( ReleaseTagFormat.NugetPackageV2, null, true ) );
        }


        [TestCase( "v0.0.0-alpha", 0, 0, 0, 1 )]
        [TestCase( "v0.0.0-alpha.0.1", 0, 0, 0, 2 )]
        [TestCase( "v0.0.0-alpha.0.2", 0, 0, 0, 3 )]
        [TestCase( "v0.0.0-alpha.1", 0, 0, 0, 101 )]
        [TestCase( "v0.0.0-beta", 0, 0, 0, 100 * 99 + 101 )]
        public void version_ordering_starts_at_1_for_the_very_first_possible_version( string tag, int oMajor, int oMinor, int oBuild, int oRevision )
        {
            var t = ReleaseTagVersion.TryParse( tag, true );
            Assert.That( t.IsValid );
            Assert.That( t.OrderedVersionMajor, Is.EqualTo( oMajor ) );
            Assert.That( t.OrderedVersionMinor, Is.EqualTo( oMinor ) );
            Assert.That( t.OrderedVersionBuild, Is.EqualTo( oBuild ) );
            Assert.That( t.OrderedVersionRevision, Is.EqualTo( oRevision ) );
            long vf = t.OrderedVersion << 1;
            Assert.That( t.ToStringFileVersion( false ),
                    Is.EqualTo( string.Format( "{0}.{1}.{2}.{3}", vf >> 48, (vf >> 32) & 0xFFFF, (vf >> 16) & 0xFFFF, vf & 0xFFFF ) ) );
            vf |= 1;
            Assert.That( t.ToStringFileVersion( true ),
                    Is.EqualTo( string.Format( "{0}.{1}.{2}.{3}", vf >> 48, (vf >> 32) & 0xFFFF, (vf >> 16) & 0xFFFF, vf & 0xFFFF ) ) );
        }

        [TestCase( "0", false )]
        [TestCase( "1", false )]
        [TestCase( "not", false )]
        [TestCase( "not.1", false )]
        [TestCase( "0.0", true )]
        [TestCase( "v0.0", true )]
        [TestCase( "v0.0.0.0", true )]
        [TestCase( "v0.0.alpha1", true )]
        [TestCase( "v0.0.alpha", true )]
        [TestCase( "v0.0.0-alpha.1.1.1", true )]
        [TestCase( "0.0not", true )]
        [TestCase( "v0.0.0-alpha.0", true )]
        [TestCase( "v0.0.0-alpha.5.0", true )]
        [TestCase( "v0.0.0+nop", true )]
        [TestCase( "v0.0.0-alpha+nop", true )]
        public void when_parsing_invalid_tags_can_detect_malformed_ones( string tag, bool isMalformed )
        {
            var t = ReleaseTagVersion.TryParse( tag, true );
            Assert.That( !t.IsValid );
            Assert.That( t.IsMalformed, Is.EqualTo( isMalformed ) );
            //Console.WriteLine( t.ParseErrorMessage );
        }

        [TestCase( "0", 0, "Invalid are always 0." )]
        [TestCase( "0.0", 1, "Malformed are always = 1." )]
        [TestCase( "0.0.0-nonstandard", 2, "Non standard prerelease name = 2." )]
        [TestCase( "0.0.0", 3, "Normal = 3" )]
        [TestCase( "0.0.0-gamma", 3, "Normal = 3" )]
        [TestCase( "88.88.88-nonstandard+Invalid", 4, "Invalid non standard = 4" )]
        [TestCase( "88.88.88+Invalid", 5, "Marked Invalid = 5" )]
        public void equal_release_tags_can_have_different_definition_strengths( string tag, int level, string message )
        {
            var t = ReleaseTagVersion.TryParse( tag, true );
            Assert.That( t.DefinitionStrength, Is.EqualTo( level ), message );
        }

        [TestCase( "0.0.0-alpha", false, 0 )]
        [TestCase( "0.0.0-alpha.0.1", false, 1 )]
        [TestCase( "0.0.0-alpha.0.2", false, 2 )]
        [TestCase( "0.0.0-alpha.99.99", false, 100 * 99 + 100 - 1 )]
        [TestCase( "0.0.0-beta", false, 100 * 99 + 100 )]
        [TestCase( "0.0.0-delta", false, 2 * (100 * 99 + 100) )]
        [TestCase( "0.0.0-rc", false, 7 * (100 * 99 + 100) )]
        [TestCase( "0.0.0-rc.99.99", false, 7 * (100 * 99 + 100) + 100 * 99 + 99 )]
        [TestCase( "0.0.0", false, 8 * 100 * 100 )]

        [TestCase( "0.0.1-alpha", false, (8 * 100 * 100) + 1 )]
        [TestCase( "0.0.1-alpha.0.1", false, ((8 * 100 * 100) + 1) + 1 )]
        [TestCase( "0.0.1-alpha.0.2", false, ((8 * 100 * 100) + 1) + 2 )]
        [TestCase( "0.0.1-alpha.99.99", false, ((8 * 100 * 100) + 1) + 100 * 99 + 100 - 1 )]
        [TestCase( "0.0.1-beta", false, ((8 * 100 * 100) + 1) + 100 * 99 + 100 )]
        [TestCase( "0.0.1-delta", false, ((8 * 100 * 100) + 1) + 2 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-epsilon", false, ((8 * 100 * 100) + 1) + 3 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-rc", false, ((8 * 100 * 100) + 1) + 7 * (100 * 99 + 100) )]
        [TestCase( "0.0.1-rc.99.99", false, ((8 * 100 * 100) + 1) + 7 * (100 * 99 + 100) + 100 * 99 + 99 )]
        [TestCase( "0.0.1", false, ((8 * 100 * 100) + 1) + 8 * 100 * 100 )]

        [TestCase( "99999.49999.9998", true, (8 * 100 * 100) + 1 )]
        [TestCase( "99999.49999.9999-prerelease", true, 2 * (100 * 99 + 100) )]
        [TestCase( "99999.49999.9999-prerelease.99.99", true, 100 * 99 + 100 + 1 )]
        [TestCase( "99999.49999.9999-rc", true, 100 * 99 + 100 )]
        [TestCase( "99999.49999.9999-rc.99.98", true, 2 )]
        [TestCase( "99999.49999.9999-rc.99.99", true, 1 )]
        [TestCase( "99999.49999.9999", true, 0 )]
        public void checking_extreme_version_ordering( string tag, bool atEnd, int expectedRank )
        {
            var t = ReleaseTagVersion.TryParse( tag );
            if( atEnd )
            {
                Assert.That( t.OrderedVersion - (ReleaseTagVersion.VeryLastVersion.OrderedVersion - expectedRank), Is.EqualTo( 0 ) );
            }
            else
            {
                Assert.That( t.OrderedVersion - (ReleaseTagVersion.VeryFirstVersion.OrderedVersion + expectedRank), Is.EqualTo( 0 ) );
            }
            var t2 = new ReleaseTagVersion( t.OrderedVersion );
            Assert.That( t2.ToString(), Is.EqualTo( t.ToString() ) );
            Assert.That( t.Equals( t2 ) );
        }


        [Test]
        public void checking_version_ordering()
        {
            var orderedTags = new[]
            {
                    "0.0.0-alpha",
                    "0.0.0-alpha.0.1",
                    "0.0.0-alpha.0.2",
                    "0.0.0-alpha.1",
                    "0.0.0-alpha.1.1",
                    "0.0.0-beta",
                    "0.0.0-beta.1",
                    "0.0.0-beta.1.1",
                    "0.0.0-gamma",
                    "0.0.0-gamma.0.1",
                    "0.0.0-gamma.50",
                    "0.0.0-gamma.50.20",
                    "0.0.0-thisisnonstandard",
                    "0.0.0-nonstandard.0.1",
                    "0.0.0-anothernonstandard.2",
                    "0.0.0-rc",
                    "0.0.0-rc.0.1",
                    "0.0.0-rc.2",
                    "0.0.0-rc.2.58",
                    "0.0.0-rc.3",
                    "0.0.0",
                    "0.0.1",
                    "0.0.2",
                    "1.0.0-alpha",
                    "1.0.0-alpha.1",
                    "1.0.0-alpha.2",
                    "1.0.0-alpha.2.1",
                    "1.0.0-alpha.3",
                    "1.0.0",
                    "99999.49999.0",
                    "99999.49999.9999-alpha.99",
                    "99999.49999.9999-alpha.99.99",
                    "99999.49999.9999-rc",
                    "99999.49999.9999-rc.0.1",
                    "99999.49999.9999"
                };
            var releasedTags = orderedTags
                                        .Select( ( tag, idx ) => new { Tag = tag, Index = idx, ReleasedTag = ReleaseTagVersion.TryParse( tag ) } )
                                        .Select( s => { Assert.That( s.ReleasedTag.IsValid, s.Tag ); return s; } );
            var orderedByFileVersion = releasedTags
                                        .OrderBy( s => s.ReleasedTag.OrderedVersion );
            var orderedByFileVersionParts = releasedTags
                                            .OrderBy( s => s.ReleasedTag.OrderedVersionMajor )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionMinor )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionBuild )
                                            .ThenBy( s => s.ReleasedTag.OrderedVersionRevision );

            Assert.That( orderedByFileVersion.Select( ( s, idx ) => s.Index - idx ).All( delta => delta == 0 ) );
            Assert.That( orderedByFileVersionParts.Select( ( s, idx ) => s.Index - idx ).All( delta => delta == 0 ) );
        }

        // A Major.0.0 can be reached from any major version below.
        // One can jump to any prerelease of it.
        [TestCase( "4.0.0, 4.0.0-alpha, 4.0.0-rc", true, "3.0.0, 3.5.44, 3.0.0-alpha, 3.49999.9999-rc.87, 3.0.3-rc.99.99, 3.0.3-alpha.54.99, 3.999.999" )]
        [TestCase( "4.1.0, 4.1.0-alpha, 4.1.0-rc", false, "3.0.0, 3.5.44, 3.0.0-alpha, 3.49999.9999-rc.87, 3.0.3-rc.99.99, 3.0.3-alpha.54.99, 3.999.999" )]

        // Same for a minor bump of 1.
        [TestCase( "4.3.0, 4.3.0-alpha, 4.3.0-rc", true, "4.2.0, 4.2.0-alpha, 4.2.44, 4.2.3-rc.87, 4.2.3-rc.99.99, 4.2.3-rc.5.8, 4.2.3-alpha, 4.2.3-alpha.54.99, 4.2.9999" )]
        [TestCase( "4.3.0, 4.3.0-rc", true, "4.3.0-alpha, 4.3.0-beta.99.99, 4.3.0-prerelease.99.99" )]

        // Patch differs: 
        [TestCase( "4.3.2", true, "4.3.1, 4.3.2-alpha, 4.3.2-rc, 4.3.2-rc.99.99" )]
        [TestCase( "4.3.2", false, "4.3.1-alpha, 4.3.1-rc, 4.3.1-rc.99.99" )]
        public void checking_some_versions_predecessors( string targets, bool previous, string candidates )
        {
            var targ = targets.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .Select( v => ReleaseTagVersion.TryParse( v ) );
            var prev = candidates.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .Select( v => ReleaseTagVersion.TryParse( v ) );
            foreach( var vTarget in targ )
            {
                foreach( var p in prev )
                {
                    Assert.That( vTarget.IsDirectPredecessor( p ), Is.EqualTo( previous ), p.ToString() + (previous ? " is a previous of " : " is NOT a previous of ") + vTarget.ToString() );
                }
            }
        }


        [TestCase( "v0.0.0-alpha", "v0.0.0-alpha.0.1" )]
        [TestCase( "v0.0.0-alpha.0.1", "v0.0.0-alpha.0.2" )]
        [TestCase( "v0.0.0-rc.99", "v0.0.0-rc.99.1" )]
        [TestCase( "v0.0.0-rc.1.99", "" )]
        [TestCase( "v0.0.0", "v0.0.1-alpha, v0.0.1-beta, v0.0.1-delta, v0.0.1-epsilon, v0.0.1-gamma, v0.0.1-kappa, v0.0.1-prerelease, v0.0.1-rc, v0.0.1" )]
        public void checking_next_fixes_and_predecessors( string start, string nextVersions )
        {
            var next = nextVersions.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .ToArray();
            var rStart = ReleaseTagVersion.TryParse( start );
            Assert.That( rStart != null && rStart.IsValid );
            // Checks successors (and that they are ordered).
            var cNext = rStart.GetDirectSuccessors( true ).Select( v => v.ToString() ).ToArray();
            CollectionAssert.AreEqual( next, cNext, start + " => " + string.Join( ", ", cNext ) );
            Assert.That( rStart.GetDirectSuccessors( true ), Is.Ordered );
            // For each successor, check that the start is a predecessor.
            foreach( var n in rStart.GetDirectSuccessors( true ) )
            {
                Assert.That( n.IsDirectPredecessor( rStart ), "{0} < {1}", rStart, n );
            }
        }


        [TestCase( 1, 2, 1000 )]
        [TestCase( -1, 2, 1000 ), Description( "Random seed version." )]
        public void randomized_checking_of_ordered_versions_mapping_and_extended_successors_and_predecessors( int seed, int count, int span )
        {
            Random r = seed >= 0 ? new Random( seed ) : new Random();
            while( --count > 0 )
            {
                long start = (long)decimal.Ceiling( r.NextDecimal() * (ReleaseTagVersion.VeryLastVersion.OrderedVersion + 1) + 1 );
                ReleaseTagVersion rStart = CheckMapping( start );
                Assert.That( rStart, Is.Not.Null );
                ReleaseTagVersion rCurrent;
                for( int i = 1; i < span; ++i )
                {
                    rCurrent = CheckMapping( start + i );
                    if( rCurrent == null ) break;
                    Assert.That( rStart < rCurrent );
                }
                for( int i = 1; i < span; ++i )
                {
                    rCurrent = CheckMapping( start - i );
                    if( rCurrent == null ) break;
                    Assert.That( rStart > rCurrent );
                }
            }
            //Console.WriteLine( "Greatest successors count = {0}.", _greatersuccessorCount );
        }

        //static int _greatersuccessorCount = 0;

        ReleaseTagVersion CheckMapping( long v )
        {
            if( v < 0 || v > ReleaseTagVersion.VeryLastVersion.OrderedVersion )
            {
                Assert.Throws<ArgumentException>( () => new ReleaseTagVersion( v ) );
                return null;
            }
            var t = new ReleaseTagVersion( v );
            Assert.That( (v == 0) == !t.IsValid );
            Assert.That( t.OrderedVersion, Is.EqualTo( v ) );
            var sSemVer = t.ToString( ReleaseTagFormat.SemVer );
            var tSemVer = ReleaseTagVersion.TryParse( sSemVer );
            var tNormalized = ReleaseTagVersion.TryParse( t.ToString( ReleaseTagFormat.Normalized ) );
            Assert.That( tSemVer.OrderedVersion, Is.EqualTo( v ) );
            Assert.That( tNormalized.OrderedVersion, Is.EqualTo( v ) );
            Assert.That( tNormalized.Equals( t ) );
            Assert.That( tSemVer.Equals( t ) );
            Assert.That( tNormalized.Equals( (object)t ) );
            Assert.That( tSemVer.Equals( (object)t ) );
            Assert.That( tNormalized.CompareTo( t ) == 0 );
            Assert.That( tSemVer == t );
            Assert.That( tSemVer.ToString(), Is.EqualTo( t.ToString() ) );
            Assert.That( tNormalized.ToString(), Is.EqualTo( t.ToString() ) );
            // Successors/Predecessors check.
            var vSemVer = SemVersion.Parse( sSemVer, true );
            int count = 0;
            foreach( var succ in t.GetDirectSuccessors( false ) )
            {
                ++count;
                Assert.That( succ.IsDirectPredecessor( t ) );
                var vSemVerSucc = SemVersion.Parse( succ.ToString( ReleaseTagFormat.SemVer ) );
                Assert.That( vSemVer < vSemVerSucc, "{0} < {1}", vSemVer, vSemVerSucc );
            }
            //if( count > _greatersuccessorCount )
            //{
            //    Console.WriteLine( " -> - found {0} successors for '{1}':", count, t );
            //    Console.WriteLine( "      " + string.Join( ", ", t.GetDirectSuccessors( false ).Select( s => s.ToString() ) ) );
            //    var closest = t.GetDirectSuccessors( true ).Select( s => s.ToString() ).ToList();
            //    Console.WriteLine( "    - {0} closest successors:", closest.Count, t );
            //    Console.WriteLine( "      " + string.Join( ", ", closest ) );
            //    _greatersuccessorCount = count;
            //}
            return t;
        }

        [Test]
        public void check_first_possible_versions()
        {
            string firstPossibleVersions = @"
                        v0.0.0-alpha, v0.0.0-beta, v0.0.0-delta, v0.0.0-epsilon, v0.0.0-gamma, v0.0.0-kappa, v0.0.0-prerelease, v0.0.0-rc, 
                        v0.0.0, 
                        v0.1.0-alpha, v0.1.0-beta, v0.1.0-delta, v0.1.0-epsilon, v0.1.0-gamma, v0.1.0-kappa, v0.1.0-prerelease, v0.1.0-rc, 
                        v0.1.0, 
                        v1.0.0-alpha, v1.0.0-beta, v1.0.0-delta, v1.0.0-epsilon, v1.0.0-gamma, v1.0.0-kappa, v1.0.0-prerelease, v1.0.0-rc, 
                        v1.0.0";
            var next = firstPossibleVersions.Split( ',' )
                                    .Select( v => v.Trim() )
                                    .Where( v => v.Length > 0 )
                                    .ToArray();
            CollectionAssert.AreEqual( next, ReleaseTagVersion.FirstPossibleVersions.Select( v => v.ToString() ).ToArray() );
        }

        [Test]
        public void operators_overloads()
        {
            // Two variables to avoid Compiler Warning (level 3) CS1718
            ReleaseTagVersion null2 = null;
            ReleaseTagVersion null1 = null;

            Assert.That( null1 == null2 );
            Assert.That( null1 >= null2 );
            Assert.That( null1 <= null2 );

            Assert.That( null1 != null2, Is.False );
            Assert.That( null1 > null2, Is.False );
            Assert.That( null1 < null2, Is.False );

            NullIsAlwaysSmaller( ReleaseTagVersion.VeryFirstVersion );
            NullIsAlwaysSmaller( ReleaseTagVersion.TryParse( "1.0.0" ) );
            NullIsAlwaysSmaller( ReleaseTagVersion.TryParse( "bug" ) );
        }

        private static void NullIsAlwaysSmaller( ReleaseTagVersion v )
        {
            Assert.That( null != v );
            Assert.That( null == v, Is.False );
            Assert.That( null >= v, Is.False );
            Assert.That( null <= v );
            Assert.That( null > v, Is.False );
            Assert.That( null < v );

            Assert.That( v != null );
            Assert.That( v == null, Is.False );
            Assert.That( v >= null );
            Assert.That( v <= null, Is.False );
            Assert.That( v > null );
            Assert.That( v < null, Is.False );
        }
    }
}


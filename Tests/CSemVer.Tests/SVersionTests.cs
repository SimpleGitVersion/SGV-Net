using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSemVer.Tests
{
    [TestFixture]
    public class SVersionTests
    {
        [Test]
        public void the_invalid_SVersion_is_syntaxically_valid_and_greater_than_null()
        {
            Assert.That( SVersion.Invalid.IsValidSyntax );
            Assert.That( SVersion.Invalid > null );
            Assert.That( null < SVersion.Invalid );
            Assert.That( SVersion.Invalid >= null );
            Assert.That( null <= SVersion.Invalid );
        }

        [TestCase( "0.0.0" )]
        [TestCase( "0.0.0--" )]
        [TestCase( "0.0.0-a" )]
        [TestCase( "0.0.0-A" )]
        public void the_invalid_SVersion_is_lower_than_any_other_syntaxically_valid_SVersion(string version)
        {
            var v = SVersion.TryParse( version );
            Assert.That( v.IsValidSyntax );
            Assert.That( v > SVersion.Invalid );
            Assert.That( v != SVersion.Invalid );
        }

        [Test]
        public void SVersion_can_be_compared_with_operators()
        {
            Assert.That( new SVersion( 0, 0, 0 ) > new SVersion( 0, 0, 0, "a" ) );
            Assert.That( new SVersion( 0, 0, 0 ) >= new SVersion( 0, 0, 0, "a" ) );
            Assert.That( new SVersion( 0, 0, 0, "a" ) < new SVersion( 0, 0, 0 ) );
            Assert.That( new SVersion( 0, 0, 0, "a" ) <= new SVersion( 0, 0, 0 ) );
            Assert.That( new SVersion( 0, 0, 0, "a" ) != new SVersion( 0, 0, 0 ) );
        }

        [TestCase( "01.0.0" )]
        [TestCase( "0.01.0" )]
        [TestCase( "0.0.01" )]
        [TestCase( "12897798127391372937.0.0" )]
        [TestCase( "1.999999999999999999.0" )]
        [TestCase( "1.2.99999999999999999999999" )]
        [TestCase( "0.0" )]
        [TestCase( "0" )]
        [TestCase( null )]
        [TestCase( "not a version at all" )]
        [TestCase( "0.0.0-+" )]
        [TestCase( "0.0.0-." )]
        [TestCase( "0.0.0-.." )]
        [TestCase( "0.0.0-a..b" )]
        [TestCase( "0.0.0-01" )]
        [TestCase( "0.0.0-$" )]
        public void Syntaxically_invalid_SVersion_are_greater_than_null_and_lower_than_the_Invalid_one(string invalid)
        {
            SVersion notV = SVersion.TryParse( invalid );
            Assert.That( !notV.IsValidSyntax );
            Assert.That( notV != SVersion.Invalid );
            Assert.That( SVersion.Invalid > notV );
            Assert.That( SVersion.Invalid >= notV );
        }
    }
}
